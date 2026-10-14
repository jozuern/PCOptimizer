using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
}

/// <summary>
/// Reads a whole NTFS volume from its file table: every record of $MFT's valid data, read only, in blocks of up to 8 MB
/// into memory aligned for reads without the cache, so they need no copy (in the test VM its 550 MB took 0.35 s in
/// blocks of 8 MB and 0.39 s in blocks of 4 MB). The next block is read while the last one is parsed on all processors straight into an
/// <see cref="MftTable"/>, then the tree comes from <see cref="MftTreeBuilder"/>. The time the tree reports includes
/// building it. Needs administrator rights. No record is skipped, not even one $MFT's own bitmap marks free: a wrong
/// bitmap could hide files no check would notice. Any doubt that the table was read whole (it cannot be opened or
/// followed, a read fails, more than 1 % of the records in use are damaged, the root's record is not in use) throws
/// <see cref="MftUnavailableException"/>, and <see cref="VolumeScanners"/> reads folder by folder instead: a tree is either
/// the whole file table or not made.
/// </summary>
public sealed class MftScanner : IVolumeScanner
{
    private const int BlockBytes = 8 << 20;
    /// <summary>
    /// Block memory starts on a page, which reads without the cache accept on sectors of up to 4 KB; on larger ones
    /// <see cref="VolumeMftSource"/> reads through its own buffer.
    /// </summary>
    private const int Alignment = 4096;
    private const ushort InUse = 0x0001;
    private const int RootRecord = 5;
    /// <summary>"FILE" and "BAAD" (a record NTFS found damaged), little endian.</summary>
    private const uint File = 0x454C4946, Baad = 0x44414142;
    /// <summary>What a record of a block is, before it is parsed: an entry index, or one of these.</summary>
    private const int Skip = -1, Extension = -2;
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
    /// Every record of <paramref name="source"/> in a table. Each block is first sorted on this thread (record headers
    /// only: unused, damaged, extension, or a base record given its entry in record order), then parsed by up to
    /// <paramref name="workers"/> threads (0: one per processor; 1: in order on this thread), each record into its own
    /// entry. Throws <see cref="MftUnavailableException"/> for damage over 1 % or a missing root.
    /// </summary>
    internal static MftTable ReadTable(IMftSource source, int blockBytes, IProgress<ScanProgress>? progress, CancellationToken ct,
        int workers = 0)
    {
        int size = source.BytesPerRecord, sector = source.BytesPerSector;
        var count = source.RecordCount;
        // The table and the builder index records by number in arrays.
        if (size < 512 || count <= 0 || count >= int.MaxValue) throw new MftUnavailableException($"unexpected file table: {count} records of {size} bytes");

        var perBlock = Math.Max(1, blockBytes / size);
        // Freed when this method ends, after the finally below has waited for a read still running into one of them.
        using var one = new AlignedBlock(perBlock * size);
        using var other = new AlignedBlock(perBlock * size);
        var (current, next) = (one, other);
        var entries = new int[perBlock];
        var table = new MftTable((int)count);
        var parse = new BlockParse(table, entries, size, sector, workers > 0 ? workers : Environment.ProcessorCount);
        long inUse = 0, damaged = 0;
        (long First, int Count) Next(long from) => from >= count ? (count, 0) : (from, (int)Math.Min(perBlock, count - from));

        // One read ahead: the next block is read while this one is parsed.
        var (first, want) = Next(0);
        var pending = ReadAhead(source, first, current, want);
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

                var (used, bad) = Sort(current, read, size, start, table, entries);
                inUse += used;
                damaged += bad;
                damaged += parse.Run(current, read, start);
                (current, next) = (next, current);
                progress?.Report(new ScanProgress(pending is null ? 100 : (start + read) * 100.0 / count, (int)parse.Files, parse.Bytes, ScanReader.FileTable));
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
        // Without the root (record 5) nothing could be placed: a table read wrong, never an empty drive.
        if (table.IndexOf(RootRecord) is var root && (root < 0 || (table.Flags[root] & MftTable.Folder) == 0))
            throw new MftUnavailableException("the root folder's record is not in use");
        // Parsed in parallel, extension records arrive in any order.
        table.Extensions.Sort((x, y) => x.Number.CompareTo(y.Number));
        return table;
    }

