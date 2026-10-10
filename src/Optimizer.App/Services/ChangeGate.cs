using CommunityToolkit.Mvvm.ComponentModel;

namespace Optimizer.App.Services;

/// <summary>
/// Whether a change may start now. Only one change runs at a time, and none while a scan reads the system (its result
/// would be stale at once) or while the app replaces itself with an update. Switches and change buttons bind to
/// <see cref="CanChange"/>; the runner checks it again, because a click can arrive before the binding updates.
/// </summary>
public sealed partial class ChangeGate : ObservableObject
{
    public static ChangeGate Instance { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChange))]
    private bool _busy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChange))]
    private bool _scanning;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanChange))]
    private bool _updating;

    public bool CanChange => !Busy && !Scanning && !Updating;
}
