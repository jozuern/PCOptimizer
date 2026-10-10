using System.Text.RegularExpressions;
using Optimizer.Core.Findings;

namespace Optimizer.Core.Docs;

public sealed record LintIssue(string DocId, string Language, string Message)
{
    public override string ToString() => $"[{Language}] {DocId}: {Message}";
}

/// <summary>
/// Explanation lint. Severity: warning in Debug builds, error in Release builds: the caller decides.
/// </summary>
public static partial class DocLint
{
    public const int MaxSummaryLength = 200;

    /// <summary>
    /// Marketing words and filler that make text sound generated (style guide: facts over adjectives). Matched as whole
    /// words, case-insensitive, after removing the <see cref="AllowedPhrases"/>.
    /// </summary>
    public static readonly string[] BannedWords =
    [
        "boost your", "unleash", "massive", "guaranteed", "insane", "turbocharge", "supercharge", "seamless", "seamlessly", "robust",
        "leverage", "comprehensive", "powerful", "effortless", "effortlessly", "elevate", "unlock", "dive into", "ultimate",
        "game-changer", "game changer", "cutting-edge", "state-of-the-art", "next-level", "blazing",
        "gewaltig", "gewaltige", "garantiert", "enorm", "enorme", "brutal schnell", "nahtlos", "nahtlose", "nahtlosen", "mühelos", "mühelose",
        "leistungsstark", "leistungsstarke", "leistungsstarken", "revolutionär", "revolutionäre",
    ];

    /// <summary>Names that contain a banned word: a Windows power plan, Windows' own boost mode names, laptop vendor modes.</summary>
    public static readonly string[] AllowedPhrases =
    [
        "ultimate performance", "pcoptimizer ultimate", "\"ultimate\"", "„ultimate“", "aggressive at guaranteed", "garantierter leistung",
    ];

    /// <summary>Banned words found in <paramref name="text"/>.</summary>
    public static IEnumerable<string> FindBannedWords(string text)
    {
        var lower = text.ToLowerInvariant();
        foreach (var allowed in AllowedPhrases) lower = lower.Replace(allowed, " ", StringComparison.Ordinal);
        return BannedWords.Where(w => Regex.IsMatch(lower, $@"(?<![\p{{L}}\-]){Regex.Escape(w)}(?![\p{{L}}\-])"));
    }

    public static IEnumerable<string> RequiredDocIds() =>
        FindingEngine.CreateChecks(null).SelectMany(c => c.DocIds).Distinct(StringComparer.Ordinal);

    /// <summary>Explanation pages for tweaks: every catalog entry plus the runtime fixes.</summary>
    public static IEnumerable<string> RequiredTweakDocIds() =>
        Tweaks.TweakCatalog.Current.Tweaks.Select(t => t.DocId)
            .Concat(Findings.Checks.RuntimeFixes.DocIds).Concat(Tweaks.DeviceTweaks.DocIds).Concat(Startup.StartupTweaks.DocIds).Concat(["service.change", "feature.change"])
            .Distinct(StringComparer.Ordinal);

    public static List<LintIssue> Run()
    {
        var issues = new List<LintIssue>();
        foreach (var id in RequiredTweakDocIds())
        foreach (var lang in DocStore.Languages)
        {
            var page = DocStore.Get(id, lang);
            if (page is null)
            {
                issues.Add(new LintIssue(id, lang, "missing explanation page"));
                continue;
            }
            issues.AddRange(Check(page, DocHeadings.TweakRequired[lang], [DocHeadings.GeneratedChanges[lang], DocHeadings.GeneratedUndo[lang]]));
        }

        // Every tweak needs at least one source and a valid impact (catalog lint).
        foreach (var t in Tweaks.TweakCatalog.Current.Tweaks)
        {
            if (t.Sources.Count == 0) issues.Add(new LintIssue(t.Id, "catalog", "no sources"));
            if (t.Impact.Gaming is < 0 or > 5) issues.Add(new LintIssue(t.Id, "catalog", "impact outside 0-5"));
            if (t.Impact.Effect.Count == 0) issues.Add(new LintIssue(t.Id, "catalog", "no effect tag"));
            if (t.Impact.Basis is not ("measured" or "situational" or "disputed")) issues.Add(new LintIssue(t.Id, "catalog", $"unknown basis '{t.Impact.Basis}'"));
            if (t.Actions.Count == 0 && !t.Hidden) issues.Add(new LintIssue(t.Id, "catalog", "no actions"));
            foreach (var a in t.Actions.OfType<Actions.PowerSettingAction>())
                if (!Actions.PowerAliases.IsKnown(a.Subgroup) || !Actions.PowerAliases.IsKnown(a.Setting))
                    issues.Add(new LintIssue(t.Id, "catalog", "power setting must use a GUID or a known alias"));
        }
        foreach (var id in RequiredDocIds())
        foreach (var lang in DocStore.Languages)
        {
            var page = DocStore.Get(id, lang);
            if (page is null)
            {
                issues.Add(new LintIssue(id, lang, "missing explanation page"));
                continue;
            }
            issues.AddRange(Check(page, DocHeadings.FindingRequired[lang], [DocHeadings.Generated[lang]]));
        }

        // Every label key used must exist in both languages.
        var labels = Labels.Current;
        foreach (var lang in DocStore.Languages)
        foreach (var key in labels.Keys("en").Where(k => !labels.Has(lang, k)))
            issues.Add(new LintIssue("labels.json", lang, $"missing label '{key}'"));
        return issues;
    }

