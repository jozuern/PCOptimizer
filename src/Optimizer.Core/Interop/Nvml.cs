using System.Runtime.InteropServices;
using System.Text;

namespace Optimizer.Core.Interop;

/// <summary>
/// NVML (nvml.dll, ships with the NVIDIA driver) for read-only telemetry: clocks, temperature, utilization, power,
/// PCIe link and clock-event (throttle) reasons. Signatures and bit values from nvml.h.
/// </summary>
public static class Nvml
{
    private const string Dll = "nvml.dll";

    // nvmlClocksEventReason* bits (nvml.h)
    public const ulong ReasonGpuIdle = 0x1, ReasonAppClocks = 0x2, ReasonSwPowerCap = 0x4, ReasonHwSlowdown = 0x8,
        ReasonSyncBoost = 0x10, ReasonSwThermal = 0x20, ReasonHwThermal = 0x40, ReasonHwPowerBrake = 0x80, ReasonDisplayClock = 0x100;

    [DllImport(Dll)] private static extern int nvmlInit_v2();
    [DllImport(Dll)] private static extern int nvmlShutdown();
    [DllImport(Dll)] private static extern int nvmlDeviceGetCount_v2(out uint count);
    [DllImport(Dll)] private static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);
    [DllImport(Dll)] private static extern int nvmlDeviceGetName(IntPtr device, byte[] name, uint length);
    [DllImport(Dll)] private static extern int nvmlDeviceGetTemperature(IntPtr device, int sensor, out uint temp);
    [DllImport(Dll)] private static extern int nvmlDeviceGetClockInfo(IntPtr device, int type, out uint clock);
    [DllImport(Dll)] private static extern int nvmlDeviceGetUtilizationRates(IntPtr device, out Utilization u);
    [DllImport(Dll)] private static extern int nvmlDeviceGetPowerUsage(IntPtr device, out uint milliwatts);
    [DllImport(Dll)] private static extern int nvmlDeviceGetCurrPcieLinkGeneration(IntPtr device, out uint gen);
    [DllImport(Dll)] private static extern int nvmlDeviceGetCurrPcieLinkWidth(IntPtr device, out uint width);
    [DllImport(Dll)] private static extern int nvmlDeviceGetGpuMaxPcieLinkGeneration(IntPtr device, out uint gen);
    [DllImport(Dll)] private static extern int nvmlDeviceGetCurrentClocksEventReasons(IntPtr device, out ulong reasons);
    [DllImport(Dll)] private static extern int nvmlDeviceGetCurrentClocksThrottleReasons(IntPtr device, out ulong reasons);

    [StructLayout(LayoutKind.Sequential)]
    private struct Utilization { public uint Gpu; public uint Memory; }

    public sealed record Sample(string Name, uint? TempC, uint? GraphicsMhz, uint? UtilizationPct, double? PowerW, uint? PcieGen, uint? PcieWidth, uint? PcieMaxGen, ulong? Reasons);

    public static bool Available => File.Exists(Path.Combine(Environment.SystemDirectory, Dll));

    private static readonly Lock Gate = new();
    private static bool _initialized;

    /// <summary>
    /// NVML is initialized once and stays initialized until the process ends: the sensor and throttle monitors read a
    /// sample every second, and starting the library each time costs far more than the reading.
    /// </summary>
    private static bool EnsureInitialized()
    {
        lock (Gate)
        {
            if (_initialized) return true;
            if (nvmlInit_v2() != 0) return false;
            _initialized = true;
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try
                {
                    nvmlShutdown();
                }
                catch (Exception)
                {
                    // Ending anyway.
                }
            };
            return true;
        }
    }

    /// <summary>One sample per NVIDIA GPU. Returns an empty list when NVML is unavailable.</summary>
    public static IReadOnlyList<Sample> Read()
    {
        var list = new List<Sample>();
        if (!Available) return list;
        try
        {
            if (!EnsureInitialized()) return list;
            lock (Gate)
            {
                if (nvmlDeviceGetCount_v2(out var count) != 0) return list;
                for (uint i = 0; i < count; i++)
                {
                    if (nvmlDeviceGetHandleByIndex_v2(i, out var d) != 0) continue;
                    var name = new byte[96];
                    var n = nvmlDeviceGetName(d, name, (uint)name.Length) == 0 ? Encoding.ASCII.GetString(name).TrimEnd('\0') : $"GPU {i}";
                    uint? U(Func<(int, uint)> f) { var (r, v) = f(); return r == 0 ? v : null; }
                    var temp = U(() => (nvmlDeviceGetTemperature(d, 0, out var t), t));
                    var clock = U(() => (nvmlDeviceGetClockInfo(d, 0, out var c), c));
                    var util = nvmlDeviceGetUtilizationRates(d, out var u) == 0 ? u.Gpu : (uint?)null;
                    var power = nvmlDeviceGetPowerUsage(d, out var mw) == 0 ? mw / 1000.0 : (double?)null;
                    var gen = U(() => (nvmlDeviceGetCurrPcieLinkGeneration(d, out var g), g));
                    var width = U(() => (nvmlDeviceGetCurrPcieLinkWidth(d, out var w), w));
                    var maxGen = U(() => (nvmlDeviceGetGpuMaxPcieLinkGeneration(d, out var mg), mg));
                    ulong? reasons = null;
                    try
                    {
                        if (nvmlDeviceGetCurrentClocksEventReasons(d, out var r) == 0) reasons = r;
                    }
                    catch (EntryPointNotFoundException)
                    {
                        // Older drivers only export the deprecated name.
                        if (nvmlDeviceGetCurrentClocksThrottleReasons(d, out var r) == 0) reasons = r;
                    }
                    list.Add(new Sample(n, temp, clock, util, power, gen, width, maxGen, reasons));
                }
            }
        }
        catch (Exception)
        {
            // DLL load or entry point problems: no NVML telemetry.
        }
        return list;
    }

    /// <summary>Throttle reasons that cost performance under load (idle, app clocks, sync boost and display clock excluded).</summary>
    public static IReadOnlyList<string> LimitingReasons(ulong reasons)
    {
        var list = new List<string>();
        if ((reasons & ReasonSwPowerCap) != 0) list.Add("powerLimit");
        if ((reasons & (ReasonSwThermal | ReasonHwThermal)) != 0) list.Add("thermal");
        if ((reasons & ReasonHwPowerBrake) != 0) list.Add("powerBrake");
        if ((reasons & ReasonHwSlowdown) != 0 && (reasons & (ReasonHwThermal | ReasonHwPowerBrake)) == 0) list.Add("hardwareSlowdown");
        return list;
    }
}
