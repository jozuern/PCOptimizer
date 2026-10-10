using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings.Checks;

public static class RamSpeed
{
    /// <summary>Rated speed decoded from the part number (catalog decoders). Null = unknown part number -> no advice.</summary>
    public static int? DecodeRated(string partNumber, CatalogData c)
    {
        var part = partNumber.Trim().ToUpperInvariant();
        foreach (var d in c.Ram.Decoders)
        {
            var m = RegexCache.Get(d.Regex).Match(part);
            if (m.Success && int.TryParse(m.Groups[1].Value, out var v)) return v * d.Multiplier;
        }
        return null;
    }

    /// <summary>Lowest JEDEC speed of the type in MT/s (DDR4-2133, DDR5-4800).</summary>
    private static int? JedecMinimum(string type) => type switch { "DDR4" => 2133, "DDR5" => 4800, _ => null };

    /// <summary>
    /// The configured speed in MT/s. Some firmware reports ConfiguredClockSpeed in MHz (half the MT/s rate, DDR4-3200 as
    /// "1600"), but low MT/s values are real too: four DDR5 modules on AM5 run at 3600, four dual-rank DDR4 modules on
    /// Ryzen 1000 at 1866. The module's own speed (Win32_PhysicalMemory.Speed) decides the unit: a value is MHz when the
    /// module speed is below the JEDEC minimum too (the firmware uses MHz for both), or when twice the value still fits
    /// the module speed (a real 3600 MT/s on a DDR5-4800 module does not). Without the module speed, only values no real
    /// configuration uses are doubled, and the range where both readings are plausible stays unknown. Values below the
    /// lowest real speed are doubled in every case (XMP on: DDR4-3200 as 1600 next to a module speed of 2133).
    /// </summary>
    public static int? NormalizeConfigured(int? value, string type, int? moduleSpeed)
    {
        if (value is null or <= 0) return null;
        if (JedecMinimum(type) is not { } min) return value;
        // Below the lowest speed any real configuration runs at, the value is MHz whatever the module reports. With XMP
        // or EXPO on, the module speed is the JEDEC rate, so twice an MHz value (DDR4-3200 as 1600) is above it.
        var lowestReal = type == "DDR5" ? 3600 : 1866;
        if (value < lowestReal) return value * 2;
        if (moduleSpeed is { } speed and > 0)
            return value < min && (speed < min * 0.95 || value * 2 <= speed * 1.05) ? value * 2 : value;
        return value < min ? null : value;
    }

    public static int? NormalizeConfigured(MemoryModule m) => NormalizeConfigured(m.ConfiguredMts, m.Type, m.SpeedMts);

    /// <summary>
    /// The memory channel of a module from its locator. Intel platforms since 11th gen name one channel A per memory
    /// controller ("Controller0-ChannelA-DIMM0", "Controller1-ChannelA-DIMM0"): every group of the matching pattern
    /// counts, so those are channels "0A" and "1A", not one channel A twice.
    /// </summary>
    public static string? Channel(MemoryModule m, CatalogData c)
    {
        var text = $"{m.DeviceLocator} {m.BankLabel}";
        foreach (var pattern in c.Ram.ChannelPatterns)
        {
            var match = RegexCache.Get(pattern).Match(text);
            if (match.Success) return string.Concat(match.Groups.Values.Skip(1).Select(g => g.Value.ToUpperInvariant()));
        }
        return null;
    }

    public static string Platform(CpuInfo? cpu, string ramType) => cpu?.Vendor switch
    {
        Vendor.Intel => "intel",
        Vendor.Amd => ramType == "DDR5" ? "amd-am5" : "amd-am4",
        _ => "*",
    };
}

/// <summary>
/// Advisor: RAM running below its rated speed (XMP / EXPO / D.O.C.P off). Four DDR5 modules on a Ryzen are expected to
/// run slower: AMD specifies DDR5-3600 for four modules (e.g. "4x1R DDR5-3600 4x2R DDR5-3600"), so that is information
/// (variant "fourDimms"), not a missing setting.
/// </summary>
public sealed class XmpCheck : IFindingCheck
{
    public const string Id = "A.xmp";

