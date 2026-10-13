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
    /// <summary>Optional second line under the value (secondary text), for example the model under a disk size.</summary>
    public string? Detail { get; init; }

    /// <summary>Optional usage bar under the value, 0 to 1 (used share of a volume).</summary>
    public double? Fill { get; init; }

    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Label}: {Value}";
}

public sealed record HwSection(string Title, IReadOnlyList<SummaryItem> Items)
{
    public SymbolRegular Icon { get; init; } = SymbolRegular.Info24;

    /// <summary>IDs and raw values, shown only with "Technical details" on (and always in the copied text and the report).</summary>
    public IReadOnlyList<SummaryItem> Technical { get; init; } = [];

    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;
}

/// <summary>A summary tile at the top of the System info page.</summary>
public sealed record HwTile(string Caption, string Value, string Detail, SymbolRegular Icon)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Caption}: {Value}, {Detail}";
}

/// <summary>
/// Heading row of a group on the Tweaks page: recommended, on but working against the profile, or a category. A click
/// opens or closes it; the rows of an open group follow it in the same list, so the list still builds only the cards
/// in view.
/// </summary>
public sealed partial class TweakGroupRow(string key, string title, SymbolRegular icon, string summary, IReadOnlyList<TweakItemViewModel> items) : ObservableObject
{
    public string Key { get; } = key;
    public string Title { get; } = title;
    public SymbolRegular Icon { get; } = icon;
    public string Summary { get; } = summary;
    public IReadOnlyList<TweakItemViewModel> Items { get; } = items;

    /// <summary>
    /// What "Turn on" in the heading applies: the group's tweaks that are off and safe to apply together (safe, fully
    /// reversible, not boot-critical, no anti-cheat concern, not a preview), with nothing blocking them.
    /// </summary>
    public IReadOnlyList<TweakItemViewModel> Applicable { get; } = ApplicableOf(items);

    /// <summary>
    /// Off, Safe risk and batch-safe, and nothing blocks it (missing admin rights block every row alike: the button is
    /// then off with the rest of the page). Alternatives that exclude each other (the power plans) are left out: which
    /// one to use is a choice, not something to turn on all at once.
    /// </summary>
    private static List<TweakItemViewModel> ApplicableOf(IReadOnlyList<TweakItemViewModel> items)
    {
        var candidates = items.Where(i => !i.IsOn && i.CanToggle && i.Tweak.IsBatchSafe && i.Tweak.EffectiveRisk == Risk.Safe
                                          && i.Status_.Blocks.All(b => b.ReasonKey == "block.notElevated")).ToList();
        var ids = candidates.Select(i => i.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        return candidates.Where(i => !i.Tweak.ConflictsWith.Any(ids.Contains)
                                     && !candidates.Any(o => o.Tweak.ConflictsWith.Contains(i.Tweak.Id))).ToList();
    }

    public bool CanApplyAll => Applicable.Count > 0;

    public string ApplyAllText => Loc.Instance.Format("Tweaks_GroupApply", Applicable.Count);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Chevron))]
    private bool _isExpanded;

    public SymbolRegular Chevron => IsExpanded ? SymbolRegular.ChevronUp20 : SymbolRegular.ChevronDown20;

    /// <summary>What screen readers announce for the heading.</summary>
    public string AutomationName => $"{Title}, {Summary}";

    public override string ToString() => AutomationName;
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

/// <summary>
/// One line of the "Apply recommended" plan: what, why, and its impact. Lines of the plan can be unticked, so the user
/// applies only what they agree with.
/// </summary>
public sealed partial class RecommendationLine(string title, string reason, int impact, TweakDefinition? tweak = null, Action? selectionChanged = null) : ObservableObject
{
    public string Title { get; } = title;
    public string Reason { get; } = reason;

    /// <summary>The line's rating mark (the impact for the profile) and its words for screen readers and the tooltip.</summary>
    public int Rating { get; } = impact;
    public string ImpactText { get; } = Loc.Instance.Format("Impact_Short", impact);
    public bool IsDisputed { get; } = tweak?.Impact.Basis == "disputed";
    public TweakDefinition? Tweak { get; } = tweak;

    [ObservableProperty] private bool _isSelected = true;

    partial void OnIsSelectedChanged(bool value) => selectionChanged?.Invoke();

    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;
}

/// <summary>A usage profile in the picker.</summary>
public sealed record ProfileOption(string Id, string Name, string Description, SymbolRegular Icon)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;
}

