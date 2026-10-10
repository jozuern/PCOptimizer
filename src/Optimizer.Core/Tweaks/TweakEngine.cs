using System.Text.Json;
using Optimizer.Core.Actions;
using Optimizer.Core.Backup;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Tweaks;

/// <summary>State of a tweak on this PC, as detected from the real values.</summary>
public enum TweakState
{
    Applied,
    NotApplied,
    Partial,
    NotApplicable,
    Unsupported,
    AppliedIneffective,
    RevertedByWindows,
    EnforcedByPolicy,
    PendingRestart,

    /// <summary>Undone, but the restored value takes effect only after the next restart (still reads as applied).</summary>
    UndoPendingRestart,
}

/// <summary>A reason that prevents applying, as a label key plus values for the text.</summary>
public sealed record Block(string ReasonKey, string? Detail = null, bool CanOverride = false);

public sealed record TweakStatus(
    TweakDefinition Tweak,
    TweakState State,
    int Impact,
    IReadOnlyList<string> Effects,
    string? ImpactReasonKey,
    bool Recommended,
    IReadOnlyList<Block> Blocks,
    bool HasBackup)
{
    public bool IsOn => State is TweakState.Applied or TweakState.PendingRestart or TweakState.AppliedIneffective or TweakState.Partial;
}

public sealed class ApplyOptions
{
    /// <summary>Expert mode is on (required for Expert and boot-critical tweaks).</summary>
    public bool ExpertMode { get; init; }

    /// <summary>The user confirmed the anti-cheat warning for this tweak.</summary>
    public bool AcknowledgeAntiCheat { get; init; }

    /// <summary>The user chose to continue although no restore point could be created.</summary>
    public bool ContinueWithoutRestorePoint { get; init; }
}

public enum ApplyOutcome { Applied, AppliedIneffective, Blocked, NeedsRestorePointDecision, Failed, NothingToDo }

/// <param name="LeftChanged">Failed, and part of the change could not be rolled back: the backup is kept for Undo.</param>
public sealed record ApplyResult(ApplyOutcome Outcome, IReadOnlyList<ChangeLine> Changes, string? Error = null, IReadOnlyList<Block>? Blocks = null, bool LeftChanged = false);

public sealed record BatchItemResult(TweakDefinition Tweak, ApplyResult Result);

public sealed record RevertResult(bool Success, IReadOnlyList<string> AlreadyRevertedByWindows, IReadOnlyList<string> Errors);

/// <summary>
/// A change this app made that is no longer in place. <see cref="WindowsUpdatedSince"/>: the Windows version (build and
/// update revision) differs from the one the change was applied on, so an update is the likely cause.
/// </summary>
public sealed record DriftItem(TweakDefinition Tweak, TweakBackup Backup, string? AppliedOn, string Current)
{
    public bool WindowsUpdatedSince => AppliedOn is not null && !string.Equals(AppliedOn, AppliedOn.Contains('.') ? Current : Current.Split('.')[0], StringComparison.Ordinal);
}

