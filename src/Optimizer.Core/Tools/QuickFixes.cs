using Optimizer.Core.Actions;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tools;

/// <summary>One documented command of a quick fix (a Windows tool in System32, run with a cleaned environment).</summary>
/// <param name="AlwaysRun">Runs even when an earlier step failed (ipconfig /renew after a /release that failed for one adapter).</param>
/// <param name="OkCodes">Exit codes that mean success; null means only 0.</param>
public sealed record QuickFixStep(string File, string Arguments, TimeSpan? Timeout = null, bool AlwaysRun = false, IReadOnlyList<int>? OkCodes = null);

/// <summary>
/// A repair that runs documented Windows commands and changes nothing that needs an undo: it restarts a service or
/// device, renews or resets network state, or rebuilds a Windows cache. Text lives in the app's string resources
/// (Quick_{Id}, Quick_{Id}Hint, Quick_{Id}Confirm, Quick_{Id}Done).
/// </summary>
public sealed record QuickFix(string Id, bool Confirm, bool NeedsRestart, IReadOnlyList<QuickFixStep> Steps, string Source)
{
    /// <summary>The result text of the tool is shown after a success (chkdsk, reagentc report what they found).</summary>
    public bool ShowOutput { get; init; }
}

public static class QuickFixes
{
    private static QuickFixStep Ps(string command) =>
        new("powershell.exe", $"-NoProfile -NonInteractive -Command \"{command}\"");

    public static IReadOnlyList<QuickFix> All { get; } =
    [
        // ipconfig: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/ipconfig
        new("IpRenew", Confirm: true, NeedsRestart: false,
            [new("ipconfig.exe", "/release"), new("ipconfig.exe", "/renew", TimeSpan.FromMinutes(2), AlwaysRun: true)],
            "https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/ipconfig"),
        // Overwrites the TCP/IP registry keys, same effect as removing and reinstalling TCP/IP; needs a restart.
        new("TcpIpReset", Confirm: true, NeedsRestart: true,
            [new("netsh.exe", "int ip reset \"" + DataPaths.TcpIpResetLog + "\"")],
            "https://learn.microsoft.com/en-us/troubleshoot/windows-server/networking/reset-tcp-ip-net-shell"),
        new("AudioRestart", Confirm: false, NeedsRestart: false, [Ps("Restart-Service -Name Audiosrv -Force")],
            "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/restart-service"),
        new("BluetoothRestart", Confirm: false, NeedsRestart: false, [Ps("Restart-Service -Name bthserv -Force")],
            "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/restart-service"),
        new("SearchRestart", Confirm: false, NeedsRestart: false, [Ps("Restart-Service -Name WSearch -Force")],
            "https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.management/restart-service"),
        // pnputil /restart-device /class needs Windows 11 22H2 or later (the app needs 24H2).
        new("GraphicsRestart", Confirm: true, NeedsRestart: false, [new("pnputil.exe", "/restart-device /class Display")],
            "https://learn.microsoft.com/en-us/windows-hardware/drivers/devtest/pnputil-command-syntax"),
        new("TimeSync", Confirm: false, NeedsRestart: false, [Ps("Start-Service -Name W32Time"), new("w32tm.exe", "/resync")],
            "https://learn.microsoft.com/en-us/windows-server/networking/windows-time-service/windows-time-service-tools-and-settings"),
        new("PerfCounters", Confirm: false, NeedsRestart: false, [new("lodctr.exe", "/r", TimeSpan.FromMinutes(5))],
            "https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/lodctr"),
        // Online scan of the system drive: Windows stays usable, found problems are queued for a fix.
        new("DiskScan", Confirm: false, NeedsRestart: false,
            // chkdsk: 0 no errors, 1 errors found and fixed, 2 no cleanup done; 3 means problems remain (its text says which).
            [new("chkdsk.exe", Path.GetPathRoot(Environment.SystemDirectory)!.TrimEnd('\\') + " /scan", TimeSpan.FromMinutes(60), OkCodes: [0, 1, 2])],
            "https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/chkdsk") { ShowOutput = true },
        new("RecoveryEnable", Confirm: false, NeedsRestart: false, [new("reagentc.exe", "/enable")],
            "https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/reagentc-command-line-options") { ShowOutput = true },
    ];

    public static QuickFix Get(string id) => All.Single(f => f.Id == id);

    /// <summary>
    /// Runs the steps in order. After a failed step (an exit code that is not a success, or a timeout) only the steps
    /// marked <see cref="QuickFixStep.AlwaysRun"/> still run. Output is the text of all steps run.
    /// </summary>
    public static (bool Ok, string Output) Run(IProcessRunner runner, QuickFix fix)
    {
        var output = new List<string>();
        var ok = true;
        foreach (var step in fix.Steps)
        {
            if (!ok && !step.AlwaysRun) continue;
            try
            {
                var (code, text) = runner.Run(step.File, step.Arguments, step.Timeout);
                if (text.Trim() is { Length: > 0 } t) output.Add(t);
                if (!(step.OkCodes ?? [0]).Contains(code)) ok = false;
            }
            catch (TimeoutException ex)
            {
                output.Add(ex.Message);
                ok = false;
            }
        }
        return (ok, string.Join("\n", output));
    }
}
