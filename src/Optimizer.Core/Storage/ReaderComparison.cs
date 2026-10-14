using System.Diagnostics;
using System.Globalization;
using System.Text;
using Optimizer.Core.Storage.Mft;

namespace Optimizer.Core.Storage;

/// <summary>
/// Developer aid (--storage-compare): reads a drive with both readers, one after the other, and lists their totals and
/// times, then the top-level folders whose totals differ. With one reader only, it also gives that reader's memory: the
/// process's working set before the scan and its peak (the peak only grows, so each reader needs a process of its own).
/// Read only, like both readers.
/// </summary>
public static class ReaderComparison
{
    /// <param name="only">One reader, with its memory; null for both readers and their differences.</param>
    public static async Task<string> CompareAsync(string root, bool elevated, ScanReader? only, CancellationToken ct)
    {
        TimedSource? source = null;
        var fileTable = only is null or ScanReader.FileTable ? new MftScanner(r => source = new TimedSource(VolumeMftSource.Open(r))) : null;
        var folders = only is null or ScanReader.Folders ? new EnumerationScanner() : null;
        var text = await CompareAsync(root, VolumeScanners.UsesFileTable(root, elevated), fileTable, folders, only is not null, ct)
            .ConfigureAwait(false);
        return source is null ? text : text + string.Create(CultureInfo.InvariantCulture,
            $"File table reads from the volume: {source.Reading.TotalSeconds:0.00} s for {source.Bytes >> 20} MB{Environment.NewLine}");
    }

    internal static async Task<string> CompareAsync(string root, bool chosen, IVolumeScanner? fileTable, IVolumeScanner? folders,
        bool memory, CancellationToken ct)
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"Drive {root}, {DateTime.Now:yyyy-MM-dd HH:mm}");
        text.AppendLine($"Analyze reads it {(chosen ? "from the file table" : "folder by folder")}");
        long? before = null;
        HeapPeak? heap = null;
        if (memory)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            using var process = Process.GetCurrentProcess();
            before = process.WorkingSet64;
            heap = new HeapPeak();
        }

        DiskTree? table = null, walk = null;
        var watch = Stopwatch.StartNew();
        if (fileTable is not null)
        {
            try
            {
                table = await fileTable.ScanAsync(root, null, ct).ConfigureAwait(false);
                text.AppendLine(Line("File table", table, watch.Elapsed, before, heap));
            }
            catch (MftUnavailableException ex)
            {
                text.AppendLine($"File table: not read ({ex.Message})");
            }
        }
        if (folders is not null)
        {
            watch.Restart();
            walk = await folders.ScanAsync(root, null, ct).ConfigureAwait(false);
            text.AppendLine(Line("Folder by folder", walk, watch.Elapsed, before, heap));
        }
        heap?.Dispose();
        if (table is null || walk is null) return text.ToString();

        text.AppendLine(CultureInfo.InvariantCulture, $"Difference: {table.Size(table.Root) - walk.Size(walk.Root)} bytes, {table.Files(table.Root) - walk.Files(walk.Root)} files");
        var other = TopLevel(walk);
        foreach (var (name, (bytes, files)) in TopLevel(table).OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
        {
            var (otherBytes, otherFiles) = other.Remove(name, out var o) ? o : (0, 0);
            if (bytes != otherBytes || files != otherFiles)
                text.AppendLine(CultureInfo.InvariantCulture, $"  {name}: file table {bytes} bytes, {files} files; folder by folder {otherBytes} bytes, {otherFiles} files");
        }
        foreach (var (name, (bytes, files)) in other.OrderBy(e => e.Key, StringComparer.OrdinalIgnoreCase))
            text.AppendLine(CultureInfo.InvariantCulture, $"  {name}: only folder by folder, {bytes} bytes, {files} files");
        return text.ToString();
    }

    private static string Line(string reader, DiskTree t, TimeSpan wall, long? before, HeapPeak? heap)
    {
        var folders = 0;
        for (var i = 1; i < t.Count; i++)
            if (t.IsFolder(i)) folders++;
        var line = string.Create(CultureInfo.InvariantCulture,
            $"{reader}: {t.Size(t.Root)} bytes, {t.Files(t.Root)} files, {folders} folders, {t.Lost} lost, {wall.TotalSeconds:0.00} s (reported {t.Elapsed.TotalSeconds:0.00} s)");
        if (before is not { } start) return line;
        using var process = Process.GetCurrentProcess();
        return line + string.Create(CultureInfo.InvariantCulture,
            $", working set {start >> 20} MB before, peak {process.PeakWorkingSet64 >> 20} MB, peak minus before {(process.PeakWorkingSet64 - start) >> 20} MB, managed heap peak {heap?.Peak >> 20} MB above the start");
    }

    /// <summary>
    /// The managed heap's highest size above its size at the start, sampled every few milliseconds on a thread of its own:
    /// what a reader holds at its worst, which the process's working set (memory the GC keeps committed) does not show.
    /// </summary>
    private sealed class HeapPeak : IDisposable
    {
        private readonly long _start = GC.GetTotalMemory(true);
        private long _peak;
        private volatile bool _stop;
        private readonly Thread _thread;

        public HeapPeak()
        {
            _thread = new Thread(() =>
            {
                while (!_stop)
                {
                    var now = GC.GetTotalMemory(false) - _start;
                    if (now > Interlocked.Read(ref _peak)) Interlocked.Exchange(ref _peak, now);
                    Thread.Sleep(2);
                }
            }) { IsBackground = true };
            _thread.Start();
        }

        public long Peak => Interlocked.Read(ref _peak);

        public void Dispose()
        {
            _stop = true;
            _thread.Join();
        }
    }

    private static Dictionary<string, (long Bytes, int Files)> TopLevel(DiskTree t)
    {
        var top = new Dictionary<string, (long, int)>(StringComparer.OrdinalIgnoreCase);
        foreach (var c in t.Children(t.Root))
        {
            var (bytes, files) = top.GetValueOrDefault(t.IsFolder(c) ? t.Name(c) : "(files in the root)");
            top[t.IsFolder(c) ? t.Name(c) : "(files in the root)"] = (bytes + t.Size(c), files + (t.IsFolder(c) ? t.Files(c) : 1));
        }
        return top;
    }

    /// <summary>Times the volume reads of the file table reader, to tell reading from parsing.</summary>
    private sealed class TimedSource(IMftSource inner) : IMftSource
    {
        private readonly Stopwatch _reading = new();
        public TimeSpan Reading => _reading.Elapsed;
        public long Bytes { get; private set; }
        public int BytesPerRecord => inner.BytesPerRecord;
        public int BytesPerSector => inner.BytesPerSector;
        public long RecordCount => inner.RecordCount;
        public (long First, int Count) NextRun(long from, int max) => inner.NextRun(from, max);
        public long? InUseRecords => inner.InUseRecords;

        public int Read(long firstRecord, Span<byte> buffer)
        {
            _reading.Start();
            var n = inner.Read(firstRecord, buffer);
            _reading.Stop();
            Bytes += (long)n * BytesPerRecord;
            return n;
        }

        public void Dispose() => inner.Dispose();
    }
}
