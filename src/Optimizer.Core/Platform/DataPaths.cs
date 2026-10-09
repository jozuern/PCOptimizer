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
}
