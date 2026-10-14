using System.Runtime.InteropServices;

namespace Optimizer.Core.Storage.Mft;

/// <summary>What FSCTL_GET_NTFS_VOLUME_DATA reports about a volume (NTFS_VOLUME_DATA_BUFFER).</summary>
internal readonly record struct NtfsVolumeData(int BytesPerSector, int BytesPerCluster, int BytesPerFileRecord, long MftStartLcn,
    long MftValidDataLength, long TotalClusters);

/// <summary>
/// A volume opened for raw reading without the cache: <see cref="Read"/> takes an offset and a length that are multiples
/// of the sector size, into memory aligned to it, and reads all of it or throws <see cref="MftUnavailableException"/>.
/// </summary>
internal interface IVolumeDevice : IDisposable
{
    NtfsVolumeData VolumeData { get; }
    void Read(long offset, Span<byte> buffer);
}

/// <summary>
/// The file table of a volume as an <see cref="IMftSource"/>. Finds where $MFT lies from its own record 0: the runs of
/// its unnamed $DATA, which on a grown $MFT continue in extension records that record 0's attribute list names (those
/// are read through the runs known so far, in VCN order). The runs must cover the valid data length without a gap, or
/// the table counts as unreadable: a partly read file table is never passed off as the whole drive. Record 0's $BITMAP
/// says which records are in use, so stretches of free records are not read (in the test VM half of the 550 MB); without
/// a readable bitmap every record is read. Reads go through one sector-aligned native buffer, at most 4 MB at a time.
/// </summary>
internal sealed unsafe class VolumeMftSource : IMftSource
{
    private const int BlockBytes = 4 << 20;
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    private readonly IVolumeDevice _device;
    private readonly NtfsVolumeData _volume;
    private readonly (long Offset, long Length)[] _extents;
    /// <summary>$MFT's own $BITMAP (bit n set: record n in use), null when it could not be read; then every record is read.</summary>
    private readonly byte[]? _bitmap;
    private byte* _block;
    private readonly int _blockSize;

    public static IMftSource Open(string root) => new VolumeMftSource(WindowsVolume.Open(root));

