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
    public InfoBarSeverity Severity => IsWarning ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;

    /// <summary>Optional button in the info bar (e.g. "Apply again").</summary>
    public string? ActionText { get; init; }

    public System.Windows.Input.ICommand? Action { get; init; }

    public SymbolRegular ActionIcon { get; init; } = SymbolRegular.ArrowSync20;
}

public sealed record SummaryItem(string Label, string Value);

/// <summary>Status drives the color of the number (problems in caution color, passed in success color).</summary>
public sealed record CountItem(string Value, string Label, string Status);

public sealed record HwSection(string Title, IReadOnlyList<SummaryItem> Items);

public sealed record CategoryItem(string Key, string Text, int Count)
{
    public string Display => $"{Text} ({Count})";
}

public sealed record ChangeRecordItem(string TweakId, string Title, string When, string Details, string StateText, string Status, bool CanUndo)
{
    /// <summary>Set when the change is no longer in place: why (Windows update or other) for the row.</summary>
    public string? ResetText { get; init; }

    public bool IsReset => ResetText is not null;
}

public sealed record LogLine(string Time, string Text);

/// <summary>One line of the "Apply recommended" plan: what, why, and its impact.</summary>
public sealed record RecommendationLine(string Title, string Reason, string ImpactText, bool IsFix);

/// <summary>A usage profile in the picker.</summary>
public sealed record ProfileOption(string Id, string Name, string Description, SymbolRegular Icon);

