using System.Diagnostics;
using System.Net.NetworkInformation;
using Microsoft.Win32;
using Optimizer.Core.Catalog;
using Optimizer.Core.Interop;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware.Probes;

/// <summary>
/// Collects <see cref="HardwareExtras"/>. Every part is isolated (a failure leaves that part empty or null) and the
/// parts run in parallel: one after another they took as long as the slowest ones added up (a 3 second process sample,
/// the event log, the installed programs).
/// </summary>
public static class ExtrasProbe
{
    /// <summary>How long background CPU use is sampled (F11).</summary>
    public static readonly TimeSpan SampleWindow = TimeSpan.FromSeconds(3);

    private const string NicClass = @"SYSTEM\CurrentControlSet\Control\Class\{4d36e972-e325-11ce-bfc1-08002be10318}";

    /// <param name="cpu">
    /// This and the other probe tasks are the scan's own, still running: a part that needs one of them waits for it, the
    /// rest start at once.
    /// </param>
    /// <param name="reuse">
    /// The previous result, after a change made by this app: parts that no tweak or fix can change (installed programs,
    /// drive health, shutdown history, the background process sample, AGESA, TPM, NVMe links) are taken from it
    /// instead of being read again.
    /// </param>
    public static HardwareExtras Read(CatalogData catalog, Task<CpuInfo?> cpu, Task<IReadOnlyList<GpuInfo>?> gpus, Task<FirmwareInfo?> firmware,
        Task<IReadOnlyList<DisplayInfo>?> displays, Task<ElevationInfo?> elevation, HardwareExtras? reuse = null)
    {
        Task<T?> Part<T>(string part, Func<T> f) => Task.Run(() =>
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                return f();
            }
            catch (Exception ex)
            {
                Log.Warn("scan", $"extras/{part}: {ex.Message}");
                return default;
            }
            finally
            {
                // Slow parts are worth knowing about: the extras are on the scan's critical path.
                if (watch.ElapsedMilliseconds > 250) Log.Debug("scan", $"extras/{part} took {watch.ElapsedMilliseconds} ms");
            }
        });
        // A part that needs another probe's result starts when that probe is done (without holding a thread meanwhile).
        async Task<T?> After<TProbe, T>(string part, Task<TProbe> probe, Func<TProbe, T> f)
        {
            var result = await probe.ConfigureAwait(false);
            return await Part(part, () => f(result)).ConfigureAwait(false);
        }
        Task<T?> Reused<T>(T? value) => Task.FromResult(value);

        // The elevation probe answers at once; the user's programs and startup entries need its SID.
        var info = elevation.GetAwaiter().GetResult();
        var elevated = info?.IsElevated == true;
        var userSid = info?.SessionUserSid ?? info?.ProcessUserSid;

        var nics = Part("nics", ReadNics);
        var agesa = reuse is not null ? Reused(reuse.Agesa is { } v ? (Version: v, Source: reuse.AgesaSource ?? "") : ((Version Version, string Source)?)null)
            : After("agesa", cpu, c => c?.Vendor == Vendor.Amd ? FindAgesa(FirmwareExtras.SmbiosStrings()) : null);
        // Read with the rest of the TPM in the firmware probe (one Win32_Tpm query per scan).
        var tpm = After("tpm", firmware, f => f?.TpmManufacturer);
        var trim = Part("trim", () => new TrimSetting(Reg.HklmInt(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableDeleteNotification")));
        var secureBoot = elevated
            ? After("secureboot", firmware, f => f?.SecureBoot != TriState.Yes ? null : reuse is not null ? reuse.SecureBootCerts : FirmwareExtras.ReadSecureBootCerts())
            : Reused<SecureBootCerts>(null);
        var overlay = Part("overlay", FirmwareExtras.EffectiveOverlay);
        var wifi = Part("wifi", Wlan.Connections);
        var nvme = reuse is not null ? Reused(reuse.NvmeLinks) : Part<IReadOnlyList<NvmeLink>>("nvme", ReadNvmeLinks);
        var overlays = Part("overlays", () => RunningOverlays(catalog));
        var services = Part("services", () => catalog.Extras.AllServiceNames
            .Where(s => Reg.HklmKeyExists($@"SYSTEM\CurrentControlSet\Services\{s}")).ToHashSet(StringComparer.OrdinalIgnoreCase));
        var nvidia = After("nvidia", displays, ReadNvidia);
        var programs = reuse is not null ? Reused(reuse.Programs) : Part("programs", () => InstalledPrograms.Read(userSid));
        var startup = Part("startup", () => ReadStartupPrograms(userSid));
        var disks = reuse is not null ? Reused(reuse.DiskHealth) : Part("diskhealth", Tools.DiskHealthReader.Read);
        var virtualization = Part("virtualization", StabilityProbe.ReadVirtualization);
        var shutdowns = reuse is not null ? Reused(reuse.UnexpectedShutdowns) : Part("shutdowns", StabilityProbe.ReadUnexpectedShutdowns);
        var throttle = Part("throttle", () => Tools.HealthStore.LoadThrottle(Tools.HealthStore.DefaultFolder));
        var nicList = nics.GetAwaiter().GetResult() ?? [];
        var msi = After("msi", gpus, g => ReadMsiDevices(g, nicList));
        Task.WaitAll(agesa, tpm, trim, secureBoot, overlay, wifi, nvme, overlays, services, nvidia, programs, startup, disks, virtualization, shutdowns, throttle, msi);

        return new HardwareExtras
        {
            TpmManufacturer = tpm.Result,
            Agesa = agesa.Result?.Version,
            AgesaSource = agesa.Result?.Source,
            Trim = trim.Result,
            SecureBootCerts = secureBoot.Result,
            PowerOverlay = overlay.Result,
            Nics = nicList,
            MsiDevices = msi.Result ?? [],
            Wifi = wifi.Result ?? [],
            NvmeLinks = nvme.Result ?? [],
            RunningOverlays = overlays.Result ?? [],
            ServicesPresent = services.Result ?? new HashSet<string>(),
            Nvidia = nvidia.Result,
            Programs = programs.Result ?? [],
            StartupPrograms = startup.Result,
            DiskHealth = disks.Result ?? [],
            Virtualization = virtualization.Result,
            UnexpectedShutdowns = shutdowns.Result,
            // The 3 second sample runs after the scan (HardwareScanner); a rescan after a change keeps the last one.
            BackgroundCpu = reuse?.BackgroundCpu,
            LastThrottle = throttle.Result,
        };
    }

    /// <summary>First SMBIOS string that carries an AGESA version, with the string itself (the package name tells desktop from mobile).</summary>
    public static (Version Version, string Source)? FindAgesa(IEnumerable<string> smbiosStrings)
    {
        foreach (var s in smbiosStrings)
            if (FirmwareExtras.AgesaVersion([s]) is { } v) return (v, s);
        return null;
    }

    // ---------------- NICs ----------------

    /// <summary>NDIS *SpeedDuplex enumeration (Microsoft "Enumeration Keywords"): 0 auto; 1 to 10 fixed; values ≥ 1000 = Mbps.</summary>
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
            // Adapters without a class key are skipped. Virtual miniports (Hyper-V vEthernet, WAN Miniport, Wintun) do have
            // one: checks that need a real card use NicDetail.IsPhysical.
            if (classKey is null) continue;
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
                Characteristics = Reg.HklmInt(classKey, "Characteristics") ?? 0,
            });
        }
        return list;
    }

    // ---------------- startup programs (F16) ----------------

    private static List<string> ReadStartupPrograms(string? userSid)
    {
        var profile = userSid is null ? null : Reg.HklmString($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{userSid}", "ProfileImagePath");
        var scanner = new Startup.StartupScanner(new SystemRegistryRoots(userSid), new SystemTaskScheduler(), profile);
        // The three sources and the signature checks are independent: read side by side (the task list and
        // WinVerifyTrust are the slow parts).
        var runKeys = Task.Run(() => scanner.RunKeys().ToList());
        var folders = Task.Run(() => scanner.StartupFolders().ToList());
        var tasks = Task.Run(() => scanner.LogonTasks().ToList());
        Task.WaitAll(runKeys, folders, tasks);
        var entries = runKeys.Result.Concat(folders.Result).Concat(tasks.Result).Where(Startup.StartupTweaks.CountsForF16)
            .Where(e => !(e.Kind == Startup.StartupKind.LogonTask && e.Name.StartsWith(@"Microsoft\", StringComparison.OrdinalIgnoreCase)))
            .ToList();
        return entries.AsParallel().AsOrdered().WithDegreeOfParallelism(4)
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
        var nvme = Wmi.PhysicalDisks().Where(r => r.Int("BusType") == 17)
            // Pooled disks (Storage Spaces) can repeat or leave out the id: one name per id, never an exception.
            .Where(r => r.Str("DeviceId").Length > 0).GroupBy(r => r.Str("DeviceId")).ToDictionary(g => g.Key, g => g.First().Str("FriendlyName"));
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
