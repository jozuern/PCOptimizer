using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Optimizer.Core.Actions;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Backup;

public sealed class BackupEntry
{
    public required string TargetKey { get; init; }
    public required string Description { get; init; }

    /// <summary>The true original: written once, at the first apply (first-original rule, plan v4 §4.4).</summary>
    public required StoredValue Original { get; init; }

    /// <summary>The value after our last apply. Undo skips targets whose value no longer matches (Windows reset it).</summary>
    public StoredValue? Applied { get; set; }

    /// <summary>
    /// JSON of the exact action that changed this target (for example the DNS action of one adapter). Undo uses it when
    /// the tweak no longer expands to this target (adapter not connected, game list changed), so no original is lost.
    /// </summary>
    public string? Action { get; set; }
}

public sealed class TweakBackup
{
    public required string TweakId { get; init; }
    public DateTimeOffset Created { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset LastApplied { get; set; } = DateTimeOffset.Now;
    public int WindowsBuild { get; init; }
    public string AppVersion { get; init; } = "";
    public string CatalogVersion { get; init; } = "";
    public List<BackupEntry> Entries { get; init; } = [];

    /// <summary>Windows version ("26300.9550") of the last apply, to tell a reset by an update from other causes.</summary>
    public string? AppliedOnVersion { get; set; }

    /// <summary>Set for tweaks that take effect after a restart; cleared once the boot time changes.</summary>
    public DateTimeOffset? PendingRestartSince { get; set; }

    /// <summary>Files exported before the change (BCD store, power scheme).</summary>
    public List<string> Exports { get; init; } = [];

    /// <summary>
    /// JSON of the tweak definition for tweaks built at runtime (startup entries, services, features, device and game
    /// tweaks, fixes). Lets undo work after a restart, when the page that built the tweak is not open.
    /// </summary>
    public string? Definition { get; set; }

    public BackupEntry? Entry(string targetKey) => Entries.FirstOrDefault(e => e.TargetKey == targetKey);
}

/// <summary>
/// JSON backups in %ProgramData%\PCOptimizer\backups. The folder is locked to Administrators + SYSTEM (no inheritance):
/// an elevated app restoring values from user-writable files would be a privilege-escalation path (plan v4 §4.4).
/// </summary>
public sealed class BackupStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private readonly bool _secure;

    public BackupStore(string root, bool secure = true)
    {
        Root = root;
        _secure = secure;
        // Locked before the subfolders are created, so new folders inherit the locked permissions.
        if (secure) SecureFolder.Lock(root);
        Directory.CreateDirectory(BackupFolder);
        Directory.CreateDirectory(HistoryFolder);
        Directory.CreateDirectory(ExportFolder);
    }

    public static string DefaultRoot => Platform.DataPaths.Root;

    public string Root { get; }
    public string BackupFolder => Path.Combine(Root, "backups");
    public string HistoryFolder => Path.Combine(Root, "backups", "history");
    public string ExportFolder => Path.Combine(Root, "exports");

    private string FileFor(string tweakId) => Path.Combine(BackupFolder, Sanitize(tweakId) + ".json");

    public TweakBackup? Get(string tweakId)
    {
        var file = FileFor(tweakId);
        return File.Exists(file) && Trusted(file) ? Read(file) : null;
    }

    public IReadOnlyList<TweakBackup> All() =>
        Directory.EnumerateFiles(BackupFolder, "*.json")
            .Where(Trusted)
            .Select(Read)
            .OfType<TweakBackup>()
            .OrderByDescending(b => b.LastApplied)
            .ToList();

    public void Save(TweakBackup backup)
    {
        var file = FileFor(backup.TweakId);
        var tmp = file + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(backup, Json));
        File.Move(tmp, file, overwrite: true);
    }

    /// <summary>After a complete undo the record moves to history (kept for the change log).</summary>
    public void Archive(string tweakId)
    {
        var file = FileFor(tweakId);
        if (!File.Exists(file)) return;
        File.Move(file, Path.Combine(HistoryFolder, $"{Sanitize(tweakId)}-{DateTime.Now:yyyyMMdd-HHmmss}.json"), overwrite: true);
    }

    private bool Trusted(string file)
    {
        if (!_secure) return true;
        if (SecureFolder.IsOwnedByAdmins(file)) return true;
        Log.Warn("backup", $"ignored backup file not owned by Administrators/SYSTEM: {file}");
        return false;
    }

    private static TweakBackup? Read(string file)
    {
        try
        {
            return JsonSerializer.Deserialize<TweakBackup>(File.ReadAllText(file), Json);
        }
        catch (Exception ex)
        {
            Log.Error("backup", $"unreadable backup {file}", ex);
            return null;
        }
    }

    private static string Sanitize(string id) => string.Concat(id.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' or '_' ? ch : '_'));
}

public static class SecureFolder
{
    private static readonly SecurityIdentifier Admins = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier System = new(WellKnownSidType.LocalSystemSid, null);

