using System.Runtime.InteropServices;

namespace Optimizer.Core.Interop;

/// <summary>
/// Native Wi-Fi API (wlanapi.dll), read-only. Used to find the band of the connected access point from the BSS center
/// frequency; channel numbers alone are ambiguous since 6 GHz reuses 1 to 233.
/// Layouts (wlanapi.h, natural alignment): WLAN_INTERFACE_INFO = 532 bytes, WLAN_BSS_ENTRY = 360 bytes.
/// </summary>
public static class Wlan
{
    [DllImport("wlanapi.dll")] private static extern uint WlanOpenHandle(uint clientVersion, IntPtr reserved, out uint negotiatedVersion, out IntPtr handle);
    [DllImport("wlanapi.dll")] private static extern uint WlanCloseHandle(IntPtr handle, IntPtr reserved);
    [DllImport("wlanapi.dll")] private static extern uint WlanEnumInterfaces(IntPtr handle, IntPtr reserved, out IntPtr list);
    [DllImport("wlanapi.dll")] private static extern uint WlanQueryInterface(IntPtr handle, ref Guid iface, int opCode, IntPtr reserved, out uint size, out IntPtr data, IntPtr valueType);
    [DllImport("wlanapi.dll")] private static extern uint WlanGetNetworkBssList(IntPtr handle, ref Guid iface, IntPtr ssid, int bssType, [MarshalAs(UnmanagedType.Bool)] bool securityEnabled, IntPtr reserved, out IntPtr list);
    [DllImport("wlanapi.dll")] private static extern void WlanFreeMemory(IntPtr memory);

    private const int InterfaceInfoSize = 532, BssEntrySize = 360;
    private const int OpCurrentConnection = 7, StateConnected = 1, BssInfrastructure = 1, BssAny = 3;

    /// <summary>A connection; <paramref name="SameSsidFrequenciesKhz"/> = every access point of this network the adapter sees.</summary>
    public sealed record Connection(string Interface, string Ssid, string Bssid, uint? CenterFrequencyKhz, IReadOnlyList<uint> SameSsidFrequenciesKhz)
    {
        /// <summary>
        /// Windows refused the connection or BSS details with ERROR_ACCESS_DENIED: apps need location permission for them
        /// ("Changes to API behavior for Wi-Fi access and location").
        /// </summary>
        public bool LocationDenied { get; init; }
    }

    private const uint ErrorAccessDenied = 5;

    public sealed record Bss(string Ssid, string Bssid, uint CenterFrequencyKhz);

    public static string Band(uint khz) => khz switch
    {
        < 3_000_000 => "2.4 GHz",
        < 5_925_000 => "5 GHz",
        _ => "6 GHz",
    };

    /// <summary>Connected Wi-Fi interfaces with the frequency of the access point; empty when none or no WLAN service.</summary>
    public static IReadOnlyList<Connection> Connections()
    {
        var result = new List<Connection>();
        if (!Open(out var h)) return result;
        try
        {
            foreach (var (guid, description, state) in Interfaces(h))
            {
                if (state != StateConnected) continue;
                var g = guid;
                var queryResult = WlanQueryInterface(h, ref g, OpCurrentConnection, IntPtr.Zero, out _, out var data, IntPtr.Zero);
                if (queryResult == ErrorAccessDenied)
                {
                    result.Add(new Connection(description, "", "", null, []) { LocationDenied = true });
                    continue;
                }
                if (queryResult != 0) continue;
                try
                {
                    // WLAN_CONNECTION_ATTRIBUTES: state(4) mode(4) profileName(512) | association: SSID @520, BSSID @560 | security: bSecurityEnabled @588
                    var ssid = ReadSsid(data + 520);
                    var bssid = ReadMac(data + 560);
                    var secure = Marshal.ReadInt32(data + 588) != 0;
                    uint? freq = null;
                    var sameSsid = new List<uint>();
                    var bssResult = WlanGetNetworkBssList(h, ref g, data + 520, BssInfrastructure, secure, IntPtr.Zero, out var list);
                    if (bssResult == 0)
                    {
                        try
                        {
                            var entries = ReadBssList(list);
                            freq = entries.FirstOrDefault(b => b.Bssid == bssid)?.CenterFrequencyKhz;
                            sameSsid.AddRange(entries.Where(b => b.Ssid == ssid).Select(b => b.CenterFrequencyKhz));
                        }
                        finally
                        {
                            WlanFreeMemory(list);
                        }
                    }
                    result.Add(new Connection(description, ssid, bssid, freq, sameSsid) { LocationDenied = bssResult == ErrorAccessDenied });
                }
                finally
                {
                    WlanFreeMemory(data);
                }
            }
        }
        finally
        {
            WlanCloseHandle(h, IntPtr.Zero);
        }
        return result;
    }

