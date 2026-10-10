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
public abstract partial class PageViewModel : ObservableObject
{
    private Task? _load;

    protected PageViewModel(MainViewModel owner)
    {
        Owner = owner;
        // What a page read may no longer be true after any change (a switch on the page changes only its own row).
        owner.Runner.Changed += (_, _) => _dataStale = true;
    }

    protected MainViewModel Owner { get; }

    /// <summary>A change was made since the last full load: a language switch reads the page again instead of relabeling.</summary>
    private bool _dataStale;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _isWorking;

    public Task EnsureLoadedAsync() => _load ??= ReloadAsync();

    /// <summary>The page was opened (or its data loaded ahead) at least once.</summary>
    protected bool WasLoaded => _load is not null;

    /// <summary>
    /// Runs a page operation that changes the system (see <see cref="Services.ChangeRunner.RunExclusiveAsync"/>): the page
    /// shows it as working, no tweak, scan or update runs at the same time, and an error is logged and shown.
    /// </summary>
    protected async Task<bool> RunWorkAsync(string what, Func<Task> work)
    {
        IsWorking = true;
        try
        {
            return await Owner.Runner.RunExclusiveAsync(what, work);
        }
        finally
        {
            IsWorking = false;
            _dataStale = true;
        }
    }

    partial void OnIsLoadingChanged(bool value) => LoadingChanged();

    /// <summary>For "nothing found" texts that must stay hidden while the page still loads.</summary>
    protected virtual void LoadingChanged()
    {
    }

    /// <summary>
    /// After a language switch: a page that was opened builds its rows again in the new language, from the data it
    /// already read (<see cref="RelabelAsync"/>).
    /// </summary>
    public void OnLanguageChanged()
    {
        if (_load is not null) RunAsync(fullLoad: false).Forget($"{GetType().Name} language switch");
    }

    private Task? _running;
    private bool _reloadRequested;
    private bool _relabelRequested;

    /// <summary>The load that runs now, or a finished task (the --perf report waits for page loads with it).</summary>
    public Task Loading => _running ?? Task.CompletedTask;

    /// <summary>
    /// One load at a time: a reload asked for while one runs (Refresh, the first load and a reload after a change can
    /// overlap) runs once after it, so two loads never fill the same list at once.
    /// </summary>
    [RelayCommand]
    public Task ReloadAsync() => RunAsync(fullLoad: true);

    /// <summary>
    /// A relabel asked for while a load runs follows that load (its rows may be in the old language); a reload asked for
    /// during a relabel replaces the relabel.
    /// </summary>
    private Task RunAsync(bool fullLoad)
    {
        if (_running is { IsCompleted: false })
        {
            if (fullLoad) _reloadRequested = true;
            else _relabelRequested = true;
            return _running;
        }
        return _running = RunUntilCurrentAsync(fullLoad);
    }

    private async Task RunUntilCurrentAsync(bool fullLoad)
    {
        while (true)
        {
            _reloadRequested = false;
            _relabelRequested = false;
            await RunOnceAsync(fullLoad);
            if (_reloadRequested) fullLoad = true;
            else if (_relabelRequested) fullLoad = false;
            else return;
        }
    }

    private async Task RunOnceAsync(bool fullLoad)
    {
        if (_dataStale) fullLoad = true;
        // A change during this load marks the data stale again.
        if (fullLoad) _dataStale = false;
        // Relabeling reads nothing: no loading state, the rows only change their text.
        if (fullLoad) IsLoading = true;
        try
        {
            await (fullLoad ? LoadAsync() : RelabelAsync());
        }
        catch (Exception ex)
        {
            Log.Error("ui", $"{GetType().Name} load failed", ex);
            Owner.ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            if (fullLoad) IsLoading = false;
        }
    }

    /// <summary>Reads the system and builds the rows.</summary>
    protected abstract Task LoadAsync();

