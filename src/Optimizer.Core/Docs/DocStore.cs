using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Optimizer.Core.Catalog;
using Optimizer.Core.Findings;

namespace Optimizer.Core.Docs;

/// <summary>One explanation page (Catalog/Docs/&lt;lang&gt;/&lt;id&gt;.md).</summary>
public sealed record DocPage(string Id, string Language, string Title, IReadOnlyList<DocSection> Sections, string Raw)
{
    public DocSection? Section(string heading) => Sections.FirstOrDefault(s => s.Heading == heading);
}

public sealed record DocSection(string Heading, string Body);

/// <summary>Fixed headings per language, in this order. "What we found" is generated and must not be hand-written.</summary>
public static class DocHeadings
{
    public static readonly IReadOnlyDictionary<string, string[]> FindingRequired = new Dictionary<string, string[]>
    {
        ["en"] = ["Summary", "Why it matters", "How we detected it", "How to fix", "How to check the fix"],
        ["de"] = ["Zusammenfassung", "Warum das wichtig ist", "Wie wir es erkennen", "So behebst du es", "So prüfst du die Behebung"],
    };

    public static readonly IReadOnlyDictionary<string, string> Sources = new Dictionary<string, string> { ["en"] = "Sources", ["de"] = "Quellen" };

    public static readonly IReadOnlyDictionary<string, string> Generated = new Dictionary<string, string> { ["en"] = "What we found", ["de"] = "Was wir gefunden haben" };

    public static readonly IReadOnlyDictionary<string, string[]> TweakRequired = new Dictionary<string, string[]>
    {
        ["en"] = ["Summary", "How it works", "Why it can help", "Evidence", "Trade-offs & risks", "When not to use it"],
        ["de"] = ["Zusammenfassung", "So funktioniert es", "Warum es helfen kann", "Belege", "Nachteile & Risiken", "Wann du es nicht nutzen solltest"],
    };

    public static readonly IReadOnlyDictionary<string, string> GeneratedChanges = new Dictionary<string, string> { ["en"] = "What changes", ["de"] = "Was sich ändert" };
    public static readonly IReadOnlyDictionary<string, string> GeneratedUndo = new Dictionary<string, string> { ["en"] = "Undo", ["de"] = "Rückgängig machen" };

    public static string Summary(string lang) => FindingRequired[lang][0];
}

public static partial class DocStore
{
    public static readonly string[] Languages = ["en", "de"];

    private static readonly Dictionary<string, DocPage?> Cache = [];
    private static readonly Lock Gate = new();

    public static DocPage? Get(string id, string lang)
    {
        var key = $"{lang}/{id}";
        lock (Gate)
        {
            if (Cache.TryGetValue(key, out var cached)) return cached;
            var resource = $"Catalog.Docs.{lang}.{id}.md";
            var page = CatalogData.ResourceNames(resource).Any() ? Parse(id, lang, CatalogData.ReadResourceText(resource)) : null;
            Cache[key] = page;
            return page;
        }
    }

    public static IEnumerable<string> AllIds(string lang) =>
        CatalogData.ResourceNames($"Catalog.Docs.{lang}.").Select(n => n[$"Catalog.Docs.{lang}.".Length..^3]);

