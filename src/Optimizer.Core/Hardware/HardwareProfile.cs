using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware;

public enum Vendor { Unknown, Intel, Amd, Nvidia, Microsoft, Qualcomm, Other }

/// <summary>Everything the scanner collected. Each section is null when its probe failed (-> findings report Unknown).</summary>
public sealed record HardwareProfile
{
    public required BuildInfo Os { get; init; }
    public ElevationInfo? Elevation { get; init; }
    public ManagedDeviceInfo? Managed { get; init; }
    public CpuInfo? Cpu { get; init; }
    public IReadOnlyList<GpuInfo>? Gpus { get; init; }
    public MemoryInfo? Memory { get; init; }
    public IReadOnlyList<DisplayInfo>? Displays { get; init; }
    public FirmwareInfo? Firmware { get; init; }
    public PowerInfo? Power { get; init; }
    public StorageInfo? Storage { get; init; }
    public IReadOnlyList<NetworkAdapterInfo>? Network { get; init; }
    public SoftwareInfo? Software { get; init; }
    public SystemInfo? System { get; init; }

    /// <summary>Data for the M3/M4 checks (NIC capabilities, Wi-Fi band, NVMe links, Secure Boot certificates, NVIDIA state).</summary>
    public HardwareExtras? Extras { get; init; }

    /// <summary>Probe name -> error message, for the Hardware page and the log.</summary>
    public IReadOnlyDictionary<string, string> ProbeErrors { get; init; } = new Dictionary<string, string>();

    public bool IsLaptop => System?.IsLaptop ?? false;
}

public sealed record SystemInfo(string Manufacturer, string Model, IReadOnlyList<int> ChassisTypes, bool HasBattery, bool LidPresent, bool HypervisorPresent)
{
    private static readonly HashSet<int> LaptopChassis = [8, 9, 10, 11, 14, 30, 31, 32];
    public bool IsLaptop => HasBattery && (LidPresent || ChassisTypes.Any(LaptopChassis.Contains));
}

// ---------------- CPU ----------------
public sealed record CacheDomain(int Level, long SizeBytes, ulong Mask, int Group);

public sealed record CpuInfo(
    string Name,
    Vendor Vendor,
    int Family,
    int Model,
    int Stepping,
    int Cores,
    int Threads,
    int MaxClockMhz,
    string Socket,
    uint? MicrocodeCurrent,
    uint? MicrocodeBios,
    string MicrocodeSource,
    IReadOnlyList<CacheDomain> L3Domains,
    IReadOnlyDictionary<int, int> CoresByEfficiencyClass)
{
    /// <summary>Hybrid = more than one efficiency class (plan v4: via EfficiencyClass, not by model).</summary>
    public bool IsHybrid => CoresByEfficiencyClass.Count > 1;

    public int PerformanceCores => CoresByEfficiencyClass.Count == 0 ? Cores : CoresByEfficiencyClass[CoresByEfficiencyClass.Keys.Max()];
}

// ---------------- GPU ----------------
public enum GpuKind { Discrete, Integrated, Basic, Virtual, Unknown }

public enum TriState { Unknown, No, Yes }

public sealed record PcieLink(int? CurrentGen, int? CurrentWidth, int? MaxGen, int? MaxWidth)
{
    public static int? SpeedCodeToGen(uint? code) => code is >= 1 and <= 7 ? (int)code : null;
}

public sealed record GpuInfo(
    string Name,
    Vendor Vendor,
    string PnpDeviceId,
    string? DriverVersion,
    DateTime? DriverDate,
    string? InfFile,
    string? DriverProvider,
    long? VramBytes,
    GpuKind Kind,
    long? LargestBarBytes,
    PcieLink? CardLink,
    PcieLink? PlatformPortLink,
    int SwitchHopsSkipped,
    TriState HagsEnabled)
{
    public string? NvidiaDriverVersion => Vendor == Vendor.Nvidia ? NvidiaVersion(DriverVersion) : null;

    /// <summary>Windows driver version -> NVIDIA version: 32.0.15.6094 -> 560.94 (last five digits).</summary>
    public static string? NvidiaVersion(string? windowsVersion)
    {
        if (string.IsNullOrEmpty(windowsVersion)) return null;
        var parts = windowsVersion.Split('.');
        if (parts.Length < 4) return null;
        var digits = parts[2] + parts[3].PadLeft(4, '0');
        if (digits.Length < 5) return null;
        var last5 = digits[^5..];
        return $"{int.Parse(last5[..3])}.{last5[3..]}";
    }
}

