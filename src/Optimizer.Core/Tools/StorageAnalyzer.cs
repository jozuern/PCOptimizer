using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Optimizer.Core.Logging;
using Optimizer.Core.Storage;

namespace Optimizer.Core.Tools;

public sealed record LargeFile(string Path, long Bytes, DateTime LastWriteUtc, bool CanDelete);

public sealed record FolderSize(string Path, long Bytes, int Files);

public sealed record DuplicateGroup(long BytesEach, IReadOnlyList<string> Paths)
{
    /// <summary>Space freed by keeping one copy.</summary>
    public long Reclaimable => BytesEach * (Paths.Count - 1);
}

public sealed record StorageReport(string Root, long ScannedBytes, int ScannedFiles, IReadOnlyList<LargeFile> LargestFiles, IReadOnlyList<FolderSize> LargestFolders,
    IReadOnlyList<DuplicateGroup> Duplicates, bool Truncated);

/// <summary>
/// Storage analyzer (PC Manager-style): largest files and folders and duplicate files on a drive or folder. Never follows
/// reparse points. Windows, program and game folders are scanned for sizes but are never offered for deletion, and
/// duplicates there are ignored (games ship identical files on purpose). Deleting goes to the Recycle Bin.
/// </summary>
public static class StorageAnalyzer
{
    public const long DuplicateMinBytes = 1L << 20;

    public static IReadOnlyList<string> ProtectedRoots(IEnumerable<string>? gameLibraries)
    {
        var list = new List<string>
        {
            Environment.GetFolderPath(Environment.SpecialFolder.Windows),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        };
        foreach (var d in DriveInfo.GetDrives().Where(d => d.IsReady))
        {
            list.Add(Path.Combine(d.RootDirectory.FullName, "$Recycle.Bin"));
            list.Add(Path.Combine(d.RootDirectory.FullName, "System Volume Information"));
            list.Add(Path.Combine(d.RootDirectory.FullName, "Recovery"));
        }
        list.AddRange(gameLibraries ?? []);
        return list.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.TrimEnd('\\') + "\\").Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Files Windows keeps in the root of a drive (page file, swap file, hibernation file, boot dump log).</summary>
    private static readonly HashSet<string> RootSystemFiles = new(StringComparer.OrdinalIgnoreCase)
    {
        "pagefile.sys", "swapfile.sys", "hiberfil.sys", "DumpStack.log", "DumpStack.log.tmp",
    };

