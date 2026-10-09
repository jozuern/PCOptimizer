using System.Globalization;
using System.Management;
using Optimizer.Core.Interop;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Actions;

/// <summary>
/// One NVIDIA driver (DRS) DWORD setting on the global profile or a game's profile. Value null = remove the profile's own
/// value so the driver default applies. Only documented setting IDs (NvApiDriverSettings.h) are used.
/// </summary>
public sealed class NvidiaDrsAction : TweakAction
{
    /// <summary>"global" or the full path of a game executable.</summary>
    public string Profile { get; init; } = "global";

    public uint SettingId { get; init; }
    public uint? Value { get; init; }

    public override string TargetKey => $"drs:{Profile}:{SettingId:X8}".ToLowerInvariant();

    public override string Describe(ActionContext c) =>
        $"NVIDIA {(Profile == "global" ? "global profile" : $"profile of {Path.GetFileName(Profile)}")}: setting 0x{SettingId:X8}";

    public override StoredValue Desired(ActionContext c) => Value is { } v ? Stored(v) : StoredValue.Missing;

    public override StoredValue? Read(ActionContext c)
    {
        if (!c.Nvidia.Available) return null;
        return c.Nvidia.ReadOwn(Profile, SettingId) is { } v ? Stored(v) : StoredValue.Missing;
    }

    public override void Apply(ActionContext c) => c.Nvidia.Write(Profile, SettingId, Value);

    public override void Restore(ActionContext c, StoredValue original) =>
        c.Nvidia.Write(Profile, SettingId, original.Existed && uint.TryParse(original.Data, CultureInfo.InvariantCulture, out var v) ? v : null);

    private static StoredValue Stored(uint v) => new(true, "drs", v.ToString(CultureInfo.InvariantCulture));
}

/// <summary>Windows power mode (Settings > System > Power > Power mode) for the current power source.</summary>
public sealed class PowerModeAction : TweakAction
{
    /// <summary>Overlay GUID; Guid.Empty = Balanced.</summary>
    public Guid Overlay { get; init; }

    public override string TargetKey => "pwr:overlay";
    public override string Describe(ActionContext c) => "Windows power mode";
    public override StoredValue Desired(ActionContext c) => new(true, "overlay", Overlay.ToString());
    public override StoredValue? Read(ActionContext c) => c.PowerMode.Read() is { } g ? new StoredValue(true, "overlay", g.ToString()) : null;
    public override void Apply(ActionContext c) => c.PowerMode.Write(Overlay);

    public override void Restore(ActionContext c, StoredValue original)
    {
        if (Guid.TryParse(original.Data, out var g)) c.PowerMode.Write(g);
    }
}

/// <summary>
/// Network adapter advanced properties (standardized "*" keywords or vendor keywords). In the catalog this is a
/// template; the engine expands it into one action per physical adapter (<see cref="NicAdapters"/>). Only keywords the
/// adapter's driver declares under Ndi\params, with the wanted value in its enum list, are touched; an adapter without
/// any of them is Unsupported. One adapter restart per action, so the driver reads the new values.
/// </summary>
public sealed class NicPropertyAction : TweakAction
{
    /// <summary>Keyword -> value (string, as the driver stores it).</summary>
    public Dictionary<string, string> Properties { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>DWORD values written directly in the class key (e.g. PnPCapabilities), not driver keywords.</summary>
    public Dictionary<string, uint> Dwords { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Only Ethernet / only Wi-Fi / both: "ethernet", "wifi", "any".</summary>
    public string Media { get; init; } = "any";

    /// <summary>Optional: limit the expansion to one adapter (interface GUID), for fixes built for one adapter.</summary>
    public string? InterfaceGuid { get; init; }

    /// <summary>Set by the expansion.</summary>
    public NicAdapter? Adapter { get; init; }

    public override string TargetKey => $"nic:{Adapter?.ClassKey}:{string.Join(",", Keys())}".ToLowerInvariant();
    public override string Describe(ActionContext c) => $"{Adapter?.Description ?? "Network adapter"}: {string.Join(", ", Keys())}";

    private IEnumerable<string> Keys() => Applicable(null).Select(p => p.Key).Concat(Dwords.Keys).OrderBy(k => k, StringComparer.OrdinalIgnoreCase);

    /// <summary>Keywords this adapter's driver supports with the wanted value.</summary>
    private IEnumerable<KeyValuePair<string, string>> Applicable(ActionContext? c) =>
        Adapter is null ? [] : Properties.Where(p => Adapter.Allowed.TryGetValue(p.Key, out var values) && values.Contains(p.Value, StringComparer.OrdinalIgnoreCase));

    public override StoredValue Desired(ActionContext c)
    {
        var map = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Applicable(c)) map[p.Key] = p.Value;
        foreach (var d in Dwords) map[d.Key] = d.Value.ToString(CultureInfo.InvariantCulture);
        return new StoredValue(true, "nic", Format(map));
    }

    public override StoredValue? Read(ActionContext c)
    {
        if (Adapter is null || (!Applicable(c).Any() && Dwords.Count == 0)) return null;
        var map = new SortedDictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var p in Applicable(c))
            map[p.Key] = RegistryValue.Read(c.Registry, Hive.Machine, Adapter.ClassKey, p.Key) is { Existed: true } v ? v.Data : null;
        foreach (var d in Dwords)
            map[d.Key] = RegistryValue.Read(c.Registry, Hive.Machine, Adapter.ClassKey, d.Key) is { Existed: true } v ? v.Data : null;
        return new StoredValue(true, "nic", Format(map));
    }

