using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// Makes the Administrators group the owner of everything the elevated process (and the tools it starts) creates.
/// The data folder trusts only files owned by Administrators or SYSTEM. With UAC on, an elevated token already uses the
/// Administrators group as default owner; with UAC off, the built-in Administrator account or Administrator protection
/// the owner can be the user account instead, and the app would then distrust and delete its own backups. Changing
/// the token's default owner covers every file at once, including exports written by bcdedit and powercfg.
/// </summary>
public static class DefaultOwner
{
    public static bool SetAdministrators()
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustDefault | TokenQuery, out var token))
        {
            Log.Warn("app", $"default owner not set: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
            return false;
        }
        var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
        var sid = new byte[admins.BinaryLength];
        admins.GetBinaryForm(sid, 0);
        var sidHandle = GCHandle.Alloc(sid, GCHandleType.Pinned);
        try
        {
            var owner = new TokenOwner { Owner = sidHandle.AddrOfPinnedObject() };
            if (SetTokenInformation(token, TokenOwnerClass, ref owner, Marshal.SizeOf<TokenOwner>())) return true;
            // Only a token with the Administrators group enabled may use it as owner (an elevated one has it).
            Log.Warn("app", $"default owner not set: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
            return false;
        }
        finally
        {
            sidHandle.Free();
            CloseHandle(token);
        }
    }

    private const uint TokenAdjustDefault = 0x0080, TokenQuery = 0x0008;
    private const int TokenOwnerClass = 4;

    [StructLayout(LayoutKind.Sequential)]
    private struct TokenOwner
    {
        public IntPtr Owner;
    }

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetTokenInformation(IntPtr token, int infoClass, ref TokenOwner info, int length);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
