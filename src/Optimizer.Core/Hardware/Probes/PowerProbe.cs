using System.Runtime.InteropServices;
using System.Text;
using Optimizer.Core.Interop;

namespace Optimizer.Core.Hardware.Probes;

/// <summary>Power state via powrprof (GUIDs and aliases only, never localized names: plan v4 §4.9).</summary>
public static class PowerProbe
{
    public static readonly Guid SubProcessor = new("54533251-82be-4824-96c1-47b60b740d00");
    public static readonly Guid ProcThrottleMax = new("bc5038f7-23e0-4960-96da-33abaf5935ec");
    public static readonly Guid ProcThrottleMin = new("893dee8e-2bef-41e0-89c6-b55d0929964c");
    public static readonly Guid PerfBoostMode = new("be337238-0d82-4146-a960-4f3749d470c7");
    public static readonly Guid CpMinCores = new("0cc5b647-c1df-4637-891a-dec35c318583");
    public static readonly Guid NoSubgroup = new("fea3413e-7e05-4911-9a71-700331f1c294");
    public static readonly Guid SchemePersonality = new("245d8541-3943-4422-b025-13a784f679b7");

    public static readonly Guid PersonalityPowerSaver = new("a1841308-3541-4fab-bc81-f71556f20b4a");
    public static readonly Guid PersonalityBalanced = new("381b4222-f694-41f0-9685-ff5bb260df2e");
    public static readonly Guid PersonalityHighPerformance = new("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c");

    public static PowerInfo Read()
    {
        Guid scheme = Guid.Empty;
        if (Native.PowerGetActiveScheme(IntPtr.Zero, out var ptr) == 0)
        {
            scheme = Marshal.PtrToStructure<Guid>(ptr);
            Native.LocalFree(ptr);
        }

        Native.GetSystemPowerStatus(out var status);
        var capabilities = new byte[128];
        var modernStandby = Native.CallNtPowerInformation(Native.SystemPowerCapabilities, IntPtr.Zero, 0, capabilities, (uint)capabilities.Length) == 0
                            && capabilities[20] != 0; // SYSTEM_POWER_CAPABILITIES.AoAc

        return new PowerInfo(
            scheme,
            FriendlyName(scheme),
            Personality(scheme),
            ReadAc(scheme, SubProcessor, ProcThrottleMax),
            ReadAc(scheme, SubProcessor, ProcThrottleMin),
            ReadAc(scheme, SubProcessor, PerfBoostMode),
            ReadAc(scheme, SubProcessor, CpMinCores),
            status.ACLineStatus == 1,
            (status.SystemStatusFlag & 1) != 0,
            modernStandby,
            status.BatteryFlag == 128 || status.BatteryLifePercent == 255 ? null : status.BatteryLifePercent);
    }

    public static bool HasLid()
    {
        var capabilities = new byte[128];
        return Native.CallNtPowerInformation(Native.SystemPowerCapabilities, IntPtr.Zero, 0, capabilities, (uint)capabilities.Length) == 0
               && capabilities[2] != 0; // LidPresent
    }

    public static bool HasBattery() => Native.GetSystemPowerStatus(out var s) && s.BatteryFlag != 128 && s.BatteryFlag != 255;

    private static uint? ReadAc(Guid scheme, Guid sub, Guid setting) =>
        scheme != Guid.Empty && Native.PowerReadACValueIndex(IntPtr.Zero, scheme, sub, setting, out var v) == 0 ? v : null;

    private static string FriendlyName(Guid scheme)
    {
        if (scheme == Guid.Empty) return "";
        uint size = 0;
        Native.PowerReadFriendlyName(IntPtr.Zero, scheme, IntPtr.Zero, IntPtr.Zero, null, ref size);
        if (size == 0) return "";
        var buffer = new byte[size];
        return Native.PowerReadFriendlyName(IntPtr.Zero, scheme, IntPtr.Zero, IntPtr.Zero, buffer, ref size) == 0
            ? Encoding.Unicode.GetString(buffer).TrimEnd('\0')
            : "";
    }

    /// <summary>Base personality of the scheme (custom plans inherit one), so a renamed copy of Power saver is still detected.</summary>
    private static PowerPersonality Personality(Guid scheme)
    {
        if (scheme == PersonalityPowerSaver) return PowerPersonality.PowerSaver;
        if (scheme == PersonalityBalanced) return PowerPersonality.Balanced;
        if (scheme == PersonalityHighPerformance) return PowerPersonality.HighPerformance;
        uint size = 16;
        var buffer = new byte[16];
        if (Native.PowerReadACValue(IntPtr.Zero, scheme, NoSubgroup, SchemePersonality, out _, buffer, ref size) != 0 || size < 16)
            return PowerPersonality.Unknown;
        var p = new Guid(buffer);
        if (p == PersonalityPowerSaver) return PowerPersonality.PowerSaver;
        if (p == PersonalityBalanced) return PowerPersonality.Balanced;
        if (p == PersonalityHighPerformance) return PowerPersonality.HighPerformance;
        return PowerPersonality.Unknown;
    }
}
