using LibreHardwareMonitor.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tools;

public sealed record SensorReading(string Hardware, string HardwareType, string Sensor, string Type, float? Value, float? Max);

/// <summary>
/// Opt-in sensor readings through LibreHardwareMonitorLib 0.9.6 (MPL-2.0). Graphics cards and drives work without
/// extra drivers; processor and mainboard sensors need the signed PawnIO driver, which the app installs only when
/// you choose to (winget namazso.PawnIO). Opened only while the Health page shows sensors.
/// </summary>
public sealed class Sensors : IDisposable
{
    private readonly Computer _computer;

    public Sensors()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = true,
            IsMemoryEnabled = true,
            IsControllerEnabled = false,
            IsNetworkEnabled = false,
            IsPsuEnabled = false,
            IsBatteryEnabled = true,
        };
        _computer.Open();
    }

    /// <summary>The PawnIO driver service is registered (CPU and mainboard sensors available).</summary>
    public static bool PawnIoInstalled => Reg.HklmKeyExists(@"SYSTEM\CurrentControlSet\Services\PawnIO");

    public IReadOnlyList<SensorReading> Read()
    {
        var list = new List<SensorReading>();
        foreach (var hw in _computer.Hardware) Collect(hw, list);
        return list;
    }

    private static void Collect(IHardware hw, List<SensorReading> list)
    {
        try
        {
            hw.Update();
        }
        catch (Exception ex)
        {
            Log.Warn("sensors", $"{hw.Name}: {ex.Message}");
            return;
        }
        foreach (var s in hw.Sensors.Where(s => s.SensorType is SensorType.Temperature or SensorType.Clock or SensorType.Load or SensorType.Power or SensorType.Fan or SensorType.Voltage))
            list.Add(new SensorReading(hw.Name, hw.HardwareType.ToString(), s.Name, s.SensorType.ToString(), s.Value, s.Max));
        foreach (var sub in hw.SubHardware) Collect(sub, list);
    }

    public void Dispose() => _computer.Close();
}
