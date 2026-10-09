using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

/// <summary>Turns a switch into apply or undo: undo when this app made the opposite change, else apply the new state.</summary>
internal static class Switching
{
    public static async Task<bool> SetAsync(AppServices services, ChangeRunner runner, Func<bool, TweakDefinition?> build, bool on)
    {
        if (build(!on) is { } opposite && services.Store.Get(opposite.Id) is not null) return await runner.UndoAsync(opposite);
        return build(on) is { } t && await runner.ApplyAsync(t);
    }
}

// ---------------- Startup ----------------

public sealed partial class StartupRow : SwitchRow
{
    private readonly AppServices _services;
    private readonly ChangeRunner _runner;

    public StartupRow(StartupEntry entry, string lang, AppServices services, ChangeRunner runner) : base(entry.Enabled ?? true)
    {
        _services = services;
        _runner = runner;
        Entry = entry;
        KindText = Labels.Current.Get(lang, $"startupKind.{entry.Kind}");
        CanToggle = entry.Enabled is not null && StartupTweaks.Set(entry, !(entry.Enabled ?? true)) is not null;
        ReadOnlyNote = CanToggle ? null : Loc.Instance["Startup_ReadOnly"];
    }

    public StartupEntry Entry { get; }
    public string Name => Entry.Name;
    public string KindText { get; }
    public string Location => Entry.Location;
    public string? Command => Entry.Command;
    public string? ReadOnlyNote { get; }
    public bool Suspicious => Entry.Suspicious;

    [ObservableProperty] private string _publisherText = "";
    [ObservableProperty] private string _signatureText = "";
    [ObservableProperty] private bool _isMicrosoft;
    [ObservableProperty] private bool _needsAttention;
    [ObservableProperty] private string? _virusTotalText;
    [ObservableProperty] private string? _virusTotalUrl;

    public void SetSignature(SignatureInfo s, string lang)
    {
        IsMicrosoft = s.IsMicrosoft;
        PublisherText = s.Publisher ?? Loc.Instance["Startup_NoPublisher"];
        SignatureText = Labels.Current.Get(lang, $"signature.{s.Status}");
        NeedsAttention = Entry.Suspicious || s.Status is SignatureStatus.Unsigned or SignatureStatus.Invalid;
    }

    protected override Task<bool> ToggleAsync(bool on) => Switching.SetAsync(_services, _runner, enabled => StartupTweaks.Set(Entry, enabled), on);
}

public sealed record FilterOption(string Key, string Text);

public sealed partial class StartupViewModel(MainViewModel owner, AppServices services, ChangeRunner runner) : PageViewModel(owner)
{
    private List<StartupRow> _all = [];
    private CancellationTokenSource? _vtCancel;

    public ObservableCollection<StartupRow> Rows { get; } = [];
    public ObservableCollection<FilterOption> Kinds { get; } = [];

    [ObservableProperty] private bool _hideMicrosoft = true;
    [ObservableProperty] private FilterOption? _selectedKind;
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private string _virusTotalStatus = "";
    [ObservableProperty] private bool _isCheckingVirusTotal;

    partial void OnHideMicrosoftChanged(bool value) => Filter();
    partial void OnSelectedKindChanged(FilterOption? value) => Filter();

