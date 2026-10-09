using Optimizer.Core.Catalog;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Findings;

/// <summary>Runs all read-only checks. A failing check yields an Unknown result instead of breaking the scan.</summary>
public sealed class FindingEngine(CatalogData catalog, Actions.IRegistryRoots? registry = null)
{
    /// <summary>All checks; <paramref name="registry"/> gives access to the session user's hive (F4).</summary>
    public static IReadOnlyList<IFindingCheck> CreateChecks(Actions.IRegistryRoots? registry) =>
    [
        new RefreshRateCheck(),
        new EdidRefreshCheck(),
        new VrrCheck(),
        new NvidiaGlobalCheck(),
        new PowerModeCheck(),
        new OverlaysCheck(),
        new AmdChipsetCheck(),
        new SecureBootCertsCheck(),
        new EthernetSpeedCheck(),
        new WifiBandCheck(),
        new NvmeLinkCheck(),
        new LaptopPanelCheck(),
        new ApoCheck(),
        new AmdFtpmCheck(),
        new RyzenMemoryCheck(),
        new BiosAgeCheck(),
        new IgpuUnusedCheck(),
        new SystemOnHddCheck(),
        new AmdAdrenalinCheck(),
        new StartupCountCheck(),
        new ThrottleCheck(),
        new DiskHealthCheck(),
        new BackgroundCpuCheck(),
        new GpuPreferenceCheck(registry),
        new LeftoverCheck(),
        new MonitorOnIgpuCheck(),
        new BasicDisplayAdapterCheck(),
        new TurboDisabledCheck(),
        new X3dCheck(),
        new PowerSaverCheck(),
        new EnergySaverCheck(),
        new LowRamCheck(),
        new GamesOnHddCheck(),
        new GpuDriverAgeCheck(),
        new LowDiskSpaceCheck(),
        new TrimCheck(),
        new XmpCheck(),
        new DualChannelCheck(),
        new MixedMemoryCheck(),
        new VirtualizationCheck(),
        new UnexpectedRestartCheck(),
        new RebarCheck(),
        new PcieLinkCheck(),
        new MicrocodeCheck(),
        new OnBatteryCheck(),
        new BatteryWearCheck(),
        new GameAccessCheck(),
    ];

    public IReadOnlyList<Finding> Evaluate(HardwareProfile profile)
    {
        var results = new List<Finding>();
        foreach (var check in CreateChecks(registry))
        {
            try
            {
                results.AddRange(check.Evaluate(profile, catalog));
            }
            catch (Exception ex)
            {
                Log.Error("findings", $"{check.GetType().Name} failed", ex);
                results.Add(new Finding { Id = check.DocIds.FirstOrDefault() ?? check.GetType().Name, Kind = FindingKind.Finding, Status = FindingStatus.Unknown });
            }
        }
        return Sort(results);
    }

    /// <summary>Problems first (critical first, then by ⚡), then Unknown, Info, Unsupported, OK.</summary>
    public static IReadOnlyList<Finding> Sort(IEnumerable<Finding> findings) =>
        findings
            .OrderBy(f => f.Status switch
            {
                FindingStatus.Problem => 0,
                FindingStatus.Unknown => 1,
                FindingStatus.Info => 2,
                FindingStatus.Unsupported => 3,
                _ => 4,
            })
            .ThenByDescending(f => f.Critical)
            .ThenByDescending(f => f.Impact ?? -1)
            .ThenBy(f => f.Id, StringComparer.Ordinal)
            .ToList();
}

/// <summary>
/// Gaming Readiness score (plan v4 §4.11): computed only from findings, weighted by ⚡. Tweaks never add points
/// and Game access / security items never count.
/// </summary>
public static class ReadinessScore
{
    public static int Weight(int impact) => impact switch { >= 5 => 20, 4 => 12, 3 => 7, 2 => 3, 1 => 1, _ => 0 };

    public static int Compute(IEnumerable<Finding> findings)
    {
        var penalty = findings
            .Where(f => f.Kind != FindingKind.GameAccess && f.Status == FindingStatus.Problem)
            .Sum(f => f.Critical ? 20 : Weight(f.Impact ?? 0));
        return Math.Clamp(100 - penalty, 0, 100);
    }
}
