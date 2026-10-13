using System.Text.Json;
using Optimizer.Core.Docs;
using Optimizer.Core.Logging;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.Services;

/// <summary>
/// The Changes page log in the UI language. The engine logs each change with its data (tweak id, apply or undo,
/// target, values); the log file keeps the English message for bug reports, the page shows this text instead.
/// </summary>
public static class ChangeLogText
{
    public static string Text(LogEntry entry, string lang)
    {
        if (entry.Data is not { } data) return entry.Message;
        try
        {
            using var doc = JsonDocument.Parse(data);
            var root = doc.RootElement;
            string? Get(string name) => root.TryGetProperty(name, out var v) && v.ValueKind is JsonValueKind.String or JsonValueKind.Number ? v.ToString() : null;
            if (Get("tweak") is not { } id || Get("kind") is not { } kind || Get("target") is not { } target) return Legacy(entry);
            var title = Title(id, lang);
            // The label (written since 1.0.1) renders the target in the UI language; older lines have only the English text.
            var label = root.TryGetProperty("label", out var l) && l.ValueKind == JsonValueKind.Object ? l.Deserialize<Optimizer.Core.Actions.TargetText>() : null;
            var where = (label is { } known ? ChangeRunner.ShortTarget(target, known) : ChangeRunner.ShortTarget(target)).Replace('\n', ' ');
            return kind switch
            {
                "apply" => Loc.Instance.Format("ChangeLog_Apply", title, where, Value(Get("before"), lang), Value(Get("after"), lang)),
                "undo" => Loc.Instance.Format("ChangeLog_Undo", title, where, Value(Get("restored"), lang)),
                _ => Legacy(entry),
            };
        }
        catch (JsonException)
        {
            return Legacy(entry);
        }
    }

    /// <summary>Placeholders such as "(not set)" in the UI language, as the confirmation shows them, also inside combined values.</summary>
    private static string Value(string? value, string lang)
    {
        if (value is null) return "?";
        var labels = Labels.Current;
        return labels.Display(lang, value).Replace("(not set)", labels.Get(lang, "value.notSet"), StringComparison.Ordinal);
    }

    /// <summary>Entries without the data (written by an older version): the message as logged.</summary>
    private static string Legacy(LogEntry entry) => entry.Message + (entry.Data is { } d ? "  " + d : "");

    /// <summary>
    /// Tweaks built at run time (startup entries, services, devices) seen in this session. Their definition lives in the
    /// backup, which an undo deletes: the log line of that undo still needs the title ("Delete startup entry: Steam").
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, TweakDefinition> Seen = new(StringComparer.Ordinal);

    public static void Remember(TweakDefinition t) => Seen[t.Id] = t;

    /// <summary>The catalog title, or a run-time tweak's title with its subject; else its page's title or the id.</summary>
    private static string Title(string id, string lang) =>
        (TweakCatalog.Current.Get(id) ?? Seen.GetValueOrDefault(id)) is { } t ? ChangeRunner.TitleOf(t, lang) : DocStore.GetOrEnglish(id, lang)?.Title ?? id;
}