    protected override async Task LoadAsync()
    {
        var lang = Lang;
        var scanner = new StartupScanner(services.Context.Registry, services.Context.Tasks, services.ProfilePath);
        var entries = await Task.Run(() => scanner.ScanAll());
        _all = entries.Select(e => new StartupRow(e, lang, services, runner)).ToList();
        if (Kinds.Count == 0)
        {
            Kinds.Add(new FilterOption("", Loc.Instance["Startup_AllKinds"]));
            foreach (var k in new[] { StartupKind.RunKey, StartupKind.StartupFolder, StartupKind.LogonTask, StartupKind.Service, StartupKind.Driver, StartupKind.ShellExtension, StartupKind.Winlogon, StartupKind.ImageHijack, StartupKind.AppInit, StartupKind.PolicyRun })
                Kinds.Add(new FilterOption(k.ToString(), Labels.Current.Get(lang, $"startupKind.{k}")));
            // Set the field directly: Filter() runs right after.
#pragma warning disable MVVMTK0034
            _selectedKind = Kinds[0];
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(SelectedKind));
        }
        Filter();
        // Signatures in the background: Microsoft entries are hidden once verified.
        var rows = _all.ToList();
        await Task.Run(() => Parallel.ForEach(rows, new ParallelOptions { MaxDegreeOfParallelism = 4 }, row =>
        {
            var sig = SignatureVerifier.Verify(row.Entry.ImagePath);
            System.Windows.Application.Current.Dispatcher.Invoke(() => row.SetSignature(sig, lang));
        }));
        Filter();
    }

    private void Filter()
    {
        var kind = SelectedKind?.Key ?? "";
        Rows.Clear();
        foreach (var r in _all.Where(r => (kind.Length == 0 || r.Entry.Kind.ToString() == kind) && !(HideMicrosoft && r.IsMicrosoft && !r.Suspicious))
                     .OrderByDescending(r => r.NeedsAttention).ThenBy(r => r.Entry.Kind).ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            Rows.Add(r);
        var atLogon = _all.Count(r => StartupTweaks.CountsForF16(r.Entry) && !r.IsMicrosoft);
        SummaryText = Loc.Instance.Format("Startup_Summary", _all.Count, atLogon, _all.Count(r => r.NeedsAttention));
    }

    /// <summary>Looks up the hashes of shown entries that are not signed by Microsoft (opt-in, own API key).</summary>
    [RelayCommand]
    private async Task CheckVirusTotalAsync()
    {
        if (Owner.VirusTotalKey is not { Length: > 0 } key)
        {
            VirusTotalStatus = Loc.Instance["Vt_NoKey"];
            return;
        }
        _vtCancel = new CancellationTokenSource();
        IsCheckingVirusTotal = true;
        var lang = Lang;
        try
        {
            using var client = new VirusTotalClient(key);
            var targets = Rows.Where(r => !r.IsMicrosoft && r.Entry.ImagePath is { } p && File.Exists(p))
                .GroupBy(r => r.Entry.ImagePath!, StringComparer.OrdinalIgnoreCase).ToList();
            var done = 0;
            foreach (var g in targets)
            {
                _vtCancel.Token.ThrowIfCancellationRequested();
                VirusTotalStatus = Loc.Instance.Format("Vt_Progress", ++done, targets.Count);
                var hash = await Task.Run(() => VirusTotalClient.Sha256(g.Key));
                var result = await client.LookupAsync(hash, _vtCancel.Token);
                var text = Labels.Current.Get(lang, $"vt.{result.Verdict}") +
                           (result.Verdict is VirusTotalVerdict.Malicious or VirusTotalVerdict.Suspicious ? $" ({result.Malicious + result.Suspicious}/{result.Total})" : "");
                foreach (var row in g)
                {
                    row.VirusTotalText = text;
                    row.VirusTotalUrl = result.Verdict is VirusTotalVerdict.NotFound or VirusTotalVerdict.Error ? null : result.ReportUrl;
                }
                if (result.Verdict is VirusTotalVerdict.InvalidKey) break;
            }
            VirusTotalStatus = Loc.Instance["Vt_Done"];
        }
        catch (OperationCanceledException)
        {
            VirusTotalStatus = Loc.Instance["Vt_Stopped"];
        }
        finally
        {
            IsCheckingVirusTotal = false;
        }
    }

    [RelayCommand]
    private void StopVirusTotal() => _vtCancel?.Cancel();

    [RelayCommand]
    private void OpenReport(string? url)
    {
        if (url is not null) Owner.OpenLinkCommand.Execute(url);
    }
}

// ---------------- Services ----------------

public sealed record StartOption(ServiceStart Start, string Text);

public sealed partial class ServiceRowVm : ObservableObject
{
    private readonly ServicesViewModel _page;
    private bool _suppress;

    public ServiceRowVm(ServiceRow row, string lang, ServicesViewModel page)
    {
        _page = page;
        Row = row;
        Note = row.Note?.Text(lang);
        StartText = row.Start is { } s ? Labels.Current.Get(lang, $"serviceStart.{s}") : "?";
        StateText = Loc.Instance[row.Running ? "Svc_Running" : "Svc_Stopped"];
        Publisher = row.Signature?.Publisher ?? "";
        MetaText = string.Join(", ", new[] { row.Name, StateText, Publisher }.Where(x => x.Length > 0));
        Options = row.Edit switch
        {
            ServiceEdit.ManualOnly => [Opt(ServiceStart.Manual, lang)],
            ServiceEdit.Full => [Opt(ServiceStart.Automatic, lang), Opt(ServiceStart.AutomaticDelayed, lang), Opt(ServiceStart.Manual, lang), Opt(ServiceStart.Disabled, lang)],
            _ => [],
        };
        if (row.Start is { } current && Options.All(o => o.Start != current)) Options = [Opt(current, lang), .. Options];
        _selected = Options.FirstOrDefault(o => o.Start == row.Start);
        EditText = row.Edit switch
        {
            ServiceEdit.ReadOnly => Loc.Instance["Svc_ReadOnly"],
            ServiceEdit.ManualOnly => row.Note?.Mode == "warn" ? Loc.Instance["Svc_Warn"] : null,
            _ => null,
        };
    }

    private static StartOption Opt(ServiceStart s, string lang) => new(s, Labels.Current.Get(lang, $"serviceStart.{s}"));

    public ServiceRow Row { get; }
    public string Name => Row.Name;
    public string DisplayName => Row.DisplayName;
    public string? Description => Row.Description;
    public string? Note { get; }
    public string StartText { get; }
    public string StateText { get; }
    public string Publisher { get; }
    public string MetaText { get; }
    public string? EditText { get; }
    public IReadOnlyList<StartOption> Options { get; }
    public bool CanEdit => Row.Edit != ServiceEdit.ReadOnly && Options.Count > 1;

