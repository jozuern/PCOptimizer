using Optimizer.Core.Catalog;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware.Probes;

public static class GpuProbe
{
    public static List<GpuInfo> Read(CatalogData catalog)
    {
        var hags = Reg.HklmInt(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode") switch
        {
            2 => TriState.Yes,
            1 => TriState.No,
            _ => TriState.Unknown,
        };
        var switchVendors = catalog.Gpu.OnCardSwitchVendors.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var list = new List<GpuInfo>();
        foreach (var row in Wmi.Query("SELECT Name, PNPDeviceID, DriverVersion, DriverDate, InfFilename, AdapterCompatibility FROM Win32_VideoController"))
        {
            var name = row.Str("Name");
            var pnp = row.Str("PNPDeviceID");
            var vendor = VendorFromPnp(pnp, catalog);
            var (vram, provider) = ReadDriverKey(pnp);
            var kind = Classify(name, pnp, vendor, row.Str("InfFilename"), catalog);

            long? bar = null;
            PcieLink? card = null, platform = null;
            var hops = 0;
            if (pnp.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase) && PciDevice.Locate(pnp) is { } dev)
            {
                bar = Safe(() => PciDevice.LargestMemoryRange(dev));
                (card, platform, hops) = Safe(() => PciDevice.WalkGpuLink(dev, switchVendors));
            }

            list.Add(new GpuInfo(name, vendor, pnp, row.Str("DriverVersion"), row.Date("DriverDate"), row.Str("InfFilename"),
                provider, vram, kind, bar, card, platform, hops, hags));
        }
        return list;
    }

    public static Vendor VendorFromPnp(string? pnp, CatalogData catalog)
    {
        var id = PciDevice.VendorId(pnp);
        if (id is null || !catalog.Gpu.PciVendors.TryGetValue(id, out var v)) return Vendor.Unknown;
        return ParseVendor(v);
    }

    public static Vendor ParseVendor(string v) => v.ToLowerInvariant() switch
    {
        "nvidia" => Vendor.Nvidia,
        "amd" => Vendor.Amd,
        "intel" => Vendor.Intel,
        "microsoft" => Vendor.Microsoft,
        "qualcomm" => Vendor.Qualcomm,
        _ => Vendor.Other,
    };

    public static GpuKind Classify(string name, string pnp, Vendor vendor, string? inf, CatalogData catalog)
    {
        if (catalog.Gpu.BasicPatterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase)) ||
            string.Equals(inf, "display.inf", StringComparison.OrdinalIgnoreCase))
            return GpuKind.Basic;
        if (catalog.Gpu.VirtualPatterns.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase)) ||
            !pnp.StartsWith("PCI\\", StringComparison.OrdinalIgnoreCase))
            return GpuKind.Virtual;
        var key = vendor switch { Vendor.Nvidia => "nvidia", Vendor.Amd => "amd", Vendor.Intel => "intel", _ => null };
        if (key is null || !catalog.Gpu.DiscretePatterns.TryGetValue(key, out var pattern)) return GpuKind.Unknown;
        return RegexCache.Get(pattern).IsMatch(name) ? GpuKind.Discrete : GpuKind.Integrated;
    }

    /// <summary>ReBAR support from the catalog table; null = GPU family not in the table (-> Unknown, no advice).</summary>
    public static RebarRule? RebarSupport(GpuInfo gpu, CatalogData catalog)
    {
        var key = gpu.Vendor switch { Vendor.Nvidia => "nvidia", Vendor.Amd => "amd", Vendor.Intel => "intel", _ => "" };
        return catalog.Gpu.Rebar.FirstOrDefault(r => r.Vendor == key && RegexCache.Get(r.Regex).IsMatch(gpu.Name));
    }

    private static (long? Vram, string? Provider) ReadDriverKey(string pnp)
    {
        try
        {
            var driver = Reg.HklmString($@"SYSTEM\CurrentControlSet\Enum\{pnp}", "Driver");
            if (driver is null) return (null, null);
            var path = $@"SYSTEM\CurrentControlSet\Control\Class\{driver}";
            var provider = Reg.HklmString(path, "ProviderName");
            var mem = Reg.HklmValue(path, "HardwareInformation.qwMemorySize") switch
            {
                long l => l,
                byte[] b when b.Length >= 8 => BitConverter.ToInt64(b, 0),
                _ => Reg.HklmValue(path, "HardwareInformation.MemorySize") switch
                {
                    int i => (long)(uint)i,
                    byte[] b when b.Length >= 4 => BitConverter.ToUInt32(b, 0),
                    _ => (long?)null,
                },
            };
            return (mem, provider);
        }
        catch (Exception)
        {
            return (null, null);
        }
    }

    private static T? Safe<T>(Func<T> f)
    {
        try
        {
            return f();
        }
        catch (Exception)
        {
            return default;
        }
    }
}
