using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;
using Optimizer.Core.Hardware.Probes;

namespace Optimizer.Core.Findings.Checks;

/// <summary>
/// F5: GPU on Microsoft Basic Display Adapter (no vendor driver). Impact 5 when the adapter is the graphics card or the
/// only GPU; an extra adapter without a display next to a vendor-driven graphics card (usually the unused iGPU) is
/// information with impact 1 (variant "secondary").
/// </summary>
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
        var secondary = basic.Count > 0 && basic.All(b => IsSecondary(b, p));
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = basic.Count == 0 ? FindingStatus.Ok : secondary ? FindingStatus.Info : FindingStatus.Problem,
            Variant = secondary ? "secondary" : null,
            Impact = secondary ? 1 : 5,
            Effects = [Effect.Fps, Effect.Stability],
            Facts = p.Gpus.Where(g => g.Kind != GpuKind.Virtual)
                .Select(g => new Fact("fact.gpuDriver", $"{g.Name}: {g.DriverProvider ?? "?"} {g.DriverVersion}")).ToList(),
        };
    }

    /// <summary>
    /// A basic-driver adapter that is not the graphics card: a vendor-driven discrete GPU exists, the basic one is not an
    /// NVIDIA device (NVIDIA makes no integrated GPUs for Windows PCs) and no display is connected to it.
    /// Without display data it cannot be told apart, so it counts as the main GPU.
    /// </summary>
    private static bool IsSecondary(GpuInfo basic, HardwareProfile p) =>
        basic.Vendor != Vendor.Nvidia &&
        p.Gpus!.Any(g => g.Kind == GpuKind.Discrete) &&
        p.Displays is { } displays &&
        !displays.Any(d => string.Equals(d.AdapterName, basic.Name, StringComparison.Ordinal));
}

/// <summary>F15 (GPU part): driver older than ~6 months.</summary>
public sealed class GpuDriverAgeCheck : IFindingCheck
{
    public const string Id = "F15.gpuDriverAge";
    public const int MaxAgeDays = 183;
    public IReadOnlyList<string> DocIds => [Id];

    private readonly Func<DateTime> _today;

    /// <param name="today">Clock for tests; default: the local date.</param>
    public GpuDriverAgeCheck(Func<DateTime>? today = null) => _today = today ?? (() => DateTime.Today);

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Gpus is null) yield break;
        var hasDiscrete = p.Gpus.Any(g => g.Kind == GpuKind.Discrete);
        foreach (var g in p.Gpus.Where(g => g.Kind is GpuKind.Discrete or GpuKind.Integrated))
        {
            // An unused iGPU next to the graphics card on a desktop does not affect games: no driver advice for it.
            if (g.Kind == GpuKind.Integrated && hasDiscrete && !p.IsLaptop && p.Displays is { } displays &&
                !displays.Any(d => string.Equals(d.AdapterName, g.Name, StringComparison.Ordinal)))
                continue;
            var age = g.DriverDate is { } d ? (int)(_today() - d.Date).TotalDays : (int?)null;
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
                        Vendor.Nvidia => "https://www.nvidia.com/en-us/drivers/",
                        Vendor.Amd => "https://www.amd.com/en/support/download/drivers.html",
                        Vendor.Intel => "https://www.intel.com/content/www/us/en/support/detect.html",
                        _ => "",
                    },
                },
            };
        }
    }
}

/// <summary>
/// Advisor: Resizable BAR. Unsupported GPU family -> "Unsupported", never BIOS advice. Laptops get no BIOS
/// advice either: an inactive BAR is information there (variant "laptop"), only the laptop maker can add support.
/// </summary>
public sealed class RebarCheck : IFindingCheck
{
    public const string Id = "A.rebar";