    [ObservableProperty] private StartOption? _selected;

    partial void OnSelectedChanged(StartOption? oldValue, StartOption? newValue)
    {
        if (_suppress || newValue is null || newValue.Start == Row.Start) return;
        _ = ChangeAsync(oldValue, newValue);
    }

    private async Task ChangeAsync(StartOption? oldValue, StartOption newValue)
    {
        var written = await _page.ChangeStartAsync(this, newValue.Start);
        if (written) return;
        _suppress = true;
        Selected = oldValue; // cancelled: show the real start type again
        _suppress = false;
    }
}

public sealed partial class ServicesViewModel(MainViewModel owner, AppServices services, ChangeRunner runner) : PageViewModel(owner)
{
    private List<ServiceRowVm> _all = [];

    public ObservableCollection<ServiceRowVm> Rows { get; } = [];
    public ObservableCollection<StartupRow> Tasks { get; } = [];

    [ObservableProperty] private string _search = "";
    [ObservableProperty] private bool _showWindowsServices;
    [ObservableProperty] private bool _showMicrosoftTasks;

    partial void OnSearchChanged(string value) => Filter();
    partial void OnShowWindowsServicesChanged(bool value) => Filter();
    partial void OnShowMicrosoftTasksChanged(bool value) => _ = ReloadAsync();

    protected override async Task LoadAsync()
    {
        var lang = Lang;
        var rows = await Task.Run(() =>
        {
            var list = ServiceManager.List(services.Context.Services, CatalogData.Current.Services);
            // Signature decides "Microsoft" for services the catalog does not explain.
            return list.AsParallel().WithDegreeOfParallelism(4).Select(r => r.Note is null ? r with { Signature = SignatureVerifier.Verify(r.File) } : r).ToList();
        });
        _all = rows.OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(r => new ServiceRowVm(r, lang, this)).ToList();
        Filter();

        var showMs = ShowMicrosoftTasks;
        var tasks = await Task.Run(() => services.Context.Tasks.List());
        Tasks.Clear();
        foreach (var t in tasks.Where(t => showMs || !t.Path.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Path, StringComparer.OrdinalIgnoreCase))
        {
            var entry = new StartupEntry(StartupKind.LogonTask, t.Path.TrimStart('\\'), t.Command, CommandLine.ImagePath(t.Command is null ? null : $"\"{t.Command}\""),
                t.AtLogon ? Loc.Instance["Task_AtLogon"] : t.AtBoot ? Loc.Instance["Task_AtBoot"] : Loc.Instance["Task_Other"], Hive.Machine, t.Enabled, $"task:{t.Path}")
            {
                Target = t.Path,
            };
            Tasks.Add(new StartupRow(entry, lang, services, runner));
        }
    }

    private void Filter()
    {
        var q = Search.Trim();
        Rows.Clear();
        foreach (var r in _all.Where(r => (ShowWindowsServices || r.Row.Edit != ServiceEdit.ReadOnly)
                                          && (q.Length == 0 || r.DisplayName.Contains(q, StringComparison.CurrentCultureIgnoreCase) || r.Name.Contains(q, StringComparison.OrdinalIgnoreCase))))
            Rows.Add(r);
    }

    public async Task<bool> ChangeStartAsync(ServiceRowVm row, ServiceStart start)
    {
        // Back to the original start type of a change this app made: undo that change instead.
        var previous = services.Store.All().FirstOrDefault(b => b.TweakId.StartsWith("service.", StringComparison.Ordinal) && b.TweakId.EndsWith("." + Slug(row.Name), StringComparison.Ordinal));
        if (previous is not null && Owner.ResolveTweak(previous.TweakId) is { } t
            && previous.Entries.FirstOrDefault()?.Original.Data == start.ToString())
            return await runner.UndoAsync(t) && await ReloadThenTrue();
        if (ServiceManager.Change(row.Row, start) is not { } tweak) return false;
        var written = await runner.ApplyAsync(tweak);
        if (written) await ReloadAsync();
        return written;
    }

    private async Task<bool> ReloadThenTrue()
    {
        await ReloadAsync();
        return true;
    }

    private static string Slug(string name) => new(name.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).ToArray());
}

// ---------------- Apps & drivers ----------------

public sealed partial class AppRow(AppEntry app, string lang, bool installed) : ObservableObject
{
    public AppEntry App { get; } = app;
    public string Name => App.Name;
    public string Text { get; } = app.Text(lang);
    public string Category { get; } = Labels.Current.Get(lang, $"appCategory.{app.Category}");
    public bool PerUser => App.Scope == "user";

