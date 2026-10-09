using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;
using Optimizer.Core.Platform;

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

    /// <summary>
    /// Some firmware reports ConfiguredClockSpeed in MHz (half the MT/s rate). Retail DDR4 starts at 2133 MT/s and
    /// DDR5 at 4800 MT/s (JEDEC), so a value below that for the type is treated as MHz (DDR4-3200 -> "1600").
    /// </summary>
    public static int? NormalizeConfigured(int? value, string type) => value switch
    {
        null or 0 => null,
        < 2133 when type == "DDR4" => value * 2,
        < 4000 when type == "DDR5" => value * 2,
        _ => value,
    };

    public static char? Channel(MemoryModule m, CatalogData c)
    {
        var text = $"{m.DeviceLocator} {m.BankLabel}";
        foreach (var pattern in c.Ram.ChannelPatterns)
        {
            var match = RegexCache.Get(pattern).Match(text);
            if (match.Success) return char.ToUpperInvariant(match.Groups[1].Value[0]);
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

/// <summary>Advisor: RAM running below its rated speed (XMP / EXPO / D.O.C.P off).</summary>
public sealed class XmpCheck : IFindingCheck
{
    public const string Id = "A.xmp";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Memory is null || p.Memory.Modules.Count == 0) yield break;
        var first = p.Memory.Modules[0];
        var rated = p.Memory.Modules.Select(m => RamSpeed.DecodeRated(m.PartNumber, c)).ToList();
        var configured = p.Memory.Modules.Select(m => RamSpeed.NormalizeConfigured(m.ConfiguredMts, m.Type)).ToList();
        var minRated = rated.All(r => r is not null) ? rated.Min() : null;
        var minConfigured = configured.All(v => v is not null) ? configured.Min() : null;

        var status = minRated is null || minConfigured is null ? FindingStatus.Unknown
            : minConfigured < minRated * (1 - c.Ram.SpeedToleranceFraction) ? FindingStatus.Problem
            : FindingStatus.Ok;

        var vendor = c.Bios.NormalizeVendor(p.Firmware?.BoardManufacturer);
        var menu = c.Bios.Find(vendor, RamSpeed.Platform(p.Cpu, first.Type), "xmp");
        var laptop = p.IsLaptop;

        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Advisor,
            Status = laptop && status == FindingStatus.Problem ? FindingStatus.Info : status, // laptops rarely offer XMP
            Variant = laptop ? "laptop" : null,
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
            // Soldered memory ("row of chips", form factor 7) can be dual channel internally -> Unknown.
            status = modules[0].FormFactor == 7 ? FindingStatus.Unknown : FindingStatus.Problem;
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

/// <summary>F17: low free space (&lt; 10 %) on the system drive or a game drive.</summary>
public sealed class LowDiskSpaceCheck : IFindingCheck
{
    public const string Id = "F17.lowDiskSpace";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Storage is null) yield break;
        foreach (var v in p.Storage.Volumes)
        {
            yield return new Finding
            {
                Id = Id,
                InstanceKey = v.Root,
                Subject = v.Root,
                Kind = FindingKind.Finding,
                Status = v.FreeFraction < c.Storage.LowFreeSpaceFraction ? FindingStatus.Problem : FindingStatus.Ok,
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
                Impact = disk.IsSmrSuspect ? 4 : 3,
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

/// <summary>F18: TRIM disabled (DisableDeleteNotification = 1). Read from the registry, no fsutil call.</summary>
public sealed class TrimCheck : IFindingCheck
{
    public const string Id = "F18.trim";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Storage is null || !p.Storage.Disks.Any(d => d.MediaType == "SSD")) yield break;
        var value = Reg.HklmInt(@"SYSTEM\CurrentControlSet\Control\FileSystem", "DisableDeleteNotification");
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = value is null ? FindingStatus.Unknown : value == 1 ? FindingStatus.Problem : FindingStatus.Ok,
            Impact = 1,
            Effects = [Effect.Stutter],
            Facts = [new("fact.trim", value switch { 0 => "@on", 1 => "@off", _ => "@unknown" })],
        };
    }
}
