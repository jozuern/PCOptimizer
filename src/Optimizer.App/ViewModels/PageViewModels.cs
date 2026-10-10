using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Actions;
using Optimizer.Core.Docs;
using Optimizer.Core.Logging;
using Optimizer.Core.Network;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>Shared plumbing: lazy first load, busy state, the owner for results and the inspector.</summary>
public abstract partial class PageViewModel(MainViewModel owner) : ObservableObject
{
    private Task? _load;

    protected MainViewModel Owner { get; } = owner;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isWorking;

    public Task EnsureLoadedAsync() => _load ??= ReloadAsync();

    partial void OnIsLoadingChanged(bool value) => LoadingChanged();

    /// <summary>For "nothing found" texts that must stay hidden while the page still loads.</summary>
    protected virtual void LoadingChanged()
    {
    }

    /// <summary>After a language switch: a page that was opened builds its rows again in the new language.</summary>
    public void OnLanguageChanged()
    {
        if (_load is not null) ReloadAsync().Forget($"{GetType().Name} language switch");
    }

    private Task? _running;
    private bool _reloadRequested;

    /// <summary>
    /// One load at a time: a reload asked for while one runs (Refresh, the first load and a reload after a change can
    /// overlap) runs once after it, so two loads never fill the same list at once.
    /// </summary>
    [RelayCommand]
    public Task ReloadAsync()
    {
        if (_running is { IsCompleted: false })
        {
            _reloadRequested = true;
            return _running;
        }
        return _running = ReloadUntilCurrentAsync();
    }

    private async Task ReloadUntilCurrentAsync()
    {
        do
        {
            _reloadRequested = false;
            await ReloadOnceAsync();
        }
        while (_reloadRequested);
    }