    /// <summary>
    /// Processors whose platform supports Resizable BAR: Intel Core 10th generation and newer (NVIDIA's list), AMD Ryzen
    /// 3000 and newer (AMD's Smart Access Memory claims), that is Zen 2 (family 17h from model 30h) and later families.
    /// Ryzen 1000 and 2000 and the Zen+ APUs 3200G and 3400G (family 17h below model 30h) are older. Null: not known.
    /// </summary>
    public static bool? PlatformSupportsRebar(CpuInfo? cpu)
    {
        if (cpu is null) return null;
        if (cpu.Vendor == Vendor.Amd) return cpu.Family switch { < 0x17 => false, 0x17 => cpu.Model >= 0x30, _ => true };
        if (cpu.Vendor != Vendor.Intel) return null;
        if (cpu.Name.Contains("Core(TM) Ultra", StringComparison.OrdinalIgnoreCase) || cpu.Name.Contains("Core Ultra", StringComparison.OrdinalIgnoreCase)) return true;
        var m = RegexCache.Get(@"\bi[3579]-(\d{4,5})").Match(cpu.Name);
        if (!m.Success) return null;
        var number = m.Groups[1].Value;
        var generation = number.Length == 5 ? int.Parse(number[..2]) : int.Parse(number[..1]);
        return generation >= 10;
    }

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
            else if (p.IsLaptop) { status = FindingStatus.Info; variant = "laptop"; }
            // Only a platform that supports it can turn it on: older or unknown processors get no BIOS steps.
            else if (PlatformSupportsRebar(p.Cpu) != true) { status = FindingStatus.Info; variant = "platform"; }
            else { status = FindingStatus.Problem; variant = "off"; }

            var vendor = c.Bios.NormalizeVendor(p.Firmware?.BoardManufacturer);
            var menu = c.Bios.Find(vendor, BiosPlatform(p.Cpu), "rebar");
            yield return new Finding
            {
                Id = Id,
                InstanceKey = g.PnpDeviceId,
                Subject = g.Name,
                Kind = FindingKind.Advisor,
                Status = status,
                Variant = variant,
                // NVIDIA: "a few percent, up to 12%" in profiled games; Intel calls ReBAR required for Arc.
                Impact = rule?.Critical == true ? 5 : 2,
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
                    ["menuUnverified"] = menu is { Verified: false } ? "yes" : "",
                    ["mbr"] = p.Firmware?.SystemDiskPartitionStyle == PartitionStyle.Mbr ? "yes" : "",
                    ["laptop"] = p.IsLaptop ? "yes" : "",
                },
            };
        }
    }

    /// <summary>Platform key for bios.json rebar entries: "intel", "amd" or "*" (Find falls back to "*").</summary>
    public static string BiosPlatform(CpuInfo? cpu) => cpu?.Vendor switch
    {
        Vendor.Intel => "intel",
        Vendor.Amd => "amd",
        _ => "*",
    };

    public static string FormatBytes(long bytes) => Platform.ByteSize.Format(bytes);
}

/// <summary>
/// Advisor: PCIe link width of the graphics card. Generation drops at idle, so only the width is judged at idle;
/// the generation is shown as information (measured under load by the throttle check).
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
            // Laptop graphics are often wired with fewer lanes by design: information, never "move the card".
            var designLimited = p.IsLaptop && verdict is LinkVerdict.SlotLimited or LinkVerdict.TrainedDown;
            yield return new Finding
            {
                Id = Id,
                InstanceKey = g.PnpDeviceId,
                Subject = g.Name,
                Kind = FindingKind.Advisor,
                Status = designLimited ? FindingStatus.Info : verdict switch
                {
                    LinkVerdict.Ok => FindingStatus.Ok,
                    LinkVerdict.Unknown => FindingStatus.Unknown,
                    _ => FindingStatus.Problem,
                },
                Variant = designLimited ? "designLimited" : verdict switch { LinkVerdict.SlotLimited => "slotLimited", LinkVerdict.TrainedDown => "trainedDown", _ => null },
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