    public override void Apply(ActionContext c)
    {
        foreach (var p in Applicable(c)) WriteKeyword(c, p.Key, p.Value);
        foreach (var d in Dwords) RegistryValue.Write(c.Registry, Hive.Machine, Adapter!.ClassKey, d.Key, "dword", d.Value.ToString(CultureInfo.InvariantCulture));
        RestartAdapter(c);
    }

    /// <summary>
    /// Driver keywords are REG_SZ by the NDIS convention, but some drivers store them as DWORD: an existing value keeps
    /// its type, so neither the change nor its undo turns a DWORD into a string.
    /// </summary>
    private void WriteKeyword(ActionContext c, string key, string value)
    {
        var existing = RegistryValue.Read(c.Registry, Hive.Machine, Adapter!.ClassKey, key);
        var kind = existing is { Existed: true, Kind: "dword" } && uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? "dword" : "string";
        RegistryValue.Write(c.Registry, Hive.Machine, Adapter.ClassKey, key, kind, value);
    }

    public override void Restore(ActionContext c, StoredValue original)
    {
        var map = original.Data is null ? null : Parse(original.Data);
        if (map is null) return;
        foreach (var (key, value) in map)
        {
            if (value is null) RegistryValue.Delete(c.Registry, Hive.Machine, Adapter!.ClassKey, key);
            else if (Dwords.ContainsKey(key)) RegistryValue.Write(c.Registry, Hive.Machine, Adapter!.ClassKey, key, "dword", value);
            else WriteKeyword(c, key, value);
        }
        RestartAdapter(c);
    }

    /// <summary>"Keyword=value, Keyword2=(not set)": readable in the confirmation dialog, parsed back on undo.</summary>
    public const string NotSet = "(not set)";

    public static string Format(IEnumerable<KeyValuePair<string, string?>> map) =>
        string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value ?? NotSet}"));

    public static Dictionary<string, string?> Parse(string data)
    {
        var map = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in data.Split(", ", StringSplitOptions.RemoveEmptyEntries))
        {
            var i = part.IndexOf('=');
            if (i <= 0) continue;
            var value = part[(i + 1)..];
            map[part[..i]] = value == NotSet ? null : value;
        }
        return map;
    }

    /// <summary>A failed restart does not undo the change: the driver reads the values at the next start (or reboot).</summary>
    private void RestartAdapter(ActionContext c)
    {
        if (Adapter?.DeviceInstanceId is not { Length: > 0 } id) return;
        try
        {
            c.Devices.Restart(id);
        }
        catch (Exception ex)
        {
            Log.Warn("nic", $"restart of {Adapter.Description} failed, values apply after a reboot: {ex.Message}");
        }
    }
}

/// <summary>A physical network adapter's class key with the values its driver allows per keyword.</summary>
public sealed record NicAdapter(string ClassKey, string InterfaceGuid, string Description, string DeviceInstanceId, bool IsWifi,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Allowed, bool IsEthernet = false);

/// <summary>Physical adapters from the network class key, read through <see cref="IRegistryRoots"/> (sandbox-testable).</summary>
public static class NicAdapters
{
    public const string ClassPath = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";
    private const int NcfPhysical = 0x4;
    private const int IfTypeWifi = 71; // IF_TYPE_IEEE80211
    private const int IfTypeEthernet = 6; // IF_TYPE_ETHERNET_CSMACD; mobile broadband and others have their own types

    public static IReadOnlyList<NicAdapter> Enumerate(IRegistryRoots registry)
    {
        var list = new List<NicAdapter>();
        using var cls = registry.Open(Hive.Machine, ClassPath, writable: false);
        if (cls is null) return list;
        foreach (var sub in cls.GetSubKeyNames().Where(s => s.Length == 4 && s.All(char.IsAsciiDigit)))
        {
            using var key = cls.OpenSubKey(sub);
            if (key is null) continue;
            var characteristics = key.GetValue("Characteristics") is int ch ? ch : 0;
            var instance = key.GetValue("DeviceInstanceID") as string ?? "";
            var guid = key.GetValue("NetCfgInstanceId") as string;
            // Physical hardware only: virtual switches, VPN and Hyper-V adapters are never changed.
            if ((characteristics & NcfPhysical) == 0 || guid is null) continue;
            if (!instance.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase) && !instance.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase)) continue;
            var allowed = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            using (var ndi = key.OpenSubKey(@"Ndi\params"))
            {
                foreach (var param in ndi?.GetSubKeyNames() ?? [])
                {
                    using var en = ndi!.OpenSubKey($@"{param}\enum");
                    if (en is not null) allowed[param] = en.GetValueNames();
                }
            }
            var ifType = key.GetValue("*IfType") switch { int t => t, string text when int.TryParse(text, out var t) => t, _ => 0 };
            list.Add(new NicAdapter($@"{ClassPath}\{sub}", guid, key.GetValue("DriverDesc") as string ?? guid, instance, ifType == IfTypeWifi, allowed, ifType == IfTypeEthernet));
        }
        return list;
    }
}

