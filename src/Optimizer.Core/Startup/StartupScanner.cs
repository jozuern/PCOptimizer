using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Optimizer.Core.Actions;
using Optimizer.Core.Logging;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Startup;

public enum StartupKind { RunKey, StartupFolder, LogonTask, Service, Driver, ShellExtension, Winlogon, ImageHijack, AppInit, PolicyRun }

/// <summary>
/// One autostart entry. <see cref="Enabled"/> is null for entries that cannot be switched here (drivers, Winlogon,
/// AppInit, policy Run entries): those are listed so you can see them, and the page explains how to handle them.
/// </summary>
public sealed record StartupEntry(
    StartupKind Kind,
    string Name,
    string? Command,
    string? ImagePath,
    string Location,
    Hive Hive,
    bool? Enabled,
    string Key)
{
    /// <summary>Registry or file details the toggle needs (approved subkey, task path, CLSID, service name).</summary>
    public string? Target { get; init; }

    /// <summary>Non-default Winlogon values and IFEO debuggers deserve attention (often malware or leftovers).</summary>
    public bool Suspicious { get; init; }
}

/// <summary>Reads every autostart location the app knows (Autoruns-style), through the swappable registry and task interfaces.</summary>
public sealed class StartupScanner(IRegistryRoots registry, ITaskScheduler tasks, string? profilePath)
{
    private const string Run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string Run32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    private const string Approved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";
    private const string BlockedShellExt = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked";
    public const string Winlogon = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon";
    public const string Ifeo = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

