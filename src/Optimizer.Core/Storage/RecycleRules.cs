using System.Runtime.InteropServices;
using Optimizer.Core.Platform;
using Optimizer.Core.Tools;

namespace Optimizer.Core.Storage;

/// <summary>Why the storage page does not offer a file or folder for the Recycle Bin.</summary>
public enum RecycleBlock { Protected, ContainsProtected, KeptFolder, SystemFile }

/// <summary>
/// What the storage page may move to the Recycle Bin. The app runs elevated, so a folder is refused when it is, or holds,
/// a system or program location (protected roots, AppData) or a folder a user expects to stay (drive roots, profiles,
/// Desktop, Documents, Downloads and the like): the user can empty those, but not delete them as a whole.
/// </summary>
public static class RecycleRules
{
    private static readonly string[] ProfileSubfolders = ["Desktop", "Documents", "Downloads", "Pictures", "Music", "Videos", "Saved Games"];

    /// <summary>Known folder ids of Desktop, Documents, Downloads, Pictures, Music, Videos and Saved Games (they can be redirected).</summary>
    private static readonly Guid[] KnownFolderIds =
    [
        new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641"), new("FDD39AD0-238F-46AF-ADB4-6C85480369C7"), new("374DE290-123F-4565-9164-39C4925E467B"),
        new("33E28130-4E1E-4676-835A-98395C3BC3BB"), new("4BD8D571-6D19-48D3-BE97-422220080E43"), new("18989B1D-99B5-455B-841C-AB7C74E4DDFC"),
        new("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4"),
    ];

    /// <summary>Folders that stay, as full paths with a trailing backslash (case-insensitive set).</summary>
    public static IReadOnlyCollection<string> KeptFolders()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try
            {
                set.Add(Normalize(path).TrimEnd('\\') + "\\");
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
            }
        }

        foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed)) Add(d.RootDirectory.FullName);

        var profiles = Reg.HklmUnexpanded(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList", "ProfilesDirectory") is { Length: > 0 } configured
            ? MachineEnvironment.Expand(configured)
            : Path.GetDirectoryName(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\'));
        Add(profiles);
        if (!string.IsNullOrEmpty(profiles) && Directory.Exists(profiles))
        {
            try
            {
                foreach (var root in Directory.EnumerateDirectories(profiles))
                {
                    Add(root);
                    foreach (var sub in ProfileSubfolders) Add(Path.Combine(root, sub));
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }

        Add(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        foreach (var id in KnownFolderIds) Add(KnownFolder(id));
        return set;
    }

    /// <summary>
    /// The first reason a path is not offered, or null. Order: system file, kept folder, protected, folder holds something
    /// protected. The roots and kept folders are normalized like the path, so a different spelling of the same folder
    /// (8.3 name, <c>..</c>, case) still matches.
    /// </summary>
    /// <param name="appData">Also refuse every AppData folder, and a folder that has one directly inside; off only in tests, whose temp folder lies there.</param>
    public static RecycleBlock? Check(string path, bool folder, bool systemFile, IReadOnlyList<string> protectedRoots,
        IReadOnlyCollection<string> keptFolders, bool appData = true) =>
        CheckNormalized(path, folder, systemFile, NormalizeAll(protectedRoots), NormalizeAll(keptFolders), appData);

    /// <summary>The lists in the form <see cref="CheckNormalized"/> expects: normalized, trailing backslash. Done once per batch.</summary>
    internal static string[] NormalizeAll(IEnumerable<string> folders) =>
        folders.Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => Normalize(f).TrimEnd('\\') + "\\").Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

    /// <summary><see cref="Check"/> for lists that <see cref="NormalizeAll"/> already normalized.</summary>
    internal static RecycleBlock? CheckNormalized(string path, bool folder, bool systemFile, IReadOnlyList<string> protectedRoots,
        IReadOnlyCollection<string> keptFolders, bool appData)
    {
        if (systemFile) return RecycleBlock.SystemFile;
        var full = Normalize(path).TrimEnd('\\') + "\\";
        if (folder && keptFolders.Contains(full, StringComparer.OrdinalIgnoreCase)) return RecycleBlock.KeptFolder;
        if (StorageAnalyzer.IsProtected(full, protectedRoots, appData)) return RecycleBlock.Protected;
        if (!folder) return null;

        bool Under(string other) => other.Length > full.Length && other.StartsWith(full, StringComparison.OrdinalIgnoreCase);
        if (protectedRoots.Any(Under) || keptFolders.Any(Under)) return RecycleBlock.ContainsProtected;
        if (appData && Directory.Exists(Path.Combine(full, "AppData"))) return RecycleBlock.ContainsProtected;
        return null;
    }

    /// <summary>
    /// The full path without a trailing backslash, with <c>..</c> resolved and 8.3 short names expanded: the form that
    /// is checked and that is moved, so a spelling cannot slip past the roots and kept folders.
    /// </summary>
    public static string Normalize(string path)
    {
        var full = SafeDelete.LongPath(Path.GetFullPath(path));
        // A drive root keeps its backslash: "C:" alone means the current folder of that drive.
        return full.Length <= 3 ? full : full.TrimEnd('\\');
    }

    private static string? KnownFolder(Guid id)
    {
        try
        {
            if (SHGetKnownFolderPath(ref id, 0, IntPtr.Zero, out var pointer) != 0) return null;
            try
            {
                return Marshal.PtrToStringUni(pointer);
            }
            finally
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }
        catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException)
        {
            return null;
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderPath(ref Guid rfid, uint flags, IntPtr token, out IntPtr path);
}
