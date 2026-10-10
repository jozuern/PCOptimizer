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

    /// <summary>The true original: written once, at the first apply (first-original rule).</summary>
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

/// <summary>An undo whose restored values take effect after the next restart (<see cref="BackupStore.GetPendingUndo"/>).</summary>
public sealed class PendingUndo
{
    public required string TweakId { get; init; }

    /// <summary>Time of the undo; a boot after it means the restored values are in effect.</summary>
    public DateTimeOffset Since { get; init; } = DateTimeOffset.Now;

    /// <summary>The restored entries: their originals are what the system will have after the restart.</summary>
    public List<BackupEntry> Entries { get; init; } = [];
}

/// <summary>
/// JSON backups in %ProgramData%\PCOptimizer\backups. The folder is locked to Administrators + SYSTEM (no inheritance):
/// an elevated app restoring values from user-writable files would be a privilege-escalation path.
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
        Directory.CreateDirectory(PendingUndoFolder);
        Directory.CreateDirectory(ExportFolder);
    }

    public static string DefaultRoot => Platform.DataPaths.Root;

    public string Root { get; }
    public string BackupFolder => Path.Combine(Root, "backups");
    public string HistoryFolder => Path.Combine(Root, "backups", "history");
    public string PendingUndoFolder => Path.Combine(Root, "backups", "pending-undo");
    public string ExportFolder => Path.Combine(Root, "exports");

    /// <summary>
    /// The backup file of a tweak. Ids that need replaced characters get a short hash of the real id, so two ids never
    /// share a file; a file under the older plain name is still found.
    /// </summary>
    private string FileFor(string tweakId)
    {
        var name = Sanitize(tweakId);
        if (name == tweakId) return Path.Combine(BackupFolder, name + ".json");
        var hashed = Path.Combine(BackupFolder, $"{name}-{IdHash(tweakId)}.json");
        var legacy = Path.Combine(BackupFolder, name + ".json");
        return !File.Exists(hashed) && File.Exists(legacy) && Read(legacy)?.TweakId == tweakId ? legacy : hashed;
    }

    private static string IdHash(string id) =>
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(id)))[..8].ToLowerInvariant();

    /// <summary>Suffix of a backup file that could not be read: kept for the user, never overwritten.</summary>
    public const string DamagedSuffix = ".damaged";

    public TweakBackup? Get(string tweakId)
    {
        var file = FileFor(tweakId);
        if (!File.Exists(file) || !Trusted(file)) return null;
        var (backup, damaged) = TryRead(file);
        if (damaged) Quarantine(file);
        return backup;
    }

    /// <summary>
    /// The tweak's backup file exists but cannot be read (cut off by a power loss, edited by hand). It still holds the
    /// only copy of the originals, so the tweak must not be applied again until the user removes it.
    /// </summary>
    public bool IsDamaged(string tweakId) => File.Exists(DamagedFile(tweakId));

    public string DamagedFile(string tweakId) => FileFor(tweakId) + DamagedSuffix;

    public IReadOnlyList<TweakBackup> All()
    {
        var list = new List<TweakBackup>();
        foreach (var file in Directory.EnumerateFiles(BackupFolder, "*.json").Where(Trusted))
        {
            var (backup, damaged) = TryRead(file);
            if (backup is not null) list.Add(backup);
            else if (damaged) Quarantine(file);
        }
        return list.OrderByDescending(b => b.LastApplied).ToList();
    }

    public void Save(TweakBackup backup) => WriteDurably(FileFor(backup.TweakId), JsonSerializer.Serialize(backup, Json));

    /// <summary>
    /// Written to a temporary file, flushed to the disk and then moved over the old file: a power loss right after a
    /// boot configuration change leaves either the old or the new backup, never an empty one.
    /// </summary>
    private static void WriteDurably(string file, string json)
    {
        var tmp = file + ".tmp";
        using (var stream = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
        using (var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false)))
        {
            writer.Write(json);
            writer.Flush();
            stream.Flush(flushToDisk: true);
        }
        File.Move(tmp, file, overwrite: true);
    }

    private static void Quarantine(string file)
    {
        try
        {
            var target = file + DamagedSuffix;
            if (File.Exists(target)) target = $"{file}-{DateTime.Now:yyyyMMdd-HHmmss}{DamagedSuffix}";
            File.Move(file, target);
            Log.Error("backup", $"unreadable backup moved to {target}; the tweak cannot be applied again until it is removed");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("backup", $"unreadable backup {file} could not be moved aside: {ex.Message}");
        }
    }

    /// <summary>After a complete undo the record moves to history (kept for the change log).</summary>
    public void Archive(string tweakId)
    {
        var file = FileFor(tweakId);
        if (!File.Exists(file)) return;
        File.Move(file, Path.Combine(HistoryFolder, $"{Sanitize(tweakId)}-{DateTime.Now:yyyyMMdd-HHmmss}.json"), overwrite: true);
    }

    // ---------------- undo that takes effect after a restart ----------------

    private string PendingUndoFile(string tweakId) => Path.Combine(PendingUndoFolder, Sanitize(tweakId) + ".json");

    /// <summary>
    /// An undone change whose restored value takes effect only after a restart (memory compression). Kept apart from the
    /// backups, so the Changes page, Undo all and drift detection do not see it as a change that is still applied.
    /// </summary>
    public PendingUndo? GetPendingUndo(string tweakId)
    {
        var file = PendingUndoFile(tweakId);
        if (!File.Exists(file) || !Trusted(file)) return null;
        try
        {
            return JsonSerializer.Deserialize<PendingUndo>(File.ReadAllText(file), Json);
        }
        catch (Exception ex)
        {
            Log.Error("backup", $"unreadable pending undo {file}", ex);
            return null;
        }
    }

    public void SavePendingUndo(PendingUndo pending) => WriteDurably(PendingUndoFile(pending.TweakId), JsonSerializer.Serialize(pending, Json));

    public void ClearPendingUndo(string tweakId)
    {
        var file = PendingUndoFile(tweakId);
        if (File.Exists(file)) File.Delete(file);
    }

    private bool Trusted(string file)
    {
        if (!_secure) return true;
        if (SecureFolder.IsOwnedByAdmins(file)) return true;
        Log.Warn("backup", $"ignored backup file not owned by Administrators/SYSTEM: {file}");
        return false;
    }

    private static TweakBackup? Read(string file) => TryRead(file).Backup;

    /// <summary>Damaged: the content cannot be parsed. A file that is only locked for a moment is not damaged.</summary>
    private static (TweakBackup? Backup, bool Damaged) TryRead(string file)
    {
        try
        {
            var backup = JsonSerializer.Deserialize<TweakBackup>(File.ReadAllText(file), Json);
            return backup is { TweakId.Length: > 0 } ? (backup, false) : (null, true);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException or ArgumentException or InvalidOperationException)
        {
            Log.Error("backup", $"unreadable backup {file}", ex);
            return (null, true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("backup", $"backup {file} not readable right now: {ex.Message}");
            return (null, false);
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