    /// <param name="appData">Also protect every AppData folder; off only in tests, whose temp folder lies there.</param>
    public static bool IsProtected(string path, IReadOnlyList<string> protectedRoots, bool appData = true)
    {
        var trimmed = path.TrimEnd('\\');
        if (RootSystemFiles.Contains(Path.GetFileName(trimmed))
            && string.Equals(Path.GetDirectoryName(trimmed), Path.GetPathRoot(trimmed), StringComparison.OrdinalIgnoreCase))
            return true;
        var p = trimmed + "\\";
        // AppData holds program state (settings, caches, saves): never offered for deletion.
        if (appData && p.Contains(@"\AppData\", StringComparison.OrdinalIgnoreCase)) return true;
        return protectedRoots.Any(r => p.StartsWith(r, StringComparison.OrdinalIgnoreCase));
    }

    public static async Task<StorageReport> ScanAsync(string root, IReadOnlyList<string> protectedRoots, int topFiles = 100, int maxFiles = 3_000_000,
        IProgress<(int Files, long Bytes)>? progress = null, CancellationToken ct = default, bool protectAppData = true)
    {
        return await Task.Run(() =>
        {
            var largest = new PriorityQueue<FileInfo, long>();
            var folderBytes = new Dictionary<string, (long Bytes, int Files)>(StringComparer.OrdinalIgnoreCase);
            var bySize = new Dictionary<long, List<string>>();
            long total = 0;
            var count = 0;
            var truncated = false;
            var rootFull = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            var stack = new Stack<DirectoryInfo>([new DirectoryInfo(rootFull)]);
            var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 };
            while (stack.Count > 0)
            {
                ct.ThrowIfCancellationRequested();
                var dir = stack.Pop();
                IEnumerable<FileSystemInfo> entries;
                try
                {
                    entries = dir.EnumerateFileSystemInfos("*", options).ToList();
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }
                foreach (var e in entries)
                {
                    if ((e.Attributes & FileAttributes.ReparsePoint) != 0) continue;
                    // Cloud files (OneDrive online-only): reading one for the duplicate hash would download it.
                    if (e is FileInfo && ((uint)e.Attributes & CloudOnly) != 0) continue;
                    if (e is DirectoryInfo sub)
                    {
                        stack.Push(sub);
                        continue;
                    }
                    if (e is not FileInfo f) continue;
                    long length;
                    try
                    {
                        length = f.Length;
                    }
                    catch (IOException)
                    {
                        continue;
                    }
                    total += length;
                    count++;
                    if (count >= maxFiles) { truncated = true; stack.Clear(); break; }
                    if (count % 5000 == 0) progress?.Report((count, total));

                    largest.Enqueue(f, length);
                    if (largest.Count > topFiles) largest.Dequeue();

                    // Size per first and second level folder below the root.
                    var relative = f.DirectoryName is { } d && d.Length >= rootFull.Length - 1 ? d[Math.Min(d.Length, rootFull.Length)..] : "";
                    var parts = relative.Split('\\', StringSplitOptions.RemoveEmptyEntries);
                    for (var level = 1; level <= Math.Min(2, parts.Length); level++)
                    {
                        var key = rootFull + string.Join('\\', parts.Take(level));
                        var (b, n) = folderBytes.GetValueOrDefault(key);
                        folderBytes[key] = (b + length, n + 1);
                    }

                    if (length >= DuplicateMinBytes && !IsSystemFile(f) && !IsProtected(f.FullName, protectedRoots, protectAppData))
                    {
                        if (!bySize.TryGetValue(length, out var same)) bySize[length] = same = [];
                        same.Add(f.FullName);
                    }
                }
            }

            var files = new List<LargeFile>();
            while (largest.TryDequeue(out var f, out var len))
                files.Add(new LargeFile(f.FullName, len, f.LastWriteTimeUtc, !IsSystemFile(f) && !IsProtected(f.FullName, protectedRoots, protectAppData)));
            files.Reverse();
            var folders = folderBytes.Select(kv => new FolderSize(kv.Key, kv.Value.Bytes, kv.Value.Files)).OrderByDescending(x => x.Bytes).Take(50).ToList();
            var duplicates = FindDuplicates(bySize.Values.Where(v => v.Count > 1), ct);
            return new StorageReport(rootFull, total, count, files, folders, duplicates, truncated);
        }, ct);
    }

    /// <summary>
    /// Candidates of equal size are compared by the hash of the first 64 KB, then by the full SHA-256. Hard links to one
    /// file count once: recycling one of the paths would free nothing.
    /// </summary>
    public static IReadOnlyList<DuplicateGroup> FindDuplicates(IEnumerable<List<string>> sameSize, CancellationToken ct)
    {
        var result = new List<DuplicateGroup>();
        foreach (var candidates in sameSize)
        {
            ct.ThrowIfCancellationRequested();
            var group = candidates.GroupBy(p => Startup.SignatureVerifier.FileId(p) ?? p, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();
            if (group.Count < 2) continue;
            foreach (var quick in group.GroupBy(p => Hash(p, 64 * 1024, ct)).Where(g => g.Key is not null && g.Count() > 1))
            {
                foreach (var full in quick.GroupBy(p => Hash(p, long.MaxValue, ct)).Where(g => g.Key is not null && g.Count() > 1))
                {
                    var paths = full.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
                    // A file can be deleted while the others are hashed: the size of one that is still there.
                    if (paths.Select(Length).FirstOrDefault(l => l is not null) is { } size) result.Add(new DuplicateGroup(size, paths));
                }
            }
        }
        return result.OrderByDescending(g => g.Reclaimable).ToList();
    }

    private static long? Length(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Files Windows marks as system files (pagefile.sys, hiberfil.sys, swapfile.sys in the drive root) are never offered.</summary>
    private static bool IsSystemFile(FileInfo f) => (f.Attributes & FileAttributes.System) != 0;

    /// <summary>SHA-256 of the first <paramref name="maxBytes"/> bytes, read in 1 MB parts so Stop ends hashing a large file.</summary>
    private static string? Hash(string path, long maxBytes, CancellationToken ct)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[1 << 20];
            long total = 0;
            while (total < maxBytes)
            {
                ct.ThrowIfCancellationRequested();
                var read = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, maxBytes - total));
                if (read == 0) break;
                hash.AppendData(buffer, 0, read);
                total += read;
            }
            return Convert.ToHexString(hash.GetHashAndReset());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>
    /// Moves files to the Recycle Bin (SHFileOperation with undo). Refuses protected paths, system files and paths that
    /// do not resolve to themselves: a parent folder swapped for a junction between scan and confirmation would otherwise
    /// send a system file to the Recycle Bin with administrator rights. The folders of the path stay held while the shell
    /// moves the file (<see cref="Platform.SafeDelete.Pin"/>), so none of them can be swapped in between; a folder emptied
    /// and turned into a mount point in that moment is reported afterwards (the file is then in the Recycle Bin and can be
    /// restored from there). A file that does not fit in the Recycle Bin (larger than its limit, or the bin is set to
    /// delete at once) would be deleted for good without a question: Windows asks first (FOF_WANTNUKEWARNING), and a "No"
    /// counts as a failure. Returns the failures.
    /// </summary>
    /// <param name="appData">Also refuse every AppData folder; off only in tests, whose temp folder lies there.</param>
    /// <param name="progress">The number of files done before each file.</param>
    public static IReadOnlyList<string> Recycle(IEnumerable<string> paths, IReadOnlyList<string> protectedRoots, bool appData = true, IProgress<int>? progress = null)
    {
        var failed = new List<string>();
        var done = 0;
        foreach (var p in paths)
        {
            progress?.Report(done++);
            if (IsProtected(p, protectedRoots, appData) || !File.Exists(p) || (File.GetAttributes(p) & FileAttributes.System) != 0)
            {
                failed.Add(p);
                continue;
            }
            if (!MoveToRecycleBin(p, folder: false)) failed.Add(p);
        }
        return failed;
    }

    /// <summary>
    /// Moves files and folders to the Recycle Bin. Each item is checked again with <see cref="RecycleRules.Check"/>
    /// (protected, kept and system locations are refused, a folder also when it holds one); a folder is moved by renaming
    /// it into the Recycle Bin after its folders are pinned and it is known to hold no link. Returns the failures.
    /// </summary>
    /// <param name="appData">Also refuse every AppData folder; off only in tests, whose temp folder lies there.</param>
    /// <param name="progress">The number of items done before each item.</param>
    public static IReadOnlyList<string> RecycleItems(IReadOnlyList<(string Path, bool Folder)> items, IReadOnlyList<string> protectedRoots,
        IReadOnlyCollection<string> keptFolders, bool appData = true, IProgress<int>? progress = null)
    {
        var failed = new List<string>();
        var done = 0;
        foreach (var (raw, folder) in items)
        {
            progress?.Report(done++);
            string path;
            try
            {
                path = RecycleRules.Normalize(raw);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                failed.Add(raw);
                continue;
            }
            // The normalized path is what is checked and what is moved.
            var exists = folder ? Directory.Exists(path) : File.Exists(path);
            var system = !folder && exists && (File.GetAttributes(path) & FileAttributes.System) != 0;
            if (!exists || RecycleRules.Check(path, folder, system, protectedRoots, keptFolders, appData) is not null || (folder && HoldsFolderLink(path)) || !MoveToRecycleBin(path, folder))
                failed.Add(raw);
        }
        return failed;
    }

    /// <summary>
    /// True when a folder below <paramref name="folder"/> is a junction, symbolic link or mount point (or the walk fails):
    /// such a folder is not recycled as a whole, because what it points to may not be what the user saw. Links are not entered.
    /// </summary>
    internal static bool HoldsFolderLink(string folder)
    {
        var options = new EnumerationOptions { IgnoreInaccessible = false, AttributesToSkip = 0 };
        var stack = new Stack<string>([folder]);
        try
        {
            while (stack.Count > 0)
            {
                foreach (var sub in new DirectoryInfo(stack.Pop()).EnumerateDirectories("*", options))
                {
                    if ((sub.Attributes & FileAttributes.ReparsePoint) != 0) return true;
                    stack.Push(sub.FullName);
                }
            }
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    /// Moves a leftover of an uninstalled program (a folder as a whole, or a shortcut) to the Recycle Bin, with the
    /// same link checks as <see cref="Recycle"/>. Which paths may be offered is decided by
    /// <see cref="Apps.ProgramLeftovers.MayOffer"/>, so the protected roots are not checked here. A folder is moved by
    /// renaming it into the Recycle Bin, which takes a link inside it along as a link and never follows it.
    /// </summary>
    public static bool RecycleLeftover(string path)
    {
        var full = Path.GetFullPath(path).TrimEnd('\\');
        if (Directory.Exists(full)) return Platform.SafeDelete.HasNoLinks(full) && MoveToRecycleBin(full, folder: true);
        return File.Exists(full) && (File.GetAttributes(full) & FileAttributes.System) == 0 && MoveToRecycleBin(full, folder: false);
    }

    private static bool MoveToRecycleBin(string p, bool folder)
    {
        using var pin = Platform.SafeDelete.Pin(Path.GetDirectoryName(Path.GetFullPath(p))!);
        if (pin is null || !(folder ? Platform.SafeDelete.HasNoLinks(p) : Platform.SafeDelete.ResolvesToItself(p))) return false;
        var op = new SHFILEOPSTRUCT
        {
            wFunc = FoDelete,
            pFrom = p + "\0\0",
            fFlags = FofAllowUndo | FofNoConfirmation | FofSilent | FofNoErrorUi | FofWantNukeWarning,
        };
        var result = OnStaThread(() => SHFileOperation(ref op));
        if (!pin.StillReal())
        {
            Log.Error("storage", $"a folder of {p} became a link while it was moved to the Recycle Bin: check the Recycle Bin");
            return false;
        }
        if (result != 0 || op.fAnyOperationsAborted || Path.Exists(p)) return false;
        Log.Info("storage", $"recycled {p}");
        return true;
    }

    /// <summary>
    /// Duplicate groups of which every copy is in <paramref name="selected"/> (the first copy can also be picked in the
    /// list of the largest files). Recycling those would keep no copy at all.
    /// </summary>
    public static IReadOnlyList<DuplicateGroup> WholeGroupsSelected(IReadOnlyCollection<string> selected, IEnumerable<DuplicateGroup> groups)
    {
        var set = new HashSet<string>(selected, StringComparer.OrdinalIgnoreCase);
        return groups.Where(g => g.Paths.Count > 0 && g.Paths.All(set.Contains)).ToList();
    }

    /// <summary>RECALL_ON_DATA_ACCESS, RECALL_ON_OPEN and OFFLINE: the content is not on the disk.</summary>
    private const uint CloudOnly = 0x00400000 | 0x00040000 | 0x00001000;

    /// <summary>
    /// The shell's file operations expect a single-threaded apartment: its question before deleting a file for good (one
    /// that does not fit in the Recycle Bin) belongs to such a thread, not to a thread-pool thread.
    /// </summary>
    private static T OnStaThread<T>(Func<T> work)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) return work();
        T result = default!;
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = work();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        }) { IsBackground = true, Name = "recycle" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error is not null) throw new InvalidOperationException("Recycle Bin operation failed", error);
        return result;
    }

    private const uint FoDelete = 3;
    private const ushort FofSilent = 0x4, FofNoConfirmation = 0x10, FofAllowUndo = 0x40, FofNoErrorUi = 0x400, FofWantNukeWarning = 0x4000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd;
        public uint wFunc;
        public string pFrom;
        public string? pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        public string? lpszProgressTitle;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
}
