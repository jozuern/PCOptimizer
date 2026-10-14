using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

    /// <summary>Every record of <paramref name="source"/> in a table; throws when more than 1 % of the records in use are damaged.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    internal static MftTable ReadTable(IMftSource source, int blockBytes, IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        int size = source.BytesPerRecord, sector = source.BytesPerSector;
        var count = source.RecordCount;
        // The table and the builder index records by number in arrays.
        if (size < 512 || count <= 0 || count >= int.MaxValue) throw new MftUnavailableException($"unexpected file table: {count} records of {size} bytes");

        var perBlock = Math.Max(1, blockBytes / size);
        var current = new byte[perBlock * size];
        var next = new byte[perBlock * size];
        var table = new MftTable((int)count, source.InUseRecords);
        long inUse = 0, damaged = 0, bytes = 0;
        var files = 0;
        // One read ahead: the next run is read while this one is parsed.
        var (first, want) = source.NextRun(0, perBlock);
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
                (first, want) = source.NextRun(start + read, perBlock);
                if (want > 0) pending = ReadAhead(source, first, next, want);

                for (var k = 0; k < read; k++)
                {
                    var r = current.AsSpan(k * size, size);
                    // The signature and the in-use flag lie before the first stride's end, so they are readable before the
                    // fixups: a record in use that then fails to parse is damaged, an unused one is simply skipped.
                    var signature = BinaryPrimitives.ReadUInt32LittleEndian(r);
                    if (signature == Baad) { inUse++; damaged++; continue; }
                    if (signature != File || (BinaryPrimitives.ReadUInt16LittleEndian(r[0x16..]) & InUse) == 0) continue;
                    inUse++;
                    // The fixups are applied in place: each record is parsed once, from this fresh read.
                    var number = start + k;
                    if (!MftRecordParser.ParseInto(r, sector, (uint)number, table)) { damaged++; continue; }
                    if (table.IndexOf((ulong)number) is var i and >= 0 && (table.Flags[i] & MftTable.Folder) == 0)
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

        if (damaged * 100 > inUse) throw new MftUnavailableException($"{damaged} of {inUse} file records in use are damaged");
        return table;
    }

    private static Task<int> ReadAhead(IMftSource source, long first, byte[] buffer, int count)
    {
        var size = source.BytesPerRecord;
        return Task.Run(() => source.Read(first, buffer.AsSpan(0, count * size)));
    }
}
