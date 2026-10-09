using Optimizer.App.Services;
using Optimizer.Core.Docs;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>One tweak row (card with a switch) and its inspector page, in the current language.</summary>
public sealed class TweakItemViewModel : InspectorItem
{
    private readonly MainViewModel? _owner;
    private bool _switch;

    /// <param name="subjectAsTitle">Lists of one kind (games, devices): the subject is the title, the section explains the rest.</param>
    public TweakItemViewModel(TweakStatus status, string lang, TweakEngine engine, MainViewModel? owner, bool subjectAsTitle = false)
    {
        _owner = owner;
        Status_ = status;
        var t = status.Tweak;
        var labels = Labels.Current;
        var page = DocStore.Get(t.DocId, lang) ?? DocStore.Get(t.DocId, "en");
        var title = page?.Title ?? t.Id;
        Title = t.Subject is { Length: > 0 } s ? subjectAsTitle ? s : $"{title}: {s}" : title;
        Summary = subjectAsTitle && t.Subject is { Length: > 0 } ? "" : page?.Section(DocHeadings.Summary(page.Language))?.Body ?? "";
        Category = t.Category;
        CategoryText = labels.Get(lang, $"category.{t.Category}");

        StatusText = labels.Get(lang, $"state.{status.State}");
        Status = status.State switch
        {
            TweakState.Applied or TweakState.PendingRestart => "Ok",
            TweakState.Partial or TweakState.RevertedByWindows or TweakState.AppliedIneffective => "Problem",
            TweakState.Unsupported or TweakState.NotApplicable => "Unsupported",
            _ => "Neutral",
        };

        Impact = status.Impact;
        ImpactText = Loc.Instance.Format("Impact_Short", status.Impact);
        var reason = status.ImpactReasonKey is { } r ? ". " + labels.Get(lang, r) : "";
        ImpactTooltip = Loc.Instance.Format("Impact_Tooltip", status.Impact) + reason;
        EffectsText = string.Join(", ", status.Effects.Select(e => labels.Get(lang, $"effect.{e}")));

        var badges = new List<string>();
        if (t.EffectiveRisk != Risk.Safe) badges.Add(labels.Get(lang, $"risk.{t.EffectiveRisk}"));
        if (t.Impact.Basis == "disputed") badges.Add(labels.Get(lang, "badge.disputed"));
        if (t.Restart) badges.Add(labels.Get(lang, "badge.restart"));
        if (t.SignOut) badges.Add(labels.Get(lang, "badge.signOut"));
        if (t.IsBootCritical) badges.Add(labels.Get(lang, "badge.bootCritical"));
        if (t.AntiCheatSensitive) badges.Add(labels.Get(lang, "badge.antiCheat"));
        if (t.Reversibility != Reversibility.Reversible) badges.Add(labels.Get(lang, $"reversibility.{t.Reversibility}"));
        BadgesText = string.Join(", ", badges);
        IsRecommended = status.Recommended;
        RecommendedText = status.Recommended
            ? labels.Get(lang, "badge.recommended") + (t.RecommendReasonKey is { } k ? ": " + labels.Get(lang, k) : "")
            : null;

        // Hard blocks disable the switch; the anti-cheat block can be overridden in the confirmation dialog.
        var hard = status.Blocks.Where(b => !b.CanOverride).ToList();
        // "Needs administrator rights" applies to every row; the page shows it once as a banner instead.
        var first = status.Blocks.FirstOrDefault(b => b.ReasonKey != "block.notElevated");
        BlockText = first is null ? null : labels.Get(lang, first.ReasonKey) + (first.Detail ?? "");
        var canChange = status.State is not (TweakState.NotApplicable or TweakState.Unsupported or TweakState.EnforcedByPolicy);
        IsOn = status.IsOn;
        HasBackup = status.HasBackup;
        _switch = IsOn;
        // On: possible when not blocked. Off: possible when this app made the change (it has the original values).
        CanToggle = canChange && (IsOn ? HasBackup : hard.Count == 0);
        var outside = canChange && IsOn && !HasBackup ? Loc.Instance["Tweak_AlreadyOn"] : null;
        MetaText = string.Join(", ", new[] { StatusText, outside, ImpactText, EffectsText, BadgesText }.Where(x => !string.IsNullOrEmpty(x)));
        // Only real blocks are shown as a warning; "set outside this app" is part of the detail line.
        ToggleNote = canChange && !IsOn && hard.Count > 0 ? BlockText : null;
        ActionText = !canChange ? null
            : HasBackup ? Loc.Instance["Tweak_Undo"]
            : status.State is TweakState.Applied or TweakState.PendingRestart or TweakState.AppliedIneffective ? null
            : Loc.Instance["Tweak_TurnOn"];
        if (status.State == TweakState.RevertedByWindows) ActionText = Loc.Instance["Tweak_Reapply"];
        ActionEnabled = HasBackup || hard.Count == 0;
        ActionNote = HasBackup || ActionText is null ? null : BlockText;

        // Inspector document: hand-written sections plus generated "What changes" and "Undo".
        var changes = engine.Preview(t);
        var notes = new List<string>();
        if (t.Scope == TweakScope.User || t.Actions.OfType<Optimizer.Core.Actions.RegistryAction>().Any(a => a.Hive == Optimizer.Core.Actions.Hive.User))
            notes.Add(labels.Get(lang, "changes.userScope"));
        var undo = new List<string> { labels.Get(lang, t.Reversibility switch { Reversibility.Reinstall => "undo.reinstall", Reversibility.Permanent => "undo.permanent", _ => "undo.reversible" }) };
        if (t.Restart) undo.Add(labels.Get(lang, "undo.restart"));
        if (t.SignOut) undo.Add(labels.Get(lang, "undo.signOut"));
        if (t.IsBootCritical) undo.Add(labels.Get(lang, "undo.bootCritical"));
        Markdown = page is null
            ? $"# {Title}"
            : $"# {Title}\n\n" + DocStore.RenderTweak(page, changes, notes, string.Join(" ", undo));
    }

    public TweakStatus Status_ { get; }
    public TweakDefinition Tweak => Status_.Tweak;
    public string Category { get; }
    public string CategoryText { get; }
    public string BadgesText { get; }
    public bool HasBadges => BadgesText.Length > 0;
    public bool IsRecommended { get; }
    public string? RecommendedText { get; }
    public string? BlockText { get; }
    public bool IsOn { get; }
    public bool HasBackup { get; }
    public bool CanToggle { get; }
    public string? ToggleNote { get; }
    public override string Key => "tweak:" + Tweak.Id;

    /// <summary>
    /// Bound two-way to the row's switch. Setting it starts apply (on) or undo (off); the switch then shows the real
    /// state again (cancelled = it flips back, done = the rescan rebuilds the row).
    /// </summary>
    public bool SwitchState
    {
        get => _switch;
        set
        {
            if (value == _switch) return;
            _switch = value;
            OnPropertyChanged();
            if (_owner is not null && value != IsOn) _ = _owner.ToggleAsync(this, value);
            else ResetSwitch();
        }
    }

    public void ResetSwitch()
    {
        _switch = IsOn;
        OnPropertyChanged(nameof(SwitchState));
    }
}
