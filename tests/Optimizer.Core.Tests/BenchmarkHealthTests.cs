using System.Diagnostics;
using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Catalog;
using Optimizer.Core.Cleanup;
using Optimizer.Core.Debloat;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

public class BenchmarkHealthTests
{
    private static IEnumerable<string> Csv(params double[] ms) =>
        ["Application,ProcessID,PresentMode,MsBetweenPresents", .. ms.Select(m => $"game.exe,1,Hardware: Independent Flip,{m.ToString(System.Globalization.CultureInfo.InvariantCulture)}")];

    [Fact]
    public void PresentMonStats()
    {
        var times = Enumerable.Repeat(10.0, 990).Concat(Enumerable.Repeat(30.0, 10)).ToArray();
        var s = PresentMon.Parse(Csv(times))!;
        Assert.Equal(1000, s.Frames);
        Assert.Equal(1000 / 10.2, s.AverageFps, 3);      // 10.2 s for 1000 frames
        Assert.InRange(s.OnePercentLowFps, 33.3, 100.0); // p99 lies at the edge of the slow frames
        Assert.Equal("Hardware: Independent Flip", s.PresentMode);
        Assert.Null(PresentMon.Parse(Csv(16.6, 16.6)));  // too few frames
        Assert.True(PresentMon.IsSafeProcessName("cs2.exe"));
        Assert.False(PresentMon.IsSafeProcessName("x.exe\" --output_file C:\\Windows\\a"));
    }

    [Fact]
    public void BenchmarkComparisonNeedsSeparatedRanges()
    {
        FrameStats R(double fps) => new(1000, 10, fps, fps * 0.7, 1000 / (fps * 0.7), null);
        Assert.Equal(Comparison.NotEnoughRuns, PresentMon.Compare([R(100), R(101)], [R(110), R(111), R(112)]).Result);
        Assert.Equal(Comparison.NoMeasurableDifference, PresentMon.Compare([R(100), R(104), R(102)], [R(103), R(106), R(105)]).Result);
        var better = PresentMon.Compare([R(100), R(101), R(102)], [R(108), R(109), R(110)]);
        Assert.Equal(Comparison.Better, better.Result);
        Assert.Equal(7.9, better.ChangePercent, 1); // (109 - 101) / 101
    }