/// <summary>
/// Detect -> preflight -> restore point -> backup (first-original) -> apply with per-tweak rollback -> verify -> log
///. Undo restores originals unless Windows already changed the value (feature-update-aware).
/// </summary>
/// <param name="windowsVersion">Full Windows version ("26300.9550"); defaults to the build number.</param>
/// <param name="catalog">The tweak catalog (runtime tweaks are those it does not contain); null = the embedded catalog.</param>
public sealed class TweakEngine(ActionContext ctx, BackupStore store, IRestorePoints restorePoints, string appVersion, int windowsBuild, string? windowsVersion = null,
    TweakCatalog? catalog = null)
{
    private readonly TweakCatalog _catalog = catalog ?? TweakCatalog.Current;

    public string WindowsVersion { get; } = windowsVersion ?? windowsBuild.ToString(System.Globalization.CultureInfo.InvariantCulture);

    public const string RestorePointFrequencyTweak = "system.restorePointFrequency";

    private bool _restorePointDone;

    public ActionContext Context => ctx;
    public BackupStore Store => store;

    // ---------------- detection ----------------

    public IReadOnlyList<TweakStatus> DetectAll(IEnumerable<TweakDefinition> tweaks, Facts facts)
    {
        var list = tweaks.ToList();
        // One read of every backup file for the whole detection instead of two or three per tweak.
        _backups = store.All().ToDictionary(b => b.TweakId, StringComparer.Ordinal);
        try
        {
            var first = list.Select(t => (t, state: DetectState(t, facts))).ToList();
            var applied = first.Where(x => x.state is TweakState.Applied or TweakState.PendingRestart).Select(x => x.t.Id).ToHashSet();
            return first.Select(x => BuildStatus(x.t, x.state, facts, applied)).ToList();
        }
        finally
        {
            _backups = null;
        }
    }

    private Dictionary<string, TweakBackup>? _backups;

    private TweakBackup? BackupOf(string id) => _backups is { } cached ? cached.GetValueOrDefault(id) : PeekBackup(id);

    /// <summary>For display and checks only: a backup that is locked right now counts as absent (apply and undo fail instead).</summary>
    private TweakBackup? PeekBackup(string id)
    {
        try
        {
            return store.Get(id);
        }
        catch (BackupUnreadableException ex)
        {
            Log.Warn("engine", ex.Message);
            return null;
        }
    }

    public TweakStatus Detect(TweakDefinition t, Facts facts, IReadOnlySet<string>? appliedIds = null) =>
        BuildStatus(t, DetectState(t, facts), facts, appliedIds ?? new HashSet<string>());

    private TweakStatus BuildStatus(TweakDefinition t, TweakState state, Facts facts, IReadOnlySet<string> applied)
    {
        var (impact, effects, reason) = ImpactFor(t, facts);
        // Expert mode is a UI filter, not a reason to show a tweak as blocked; the anti-cheat warning is kept.
        var blocks = Preflight(t, facts, applied, new ApplyOptions { ExpertMode = true });
        var recommended = state is TweakState.NotApplied or TweakState.Partial or TweakState.RevertedByWindows
                          && t.RecommendWhen?.Evaluate(facts) == true && blocks.Count == 0;
        return new TweakStatus(t, state, impact, effects, reason, recommended, blocks, BackupOf(t.Id) is not null);
    }

    public TweakState DetectState(TweakDefinition t, Facts facts)
    {
        if (!AppliesTo(t, facts)) return TweakState.NotApplicable;
        var actions = Expand(t);
        var states = actions.Select(a => SafeState(a)).ToList();
        var supported = states.Where(s => s != ActionState.Unsupported).ToList();
        if (supported.Count == 0) return TweakState.Unsupported;
        if (ActivePendingUndo(t, actions) is not null) return TweakState.UndoPendingRestart;

        var appliedCount = supported.Count(s => s == ActionState.Applied);
        var state = appliedCount == supported.Count ? TweakState.Applied : appliedCount == 0 ? TweakState.NotApplied : TweakState.Partial;

        var backup = BackupOf(t.Id);
        if (backup is not null)
        {
            // Pending restart: everything reads as applied, except changes that only show their value after the restart.
            if (IsRestartPending(backup) && actions.Select((a, i) => (a, s: states[i])).All(x => x.s != ActionState.NotApplied || x.a.TakesEffectAfterRestart))
                return TweakState.PendingRestart;
            // We applied it, now it is gone: a feature update or another tool reset it (drift). Only
            // targets we changed count: a newly added adapter or a target outside the backup is simply not applied.
            if (actions.Select((a, i) => (a, s: states[i])).Any(x => x.s == ActionState.NotApplied && backup.Entry(x.a.TargetKey) is not null))
                return TweakState.RevertedByWindows;
        }
        else if (state != TweakState.Applied && facts.Get("device.managed") is true && actions.OfType<RegistryAction>().Any(a => IsPolicyPath(a.Path) && SafeRead(a) is { Existed: true }))
        {
            return TweakState.EnforcedByPolicy;
        }
        return state;
    }

    private ActionState SafeState(TweakAction a)
    {
        try
        {
            return a.State(ctx);
        }
        catch (Exception ex)
        {
            Log.Warn("engine", $"state of {a.TargetKey} unreadable: {ex.Message}");
            return ActionState.Unsupported;
        }
    }

    public static bool AppliesTo(TweakDefinition t, Facts facts)
    {
        var a = t.AppliesTo;
        var build = facts.Get("os.build") as int? ?? 0;
        if (build != 0 && (build < a.MinBuild || (a.MaxBuild is { } max && build > max))) return false;
        if (a.CpuVendor is { Count: > 0 } cpus && facts.Get("cpu.vendor") is string cpu && !cpus.Contains(cpu, StringComparer.OrdinalIgnoreCase)) return false;
        if (a.GpuVendor is { Count: > 0 } gpus && !gpus.Any(g => facts.Get($"gpu.has{char.ToUpperInvariant(g[0])}{g[1..]}") is true)) return false;
        if (a.FormFactor == "desktop" && facts.Get("system.laptop") is true) return false;
        if (a.FormFactor == "laptop" && facts.Get("system.laptop") is false) return false;
        return a.When?.Evaluate(facts) ?? true;
    }

    public static (int Impact, IReadOnlyList<string> Effects, string? ReasonKey) ImpactFor(TweakDefinition t, Facts facts)
    {
        foreach (var o in t.ImpactOverrides)
            if (o.When.Evaluate(facts)) return (o.Gaming, o.Effect ?? t.Impact.Effect, o.ReasonKey);
        return (t.Impact.Gaming, t.Impact.Effect, null);
    }

    private static bool IsPolicyPath(string path) => path.Contains(@"\Policies\", StringComparison.OrdinalIgnoreCase);

    // ---------------- preflight / guard rules ----------------

    public List<Block> Preflight(TweakDefinition t, Facts facts, IReadOnlySet<string> applied, ApplyOptions options)
    {
        var blocks = new List<Block>();
        if (!AppliesTo(t, facts)) blocks.Add(new Block("block.notApplicable"));
        foreach (var c in t.BlockedWhen.Where(c => c.Evaluate(facts)))
            blocks.Add(new Block(c.ReasonKey ?? "block.guardRule"));
        // A guard whose facts could not be read (a probe failed) fails closed: the danger it guards against may be there.
        if (t.BlockedWhen.Any(c => !c.CanEvaluate(facts)) && blocks.Count == 0) blocks.Add(new Block("block.cannotCheck"));
        if (t.AntiCheatSensitive && facts.Get("anticheat.strict") is true && !options.AcknowledgeAntiCheat)
            blocks.Add(new Block("block.antiCheat", facts.Get("anticheat.strictNames") as string, CanOverride: true));
        if (t.EffectiveRisk == Risk.Expert && !options.ExpertMode) blocks.Add(new Block("block.expertMode", CanOverride: true));
        // Conflicts: only changes this app made and has not undone. A value that merely matches the other tweak (the
        // Windows default Balanced plan matches power.balancedPlan) is not a change to protect; this tweak's own backup
        // keeps it as the original, so undo brings it back.
        foreach (var other in t.ConflictsWith.Where(o => BackupOf(o) is not null)) blocks.Add(new Block("block.conflict", other));
        // A backup file that cannot be read still holds the true originals: applying again would record the changed
        // values as originals and overwrite it.
        if (store.IsDamaged(t.Id)) blocks.Add(new Block("block.backupDamaged", store.DamagedFile(t.Id)));
        foreach (var req in t.Requires.Where(r => !applied.Contains(r))) blocks.Add(new Block("block.requires", req));
        if (facts.Get("elevated") is false) blocks.Add(new Block("block.notElevated"));
        return blocks;
    }

    // ---------------- preview ----------------

    public IReadOnlyList<ChangeLine> Preview(TweakDefinition t) =>
        Expand(t).Select(a =>
        {
            try
            {
                return a.Change(ctx);
            }
            catch (Exception ex)
            {
                return new ChangeLine(a.TargetKey, "(unreadable)", ex.Message);
            }
        }).ToList();

    // ---------------- apply ----------------

    /// <summary>
    /// "Apply recommended": applies each batch-safe tweak in order with the normal pipeline (one restore point for the
    /// whole batch, per-tweak rollback). Stops early only when the restore point decision is needed.
    /// </summary>
    public async Task<IReadOnlyList<BatchItemResult>> ApplyBatchAsync(IEnumerable<TweakDefinition> tweaks, Facts facts, IReadOnlySet<string> applied,
        ApplyOptions options, IProgress<TweakDefinition>? progress = null)
    {
        var results = new List<BatchItemResult>();
        var nowApplied = new HashSet<string>(applied);
        foreach (var t in tweaks)
        {
            if (!t.IsBatchSafe)
            {
                results.Add(new BatchItemResult(t, new ApplyResult(ApplyOutcome.Blocked, [], Blocks: [new Block("block.notBatchSafe")])));
                continue;
            }
            progress?.Report(t);
            var r = await ApplyAsync(t, facts, nowApplied, options);
            results.Add(new BatchItemResult(t, r));
            if (r.Outcome == ApplyOutcome.NeedsRestorePointDecision) break;
            if (r.Outcome is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective) nowApplied.Add(t.Id);
        }
        return results;
    }

    public async Task<ApplyResult> ApplyAsync(TweakDefinition t, Facts facts, IReadOnlySet<string> applied, ApplyOptions options)
    {
        var blocks = Preflight(t, facts, applied, options);
        if (blocks.Count > 0) return new ApplyResult(ApplyOutcome.Blocked, [], Blocks: blocks);

        var actions = Expand(t);
        var changes = Preview(t);
        // After an undo that waits for a restart the running value still matches, but the setting is already reverted.
        var pendingUndo = ActivePendingUndo(t, actions);
        if (pendingUndo is null && actions.All(a => SafeState(a) != ActionState.NotApplied)) return new ApplyResult(ApplyOutcome.NothingToDo, changes);

        // Safety net: one restore point per session before the first change (secondary to the JSON backup).
        if (!_restorePointDone && t.Id != RestorePointFrequencyTweak)
        {
            var ok = await EnsureRestorePointAsync(facts);
            if (!ok && !options.ContinueWithoutRestorePoint) return new ApplyResult(ApplyOutcome.NeedsRestorePointDecision, changes);
            _restorePointDone = true;
        }

        TweakBackup? existing;
        try
        {
            existing = store.Get(t.Id);
        }
        catch (BackupUnreadableException ex)
        {
            return new ApplyResult(ApplyOutcome.Failed, changes, ex.Message);
        }
        var backup = existing ?? new TweakBackup
        {
            TweakId = t.Id,
            WindowsBuild = windowsBuild,
            AppVersion = appVersion,
            CatalogVersion = TweakCatalog.Version,
        };

        // Exports before boot-critical or power-plan changes.
        try
        {
            if (actions.Any(a => a.IsBootCritical))
            {
                var file = Path.Combine(ctx.ExportFolder, $"bcd-{DateTime.Now:yyyyMMdd-HHmmss}.bcd");
                ctx.Bcd.Export(file);
                backup.Exports.Add(file);
            }
            if (actions.Any(a => a is PowerSettingAction or PowerSchemeAction))
            {
                var scheme = ctx.Power.ActiveScheme();
                var file = Path.Combine(ctx.ExportFolder, $"power-{scheme}-{DateTime.Now:yyyyMMdd-HHmmss}.pow");
                ctx.Power.Export(scheme, file);
                backup.Exports.Add(file);
            }
        }
        catch (Exception ex)
        {
            return Fail(t, changes, $"Export before change failed: {ex.Message}");
        }

        // First-original rule: an entry is written once and never overwritten by a later apply.
        var before = new Dictionary<string, StoredValue>();
        var added = new HashSet<string>(StringComparer.Ordinal);
        foreach (var a in actions)
        {
            var current = SafeRead(a);
            if (current is null) continue;
            // Undone and not restarted yet: the setting is the restored original, not the running value.
            if (a.TakesEffectAfterRestart && pendingUndo?.Entries.FirstOrDefault(e => e.TargetKey == a.TargetKey) is { } restored) current = restored.Original;
            before[a.TargetKey] = current;
            if (backup.Entry(a.TargetKey) is not { } entry)
            {
                backup.Entries.Add(entry = new BackupEntry { TargetKey = a.TargetKey, Description = a.Describe(ctx), Original = current });
                added.Add(a.TargetKey);
            }
            entry.Action = JsonSerializer.Serialize(a, TweakCatalog.JsonOptions);
        }
        // Runtime tweaks keep one id while their content can change (a service's start type, a game list): the latest
        // definition is what detection compares against; each entry restores itself through its own action.
        if (_catalog.Get(t.Id) is null) backup.Definition = JsonSerializer.Serialize(t, TweakCatalog.JsonOptions);
        store.Save(backup); // persisted before the first write, so a crash mid-way still has the originals

        // Apply in order; on failure roll back what this run changed (per-tweak transaction).
        var done = new List<TweakAction>();
        foreach (var a in actions.Where(a => before.ContainsKey(a.TargetKey)))
        {
            try
            {
                a.Apply(ctx);
                done.Add(a);
            }
            catch (Exception ex)
            {
                Log.Error("engine", $"{t.Id}: {a.TargetKey} failed, rolling back {done.Count} action(s) and the failed one", ex);
                // The failing action may have written part of its change before it threw (one adapter keyword, the
                // mains side of a power setting): it is rolled back too. A target counts as stuck only when it no
                // longer reads as before, so a write that Windows refused outright is not reported as half-changed.
                var stuck = new List<TweakAction>();
                foreach (var undo in Enumerable.Reverse(done).Prepend(a))
                {
                    var restored = true;
                    try
                    {
                        undo.Restore(ctx, before[undo.TargetKey]);
                    }
                    catch (Exception rex)
                    {
                        restored = false;
                        Log.Error("engine", $"rollback of {undo.TargetKey} failed", rex);
                    }
                    var now = SafeRead(undo);
                    if (now is not null ? !now.SameAs(before[undo.TargetKey]) : !restored) stuck.Add(undo);
                }
                if (stuck.Count > 0)
                {
                    // Part of the change is still on the system: keep the backup so Undo can restore it later. Entries this
                    // run added for targets that were rolled back cleanly are dropped; they would later read as "reset
                    // by Windows" and raise a drift notice.
                    backup.Entries.RemoveAll(e => added.Contains(e.TargetKey) && !stuck.Any(s => s.TargetKey == e.TargetKey));
                    foreach (var s in stuck)
                        if (backup.Entry(s.TargetKey) is { } e) e.Applied = SafeRead(s);
                    backup.LastApplied = DateTimeOffset.Now;
                    backup.AppliedOnVersion = WindowsVersion;
                    store.Save(backup);
                    return Fail(t, changes, $"{ex.Message} (rollback failed for {string.Join(", ", stuck.Select(s => s.TargetKey))}; the backup is kept, use Undo)") with { LeftChanged = true };
                }
                // Remove entries this run added if nothing of this tweak remains applied.
                if (backup.Entries.All(e => before.TryGetValue(e.TargetKey, out var b) && b.SameAs(e.Original)))
                    store.Archive(t.Id);
                return Fail(t, changes, ex.Message);
            }
        }

        // A change that takes effect after a restart still reads the old value: record what was written instead, or undo
        // would later take the new running value for a change by Windows and skip it.
        foreach (var a in done)
            if (backup.Entry(a.TargetKey) is { } e) e.Applied = a.TakesEffectAfterRestart ? a.Desired(ctx) : SafeRead(a);
        backup.LastApplied = DateTimeOffset.Now;
        backup.AppliedOnVersion = WindowsVersion;
        if (t.Restart || t.Verify == "afterRestart" || done.Any(a => a.TakesEffectAfterRestart)) backup.PendingRestartSince = DateTimeOffset.Now;
        store.Save(backup);
        if (pendingUndo is not null) store.ClearPendingUndo(t.Id);

        foreach (var line in changes) Log.Info("change", $"{t.Id}: {line.Target}", new { line.Before, line.After });

        // Verify: re-read every action. "afterRestart" tweaks are confirmed on the next scan after a reboot.
        var ineffective = t.Verify != "afterRestart" && done.Any(a => !a.TakesEffectAfterRestart && SafeState(a) != ActionState.Applied);
        return new ApplyResult(ineffective ? ApplyOutcome.AppliedIneffective : ApplyOutcome.Applied, changes);
    }

    private ApplyResult Fail(TweakDefinition t, IReadOnlyList<ChangeLine> changes, string error)
    {
        Log.Error("engine", $"{t.Id}: apply failed: {error}");
        return new ApplyResult(ApplyOutcome.Failed, changes, error);
    }

    private async Task<bool> EnsureRestorePointAsync(Facts facts)
    {
        if (restorePoints.IsEnabled() != true) return false;
        // Lift the 24 h limit through a normal, backed-up tweak, so undo puts the original value back.
        var freq = _catalog.Get(RestorePointFrequencyTweak);
        var lifted = freq is not null && PeekBackup(freq.Id) is null && !store.IsDamaged(freq.Id) &&
                     (await ApplyAsync(freq, facts, new HashSet<string>(), new ApplyOptions { ExpertMode = true, ContinueWithoutRestorePoint = true })).Outcome
                     is ApplyOutcome.Applied or ApplyOutcome.AppliedIneffective;
        var created = await restorePoints.CreateAsync($"PCOptimizer {DateTime.Now:yyyy-MM-dd HH:mm}");
        // No restore point came of it (the user may now cancel the change): the limit goes back as it was.
        if (!created && lifted) Revert(freq!);
        return created;
    }

    // ---------------- undo ----------------

    public RevertResult Revert(TweakDefinition t)
    {
        TweakBackup? backup;
        try
        {
            backup = store.Get(t.Id);
        }
        catch (BackupUnreadableException ex)
        {
            return new RevertResult(false, [], [ex.Message]);
        }
        if (backup is null)
            return store.IsDamaged(t.Id)
                ? new RevertResult(false, [], [$"The backup file {store.DamagedFile(t.Id)} is damaged; the original values cannot be read from it."])
                : new RevertResult(true, [], []);
        var skipped = new List<string>();
        var errors = new List<string>();
        var kept = new List<BackupEntry>();
        var restartBound = new List<BackupEntry>();
        var pending = IsRestartPending(backup);
        var current = new Dictionary<string, TweakAction>();
        foreach (var a in Expand(t)) current.TryAdd(a.TargetKey, a);
        // Every entry of the backup is restored, also targets the tweak no longer expands to (through the stored action).
        foreach (var entry in Enumerable.Reverse(backup.Entries))
        {
            // The action that wrote the change first: a later catalog version can change an action under the same target
            // (for example write only one side of a power setting), and undo must restore what was written then.
            var a = StoredAction(entry) ?? current.GetValueOrDefault(entry.TargetKey);
            if (a is null)
            {
                kept.Add(entry);
                errors.Add($"{entry.Description}: cannot be restored automatically (not part of this change any more)");
                continue;
            }
            try
            {
                // Feature-update-aware: if the value is no longer what we applied, Windows (or the user) changed it: leave it.
                // Not for a change that takes effect after a restart while the restart is pending (the running value is
                // still the old one), nor for backups of version 0.4.0 and older, which stored that old value as applied.
                var waitsForRestart = a.TakesEffectAfterRestart && (pending || entry.Applied?.SameAs(entry.Original) == true);
                var outcome = a.RestoreIfUnchanged(ctx, entry.Original, waitsForRestart ? null : entry.Applied);
                if (outcome == RestoreOutcome.ChangedSince)
                {
                    skipped.Add(entry.Description);
                    continue;
                }
                // Parts changed since (one side of a power setting, one adapter keyword) keep the value set since.
                if (outcome == RestoreOutcome.PartlyRestored) skipped.Add(entry.Description);
                if (a.TakesEffectAfterRestart) restartBound.Add(entry);
                Log.Info("change", $"{t.Id}: undo {entry.Description}", new { restored = entry.Original.Display });
            }
            catch (Exception ex)
            {
                kept.Add(entry);
                errors.Add($"{entry.Description}: {ex.Message}");
                Log.Error("engine", $"undo {t.Id} {a.TargetKey} failed", ex);
            }
        }
        if (kept.Count == 0)
        {
            DeleteExports(backup);
            store.Archive(t.Id);
        }
        else
        {
            // Only what could not be restored stays: a retry does not report restored targets as "reset by Windows".
            backup.Entries.RemoveAll(e => !kept.Contains(e));
            store.Save(backup);
        }
        // Restored values that take effect after a restart: shown as "off after restart" until then.
        if (restartBound.Count > 0) store.SavePendingUndo(new PendingUndo { TweakId = t.Id, Entries = restartBound });
        return new RevertResult(errors.Count == 0, skipped, errors);
    }

    /// <summary>
    /// The BCD and power plan exports made before the change: kept while the change is in place (a manual way back),
    /// removed with its complete undo, so the exports folder does not grow with every apply.
    /// </summary>
    private void DeleteExports(TweakBackup backup)
    {
        foreach (var file in backup.Exports)
        {
            try
            {
                if (Path.GetDirectoryName(Path.GetFullPath(file)) is { } dir && dir.Equals(Path.GetFullPath(ctx.ExportFolder).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                    File.Delete(file);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                Log.Warn("engine", $"export {file} not removed: {ex.Message}");
            }
        }
    }

    private static TweakAction? StoredAction(BackupEntry entry)
    {
        if (entry.Action is not { } json) return null;
        try
        {
            var a = JsonSerializer.Deserialize<TweakAction>(json, TweakCatalog.JsonOptions);
            return a?.TargetKey == entry.TargetKey ? a : null;
        }
        catch (JsonException ex)
        {
            Log.Warn("engine", $"stored action of {entry.TargetKey} unreadable: {ex.Message}");
            return null;
        }
    }

    private StoredValue? SafeRead(TweakAction a)
    {
        try
        {
            return a.Read(ctx);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Undoes every backed-up change: catalog tweaks and runtime tweaks (from their stored definition).</summary>
    public IReadOnlyList<(TweakDefinition Tweak, RevertResult Result)> RevertAll()
    {
        var results = new List<(TweakDefinition, RevertResult)>();
        foreach (var b in store.All())
            if (Resolve(b.TweakId) is { Reversibility: Reversibility.Reversible } t)
                results.Add((t, Revert(t)));
        return results;
    }

    /// <summary>
    /// Every change this app made (catalog and runtime tweaks) whose values are no longer in place. Run after each scan;
    /// a Windows update between apply and now is reported as the likely cause.
    /// </summary>
    public IReadOnlyList<DriftItem> CheckDrift(Facts facts)
    {
        var list = new List<DriftItem>();
        foreach (var backup in store.All())
        {
            if (Resolve(backup.TweakId) is not { } t) continue;
            if (DetectState(t, facts) != TweakState.RevertedByWindows) continue;
            // Older backups only know the build number of their first apply (compared by build only).
            var appliedOn = backup.AppliedOnVersion ?? (backup.WindowsBuild > 0 ? backup.WindowsBuild.ToString(System.Globalization.CultureInfo.InvariantCulture) : null);
            list.Add(new DriftItem(t, backup, appliedOn, WindowsVersion));
        }
        return list;
    }

    /// <summary>The definition of a tweak id: the catalog entry, or the one stored with the backup of a runtime tweak.</summary>
    public TweakDefinition? Resolve(string id)
    {
        if (_catalog.Get(id) is { } t) return t;
        if (PeekBackup(id) is not { } backup) return null;
        if (backup.Definition is { } json)
        {
            try
            {
                return JsonSerializer.Deserialize<TweakDefinition>(json, TweakCatalog.JsonOptions);
            }
            catch (JsonException ex)
            {
                Log.Warn("engine", $"stored definition of {id} unreadable: {ex.Message}");
            }
        }
        return Retired(backup);
    }

    /// <summary>
    /// A catalog tweak that a later version removed: it is no longer offered, but a change made with it must still be
    /// undoable. Undo restores every entry through the action stored with it, so the definition needs no actions.
    /// </summary>
    private static TweakDefinition? Retired(TweakBackup backup) =>
        backup.Entries.Any(e => e.Action is not null)
            ? new TweakDefinition
            {
                Id = backup.TweakId,
                Category = "Retired",
                Docs = "retired",
                Subject = backup.TweakId,
                Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
                Hidden = true,
                Actions = [],
            }
            : null;

    // ---------------- helpers ----------------

    /// <summary>
    /// Expands per-adapter templates: "{nic}" registry paths and DNS actions into one action per active network
    /// interface, NIC property templates into one action per physical adapter of the requested media.
    /// </summary>
    public IReadOnlyList<TweakAction> Expand(TweakDefinition t)
    {
        var list = new List<TweakAction>();
        IReadOnlyList<NicAdapter>? adapters = null;
        Guid? activeScheme = null;
        IReadOnlyList<string>? nics = null;
        foreach (var a in t.Actions)
        {
            switch (a)
            {
                case RegistryAction r when r.Path.Contains("{nic}", StringComparison.Ordinal):
                    foreach (var id in nics ??= ctx.CurrentNetworkInterfaceIds())
                        list.Add(new RegistryAction
                        {
                            Hive = r.Hive, Path = r.Path.Replace("{nic}", id), Name = r.Name, Kind = r.Kind, Value = r.Value, Delete = r.Delete,
                            Notify = r.Notify, MissingMeans = r.MissingMeans, RemoveKeyOnUndo = r.RemoveKeyOnUndo?.Replace("{nic}", id),
                        });
                    break;
                case DnsAction d when d.InterfaceGuid == "{nic}":
                    foreach (var id in nics ??= ctx.CurrentNetworkInterfaceIds())
                        list.Add(new DnsAction { InterfaceGuid = id, Servers = d.Servers });
                    break;
                case PowerSettingAction p when p.Scheme is null:
                    activeScheme ??= SafeActiveScheme();
                    list.Add(activeScheme is { } scheme ? p.For(scheme) : p);
                    break;
                case PowerModeAction m when m.Source is null:
                    list.Add(PowerModeAction.CurrentSource(ctx) is { } source ? m.For(source) : m);
                    break;
                case NicPropertyAction n when n.Adapter is null:
                    adapters ??= SafeAdapters();
                    foreach (var adapter in adapters.Where(x => n.Media switch { "ethernet" => x.IsEthernet, "wifi" => x.IsWifi, _ => true }))
                    {
                        if (n.InterfaceGuid is { } only && !string.Equals(only, adapter.InterfaceGuid, StringComparison.OrdinalIgnoreCase)) continue;
                        list.Add(new NicPropertyAction { Properties = n.Properties, Dwords = n.Dwords, Media = n.Media, InterfaceGuid = n.InterfaceGuid, Adapter = adapter });
                    }
                    break;
                default:
                    list.Add(a);
                    break;
            }
        }
        return list;
    }

    private Guid? SafeActiveScheme()
    {
        try
        {
            return ctx.Power.ActiveScheme() is var g && g != Guid.Empty ? g : null;
        }
        catch (Exception ex)
        {
            Log.Warn("engine", $"active power plan unreadable: {ex.Message}");
            return null;
        }
    }

    private IReadOnlyList<NicAdapter> SafeAdapters()
    {
        try
        {
            return NicAdapters.Enumerate(ctx.Registry);
        }
        catch (Exception ex)
        {
            Log.Warn("engine", $"network adapters unreadable: {ex.Message}");
            return [];
        }
    }

    private static DateTimeOffset LastBoot() => DateTimeOffset.Now - TimeSpan.FromMilliseconds(Environment.TickCount64);

    private static bool IsRestartPending(TweakBackup backup) => backup.PendingRestartSince is { } since && LastBoot() < since;

    /// <summary>
    /// The pending undo of this tweak while it still waits: no boot since the undo, and a restored value is not in
    /// effect yet (it still reads as applied). Otherwise the record is done and removed.
    /// </summary>
    private PendingUndo? ActivePendingUndo(TweakDefinition t, IReadOnlyList<TweakAction> actions)
    {
        if (store.GetPendingUndo(t.Id) is not { } p) return null;
        if (LastBoot() < p.Since && actions.Any(a => a.TakesEffectAfterRestart && p.Entries.Any(e => e.TargetKey == a.TargetKey) && SafeState(a) == ActionState.Applied))
            return p;
        try
        {
            store.ClearPendingUndo(t.Id);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("engine", $"pending undo of {t.Id} not removed: {ex.Message}");
        }
        return null;
    }
}
