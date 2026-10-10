using Optimizer.Core.Actions;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Tools;

public sealed record RepairStep(string Key, bool Ok, string? Detail);

/// <summary>
/// Windows Update component reset (the documented manual procedure): stop the update services, rename the
/// SoftwareDistribution and catroot2 folders (Windows rebuilds them), start the services again. Renamed folders are
/// kept with a date suffix, so nothing is deleted. Downloads and update history in Settings start over.
/// </summary>
public static class UpdateRepair
{
    public static readonly string[] Services = ["wuauserv", "bits", "cryptsvc", "msiserver"];

    /// <summary>Attempts to rename catroot2; Cryptographic Services is stopped again before each retry.</summary>
    public const int CatrootAttempts = 3;

    public static IReadOnlyList<RepairStep> Run(IProcessRunner processes, IProgress<RepairStep>? progress = null, string? windowsDir = null, DateTime? now = null,
        Action<TimeSpan>? wait = null)
    {
        wait ??= Thread.Sleep;
        var steps = new List<RepairStep>();
        void Add(RepairStep s)
        {
            steps.Add(s);
            progress?.Report(s);
            Log.Info("updaterepair", s.Key, new { s.Ok, s.Detail });
        }

        var win = windowsDir ?? Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var suffix = $".old-{now ?? DateTime.Now:yyyyMMdd-HHmmss}";

        foreach (var s in Services)
        {
            // "net stop" returns 2 when the service is not running, which is fine here.
            var (code, output) = processes.Run("net.exe", $"stop {s} /y", TimeSpan.FromMinutes(2));
            Add(new RepairStep($"repair.stop.{s}", code is 0 or 2, code is 0 or 2 ? null : output.Trim()));
        }

        foreach (var folder in new[] { Path.Combine(win, "SoftwareDistribution"), Path.Combine(win, @"System32\catroot2") })
        {
            var key = $"repair.rename.{Path.GetFileName(folder).ToLowerInvariant()}";
            var catroot = key == "repair.rename.catroot2";
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        Directory.Move(folder, folder + suffix);
                        Add(new RepairStep(key, true, folder + suffix));
                    }
                    else
                    {
                        Add(new RepairStep(key, true, null));
                    }
                    break;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Cryptographic Services is trigger-started: any signature check starts it again, and it holds catroot2
                    // (seen in the VM test: "Access denied" three seconds after the stop).
                    if (catroot && attempt < CatrootAttempts)
                    {
                        processes.Run("net.exe", "stop cryptsvc /y", TimeSpan.FromMinutes(2));
                        wait(TimeSpan.FromSeconds(2));
                        continue;
                    }
                    Add(new RepairStep(key, false, ex.Message));
                    break;
                }
            }
        }

        foreach (var s in Services.Reverse())
        {
            if (s == "msiserver") continue; // Windows Installer starts on demand
            var (code, output) = processes.Run("net.exe", $"start {s}", TimeSpan.FromMinutes(2));
            // 2 = already running
            Add(new RepairStep($"repair.start.{s}", code is 0 or 2, code is 0 or 2 ? null : output.Trim()));
        }
        return steps;
    }
}
