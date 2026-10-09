using System.Globalization;
using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings.Checks;

/// <summary>
/// F29: a gaming laptop running on battery. On battery, Windows and the GPU driver cap the graphics card and processor
/// (and many laptops lock their performance mode), so games run far slower than on the power adapter.
/// </summary>
public sealed class OnBatteryCheck : IFindingCheck
{
    public const string Id = "F29.onBattery";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (!p.IsLaptop || p.Power is null) yield break;
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = p.Power.OnAc ? FindingStatus.Ok : FindingStatus.Problem,
            Impact = 4,
            Effects = [Effect.Fps, Effect.Lows],
            Facts =
            [
                new("fact.acPower", p.Power.OnAc ? "@yes" : "@no"),
                new("fact.batteryLevel", p.Power.BatteryPercent is { } pct ? $"{pct} %" : "@unknown"),
            ],
        };
    }
}

/// <summary>F30: battery capacity below 80 % of its design capacity (Microsoft's end-of-life rule of thumb).</summary>
public sealed class BatteryWearCheck : IFindingCheck
{
    public const string Id = "F30.batteryWear";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (!p.IsLaptop) yield break;
        if (p.Battery is not { } b)
        {
            // A laptop whose driver gives no capacity data: unknown, never advice.
            yield return new Finding { Id = Id, Kind = FindingKind.Finding, Status = FindingStatus.Unknown, Impact = 0 };
            yield break;
        }
        var health = (int)Math.Round(b.Health * 100);
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = b.IsWorn ? FindingStatus.Problem : FindingStatus.Ok,
            Impact = 0, // matters for the battery profiles; the profiles set its weight
            Effects = [],
            Facts =
            [
                new("fact.batteryDesign", Mwh(b.DesignedMwh)),
                new("fact.batteryFull", Mwh(b.FullChargedMwh)),
                new("fact.batteryHealth", $"{health} %"),
            ],
            Params = new Dictionary<string, string> { ["health"] = health.ToString(CultureInfo.InvariantCulture) },
        };
    }

    private static string Mwh(long mwh) => $"{mwh / 1000.0:0.0} Wh".Replace(',', '.');
}
