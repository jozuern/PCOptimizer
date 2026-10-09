using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using Optimizer.Core.Catalog;
using Optimizer.Core.Interop;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware.Probes;

/// <summary>Collects <see cref="HardwareExtras"/>. Every part is isolated: a failure leaves that part empty/null.</summary>
public static class ExtrasProbe
{
    private const string NicClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    public static HardwareExtras Read(CatalogData catalog, CpuInfo? cpu, IReadOnlyList<GpuInfo>? gpus, FirmwareInfo? firmware, IReadOnlyList<DisplayInfo>? displays, bool elevated, string? userSid)
    {
        T? Try<T>(string part, Func<T> f)
        {
            try
            {
                return f();
            }
            catch (Exception ex)
            {
                Log.Warn("scan", $"extras/{part}: {ex.Message}");
                return default;
            }
        }

        var nics = Try("nics", ReadNics) ?? [];
        return new HardwareExtras
        {
            TpmManufacturer = Try("tpm", () => Wmi.Query("SELECT ManufacturerIdTxt FROM Win32_Tpm", @"root\CIMV2\Security\MicrosoftTpm").FirstOrDefault()?.Str("ManufacturerIdTxt")),
            Agesa = cpu?.Vendor == Vendor.Amd ? Try("agesa", () => FirmwareExtras.AgesaVersion(FirmwareExtras.SmbiosStrings())) : null,
            SecureBootCerts = elevated && firmware?.SecureBoot == TriState.Yes ? Try("secureboot", FirmwareExtras.ReadSecureBootCerts) : null,
            PowerOverlay = Try("overlay", FirmwareExtras.EffectiveOverlay),
            Nics = nics,
            MsiDevices = Try("msi", () => ReadMsiDevices(gpus, nics)) ?? [],
            Wifi = Try("wifi", Wlan.Connections) ?? [],
            NvmeLinks = Try("nvme", ReadNvmeLinks) ?? [],
            RunningOverlays = Try("overlays", () => RunningOverlays(catalog)) ?? [],
            ServicesPresent = Try("services", () => catalog.Extras.AllServiceNames
                .Where(s => Reg.HklmKeyExists($@"SYSTEM\CurrentControlSet\Services\{s}")).ToHashSet(StringComparer.OrdinalIgnoreCase)) ?? new HashSet<string>(),
            Nvidia = Try("nvidia", () => ReadNvidia(displays)),
            PrintersInstalled = Try("printers", ReadPrinters) ?? [],
            Programs = Try("programs", () => InstalledPrograms.Read(userSid)) ?? [],
            StartupPrograms = Try("startup", () => ReadStartupPrograms(userSid)),
            DiskHealth = Try("diskhealth", Tools.DiskHealthReader.Read) ?? [],
            BackgroundCpu = Try("processes", () => Tools.ProcessSampler.SampleAsync(TimeSpan.FromSeconds(3)).GetAwaiter().GetResult()),
            LastThrottle = Try("throttle", () => Tools.HealthStore.LoadThrottle(Tools.HealthStore.DefaultFolder)),
        };
    }

    // ---------------- NICs ----------------

    /// <summary>NDIS *SpeedDuplex enumeration (Microsoft "Enumeration Keywords"): 0 auto; 1–10 fixed; values ≥ 1000 = Mbps.</summary>
    public static int? SpeedDuplexMbps(string value) => value switch
    {
        "1" or "2" => 10,
        "3" or "4" => 100,
        "5" or "6" => 1000,
        "7" => 10_000,
        "8" => 20_000,
        "9" => 40_000,
        "10" => 100_000,
        _ => int.TryParse(value, out var v) && v >= 1000 ? v : null,
    };

    private static List<NicDetail> ReadNics()
    {
        var classKeys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sub in Reg.HklmSubKeys(NicClass).Where(s => s.Length == 4 && s.All(char.IsAsciiDigit)))
            if (Reg.HklmString($@"{NicClass}\{sub}", "NetCfgInstanceId") is { } id) classKeys[id] = $@"{NicClass}\{sub}";

