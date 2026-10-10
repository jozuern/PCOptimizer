using System.Collections.Concurrent;
using System.Diagnostics;
using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware.Probes;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware;

/// <summary>
/// Runs every probe in isolation: a failing probe leaves its section null (-> findings say Unknown)
/// instead of breaking the scan. Read-only.
/// </summary>
public sealed class HardwareScanner(CatalogData catalog)
{
    /// <summary>
    /// Longest wait for one probe. A hung WMI service or driver call cannot be cancelled, but the scan no longer waits
    /// for it: the section stays empty (findings say Unknown) and the timeout is recorded in the probe errors.
    /// </summary>
    public static readonly TimeSpan ProbeTimeLimit = TimeSpan.FromSeconds(60);

    /// <summary>
    /// The background CPU sample of the last scan when the scan did not wait for it (it measures for a fixed 3
    /// seconds). Complete it with <see cref="WithBackgroundSample"/>; only the background activity check (F11) needs it.
    /// </summary>
    public Task<IReadOnlyList<Tools.ProcessCpu>>? PendingBackgroundSample { get; private set; }

    /// <summary>The profile with the finished background CPU sample.</summary>
    public static HardwareProfile WithBackgroundSample(HardwareProfile profile, IReadOnlyList<Tools.ProcessCpu>? sample) =>
        profile.Extras is { } extras ? profile with { Extras = extras with { BackgroundCpu = sample } } : profile;

    /// <summary>
    /// The probes run about 20 parts at once, and most of them wait inside WMI, COM, the registry or a tool. The thread
    /// pool starts with one thread per processor and adds more only slowly, so on a PC with 2 to 4 threads the parts
    /// would wait for each other. Raised once to a minimum of 32 threads.
    /// </summary>
    private static void EnsureThreads()
    {
        ThreadPool.GetMinThreads(out var workers, out var io);
        if (workers < 32) ThreadPool.SetMinThreads(32, io);
    }