    /// <summary>All access points the adapters currently see (used to verify the struct layout on real hardware).</summary>
    public static IReadOnlyList<Bss> VisibleNetworks()
    {
        var result = new List<Bss>();
        if (!Open(out var h)) return result;
        try
        {
            foreach (var (guid, _, _) in Interfaces(h))
            {
                var g = guid;
                if (WlanGetNetworkBssList(h, ref g, IntPtr.Zero, BssAny, false, IntPtr.Zero, out var list) != 0) continue;
                try
                {
                    result.AddRange(ReadBssList(list));
                }
                finally
                {
                    WlanFreeMemory(list);
                }
            }
        }
        finally
        {
            WlanCloseHandle(h, IntPtr.Zero);
        }
        return result;
    }

    private static bool Open(out IntPtr handle)
    {
        handle = IntPtr.Zero;
        try
        {
            return WlanOpenHandle(2, IntPtr.Zero, out _, out handle) == 0;
        }
        catch (DllNotFoundException)
        {
            return false; // WLAN AutoConfig not installed (e.g. Server SKUs)
        }
    }

    private static List<(Guid Guid, string Description, int State)> Interfaces(IntPtr h)
    {
        var list = new List<(Guid, string, int)>();
        if (WlanEnumInterfaces(h, IntPtr.Zero, out var ptr) != 0) return list;
        try
        {
            var count = Marshal.ReadInt32(ptr);
            for (var i = 0; i < count; i++)
            {
                var item = ptr + 8 + i * InterfaceInfoSize;
                var guid = Marshal.PtrToStructure<Guid>(item);
                var description = Marshal.PtrToStringUni(item + 16) ?? "";
                var state = Marshal.ReadInt32(item + 16 + 512);
                list.Add((guid, description, state));
            }
        }
        finally
        {
            WlanFreeMemory(ptr);
        }
        return list;
    }

    // WLAN_BSS_LIST: dwTotalSize(4) dwNumberOfItems(4) entries @8
    // WLAN_BSS_ENTRY: SSID @0 (36), uPhyId @36, BSSID @40 (6), bssType @48, phyType @52, lRssi @56, uLinkQuality @60,
    // bInRegDomain @64, usBeaconPeriod @66, ullTimestamp @72, ullHostTimestamp @80, usCapability @88, ulChCenterFrequency @92 (kHz)
    private static List<Bss> ReadBssList(IntPtr list)
    {
        var result = new List<Bss>();
        var count = Marshal.ReadInt32(list + 4);
        for (var i = 0; i < count; i++)
        {
            var e = list + 8 + i * BssEntrySize;
            result.Add(new Bss(ReadSsid(e), ReadMac(e + 40), (uint)Marshal.ReadInt32(e + 92)));
        }
        return result;
    }

    private static string ReadSsid(IntPtr p)
    {
        var len = Math.Clamp(Marshal.ReadInt32(p), 0, 32);
        var bytes = new byte[len];
        Marshal.Copy(p + 4, bytes, 0, len);
        return System.Text.Encoding.UTF8.GetString(bytes);
    }

    private static string ReadMac(IntPtr p)
    {
        var b = new byte[6];
        Marshal.Copy(p, b, 0, 6);
        return Convert.ToHexString(b);
    }
}
