using System.Diagnostics;
using System.Globalization;
using System.Text;
using Optimizer.Core.Storage.Mft;

namespace Optimizer.Core.Storage;

/// <summary>
/// Developer aid (--storage-compare): reads a drive with both readers, one after the other, and lists their totals and
/// times (with the process's peak memory after each), then the top-level folders whose totals differ. Read only, like
/// both readers.
/// </summary>
public static class ReaderComparison
{
    public static async Task<string> CompareAsync(string root, bool elevated, CancellationToken ct)
    {
        TimedSource? source = null;
        var text = await CompareAsync(root, VolumeScanners.UsesFileTable(root, elevated),
            new MftScanner(r => source = new TimedSource(VolumeMftSource.Open(r))), new EnumerationScanner(), ct).ConfigureAwait(false);
        return source is null ? text : text + string.Create(CultureInfo.InvariantCulture,
            $"File table reads from the volume: {source.Reading.TotalSeconds:0.00} s for {source.Bytes >> 20} MB{Environment.NewLine}");
    }

    internal static async Task<string> CompareAsync(string root, bool chosen, IVolumeScanner fileTable, IVolumeScanner folders, CancellationToken ct)
    {
        var text = new StringBuilder();
        text.AppendLine(CultureInfo.InvariantCulture, $"Drive {root}, {DateTime.Now:yyyy-MM-dd HH:mm}");
        text.AppendLine($"Analyze reads it {(chosen ? "from the file table" : "folder by folder")}");
        DiskTree? table = null;
        var watch = Stopwatch.StartNew();
        try
        {
            table = await fileTable.ScanAsync(root, null, ct).ConfigureAwait(false);
            text.AppendLine(Line("File table", table, watch.Elapsed));
        }
        catch (MftUnavailableException ex)
        {
            text.AppendLine($"File table: not read ({ex.Message})");
        }
        watch.Restart();
        var walk = await folders.ScanAsync(root, null, ct).ConfigureAwait(false);
        text.AppendLine(Line("Folder by folder", walk, watch.Elapsed));
        if (table is null) return text.ToString();

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

    private static string Line(string reader, DiskTree t, TimeSpan wall)
    {
        var folders = 0;
        for (var i = 1; i < t.Count; i++)
            if (t.IsFolder(i)) folders++;
        using var process = Process.GetCurrentProcess();
        return string.Create(CultureInfo.InvariantCulture,
            $"{reader}: {t.Size(t.Root)} bytes, {t.Files(t.Root)} files, {folders} folders, {t.Lost} lost, {wall.TotalSeconds:0.0} s (tree {t.Elapsed.TotalSeconds:0.0} s), peak memory {process.PeakWorkingSet64 >> 20} MB");
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
