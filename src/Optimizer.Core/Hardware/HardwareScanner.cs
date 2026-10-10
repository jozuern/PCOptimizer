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

    /// <param name="previous">The last result, after a change made by this app: slow parts no change can affect are reused.</param>
    /// <param name="waitForBackgroundSample">
    /// False: return as soon as everything else is read and leave the 3 second CPU sample in <see cref="PendingBackgroundSample"/>.
    /// </param>
    public async Task<HardwareProfile> ScanAsync(IProgress<string>? progress = null, CancellationToken ct = default, HardwareProfile? previous = null,
        bool waitForBackgroundSample = true)
    {
        var errors = new ConcurrentDictionary<string, string>();
        var sw = Stopwatch.StartNew();

        async Task<T?> Run<T>(string name, Func<T> probe) where T : class
        {
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
                return await work.WaitAsync(ProbeTimeLimit, ct);
            }
            catch (TimeoutException)
            {
                errors[name] = $"no answer within {ProbeTimeLimit.TotalSeconds:0} s";
                Log.Error("scan", $"{name} timed out");
                return null;
            }
        }

        if (previous is null) SystemTaskScheduler.ForgetList();
        // The background CPU sample takes a fixed 3 seconds of measuring: started first, it overlaps every other probe.
        var processSample = previous is null ? Task.Run(() => Tools.ProcessSampler.SampleAsync(ExtrasProbe.SampleWindow)) : null;

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

        await Task.WhenAll(elevation, managed, cpu, gpus, memory, displays, firmware, power, storage, network, software, system, battery);

        var gpuList = gpus.Result;
        var extras = await Run("Extras", () => ExtrasProbe.Read(catalog, cpu.Result, gpuList, firmware.Result, displays.Result, elevation.Result?.IsElevated == true,
            elevation.Result?.SessionUserSid ?? elevation.Result?.ProcessUserSid, previous?.Extras, processSample, waitForBackgroundSample));
        PendingBackgroundSample = processSample is { IsCompleted: false } && !waitForBackgroundSample ? processSample : null;
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
        return profile;
    }

    /// <summary>"\\?\PCI#VEN_10DE&amp;DEV_1F02&amp;...#4&amp;1f822d9d&amp;0&amp;0008#{guid}" ↔ GPU PNPDeviceID.</summary>
    private static string? MatchAdapter(string adapterPath, IReadOnlyList<GpuInfo>? gpus)
    {
        var instance = DisplayProbe.MonitorPathToInstanceId(adapterPath);
        if (instance is null || gpus is null) return null;
        return gpus.FirstOrDefault(g => string.Equals(g.PnpDeviceId, instance, StringComparison.OrdinalIgnoreCase))?.Name;
    }
}
