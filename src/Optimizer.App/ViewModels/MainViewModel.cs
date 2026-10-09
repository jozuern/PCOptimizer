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
using Optimizer.Core.Tweaks;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

public enum Page { Overview, Tweaks, Advisor, Network, Debloat, Cleanup, Startup, Services, Apps, Tools, Health, Changes, Hardware, Settings }

/// <summary>A notice at the top of the page (Fluent InfoBar).</summary>
public sealed record Banner(string Text, bool IsWarning)
{
    public InfoBarSeverity Severity => IsWarning ? InfoBarSeverity.Warning : InfoBarSeverity.Informational;
}

public sealed record SummaryItem(string Label, string Value);

/// <summary>Status drives the color of the number (problems in caution color, passed in success color).</summary>
public sealed record CountItem(string Value, string Label, string Status);

public sealed record HwSection(string Title, IReadOnlyList<SummaryItem> Items);

public sealed record CategoryItem(string Key, string Text, int Count)
{
    public string Display => $"{Text} ({Count})";
}

public sealed record ChangeRecordItem(string TweakId, string Title, string When, string Details, string StateText, string Status, bool CanUndo);

public sealed record LogLine(string Time, string Text);

/// <summary>One line of the "Apply recommended" plan: what, why, and its impact.</summary>
public sealed record RecommendationLine(string Title, string Reason, string ImpactText, bool IsFix);

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
    private Facts _facts = new();
    private DateTime? _lastScanTime;

    public MainViewModel(AppSettings settings, AppServices services, IDialogs dialogs)
    {
        _settings = settings;
        _services = services;
        _dialogs = dialogs;
        _expertMode = settings.ExpertMode;
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
        Health = new HealthViewModel(this, services, dialogs);
        _virusTotalConfigured = !string.IsNullOrEmpty(settings.VirusTotalKey);
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
    [ObservableProperty] private string _restorePointText = "";
    [ObservableProperty] private bool _canEnableRestorePoints;
    [ObservableProperty] private string _tweaksSummary = "";
    [ObservableProperty] private string? _resultText;
    [ObservableProperty] private bool _resultOpen;
    [ObservableProperty] private string _recommendedButtonText = "";
    [ObservableProperty] private bool _hasRecommendations;
    [ObservableProperty] private bool _virusTotalConfigured;

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

    public void ShowResult(string text)
    {
        ResultText = text;
        ResultOpen = !string.IsNullOrWhiteSpace(text);
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
        _plan = Optimizer.Core.Tweaks.Recommendations.Build(_tweakStates, _findings);
    }

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

        Fill(Findings, _findings.Where(f => f.Kind == FindingKind.Finding), lang);
        Fill(AdvisorItems, _findings.Where(f => f.Kind == FindingKind.Advisor), lang);
        OnPropertyChanged(nameof(FindingsEmpty));
        OnPropertyChanged(nameof(AdvisorEmpty));
        var access = _findings.FirstOrDefault(f => f.Kind == FindingKind.GameAccess);
        GameAccess = access is null ? null : new FindingItemViewModel(access, lang);

        Score = ReadinessScore.Compute(_findings);
        var problems = _findings.Count(f => f.IsProblem && f.Kind != FindingKind.GameAccess);
        ProblemSummary = problems == 0 ? Loc.Instance["Dash_NoProblems"] : Loc.Instance.Format("Dash_Problems", problems);
        ScoreVerdict = Loc.Instance[Score >= 85 ? "Verdict_Good" : Score >= 60 ? "Verdict_Fair" : "Verdict_Poor"];
        Counts.Clear();
        var findingProblems = _findings.Count(f => f.IsProblem && f.Kind == FindingKind.Finding);
        var advisorProblems = _findings.Count(f => f.IsProblem && f.Kind == FindingKind.Advisor);
        Counts.Add(new CountItem(findingProblems.ToString(), Loc.Instance["Count_Problems"], findingProblems > 0 ? "Problem" : "Ok"));
        Counts.Add(new CountItem(advisorProblems.ToString(), Loc.Instance["Count_Advisor"], advisorProblems > 0 ? "Problem" : "Ok"));
        Counts.Add(new CountItem(_findings.Count(f => f.Status == FindingStatus.Ok).ToString(), Loc.Instance["Count_Passed"], "Ok"));
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
            target.Add(new FindingItemViewModel(f, lang, FixFor(f)));
    }

    /// <summary>Runtime fix of the finding, or the first catalog tweak that fixes it and is not on yet.</summary>
    private TweakDefinition? FixFor(Finding f) =>
        f.Fix ?? _tweakStates.FirstOrDefault(s => s.Tweak.Fixes.Contains(f.Id) && !s.IsOn && s.State != TweakState.NotApplicable
                                                  && (ExpertMode || s.Tweak.EffectiveRisk != Risk.Expert))?.Tweak;

    private IEnumerable<TweakStatus> VisibleTweaks() =>
        _tweakStates.Where(s => ExpertMode || s.Tweak.EffectiveRisk != Risk.Expert || s.HasBackup)
                    .Where(s => s.State != TweakState.NotApplicable || ShowPassed);

    private void BuildCategories()
    {
        var lang = Loc.Instance.Language;
        var visible = VisibleTweaks().ToList();
        var selected = SelectedCategory?.Key ?? "";
        Categories.Clear();
        Categories.Add(new CategoryItem("", Loc.Instance["Tweaks_All"], visible.Count));
        foreach (var g in visible.GroupBy(s => s.Tweak.Category).OrderBy(g => g.Key, StringComparer.Ordinal))
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
            .Where(s => category.Length == 0 || s.Tweak.Category == category)
            .Where(s => !OnlyRecommended || s.Recommended)
            .OrderByDescending(s => s.Recommended)
            .ThenByDescending(s => s.Impact)
            .ThenBy(s => s.Tweak.Category, StringComparer.Ordinal)
            .ToList();
        foreach (var s in list) Tweaks.Add(new TweakItemViewModel(s, lang, _services.Engine, this));
        var visible = VisibleTweaks().ToList();
        TweaksSummary = Loc.Instance.Format("Tweaks_Summary", visible.Count, visible.Count(s => s.IsOn), visible.Count(s => s.Recommended));
    }

    /// <summary>Rows for catalog tweaks shown on other pages (GPU settings, DNS), with the same Expert filter as the Tweaks page.</summary>
    public IEnumerable<TweakItemViewModel> CatalogItems(Func<TweakDefinition, bool> filter) =>
        VisibleTweaks().Where(s => filter(s.Tweak) && s.State != TweakState.NotApplicable)
            .Select(s => new TweakItemViewModel(s, Loc.Instance.Language, _services.Engine, this)).ToList();

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
            ChangeRecords.Add(new ChangeRecordItem(
                b.TweakId,
                title,
                b.LastApplied.LocalDateTime.ToString("g"),
                Loc.Instance.Format("Changes_Entries", b.Entries.Count),
                state is null ? "" : Labels.Current.Get(lang, $"state.{state}"),
                state is TweakState.RevertedByWindows ? "Problem" : "Ok",
                t is not null));
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

    private void BuildBanners()
    {
        Banners.Clear();
        if (Profile is null) return;
        var drift = _tweakStates.Count(s => s.State == TweakState.RevertedByWindows);
        if (drift > 0) Banners.Add(new Banner(Loc.Instance.Format("Banner_Drift", drift), true));
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
