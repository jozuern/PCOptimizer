using System.Security.Principal;

namespace Optimizer.Core.Platform;

/// <summary>
/// The app's data folder (backups, logs, settings, tools). Elevated, as every Release start is:
/// %ProgramData%\PCOptimizer, locked to Administrators and SYSTEM. Not elevated (Debug builds, screenshots):
/// %LocalAppData%\PCOptimizer, because the locked folder cannot even be listed then, and a process without admin
/// rights cannot apply anything anyway.
/// </summary>
public static class DataPaths
{
    public static bool ProcessIsElevated { get; } = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(ProcessIsElevated ? Environment.SpecialFolder.CommonApplicationData : Environment.SpecialFolder.LocalApplicationData),
        "PCOptimizer");

    // Every place inside the data folder, named once (backups live in BackupStore, below Root).
    public static string Logs => Path.Combine(Root, "logs");
    public static string Settings => Path.Combine(Root, "settings.json");
    public static string Tools => Path.Combine(Root, "tools");
    public static string Captures => Path.Combine(Tools, "captures");
    public static string Updates => Path.Combine(Root, "updates");
    public static string Runtime => Path.Combine(Root, "runtime");
}

/// <summary>The running app's version (the version the pre-commit hook raises).</summary>
public static class AppInfo
{
    public static Version Version { get; } = Normalize(System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version);

    /// <summary>"0.4.5": three parts, as release tags use.</summary>
    public static string Text => Version.ToString(3);

    private static Version Normalize(Version? v) => v is null ? new Version(0, 0, 0) : new Version(v.Major, v.Minor, Math.Max(0, v.Build));
}

/// <summary>Sizes as people read them.</summary>
public static class ByteSize
{
    /// <summary>"1.5 GB", "512 MB", "12 KB".</summary>
    public static string Format(long bytes) =>
        bytes >= 1L << 30 ? $"{bytes / (double)(1L << 30):0.#} GB"
        : bytes >= 1L << 20 ? $"{bytes / (double)(1L << 20):0} MB"
        : $"{bytes / 1024.0:0} KB";
}
