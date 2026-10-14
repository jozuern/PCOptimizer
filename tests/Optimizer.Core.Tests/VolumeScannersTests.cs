using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Optimizer.Core.Storage;
using Optimizer.Core.Storage.Mft;

namespace Optimizer.Core.Tests;

/// <summary>
/// The file table reader and the choice of reader, on the VM-made $MFT fixture (Fixtures/mft): served as it is, with
/// damaged records, or laid out on an in-memory volume. No volume is opened here.
/// </summary>
public class VolumeScannersTests
{
    private static readonly Lazy<Fixture> Data = new(Fixture.Load);

    private sealed record Fixture(byte[] Mft, int RecordSize, int SectorSize, int ClusterSize, IReadOnlyList<MftRecord> Parsed, DiskTree Expected)
    {
        public long Records => Mft.Length / RecordSize;

        public static Fixture Load()
        {
            var folder = Path.Combine(RepoPaths.Root, "tests", "Optimizer.Core.Tests", "Fixtures", "mft");
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "test-volume.json")));
            var made = json.RootElement.GetProperty("made");
            using var gz = new GZipStream(File.OpenRead(Path.Combine(folder, "test-volume.mft.gz")), CompressionMode.Decompress);
            using var ms = new MemoryStream();
            gz.CopyTo(ms);
            var mft = ms.ToArray();
            var recordSize = made.GetProperty("bytesPerFileRecord").GetInt32();
            var sectorSize = made.GetProperty("bytesPerSector").GetInt32();

            // What the builder makes of the records parsed one by one: the reader must give the same tree.
            var work = (byte[])mft.Clone();
            var records = new List<MftRecord>();
            for (var n = 0; n < work.Length / recordSize; n++)
                if (MftRecordParser.Parse(work.AsSpan(n * recordSize, recordSize), sectorSize, (uint)n) is { } r) records.Add(r);
            return new Fixture(mft, recordSize, sectorSize, made.GetProperty("bytesPerCluster").GetInt32(), records,
                MftTreeBuilder.Build(records, Root, TimeSpan.Zero));
        }

        public bool InUse(int n) =>
            Mft.AsSpan(n * RecordSize, 4).SequenceEqual("FILE"u8) && (BinaryPrimitives.ReadUInt16LittleEndian(Mft.AsSpan(n * RecordSize + 0x16)) & 1) != 0;
    }

    private const string Root = @"T:\";

    /// <summary>
    /// Serves the fixture bytes as a volume's file table would; can cancel or fail on a read. The scanner reads the next
    /// block on another thread while it parses: a read must never overlap another or come after the source is closed.
    /// </summary>
    private sealed class FixtureSource(byte[] mft, int recordSize, int sectorSize, Action<long>? onRead = null) : IMftSource
    {
        private int _active;
        public bool Disposed { get; private set; }
        public bool Misused { get; private set; }
        public List<long> Reads { get; } = [];
        public int BytesPerRecord => recordSize;
        public int BytesPerSector => sectorSize;
        public long RecordCount => mft.Length / recordSize;

        public int Read(long firstRecord, Span<byte> buffer)
        {
            if (Interlocked.Increment(ref _active) != 1 || Disposed) Misused = true;
            try
            {
                Reads.Add(firstRecord);
                // Slow enough that the scanner parses the block before meanwhile.
                Thread.Sleep(5);
                onRead?.Invoke(firstRecord);
                var n = (int)Math.Min(buffer.Length / recordSize, RecordCount - firstRecord);
                mft.AsSpan((int)(firstRecord * recordSize), n * recordSize).CopyTo(buffer);
                return n;
            }
            finally { Interlocked.Decrement(ref _active); }
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _active) != 0) Misused = true;
            Disposed = true;
        }
    }

    /// <summary>Collects every report on the reporting thread (Progress&lt;T&gt; would post them later).</summary>
    private sealed class Reports : IProgress<ScanProgress>
    {
        private readonly List<ScanProgress> _all = [];
        public IReadOnlyList<ScanProgress> All { get { lock (_all) return _all.ToList(); } }
        public void Report(ScanProgress value) { lock (_all) _all.Add(value); }
    }

    /// <summary>A reader that returns a fixed tree and remembers that it ran.</summary>
    private sealed class FakeScanner(ScanReader reader, Exception? failure = null) : IVolumeScanner
    {
        public int Runs { get; private set; }

        public Task<DiskTree> ScanAsync(string root, IProgress<ScanProgress>? progress, CancellationToken ct)
        {
            Runs++;
            if (failure is not null) return Task.FromException<DiskTree>(failure);
            var b = new DiskTree.Builder(root);
            b.AddFile(b.Root, "one.bin", 10, -1, default, EntryFlags.None);
            return Task.FromResult(b.Build(reader, TimeSpan.Zero));
        }
    }

    private static MftScanner Scanner(IMftSource source, int blockBytes = 4 << 20) => new(_ => source, blockBytes);

    private static void AssertSameTree(DiskTree expected, DiskTree actual)
    {
        Assert.Equal(ScanReader.FileTable, actual.Reader);
        Assert.Equal(expected.Count, actual.Count);
        Assert.Equal(expected.Lost, actual.Lost);
        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected.FullPath(i), actual.FullPath(i));
            Assert.Equal(expected.Size(i), actual.Size(i));
            Assert.Equal(expected.Allocated(i), actual.Allocated(i));
            Assert.Equal(expected.Files(i), actual.Files(i));
            Assert.Equal(expected.Flags(i), actual.Flags(i));
            Assert.Equal(expected.LastWriteUtc(i), actual.LastWriteUtc(i));
        }
    }

    [Fact]
    public async Task FixtureSourceGivesTheSameTreeAsTheBuilder()
    {
        var f = Data.Value;
        var source = new FixtureSource((byte[])f.Mft.Clone(), f.RecordSize, f.SectorSize);
        var reports = new Reports();

        // Blocks of 16 records: the file table is read in several parts, as a drive's is in 4 MB blocks.
        var tree = await Scanner(source, blockBytes: 16 * f.RecordSize).ScanAsync(Root, reports, CancellationToken.None);

        AssertSameTree(f.Expected, tree);
        Assert.Equal(Enumerable.Range(0, (int)f.Records / 16).Select(k => k * 16L), source.Reads);
        Assert.True(source.Disposed);
        Assert.False(source.Misused);
        var all = reports.All;
        Assert.All(all, p => Assert.Equal(ScanReader.FileTable, p.Reader));
        Assert.Equal(100, all[^1].Percent);
        Assert.True(all.Count > 1);
        Assert.Equal(all.Select(p => p.Percent).Order(), all.Select(p => p.Percent));
    }

    [Fact]
    public async Task BrokenFixupsFallBackToEnumeration()
    {
        var f = Data.Value;
        var broken = (byte[])f.Mft.Clone();
        // Every 20th record in use (5 %) torn: the end of its first stride no longer carries the update sequence number.
        var inUse = Enumerable.Range(0, (int)f.Records).Where(f.InUse).ToList();
        foreach (var n in inUse.Where((_, k) => k % 20 == 0)) broken[n * f.RecordSize + 510] ^= 0xFF;
        var source = new FixtureSource(broken, f.RecordSize, f.SectorSize);
        var folders = new FakeScanner(ScanReader.Folders);

        var tree = await VolumeScanners.ScanAsync(Root, fileTable: true, null, CancellationToken.None, Scanner(source), folders);

        Assert.Equal(ScanReader.Folders, tree.Reader);
        Assert.Equal(1, folders.Runs);
        Assert.True(source.Disposed);
    }

    [Fact]
    public async Task UpToOnePercentOfDamagedRecordsIsSkipped()
    {
        var f = Data.Value;
        // Files whose name is in the tree once: each damaged one is missing from it.
        var t = f.Expected;
        var once = Enumerable.Range(1, t.Count - 1).Where(i => !t.IsFolder(i)).GroupBy(t.Name).Where(g => g.Count() == 1)
            .Select(g => g.Key).ToHashSet(StringComparer.Ordinal);
        var inUse = f.Parsed.Where(r => r.Number >= 24 && r.BaseReference == 0 && !r.IsFolder && r.Names.Any(n => once.Contains(n.Name)))
            .Select(r => (int)r.Number).ToList();
        var allowed = Enumerable.Range(0, (int)f.Records).Count(f.InUse) / 100;
        Assert.True(allowed >= 1 && inUse.Count > allowed);

        // One record marked bad by NTFS, the others torn: as many as 1 % allows still give the file table's tree.
        byte[] Damage(int count)
        {
            var b = (byte[])f.Mft.Clone();
            "BAAD"u8.CopyTo(b.AsSpan(inUse[0] * f.RecordSize));
            foreach (var n in inUse.Skip(1).Take(count - 1)) b[n * f.RecordSize + 1022] ^= 0xFF;
            return b;
        }

        var tree = await Scanner(new FixtureSource(Damage(allowed), f.RecordSize, f.SectorSize)).ScanAsync(Root, null, CancellationToken.None);
        Assert.Equal(ScanReader.FileTable, tree.Reader);
        Assert.Equal(f.Expected.Count - allowed, tree.Count);

        await Assert.ThrowsAsync<MftUnavailableException>(() =>
            Scanner(new FixtureSource(Damage(allowed + 1), f.RecordSize, f.SectorSize)).ScanAsync(Root, null, CancellationToken.None));
    }

    [Fact]
    public async Task CancelledMftScanThrows()
    {
        var f = Data.Value;
        using var cts = new CancellationTokenSource();
        var source = new FixtureSource(f.Mft, f.RecordSize, f.SectorSize, first => { if (first == 32) cts.Cancel(); });
        var folders = new FakeScanner(ScanReader.Folders);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            VolumeScanners.ScanAsync(Root, fileTable: true, null, cts.Token, Scanner(source, blockBytes: 16 * f.RecordSize), folders));

        // Stopped at the next block, the file table is closed and nothing else is read.
        Assert.Equal(new[] { 0L, 16, 32 }, source.Reads);
        Assert.True(source.Disposed);
        Assert.False(source.Misused);
        Assert.Equal(0, folders.Runs);
    }

    [Fact]
    public async Task AFailedReadAheadEndsTheScanAndClosesTheTable()
    {
        var f = Data.Value;
        var source = new FixtureSource(f.Mft, f.RecordSize, f.SectorSize,
            first => { if (first == 48) throw new MftUnavailableException("reading the volume failed (error 23)"); });

        // The read of block 48 runs while block 32 is parsed; its failure ends the scan before anything else is read.
        await Assert.ThrowsAsync<MftUnavailableException>(() =>
            Scanner(source, blockBytes: 16 * f.RecordSize).ScanAsync(Root, null, CancellationToken.None));
        Assert.Equal(new[] { 0L, 16, 32, 48 }, source.Reads);
        Assert.True(source.Disposed);
        Assert.False(source.Misused);
    }

    [Fact]
    public void TheTableKeepsWhatTheParserReads()
    {
        // The scan fills the table record by record without making an MftRecord for a base record; the builder's path
        // for records parsed one by one must give the same entries, best names and sizes.
        var f = Data.Value;
        var scanned = MftScanner.ReadTable(new FixtureSource((byte[])f.Mft.Clone(), f.RecordSize, f.SectorSize), 4 << 20, null,
            CancellationToken.None);
        var parsed = MftTable.From(f.Parsed);

        Assert.Equal(parsed.Count, scanned.Count);
        Assert.Equal(parsed.Extensions.Select(r => r.Number), scanned.Extensions.Select(r => r.Number));
        for (var i = 0; i < parsed.Count; i++)
        {
            Assert.Equal(parsed.Number[i], scanned.Number[i]);
            Assert.Equal(parsed.Name[i], scanned.Name[i]);
            Assert.Equal(parsed.Parent[i], scanned.Parent[i]);
            Assert.Equal(parsed.Sequence[i], scanned.Sequence[i]);
            Assert.Equal(parsed.Size[i], scanned.Size[i]);
            Assert.Equal(parsed.Allocated[i], scanned.Allocated[i]);
            Assert.Equal(parsed.LastWrite[i], scanned.LastWrite[i]);
            Assert.Equal(parsed.Attributes[i], scanned.Attributes[i]);
            Assert.Equal(parsed.Flags[i], scanned.Flags[i]);
        }
        // Free records cost only their slot.
        Assert.True(scanned.Count < f.Records);
        Assert.Equal(-1, scanned.IndexOf(16));
    }

    [Fact]
    public async Task TheFileTableIsReadOnlyWhenChosen()
    {
        var mft = new FakeScanner(ScanReader.FileTable);
        var folders = new FakeScanner(ScanReader.Folders);
        Assert.Equal(ScanReader.Folders, (await VolumeScanners.ScanAsync(Root, fileTable: false, null, CancellationToken.None, mft, folders)).Reader);
        Assert.Equal(0, mft.Runs);
        Assert.Equal(ScanReader.FileTable, (await VolumeScanners.ScanAsync(Root, fileTable: true, null, CancellationToken.None, mft, folders)).Reader);
        Assert.Equal(1, folders.Runs);

        // Only "the file table cannot be read" means fall back; another failure is reported as it is.
        var failing = new FakeScanner(ScanReader.FileTable, new InvalidOperationException("bug"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            VolumeScanners.ScanAsync(Root, fileTable: true, null, CancellationToken.None, failing, folders));
        Assert.Equal(1, folders.Runs);
    }

    [Theory]
    [InlineData(@"C:\", true)]
    [InlineData(@"c:\", true)]
    [InlineData(@"C:\Users", false)]
    [InlineData(@"\\server\share\", false)]
    public void OnlyADriveRootIsReadFromTheFileTable(string root, bool driveRoot) =>
        Assert.Equal(driveRoot, VolumeScanners.IsDriveRoot(root));

    // The real source over an in-memory volume: the file table in two extents, the second one described by an extension
    // record that record 0's attribute list names, as on a drive whose $MFT has grown in pieces.

    private const int FirstLcn = 100, SecondLcn = 20, FirstClusters = 16, ExtensionRecord = 16;

    /// <summary>A volume in memory that accepts only sector-aligned reads, as a volume opened without buffering does.</summary>
    private sealed class MemoryVolume(byte[] bytes, NtfsVolumeData data) : IVolumeDevice
    {
        public bool Disposed { get; private set; }
        public long BytesRead { get; private set; }
        public NtfsVolumeData VolumeData => data;

        public void Read(long offset, Span<byte> buffer)
        {
            BytesRead += buffer.Length;
            Assert.True(offset % data.BytesPerSector == 0 && buffer.Length % data.BytesPerSector == 0, $"unaligned read {offset} + {buffer.Length}");
            if (offset < 0 || offset + buffer.Length > bytes.Length) throw new MftUnavailableException("read past the end of the volume");
            bytes.AsSpan((int)offset, buffer.Length).CopyTo(buffer);
        }

        public void Dispose() => Disposed = true;
    }

    public enum Layout { Fragmented, ExtensionMissing, ExtensionReused, RunsTooShort, WithBitmap }

    private const int BitmapLcn = 90;

    private static MemoryVolume FragmentedVolume(int sectorSize, Layout layout)
    {
        var f = Data.Value;
        var size = f.RecordSize;
        var cluster = f.ClusterSize;
        var mft = (byte[])f.Mft.Clone();
        var totalClusters = mft.Length / cluster;
        var record0 = mft.AsSpan(0, size);
        var sequence0 = BinaryPrimitives.ReadUInt16LittleEndian(record0[0x10..]);

        // Record 0: $STANDARD_INFORMATION, $ATTRIBUTE_LIST, $FILE_NAME, the first $DATA segment, $BITMAP.
        Unprotect(record0);
        var attributes = Attributes(record0);
        var list = new List<byte>();
        list.AddRange(ListEntry(0x10, sequence0, 0, 0));
        list.AddRange(ListEntry(0x30, sequence0, 0, 0));
        list.AddRange(ListEntry(0x80, sequence0, 0, 0));
        if (layout != Layout.RunsTooShort) list.AddRange(ListEntry(0x80, 1, ExtensionRecord, FirstClusters));
        list.AddRange(ListEntry(0xB0, sequence0, 0, 0));
        var firstData = DataSegment(0, FirstClusters - 1, FirstClusters, FirstLcn, allocated: mft.Length);
        // The fixture's $BITMAP lies outside this small volume, so the source reads every record; WithBitmap puts one on it.
        var bitmap = layout == Layout.WithBitmap
            ? NonResident(0xB0, 0, 0, clusters: 1, BitmapLcn, allocated: cluster, size: f.Records / 8)
            : attributes[0xB0];
        var rebuilt = new List<byte[]> { attributes[0x10], Resident(0x20, [.. list]), attributes[0x30], firstData, bitmap };
        WriteAttributes(record0, rebuilt);
        Protect(record0);

        // The extension record holding the rest of the runs, in a record the fixture leaves empty.
        var extension = mft.AsSpan(ExtensionRecord * size, size);
        Assert.True(extension.IndexOfAnyExcept((byte)0) < 0);
        if (layout != Layout.ExtensionMissing)
        {
            "FILE"u8.CopyTo(extension);
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x04..], 0x30); // update sequence array
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x06..], (ushort)(size / 512 + 1));
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x10..], layout == Layout.ExtensionReused ? (ushort)2 : (ushort)1);
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x14..], 0x38); // first attribute
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x16..], 1);    // in use
            BinaryPrimitives.WriteUInt32LittleEndian(extension[0x1C..], (uint)size);
            BinaryPrimitives.WriteUInt64LittleEndian(extension[0x20..], (ulong)sequence0 << 48); // base: record 0
            BinaryPrimitives.WriteUInt16LittleEndian(extension[0x30..], 0x0001);
            WriteAttributes(extension, [DataSegment(FirstClusters, totalClusters - 1, totalClusters - FirstClusters, SecondLcn, allocated: 0)]);
            Protect(extension);
        }

        var volume = new byte[128 * cluster];
        if (layout == Layout.WithBitmap)
            for (var n = 0; n < f.Records; n++)
            {
                var r = mft.AsSpan(n * size, size);
                if (r[..4].SequenceEqual("FILE"u8) && (BinaryPrimitives.ReadUInt16LittleEndian(r[0x16..]) & 1) != 0)
                    volume[BitmapLcn * cluster + n / 8] |= (byte)(1 << (n % 8));
            }
        mft.AsSpan(0, FirstClusters * cluster).CopyTo(volume.AsSpan(FirstLcn * cluster));
        mft.AsSpan(FirstClusters * cluster).CopyTo(volume.AsSpan(SecondLcn * cluster));
        return new MemoryVolume(volume, new NtfsVolumeData(sectorSize, cluster, size, FirstLcn, mft.Length, 128));
    }

    [Theory]
    [InlineData(512)]
    [InlineData(4096)]
    public async Task VolumeSourceFollowsTheFileTableIntoItsExtensionRecord(int sectorSize)
    {
        var f = Data.Value;
        var volume = FragmentedVolume(sectorSize, Layout.Fragmented);

        // Blocks of 12 records: a block crosses from the first extent into the second.
        var tree = await new MftScanner(_ => new VolumeMftSource(volume), 12 * f.RecordSize).ScanAsync(Root, null, CancellationToken.None);

        AssertSameTree(f.Expected, tree);
        Assert.True(volume.Disposed);
    }

    [Fact]
    public async Task VolumeSourceSkipsTheRecordsTheBitmapMarksFree()
    {
        var f = Data.Value;
        var everything = FragmentedVolume(512, Layout.Fragmented);
        await new MftScanner(_ => new VolumeMftSource(everything), 4 * f.RecordSize).ScanAsync(Root, null, CancellationToken.None);
        var volume = FragmentedVolume(512, Layout.WithBitmap);

        var tree = await new MftScanner(_ => new VolumeMftSource(volume), 4 * f.RecordSize).ScanAsync(Root, null, CancellationToken.None);

        AssertSameTree(f.Expected, tree);
        // The fixture's free records (16 to 23 and many at the end) are not read; the bitmap's own cluster is.
        var inUse = Enumerable.Range(0, (int)f.Records).Count(f.InUse) + 1; // and the extension record made here
        Assert.True(volume.BytesRead < everything.BytesRead, $"{volume.BytesRead} of {everything.BytesRead} bytes");
        Assert.True(volume.BytesRead <= (inUse + 16) * f.RecordSize + 2 * f.ClusterSize, $"{volume.BytesRead} bytes for {inUse} records in use");
    }

    [Theory]
    [InlineData(Layout.ExtensionMissing)]
    [InlineData(Layout.ExtensionReused)]
    [InlineData(Layout.RunsTooShort)]
    public async Task VolumeSourceRefusesAFileTableItCannotReadWhole(Layout layout)
    {
        var volume = FragmentedVolume(512, layout);
        await Assert.ThrowsAsync<MftUnavailableException>(() =>
            new MftScanner(_ => new VolumeMftSource(volume)).ScanAsync(Root, null, CancellationToken.None));
        Assert.True(volume.Disposed);
    }

    [Fact]
    public async Task ReaderComparisonListsBothReaders()
    {
        var text = await ReaderComparison.CompareAsync(Root, true, new FakeScanner(ScanReader.FileTable), new FakeScanner(ScanReader.Folders),
            memory: false, CancellationToken.None);
        Assert.Contains("File table: 10 bytes, 1 files, 0 folders, 0 lost", text);
        Assert.Contains("Folder by folder: 10 bytes, 1 files, 0 folders, 0 lost", text);

        Assert.Contains("Analyze reads it from the file table", text);

        var unavailable = await ReaderComparison.CompareAsync(Root, false, new FakeScanner(ScanReader.FileTable, new MftUnavailableException("not elevated")),
            new FakeScanner(ScanReader.Folders), memory: false, CancellationToken.None);
        Assert.Contains("File table: not read (not elevated)", unavailable);
        Assert.Contains("Analyze reads it folder by folder", unavailable);
        Assert.DoesNotContain("peak", text);

        // One reader per process: its memory, and no differences.
        var one = await ReaderComparison.CompareAsync(Root, true, new FakeScanner(ScanReader.FileTable), null, memory: true, CancellationToken.None);
        Assert.Contains("File table: 10 bytes", one);
        Assert.Contains("MB before, peak ", one);
        Assert.DoesNotContain("Folder by folder", one);
        Assert.DoesNotContain("Difference", one);
    }

    // Record surgery for the in-memory volume.

    private static void Unprotect(Span<byte> r)
    {
        int usa = BinaryPrimitives.ReadUInt16LittleEndian(r[0x04..]), count = BinaryPrimitives.ReadUInt16LittleEndian(r[0x06..]);
        for (var i = 1; i < count; i++) r.Slice(usa + 2 * i, 2).CopyTo(r.Slice(i * 512 - 2, 2));
    }

    private static void Protect(Span<byte> r)
    {
        int usa = BinaryPrimitives.ReadUInt16LittleEndian(r[0x04..]), count = BinaryPrimitives.ReadUInt16LittleEndian(r[0x06..]);
        for (var i = 1; i < count; i++)
        {
            r.Slice(i * 512 - 2, 2).CopyTo(r.Slice(usa + 2 * i, 2));
            r.Slice(usa, 2).CopyTo(r.Slice(i * 512 - 2, 2));
        }
    }

    private static Dictionary<uint, byte[]> Attributes(ReadOnlySpan<byte> r)
    {
        var found = new Dictionary<uint, byte[]>();
        for (int pos = BinaryPrimitives.ReadUInt16LittleEndian(r[0x14..]); ;)
        {
            var type = BinaryPrimitives.ReadUInt32LittleEndian(r[pos..]);
            if (type == 0xFFFF_FFFF) return found;
            var length = (int)BinaryPrimitives.ReadUInt32LittleEndian(r[(pos + 4)..]);
            found[type] = r.Slice(pos, length).ToArray();
            pos += length;
        }
    }

    private static void WriteAttributes(Span<byte> r, IEnumerable<byte[]> attributes)
    {
        int pos = BinaryPrimitives.ReadUInt16LittleEndian(r[0x14..]);
        r[pos..(r.Length - 2)].Clear();
        foreach (var a in attributes)
        {
            a.CopyTo(r[pos..]);
            pos += a.Length;
        }
        BinaryPrimitives.WriteUInt32LittleEndian(r[pos..], 0xFFFF_FFFF);
        BinaryPrimitives.WriteUInt32LittleEndian(r[0x18..], (uint)(pos + 8)); // bytes in use
    }

    private static byte[] Resident(uint type, byte[] value)
    {
        var a = new byte[(24 + value.Length + 7) & ~7];
        BinaryPrimitives.WriteUInt32LittleEndian(a, type);
        BinaryPrimitives.WriteUInt32LittleEndian(a.AsSpan(4), (uint)a.Length);
        BinaryPrimitives.WriteUInt32LittleEndian(a.AsSpan(16), (uint)value.Length);
        BinaryPrimitives.WriteUInt16LittleEndian(a.AsSpan(20), 24);
        value.CopyTo(a, 24);
        return a;
    }

    /// <summary>A non-resident unnamed $DATA segment of one run; only the segment at VCN 0 carries the sizes.</summary>
    private static byte[] DataSegment(long lowestVcn, long highestVcn, int clusters, int lcn, long allocated) =>
        NonResident(0x80, lowestVcn, highestVcn, clusters, lcn, allocated, allocated);

    /// <summary>A non-resident unnamed attribute segment of one run.</summary>
    private static byte[] NonResident(uint type, long lowestVcn, long highestVcn, int clusters, int lcn, long allocated, long size)
    {
        var a = new byte[0x48];
        BinaryPrimitives.WriteUInt32LittleEndian(a, type);
        BinaryPrimitives.WriteUInt32LittleEndian(a.AsSpan(4), (uint)a.Length);
        a[8] = 1;
        BinaryPrimitives.WriteUInt16LittleEndian(a.AsSpan(10), 0x40);
        BinaryPrimitives.WriteInt64LittleEndian(a.AsSpan(0x10), lowestVcn);
        BinaryPrimitives.WriteInt64LittleEndian(a.AsSpan(0x18), highestVcn);
        BinaryPrimitives.WriteUInt16LittleEndian(a.AsSpan(0x20), 0x40);
        BinaryPrimitives.WriteInt64LittleEndian(a.AsSpan(0x28), allocated);
        BinaryPrimitives.WriteInt64LittleEndian(a.AsSpan(0x30), size);
        BinaryPrimitives.WriteInt64LittleEndian(a.AsSpan(0x38), size);
        // One run: one byte of cluster count, one byte of LCN (both under 128 here), then the end of the list.
        a[0x40] = 0x11;
        a[0x41] = (byte)clusters;
        a[0x42] = (byte)lcn;
        return a;
    }

    private static byte[] ListEntry(uint type, ushort sequence, uint record, long lowestVcn)
    {
        var e = new byte[0x20];
        BinaryPrimitives.WriteUInt32LittleEndian(e, type);
        BinaryPrimitives.WriteUInt16LittleEndian(e.AsSpan(4), (ushort)e.Length);
        e[7] = 0x1A;
        BinaryPrimitives.WriteInt64LittleEndian(e.AsSpan(8), lowestVcn);
        BinaryPrimitives.WriteUInt64LittleEndian(e.AsSpan(16), (ulong)sequence << 48 | record);
        return e;
    }
}