    /// <summary>AMD's specified speed for four DDR5 modules (product pages, "4x1R DDR5-3600").</summary>
    public const int FourDimmDdr5Mts = 3600;

    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Memory is null || p.Memory.Modules.Count == 0) yield break;
        var first = p.Memory.Modules[0];
        var rated = p.Memory.Modules.Select(m => RamSpeed.DecodeRated(m.PartNumber, c)).ToList();
        var configured = p.Memory.Modules.Select(RamSpeed.NormalizeConfigured).ToList();
        var minRated = rated.All(r => r is not null) ? rated.Min() : null;
        var minConfigured = configured.All(v => v is not null) ? configured.Min() : null;

        var status = minRated is null || minConfigured is null ? FindingStatus.Unknown
            : minConfigured < minRated * (1 - c.Ram.SpeedToleranceFraction) ? FindingStatus.Problem
            : FindingStatus.Ok;

        var vendor = c.Bios.NormalizeVendor(p.Firmware?.BoardManufacturer);
        var menu = c.Bios.Find(vendor, RamSpeed.Platform(p.Cpu, first.Type), "xmp");
        var laptop = p.IsLaptop;
        var fourDimms = !laptop && p.Cpu?.Vendor == Vendor.Amd && first.Type == "DDR5" && p.Memory.Modules.Count >= 4 && minRated > FourDimmDdr5Mts;

        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Advisor,
            // Laptops rarely offer XMP; four DDR5 modules on AM5 run below the kit rating by AMD's specification.
            Status = (laptop || fourDimms) && status == FindingStatus.Problem ? FindingStatus.Info : status,
            Variant = laptop ? "laptop" : fourDimms && status == FindingStatus.Problem ? "fourDimms" : null,
            Impact = 4,
            Effects = [Effect.Fps, Effect.Lows],
            Facts =
            [
                .. p.Memory.Modules.Select((m, i) => new Fact("fact.ramModule",
                    $"{m.DeviceLocator}: {m.Manufacturer} {m.PartNumber.Trim()}, {m.CapacityBytes >> 30} GB {m.Type}")),
                new("fact.ramRated", minRated is null ? "@unknown" : $"{minRated} MT/s"),
                new("fact.ramConfigured", minConfigured is null ? "@unknown" : $"{minConfigured} MT/s"),
            ],
            Params = new Dictionary<string, string>
            {
                ["rated"] = minRated?.ToString() ?? "?",
                ["configured"] = minConfigured?.ToString() ?? "?",
                ["profileName"] = menu?.ProfileName ?? (first.Type == "DDR5" && p.Cpu?.Vendor == Vendor.Amd ? "EXPO" : "XMP"),
                ["menuPath"] = menu?.Path ?? "",
                ["menuUnverified"] = menu is { Verified: false } ? "yes" : "",
                ["board"] = $"{p.Firmware?.BoardManufacturer} {p.Firmware?.BoardProduct}".Trim(),
            },
        };
    }
}

/// <summary>Advisor: single channel or both DIMMs in the same channel.</summary>
public sealed class DualChannelCheck : IFindingCheck
{
    public const string Id = "A.dualChannel";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Memory is null || p.Memory.Modules.Count == 0) yield break;
        var modules = p.Memory.Modules;
        var channels = modules.Select(m => RamSpeed.Channel(m, c)).ToList();
        FindingStatus status;
        string? variant = null;
        if (modules.Count == 1)
        {
            // Only a socketed module (Win32_PhysicalMemory.FormFactor 8 = DIMM, 12 = SODIMM) proves single channel.
            // Anything else may be soldered memory, which can be dual channel internally -> Unknown.
            status = modules[0].FormFactor is 8 or 12 ? FindingStatus.Problem : FindingStatus.Unknown;
            variant = "single";
        }
        else if (channels.Any(ch => ch is null))
        {
            status = FindingStatus.Unknown;
        }
        else if (channels.Distinct().Count() == 1)
        {
            status = FindingStatus.Problem;
            variant = "sameChannel";
        }
        else
        {
            status = FindingStatus.Ok;
        }

        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Advisor,
            Status = status,
            Variant = variant,
            Impact = 4,
            Effects = [Effect.Fps, Effect.Lows],
            Facts =
            [
                new("fact.ramModuleCount", modules.Count.ToString()),
                .. modules.Select((m, i) => new Fact("fact.ramSlot", $"{m.DeviceLocator} / {m.BankLabel}: {(channels[i] is { } ch ? $"Channel {ch}" : "?")}")),
            ],
            Params = new Dictionary<string, string> { ["board"] = $"{p.Firmware?.BoardManufacturer} {p.Firmware?.BoardProduct}".Trim() },
        };
    }
}

