using Optimizer.Core.Logging;
using Optimizer.Core.Storage.Mft;

namespace Optimizer.Core.Storage;

/// <summary>
/// Picks the reader for a scan: the file table (<see cref="MftScanner"/>) for the root of an NTFS drive when the process
/// is elevated, otherwise folder by folder (<see cref="EnumerationScanner"/>). When the file table cannot be read whole,
/// or its reader fails in any other way than being stopped, the folder walk starts from scratch, so a result never mixes
/// the two.
/// </summary>
public static class VolumeScanners
{
    public static Task<DiskTree> ScanAsync(string root, bool elevated, IProgress<ScanProgress>? progress, CancellationToken ct) =>
        ScanAsync(root, UsesFileTable(root, elevated), progress, ct, new MftScanner(), new EnumerationScanner());

    /// <summary>Whether a scan of <paramref name="root"/> tries the file table first.</summary>
    internal static bool UsesFileTable(string root, bool elevated) => elevated && IsNtfsDriveRoot(root);

    internal static async Task<DiskTree> ScanAsync(string root, bool fileTable, IProgress<ScanProgress>? progress, CancellationToken ct,
        IVolumeScanner mft, IVolumeScanner folders)
    {
        if (fileTable)
        {
            try
            {
                return await mft.ScanAsync(root, progress, ct).ConfigureAwait(false);
            }
            catch (MftUnavailableException ex)
            {
                Log.Warn("storage", "file table not read, reading folder by folder", new { reason = ex.Message });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A fault of the reader itself (a parse it did not expect, memory): the folder walk still gives a result.
                // The type and message only: the reader's own messages name no file.
                Log.Warn("storage", "file table reader failed, reading folder by folder", new { type = ex.GetType().Name, reason = ex.Message });
            }
        }
        return await folders.ScanAsync(root, progress, ct).ConfigureAwait(false);
    }

    /// <summary>A local drive letter root such as "C:\" (not a folder, not a network share).</summary>
    internal static bool IsDriveRoot(string root)
    {
        try
        {
            var full = Path.GetFullPath(root);
            return full.Length == 3 && char.IsAsciiLetter(full[0]) && full[1] == ':' && full[2] == '\\';
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException) { return false; }
    }

    private static bool IsNtfsDriveRoot(string root)
    {
        if (!IsDriveRoot(root)) return false;
        try
        {
            var drive = new DriveInfo(root);
            return drive.IsReady && drive.DriveFormat == "NTFS";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException) { return false; }
    }
}
