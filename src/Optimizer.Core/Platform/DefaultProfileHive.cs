using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// The Default profile's registry hive (C:\Users\Default\NTUSER.DAT), the template Windows copies for every account
/// created later. It is not loaded while Windows runs: a hold loads it under HKU (RegLoadKey, as Microsoft's
/// CopyProfile page does by hand with Load Hive), and the last hold unloads it again, so the file is free when Windows
/// creates a profile. The mount name carries the process id: another Tunero never unloads this one's hive, and a hive
/// left loaded by a process that ended is unloaded before the next load.
/// </summary>
public static class DefaultProfileHive
{
    private const string MountPrefix = "TuneroDefaultProfile-";
    private static readonly string MountName = MountPrefix + Environment.ProcessId;
    private static readonly IntPtr HkeyUsers = new(unchecked((int)0x80000003));
    private static readonly object Gate = new();
    private static int _holds;
    private static bool _loaded;

    /// <summary>The hive file from the profile list (HKLM, administrators only); null when it is not there.</summary>
    public static string? FilePath()
    {
        try
        {
            using var list = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                .OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (list?.GetValue("Default") is not string folder || folder.Length == 0) return null;
            var file = Path.Combine(Environment.ExpandEnvironmentVariables(folder), "NTUSER.DAT");
            return Path.IsPathFullyQualified(file) && File.Exists(file) ? file : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
        {
            Log.Warn("registry", $"Default profile path unreadable: {ex.Message}");
            return null;
        }
    }

    /// <summary>Loads the hive (unless a hold has it loaded already) until the result is disposed.</summary>
    public static IDisposable Hold()
    {
        lock (Gate)
        {
            if (!_loaded) Load();
            _holds++;
        }
        return new Release();
    }

    /// <summary>The loaded hive's root; only inside a hold.</summary>
    internal static RegistryKey OpenRoot(bool writable)
    {
        lock (Gate)
        {
            if (_holds == 0) throw new InvalidOperationException("The Default profile is opened outside a hold.");
        }
        using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
        return users.OpenSubKey(MountName, writable) ?? throw new InvalidOperationException("The Default profile is not loaded.");
    }

    /// <summary>Unloads hives that Tunero processes no longer running left loaded (a crash between load and unload).</summary>
    public static void UnloadLeftovers()
    {
        string[] names;
        try
        {
            using var users = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64);
            names = users.GetSubKeyNames();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            return;
        }
        foreach (var name in names.Where(n => n.StartsWith(MountPrefix, StringComparison.OrdinalIgnoreCase)))
        {
            if (int.TryParse(name.AsSpan(MountPrefix.Length), out var pid) && pid != Environment.ProcessId && IsRunning(pid)) continue;
            if (name.Equals(MountName, StringComparison.OrdinalIgnoreCase) && _loaded) continue;
            Unload(name);
        }
    }

    private static bool IsRunning(int pid)
    {
        try
        {
            using var p = System.Diagnostics.Process.GetProcessById(pid);
            return !p.HasExited;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or Win32Exception)
        {
            return false;
        }
    }

    private static void Load()
    {
        var file = FilePath() ?? throw new InvalidOperationException("The Default profile (NTUSER.DAT) was not found.");
        UnloadLeftovers();
        using var restore = FirmwareExtras.EnablePrivilege("SeRestorePrivilege");
        using var backup = FirmwareExtras.EnablePrivilege("SeBackupPrivilege");
        if (restore is null || backup is null) throw new InvalidOperationException("Loading the Default profile needs administrator rights.");
        var rc = RegLoadKey(HkeyUsers, MountName, file);
        if (rc != 0) throw new InvalidOperationException($"The Default profile could not be loaded: {new Win32Exception(rc).Message}");
        _loaded = true;
        Log.Info("registry", "Default profile loaded", new { file });
    }

    // A key handle that is still open (antivirus, a search indexer) makes the unload fail for a moment: try again.
    private const int UnloadTries = 20;

    private static void Unload(string name)
    {
        for (var i = 1; !TryUnload(name, i == UnloadTries); i++) Thread.Sleep(100);
    }

    /// <summary>One unload attempt; false when it failed and more tries are left.</summary>
    private static bool TryUnload(string name, bool lastTry)
    {
        using var restore = FirmwareExtras.EnablePrivilege("SeRestorePrivilege");
        using var backup = FirmwareExtras.EnablePrivilege("SeBackupPrivilege");
        var rc = RegUnLoadKey(HkeyUsers, name);
        if (rc is 0 or 2 /* ERROR_FILE_NOT_FOUND: not loaded */)
        {
            if (rc == 0) Log.Info("registry", "Default profile unloaded", new { name });
            if (name.Equals(MountName, StringComparison.OrdinalIgnoreCase)) _loaded = false;
            return true;
        }
        if (!lastTry) return false;
        Log.Warn("registry", $"Default profile {name} not unloaded: {new Win32Exception(rc).Message}");
        return true;
    }

    private sealed class Release : IDisposable
    {
        private int _done;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _done, 1) == 1) return;
            lock (Gate)
            {
                if (--_holds > 0) return;
            }
            // Each try takes the gate on its own, so a hold that comes in meanwhile does not wait out the retries:
            // it keeps the hive that is still loaded, and the last release unloads it.
            for (var i = 1; ; i++)
            {
                lock (Gate)
                {
                    if (_holds > 0 || !_loaded || TryUnload(MountName, i == UnloadTries)) return;
                }
                Thread.Sleep(100);
            }
        }
    }

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] private static extern int RegLoadKey(IntPtr hKey, string subKey, string file);
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)] private static extern int RegUnLoadKey(IntPtr hKey, string subKey);
}
