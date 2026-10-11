using System.Runtime.InteropServices;
using System.Text;

namespace Optimizer.Core.Platform;

/// <summary>
/// The app's entry in the signed-in user's Start menu ("PCOptimizer.lnk" in Programs). Windows Search lists apps from
/// the Start menu, and a portable exe (also one winget installed) has no entry there otherwise.
/// </summary>
public static class StartMenuShortcut
{
    public const string FileName = "PCOptimizer.lnk";

    /// <summary>
    /// Start menu programs folder of the signed-in user. Under Administrator protection or a separate admin account this
    /// process runs as another user, whose Start menu the signed-in user never sees: the folder then comes from the
    /// signed-in user's profile.
    /// </summary>
    public static string? ProgramsFolder(ElevationInfo? elevation, string? sessionProfile) =>
        elevation is { UserMismatch: true }
            ? sessionProfile is null ? null : Path.Combine(sessionProfile, @"AppData\Roaming\Microsoft\Windows\Start Menu\Programs")
            : Environment.GetFolderPath(Environment.SpecialFolder.Programs);

    public static bool Exists(string folder) => File.Exists(Path.Combine(folder, FileName));

    /// <summary>Exe the shortcut starts, or null when there is none.</summary>
    public static string? Target(string folder)
    {
        var path = Path.Combine(folder, FileName);
        if (!File.Exists(path)) return null;
        var link = (IShellLinkW)new ShellLink();
        try
        {
            ((IPersistFile)link).Load(path, 0);
            var sb = new StringBuilder(1024);
            link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
            return sb.ToString();
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
    }

    /// <summary>
    /// Writes the shortcut. The folder belongs to the signed-in user, who can replace it or a file in it with a link while
    /// the elevated app writes: the shortcut is saved in the app's own temp folder first and then copied into a file that
    /// is created new and checked for its real path (<see cref="SafeDelete.CreateNew"/>), so it can only land in the
    /// Programs folder itself. An earlier shortcut is deleted through <see cref="SafeDelete"/> first.
    /// </summary>
    /// <param name="workFolder">Where the shortcut is saved first; null uses the tools' TEMP folder (admin-only in the app).</param>
    public static void Create(string folder, string exe, string description, string? workFolder = null)
    {
        if (!Directory.Exists(folder)) throw new DirectoryNotFoundException($"{folder} does not exist");
        workFolder ??= ProcessHardening.TempFolder ?? Path.GetTempPath();
        var draft = Path.Combine(workFolder, $"{Guid.NewGuid():N}.lnk");
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(exe);
            link.SetWorkingDirectory(Path.GetDirectoryName(exe) ?? "");
            link.SetDescription(description);
            link.SetIconLocation(exe, 0);
            ((IPersistFile)link).Save(draft, true);
        }
        finally
        {
            Marshal.FinalReleaseComObject(link);
        }
        try
        {
            var bytes = File.ReadAllBytes(draft);
            Remove(folder);
            using var target = SafeDelete.CreateNew(Path.Combine(folder, FileName));
            target.Write(bytes);
        }
        finally
        {
            File.Delete(draft);
        }
    }

    /// <summary>Deletes the shortcut through <see cref="SafeDelete"/>: a link in its path is refused instead of followed.</summary>
    public static void Remove(string folder)
    {
        var path = Path.Combine(folder, FileName);
        if (File.Exists(path) || Directory.Exists(path)) SafeDelete.DeleteFile(path);
    }

    [ComImport, Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink;

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("0000010b-0000-0000-C000-000000000046")]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
