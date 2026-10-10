using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

/// <summary>Anything the inspector can show: findings, advisor items and tweaks share one header and one document.</summary>
public abstract class InspectorItem : ObservableObject
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;

    public abstract string Key { get; }
    public string Title { get; protected init; } = "";
    public string Summary { get; protected init; } = "";
    private string? _markdown;
    private Func<string>? _markdownFactory;

    /// <summary>
    /// The inspector document, built on first use and off the UI thread: only the selected item needs it, and building it
    /// for tweaks reads the current values from the system (registry, power settings, bcdedit), which froze the window
    /// for a moment on every selection. Kept per item and language until the next scan or change
    /// (<see cref="ClearMarkdownCache"/>), so the rows a rebuild recreates do not read the system again.
    /// </summary>
    public Task<string> MarkdownAsync()
    {
        if (_markdown is { } ready) return Task.FromResult(ready);
        return _markdownTask ??= BuildMarkdownAsync();
    }

    protected string MarkdownText { init => _markdown = value; }

    private Task<string>? _markdownTask;
    private static readonly Dictionary<(string Key, string Lang), string> Cache = [];

    private async Task<string> BuildMarkdownAsync()
    {
        var cacheKey = (Key, Optimizer.App.Services.Loc.Instance.Language);
        if (Cache.TryGetValue(cacheKey, out var cached)) return _markdown = cached;
        var factory = _markdownFactory;
        var text = factory is null ? "" : await Task.Run(factory);
        Cache[cacheKey] = text;
        return _markdown = text;
    }

    /// <summary>After a scan or a change the "What changes" values are different: documents are built again.</summary>
    public static void ClearMarkdownCache() => Cache.Clear();

    protected Func<string> MarkdownFactory { init => _markdownFactory = value; }
    public string StatusText { get; protected init; } = "";

    /// <summary>Ok, Problem, Critical, Info, Unknown, Unsupported, Neutral: drives the Fluent status icon.</summary>
    public string Status { get; protected init; } = "";

    public string EffectsText { get; protected init; } = "";

    /// <summary>Secondary line of a row: status, impact, effects and badges, separated by commas.</summary>
    public string MetaText { get; protected init; } = "";

    /// <summary>Gaming impact 0-5 (null = not rated, e.g. game access and critical items).</summary>
    public int? Impact { get; protected init; }

    public string ImpactText { get; protected init; } = "";
    public string ImpactTooltip { get; protected init; } = "";
    public string? CriticalText { get; protected init; }
    public bool IsCritical => CriticalText is not null;

    /// <summary>Primary action in the inspector ("Fix", "Turn on", "Undo"); null = none.</summary>
    public string? ActionText { get; protected init; }

    public bool ActionEnabled { get; protected init; } = true;

    /// <summary>Why the action is not available (block reason), shown under the button.</summary>
    public string? ActionNote { get; protected init; }

    public bool HasAction => ActionText is not null;

    public SymbolRegular StatusSymbol => StatusIcons.Symbol(IsCritical ? "Critical" : Status);
    public string StatusBrushKey => StatusIcons.BrushKey(IsCritical ? "Critical" : Status);
}

/// <summary>Fluent status icons and the theme brush key for each status.</summary>
public static class StatusIcons
{
    public static SymbolRegular Symbol(string status) => status switch
    {
        "Ok" => SymbolRegular.CheckmarkCircle20,
        "Problem" => SymbolRegular.Warning20,
        "Critical" => SymbolRegular.ErrorCircle20,
        "Info" => SymbolRegular.Info20,
        "Unknown" => SymbolRegular.QuestionCircle20,
        "Unsupported" => SymbolRegular.Prohibited20,
        // Neutral (a switch that is off): a filled grey minus. An empty outline circle looked like a radio button to click.
        _ => SymbolRegular.SubtractCircle20,
    };

    public static string BrushKey(string status) => status switch
    {
        "Ok" => "SystemFillColorSuccessBrush",
        "Problem" => "SystemFillColorCautionBrush",
        "Critical" => "SystemFillColorCriticalBrush",
        "Info" => "AccentTextFillColorPrimaryBrush",
        _ => "TextFillColorTertiaryBrush",
    };
}
