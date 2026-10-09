using CommunityToolkit.Mvvm.ComponentModel;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

/// <summary>Anything the inspector can show: findings, advisor items and tweaks share one header and one document.</summary>
public abstract class InspectorItem : ObservableObject
{
    public abstract string Key { get; }
    public string Title { get; protected init; } = "";
    public string Summary { get; protected init; } = "";
    public string Markdown { get; protected init; } = "";
    public string StatusText { get; protected init; } = "";

    /// <summary>Ok, Problem, Critical, Info, Unknown, Unsupported, Neutral: drives the Fluent status icon.</summary>
    public string Status { get; protected init; } = "";

    public string EffectsText { get; protected init; } = "";

    /// <summary>Secondary line of a row: status, impact, effects and badges, separated by commas.</summary>
    public string MetaText { get; protected init; } = "";

    /// <summary>Gaming impact 0-5 (null = not rated, e.g. game access and critical items).</summary>
    public int? Impact { get; protected init; }

    public bool HasImpact => Impact is not null;
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

    /// <summary>Neutral rows (a switch that is off) get a light outline circle instead of a filled icon.</summary>
    public bool StatusFilled => Status is not ("Neutral" or "");
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
        "Unsupported" => SymbolRegular.SubtractCircle20,
        _ => SymbolRegular.Circle20, // neutral: keeps titles aligned with rows that have a status icon
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
