using System.Diagnostics;
using System.Runtime.InteropServices;
using Optimizer.Core.Interop;

namespace Optimizer.Core.Tools;

public sealed record ThrottleSample(DateTime Time, double? CpuPerformanceLimit, double? CpuPerformance, double? CpuUtility, IReadOnlyList<Nvml.Sample> Gpus);

/// <summary>
/// Result of a throttle check under load. CPU: share of samples where "% Performance Limit" was below 100 (power policy,
/// power budget or heat limited the guaranteed performance). GPU: share of samples per NVML limiting reason.
/// </summary>
public sealed record ThrottleResult(
    DateTimeOffset Measured,
    int Samples,
    double CpuLimitedShare,
    double? CpuLowestLimit,
    double CpuBusyShare,
    IReadOnlyDictionary<string, double> GpuReasonShare,
    int? GpuMaxTempC)
{
    /// <summary>A share above 10 % of busy samples counts as real throttling (short dips are normal).</summary>
    public const double Significant = 0.10;

    /// <summary>
    /// Share of busy samples (above 50 % processor utility) needed to say anything about the processor. A game on a
    /// processor with many cores often stays below that: then the run cannot show processor throttling either way.
    /// </summary>
    public const double MinBusyShare = 0.2;

    public bool CpuJudged => CpuBusyShare > MinBusyShare;
    public bool CpuThrottled => CpuJudged && CpuLimitedShare > Significant;
    public bool GpuThermal => GpuReasonShare.TryGetValue("thermal", out var t) && t > Significant;
    public bool GpuPower => GpuReasonShare.TryGetValue("powerLimit", out var p) && p > Significant;

    /// <summary>
    /// The card's hardware cut its clocks to half or less for a reason other than heat: an external power brake (for
    /// example from the power supply) or power draw spikes (NVML HW slowdown). That is a fault, not normal behavior.
    /// </summary>
    public bool GpuSlowdown => GpuReasonShare.Where(r => r.Key is "powerBrake" or "hardwareSlowdown").Sum(r => r.Value) > Significant;
}

/// <summary>
/// Samples processor performance counters (PDH, English counter names so it works on every display language) and NVIDIA
/// clock limit reasons (NVML) once per second while you play. Nothing is changed.
/// </summary>
public sealed class ThrottleMonitor : IDisposable
{
    private const string Limit = @"\Processor Information(_Total)\% Performance Limit";
    private const string Performance = @"\Processor Information(_Total)\% Processor Performance";
    private const string Utility = @"\Processor Information(_Total)\% Processor Utility";

    private readonly IntPtr _query;
    private readonly IntPtr _limit, _performance, _utility;
    private readonly List<ThrottleSample> _samples = [];

    public ThrottleMonitor()
    {
        if (PdhOpenQuery(null, IntPtr.Zero, out _query) != 0) throw new InvalidOperationException("PdhOpenQuery failed");
        PdhAddEnglishCounter(_query, Limit, IntPtr.Zero, out _limit);
        PdhAddEnglishCounter(_query, Performance, IntPtr.Zero, out _performance);
        PdhAddEnglishCounter(_query, Utility, IntPtr.Zero, out _utility);
        PdhCollectQueryData(_query); // rate counters need a first sample
    }

    public IReadOnlyList<ThrottleSample> Samples => _samples;

    public ThrottleSample Sample()
    {
        PdhCollectQueryData(_query);
        var s = new ThrottleSample(DateTime.UtcNow, Value(_limit), Value(_performance), Value(_utility), Nvml.Available ? Nvml.Read() : []);
        _samples.Add(s);
        return s;
    }

