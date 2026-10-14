using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// The records of a volume's $MFT, read in order. <see cref="Read"/> fills <c>buffer</c> with as many whole records from
/// <c>firstRecord</c> on as fit and returns how many; it throws <see cref="MftUnavailableException"/> when a read fails.
/// One read at a time, not always from the same thread.
/// </summary>
internal interface IMftSource : IDisposable
{
    int BytesPerRecord { get; }
    int BytesPerSector { get; }
    long RecordCount { get; }
    int Read(long firstRecord, Span<byte> buffer);

    /// <summary>
    /// The next records worth reading at or after <paramref name="from"/>, at most <paramref name="max"/>; a count of 0
    /// when none is left. A source that knows which records are free (the volume's $MFT bitmap) skips them.
    /// </summary>
    (long First, int Count) NextRun(long from, int max) =>
        from >= RecordCount ? (RecordCount, 0) : (from, (int)Math.Min(max, RecordCount - from));

    /// <summary>How many records are in use, when the source knows (the volume's $MFT bitmap); null otherwise.</summary>
    long? InUseRecords => null;

    /// <summary>
    /// Whether the source's bitmap marks <paramref name="record"/> in use; null when the source has no bitmap or the
    /// record lies past its end.
    /// </summary>
    bool? MarkedInUse(long record) => null;

    /// <summary>Whether <see cref="NextRun"/> skips records its bitmap marks free.</summary>
    bool HasBitmap => false;

    /// <summary>Bytes in use on the volume (clusters not free), when known.</summary>
    long? UsedBytes => null;
}

/// <summary>
/// Reads a whole NTFS volume from its file table: the records of $MFT that its bitmap marks in use, in blocks of up to
/// 2 MB (in the test VM within 0.03 s of 4 MB blocks, with 4 MB less memory), read only, the next block read
/// while the last one is parsed, straight into an <see cref="MftTable"/>, then the tree from
/// <see cref="MftTreeBuilder"/>. The time the tree reports includes building it. Needs administrator rights. Any doubt
/// that the table was read whole (it cannot be opened
/// or followed, a read fails, more than 1 % of the records in use are damaged) throws
/// <see cref="MftUnavailableException"/>, and <see cref="VolumeScanners"/> reads folder by folder instead: a tree is
/// either the whole file table or not made.
/// </summary>
public sealed class MftScanner : IVolumeScanner
{
    private const int BlockBytes = 2 << 20;
    private const ushort InUse = 0x0001;
    private const int RootRecord = 5, MismatchesTolerated = 16;
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    /// <summary>"FILE" and "BAAD" (a record NTFS found damaged), little endian.</summary>
    private const uint File = 0x454C4946, Baad = 0x44414142;
    private readonly Func<string, IMftSource> _open;
    private readonly int _blockBytes;

    public MftScanner() : this(VolumeMftSource.Open) { }

    internal MftScanner(Func<string, IMftSource> open, int blockBytes = BlockBytes) => (_open, _blockBytes) = (open, blockBytes);

    public Task<DiskTree> ScanAsync(string root, IProgress<ScanProgress>? progress, CancellationToken ct) =>
        Task.Run(() => Scan(root, progress, ct), ct);

    private DiskTree Scan(string root, IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var watch = Stopwatch.StartNew();
        MftTable table;
        using (var source = _open(root)) table = ReadTable(source, _blockBytes, progress, ct);
        ct.ThrowIfCancellationRequested();
        return MftTreeBuilder.Build(table, root, watch);
    }

    /// <summary>
    /// Every record of <paramref name="source"/> in a table. With a bitmap, only the records it marks in use are read, and
    /// the result is checked against the data: records read whose header disagrees with their bit, a record whose
    /// folder the bitmap marks free, files that add up to less than half the volume's used space, damage, or no root
    /// mean the bitmap is not trusted, and the whole table is read again without it (logged). Only that full read, or a
    /// source without a bitmap, throws <see cref="MftUnavailableException"/> for damage or a missing root.
    /// </summary>
    internal static MftTable ReadTable(IMftSource source, int blockBytes, IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        if (source.HasBitmap)
        {
            var (table, doubt) = Read(source, blockBytes, progress, useBitmap: true, ct);
            if (doubt is null) return table;
            Log.Warn("storage", "file table bitmap not trusted, reading every record", new { reason = doubt });
        }
        var (full, _) = Read(source, blockBytes, progress, useBitmap: false, ct);
        return full;
    }

