using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Profiles;
using Optimizer.Core.Tweaks;
using Optimizer.Core.Updates;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

public enum Page { Overview, Tweaks, Advisor, Network, Debloat, Cleanup, Startup, Services, Apps, Tools, Health, Changes, Hardware, Settings }

/// <summary>A notice at the top of the page (Fluent InfoBar).</summary>
public sealed record Banner(string Text, bool IsWarning)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Text;

    public InfoBarSeverity Severity => IsWarning ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;

    /// <summary>Optional button in the info bar (e.g. "Apply again").</summary>
    public string? ActionText { get; init; }

    public System.Windows.Input.ICommand? Action { get; init; }

    public SymbolRegular ActionIcon { get; init; } = SymbolRegular.ArrowSync20;
}

public sealed record SummaryItem(string Label, string Value)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Label}: {Value}";
}

/// <summary>Status drives the color of the number (problems in caution color, passed in success color).</summary>
public sealed record CountItem(string Value, string Label, string Status)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Value} {Label}";
}

public sealed record HwSection(string Title, IReadOnlyList<SummaryItem> Items)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;
}

public sealed record CategoryItem(string Key, string Text, int Count)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Text;

    public string Display => $"{Text} ({Count})";
}

public sealed record ChangeRecordItem(string TweakId, string Title, string When, string Details, string StateText, string Status, bool CanUndo)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;

    /// <summary>Set when the change is no longer in place: why (Windows update or other) for the row.</summary>
    public string? ResetText { get; init; }

    public bool IsReset => ResetText is not null;
}

public sealed record LogLine(string Time, string Text)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Time} {Text}";
}

/// <summary>One line of the "Apply recommended" plan: what, why, and its impact.</summary>
public sealed record RecommendationLine(string Title, string Reason, string ImpactText)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;
}

/// <summary>A usage profile in the picker.</summary>
public sealed record ProfileOption(string Id, string Name, string Description, SymbolRegular Icon)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;
}

