using System.Diagnostics;

namespace Optimizer.Core.Platform;

/// <summary>
/// Start settings for every tool the elevated app runs. The child inherits the environment that the signed-in user
/// controls (HKCU\Environment), so variables that make a tool load code from elsewhere are removed: PowerShell
/// modules (PSModulePath starts with the user's Documents folder, and commands load their module on first use),
/// .NET Framework profilers (COR_PROFILER and friends load a DLL into powershell.exe) and runtime switches. The
/// working directory is System32 instead of the exe's folder, which is often Downloads.
/// </summary>
public static class ProcessHardening
{
    /// <summary>Only Windows' own modules (PowerShell adds the admin-only Program Files modules folder itself).</summary>
    public static string PowerShellModules => Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\Modules");

    private static readonly string[] RemovedVariables = ["COR_ENABLE_PROFILING", "COR_PROFILER", "COR_PROFILER_PATH", "PSExecutionPolicyPreference"];
    private static readonly string[] RemovedPrefixes = ["COMPlus_", "DOTNET_", "CORECLR_"];

    /// <summary>A plain tool name resolves to System32; powershell.exe to Windows PowerShell 5.1 in System32.</summary>
    public static string ResolveSystemTool(string file)
    {
        if (file.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
            return Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        return Path.IsPathRooted(file) ? file : Path.Combine(Environment.SystemDirectory, file);
    }

    public static void Apply(ProcessStartInfo psi)
    {
        psi.WorkingDirectory = Environment.SystemDirectory;
        var env = psi.Environment;
        foreach (var name in env.Keys.ToList())
        {
            if (RemovedVariables.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                RemovedPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                env.Remove(name);
        }
        env["PSModulePath"] = PowerShellModules;
    }

    /// <summary>Ends the process and its children; a process that already exited or cannot be ended is left alone.</summary>
    public static void KillTree(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException or AggregateException)
        {
            Logging.Log.Warn("command", $"could not end process: {ex.Message}");
        }
    }
}