/// <summary>A change on this PC that works against the active profile (with Undo when this app made it).</summary>
public sealed record AgainstLine(string TweakId, string Title, string Reason, bool CanUndo);

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
        Runner.Status += (_, text) => ShowResult(text);
        Runner.BusyChanged += (_, busy) => IsBusy = busy;
        Runner.Changed += async (_, t) => await OnChangedAsync(t);

        Network = new NetworkViewModel(this, services);
        Debloat = new DebloatViewModel(this, services, dialogs);
        Cleanup = new CleanupViewModel(this, services, dialogs);
        Startup = new StartupViewModel(this, services, Runner);
        ServicesPage = new ServicesViewModel(this, services, Runner);
        Apps = new AppsViewModel(this, services);
        Tools = new ToolsViewModel(this, services, Runner, dialogs);
        Health = new HealthViewModel(this, dialogs);
        _virusTotalConfigured = !string.IsNullOrEmpty(settings.VirusTotalKey);
        ShowPendingCounts();
        Loc.Instance.LanguageChanged += (_, _) => Rebuild();
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
    public UsageProfile ActiveProfile => _usage;

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

    public IReadOnlyList<Finding> AllFindings => _findings;
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

    public string Version => typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.3.0";
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
        _ = value switch
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
        };
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
        NotifyScore();
        if (value) ShowPendingCounts();
    }

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
        using var check = new ReleaseCheck();
        _release = await check.CheckAsync(typeof(MainViewModel).Assembly.GetName().Version ?? new Version(0, 0, 0));
        _releaseChecking = false;
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
    public static string UpdatesFolder => System.IO.Path.Combine(DataPaths.Root, "updates");

    /// <summary>
    /// Self-update only replaces the published single-file exe running elevated. A Debug build has PCOptimizer.dll next
    /// to its exe and gets the download page instead.
    /// </summary>
    public bool CanSelfUpdate => UpdateAvailable && _release?.Assets is not null && DataPaths.ProcessIsElevated &&
        Environment.ProcessPath is { } exe && System.IO.Path.GetFileName(exe).Equals("PCOptimizer.exe", StringComparison.OrdinalIgnoreCase) &&
        !System.IO.File.Exists(System.IO.Path.ChangeExtension(exe, ".dll"));

    [ObservableProperty] private bool _updating;

    /// <summary>After confirmation: download the release exe, check its SHA-256, replace this exe and restart.</summary>
    [RelayCommand]
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
                ShowResult(Loc.Instance[download.Outcome == UpdateOutcome.ChecksumMismatch ? "Update_BadChecksum" : "Update_Failed"]);
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
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(exe) { UseShellExecute = false });
            System.Windows.Application.Current.Shutdown();
        }
        finally
        {
            Updating = false;
        }
    }

    private System.Windows.Threading.DispatcherTimer? _resultTimer;

    /// <summary>Shows the result of an action at the bottom; it closes by itself after long enough to read it.</summary>
    public void ShowResult(string text)
    {
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

    [RelayCommand(AllowConcurrentExecutions = false)]
    public async Task ScanAsync()
    {
        IsScanning = true;
        ScanStatus = Loc.Instance["Scan_Running"];
        Headline = Loc.Instance["Headline_Scanning"];
        string? error = null;
        try
        {
            var progress = new Progress<string>(probe => ScanStatus = Loc.Instance.Format("Scan_Probe", probe));
            var profile = await new HardwareScanner(_catalog).ScanAsync(progress);
            var registry = _services.Context.Registry;
            _findings = await Task.Run(() => new FindingEngine(_catalog, registry).Evaluate(profile));
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
    }

    /// <summary>Re-reads every tweak's state (after a scan or after applying/undoing).</summary>
    private async Task RefreshTweaksAsync()
    {
        if (Profile is null) return;
        var profile = Profile;
        var engine = _services.Engine;
        var registry = _services.Context.Registry;
        (_facts, _tweakStates, _deviceStates) = await Task.Run(() =>
        {
            var facts = FactsBuilder.Build(profile, _findings, _catalog, registry);
            var catalog = engine.DetectAll(_services.Catalog.Visible, facts);
            var device = engine.DetectAll(DeviceTweaks.Build(profile), facts);
            return (facts, catalog, device);
        });
        EnsureProfile();
        ApplyProfile();
        var facts = _facts;
        _drift = await Task.Run(() => engine.CheckDrift(facts));
        foreach (var d in _drift)
            Log.Warn("drift", $"{d.Tweak.Id} is no longer in place", new { d.AppliedOn, d.Current, d.WindowsUpdatedSince });

        // Once per Windows update: say so when all changes survived it (a reset shows in the drift banner instead).
        var current = _services.Os.BuildString;
        var count = _services.Store.All().Count;
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
        OnPropertyChanged(nameof(ActiveProfile));
    }

    /// <summary>Applies every reset change again (one confirmation). Expert changes are left for the Changes page.</summary>
    [RelayCommand]
    private async Task ReapplyDriftAsync()
    {
        var safe = _drift.Where(d => d.Tweak.IsBatchSafe).Select(d => d.Tweak).ToList();
        var expert = _drift.Count - safe.Count;
        if (safe.Count > 0) await Runner.ApplyBatchAsync(safe, Loc.Instance["Drift_ApplyAgain"], Loc.Instance["Drift_ConfirmIntro"]);
        if (expert > 0) ShowResult(Loc.Instance.Format("Drift_ExpertIndividually", expert));
    }

    [RelayCommand]
    private async Task ReapplyRecordAsync(string tweakId)
    {
        if (ResolveTweak(tweakId) is { } t) await Runner.ApplyAsync(t);
    }

    private string DriftReason(DriftItem d) =>
        d.WindowsUpdatedSince ? Loc.Instance.Format("Drift_ResetUpdate", d.AppliedOn, d.Current) : Loc.Instance["Drift_ResetOther"];

    // ---------------- apply / undo ----------------

    [RelayCommand]
    private async Task RunActionAsync(InspectorItem? item)
    {
        item ??= SelectedItem;
        switch (item)
        {
            case TweakItemViewModel t when t.HasBackup:
                await Runner.UndoAsync(t.Tweak);
                break;
            case TweakItemViewModel t:
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
            if (on && !item.IsOn) await Runner.ApplyAsync(item.Tweak);
            else if (!on && item.HasBackup) await Runner.UndoAsync(item.Tweak);
            else if (!on) ShowResult(Loc.Instance["Tweak_NoBackup"]);
        }
        finally
        {
            item.ResetSwitch();
        }
    }

    [RelayCommand]
    private async Task ApplyRecommendedAsync()
    {
        if (_plan.IsEmpty)
        {
            ShowResult(Loc.Instance["Rec_None"]);
            return;
        }
        await Runner.ApplyBatchAsync(_plan.Items.Select(i => i.Tweak).ToList(), Loc.Instance["Rec_Title"], Loc.Instance["Rec_Intro"]);
    }

    [RelayCommand]
    private async Task UndoRecordAsync(string tweakId)
    {
        if (ResolveTweak(tweakId) is { } t) await Runner.UndoAsync(t);
        else ShowResult(Loc.Instance.Format("Result_Failed", tweakId));
    }

    /// <summary>Catalog entry, runtime fix of this scan, or the definition stored with the backup.</summary>
    public TweakDefinition? ResolveTweak(string id) =>
        _services.Catalog.Get(id) ?? _findings.Select(f => f.Fix).FirstOrDefault(f => f?.Id == id) ?? _services.Engine.Resolve(id);

    [RelayCommand]
    private async Task UndoAllAsync()
    {
        var count = _services.Store.All().Count;
        if (count == 0 || !_dialogs.ConfirmUndoAll(count)) return;
        IsBusy = true;
        try
        {
            var results = await Task.Run(() => _services.Engine.RevertAll());
            var failed = results.Count(r => !r.Result.Success);
            ShowResult(failed == 0 ? Loc.Instance.Format("Result_UndoAll", results.Count) : Loc.Instance.Format("Result_Failed", $"{failed} / {results.Count}"));
        }
        finally
        {
            IsBusy = false;
        }
        await AfterChangeAsync();
    }

    [RelayCommand]
    private async Task EnableRestorePointsAsync()
    {
        IsBusy = true;
        try
        {
            await Task.Run(() => _services.RestorePoints.Enable());
        }
        catch (Exception ex)
        {
            ShowResult(Loc.Instance.Format("Result_Failed", ex.Message));
        }
        finally
        {
            IsBusy = false;
        }
        BuildChanges();
    }

    /// <summary>Tweaks that change findings need a rescan; page-level changes (startup, services, features) refresh their page.</summary>
    private async Task OnChangedAsync(TweakDefinition t)
    {
        if (t.Category is "Startup" or "Services" or "Features")
        {
            BuildChanges();
            return;
        }
        await AfterChangeAsync();
    }

    private async Task AfterChangeAsync()
    {
        var result = ResultText;
        await ScanAsync();
        if (result is not null) ShowResult(result);
        BuildChanges();
    }

    public HashSet<string> AppliedIds() =>
        _tweakStates.Concat(_deviceStates).Where(s => s.State is TweakState.Applied or TweakState.PendingRestart).Select(s => s.Tweak.Id).ToHashSet();

    // ---------------- building view state ----------------

    /// <summary>Recreates all language-dependent items (after a scan, a language switch or a theme switch).</summary>
    public void Rebuild()
    {
        var lang = Loc.Instance.Language;
        var selectedKey = SelectedItem?.Key;

        BuildProfileOptions();
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

        SelectedItem = selectedKey is null ? null :
            Findings.Cast<InspectorItem>().Concat(AdvisorItems).Concat(Tweaks).Concat(Network.DeviceTweaks).Append(GameAccess).FirstOrDefault(i => i?.Key == selectedKey);
        OnPropertyChanged(nameof(AboutText));
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
            Recommendations.Add(new RecommendationLine(Runner.Title(i.Tweak), Reason(i), Loc.Instance.Format("Impact_Short", i.Impact), i.FixesFinding is not null));
        RecommendationsExcluded.Clear();
        foreach (var i in _plan.Excluded)
            RecommendationsExcluded.Add(new RecommendationLine(Runner.Title(i.Tweak), Excluded(i), Loc.Instance.Format("Impact_Short", i.Impact), i.FixesFinding is not null));
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
        foreach (var b in _services.Store.All().OrderByDescending(b => b.LastApplied))
        {
            var t = ResolveTweak(b.TweakId);
            var title = t is null ? b.TweakId : Runner.Title(t);
            var state = _tweakStates.Concat(_deviceStates).FirstOrDefault(s => s.Tweak.Id == b.TweakId)?.State;
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

        var sr = SafeRestoreEnabled();
        RestorePointText = sr switch { true => Loc.Instance["Rp_On"], false => Loc.Instance["Rp_Off"], _ => Loc.Instance["Rp_Unknown"] };
        CanEnableRestorePoints = sr == false && IsElevated;

        ChangeLog.Clear();
        foreach (var e in Log.Session.Where(e => e.Source == "change").Reverse().Take(200))
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
        var pending = _tweakStates.Concat(_deviceStates).Count(s => s.State == TweakState.PendingRestart);
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
            var speed = first is null ? "" : $" {first.Type}-{Optimizer.Core.Findings.Checks.RamSpeed.NormalizeConfigured(first.ConfiguredMts, first.Type)}";
            Summary.Add(new SummaryItem(l["Sum_Memory"], $"{m.TotalBytes / (double)(1L << 30):0} GB{speed}, {m.Modules.Count} " + l["Sum_Modules"]));
        }
        if (p.Displays is { Count: > 0 } d)
            Summary.Add(new SummaryItem(l["Sum_Displays"], string.Join("\n", d.Select(x => $"{x.FriendlyName}, {x.Width} × {x.Height}, {x.CurrentRefresh.Hz:0.##} Hz"))));
        if (p.Firmware is { } fw) Summary.Add(new SummaryItem(l["Sum_Board"], $"{fw.BoardManufacturer} {fw.BoardProduct}\nBIOS {fw.BiosVersion}"));
        Summary.Add(new SummaryItem(l["Sum_Windows"], $"Windows 11 {p.Os.DisplayVersion} {p.Os.Edition}\nBuild {p.Os.BuildString}"));
    }
}
