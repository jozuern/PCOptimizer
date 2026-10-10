using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Optimizer.Core.Platform;

/// <summary>Secure Boot certificate state (F22). Read-only: the app never writes UEFI variables.</summary>
public sealed record SecureBootCerts(bool? Kek2023, bool? WindowsUefiCa2023, bool? MicrosoftUefiCa2023, bool? OptionRomCa2023, string? ServicingStatus);

public static partial class FirmwareExtras
{
    private const string GlobalVariableGuid = "{8BE4DF61-93CA-11D2-AA0D-00E098032B8C}";
    private const string ImageSecurityDatabaseGuid = "{D719B2CB-3D3A-4596-A3BC-DAD00E67656F}";

    /// <summary>
    /// Reads the KEK and db variables (needs administrator rights and SeSystemEnvironmentPrivilege) and searches the
    /// X.509 certificates for the 2023 Microsoft CA names. Null fields = could not read.
    /// </summary>
    public static SecureBootCerts ReadSecureBootCerts()
    {
        var servicing = Reg.HklmValue(@"SYSTEM\CurrentControlSet\Control\SecureBoot\Servicing", "UEFICA2023Status")?.ToString();
        if (!EnablePrivilege("SeSystemEnvironmentPrivilege")) return new SecureBootCerts(null, null, null, null, servicing);
        var kek = ReadVariable("KEK", GlobalVariableGuid);
        var db = ReadVariable("db", ImageSecurityDatabaseGuid);
        bool? Has(byte[]? data, string name) => data is null ? null : Contains(data, name);
        return new SecureBootCerts(
            Has(kek, "Microsoft Corporation KEK 2K CA 2023"),
            Has(db, "Windows UEFI CA 2023"),
            Has(db, "Microsoft UEFI CA 2023"),
            Has(db, "Microsoft Option ROM UEFI CA 2023"),
            servicing);
    }

    internal static bool Contains(byte[] data, string ascii)
    {
        var needle = Encoding.ASCII.GetBytes(ascii);
        return data.AsSpan().IndexOf(needle) >= 0;
    }

    private static byte[]? ReadVariable(string name, string guid)
    {
        var buffer = new byte[64 * 1024];
        var size = GetFirmwareEnvironmentVariableExW(name, guid, buffer, (uint)buffer.Length, out _);
        if (size == 0) return null;
        Array.Resize(ref buffer, (int)size);
        return buffer;
    }

    /// <summary>All printable strings of the raw SMBIOS table (BIOS, board, AGESA strings on AMD boards).</summary>
    public static IReadOnlyList<string> SmbiosStrings()
    {
        const uint rsmb = 0x52534D42; // 'RSMB'
        var size = GetSystemFirmwareTable(rsmb, 0, null, 0);
        if (size == 0) return [];
        var buffer = new byte[size];
        if (GetSystemFirmwareTable(rsmb, 0, buffer, size) == 0) return [];
        return PrintableRegex().Matches(Encoding.ASCII.GetString(buffer)).Select(m => m.Value.Trim()).Where(s => s.Length >= 4).Distinct().ToList();
    }

    /// <summary>AGESA version from SMBIOS strings, e.g. "ComboAM4v2PI 1.2.0.7" → 1.2.0.7. Null when the board does not publish it.</summary>
    public static Version? AgesaVersion(IEnumerable<string> smbiosStrings)
    {
        foreach (var s in smbiosStrings)
        {
            if (!s.Contains("PI", StringComparison.Ordinal) && !s.Contains("AGESA", StringComparison.OrdinalIgnoreCase)) continue;
            var m = AgesaRegex().Match(s);
            if (m.Success && Version.TryParse(m.Groups[1].Value, out var v)) return v;
        }
        return null;
    }

    [GeneratedRegex(@"[\x20-\x7E]{4,}")]
    private static partial Regex PrintableRegex();

    [GeneratedRegex(@"(?:AM4|AM5|Combo|Genesis|Cezanne|Renoir|Matisse|Vermeer|Raphael)[A-Za-z0-9]*PI\s*V?(\d+\.\d+\.\d+\.\d+)")]
    private static partial Regex AgesaRegex();

    // ---------------- Windows power mode (slider overlay) ----------------

    public static readonly Guid OverlayBetterBattery = new("961cc777-2547-4f9d-8174-7d86181b8a7a");
    public static readonly Guid OverlayBetterPerformance = new("3af9b8d9-7c97-431d-ad78-34a8bfea439f");
    public static readonly Guid OverlayBestPerformance = new("ded574b5-45a0-4f42-8737-46345c09c238");

    /// <summary>
    /// Effective power mode overlay. The export is undocumented, so it is resolved at runtime and a missing
    /// export yields null. GUIDs from Microsoft's power slider documentation.
    /// </summary>
    public static Guid? EffectiveOverlay()
    {
        try
        {
            return PowerGetEffectiveOverlayScheme(out var g) == 0 ? g : null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }

    public static void SetOverlay(Guid overlay)
    {
        var r = PowerSetActiveOverlayScheme(overlay);
        if (r != 0) throw new Win32Exception((int)r, "PowerSetActiveOverlayScheme failed");
    }

    public static bool OverlayApiAvailable()
    {
        try
        {
            PowerGetEffectiveOverlayScheme(out _);
            return true;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    // ---------------- privilege helper ----------------

    private static bool EnablePrivilege(string name)
    {
        if (!OpenProcessToken(GetCurrentProcess(), 0x0020 | 0x0008 /* ADJUST_PRIVILEGES | QUERY */, out var token)) return false;
        try
        {
            if (!LookupPrivilegeValue(null, name, out var luid)) return false;
            var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = 0x2 /* SE_PRIVILEGE_ENABLED */ };
            if (!AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero)) return false;
            return Marshal.GetLastWin32Error() == 0; // ERROR_NOT_ALL_ASSIGNED (1300) when the account lacks the privilege
        }
        finally
        {
            CloseHandle(token);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public long Luid; public uint Attributes; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint GetFirmwareEnvironmentVariableExW(string name, string guid, byte[] buffer, uint size, out uint attributes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern uint GetSystemFirmwareTable(uint provider, uint tableId, byte[]? buffer, uint size);

    [DllImport("powrprof.dll")] private static extern uint PowerGetEffectiveOverlayScheme(out Guid overlay);
    [DllImport("powrprof.dll")] private static extern uint PowerSetActiveOverlayScheme(Guid overlay);

    [DllImport("kernel32.dll")] private static extern IntPtr GetCurrentProcess();
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(IntPtr h);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);
    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool LookupPrivilegeValue(string? system, string name, out long luid);
    [DllImport("advapi32.dll", SetLastError = true)] private static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES state, uint length, IntPtr previous, IntPtr returnLength);
}
