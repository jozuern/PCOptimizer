using System.Text.Json;
using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;
using Optimizer.Core.Hardware.Probes;
using Optimizer.Core.Interop;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Read-only checks of the M3/M4 probes on this machine (Category=Hardware).</summary>
public class ExtrasHardwareTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Hardware")]
    public async Task ExtrasOnThisPc()
    {
        var profile = await new HardwareScanner(CatalogData.Current).ScanAsync();
        output.WriteLine(JsonSerializer.Serialize(profile.Extras, new JsonSerializerOptions { WriteIndented = true }));
        Assert.NotNull(profile.Extras);
    }

    [Fact]
    [Trait("Category", "Hardware")]
    public void WlanBssLayoutGivesRealFrequencies()
    {
        var networks = Wlan.VisibleNetworks();
        output.WriteLine($"{networks.Count} access points visible");
        foreach (var n in networks.Take(20)) output.WriteLine($"{n.Bssid} {n.CenterFrequencyKhz} kHz {Wlan.Band(n.CenterFrequencyKhz)} '{n.Ssid}'");
        // Every frequency must be a real Wi-Fi channel center (2.4 / 5 / 6 GHz bands), proving the struct offsets.
        Assert.All(networks, n => Assert.True(n.CenterFrequencyKhz is >= 2_400_000 and <= 7_200_000, $"{n.CenterFrequencyKhz}"));
    }

    [Fact]
    [Trait("Category", "Hardware")]
    public void NvmlReadsWithoutError()
    {
        foreach (var s in Nvml.Read())
            output.WriteLine($"{s.Name}: {s.TempC} C, {s.GraphicsMhz} MHz, {s.UtilizationPct} %, {s.PowerW} W, PCIe Gen {s.PcieGen} x{s.PcieWidth} (max Gen {s.PcieMaxGen}), reasons 0x{s.Reasons:X}");
    }

    [Theory]
    [InlineData("0", null)]
    [InlineData("4", 100)]
    [InlineData("6", 1000)]
    [InlineData("10", 100_000)]
    [InlineData("2500", 2500)]
    [InlineData("999", null)]
    public void SpeedDuplexMapping(string value, int? mbps) => Assert.Equal(mbps, ExtrasProbe.SpeedDuplexMbps(value));
}