/// <summary>F13: low RAM (≤ 8 GB).</summary>
public sealed class LowRamCheck : IFindingCheck
{
    public const string Id = "F13.lowRam";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Memory is null) yield break;
        var gb = p.Memory.TotalBytes / (double)(1L << 30);
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = gb <= 8.5 ? FindingStatus.Problem : FindingStatus.Ok,
            Impact = 4,
            Effects = [Effect.Stutter, Effect.Lows],
            Facts = [new("fact.ramTotal", $"{gb:0.#} GB")],
        };
    }
}

/// <summary>
/// F17: low free space (&lt; 10 %). Problem on the system drive and drives that hold a game library; other fixed drives
/// (data or backup disks) are information.
/// </summary>
public sealed class LowDiskSpaceCheck : IFindingCheck
{
    public const string Id = "F17.lowDiskSpace";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Storage is null) yield break;
        var gameRoots = (p.Software?.GameLibraryPaths ?? []).Select(Path.GetPathRoot).Where(r => r is not null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var v in p.Storage.Volumes)
        {
            var matters = v.IsSystem || gameRoots.Contains(v.Root);
            var low = v.FreeFraction < c.Storage.LowFreeSpaceFraction;
            yield return new Finding
            {
                Id = Id,
                InstanceKey = v.Root,
                Subject = v.Root,
                Kind = FindingKind.Finding,
                Status = !low ? FindingStatus.Ok : matters ? FindingStatus.Problem : FindingStatus.Info,
                Variant = v.IsSystem ? "system" : gameRoots.Contains(v.Root) ? "games" : "other",
                Impact = v.IsSystem ? 2 : 1,
                Effects = [Effect.Stability, Effect.Stutter],
                Facts =
                [
                    new("fact.volume", $"{v.Root} {v.Label}".Trim()),
                    new("fact.freeSpace", $"{RebarCheck.FormatBytes(v.FreeBytes)} / {RebarCheck.FormatBytes(v.SizeBytes)} ({v.FreeFraction:P0})"),
                ],
                Params = new Dictionary<string, string> { ["volume"] = v.Root },
            };
        }
    }
}

/// <summary>F12: game library on a hard disk (extra note for SMR drives).</summary>
public sealed class GamesOnHddCheck : IFindingCheck
{
    public const string Id = "F12.gamesOnHdd";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Storage is null || p.Software is null || p.Software.GameLibraryPaths.Count == 0) yield break;
        foreach (var library in p.Software.GameLibraryPaths)
        {
            var root = Path.GetPathRoot(library);
            var volume = p.Storage.Volumes.FirstOrDefault(v => string.Equals(v.Root, root, StringComparison.OrdinalIgnoreCase));
            var disk = volume?.DiskNumber is { } n ? p.Storage.Disks.FirstOrDefault(d => d.Number == n) : null;
            if (disk is null) continue;
            yield return new Finding
            {
                Id = Id,
                InstanceKey = library,
                Subject = library,
                Kind = FindingKind.Finding,
                Status = disk.MediaType == "HDD" ? FindingStatus.Problem : disk.MediaType == "Unspecified" ? FindingStatus.Unknown : FindingStatus.Ok,
                Variant = disk.IsSmrSuspect ? "smr" : null,
                // SMR mainly slows sustained writes (installs, updates), not the reads during play: same impact.
                Impact = 3,
                Effects = [Effect.Stutter],
                Facts =
                [
                    new("fact.library", library),
                    new("fact.disk", $"{disk.FriendlyName} ({disk.MediaType}, {disk.BusType})"),
                    new("fact.smr", disk.IsSmrSuspect ? "@yes" : "@no"),
                ],
                Params = new Dictionary<string, string> { ["library"] = library, ["disk"] = disk.FriendlyName },
            };
        }
    }
}

/// <summary>
/// F18: TRIM disabled for NTFS (DisableDeleteNotification = 1). The probe reads the registry (no fsutil call); a missing
/// value means TRIM is on, the NTFS default.
/// </summary>
public sealed class TrimCheck : IFindingCheck
{
    public const string Id = "F18.trim";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Storage is null || !p.Storage.Disks.Any(d => d.MediaType == "SSD")) yield break;
        var trim = p.Extras?.Trim;
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = trim is null ? FindingStatus.Unknown : trim.TrimOn ? FindingStatus.Ok : FindingStatus.Problem,
            Impact = 1,
            Effects = [Effect.Stutter],
            Facts = [new("fact.trim", trim is null ? "@unknown" : trim.TrimOn ? "@on" : "@off")],
        };
    }
}
