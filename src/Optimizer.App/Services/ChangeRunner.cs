using Optimizer.Core.Actions;
using Optimizer.Core.Docs;
using Optimizer.Core.Logging;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.Services;

/// <summary>
/// One path for every change in the app (tweaks, fixes, startup entries, services, features, device tweaks): exact
/// changes in the confirmation dialog, the restore point decision, apply with backup, undo. Pages only build the
/// TweakDefinition; this class does the rest and reports a result line.
/// </summary>
public sealed class ChangeRunner(AppServices services, IDialogs dialogs, Func<Facts> facts, Func<IReadOnlySet<string>> appliedIds, Func<bool> expertMode)
{
    public event EventHandler<string>? Status;
    public event EventHandler<bool>? BusyChanged;

    /// <summary>Raised after any change was written, so pages refresh their state.</summary>
    public event EventHandler<TweakDefinition>? Changed;

    public string Title(TweakDefinition t)
    {
        var lang = Loc.Instance.Language;
        var title = DocStore.Get(t.DocId, lang)?.Title ?? DocStore.Get(t.DocId, "en")?.Title ?? t.Id;
        return t.Subject is { Length: > 0 } s ? $"{title}: {s}" : title;
    }

    public string Summary(TweakDefinition t)
    {
        var lang = Loc.Instance.Language;
        var page = DocStore.Get(t.DocId, lang) ?? DocStore.Get(t.DocId, "en");
        return page?.Section(DocHeadings.Summary(page.Language))?.Body ?? "";
    }

    /// <summary>Confirms and applies one tweak. Returns true when something was written.</summary>
    public async Task<bool> ApplyAsync(TweakDefinition t)
    {
        var lang = Loc.Instance.Language;
        var labels = Labels.Current;
        var applied = appliedIds();
        var currentFacts = facts();
        var status = services.Engine.Detect(t, currentFacts, applied);
        var badges = new List<string> { labels.Get(lang, $"risk.{t.EffectiveRisk}"), labels.Get(lang, $"reversibility.{t.Reversibility}") };
        if (t.Restart) badges.Add(labels.Get(lang, "badge.restart"));
        if (t.SignOut) badges.Add(labels.Get(lang, "badge.signOut"));
        if (t.Preview) badges.Insert(0, labels.Get(lang, "badge.preview"));
        var warnings = new List<string>();
        if (t.Preview) warnings.Add(labels.Get(lang, "preview.warning"));
        if (t.IsBootCritical) warnings.Add(labels.Get(lang, "undo.bootCritical"));
        warnings.AddRange(status.Blocks.Where(b => b.ReasonKey is not ("block.antiCheat" or "block.expertMode")).Select(b => labels.Get(lang, b.ReasonKey) + (b.Detail ?? "")));
        if (t.EffectiveRisk == Risk.Expert && !expertMode()) warnings.Add(labels.Get(lang, "block.expertMode"));
        var antiCheat = status.Blocks.FirstOrDefault(b => b.ReasonKey == "block.antiCheat") is { } ac ? labels.Get(lang, ac.ReasonKey) + ac.Detail : null;

        var title = Title(t);
        IReadOnlyList<ChangeLine> preview = await Task.Run(() => services.Engine.Preview(t));
        var confirm = dialogs.ConfirmApply(new ConfirmRequest(title, Summary(t), preview, badges, warnings, antiCheat, false));
        if (confirm is null) return false;

        var options = new ApplyOptions { ExpertMode = expertMode(), AcknowledgeAntiCheat = confirm.AcknowledgedAntiCheat };
        Busy(true);
        try
        {
            var result = await services.Engine.ApplyAsync(t, currentFacts, applied, options);
            if (result.Outcome == ApplyOutcome.NeedsRestorePointDecision)
            {
                var choice = dialogs.AskRestorePoint();
                if (choice == RestorePointChoice.Cancel) return false;
                if (choice == RestorePointChoice.Enable) await Task.Run(() => services.RestorePoints.Enable());
                options = new ApplyOptions { ExpertMode = options.ExpertMode, AcknowledgeAntiCheat = options.AcknowledgeAntiCheat, ContinueWithoutRestorePoint = choice == RestorePointChoice.Continue };
                result = await services.Engine.ApplyAsync(t, currentFacts, applied, options);
            }
            Report(ResultText(t, title, result));
            var written = result.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective;
            if (written) Changed?.Invoke(this, t);
            return written;
        }
        catch (Exception ex)
        {
            Log.Error("ui", $"apply {t.Id} failed", ex);
            Report(Loc.Instance.Format("Result_Failed", ex.Message));
            return false;
        }
        finally
        {
            Busy(false);
        }
    }

