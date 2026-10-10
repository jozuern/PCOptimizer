using System.Diagnostics;

namespace Optimizer.Core.Platform;

/// <summary>
/// Start settings for every tool the elevated app runs. The child inherits the environment that the signed-in user
/// controls (HKCU\Environment), so variables that make a tool load code from elsewhere are removed: PowerShell
/// modules (PSModulePath starts with the user's Documents folder, and commands load their module on first use),
/// .NET Framework profilers (COR_PROFILER and friends load a DLL into powershell.exe) and runtime switches. The
/// working directory is System32 instead of the exe's folder, which is often Downloads. SystemRoot, windir, ComSpec
/// and PATH are set to Windows' own values, and TEMP to the admin-only folder of the app.
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

    /// <summary>
    /// TEMP and TMP for the tools (set at app start to the admin-only <see cref="DataPaths.Temp"/>). The user's %TEMP%
    /// can be written by any program of the user: DISM runs DismHost.exe and its DLLs from there, and installers and
    /// uninstallers copy themselves there. Null leaves TEMP as it is (tests, tools run without the app).
    /// </summary>
    public static string? TempFolder { get; set; }

    public static void Apply(ProcessStartInfo psi)
    {
        psi.WorkingDirectory = Environment.SystemDirectory;
        var env = psi.Environment;
        RemoveCodeLoadingVariables(env);
        env["PSModulePath"] = PowerShellModules;
        // The user can set these in HKCU\Environment: a changed windir or a user folder early in PATH makes a tool load
        // another program or DLL. The machine part of PATH is admin-only; the user part is left out.
        var windows = Path.GetDirectoryName(Environment.SystemDirectory)!;
        env["SystemRoot"] = windows;
        env["windir"] = windows;
        env["ComSpec"] = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        env["PATH"] = SystemPath(windows);
        if (TempFolder is { } temp)
        {
            env["TEMP"] = temp;
            env["TMP"] = temp;
        }
    }

    private static string? _systemPath;

    /// <summary>
    /// Windows' own folders first, then the folders of the machine PATH that only administrators can change (read once
    /// per start: the folder checks read permissions up to the drive root).
    /// </summary>
    public static string SystemPath(string windows) => _systemPath ??= BuildSystemPath(windows);

    private static string BuildSystemPath(string windows)
    {
        var system = Environment.SystemDirectory;
        var parts = new List<string> { system, windows, Path.Combine(system, "Wbem"), Path.Combine(system, @"WindowsPowerShell\v1.0"), Path.Combine(system, "OpenSSH") };
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            if (key?.GetValue("Path", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) is string machine)
                foreach (var part in machine.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    // Expanded from the known folders, not from this process's variables (the user can override those).
                    var expanded = part;
                    foreach (var (name, value) in KnownFolders(windows))
                        expanded = expanded.Replace($"%{name}%", value, StringComparison.OrdinalIgnoreCase);
                    // A machine PATH entry can still point to a folder users can write (C:\Python, a user's AppData).
                    if (!expanded.Contains('%') && Path.IsPathFullyQualified(expanded) && !parts.Contains(expanded, StringComparer.OrdinalIgnoreCase) &&
                        TrustedPath.IsAdminOnlyWritableFolder(expanded))
                        parts.Add(expanded);
                }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Logging.Log.Warn("command", $"machine PATH not readable: {ex.Message}");
        }
        return string.Join(';', parts);
    }

    private static IEnumerable<(string, string)> KnownFolders(string windows)
    {
        yield return ("SystemRoot", windows);
        yield return ("windir", windows);
        yield return ("SystemDrive", Path.GetPathRoot(windows)!.TrimEnd('\\'));
        yield return ("ProgramFiles", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
        yield return ("ProgramFiles(x86)", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
        yield return ("ProgramData", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        yield return ("ALLUSERSPROFILE", Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
    }

    /// <summary>Creates the tools' TEMP folder empty (leftovers of earlier runs are removed) and uses it from now on.</summary>
    public static void UseTempFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
                foreach (var entry in new DirectoryInfo(folder).EnumerateFileSystemInfos())
                    try
                    {
                        if (entry is DirectoryInfo d && (d.Attributes & FileAttributes.ReparsePoint) == 0) d.Delete(recursive: true);
                        else entry.Delete();
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        Logging.Log.Warn("command", $"temp leftover {entry.FullName} not removed: {ex.Message}");
                    }
            Directory.CreateDirectory(folder);
            TempFolder = folder;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Logging.Log.Warn("command", $"tools TEMP folder {folder} not usable: {ex.Message}");
        }
    }

    /// <summary>Removes the profiler, runtime and PowerShell policy variables (also used for the app's own restart).</summary>
    public static void RemoveCodeLoadingVariables(IDictionary<string, string?> env, params string[] keep)
    {
        foreach (var name in env.Keys.ToList())
        {
            if (keep.Contains(name, StringComparer.OrdinalIgnoreCase)) continue;
            if (RemovedVariables.Contains(name, StringComparer.OrdinalIgnoreCase) ||
                RemovedPrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                env.Remove(name);
        }
    }

    /// <summary>
    /// Runtime switches that let another program into this process or make it write files, read by the .NET runtime
    /// from the environment before any of the app's code runs: profilers (also notification profilers), a diagnostic
    /// port the runtime connects to (a client there can attach a profiler), EventPipe traces and crash dumps written to
    /// a path of the user's choice.
    /// </summary>
    private static readonly string[] EnablingVariables =
    [
        "CORECLR_ENABLE_PROFILING", "DOTNET_ENABLE_PROFILING", "CORECLR_ENABLE_NOTIFICATION_PROFILERS", "DOTNET_ENABLE_NOTIFICATION_PROFILERS",
        "DOTNET_DiagnosticPorts", "COMPlus_DiagnosticPorts", "DOTNET_EnableEventPipe", "COMPlus_EnableEventPipe",
        "DOTNET_DbgEnableMiniDump", "COMPlus_DbgEnableMiniDump",
    ];

    /// <summary>
    /// The first variable of <see cref="EnablingVariables"/> that is set (not empty, not 0), or null. An elevated start
    /// with one set refuses to go on; what the runtime loaded before that has run already (SECURITY.md, Known limits).
    /// </summary>
    public static string? DiagnosticsRequested(Func<string, string?> getVariable) =>
        EnablingVariables.FirstOrDefault(name => getVariable(name) is { } v && v.Trim().Length > 0 && v.Trim() != "0");

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