    /// <summary>
    /// Builds the rows again in the current language from what <see cref="LoadAsync"/> read last. Pages whose reading is
    /// slow (tools, the startup list, folder sizes) override it; the others read again.
    /// </summary>
    protected virtual Task RelabelAsync() => LoadAsync();

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
            // Return from the binding setter before the confirmation opens: a modal dialog inside the setter keeps the
            // switch's click (and a screen reader's toggle call) waiting until the dialog closes.
            await Task.Yield();
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

public sealed record DnsResultRow(string Name, string Server, string Median, string Best, string Answered)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;
}

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
        BuildRows();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rebuilt after every scan and switch (device states come from the main scan), once the page was opened: until
    /// then nobody sees the rows, and opening the page builds them.
    /// </summary>
    public void Rebuild()
    {
        if (WasLoaded) BuildRows();
    }

    private void BuildRows()
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
        ShowCurrentDnsAsync().Forget("current DNS servers");
        OnPropertyChanged(nameof(ShowDeviceTweaks));
        OnPropertyChanged(nameof(GameProfilesEmpty));
    }

    /// <summary>The DNS servers in use, read off the UI thread (it asks every network adapter).</summary>
    private async Task ShowCurrentDnsAsync()
    {
        IReadOnlyList<System.Net.IPAddress> servers;
        try
        {
            servers = await Task.Run(DnsBenchmark.CurrentServers);
        }
        catch (Exception)
        {
            servers = [];
        }
        CurrentDns = servers.Count == 0 ? Loc.Instance["Net_NoDns"] : string.Join(", ", servers);
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
            // Offered only when that preset can be switched on now (another preset of this app blocks it until undone).
            FastestText = preset is { IsOn: false, CanToggle: true } ? Loc.Instance.Format("Net_UseFastest", preset.Tweak.Subject ?? best!.Name) : null;
            foreach (var r in results)
                DnsResults.Add(new DnsResultRow(r.Name, r.Server.ToString(), r.MedianMs is { } m ? $"{m:0.0} ms" : Loc.Instance["Net_NoAnswer"],
                    r.BestMs is { } b ? $"{b:0.0} ms" : "", $"{r.Answered}/{r.Sent}"));
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
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;

    public Optimizer.Core.Debloat.DebloatItem Item { get; } = item;
    public string Name { get; } = item.Entry.Label(lang);
    public string PackageName => Item.Installed.Name;
    public string Text { get; } = item.Entry.Text(lang);
    public string Version => Item.Installed.Version;
    public string Group { get; } = Labels.Current.Get(lang, $"debloatGroup.{item.Entry.Group}");

    // Removing for all users needs administrator rights: the page says that once above the list, so the row only
    // shows reasons that belong to this app.
    public string? BlockText { get; } = item.BlockKey is { } k ? Labels.Current.Get(lang, k) : null;
    public bool CanRemove => BlockText is null && elevated;
}

