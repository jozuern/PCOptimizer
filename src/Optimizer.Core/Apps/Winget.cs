using System.Text.RegularExpressions;
using Optimizer.Core.Hardware.Probes;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Apps;

public sealed class AppCatalog
{
    public List<AppEntry> Apps { get; init; } = [];
}

public sealed class AppEntry
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Category { get; init; } = "tools";

    /// <summary>machine = installed elevated; user = per-user installer, started as the signed-in user.</summary>
    public string Scope { get; init; } = "machine";

    /// <summary>
    /// winget --scope ("machine" or "user") for packages that offer both installers: winget prefers the per-user one by
    /// default, also when it runs elevated. Null = winget's choice.
    /// </summary>
    public string? WingetScope { get; init; }

    /// <summary>Regex on installed program names; empty = cannot be detected.</summary>
    public string Detect { get; init; } = "";

    public string En { get; init; } = "";
    public string De { get; init; } = "";

    public string Text(string lang) => lang == "de" && De.Length > 0 ? De : En;

    public bool IsInstalled(IEnumerable<InstalledProgram> programs) =>
        Detect.Length > 1 && programs.Any(p => Regex.IsMatch(p.Name, Detect, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100)));
}

/// <summary>
/// winget (App Installer) for the app page. Machine-scope installs run elevated with streamed output; per-user
/// installers (Discord, Spotify) are started as the signed-in user, so they land in that user's profile.
/// </summary>
public static class Winget
{
    private const string PackageRepository = @"SOFTWARE\Classes\Local Settings\Software\Microsoft\Windows\CurrentVersion\AppModel\PackageRepository\Packages";

    /// <summary>
    /// winget.exe for elevated runs: only from the App Installer package folder under Program Files\WindowsApps (writable
    /// only by TrustedInstaller), found through the machine's package repository (HKLM, admin-only). Never the execution
    /// alias in the user's WindowsApps folder: the user can put any program there, and this process runs it as admin.
    /// </summary>
    public static string? FindTrusted() => FindTrusted(ReadPackageFolders());

    public static string? FindTrusted(IEnumerable<(string Package, string Folder)> packages)
    {
        var protectedRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps") + "\\";
        return packages
            .Select(p => (Version: PackageVersion(p.Package), p.Folder))
            .Where(p => p.Version is not null && p.Folder.StartsWith(protectedRoot, StringComparison.OrdinalIgnoreCase) && !p.Folder.Contains("..", StringComparison.Ordinal))
            .OrderByDescending(p => p.Version)
            .Select(p => Path.Combine(p.Folder, "winget.exe"))
            .FirstOrDefault(p => File.Exists(p) && Platform.SafeDelete.HasNoLinks(p));
    }

    /// <summary>
    /// winget.exe for installers started as the signed-in user (they run with that user's rights anyway): the user's
    /// execution alias, else the package folder.
    /// </summary>
    public static string? FindForUser(string? profilePath)
    {
        if (profilePath is not null)
        {
            var alias = Path.Combine(profilePath, @"AppData\Local\Microsoft\WindowsApps\winget.exe");
            if (File.Exists(alias)) return alias;
        }
        return FindTrusted();
    }

    /// <summary>"Microsoft.DesktopAppInstaller_1.29.380.0_x64__8wekyb3d8bbwe" -> 1.29.380.0; other packages (language, bundle) -> null.</summary>
    public static Version? PackageVersion(string package)
    {
        const string prefix = "Microsoft.DesktopAppInstaller_", suffix = "_x64__8wekyb3d8bbwe";
        if (!package.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) || !package.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) return null;
        return Version.TryParse(package[prefix.Length..^suffix.Length], out var v) ? v : null;
    }

    private static List<(string, string)> ReadPackageFolders()
    {
        var list = new List<(string, string)>();
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(PackageRepository);
            foreach (var name in key?.GetSubKeyNames().Where(n => n.StartsWith("Microsoft.DesktopAppInstaller_", StringComparison.OrdinalIgnoreCase)) ?? [])
            {
                using var sub = key!.OpenSubKey(name);
                if (sub?.GetValue("Path") is string folder) list.Add((name, folder));
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
        }
        return list;
    }

    /// <summary>Only catalog ids are passed to winget, and only if they contain nothing but id characters.</summary>
    public static bool IsSafeId(string id) => id.Length is > 2 and < 100 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or '+');

    public static string InstallArguments(string id, string? scope = null) =>
        $"install --id {id} --exact --source winget{ScopeArgument(scope)} --accept-package-agreements --accept-source-agreements --silent --disable-interactivity";

    public static string UpgradeArguments(string id, string? scope = null) =>
        $"upgrade --id {id} --exact --source winget{ScopeArgument(scope)} --accept-package-agreements --accept-source-agreements --silent --disable-interactivity";

    /// <summary>Only the two values winget documents; anything else is left out.</summary>
    private static string ScopeArgument(string? scope) => scope is "machine" or "user" ? $" --scope {scope}" : "";

    /// <summary>Installs a machine-scope app with progress lines. Returns winget's exit code (0 = success).</summary>
    public static Task<int> InstallAsync(string winget, AppEntry app, IProgress<string>? lines, CancellationToken ct)
    {
        if (!IsSafeId(app.Id)) throw new ArgumentException($"invalid winget id {app.Id}");
        if (app.Scope == "user") throw new InvalidOperationException("per-user apps are started with InstallForUser");
        return StreamingProcess.RunAsync(winget, InstallArguments(app.Id, app.WingetScope), lines, ct);
    }

    /// <summary>Starts a per-user installer as the signed-in user (window visible; the result shows on the next scan).</summary>
    public static DeElevatedLauncher.Path InstallForUser(string winget, AppEntry app, ElevationInfo? elevation)
    {
        if (!IsSafeId(app.Id)) throw new ArgumentException($"invalid winget id {app.Id}");
        return DeElevatedLauncher.Open(winget, InstallArguments(app.Id, app.WingetScope), elevation);
    }

    /// <summary>winget's documented exit codes that are not failures for an install request.</summary>
    public static bool IsSuccess(int exitCode) => exitCode is 0
        or unchecked((int)0x8A15002B) // APPINSTALLER_CLI_ERROR_UPDATE_NOT_APPLICABLE: already installed, no newer version
        or unchecked((int)0x8A150109); // APPINSTALLER_CLI_ERROR_INSTALL_REBOOT_REQUIRED_TO_FINISH
}