/// <summary>
/// A setting on this PC that works against the active profile: Undo when this app made it, otherwise Details, whose page
/// says how to change it back (the app has no original value to restore).
/// </summary>
public sealed record AgainstLine(string TweakId, string Title, string Reason, bool CanUndo)
{
    public bool SetOutside => !CanUndo;

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
                         RunActionCommand, ApplyRecommendedCommand, ApplyTweakGroupCommand, UndoAllCommand, UndoRecordCommand, ReapplyRecordCommand,
                         ReapplyDriftCommand, EnableRestorePointsCommand, ScanCommand, UpdateNowCommand, ImportChangesCommand, UninstallCommand,
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
        Loc.Instance.LanguageChanged += (_, _) =>
        {
            Rebuild();
            Network.Rebuild();
            foreach (var page in new PageViewModel[] { Debloat, Cleanup, Startup, ServicesPage, Apps, Tools, Health }) page.OnLanguageChanged();
        };
    }

    /// <summary>Every page that loads its own data.</summary>
    public IReadOnlyList<PageViewModel> Pages => [Network, Debloat, Cleanup, Startup, ServicesPage, Apps, Tools, Health];

    /// <summary>
    /// After the first scan, the pages that read slow data of their own (startup entries, services, programs and
    /// drivers, AppX packages, folder sizes) load it in the background, one after another and only while the window is
    /// idle, so they open with their rows instead of a loading state. Everything here only reads; a page the user opens
    /// meanwhile starts its own load at once. Tools is not among them: its two DISM lists take about 12 seconds of
    /// processor time when elevated, too much to spend on every start for a page that is opened rarely.
    /// </summary>
    public async Task PreloadPagesAsync()
    {
        foreach (var page in new PageViewModel[] { Startup, ServicesPage, Apps, Debloat, Cleanup })
        {
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            await page.EnsureLoadedAsync();
        }
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
    [ObservableProperty] private string _lastScanText = "";
    [ObservableProperty] private string _headline = "";
    [ObservableProperty] private string _scoreVerdict = "";

    /// <summary>The Overview's verdict: one sentence on what holds this PC back (the problems listed right under it).</summary>
    [ObservableProperty] private string _verdict = "";

    /// <summary>The readiness score and the passed checks, one quiet line under the verdict.</summary>
    [ObservableProperty] private string _readinessLine = "";

    /// <summary>"To change by hand: 1": the way to Manual checks when the BIOS or the hardware has something to change.</summary>
    [ObservableProperty] private string? _advisorLinkText;
    [ObservableProperty] private bool _showPassed;
    [ObservableProperty] private InspectorItem? _selectedItem;
    [ObservableProperty] private FindingItemViewModel? _gameAccess;
    [ObservableProperty] private bool _onlyRecommended;
    /// <summary>Tweaks page: words in the title or summary (or the tweak id). Not saved.</summary>
    [ObservableProperty] private string _tweakSearch = "";
    [ObservableProperty] private bool _expertMode;
    [ObservableProperty] private bool _checkForUpdates;
    [ObservableProperty] private string _restorePointText = "";
    [ObservableProperty] private bool _canEnableRestorePoints;

    /// <summary>The safety net card's icon follows the state: a check only when System Protection is known to be on.</summary>
    [ObservableProperty] private SymbolRegular _restorePointIcon = SymbolRegular.Shield24;
    [ObservableProperty] private string _tweaksSummary = "";
    [ObservableProperty] private string? _resultText;

    /// <summary>The result is about a change: the bar offers the way to the Changes page.</summary>
    [ObservableProperty] private bool _resultShowsChanges;

    /// <summary>The result says a new version is out: the bar offers the update (<see cref="ResultUpdateText"/>).</summary>
    [ObservableProperty] private bool _resultShowsUpdate;
    [ObservableProperty] private string? _resultUpdateText;

    /// <summary>Counts in the navigation (null = no badge).</summary>
    [ObservableProperty] private string? _overviewBadge;
    [ObservableProperty] private string? _advisorBadge;

    /// <summary>A new version is available: Settings, where the update is, carries a badge on every page.</summary>
    [ObservableProperty] private string? _settingsBadge;

    [RelayCommand]
    private void OpenChanges()
    {
        ResultOpen = false;
        CurrentPage = Page.Changes;
    }
    [ObservableProperty] private bool _resultOpen;
    [ObservableProperty] private string _recommendedButtonText = "";
    [ObservableProperty] private bool _hasRecommendations;
    [ObservableProperty] private string _recommendationsEmptyText = "";
    [ObservableProperty] private bool _virusTotalConfigured;
    [ObservableProperty] private ProfileOption? _selectedProfile;
    [ObservableProperty] private bool _onlyProfile = true;
    [ObservableProperty] private string _profileDescription = "";
    [ObservableProperty] private string? _suggestedProfileText;
    [ObservableProperty] private string _scoreHint = "";
    [ObservableProperty] private string? _hiddenFindingsText;
    /// <summary>BIOS and hardware page: how many passed checks "Show passed checks" would add (null when shown or none).</summary>
    [ObservableProperty] private string? _advisorPassedText;
    [ObservableProperty] private string _recommendationsIntro = "";
    [ObservableProperty] private bool _hasAgainst;
    [ObservableProperty] private string _againstTitle = "";

    public ObservableCollection<ProfileOption> ProfileOptions { get; } = [];
    public ObservableCollection<AgainstLine> AgainstProfile { get; } = [];

    /// <summary>Developer switch --profile: use this profile for the session without saving it.</summary>
    public string? ProfileOverride { get; set; }

    public ObservableCollection<FindingItemViewModel> Findings { get; } = [];
    public ObservableCollection<FindingItemViewModel> AdvisorItems { get; } = [];
    /// <summary>Every tweak that passes the filters, also those in closed groups (selection, details pane).</summary>
    public ObservableCollection<TweakItemViewModel> Tweaks { get; } = [];

    /// <summary>What the Tweaks page lists: each group's heading, followed by its tweaks when the group is open.</summary>
    public ObservableCollection<object> TweakRows { get; } = [];
    public ObservableCollection<ChangeRecordItem> ChangeRecords { get; } = [];
    public ObservableCollection<LogLine> ChangeLog { get; } = [];
    public ObservableCollection<Banner> Banners { get; } = [];
    public ObservableCollection<HwSection> HardwareSections { get; } = [];
    public ObservableCollection<HwTile> HardwareTiles { get; } = [];

    /// <summary>System info: also show IDs and raw values (PnP IDs, GUIDs, microcode). Not saved.</summary>
    [ObservableProperty] private bool _showHardwareDetails;
    public ObservableCollection<RecommendationLine> Recommendations { get; } = [];
    public ObservableCollection<RecommendationLine> RecommendationsExcluded { get; } = [];

    public Facts Facts => _facts;
    public IReadOnlyList<TweakStatus> DeviceStates => _deviceStates;

    public bool FindingsEmpty => Profile is not null && Findings.Count == 0;
    public bool AdvisorEmpty => Profile is not null && AdvisorItems.Count == 0;

    /// <summary>Manual checks page: what was checked and how it came out, above the list (which hides the passed ones).</summary>
    [ObservableProperty] private string? _advisorSummaryTitle;
    [ObservableProperty] private string? _advisorSummaryText;
    [ObservableProperty] private bool _advisorAllGood;

    private void BuildAdvisorSummary()
    {
        // Checks that do not apply to this PC (no such hardware) are not counted as checked.
        var all = _profiledFindings.Where(f => f.Kind == FindingKind.Advisor && f.Status != FindingStatus.Unsupported).ToList();
        if (Profile is null || all.Count == 0)
        {
            AdvisorSummaryTitle = null;
            return;
        }
        var problems = all.Count(f => f.IsProblem);
        var passed = all.Count(f => f.Status == FindingStatus.Ok);
        var unknown = all.Count(f => f.Status == FindingStatus.Unknown);
        var notes = all.Count(f => f.Status == FindingStatus.Info);
        AdvisorAllGood = problems == 0;
        AdvisorSummaryTitle = problems == 0 ? Loc.Instance["Advisor_SummaryGood"] : Loc.Instance.Format("Advisor_SummaryProblems", problems);
        // "9 checked: 8 passed, 1 could not be checked", parts of zero left out (like the counts on the Tweaks page)
        var parts = new[]
        {
            (Count: passed, Key: "Advisor_CountPassed"),
            (Count: unknown, Key: "Advisor_CountUnknown"),
            (Count: notes, Key: "Advisor_CountNotes"),
        }.Where(x => x.Count > 0).Select(x => Loc.Instance.Format(x.Key, x.Count)).ToList();
        var checkedText = Loc.Instance.Format("Advisor_SummaryCounts", all.Count);
        AdvisorSummaryText = parts.Count == 0 ? checkedText : $"{checkedText}: {string.Join(", ", parts)}";
    }

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

    partial void OnShowPassedChanged(bool value)
    {
        // Set from elsewhere (the overview, the search): the Changed view no longer holds.
        if (value) _onlyChanged = false;
        OnPropertyChanged(nameof(TweakViewMode));
        Rebuild();
    }

    partial void OnOnlyRecommendedChanged(bool value)
    {
        // Set from elsewhere (the overview, the search): the Changed view no longer holds.
        if (value) _onlyChanged = false;
        OnPropertyChanged(nameof(TweakViewMode));
        RefillTweaks();
    }

    partial void OnTweakSearchChanged(string value) => RefillTweaks();

    partial void OnOnlyProfileChanged(bool value)
    {
        // Set from elsewhere (the overview, the search): the Changed view no longer holds.
        if (value) _onlyChanged = false;
        OnPropertyChanged(nameof(TweakViewMode));
        RefillTweaks();
    }

    /// <summary>A filter or the search changed: the rows are created again, and the selected tweak stays selected if it is still listed.</summary>
    private void RefillTweaks()
    {
        var selectedKey = SelectedItem?.Key;
        FillTweaks();
        RestoreSelection(selectedKey);
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

    /// <summary>Developer aid (--perf): switches the profile as the picker does, without saving it.</summary>
    public void SetProfileForSession(ProfileOption option)
    {
        if (option.Id == _usage.Id) return;
#pragma warning disable MVVMTK0034
        _selectedProfile = option;
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(SelectedProfile));
        _usage = _catalog.Profiles.Get(option.Id);
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
            // The page also lists the installed programs, which the Apps view model reads.
            Page.Debloat => Task.WhenAll(Debloat.EnsureLoadedAsync(), Apps.EnsureLoadedAsync()),
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

    /// <summary>Developer aid (--perf): switches Expert mode for this session as the switch does, without the question and without saving.</summary>
    public void SetExpertModeForSession(bool value)
    {
#pragma warning disable MVVMTK0034
        _expertMode = value;
#pragma warning restore MVVMTK0034
        OnPropertyChanged(nameof(ExpertMode));
        Rebuild();
        Network.Rebuild();
    }

    /// <summary>Score and counts only after a finished scan: during a scan the cards keep their layout but show "--".</summary>
    public bool HasScore => Profile is not null && !IsScanning;

    /// <summary>While a scan runs (and before the first): what the scan is doing, not an old or empty verdict.</summary>
    public string VerdictShown => HasScore ? Verdict : Loc.Instance["Dash_VerdictPending"];
    public string ReadinessShown => HasScore ? ReadinessLine : "";
    public string? AdvisorLinkShown => HasScore ? AdvisorLinkText : null;

    private void NotifyScore()
    {
        foreach (var name in new[] { nameof(HasScore), nameof(VerdictShown), nameof(ReadinessShown), nameof(AdvisorLinkShown) })
            OnPropertyChanged(name);
    }

    partial void OnVerdictChanged(string value) => NotifyScore();
    partial void OnReadinessLineChanged(string value) => NotifyScore();
    partial void OnAdvisorLinkTextChanged(string? value) => NotifyScore();
    partial void OnProfileChanged(HardwareProfile? value) => NotifyScore();

    partial void OnIsScanningChanged(bool value)
    {
        ChangeGate.Instance.Scanning = value;
        NotifyScore();
    }

    /// <summary>No change, scan or update is running: switches and change buttons are enabled.</summary>
    public bool CanChange => ChangeGate.Instance.CanChange;

    /// <summary>A scan may start while no change or update runs (a running scan is joined, not repeated).</summary>
    private bool CanScan() => !ChangeGate.Instance.Busy && !ChangeGate.Instance.Updating;

    /// <summary>
    /// The simple or the technical view of the lists: off hides paths, registry locations and package names (the details
    /// pane, confirmations and copied reports keep them).
    /// </summary>
    public bool ShowTechnical
    {
        get => _settings.TechnicalDetails;
        set
        {
            if (value == _settings.TechnicalDetails) return;
            _settings.TechnicalDetails = value;
            _settings.Save();
            OnPropertyChanged();
        }
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
        OnPropertyChanged(nameof(CanUpdateInApp));
        BuildBanners();
        SettingsBadge = UpdateAvailable ? "1" : null;
        if (atStart && UpdateAvailable) ShowUpdateNotice();
    }

    /// <summary>
    /// The check at start found a new version: the result bar says so on whatever page is open, with the button that
    /// updates (or opens the release page when this copy cannot update itself), and stays until it is closed or used.
    /// </summary>
    /// <summary>Developer aid (--screenshot with --update-shot): as if the check at start had found <paramref name="latest"/>.</summary>
    public void PreviewUpdateFound(Version latest)
    {
        _release = new ReleaseCheckResult(ReleaseCheckStatus.NewerAvailable, latest);
        OnPropertyChanged(nameof(UpdateStatus));
        OnPropertyChanged(nameof(UpdateAvailable));
        OnPropertyChanged(nameof(CanUpdateInApp));
        BuildBanners();
        SettingsBadge = "1";
        ShowUpdateNotice();
    }

    private void ShowUpdateNotice()
    {
        if (_release?.Latest is not { } latest) return;
        var version = latest.ToString(3);
        // A package manager offers the version only after its review (winget and Chocolatey can take days): its copy gets
        // the bar once per version, saying so; the Settings badge stays until the update is installed.
        if (Package is { } package)
        {
            if (_settings.UpdateNoticeShownFor == version) return;
            _settings.UpdateNoticeShownFor = version;
            _settings.Save();
            ShowResult(Loc.Instance.Format("Update_AvailablePackage", version, package.DisplayName));
        }
        else
        {
            ShowResult(UpdateStatus!);
        }
        _resultTimer?.Stop();
        ResultUpdateText = Loc.Instance[CanUpdateInApp ? "Update_Install" : "Update_Open"];
        ResultShowsUpdate = true;
    }

    [RelayCommand]
    private Task CheckUpdatesNow() => CheckForUpdatesAsync(atStart: false);

    [RelayCommand]
    private void OpenReleasePage() => OpenLink(ReleaseCheck.LatestReleaseUrl);

    /// <summary>
    /// Opens the GitHub bug form with version and Windows build filled in. Both are part of the link, so GitHub receives
    /// them when the browser opens it (docs/PRIVACY.md); the report itself is sent only when the user submits it there.
    /// </summary>
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

    /// <summary>Copies the System info page as text (technical rows included), for a forum post or a bug report.</summary>
    [RelayCommand]
    private void CopyHardware()
    {
        try
        {
            System.Windows.Clipboard.SetText(HardwareReport.ToText(HardwareSections));
            ShowResult(Loc.Instance["Hw_Copied"]);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            ShowResult(Loc.Instance["Log_CopyFailed"]);
        }
    }

    /// <summary>Downloaded updates (protected data folder); emptied on the next start.</summary>
    public static string UpdatesFolder => DataPaths.Updates;

    /// <summary>
    /// Self-update only replaces the published single-file exe running elevated. A Debug build has Tunero.dll next
    /// to its exe and gets the download page instead. The exe's own name does not matter: browsers save a second download
    /// as "Tunero (1).exe", and the update replaces the file under the name it has.
    /// </summary>
    public bool CanSelfUpdate => UpdateAvailable && _release?.Assets is not null && UpdateSignature.IsConfigured && DataPaths.ProcessIsElevated &&
        IsPublishedExe && Package is null;

    /// <summary>The published single-file exe (a Debug or Release build has Tunero.dll next to its exe).</summary>
    public static bool IsPublishedExe { get; } = Environment.ProcessPath is { } exe &&
        System.IO.Path.GetExtension(exe).Equals(".exe", StringComparison.OrdinalIgnoreCase) &&
        !System.IO.File.Exists(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(exe) ?? "", typeof(MainViewModel).Assembly.GetName().Name + ".dll"));

    private PackageInstall? _package;
    private bool _packageDetected;

    /// <summary>
    /// Installed with winget, Chocolatey or Scoop: the package manager installs the update and removes the app, so its
    /// record of the version stays right. Null for a downloaded copy.
    /// </summary>
    public PackageInstall? Package
    {
        get
        {
            if (!_packageDetected)
            {
                _package = Environment.ProcessPath is { } exe && IsPublishedExe ? PackageInstall.Detect(exe, _services.ProfilePath) : null;
                _packageDetected = true;
            }
            return _package;
        }
    }

    /// <summary>"Update now" is offered: the app replaces its exe, or hands the update to the package manager.</summary>
    public bool CanUpdateInApp => CanSelfUpdate || (UpdateAvailable && Package is not null);

    [ObservableProperty] private bool _updating;

    partial void OnUpdatingChanged(bool value) => ChangeGate.Instance.Updating = value;

    /// <summary>After confirmation: download the release exe, check its signature and SHA-256, replace this exe and restart.</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task UpdateNowAsync()
    {
        if (Package is { } package && UpdateAvailable && _release?.Latest is { } packageVersion)
        {
            UpdateWithPackageManager(package, packageVersion.ToString(3));
            return;
        }
        if (!CanSelfUpdate || _release is not { Latest: { } latest } release || Environment.ProcessPath is not { } exe)
        {
            OpenReleasePage();
            return;
        }
        // Any change (tweaks and page operations such as uninstalls) or scan must be finished first.
        if (ChangeGate.Instance.Busy || ChangeGate.Instance.Scanning || Updating)
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
            if (ChangeGate.Instance.Busy)
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
            bool started;
            try
            {
                started = Updater.StartVerified(exe, download.Sha256!, path => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = false })?.Dispose());
            }
            catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                // The new version is in place; only its start failed (a program holds the file, or antivirus blocked it).
                Log.Error("update", "installed, but the restart failed", ex);
                ShowResult(Loc.Instance.Format("Update_RestartFailed", version), InfoBarSeverity.Warning);
                return;
            }
            if (!started)
            {
                // The running version stays in use: it goes back in place of the file that was swapped.
                var rolledBack = Updater.RollBack(exe);
                Log.Error("update", $"the installed exe changed before the restart; not started (previous exe put back: {rolledBack})");
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

    /// <summary>
    /// After confirmation: starts the package manager in its own window and ends the app (no package manager can replace a
    /// running exe). The window waits for the app to end, updates it and starts it again. winget and Scoop run as the
    /// signed-in user, Chocolatey elevated (<see cref="PackageInstall"/>).
    /// </summary>
    private void UpdateWithPackageManager(PackageInstall package, string version)
    {
        if (ChangeGate.Instance.Busy || ChangeGate.Instance.Scanning || Updating || Environment.ProcessPath is not { } exe)
        {
            ShowResult(Loc.Instance["Update_Busy"]);
            return;
        }
        // choco.exe is checked once, before the question: the same path goes into the window's command.
        var choco = package.TrustedChocolatey();
        if (package.Manager == PackageManager.Chocolatey ? choco is null : !package.CanRunFromApp())
        {
            ShowResult(Loc.Instance.Format("Update_PackageManual", package.DisplayName, package.UpdateCommand), InfoBarSeverity.Warning);
            return;
        }
        // Chocolatey's window is elevated and starts Tunero elevated: Windows does not ask again.
        var text = package.RunsAsUser ? "Update_PackageText" : "Update_PackageTextElevated";
        if (!_dialogs.Ask(Loc.Instance.Format("Update_ConfirmTitle", version), Loc.Instance.Format(text, package.DisplayName, version), Loc.Instance["Update_Restart"])) return;
        var arguments = package.UpdateArguments(Environment.ProcessId, exe, Loc.Instance.Format("Update_PackageRunning", package.DisplayName),
            Loc.Instance.Format("Update_PackageFailed", package.DisplayName), choco, version);
        if (!StartPackageManager(package, arguments, out _))
        {
            ShowResult(Loc.Instance.Format("Update_PackageManual", package.DisplayName, package.UpdateCommand), InfoBarSeverity.Warning);
            return;
        }
        Log.Info("update", "handed to the package manager, closing", new { package.Manager, version });
        System.Windows.Application.Current.Shutdown();
    }

    /// <summary>
    /// The package manager's PowerShell window: as the signed-in user, or elevated for Chocolatey, whose process id comes
    /// back in <paramref name="elevatedProcessId"/> (the uninstall cleanup waits for it).
    /// </summary>
    private bool StartPackageManager(PackageInstall package, string arguments, out int? elevatedProcessId)
    {
        elevatedProcessId = null;
        if (package.RunsAsUser) return DeElevatedLauncher.Open(WingetPackage.PowerShell, arguments, _services.Elevation) != DeElevatedLauncher.Path.Failed;
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo(ProcessHardening.ResolveSystemTool("powershell.exe"), arguments) { UseShellExecute = false };
            ProcessHardening.Apply(psi);
            package.HardenChocolateyEnvironment(psi.Environment);
            using var process = System.Diagnostics.Process.Start(psi);
            elevatedProcessId = process?.Id;
            return process is not null;
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Error("package", "PowerShell not started", ex);
            return false;
        }
    }

    // ---------------- Start menu entry ----------------

    private string? StartMenuFolder => StartMenuShortcut.ProgramsFolder(_services.Elevation, _services.ProfilePath);

    /// <summary>The app has an entry in the signed-in user's Start menu, so Windows Search finds it.</summary>
    public bool StartMenuEntry
    {
        get => StartMenuFolder is { } folder && StartMenuShortcut.Exists(folder);
        set
        {
            if (value != StartMenuEntry) SetStartMenuEntry(value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Only the published exe gets an entry (a build folder is not where the app lives). Scoop creates its own entry (Scoop
    /// Apps) from the manifest and removes it with the app, so a Scoop copy does not add a second one.
    /// </summary>
    public bool CanAddToStartMenu => IsPublishedExe && Package?.Manager != PackageManager.Scoop;

    private void SetStartMenuEntry(bool on)
    {
        if (StartMenuFolder is not { } folder || Environment.ProcessPath is not { } exe) return;
        try
        {
            if (on) StartMenuShortcut.Create(folder, exe, Loc.Instance["StartMenu_Description"]);
            else StartMenuShortcut.Remove(folder);
            Log.Info("startmenu", on ? "entry added" : "entry removed", new { folder });
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            Log.Error("startmenu", "entry not changed", ex);
            ShowResult(Loc.Instance.Format("StartMenu_Failed", ex.Message), InfoBarSeverity.Warning);
        }
    }

    /// <summary>First start of the published exe: asks once whether to add the Start menu entry.</summary>
    public void OfferStartMenuEntry()
    {
        if (_settings.StartMenuAsked || !CanAddToStartMenu || StartMenuEntry) return;
        _settings.StartMenuAsked = true;
        _settings.Save();
        if (_dialogs.Ask(Loc.Instance["StartMenu_AskTitle"], Loc.Instance["StartMenu_AskText"], Loc.Instance["StartMenu_Add"])) StartMenuEntry = true;
    }

    // ---------------- Uninstall ----------------

    /// <summary>
    /// Only the published exe removes itself (a build folder is not where the app lives), and only elevated: the data
    /// folder is admin-only.
    /// </summary>
    public bool CanUninstall => IsPublishedExe && DataPaths.ProcessIsElevated;

    /// <summary>
    /// After confirmation: undoes the app's changes if the user wants (and stops when one fails), removes the Start menu
    /// entry, starts the cleanup that deletes the data folder and the exe once the app has ended (a winget copy is removed
    /// by winget, as the signed-in user) and ends the app (<see cref="SelfUninstall"/>).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task UninstallAsync()
    {
        if (!CanUninstall || Environment.ProcessPath is not { } exe) return;
        if (ChangeGate.Instance.Busy || ChangeGate.Instance.Scanning || Updating)
        {
            ShowResult(Loc.Instance["Uninstall_Busy"]);
            return;
        }
        // Before anything is undone: a package manager the app may not start leaves the command to type. choco.exe is
        // checked once, here: the same path goes into the window's command after the undo.
        var choco = Package?.TrustedChocolatey();
        if (Package is { } manual && (manual.Manager == PackageManager.Chocolatey ? choco is null : !manual.CanRunFromApp()))
        {
            ShowResult(Loc.Instance.Format("Uninstall_PackageManual", manual.DisplayName, manual.UninstallCommand), InfoBarSeverity.Warning);
            return;
        }
        var active = await Task.Run(() => _services.Store.All().Count);
        var choice = _dialogs.ConfirmUninstall(active, DataPaths.Root);
        if (choice == UninstallChoice.Cancel) return;
        if (choice == UninstallChoice.UndoFirst && !await Runner.UndoAllAsync(confirm: false, incompleteNote: Loc.Instance["Uninstall_UndoFailed"]))
            return;

        var processName = System.IO.Path.GetFileNameWithoutExtension(exe);
        int? packageManagerProcess = null;
        if (Package is { } package)
        {
            if (!StartPackageManager(package, package.UninstallArguments(processName,
                    Loc.Instance.Format("Uninstall_PackageRunning", package.DisplayName), Loc.Instance.Format("Uninstall_PackageFailed", package.DisplayName),
                    choco), out packageManagerProcess))
            {
                ShowResult(Loc.Instance.Format("Uninstall_PackageManual", package.DisplayName, package.UninstallCommand), InfoBarSeverity.Warning);
                return;
            }
        }
        // A Start menu entry that cannot be removed (a link put in its place) does not stop the uninstall.
        try
        {
            if (StartMenuFolder is { } folder) StartMenuShortcut.Remove(folder);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException)
        {
            Log.Warn("uninstall", $"Start menu entry not removed: {ex.Message}");
        }
        try
        {
            var script = SelfUninstall.CleanupScript(processName, DataPaths.Root, exe, removedByPackageManager: Package is not null, packageManagerProcess);
            var psi = new System.Diagnostics.ProcessStartInfo(ProcessHardening.ResolveSystemTool("powershell.exe"), SelfUninstall.PowerShellArguments(script))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            ProcessHardening.Apply(psi);
            System.Diagnostics.Process.Start(psi)?.Dispose();
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Error("uninstall", "cleanup not started", ex);
            // The package manager's window already waits for the app to end: it removes the app, and the data folder
            // with the backups stays (deleted by hand, or by the next copy's Uninstall).
            if (Package is null)
            {
                ShowResult(Loc.Instance.Format("Uninstall_Failed", ex.Message), InfoBarSeverity.Error);
                return;
            }
        }
        Log.Info("uninstall", "cleanup started, closing", new { package = Package?.Manager, undone = choice == UninstallChoice.UndoFirst });
        System.Windows.Application.Current.Shutdown();
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
        ResultShowsChanges = false;
        ResultShowsUpdate = false;
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
        // Errors and warnings stay until closed: they must not disappear before they are read.
        if (!ResultOpen || severity is InfoBarSeverity.Error or InfoBarSeverity.Warning) return;
        _resultTimer.Interval = TimeSpan.FromSeconds(Math.Clamp(5 + text.Length / 15.0, 8, 20));
        _resultTimer.Start();
    }

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

    // Takes any row: the Tweaks list reuses a card for a group heading and for a tweak, and while it switches the
    // template the old card asks with the other kind of row (a typed command would throw).
    [RelayCommand]
    private void Select(object? row)
    {
        if (row is InspectorItem item) SelectedItem = item;
    }

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

    /// <summary>The scan that runs now, or a finished task.</summary>
    public Task ScanInProgress => _scan is { IsCompleted: false } scan ? scan : Task.CompletedTask;

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
            var service = ScanService;
            var lastSample = _lastBackgroundSample;
            // Scan and findings in one trip to the background: at the start the UI thread is busy drawing the window,
            // and a return to it in between waited for that. The page is ready without the 3 second background CPU
            // sample; that one check follows when it is done.
            var (profile, findings, sampleTask) = await Task.Run(() => service.RunAsync(progress, previous, lastSample, waitForBackgroundSample: false));
            pendingSample = sampleTask;
            _findings = findings;
            Profile = profile;
            await RefreshTweaksAsync();
            _lastScanTime = DateTime.Now;
            var rows = System.Diagnostics.Stopwatch.StartNew();
            Rebuild();
            Network.Rebuild();
            foreach (var page in new PageViewModel[] { Debloat, Apps, Tools, Health }) page.OnProfileChanged();
            Log.Debug("scan", "rows built", new { ms = rows.ElapsedMilliseconds });
        }
        catch (Exception ex)
        {
            Log.Error("ui", "scan failed", ex);
            error = Loc.Instance["Scan_Failed"];
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

    /// <summary>The scan pipeline (hardware probes, reads ahead for the tweak detection, checks), the same as --report uses.</summary>
    private ScanService ScanService => new(_catalog, _services.Context.Registry, _services.BcdElements, _services.Engine, _services.Catalog.Visible);

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
        var registry = _services.Context.Registry;
        var service = ScanService;
        var (completed, findings) = await Task.Run(() => service.WithSample(scanned, sample));
        var facts = await Task.Run(() => FactsBuilder.Build(completed, findings, _catalog, registry));
        if (!ReferenceEquals(Profile, scanned) || IsScanning) return;
        _findings = findings;
        Profile = completed;
        // Reading every tweak's state takes most of a second and recreating every row makes each page draw again; the
        // sample changes only its own check, which no tweak reads today. Both happen only when a tweak depends on it.
        if (TweaksDependOn(_facts, facts))
        {
            // A change that runs now rescans when it is done; states read in the middle of it would only be older.
            if (ChangeGate.Instance.Busy) return;
            await RefreshTweaksAsync();
            // A scan or change that started meanwhile has newer states: its rebuild follows.
            if (!ReferenceEquals(Profile, completed) || IsScanning) return;
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
        var findings = _findings;
        var engine = _services.Engine;
        var registry = _services.Context.Registry;
        var visible = _services.Catalog.Visible;
        var store = _services.Store;
        // One trip to the background and back (each return to the UI thread waits for what it is drawing).
        (_facts, _tweakStates, _deviceStates, _drift, var count) = await Task.Run(() =>
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var facts = FactsBuilder.Build(profile, findings, _catalog, registry);
            var catalog = engine.DetectAll(visible, facts);
            var device = engine.DetectAll(DeviceTweaks.Build(profile), facts);
            Log.Info("scan", "tweak states read", new { ms = watch.ElapsedMilliseconds });
            // The states just read are not read again (runtime tweaks of other pages are).
            var known = catalog.Concat(device).GroupBy(s => s.Tweak.Id, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First().State, StringComparer.Ordinal);
            var drift = engine.CheckDrift(facts, known);
            return (facts, catalog, device, drift, store.All().Count);
        });
        EnsureProfile();
        ApplyProfile();
        foreach (var d in _drift)
            Log.Warn("drift", $"{d.Tweak.Id} is no longer in place", new { d.AppliedOn, d.Current, d.WindowsUpdatedSince });

        // Once per Windows update: say so when all changes survived it (a reset shows in the drift banner instead).
        var current = _services.Os.BuildString;
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
        // Called from the switch's binding setter: the confirmation dialog must not open inside the click and the
        // switch's property update (undo reaches ShowDialog before its first await), as SwitchRow.RunAsync does.
        await Task.Yield();
        var done = false;
        try
        {
            if (!CanChange) ShowResult(Loc.Instance["Change_Busy"]);
            else if (on && !item.IsOn) done = await Runner.ApplyAsync(item.Tweak);
            else if (!on && item.HasBackup) done = await Runner.UndoAsync(item.Tweak);
            else if (!on) ShowResult(Loc.Instance["Tweak_NoBackup"]);
        }
        finally
        {
            // A switch that was just turned on and confirmed stays on until the rescan rebuilds the row (flipping back for a
            // few seconds reads like a failure); cancelled or failed, it shows the real state again.
            item.ResetSwitch(done ? on : null);
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
        // Only the ticked lines of the plan on the overview (all of them unless the user unticked some).
        var chosen = Recommendations.Where(r => r.IsSelected && r.Tweak is not null).Select(r => r.Tweak!).ToList();
        if (chosen.Count == 0)
        {
            ShowResult(Loc.Instance["Rec_NoneSelected"]);
            return;
        }
        await Runner.ApplyBatchAsync(chosen, Loc.Instance["Rec_Title"], Loc.Instance["Rec_Intro"]);
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
        // The same gate as every change: one at a time, errors logged and shown.
        await Runner.RunExclusiveAsync("enable System Restore", () => Task.Run(() => _services.RestorePoints.Enable()));
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
            if (t?.Category is "Startup" or "Services" or "Features" or "Priorities")
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
        if (result is not null)
        {
            ShowResult(result, severity);
            ResultShowsChanges = true;
        }
    }

    // The Changes page reads every backup file and asks WMI whether System Restore is on: done in the background after a
    // scan or a change, not on every rebuild (filter, language, theme, profile).
    private IReadOnlyList<Optimizer.Core.Backup.TweakBackup> _backups = [];
    private bool? _restoreEnabled;

    // The definitions of the backups, resolved with them in the background (a definition stored with a backup would
    // otherwise be read from its file again on the UI thread at every rebuild).
    private IReadOnlyDictionary<string, TweakDefinition?> _backupTweaks = new Dictionary<string, TweakDefinition?>();

    // States of changes the tweak lists do not cover (startup entries, services, tasks, the restore point frequency),
    // so their Changes rows show On or Off like the others.
    private IReadOnlyDictionary<string, TweakState> _otherChangeStates = new Dictionary<string, TweakState>();

    private async Task RefreshChangesStateAsync()
    {
        var store = _services.Store;
        var engine = _services.Engine;
        var facts = _facts;
        var known = _tweakStates.Concat(_deviceStates).Select(s => s.Tweak.Id).ToHashSet(StringComparer.Ordinal);
        (_backups, _restoreEnabled, _otherChangeStates, _backupTweaks) = await Task.Run(() =>
        {
            var backups = store.All();
            var tweaks = backups.ToDictionary(b => b.TweakId, b => engine.Resolve(b), StringComparer.Ordinal);
            return (backups, SafeRestoreEnabled(), OtherChangeStates(tweaks, engine, backups, known, facts), (IReadOnlyDictionary<string, TweakDefinition?>)tweaks);
        });
        BuildChanges();
    }

    private static IReadOnlyDictionary<string, TweakState> OtherChangeStates(IReadOnlyDictionary<string, TweakDefinition?> tweaks,
        TweakEngine engine, IReadOnlyList<Optimizer.Core.Backup.TweakBackup> backups, HashSet<string> known, Facts facts)
    {
        var states = new Dictionary<string, TweakState>(StringComparer.Ordinal);
        foreach (var b in backups.Where(b => !known.Contains(b.TweakId)))
        {
            // A retired tweak has no actions: its state cannot be read, only undone.
            if (tweaks.GetValueOrDefault(b.TweakId) is not { Actions.Count: > 0 } t) continue;
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

    /// <summary>Recreates all language-dependent items (after a scan, a language switch, a profile or Expert mode switch).</summary>
    public void Rebuild()
    {
        var selectedKey = SelectedItem?.Key;

        BuildProfileOptions();
        BuildFindingLists();
        OnPropertyChanged(nameof(IsEnglish));
        OnPropertyChanged(nameof(TweakViewOptions));
        OnPropertyChanged(nameof(TweaksProfileLink));
        OnPropertyChanged(nameof(TweakViewMode));
        OnPropertyChanged(nameof(IsGerman));

        FillTweaks();
        BuildRecommendations();
        BuildBanners();
        BuildChanges();
        BuildHardware();

        RestoreSelection(selectedKey);
        OnPropertyChanged(nameof(AboutText));
    }

    private (HardwareProfile? Profile, string Language) _hardwareShown;

    /// <summary>
    /// The hardware page shows the scan result only (no profile, Expert mode or theme brushes): built again for a new
    /// scan or language, so a switch elsewhere does not recreate its rows.
    /// </summary>
    private void BuildHardware()
    {
        var shown = (Profile, Loc.Instance.Language);
        if (ReferenceEquals(_hardwareShown.Profile, shown.Profile) && _hardwareShown.Language == shown.Language) return;
        _hardwareShown = shown;
        HardwareSections.Clear();
        HardwareTiles.Clear();
        if (Profile is null) return;
        foreach (var s in HardwareReport.Build(Profile, Loc.Instance)) HardwareSections.Add(s);
        foreach (var t in HardwareReport.Tiles(Profile, Loc.Instance)) HardwareTiles.Add(t);
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
        var passed = ShowPassed ? 0 : _profiledFindings.Count(f => f.Kind == FindingKind.Advisor && f.Status is FindingStatus.Ok or FindingStatus.Unsupported);
        AdvisorPassedText = passed > 0 ? Loc.Instance.Format("Advisor_PassedHidden", passed) : null;
        BuildAdvisorSummary();

        // The score counts what matters for the profile, weighted by the profile's impact.
        Score = ReadinessScore.Compute(_profiledFindings);
        ScoreHint = Loc.Instance.Format("Dash_ScoreHintGoal", Labels.Current.Get(lang, $"effect.{_usage.Goal}"));
        ScoreVerdict = Loc.Instance[Score >= 85 ? "Verdict_Good" : Score >= 60 ? "Verdict_Fair" : "Verdict_Poor"];
        var findingProblems = _profiledFindings.Count(f => f.IsProblem && f.Kind == FindingKind.Finding);
        var advisorProblems = _profiledFindings.Count(f => f.IsProblem && f.Kind == FindingKind.Advisor);
        // The verdict counts the problems listed right under it; BIOS and hardware have their own page and the link.
        Verdict = findingProblems switch
        {
            0 => Loc.Instance["Dash_VerdictNone"],
            1 => Loc.Instance["Dash_VerdictOne"],
            _ => Loc.Instance.Format("Dash_VerdictMany", findingProblems),
        };
        ReadinessLine = Loc.Instance.Format("Dash_Readiness", ProfileName(_usage), Score, ScoreVerdict,
            _profiledFindings.Count(f => f.Status == FindingStatus.Ok));
        AdvisorLinkText = advisorProblems > 0 ? Loc.Instance.Format("Dash_AdvisorLink", advisorProblems) : null;
        // The navigation shows where something waits: problems on the overview, things to change by hand.
        OverviewBadge = findingProblems > 0 ? findingProblems.ToString() : null;
        AdvisorBadge = advisorProblems > 0 ? advisorProblems.ToString() : null;
        LastScanText = _lastScanTime is { } t ? Loc.Instance.Format("LastScan", t.ToString("HH:mm")) : "";
        if (Profile is { } prof)
            Headline = string.Join(", ", new[] { prof.Cpu?.ShortName, prof.Gpus?.FirstOrDefault(g => g.Kind == GpuKind.Discrete)?.Name ?? prof.Gpus?.FirstOrDefault()?.Name,
                $"Windows 11 {prof.Os.DisplayVersion}" }.Where(x => !string.IsNullOrEmpty(x)));
    }

    private void Fill(ObservableCollection<FindingItemViewModel> target, IEnumerable<Finding> source, string lang)
    {
        target.Clear();
        // Problems first, then what could not be checked, then notes and passed checks (stable within each).
        static int Rank(FindingStatus s) => s switch
        {
            FindingStatus.Problem => 0,
            FindingStatus.Unknown => 1,
            FindingStatus.Info => 2,
            FindingStatus.Ok => 3,
            _ => 4,
        };
        foreach (var f in source.Where(f => ShowPassed || f.Status is not (FindingStatus.Ok or FindingStatus.Unsupported)).OrderBy(f => Rank(f.Status)))
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

    private const string RecommendedGroup = "recommended";
    private const string AgainstGroup = "against";

    private readonly List<TweakGroupRow> _tweakGroups = [];

    /// <summary>Groups open on the Tweaks page this session: recommended and against the profile start open, the sections closed.</summary>
    private readonly HashSet<string> _openGroups = [RecommendedGroup, AgainstGroup];

    /// <summary>The sections of the Tweaks page (<see cref="Optimizer.Core.Tweaks.TweakSections"/>) with their icons.</summary>
    private static readonly (string Key, SymbolRegular Icon, string[] Categories)[] TweakSections =
        Optimizer.Core.Tweaks.TweakSections.All.Select(s => (s.Key, SectionIcon(s.Key), s.Categories)).ToArray();

    private static SymbolRegular SectionIcon(string key) => key switch
    {
        "performance" => SymbolRegular.Flash24,
        "network" => SymbolRegular.Router24,
        "background" => SymbolRegular.Alert24,
        "privacy" => SymbolRegular.EyeOff24,
        "security" => SymbolRegular.Shield24,
        "desktop" => SymbolRegular.Window24,
        "browser" => SymbolRegular.Globe24,
        _ => SymbolRegular.Apps24,
    };

    private static int SectionIndex(string category) => Optimizer.Core.Tweaks.TweakSections.IndexOf(category);

    private Dictionary<string, int>? _categoryOrder;

    /// <summary>
    /// Fills the Tweaks page: recommended first, then applied changes that work against the profile, then the sections.
    /// In a group the larger impact first, then by category in catalog order, so related tweaks stay together. A search
    /// opens every group with a match.
    /// </summary>
    private void FillTweaks()
    {
        var lang = Loc.Instance.Language;
        var q = TweakSearch.Trim();
        bool Matches(ProfiledTweak p) => q.Length == 0 || p.Tweak.Id.Contains(q, StringComparison.OrdinalIgnoreCase)
            || Runner.Title(p.Tweak).Contains(q, StringComparison.CurrentCultureIgnoreCase) || Runner.Summary(p.Tweak).Contains(q, StringComparison.CurrentCultureIgnoreCase);
        _categoryOrder ??= _services.Catalog.Tweaks.Select(t => t.Category).Distinct()
            .Select((c, i) => (c, i)).ToDictionary(x => x.c, x => x.i);
        int CategoryOrder(string category) => _categoryOrder.TryGetValue(category, out var i) ? i : int.MaxValue;
        int Order(string key) => key switch
        {
            RecommendedGroup => -2,
            AgainstGroup => -1,
            _ => Array.FindIndex(TweakSections, s => s.Key == key),
        };

        Tweaks.Clear();
        _tweakGroups.Clear();
        var groups = VisibleTweaks()
            .Where(p => !OnlyRecommended || p.Recommended)
            .Where(p => !_onlyChanged || p.Status.HasBackup)
            // DNS servers are one choice, made on Graphics & DNS: not a second list of tweaks here.
            .Where(p => !p.Tweak.Id.StartsWith("network.dns.", StringComparison.Ordinal))
            .Where(Matches)
            .GroupBy(p => p.Recommended ? RecommendedGroup : p.Flagged ? AgainstGroup : TweakSections[SectionIndex(p.Tweak.Category)].Key)
            .OrderBy(g => Order(g.Key));
        foreach (var g in groups)
        {
            var items = g.OrderByDescending(p => p.Impact)
                .ThenBy(p => CategoryOrder(p.Tweak.Category))
                .Select(p => new TweakItemViewModel(p.Status, lang, _services.Engine, this, profiled: p, profile: _usage))
                .ToList();
            foreach (var item in items) Tweaks.Add(item);
            // "12 of 34 on, 3 by this app": the same split as the page summary; "0 by this app" is left out.
            var byApp = items.Count(i => i.IsOn && i.HasBackup);
            var summary = Loc.Instance.Format("Tweaks_GroupCount", items.Count, items.Count(i => i.IsOn))
                + (byApp > 0 ? ", " + Loc.Instance.Format("Tweaks_CountByApp", byApp) : "");
            _tweakGroups.Add(new TweakGroupRow(g.Key, GroupTitle(g.Key), GroupIcon(g.Key), summary, items)
            {
                IsExpanded = q.Length > 0 || _openGroups.Contains(g.Key),
            });
        }
        ShowTweakRows();
        var visible = VisibleTweaks().ToList();
        // "On" alone would count settings that were already that way as changes of this app. Counts of zero are left out:
        // "202 tweaks: 91 already set" reads, "0 changed by this app, 0 recommended" was noise.
        var parts = new[]
        {
            (Count: visible.Count(p => p.Status.IsOn && p.Status.HasBackup), Key: "Tweaks_CountByApp"),
            (Count: visible.Count(p => p.Status.IsOn && !p.Status.HasBackup), Key: "Tweaks_CountAlreadySet"),
            (Count: visible.Count(p => p.Recommended), Key: "Tweaks_CountRecommended"),
        }.Where(x => x.Count > 0).Select(x => Loc.Instance.Format(x.Key, x.Count)).ToList();
        var total = Loc.Instance.Format("Tweaks_Summary", visible.Count);
        TweaksSummary = parts.Count == 0 ? total : $"{total}: {string.Join(", ", parts)}";
    }

    private static string GroupTitle(string key) => key switch
    {
        RecommendedGroup => Loc.Instance["Tweaks_GroupRecommended"],
        AgainstGroup => Loc.Instance["Tweaks_GroupAgainstOn"],
        "performance" => Loc.Instance["Tweaks_SectionPerformance"],
        "network" => Loc.Instance["Tweaks_SectionNetwork"],
        "background" => Loc.Instance["Tweaks_SectionBackground"],
        "privacy" => Loc.Instance["Tweaks_SectionPrivacy"],
        "security" => Loc.Instance["Tweaks_SectionSecurity"],
        "desktop" => Loc.Instance["Tweaks_SectionDesktop"],
        _ => Loc.Instance["Tweaks_SectionBrowser"],
    };

    private static SymbolRegular GroupIcon(string key) => key switch
    {
        RecommendedGroup => SymbolRegular.ThumbLike24,
        AgainstGroup => SymbolRegular.Warning24,
        _ => TweakSections.First(s => s.Key == key).Icon,
    };

    private void ShowTweakRows()
    {
        TweakRows.Clear();
        foreach (var g in _tweakGroups)
        {
            TweakRows.Add(g);
            if (g.IsExpanded)
                foreach (var t in g.Items) TweakRows.Add(t);
        }
    }

    /// <summary>
    /// Opens or closes one group; only its rows are added or removed, so the list keeps its scroll position. Takes any
    /// row for the same reason as <see cref="Select"/>.
    /// </summary>
    [RelayCommand]
    private void ToggleTweakGroup(object? row)
    {
        if (row is not TweakGroupRow group) return;
        group.IsExpanded = !group.IsExpanded;
        if (group.IsExpanded) _openGroups.Add(group.Key);
        else _openGroups.Remove(group.Key);
        var at = TweakRows.IndexOf(group) + 1;
        if (at == 0) return;
        if (group.IsExpanded)
            for (var i = 0; i < group.Items.Count; i++) TweakRows.Insert(at + i, group.Items[i]);
        else
            for (var i = 0; i < group.Items.Count; i++) TweakRows.RemoveAt(at);
    }

    /// <summary>
    /// Shows a file selected in Explorer, or opens a folder (Settings, Diagnostics). Explorer runs as the signed-in user;
    /// nothing in the folder is started.
    /// </summary>
    [RelayCommand]
    private void ShowInExplorer(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        var explorer = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe");
        if (System.IO.File.Exists(path)) DeElevatedLauncher.Open(explorer, $"/select,\"{path}\"", _services.Elevation);
        else if (System.IO.Directory.Exists(path)) DeElevatedLauncher.Open(explorer, $"\"{path}\"", _services.Elevation);
    }

    [RelayCommand]
    private void DismissIntro()
    {
        _settings.IntroSeen = true;
        _settings.Save();
        BuildBanners();
    }

    /// <summary>From a page that needs a setting (Expert mode, the VirusTotal key) to Settings.</summary>
    [RelayCommand]
    private void OpenSettings() => CurrentPage = Page.Settings;

    /// <summary>Turns on the safe tweaks of one group after one confirmation that lists every change.</summary>
    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ApplyTweakGroupAsync(object? row)
    {
        if (row is not TweakGroupRow { CanApplyAll: true } group) return;
        await Runner.ApplyBatchAsync(group.Applicable.Select(i => i.Tweak).ToList(), Loc.Instance.Format("Tweaks_GroupApplyTitle", group.Title),
            Loc.Instance["Tweaks_GroupApplyIntro"]);
    }

    [RelayCommand]
    private void OpenAllTweakGroups() => SetAllTweakGroups(true);

    [RelayCommand]
    private void CloseAllTweakGroups() => SetAllTweakGroups(false);

    private void SetAllTweakGroups(bool open)
    {
        foreach (var g in _tweakGroups)
        {
            g.IsExpanded = open;
            if (open) _openGroups.Add(g.Key);
            else _openGroups.Remove(g.Key);
        }
        ShowTweakRows();
    }

    /// <summary>Developer switch --category: opens that group on the Tweaks page (a section key or a catalog category).</summary>
    public void OpenTweakGroup(string key)
    {
        if (_tweakGroups.All(g => g.Key != key)) key = TweakSections[SectionIndex(key)].Key;
        if (_tweakGroups.FirstOrDefault(g => g.Key == key) is { IsExpanded: false } group) ToggleTweakGroup(group);
    }

    /// <summary>Rows for catalog tweaks shown on other pages (GPU settings, DNS), with the same Expert filter as the Tweaks page.</summary>
    public IEnumerable<TweakItemViewModel> CatalogItems(Func<TweakDefinition, bool> filter) =>
        VisibleTweaks(profileFilter: false).Where(p => filter(p.Tweak) && p.Status.State != TweakState.NotApplicable)
            .Select(p => new TweakItemViewModel(p.Status, Loc.Instance.Language, _services.Engine, this, profiled: p, profile: _usage)).ToList();

    /// <summary>The button counts the ticked lines of the plan.</summary>
    private void UpdateRecommendedButton() =>
        RecommendedButtonText = Loc.Instance.Format("Rec_Button", Recommendations.Count(r => r.IsSelected));

    private void BuildRecommendations()
    {
        var lang = Loc.Instance.Language;
        string Reason(RecommendedItem i) =>
            i.FixesFinding is { } f ? Loc.Instance.Format("Rec_Fixes", new FindingItemViewModel(f, lang).Title)
            : i.ReasonKey is { } k ? Labels.Current.Get(lang, k) : Loc.Instance["Rec_Matches"];
        string Excluded(RecommendedItem i) =>
            i.Blocked is { } b ? ChangeRunner.BlockText(b, lang)
            : i.Tweak.EffectiveRisk == Risk.Expert ? Labels.Current.Get(lang, "risk.Expert")
            : i.Tweak.AntiCheatSensitive ? Labels.Current.Get(lang, "badge.antiCheat")
            : Labels.Current.Get(lang, $"reversibility.{i.Tweak.Reversibility}");
        Recommendations.Clear();
        foreach (var i in _plan.Items)
            Recommendations.Add(new RecommendationLine(Runner.Title(i.Tweak), Reason(i), i.Impact, i.Tweak, UpdateRecommendedButton));
        RecommendationsExcluded.Clear();
        foreach (var i in _plan.Excluded)
            RecommendationsExcluded.Add(new RecommendationLine(Runner.Title(i.Tweak), Excluded(i), i.Impact, i.Tweak));
        HasRecommendations = _plan.Items.Count > 0;
        // Nothing to apply in one click, but fixes that need something first: say so instead of "nothing to recommend".
        RecommendationsEmptyText = _plan.Excluded.Count > 0 ? Loc.Instance["Rec_EmptyButExcluded"] : Loc.Instance["Rec_Empty"];
        UpdateRecommendedButton();
        RecommendationsIntro = Loc.Instance.Format("Rec_IntroProfile", ProfileName(_usage));

        // Changes on this PC that lower the profile's goal (for example a battery drain in the Battery profile).
        AgainstProfile.Clear();
        foreach (var p in _profiled.Where(p => p.Flagged).OrderBy(p => p.Impact))
            AgainstProfile.Add(new AgainstLine(p.Tweak.Id, Runner.Title(p.Tweak), p.ReasonKey is { } k ? Labels.Current.Get(lang, k) : "", p.Status.HasBackup));
        HasAgainst = AgainstProfile.Count > 0;
        AgainstTitle = Loc.Instance.Format("Rec_AgainstTitle", ProfileName(_usage));
    }

    /// <summary>Opens a tweak in the details pane on the Tweaks page, as the search does.</summary>
    [RelayCommand]
    private void ShowTweak(string? tweakId)
    {
        if (Tweaks.FirstOrDefault(t => t.Tweak.Id == tweakId) is not { } tweak) return;
        TweakViewMode = TweakView.Applicable;
        TweakSearch = tweak.Title;
        CurrentPage = Page.Tweaks;
        SelectedItem = tweak;
    }

    public void BuildChanges()
    {
        var lang = Loc.Instance.Language;
        ChangeRecords.Clear();
        foreach (var b in _backups.OrderByDescending(b => b.LastApplied))
        {
            var t = _services.Catalog.Get(b.TweakId) ?? _findings.Select(f => f.Fix).FirstOrDefault(f => f?.Id == b.TweakId) ?? _backupTweaks.GetValueOrDefault(b.TweakId);
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
        RestorePointIcon = sr switch { true => SymbolRegular.ShieldCheckmark24, false => SymbolRegular.ShieldDismiss24, _ => SymbolRegular.Shield24 };

        ChangeLog.Clear();
        foreach (var e in Log.Newest(e => e.Source == "change", 200))
            ChangeLog.Add(new LogLine(e.Time.ToString("HH:mm:ss"), ChangeLogText.Text(e, Loc.Instance.Language)));
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
        // First start: how the app works, until the user dismisses it.
        if (!_settings.IntroSeen)
            Banners.Add(new Banner(Loc.Instance["Intro_Text"], false) { ActionText = Loc.Instance["Intro_Dismiss"], Action = DismissIntroCommand, ActionIcon = SymbolRegular.Checkmark20 });
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
            Banners.Add(CanUpdateInApp
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
}