/// <summary>
/// IPv4 DNS servers of one adapter (by interface GUID). Stored value: "dhcp" or a comma-separated list.
/// Read from the TCP/IP interface key (NameServer is empty when the servers come from DHCP).
/// </summary>
public sealed class DnsAction : TweakAction
{
    public string InterfaceGuid { get; init; } = "";

    /// <summary>"dhcp" or comma-separated IPv4 addresses.</summary>
    public string Servers { get; init; } = "dhcp";

    public override string TargetKey => $"dns:{InterfaceGuid}".ToLowerInvariant();
    public override string Describe(ActionContext c) => $"IPv4 DNS servers of adapter {InterfaceGuid}";
    public override StoredValue Desired(ActionContext c) => new(true, "dns", Servers);

    public override StoredValue? Read(ActionContext c)
    {
        var path = $@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{InterfaceGuid}";
        using (var key = c.Registry.Open(Hive.Machine, path, writable: false))
            if (key is null) return null;
        var v = RegistryValue.Read(c.Registry, Hive.Machine, path, "NameServer");
        var servers = string.Join(",", (v.Data ?? "").Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return new StoredValue(true, "dns", servers.Length == 0 ? "dhcp" : servers);
    }

    public override void Apply(ActionContext c) => c.Network.SetDns(InterfaceGuid, Parse(Servers));

    public override void Restore(ActionContext c, StoredValue original) => c.Network.SetDns(InterfaceGuid, Parse(original.Data));

    private static string[]? Parse(string? servers) => servers is null or "dhcp" ? null : servers.Split(',');
}

// ---------------- system interfaces (faked in tests) ----------------

public interface IPowerModeManager
{
    /// <summary>Effective power mode overlay; null when the API is unavailable.</summary>
    Guid? Read();

    void Write(Guid overlay);
}

public interface IDeviceManager
{
    void Restart(string deviceInstanceId);
}

public interface INetworkManager
{
    /// <summary>Null = obtain DNS servers automatically (DHCP).</summary>
    void SetDns(string interfaceGuid, string[]? servers);
}

public interface INvidiaSettings
{
    bool Available { get; }

    /// <summary>Value stored in the profile itself; null = not set there (inherited or driver default).</summary>
    uint? ReadOwn(string profile, uint settingId);

    /// <summary>null removes the profile's own value.</summary>
    void Write(string profile, uint settingId, uint? value);
}

// ---------------- real implementations ----------------

public sealed class SystemPowerModeManager : IPowerModeManager
{
    public Guid? Read() => FirmwareExtras.OverlayApiAvailable() ? FirmwareExtras.EffectiveOverlay() : null;
    public void Write(Guid overlay) => FirmwareExtras.SetOverlay(overlay);
}

public sealed class SystemDeviceManager : IDeviceManager
{
    public void Restart(string deviceInstanceId)
    {
        if (Hardware.Probes.PciDevice.Restart(deviceInstanceId) is { } error) throw new InvalidOperationException(error);
    }
}

public sealed class SystemNetworkManager : INetworkManager
{
    public void SetDns(string interfaceGuid, string[]? servers)
    {
        if (!Guid.TryParse(interfaceGuid, out var g)) throw new ArgumentException($"Invalid interface id {interfaceGuid}");
        using var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapterConfiguration WHERE SettingID = '{g:B}'");
        foreach (var o in searcher.Get())
        {
            using var mo = (ManagementObject)o;
            var code = Convert.ToUInt32(mo.InvokeMethod("SetDNSServerSearchOrder", [servers]), CultureInfo.InvariantCulture);
            // 0 = done, 1 = done but needs a restart (documented return codes of the method).
            if (code is not (0 or 1)) throw new InvalidOperationException($"SetDNSServerSearchOrder returned {code}");
            Log.Info("dns", $"{g:B}: {(servers is null ? "DHCP" : string.Join(", ", servers))}");
            return;
        }
        throw new InvalidOperationException($"Adapter {interfaceGuid} not found");
    }
}

public sealed class SystemNvidiaSettings : INvidiaSettings
{
    public bool Available => Nvapi.Available;

    public uint? ReadOwn(string profile, uint settingId)
    {
        using var s = new Nvapi.Session();
        var p = profile == "global" ? s.BaseProfile() : s.ProfileForExe(profile, create: false);
        return p == IntPtr.Zero ? null : s.GetOwn(p, settingId);
    }

    public void Write(string profile, uint settingId, uint? value)
    {
        using var s = new Nvapi.Session();
        var p = profile == "global" ? s.BaseProfile() : s.ProfileForExe(profile, create: value is not null);
        if (p == IntPtr.Zero) return; // resetting a setting on a profile that does not exist: nothing to do
        if (value is { } v) s.Set(p, settingId, v);
        else s.Delete(p, settingId);
        s.Save();
    }
}
