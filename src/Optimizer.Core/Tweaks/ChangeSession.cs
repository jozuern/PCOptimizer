using Optimizer.Core.Backup;
using Optimizer.Core.Catalog;
using Optimizer.Core.Findings;

namespace Optimizer.Core.Tweaks;

/// <summary>What to do when Windows cannot create the restore point a change asks for.</summary>
public enum RestorePointChoice { Enable, Continue, Cancel }

/// <summary>A set applied together: every result, and the counts the result line reports.</summary>
public sealed record BatchOutcome(IReadOnlyList<BatchItemResult> Results)
{
    /// <summary>No restore point and the change did not go on: nothing was changed.</summary>
    public bool StoppedForRestorePoint => Results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision);

    public int Applied => Results.Count(r => r.Result.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective);
    public int Failed => Results.Count(r => r.Result.Outcome == ApplyOutcome.Failed);
    public int Skipped => Results.Count - Applied - Failed;
    public bool BackupsNotUpdated => Results.Any(r => r.Result.BackupNotUpdated);

    public static readonly BatchOutcome Cancelled = new([]);
}

/// <summary>Undo of every change: each tweak with its result, and the counts.</summary>
public sealed record UndoAllOutcome(IReadOnlyList<(TweakDefinition Tweak, RevertResult Result)> Results)
{
    public int Undone => Results.Count(r => r.Result.Success);
    public int Failed => Results.Count - Undone;
}

/// <summary>The PC as a scan sees it: the facts that decide what fits, the state of each tweak, the findings.</summary>
public sealed record PcSnapshot(Facts Facts, IReadOnlyList<TweakStatus> States, IReadOnlySet<string> Applied, IReadOnlyList<Finding> Findings);

/// <summary>
/// The steps every change takes, for the window and the command line alike: apply one tweak or a set, and when Windows
/// cannot make the restore point, ask <c>decide</c> (a dialog in the window, a switch on the command line), turn System
/// Protection on if chosen and try again; undo one tweak or all. What differs between the two is only how a decision is
/// made and how a result is shown. Both hold <see cref="Platform.ChangeLock"/> around it, so two copies of Tunero never
/// change the PC at the same time.
/// </summary>
public sealed class ChangeSession(TweakEngine engine, IRestorePoints restorePoints)
{
    /// <summary>Applies one tweak. Null when <paramref name="decide"/> cancelled at the restore point question.</summary>
    public async Task<ApplyResult?> ApplyAsync(TweakDefinition t, Facts facts, IReadOnlySet<string> applied, ApplyOptions options, Func<RestorePointChoice> decide)
    {
        var result = await Task.Run(() => engine.ApplyAsync(t, facts, applied, options));
        if (result.Outcome != ApplyOutcome.NeedsRestorePointDecision) return result;
        return await AfterDecision(options, decide) is { } again ? await Task.Run(() => engine.ApplyAsync(t, facts, applied, again)) : null;
    }

    /// <summary>
    /// Applies a set with one restore point. <see cref="BatchOutcome.Cancelled"/> when <paramref name="decide"/> cancelled;
    /// <see cref="BatchOutcome.StoppedForRestorePoint"/> when even after the decision no restore point came of it.
    /// </summary>
    public async Task<BatchOutcome> ApplyBatchAsync(IReadOnlyList<TweakDefinition> tweaks, Facts facts, IReadOnlySet<string> applied, ApplyOptions options,
        Func<RestorePointChoice> decide)
    {
        if (tweaks.Count == 0) return BatchOutcome.Cancelled;
        var results = await Task.Run(() => engine.ApplyBatchAsync(tweaks, facts, applied, options));
        if (!results.Any(r => r.Result.Outcome == ApplyOutcome.NeedsRestorePointDecision)) return new BatchOutcome(results);
        // Nothing was changed yet: a cancel ends here like a cancelled confirmation.
        if (await AfterDecision(options, decide) is not { } again) return BatchOutcome.Cancelled;
        return new BatchOutcome(await Task.Run(() => engine.ApplyBatchAsync(tweaks, facts, applied, again)));
    }

    /// <summary>The options for the second try after the restore point decision; null for Cancel.</summary>
    private async Task<ApplyOptions?> AfterDecision(ApplyOptions options, Func<RestorePointChoice> decide)
    {
        var choice = decide();
        if (choice == RestorePointChoice.Cancel) return null;
        if (choice == RestorePointChoice.Enable) await Task.Run(() => restorePoints.Enable());
        return new ApplyOptions
        {
            ExpertMode = options.ExpertMode,
            AcknowledgeAntiCheat = options.AcknowledgeAntiCheat,
            ContinueWithoutRestorePoint = choice == RestorePointChoice.Continue,
        };
    }

    /// <summary>
    /// The re-apply task's run (<see cref="ReapplyTask"/>): the changes the Overview lists as reset, the ones "Apply
    /// again" offers (no Expert, boot-critical or anti-cheat sensitive change), applied again in one go. A restore point
    /// is made when Windows can; without one the run goes on, since the user chose these changes before and nobody is
    /// there to ask. <see cref="BatchOutcome.Cancelled"/> when nothing was reset.
    /// </summary>
    public async Task<BatchOutcome> ReapplyResetAsync(PcSnapshot pc)
    {
        var known = pc.States.ToDictionary(s => s.Tweak.Id, s => s.State, StringComparer.Ordinal);
        var reset = await Task.Run(() => engine.CheckDrift(pc.Facts, known).Select(d => d.Tweak).Where(t => t.IsBatchSafe).ToList());
        if (reset.Count == 0) return BatchOutcome.Cancelled;
        return await ApplyBatchAsync(reset, pc.Facts, pc.Applied, new ApplyOptions { ContinueWithoutRestorePoint = true }, () => RestorePointChoice.Continue);
    }

    public Task<RevertResult> UndoAsync(TweakDefinition t) => Task.Run(() => engine.Revert(t));

    public async Task<UndoAllOutcome> UndoAllAsync() => new(await Task.Run(engine.RevertAll));

    /// <summary>The scan the window runs, without the window: findings, facts and the state of every visible tweak.</summary>
    public static async Task<PcSnapshot> ScanAsync(CatalogData catalog, TweakCatalog tweaks, TweakEngine engine, Actions.IRegistryRoots registry,
        Func<IReadOnlyDictionary<string, string>?> bcd)
    {
        var visible = tweaks.Visible.ToList();
        var scan = await new ScanService(catalog, registry, bcd, engine, visible).RunAsync();
        var facts = FactsBuilder.Build(scan.Profile, scan.Findings, catalog, registry);
        var states = await Task.Run(() => engine.DetectAll(visible, facts));
        var applied = states.Where(s => s.State is TweakState.Applied or TweakState.PendingRestart).Select(s => s.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        return new PcSnapshot(facts, states, applied, scan.Findings);
    }
}
