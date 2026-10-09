using System.Text;
using System.Text.Json;
using Optimizer.Core.Actions;
using Optimizer.Core.Catalog;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Debloat;

public sealed class AppxCatalog
{
    public List<AppxEntry> Apps { get; init; } = [];
    public List<string> Protected { get; init; } = [];

    public bool IsProtected(string name) => Protected.Any(p =>
        p.EndsWith('*') ? name.StartsWith(p[..^1], StringComparison.OrdinalIgnoreCase) : string.Equals(p, name, StringComparison.OrdinalIgnoreCase));
}

public sealed class AppxEntry
{
    public string Name { get; init; } = "";

    /// <summary>consumer | microsoft | xbox | media.</summary>
    public string Group { get; init; } = "consumer";

    /// <summary>null | xbox | xboxOrX3d.</summary>
    public string? Guard { get; init; }

    public string Title { get; init; } = "";
    public string TitleDe { get; init; } = "";
    public string En { get; init; } = "";
    public string De { get; init; } = "";

    public string Text(string lang) => lang == "de" && De.Length > 0 ? De : En;
    public string Label(string lang) => lang == "de" && TitleDe.Length > 0 ? TitleDe : Title.Length > 0 ? Title : Name;
}

public sealed record InstalledAppx(string Name, string FamilyName, string Version, bool NonRemovable);

/// <summary>An offered app on this PC, with the reason it is blocked (label key) if a guard applies.</summary>
public sealed record DebloatItem(AppxEntry Entry, InstalledAppx Installed, string? BlockKey);

public sealed record RemovedApp(string Name, string FamilyName, string Version, DateTimeOffset RemovedAt)
{
    /// <summary>Microsoft Store page of the package (documented ms-windows-store PFN link).</summary>
    public string StoreLink => $"ms-windows-store://pdp/?PFN={Uri.EscapeDataString(FamilyName)}";
}

/// <summary>
/// AppX debloat (plan v4 M5): lists installed packages, offers only catalog entries (allowlist), removes for all users and
/// deprovisions them so new accounts do not get them again. Removal is not undoable by the app; the Store link of every
/// removed package is kept for reinstalling.
/// </summary>
public sealed class DebloatService(IProcessRunner processes, string dataFolder)
{
    private string LogFile => Path.Combine(dataFolder, "removed-apps.json");

    public static string EncodedCommand(string script) => Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    private (int Code, string Output) PowerShell(string script, TimeSpan timeout) =>
        processes.Run("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {EncodedCommand(script)}", timeout);

    public IReadOnlyList<InstalledAppx> ListInstalled(bool allUsers)
    {
        var script = $"$ProgressPreference='SilentlyContinue'; Get-AppxPackage{(allUsers ? " -AllUsers" : "")} | " +
                     "Where-Object { -not $_.IsFramework } | Select-Object Name,PackageFamilyName,Version,NonRemovable | ConvertTo-Json -Compress";
        var (code, output) = PowerShell(script, TimeSpan.FromMinutes(2));
        if (code != 0) throw new InvalidOperationException($"Get-AppxPackage failed ({code})");
        return ParseList(output);
    }