    [Fact]
    public void PresentMonResourceMatchesItsHash()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pco-pm-" + Guid.NewGuid().ToString("N"));
        try
        {
            var exe = PresentMon.Extract(dir);
            Assert.True(File.Exists(exe));
            Assert.Equal(exe, PresentMon.Extract(dir)); // second call keeps the verified file
        }
        finally
        {
            TestFolders.Delete(dir);
        }
    }

    [Fact]
    public void ThrottleSummaryCountsOnlyBusySamples()
    {
        var now = DateTime.UtcNow;
        ThrottleSample S(double limit, double util, ulong reasons = 0) =>
            new(now, limit, 100, util, [new Interop.Nvml.Sample("RTX", 70, 1800, 99, 200, 4, 16, 4, reasons)]);
        var samples = new List<ThrottleSample>();
        samples.AddRange(Enumerable.Range(0, 8).Select(_ => S(100, 90)));
        samples.AddRange(Enumerable.Range(0, 2).Select(_ => S(80, 95, Interop.Nvml.ReasonSwThermal)));
        samples.AddRange(Enumerable.Range(0, 10).Select(_ => S(50, 5))); // idle: ignored for CPU
        var r = ThrottleMonitor.Summarize(samples);
        Assert.Equal(0.2, r.CpuLimitedShare, 3);
        Assert.Equal(80, r.CpuLowestLimit);
        Assert.True(r.CpuThrottled);
        Assert.Equal(0.1, r.GpuReasonShare["thermal"], 3);
        Assert.False(r.GpuThermal); // exactly 10 % is not above the threshold
    }

    private static HardwareProfile P(HardwareExtras extras, bool laptop = false) => new()
    {
        Os = TestData.Os(),
        Extras = extras,
        System = laptop ? new SystemInfo("x", "y", [10], true, true, false) : null,
    };

    private static void AssertRenders(Finding f)
    {
        foreach (var lang in DocStore.Languages)
        {
            var md = DocStore.RenderFinding(DocStore.Get(f.Id, lang)!, f, Labels.Current);
            Assert.DoesNotContain("{{", md);
            Assert.DoesNotContain(":::", md);
            foreach (var fact in f.Facts.Where(x => x.Value.StartsWith('@')))
                Assert.True(Labels.Current.Has(lang, "value." + fact.Value[1..]), fact.Value);
        }
    }

    [Fact]
    public void StorageAndHealthFindingsRender()
    {
        var throttle = new ThrottleResult(DateTimeOffset.Now, 120, 0.4, 72, 0.8, new Dictionary<string, double> { ["thermal"] = 0, ["powerLimit"] = 0.9 }, 76);
        var f23 = new ThrottleCheck().Evaluate(P(new HardwareExtras { LastThrottle = throttle }, laptop: true), CatalogData.Current).Single();
        Assert.Equal((FindingStatus.Problem, "cpu"), (f23.Status, f23.Variant));
        AssertRenders(f23);
        var gpuPowerOnly = throttle with { CpuLimitedShare = 0 };
        Assert.Equal(FindingStatus.Info, new ThrottleCheck().Evaluate(P(new HardwareExtras { LastThrottle = gpuPowerOnly }), CatalogData.Current).Single().Status);
        Assert.Empty(new ThrottleCheck().Evaluate(P(new HardwareExtras()), CatalogData.Current));

        var disks = new[] { new DiskHealth("Samsung SSD 970 EVO Plus", "SSD", "NVMe", "Healthy", 40, 70, 93, 0, 0, 12000), new DiskHealth("ST2000", "HDD", "SATA", "Unhealthy", 35, 50, null, 10, 2, 40000) };
        var f28 = new DiskHealthCheck().Evaluate(P(new HardwareExtras { DiskHealth = disks }), CatalogData.Current).ToList();
        Assert.All(f28, f => Assert.Equal(FindingStatus.Problem, f.Status));
        Assert.False(f28[0].Critical);
        Assert.True(f28[1].Critical);
        f28.ForEach(AssertRenders);

        var f16 = new StartupCountCheck().Evaluate(P(new HardwareExtras { StartupPrograms = Enumerable.Range(1, 9).Select(i => $"App {i}").ToList() }), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Info, f16.Status);
        AssertRenders(f16);

        var sample = new[] { new ProcessCpu("MsMpEng", 1, 0.12, 1, null), new ProcessCpu("OneDrive", 2, 0.06, 1, null), new ProcessCpu("dwm", 3, 0.2, 1, null), new ProcessCpu("tiny", 4, 0.01, 1, null) };
        var f11 = new BackgroundCpuCheck().Evaluate(P(new HardwareExtras { BackgroundCpu = sample }), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Info, f11.Status);
        Assert.Equal("MsMpEng, OneDrive", f11.Params["names"]);
        Assert.Equal("yes", f11.Params["defender"]);
        AssertRenders(f11);
    }

    [Fact]
    public void ToolOutputKeepsOneLinePerProgress()
    {
        var lines = new List<string>();
        foreach (var l in new[]
                 {
                     "Beginning verification phase of system scan.", "Verification 1% complete.", "Verification 1% complete.",
                     "Verification 57% complete.", "Verification 100% complete.", "Windows Resource Protection did not find any integrity violations.",
                     "[=          2.0%            ]", "[==========100.0%==========]", "The operation completed successfully.",
                     "Überprüfung 3 % abgeschlossen.", "Überprüfung 4 % abgeschlossen.", "Fortschritt: 5 %", "Überprüfung 6 % abgeschlossen.",
                 })
            Optimizer.Core.Platform.OutputLines.Add(lines, l);
        Assert.Equal(new[]
        {
            "Beginning verification phase of system scan.", "Verification 100% complete.", "Windows Resource Protection did not find any integrity violations.",
            "[==========100.0%==========]", "The operation completed successfully.",
            "Überprüfung 4 % abgeschlossen.", "Fortschritt: 5 %", "Überprüfung 6 % abgeschlossen.", // a different progress line is kept
        }, lines);

        var capped = new List<string>();
        for (var i = 0; i < 5; i++) Optimizer.Core.Platform.OutputLines.Add(capped, $"line {i}", max: 3);
        Assert.Equal(new[] { "line 2", "line 3", "line 4" }, capped);
    }
}