    public async Task<ThrottleResult> RunAsync(TimeSpan duration, IProgress<ThrottleSample>? progress, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < duration && !ct.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                break; // stopping early keeps what was measured
            }
            progress?.Report(Sample());
        }
        return Summarize(_samples);
    }

    public static ThrottleResult Summarize(IReadOnlyList<ThrottleSample> samples)
    {
        // Only samples where the processor was busy say something about throttling under load.
        var busy = samples.Where(s => s.CpuUtility is > 50).ToList();
        var limited = busy.Count(s => s.CpuPerformanceLimit is < 99.5);
        var reasons = new Dictionary<string, double>();
        var gpuSamples = samples.Where(s => s.Gpus.Count > 0).ToList();
        foreach (var reason in new[] { "powerLimit", "thermal", "powerBrake", "hardwareSlowdown" })
        {
            var hits = gpuSamples.Count(s => s.Gpus.Any(g => Nvml.LimitingReasons(g.Reasons ?? 0).Contains(reason)));
            if (gpuSamples.Count > 0) reasons[reason] = (double)hits / gpuSamples.Count;
        }
        return new ThrottleResult(
            DateTimeOffset.Now,
            samples.Count,
            busy.Count == 0 ? 0 : (double)limited / busy.Count,
            busy.Select(s => s.CpuPerformanceLimit).Where(v => v is not null).Min(),
            samples.Count == 0 ? 0 : (double)busy.Count / samples.Count,
            reasons,
            gpuSamples.SelectMany(s => s.Gpus).Select(g => (int?)g.TempC).Max());
    }

    private static double? Value(IntPtr counter)
    {
        if (counter == IntPtr.Zero) return null;
        return PdhGetFormattedCounterValue(counter, PdhFmtDouble | PdhFmtNoCap100, out _, out var v) == 0 && v.CStatus is 0 or 1 ? v.doubleValue : null;
    }

    public void Dispose() => PdhCloseQuery(_query);

    private const uint PdhFmtDouble = 0x00000200, PdhFmtNoCap100 = 0x00008000;

    [StructLayout(LayoutKind.Sequential)]
    private struct PDH_FMT_COUNTERVALUE
    {
        public uint CStatus;
        public double doubleValue;
    }

    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhOpenQueryW")] private static extern uint PdhOpenQuery(string? dataSource, IntPtr userData, out IntPtr query);
    [DllImport("pdh.dll", CharSet = CharSet.Unicode, EntryPoint = "PdhAddEnglishCounterW")] private static extern uint PdhAddEnglishCounter(IntPtr query, string path, IntPtr userData, out IntPtr counter);
    [DllImport("pdh.dll")] private static extern uint PdhCollectQueryData(IntPtr query);
    [DllImport("pdh.dll")] private static extern uint PdhGetFormattedCounterValue(IntPtr counter, uint format, out uint type, out PDH_FMT_COUNTERVALUE value);
    [DllImport("pdh.dll")] private static extern uint PdhCloseQuery(IntPtr query);
}

public sealed record ProcessCpu(string Name, int Pid, double CpuShare, long WorkingSetBytes);

/// <summary>CPU use per process over a short window (F11, "background hogs"). Share = fraction of all logical processors.</summary>
public static class ProcessSampler
{
    public static async Task<IReadOnlyList<ProcessCpu>> SampleAsync(TimeSpan window, CancellationToken ct = default)
    {
        // A snapshot of 300 processes takes a moment while a scan runs: each process is read somewhere inside it, so the
        // window is measured from the middle of one snapshot to the middle of the next (from the end of the first, a
        // slow first snapshot would inflate every share).
        var start1 = Stopwatch.GetTimestamp();
        var first = Snapshot();
        var end1 = Stopwatch.GetTimestamp();
        await Task.Delay(window, ct);
        var start2 = Stopwatch.GetTimestamp();
        var second = Snapshot();
        var end2 = Stopwatch.GetTimestamp();
        var elapsed = Stopwatch.GetElapsedTime(start1 + (end1 - start1) / 2, start2 + (end2 - start2) / 2).TotalMilliseconds * Environment.ProcessorCount;
        var own = Environment.ProcessId;
        var list = new List<ProcessCpu>();
        foreach (var (pid, (name, cpu, ws)) in second)
        {
            if (pid is 0 or 4 || pid == own || !first.TryGetValue(pid, out var before) || before.Name != name) continue;
            var share = (cpu - before.Cpu).TotalMilliseconds / elapsed;
            if (share > 0) list.Add(new ProcessCpu(name, pid, share, ws));
        }
        return list.OrderByDescending(p => p.CpuShare).ToList();
    }

    private static Dictionary<int, (string Name, TimeSpan Cpu, long WorkingSet)> Snapshot()
    {
        var map = new Dictionary<int, (string, TimeSpan, long)>();
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    map[p.Id] = (p.ProcessName, p.TotalProcessorTime, p.WorkingSet64);
                }
                catch (Exception)
                {
                    // access denied (protected processes) or exited
                }
            }
        }
        return map;
    }
}
