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
/// and are listed instead), <c>--apply-recommended</c> applies the Overview's recommended list for the profile
/// (<c>--profile &lt;id&gt;</c>, else the saved one), <c>--export &lt;file&gt;</c> writes the changes file the Changes
/// page exports, and <c>--undo-all</c> undoes every change of this app. An apply creates a restore point
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
    /// <summary>The start was refused before anything ran: a profiler variable, or an unsafe data or runtime folder.</summary>
    public const int StartRefused = 8;
    /// <summary>--profile names a profile this version does not know, or one this PC does not offer (a laptop profile on a desktop).</summary>
    public const int UnknownProfile = 9;

    /// <summary>How long a run waits for a change of another Tunero (the window, another run) to finish.</summary>
    public static TimeSpan LockWait { get; set; } = TimeSpan.FromMinutes(5);

    public static async Task<int> ApplyAsync(string path, AppServices services, string? logPath, bool withoutRestorePoint)
    {
        var lines = new List<string>();
        var code = await Locked(lines, () => ApplyCoreAsync(path, services, withoutRestorePoint, lines));
        Finish("apply", lines, code, logPath);
        return code;
    }

    /// <param name="profileId">The profile named with --profile; it must exist and fit this PC.</param>
    /// <param name="savedProfileId">The profile saved in the app, used without --profile; when this PC no longer offers it (or none was saved), the one that fits this PC best.</param>
    public static async Task<int> ApplyRecommendedAsync(AppServices services, string? profileId, string? savedProfileId, string? logPath, bool withoutRestorePoint)
    {
        var lines = new List<string>();
        var code = await Locked(lines, () => ApplyRecommendedCoreAsync(services, profileId, savedProfileId, withoutRestorePoint, lines));
        Finish("apply recommended", lines, code, logPath);
        return code;
    }

    public static async Task<int> ExportAsync(string path, AppServices services, string? logPath)
    {
        var lines = new List<string>();
        var code = await Locked(lines, () => ExportCoreAsync(path, services, lines));
        Finish("export", lines, code, logPath);
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

        var pc = await ScanAsync(services);
        var plan = ChangeTransfer.Plan(file, services.Catalog, pc.Facts, pc.Applied);
        Log.Info("cli", "apply plan", new { together = plan.Together.Count, oneByOne = plan.OneByOne.Count, plan.AlreadyOn, plan.NotForThisPc, plan.Unknown });
        return await ApplyTogetherAsync(services, pc, plan.Together, plan.OneByOne, withoutRestorePoint, lines,
            (ok, failed, skipped) => Loc.Instance.Format("Cli_Summary", ok, failed, skipped, plan.AlreadyOn, plan.NotForThisPc, plan.Unknown));
    }

    /// <summary>
    /// The Overview's "Apply recommended" without the window: the same plan for the same profile (fixes of problems
    /// first, then what the profile recommends for this PC), the same rule for what applies together, and the rest
    /// listed for the app.
    /// </summary>
    private static async Task<int> ApplyRecommendedCoreAsync(AppServices services, string? profileId, string? savedProfileId, bool withoutRestorePoint, List<string> lines)
    {
        if (!services.Elevation.IsElevated)
        {
            lines.Add(Loc.Instance["Cli_NotElevated"]);
            return NotElevated;
        }
        var pc = await ScanAsync(services);
        var profiles = CatalogData.Current.Profiles;
        var profile = profileId is not null
            ? profiles.Profiles.FirstOrDefault(p => p.Id == profileId && p.IsAvailable(pc.Facts))
            : profiles.Profiles.FirstOrDefault(p => p.Id == savedProfileId && p.IsAvailable(pc.Facts)) ?? profiles.Suggest(pc.Facts);
        if (profile is null)
        {
            lines.Add(Loc.Instance.Format("Cli_UnknownProfile", profileId ?? "", string.Join(", ", profiles.Profiles.Where(p => p.IsAvailable(pc.Facts)).Select(p => p.Id))));
            return UnknownProfile;
        }
        var lang = Loc.Instance.Language;
        lines.Add(Loc.Instance.Format("Cli_Profile", ViewModels.MainViewModel.ProfileName(profile)));
        var plan = Recommendations.Build(Optimizer.Core.Profiles.ProfileView.For(profile, pc.States, pc.Facts),
            Optimizer.Core.Profiles.ProfileView.Findings(profile, pc.Findings));
        Log.Info("cli", "recommended plan", new { profile = profile.Id, items = plan.Items.Count, excluded = plan.Excluded.Count });
        return await ApplyTogetherAsync(services, pc, plan.Items.Select(i => i.Tweak).ToList(), plan.Excluded.Select(i => i.Tweak).ToList(), withoutRestorePoint, lines,
            (ok, failed, skipped) => Loc.Instance.Format("Cli_RecommendedSummary", ok, failed, skipped));
    }

    /// <summary>The changes file of the Changes page's Export, written to <paramref name="path"/>.</summary>
    private static async Task<int> ExportCoreAsync(string path, AppServices services, List<string> lines)
    {
        if (!services.Elevation.IsElevated)
        {
            lines.Add(Loc.Instance["Cli_NotElevated"]);
            return NotElevated;
        }
        var pc = await ScanAsync(services);
        var (file, pcSpecific) = ChangeTransfer.Create(services.Store.All().Select(b => b.TweakId), pc.Applied, services.Catalog,
            Optimizer.Core.Tools.ProgramPriority.Read(services.Context.Registry), AppInfo.Text, services.Os.BuildString);
        try
        {
            // Only a new file when elevated: whoever starts the exe names the path, and the app writes with administrator rights.
            await using var stream = new FileStream(path, DataPaths.ProcessIsElevated ? FileMode.CreateNew : FileMode.Create, FileAccess.Write, FileShare.None);
            await using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            await writer.WriteAsync(ChangeTransfer.Serialize(file));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            lines.Add(Loc.Instance.Format("Cli_FileUnwritable", path, ex.Message));
            return InvalidFile;
        }
        lines.Add(Loc.Instance.Format("Cli_Exported", file.Tweaks.Count + file.Priorities.Count, path, pcSpecific));
        return Done;
    }

    /// <summary>The scan the window runs: the facts decide what applies to this PC and what is on already.</summary>
    private sealed record PcState(Facts Facts, IReadOnlyList<TweakStatus> States, IReadOnlySet<string> Applied, IReadOnlyList<Finding> Findings);

    private static async Task<PcState> ScanAsync(AppServices services)
    {
        var catalog = CatalogData.Current;
        var visible = services.Catalog.Visible.ToList();
        var scan = await new ScanService(catalog, services.Context.Registry, services.BcdValues, services.Engine, visible).RunAsync();
        var facts = FactsBuilder.Build(scan.Profile, scan.Findings, catalog, services.Context.Registry);
        var states = await Task.Run(() => services.Engine.DetectAll(visible, facts));
        var applied = states.Where(s => s.State is TweakState.Applied or TweakState.PendingRestart).Select(s => s.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        return new PcState(facts, states, applied, scan.Findings);
    }

    /// <summary>Applies the batch-safe changes in one go (one restore point) and lists the rest for the app.</summary>
    private static async Task<int> ApplyTogetherAsync(AppServices services, PcState pc, IReadOnlyList<TweakDefinition> together, IReadOnlyList<TweakDefinition> inApp,
        bool withoutRestorePoint, List<string> lines, Func<int, int, int, string> summary)
    {
        var lang = Loc.Instance.Language;
        int ok = 0, failed = 0, skipped = 0;
        if (together.Count > 0)
        {
            var options = new ApplyOptions { ContinueWithoutRestorePoint = withoutRestorePoint };
            var results = await Task.Run(() => services.Engine.ApplyBatchAsync(together, pc.Facts, pc.Applied, options));
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
        foreach (var t in inApp) lines.Add(Loc.Instance.Format("Cli_NeedsApp", ChangeRunner.TitleOf(t, lang)));
        lines.Add(summary(ok, failed, skipped + inApp.Count));
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