    private async Task ReloadOnceAsync()
    {
        IsLoading = true;
        try
        {
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Log.Error("ui", $"{GetType().Name} load failed", ex);
            Owner.ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    protected abstract Task LoadAsync();

    protected string Lang => Loc.Instance.Language;
}

/// <summary>A row with a switch that runs an engine tweak (startup entries, tasks, features). Same pattern as tweak rows.</summary>
public abstract partial class SwitchRow : ObservableObject
{
    private bool _switch;

    protected SwitchRow(bool isOn)
    {
        IsOn = isOn;
        _switch = isOn;
    }

    public bool IsOn { get; protected set; }
    public bool CanToggle { get; init; } = true;

    public bool SwitchState
    {
        get => _switch;
        set
        {
            if (value == _switch) return;
            _switch = value;
            OnPropertyChanged();
            if (value != IsOn) RunAsync(value).Forget("switch");
        }
    }

    private async Task RunAsync(bool on)
    {
        try
        {
            if (await ToggleAsync(on)) IsOn = on;
        }
        finally
        {
            _switch = IsOn;
            OnPropertyChanged(nameof(SwitchState));
        }
    }

    /// <summary>Returns true when the change was written.</summary>
    protected abstract Task<bool> ToggleAsync(bool on);
}

// ---------------- Graphics & network ----------------

public sealed record DnsResultRow(string Name, string Server, string Median, string Best, string Answered, bool IsBest);

public sealed partial class NetworkViewModel(MainViewModel owner, AppServices services) : PageViewModel(owner)
{
    public ObservableCollection<TweakItemViewModel> GameProfiles { get; } = [];
    public ObservableCollection<TweakItemViewModel> DeviceTweaks { get; } = [];
    public ObservableCollection<TweakItemViewModel> DnsOptions { get; } = [];
    public ObservableCollection<TweakItemViewModel> GpuTweaks { get; } = [];
    public ObservableCollection<DnsResultRow> DnsResults { get; } = [];

    [ObservableProperty] private string _currentDns = "";
    [ObservableProperty] private bool _hasNvidia;
    [ObservableProperty] private string _benchmarkStatus = "";

    public bool ShowDeviceTweaks => Owner.ExpertMode && DeviceTweaks.Count > 0;
    public bool GameProfilesEmpty => GameProfiles.Count == 0;

    protected override Task LoadAsync()
    {
        Rebuild();
        return Task.CompletedTask;
    }

    /// <summary>Rebuilt after every scan (device states come from the main scan).</summary>
    public void Rebuild()
    {
        var lang = Lang;
        var engine = services.Engine;
        GameProfiles.Clear();
        DeviceTweaks.Clear();
        foreach (var s in Owner.DeviceStates.OrderBy(s => s.Tweak.Subject, StringComparer.CurrentCultureIgnoreCase))
        {
            var item = new TweakItemViewModel(s, lang, engine, Owner, subjectAsTitle: true);
            if (s.Tweak.Id.StartsWith("nvidia.game.", StringComparison.Ordinal)) GameProfiles.Add(item);
            else DeviceTweaks.Add(item);
        }
        GpuTweaks.Clear();
        DnsOptions.Clear();
        foreach (var t in Owner.CatalogItems(t => t.Id.StartsWith("network.dns.", StringComparison.Ordinal))) DnsOptions.Add(t);
        foreach (var t in Owner.CatalogItems(t => t.Id.StartsWith("nvidia.", StringComparison.Ordinal))) GpuTweaks.Add(t);
        HasNvidia = Owner.Profile?.Gpus?.Any(g => g.Vendor == Optimizer.Core.Hardware.Vendor.Nvidia) == true;
        try
        {
            var servers = DnsBenchmark.CurrentServers();
            CurrentDns = servers.Count == 0 ? Loc.Instance["Net_NoDns"] : string.Join(", ", servers);
        }
        catch (Exception)
        {
            CurrentDns = Loc.Instance["Net_NoDns"];
        }
        OnPropertyChanged(nameof(ShowDeviceTweaks));
        OnPropertyChanged(nameof(GameProfilesEmpty));
    }

    private string? _fastestPresetId;
    [ObservableProperty] private string? _fastestText;

    /// <summary>Turns on the DNS preset of the fastest public server from the benchmark (with the usual confirmation).</summary>
    [RelayCommand]
    private void UseFastest()
    {
        if (DnsOptions.FirstOrDefault(o => o.Tweak.Id == _fastestPresetId) is { } preset) preset.SwitchState = true;
        FastestText = null;
    }

    [RelayCommand]
    private async Task RunDnsBenchmarkAsync()
    {
        FastestText = null;
        IsWorking = true;
        DnsResults.Clear();
        try
        {
            var current = DnsBenchmark.CurrentServers().Select((a, i) => (Loc.Instance.Format("Net_CurrentServer", i + 1), a));
            var servers = current.Concat(DnsBenchmark.PublicServers).DistinctBy(s => s.Item2).ToList();
            var progress = new Progress<string>(name => BenchmarkStatus = Loc.Instance.Format("Net_Testing", name));
            var results = await Task.Run(() => DnsBenchmark.RunAsync(servers, 2, progress));
            var best = results.FirstOrDefault(r => r.MedianMs is not null);
            // A preset for the fastest server, unless it is already in use.
            _fastestPresetId = best is null ? null : DnsBenchmark.PresetFor(best.Server);
            var preset = DnsOptions.FirstOrDefault(o => o.Tweak.Id == _fastestPresetId);
            FastestText = preset is not null && !preset.IsOn ? Loc.Instance.Format("Net_UseFastest", preset.Tweak.Subject ?? best!.Name) : null;
            foreach (var r in results)
                DnsResults.Add(new DnsResultRow(r.Name, r.Server.ToString(), r.MedianMs is { } m ? $"{m:0.0} ms" : Loc.Instance["Net_NoAnswer"],
                    r.BestMs is { } b ? $"{b:0.0} ms" : "", $"{r.Answered}/{r.Sent}", ReferenceEquals(r, best)));
            BenchmarkStatus = Loc.Instance["Net_BenchmarkDone"];
        }
        catch (Exception ex)
        {
            BenchmarkStatus = Loc.Instance.Format("Result_Error", ex.Message);
        }
        finally
        {
            IsWorking = false;
        }
    }
}

// ---------------- Debloat ----------------

public sealed partial class DebloatItem(Optimizer.Core.Debloat.DebloatItem item, string lang, bool elevated) : ObservableObject
{
    public Optimizer.Core.Debloat.DebloatItem Item { get; } = item;
    public string Name { get; } = item.Entry.Label(lang);
    public string PackageName => Item.Installed.Name;
    public string Text { get; } = item.Entry.Text(lang);
    public string Version => Item.Installed.Version;
    public string Group { get; } = Labels.Current.Get(lang, $"debloatGroup.{item.Entry.Group}");

    // Removing for all users needs administrator rights.
    public string? BlockText { get; } = item.BlockKey is { } k ? Labels.Current.Get(lang, k) : elevated ? null : Labels.Current.Get(lang, "block.notElevated");
    public bool CanRemove => BlockText is null;
}

public sealed record RemovedRow(string Name, string When, string? StoreLink);

public sealed partial class DebloatViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    private Optimizer.Core.Debloat.DebloatService Service => new(services.Context.Processes, AppServices.DataFolder);

    public ObservableCollection<DebloatItem> Items { get; } = [];
    public ObservableCollection<RemovedRow> Removed { get; } = [];

    [ObservableProperty] private string _oneDriveText = "";
    [ObservableProperty] private string? _oneDriveBlock;
    [ObservableProperty] private string? _cameBackText;
    [ObservableProperty] private bool _canUninstallOneDrive;

    public bool ItemsEmpty => !IsLoading && Items.Count == 0;

    protected override void LoadingChanged() => OnPropertyChanged(nameof(ItemsEmpty));
    public bool RemovedEmpty => Removed.Count == 0;

    protected override async Task LoadAsync()
    {
        var lang = Lang;
        var profile = Owner.Profile;
        var elevated = services.Elevation.IsElevated;
        var (offered, removed, oneDrive, cameBack) = await Task.Run(() =>
        {
            var service = Service;
            var installed = service.ListInstalled(allUsers: elevated);
            var offer = profile is null ? [] : Optimizer.Core.Debloat.DebloatService.Offer(Optimizer.Core.Catalog.CatalogData.Current.Appx, installed, profile, Optimizer.Core.Catalog.CatalogData.Current);
            var state = Optimizer.Core.Debloat.OneDrive.Read(services.Context.Registry, services.ProfilePath);
            var removedList = service.Removed();
            return (offer, removedList, state, Optimizer.Core.Debloat.DebloatService.CameBack(removedList, installed));
        });
        Items.Clear();
        foreach (var i in offered.OrderBy(i => i.Entry.Group).ThenBy(i => i.Entry.Label(lang))) Items.Add(new DebloatItem(i, lang, elevated));
        CameBackText = cameBack.Count == 0 ? null : Loc.Instance.Format("Debloat_CameBack", string.Join(", ", cameBack.Select(r => r.Name)));
        Removed.Clear();
        foreach (var r in removed.OrderByDescending(r => r.RemovedAt)) Removed.Add(new RemovedRow(r.Name, r.RemovedAt.LocalDateTime.ToString("g"), r.StoreLink));
        // "Not installed" is already the description; repeating it as a warning adds nothing.
        OneDriveBlock = oneDrive.Installed && oneDrive.BlockKey is { } b ? Labels.Current.Get(lang, b) : null;
        CanUninstallOneDrive = oneDrive.BlockKey is null;
        OneDriveText = oneDrive.Installed
            ? Loc.Instance.Format("Debloat_OneDriveInstalled", oneDrive.UserFolder ?? Loc.Instance["Debloat_OneDriveNotSignedIn"])
            : Loc.Instance["Debloat_OneDriveMissing"];
        OnPropertyChanged(nameof(ItemsEmpty));
        OnPropertyChanged(nameof(RemovedEmpty));
    }

    [RelayCommand]
    private async Task RemoveAsync(DebloatItem? item)
    {
        if (item is null || !item.CanRemove) return;
        var confirm = item.Item.Entry.CanReinstall ? "Debloat_ConfirmText" : "Debloat_ConfirmTextNoStore";
        if (!dialogs.Ask(Loc.Instance.Format("Debloat_ConfirmTitle", item.Name), Loc.Instance.Format(confirm, item.Text), Loc.Instance["Debloat_Remove"])) return;
        IsWorking = true;
        try
        {
            var error = await Task.Run(() => Service.Remove(item.Item.Installed, item.Item.Entry));
            Owner.ShowResult(error is null ? Loc.Instance.Format("Debloat_Removed", item.Name) : Loc.Instance.Format("Result_Error", error));
        }
        finally
        {
            IsWorking = false;
        }
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task UninstallOneDriveAsync()
    {
        var state = await Task.Run(() => Optimizer.Core.Debloat.OneDrive.Read(services.Context.Registry, services.ProfilePath));
        if (state.BlockKey is { } block)
        {
            Owner.ShowResult(Labels.Current.Get(Lang, block));
            return;
        }
        if (!dialogs.Ask(Loc.Instance["Debloat_OneDriveTitle"], Loc.Instance["Debloat_OneDriveConfirm"], Loc.Instance["Debloat_Remove"])) return;
        var path = Optimizer.Core.Debloat.OneDrive.Uninstall(state, services.Elevation);
        Owner.ShowResult(path == Optimizer.Core.Platform.DeElevatedLauncher.Path.Failed ? Loc.Instance.Format("Result_Error", "OneDriveSetup") : Loc.Instance["Debloat_OneDriveStarted"]);
    }

    [RelayCommand]
    private void OpenStore(string link) => Owner.OpenLinkCommand.Execute(link);
}

// ---------------- Cleanup ----------------

public sealed partial class CleanupRow(Optimizer.Core.Cleanup.CleanupCategory category, string lang) : ObservableObject
{
    public Optimizer.Core.Cleanup.CleanupCategory Category { get; } = category;
    public string Title { get; } = Labels.Current.Get(lang, category.Id);
    public string? Warning { get; } = category.WarningKey is { } k ? Labels.Current.Get(lang, k) : null;

    [ObservableProperty] private bool _selected = category.DefaultSelected;
    [ObservableProperty] private string _sizeText = "";
    [ObservableProperty] private long _bytes;
}

public sealed partial class CleanupViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    public ObservableCollection<CleanupRow> Rows { get; } = [];

    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private string _toolOutput = "";

    public static string Size(long bytes) => Optimizer.Core.Platform.ByteSize.Format(bytes);

    protected override async Task LoadAsync()
    {
        var lang = Lang;
        var categories = Optimizer.Core.Cleanup.CleanupEngine.Categories(services.ProfilePath, services.UserSid);
        Rows.Clear();
        foreach (var c in categories) Rows.Add(new CleanupRow(c, lang));
        foreach (var row in Rows.ToList())
        {
            var scan = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.Scan(row.Category));
            row.Bytes = scan.Bytes;
            row.SizeText = Loc.Instance.Format("Cleanup_Size", Size(scan.Bytes), scan.Files);
        }
        UpdateTotal();
    }

