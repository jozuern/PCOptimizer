using Microsoft.Win32;

namespace Optimizer.Core.Actions;

/// <summary>Which registry root an action targets. <see cref="User"/> = the signed-in session user's hive (plan v4 §4.5).</summary>
public enum Hive { Machine, User }

/// <summary>Registry roots. Real: HKLM (64-bit view) and HKU\&lt;session SID&gt;. Tests: a sandbox under HKCU.</summary>
public interface IRegistryRoots
{
    /// <summary>Opens <paramref name="path"/> under the hive; returns null if missing and <paramref name="create"/> is false.</summary>
    RegistryKey? Open(Hive hive, string path, bool writable, bool create = false);

    /// <summary>Human-readable prefix for change lists ("HKLM", "HKU\S-1-5-...").</summary>
    string DisplayRoot(Hive hive);
}

public enum ServiceStart { Boot = 0, System = 1, Automatic = 2, Manual = 3, Disabled = 4, AutomaticDelayed = 102 }

public interface IServiceManager
{
    /// <summary>Null when the service does not exist.</summary>
    ServiceStart? GetStartType(string name);

    void SetStartType(string name, ServiceStart start);
}

public interface IPowerManager
{
    Guid ActiveScheme();
    uint? ReadAc(Guid scheme, Guid subgroup, Guid setting);
    uint? ReadDc(Guid scheme, Guid subgroup, Guid setting);
    void WriteAc(Guid scheme, Guid subgroup, Guid setting, uint value);
    void WriteDc(Guid scheme, Guid subgroup, Guid setting, uint value);
    void SetActive(Guid scheme);
    bool SchemeExists(Guid scheme);
    Guid Duplicate(Guid source, string friendlyName);
    void Delete(Guid scheme);
    IReadOnlyList<(Guid Id, string Name)> Schemes();
    void Export(Guid scheme, string file);
}

public interface IBcdStore
{
    /// <summary>Elements present on {current} (identifiers are not localized; values may be).</summary>
    IReadOnlySet<string> CurrentElements();
    void Set(string element, string value);
    void Delete(string element);
    void Export(string file);
}

public interface ITaskScheduler
{
    /// <summary>Null when the task does not exist.</summary>
    bool? IsEnabled(string path);
    void SetEnabled(string path, bool enabled);

    /// <summary>All registered tasks (read-only listing for the startup and task pages).</summary>
    IReadOnlyList<ScheduledTaskInfo> List();
}

/// <summary>A scheduled task: first action's program, author, and whether it starts at logon or at boot.</summary>
public sealed record ScheduledTaskInfo(string Path, bool Enabled, string? Author, string? Command, string? Arguments, bool AtLogon, bool AtBoot, DateTime? LastRun);

public interface IDisplayManager
{
    /// <summary>Current whole-Hz refresh of a display; null if the display is gone.</summary>
    int? CurrentRefresh(string gdiName);
    void SetMode(string gdiName, int width, int height, int refreshHz);
}

public interface IProcessRunner
{
    (int ExitCode, string Output) Run(string file, string arguments, TimeSpan? timeout = null);
}

/// <summary>Everything an action needs to read and write the system. Swappable for tests.</summary>
public sealed class ActionContext
{
    public required IRegistryRoots Registry { get; init; }
    public required IServiceManager Services { get; init; }
    public required IPowerManager Power { get; init; }
    public required IBcdStore Bcd { get; init; }
    public required ITaskScheduler Tasks { get; init; }
    public required IDisplayManager Displays { get; init; }
    public required IProcessRunner Processes { get; init; }
    public required IPowerModeManager PowerMode { get; init; }
    public required IDeviceManager Devices { get; init; }
    public required INetworkManager Network { get; init; }
    public required INvidiaSettings Nvidia { get; init; }

    /// <summary>Called after user-scope changes that need a live refresh (mouse, Explorer settings).</summary>
    public Action<string>? Notify { get; init; }

    /// <summary>Folder for BCD/power exports belonging to the backup store.</summary>
    public string ExportFolder { get; init; } = Path.GetTempPath();

    /// <summary>Interface GUIDs ("{...}") of active Ethernet/Wi-Fi adapters, for per-interface TCP values ("{nic}" in paths).</summary>
    public IReadOnlyList<string> NetworkInterfaceIds { get; init; } = [];
}
