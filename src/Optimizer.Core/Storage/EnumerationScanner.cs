using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Optimizer.Core.Storage;

/// <summary>
/// Folder walk on several threads with FindFirstFileEx (basic info, large fetch): reads names, sizes and attributes
/// only and never opens a file. Links (junctions, symlinks, mount points) are listed but not entered, so they cannot loop
/// or double count; cloud placeholder folders (OneDrive Files On-Demand) are entered like normal folders.
/// </summary>
public sealed unsafe partial class EnumerationScanner(int threads = 4, long? usedBytes = null) : IVolumeScanner
{
    private const uint AttrSystem = 0x4, AttrDirectory = 0x10, AttrOffline = 0x1000, AttrReparse = 0x400,
        AttrRecallOnOpen = 0x40000, AttrRecallOnDataAccess = 0x400000;
    private const int ReportEvery = 5000;

    private sealed record Work(int Parent, string Name, string Path, DateTime LastWrite, EntryFlags Flags);

    private readonly record struct Found(string Name, bool Folder, bool Link, long Size, DateTime Write, EntryFlags Flags);

    public Task<DiskTree> ScanAsync(string root, IProgress<ScanProgress>? progress, CancellationToken ct) =>
        Task.Run(() => Scan(root, progress, ct), ct);

    private DiskTree Scan(string root, IProgress<ScanProgress>? progress, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var watch = Stopwatch.StartNew();
        var used = usedBytes ?? UsedBytesOfDrive(root);
        var builder = new DiskTree.Builder(root);
        var gate = new object();
        var queue = new Queue<Work>();
        var busy = 0;
        var files = 0;
        long bytes = 0;
        var nextReport = ReportEvery;
        var lost = 0;
        Exception? failure = null;
        var stop = false;

        // The root is entered like any folder; its own entry (index 0) already exists in the builder.
        queue.Enqueue(new Work(-1, root, ExtendedPath(root), default, EntryFlags.None));

        void Report()
        {
            double? percent = used is > 0 ? Math.Min(100.0, bytes * 100.0 / used.Value) : null;
            progress?.Report(new ScanProgress(percent, files, bytes, ScanReader.Folders));
        }

        void Worker()
        {
            while (true)
            {
                Work item;
                lock (gate)
                {
                    while (queue.Count == 0 && busy > 0 && !stop) Monitor.Wait(gate);
                    if (stop || queue.Count == 0) { Monitor.PulseAll(gate); return; }
                    item = queue.Dequeue();
                    busy++;
                }
                try
                {
                    ct.ThrowIfCancellationRequested();
                    ReadFolder(item);
                }
                catch (Exception ex)
                {
                    lock (gate) { failure ??= ex; stop = true; }
                }
                finally
                {
                    lock (gate) { busy--; Monitor.PulseAll(gate); }
                }
            }
        }

        void ReadFolder(Work item)
        {
            var found = new List<Found>();
            var unreadable = !List(item.Path, found, ct);
            lock (gate)
            {
                // A child entry is added when its folder is read, so a folder that cannot be opened can carry Unreadable.
                var self = item.Parent < 0 ? builder.Root
                    : builder.AddFolder(item.Parent, item.Name, item.LastWrite, item.Flags | (unreadable ? EntryFlags.Unreadable : 0));
                if (unreadable) lost++;
                foreach (var e in found)
                {
                    if (e.Folder)
                    {
                        if (e.Link) builder.AddFolder(self, e.Name, e.Write, e.Flags | EntryFlags.ReparsePoint);
                        else queue.Enqueue(new Work(self, e.Name, item.Path.TrimEnd('\\') + "\\" + e.Name, e.Write, e.Flags));
                    }
                    else
                    {
                        builder.AddFile(self, e.Name, e.Size, -1, e.Write, e.Flags);
                        files++;
                        bytes += e.Size;
                        if (files >= nextReport) { nextReport = files + ReportEvery; Report(); }
                    }
                }
                Monitor.PulseAll(gate);
            }
        }

        var workers = Enumerable.Range(0, Math.Max(1, threads))
            .Select(_ => new Thread(Worker) { IsBackground = true }).ToList();
        foreach (var w in workers) w.Start();
        foreach (var w in workers) w.Join();

        ct.ThrowIfCancellationRequested();
        if (failure is not null) throw failure;
        Report();
        return builder.Build(ScanReader.Folders, watch.Elapsed, lost);
    }

