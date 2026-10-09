using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings;

/// <summary>Plan v4 §5.1: every check returns OK / Problem / Unsupported / Unknown. Unknown never produces advice.</summary>
public enum FindingStatus { Ok, Problem, Unsupported, Unknown, Info }

public enum FindingKind { Finding, Advisor, GameAccess }

/// <summary>Effect tags (plan v4 §4.1).</summary>
public static class Effect
{
    public const string Fps = "fps", Lows = "lows", Latency = "latency", Stutter = "stutter",
        Stability = "stability", Input = "input", Prerequisite = "prerequisite", None = "none";
}

/// <summary>
/// One line of the generated "What we found" section. <see cref="LabelKey"/> resolves through labels.json;
/// a value starting with '@' is a label key too (e.g. "@yes"), so facts stay language-neutral.
/// </summary>
public sealed record Fact(string LabelKey, string Value);

public sealed record Finding
{
    /// <summary>Explanation page id (Catalog/Docs/&lt;lang&gt;/&lt;Id&gt;.md).</summary>
    public required string Id { get; init; }

    /// <summary>Distinguishes several results of one check (e.g. one per display).</summary>
    public string? InstanceKey { get; init; }

    /// <summary>Shown after the title, e.g. the monitor name.</summary>
    public string? Subject { get; init; }

    public required FindingKind Kind { get; init; }
    public required FindingStatus Status { get; init; }

    /// <summary>0-5 ⚡; null for Game access items (shown separately from ⚡).</summary>
    public int? Impact { get; init; }

    public IReadOnlyList<string> Effects { get; init; } = [];
    public bool Critical { get; init; }

    /// <summary>Variant of the explanation, e.g. "slotLimited" vs "trainedDown" (selects a ":::variant" block in the page).</summary>
    public string? Variant { get; init; }

    public IReadOnlyList<Fact> Facts { get; init; } = [];

    /// <summary>Placeholder values for the explanation page ({{name}}).</summary>
    public IReadOnlyDictionary<string, string> Params { get; init; } = new Dictionary<string, string>();

    /// <summary>One-click fix built for this PC (e.g. the exact display mode); catalog fixes are linked via TweakDefinition.Fixes.</summary>
    public Tweaks.TweakDefinition? Fix { get; init; }

    public bool IsProblem => Status == FindingStatus.Problem;
    public string Key => InstanceKey is null ? Id : $"{Id}#{InstanceKey}";
}

public interface IFindingCheck
{
    /// <summary>Ids of the explanation pages this check can produce (used by the docs lint).</summary>
    IReadOnlyList<string> DocIds { get; }

    IEnumerable<Finding> Evaluate(HardwareProfile profile, CatalogData catalog);
}
