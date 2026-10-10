using Optimizer.Core.Interop;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Hardware;

/// <summary>Additional facts for advisor checks. Every field is optional: null/empty means "could not read".</summary>
public sealed record HardwareExtras
{
    public string? TpmManufacturer { get; init; }
    public Version? Agesa { get; init; }

    /// <summary>The SMBIOS string the AGESA version was read from (e.g. "AGESA ComboAM4v2PI 1.2.0.7").</summary>
    public string? AgesaSource { get; init; }

    /// <summary>TRIM setting for NTFS (F18); null when the registry could not be read.</summary>
    public TrimSetting? Trim { get; init; }
    public SecureBootCerts? SecureBootCerts { get; init; }
    public Guid? PowerOverlay { get; init; }
    public IReadOnlyList<NicDetail> Nics { get; init; } = [];
    public IReadOnlyList<Wlan.Connection> Wifi { get; init; } = [];
    public IReadOnlyList<NvmeLink> NvmeLinks { get; init; } = [];
    public IReadOnlyList<string> RunningOverlays { get; init; } = [];
    public IReadOnlySet<string> ServicesPresent { get; init; } = new HashSet<string>();
    public NvidiaInfo? Nvidia { get; init; }
    public IReadOnlyList<string> PrintersInstalled { get; init; } = [];

    /// <summary>Display names of installed programs (Uninstall keys), for chipset and tool checks.</summary>
    public IReadOnlyList<Probes.InstalledProgram> Programs { get; init; } = [];

    /// <summary>Graphics cards and network adapters with their interrupt capabilities (MSI mode tweak).</summary>
    public IReadOnlyList<MsiDevice> MsiDevices { get; init; } = [];

    /// <summary>Third-party programs enabled at logon (Run keys, Startup folders, logon tasks), for F16. Null = not read.</summary>
    public IReadOnlyList<string>? StartupPrograms { get; init; }

    /// <summary>Hardware virtualization state (A.virtualization); null when it could not be read.</summary>
    public Probes.VirtualizationInfo? Virtualization { get; init; }

    /// <summary>Unexpected shutdowns of the last 30 days (Kernel-Power event 41), newest first; null when the System log could not be read.</summary>
    public IReadOnlyList<Probes.UnexpectedShutdown>? UnexpectedShutdowns { get; init; }

    /// <summary>Drive health (F28); empty when Storage Management could not be read.</summary>
    public IReadOnlyList<Tools.DiskHealth> DiskHealth { get; init; } = [];

    /// <summary>CPU use per process over a 3 second window during the scan (F11). Null = not sampled.</summary>
    public IReadOnlyList<Tools.ProcessCpu>? BackgroundCpu { get; init; }

    /// <summary>Last throttle check under load (F23), saved by the Health page. Null = never measured.</summary>
    public Tools.ThrottleResult? LastThrottle { get; init; }
}

/// <summary>A physical network adapter with its driver keywords (class key under {4d36e972-e325-11ce-bfc1-08002be10318}).</summary>
public sealed record NicDetail(
    string Id,
    string Name,
    string Description,
    string Type,
    bool IsUp,
    long SpeedBps,
    int? MaxSpeedMbps,
    string? ClassKey,
    IReadOnlyDictionary<string, string> Keywords,
    IReadOnlyDictionary<string, IReadOnlyList<string>> AllowedValues)
{
    /// <summary>PnP instance id (PCI\... or USB\...) from the class key.</summary>
    public string? DeviceInstanceId { get; init; }

    /// <summary>
    /// A real network card (PCI or USB device). Virtual miniports also have class keys and report themselves as Ethernet:
    /// Hyper-V vEthernet (ROOT\VMS_MP), WAN Miniport (SWD\MSRRAS), Wintun (SWD\Wintun), Kernel Debug NIC (ROOT\KDNIC).
    /// </summary>
    public bool IsPhysical => DeviceInstanceId is { } id &&
                              (id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase) || id.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// HKLM\SYSTEM\CurrentControlSet\Control\FileSystem DisableDeleteNotification (NTFS). Null = value not set, which means
/// TRIM is on: "For systems using NTFS, trim is enabled by default unless an administrator disables it" (fsutil behavior).
/// </summary>
public sealed record TrimSetting(int? DisableDeleteNotification)
{
    public bool TrimOn => DisableDeleteNotification != 1;
}

/// <summary>
/// A PCI device that can use message-signaled interrupts: DEVPKEY_PciDevice_InterruptSupport bit 1 = MSI, bit 2 = MSI-X.
/// <see cref="MsiSupportedValue"/> is the registry override (null = not set, the driver's INF decides).
/// </summary>
public sealed record MsiDevice(string InstanceId, string Name, string Kind, uint? InterruptSupport, uint? MessageMaximum, uint? MsiSupportedValue)
{
    public bool SupportsMsi => InterruptSupport is { } s && (s & 0x6) != 0;
}

/// <summary>PCIe link of an NVMe controller and of the port above it.</summary>
public sealed record NvmeLink(string Disk, string PnpId, PcieLink? Link, PcieLink? Port);

/// <summary>NVIDIA driver state (global profile values and per-display VRR), read through NVAPI.</summary>
public sealed record NvidiaInfo(
    string? DriverVersion,
    uint? FrameRateLimit,
    uint? VSyncMode,
    uint? PowerMode,
    uint? BatteryBoostFps,
    uint? GsyncGlobalMode,
    IReadOnlyDictionary<string, NvidiaVrr> Vrr);

/// <summary>Enabled = G-SYNC turned on for this display; Possible = display and connection support it.</summary>
public sealed record NvidiaVrr(bool Enabled, bool Possible, bool Requested, bool InVrrMode);
