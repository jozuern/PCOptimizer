using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Findings;

namespace Optimizer.App.ViewModels;

/// <summary>
/// Results the user ignores: a game library on a hard disk kept there on purpose (a backup, copying being faster than
/// downloading), a check that does not apply to how this PC is used. An ignored result leaves the lists, the score and
/// the problem counts; it stays one click away under "Ignored" and comes back with Stop ignoring. One result is
/// ignored, not the whole check: another library on a hard disk still shows.
/// </summary>
public sealed partial class MainViewModel
{
    private IReadOnlyList<Finding> _ignoredFindings = [];

    public ObservableCollection<FindingItemViewModel> IgnoredItems { get; } = [];

    [ObservableProperty] private string? _ignoredText;
    [ObservableProperty] private bool _showIgnored;

    /// <summary>The check and the one result it means: the instance, or the subject where a check has no instance key (a library path).</summary>
    public static string IgnoreKey(Finding f) => f.InstanceKey is null && f.Subject is { Length: > 0 } subject ? $"{f.Key}|{subject}" : f.Key;

    private bool IsIgnored(Finding f) => _settings.IgnoredFindings.Contains(IgnoreKey(f), StringComparer.Ordinal);

    /// <summary>Splits the profiled results into the shown ones and the ignored ones.</summary>
    private IReadOnlyList<Finding> WithoutIgnored(IReadOnlyList<Finding> profiled)
    {
        _ignoredFindings = profiled.Where(f => f.Kind != FindingKind.GameAccess && IsIgnored(f)).ToList();
        return _ignoredFindings.Count == 0 ? profiled : profiled.Where(f => !_ignoredFindings.Contains(f)).ToList();
    }

    private void BuildIgnored(string lang)
    {
        IgnoredItems.Clear();
        foreach (var f in _ignoredFindings) IgnoredItems.Add(new FindingItemViewModel(f, lang, null, _usage.Goal) { IsIgnored = true });
        IgnoredText = IgnoredItems.Count > 0 ? Loc.Instance.Format("Findings_IgnoredCount", IgnoredItems.Count) : null;
        if (IgnoredItems.Count == 0) ShowIgnored = false;
    }

    [RelayCommand]
    private void ToggleIgnored() => ShowIgnored = !ShowIgnored;

    [RelayCommand]
    private void IgnoreFinding(FindingItemViewModel? item)
    {
        if (item is null || !item.CanIgnore) return;
        var key = IgnoreKey(item.Finding);
        if (!_settings.IgnoredFindings.Contains(key, StringComparer.Ordinal)) _settings.IgnoredFindings.Add(key);
        _settings.Save();
        AfterIgnoreChanged();
        ShowResult(Loc.Instance.Format("Findings_IgnoredResult", item.Title));
    }

    [RelayCommand]
    private void StopIgnoring(FindingItemViewModel? item)
    {
        if (item is null) return;
        _settings.IgnoredFindings.RemoveAll(k => k == IgnoreKey(item.Finding));
        _settings.Save();
        AfterIgnoreChanged();
        ShowResult(Loc.Instance.Format("Findings_UnignoredResult", item.Title));
    }

    private void AfterIgnoreChanged()
    {
        ApplyProfile();
        RebuildFindings();
    }
}
