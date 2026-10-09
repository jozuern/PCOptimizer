using Microsoft.Win32;

namespace Optimizer.Core.Hardware.Probes;

public sealed record InstalledProgram(string Name, string? Version, string? Publisher, bool PerUser);

/// <summary>
/// Installed programs from the Uninstall registry keys (machine 64/32-bit and the session user's hive).
/// Never uses Win32_Product: querying it triggers MSI self-repair (plan v4 §7.8).
/// </summary>
public static class InstalledPrograms
{
    private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
    private const string Uninstall32 = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall";

    public static IReadOnlyList<InstalledProgram> Read(string? userSid = null)
    {
        var list = new List<InstalledProgram>();
        using var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        Collect(hklm, Uninstall, false, list);
        Collect(hklm, Uninstall32, false, list);
        if (userSid is not null)
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            using var user = users.OpenSubKey(userSid);
            if (user is not null) Collect(user, Uninstall, true, list);
        }
        return list.GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void Collect(RegistryKey root, string path, bool perUser, List<InstalledProgram> list)
    {
        try
        {
            using var key = root.OpenSubKey(path);
            if (key is null) return;
            foreach (var sub in key.GetSubKeyNames())
            {
                using var app = key.OpenSubKey(sub);
                if (app?.GetValue("DisplayName") is not string name || string.IsNullOrWhiteSpace(name)) continue;
                if (app.GetValue("SystemComponent") is int sc && sc == 1) continue; // hidden components
                list.Add(new InstalledProgram(name.Trim(), app.GetValue("DisplayVersion")?.ToString(), app.GetValue("Publisher")?.ToString(), perUser));
            }
        }
        catch (Exception)
        {
            // unreadable key: skip
        }
    }
}
