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

    /// <summary>A trailing * matches a prefix; an entry "!name" exempts that one package from a prefix (Edge Game Assist under Microsoft.Edge*).</summary>
    public bool IsProtected(string name) =>
        !Protected.Any(p => p.StartsWith('!') && Matches(p[1..], name)) && Protected.Any(p => !p.StartsWith('!') && Matches(p, name));

    private static bool Matches(string pattern, string name) => pattern.EndsWith('*')
        ? name.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase)
        : string.Equals(pattern, name, StringComparison.OrdinalIgnoreCase);
}

public sealed class AppxEntry
{
    public string Name { get; init; } = "";

    /// <summary>consumer | microsoft | xbox | media | legacy | thirdParty | oem.</summary>
    public string Group { get; init; } = "consumer";

    /// <summary>
    /// The name is the end of the package name after the publisher prefix ("Asphalt8Airborne" matches
    /// "GAMELOFTSA.Asphalt8Airborne"), for third-party apps whose publisher prefix differs between PC makers.
    /// </summary>
    public bool Suffix { get; init; }

    public bool Matches(string packageName) =>
        string.Equals(packageName, Name, StringComparison.OrdinalIgnoreCase)
        || Suffix && packageName.EndsWith("." + Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>null | xbox | xboxOrX3d.</summary>
    public string? Guard { get; init; }

    public string Title { get; init; } = "";
    public string TitleDe { get; init; } = "";
    public string En { get; init; } = "";
    public string De { get; init; } = "";

    /// <summary>"store": the Microsoft Store still offers the app; "none": it cannot be installed again.</summary>
    public string Reinstall { get; init; } = "store";

    /// <summary>Microsoft Store product ID, for the documented ms-windows-store ProductId link.</summary>
    public string? StoreId { get; init; }

    public bool CanReinstall => Reinstall != "none";

    public string Text(string lang) => lang == "de" && De.Length > 0 ? De : En;
    public string Label(string lang) => lang == "de" && TitleDe.Length > 0 ? TitleDe : Title.Length > 0 ? Title : Name;
}

public sealed record InstalledAppx(string Name, string FamilyName, string Version, bool NonRemovable);

/// <summary>An offered app on this PC, with the reason it is blocked (label key) if a guard applies.</summary>
public sealed record DebloatItem(AppxEntry Entry, InstalledAppx Installed, string? BlockKey);

public sealed record RemovedApp(string Name, string FamilyName, string Version, DateTimeOffset RemovedAt, string? StoreId = null, bool Reinstallable = true)
{
    /// <summary>
    /// Microsoft Store page of the app: the ProductId link Microsoft recommends, or the package family name link (still
    /// working, but deprecated) for records without a product ID. Null when the Store no longer offers the app.
    /// </summary>
    public string? StoreLink => !Reinstallable ? null
        : StoreId is { Length: > 0 } id ? $"ms-windows-store://pdp/?ProductId={Uri.EscapeDataString(id)}"
        : $"ms-windows-store://pdp/?PFN={Uri.EscapeDataString(FamilyName)}";
}

/// <summary>
/// AppX debloat: lists installed packages, offers only catalog entries (allowlist), removes for all users and
/// deprovisions them so new accounts do not get them again. Removal is not undoable by the app; for apps the Store still
/// offers, the Store link is kept for reinstalling.
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
        var installedList = installed.ToList();
        var list = new List<DebloatItem>();
        foreach (var e in catalog.Apps)
        foreach (var app in installedList.Where(a => e.Matches(a.Name)))
        {
            if (catalog.IsProtected(e.Name) || catalog.IsProtected(app.Name)) continue;
            string? block = null;
            if (app.NonRemovable) block = "block.appNonRemovable";
            else if (e.Guard is "xbox" or "xboxOrX3d" && xboxGames) block = "block.appXboxGames";
            else if (e.Guard == "xboxOrX3d" && x3dDual) block = "block.appX3dGameBar";
            list.Add(new DebloatItem(e, app, block));
        }
        return list;
    }

    /// <summary>Removes the package for all users and its provisioned copy. Returns null on success, else the error text.</summary>
    public string? Remove(InstalledAppx app, AppxEntry? entry = null)
    {
        if (!IsSafeName(app.Name)) return "invalid package name";
        var script = "$ProgressPreference='SilentlyContinue'; $ErrorActionPreference='Stop'; " +
                     $"Get-AppxPackage -AllUsers -Name '{app.Name}' | Remove-AppxPackage -AllUsers; " +
                     $"Get-AppxProvisionedPackage -Online | Where-Object DisplayName -eq '{app.Name}' | Remove-AppxProvisionedPackage -Online -AllUsers -ErrorAction SilentlyContinue | Out-Null";
        var (code, output) = PowerShell(script, TimeSpan.FromMinutes(3));
        if (code != 0) return string.IsNullOrWhiteSpace(output) ? $"exit code {code}" : output.Trim();
        Record(new RemovedApp(app.Name, app.FamilyName, app.Version, DateTimeOffset.Now, entry?.StoreId, entry?.CanReinstall ?? true));
        Log.Info("debloat", $"removed {app.Name}", new { app.Version });
        return null;
    }

    /// <summary>
    /// Apps this app removed that are installed again, usually brought back by a Windows feature update or by the
    /// manufacturer's software. Removed after the app was installed again counts only once per name.
    /// </summary>
    public static IReadOnlyList<RemovedApp> CameBack(IEnumerable<RemovedApp> removed, IEnumerable<InstalledAppx> installed)
    {
        var names = installed.Select(a => a.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return removed.Where(r => names.Contains(r.Name)).DistinctBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    /// <summary>Package names come from the catalog, but never pass anything but [A-Za-z0-9._-] into a script.</summary>
    public static bool IsSafeName(string name) => name.Length is > 0 and < 128 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-');

    public IReadOnlyList<RemovedApp> Removed() => ReadRemoved(out _);

    /// <summary><paramref name="damaged"/>: the file exists but its content cannot be parsed.</summary>
    private IReadOnlyList<RemovedApp> ReadRemoved(out bool damaged)
    {
        damaged = false;
        try
        {
            return File.Exists(LogFile) ? JsonSerializer.Deserialize<List<RemovedApp>>(File.ReadAllText(LogFile)) ?? [] : [];
        }
        catch (JsonException ex)
        {
            damaged = true;
            Log.Warn("debloat", $"removed-apps log unreadable: {ex.Message}");
            return [];
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("debloat", $"removed-apps log unreadable: {ex.Message}");
            return [];
        }
    }

    /// <summary>
    /// Takes apps off the removed list (the user installed them again on purpose): the "came back" notice stops for
    /// them, and a later removal records them again.
    /// </summary>
    public void Forget(IEnumerable<string> names)
    {
        var set = names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var list = ReadRemoved(out var damaged);
        if (damaged || !list.Any(r => set.Contains(r.Name))) return;
        Write(list.Where(r => !set.Contains(r.Name)).ToList());
    }

    private void Write(List<RemovedApp> list)
    {
        var temp = LogFile + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(list, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, LogFile, overwrite: true);
    }

    /// <summary>
    /// Written to a temporary file and moved over the log, so a power loss leaves the old or the new list. A log that
    /// cannot be parsed is kept aside (.damaged) instead of being replaced by a list with only the new app: it holds the
    /// Store links of the apps removed before.
    /// </summary>
    private void Record(RemovedApp app)
    {
        var list = ReadRemoved(out var damaged).Where(r => !string.Equals(r.Name, app.Name, StringComparison.OrdinalIgnoreCase)).Append(app).ToList();
        Directory.CreateDirectory(dataFolder);
        if (damaged) File.Move(LogFile, $"{LogFile}-{DateTime.Now:yyyyMMdd-HHmmss}.damaged", overwrite: true);
        Write(list);
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

    /// <summary>
    /// Label key of the reason uninstall is blocked, or null. A scan that stopped at its file limit blocks too: files
    /// that exist only online may be among those not looked at.
    /// </summary>
    public string? BlockKey => !Installed ? "block.oneDriveNotInstalled" : KnownFolderMove ? "block.oneDriveKfm"
        : CloudOnlyFiles > 0 ? "block.oneDriveCloudOnly" : ScanTruncated ? "block.oneDriveScanIncomplete" : null;
}

public static class OneDrive
{
    private const uint RecallOnDataAccess = 0x00400000, RecallOnOpen = 0x00040000, Offline = 0x00001000;

    private const string Accounts = @"Software\Microsoft\OneDrive\Accounts";

    /// <summary>
    /// Every signed-in account counts (Personal, Business1, Business2 and further ones): folders moved into any of them
    /// and files that exist only online in any of them block the uninstall.
    /// </summary>
    public static OneDriveState Read(IRegistryRoots registry, string? profilePath, int maxFiles = 200_000)
    {
        var userFolders = UserFolders(registry);
        var redirected = new List<string>();
        foreach (var name in new[] { "Desktop", "Personal", "My Pictures", "My Music", "My Video" })
        {
            var v = RegistryValue.Read(registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", name);
            var expanded = Expand(v.Data, profilePath);
            if (expanded is not null && userFolders.Any(f => IsBelow(expanded, f))) redirected.Add(name);
        }

        var cloudOnly = 0;
        var truncated = false;
        foreach (var folder in userFolders.Where(Directory.Exists))
        {
            var (count, cut) = CountCloudOnly(folder, maxFiles);
            cloudOnly += count;
            truncated |= cut;
        }
        // System32\OneDriveSetup.exe exists on every Windows; OneDrive is installed only when OneDrive.exe exists.
        return new OneDriveState(IsInstalled(profilePath) ? FindSetup(profilePath) : null, userFolders.FirstOrDefault(), redirected, cloudOnly, truncated);
    }

    private static List<string> UserFolders(IRegistryRoots registry)
    {
        var folders = new List<string>();
        Microsoft.Win32.RegistryKey? key;
        try
        {
            key = registry.Open(Hive.User, Accounts, writable: false);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return folders;
        }
        using (key)
        {
            foreach (var account in key?.GetSubKeyNames() ?? [])
            {
                var v = RegistryValue.Read(registry, Hive.User, $@"{Accounts}\{account}", "UserFolder");
                if (v is { Existed: true, Data: { Length: > 0 } folder } && !folders.Contains(folder, StringComparer.OrdinalIgnoreCase)) folders.Add(folder);
            }
        }
        return folders;
    }

    /// <summary>The folder itself or a folder below it (C:\Users\x\OneDrive matches, C:\Users\x\OneDriveOld does not).</summary>
    private static bool IsBelow(string path, string folder)
    {
        var p = path.TrimEnd('\\');
        var f = folder.TrimEnd('\\');
        return p.Equals(f, StringComparison.OrdinalIgnoreCase) || p.StartsWith(f + "\\", StringComparison.OrdinalIgnoreCase);
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
