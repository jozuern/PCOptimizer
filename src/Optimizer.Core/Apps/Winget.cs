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
    /// <summary>winget.exe: the signed-in user's execution alias, else the newest App Installer package folder.</summary>
    public static string? Find(string? profilePath)
    {
        if (profilePath is not null)
        {
            var alias = Path.Combine(profilePath, @"AppData\Local\Microsoft\WindowsApps\winget.exe");
            if (File.Exists(alias)) return alias;
        }
        var own = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WindowsApps\winget.exe");
        if (File.Exists(own)) return own;
        try
        {
            var apps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
            return Directory.EnumerateDirectories(apps, "Microsoft.DesktopAppInstaller_*_x64__8wekyb3d8bbwe")
                .Select(d => Path.Combine(d, "winget.exe")).Where(File.Exists).OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Only catalog ids are passed to winget, and only if they contain nothing but id characters.</summary>
    public static bool IsSafeId(string id) => id.Length is > 2 and < 100 && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '-' or '_' or '+');

    public static string InstallArguments(string id) =>
        $"install --id {id} --exact --source winget --accept-package-agreements --accept-source-agreements --silent --disable-interactivity";

    public static string UpgradeArguments(string id) =>
        $"upgrade --id {id} --exact --source winget --accept-package-agreements --accept-source-agreements --silent --disable-interactivity";

    /// <summary>Installs a machine-scope app with progress lines. Returns winget's exit code (0 = success).</summary>
    public static Task<int> InstallAsync(string winget, AppEntry app, IProgress<string>? lines, CancellationToken ct)
    {
        if (!IsSafeId(app.Id)) throw new ArgumentException($"invalid winget id {app.Id}");
        if (app.Scope == "user") throw new InvalidOperationException("per-user apps are started with InstallForUser");
        return StreamingProcess.RunAsync(winget, InstallArguments(app.Id), lines, ct);
    }

    /// <summary>Starts a per-user installer as the signed-in user (window visible; the result shows on the next scan).</summary>
    public static DeElevatedLauncher.Path InstallForUser(string winget, AppEntry app, ElevationInfo? elevation)
    {
        if (!IsSafeId(app.Id)) throw new ArgumentException($"invalid winget id {app.Id}");
        return DeElevatedLauncher.Open(winget, InstallArguments(app.Id), elevation);
    }

    /// <summary>winget's documented exit codes that are not failures for an install request.</summary>
    public static bool IsSuccess(int exitCode) => exitCode is 0
        or unchecked((int)0x8A15002B) // APPINSTALLER_CLI_ERROR_UPDATE_NOT_APPLICABLE: already installed, no newer version
        or unchecked((int)0x8A150109); // APPINSTALLER_CLI_ERROR_INSTALL_REBOOT_REQUIRED_TO_FINISH
}
