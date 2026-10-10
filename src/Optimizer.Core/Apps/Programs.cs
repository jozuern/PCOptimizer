using System.Globalization;
using Optimizer.Core.Actions;
using Optimizer.Core.Platform;
using Optimizer.Core.Startup;

namespace Optimizer.Core.Apps;

/// <summary>A desktop program from the Windows uninstall list (what Settings > Apps > Installed apps shows).</summary>
public sealed record DesktopProgram(
    string Name,
    string? Publisher,
    string? Version,
    long? SizeKb,
    DateTime? InstallDate,
    string? UninstallString,
    string? ProductCode,
    bool PerUser,
    string RegistryKey)
{
    public bool CanUninstall => ProductCode is not null || !string.IsNullOrWhiteSpace(UninstallString);
}

/// <summary>How an uninstall runs: Windows Installer (msiexec from System32), elevated, or as the signed-in user.</summary>
public enum UninstallMode { WindowsInstaller, Elevated, AsUser }

public sealed record UninstallCommand(UninstallMode Mode, string File, string Arguments);

/// <summary>
/// Reads the Uninstall keys (64-bit, 32-bit and the session user's) and builds the uninstall command. The program's
/// own uninstaller runs elevated only when it is in a folder only administrators can change; a per-user program or an
/// uninstaller in a user-writable folder is started as the signed-in user, and asks for administrator rights itself if
/// it needs them.
/// </summary>
public static class Programs
{
    private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Uninstall32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public static IReadOnlyList<DesktopProgram> Read(IRegistryRoots registry)
    {
        var list = new List<DesktopProgram>();
        foreach (var (hive, path) in new[] { (Hive.Machine, Uninstall), (Hive.Machine, Uninstall32), (Hive.User, Uninstall) })
        {
            Microsoft.Win32.RegistryKey? key;
            try
            {
                key = registry.Open(hive, path, writable: false);
            }
            catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
            {
                continue; // no user hive
            }
            using (key)
            {
                foreach (var sub in key?.GetSubKeyNames() ?? [])
                {
                    using var app = key!.OpenSubKey(sub);
                    if (app is null || Read(app, sub, hive, path) is not { } p) continue;
                    list.Add(p);
                }
            }
        }
        // The same program can be listed by both views; keep the entry that can be uninstalled.
        return list.GroupBy(p => (p.Name, p.Version), new NameVersionComparer())
            .Select(g => g.OrderByDescending(p => p.CanUninstall).First())
            .OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    private sealed class NameVersionComparer : IEqualityComparer<(string, string?)>
    {
        public bool Equals((string, string?) a, (string, string?) b) =>
            string.Equals(a.Item1, b.Item1, StringComparison.OrdinalIgnoreCase) && string.Equals(a.Item2, b.Item2, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((string, string?) v) => StringComparer.OrdinalIgnoreCase.GetHashCode(v.Item1);
    }

    private static DesktopProgram? Read(Microsoft.Win32.RegistryKey app, string sub, Hive hive, string path)
    {
        if (app.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name)) return null;
        // Windows components, updates and parts of other products are not listed in Settings either.
        if (app.GetValue("SystemComponent") is int sc && sc == 1) return null;
        if (app.GetValue("ParentKeyName") is string { Length: > 0 }) return null;
        if (app.GetValue("ReleaseType") is string release && release is "Update" or "Hotfix" or "Security Update") return null;
        var uninstall = app.GetValue("UninstallString", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) as string;
        string? productCode = null;
        if (app.GetValue("WindowsInstaller") is int wi && wi == 1 && Guid.TryParse(sub, out var code)) productCode = code.ToString("B").ToUpperInvariant();
        long? size = app.GetValue("EstimatedSize") is int kb && kb > 0 ? kb : null;
        DateTime? date = app.GetValue("InstallDate") is string d && DateTime.TryParseExact(d, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt) ? dt : null;
        return new DesktopProgram(name.Trim(), app.GetValue("Publisher") as string, app.GetValue("DisplayVersion") as string, size, date,
            uninstall, productCode, hive == Hive.User, $@"{(hive == Hive.Machine ? "HKLM" : "HKCU")}\{path}\{sub}");
    }

    /// <summary>
    /// Null when the program has no usable uninstall command. Windows Installer entries always use System32's msiexec
    /// with /x; a per-user entry (HKCU, which any program of the user can write) runs it as the signed-in user, so a
    /// planted entry cannot make an elevated msiexec remove a program installed for the whole PC. Other uninstallers run
    /// elevated only when the program and, for rundll32 lines, its DLL are in folders only administrators can change.
    /// </summary>
    public static UninstallCommand? Command(DesktopProgram p, Func<string, bool>? isAdminOnly = null, Func<string, bool>? exists = null)
    {
        isAdminOnly ??= TrustedPath.IsAdminOnlyWritable;
        var msiMode = p.PerUser ? UninstallMode.AsUser : UninstallMode.WindowsInstaller;
        var msiexec = ProcessHardening.ResolveSystemTool("msiexec.exe");
        if (p.ProductCode is { } productCode) return new UninstallCommand(msiMode, msiexec, $"/x {productCode}");
        if (string.IsNullOrWhiteSpace(p.UninstallString)) return null;
        var expanded = Environment.ExpandEnvironmentVariables(p.UninstallString.Trim());
        if (CommandLine.Program(expanded, s => s, exists) is not var (file, args)) return null;
        // "MsiExec.exe /I{GUID}" or "/X{GUID}" without the WindowsInstaller flag: always the remove action of System32's msiexec.
        if (Path.GetFileName(file).Equals("msiexec.exe", StringComparison.OrdinalIgnoreCase) || Path.GetFileName(file).Equals("msiexec", StringComparison.OrdinalIgnoreCase))
        {
            var start = expanded.IndexOf('{');
            var end = start >= 0 ? expanded.IndexOf('}', start) : -1;
            return start >= 0 && end > start && Guid.TryParse(expanded[start..(end + 1)], out var g)
                ? new UninstallCommand(msiMode, msiexec, $"/x {g.ToString("B").ToUpperInvariant()}")
                : null;
        }
        // For a rundll32 line the DLL is the code that runs: it must be admin-only too.
        var image = CommandLine.ImagePath(expanded, s => s, exists);
        var elevated = !p.PerUser && isAdminOnly(file)
            && (image is null || string.Equals(image, file, StringComparison.OrdinalIgnoreCase) || isAdminOnly(image));
        return new UninstallCommand(elevated ? UninstallMode.Elevated : UninstallMode.AsUser, file, args);
    }
}

/// <summary>Runs an uninstall command with the uninstaller's own window, so the user answers its questions.</summary>
public static class ProgramUninstaller
{
    /// <summary>Exit code for elevated runs; null when the uninstaller was started as the signed-in user (not waited for).</summary>
    public static async Task<int?> RunAsync(UninstallCommand command, ElevationInfo? elevation, CancellationToken ct = default)
    {
        Logging.Log.Info("uninstall", $"{command.Mode}: {command.File} {command.Arguments}");
        if (command.Mode == UninstallMode.AsUser)
        {
            if (DeElevatedLauncher.Open(command.File, command.Arguments, elevation) == DeElevatedLauncher.Path.Failed)
                throw new InvalidOperationException("The uninstaller could not be started as the signed-in user.");
            return null;
        }
        var psi = new System.Diagnostics.ProcessStartInfo(command.File, command.Arguments) { UseShellExecute = false };
        ProcessHardening.Apply(psi);
        using var p = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException($"Cannot start {command.File}");
        await p.WaitForExitAsync(ct);
        Logging.Log.Info("uninstall", $"exit {p.ExitCode}");
        return p.ExitCode;
    }
}