    /// <summary>Confirms and undoes one tweak from its backup.</summary>
    public async Task<bool> UndoAsync(TweakDefinition t)
    {
        var lang = Loc.Instance.Language;
        var backup = services.Store.Get(t.Id);
        if (backup is null) return false;
        var lines = backup.Entries.Select(e => new ChangeLine(e.Description, e.Applied?.Display ?? "?", e.Original.Display)).ToList();
        var warnings = t.IsBootCritical ? new List<string> { Labels.Current.Get(lang, "undo.bootCritical") } : [];
        var title = Title(t);
        if (dialogs.ConfirmApply(new ConfirmRequest(title, Labels.Current.Get(lang, "undo.reversible"), lines, [], warnings, null, true)) is null) return false;
        Busy(true);
        try
        {
            var result = await Task.Run(() => services.Engine.Revert(t));
            Report(!result.Success ? Loc.Instance.Format("Result_Failed", string.Join("; ", result.Errors))
                : result.AlreadyRevertedByWindows.Count > 0 ? Loc.Instance.Format("Result_UndoneSkipped", title, result.AlreadyRevertedByWindows.Count)
                : Loc.Instance.Format("Result_Undone", title));
            Changed?.Invoke(this, t);
            return result.Success;
        }
        finally
        {
            Busy(false);
        }
    }

    /// <summary>Applies the given set after one confirmation listing every change (Apply recommended).</summary>
    public async Task<IReadOnlyList<BatchItemResult>> ApplyBatchAsync(IReadOnlyList<TweakDefinition> tweaks, string title, string summary)
    {
        if (tweaks.Count == 0) return [];
        var lang = Loc.Instance.Language;
        var preview = await Task.Run(() => tweaks.SelectMany(t => services.Engine.Preview(t).Select(c => c with { Target = $"{Title(t)}\n{c.Target}" })).ToList());
        var badges = new List<string> { Loc.Instance.Format("Rec_Count", tweaks.Count) };
        if (tweaks.Any(t => t.Restart)) badges.Add(Labels.Current.Get(lang, "badge.restart"));
        if (dialogs.ConfirmApply(new ConfirmRequest(title, summary, preview, badges, [], null, false)) is null) return [];

        Busy(true);
        try
        {
            var options = new ApplyOptions { ExpertMode = expertMode() };
            var results = await services.Engine.ApplyBatchAsync(tweaks, facts(), appliedIds(), options);
            if (results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision))
            {
                var choice = dialogs.AskRestorePoint();
                if (choice == RestorePointChoice.Cancel) return results;
                if (choice == RestorePointChoice.Enable) await Task.Run(() => services.RestorePoints.Enable());
                options = new ApplyOptions { ExpertMode = options.ExpertMode, ContinueWithoutRestorePoint = choice == RestorePointChoice.Continue };
                results = await services.Engine.ApplyBatchAsync(tweaks, facts(), appliedIds(), options);
            }
            if (results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision))
            {
                // Enabling System Protection did not lead to a restore point: the batch stopped before changing anything.
                Report(Loc.Instance["Result_NoRestorePoint"]);
                return results;
            }
            var ok = results.Count(r => r.Result.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective);
            var failed = results.Count(r => r.Result.Outcome == ApplyOutcome.Failed);
            var skipped = results.Count - ok - failed;
            Report(Loc.Instance.Format("Result_Batch", ok, failed, skipped));
            if (ok > 0) Changed?.Invoke(this, tweaks[0]);
            return results;
        }
        finally
        {
            Busy(false);
        }
    }

    public static string ResultText(TweakDefinition t, string title, ApplyResult result)
    {
        var lang = Loc.Instance.Language;
        return result.Outcome switch
        {
            ApplyOutcome.Applied => Loc.Instance.Format(t.Restart ? "Result_AppliedRestart" : t.SignOut ? "Result_AppliedSignOut" : "Result_Applied", title),
            ApplyOutcome.AppliedIneffective => Loc.Instance.Format("Result_Ineffective", title),
            ApplyOutcome.NothingToDo => Loc.Instance["Result_NothingToDo"],
            ApplyOutcome.Blocked => string.Join(" ", (result.Blocks ?? []).Select(b => Labels.Current.Get(lang, b.ReasonKey) + (b.Detail ?? ""))),
            // Only reached when a restore point was requested but could not be created: nothing was changed.
            ApplyOutcome.NeedsRestorePointDecision => Loc.Instance["Result_NoRestorePoint"],
            _ => Loc.Instance.Format("Result_Failed", result.Error ?? "?"),
        };
    }

    public void Report(string text) => Status?.Invoke(this, text);

    private void Busy(bool busy) => BusyChanged?.Invoke(this, busy);
}
