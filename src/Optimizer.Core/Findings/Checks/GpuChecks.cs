using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;
using Optimizer.Core.Hardware.Probes;

namespace Optimizer.Core.Findings.Checks;

/// <summary>F5: GPU on Microsoft Basic Display Adapter (no vendor driver).</summary>
public sealed class BasicDisplayAdapterCheck : IFindingCheck
{
    public const string Id = "F5.basicDisplay";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Gpus is null)
        {
            yield return new Finding { Id = Id, Kind = FindingKind.Finding, Status = FindingStatus.Unknown, Impact = 5, Effects = [Effect.Fps] };
            yield break;
        }
        var basic = p.Gpus.Where(g => g.Kind == GpuKind.Basic).ToList();
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = basic.Count > 0 ? FindingStatus.Problem : FindingStatus.Ok,
            Impact = 5,
            Effects = [Effect.Fps, Effect.Stability],
            Facts = p.Gpus.Where(g => g.Kind != GpuKind.Virtual)
                .Select(g => new Fact("fact.gpuDriver", $"{g.Name}: {g.DriverProvider ?? "?"} {g.DriverVersion}")).ToList(),
        };
    }
}

/// <summary>F15 (GPU part): driver older than ~6 months.</summary>
public sealed class GpuDriverAgeCheck : IFindingCheck
{
    public const string Id = "F15.gpuDriverAge";
    public const int MaxAgeDays = 183;
    public IReadOnlyList<string> DocIds => [Id];

    public static Func<DateTime> Today { get; set; } = () => DateTime.Today;

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Gpus is null) yield break;
        foreach (var g in p.Gpus.Where(g => g.Kind is GpuKind.Discrete or GpuKind.Integrated))
        {
            var age = g.DriverDate is { } d ? (int)(Today() - d.Date).TotalDays : (int?)null;
            var version = g.NvidiaDriverVersion is { } nv ? $"{nv} ({g.DriverVersion})" : g.DriverVersion ?? "?";
            yield return new Finding
            {
                Id = Id,
                InstanceKey = g.PnpDeviceId,
                Subject = g.Name,
                Kind = FindingKind.Finding,
                Status = age is null ? FindingStatus.Unknown : age > MaxAgeDays ? FindingStatus.Problem : FindingStatus.Ok,
                Impact = 2,
                Effects = [Effect.Fps, Effect.Stability],
                Facts =
                [
                    new("fact.gpu", g.Name),
                    new("fact.driverVersion", version),
                    new("fact.driverDate", g.DriverDate?.ToString("yyyy-MM-dd") ?? "@unknown"),
                    new("fact.driverAgeDays", age?.ToString() ?? "@unknown"),
                ],
                Params = new Dictionary<string, string>
                {
                    ["gpu"] = g.Name,
                    ["vendorUrl"] = g.Vendor switch
                    {
                        Vendor.Nvidia => "https://www.nvidia.com/Download/index.aspx",
                        Vendor.Amd => "https://www.amd.com/en/support/download/drivers.html",
                        Vendor.Intel => "https://www.intel.com/content/www/us/en/download-center/home.html",
                        _ => "",
                    },
                },
            };
        }
    }
}

/// <summary>Advisor: Resizable BAR. Unsupported GPU family -> "Unsupported", never BIOS advice (plan v4 §5.3).</summary>
public sealed class RebarCheck : IFindingCheck
{
    public const string Id = "A.rebar";