    public Wpf.Ui.Controls.SymbolRegular Symbol => App.Category switch
    {
        "launchers" => Wpf.Ui.Controls.SymbolRegular.Games24,
        "communication" => Wpf.Ui.Controls.SymbolRegular.Chat24,
        "monitoring" => Wpf.Ui.Controls.SymbolRegular.Gauge24,
        "media" => Wpf.Ui.Controls.SymbolRegular.Play24,
        "browsers" => Wpf.Ui.Controls.SymbolRegular.Globe24,
        "runtimes" => Wpf.Ui.Controls.SymbolRegular.Box24,
        _ => Wpf.Ui.Controls.SymbolRegular.Toolbox24,
    };

    [ObservableProperty] private bool _installed = installed;
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _stateText = installed ? Loc.Instance["Apps_Installed"] : "";
}

public sealed record DriverItem(string Device, string ClassText, string Provider, string Version, string Date, string Age, string? UpdateUrl, bool Old);

public sealed partial class AppsViewModel(MainViewModel owner, AppServices services) : PageViewModel(owner)
{
    private string? _winget;

    public ObservableCollection<AppRow> Apps { get; } = [];
    public ObservableCollection<DriverItem> Drivers { get; } = [];

    [ObservableProperty] private string _output = "";
    [ObservableProperty] private bool _wingetMissing;

    protected override async Task LoadAsync()
    {
        var lang = Lang;
        var programs = Owner.Profile?.Extras?.Programs ?? [];
        _winget = await Task.Run(() => Winget.Find(services.ProfilePath));
        WingetMissing = _winget is null;
        Apps.Clear();
        foreach (var a in CatalogData.Current.Apps.Apps) Apps.Add(new AppRow(a, lang, a.IsInstalled(programs)));

        var vendor = CatalogData.Current.Bios.NormalizeVendor(Owner.Profile?.Firmware?.BoardManufacturer);
        var board = BiosAgeCheck.SupportUrl(vendor);
        var drivers = await Task.Run(DriverInventory.Read);
        Drivers.Clear();
        var today = DateTime.Today;
        foreach (var d in drivers)
        {
            var age = d.AgeDays(today);
            Drivers.Add(new DriverItem(d.Device, Loc.Instance[$"DrvClass_{d.Class}"] is var c && c != $"DrvClass_{d.Class}" ? c : d.Class,
                d.Provider ?? "", d.Version ?? "", d.Date?.ToString("yyyy-MM-dd") ?? "", age is { } a ? Loc.Instance.Format("Drv_AgeDays", a) : "",
                DriverInventory.UpdateUrl(d, board), age > 2 * 365 && d.Class is "DISPLAY" or "NET" or "MEDIA"));
        }
    }

    [RelayCommand]
    private async Task InstallAsync(AppRow? row)
    {
        if (row is null || _winget is null) return;
        if (row.PerUser)
        {
            var path = Winget.InstallForUser(_winget, row.App, services.Elevation);
            row.StateText = path == DeElevatedLauncher.Path.Failed ? Loc.Instance.Format("Result_Failed", "winget") : Loc.Instance["Apps_StartedForUser"];
            return;
        }
        row.IsInstalling = true;
        row.StateText = Loc.Instance["Apps_Installing"];
        var lines = new List<string>();
        var progress = new Progress<string>(l =>
        {
            lines.Add(l);
            if (lines.Count > 40) lines.RemoveAt(0);
            Output = string.Join("\n", lines);
        });
        try
        {
            var code = await Winget.InstallAsync(_winget, row.App, progress, CancellationToken.None);
            row.Installed = Winget.IsSuccess(code);
            row.StateText = row.Installed ? Loc.Instance["Apps_Installed"] : Loc.Instance.Format("Apps_Failed", $"0x{code:X8}");
        }
        catch (Exception ex)
        {
            Log.Error("apps", $"install {row.App.Id} failed", ex);
            row.StateText = Loc.Instance.Format("Result_Failed", ex.Message);
        }
        finally
        {
            row.IsInstalling = false;
        }
    }

    [RelayCommand]
    private void OpenDriverPage(string? url)
    {
        if (url is not null) Owner.OpenLinkCommand.Execute(url);
    }
}

// ---------------- Tools ----------------

public sealed partial class FileRow(string path, long bytes, string meta, bool canDelete) : ObservableObject
{
    public string Path { get; } = path;
    public string SizeText { get; } = CleanupViewModel.Size(bytes);
    public string Meta { get; } = meta;
    public bool CanDelete { get; } = canDelete;

    [ObservableProperty] private bool _selected;
}

public sealed record FolderRow(string Path, string SizeText, string Files);

public sealed partial class FeatureRow : SwitchRow
{
    private readonly AppServices _services;
    private readonly ChangeRunner _runner;

    public FeatureRow(FeatureEntry entry, bool enabled, string lang, AppServices services, ChangeRunner runner) : base(enabled)
    {
        _services = services;
        _runner = runner;
        Entry = entry;
        Title = entry.Label(lang);
        Text = entry.Text(lang);
        Note = entry.Recommend == "off" && enabled ? Loc.Instance["Feat_RecommendOff"] : entry.Hypervisor ? Loc.Instance["Feat_Hypervisor"] : null;
    }