    /// <summary>
    /// What each record of a block is, from its header alone (the signature, the in-use flag and the base reference lie
    /// before the first stride's end, so they read the same before the fixups): records not in use are skipped silently,
    /// "BAAD" counts as damaged, and every base record in use gets its entry here, in record order. A record whose first
    /// four bytes are neither "FILE" nor "BAAD" nor zero is garbage (NTFS formats every record it adds to the table as
    /// "FILE", and one never written reads as zeros, as the VM's fixture shows: only "FILE" and zero records): bytes from
    /// the wrong place, such as a run of the table mapped wrong. It counts as in use and damaged, so the 1 % limit
    /// applies to it. Returns the records in use and the damaged ones.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static (long InUse, long Damaged) Sort(AlignedBlock block, int read, int size, long start, MftTable table, int[] entries)
    {
        long inUse = 0, damaged = 0;
        var records = block.Span;
        for (var k = 0; k < read; k++)
        {
            var r = records.Slice(k * size, size);
            var signature = BinaryPrimitives.ReadUInt32LittleEndian(r);
            entries[k] = Skip;
            if (signature == Baad)
            {
                inUse++;
                damaged++;
            }
            else if (signature == File)
            {
                if ((BinaryPrimitives.ReadUInt16LittleEndian(r[0x16..]) & InUse) == 0) continue;
                inUse++;
                entries[k] = MftRecordParser.IsExtension(r) ? Extension : table.Reserve((uint)(start + k));
            }
            else if (signature != 0)
            {
                inUse++;
                damaged++;
            }
        }
        return (inUse, damaged);
    }

    /// <summary>
    /// Parses the records of one block <see cref="Sort"/> has sorted, in slices on up to <c>workers</c> threads: each
    /// record writes only its own entry, extension records go into the table's locked list. Counts the files and their
    /// bytes so far for the progress.
    /// </summary>
    private sealed class BlockParse(MftTable table, int[] entries, int size, int sector, int workers)
    {
        private readonly ParallelOptions _options = new() { MaxDegreeOfParallelism = workers };
        private AlignedBlock? _block;
        private long _start, _damaged;
        public long Files, Bytes;

        /// <summary>The damaged records found in the block.</summary>
        public long Run(AlignedBlock block, int read, long start)
        {
            _damaged = 0;
            if (workers == 1)
            {
                Slice(block, 0, read, start);
                return _damaged;
            }
            (_block, _start) = (block, start);
            // About four slices per thread, so a thread that meets long records does not hold up the block.
            var slice = Math.Max(16, (read + workers * 4 - 1) / (workers * 4));
            var slices = (read + slice - 1) / slice;
            Parallel.For(0, slices, _options, s => Slice(_block!, s * slice, Math.Min(read, (s + 1) * slice), _start));
            return _damaged;
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Slice(AlignedBlock block, int from, int to, long start)
        {
            long files = 0, bytes = 0, damaged = 0;
            var records = block.Span;
            for (var k = from; k < to; k++)
            {
                var entry = entries[k];
                if (entry == Skip) continue;
                var r = records.Slice(k * size, size);
                var number = (uint)(start + k);
                if (entry == Extension)
                {
                    // The fixups are applied in place: each record is parsed once, from this fresh read.
                    if (MftRecordParser.Parse(r, sector, number) is { } extension) table.AddExtension(extension);
                    else damaged++;
                    continue;
                }
                // In use by its header, then failing to parse: damaged.
                if (!MftRecordParser.ParseInto(r, sector, number, table, entry))
                {
                    table.Release(number);
                    damaged++;
                    continue;
                }
                if ((table.Flags[entry] & MftTable.Folder) == 0)
                {
                    files++;
                    bytes += table.Size[entry];
                }
            }
            Interlocked.Add(ref _damaged, damaged);
            Interlocked.Add(ref Files, files);
            Interlocked.Add(ref Bytes, bytes);
        }
    }

    private static Task<int> ReadAhead(IMftSource source, long first, AlignedBlock buffer, int count)
    {
        var size = source.BytesPerRecord;
        return Task.Run(() => source.Read(first, buffer.Span[..(count * size)]));
    }

    /// <summary>
    /// Native memory for one block, aligned so the volume can be read into it without the cache and without a copy (see
    /// <see cref="VolumeMftSource"/>), and freed at once instead of left to the garbage collector.
    /// </summary>
    private sealed unsafe class AlignedBlock(int length) : IDisposable
    {
        private byte* _memory = (byte*)NativeMemory.AlignedAlloc((nuint)length, Alignment);

        public Span<byte> Span => new(_memory, length);

        public void Dispose()
        {
            NativeMemory.AlignedFree(_memory);
            _memory = null;
        }
    }
}
