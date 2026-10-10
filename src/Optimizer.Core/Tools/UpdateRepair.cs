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

    /// <summary>
    /// Each service step is judged by the service's state afterwards: net.exe returns 2 for "not started" and "already
    /// running", but also for "access denied" and "could not be started", so its exit code says little.
    /// </summary>
    public static IReadOnlyList<RepairStep> Run(IProcessRunner processes, IServiceManager services, IProgress<RepairStep>? progress = null,
        string? windowsDir = null, DateTime? now = null, TimeSpan? settle = null, Action<TimeSpan>? wait = null)
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
            var (code, output) = processes.Run("net.exe", $"stop {s} /y", TimeSpan.FromMinutes(2));
            var stopped = WaitFor(services, s, running: false, settle ?? TimeSpan.FromSeconds(15));
            Add(new RepairStep($"repair.stop.{s}", stopped, stopped ? null : Detail(code, output)));
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
            var running = WaitFor(services, s, running: true, settle ?? TimeSpan.FromSeconds(15));
            Add(new RepairStep($"repair.start.{s}", running, running ? null : Detail(code, output)));
        }
        return steps;
    }

    private static string Detail(int code, string output) => output.Trim() is { Length: > 0 } text ? text : $"exit code {code}";

    /// <summary>Waits until the service is in the wanted state (a service can still be stopping or starting).</summary>
    private static bool WaitFor(IServiceManager services, string name, bool running, TimeSpan timeout)
    {
        var until = DateTime.UtcNow + timeout;
        while (true)
        {
            if (services.IsRunning(name) == running) return true;
            if (DateTime.UtcNow >= until) return false;
            Thread.Sleep(250);
        }
    }
}