    /// <summary>A BAR above 256 MB means the large BAR is active (classic BAR1 is 256 MB).</summary>
    public const long ClassicBarBytes = 256L * 1024 * 1024;

    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Gpus is null) yield break;
        foreach (var g in p.Gpus.Where(g => g.Kind == GpuKind.Discrete))
        {
            var rule = GpuProbe.RebarSupport(g, c);
            var active = g.LargestBarBytes is { } bar ? bar > ClassicBarBytes : (bool?)null;
            FindingStatus status;
            string variant;
            if (rule is null) { status = FindingStatus.Unknown; variant = "unknownGpu"; }
            else if (!rule.Supported) { status = FindingStatus.Unsupported; variant = "unsupported"; }
            else if (active is null) { status = FindingStatus.Unknown; variant = "unknownState"; }
            else if (active.Value) { status = FindingStatus.Ok; variant = "active"; }
            else { status = FindingStatus.Problem; variant = "off"; }

            var vendor = c.Bios.NormalizeVendor(p.Firmware?.BoardManufacturer);
            var menu = c.Bios.Find(vendor, "*", "rebar");
            yield return new Finding
            {
                Id = Id,
                InstanceKey = g.PnpDeviceId,
                Subject = g.Name,
                Kind = FindingKind.Advisor,
                Status = status,
                Variant = variant,
                Impact = rule?.Critical == true ? 5 : 3,
                Effects = [Effect.Fps, Effect.Lows],
                Facts =
                [
                    new("fact.gpu", g.Name),
                    new("fact.rebarSupported", rule is null ? "@unknown" : rule.Supported ? "@yes" : "@no"),
                    new("fact.largestBar", g.LargestBarBytes is { } b ? FormatBytes(b) : "@unknown"),
                    new("fact.vram", g.VramBytes is { } v ? FormatBytes(v) : "@unknown"),
                    new("fact.uefi", p.Firmware?.IsUefi == true ? "@yes" : p.Firmware is null ? "@unknown" : "@no"),
                    new("fact.systemDiskStyle", p.Firmware?.SystemDiskPartitionStyle.ToString().ToUpperInvariant() ?? "@unknown"),
                ],
                Params = new Dictionary<string, string>
                {
                    ["gpu"] = g.Name,
                    ["board"] = $"{p.Firmware?.BoardManufacturer} {p.Firmware?.BoardProduct}".Trim(),
                    ["menuPath"] = menu?.Path ?? "",
                    ["mbr"] = p.Firmware?.SystemDiskPartitionStyle == PartitionStyle.Mbr ? "yes" : "",
                },
            };
        }
    }

    public static string FormatBytes(long bytes) =>
        bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.#} GB" : $"{bytes / (double)(1L << 20):0} MB";
}

/// <summary>
/// Advisor: PCIe link width of the graphics card. Generation drops at idle, so only the width is judged at idle;
/// the generation is shown as information (measured under load in M6).
/// </summary>
public sealed class PcieLinkCheck : IFindingCheck
{
    public const string Id = "A.pcieLink";
    public IReadOnlyList<string> DocIds => [Id];

    public enum LinkVerdict { Ok, SlotLimited, TrainedDown, Unknown }

    public static LinkVerdict Judge(PcieLink? card, PcieLink? platform)
    {
        if (card?.MaxWidth is not { } cardMax || card.CurrentWidth is not { } current || current == 0) return LinkVerdict.Unknown;
        if (current >= cardMax) return LinkVerdict.Ok;
        if (platform?.MaxWidth is { } slotMax && slotMax < cardMax && current >= slotMax) return LinkVerdict.SlotLimited;
        return LinkVerdict.TrainedDown;
    }

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Gpus is null) yield break;
        foreach (var g in p.Gpus.Where(g => g.Kind == GpuKind.Discrete))
        {
            var verdict = Judge(g.CardLink, g.PlatformPortLink);
            yield return new Finding
            {
                Id = Id,
                InstanceKey = g.PnpDeviceId,
                Subject = g.Name,
                Kind = FindingKind.Advisor,
                Status = verdict switch
                {
                    LinkVerdict.Ok => FindingStatus.Ok,
                    LinkVerdict.Unknown => FindingStatus.Unknown,
                    _ => FindingStatus.Problem,
                },
                Variant = verdict switch { LinkVerdict.SlotLimited => "slotLimited", LinkVerdict.TrainedDown => "trainedDown", _ => null },
                Impact = verdict == LinkVerdict.SlotLimited && g.CardLink?.MaxWidth >= 16 && g.PlatformPortLink?.MaxWidth >= 8 ? 2 : 3,
                Effects = [Effect.Fps, Effect.Lows],
                Facts =
                [
                    new("fact.gpu", g.Name),
                    new("fact.cardMaxLink", Format(g.CardLink?.MaxGen, g.CardLink?.MaxWidth)),
                    new("fact.currentLink", Format(g.CardLink?.CurrentGen, g.CardLink?.CurrentWidth)),
                    new("fact.slotMaxLink", Format(g.PlatformPortLink?.MaxGen, g.PlatformPortLink?.MaxWidth)),
                    new("fact.onCardSwitchHops", g.SwitchHopsSkipped.ToString()),
                ],
                Params = new Dictionary<string, string> { ["gpu"] = g.Name },
            };
        }
    }

    private static string Format(int? gen, int? width) =>
        gen is null && width is null ? "@unknown" : $"{(gen is null ? "Gen ?" : $"Gen {gen}")} x{width?.ToString() ?? "?"}";
}