    /// <summary>One pass over the table; with <paramref name="useBitmap"/> a failed check is returned as the doubt instead of thrown.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static (MftTable Table, string? Doubt) Read(IMftSource source, int blockBytes, IProgress<ScanProgress>? progress,
        bool useBitmap, CancellationToken ct)
    {
        int size = source.BytesPerRecord, sector = source.BytesPerSector;
        var count = source.RecordCount;
        // The table and the builder index records by number in arrays.
        if (size < 512 || count <= 0 || count >= int.MaxValue) throw new MftUnavailableException($"unexpected file table: {count} records of {size} bytes");

        var perBlock = Math.Max(1, blockBytes / size);
        var current = new byte[perBlock * size];
        var next = new byte[perBlock * size];
        var table = new MftTable((int)count, useBitmap ? source.InUseRecords : null);
        long inUse = 0, damaged = 0, bytes = 0, readRecords = 0, mismatched = 0, parentsMarkedFree = 0;
        var files = 0;
        (long First, int Count) Next(long from) =>
            useBitmap ? source.NextRun(from, perBlock) : from >= count ? (count, 0) : (from, (int)Math.Min(perBlock, count - from));

        // One read ahead: the next run is read while this one is parsed.
        var (first, want) = Next(0);
        var pending = want > 0 ? ReadAhead(source, first, current, want) : null;
        try
        {
            while (pending is not null)
            {
                var read = pending.GetAwaiter().GetResult();
                pending = null;
                ct.ThrowIfCancellationRequested();
                if (read <= 0 || read > want) throw new MftUnavailableException($"the file table ended after {first} of {count} records");
                var start = first;
                (first, want) = Next(start + read);
                if (want > 0) pending = ReadAhead(source, first, next, want);

                readRecords += read;
                for (var k = 0; k < read; k++)
                {
                    var r = current.AsSpan(k * size, size);
                    var number = start + k;
                    // The signature and the in-use flag lie before the first stride's end, so they are readable before the
                    // fixups: a record in use that then fails to parse is damaged, an unused one is simply skipped.
                    var signature = BinaryPrimitives.ReadUInt32LittleEndian(r);
                    var headerInUse = signature == Baad
                        || (signature == File && (BinaryPrimitives.ReadUInt16LittleEndian(r[0x16..]) & InUse) != 0);
                    // A bitmap that disagrees with the records read may also have hidden records in the stretches skipped.
                    if (useBitmap && source.MarkedInUse(number) is { } marked && marked != headerInUse) mismatched++;
                    if (!headerInUse) continue;
                    inUse++;
                    if (signature == Baad) { damaged++; continue; }
                    // The fixups are applied in place: each record is parsed once, from this fresh read.
                    if (!MftRecordParser.ParseInto(r, sector, (uint)number, table)) { damaged++; continue; }
                    if (table.IndexOf((ulong)number) is not (var i and >= 0)) continue;
                    // A folder the bitmap marks free was not read: the bitmap hides at least that record.
                    if (useBitmap && table.Name[i] is not null && source.MarkedInUse((long)(table.Parent[i] & RecordNumberMask)) == false)
                        parentsMarkedFree++;
                    if ((table.Flags[i] & MftTable.Folder) == 0)
                    {
                        files++;
                        bytes += table.Size[i];
                    }
                }
                (current, next) = (next, current);
                progress?.Report(new ScanProgress(pending is null ? 100 : (start + read) * 100.0 / count, files, bytes, ScanReader.FileTable));
            }
        }
        finally
        {
            // A read still running uses the source: it must end before the source is closed. Its own failure no longer
            // matters, the reason this method ends is already on its way out.
            if (pending is not null)
                try { pending.GetAwaiter().GetResult(); }
                catch (Exception) { }
        }

        string? doubt = null;
        if (useBitmap)
        {
            // A few disagreements are files made or removed while the table is read; more mean a bitmap not to be trusted.
            if (mismatched > Math.Max(MismatchesTolerated, readRecords / 100))
                doubt = $"the bitmap disagrees with {mismatched} of {readRecords} records read";
            else if (parentsMarkedFree > 0)
                doubt = $"{parentsMarkedFree} records read lie in folders the bitmap marks free";
            else if (source.UsedBytes is { } used && Allocated(table) * 2 < used)
                doubt = $"the records read hold {Allocated(table)} of {used} bytes in use on the volume";
        }
        if (doubt is null && damaged * 100 > inUse) doubt = $"{damaged} of {inUse} file records in use are damaged";
        // Without the root (record 5) nothing could be placed: a table read wrong, never an empty drive.
        if (doubt is null && table.IndexOf(RootRecord) is var root && (root < 0 || (table.Flags[root] & MftTable.Folder) == 0))
            doubt = "the root folder's record is not in use";
        if (doubt is not null && !useBitmap) throw new MftUnavailableException(doubt);
        return (table, doubt);
    }

    /// <summary>The space the files found take: unnamed $DATA of every record, NTFS's own files included.</summary>
    private static long Allocated(MftTable table)
    {
        long total = 0;
        for (var i = 0; i < table.Count; i++)
            if ((table.Flags[i] & MftTable.HasData) != 0 && table.Allocated[i] > 0) total += table.Allocated[i];
        foreach (var r in table.Extensions)
            if (r.DataSize is not null && r.DataAllocated is > 0) total += r.DataAllocated.Value;
        return total;
    }

    private static Task<int> ReadAhead(IMftSource source, long first, byte[] buffer, int count)
    {
        var size = source.BytesPerRecord;
        return Task.Run(() => source.Read(first, buffer.AsSpan(0, count * size)));
    }
}
