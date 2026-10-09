namespace Optimizer.Core.Hardware.Probes;

/// <summary>
/// Battery capacity from the battery class driver's WMI data (root\WMI): BatteryStaticData.DesignedCapacity (when new)
/// and BatteryFullChargedCapacity.FullChargedCapacity (now), both in mWh. Several batteries are added up. Null when there
/// is no battery or the driver reports no usable values (relative units or zero).
/// </summary>
public static class BatteryProbe
{
    public static BatteryHealth? Read()
    {
        if (!PowerProbe.HasBattery()) return null;
        var designed = Wmi.Query("SELECT DesignedCapacity FROM BatteryStaticData", @"root\WMI").Sum(r => r.Long("DesignedCapacity") ?? 0);
        var full = Wmi.Query("SELECT FullChargedCapacity FROM BatteryFullChargedCapacity", @"root\WMI").Sum(r => r.Long("FullChargedCapacity") ?? 0);
        return BatteryHealth.From(designed, full);
    }
}

/// <param name="DesignedMwh">Capacity when new.</param>
/// <param name="FullChargedMwh">Capacity of a full charge today.</param>
public sealed record BatteryHealth(long DesignedMwh, long FullChargedMwh)
{
    /// <summary>Microsoft's rule of thumb: a battery is at its end of life below 80 % of its design capacity.</summary>
    public const double WornBelow = 0.8;

    /// <summary>Full charge relative to design (0.0 to about 1.05; new batteries can be slightly above 100 %).</summary>
    public double Health => (double)FullChargedMwh / DesignedMwh;

    public bool IsWorn => Health < WornBelow;

    /// <summary>Null for missing or implausible values (zero, or a full charge far above the design capacity).</summary>
    public static BatteryHealth? From(long designedMwh, long fullChargedMwh) =>
        designedMwh <= 0 || fullChargedMwh <= 0 || fullChargedMwh > designedMwh * 3 / 2 ? null : new BatteryHealth(designedMwh, fullChargedMwh);
}
