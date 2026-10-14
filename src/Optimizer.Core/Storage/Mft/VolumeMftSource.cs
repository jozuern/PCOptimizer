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
/// the table counts as unreadable: a partly read file table is never passed off as the whole drive. Reads go through
/// one sector-aligned native buffer, at most 4 MB at a time.
/// </summary>
internal sealed unsafe class VolumeMftSource : IMftSource
{
    private const int BlockBytes = 4 << 20;
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    private readonly IVolumeDevice _device;
    private readonly NtfsVolumeData _volume;
    private readonly (long Offset, long Length)[] _extents;
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
            _extents = FindExtents();
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

    public void Dispose()
    {
        if (_block is not null)
        {
            NativeMemory.AlignedFree(_block);
            _block = null;
        }
        _device.Dispose();
    }

    /// <summary>The extents of $MFT on the volume (byte offset and length) in VCN order.</summary>
    private (long, long)[] FindExtents()
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
        return extents;
    }

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