    /// <param name="previous">The last result, after a change made by this app: slow parts no change can affect are reused.</param>
    /// <param name="waitForBackgroundSample">
    /// False: return as soon as everything else is read and leave the 3 second CPU sample in <see cref="PendingBackgroundSample"/>.
    /// </param>
    /// <param name="sampleAfter">Other work of the app the CPU sample must not overlap (tools it started meanwhile).</param>
    public async Task<HardwareProfile> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default, HardwareProfile? previous = null,
        bool waitForBackgroundSample = true, Task? sampleAfter = null)
    {
        EnsureThreads();
        var errors = new ConcurrentDictionary<string, string>();
        var sw = Stopwatch.StartNew();

        async Task<T?> Run<T>(string name, Func<T> probe, TimeSpan? limit = null) where T : class
        {
            var timeLimit = limit ?? ProbeTimeLimit;
            var work = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                progress?.Report(name);
                var t = Stopwatch.StartNew();
                try
                {
                    var result = probe();
                    Log.Debug("scan", $"{name} ok", new { ms = t.ElapsedMilliseconds });
                    return result;
                }
                catch (Exception ex)
                {
                    errors[name] = ex.Message;
                    Log.Error("scan", $"{name} failed", ex);
                    return null;
                }
            }, ct);
            try
            {
                // Not back on the caller's thread: the app starts the scan on the UI thread, which is busy building the
                // window meanwhile, and the extras wait for some of these results.
                return await work.WaitAsync(timeLimit, ct).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                errors[name] = $"no answer within {timeLimit.TotalSeconds:0} s";
                Log.Error("scan", $"{name} timed out");
                return null;
            }
        }

        if (previous is null) SystemTaskScheduler.ForgetList();

        var os = BuildInfo.Read();
        var elevation = Run("Elevation", ElevationInfo.Read);
        var managed = Run("Managed", ManagedDeviceInfo.Read);
        var cpu = Run("CPU", CpuProbe.Read);
        var gpus = Run<IReadOnlyList<GpuInfo>>("GPU", () => GpuProbe.Read(catalog));
        var memory = Run("Memory", MemoryProbe.Read);
        var displays = Run<IReadOnlyList<DisplayInfo>>("Displays", () => DisplayProbe.Read(catalog));
        var firmware = Run("Firmware", FirmwareProbe.Read);
        var power = Run("Power", PowerProbe.Read);
        var storage = Run("Storage", () => StorageProbe.Read(catalog));
        var network = Run<IReadOnlyList<NetworkAdapterInfo>>("Network", NetworkProbe.Read);
        var software = Run("Software", () => SoftwareProbe.Read(catalog));
        var system = Run("System", SystemProbe.Read);
        // Null (no battery or no usable driver data) is a valid result, not a probe error.
        var battery = Run<BatteryHealth>("Battery", () => BatteryProbe.Read()!);
        // The extras start with the probes (the startup programs and installed programs alone take a second); their few
        // parts that need a probe's result wait for it. Two time limits: a probe it waits for may use up one of them.
        var extrasTask = Run("Extras", () => ExtrasProbe.Read(catalog, cpu, gpus, firmware, displays, elevation, previous?.Extras), 2 * ProbeTimeLimit);

        await Task.WhenAll(elevation, managed, cpu, gpus, memory, displays, firmware, power, storage, network, software, system, battery, extrasTask).ConfigureAwait(false);

        var gpuList = gpus.Result;
        var extras = extrasTask.Result;
        // The background CPU sample measures other programs for a fixed 3 seconds. It starts once the scan's own work is
        // done (the probes, and the virus scanner checking the files they open, would count as background activity);
        // a rescan after a change keeps the last sample.
        var sample = previous is null ? SampleAfterAsync(sampleAfter) : null;
        PendingBackgroundSample = waitForBackgroundSample ? null : sample;
        var displayList = displays.Result?.Select(d => d with { AdapterName = MatchAdapter(d.AdapterDevicePath, gpuList) }).ToList();

        var profile = new HardwareProfile
        {
            Os = os,
            Elevation = elevation.Result,
            Managed = managed.Result,
            Cpu = cpu.Result,
            Gpus = gpuList,
            Memory = memory.Result,
            Displays = displayList,
            Firmware = firmware.Result,
            Power = power.Result,
            Storage = storage.Result,
            Network = network.Result,
            Software = software.Result,
            System = system.Result,
            Battery = battery.Result,
            Extras = extras,
            ProbeErrors = new Dictionary<string, string>(errors),
        };
        Log.Info("scan", "scan finished", new { ms = sw.ElapsedMilliseconds, errors = errors.Count });
        if (waitForBackgroundSample && sample is not null) profile = WithBackgroundSample(profile, await SampleOrNullAsync(sample).ConfigureAwait(false));
        return profile;
    }

    private static async Task<IReadOnlyList<Tools.ProcessCpu>> SampleAfterAsync(Task? before)
    {
        if (before is not null)
        {
            try
            {
                await before.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Only the start of the sample waits for it.
                Log.Debug("scan", $"work before the CPU sample failed: {ex.Message}");
            }
        }
        return await Task.Run(() => Tools.ProcessSampler.SampleAsync(ExtrasProbe.SampleWindow)).ConfigureAwait(false);
    }

    /// <summary>A failed sample leaves the background activity check without data, like a failed part of the extras.</summary>
    private static async Task<IReadOnlyList<Tools.ProcessCpu>?> SampleOrNullAsync(Task<IReadOnlyList<Tools.ProcessCpu>> sample)
    {
        try
        {
            return await sample.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Warn("scan", $"extras/processes: {ex.Message}");
            return null;
        }
    }

    /// <summary>"\\?\PCI#VEN_10DE&amp;DEV_1F02&amp;...#4&amp;1f822d9d&amp;0&amp;0008#{guid}" ↔ GPU PNPDeviceID.</summary>
    private static string? MatchAdapter(string adapterPath, IReadOnlyList<GpuInfo>? gpus)
    {
        var instance = DisplayProbe.MonitorPathToInstanceId(adapterPath);
        if (instance is null || gpus is null) return null;
        return gpus.FirstOrDefault(g => string.Equals(g.PnpDeviceId, instance, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}
