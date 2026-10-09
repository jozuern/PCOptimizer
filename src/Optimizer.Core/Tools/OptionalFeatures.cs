using System.Text.RegularExpressions;
using Optimizer.Core.Actions;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tools;

public sealed class FeatureCatalog
{
    public List<FeatureEntry> Features { get; init; } = [];
}

public sealed class FeatureEntry
{
    /// <summary>DISM feature name.</summary>
    public string Name { get; init; } = "";

    public string Title { get; init; } = "";
    public string TitleDe { get; init; } = "";
    public string En { get; init; } = "";
    public string De { get; init; } = "";

    /// <summary>Starts the Windows hypervisor when on (VBS-related performance note, anti-cheat note).</summary>
    public bool Hypervisor { get; init; }

    /// <summary>"off" = recommended off (e.g. SMB1), "on" = needed by games when off, null = your choice.</summary>
    public string? Recommend { get; init; }

    public string Text(string lang) => lang == "de" && De.Length > 0 ? De : En;
    public string Label(string lang) => lang == "de" && TitleDe.Length > 0 ? TitleDe : Title;
}

/// <summary>
/// A Windows optional feature, switched with DISM (/English output, so the state is never localized). Most changes
/// need a restart; DISM exit code 3010 means "done, restart required".
/// </summary>
public sealed partial class OptionalFeatureAction : TweakAction
{
    public string Feature { get; init; } = "";
    public bool Enabled { get; init; }

    public override string TargetKey => $"feature:{Feature}".ToLowerInvariant();
    public override string Describe(ActionContext c) => $"Windows feature {Feature}";
    public override StoredValue Desired(ActionContext c) => new(true, "feature", Enabled ? "Enabled" : "Disabled");

    public override StoredValue? Read(ActionContext c) => ReadState(c.Processes, Feature) is { } s ? new StoredValue(true, "feature", s) : null;

    public override void Apply(ActionContext c) => Set(c.Processes, Feature, Enabled);

    public override void Restore(ActionContext c, StoredValue original)
    {
        if (original.Data is "Enabled" or "Disabled") Set(c.Processes, Feature, original.Data == "Enabled");
    }

    /// <summary>"Enabled", "Disabled", or null when the feature does not exist on this edition.</summary>
    public static string? ReadState(IProcessRunner processes, string feature)
    {
        if (!IsSafeName(feature)) return null;
        var (code, output) = processes.Run("dism.exe", $"/Online /English /Get-FeatureInfo /FeatureName:{feature}", TimeSpan.FromMinutes(2));
        if (code != 0) return null;
        var m = StateRegex().Match(output);
        if (!m.Success) return null;
        // "Enable Pending" / "Disable Pending" count as the target state (a restart finishes them).
        return m.Groups[1].Value.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) ? "Enabled" : "Disabled";
    }

    private static void Set(IProcessRunner processes, string feature, bool enabled)
    {
        if (!IsSafeName(feature)) throw new ArgumentException($"invalid feature name {feature}");
        var args = enabled ? $"/Online /English /Enable-Feature /FeatureName:{feature} /All /NoRestart" : $"/Online /English /Disable-Feature /FeatureName:{feature} /NoRestart";
        var (code, output) = processes.Run("dism.exe", args, TimeSpan.FromMinutes(30));
        if (code is not (0 or 3010)) throw new InvalidOperationException($"DISM failed ({code}): {output.Trim()}");
    }

    /// <summary>
    /// All feature states in one DISM call (/Get-Features /Format:Table, English): "Name | State" rows. Pending states
    /// count as their target state.
    /// </summary>
    public static IReadOnlyDictionary<string, string> ReadAll(IProcessRunner processes)
    {
        var (code, output) = processes.Run("dism.exe", "/Online /English /Get-Features /Format:Table", TimeSpan.FromMinutes(3));
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (code != 0) return map;
        foreach (var line in output.Split('\n'))
        {
            var parts = line.Split('|', StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[0].StartsWith("Feature Name", StringComparison.OrdinalIgnoreCase) || parts[0].StartsWith('-')) continue;
            var state = parts[1];
            if (!state.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) && !state.StartsWith("Disable", StringComparison.OrdinalIgnoreCase)) continue;
            map[parts[0]] = state.StartsWith("Enable", StringComparison.OrdinalIgnoreCase) ? "Enabled" : "Disabled";
        }
        return map;
    }

    public static bool IsSafeName(string name) => name.Length is > 0 and < 128 && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.');

    [GeneratedRegex(@"^State\s*:\s*(Enabled|Disabled|Enable Pending|Disable Pending|Disabled with Payload Removed)", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex StateRegex();

    /// <summary>Engine tweak for one feature switch (backup, undo, restart badge).</summary>
    public static TweakDefinition Tweak(FeatureEntry f, bool enabled) => new()
    {
        Id = $"feature.{(enabled ? "on" : "off")}.{f.Name.ToLowerInvariant()}",
        Docs = "feature.change",
        Subject = f.Title,
        Category = "Features",
        Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
        Risk = f.Hypervisor && enabled ? Risk.Moderate : Risk.Safe,
        Restart = true,
        Verify = "afterRestart",
        Hidden = true,
        Actions = [new OptionalFeatureAction { Feature = f.Name, Enabled = enabled }],
        Sources = ["https://learn.microsoft.com/en-us/windows-hardware/manufacture/desktop/enable-or-disable-windows-features-using-dism"],
    };
}