    public static DocPage Parse(string id, string lang, string markdown)
    {
        var text = markdown.Replace("\r\n", "\n");
        var title = "";
        var sections = new List<DocSection>();
        string? heading = null;
        var body = new StringBuilder();
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith("# ", StringComparison.Ordinal) && title.Length == 0)
            {
                title = line[2..].Trim();
                continue;
            }
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (heading is not null) sections.Add(new DocSection(heading, body.ToString().Trim()));
                heading = line[3..].Trim();
                body.Clear();
                continue;
            }
            if (heading is not null) body.AppendLine(line);
        }
        if (heading is not null) sections.Add(new DocSection(heading, body.ToString().Trim()));
        return new DocPage(id, lang, title, sections, markdown);
    }

    /// <summary>
    /// Builds the final Markdown for a finding: applies ":::variant x" / ":::if param" blocks and {{param}} placeholders,
    /// and inserts the generated "What we found" section after the summary.
    /// </summary>
    public static string RenderFinding(DocPage page, Finding finding, Labels labels)
    {
        var sb = new StringBuilder();
        foreach (var section in page.Sections)
        {
            var body = Substitute(ApplyBlocks(section.Body, finding), finding.Params).Trim();
            if (body.Length == 0) continue;
            sb.Append("## ").AppendLine(section.Heading).AppendLine().AppendLine(body).AppendLine();
            if (section.Heading == DocHeadings.Summary(page.Language) && finding.Facts.Count > 0)
            {
                sb.Append("## ").AppendLine(DocHeadings.Generated[page.Language]).AppendLine();
                foreach (var fact in finding.Facts)
                    sb.Append("- **").Append(labels.Get(page.Language, fact.LabelKey)).Append(":** ").AppendLine(labels.Value(page.Language, fact.Value));
                sb.AppendLine();
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Tweak page: inserts the generated "What changes" (exact targets, current -> new) after the summary and the
    /// generated "Undo" before the sources, so text and behavior cannot drift apart.
    /// </summary>
    public static string RenderTweak(DocPage page, IReadOnlyList<Actions.ChangeLine> changes, IReadOnlyList<string> notes, string undoText,
        IReadOnlyDictionary<string, string>? values = null)
    {
        var lang = page.Language;
        var dummy = new Finding { Id = page.Id, Kind = FindingKind.Finding, Status = FindingStatus.Info, Params = values ?? new Dictionary<string, string>() };
        var sb = new StringBuilder();
        void Generated(string heading, Action body)
        {
            sb.Append("## ").AppendLine(heading).AppendLine();
            body();
            sb.AppendLine();
        }
        var sourcesHeading = DocHeadings.Sources[lang];
        foreach (var section in page.Sections)
        {
            if (section.Heading == sourcesHeading)
                Generated(DocHeadings.GeneratedUndo[lang], () => sb.AppendLine(undoText));
            var body = Substitute(ApplyBlocks(section.Body, dummy), dummy.Params).Trim();
            if (body.Length == 0) continue;
            sb.Append("## ").AppendLine(section.Heading).AppendLine().AppendLine(body).AppendLine();
            if (section.Heading == DocHeadings.Summary(lang))
            {
                Generated(DocHeadings.GeneratedChanges[lang], () =>
                {
                    var (now, next) = lang == "de" ? ("jetzt", "neu") : ("now", "new");
                    foreach (var c in changes) sb.Append("- `").Append(c.Target).Append("`: ").Append(now).Append(' ').Append(Labels.Current.Display(lang, c.Before)).Append(", ").Append(next).Append(" **").Append(Labels.Current.Display(lang, c.After)).AppendLine("**");
                    foreach (var n in notes) sb.Append("- ").AppendLine(n);
                });
            }
        }
        if (!page.Sections.Any(s => s.Heading == sourcesHeading)) Generated(DocHeadings.GeneratedUndo[lang], () => sb.AppendLine(undoText));
        return sb.ToString();
    }

    public static string Summary(DocPage page, Finding finding) =>
        Substitute(ApplyBlocks(page.Section(DocHeadings.Summary(page.Language))?.Body ?? "", finding), finding.Params).Trim();

    /// <summary>Keeps ":::variant v" blocks only for the finding's variant, ":::if p" only when param p is non-empty.</summary>
    public static string ApplyBlocks(string body, Finding finding)
    {
        var sb = new StringBuilder();
        var keep = new Stack<bool>();
        foreach (var raw in body.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var trimmed = line.Trim();
            if (trimmed.StartsWith(":::", StringComparison.Ordinal))
            {
                var directive = trimmed[3..].Trim();
                // Keep block boundaries as paragraph breaks, so conditional text never runs into the paragraph before it.
                if (keep.Count == 0 || keep.Peek()) sb.AppendLine();
                if (directive.Length == 0)
                {
                    if (keep.Count > 0) keep.Pop();
                    continue;
                }
                var parts = directive.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var arg = parts.Length > 1 ? parts[1] : "";
                var parentKeep = keep.Count == 0 || keep.Peek();
                var cond = parts[0] switch
                {
                    "variant" => arg.Split(',', StringSplitOptions.TrimEntries).Contains(finding.Variant ?? "default"),
                    "status" => arg.Split(',', StringSplitOptions.TrimEntries).Contains(finding.Status.ToString(), StringComparer.OrdinalIgnoreCase),
                    "if" => finding.Params.TryGetValue(arg, out var v) && !string.IsNullOrEmpty(v),
                    "ifnot" => !finding.Params.TryGetValue(arg, out var v2) || string.IsNullOrEmpty(v2),
                    _ => true,
                };
                keep.Push(parentKeep && cond);
                continue;
            }
            if (keep.Count == 0 || keep.Peek()) sb.AppendLine(line);
        }
        return sb.ToString();
    }

    public static string Substitute(string text, IReadOnlyDictionary<string, string> values) =>
        PlaceholderRegex().Replace(text, m => values.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    [GeneratedRegex(@"\{\{(\w+)\}\}")]
    internal static partial Regex PlaceholderRegex();
}

/// <summary>Language-neutral labels for generated content (facts, values, statuses): Catalog/Data/labels.json.</summary>
public sealed class Labels
{
    private readonly Dictionary<string, Dictionary<string, string>> _byLang;

    private Labels(Dictionary<string, Dictionary<string, string>> byLang) => _byLang = byLang;

    private static readonly Lazy<Labels> Lazy = new(() =>
        new Labels(JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(CatalogData.ReadResourceText("Catalog.Data.labels.json"))
                   ?? []));

    public static Labels Current => Lazy.Value;

    public IReadOnlyCollection<string> Keys(string lang) => _byLang.TryGetValue(lang, out var d) ? d.Keys : [];

    public bool Has(string lang, string key) => _byLang.TryGetValue(lang, out var d) && d.ContainsKey(key);

    public string Get(string lang, string key) =>
        _byLang.TryGetValue(lang, out var d) && d.TryGetValue(key, out var v) ? v
        : _byLang.TryGetValue("en", out var en) && en.TryGetValue(key, out var e) ? e
        : key;

    /// <summary>Stored-value placeholders ("(not set)", "(unavailable)") in the UI language.</summary>
    public string Display(string lang, string value) => value switch
    {
        "(not set)" => Get(lang, "value.notSet"),
        "(unavailable)" or "(unreadable)" => Get(lang, "value.unknown"),
        _ => value,
    };

    /// <summary>Values starting with '@' are label keys ("@yes" -> "Yes"/"Ja").</summary>
    public string Value(string lang, string value) => value.StartsWith('@') ? Get(lang, "value." + value[1..]) : value;
}
