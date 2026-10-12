using System.IO;
using System.Text;
using Optimizer.Core.Backup;
using Optimizer.Core.Catalog;
using Optimizer.Core.Findings;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.Services;

/// <summary>
/// Changes without the window, for a fresh install or several PCs:
/// <c>--apply &lt;file&gt;</c> applies an exported changes file with the rules of Import (only what is safe to apply
/// together; previews, Expert, boot-critical and anti-cheat sensitive changes need their own confirmation in the app
/// and are listed instead), and <c>--undo-all</c> undoes every change of this app. An apply creates a restore point
/// first, as the window does, and stops when that is not possible unless <c>--no-restore-point</c> is given. A run waits
/// for a change of another Tunero (<see cref="ChangeLock"/>). The result goes to the session log and, with
/// <c>--log &lt;file&gt;</c>, to a new text file; the exit code says how it went.
/// </summary>
public static class CommandLineApply
{
    public const int Done = 0;
    public const int SomeFailed = 1;
    public const int InvalidFile = 2;
    public const int NotElevated = 3;
    public const int NoRestorePoint = 4;
    public const int Busy = 5;
    public const int Error = 6;
    public const int NotSupported = 7;

    /// <summary>How long a run waits for a change of another Tunero (the window, another run) to finish.</summary>
    public static TimeSpan LockWait { get; set; } = TimeSpan.FromMinutes(5);

    public static async Task<int> ApplyAsync(string path, AppServices services, string? logPath, bool withoutRestorePoint)
    {
        var lines = new List<string>();
        var code = await Locked(lines, () => ApplyCoreAsync(path, services, withoutRestorePoint, lines));
        Finish("apply", lines, code, logPath);
        return code;
    }

    public static async Task<int> UndoAllAsync(AppServices services, string? logPath)
    {
        var lines = new List<string>();
        var code = await Locked(lines, () => UndoCoreAsync(services, lines));
        Finish("undo all", lines, code, logPath);
        return code;
    }

    /// <summary>Runs <paramref name="work"/> as the only change on this PC (<see cref="ChangeLock"/>), or returns <see cref="Busy"/>.</summary>
    private static async Task<int> Locked(List<string> lines, Func<Task<int>> work)
    {
        using var crossProcess = await Task.Run(() => ChangeLock.TryAcquire(LockWait));
        if (crossProcess is null)
        {
            lines.Add(Loc.Instance["Cli_Busy"]);
            return Busy;
        }
        // An unexpected error ends the run with its own exit code and a line in the log file; a run without a window
        // must never wait on an error dialog.
        try
        {
            return await work();
        }
        catch (Exception ex)
        {
            Log.Error("cli", "the run failed", ex);
            lines.Add(Loc.Instance.Format("Cli_Error", ex.Message));
            return Error;
        }
    }

    private static async Task<int> ApplyCoreAsync(string path, AppServices services, bool withoutRestorePoint, List<string> lines)
    {
        if (!services.Elevation.IsElevated)
        {
            lines.Add(Loc.Instance["Cli_NotElevated"]);
            return NotElevated;
        }
        TransferFile? file;
        try
        {
            file = new FileInfo(path).Length > ChangeTransfer.MaxFileBytes ? null : ChangeTransfer.Parse(await File.ReadAllTextAsync(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            lines.Add(Loc.Instance.Format("Cli_FileUnreadable", path, ex.Message));
            return InvalidFile;
        }
        if (file is null)
        {
            lines.Add(Loc.Instance.Format("Cli_FileInvalid", path));
            return InvalidFile;
        }

        // The same scan as the window: the facts decide what applies to this PC and what is on already.
        var catalog = CatalogData.Current;
        var visible = services.Catalog.Visible.ToList();
        var scan = await new ScanService(catalog, services.Context.Registry, services.BcdElements, services.Engine, visible).RunAsync();
        var facts = FactsBuilder.Build(scan.Profile, scan.Findings, catalog, services.Context.Registry);
        var applied = services.Engine.DetectAll(visible, facts)
            .Where(s => s.State is TweakState.Applied or TweakState.PendingRestart).Select(s => s.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        var plan = ChangeTransfer.Plan(file, services.Catalog, facts, applied);
        Log.Info("cli", "apply plan", new { together = plan.Together.Count, oneByOne = plan.OneByOne.Count, plan.AlreadyOn, plan.NotForThisPc, plan.Unknown });

        var lang = Loc.Instance.Language;
        int ok = 0, failed = 0, skipped = 0;
        if (plan.Together.Count > 0)
        {
            var options = new ApplyOptions { ContinueWithoutRestorePoint = withoutRestorePoint };
            var results = await Task.Run(() => services.Engine.ApplyBatchAsync(plan.Together, facts, applied, options));
            if (results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision))
            {
                lines.Add(Loc.Instance["Cli_NoRestorePoint"]);
                return NoRestorePoint;
            }
            foreach (var r in results)
            {
                lines.Add(ChangeRunner.ResultText(r.Tweak, ChangeRunner.TitleOf(r.Tweak, lang), r.Result));
                switch (r.Result.Outcome)
                {
                    case ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective: ok++; break;
                    case ApplyOutcome.Failed: failed++; break;
                    default: skipped++; break;
                }
            }
        }
        foreach (var t in plan.OneByOne) lines.Add(Loc.Instance.Format("Cli_NeedsApp", ChangeRunner.TitleOf(t, lang)));
        lines.Add(Loc.Instance.Format("Cli_Summary", ok, failed, skipped + plan.OneByOne.Count, plan.AlreadyOn, plan.NotForThisPc, plan.Unknown));
        return failed > 0 ? SomeFailed : Done;
    }

    private static async Task<int> UndoCoreAsync(AppServices services, List<string> lines)
    {
        if (!services.Elevation.IsElevated)
        {
            lines.Add(Loc.Instance["Cli_NotElevated"]);
            return NotElevated;
        }
        var results = await Task.Run(() => services.Engine.RevertAll());
        var lang = Loc.Instance.Language;
        var failed = 0;
        foreach (var (tweak, result) in results)
        {
            var title = ChangeRunner.TitleOf(tweak, lang);
            if (result.Success) lines.Add(Loc.Instance.Format("Result_Undone", title));
            else
            {
                failed++;
                lines.Add(Loc.Instance.Format("Result_UndoIncomplete", title, string.Join("; ", result.Errors)));
            }
        }
        lines.Add(Loc.Instance.Format("Cli_UndoSummary", results.Count - failed, failed));
        return failed > 0 ? SomeFailed : Done;
    }

    /// <summary>
    /// Logs every line and writes them to <paramref name="logPath"/>. An elevated run only creates a new file there,
    /// never overwrites one: whoever starts the exe names the path, and the app writes with administrator rights.
    /// </summary>
    private static void Finish(string what, List<string> lines, int code, string? logPath)
    {
        foreach (var line in lines) Log.Info("cli", line);
        Log.Info("cli", $"{what} finished", new { exitCode = code });
        if (logPath is null) return;
        try
        {
            using var file = new FileStream(logPath, DataPaths.ProcessIsElevated ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(file, new UTF8Encoding(false));
            foreach (var line in lines) writer.WriteLine(line);
            writer.WriteLine(Loc.Instance.Format("Cli_ExitCode", code));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            Log.Warn("cli", $"log file not written: {ex.Message}");
        }
    }
}
