using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// Builds the storage tree from the records of a volume's $MFT (record 5 is the root), kept in an <see cref="MftTable"/>.
/// Extension records are folded into their base record by its record number, not only through the attribute list: a
/// non-resident $ATTRIBUTE_LIST (a file with many hard links has one) names no extension record, yet the file's names and
/// data sit in them. A file is one entry under one name (a Win32 name before a POSIX link before a DOS short name), so a
/// file with several hard links counts once. A parent or base reference counts only when its sequence number matches the
/// record's own: the volume can change while it is read, and a reused record must not collect another file's children or
/// parts. NTFS's own files (records 0 to 23 and everything below $Extend) are left out; records whose parent chain does
/// not reach the root count as lost.
/// </summary>
public static class MftTreeBuilder
{
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    private const int SequenceShift = 48;
    private const int RootRecord = 5;
    /// <summary>Records 0 to 15 are NTFS metadata files and 16 to 23 are reserved; user files start at 24.</summary>
    private const int FirstUserRecord = 24;
    private const uint AttrSystem = 0x4, AttrReparse = 0x400, AttrOffline = 0x1000, AttrRecallOnOpen = 0x40000,
        AttrRecallOnDataAccess = 0x400000;

    /// <summary>The tree of records parsed one by one (the tests and the fixture).</summary>
    public static DiskTree Build(IReadOnlyList<MftRecord> records, string rootPath, TimeSpan elapsed) =>
        Build(MftTable.From(records), rootPath, null, elapsed);

    /// <summary>
    /// The tree of <paramref name="table"/>, whose flags it changes (one build per table). With <paramref name="watch"/>
    /// the tree's time is read when it is done, so it includes the build; otherwise it is <paramref name="elapsed"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static DiskTree Build(MftTable table, string rootPath, Stopwatch? watch, TimeSpan elapsed = default)
    {
        var t = table;
        FoldExtensions(t);
        var count = t.Count;
        var flags = t.Flags;
        var number = t.Number;

        // Children as linked lists in record order (entries are in record order, filled backwards), each entry under the
        // parent of its chosen name.
        var firstChild = new int[count];
        var nextSibling = new int[count];
        Array.Fill(firstChild, -1);
        var expected = 1;
        for (var i = count - 1; i >= 0; i--)
        {
            if (number[i] == RootRecord || t.Name[i] is null) continue;
            var reference = t.Parent[i];
            var p = t.IndexOf(reference & RecordNumberMask);
            if (p < 0 || (flags[p] & MftTable.Folder) == 0 || (ushort)(reference >> SequenceShift) != t.Sequence[p]) continue;
            nextSibling[i] = firstChild[p];
            firstChild[p] = i;
            if (number[i] >= FirstUserRecord) expected++;
        }

        // Depth first from the root. An entry is reached at most once, so damaged parent references cannot loop; below
        // a metadata record (builder index -1) entries are only marked as reached.
        var builder = new DiskTree.Builder(rootPath, expected);
        var stack = new Stack<(int Entry, int Index)>();
        if (t.IndexOf(RootRecord) is var root and >= 0 && (flags[root] & MftTable.Folder) != 0)
        {
            flags[root] |= MftTable.Reached;
            stack.Push((root, builder.Root));
        }
        while (stack.Count > 0)
        {
            var (folder, at) = stack.Pop();
            for (var c = firstChild[folder]; c >= 0; c = nextSibling[c])
            {
                if ((flags[c] & MftTable.Reached) != 0) continue;
                flags[c] |= MftTable.Reached;
                var isFolder = (flags[c] & MftTable.Folder) != 0;
                if (at < 0 || number[c] < FirstUserRecord)
                {
                    if (isFolder) stack.Push((c, -1));
                    continue;
                }

                var a = t.Attributes[c];
                var entry = EntryFlags.None;
                if ((a & AttrSystem) != 0) entry |= EntryFlags.System;
                if ((a & (AttrOffline | AttrRecallOnOpen | AttrRecallOnDataAccess)) != 0) entry |= EntryFlags.CloudOnly;
                var lastWrite = MftTable.Time(t.LastWrite[c]);
                if (isFolder)
                {
                    // A link's target is elsewhere or already counted. Other reparse folders, like OneDrive's cloud
                    // folders, hold real children. Without a readable tag the folder is entered, as EnumerationScanner
                    // does, so both readers agree.
                    var link = (a & AttrReparse) != 0 && (flags[c] & MftTable.LinkTag) != 0;
                    var i = builder.AddFolder(at, t.Name[c]!, lastWrite, entry | (link ? EntryFlags.ReparsePoint : 0));
                    if (!link) stack.Push((c, i));
                }
                else
                {
                    // No unnamed $DATA found: nothing to count, and one odd record must not make every total unknown.
                    builder.AddFile(at, t.Name[c]!, t.Size[c], t.Allocated[c], lastWrite, entry);
                }
            }
        }

        var lost = 0;
        for (var i = 0; i < count; i++)
            if (number[i] >= FirstUserRecord && (flags[i] & MftTable.Reached) == 0) lost++;
        return watch is null ? builder.Build(ScanReader.FileTable, elapsed, lost) : builder.Build(ScanReader.FileTable, watch, lost);
    }

    /// <summary>
    /// Folds each extension record, in record order, into its base: names that rank higher than the base's best, and the
    /// data size and reparse tag when the base has none. Its base must be in use with the sequence number it names.
    /// </summary>
    private static void FoldExtensions(MftTable t)
    {
        var list = t.Extensions;
        list.Sort((x, y) => x.Number.CompareTo(y.Number));
        foreach (var r in list)
        {
            var b = t.IndexOf(r.BaseReference & RecordNumberMask);
            // Its base is not in use, or the record was reused since: nothing to add to.
            if (b < 0 || (ushort)(r.BaseReference >> SequenceShift) != t.Sequence[b]) continue;
            foreach (var name in r.Names) t.OfferName(b, name);
            if ((t.Flags[b] & MftTable.HasData) == 0 && r.DataSize is { } size)
            {
                t.Size[b] = size;
                t.Allocated[b] = r.DataAllocated ?? -1;
                t.Flags[b] |= MftTable.HasData;
            }
            if ((t.Flags[b] & MftTable.HasTag) == 0) t.Flags[b] |= MftTable.TagFlags(r.ReparseTag);
        }
    }
}