    public FeatureEntry Entry { get; }
    public string Title { get; }
    public string Text { get; }
    public string? Note { get; }

    protected override Task<bool> ToggleAsync(bool on) => Switching.SetAsync(_services, _runner, enabled => OptionalFeatureAction.Tweak(Entry, enabled), on);
}

public sealed record StepRow(string Text, bool Ok, string? Detail);

public sealed partial class ToolsViewModel(MainViewModel owner, AppServices services, ChangeRunner runner, IDialogs dialogs) : PageViewModel(owner)
{
    private CancellationTokenSource? _scanCancel;
    private IReadOnlyList<string> _protected = [];

    public ObservableCollection<string> Drives { get; } = [];
    public ObservableCollection<FileRow> LargestFiles { get; } = [];
    public ObservableCollection<FolderRow> LargestFolders { get; } = [];
    public ObservableCollection<FileRow> Duplicates { get; } = [];
    public ObservableCollection<FeatureRow> Features { get; } = [];
    public ObservableCollection<StepRow> RepairSteps { get; } = [];

    [ObservableProperty] private string? _selectedDrive;
    [ObservableProperty] private string _storageStatus = "";
    [ObservableProperty] private bool _isScanningStorage;
    [ObservableProperty] private string _quickOutput = "";
    [ObservableProperty] private string? _featuresNote;

    protected override async Task LoadAsync()
    {
        Drives.Clear();
        foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady)) Drives.Add(d.RootDirectory.FullName);
        SelectedDrive ??= Drives.FirstOrDefault();
        _protected = StorageAnalyzer.ProtectedRoots(Owner.Profile?.Software?.GameLibraryPaths);

        var lang = Lang;
        var states = await Task.Run(() => OptionalFeatureAction.ReadAll(services.Context.Processes));
        Features.Clear();
        foreach (var f in CatalogData.Current.Features.Features)
            if (states.TryGetValue(f.Name, out var s)) Features.Add(new FeatureRow(f, s == "Enabled", lang, services, runner));
        // DISM needs administrator rights to list features.
        FeaturesNote = Features.Count > 0 ? null : services.Elevation.IsElevated ? Loc.Instance["Feat_None"] : Loc.Instance["Feat_NeedsAdmin"];
    }

    [RelayCommand]
    private async Task ScanStorageAsync()
    {
        if (SelectedDrive is null) return;
        _scanCancel = new CancellationTokenSource();
        IsScanningStorage = true;
        LargestFiles.Clear();
        LargestFolders.Clear();
        Duplicates.Clear();
        try
        {
            var progress = new Progress<(int Files, long Bytes)>(p => StorageStatus = Loc.Instance.Format("Storage_Progress", p.Files, CleanupViewModel.Size(p.Bytes)));
            var report = await StorageAnalyzer.ScanAsync(SelectedDrive, _protected, progress: progress, ct: _scanCancel.Token);
            foreach (var f in report.LargestFiles)
                LargestFiles.Add(new FileRow(f.Path, f.Bytes, f.LastWriteUtc.ToLocalTime().ToString("yyyy-MM-dd"), f.CanDelete));
            foreach (var f in report.LargestFolders.Take(20))
                LargestFolders.Add(new FolderRow(f.Path, CleanupViewModel.Size(f.Bytes), Loc.Instance.Format("Storage_Files", f.Files)));
            foreach (var g in report.Duplicates.Take(50))
                foreach (var (p, i) in g.Paths.Select((p, i) => (p, i)))
                    Duplicates.Add(new FileRow(p, g.BytesEach, i == 0 ? Loc.Instance.Format("Storage_DuplicateOf", g.Paths.Count) : "", i > 0));
            StorageStatus = Loc.Instance.Format("Storage_Done", report.ScannedFiles, CleanupViewModel.Size(report.ScannedBytes),
                CleanupViewModel.Size(report.Duplicates.Sum(d => d.Reclaimable)));
            if (report.Truncated) StorageStatus += " " + Loc.Instance["Storage_Truncated"];
        }
        catch (OperationCanceledException)
        {
            StorageStatus = Loc.Instance["Storage_Stopped"];
        }
        finally
        {
            IsScanningStorage = false;
        }
    }

    [RelayCommand]
    private void StopStorage() => _scanCancel?.Cancel();

    [RelayCommand]
    private async Task RecycleSelectedAsync()
    {
        var selected = LargestFiles.Concat(Duplicates).Where(f => f.Selected && f.CanDelete).Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (selected.Count == 0) return;
        if (!dialogs.Ask(Loc.Instance["Storage_RecycleTitle"], Loc.Instance.Format("Storage_RecycleText", selected.Count, string.Join("\n", selected.Take(10))), Loc.Instance["Storage_Recycle"])) return;
        var failed = await Task.Run(() => StorageAnalyzer.Recycle(selected, _protected));
        Owner.ShowResult(Loc.Instance.Format("Storage_Recycled", selected.Count - failed.Count, failed.Count));
        await ScanStorageAsync();
    }

    [RelayCommand]
    private async Task RepairUpdateAsync()
    {
        if (!dialogs.Ask(Loc.Instance["Repair_Title"], Loc.Instance["Repair_Text"], Loc.Instance["Repair_Run"])) return;
        IsWorking = true;
        RepairSteps.Clear();
        var lang = Lang;
        var progress = new Progress<RepairStep>(s => RepairSteps.Add(new StepRow(Labels.Current.Get(lang, s.Key), s.Ok, s.Detail)));
        try
        {
            var steps = await Task.Run(() => UpdateRepair.Run(services.Context.Processes, progress));
            Owner.ShowResult(steps.All(s => s.Ok) ? Loc.Instance["Repair_Done"] : Loc.Instance["Repair_Partial"]);
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task FlushDnsAsync()
    {
        var (code, _) = await Task.Run(() => services.Context.Processes.Run("ipconfig.exe", "/flushdns"));
        QuickOutput = code == 0 ? Loc.Instance["Quick_DnsFlushed"] : Loc.Instance.Format("Result_Failed", code);
    }

    [RelayCommand]
    private async Task RestartExplorerAsync()
    {
        await Task.Run(() => services.Context.Processes.Run("taskkill.exe", "/f /im explorer.exe"));
        await Task.Delay(1000);
        // Explorer must run as the signed-in user, never elevated.
        var path = DeElevatedLauncher.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), null, services.Elevation);
        QuickOutput = path == DeElevatedLauncher.Path.Failed ? Loc.Instance["Quick_ExplorerManual"] : Loc.Instance["Quick_ExplorerRestarted"];
    }

    [RelayCommand]
    private async Task WinsockResetAsync()
    {
        if (!dialogs.Ask(Loc.Instance["Quick_WinsockTitle"], Loc.Instance["Quick_WinsockText"], Loc.Instance["Quick_Winsock"])) return;
        var (code, output) = await Task.Run(() => services.Context.Processes.Run("netsh.exe", "winsock reset"));
        QuickOutput = code == 0 ? Loc.Instance["Quick_WinsockDone"] : Loc.Instance.Format("Result_Failed", output.Trim());
    }
}