    public IReadOnlyList<StartupEntry> ScanAll(bool includeServicesAndDrivers = true)
    {
        var list = new List<StartupEntry>();
        void Try(string part, Action a)
        {
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Log.Warn("startup", $"{part}: {ex.Message}");
            }
        }
        Try("run", () => list.AddRange(RunKeys()));
        Try("folders", () => list.AddRange(StartupFolders()));
        Try("tasks", () => list.AddRange(LogonTasks()));
        Try("shellext", () => list.AddRange(ShellExtensions()));
        Try("winlogon", () => list.AddRange(WinlogonEntries()));
        Try("ifeo", () => list.AddRange(ImageHijacks()));
        Try("appinit", () => list.AddRange(AppInit()));
        if (includeServicesAndDrivers) Try("services", () => list.AddRange(ServicesAndDrivers()));
        return list;
    }

    // ---------------- Run keys ----------------

    public IEnumerable<StartupEntry> RunKeys()
    {
        foreach (var (hive, path, approvedSub, label) in new[]
                 {
                     (Hive.Machine, Run, "Run", @"HKLM\" + Run),
                     (Hive.Machine, Run32, "Run32", @"HKLM\" + Run32),
                     (Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Run", "Run", @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run"),
                 })
        {
            using var key = Open(hive, path);
            if (key is null) continue;
            foreach (var name in key.GetValueNames().Where(n => n.Length > 0))
            {
                var command = key.GetValue(name, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
                var enabled = ApprovedState(hive, approvedSub, name);
                yield return new StartupEntry(StartupKind.RunKey, name, command, CommandLine.ImagePath(command), label, hive, enabled, $"run:{hive}:{approvedSub}:{name}")
                {
                    Target = $@"{Approved}\{approvedSub}",
                };
            }
        }
        // Policy Run entries (Group Policy "Run these programs at user logon"): listed, changed only through the policy.
        foreach (var hive in new[] { Hive.Machine, Hive.User })
        {
            var path = (hive == Hive.Machine ? "SOFTWARE" : "Software") + @"\Microsoft\Windows\CurrentVersion\Policies\Explorer\Run";
            using var key = Open(hive, path);
            foreach (var name in key?.GetValueNames() ?? [])
            {
                var command = key!.GetValue(name)?.ToString();
                yield return new StartupEntry(StartupKind.PolicyRun, name, command, CommandLine.ImagePath(command), $@"{(hive == Hive.Machine ? "HKLM" : "HKCU")}\{path}", hive, null, $"policyrun:{hive}:{name}");
            }
        }
    }

    /// <summary>StartupApproved data: missing = enabled; first byte odd (0x03, 0x01, 0x07) = disabled (as Task Manager writes it).</summary>
    public bool ApprovedState(Hive hive, string sub, string name)
    {
        var v = RegistryValue.Read(registry, hive, $@"{Approved}\{sub}", name);
        return StartupApprovedAction.IsEnabled(v);
    }

    // ---------------- Startup folders ----------------

    public IEnumerable<StartupEntry> StartupFolders()
    {
        var common = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), @"Microsoft\Windows\Start Menu\Programs\StartUp");
        var folders = new List<(string Dir, Hive Hive)> { (common, Hive.Machine) };
        if (profilePath is not null) folders.Add((Path.Combine(profilePath, @"AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup"), Hive.User));
        foreach (var (dir, hive) in folders)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.EnumerateFiles(dir).Where(f => !Path.GetFileName(f).Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)))
            {
                var name = Path.GetFileName(file);
                var target = file.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) ? ShortcutTarget(file) : file;
                yield return new StartupEntry(StartupKind.StartupFolder, Path.GetFileNameWithoutExtension(file), target, target, dir, hive,
                    ApprovedState(hive, "StartupFolder", name), $"folder:{hive}:{name}")
                {
                    Target = $@"{Approved}\StartupFolder|{name}",
                };
            }
        }
    }

    private static string? ShortcutTarget(string lnk)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type is null) return null;
            dynamic shell = Activator.CreateInstance(type)!;
            try
            {
                var shortcut = shell.CreateShortcut(lnk);
                string target = shortcut.TargetPath;
                string args = shortcut.Arguments;
                return string.IsNullOrEmpty(args) ? target : $"\"{target}\" {args}";
            }
            finally
            {
                Marshal.FinalReleaseComObject(shell);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    // ---------------- Scheduled tasks ----------------

    public IEnumerable<StartupEntry> LogonTasks() =>
        tasks.List().Where(t => t.AtLogon || t.AtBoot).Select(t => new StartupEntry(StartupKind.LogonTask, t.Path.TrimStart('\\'),
            t.Command is null ? null : $"\"{t.Command}\" {t.Arguments}".Trim(), CommandLine.ImagePath(t.Command is null ? null : $"\"{t.Command}\""),
            t.Path, Hive.Machine, t.Enabled, $"task:{t.Path}")
        {
            Target = t.Path,
        });

    // ---------------- Services and drivers ----------------

    public IEnumerable<StartupEntry> ServicesAndDrivers()
    {
        const string services = @"SYSTEM\CurrentControlSet\Services";
        using var root = Open(Hive.Machine, services);
        foreach (var name in root?.GetSubKeyNames() ?? [])
        {
            using var key = root!.OpenSubKey(name);
            if (key?.GetValue("Start") is not int start || key.GetValue("Type") is not int type) continue;
            var image = key.GetValue("ImagePath", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
            if (image is null) continue;
            var isDriver = (type & 0x3) != 0;
            var isService = (type & 0x30) != 0;
            if (!isDriver && !isService) continue;
            // Drivers: boot/system/automatic. Services: automatic only (manual services start on demand).
            if (isDriver ? start > 2 : start != 2) continue;
            string? file = CommandLine.ImagePath(image);
            if (isService && file is not null && Path.GetFileName(file).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase))
            {
                using var p = key.OpenSubKey("Parameters");
                if (p?.GetValue("ServiceDll", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() is { } dll) file = CommandLine.ImagePath(dll);
            }
            var display = ResolveIndirect(key.GetValue("DisplayName")?.ToString()) ?? name;
            yield return new StartupEntry(isDriver ? StartupKind.Driver : StartupKind.Service, display, image, file, $@"HKLM\{services}\{name}", Hive.Machine,
                isDriver ? null : true, $"{(isDriver ? "driver" : "service")}:{name}")
            {
                Target = name,
            };
        }
    }

    // ---------------- Explorer shell extensions ----------------

    private static readonly string[] HandlerRoots =
    [
        @"*\shellex\ContextMenuHandlers", @"Directory\shellex\ContextMenuHandlers", @"Directory\Background\shellex\ContextMenuHandlers",
        @"Folder\shellex\ContextMenuHandlers", @"Drive\shellex\ContextMenuHandlers", @"AllFilesystemObjects\shellex\ContextMenuHandlers",
    ];

    public IEnumerable<StartupEntry> ShellExtensions()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var blocked = Open(Hive.Machine, BlockedShellExt);
        var blockedSet = (blocked?.GetValueNames() ?? []).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var handlerRoot in HandlerRoots)
        {
            using var key = Open(Hive.Machine, $@"SOFTWARE\Classes\{handlerRoot}");
            foreach (var handler in key?.GetSubKeyNames() ?? [])
            {
                using var h = key!.OpenSubKey(handler);
                var clsid = h?.GetValue("")?.ToString();
                if (string.IsNullOrEmpty(clsid) && Guid.TryParse(handler, out _)) clsid = handler;
                if (clsid is null || !Guid.TryParse(clsid, out var g)) continue;
                var id = g.ToString("B").ToUpperInvariant();
                if (!seen.Add(id)) continue;
                using var server = Open(Hive.Machine, $@"SOFTWARE\Classes\CLSID\{id}\InprocServer32");
                var dll = server?.GetValue("", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
                using var cls = Open(Hive.Machine, $@"SOFTWARE\Classes\CLSID\{id}");
                var name = cls?.GetValue("")?.ToString() is { Length: > 0 } n ? n : handler;
                yield return new StartupEntry(StartupKind.ShellExtension, name, dll, CommandLine.ImagePath(dll), $@"HKCR\{handlerRoot}\{handler}", Hive.Machine,
                    !blockedSet.Contains(id), $"shellext:{id}")
                {
                    Target = id,
                };
            }
        }
    }

    // ---------------- Winlogon, IFEO, AppInit ----------------

    public static readonly IReadOnlyDictionary<string, string> WinlogonDefaults = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Shell"] = "explorer.exe",
        ["Userinit"] = @"C:\Windows\system32\userinit.exe,",
    };

    public IEnumerable<StartupEntry> WinlogonEntries()
    {
        using var key = Open(Hive.Machine, Winlogon);
        if (key is null) yield break;
        foreach (var (name, expected) in WinlogonDefaults)
        {
            var value = key.GetValue(name)?.ToString();
            if (value is null) continue;
            var normal = string.Equals(value.Trim().TrimEnd(','), expected.TrimEnd(','), StringComparison.OrdinalIgnoreCase)
                         || (name == "Userinit" && string.Equals(value.Trim().TrimEnd(','), Path.Combine(Environment.SystemDirectory, "userinit.exe"), StringComparison.OrdinalIgnoreCase));
            yield return new StartupEntry(StartupKind.Winlogon, name, value, CommandLine.ImagePath(value.Split(',')[0]), $@"HKLM\{Winlogon}", Hive.Machine, null, $"winlogon:{name}")
            {
                Suspicious = !normal,
            };
        }
    }

    public IEnumerable<StartupEntry> ImageHijacks()
    {
        using var key = Open(Hive.Machine, Ifeo);
        foreach (var exe in key?.GetSubKeyNames() ?? [])
        {
            using var sub = key!.OpenSubKey(exe);
            if (sub?.GetValue("Debugger")?.ToString() is not { Length: > 0 } debugger) continue;
            yield return new StartupEntry(StartupKind.ImageHijack, exe, debugger, CommandLine.ImagePath(debugger), $@"HKLM\{Ifeo}\{exe}", Hive.Machine, true, $"ifeo:{exe}")
            {
                Target = $@"{Ifeo}\{exe}",
                Suspicious = true,
            };
        }
    }

    public IEnumerable<StartupEntry> AppInit()
    {
        foreach (var path in new[] { @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows", @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows" })
        {
            using var key = Open(Hive.Machine, path);
            if (key?.GetValue("AppInit_DLLs")?.ToString() is not { Length: > 0 } dlls) continue;
            var load = key.GetValue("LoadAppInit_DLLs") is int l && l != 0;
            foreach (var dll in dlls.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries))
                yield return new StartupEntry(StartupKind.AppInit, Path.GetFileName(dll), dll, CommandLine.ImagePath(dll), $@"HKLM\{path}\AppInit_DLLs", Hive.Machine, null, $"appinit:{dll}")
                {
                    Suspicious = load,
                };
        }
    }

    private Microsoft.Win32.RegistryKey? Open(Hive hive, string path)
    {
        try
        {
            return registry.Open(hive, path, writable: false);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <summary>"@%SystemRoot%\system32\x.dll,-101" display names resolved through SHLoadIndirectString.</summary>
    public static string? ResolveIndirect(string? value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('@')) return value;
        var sb = new StringBuilder(512);
        return SHLoadIndirectString(value, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : value;
    }

    [DllImport("shlwapi.dll", CharSet = CharSet.Unicode)]
    private static extern int SHLoadIndirectString(string source, StringBuilder output, int outputSize, IntPtr reserved);
}

/// <summary>
/// Enables or disables a Run key or Startup folder entry the way Task Manager does: through the StartupApproved value
/// (12 bytes; first byte 0x02 = enabled, 0x03 = disabled, followed by the time it was disabled). The entry itself is
/// never deleted, so the program's own settings and updates keep working. Undo writes the original bytes back.
/// </summary>
public sealed class StartupApprovedAction : TweakAction
{
    public Hive Hive { get; init; }

    /// <summary>Full path of the StartupApproved subkey (Run, Run32 or StartupFolder).</summary>
    public string Path { get; init; } = "";

    public string Name { get; init; } = "";
    public bool Enabled { get; init; }

    public static bool IsEnabled(StoredValue raw) =>
        !raw.Existed || raw.Data is not { Length: >= 2 } hex || (Convert.ToByte(hex[..2], 16) & 0x1) == 0;

    public override string TargetKey => $"startupapproved:{Hive}:{Path}\\{Name}".ToLowerInvariant();
    public override string Describe(ActionContext c) => $"{c.Registry.DisplayRoot(Hive)}\\{Path}\\{Name}";

    // Data = "Enabled"/"Disabled" (shown and compared); Kind carries the raw bytes for restore ("raw:" + hex, or "raw:" when missing).
    private static StoredValue Semantic(StoredValue raw) =>
        new(true, "raw:" + (raw.Existed ? raw.Data : ""), IsEnabled(raw) ? "Enabled" : "Disabled");

    public override StoredValue Desired(ActionContext c) => new(true, "startupapproved", Enabled ? "Enabled" : "Disabled");

    public override StoredValue? Read(ActionContext c) => Semantic(RegistryValue.Read(c.Registry, Hive, Path, Name));

    public override void Apply(ActionContext c) => RegistryValue.Write(c.Registry, Hive, Path, Name, "binary", Bytes(Enabled, DateTime.UtcNow));

    public override void Restore(ActionContext c, StoredValue original)
    {
        var raw = original.Kind is { } k && k.StartsWith("raw:", StringComparison.Ordinal) ? k[4..] : null;
        if (string.IsNullOrEmpty(raw)) RegistryValue.Delete(c.Registry, Hive, Path, Name);
        else RegistryValue.Write(c.Registry, Hive, Path, Name, "binary", raw);
    }

    public static string Bytes(bool enabled, DateTime utc)
    {
        var b = new byte[12];
        b[0] = enabled ? (byte)0x02 : (byte)0x03;
        if (!enabled) BitConverter.GetBytes(utc.ToFileTimeUtc()).CopyTo(b, 4);
        return Convert.ToHexString(b);
    }
}

/// <summary>Builds the engine tweaks behind the startup page's switches (backup, undo and the change log come for free).</summary>
public static class StartupTweaks
{
    private static string Slug(string s) => new(s.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).Take(48).ToArray());

    /// <summary>Null when the entry cannot be switched here.</summary>
    public static TweakDefinition? Set(StartupEntry e, bool enabled)
    {
        TweakAction? action = e.Kind switch
        {
            StartupKind.RunKey => new StartupApprovedAction { Hive = e.Hive, Path = e.Target!, Name = e.Name, Enabled = enabled },
            StartupKind.StartupFolder => new StartupApprovedAction
            {
                Hive = e.Hive, Path = e.Target!.Split('|')[0], Name = e.Target.Split('|')[1], Enabled = enabled,
            },
            StartupKind.LogonTask => new ScheduledTaskAction { Path = e.Target!, Enabled = enabled },
            // Blocked shell extensions are listed by CLSID under Shell Extensions\Blocked (read by Explorer at start).
            StartupKind.ShellExtension => new RegistryAction
            {
                Hive = Hive.Machine,
                Path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked",
                Name = e.Target!,
                Kind = "string",
                Value = JsonSerializer.SerializeToElement(""),
                Delete = enabled,
            },
            // Services are set to Manual, never Disabled, so anything that needs them can still start them.
            StartupKind.Service => new ServiceAction { Name = e.Target!, StartType = enabled ? ServiceStart.Automatic : ServiceStart.Manual },
            StartupKind.ImageHijack when !enabled => new RegistryAction { Hive = Hive.Machine, Path = e.Target!, Name = "Debugger", Delete = true },
            _ => null,
        };
        if (action is null) return null;
        var expert = e.Kind is StartupKind.ImageHijack or StartupKind.Service;
        return new TweakDefinition
        {
            Id = $"startup.{(enabled ? "on" : "off")}.{e.Kind.ToString().ToLowerInvariant()}.{Slug(e.Key)}",
            Docs = $"startup.{e.Kind.ToString().ToLowerInvariant()}",
            Subject = e.Name,
            Category = "Startup",
            Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Risk = expert ? Risk.Expert : Risk.Safe,
            Hidden = true,
            SignOut = e.Kind is StartupKind.ShellExtension,
            Actions = [action],
            Sources = ["https://learn.microsoft.com/en-us/sysinternals/downloads/autoruns"],
        };
    }

    public static IReadOnlyList<string> DocIds =>
        new[] { StartupKind.RunKey, StartupKind.StartupFolder, StartupKind.LogonTask, StartupKind.ShellExtension, StartupKind.Service, StartupKind.ImageHijack }
            .Select(k => $"startup.{k.ToString().ToLowerInvariant()}").ToList();

    /// <summary>Third-party entries enabled at logon: what F16 counts.</summary>
    public static bool CountsForF16(StartupEntry e) => e.Enabled == true && e.Kind is StartupKind.RunKey or StartupKind.StartupFolder or StartupKind.LogonTask;
}
