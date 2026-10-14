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

    /// <summary>
    /// The Default profile's hive passes the admin-only check on a real Windows (read-only: the path and its ACLs). If
    /// this fails, Copy to new accounts would refuse on every PC.
    /// </summary>
    [Fact]
    [Trait("Category", "Hardware")]
    public void TheDefaultProfileHiveIsFoundAndAdminOnly()
    {
        var file = Optimizer.Core.Platform.DefaultProfileHive.FilePath();
        output.WriteLine(file ?? "(none)");
        Assert.NotNull(file);
        Assert.EndsWith(@"\Default\NTUSER.DAT", file, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>The pipeline the window and --report share: findings come with the scan, the CPU sample is waited for.</summary>
    [Fact]
    [Trait("Category", "Hardware")]
    public async Task ScanServiceRunsTheWholePipeline()
    {
        var result = await new ScanService(CatalogData.Current, null, null).RunAsync();
        Assert.NotNull(result.Profile.Os);
        Assert.Null(result.PendingSample);
        Assert.NotEmpty(result.Findings);
        Assert.Contains(result.Findings, f => f.Id == Optimizer.Core.Findings.Checks.LeftoverCheck.Id);
    }
}
