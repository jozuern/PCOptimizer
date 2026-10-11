using Optimizer.Core.Actions;
using Optimizer.Core.Backup;
using Optimizer.Core.Docs;
using Optimizer.Core.Logging;
using Optimizer.Core.Tweaks;
using Wpf.Ui.Controls;

namespace Optimizer.App.Services;

/// <summary>A result line and how it is shown (success, caution, error).</summary>
public sealed record ResultMessage(string Text, InfoBarSeverity Severity);

/// <summary>
/// One path for every change in the app (tweaks, fixes, startup entries, services, features, device tweaks): exact
/// changes in the confirmation dialog, the restore point decision, apply with backup, undo. Pages only build the
/// TweakDefinition; this class does the rest and reports a result line.
/// <para>
/// One change at a time (<see cref="ChangeGate"/>): the gate is taken before the preview, so a second click cannot
/// stack a second confirmation built from the same, soon stale state. Detection, preview, apply and undo run off the UI
/// thread (DISM, bcdedit, powercfg and PowerShell can take minutes); the dialogs stay on it. Every failure is caught,
/// logged and reported with a text that says what is left.
/// </para>
/// </summary>
public sealed class ChangeRunner(AppServices services, IDialogs dialogs, Func<Facts> facts, Func<IReadOnlySet<string>> appliedIds, Func<bool> expertMode)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    public event EventHandler<ResultMessage>? Status;
    public event EventHandler<bool>? BusyChanged;

    /// <summary>Raised after any change was written or undone (also when it failed half-way), so pages refresh their state.</summary>
    public event EventHandler<TweakDefinition?>? Changed;

    public string Title(TweakDefinition t) => TitleOf(t, Loc.Instance.Language);

    // HKU\<SID of this process>: the hive that this process sees as HKCU.
    private static readonly string? OwnHive = System.Security.Principal.WindowsIdentity.GetCurrent().User?.Value is { } sid ? $@"HKU\{sid}" : null;

    /// <summary>
    /// A registry path of the signed-in user as people know it ("HKCU\Software\...") instead of "HKU\S-1-5-21-...\Software\...".
    /// Only when this app runs as that user: with a separate administrator account the SID stays, since HKCU would be
    /// that account's hive.
    /// </summary>
    public static string ShortTarget(string target) =>
        OwnHive is { } own && target.StartsWith(own, StringComparison.OrdinalIgnoreCase) && (target.Length == own.Length || target[own.Length] == '\\')
            ? "HKCU" + target[own.Length..]
            : target;

    public static IReadOnlyList<ChangeLine> ShortTargets(IReadOnlyList<ChangeLine> lines) => lines.Select(c => c with { Target = ShortTarget(c.Target) }).ToList();

    /// <summary>The explanation page's title, with the subject for tweaks that share a page (DNS presets, services).</summary>
    public static string TitleOf(TweakDefinition t, string lang)
    {
        var title = DocStore.Get(t.DocId, lang)?.Title ?? DocStore.Get(t.DocId, "en")?.Title ?? t.Id;
        return t.Subject is { Length: > 0 } s ? $"{title}: {s}" : title;
    }

    /// <summary>
    /// A block reason as text. Conflicts and requirements name another tweak by id; the user sees its title instead.
    /// </summary>
    public static string BlockText(Block b, string lang)
    {
        var detail = b.Detail ?? "";
        if (b.ReasonKey is "block.conflict" or "block.requires" && TweakCatalog.Current.Get(detail) is { } other) detail = TitleOf(other, lang);
        return Labels.Current.Get(lang, b.ReasonKey) + detail;
    }

    public string Summary(TweakDefinition t)
    {
        var lang = Loc.Instance.Language;
        var page = DocStore.Get(t.DocId, lang) ?? DocStore.Get(t.DocId, "en");
        return page?.Section(DocHeadings.Summary(page.Language))?.Body ?? "";
    }

    /// <summary>
    /// Runs <paramref name="work"/> as the only change. Returns <paramref name="whenNotRun"/> when another change, a
    /// scan or an update is running, and after an unexpected error.
    /// </summary>
    private async Task<T> ExclusiveAsync<T>(string what, Func<Task<T>> work, T whenNotRun)
    {
        if (!ChangeGate.Instance.CanChange || !_gate.Wait(0))
        {
            Report(Loc.Instance["Change_Busy"], InfoBarSeverity.Warning);
            return whenNotRun;
        }
        Busy(true);
        try
        {
            return await work();
        }
        catch (Exception ex)
        {
            Log.Error("ui", $"{what} failed", ex);
            Report(Loc.Instance.Format("Result_Error", ex.Message), InfoBarSeverity.Error);
            return whenNotRun;
        }
        finally
        {
            Busy(false);
            _gate.Release();
        }
    }

    /// <summary>
    /// A page operation that changes the system outside the tweak engine (remove an app, clean up, uninstall, a quick
    /// fix): runs as the only change, like an apply, and an unexpected error is logged and shown. False when it did not
    /// run or failed.
    /// </summary>
    public Task<bool> RunExclusiveAsync(string what, Func<Task> work) => ExclusiveAsync(what, async () =>
    {
        await work();
        return true;
    }, false);

    /// <summary>Confirms and applies one tweak. Returns true when something was written.</summary>
    public Task<bool> ApplyAsync(TweakDefinition t) => ExclusiveAsync($"apply {t.Id}", async () =>
    {
        var applied = appliedIds();
        var currentFacts = facts();
        var status = await Task.Run(() => services.Engine.Detect(t, currentFacts, applied));
        var request = ConfirmRequestFor(t, status, expertMode(), Title(t), Summary(t), await Task.Run(() => services.Engine.Preview(t)));
        var confirm = dialogs.ConfirmApply(request);
        if (confirm is null) return false;

        var options = new ApplyOptions { ExpertMode = expertMode(), AcknowledgeAntiCheat = confirm.AcknowledgedAntiCheat };
        var result = await Task.Run(() => services.Engine.ApplyAsync(t, currentFacts, applied, options));
        if (result.Outcome == ApplyOutcome.NeedsRestorePointDecision)
        {
            var choice = dialogs.AskRestorePoint();
            if (choice == RestorePointChoice.Cancel) return false;
            if (choice == RestorePointChoice.Enable) await Task.Run(() => services.RestorePoints.Enable());
            options = new ApplyOptions { ExpertMode = options.ExpertMode, AcknowledgeAntiCheat = options.AcknowledgeAntiCheat, ContinueWithoutRestorePoint = choice == RestorePointChoice.Continue };
            result = await Task.Run(() => services.Engine.ApplyAsync(t, currentFacts, applied, options));
        }
        Report(ResultText(t, request.Title, result), SeverityOf(result));
        var written = result.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective;
        // A failed apply can leave a backup behind (a rollback that did not work): the pages show it as a change.
        if (written || result.Outcome == ApplyOutcome.Failed) Changed?.Invoke(this, t);
        return written;
    }, false);

    /// <summary>The confirmation dialog of one tweak (also used by the --confirm-shot developer switch).</summary>
    public static ConfirmRequest ConfirmRequestFor(TweakDefinition t, TweakStatus status, bool expert, string title, string summary, IReadOnlyList<ChangeLine> preview)
    {
        var lang = Loc.Instance.Language;
        var labels = Labels.Current;
        var badges = new List<string> { labels.Get(lang, $"risk.{t.EffectiveRisk}"), labels.Get(lang, $"reversibility.{t.Reversibility}") };
        if (t.Restart) badges.Add(labels.Get(lang, "badge.restart"));
        if (t.SignOut) badges.Add(labels.Get(lang, "badge.signOut"));
        if (t.Preview) badges.Insert(0, labels.Get(lang, "badge.preview"));
        if (t.Undocumented) badges.Add(labels.Get(lang, "badge.undocumented"));
        var warnings = new List<string>();
        if (t.Preview) warnings.Add(labels.Get(lang, "preview.warning"));
        if (t.IsBootCritical) warnings.Add(labels.Get(lang, "undo.bootCritical"));
        warnings.AddRange(status.Blocks.Where(b => b.ReasonKey is not ("block.antiCheat" or "block.expertMode")).Select(b => BlockText(b, lang)));
        if (t.EffectiveRisk == Risk.Expert && !expert) warnings.Add(labels.Get(lang, "block.expertMode"));
        var antiCheat = status.Blocks.FirstOrDefault(b => b.ReasonKey == "block.antiCheat") is { } ac ? labels.Get(lang, ac.ReasonKey) + ac.Detail : null;
        return new ConfirmRequest(title, summary, preview, badges, warnings, antiCheat, false);
    }

    /// <summary>Confirms and undoes one tweak from its backup.</summary>
    public Task<bool> UndoAsync(TweakDefinition t) => ExclusiveAsync($"undo {t.Id}", async () =>
    {
        var lang = Loc.Instance.Language;
        TweakBackup? backup;
        try
        {
            backup = services.Store.Get(t.Id);
        }
        catch (BackupUnreadableException ex)
        {
            Report(Loc.Instance.Format("Result_Error", ex.Message), InfoBarSeverity.Error);
            return false;
        }
        if (backup is null)
        {
            Report(services.Store.IsDamaged(t.Id)
                ? Labels.Current.Get(lang, "block.backupDamaged") + services.Store.DamagedFile(t.Id)
                : Loc.Instance["Tweak_NoBackup"], InfoBarSeverity.Warning);
            return false;
        }
        var lines = backup.Entries.Select(e => new ChangeLine(e.Description, e.Applied?.Display ?? "?", e.Original.Display)).ToList();
        var warnings = t.IsBootCritical ? new List<string> { Labels.Current.Get(lang, "undo.bootCritical") } : [];
        var title = Title(t);
        if (dialogs.ConfirmApply(new ConfirmRequest(title, Labels.Current.Get(lang, "undo.reversible"), lines, [], warnings, null, true)) is null) return false;
        try
        {
            var result = await Task.Run(() => services.Engine.Revert(t));
            if (!result.Success) Report(Loc.Instance.Format("Result_UndoIncomplete", title, string.Join("; ", result.Errors)), InfoBarSeverity.Error);
            else if (result.AlreadyRevertedByWindows.Count > 0) Report(Loc.Instance.Format("Result_UndoneSkipped", title, result.AlreadyRevertedByWindows.Count));
            else Report(Loc.Instance.Format("Result_Undone", title), InfoBarSeverity.Success);
            return result.Success;
        }
        finally
        {
            // Also after a failure: part of it may be undone, and the Changes page must show what is left.
            Changed?.Invoke(this, t);
        }
    }, false);

    /// <summary>Undoes every change this app made (one confirmation).</summary>
    public Task<bool> UndoAllAsync() => ExclusiveAsync("undo all", async () =>
    {
        var count = services.Store.All().Count;
        if (count == 0 || !dialogs.ConfirmUndoAll(count)) return false;
        try
        {
            var results = await Task.Run(() => services.Engine.RevertAll());
            var failed = results.Where(r => !r.Result.Success).ToList();
            if (failed.Count == 0) Report(Loc.Instance.Format("Result_UndoAll", results.Count), InfoBarSeverity.Success);
            else Report(Loc.Instance.Format("Result_UndoAllIncomplete", results.Count - failed.Count, failed.Count), InfoBarSeverity.Error);
            return failed.Count == 0;
        }
        finally
        {
            Changed?.Invoke(this, null);
        }
    }, false);

    /// <summary>Applies the given set after one confirmation listing every change (Apply recommended).</summary>
    public Task<IReadOnlyList<BatchItemResult>> ApplyBatchAsync(IReadOnlyList<TweakDefinition> tweaks, string title, string summary) =>
        tweaks.Count == 0 ? Task.FromResult<IReadOnlyList<BatchItemResult>>([]) : ExclusiveAsync<IReadOnlyList<BatchItemResult>>("apply batch", async () =>
    {
        var lang = Loc.Instance.Language;
        var preview = await Task.Run(() => tweaks.SelectMany(t => services.Engine.Preview(t).Select(c => c with { Target = $"{Title(t)}\n{c.Target}" })).ToList());
        var badges = new List<string> { Loc.Instance.Format("Rec_Count", tweaks.Count) };
        if (tweaks.Any(t => t.Restart)) badges.Add(Labels.Current.Get(lang, "badge.restart"));
        if (dialogs.ConfirmApply(new ConfirmRequest(title, summary, preview, badges, [], null, false)) is null) return [];

        var options = new ApplyOptions { ExpertMode = expertMode() };
        var currentFacts = facts();
        var applied = appliedIds();
        IReadOnlyList<BatchItemResult> results = await Task.Run(() => services.Engine.ApplyBatchAsync(tweaks, currentFacts, applied, options));
        if (results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision))
        {
            var choice = dialogs.AskRestorePoint();
            if (choice == RestorePointChoice.Cancel) return results;
            if (choice == RestorePointChoice.Enable) await Task.Run(() => services.RestorePoints.Enable());
            options = new ApplyOptions { ExpertMode = options.ExpertMode, ContinueWithoutRestorePoint = choice == RestorePointChoice.Continue };
            results = await Task.Run(() => services.Engine.ApplyBatchAsync(tweaks, currentFacts, applied, options));
        }
        if (results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision))
        {
            // Enabling System Protection did not lead to a restore point: the batch stopped before changing anything.
            Report(Loc.Instance["Result_NoRestorePoint"], InfoBarSeverity.Warning);
            return results;
        }
        var ok = results.Count(r => r.Result.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective);
        var failed = results.Count(r => r.Result.Outcome == ApplyOutcome.Failed);
        var skipped = results.Count - ok - failed;
        Report(Loc.Instance.Format("Result_Batch", ok, failed, skipped), failed > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        // Several changes at once: null makes the app read everything again (a single page-level item would not).
        if (ok > 0 || failed > 0) Changed?.Invoke(this, tweaks.Count == 1 ? tweaks[0] : null);
        return results;
    }, []);

    public static string ResultText(TweakDefinition t, string title, ApplyResult result)
    {
        var lang = Loc.Instance.Language;
        return result.Outcome switch
        {
            ApplyOutcome.Applied => Loc.Instance.Format(t.Restart ? "Result_AppliedRestart" : t.SignOut ? "Result_AppliedSignOut" : "Result_Applied", title),
            ApplyOutcome.AppliedIneffective => Loc.Instance.Format("Result_Ineffective", title),
            ApplyOutcome.NothingToDo => Loc.Instance["Result_NothingToDo"],
            ApplyOutcome.Blocked => string.Join(" ", (result.Blocks ?? []).Select(b => BlockText(b, lang))),
            // Only reached when a restore point was requested but could not be created: nothing was changed.
            ApplyOutcome.NeedsRestorePointDecision => Loc.Instance["Result_NoRestorePoint"],
            // The engine keeps the backup when part of the change could not be rolled back: then say so.
            _ when result.LeftChanged => Loc.Instance.Format("Result_FailedPartly", title, result.Error),
            _ => Loc.Instance.Format("Result_Failed", result.Error ?? "?"),
        };
    }

    public void Report(string text, InfoBarSeverity severity = InfoBarSeverity.Informational) => Status?.Invoke(this, new ResultMessage(text, severity));

    private static InfoBarSeverity SeverityOf(ApplyResult result) => result.Outcome switch
    {
        ApplyOutcome.Applied => InfoBarSeverity.Success,
        ApplyOutcome.AppliedIneffective or ApplyOutcome.Blocked or ApplyOutcome.NeedsRestorePointDecision => InfoBarSeverity.Warning,
        ApplyOutcome.Failed => InfoBarSeverity.Error,
        _ => InfoBarSeverity.Informational,
    };

    private void Busy(bool busy)
    {
        ChangeGate.Instance.Busy = busy;
        BusyChanged?.Invoke(this, busy);
    }
}
