using System.Text.Json;
using Optimizer.Core.Catalog;
using Optimizer.Core.Findings;
using Optimizer.Core.Hardware;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Runs the real scanner on this machine (read-only) and prints the results. Never fails on hardware differences.</summary>
public class HardwareSmokeTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "Hardware")]
    public async Task ScanThisPc()
    {
        var profile = await new HardwareScanner(CatalogData.Current).ScanAsync();
        output.WriteLine(JsonSerializer.Serialize(profile, new JsonSerializerOptions { WriteIndented = true }));
        foreach (var f in new FindingEngine(CatalogData.Current).Evaluate(profile))
            output.WriteLine($"{f.Status,-11} ⚡{f.Impact?.ToString() ?? "-"} {f.Key} {f.Subject} {f.Variant}");
        Assert.NotNull(profile.Os);
    }
}