    public static IEnumerable<LintIssue> Check(DocPage page, string[] required, string[] generated)
    {
        var lang = page.Language;
        if (string.IsNullOrWhiteSpace(page.Title)) yield return new LintIssue(page.Id, lang, "missing '# Title'");

        var headings = page.Sections.Select(s => s.Heading).ToList();
        var position = -1;
        foreach (var h in required)
        {
            var i = headings.IndexOf(h);
            if (i < 0) yield return new LintIssue(page.Id, lang, $"missing section '## {h}'");
            else if (i < position) yield return new LintIssue(page.Id, lang, $"section '## {h}' out of order");
            else position = i;
        }
        foreach (var g in generated.Where(headings.Contains))
            yield return new LintIssue(page.Id, lang, $"'## {g}' is generated and must not be hand-written");

        foreach (var s in page.Sections.Where(s => string.IsNullOrWhiteSpace(StripDirectives(s.Body))))
            yield return new LintIssue(page.Id, lang, $"section '## {s.Heading}' is empty");

        // Summary ≤ 200 characters per variant (each variant is shown alone).
        var summary = page.Section(DocHeadings.Summary(lang))?.Body ?? "";
        foreach (var variantText in SplitVariants(summary))
        {
            var plain = PlaceholderPattern().Replace(variantText, "xxxxxxxx").Trim();
            if (plain.Length > MaxSummaryLength)
                yield return new LintIssue(page.Id, lang, $"summary is {plain.Length} characters (max {MaxSummaryLength})");
        }

        foreach (var word in FindBannedWords(page.Raw))
            yield return new LintIssue(page.Id, lang, $"banned marketing word '{word}'");

        // Every [n] reference must resolve to an entry in the Sources section.
        var sources = page.Section(DocHeadings.Sources[lang])?.Body ?? "";
        var sourceNumbers = SourceEntryPattern().Matches(sources).Select(m => int.Parse(m.Groups[1].Value)).ToHashSet();
        var body = string.Join("\n", page.Sections.Where(s => s.Heading != DocHeadings.Sources[lang]).Select(s => s.Body));
        foreach (Match m in ReferencePattern().Matches(body))
        {
            var n = int.Parse(m.Groups[1].Value);
            if (!sourceNumbers.Contains(n)) yield return new LintIssue(page.Id, lang, $"reference [{n}] has no entry under '## {DocHeadings.Sources[lang]}'");
        }
    }

    private static string StripDirectives(string body) =>
        string.Join("\n", body.Split('\n').Where(l => !l.TrimStart().StartsWith(":::", StringComparison.Ordinal)));

    /// <summary>Returns the text shown for every status × variant combination (each is shown alone in the UI).</summary>
    private static IEnumerable<string> SplitVariants(string body)
    {
        var variants = VariantPattern().Matches(body)
            .SelectMany(m => m.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .Distinct().Cast<string?>().Append(null).ToList();
        foreach (var status in Enum.GetValues<FindingStatus>())
        foreach (var v in variants)
        {
            var finding = new Finding { Id = "lint", Kind = FindingKind.Finding, Status = status, Variant = v };
            yield return DocStore.ApplyBlocks(body, finding);
        }
    }

    [GeneratedRegex(@"\{\{\w+\}\}")]
    private static partial Regex PlaceholderPattern();

    [GeneratedRegex(@"(?<!\]\()\[(\d+)\](?!\()")]
    private static partial Regex ReferencePattern();

    [GeneratedRegex(@"^\s*(\d+)\.\s", RegexOptions.Multiline)]
    private static partial Regex SourceEntryPattern();

    [GeneratedRegex(@":::\s*variant\s+([\w,\s]+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex VariantPattern();
}