    internal VolumeMftSource(IVolumeDevice device)
    {
        _device = device;
        try
        {
            var v = _volume = device.VolumeData;
            if (v.BytesPerSector < 512 || !IsPowerOfTwo(v.BytesPerSector) || v.BytesPerSector > BlockBytes
                || v.BytesPerCluster < v.BytesPerSector || v.BytesPerCluster % v.BytesPerSector != 0
                || v.BytesPerFileRecord < 512 || !IsPowerOfTwo(v.BytesPerFileRecord) || v.BytesPerFileRecord > BlockBytes
                || v.MftStartLcn <= 0 || v.MftStartLcn >= v.TotalClusters
                || v.MftValidDataLength < v.BytesPerFileRecord || v.MftValidDataLength % v.BytesPerFileRecord != 0)
                throw new MftUnavailableException("unexpected volume data");
            _blockSize = BlockBytes;
            _block = (byte*)NativeMemory.AlignedAlloc(BlockBytes, (nuint)Math.Max(v.BytesPerSector, 4096));
            (_extents, _bitmap) = FindExtents();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public int BytesPerRecord => _volume.BytesPerFileRecord;
    public int BytesPerSector => _volume.BytesPerSector;
    public long RecordCount => _volume.MftValidDataLength / _volume.BytesPerFileRecord;

    public int Read(long firstRecord, Span<byte> buffer)
    {
        var count = (int)Math.Min(buffer.Length / BytesPerRecord, RecordCount - firstRecord);
        if (count <= 0) return 0;
        if (!ReadMft(_extents, firstRecord * BytesPerRecord, buffer[..(count * BytesPerRecord)]))
            throw new MftUnavailableException("a file record lies outside the file table's runs");
        return count;
    }

    /// <summary>
    /// The next records to read: from the first one at or after <paramref name="from"/> that the bitmap marks in use to
    /// the last one so marked within <paramref name="max"/> records. Free records between are read too; whole free
    /// stretches (an $MFT keeps many, often more than half its records) are not. A record put in use after the bitmap
    /// was read is missed, like any file made while the drive is read.
    /// </summary>
    public (long First, int Count) NextRun(long from, int max)
    {
        var count = RecordCount;
        if (_bitmap is null) return from >= count ? (count, 0) : (from, (int)Math.Min(max, count - from));
        var first = from;
        while (first < count && !MayBeInUse(first)) first++;
        if (first >= count) return (count, 0);
        var last = Math.Min(count, first + max) - 1;
        while (last > first && !MayBeInUse(last)) last--;
        return (first, (int)(last - first + 1));
    }

    public long? InUseRecords
    {
        get
        {
            if (_bitmap is null) return null;
            long used = 0;
            foreach (var b in _bitmap) used += System.Numerics.BitOperations.PopCount(b);
            return Math.Min(RecordCount, used + Math.Max(0, RecordCount - (long)_bitmap.Length * 8));
        }
    }

    /// <summary>Records past the end of the bitmap count as in use.</summary>
    private bool MayBeInUse(long n) => n >= (long)_bitmap!.Length * 8 || (_bitmap[n >> 3] & (1 << (int)(n & 7))) != 0;

    public void Dispose()
    {
        if (_block is not null)
        {
            NativeMemory.AlignedFree(_block);
            _block = null;
        }
        _device.Dispose();
    }

    /// <summary>The extents of $MFT on the volume (byte offset and length) in VCN order, and its bitmap when readable.</summary>
    private ((long, long)[], byte[]?) FindExtents()
    {
        var v = _volume;
        var size = v.BytesPerFileRecord;
        var record = new byte[size];
        ReadVolume(v.MftStartLcn * v.BytesPerCluster, record);
        var own = MftRecordParser.ReadDataSegments(record, v.BytesPerSector)
            ?? throw new MftUnavailableException("the file table's own record is damaged");
        if (own.ListNonResident) throw new MftUnavailableException("the file table's attribute list is not resident");

        var segments = new List<MftDataSegment>(own.Segments);
        foreach (var (lowestVcn, reference) in own.Listed.OrderBy(l => l.LowestVcn))
        {
            if (segments.Any(s => s.LowestVcn == lowestVcn)) continue;
            // In an extension record, read through the runs found so far.
            var known = Extents(segments, partial: true);
            var number = (long)(reference & RecordNumberMask);
            if (number == 0 || !ReadMft(known, number * size, record))
                throw new MftUnavailableException("a part of the file table's runs cannot be reached");
            var extension = MftRecordParser.ReadDataSegments(record, v.BytesPerSector);
            if (extension is null || (extension.BaseReference & RecordNumberMask) != 0 || extension.Sequence != (ushort)(reference >> 48)
                || extension.Segments.FirstOrDefault(s => s.LowestVcn == lowestVcn) is not { } segment)
                throw new MftUnavailableException("an extension record of the file table is damaged or reused");
            segments.Add(segment);
        }

        var extents = Extents(segments, partial: false);
        var covered = extents.Sum(e => e.Item2);
        if (covered < v.MftValidDataLength) throw new MftUnavailableException($"the file table's runs cover {covered} of {v.MftValidDataLength} bytes");
        return (extents, ReadBitmap(own.Bitmap));
    }

    /// <summary>
    /// $MFT's $BITMAP from record 0: resident, or non-resident with runs in record 0 that lie on the volume and cover it.
    /// Anything else gives null and every record is read: the bitmap only saves reading free records.
    /// </summary>
    private byte[]? ReadBitmap(MftBitmap? bitmap)
    {
        var v = _volume;
        // One bit per record; more than that (allocated ahead) is not needed.
        var needed = (RecordCount + 7) / 8;
        if (bitmap is null) return null;
        if (bitmap.Value is { } value) return Plausible(value) ? value : null;
        var size = Math.Min(bitmap.Size, needed);
        long covered = 0;
        foreach (var (lcn, clusters) in bitmap.Runs)
        {
            if (lcn < 0 || clusters > v.TotalClusters - lcn) return null;
            covered += clusters * v.BytesPerCluster;
        }
        if (covered < size || size <= 0) return null;

        var bytes = new byte[size];
        var into = bytes.AsSpan();
        foreach (var (lcn, clusters) in bitmap.Runs)
        {
            if (into.IsEmpty) break;
            var take = (int)Math.Min(into.Length, clusters * v.BytesPerCluster);
            ReadVolume(lcn * v.BytesPerCluster, into[..take]);
            into = into[take..];
        }
        return Plausible(bytes) ? bytes : null;
    }

    /// <summary>
    /// Records 0 to 11 are NTFS's own files ($MFT to $UpCase, with the root as record 5) and always in use: a bitmap that
    /// does not mark them is wrong (read from the wrong clusters, damaged) and must not decide which records are read.
    /// </summary>
    private static bool Plausible(byte[] bitmap) => bitmap.Length >= 2 && bitmap[0] == 0xFF && (bitmap[1] & 0x0F) == 0x0F;

    public bool? MarkedInUse(long record) => _bitmap is null ? null : MayBeInUse(record);

    /// <summary>
    /// The runs of the segments in VCN order as volume extents. A gap, an overlap, a sparse run or a run past the end of
    /// the volume makes the table unreadable; with <paramref name="partial"/> the extents stop at the first gap instead.
    /// </summary>
    private (long, long)[] Extents(List<MftDataSegment> segments, bool partial)
    {
        var v = _volume;
        var extents = new List<(long, long)>();
        long next = 0;
        foreach (var s in segments.OrderBy(s => s.LowestVcn))
        {
            if (s.LowestVcn != next)
            {
                if (partial && s.LowestVcn > next) break;
                throw new MftUnavailableException("the file table's runs have a gap or overlap");
            }
            foreach (var (lcn, clusters) in s.Runs)
            {
                if (lcn < 0 || clusters > v.TotalClusters - lcn) throw new MftUnavailableException("a run of the file table lies outside the volume");
                extents.Add((lcn * v.BytesPerCluster, clusters * v.BytesPerCluster));
            }
            next = s.HighestVcn + 1;
        }
        return [.. extents];
    }

    /// <summary>Reads <paramref name="into"/> from the file table at byte <paramref name="offset"/>; false past its extents.</summary>
    private bool ReadMft((long Offset, long Length)[] extents, long offset, Span<byte> into)
    {
        long start = 0;
        foreach (var (at, length) in extents)
        {
            if (into.IsEmpty) break;
            if (offset < start + length)
            {
                var take = (int)Math.Min(into.Length, start + length - offset);
                ReadVolume(at + offset - start, into[..take]);
                into = into[take..];
                offset += take;
            }
            start += length;
        }
        return into.IsEmpty;
    }

    /// <summary>
    /// Reads any range of the volume through the aligned buffer: whole sectors around it, at most 4 MB per read. The
    /// rounding stays inside the cluster, and so inside the extent, that holds the range.
    /// </summary>
    private void ReadVolume(long offset, Span<byte> into)
    {
        var sector = _volume.BytesPerSector;
        while (!into.IsEmpty)
        {
            var start = offset - offset % sector;
            var skip = (int)(offset - start);
            var length = (int)Math.Min(_blockSize, (skip + (long)into.Length + sector - 1) / sector * sector);
            _device.Read(start, new Span<byte>(_block, length));
            var take = Math.Min(length - skip, into.Length);
            new ReadOnlySpan<byte>(_block + skip, take).CopyTo(into);
            into = into[take..];
            offset += take;
        }
    }

    private static bool IsPowerOfTwo(int n) => (n & (n - 1)) == 0;
}
