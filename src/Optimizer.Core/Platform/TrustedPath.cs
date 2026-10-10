using System.Security.AccessControl;
using System.Security.Principal;

namespace Optimizer.Core.Platform;

/// <summary>
/// Decides whether a program may run elevated: the file and every folder above it must be owned by Administrators,
/// SYSTEM or TrustedInstaller, and no one else may change, replace or add files there (a DLL placed next to a program is
/// loaded by it). Anything else is started as the signed-in user instead.
/// </summary>
public static class TrustedPath
{
    private static readonly SecurityIdentifier Admins = new(WellKnownSidType.BuiltinAdministratorsSid, null);
    private static readonly SecurityIdentifier LocalSystem = new(WellKnownSidType.LocalSystemSid, null);
    private static readonly SecurityIdentifier TrustedInstaller = new("S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464");
    private static readonly SecurityIdentifier CreatorOwner = new(WellKnownSidType.CreatorOwnerSid, null);

    private const FileSystemRights Changing =
        FileSystemRights.WriteData | FileSystemRights.AppendData | FileSystemRights.WriteExtendedAttributes | FileSystemRights.WriteAttributes |
        FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;

    private static bool IsTrusted(IdentityReference? sid) => sid == Admins || sid == LocalSystem || sid == TrustedInstaller;

    public static bool IsAdminOnlyWritable(string path)
    {
        try
        {
            var full = Path.GetFullPath(path);
            if (!File.Exists(full) || !SafeDelete.HasNoLinks(full)) return false;
            if (!Check(new FileInfo(full).GetAccessControl())) return false;
            return FolderChainIsAdminOnly(new DirectoryInfo(Path.GetDirectoryName(full)!));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return false;
        }
    }

    /// <summary>A folder (for example a PATH entry) in which only Administrators, SYSTEM or TrustedInstaller can add or change files.</summary>
    public static bool IsAdminOnlyWritableFolder(string folder)
    {
        try
        {
            var dir = new DirectoryInfo(Path.GetFullPath(folder));
            return dir.Exists && FolderChainIsAdminOnly(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or System.Security.SecurityException)
        {
            return false;
        }
    }

    private static bool FolderChainIsAdminOnly(DirectoryInfo start)
    {
        for (var dir = start; dir is not null; dir = dir.Parent)
        {
            // The drive root lets users create folders by default; what matters is that they cannot change the folders below.
            if (dir.Parent is null) break;
            if ((dir.Attributes & FileAttributes.ReparsePoint) != 0 || !Check(dir.GetAccessControl())) return false;
        }
        return true;
    }

    /// <summary>GENERIC_ALL and GENERIC_WRITE: some installers write these generic bits into an entry instead of file rights.</summary>
    private const int GenericWriting = 0x10000000 | 0x40000000;

    private static bool Check(FileSystemSecurity security)
    {
        if (!IsTrusted(security.GetOwner(typeof(SecurityIdentifier)))) return false;
        foreach (FileSystemAccessRule rule in security.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType != AccessControlType.Allow || IsTrusted(rule.IdentityReference)) continue;
            // Inherit-only entries apply to what is created inside, not to this file or folder.
            if ((rule.PropagationFlags & PropagationFlags.InheritOnly) != 0) continue;
            // CREATOR OWNER rights only reach files someone creates, which the other rules decide.
            if (rule.IdentityReference == CreatorOwner) continue;
            if ((rule.FileSystemRights & Changing) != 0 || ((int)rule.FileSystemRights & GenericWriting) != 0) return false;
        }
        return true;
    }
}