// ---------------- Health ----------------

public sealed record DiskRow(string Name, string Health, string Status, string Details);

public sealed record SensorRow(string Hardware, string Sensor, string Value);

public sealed record RunRow(string Label, string AvgFps, string Low, string Frames);

public sealed partial class HealthViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    private CancellationTokenSource? _toolCancel;
    private CancellationTokenSource? _throttleCancel;
    private Sensors? _sensors;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private readonly List<FrameStats> _before = [];
    private readonly List<FrameStats> _after = [];

    public ObservableCollection<DiskRow> Disks { get; } = [];
    public ObservableCollection<SensorRow> SensorRows { get; } = [];
    public ObservableCollection<string> Processes { get; } = [];
    public ObservableCollection<RunRow> Runs { get; } = [];
    public ObservableCollection<int> DurationOptions { get; } = [60, 120, 300];
    public ObservableCollection<int> CaptureOptions { get; } = [30, 60, 90];

    [ObservableProperty] private string _toolOutput = "";
    [ObservableProperty] private bool _isToolRunning;
    [ObservableProperty] private int _throttleSeconds = 120;
    [ObservableProperty] private bool _isThrottleRunning;
    [ObservableProperty] private string _throttleStatus = "";
    [ObservableProperty] private string _throttleResult = "";
    [ObservableProperty] private string? _selectedProcess;
    [ObservableProperty] private int _captureSeconds = 60;
    [ObservableProperty] private bool _isCapturing;
    [ObservableProperty] private string _benchmarkStatus = "";
    [ObservableProperty] private string _comparisonText = "";
    [ObservableProperty] private bool _sensorsOn;
    [ObservableProperty] private string _pawnIoText = "";
    [ObservableProperty] private bool _canInstallPawnIo;

    protected override Task LoadAsync()
    {
        BuildDisks();
        RefreshProcesses();
        CanInstallPawnIo = !Sensors.PawnIoInstalled;
        PawnIoText = Loc.Instance[Sensors.PawnIoInstalled ? "Sensors_PawnIoOn" : "Sensors_PawnIoOff"];
        if (Optimizer.Core.Tools.HealthStore.LoadThrottle(Optimizer.Core.Tools.HealthStore.DefaultFolder) is { } last) ThrottleResult = Describe(last);
        return Task.CompletedTask;
    }

    private void BuildDisks()
    {
        Disks.Clear();
        var lang = Lang;
        foreach (var d in Owner.Profile?.Extras?.DiskHealth ?? [])
        {
            var details = new List<string>();
            if (d.TemperatureC is { } t) details.Add($"{Labels.Current.Get(lang, "fact.diskTemperature")}: {t} °C");
            if (d.WearPercent is { } w) details.Add($"{Labels.Current.Get(lang, "fact.diskWear")}: {w} %");
            if (d.PowerOnHours is { } h) details.Add($"{Labels.Current.Get(lang, "fact.diskPowerOnHours")}: {h}");
            Disks.Add(new DiskRow($"{d.Name} ({d.MediaType}, {d.BusType})", Labels.Current.Get(lang, $"value.health{d.Health}"),
                d.IsCritical ? "Critical" : d.IsProblem ? "Problem" : d.Health == "Unknown" ? "Unknown" : "Ok",
                details.Count > 0 ? string.Join(", ", details) : Loc.Instance["Health_CountersNeedAdmin"]));
        }
    }

    // SFC / DISM

    private async Task RunToolAsync(Func<IProgress<string>, CancellationToken, Task<int>> run)
    {
        _toolCancel = new CancellationTokenSource();
        IsToolRunning = true;
        var lines = new List<string>();
        var progress = new Progress<string>(l =>
        {
            lines.Add(l);
            if (lines.Count > 200) lines.RemoveAt(0);
            ToolOutput = string.Join("\n", lines);
        });
        try
        {
            var code = await run(progress, _toolCancel.Token);
            lines.Add(Loc.Instance.Format("Health_ExitCode", code));
            ToolOutput = string.Join("\n", lines);
        }
        catch (OperationCanceledException)
        {
            ToolOutput += "\n" + Loc.Instance["Health_Stopped"];
        }
        finally
        {
            IsToolRunning = false;
        }
    }

    [RelayCommand] private Task SfcAsync() => RunToolAsync(SystemRepair.ScanNowAsync);
    [RelayCommand] private Task DismScanAsync() => RunToolAsync(SystemRepair.ScanHealthAsync);
    [RelayCommand] private Task DismRestoreAsync() => RunToolAsync(SystemRepair.RestoreHealthAsync);
    [RelayCommand] private void StopTool() => _toolCancel?.Cancel();

    // Throttle check

    [RelayCommand]
    private async Task StartThrottleAsync()
    {
        _throttleCancel = new CancellationTokenSource();
        IsThrottleRunning = true;
        try
        {
            using var monitor = new ThrottleMonitor();
            var progress = new Progress<ThrottleSample>(s => ThrottleStatus = Loc.Instance.Format("Throttle_Sample", monitor.Samples.Count,
                s.CpuPerformanceLimit is { } l ? $"{l:0}" : "?", s.CpuUtility is { } u ? $"{u:0}" : "?",
                s.Gpus.FirstOrDefault()?.TempC is { } t ? $"{t} °C" : "?"));
            var result = await monitor.RunAsync(TimeSpan.FromSeconds(ThrottleSeconds), progress, _throttleCancel.Token);
            if (result.Samples >= 10)
            {
                Optimizer.Core.Tools.HealthStore.SaveThrottle(Optimizer.Core.Tools.HealthStore.DefaultFolder, result);
                ThrottleResult = Describe(result);
                await Owner.ScanAsync(); // updates the throttling finding
            }
            else
            {
                ThrottleResult = Loc.Instance["Throttle_TooShort"];
            }
        }
        catch (Exception ex)
        {
            ThrottleResult = Loc.Instance.Format("Result_Failed", ex.Message);
        }
        finally
        {
            IsThrottleRunning = false;
            ThrottleStatus = "";
        }
    }

    [RelayCommand] private void StopThrottle() => _throttleCancel?.Cancel();

    private static string Describe(ThrottleResult r) => Loc.Instance.Format("Throttle_Result",
        r.Measured.LocalDateTime.ToString("g"), r.Samples, $"{r.CpuBusyShare * 100:0}", $"{r.CpuLimitedShare * 100:0}",
        r.GpuReasonShare.TryGetValue("thermal", out var th) ? $"{th * 100:0}" : "?",
        r.GpuReasonShare.TryGetValue("powerLimit", out var pw) ? $"{pw * 100:0}" : "?",
        r.CpuThrottled || r.GpuThermal ? Loc.Instance["Throttle_Verdict_Bad"] : Loc.Instance["Throttle_Verdict_Ok"]);

    // Benchmark

    [RelayCommand]
    private void RefreshProcesses()
    {
        var selected = SelectedProcess;
        Processes.Clear();
        var names = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in Owner.Profile?.Software?.Games ?? [])
            if (g.Executable is { } exe) names.Add(Path.GetFileName(exe));
        foreach (var p in Process.GetProcesses())
        {
            using (p)
            {
                try
                {
                    if (p.MainWindowHandle != IntPtr.Zero && p.Id != Environment.ProcessId) names.Add(p.ProcessName + ".exe");
                }
                catch (Exception)
                {
                    // protected process
                }
            }
        }
        foreach (var n in names.Where(PresentMon.IsSafeProcessName)) Processes.Add(n);
        SelectedProcess = selected is not null && Processes.Contains(selected) ? selected : Processes.FirstOrDefault();
    }

    [RelayCommand] private Task CaptureBeforeAsync() => CaptureAsync(_before, "A");
    [RelayCommand] private Task CaptureAfterAsync() => CaptureAsync(_after, "B");

    private async Task CaptureAsync(List<FrameStats> set, string label)
    {
        if (SelectedProcess is null) return;
        IsCapturing = true;
        try
        {
            var exe = await Task.Run(() => PresentMon.Extract(AppServices.ToolsFolder));
            BenchmarkStatus = Loc.Instance.Format("Bench_Capturing", SelectedProcess, CaptureSeconds);
            var stats = await PresentMon.CaptureAsync(exe, SelectedProcess, CaptureSeconds, Path.Combine(AppServices.ToolsFolder, "captures"), null, CancellationToken.None);
            if (stats is null)
            {
                BenchmarkStatus = Loc.Instance["Bench_NoFrames"];
                return;
            }
            set.Add(stats);
            Runs.Add(new RunRow($"{label}{set.Count}", $"{stats.AverageFps:0.0}", $"{stats.OnePercentLowFps:0.0}", stats.Frames.ToString()));
            BenchmarkStatus = Loc.Instance["Bench_Done"];
            Compare();
        }
        catch (Exception ex)
        {
            BenchmarkStatus = Loc.Instance.Format("Result_Failed", ex.Message);
        }
        finally
        {
            IsCapturing = false;
        }
    }

    private void Compare()
    {
        var c = PresentMon.Compare(_before, _after);
        ComparisonText = c.Result switch
        {
            Comparison.NotEnoughRuns => Loc.Instance.Format("Bench_NeedRuns", PresentMon.MinRuns),
            Comparison.NoMeasurableDifference => Loc.Instance.Format("Bench_NoDifference", $"{c.BeforeMean:0.0}", $"{c.AfterMean:0.0}"),
            Comparison.Better => Loc.Instance.Format("Bench_Better", $"{c.ChangePercent:0.0}", $"{c.BeforeMean:0.0}", $"{c.AfterMean:0.0}"),
            _ => Loc.Instance.Format("Bench_Worse", $"{-c.ChangePercent:0.0}", $"{c.BeforeMean:0.0}", $"{c.AfterMean:0.0}"),
        };
    }

    [RelayCommand]
    private void ClearRuns()
    {
        _before.Clear();
        _after.Clear();
        Runs.Clear();
        ComparisonText = "";
    }

    // Sensors (opt-in)

    partial void OnSensorsOnChanged(bool value)
    {
        if (value)
        {
            try
            {
                _sensors = new Sensors();
                _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _timer.Tick += async (_, _) => await ReadSensorsAsync();
                _timer.Start();
                _ = ReadSensorsAsync();
            }
            catch (Exception ex)
            {
                Owner.ShowResult(Loc.Instance.Format("Result_Failed", ex.Message));
                SensorsOn = false;
            }
        }
        else
        {
            _timer?.Stop();
            _sensors?.Dispose();
            _sensors = null;
            SensorRows.Clear();
        }
    }

    private async Task ReadSensorsAsync()
    {
        if (_sensors is not { } s) return;
        var readings = await Task.Run(s.Read);
        SensorRows.Clear();
        foreach (var r in readings.Where(r => r.Value is not null && r.Type is "Temperature" or "Load" or "Clock" or "Power" or "Fan"))
            SensorRows.Add(new SensorRow(r.Hardware, $"{r.Sensor} ({Loc.Instance[$"SensorType_{r.Type}"]})", Unit(r)));
    }

    private static string Unit(SensorReading r) => r.Type switch
    {
        "Temperature" => $"{r.Value:0} °C",
        "Load" => $"{r.Value:0} %",
        "Clock" => $"{r.Value:0} MHz",
        "Power" => $"{r.Value:0.0} W",
        "Fan" => $"{r.Value:0} RPM",
        _ => $"{r.Value}",
    };

    [RelayCommand]
    private async Task InstallPawnIoAsync()
    {
        var app = CatalogData.Current.Apps.Apps.First(a => a.Id == "namazso.PawnIO");
        if (Winget.Find(services.ProfilePath) is not { } winget)
        {
            PawnIoText = Loc.Instance["Apps_NoWinget"];
            return;
        }
        if (!dialogs.Ask(Loc.Instance["Sensors_PawnIoTitle"], Loc.Instance["Sensors_PawnIoText"], Loc.Instance["Apps_Install"])) return;
        IsWorking = true;
        try
        {
            var code = await Winget.InstallAsync(winget, app, null, CancellationToken.None);
            PawnIoText = Winget.IsSuccess(code) ? Loc.Instance["Sensors_PawnIoOn"] : Loc.Instance.Format("Apps_Failed", $"0x{code:X8}");
            CanInstallPawnIo = !Winget.IsSuccess(code);
        }
        finally
        {
            IsWorking = false;
        }
    }
}