    /// <summary>
    /// Makes the data folder safe before the elevated app writes or reads anything in it (log, settings, backups).
    /// A standard user can create C:\ProgramData\PCOptimizer before the first elevated start: as a junction (every
    /// write would land where the link points) or as a real folder with files in it (planted backups and settings,
    /// and a handle kept open could keep adding files after the permissions change). A link is removed; a folder not
    /// owned by Administrators or SYSTEM was not created by this app and is deleted, never adopted. Returns false
    /// when the folder cannot be made safe; the caller must not use it then.
    /// </summary>
    public static bool PrepareRoot(string folder)
    {
        try
        {
            if (Path.Exists(folder))
            {
                if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
                {
                    Log.Warn("backup", $"{folder} was a link; replaced by a real folder");
                    Directory.Delete(folder);
                }
                else if (!IsOwnedByAdmins(new DirectoryInfo(folder)))
                {
                    Log.Warn("backup", $"{folder} was not created by an administrator; deleted");
                    DeleteTree(folder);
                }
            }
            Lock(folder);
            var info = new DirectoryInfo(folder);
            return info.Exists && (info.Attributes & FileAttributes.ReparsePoint) == 0 && IsOwnedByAdmins(info);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            Log.Error("backup", $"data folder {folder} cannot be secured", ex);
            return false;
        }
    }

    /// <summary>
    /// Administrators + SYSTEM full control, inheritance from ProgramData removed, for the folder and everything in it.
    /// Links below the folder are removed first, then files and folders that Administrators or SYSTEM do not own
    /// (put there by another account) are deleted; the rest gets the Administrators owner and only the inherited
    /// (locked) permissions.
    /// </summary>
    public static void Lock(string folder)
    {
        try
        {
            RemoveLinks(folder);
            RemoveUntrusted(folder);
            Directory.CreateDirectory(folder);
            var info = new DirectoryInfo(folder);
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            const InheritanceFlags inherit = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
            security.AddAccessRule(new FileSystemAccessRule(Admins, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
            security.AddAccessRule(new FileSystemAccessRule(System, FileSystemRights.FullControl, inherit, PropagationFlags.None, AccessControlType.Allow));
            security.SetOwner(Admins);
            info.SetAccessControl(security);

            // Children: owner Administrators, no explicit entries, inheritance on (explicit user entries are dropped).
            foreach (var dir in info.EnumerateDirectories("*", SearchOption.AllDirectories))
            {
                var s = new DirectorySecurity();
                s.SetAccessRuleProtection(isProtected: false, preserveInheritance: false);
                s.SetOwner(Admins);
                dir.SetAccessControl(s);
            }
            foreach (var file in info.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                var s = new FileSecurity();
                s.SetAccessRuleProtection(isProtected: false, preserveInheritance: false);
                s.SetOwner(Admins);
                file.SetAccessControl(s);
            }
        }
        catch (Exception ex)
        {
            Log.Warn("backup", $"could not lock {folder}: {ex.Message}");
        }
    }

    /// <summary>
    /// Deletes junctions and symbolic links at <paramref name="folder"/> and below (only the links, never their
    /// targets), so nothing the elevated app writes or reads there can be redirected.
    /// </summary>
    public static void RemoveLinks(string folder)
    {
        if (!Path.Exists(folder)) return;
        if ((File.GetAttributes(folder) & FileAttributes.ReparsePoint) != 0)
        {
            Log.Warn("backup", $"{folder} was a link; replaced by a real folder");
            Directory.Delete(folder); // removes the link itself
            return;
        }
        var stack = new Stack<string>([folder]);
        while (stack.Count > 0)
        {
            foreach (var entry in new DirectoryInfo(stack.Pop()).EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true }))
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    Log.Warn("backup", $"removed link {entry.FullName}");
                    entry.Delete(); // a directory link is removed without touching its target
                }
                else if (entry is DirectoryInfo d)
                {
                    stack.Push(d.FullName);
                }
            }
        }
    }

    /// <summary>
    /// Deletes files and folders below <paramref name="folder"/> that neither Administrators nor SYSTEM own. Everything
    /// the elevated app creates is owned by Administrators; anything else was put there by another account and is
    /// never read (a planted backup could make Undo write chosen registry values as administrator). Call after
    /// <see cref="RemoveLinks"/>.
    /// </summary>
    public static void RemoveUntrusted(string folder)
    {
        if (!Directory.Exists(folder)) return;
        var stack = new Stack<DirectoryInfo>([new DirectoryInfo(folder)]);
        while (stack.Count > 0)
        {
            foreach (var entry in stack.Pop().EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true }))
            {
                if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) continue; // RemoveLinks handles links
                if (IsOwnedByAdmins(entry))
                {
                    if (entry is DirectoryInfo trusted) stack.Push(trusted);
                    continue;
                }
                Log.Warn("backup", $"removed {entry.FullName}: not created by an administrator");
                if (entry is DirectoryInfo d)
                {
                    DeleteTree(d.FullName);
                }
                else
                {
                    entry.Attributes = FileAttributes.Normal;
                    entry.Delete();
                }
            }
        }
    }

    private static void DeleteTree(string folder)
    {
        RemoveLinks(folder);
        if (!Directory.Exists(folder)) return;
        foreach (var file in new DirectoryInfo(folder).EnumerateFiles("*", new EnumerationOptions { AttributesToSkip = 0, RecurseSubdirectories = true }))
            file.Attributes = FileAttributes.Normal;
        Directory.Delete(folder, recursive: true);
    }

    public static bool IsOwnedByAdmins(string file) => IsOwnedByAdmins(new FileInfo(file));

    public static bool IsOwnedByAdmins(FileSystemInfo entry)
    {
        try
        {
            var owner = entry switch
            {
                DirectoryInfo d => d.GetAccessControl().GetOwner(typeof(SecurityIdentifier)),
                FileInfo f => f.GetAccessControl().GetOwner(typeof(SecurityIdentifier)),
                _ => null,
            };
            return owner == Admins || owner == System;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
