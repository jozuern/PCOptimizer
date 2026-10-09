using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings.Checks;

/// <summary>F1: display below its maximum offered refresh rate (per active display, rational compare).</summary>
public sealed class RefreshRateCheck : IFindingCheck
{
    public const string Id = "F1.refresh";
    public IReadOnlyList<string> DocIds => [Id];

    /// <summary>
    /// Tolerance for fractional rates: 143.98 Hz on a 144 Hz mode and 59.94 Hz on a 60 Hz mode are OK.
    /// DEVMODE reports whole Hz only, so the offered maximum is an integer.
    /// </summary>
    public static bool IsBelowMax(RefreshRate current, int maxOfferedHz) => maxOfferedHz > 0 && current.Hz < maxOfferedHz - 1.0;

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Displays is null)
        {
            yield return new Finding { Id = Id, Kind = FindingKind.Finding, Status = FindingStatus.Unknown, Impact = 5, Effects = [Effect.Fps, Effect.Latency] };
            yield break;
        }
        foreach (var d in p.Displays)
        {
            var facts = new List<Fact>
            {
                new("fact.display", d.FriendlyName),
                new("fact.resolution", $"{d.Width} × {d.Height}"),
                new("fact.currentRefresh", $"{d.CurrentRefresh.Hz:0.##} Hz"),
                new("fact.maxOfferedRefresh", d.MaxOfferedRefreshAtCurrentResolution > 0 ? $"{d.MaxOfferedRefreshAtCurrentResolution} Hz" : "@unknown"),
                new("fact.connectedTo", d.AdapterName ?? d.AdapterVendor.ToString()),
            };
            if (d.Edid?.MaxVHz is { } edidMax) facts.Add(new Fact("fact.edidMaxRefresh", $"{edidMax} Hz"));

            var status = d.MaxOfferedRefreshAtCurrentResolution == 0 ? FindingStatus.Unknown
                : IsBelowMax(d.CurrentRefresh, d.MaxOfferedRefreshAtCurrentResolution) ? FindingStatus.Problem
                : FindingStatus.Ok;

            yield return new Finding
            {
                Id = Id,
                InstanceKey = d.GdiName,
                Subject = d.FriendlyName,
                Kind = FindingKind.Finding,
                Status = status,
                Impact = 5,
                Effects = [Effect.Fps, Effect.Latency],
                Facts = facts,
                Fix = status == FindingStatus.Problem ? RuntimeFixes.RefreshRate(d) : null,
                Params = new Dictionary<string, string>
                {
                    ["display"] = d.FriendlyName,
                    ["current"] = $"{d.CurrentRefresh.Hz:0.##}",
                    ["max"] = d.MaxOfferedRefreshAtCurrentResolution.ToString(),
                },
            };
        }
    }
}

/// <summary>F3: a monitor is connected to the integrated GPU while a discrete GPU exists (desktops).</summary>
public sealed class MonitorOnIgpuCheck : IFindingCheck
{
    public const string Id = "F3.monitorOnIgpu";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.IsLaptop) yield break; // laptops: F27 (hybrid panel) covers this case
        if (p.Displays is null || p.Gpus is null)
        {
            yield return new Finding { Id = Id, Kind = FindingKind.Finding, Status = FindingStatus.Unknown, Impact = 5, Effects = [Effect.Fps] };
            yield break;
        }
        var discrete = p.Gpus.Where(g => g.Kind == GpuKind.Discrete).ToList();
        if (discrete.Count == 0) yield break; // iGPU-only PC: nothing to fix
        var onIgpu = p.Displays.Where(d =>
                p.Gpus.FirstOrDefault(g => string.Equals(g.Name, d.AdapterName, StringComparison.Ordinal))?.Kind == GpuKind.Integrated)
            .ToList();
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = onIgpu.Count > 0 ? FindingStatus.Problem : FindingStatus.Ok,
            Impact = 5,
            Effects = [Effect.Fps, Effect.Latency],
            Facts =
            [
                new("fact.discreteGpu", string.Join(", ", discrete.Select(g => g.Name))),
                .. p.Displays.Select(d => new Fact("fact.displayAdapter", $"{d.FriendlyName}: {d.AdapterName ?? "?"}")),
            ],
            Params = new Dictionary<string, string>
            {
                ["displays"] = string.Join(", ", onIgpu.Select(d => d.FriendlyName)),
                ["dgpu"] = discrete[0].Name,
            },
        };
    }
}
