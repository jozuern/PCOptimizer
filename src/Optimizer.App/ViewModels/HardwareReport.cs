using Optimizer.App.Services;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;

namespace Optimizer.App.ViewModels;

/// <summary>
/// Flattens the HardwareProfile into labeled sections for the System info page and the --report file. Row labels and
/// words inside values come from labels.json ("hw.*" keys, English and German); names read from the hardware (models,
/// adapters, volumes) are shown as they are.
/// </summary>
public static class HardwareReport
{
    public static List<HwSection> Build(HardwareProfile p, Loc l)
    {
        var sections = new List<HwSection>();
        var lang = l.Language;
        string T(string key) => Labels.Current.Get(lang, $"hw.{key}");
        string F(string key, params object?[] args) => string.Format(System.Globalization.CultureInfo.CurrentCulture, T(key), args);
        SummaryItem I(string key, object? v) => new(T(key), Value(v));
        SummaryItem Raw(string label, object? v) => new(label, Value(v));
        string Value(object? v) => v switch
        {
            null => Labels.Current.Get(lang, "value.unknown"),
            bool b => Labels.Current.Get(lang, b ? "value.yes" : "value.no"),
            TriState t => Labels.Current.Get(lang, t switch { TriState.Yes => "value.yes", TriState.No => "value.no", _ => "value.unknown" }),
            _ => v.ToString() ?? Labels.Current.Get(lang, "value.unknown"),
        };
        string Gb(long? b) => b is { } x ? RebarCheck.FormatBytes(x) : Labels.Current.Get(lang, "value.unknown");

        sections.Add(new HwSection(l["Hw_Os"],
        [
            I("build", p.Os.BuildString), I("version", p.Os.DisplayVersion), I("edition", p.Os.Edition),
            I("architecture", p.Os.NativeArchitecture), I("insider", p.Os.FlightingActive),
            I("elevated", p.Elevation?.IsElevated), I("processUser", p.Elevation?.ProcessUser), I("sessionUser", p.Elevation?.SessionUser),
            I("adminProtection", p.Elevation?.AdministratorProtection), I("managed", p.Managed?.IsManaged),
            I("hypervisor", p.System?.HypervisorPresent), I("formFactor", T(p.IsLaptop ? "laptop" : "desktop")),
        ]));

        if (p.Cpu is { } c)
        {
            sections.Add(new HwSection(l["Hw_Cpu"],
            [
                I("name", c.Name), I("vendor", c.Vendor), I("familyModel", $"{c.Family} / {c.Model} (0x{c.Model:X}) / {c.Stepping}"),
                I("coresThreads", $"{c.Cores} / {c.Threads}"), I("maxClock", c.MaxClockMhz > 0 ? $"{c.MaxClockMhz} MHz" : null), I("socket", c.Socket),
                I("hybrid", c.IsHybrid ? string.Join(", ", c.CoresByEfficiencyClass.Select(kv => F("efficiencyClass", kv.Key, kv.Value))) : false),
                I("performanceCores", c.IsHybrid ? c.PerformanceCores : null),
                I("l3", string.Join(" + ", c.L3Domains.Select(d => $"{d.SizeBytes >> 20} MB"))),
                I("microcodeRunning", c.MicrocodeCurrent is { } m1 ? $"0x{m1:X}" : null),
                I("microcodeBios", c.MicrocodeBios is { } m2 ? $"0x{m2:X} ({c.MicrocodeSource})" : null),
            ]));
        }

        foreach (var g in p.Gpus ?? [])
        {
            var items = new List<SummaryItem>
            {
                I("kind", g.Kind), I("vendor", g.Vendor), I("driver", $"{g.DriverProvider} {g.NvidiaDriverVersion ?? g.DriverVersion}"),
                I("driverDate", g.DriverDate?.ToString("yyyy-MM-dd")), I("videoMemory", Gb(g.VramBytes)), I("largestBar", Gb(g.LargestBarBytes)),
                I("pcieCard", g.CardLink is { } cl ? $"Gen {cl.CurrentGen} x{cl.CurrentWidth} / Gen {cl.MaxGen} x{cl.MaxWidth}" : null),
                I("pcieSlot", g.PlatformPortLink is { } pl ? $"Gen {pl.MaxGen} x{pl.MaxWidth}" : null),
                I("switchHops", g.SwitchHopsSkipped), I("hags", g.HagsEnabled),
            };
            // Interrupt mode of the card (the MSI tweak): what the device supports and what the registry sets.
            if (p.Extras?.MsiDevices.FirstOrDefault(m => string.Equals(m.InstanceId, g.PnpDeviceId, StringComparison.OrdinalIgnoreCase)) is { } msi)
                items.Add(I("msi", F("msiValue", msi.MessageMaximum?.ToString() ?? "?", msi.MsiSupportedValue is { } v ? v.ToString() : T("notSet"))));
            if (g.Vendor == Vendor.Nvidia && p.Extras?.Nvidia is { } nv)
            {
                items.Add(I("batteryBoost", nv.BatteryBoostFps is > 0 ? $"{nv.BatteryBoostFps} FPS" : T("notSet")));
                items.Add(I("gsyncGlobal", nv.GsyncGlobalMode?.ToString() ?? T("notSet")));
            }
            items.Add(I("pnp", g.PnpDeviceId));
            sections.Add(new HwSection($"{l["Hw_Gpu"]}: {g.Name}", items));
        }

        if (p.Memory is { } mem)
        {
            var items = new List<SummaryItem> { I("total", Gb(mem.TotalBytes)) };
            items.AddRange(mem.Modules.Select(m => Raw(m.DeviceLocator,
                $"{m.Manufacturer} {m.PartNumber.Trim()}, {Gb(m.CapacityBytes)} {m.Type}, {m.ConfiguredMts} MT/s (SPD {m.SpeedMts}), {m.BankLabel}")));
            sections.Add(new HwSection(l["Hw_Memory"], items));
        }

        foreach (var d in p.Displays ?? [])
        {
            var items = new List<SummaryItem>
            {
                I("mode", $"{d.Width}×{d.Height} @ {d.CurrentRefresh.Hz:0.###} Hz ({d.CurrentRefresh.Numerator}/{d.CurrentRefresh.Denominator})"),
                I("maxOffered", $"{d.MaxOfferedRefreshAtCurrentResolution} Hz"),
                I("edid", d.Edid is { } e ? F("edidValue", $"{e.ManufacturerId} {e.ProductCode:X4}", e.MinVHz, e.MaxVHz, $"{e.PreferredWidth}×{e.PreferredHeight}", e.PreferredRefreshHz) : null),
                I("connectedTo", d.AdapterName), I("internal", d.IsInternal),
                I("hdr", $"{Value(d.HdrSupported)} / {Value(d.HdrEnabled)}"),
            };
            if (p.Extras?.Nvidia?.Vrr.TryGetValue(d.GdiName, out var vrr) == true)
                items.Add(I("gsync", F("gsyncValue", Value(vrr.Possible), Value(vrr.Enabled), Value(vrr.Requested), Value(vrr.InVrrMode))));
            items.Add(I("gdi", d.GdiName));
            sections.Add(new HwSection($"{l["Hw_Displays"]}: {d.FriendlyName}", items));
        }

        if (p.Firmware is { } fw)
        {
            sections.Add(new HwSection(l["Hw_Firmware"],
            [
                I("board", $"{fw.BoardManufacturer} {fw.BoardProduct}"), I("bios", $"{fw.BiosVendor} {fw.BiosVersion} ({fw.BiosDate:yyyy-MM-dd})"),
                I("uefi", fw.IsUefi), I("secureBoot", fw.SecureBoot), I("tpm", fw.TpmPresent == TriState.Yes ? fw.TpmSpecVersion : Value(fw.TpmPresent)),
                I("tpmReady", fw.TpmReady),
                I("vbs", Labels.Current.Get(lang, fw.VbsStatus switch { 2 => "value.running", 1 => "value.configured", 0 => "value.off", _ => "value.unknown" })),
                I("hvci", fw.HvciRunning), I("credentialGuard", fw.CredentialGuardRunning), I("mbec", fw.MbecAvailable), I("dma", fw.DmaProtectionAvailable),
                I("systemDisk", fw.SystemDiskPartitionStyle),
            ]));
        }

        if (p.Power is { } pw)
        {
            sections.Add(new HwSection(l["Hw_Power"],
            [
                I("plan", $"{pw.ActiveSchemeName} ({pw.Personality})"), I("schemeGuid", pw.ActiveScheme),
                I("processorState", $"{pw.MaxProcessorStateAc} % / {pw.MinProcessorStateAc} %"),
                I("boostMode", pw.BoostModeAc), I("coreParking", pw.CoreParkingMinCoresAc is { } cp ? $"{cp} %" : null),
                I("onAc", pw.OnAc), I("energySaver", pw.EnergySaverOn), I("modernStandby", pw.ModernStandby), I("battery", pw.BatteryPercent is { } b ? $"{b} %" : null),
                I("batteryCapacity", p.Battery is { } bh ? $"{bh.FullChargedMwh / 1000.0:0.0} / {bh.DesignedMwh / 1000.0:0.0} Wh ({Math.Round(bh.Health * 100)} %)" : null),
            ]));
        }

        if (p.Storage is { } st)
        {
            var items = st.Disks.Select(d => Raw(F("disk", d.Number), $"{d.FriendlyName}, {d.MediaType}, {d.BusType}, {Gb(d.SizeBytes)}, {d.Health}{(d.IsSmrSuspect ? ", SMR" : "")}")).ToList();
            items.AddRange(st.Volumes.Select(v => Raw(v.Root, F("volume", v.Label, Gb(v.FreeBytes), Gb(v.SizeBytes), v.DiskNumber?.ToString() ?? "?"))));
            sections.Add(new HwSection(l["Hw_Storage"], items));
        }

        if (p.Network is { } net)
            sections.Add(new HwSection(l["Hw_Network"], net.Select(n => Raw(n.Name, $"{n.Description}, {n.Type}, {(n.IsUp ? $"{n.SpeedBps / 1_000_000} Mbit/s" : T("disconnected"))}")).ToList()));

        if (p.Software is { } sw)
        {
            sections.Add(new HwSection(l["Hw_Software"],
            [
                I("antiCheats", sw.AntiCheats.Count == 0 ? Labels.Current.Get(lang, "value.unknown") : string.Join(", ", sw.AntiCheats.Select(a => $"{a.DisplayName} ({string.Join("/", a.FoundServices)})"))),
                I("launchers", string.Join(", ", sw.Launchers)),
                I("gameLibraries", string.Join("\n", sw.GameLibraryPaths)),
            ]));
        }

        if (p.ProbeErrors.Count > 0)
            sections.Add(new HwSection(l["Hw_Errors"], p.ProbeErrors.Select(e => Raw(e.Key, e.Value)).ToList()));
        return sections;
    }
}
