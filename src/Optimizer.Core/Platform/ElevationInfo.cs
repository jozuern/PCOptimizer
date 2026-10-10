using System.Runtime.InteropServices;
using System.Security.Principal;
using Optimizer.Core.Interop;

namespace Optimizer.Core.Platform;

/// <summary>
/// Who the process runs as vs. who is signed in. Read-only; the action layer uses it for user-scope writes.
/// </summary>
public sealed record ElevationInfo(
    bool IsElevated,
    string ProcessUser,
    string? ProcessUserSid,
    string? SessionUser,
    string? SessionUserSid,
    bool AdministratorProtection)
{
    public bool UserMismatch => SessionUserSid is not null && ProcessUserSid is not null &&
                                !string.Equals(SessionUserSid, ProcessUserSid, StringComparison.OrdinalIgnoreCase);

    /// <summary>Banner only for the separate-admin-account case; under Administrator protection the mismatch is expected.</summary>
    public bool ShowSeparateAdminBanner => UserMismatch && !AdministratorProtection;

    public static ElevationInfo Read()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var elevated = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        var (sessionUser, sessionSid) = ReadSessionUser();
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var processName = identity.Name;
        // Administrator protection: elevation runs as a hidden system-managed account with a C:\Users\ADMIN_<name> profile.
        var adminProtection = Path.GetFileName(profile).StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase) ||
                              processName.Split('\\').Last().StartsWith("ADMIN_", StringComparison.OrdinalIgnoreCase);
        return new ElevationInfo(elevated, processName, identity.User?.Value, sessionUser, sessionSid, adminProtection);
    }

    private static (string?, string?) ReadSessionUser()
    {
        try
        {
            var user = QueryWts(Native.WtsUserName);
            if (string.IsNullOrEmpty(user)) return (null, null);
            var domain = QueryWts(Native.WtsDomainName);
            var account = string.IsNullOrEmpty(domain) ? user : $"{domain}\\{user}";
            string? sid = null;
            try
            {
                sid = ((SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier))).Value;
            }
            catch (IdentityNotMappedException)
            {
            }
            return (account, sid);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private static string? QueryWts(int infoClass)
    {
        if (!Native.WTSQuerySessionInformation(IntPtr.Zero, Native.WtsCurrentSession, infoClass, out var buffer, out _))
            return null;
        try
        {
            return Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Native.WTSFreeMemory(buffer);
        }
    }
}

/// <summary>Domain join and MDM enrollment. Policies may override changes on such devices.</summary>
public sealed record ManagedDeviceInfo(bool DomainJoined, bool EntraJoined, bool MdmEnrolled)
{
    public bool IsManaged => DomainJoined || EntraJoined || MdmEnrolled;

    public static ManagedDeviceInfo Read()
    {
        var domain = false;
        try
        {
            if (Native.NetGetJoinInformation(null, out var name, out var type) == 0)
            {
                domain = type == Native.NetSetupDomainName;
                Native.NetApiBufferFree(name);
            }
        }
        catch (Exception)
        {
        }

        var entra = Reg.HklmSubKeys(@"SYSTEM\CurrentControlSet\Control\CloudDomainJoin\JoinInfo").Length > 0;

        var mdm = false;
        foreach (var sub in Reg.HklmSubKeys(@"SOFTWARE\Microsoft\Enrollments"))
        {
            var path = $@"SOFTWARE\Microsoft\Enrollments\{sub}";
            // Real MDM: EnrollmentType 6 (MDM full) or 13 (MDM via Entra join), state 1. Windows creates built-in
            // "Deploy/Cloud/Local Authority" enrollments (types 28/30/31) on unmanaged PCs too (seen on the Gaming PC).
            if (Reg.HklmInt(path, "EnrollmentType") is 6 or 13 && Reg.HklmInt(path, "EnrollmentState") == 1)
            {
                mdm = true;
                break;
            }
        }
        return new ManagedDeviceInfo(domain, entra, mdm);
    }
}