/// <summary>A change on this PC that works against the active profile (with Undo when this app made it).</summary>
public sealed record AgainstLine(string TweakId, string Title, string Reason, bool CanUndo)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly CatalogData _catalog = CatalogData.Current;
    private readonly AppSettings _settings;
    private readonly AppServices _services;
    private readonly IDialogs _dialogs;
    private IReadOnlyList<Finding> _findings = [];
    private IReadOnlyList<TweakStatus> _tweakStates = [];
    private IReadOnlyList<TweakStatus> _deviceStates = [];
    private RecommendationPlan _plan = new([], []);
    private IReadOnlyList<DriftItem> _drift = [];
    private string? _updateNotice;
    private ReleaseCheckResult? _release;
    private bool _releaseChecking;
    private Facts _facts = new();
    private DateTime? _lastScanTime;

    // Usage profile: decides recommendations, impact, "works against" and which findings count (no setting by itself).
    private UsageProfile _usage = CatalogData.Current.Profiles.Default;
    private IReadOnlyList<ProfiledTweak> _profiled = [];
    private IReadOnlyList<Finding> _profiledFindings = [];
    private UsageProfile? _suggested;
    private bool _profileAutoSet;

    public MainViewModel(AppSettings settings, AppServices services, IDialogs dialogs)
    {
        _settings = settings;
        _services = services;
        _dialogs = dialogs;
        _expertMode = settings.ExpertMode;
        _checkForUpdates = settings.CheckForUpdates;
        Runner = new ChangeRunner(services, dialogs, () => _facts, AppliedIds, () => ExpertMode);
        Runner.Status += (_, result) => ShowResult(result.Text, result.Severity);
        Runner.BusyChanged += (_, busy) => IsBusy = busy;
        Runner.Changed += async (_, t) => await OnChangedAsync(t);
        ChangeGate.Instance.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(ChangeGate.CanChange)) return;
            OnPropertyChanged(nameof(CanChange));
            foreach (var command in new CommunityToolkit.Mvvm.Input.IRelayCommand[]
                     {
                         RunActionCommand, ApplyRecommendedCommand, UndoAllCommand, UndoRecordCommand, ReapplyRecordCommand,
                         ReapplyDriftCommand, EnableRestorePointsCommand, ScanCommand, UpdateNowCommand,
                     })
                command.NotifyCanExecuteChanged();
        };

        Network = new NetworkViewModel(this, services);
        Debloat = new DebloatViewModel(this, services, dialogs);
        Cleanup = new CleanupViewModel(this, services, dialogs);
        Startup = new StartupViewModel(this, services, Runner);
        ServicesPage = new ServicesViewModel(this, services, Runner);
        Apps = new AppsViewModel(this, services, dialogs);
        Tools = new ToolsViewModel(this, services, Runner, dialogs);
        Health = new HealthViewModel(this, dialogs);
        _virusTotalConfigured = !string.IsNullOrEmpty(settings.VirusTotalKey);
        ShowPendingCounts();
        Loc.Instance.LanguageChanged += (_, _) =>
        {
            Rebuild();
            Network.Rebuild();
            foreach (var page in new PageViewModel[] { Debloat, Cleanup, Startup, ServicesPage, Apps, Tools, Health }) page.OnLanguageChanged();
        };
    }

    public AppServices Services => _services;
    public ChangeRunner Runner { get; }
    public NetworkViewModel Network { get; }
    public DebloatViewModel Debloat { get; }
    public CleanupViewModel Cleanup { get; }
    public StartupViewModel Startup { get; }
    public ServicesViewModel ServicesPage { get; }
    public AppsViewModel Apps { get; }
    public ToolsViewModel Tools { get; }
    public HealthViewModel Health { get; }

    [ObservableProperty] private Page _currentPage = Page.Overview;
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private string _scanStatus = "";
    [ObservableProperty] private HardwareProfile? _profile;
    [ObservableProperty] private int _score;
    [ObservableProperty] private string _problemSummary = "";
    [ObservableProperty] private string _lastScanText = "";
    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private string _scoreVerdict = "";
    [ObservableProperty] private string _scoreStatus = "Neutral";
    [ObservableProperty] private bool _showPassed;
    [ObservableProperty] private InspectorItem? _selectedItem;
    [ObservableProperty] private FindingItemViewModel? _gameAccess;
    [ObservableProperty] private CategoryItem? _selectedCategory;
    [ObservableProperty] private bool _onlyRecommended;
    [ObservableProperty] private bool _expertMode;
    [ObservableProperty] private bool _checkForUpdates;
    [ObservableProperty] private string _restorePointText = "";
    [ObservableProperty] private bool _canEnableRestorePoints;
    [ObservableProperty] private string _tweaksSummary = "";
    [ObservableProperty] private string? _resultText;
    [ObservableProperty] private bool _resultOpen;
    [ObservableProperty] private string _recommendedButtonText = "";
    [ObservableProperty] private bool _hasRecommendations;
    [ObservableProperty] private bool _virusTotalConfigured;
    [ObservableProperty] private ProfileOption? _selectedProfile;
    [ObservableProperty] private bool _onlyProfile = true;
    [ObservableProperty] private string _profileDescription = "";
    [ObservableProperty] private string? _suggestedProfileText;
    [ObservableProperty] private string _scoreTitle = "";
    [ObservableProperty] private string _scoreHint = "";
    [ObservableProperty] private string? _hiddenFindingsText;
    [ObservableProperty] private string _recommendationsIntro = "";
    [ObservableProperty] private bool _hasAgainst;

    public ObservableCollection<ProfileOption> ProfileOptions { get; } = [];
    public ObservableCollection<AgainstLine> AgainstProfile { get; } = [];

    /// <summary>Developer switch --profile: use this profile for the session without saving it.</summary>
    public string? ProfileOverride { get; set; }

    public ObservableCollection<FindingItemViewModel> Findings { get; } = [];
    public ObservableCollection<FindingItemViewModel> AdvisorItems { get; } = [];
    public ObservableCollection<TweakItemViewModel> Tweaks { get; } = [];
    public ObservableCollection<CategoryItem> Categories { get; } = [];
    public ObservableCollection<ChangeRecordItem> ChangeRecords { get; } = [];
    public ObservableCollection<LogLine> ChangeLog { get; } = [];
    public ObservableCollection<Banner> Banners { get; } = [];
    public ObservableCollection<SummaryItem> Summary { get; } = [];
    public ObservableCollection<HwSection> HardwareSections { get; } = [];
    public ObservableCollection<CountItem> Counts { get; } = [];
    public ObservableCollection<RecommendationLine> Recommendations { get; } = [];
    public ObservableCollection<RecommendationLine> RecommendationsExcluded { get; } = [];

    public Facts Facts => _facts;
    public IReadOnlyList<TweakStatus> DeviceStates => _deviceStates;

    public bool FindingsEmpty => Profile is not null && Findings.Count == 0;
    public bool AdvisorEmpty => Profile is not null && AdvisorItems.Count == 0;
    public bool ChangesEmpty => ChangeRecords.Count == 0;
    public bool ChangeLogEmpty => ChangeLog.Count == 0;
    public bool IsElevated => _services.Elevation.IsElevated;

    public bool IsEnglish => Loc.Instance.Language == "en";
    public bool IsGerman => Loc.Instance.Language == "de";
    public string ThemeSetting => _settings.Theme;

    public string Version => AppInfo.Text;
    public string AboutText => Loc.Instance.Format("About_Text", Version);
    public string LogFile => Log.CurrentFile ?? Log.Directory;
    public string DataFolder => AppServices.DataFolder;
    public string? VirusTotalKey => Optimizer.Core.Startup.Dpapi.Unprotect(_settings.VirusTotalKey);

    partial void OnShowPassedChanged(bool value) => Rebuild();
    partial void OnSelectedCategoryChanged(CategoryItem? value) => FillTweaks();
    partial void OnOnlyRecommendedChanged(bool value) => FillTweaks();

    partial void OnOnlyProfileChanged(bool value)
    {
        BuildCategories();
        FillTweaks();
    }

    /// <summary>The user picked another profile: re-rate everything (nothing on the PC changes).</summary>
    partial void OnSelectedProfileChanged(ProfileOption? value)
    {
        if (value is null || value.Id == _usage.Id) return;
        _usage = _catalog.Profiles.Get(value.Id);
        _profileAutoSet = false;
        _settings.Profile = _usage.Id;
        _settings.Save();
        Log.Info("profile", "profile changed", new { profile = _usage.Id });
        ApplyProfile();
        Rebuild();
        Network.Rebuild();
    }

    [RelayCommand]
    private void UseSuggestedProfile()
    {
        if (_suggested is not null && ProfileOptions.FirstOrDefault(o => o.Id == _suggested.Id) is { } option) SelectedProfile = option;
    }

    public static string ProfileName(UsageProfile p) => Loc.Instance[$"Profile_{p.Id}"];

    partial void OnCurrentPageChanged(Page value)
    {
        SelectedItem = null;
        if (value == Page.Changes) BuildChanges();
        (value switch
        {
            Page.Debloat => Debloat.EnsureLoadedAsync(),
            Page.Cleanup => Cleanup.EnsureLoadedAsync(),
            Page.Startup => Startup.EnsureLoadedAsync(),
            Page.Services => ServicesPage.EnsureLoadedAsync(),
            Page.Apps => Apps.EnsureLoadedAsync(),
            Page.Tools => Tools.EnsureLoadedAsync(),
            Page.Health => Health.EnsureLoadedAsync(),
            Page.Network => Network.EnsureLoadedAsync(),
            _ => Task.CompletedTask,
        }).Forget($"loading page {value}");
    }

    /// <summary>Developer switch --expert on: Expert mode for this session only (not saved, no confirmation).</summary>
    public void EnableExpertForSession()
    {
#pragma warning disable MVVMTK0034
        _expertMode = true;
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(ExpertMode));
    }

    partial void OnExpertModeChanged(bool value)
    {
        if (value && !_settings.ExpertMode && !_dialogs.ConfirmExpertMode())
        {
            ExpertMode = false;
            return;
        }
        _settings.ExpertMode = value;
        _settings.Save();
        Rebuild();
        Network.Rebuild();
    }

    /// <summary>Score and counts only after a finished scan: during a scan the cards keep their layout but show "--".</summary>
    public bool HasScore => Profile is not null && !IsScanning;

    public string ScoreText => HasScore ? Score.ToString(System.Globalization.CultureInfo.CurrentCulture) : "--";
    public int ScoreBarValue => HasScore ? Score : 0;
    public string ScoreStatusShown => HasScore ? ScoreStatus : "Neutral";
    public string ScoreVerdictShown => HasScore ? ScoreVerdict : Loc.Instance["Score_Pending"];

    private void NotifyScore()
    {
        foreach (var name in new[] { nameof(HasScore), nameof(ScoreText), nameof(ScoreBarValue), nameof(ScoreStatusShown), nameof(ScoreVerdictShown) })
            OnPropertyChanged(name);
    }

    partial void OnScoreChanged(int value) => NotifyScore();
    partial void OnScoreStatusChanged(string value) => NotifyScore();
    partial void OnScoreVerdictChanged(string value) => NotifyScore();
    partial void OnProfileChanged(HardwareProfile? value) => NotifyScore();

    partial void OnIsScanningChanged(bool value)
    {
        ChangeGate.Instance.Scanning = value;
        NotifyScore();
        if (value) ShowPendingCounts();
    }

    /// <summary>No change, scan or update is running: switches and change buttons are enabled.</summary>
    public bool CanChange => ChangeGate.Instance.CanChange;

    /// <summary>A scan may start while no change or update runs (a running scan is joined, not repeated).</summary>
    private bool CanScan() => !ChangeGate.Instance.Busy && !ChangeGate.Instance.Updating;

    /// <summary>While scanning (and before the first scan): the count tiles with "--" instead of old or empty numbers.</summary>
    private void ShowPendingCounts()
    {
        ProblemSummary = Loc.Instance["Dash_ProblemsPending"];
        ScoreTitle = Loc.Instance.Format("Dash_ScoreFor", ProfileName(_usage));
        Counts.Clear();
        foreach (var label in new[] { "Count_Problems", "Count_Advisor", "Count_Passed" })
            Counts.Add(new CountItem("--", Loc.Instance[label], "Neutral"));
    }

    partial void OnCheckForUpdatesChanged(bool value)
    {
        _settings.CheckForUpdates = value;
        _settings.Save();
    }

    public string? UpdateStatus => _releaseChecking ? Loc.Instance["Update_Checking"] : _release?.Status switch
    {
        ReleaseCheckStatus.UpToDate => Loc.Instance.Format("Update_UpToDate", Version),
        ReleaseCheckStatus.NewerAvailable => Loc.Instance.Format("Update_Available", _release.Latest!.ToString(3)),
        ReleaseCheckStatus.NoRelease => Loc.Instance["Update_NoRelease"],
        ReleaseCheckStatus.Error => Loc.Instance["Update_Error"],
        _ => null,
    };

    public bool UpdateAvailable => _release?.Status == ReleaseCheckStatus.NewerAvailable;

    /// <summary>At start only when the user turned the check on; "Check now" always asks.</summary>
    public async Task CheckForUpdatesAsync(bool atStart)
    {
        if ((atStart && !_settings.CheckForUpdates) || _releaseChecking) return;
        _releaseChecking = true;
        OnPropertyChanged(nameof(UpdateStatus));
        try
        {
            using var check = new ReleaseCheck();
            _release = await check.CheckAsync(AppInfo.Version);
        }
        catch (Exception ex)
        {
            Log.Error("update", "release check failed", ex);
            _release = new ReleaseCheckResult(ReleaseCheckStatus.Error);
        }
        finally
        {
            _releaseChecking = false;
        }
        Log.Info("update", "release check", new { _release.Status, latest = _release.Latest?.ToString(3) });
        OnPropertyChanged(nameof(UpdateStatus));
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(CanSelfUpdate));
        BuildBanners();
    }

    [RelayCommand]
    private Task CheckUpdatesNow() => CheckForUpdatesAsync(atStart: false);

    [RelayCommand]
    private void OpenReleasePage() => OpenLink(ReleaseCheck.LatestReleaseUrl);

    /// <summary>Opens the GitHub bug form with version and Windows build filled in; nothing is sent until the user submits it there.</summary>
    [RelayCommand]
    private void ReportProblem() =>
        OpenLink($"https://github.com/{ReleaseCheck.Repository}/issues/new?template=bug_report.yml" +
                 $"&version={Uri.EscapeDataString(Version)}&windows={Uri.EscapeDataString(_services.Os.BuildString)}");

    /// <summary>Copies this session's log for a bug report (the clipboard, not a file in a user-writable folder).</summary>
    [RelayCommand]
    private void CopyLog()
    {
        var entries = Log.Snapshot(400);
        var text = string.Join(Environment.NewLine, entries.Select(e =>
            $"{e.Time:HH:mm:ss} {e.Level} {e.Source}: {e.Message}{(e.Data is null ? "" : " " + e.Data)}"));
        try
        {
            System.Windows.Clipboard.SetText(text);
            ShowResult(Loc.Instance.Format("Log_Copied", entries.Count));
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            ShowResult(Loc.Instance["Log_CopyFailed"]);
        }
    }

    /// <summary>Downloaded updates (protected data folder); emptied on the next start.</summary>
    public static string UpdatesFolder => DataPaths.Updates;

    /// <summary>
    /// Self-update only replaces the published single-file exe running elevated. A Debug build has PCOptimizer.dll next
    /// to its exe and gets the download page instead.
    /// </summary>
    public bool CanSelfUpdate => UpdateAvailable && _release?.Assets is not null && UpdateSignature.IsConfigured && DataPaths.ProcessIsElevated &&
        Environment.ProcessPath is { } exe && System.IO.Path.GetFileName(exe).Equals("PCOptimizer.exe", StringComparison.OrdinalIgnoreCase) &&
        !System.IO.File.Exists(System.IO.Path.ChangeExtension(exe, ".dll"));

    [ObservableProperty] private bool _updating;

    partial void OnUpdatingChanged(bool value) => ChangeGate.Instance.Updating = value;

    /// <summary>After confirmation: download the release exe, check its signature and SHA-256, replace this exe and restart.</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task UpdateNowAsync()
    {
        if (!CanSelfUpdate || _release is not { Latest: { } latest } release || Environment.ProcessPath is not { } exe)
        {
            OpenReleasePage();
            return;
        }
        if (IsBusy || Updating)
        {
            ShowResult(Loc.Instance["Update_Busy"]);
            return;
        }
        var version = latest.ToString(3);
        if (!_dialogs.Ask(Loc.Instance.Format("Update_ConfirmTitle", version), Loc.Instance.Format("Update_ConfirmText", version), Loc.Instance["Update_Restart"])) return;
        Updating = true;
        try
        {
            using var updater = new Updater();
            ShowResult(Loc.Instance.Format("Update_Downloading", version, 0));
            var progress = new Progress<int>(p => ShowResult(Loc.Instance.Format("Update_Downloading", version, p)));
            var download = await updater.DownloadAsync(release, UpdatesFolder, progress);
            Log.Info("update", "download", new { download.Outcome, version });
            if (download.Outcome != UpdateOutcome.Ready)
            {
                ShowResult(Loc.Instance[download.Outcome switch
                {
                    UpdateOutcome.ChecksumMismatch => "Update_BadChecksum",
                    UpdateOutcome.SignatureInvalid => "Update_BadSignature",
                    _ => "Update_Failed",
                }]);
                return;
            }
            // A change may have started while the download ran: never replace the exe in the middle of it.
            if (IsBusy)
            {
                ShowResult(Loc.Instance["Update_Busy"]);
                return;
            }
            try
            {
                Updater.Install(download.FilePath!, exe);
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
            {
                Log.Error("update", "replacing the exe failed", ex);
                ShowResult(Loc.Instance.Format("Update_CannotReplace", ex.Message));
                return;
            }
            Log.Info("update", "installed, restarting", new { version });
            if (!Updater.StartVerified(exe, download.Sha256!, path => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = false })?.Dispose()))
            {
                Log.Error("update", "the installed exe changed before the restart; not started");
                ShowResult(Loc.Instance["Update_BadChecksum"]);
                return;
            }
            System.Windows.Application.Current.Shutdown();
        }
        finally
        {
            Updating = false;
        }
    }

    private System.Windows.Threading.DispatcherTimer? _resultTimer;

    /// <summary>Shows the result of an action at the bottom; it closes by itself after long enough to read it.</summary>
    [ObservableProperty] private InfoBarSeverity _resultSeverity = InfoBarSeverity.Informational;

    /// <summary>Shows the result of an action; failures in the caution or error style, so they are not read as done.</summary>
    public void ShowResult(string text, InfoBarSeverity severity = InfoBarSeverity.Informational)
    {
        ResultSeverity = severity;
        ResultText = text;
        ResultOpen = !string.IsNullOrWhiteSpace(text);
        if (_resultTimer is null)
        {
            _resultTimer = new System.Windows.Threading.DispatcherTimer();
            _resultTimer.Tick += (_, _) =>
            {
                _resultTimer.Stop();
                ResultOpen = false;
            };
        }
        _resultTimer.Stop();
        if (!ResultOpen) return;
        _resultTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(5 + text.Length / 15.0, 8, 20));
        _resultTimer.Start();
    }

    [RelayCommand]
    private void Navigate(Page page) => CurrentPage = page;

    [RelayCommand]
    private void SetLanguage(string lang)
    {
        _settings.Language = lang;
        _settings.Save();
        Loc.Instance.SetLanguage(lang);
        OnPropertyChanged(nameof(AboutText));
        OnPropertyChanged(nameof(UpdateStatus));
    }

    [RelayCommand]
    private void OpenLink(string url)
    {
        if (!DeElevatedLauncher.IsLink(url))
        {
            Log.Warn("launcher", $"not a web or Store link, not opened: {url}");
            return;
        }
        if (DeElevatedLauncher.Open(url, null, _services.Elevation) == DeElevatedLauncher.Path.Failed) ShowResult(url);
    }

    [RelayCommand]
    private void Select(InspectorItem? item) => SelectedItem = item;

    [RelayCommand]
    private void CloseInspector() => SelectedItem = null;

    public void SaveVirusTotalKey(string? key)
    {
        _settings.VirusTotalKey = string.IsNullOrWhiteSpace(key) ? null : Optimizer.Core.Startup.Dpapi.Protect(key.Trim());
        _settings.Save();
        VirusTotalConfigured = _settings.VirusTotalKey is not null;
    }

    // ---------------- scanning ----------------

    private Task? _scan;
    private bool _rescanRequested;

    /// <summary>
    /// One scan at a time. A call while a scan runs asks for one more scan after it (the running one may have read the
    /// system before a change finished) and returns the task that ends after both, so two scans never race and an
    /// older result can never overwrite a newer one.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanScan))]
    public Task ScanAsync()
    {
        if (_scan is { IsCompleted: false })
        {
            _rescanRequested = true;
            return _scan;
        }
        return _scan = ScanUntilCurrentAsync();
    }

    private async Task ScanUntilCurrentAsync()
    {
        do
        {
            _rescanRequested = false;
            await ScanOnceAsync();
        }
        while (_rescanRequested);
    }

    private async Task ScanOnceAsync()
    {
        Task<IReadOnlyList<Optimizer.Core.Tools.ProcessCpu>>? pendingSample = null;
        IsScanning = true;
        ScanStatus = Loc.Instance["Scan_Running"];
        Headline = Loc.Instance["Headline_Scanning"];
        string? error = null;
        try
        {
            var progress = new Progress<string>(probe => ScanStatus = Loc.Instance.Format("Scan_Probe", probe));
            // After a change only what a change can affect is read again; "Scan again" reads everything.
            var previous = _reuseSlowParts ? Profile : null;
            _reuseSlowParts = false;
            var scanner = new HardwareScanner(_catalog);
            // The page is ready without the 3 second background CPU sample; that one check follows when it is done.
            var profile = await scanner.ScanAsync(progress, previous: previous, waitForBackgroundSample: false);
            pendingSample = scanner.PendingBackgroundSample;
            // A quick rescan after a change reuses the last finished sample.
            if (profile.Extras is { BackgroundCpu: null } && pendingSample is null && _lastBackgroundSample is { } last)
                profile = HardwareScanner.WithBackgroundSample(profile, last);
            var registry = _services.Context.Registry;
            var bcd = _services.BcdElements;
            _findings = await Task.Run(() => new FindingEngine(_catalog, registry, bcd).Evaluate(profile));
            Profile = profile;
            await RefreshTweaksAsync();
            _lastScanTime = DateTime.Now;
            Rebuild();
            Network.Rebuild();
        }
        catch (Exception ex)
        {
            Log.Error("ui", "scan failed", ex);
            error = ex.Message;
        }
        finally
        {
            IsScanning = false;
            ScanStatus = error ?? Loc.Instance["Scan_Idle"];
            OnPropertyChanged(nameof(LogFile));
        }
        await RefreshChangesStateAsync();
        if (pendingSample is not null && Profile is { } scanned) CompleteBackgroundSampleAsync(scanned, pendingSample).Forget("background CPU sample");
    }

    private IReadOnlyList<Optimizer.Core.Tools.ProcessCpu>? _lastBackgroundSample;

    /// <summary>
    /// The 3 second background CPU sample finished after the page was ready: its check (F11) is evaluated now. Skipped
    /// when a newer scan replaced the result meanwhile; that scan's own sample follows.
    /// </summary>
    private async Task CompleteBackgroundSampleAsync(HardwareProfile scanned, Task<IReadOnlyList<Optimizer.Core.Tools.ProcessCpu>> pending)
    {
        IReadOnlyList<Optimizer.Core.Tools.ProcessCpu>? sample;
        try
        {
            sample = await pending;
        }
        catch (Exception ex)
        {
            Log.Warn("scan", $"background CPU sample failed: {ex.Message}");
            return;
        }
        _lastBackgroundSample = sample;
        if (!ReferenceEquals(Profile, scanned) || IsScanning) return;
        var completed = HardwareScanner.WithBackgroundSample(scanned, sample);
        var registry = _services.Context.Registry;
        var bcd = _services.BcdElements;
        var findings = await Task.Run(() => new FindingEngine(_catalog, registry, bcd).Evaluate(completed));
        var facts = await Task.Run(() => FactsBuilder.Build(completed, findings, _catalog, registry));
        if (!ReferenceEquals(Profile, scanned) || IsScanning) return;
        _findings = findings;
        Profile = completed;
        // Reading every tweak's state takes most of a second and recreating every row makes each page draw again; the
        // sample changes only its own check, which no tweak reads today. Both happen only when a tweak depends on it.
        if (TweaksDependOn(_facts, facts))
        {
            await RefreshTweaksAsync();
            Rebuild();
            Network.Rebuild();
            return;
        }
        _facts = facts;
        ApplyProfile();
        RebuildFindings();
    }

    /// <summary>
    /// True when the facts differ in one the tweak rows depend on: any fact that is not a finding's (the engine reads
    /// hardware and system facts directly), or a finding that a tweak fixes or a tweak or profile condition reads.
    /// </summary>
    private bool TweaksDependOn(Facts before, Facts after)
    {
        var changed = before.All.Keys.Union(after.All.Keys, StringComparer.OrdinalIgnoreCase)
            .Where(k => !Equals(before.Get(k), after.Get(k))).ToList();
        if (changed.Count == 0) return false;
        if (changed.Any(k => !k.StartsWith("finding.", StringComparison.OrdinalIgnoreCase))) return true;
        var tweaks = _tweakStates.Concat(_deviceStates).Select(s => s.Tweak).ToList();
        var findingIds = changed.Select(k => k.Split('.')[1]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (tweaks.Any(t => t.Fixes.Any(findingIds.Contains))) return true;
        var profiles = _catalog.Profiles.Profiles;
        var read = tweaks.SelectMany(t => t.Conditions())
            .Concat(profiles.Select(p => p.SuggestWhen).OfType<Condition>())
            .Concat(profiles.SelectMany(p => p.Tweaks).Select(pt => pt.RecommendWhen).OfType<Condition>())
            .SelectMany(c => c.ReferencedFacts())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return changed.Any(read.Contains);
    }

    /// <summary>Re-reads every tweak's state (after a scan or after applying/undoing).</summary>
    private async Task RefreshTweaksAsync()
    {
        if (Profile is null) return;
        InspectorItem.ClearMarkdownCache();
        var profile = Profile;
        var engine = _services.Engine;
        var registry = _services.Context.Registry;
        var watch = System.Diagnostics.Stopwatch.StartNew();
        (_facts, _tweakStates, _deviceStates) = await Task.Run(() =>
        {
            var facts = FactsBuilder.Build(profile, _findings, _catalog, registry);
            var catalog = engine.DetectAll(_services.Catalog.Visible, facts);
            var device = engine.DetectAll(DeviceTweaks.Build(profile), facts);
            return (facts, catalog, device);
        });
        Log.Info("scan", "tweak states read", new { ms = watch.ElapsedMilliseconds });
        EnsureProfile();
        ApplyProfile();
        var facts = _facts;
        _drift = await Task.Run(() => engine.CheckDrift(facts));
        foreach (var d in _drift)
            Log.Warn("drift", $"{d.Tweak.Id} is no longer in place", new { d.AppliedOn, d.Current, d.WindowsUpdatedSince });

        // Once per Windows update: say so when all changes survived it (a reset shows in the drift banner instead).
        var current = _services.Os.BuildString;
        var count = await Task.Run(() => _services.Store.All().Count);
        if (_settings.LastSeenWindowsVersion is { } last && last != current && count > 0 && _drift.Count == 0)
            _updateNotice = Loc.Instance.Format("WinUpdated_AllGood", last, current, count);
        if (_settings.LastSeenWindowsVersion != current)
        {
            _settings.LastSeenWindowsVersion = current;
            _settings.Save();
        }
    }

    /// <summary>
    /// Picks the active profile after a scan: the saved one if this PC offers it, else (first start) the one that fits
    /// this PC best, which a banner then mentions once.
    /// </summary>
    private void EnsureProfile()
    {
        var profiles = _catalog.Profiles;
        _suggested = profiles.Suggest(_facts);
        if (ProfileOverride is { } forced && profiles.Profiles.FirstOrDefault(p => p.Id == forced && p.IsAvailable(_facts)) is { } chosen)
        {
            _usage = chosen;
            return;
        }
        if (_settings.Profile is null)
        {
            _usage = _suggested;
            _profileAutoSet = true;
            _settings.Profile = _usage.Id;
            _settings.Save();
            Log.Info("profile", "profile suggested", new { profile = _usage.Id });
        }
        else
        {
            // A laptop profile copied to a desktop (or no longer offered) falls back to Gaming for this session only.
            _usage = profiles.Profiles.FirstOrDefault(p => p.Id == _settings.Profile && p.IsAvailable(_facts)) ?? profiles.Default;
        }
    }

    /// <summary>Rates tweaks and findings through the active profile and rebuilds the recommendation plan.</summary>
    private void ApplyProfile()
    {
        _profiled = ProfileView.For(_usage, _tweakStates, _facts);
        _profiledFindings = ProfileView.Findings(_usage, _findings);
        _plan = Optimizer.Core.Tweaks.Recommendations.Build(_profiled, _profiledFindings);
    }

    /// <summary>Picker entries in the current language: the profiles this PC offers (laptop profiles only on laptops).</summary>
    private void BuildProfileOptions()
    {
        var options = _catalog.Profiles.Profiles.Where(p => p.IsAvailable(_facts))
            .Select(p => new ProfileOption(p.Id, ProfileName(p), Loc.Instance[$"ProfileDesc_{p.Id}"],
                Enum.TryParse<SymbolRegular>(p.Icon, out var icon) ? icon : SymbolRegular.Games24))
            .ToList();
        // Replacing the list while a combo box is changing its selection would blank it: only when the entries differ.
        if (!options.SequenceEqual(ProfileOptions))
        {
            ProfileOptions.Clear();
            foreach (var o in options) ProfileOptions.Add(o);
        }
        // Set the field directly: the change handler is for user choices.
#pragma warning disable MVVMTK0034
        _selectedProfile = ProfileOptions.FirstOrDefault(o => o.Id == _usage.Id);
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(SelectedProfile));
        ProfileDescription = Loc.Instance[$"ProfileDesc_{_usage.Id}"];
        SuggestedProfileText = _suggested is { } s && s.Id != _usage.Id && Profile is not null ? Loc.Instance.Format("Profile_Suggested", ProfileName(s)) : null;
    }

    /// <summary>Applies every reset change again (one confirmation). Expert changes are left for the Changes page.</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ReapplyDriftAsync()
    {
        var safe = _drift.Where(d => d.Tweak.IsBatchSafe).Select(d => d.Tweak).ToList();
        var expert = _drift.Count - safe.Count;
        if (safe.Count > 0) await Runner.ApplyBatchAsync(safe, Loc.Instance["Drift_ApplyAgain"], Loc.Instance["Drift_ConfirmIntro"]);
        if (expert > 0) ShowResult(Loc.Instance.Format("Drift_ExpertIndividually", expert));
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ReapplyRecordAsync(string tweakId)
    {
        if (ResolveTweak(tweakId) is { } t) await Runner.ApplyAsync(t);
    }

    private string DriftReason(DriftItem d) =>
        d.WindowsUpdatedSince ? Loc.Instance.Format("Drift_ResetUpdate", d.AppliedOn, d.Current) : Loc.Instance["Drift_ResetOther"];

    // ---------------- apply / undo ----------------

    /// <summary>The row's action button: the same action its label names (<see cref="TweakItemViewModel.Action"/>).</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task RunActionAsync(InspectorItem? item)
    {
        item ??= SelectedItem;
        switch (item)
        {
            case TweakItemViewModel { Action: RowAction.Undo } t:
                await Runner.UndoAsync(t.Tweak);
                break;
            case TweakItemViewModel { Action: RowAction.Apply or RowAction.ApplyAgain } t:
                await Runner.ApplyAsync(t.Tweak);
                break;
            case FindingItemViewModel { Fix: { } fix }:
                await Runner.ApplyAsync(fix);
                break;
        }
    }

    /// <summary>Switch on a tweak row: on = apply (confirmation first), off = undo. The switch shows the real state.</summary>
    public async Task ToggleAsync(TweakItemViewModel item, bool on)
    {
        try
        {
            if (!CanChange) ShowResult(Loc.Instance["Change_Busy"]);
            else if (on && !item.IsOn) await Runner.ApplyAsync(item.Tweak);
            else if (!on && item.HasBackup) await Runner.UndoAsync(item.Tweak);
            else if (!on) ShowResult(Loc.Instance["Tweak_NoBackup"]);
        }
        finally
        {
            item.ResetSwitch();
        }
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ApplyRecommendedAsync()
    {
        if (_plan.IsEmpty)
        {
            ShowResult(Loc.Instance["Rec_None"]);
            return;
        }
        await Runner.ApplyBatchAsync(_plan.Items.Select(i => i.Tweak).ToList(), Loc.Instance["Rec_Title"], Loc.Instance["Rec_Intro"]);
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task UndoRecordAsync(string tweakId)
    {
        if (ResolveTweak(tweakId) is { } t) await Runner.UndoAsync(t);
        else ShowResult(Loc.Instance.Format("Result_Error", tweakId), Wpf.Ui.Controls.InfoBarSeverity.Error);
    }

    /// <summary>Catalog entry, runtime fix of this scan, or the definition stored with the backup.</summary>
    public TweakDefinition? ResolveTweak(string id) =>
        _services.Catalog.Get(id) ?? _findings.Select(f => f.Fix).FirstOrDefault(f => f?.Id == id) ?? _services.Engine.Resolve(id);

    /// <summary>The runner refreshes the pages after it (also when some changes could not be undone).</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private Task UndoAllAsync() => Runner.UndoAllAsync();

    /// <summary>Opens Windows' System Restore wizard (rstrui.exe from System32) to go back to a restore point.</summary>
    [RelayCommand]
    private void OpenSystemRestore()
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(Optimizer.Core.Platform.ProcessHardening.ResolveSystemTool("rstrui.exe")) { UseShellExecute = false };
            Optimizer.Core.Platform.ProcessHardening.Apply(psi);
            System.Diagnostics.Process.Start(psi)?.Dispose();
        }
        catch (Exception ex)
        {
            Log.Error("ui", "starting System Restore failed", ex);
            ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task EnableRestorePointsAsync()
    {
        ChangeGate.Instance.Busy = true;
        IsBusy = true;
        try
        {
            await Task.Run(() => _services.RestorePoints.Enable());
        }
        catch (Exception ex)
        {
            Log.Error("ui", "enabling System Restore failed", ex);
            ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
        finally
        {
            IsBusy = false;
            ChangeGate.Instance.Busy = false;
        }
        await RefreshChangesStateAsync();
    }

    /// <summary>
    /// Tweaks that change findings need a rescan; page-level changes (startup, services, features) refresh their page.
    /// Null: several changes at once (Undo all).
    /// </summary>
    private async Task OnChangedAsync(TweakDefinition? t)
    {
        try
        {
            if (t?.Category is "Startup" or "Services" or "Features")
            {
                await RefreshChangesStateAsync();
                return;
            }
            await AfterChangeAsync();
        }
        catch (Exception ex)
        {
            Log.Error("ui", "refresh after a change failed", ex);
        }
    }

    private bool _reuseSlowParts;

    private async Task AfterChangeAsync()
    {
        var result = ResultText;
        var severity = ResultSeverity;
        _reuseSlowParts = true;
        await ScanAsync();
        if (result is not null) ShowResult(result, severity);
    }

    // The Changes page reads every backup file and asks WMI whether System Restore is on: done in the background after a
    // scan or a change, not on every rebuild (filter, language, theme, profile).
    private IReadOnlyList<Optimizer.Core.Backup.TweakBackup> _backups = [];
    private bool? _restoreEnabled;

    // States of changes the tweak lists do not cover (startup entries, services, tasks, the restore point frequency),
    // so their Changes rows show On or Off like the others.
    private IReadOnlyDictionary<string, TweakState> _otherChangeStates = new Dictionary<string, TweakState>();

    private async Task RefreshChangesStateAsync()
    {
        var store = _services.Store;
        var engine = _services.Engine;
        var facts = _facts;
        var known = _tweakStates.Concat(_deviceStates).Select(s => s.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        (_backups, _restoreEnabled, _otherChangeStates) = await Task.Run(() =>
        {
            var backups = store.All();
            return (backups, SafeRestoreEnabled(), OtherChangeStates(engine, backups, known, facts));
        });
        BuildChanges();
    }

    private static IReadOnlyDictionary<string, TweakState> OtherChangeStates(
        TweakEngine engine, IReadOnlyList<Optimizer.Core.Backup.TweakBackup> backups, HashSet<string> known, Facts facts)
    {
        var states = new Dictionary<string, TweakState>(StringComparer.Ordinal);
        foreach (var b in backups.Where(b => !known.Contains(b.TweakId)))
        {
            // A retired tweak has no actions: its state cannot be read, only undone.
            if (engine.Resolve(b.TweakId) is not { Actions.Count: > 0 } t) continue;
            try
            {
                states[b.TweakId] = engine.DetectState(t, facts);
            }
            catch (Exception ex)
            {
                Log.Warn("changes", $"state of {b.TweakId} unreadable: {ex.Message}");
            }
        }
        return states;
    }

    public HashSet<string> AppliedIds() =>
        _tweakStates.Concat(_deviceStates).Where(s => s.State is TweakState.Applied or TweakState.PendingRestart).Select(s => s.Tweak.Id).ToHashSet();

    // ---------------- building view state ----------------

    /// <summary>
    /// Theme switched (in Settings or by Windows): rows and status colors are rebuilt, and properties whose value did
    /// not change are raised again, so their brushes are looked up in the new theme.
    /// </summary>
    public void OnThemeChanged()
    {
        Rebuild();
        Network.Rebuild();
        NotifyScore();
        Health.OnThemeChanged();
    }

    /// <summary>Recreates all language-dependent items (after a scan, a language switch or a theme switch).</summary>
    public void Rebuild()
    {
        var selectedKey = SelectedItem?.Key;

        BuildProfileOptions();
        BuildFindingLists();
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(IsGerman));

        BuildCategories();
        FillTweaks();
        BuildRecommendations();
        BuildBanners();
        BuildSummary();
        BuildChanges();
        HardwareSections.Clear();
        if (Profile is not null)
            foreach (var s in HardwareReport.Build(Profile, Loc.Instance)) HardwareSections.Add(s);

        RestoreSelection(selectedKey);
        OnPropertyChanged(nameof(AboutText));
    }

    /// <summary>
    /// Only the findings changed (the background CPU sample): the finding lists, score and plan are built again, while
    /// the tweak, network and hardware rows stay, so pages that show them need not draw again.
    /// </summary>
    private void RebuildFindings()
    {
        var selectedKey = SelectedItem?.Key;
        BuildFindingLists();
        BuildRecommendations();
        BuildBanners();
        RestoreSelection(selectedKey);
    }

    private void RestoreSelection(string? key) =>
        SelectedItem = key is null ? null :
            Findings.Cast<InspectorItem>().Concat(AdvisorItems).Concat(Tweaks).Concat(Network.DeviceTweaks).Append(GameAccess).FirstOrDefault(i => i?.Key == key);

    /// <summary>The finding and advisor lists, the score and the counts on the overview.</summary>
    private void BuildFindingLists()
    {
        var lang = Loc.Instance.Language;
        Fill(Findings, _profiledFindings.Where(f => f.Kind == FindingKind.Finding), lang);
        Fill(AdvisorItems, _profiledFindings.Where(f => f.Kind == FindingKind.Advisor), lang);
        OnPropertyChanged(nameof(FindingsEmpty));
        OnPropertyChanged(nameof(AdvisorEmpty));
        var access = _profiledFindings.FirstOrDefault(f => f.Kind == FindingKind.GameAccess);
        GameAccess = access is null ? null : new FindingItemViewModel(access, lang);
        var hidden = ProfileView.HiddenProblems(_usage, _findings);
        HiddenFindingsText = hidden > 0 ? Loc.Instance.Format("Findings_HiddenByProfile", hidden) : null;

        // The score counts what matters for the profile, weighted by the profile's impact.
        Score = ReadinessScore.Compute(_profiledFindings);
        ScoreTitle = Loc.Instance.Format("Dash_ScoreFor", ProfileName(_usage));
        ScoreHint = Loc.Instance.Format("Dash_ScoreHintGoal", Labels.Current.Get(lang, $"effect.{_usage.Goal}"));
        var problems = _profiledFindings.Count(f => f.IsProblem && f.Kind != FindingKind.GameAccess);
        ProblemSummary = problems == 0 ? Loc.Instance["Dash_NoProblems"] : Loc.Instance.Format("Dash_Problems", problems);
        ScoreVerdict = Loc.Instance[Score >= 85 ? "Verdict_Good" : Score >= 60 ? "Verdict_Fair" : "Verdict_Poor"];
        Counts.Clear();
        var findingProblems = _profiledFindings.Count(f => f.IsProblem && f.Kind == FindingKind.Finding);
        var advisorProblems = _profiledFindings.Count(f => f.IsProblem && f.Kind == FindingKind.Advisor);
        Counts.Add(new CountItem(findingProblems.ToString(), Loc.Instance["Count_Problems"], findingProblems > 0 ? "Problem" : "Ok"));
        Counts.Add(new CountItem(advisorProblems.ToString(), Loc.Instance["Count_Advisor"], advisorProblems > 0 ? "Problem" : "Ok"));
        Counts.Add(new CountItem(_profiledFindings.Count(f => f.Status == FindingStatus.Ok).ToString(), Loc.Instance["Count_Passed"], "Ok"));
        ScoreStatus = Score >= 85 ? "Ok" : Score >= 60 ? "Problem" : "Critical";
        LastScanText = _lastScanTime is { } t ? Loc.Instance.Format("LastScan", t.ToString("HH:mm")) : "";
        if (Profile is { } prof)
            Headline = string.Join(", ", new[] { prof.Cpu?.Name, prof.Gpus?.FirstOrDefault(g => g.Kind == GpuKind.Discrete)?.Name ?? prof.Gpus?.FirstOrDefault()?.Name,
                $"Windows 11 {prof.Os.DisplayVersion}" }.Where(x => !string.IsNullOrEmpty(x)));
    }

    private void Fill(ObservableCollection<FindingItemViewModel> target, IEnumerable<Finding> source, string lang)
    {
        target.Clear();
        foreach (var f in source.Where(f => ShowPassed || f.Status is not (FindingStatus.Ok or FindingStatus.Unsupported)))
            target.Add(new FindingItemViewModel(f, lang, FixFor(f), _usage.Goal));
    }

    /// <summary>Runtime fix of the finding, or the first catalog tweak that fixes it and is not on yet.</summary>
    private TweakDefinition? FixFor(Finding f) =>
        f.Fix is { } fix
            ? ProfileView.RuntimeFixAllowed(fix, _profiled) ? fix : null
            : _profiled.FirstOrDefault(p => p.CanFix(f.Id) && (ExpertMode || p.Tweak.EffectiveRisk != Risk.Expert))?.Tweak;

    /// <param name="profileFilter">Tweaks page: only what the active profile rates (unless "Only this profile" is off).</param>
    private IEnumerable<ProfiledTweak> VisibleTweaks(bool profileFilter = true) =>
        _profiled.Where(p => ExpertMode || p.Tweak.EffectiveRisk != Risk.Expert || p.Status.HasBackup)
                 .Where(p => p.Status.State != TweakState.NotApplicable || ShowPassed)
                 .Where(p => !profileFilter || !OnlyProfile || p.Relevant);

    private void BuildCategories()
    {
        var lang = Loc.Instance.Language;
        var visible = VisibleTweaks().ToList();
        var selected = SelectedCategory?.Key ?? "";
        Categories.Clear();
        Categories.Add(new CategoryItem("", Loc.Instance["Tweaks_All"], visible.Count));
        foreach (var g in visible.GroupBy(p => p.Tweak.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
            Categories.Add(new CategoryItem(g.Key, Labels.Current.Get(lang, $"category.{g.Key}"), g.Count()));
        // Set the field directly: the change handler would refill the list, which the caller does anyway.
#pragma warning disable MVVMTK0034
        _selectedCategory = Categories.FirstOrDefault(c => c.Key == selected) ?? Categories[0];
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(SelectedCategory));
    }

    private void FillTweaks()
    {
        var lang = Loc.Instance.Language;
        var category = SelectedCategory?.Key ?? "";
        Tweaks.Clear();
        var list = VisibleTweaks()
            .Where(p => category.Length == 0 || p.Tweak.Category == category)
            .Where(p => !OnlyRecommended || p.Recommended)
            .OrderByDescending(p => p.Recommended)
            .ThenByDescending(p => p.Flagged) // applied changes that work against the profile stay visible
            .ThenByDescending(p => p.Impact)
            .ThenBy(p => p.Tweak.Category, StringComparer.Ordinal)
            .ToList();
        foreach (var p in list) Tweaks.Add(new TweakItemViewModel(p.Status, lang, _services.Engine, this, profiled: p, profile: _usage));
        var visible = VisibleTweaks().ToList();
        TweaksSummary = Loc.Instance.Format("Tweaks_Summary", visible.Count, visible.Count(p => p.Status.IsOn), visible.Count(p => p.Recommended));
    }

    /// <summary>Rows for catalog tweaks shown on other pages (GPU settings, DNS), with the same Expert filter as the Tweaks page.</summary>
    public IEnumerable<TweakItemViewModel> CatalogItems(Func<TweakDefinition, bool> filter) =>
        VisibleTweaks(profileFilter: false).Where(p => filter(p.Tweak) && p.Status.State != TweakState.NotApplicable)
            .Select(p => new TweakItemViewModel(p.Status, Loc.Instance.Language, _services.Engine, this, profiled: p, profile: _usage)).ToList();

    private void BuildRecommendations()
    {
        var lang = Loc.Instance.Language;
        string Reason(RecommendedItem i) =>
            i.FixesFinding is { } f ? Loc.Instance.Format("Rec_Fixes", new FindingItemViewModel(f, lang).Title)
            : i.ReasonKey is { } k ? Labels.Current.Get(lang, k) : Loc.Instance["Rec_Matches"];
        string Excluded(RecommendedItem i) =>
            i.Tweak.EffectiveRisk == Risk.Expert ? Labels.Current.Get(lang, "risk.Expert")
            : i.Tweak.AntiCheatSensitive ? Labels.Current.Get(lang, "badge.antiCheat")
            : Labels.Current.Get(lang, $"reversibility.{i.Tweak.Reversibility}");
        Recommendations.Clear();
        foreach (var i in _plan.Items)
            Recommendations.Add(new RecommendationLine(Runner.Title(i.Tweak), Reason(i), Loc.Instance.Format("Impact_Short", i.Impact)));
        RecommendationsExcluded.Clear();
        foreach (var i in _plan.Excluded)
            RecommendationsExcluded.Add(new RecommendationLine(Runner.Title(i.Tweak), Excluded(i), Loc.Instance.Format("Impact_Short", i.Impact)));
        HasRecommendations = _plan.Items.Count > 0;
        RecommendedButtonText = Loc.Instance.Format("Rec_Button", _plan.Items.Count);
        RecommendationsIntro = Loc.Instance.Format("Rec_IntroProfile", ProfileName(_usage));

        // Changes on this PC that lower the profile's goal (for example a battery drain in the Battery profile).
        AgainstProfile.Clear();
        foreach (var p in _profiled.Where(p => p.Flagged).OrderBy(p => p.Impact))
            AgainstProfile.Add(new AgainstLine(p.Tweak.Id, Runner.Title(p.Tweak), p.ReasonKey is { } k ? Labels.Current.Get(lang, k) : "", p.Status.HasBackup));
        HasAgainst = AgainstProfile.Count > 0;
    }

    public void BuildChanges()
    {
        var lang = Loc.Instance.Language;
        ChangeRecords.Clear();
        foreach (var b in _backups.OrderByDescending(b => b.LastApplied))
        {
            var t = ResolveTweak(b.TweakId);
            var title = t is null ? b.TweakId : Runner.Title(t);
            var state = _tweakStates.Concat(_deviceStates).FirstOrDefault(s => s.Tweak.Id == b.TweakId)?.State
                ?? (_otherChangeStates.TryGetValue(b.TweakId, out var other) ? other : null);
            var drift = _drift.FirstOrDefault(d => d.Tweak.Id == b.TweakId);
            if (drift is not null) state = TweakState.RevertedByWindows;
            ChangeRecords.Add(new ChangeRecordItem(
                b.TweakId,
                title,
                b.LastApplied.LocalDateTime.ToString("g"),
                Loc.Instance.Format("Changes_Entries", b.Entries.Count),
                state is null ? "" : Labels.Current.Get(lang, $"state.{state}"),
                state is TweakState.RevertedByWindows ? "Problem" : "Ok",
                t is not null)
            {
                ResetText = drift is null ? null : DriftReason(drift),
            });
        }
        OnPropertyChanged(nameof(ChangesEmpty));

        var sr = _restoreEnabled;
        RestorePointText = sr switch { true => Loc.Instance["Rp_On"], false => Loc.Instance["Rp_Off"], _ => Loc.Instance["Rp_Unknown"] };
        CanEnableRestorePoints = sr == false && IsElevated;

        ChangeLog.Clear();
        foreach (var e in Log.Newest(e => e.Source == "change", 200))
            ChangeLog.Add(new LogLine(e.Time.ToString("HH:mm:ss"), e.Message + (e.Data is { } d ? "  " + d : "")));
        OnPropertyChanged(nameof(ChangeLogEmpty));
    }

    private bool? SafeRestoreEnabled()
    {
        if (!IsElevated) return null; // System Restore WMI needs admin rights; never guess "off"
        try
        {
            return _services.RestorePoints.IsEnabled();
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>Developer aid for screenshots: shows the drift banner for three catalog tweaks without touching anything.</summary>
    public void PreviewDrift()
    {
        _drift = _services.Catalog.Tweaks.Where(t => t.IsBatchSafe).Take(3)
            .Select(t => new DriftItem(t, new Optimizer.Core.Backup.TweakBackup { TweakId = t.Id }, "26300.9000", _services.Os.BuildString))
            .ToList();
        BuildBanners();
    }

    private void BuildBanners()
    {
        Banners.Clear();
        if (Profile is null) return;
        if (_drift.Count > 0)
        {
            var names = _drift.Take(4).Select(d => Runner.Title(d.Tweak)).ToList();
            var list = string.Join(", ", names) + (_drift.Count > names.Count ? " " + Loc.Instance.Format("Drift_More", _drift.Count - names.Count) : "");
            var update = _drift.FirstOrDefault(d => d.WindowsUpdatedSince);
            var text = update is not null ? Loc.Instance.Format("Drift_BannerUpdate", update.Current, list) : Loc.Instance.Format("Drift_BannerOther", list);
            Banners.Add(new Banner(text, true) { ActionText = Loc.Instance["Drift_ApplyAgain"], Action = ReapplyDriftCommand });
        }
        else if (_updateNotice is not null)
        {
            Banners.Add(new Banner(_updateNotice, false));
        }
        if (UpdateAvailable)
            Banners.Add(CanSelfUpdate
                ? new Banner(UpdateStatus!, false) { ActionText = Loc.Instance["Update_Install"], Action = UpdateNowCommand, ActionIcon = SymbolRegular.ArrowDownload20 }
                : new Banner(UpdateStatus!, false) { ActionText = Loc.Instance["Update_Open"], Action = OpenReleasePageCommand, ActionIcon = SymbolRegular.Open20 });
        if (_profileAutoSet) Banners.Add(new Banner(Loc.Instance.Format("Profile_AutoSet", ProfileName(_usage)), false));
        var pending = _tweakStates.Concat(_deviceStates).Count(s => s.State is TweakState.PendingRestart or TweakState.UndoPendingRestart);
        if (pending > 0) Banners.Add(new Banner(Loc.Instance.Format("Banner_Restart", pending), true));
        var os = Profile.Os;
        if (OsGate.Evaluate(os.Build, os.NativeArchitecture) == OsGateResult.SupportedNotValidated)
            Banners.Add(new Banner(Loc.Instance.Format("Banner_NotValidated", os.BuildString), true));
        if (os.FlightingActive) Banners.Add(new Banner(Loc.Instance.Format("Banner_Insider", os.FlightingDetail), false));
        if (OsGate.Is24H2EndOfUpdates(os.Build, os.Edition, DateOnly.FromDateTime(DateTime.Today)))
            Banners.Add(new Banner(Loc.Instance["Banner_EndOfUpdates"], true));
        if (Profile.Managed?.IsManaged == true) Banners.Add(new Banner(Loc.Instance["Banner_Managed"], true));
        if (Profile.Elevation is { } e)
        {
            if (!e.IsElevated) Banners.Add(new Banner(Loc.Instance["Banner_NotElevated"], true));
            if (e.ShowSeparateAdminBanner) Banners.Add(new Banner(Loc.Instance.Format("Banner_SeparateAdmin", e.ProcessUser, e.SessionUser), true));
        }
    }

    private void BuildSummary()
    {
        Summary.Clear();
        if (Profile is not { } p) return;
        var l = Loc.Instance;
        if (p.Cpu is { } cpu) Summary.Add(new SummaryItem(l["Sum_Cpu"], cpu.Name));
        if (p.Gpus is { } gpus)
            Summary.Add(new SummaryItem(l["Sum_Gpu"], string.Join("\n", gpus.Where(g => g.Kind is not GpuKind.Virtual).Select(g => g.Name))));
        if (p.Memory is { } m)
        {
            var first = m.Modules.FirstOrDefault();
            var speed = first is null ? "" : $" {first.Type}-{Optimizer.Core.Findings.Checks.RamSpeed.NormalizeConfigured(first)}";
            Summary.Add(new SummaryItem(l["Sum_Memory"], $"{m.TotalBytes / (double)(1L << 30):0} GB{speed}, {m.Modules.Count} " + l["Sum_Modules"]));
        }
        if (p.Displays is { Count: > 0 } d)
            Summary.Add(new SummaryItem(l["Sum_Displays"], string.Join("\n", d.Select(x => $"{x.FriendlyName}, {x.Width} × {x.Height}, {x.CurrentRefresh.Hz:0.##} Hz"))));
        if (p.Firmware is { } fw) Summary.Add(new SummaryItem(l["Sum_Board"], $"{fw.BoardManufacturer} {fw.BoardProduct}\nBIOS {fw.BiosVersion}"));
        Summary.Add(new SummaryItem(l["Sum_Windows"], $"Windows 11 {p.Os.DisplayVersion} {p.Os.Edition}\nBuild {p.Os.BuildString}"));
    }
}
