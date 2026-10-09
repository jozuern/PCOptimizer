using Optimizer.App.Services;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;

namespace Optimizer.App.ViewModels;

/// <summary>Flattens the HardwareProfile into labeled sections for the System info page.</summary>
public static class HardwareReport
{
    public static List<HwSection> Build(HardwareProfile p, Loc l)
    {
        var sections = new List<HwSection>();
        var lang = l.Language;
        SummaryItem I(string k, object? v) => new(k, v switch
        {
            null => Labels.Current.Get(lang, "value.unknown"),
            bool b => Labels.Current.Get(lang, b ? "value.yes" : "value.no"),
            _ => v.ToString() ?? Labels.Current.Get(lang, "value.unknown"),
        });
        string Gb(long? b) => b is { } x ? RebarCheck.FormatBytes(x) : Labels.Current.Get(lang, "value.unknown");

        sections.Add(new HwSection(l["Hw_Os"],
        [
            I("Build", p.Os.BuildString), I("Version", p.Os.DisplayVersion), I("Edition", p.Os.Edition),
            I("Architecture", p.Os.NativeArchitecture), I("Insider (flighting)", p.Os.FlightingActive),
            I("Elevated", p.Elevation?.IsElevated), I("Process user", p.Elevation?.ProcessUser), I("Session user", p.Elevation?.SessionUser),
            I("Administrator protection", p.Elevation?.AdministratorProtection), I("Managed device", p.Managed?.IsManaged),
            I("Hypervisor present", p.System?.HypervisorPresent), I("Form factor", p.IsLaptop ? "Laptop" : "Desktop"),
        ]));

        if (p.Cpu is { } c)
        {
            sections.Add(new HwSection(l["Hw_Cpu"],
            [
                I("Name", c.Name), I("Vendor", c.Vendor), I("Family / model / stepping", $"{c.Family} / {c.Model} (0x{c.Model:X}) / {c.Stepping}"),
                I("Cores / threads", $"{c.Cores} / {c.Threads}"), I("Socket", c.Socket),
                I("Hybrid (efficiency classes)", c.IsHybrid ? string.Join(", ", c.CoresByEfficiencyClass.Select(kv => $"class {kv.Key}: {kv.Value}")) : "No"),
                I("L3 domains", string.Join(" + ", c.L3Domains.Select(d => $"{d.SizeBytes >> 20} MB"))),
                I("Microcode (running)", c.MicrocodeCurrent is { } m1 ? $"0x{m1:X}" : null),
                I("Microcode (BIOS)", c.MicrocodeBios is { } m2 ? $"0x{m2:X} ({c.MicrocodeSource})" : null),
            ]));
        }

        foreach (var g in p.Gpus ?? [])
        {
            sections.Add(new HwSection($"{l["Hw_Gpu"]}: {g.Name}",
            [
                I("Kind", g.Kind), I("Vendor", g.Vendor), I("Driver", $"{g.DriverProvider} {g.NvidiaDriverVersion ?? g.DriverVersion}"),
                I("Driver date", g.DriverDate?.ToString("yyyy-MM-dd")), I("Video memory", Gb(g.VramBytes)), I("Largest BAR", Gb(g.LargestBarBytes)),
                I("PCIe (card): current / max", g.CardLink is { } cl ? $"Gen {cl.CurrentGen} x{cl.CurrentWidth} / Gen {cl.MaxGen} x{cl.MaxWidth}" : null),
                I("PCIe (slot): max", g.PlatformPortLink is { } pl ? $"Gen {pl.MaxGen} x{pl.MaxWidth}" : null),
                I("On-card switch hops", g.SwitchHopsSkipped), I("HAGS", g.HagsEnabled), I("PnP ID", g.PnpDeviceId),
            ]));
        }

        if (p.Memory is { } mem)
        {
            var items = new List<SummaryItem> { I("Total", Gb(mem.TotalBytes)) };
            items.AddRange(mem.Modules.Select(m => I(m.DeviceLocator,
                $"{m.Manufacturer} {m.PartNumber.Trim()}, {Gb(m.CapacityBytes)} {m.Type}, {m.ConfiguredMts} MT/s (SPD {m.SpeedMts}), {m.BankLabel}")));
            sections.Add(new HwSection(l["Hw_Memory"], items));
        }

        foreach (var d in p.Displays ?? [])
        {
            sections.Add(new HwSection($"{l["Hw_Displays"]}: {d.FriendlyName}",
            [
                I("Mode", $"{d.Width}×{d.Height} @ {d.CurrentRefresh.Hz:0.###} Hz ({d.CurrentRefresh.Numerator}/{d.CurrentRefresh.Denominator})"),
                I("Max offered at this resolution", $"{d.MaxOfferedRefreshAtCurrentResolution} Hz"),
                I("EDID", d.Edid is { } e ? $"{e.ManufacturerId} {e.ProductCode:X4}, range {e.MinVHz} to {e.MaxVHz} Hz, preferred {e.PreferredWidth}×{e.PreferredHeight} @ {e.PreferredRefreshHz} Hz" : null),
                I("Connected to", d.AdapterName), I("Internal panel", d.IsInternal), I("HDR supported / on", $"{d.HdrSupported} / {d.HdrEnabled}"),
                I("GDI name", d.GdiName),
            ]));
        }

        if (p.Firmware is { } fw)
        {
            sections.Add(new HwSection(l["Hw_Firmware"],
            [
                I("Board", $"{fw.BoardManufacturer} {fw.BoardProduct}"), I("BIOS", $"{fw.BiosVendor} {fw.BiosVersion} ({fw.BiosDate:yyyy-MM-dd})"),
                I("UEFI", fw.IsUefi), I("Secure Boot", fw.SecureBoot), I("TPM", fw.TpmPresent == TriState.Yes ? fw.TpmSpecVersion : fw.TpmPresent.ToString()),
                I("TPM ready", fw.TpmReady), I("VBS status", fw.VbsStatus switch { 2 => "Running", 1 => "Configured", 0 => "Off", _ => "Unknown" }),
                I("HVCI running", fw.HvciRunning), I("MBEC/GMET", fw.MbecAvailable), I("DMA protection", fw.DmaProtectionAvailable),
                I("System disk", fw.SystemDiskPartitionStyle),
            ]));
        }

        if (p.Power is { } pw)
        {
            sections.Add(new HwSection(l["Hw_Power"],
            [
                I("Plan", $"{pw.ActiveSchemeName} ({pw.Personality})"), I("Scheme GUID", pw.ActiveScheme),
                I("Max / min processor state (AC)", $"{pw.MaxProcessorStateAc} % / {pw.MinProcessorStateAc} %"),
                I("Boost mode (AC)", pw.BoostModeAc), I("Core parking min cores (AC)", pw.CoreParkingMinCoresAc is { } cp ? $"{cp} %" : null),
                I("On AC", pw.OnAc), I("Energy Saver", pw.EnergySaverOn), I("Modern Standby", pw.ModernStandby), I("Battery", pw.BatteryPercent is { } b ? $"{b} %" : null),
            ]));
        }

        if (p.Storage is { } st)
        {
            var items = st.Disks.Select(d => I($"Disk {d.Number}", $"{d.FriendlyName}, {d.MediaType}, {d.BusType}, {Gb(d.SizeBytes)}, {d.Health}{(d.IsSmrSuspect ? ", SMR" : "")}")).ToList();
            items.AddRange(st.Volumes.Select(v => I(v.Root, $"{v.Label}, {Gb(v.FreeBytes)} free of {Gb(v.SizeBytes)}, disk {v.DiskNumber}")));
            sections.Add(new HwSection(l["Hw_Storage"], items));
        }

        if (p.Network is { } net)
            sections.Add(new HwSection(l["Hw_Network"], net.Select(n => I(n.Name, $"{n.Description}, {n.Type}, {(n.IsUp ? $"{n.SpeedBps / 1_000_000} Mbit/s" : "down")}")).ToList()));

        if (p.Software is { } sw)
        {
            sections.Add(new HwSection(l["Hw_Software"],
            [
                I("Anti-cheats", sw.AntiCheats.Count == 0 ? Labels.Current.Get(lang, "value.unknown") : string.Join(", ", sw.AntiCheats.Select(a => $"{a.DisplayName} ({string.Join("/", a.FoundServices)})"))),
                I("Launchers", string.Join(", ", sw.Launchers)),
                I("Game libraries", string.Join("\n", sw.GameLibraryPaths)),
            ]));
        }

        if (p.ProbeErrors.Count > 0)
            sections.Add(new HwSection(l["Hw_Errors"], p.ProbeErrors.Select(e => I(e.Key, e.Value)).ToList()));
        return sections;
    }
}
