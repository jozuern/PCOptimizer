using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Hardware;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>
/// What a page's view model may use of the window: the result line and links (<see cref="IShell"/>), the last scan
/// (hardware profile, facts, tweak states), the change runner, and the details pane. Pages see this instead of
/// <see cref="MainViewModel"/>, which implements it.
/// </summary>
public interface IPageHost : IShell
{
    AppServices Services { get; }
    ChangeRunner Runner { get; }

    /// <summary>The hardware the last scan read; null before the first scan has finished.</summary>
    HardwareProfile? Profile { get; }

    Facts Facts { get; }
    bool ExpertMode { get; }
    HashSet<string> AppliedIds();
    IReadOnlyList<TweakStatus> DeviceStates { get; }
    IEnumerable<TweakItemViewModel> CatalogItems(Func<TweakDefinition, bool> filter);
    TweakDefinition? ResolveTweak(string id);

    /// <summary>A tweak's switch: confirm and apply, or undo (through the runner).</summary>
    Task ToggleAsync(TweakItemViewModel item, bool on);

    /// <summary>The scan that runs now, or a completed task.</summary>
    Task ScanInProgress { get; }

    IAsyncRelayCommand ScanCommand { get; }
    Page CurrentPage { get; }
    InspectorItem? SelectedItem { get; set; }
    string? VirusTotalKey { get; }
    string? PickSaveFile(string defaultName, string filterName, string extension);
}
