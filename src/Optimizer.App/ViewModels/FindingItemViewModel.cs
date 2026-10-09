using Optimizer.App.Services;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>A finding/advisor item as shown in the list and the inspector, in the current language.</summary>
public sealed class FindingItemViewModel : InspectorItem
{
    /// <param name="goal">The active profile's goal ("gaming", "battery", ...): names what the impact is measured on.</param>
    public FindingItemViewModel(Finding finding, string lang, TweakDefinition? fix = null, string goal = "gaming")
    {
        Finding = finding;
        Fix = fix;
        var labels = Labels.Current;
        var page = DocStore.Get(finding.Id, lang) ?? DocStore.Get(finding.Id, "en");
        var title = page is null ? finding.Id : DocStore.Substitute(page.Title, finding.Params);
        Title = finding.Subject is { Length: > 0 } s && !title.Contains(s, StringComparison.OrdinalIgnoreCase) ? $"{title}: {s}" : title;
        Summary = page is null ? "" : DocStore.Summary(page, finding);
        Markdown = page is null ? $"# {finding.Id}\n\n(no explanation page)" : $"# {Title}\n\n" + DocStore.RenderFinding(page, finding, labels);
        StatusText = labels.Get(lang, $"status.{finding.Status}");
        Status = finding.Status.ToString();
        EffectsText = string.Join(", ", finding.Effects.Where(e => e != Effect.Prerequisite || finding.Impact is null).Select(e => labels.Get(lang, $"effect.{e}")));
        var rated = finding.Impact is not null && !finding.Critical && finding.Status != FindingStatus.Unsupported;
        Impact = rated ? Math.Clamp(finding.Impact!.Value, 0, 5) : null;
        ImpactText = Impact is { } i ? Loc.Instance.Format("Impact_Short", i) : "";
        ImpactTooltip = Impact is { } j ? Loc.Instance.Format("Impact_TooltipGoal", labels.Get(lang, $"effect.{goal}"), j) : "";
        FactRows = finding.Facts.Select(f => new SummaryItem(labels.Get(lang, f.LabelKey), labels.Value(lang, f.Value))).ToList();
        CriticalText = finding.Critical ? Loc.Instance["Impact_Critical"] : null;
        MetaText = string.Join(", ", new[] { StatusText, CriticalText ?? ImpactText, EffectsText }.Where(x => !string.IsNullOrEmpty(x)));
        if (fix is not null && finding.Status == FindingStatus.Problem) ActionText = Loc.Instance["Fix_Apply"];
    }

    public Finding Finding { get; }

    /// <summary>One-click fix for this finding (runtime fix or catalog tweak), if any.</summary>
    public TweakDefinition? Fix { get; }

    public IReadOnlyList<SummaryItem> FactRows { get; }
    public bool IsProblem => Finding.Status == FindingStatus.Problem;
    public bool HasFix => HasAction;
    public override string Key => "finding:" + Finding.Key;
}
