using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Optimizer.App.Services;

/// <summary>
/// The banner over every page while a change, a cleanup or a long tool runs: what runs, "3 of 12" and the item, a bar
/// that fills when a count or percentage is known and moves without one, and Stop where the work can stop safely.
/// <para>
/// A run starts with <see cref="Begin"/> after its confirmation (the dialog is not covered by a banner about work that
/// has not started) and ends when the scope is disposed. The banner shows only after <see cref="ShowDelay"/>, so a switch
/// that is done at once does not flash it. A run started while another one shows (a scan of storage, then a change)
/// takes the banner until it ends. Called on the UI thread only (reports from other threads come through Progress&lt;T&gt;).
/// </para>
/// </summary>
public sealed partial class WorkProgress : ObservableObject
{
    public static TimeSpan ShowDelay { get; set; } = TimeSpan.FromMilliseconds(400);

    private readonly List<Scope> _scopes = [];
    private DispatcherTimer? _timer;

    [ObservableProperty] private bool _isShown;
    [ObservableProperty] private string _title = "";
    [ObservableProperty] private string? _count;
    [ObservableProperty] private string? _detail;
    [ObservableProperty] private double _value;
    /// <summary>
    /// The bar moves without a known count. False whenever no run is going on: a moving bar animates even while the banner
    /// is collapsed, and the window would never be idle (the pages preloaded at idle time waited seconds for it).
    /// </summary>
    [ObservableProperty] private bool _isIndeterminate;
    [ObservableProperty] private bool _canStop;
    [ObservableProperty] private bool _stopping;

    /// <summary>A run is going on (shown or still within <see cref="ShowDelay"/>).</summary>
    public bool IsActive => _scopes.Count > 0;

    /// <param name="stop">Cancelled by the Stop button; null when the work cannot stop half-way.</param>
    public Scope Begin(string title, CancellationTokenSource? stop = null)
    {
        var scope = new Scope(this, title, stop);
        _scopes.Add(scope);
        Show(scope);
        if (_scopes.Count == 1)
        {
            if (ShowDelay <= TimeSpan.Zero || Application.Current is null) IsShown = true;
            else
            {
                _timer = new DispatcherTimer { Interval = ShowDelay };
                _timer.Tick += (_, _) =>
                {
                    _timer?.Stop();
                    IsShown = IsActive;
                };
                _timer.Start();
            }
        }
        return scope;
    }

    [RelayCommand]
    private void Stop()
    {
        if (_scopes.LastOrDefault() is not { Stop: { } cts } scope) return;
        cts.Cancel();
        Show(scope);
    }

    private void End(Scope scope)
    {
        if (!_scopes.Remove(scope)) return;
        if (_scopes.LastOrDefault() is { } previous)
        {
            Show(previous);
            return;
        }
        _timer?.Stop();
        _timer = null;
        IsShown = false;
        IsIndeterminate = false;
    }

    private void Show(Scope s)
    {
        Title = s.Title;
        Count = s.Total > 0 ? Loc.Instance.Format("Work_Count", Math.Min(s.Done + 1, s.Total), s.Total) : null;
        Stopping = s.Stop?.IsCancellationRequested == true;
        Detail = Stopping ? Loc.Instance["Work_Stopping"] : s.Detail;
        IsIndeterminate = s.Percent is null;
        Value = s.Percent ?? 0;
        CanStop = s.Stop is not null && !Stopping;
    }

    private void Changed(Scope s)
    {
        if (_scopes.LastOrDefault() == s) Show(s);
    }

    /// <summary>One run in the banner. Dispose ends it.</summary>
    public sealed class Scope(WorkProgress owner, string title, CancellationTokenSource? stop) : IDisposable
    {
        public string Title { get; private set; } = title;
        internal CancellationTokenSource? Stop { get; } = stop;
        internal int Done { get; private set; }
        internal int Total { get; private set; }
        internal string? Detail { get; private set; }
        internal double? Percent { get; private set; }

        /// <summary>Item <paramref name="done"/> + 1 of <paramref name="total"/> starts; the bar shows what is done.</summary>
        public void Step(int done, int total, string? item)
        {
            (Done, Total, Detail) = (done, total, item);
            Percent = total > 0 ? 100.0 * Math.Clamp(done, 0, total) / total : null;
            owner.Changed(this);
        }

        /// <summary>A percentage the tool printed, or null for a moving bar; the count, if any, stays.</summary>
        public void Report(double? percent, string? detail)
        {
            Percent = percent is { } p ? Math.Clamp(p, 0, 100) : null;
            Detail = detail;
            owner.Changed(this);
        }

        /// <summary>A step of unknown length (the restore point): a moving bar, the count stays.</summary>
        public void Indeterminate(string? detail) => Report(null, detail);

        public void Rename(string title)
        {
            Title = title;
            owner.Changed(this);
        }

        public void Dispose() => owner.End(this);
    }
}
