using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Optimizer.Core.Platform;

/// <summary>
/// Deletes files and empty folders in places a standard user can write to, from the elevated process, without
/// following links. The path is opened without following a final reparse point, the handle's real path is compared
/// with the expected one (a folder swapped for a junction after the scan resolves elsewhere and is refused), and the
/// delete goes through the handle, so it hits exactly the object that was checked.
/// </summary>
public static class SafeDelete
{
    /// <summary>True when the path and every existing parent folder are real folders (no junction, symbolic link or mount point).</summary>
    public static bool HasNoLinks(string path)
    {
        try
        {
            var full = Path.GetFullPath(path).TrimEnd('\\');
            var root = Path.GetPathRoot(full);
            for (var p = full; !string.IsNullOrEmpty(p) && !string.Equals(p.TrimEnd('\\'), root?.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); p = Path.GetDirectoryName(p))
            {
                if (!Path.Exists(p)) continue;
                if ((File.GetAttributes(p) & FileAttributes.ReparsePoint) != 0) return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// True when the file exists, no part of its path is a link, and its handle's real path is the path itself. For
    /// callers that must hand the path to another API (the Recycle Bin), checked right before that call.
    /// </summary>
    public static bool ResolvesToItself(string path)
    {
        if (!HasNoLinks(path)) return false;
        try
        {
            var expected = LongPath(Path.GetFullPath(path)).TrimEnd('\\');
            using var handle = CreateFileW(@"\\?\" + expected, FileReadAttributes, FileShareRead | FileShareWrite | FileShareDelete, IntPtr.Zero, OpenExisting, FileFlagOpenReparsePoint, IntPtr.Zero);
            return !handle.IsInvalid && string.Equals(FinalPath(handle), expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return false;
        }
    }

    /// <summary>Deletes the file (read-only included). Throws IOException when the path does not resolve to itself.</summary>
    public static void DeleteFile(string path) => Delete(path, directory: false);

    /// <summary>Deletes the folder if it is empty (the call fails for a non-empty folder).</summary>
    public static void DeleteEmptyDirectory(string path) => Delete(path, directory: true);

    private static void Delete(string path, bool directory)
    {
        // Long names: the final path never contains 8.3 short names (C:\Users\EXAMPL~1), the input may.
        var expected = LongPath(Path.GetFullPath(path)).TrimEnd('\\');
        var flags = FileFlagOpenReparsePoint | (directory ? FileFlagBackupSemantics : 0);
        using var handle = CreateFileW(@"\\?\" + expected, Delete_ | FileReadAttributes, FileShareRead | FileShareWrite | FileShareDelete, IntPtr.Zero, OpenExisting, flags, IntPtr.Zero);
        if (handle.IsInvalid) throw new IOException($"cannot open {expected}", new Win32Exception(Marshal.GetLastWin32Error()));

        var final = FinalPath(handle);
        if (!string.Equals(final, expected, StringComparison.OrdinalIgnoreCase))
            throw new IOException($"{expected} resolves to {final}: not deleted");

        // POSIX semantics (the name goes away at once) and ignore the read-only attribute (Windows 10 1809 and newer).
        var info = new FileDispositionInfoEx { Flags = DispositionDelete | DispositionPosixSemantics | DispositionIgnoreReadOnly };
        if (!SetFileInformationByHandle(handle, FileDispositionInfoExClass, ref info, Marshal.SizeOf<FileDispositionInfoEx>()))
            throw new IOException($"cannot delete {expected}", new Win32Exception(Marshal.GetLastWin32Error()));
    }

    // Both APIs return the needed size (with the terminating null) when the buffer is too small; paths can be up to
    // 32767 characters with the \\?\ prefix.
    private static string LongPath(string path)
    {
        var buffer = new char[1024];
        var length = GetLongPathNameW(@"\\?\" + path, buffer, (uint)buffer.Length);
        if (length >= buffer.Length)
        {
            buffer = new char[length];
            length = GetLongPathNameW(@"\\?\" + path, buffer, (uint)buffer.Length);
        }
        if (length == 0 || length >= buffer.Length) return path;
        var s = new string(buffer, 0, (int)length);
        return s.StartsWith(@"\\?\", StringComparison.Ordinal) ? s[4..] : s;
    }

    private static string FinalPath(SafeFileHandle handle)
    {
        var buffer = new char[1024];
        var length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Length, 0);
        if (length >= buffer.Length)
        {
            buffer = new char[length];
            length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Length, 0);
        }
        if (length == 0 || length >= buffer.Length) throw new IOException("cannot read the final path", new Win32Exception(Marshal.GetLastWin32Error()));
        var s = new string(buffer, 0, (int)length);
        if (s.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) return @"\\" + s[8..];
        return s.StartsWith(@"\\?\", StringComparison.Ordinal) ? s[4..] : s;
    }

    private const uint Delete_ = 0x00010000, FileReadAttributes = 0x80;
    private const uint FileShareRead = 1, FileShareWrite = 2, FileShareDelete = 4, OpenExisting = 3;
    private const uint FileFlagOpenReparsePoint = 0x00200000, FileFlagBackupSemantics = 0x02000000;
    private const int FileDispositionInfoExClass = 21;
    private const uint DispositionDelete = 0x1, DispositionPosixSemantics = 0x2, DispositionIgnoreReadOnly = 0x10;

    [StructLayout(LayoutKind.Sequential)]
    private struct FileDispositionInfoEx
    {
        public uint Flags;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLongPathNameW(string shortPath, [Out] char[] buffer, uint size);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, [Out] char[] buffer, uint size, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetFileInformationByHandle(SafeFileHandle handle, int infoClass, ref FileDispositionInfoEx info, int size);
}
