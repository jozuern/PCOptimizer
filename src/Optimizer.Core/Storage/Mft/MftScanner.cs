using System.Buffers.Binary;
using System.Diagnostics;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// The records of a volume's $MFT, read in order. <see cref="Read"/> fills <c>buffer</c> with as many whole records from
/// <c>firstRecord</c> on as fit and returns how many; it throws <see cref="MftUnavailableException"/> when a read fails.
/// </summary>
internal interface IMftSource : IDisposable
{
    int BytesPerRecord { get; }
    int BytesPerSector { get; }
    long RecordCount { get; }
    int Read(long firstRecord, Span<byte> buffer);
}

/// <summary>
/// Reads a whole NTFS volume from its file table: every record of $MFT in 4 MB blocks, read only, then the tree from
/// <see cref="MftTreeBuilder"/>. Needs administrator rights. Any doubt that the table was read whole (it cannot be opened
/// or followed, a read fails, more than 1 % of the records in use are damaged) throws
/// <see cref="MftUnavailableException"/>, and <see cref="VolumeScanners"/> reads folder by folder instead: a tree is
/// either the whole file table or not made.
/// </summary>
public sealed class MftScanner : IVolumeScanner
{
    private const int BlockBytes = 4 << 20;
    private const ushort InUse = 0x0001;
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
        using var source = _open(root);
        int size = source.BytesPerRecord, sector = source.BytesPerSector;
        var count = source.RecordCount;
        // The builder indexes records by number in arrays.
        if (size < 512 || count <= 0 || count >= int.MaxValue) throw new MftUnavailableException($"unexpected file table: {count} records of {size} bytes");

        var perBlock = Math.Max(1, _blockBytes / size);
        var buffer = new byte[perBlock * size];
        var records = new List<MftRecord>((int)Math.Min(count, 1 << 20));
        long inUse = 0, damaged = 0, bytes = 0;
        var files = 0;
        for (long first = 0; first < count;)
        {
            ct.ThrowIfCancellationRequested();
            var want = (int)Math.Min(perBlock, count - first);
            var read = source.Read(first, buffer.AsSpan(0, want * size));
            if (read <= 0 || read > want) throw new MftUnavailableException($"the file table ended after {first} of {count} records");

            for (var k = 0; k < read; k++)
            {
                var r = buffer.AsSpan(k * size, size);
                // The signature and the in-use flag lie before the first stride's end, so they are readable before the
                // fixups: a record in use that then fails to parse is damaged, an unused one is simply skipped.
                var signature = BinaryPrimitives.ReadUInt32LittleEndian(r);
                if (signature == Baad) { inUse++; damaged++; continue; }
                if (signature != File || (BinaryPrimitives.ReadUInt16LittleEndian(r[0x16..]) & InUse) == 0) continue;
                inUse++;
                // Parse applies the fixups in place: each record is parsed once, from this fresh read.
                if (MftRecordParser.Parse(r, sector, (uint)(first + k)) is not { } record) { damaged++; continue; }
                records.Add(record);
                if (record is { BaseReference: 0, IsFolder: false })
                {
                    files++;
                    bytes += record.DataSize ?? 0;
                }
            }
            first += read;
            progress?.Report(new ScanProgress(first * 100.0 / count, files, bytes, ScanReader.FileTable));
        }

        if (damaged * 100 > inUse) throw new MftUnavailableException($"{damaged} of {inUse} file records in use are damaged");
        ct.ThrowIfCancellationRequested();
        return MftTreeBuilder.Build(records, root, watch.Elapsed);
    }

    /// <summary>"FILE" and "BAAD" (a record NTFS found damaged), little endian.</summary>
    private const uint File = 0x454C4946, Baad = 0x44414142;
}
