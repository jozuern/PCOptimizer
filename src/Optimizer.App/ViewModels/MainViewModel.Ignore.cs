using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Findings;

namespace Optimizer.App.ViewModels;

/// <summary>
/// Results the user ignores (<see cref="IgnoredResults"/>): an ignored result leaves the lists, the score, the problem
/// counts and the facts the recommendations read; it stays one click away under "Ignored" and comes back with Stop
/// ignoring.
/// </summary>
public sealed partial class MainViewModel
{
    private IReadOnlyList<Finding> _ignoredFindings = [];

    public ObservableCollection<FindingItemViewModel> IgnoredItems { get; } = [];

    [ObservableProperty] private string? _ignoredText;
    [ObservableProperty] private bool _showIgnored;

    /// <summary>The ignored results' keys, as a copy a background read can use while the list changes.</summary>
    private IReadOnlySet<string> IgnoredKeys() => _settings.IgnoredFindings.ToHashSet(StringComparer.Ordinal);

    /// <summary>Splits the profiled results into the shown ones and the ignored ones.</summary>
    private IReadOnlyList<Finding> WithoutIgnored(IReadOnlyList<Finding> profiled)
    {
        var keys = IgnoredKeys();
        _ignoredFindings = profiled.Where(f => IgnoredResults.IsIgnored(f, keys)).ToList();
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

    // Take any inspector row, as RunAction does: the buttons' parameter is the selected item, which is a tweak as often
    // as a result, and a typed command would throw when it asks whether it can run.
    [RelayCommand]
    private async Task IgnoreFindingAsync(InspectorItem? row)
    {
        if (row is not FindingItemViewModel item || !item.CanIgnore) return;
        var key = IgnoredResults.Key(item.Finding);
        if (!_settings.IgnoredFindings.Contains(key, StringComparer.Ordinal)) _settings.IgnoredFindings.Add(key);
        _settings.Save();
        await AfterIgnoreChangedAsync();
        ShowResult(Loc.Instance.Format("Findings_IgnoredResult", item.Title));
    }

    [RelayCommand]
    private async Task StopIgnoringAsync(InspectorItem? row)
    {
        if (row is not FindingItemViewModel item) return;
        _settings.IgnoredFindings.RemoveAll(k => k == IgnoredResults.Key(item.Finding));
        _settings.Save();
        await AfterIgnoreChangedAsync();
        ShowResult(Loc.Instance.Format("Findings_UnignoredResult", item.Title));
    }

    /// <summary>The facts change with the ignored results (what is recommended for them): read them again, then every list.</summary>
    private async Task AfterIgnoreChangedAsync()
    {
        await RefreshTweaksAsync();
        Rebuild();
    }
}