public sealed record RemovedRow(string Name, string When, string? StoreLink)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;
}

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

    private sealed record DebloatData(
        IReadOnlyList<Optimizer.Core.Debloat.DebloatItem> Offered,
        IReadOnlyList<Optimizer.Core.Debloat.RemovedApp> Removed,
        Optimizer.Core.Debloat.OneDriveState OneDrive,
        IReadOnlyList<Optimizer.Core.Debloat.RemovedApp> CameBack);

    private DebloatData? _data;

    protected override async Task LoadAsync()
    {
        var profile = Owner.Profile;
        var elevated = services.Elevation.IsElevated;
        _data = await Task.Run(() =>
        {
            var service = Service;
            var installed = service.ListInstalled(allUsers: elevated);
            var offer = profile is null ? [] : Optimizer.Core.Debloat.DebloatService.Offer(Optimizer.Core.Catalog.CatalogData.Current.Appx, installed, profile, Optimizer.Core.Catalog.CatalogData.Current);
            var state = Optimizer.Core.Debloat.OneDrive.Read(services.Context.Registry, services.ProfilePath);
            var removedList = service.Removed();
            return new DebloatData(offer, removedList, state, Optimizer.Core.Debloat.DebloatService.CameBack(removedList, installed));
        });
        Show(_data);
    }

    // A language switch builds the rows from the app list already read (listing the apps starts PowerShell).
    protected override Task RelabelAsync()
    {
        if (_data is null) return LoadAsync();
        Show(_data);
        return Task.CompletedTask;
    }

    private void Show(DebloatData data)
    {
        var lang = Lang;
        var elevated = services.Elevation.IsElevated;
        var (offered, removed, oneDrive, cameBack) = data;
        Items.Clear();
        foreach (var i in offered.OrderBy(i => i.Entry.Group).ThenBy(i => i.Entry.Label(lang))) Items.Add(new DebloatItem(i, lang, elevated));
        _cameBack = cameBack.Select(r => r.Name).ToList();
        CameBackText = cameBack.Count == 0 ? null : Loc.Instance.Format("Debloat_CameBack", string.Join(", ", cameBack.Select(r => r.Name)));
        Removed.Clear();
        // The app name from the catalog (the record keeps the package name); the package name for apps no longer listed.
        var apps = Optimizer.Core.Catalog.CatalogData.Current.Appx.Apps;
        foreach (var r in removed.OrderByDescending(r => r.RemovedAt))
            Removed.Add(new RemovedRow(apps.FirstOrDefault(a => a.Name.Equals(r.Name, StringComparison.OrdinalIgnoreCase))?.Label(lang) ?? r.Name,
                r.RemovedAt.LocalDateTime.ToString("g"), r.StoreLink));
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
        await RunWorkAsync($"remove {item.Item.Entry.Name}", async () =>
        {
            var error = await Task.Run(() => Service.Remove(item.Item.Installed, item.Item.Entry));
            Owner.ShowResult(error is null ? Loc.Instance.Format("Debloat_Removed", item.Name) : Loc.Instance.Format("Result_Error", error),
                error is null ? Wpf.Ui.Controls.InfoBarSeverity.Success : Wpf.Ui.Controls.InfoBarSeverity.Error);
        });
        await ReloadAsync();
    }

    private IReadOnlyList<string> _cameBack = [];

    /// <summary>The user installed the apps again on purpose (for example with the Store link): stop the notice for them.</summary>
    [RelayCommand]
    private async Task DismissCameBackAsync()
    {
        var names = _cameBack;
        await Task.Run(() => Service.Forget(names));
        CameBackText = null;
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
        // Through the change gate like every other change: not while a tweak, a scan or an update runs.
        await RunWorkAsync("OneDrive uninstall", () =>
        {
            try
            {
                var path = Optimizer.Core.Debloat.OneDrive.Uninstall(state, services.Elevation);
                Owner.ShowResult(path == Optimizer.Core.Platform.DeElevatedLauncher.Path.Failed ? Loc.Instance.Format("Result_Error", "OneDriveSetup") : Loc.Instance["Debloat_OneDriveStarted"]);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // OneDriveSetup.exe could not be started (removed or blocked meanwhile).
                Owner.ShowResult(Loc.Instance.Format("Result_Error", $"OneDriveSetup: {ex.Message}"), Wpf.Ui.Controls.InfoBarSeverity.Error);
            }
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private void OpenStore(string link) => Owner.OpenLinkCommand.Execute(link);
}

// ---------------- Cleanup ----------------

public sealed partial class CleanupRow(Optimizer.Core.Cleanup.CleanupCategory category, string lang) : ObservableObject
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;

    public Optimizer.Core.Cleanup.CleanupCategory Category { get; } = category;
    public string Title { get; } = Labels.Current.Get(lang, category.Id);
    public string? Warning { get; } = category.WarningKey is { } k ? Labels.Current.Get(lang, k) : null;

    [ObservableProperty] private bool _selected = category.DefaultSelected;
    [ObservableProperty] private string _sizeText = "";
    [ObservableProperty] private long _bytes;

    /// <summary>The last size reading (kept for a language switch, which builds the rows again).</summary>
    public Optimizer.Core.Cleanup.CleanupScan? Scan { get; private set; }

    public void SetScan(Optimizer.Core.Cleanup.CleanupScan scan)
    {
        Scan = scan;
        Bytes = scan.Bytes;
        SizeText = Loc.Instance.Format("Cleanup_Size", CleanupViewModel.Size(scan.Bytes), scan.Files);
    }
}

public sealed partial class CleanupViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    public ObservableCollection<CleanupRow> Rows { get; } = [];

    [ObservableProperty] private string _totalText = "";
    [ObservableProperty] private string _toolOutput = "";

    public static string Size(long bytes) => Optimizer.Core.Platform.ByteSize.Format(bytes);

    protected override async Task LoadAsync()
    {
        // The categories look up folders (and the Windows folder's size), so off the UI thread like the sizes.
        var profilePath = services.ProfilePath;
        var userSid = services.UserSid;
        var categories = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.Categories(profilePath, userSid));
        var rows = BuildRows(categories);
        // The folders are measured side by side; each row shows its size as soon as it is known.
        await Task.WhenAll(rows.Select(async row => row.SetScan(await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.Scan(row.Category)))));
        UpdateTotal();
    }

    // A language switch builds the rows again with the sizes already measured.
    protected override Task RelabelAsync()
    {
        if (Rows.Count == 0 || Rows.Any(r => r.Scan is null)) return LoadAsync();
        var scans = Rows.Select(r => r.Scan!).ToList();
        var rows = BuildRows(Rows.Select(r => r.Category).ToList());
        for (var i = 0; i < rows.Count; i++) rows[i].SetScan(scans[i]);
        UpdateTotal();
        return Task.CompletedTask;
    }

    /// <summary>New rows in the current language; a row the user ticked or cleared keeps that choice (also after cleaning).</summary>
    private List<CleanupRow> BuildRows(IReadOnlyList<Optimizer.Core.Cleanup.CleanupCategory> categories)
    {
        var lang = Lang;
        var selected = Rows.GroupBy(r => r.Category.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().Selected, StringComparer.Ordinal);
        Rows.Clear();
        foreach (var c in categories)
            Rows.Add(new CleanupRow(c, lang) { Selected = selected.TryGetValue(c.Id, out var on) ? on : c.DefaultSelected });
        return Rows.ToList();
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
        long freed = 0;
        var skipped = 0;
        await RunWorkAsync("cleanup", async () =>
        {
            foreach (var r in selected)
            {
                var result = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.Clean(r.Category));
                freed += result.FreedBytes;
                skipped += result.Skipped;
            }
            Owner.ShowResult(Loc.Instance.Format("Cleanup_Done", Size(freed), skipped), Wpf.Ui.Controls.InfoBarSeverity.Success);
        });
        await ReloadAsync();
    }

    [RelayCommand]
    private async Task ComponentCleanupAsync()
    {
        if (!dialogs.Ask(Labels.Current.Get(Lang, "cleanup.componentStore"), Loc.Instance["Cleanup_ComponentText"], Loc.Instance["Cleanup_Clean"])) return;
        ToolOutput = Loc.Instance["Tools_Running"];
        if (!await RunWorkAsync("component store cleanup", async () =>
            {
                var (code, output) = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.ComponentStoreCleanup(services.Context.Processes));
                ToolOutput = code == 0 ? Loc.Instance["Tools_Done"] : Loc.Instance.Format("Result_Error", output.Trim().Split('\n').Select(l => l.Trim()).LastOrDefault(l => l.Length > 0) ?? code.ToString());
            }))
            ToolOutput = "";
    }

    [RelayCommand]
    private async Task DeliveryOptimizationCleanupAsync()
    {
        await RunWorkAsync("delivery optimization cleanup", async () =>
        {
            var (code, output) = await Task.Run(() => Optimizer.Core.Cleanup.CleanupEngine.DeliveryOptimizationCleanup(services.Context.Processes));
            ToolOutput = code == 0 ? Loc.Instance["Tools_Done"] : Loc.Instance.Format("Result_Error", output.Trim());
        });
    }
}