// ---------------- Memory ----------------
public sealed record MemoryModule(
    long CapacityBytes,
    int? SpeedMts,
    int? ConfiguredMts,
    string PartNumber,
    string Manufacturer,
    string DeviceLocator,
    string BankLabel,
    int SmbiosMemoryType,
    int FormFactor)
{
    public string Type => SmbiosMemoryType switch
    {
        26 => "DDR4",
        34 => "DDR5",
        24 => "DDR3",
        35 => "LPDDR5",
        30 => "LPDDR4",
        _ => "Unknown",
    };
}

public sealed record MemoryInfo(long TotalBytes, IReadOnlyList<MemoryModule> Modules);

// ---------------- Displays ----------------
public sealed record RefreshRate(uint Numerator, uint Denominator)
{
    public double Hz => Denominator == 0 ? 0 : (double)Numerator / Denominator;
    public override string ToString() => $"{Hz:0.##} Hz";
}

public sealed record DisplayMode(int Width, int Height, int RefreshHz, int BitsPerPixel);

public sealed record DisplayInfo(
    string GdiName,
    string FriendlyName,
    string MonitorDevicePath,
    bool IsInternal,
    uint OutputTechnology,
    int Width,
    int Height,
    RefreshRate CurrentRefresh,
    int MaxOfferedRefreshAtCurrentResolution,
    IReadOnlyList<DisplayMode> Modes,
    string AdapterDevicePath,
    Vendor AdapterVendor,
    string? AdapterName,
    bool HdrSupported,
    bool HdrEnabled,
    EdidInfo? Edid);

public sealed record EdidInfo(string ManufacturerId, ushort ProductCode, string? Name, int? MinVHz, int? MaxVHz, int? PreferredWidth, int? PreferredHeight, double? PreferredRefreshHz)
{
    /// <summary>EDID 1.4 feature byte bit 0: the display accepts a continuous range of frequencies (a hint for VRR panels).</summary>
    public bool ContinuousFrequency { get; init; }

    /// <summary>A refresh range wide enough for variable refresh (e.g. 48–144 Hz). Hint only; the GPU driver decides.</summary>
    public bool LooksVrrCapable => ContinuousFrequency && MinVHz is <= 60 && MaxVHz is >= 90 && MaxVHz >= MinVHz * 1.5;
}

// ---------------- Firmware & security ----------------
public enum PartitionStyle { Unknown, Mbr, Gpt }

public sealed record FirmwareInfo(
    bool IsUefi,
    TriState SecureBoot,
    TriState TpmPresent,
    string? TpmSpecVersion,
    TriState TpmReady,
    int VbsStatus,
    bool HvciRunning,
    bool CredentialGuardRunning,
    bool MbecAvailable,
    bool DmaProtectionAvailable,
    string BiosVendor,
    string BiosVersion,
    DateTime? BiosDate,
    string BoardManufacturer,
    string BoardProduct,
    PartitionStyle SystemDiskPartitionStyle)
{
    public bool VbsRunning => VbsStatus == 2;
}

// ---------------- Power ----------------
public enum PowerPersonality { Unknown, PowerSaver, Balanced, HighPerformance }

public sealed record PowerInfo(
    Guid ActiveScheme,
    string ActiveSchemeName,
    PowerPersonality Personality,
    uint? MaxProcessorStateAc,
    uint? MinProcessorStateAc,
    uint? BoostModeAc,
    uint? CoreParkingMinCoresAc,
    bool OnAc,
    bool EnergySaverOn,
    bool ModernStandby,
    int? BatteryPercent);

// ---------------- Storage ----------------
public sealed record PhysicalDisk(int Number, string FriendlyName, string MediaType, string BusType, long SizeBytes, string Health, bool IsSmrSuspect);

public sealed record Volume(string Root, string Label, long SizeBytes, long FreeBytes, int? DiskNumber, bool IsSystem)
{
    public double FreeFraction => SizeBytes == 0 ? 1 : (double)FreeBytes / SizeBytes;
}

public sealed record StorageInfo(IReadOnlyList<PhysicalDisk> Disks, IReadOnlyList<Volume> Volumes);

// ---------------- Network ----------------
public sealed record NetworkAdapterInfo(string Name, string Description, string Type, long SpeedBps, bool IsUp);

// ---------------- Software ----------------
public sealed record AntiCheatPresence(string Id, string DisplayName, IReadOnlyList<string> FoundServices);

public sealed record InstalledGame(string Name, string Launcher, string InstallDir, string? Executable);

public sealed record SoftwareInfo(IReadOnlyList<AntiCheatPresence> AntiCheats, IReadOnlyList<string> Launchers, IReadOnlyList<string> GameLibraryPaths)
{
    public IReadOnlyList<InstalledGame> Games { get; init; } = [];
}