    /// <summary>Lists one folder; false when it cannot be opened.</summary>
    private static bool List(string path, List<Found> into, CancellationToken ct)
    {
        WIN32_FIND_DATAW data;
        var handle = FindFirstFileExW(path.TrimEnd('\\') + "\\*", 1 /* FindExInfoBasic */, &data, 0, IntPtr.Zero, 2 /* LARGE_FETCH */);
        if (handle == new IntPtr(-1)) return false;
        try
        {
            do
            {
                var name = new string(data.cFileName);
                if (name is "." or "..") continue;
                var a = data.dwFileAttributes;
                var flags = EntryFlags.None;
                if ((a & AttrSystem) != 0) flags |= EntryFlags.System;
                if ((a & (AttrRecallOnDataAccess | AttrRecallOnOpen | AttrOffline)) != 0) flags |= EntryFlags.CloudOnly;
                var size = ((long)data.nFileSizeHigh << 32) | data.nFileSizeLow;
                into.Add(new Found(name, (a & AttrDirectory) != 0, IsLink(a, data.dwReserved0), size, ToUtc(data.ftLastWriteTime), flags));
                if (into.Count % 256 == 0) ct.ThrowIfCancellationRequested();
            } while (FindNextFileW(handle, &data));
        }
        finally { FindClose(handle); }
        return true;
    }

    /// <summary>
    /// A link is a reparse point whose tag is a name surrogate (bit 0x20000000: junction, symlink, mount point). Other
    /// reparse points, like the OneDrive cloud placeholders, hold real children and are entered.
    /// </summary>
    internal static bool IsLink(uint attributes, uint reparseTag) =>
        (attributes & AttrReparse) != 0 && (reparseTag & 0x20000000) != 0;

    private static DateTime ToUtc(long fileTime)
    {
        try { return fileTime > 0 ? DateTime.FromFileTimeUtc(fileTime) : default; }
        catch (ArgumentOutOfRangeException) { return default; }
    }

    /// <summary>Win32 path with the long-path prefix, so paths beyond 260 characters work.</summary>
    private static string ExtendedPath(string path)
    {
        var full = Path.GetFullPath(path);
        if (full.StartsWith(@"\\?\", StringComparison.Ordinal)) return full;
        return full.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + full[2..] : @"\\?\" + full;
    }

    private static long? UsedBytesOfDrive(string root)
    {
        try
        {
            var full = Path.GetFullPath(root);
            if (!string.Equals(Path.GetPathRoot(full), full, StringComparison.OrdinalIgnoreCase)) return null;
            var drive = new DriveInfo(full);
            return drive.IsReady ? drive.TotalSize - drive.TotalFreeSpace : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return null; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct WIN32_FIND_DATAW
    {
        public uint dwFileAttributes;
        public long ftCreationTime;
        public long ftLastAccessTime;
        public long ftLastWriteTime;
        public uint nFileSizeHigh;
        public uint nFileSizeLow;
        public uint dwReserved0;
        public uint dwReserved1;
        public fixed char cFileName[260];
        public fixed char cAlternateFileName[14];
    }

    [LibraryImport("kernel32.dll", EntryPoint = "FindFirstFileExW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial IntPtr FindFirstFileExW(string fileName, int infoLevel, WIN32_FIND_DATAW* data, int searchOp, IntPtr searchFilter, int additionalFlags);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FindNextFileW(IntPtr handle, WIN32_FIND_DATAW* data);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FindClose(IntPtr handle);
}