    public static IReadOnlyList<InstalledAppx> ParseList(string json)
    {
        json = json.Trim();
        if (json.Length == 0) return [];
        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.ValueKind == JsonValueKind.Array ? doc.RootElement.EnumerateArray().ToList() : [doc.RootElement];
        return items.Select(e => new InstalledAppx(
                e.GetProperty("Name").GetString() ?? "",
                e.GetProperty("PackageFamilyName").GetString() ?? "",
                e.TryGetProperty("Version", out var v) ? v.ToString() : "",
                e.TryGetProperty("NonRemovable", out var nr) && nr.ValueKind == JsonValueKind.True))
            .Where(a => a.Name.Length > 0)
            .DistinctBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Catalog entries that are installed, with guards evaluated against the hardware profile.</summary>
    public static IReadOnlyList<DebloatItem> Offer(AppxCatalog catalog, IEnumerable<InstalledAppx> installed, HardwareProfile profile, CatalogData data)
    {
        var xboxGames = profile.Software?.Games.Any(g => g.Launcher.Contains("Xbox", StringComparison.OrdinalIgnoreCase)) == true
                        || profile.Software?.Launchers.Any(l => l.Contains("Xbox", StringComparison.OrdinalIgnoreCase)) == true;
        var x3dDual = profile.Cpu is { } cpu && X3d.Classify(cpu, data) == X3dLayout.MultiCcdAsymmetric;
        var byName = installed.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        var list = new List<DebloatItem>();
        foreach (var e in catalog.Apps)
        {
            if (!byName.TryGetValue(e.Name, out var app) || catalog.IsProtected(e.Name)) continue;
            string? block = null;
            if (app.NonRemovable) block = "block.appNonRemovable";
            else if (e.Guard is "xbox" or "xboxOrX3d" && xboxGames) block = "block.appXboxGames";
            else if (e.Guard == "xboxOrX3d" && x3dDual) block = "block.appX3dGameBar";
            list.Add(new DebloatItem(e, app, block));
        }
        return list;
    }

    /// <summary>Removes the package for all users and its provisioned copy. Returns null on success, else the error text.</summary>
    public string? Remove(InstalledAppx app)
    {
        if (!IsSafeName(app.Name)) return "invalid package name";
        var script = "$ProgressPreference='SilentlyContinue'; $ErrorActionPreference='Stop'; " +
                     $"Get-AppxPackage -AllUsers -Name '{app.Name}' | Remove-AppxPackage -AllUsers; " +
                     $"Get-AppxProvisionedPackage -Online | Where-Object DisplayName -eq '{app.Name}' | Remove-AppxProvisionedPackage -Online -AllUsers -ErrorAction SilentlyContinue | Out-Null";
        var (code, output) = PowerShell(script, TimeSpan.FromMinutes(3));
        if (code != 0) return string.IsNullOrWhiteSpace(output) ? $"exit code {code}" : output.Trim();
        Record(new RemovedApp(app.Name, app.FamilyName, app.Version, DateTimeOffset.Now));
        Log.Info("debloat", $"removed {app.Name}", new { app.Version });
        return null;
    }

    /// <summary>Package names come from the catalog, but never pass anything but [A-Za-z0-9._-] into a script.</summary>
    public static bool IsSafeName(string name) => name.Length is > 0 and < 128 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-');

    public IReadOnlyList<RemovedApp> Removed()
    {
        try
        {
            return File.Exists(LogFile) ? JsonSerializer.Deserialize<List<RemovedApp>>(File.ReadAllText(LogFile)) ?? [] : [];
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            Log.Warn("debloat", $"removed-apps log unreadable: {ex.Message}");
            return [];
        }
    }

    private void Record(RemovedApp app)
    {
        var list = Removed().Where(r => !string.Equals(r.Name, app.Name, StringComparison.OrdinalIgnoreCase)).Append(app).ToList();
        Directory.CreateDirectory(dataFolder);
        File.WriteAllText(LogFile, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
    }
}

/// <summary>
/// OneDrive state for the uninstall guard: Known Folder Move (Desktop/Documents/Pictures redirected into OneDrive) and
/// cloud-only files would leave the user without local copies after an uninstall, so uninstall is blocked then.
/// </summary>
public sealed record OneDriveState(string? SetupPath, string? UserFolder, IReadOnlyList<string> RedirectedFolders, int CloudOnlyFiles, bool ScanTruncated)
{
    public bool Installed => SetupPath is not null;
    public bool KnownFolderMove => RedirectedFolders.Count > 0;

    /// <summary>Label key of the reason uninstall is blocked, or null.</summary>
    public string? BlockKey => !Installed ? "block.oneDriveNotInstalled" : KnownFolderMove ? "block.oneDriveKfm" : CloudOnlyFiles > 0 ? "block.oneDriveCloudOnly" : null;
}

public static class OneDrive
{
    private const uint RecallOnDataAccess = 0x00400000, RecallOnOpen = 0x00040000, Offline = 0x00001000;

    public static OneDriveState Read(IRegistryRoots registry, string? profilePath, int maxFiles = 200_000)
    {
        string? userFolder = null;
        foreach (var account in new[] { "Personal", "Business1", "Business2" })
        {
            var v = RegistryValue.Read(registry, Hive.User, $@"Software\Microsoft\OneDrive\Accounts\{account}", "UserFolder");
            if (v is { Existed: true, Data: { Length: > 0 } folder }) { userFolder = folder; break; }
        }

        var redirected = new List<string>();
        if (userFolder is not null)
        {
            foreach (var name in new[] { "Desktop", "Personal", "My Pictures", "My Music", "My Video" })
            {
                var v = RegistryValue.Read(registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", name);
                var expanded = Expand(v.Data, profilePath);
                if (expanded is not null && expanded.StartsWith(userFolder, StringComparison.OrdinalIgnoreCase)) redirected.Add(name);
            }
        }

        var (cloudOnly, truncated) = userFolder is not null && Directory.Exists(userFolder) ? CountCloudOnly(userFolder, maxFiles) : (0, false);
        // System32\OneDriveSetup.exe exists on every Windows; OneDrive is installed only when OneDrive.exe exists.
        return new OneDriveState(IsInstalled(profilePath) ? FindSetup(profilePath) : null, userFolder, redirected, cloudOnly, truncated);
    }

    public static bool IsInstalled(string? profilePath)
    {
        var machine = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Microsoft OneDrive\OneDrive.exe");
        var perUser = profilePath is null ? null : Path.Combine(profilePath, @"AppData\Local\Microsoft\OneDrive\OneDrive.exe");
        return File.Exists(machine) || (perUser is not null && File.Exists(perUser));
    }

    private static string? Expand(string? value, string? profilePath)
    {
        if (string.IsNullOrEmpty(value)) return null;
        // %USERPROFILE% must be the session user's profile, not the elevated account's.
        if (profilePath is not null) value = value.Replace("%USERPROFILE%", profilePath, StringComparison.OrdinalIgnoreCase);
        return Environment.ExpandEnvironmentVariables(value);
    }

    public static string? FindSetup(string? profilePath)
    {
        var candidates = new List<string>();
        if (profilePath is not null)
        {
            var perUser = Path.Combine(profilePath, @"AppData\Local\Microsoft\OneDrive");
            if (Directory.Exists(perUser)) candidates.AddRange(Directory.GetDirectories(perUser).Select(d => Path.Combine(d, "OneDriveSetup.exe")));
        }
        var machine = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft OneDrive");
        if (Directory.Exists(machine)) candidates.AddRange(Directory.GetDirectories(machine).Select(d => Path.Combine(d, "OneDriveSetup.exe")));
        candidates.Add(Path.Combine(Environment.SystemDirectory, "OneDriveSetup.exe"));
        return candidates.Where(File.Exists).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
    }

    /// <summary>Counts files whose content is only in the cloud (placeholders). Does not follow junctions or links.</summary>
    public static (int Count, bool Truncated) CountCloudOnly(string root, int maxFiles)
    {
        var count = 0;
        var seen = 0;
        var options = new EnumerationOptions { IgnoreInaccessible = true, AttributesToSkip = 0 };
        var stack = new Stack<string>([root]);
        while (stack.Count > 0)
        {
            var dir = stack.Pop();
            IEnumerable<FileSystemInfo> entries;
            try
            {
                entries = new DirectoryInfo(dir).EnumerateFileSystemInfos("*", options);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                continue;
            }
            foreach (var e in entries)
            {
                if (++seen > maxFiles) return (count, true);
                var attrs = (uint)e.Attributes;
                if (e is DirectoryInfo)
                {
                    // OneDrive folders are reparse points themselves (cloud files), so only skip real links/junctions.
                    if ((e.Attributes & FileAttributes.ReparsePoint) != 0 && e.LinkTarget is not null) continue;
                    stack.Push(e.FullName);
                }
                else if ((attrs & (RecallOnDataAccess | RecallOnOpen | Offline)) != 0)
                {
                    count++;
                }
            }
        }
        return (count, false);
    }

    /// <summary>Runs OneDriveSetup /uninstall as the signed-in user (OneDrive is a per-user install).</summary>
    public static DeElevatedLauncher.Path Uninstall(OneDriveState state, ElevationInfo? elevation)
    {
        if (state.BlockKey is { } block) throw new InvalidOperationException(block);
        return DeElevatedLauncher.Open(state.SetupPath!, "/uninstall", elevation);
    }
}
