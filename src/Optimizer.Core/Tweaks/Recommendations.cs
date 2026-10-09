using Optimizer.Core.Findings;

namespace Optimizer.Core.Tweaks;

/// <summary>One entry of the "Apply recommended" plan, with the reason shown to the user.</summary>
public sealed record RecommendedItem(TweakDefinition Tweak, string? ReasonKey, Finding? FixesFinding, int Impact);

/// <summary>What "Apply recommended" would do and what it leaves out (and why).</summary>
public sealed record RecommendationPlan(IReadOnlyList<RecommendedItem> Items, IReadOnlyList<RecommendedItem> Excluded)
{
    public bool IsEmpty => Items.Count == 0;
}

/// <summary>
/// Builds the "Apply recommended" plan (plan v4 §4.3): catalog tweaks whose recommendation rule matches this PC and that
/// are batch-safe, plus the one-click fixes of problem findings. Expert, boot-critical, anti-cheat sensitive and not fully
/// reversible items are listed as excluded so the user can apply them one by one.
/// </summary>
public static class Recommendations
{
    public static RecommendationPlan Build(IEnumerable<TweakStatus> statuses, IEnumerable<Finding> findings)
    {
        var items = new List<RecommendedItem>();
        var excluded = new List<RecommendedItem>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        void Add(RecommendedItem item)
        {
            if (!seen.Add(item.Tweak.Id)) return;
            (item.Tweak.IsBatchSafe ? items : excluded).Add(item);
        }

        var statusList = statuses.ToList();

        // Fixes first: they address measured problems on this PC. Runtime fixes are built for this PC; catalog tweaks
        // name the findings they fix.
        foreach (var f in findings.Where(f => f.Status == FindingStatus.Problem))
        {
            if (f.Fix is { } fix)
            {
                Add(new RecommendedItem(fix, null, f, f.Impact ?? 0));
                continue;
            }
            var catalogFix = statusList.FirstOrDefault(s => s.Tweak.Fixes.Contains(f.Id) && !s.IsOn && s.Blocks.Count == 0
                                                            && s.State is not (TweakState.NotApplicable or TweakState.Unsupported));
            if (catalogFix is not null) Add(new RecommendedItem(catalogFix.Tweak, catalogFix.Tweak.RecommendReasonKey, f, f.Impact ?? catalogFix.Impact));
        }

        foreach (var s in statusList.Where(s => s.Recommended))
            Add(new RecommendedItem(s.Tweak, s.Tweak.RecommendReasonKey, null, s.Impact));

        static List<RecommendedItem> Order(IEnumerable<RecommendedItem> list) =>
            list.OrderByDescending(i => i.FixesFinding is not null).ThenByDescending(i => i.Impact).ThenBy(i => i.Tweak.Id, StringComparer.Ordinal).ToList();

        return new RecommendationPlan(Order(items), Order(excluded));
    }
}
