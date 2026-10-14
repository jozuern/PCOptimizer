namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// Builds the storage tree from the parsed records of a volume's $MFT (record 5 is the root). Extension records are
/// folded into their base record by its record number, not only through the attribute list: a non-resident
/// $ATTRIBUTE_LIST (a file with many hard links has one) names no extension record, yet the file's names and data sit in
/// them. A file is one entry under its first name in record order that is not a DOS short name, so a file with several
/// hard links counts once. NTFS's own files (records 0 to 23 and everything below $Extend) are left out; records whose
/// parent chain does not reach the root count as lost.
/// </summary>
public static class MftTreeBuilder
{
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    private const int RootRecord = 5;
    /// <summary>Records 0 to 15 are NTFS metadata files and 16 to 23 are reserved; user files start at 24.</summary>
    private const int FirstUserRecord = 24;
    private const byte DosNamespace = 2;
    private const uint AttrSystem = 0x4, AttrReparse = 0x400, AttrOffline = 0x1000, AttrRecallOnOpen = 0x40000,
        AttrRecallOnDataAccess = 0x400000;
    /// <summary>Reparse tag bit of links (junction, mount point, symlink), as <see cref="EnumerationScanner"/> tests it.</summary>
    private const uint NameSurrogate = 0x2000_0000;

    /// <summary>What a base record's extension records add to it.</summary>
    private sealed class Extension
    {
        public readonly List<MftName> Names = [];
        public long? DataSize, DataAllocated;
        public uint ReparseTag;
    }

    public static DiskTree Build(IReadOnlyList<MftRecord> records, string rootPath, TimeSpan elapsed)
    {
        var count = 0;
        foreach (var r in records)
            if (r.Number < int.MaxValue) count = Math.Max(count, (int)r.Number + 1);

        var bases = new MftRecord?[count];
        foreach (var r in records)
            if (r.BaseReference == 0 && r.Number < count) bases[r.Number] = r;

        // In record order, so the names of extension records follow the base's own names in a fixed order.
        var extensions = new Dictionary<int, Extension>();
        foreach (var r in records.Where(r => r.BaseReference != 0).OrderBy(r => r.Number))
        {
            var b = r.BaseReference & RecordNumberMask;
            if (b >= (ulong)count || bases[b] is null) continue; // its base is not in use: nothing to add to
            if (!extensions.TryGetValue((int)b, out var e)) extensions.Add((int)b, e = new Extension());
            e.Names.AddRange(r.Names);
            if (e.DataSize is null && r.DataSize is not null) (e.DataSize, e.DataAllocated) = (r.DataSize, r.DataAllocated);
            if (e.ReparseTag == 0) e.ReparseTag = r.ReparseTag;
        }

        // Children as linked lists in record order (filled backwards), each record under the parent of its chosen name.
        var name = new string?[count];
        var firstChild = new int[count];
        var nextSibling = new int[count];
        Array.Fill(firstChild, -1);
        for (var n = count - 1; n >= 0; n--)
        {
            if (n == RootRecord || bases[n] is not { } r) continue;
            if (Primary(r, extensions.GetValueOrDefault(n)) is not { } chosen) continue;
            var parent = chosen.ParentReference & RecordNumberMask;
            if (parent >= (ulong)count || bases[parent] is not { IsFolder: true }) continue;
            name[n] = chosen.Name;
            nextSibling[n] = firstChild[parent];
            firstChild[parent] = n;
        }

        // Depth first from the root. A record is reached at most once, so damaged parent references cannot loop; below
        // a metadata record (builder index -1) records are only marked as reached.
        var builder = new DiskTree.Builder(rootPath);
        var reached = new bool[count];
        var stack = new Stack<(int Record, int Index)>();
        if (count > RootRecord && bases[RootRecord] is { IsFolder: true })
        {
            reached[RootRecord] = true;
            stack.Push((RootRecord, builder.Root));
        }
        while (stack.Count > 0)
        {
            var (folder, at) = stack.Pop();
            for (var c = firstChild[folder]; c >= 0; c = nextSibling[c])
            {
                if (reached[c]) continue;
                reached[c] = true;
                var r = bases[c]!;
                if (at < 0 || c < FirstUserRecord)
                {
                    if (r.IsFolder) stack.Push((c, -1));
                    continue;
                }

                var e = extensions.GetValueOrDefault(c);
                var a = r.FileAttributes;
                var flags = EntryFlags.None;
                if ((a & AttrSystem) != 0) flags |= EntryFlags.System;
                if ((a & (AttrOffline | AttrRecallOnOpen | AttrRecallOnDataAccess)) != 0) flags |= EntryFlags.CloudOnly;
                if (r.IsFolder)
                {
                    // A link's target is elsewhere or already counted. Other reparse folders, like OneDrive's cloud
                    // folders, hold real children. Without a readable tag the folder is taken for a link.
                    var tag = r.ReparseTag != 0 ? r.ReparseTag : e?.ReparseTag ?? 0;
                    var link = (a & AttrReparse) != 0 && (tag == 0 || (tag & NameSurrogate) != 0);
                    var i = builder.AddFolder(at, name[c]!, r.LastWriteUtc, flags | (link ? EntryFlags.ReparsePoint : 0));
                    if (!link) stack.Push((c, i));
                }
                else
                {
                    var (size, allocated) = r.DataSize is not null ? (r.DataSize, r.DataAllocated) : (e?.DataSize, e?.DataAllocated);
                    builder.AddFile(at, name[c]!, size ?? 0, allocated ?? -1, r.LastWriteUtc, flags);
                }
            }
        }

        var lost = 0;
        for (var n = FirstUserRecord; n < count; n++)
            if (bases[n] is not null && !reached[n]) lost++;
        return builder.Build(ScanReader.FileTable, elapsed, lost);
    }

    /// <summary>The first name that is not a DOS short name, base record first; a DOS name only when there is no other.</summary>
    private static MftName? Primary(MftRecord r, Extension? e)
    {
        MftName? dos = null;
        foreach (var n in r.Names)
        {
            if (n.Namespace != DosNamespace) return n;
            dos ??= n;
        }
        foreach (var n in e?.Names ?? [])
        {
            if (n.Namespace != DosNamespace) return n;
            dos ??= n;
        }
        return dos;
    }
}