        var list = new List<NicDetail>();
        foreach (var n in NetworkInterface.GetAllNetworkInterfaces()
                     .Where(n => n.NetworkInterfaceType is NetworkInterfaceType.Ethernet or NetworkInterfaceType.Wireless80211 or NetworkInterfaceType.GigabitEthernet))
        {
            classKeys.TryGetValue(n.Id, out var classKey);
            if (classKey is null) continue; // virtual adapters without a hardware class key
            var keywords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var allowed = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            using (var key = Reg.OpenHklm(classKey))
            using (var ndi = Reg.OpenHklm($@"{classKey}\Ndi\params"))
            {
                if (key is null || ndi is null) continue;
                foreach (var param in ndi.GetSubKeyNames())
                {
                    if (key.GetValue(param) is { } v) keywords[param] = v.ToString() ?? "";
                    using var en = ndi.OpenSubKey($@"{param}\enum");
                    if (en is not null) allowed[param] = en.GetValueNames();
                }
            }
            int? max = allowed.TryGetValue("*SpeedDuplex", out var speeds) ? speeds.Select(SpeedDuplexMbps).Max() : null;
            list.Add(new NicDetail(n.Id, n.Name, n.Description, n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ? "Wi-Fi" : "Ethernet",
                n.OperationalStatus == OperationalStatus.Up, n.OperationalStatus == OperationalStatus.Up ? n.Speed : 0, max, classKey, keywords, allowed)
            {
                DeviceInstanceId = Reg.HklmString(classKey, "DeviceInstanceID"),
            });
        }
        return list;
    }

    // ---------------- startup programs (F16) ----------------

    private static List<string> ReadStartupPrograms(string? userSid)
    {
        var profile = userSid is null ? null : Reg.HklmString($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{userSid}", "ProfileImagePath");
        var scanner = new Startup.StartupScanner(new SystemRegistryRoots(userSid), new SystemTaskScheduler(), profile);
        var entries = scanner.RunKeys().Concat(scanner.StartupFolders()).Concat(scanner.LogonTasks()).Where(Startup.StartupTweaks.CountsForF16);
        return entries
            .Where(e => !(e.Kind == Startup.StartupKind.LogonTask && e.Name.StartsWith(@"Microsoft\", StringComparison.OrdinalIgnoreCase)))
            .Where(e => e.RunsScriptHost || !Startup.SignatureVerifier.Verify(e.ImagePath).IsMicrosoft)
            .Select(e => e.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // ---------------- MSI capability ----------------

    public const string MsiKeySuffix = @"Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";

    private static List<MsiDevice> ReadMsiDevices(IReadOnlyList<GpuInfo>? gpus, IReadOnlyList<NicDetail> nics)
    {
        var candidates = new List<(string Id, string Name, string Kind)>();
        foreach (var g in gpus?.Where(g => g.Kind == GpuKind.Discrete) ?? []) candidates.Add((g.PnpDeviceId, g.Name, "gpu"));
        foreach (var n in nics.Where(n => n.DeviceInstanceId?.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase) == true))
            candidates.Add((n.DeviceInstanceId!, n.Description, "nic"));

        var list = new List<MsiDevice>();
        foreach (var (id, name, kind) in candidates.DistinctBy(c => c.Id, StringComparer.OrdinalIgnoreCase))
        {
            if (PciDevice.Locate(id) is not { } dev) continue;
            var value = Reg.HklmInt($@"SYSTEM\CurrentControlSet\Enum\{id}\{MsiKeySuffix}", "MSISupported");
            list.Add(new MsiDevice(id, name, kind, PciDevice.InterruptSupport(dev), PciDevice.InterruptMessageMaximum(dev), value is { } v ? (uint)v : null));
        }
        return list;
    }

    // ---------------- NVMe links ----------------

    private static List<NvmeLink> ReadNvmeLinks()
    {
        var list = new List<NvmeLink>();
        var nvme = Wmi.Query("SELECT DeviceId, FriendlyName FROM MSFT_PhysicalDisk WHERE BusType = 17", @"root\Microsoft\Windows\Storage")
            .ToDictionary(r => r.Str("DeviceId"), r => r.Str("FriendlyName"));
        foreach (var d in Wmi.Query("SELECT Index, PNPDeviceID, Model FROM Win32_DiskDrive"))
        {
            if (!nvme.TryGetValue(d.Int("Index")?.ToString() ?? "", out var name)) continue;
            if (PciDevice.Locate(d.Str("PNPDeviceID")) is not { } dev) continue;
            // Walk up from the disk to the PCI NVMe controller.
            var node = dev;
            string? id = null;
            for (var i = 0; i < 6 && PciDevice.Parent(node) is { } parent; i++)
            {
                node = parent;
                id = PciDevice.InstanceId(node);
                if (id?.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) == true) break;
            }
            if (id is null || !id.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase)) continue;
            var port = PciDevice.Parent(node) is { } p && PciDevice.InstanceId(p)?.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) == true ? PciDevice.Link(p) : null;
            list.Add(new NvmeLink(name, id, PciDevice.Link(node), port));
        }
        return list;
    }

    // ---------------- overlays, printers ----------------

    private static List<string> RunningOverlays(CatalogData catalog)
    {
        var names = Process.GetProcesses().Select(p =>
        {
            using (p) return p.ProcessName;
        }).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return catalog.Extras.OverlayProcesses.Where(o => names.Contains(o.Process)).Select(o => o.Name).Distinct().ToList();
    }

    private static List<string> ReadPrinters()
    {
        string[] virtualPrinters = ["Microsoft Print to PDF", "Microsoft XPS Document Writer", "OneNote", "Fax", "Send To OneNote", "AnyDesk"];
        return Wmi.Query("SELECT Name FROM Win32_Printer").Select(r => r.Str("Name"))
            .Where(n => !virtualPrinters.Any(v => n.Contains(v, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    // ---------------- NVIDIA ----------------

    private static NvidiaInfo? ReadNvidia(IReadOnlyList<DisplayInfo>? displays)
    {
        if (!Nvapi.Available) return null;
        uint? frl = null, vsync = null, pstate = null, battery = null, vrr = null;
        try
        {
            using var s = new Nvapi.Session();
            var baseProfile = s.BaseProfile();
            frl = s.GetEffective(baseProfile, Nvapi.SettingFrameRateLimiter);
            vsync = s.GetEffective(baseProfile, Nvapi.SettingVSyncMode);
            pstate = s.GetEffective(baseProfile, Nvapi.SettingPreferredPState);
            battery = s.GetEffective(baseProfile, Nvapi.SettingBatteryBoostFps);
            vrr = s.GetEffective(baseProfile, Nvapi.SettingVrrMode);
        }
        catch (Exception ex)
        {
            Log.Warn("scan", $"NVAPI DRS: {ex.Message}");
        }
        var perDisplay = new Dictionary<string, NvidiaVrr>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in displays ?? [])
            if (d.AdapterVendor == Vendor.Nvidia && Nvapi.VrrInfo(d.GdiName) is { } v)
                perDisplay[d.GdiName] = new NvidiaVrr(v.Enabled, v.Possible, v.Requested, v.InVrrMode);
        return new NvidiaInfo(Nvapi.DriverVersion(), frl, vsync, pstate, battery, vrr, perDisplay);
    }
}