    private void UpdateTotal() => TotalText = Loc.Instance.Format("Cleanup_Total", Size(Rows.Where(r => r.Selected).Sum(r => r.Bytes)));

    [RelayCommand]
    private void SelectionChanged() => UpdateTotal();

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = Rows.Where(r => r.Selected && r.Bytes > 0).ToList();
        if (selected.Count == 0) return;
        var list = string.Join("\n", selected.Select(r => $"{r.Title}: {Size(r.Bytes)}"));
        if (!dialogs.Ask(Loc.Instance["Cleanup_ConfirmTitle"], Loc.Instance.Format("Cleanup_ConfirmText", list), Loc.Instance["Cleanup_Clean"])) return;
        IsWorking = true;
        long freed = 0;
        var skipped = 0;
        try
        {
            foreach (var r in selected)
            {
                var result = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.Clean(r.Category));
                freed += result.FreedBytes;
                skipped += result.Skipped;
            }
            Owner.ShowResult(Loc.Instance.Format("Cleanup_Done", Size(freed), skipped));
        }
        finally
        {
            IsWorking = false;
        }
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ComponentCleanupAsync()
    {
        if (!dialogs.Ask(Labels.Current.Get(Lang, "cleanup.componentStore"), Loc.Instance["Cleanup_ComponentText"], Loc.Instance["Cleanup_Clean"])) return;
        IsWorking = true;
        ToolOutput = Loc.Instance["Tools_Running"];
        try
        {
            var (code, output) = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.ComponentStoreCleanup(services.Context.Processes));
            ToolOutput = code == 0 ? Loc.Instance["Tools_Done"] : Loc.Instance.Format("Result_Error", output.Trim().Split('\n').LastOrDefault() ?? code.ToString());
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task DeliveryOptimizationCleanupAsync()
    {
        IsWorking = true;
        try
        {
            var (code, output) = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.DeliveryOptimizationCleanup(services.Context.Processes));
            ToolOutput = code == 0 ? Loc.Instance["Tools_Done"] : Loc.Instance.Format("Result_Error", output.Trim());
        }
        finally
        {
            IsWorking = false;
        }
    }
}
