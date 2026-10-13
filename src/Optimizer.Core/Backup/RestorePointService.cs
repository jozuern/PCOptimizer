using System.Management;
using Optimizer.Core.Actions;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Backup;

public interface IRestorePoints
{
    /// <summary>System Protection on the system drive: true/false, null = unknown.</summary>
    bool? IsEnabled();

    /// <summary>Turns System Protection on for the system drive with a size cap.</summary>
    void Enable(int maxPercent = 5);

    Task<bool> CreateAsync(string description);
}

/// <summary>
/// System Restore via WMI (root\default:SystemRestore). Restore points are the secondary safety net; the JSON backup is
/// primary. The 24 h creation limit is lifted by the hidden tweak "system.restorePointFrequency".
/// </summary>
public sealed class RestorePointService(IProcessRunner processes) : IRestorePoints
{
    public bool? IsEnabled()
    {
        if (Reg.HklmInt(@"SOFTWARE\Policies\Microsoft\Windows NT\SystemRestore", "DisableSR") == 1) return false;
        try
        {
            var cfg = Hardware.Wmi.Query("SELECT RPSessionInterval FROM SystemRestoreConfig", @"root\default").FirstOrDefault();
            if (cfg is not null) return Hardware.Wmi.Int(cfg, "RPSessionInterval") is > 0;
        }
        catch (Exception ex)
        {
            Log.Warn("restore", $"SystemRestoreConfig unavailable: {ex.Message}");
        }
        return Reg.HklmInt(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "RPSessionInterval") is { } v ? v > 0 : null;
    }

    public void Enable(int maxPercent = 5)
    {
        var drive = Path.GetPathRoot(Environment.SystemDirectory)!.TrimEnd('\\');
        using var cls = new ManagementClass(new ManagementScope(@"root\default"), new ManagementPath("SystemRestore"), null);
        var result = cls.InvokeMethod("Enable", [drive + "\\"]);
        Log.Info("restore", "enable system protection", new { drive, result });
        processes.Run("vssadmin.exe", $"resize shadowstorage /for={drive} /on={drive} /maxsize={maxPercent}%");
    }

    /// <summary>
    /// Windows makes a new restore point only when the newest one is older than SystemRestorePointCreationFrequency
    /// minutes (1440 when the value is not set, 0 = always); within that time it skips the request and still reports
    /// success. The engine would take that for a restore point made for this change, so the limit is checked first.
    /// (The new point shows in the list only some seconds after the call, so it cannot be checked afterwards.)
    /// </summary>
    public Task<bool> CreateAsync(string description) => Task.Run(() =>
    {
        try
        {
            if (WithinCreationLimit() is { } newest)
            {
                Log.Warn("restore", $"no new restore point: Windows makes one only after the creation limit, and the newest is from {newest:yyyy-MM-dd HH:mm}");
                return false;
            }
            using var cls = new ManagementClass(new ManagementScope(@"root\default"), new ManagementPath("SystemRestore"), null);
            // 12 = MODIFY_SETTINGS, 100 = BEGIN_SYSTEM_CHANGE
            var result = cls.InvokeMethod("CreateRestorePoint", [description, 12u, 100u]);
            var ok = Convert.ToUInt32(result) == 0;
            Log.Info("restore", "create restore point", new { description, result });
            return ok;
        }
        catch (Exception ex)
        {
            Log.Error("restore", "restore point failed", ex);
            return false;
        }
    });

    /// <summary>The time of the newest restore point when Windows would skip a new one now; null when it would not (or that is unknown).</summary>
    private static DateTime? WithinCreationLimit()
    {
        var minutes = Reg.HklmInt(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "SystemRestorePointCreationFrequency") ?? 1440;
        if (minutes <= 0) return null;
        try
        {
            var newest = Hardware.Wmi.Query("SELECT CreationTime FROM SystemRestore", @"root\default")
                .Select(r => Hardware.Wmi.Str(r, "CreationTime")).Where(t => t.Length > 0).Select(ManagementDateTimeConverter.ToDateTime).DefaultIfEmpty(DateTime.MinValue).Max();
            return DateTime.Now - newest < TimeSpan.FromMinutes(minutes) ? newest : null;
        }
        catch (Exception ex)
        {
            Log.Warn("restore", $"restore points not listed: {ex.Message}");
            return null;
        }
    }
}
