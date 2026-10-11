using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Backup;
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
        if (build(!on) is { } opposite && services.Store.Exists(opposite.Id)) return await runner.UndoAsync(opposite);
        return build(on) is { } t && await runner.ApplyAsync(t);
    }
}

// ---------------- Startup ----------------

public sealed partial class StartupRow : SwitchRow
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;

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

    /// <summary>Signed by Microsoft does not mean harmless here: the command line decides what the script host runs.</summary>
    public string? ScriptHostNote => Entry.RunsScriptHost ? Loc.Instance["Startup_ScriptHost"] : null;

    /// <summary>Hidden by "Hide Microsoft entries" only when signed by Microsoft and nothing else needs a look.</summary>
    public bool IsPlainMicrosoft => IsMicrosoft && !Entry.Suspicious && !Entry.RunsScriptHost;

    [ObservableProperty, NotifyPropertyChangedFor(nameof(MetaText))] private string _publisherText = "";
    [ObservableProperty, NotifyPropertyChangedFor(nameof(MetaText))] private string _signatureText = "";
    [ObservableProperty] private bool _isMicrosoft;

    /// <summary>Kind, publisher and signature; parts not known yet (signatures load after the list) are left out.</summary>
    public string MetaText => string.Join(", ", new[] { KindText, PublisherText, SignatureText }.Where(x => x.Length > 0));
    [ObservableProperty] private bool _needsAttention;
    [ObservableProperty] private string? _virusTotalText;
    [ObservableProperty] private string? _virusTotalUrl;

    /// <summary>Added or changed since the saved snapshot (set after a comparison).</summary>
    [ObservableProperty] private bool _isNew;

    public void SetSignature(SignatureInfo s, string lang)
    {
        IsMicrosoft = s.IsMicrosoft;
        PublisherText = s.Publisher ?? Loc.Instance["Startup_NoPublisher"];
        SignatureText = Labels.Current.Get(lang, $"signature.{s.Status}");
        NeedsAttention = Entry.Suspicious || Entry.RunsScriptHost || s.Status is SignatureStatus.Unsigned or SignatureStatus.Invalid;
    }

    protected override Task<bool> ToggleAsync(bool on) => Switching.SetAsync(_services, _runner, enabled => StartupTweaks.Set(Entry, enabled), on);
}

public sealed record FilterOption(string Key, string Text)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Text;
}

public sealed partial class StartupViewModel(MainViewModel owner, AppServices services, ChangeRunner runner) : PageViewModel(owner)
{
    private List<StartupRow> _all = [];
    private CancellationTokenSource? _vtCancel;

    public ObservableCollection<StartupRow> Rows { get; } = [];
    public ObservableCollection<FilterOption> Kinds { get; } = [];
    private string? _kindsLang;

    [ObservableProperty] private bool _hideMicrosoft = true;
    [ObservableProperty] private FilterOption? _selectedKind;
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private string _virusTotalStatus = "";
    [ObservableProperty] private bool _isCheckingVirusTotal;
    [ObservableProperty] private string _snapshotText = "";
    [ObservableProperty] private bool _hasComparison;
    [ObservableProperty] private bool _onlyNew;
    [ObservableProperty] private string _search = "";
    private bool _compareAfterLoad;

    partial void OnSearchChanged(string value) => Filter();
    partial void OnOnlyNewChanged(bool value) => Filter();
    partial void OnHideMicrosoftChanged(bool value) => Filter();
    partial void OnSelectedKindChanged(FilterOption? value) => Filter();

    private IReadOnlyList<StartupEntry>? _entries;

    protected override async Task LoadAsync()
    {
        var scanner = new StartupScanner(services.Context.Registry, services.Context.Tasks, services.ProfilePath);
        _entries = await Task.Run(() => scanner.ScanAll());
        await ShowAsync(_entries);
    }

    // A language switch builds the rows from the entries already read; the signatures come from the verifier's cache.
    protected override Task RelabelAsync() => _entries is { } entries ? ShowAsync(entries) : LoadAsync();

    private async Task ShowAsync(IReadOnlyList<StartupEntry> entries)
    {
        var lang = Lang;
        _all = entries.Select(e => new StartupRow(e, lang, services, runner)).ToList();
        if (_compareAfterLoad && await Task.Run(() => StartupSnapshot.Load(StartupSnapshot.DefaultFile)) is { } snapshot)
            ShowComparison(StartupSnapshot.Compare(snapshot, entries));
        // Built again after a language switch; the chosen kind stays selected.
        if (Kinds.Count == 0 || _kindsLang != lang)
        {
            // First view: the programs that start at sign-in (what Task Manager shows); every location is one choice away.
            var selectedKey = SelectedKind?.Key ?? SignInKey;
            _kindsLang = lang;
            Kinds.Clear();
            Kinds.Add(new FilterOption(SignInKey, Loc.Instance["Startup_SignInKinds"]));
            Kinds.Add(new FilterOption("", Loc.Instance["Startup_AllKinds"]));
            foreach (var k in Enum.GetValues<StartupKind>())
                Kinds.Add(new FilterOption(k.ToString(), Labels.Current.Get(lang, $"startupKind.{k}")));
            // Set the field directly: Filter() runs right after.
#pragma warning disable MVVMTK0034
            _selectedKind = Kinds.FirstOrDefault(k => k.Key == selectedKey) ?? Kinds[0];
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(SelectedKind));
        }
        Filter();
        // Signatures in the background, shown together: Microsoft entries are hidden once verified.
        var rows = _all.ToList();
        var signatures = await Task.Run(() => rows.AsParallel().AsOrdered().WithDegreeOfParallelism(4)
            .Select(row => SignatureVerifier.Verify(row.Entry.ImagePath)).ToList());
        for (var i = 0; i < rows.Count; i++) rows[i].SetSignature(signatures[i], lang);
        Filter();
    }

    private const string SignInKey = "signIn";

    private static bool IsSignIn(StartupEntry e) => e.Kind is StartupKind.RunKey or StartupKind.StartupFolder or StartupKind.LogonTask;

    private void Filter()
    {
        var kind = SelectedKind?.Key ?? SignInKey;
        var q = Search.Trim();
        bool Matches(StartupRow r) => q.Length == 0 || r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase)
            || r.Command?.Contains(q, StringComparison.OrdinalIgnoreCase) == true || r.Location.Contains(q, StringComparison.OrdinalIgnoreCase)
            || r.MetaText.Contains(q, StringComparison.CurrentCultureIgnoreCase);
        Rows.Clear();
        foreach (var r in _all.Where(r => (kind.Length == 0 || (kind == SignInKey ? IsSignIn(r.Entry) : r.Entry.Kind.ToString() == kind)) && !(HideMicrosoft && r.IsPlainMicrosoft) && (!OnlyNew || r.IsNew) && Matches(r))
                     .OrderByDescending(r => r.IsNew).ThenByDescending(r => r.NeedsAttention).ThenBy(r => r.Entry.Kind).ThenBy(r => r.Name, StringComparer.CurrentCultureIgnoreCase))
            Rows.Add(r);
        var atLogon = _all.Count(r => StartupTweaks.CountsForF16(r.Entry) && !r.IsPlainMicrosoft);
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
                string hash;
                try
                {
                    var userSid = Owner.Services.UserSid;
                    if (!await Task.Run(() => VirusTotalClient.UserCanRead(g.Key, userSid))) continue;
                    hash = await Task.Run(() => VirusTotalClient.Sha256(g.Key));
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue; // locked or removed meanwhile: the other files are still checked
                }
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
        catch (OperationCanceledException) when (_vtCancel?.IsCancellationRequested == true)
        {
            VirusTotalStatus = Loc.Instance["Vt_Stopped"];
        }
        catch (OperationCanceledException ex)
        {
            // An HTTP timeout, not the Stop button.
            VirusTotalStatus = Loc.Instance.Format("Result_Error", ex.Message);
        }
        finally
        {
            IsCheckingVirusTotal = false;
            _vtCancel?.Dispose();
            _vtCancel = null;
        }
    }

    [RelayCommand]
    private void StopVirusTotal() => _vtCancel?.Cancel();

    /// <summary>Saves the current list, so a later comparison shows what an installer or update added.</summary>
    [RelayCommand]
    private async Task SaveSnapshotAsync()
    {
        // While the list loads it is empty or partial: saving it would replace a good snapshot.
        if (IsLoading || _all.Count == 0)
        {
            SnapshotText = Loc.Instance["Startup_SnapshotWait"];
            return;
        }
        var entries = _all.Select(r => r.Entry).ToList();
        try
        {
            await Task.Run(() => StartupSnapshot.Save(StartupSnapshot.DefaultFile, StartupSnapshot.Take(entries, DateTimeOffset.Now)));
            SnapshotText = Loc.Instance.Format("Startup_SnapshotSaved", entries.Count);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SnapshotText = Loc.Instance.Format("Result_Error", ex.Message);
            return;
        }
        _compareAfterLoad = false;
        HasComparison = false;
        OnlyNew = false;
        foreach (var r in _all) r.IsNew = false;
        Filter();
    }

    [RelayCommand]
    private async Task CompareSnapshotAsync()
    {
        if (IsLoading || _all.Count == 0)
        {
            SnapshotText = Loc.Instance["Startup_SnapshotWait"];
            return;
        }
        if (await Task.Run(() => StartupSnapshot.Load(StartupSnapshot.DefaultFile)) is not { } snapshot)
        {
            SnapshotText = Loc.Instance["Startup_NoSnapshot"];
            return;
        }
        _compareAfterLoad = true;
        ShowComparison(StartupSnapshot.Compare(snapshot, _all.Select(r => r.Entry)));
        Filter();
    }

    private void ShowComparison(SnapshotDiff diff)
    {
        foreach (var r in _all) r.IsNew = diff.NewKeys.Contains(r.Entry.Key);
        var text = Loc.Instance.Format("Startup_SnapshotDiff", diff.TakenAt.LocalDateTime.ToString("g"), diff.NewKeys.Count, diff.Removed.Count);
        if (diff.Removed.Count > 0)
            text += "\n" + Loc.Instance.Format("Startup_SnapshotRemoved", string.Join(", ", diff.Removed.Take(10).Select(e => e.Name)) + (diff.Removed.Count > 10 ? $" (+{diff.Removed.Count - 10})" : ""));
        SnapshotText = text;
        HasComparison = true;
    }

    /// <summary>Selects the entry's file in File Explorer (started as the signed-in user, never elevated).</summary>
    [RelayCommand]
    private void ShowFile(StartupRow? row)
    {
        if (row?.Entry.ImagePath is not { } path || !File.Exists(path))
        {
            Owner.ShowResult(Loc.Instance["Startup_FileMissing"], Wpf.Ui.Controls.InfoBarSeverity.Warning);
            return;
        }
        DeElevatedLauncher.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), $"/select,\"{path}\"", services.Elevation);
    }

    [RelayCommand]
    private void CopyLocation(StartupRow? row)
    {
        if (row is null) return;
        try
        {
            System.Windows.Clipboard.SetText(row.Entry.Location);
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // The clipboard is held by another program; nothing to do.
        }
    }

    [RelayCommand]
    private void OpenReport(string? url)
    {
        if (url is not null) Owner.OpenLinkCommand.Execute(url);
    }
}

// ---------------- Services ----------------

public sealed record StartOption(ServiceStart Start, string Text)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Text;
}

public sealed partial class ServiceRowVm : ObservableObject
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => DisplayName;

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
            // Only when there is something to choose: a service already on Manual has no change to warn about.
            ServiceEdit.ManualOnly => row.Note?.Mode == "warn" && Options.Count > 1 ? Loc.Instance["Svc_Warn"] : null,
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
        ChangeAsync(oldValue, newValue).Forget("service start type");
    }

    private async Task ChangeAsync(StartOption? oldValue, StartOption newValue)
    {
        var written = false;
        try
        {
            written = await _page.ChangeStartAsync(this, newValue.Start);
        }
        finally
        {
            // Cancelled or failed (also by an exception): show the real start type again, not one that was never written.
            if (!written)
            {
                _suppress = true;
                Selected = oldValue;
                _suppress = false;
            }
        }
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
    // The task list is kept: switching the filter shows or hides rows without reading every service and task again.
    partial void OnShowMicrosoftTasksChanged(bool value) => FillTasks();

    private IReadOnlyList<ScheduledTaskInfo> _taskList = [];

    private IReadOnlyList<ServiceRow>? _serviceRows;

    protected override async Task LoadAsync()
    {
        _serviceRows = await Task.Run(() =>
        {
            var list = ServiceManager.List(services.Context.Services, CatalogData.Current.Services);
            // Signature decides "Microsoft" for services the catalog does not explain.
            return list.AsParallel().WithDegreeOfParallelism(4).Select(r => r.Note is null ? r with { Signature = SignatureVerifier.Verify(r.File) } : r).ToList();
        });
        ShowServices(_serviceRows);

        _taskList = await Task.Run(() => services.Context.Tasks.List());
        FillTasks();
    }

    // A language switch builds the rows from the services and tasks already read.
    protected override Task RelabelAsync()
    {
        if (_serviceRows is null) return LoadAsync();
        ShowServices(_serviceRows);
        FillTasks();
        return Task.CompletedTask;
    }

    private void ShowServices(IReadOnlyList<ServiceRow> rows)
    {
        var lang = Lang;
        _all = rows.OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(r => new ServiceRowVm(r, lang, this)).ToList();
        Filter();
    }

    private void FillTasks()
    {
        var lang = Lang;
        var showMs = ShowMicrosoftTasks;
        Tasks.Clear();
        foreach (var t in _taskList.Where(t => showMs || !t.Path.StartsWith(@"\Microsoft\", StringComparison.OrdinalIgnoreCase)).OrderBy(t => t.Path, StringComparer.OrdinalIgnoreCase))
        {
            // Full command with arguments: rundll32 resolves to its DLL, and script hosts are judged by what they run.
            var command = t.Command is null ? null : $"\"{t.Command}\" {t.Arguments}".Trim();
            var entry = new StartupEntry(StartupKind.LogonTask, t.Path.TrimStart('\\'), command, CommandLine.ImagePath(command),
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
        var id = ServiceManager.ChangeId(ServiceManager.StartTypeOwner(row.Name));
        TweakBackup? previousBackup;
        try
        {
            previousBackup = services.Store.Get(id);
        }
        catch (BackupUnreadableException ex)
        {
            // Locked by another process right now: deciding without it could record a changed value as the original.
            Owner.ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
            return false;
        }
        if (previousBackup is { } previous && previous.Entries.FirstOrDefault()?.Original.Data == start.ToString()
            && Owner.ResolveTweak(id) is { } t)
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
}

// ---------------- Apps & drivers ----------------

public sealed partial class AppRow(AppEntry app, string lang, bool installed) : ObservableObject
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;

    public AppEntry App { get; } = app;
    public string Name => App.Name;
    public string Text { get; } = app.Text(lang);
    public string Category { get; } = Labels.Current.Get(lang, $"appCategory.{app.Category}");
    public bool PerUser => App.Scope == "user";

    /// <summary>Category heading above the first app of each category (the catalog lists them grouped).</summary>
    public string? GroupHeader { get; init; }

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

public sealed record DriverItem(string Device, string ClassText, string Provider, string Version, string Date, string Age, string? UpdateUrl, bool Old)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Device;

    /// <summary>Class, provider and version; a driver without provider or version shows no empty part.</summary>
    public string MetaText => string.Join(", ", new[] { ClassText, $"{Provider} {Version}".Trim() }.Where(x => x.Length > 0));

    public string DateText => string.Join(", ", new[] { Date, Age }.Where(x => x.Length > 0));
}

/// <summary>A desktop program in the uninstall list.</summary>
public sealed partial class ProgramRow(DesktopProgram program) : ObservableObject
{
    public DesktopProgram Program { get; } = program;
    public string Name => Program.Name;

    public string Meta { get; } = string.Join(", ", new[]
    {
        program.Publisher, program.Version,
        program.SizeKb is { } kb ? CleanupViewModel.Size(kb * 1024) : null,
        program.InstallDate?.ToString("d"),
    }.Where(s => !string.IsNullOrWhiteSpace(s)));

    [ObservableProperty] private string? _stateText;
    [ObservableProperty] private bool _isRemoving;
}

public sealed partial class AppsViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    private List<ProgramRow> _allPrograms = [];
    private bool _restorePointReady;

    public ObservableCollection<ProgramRow> ProgramRows { get; } = [];

    [ObservableProperty] private string _programSearch = "";
    [ObservableProperty] private string _programsText = "";
    [ObservableProperty] private bool _isUpdatingAll;

    partial void OnProgramSearchChanged(string value) => FilterPrograms();

    private void FilterPrograms()
    {
        ProgramRows.Clear();
        var q = ProgramSearch.Trim();
        foreach (var r in _allPrograms.Where(r => q.Length == 0 || r.Name.Contains(q, StringComparison.CurrentCultureIgnoreCase)
                                                  || (r.Program.Publisher?.Contains(q, StringComparison.CurrentCultureIgnoreCase) ?? false)))
            ProgramRows.Add(r);
        ProgramsText = Loc.Instance.Format("Programs_Count", _allPrograms.Count);
    }

    private async Task LoadProgramsAsync()
    {
        var list = await Task.Run(() => Programs.Read(services.Context.Registry));
        _allPrograms = list.Select(p => new ProgramRow(p)).ToList();
        FilterPrograms();
    }

    /// <summary>Runs the program's own uninstaller (see <see cref="Programs.Command"/> for when it runs elevated).</summary>
    [RelayCommand]
    private async Task UninstallProgramAsync(ProgramRow? row)
    {
        if (row is null || row.IsRemoving) return;
        if (Programs.Command(row.Program) is not { } command)
        {
            row.StateText = Loc.Instance["Programs_NoUninstaller"];
            return;
        }
        var text = Loc.Instance.Format("Programs_ConfirmText", row.Name) + (command.Mode == UninstallMode.AsUser ? "\n\n" + Loc.Instance["Programs_AsUser"] : "");
        if (!dialogs.Ask(Loc.Instance["Programs_ConfirmTitle"], text, Loc.Instance["Programs_Uninstall"])) return;
        row.IsRemoving = true;
        row.StateText = Loc.Instance["Programs_Running"];
        string? state = null;
        try
        {
            await RunWorkAsync($"uninstall {row.Name}", async () =>
            {
                // A restore point before the first uninstall of the session. When none can be made (System Protection
                // off, or Windows' limit of one per day), the user decides whether to go on.
                if (!_restorePointReady)
                {
                    var made = await Task.Run(() => services.RestorePoints.IsEnabled()) == true
                               && await services.RestorePoints.CreateAsync(Loc.Instance["Programs_RestorePointName"]);
                    if (!made && !dialogs.Ask(Loc.Instance["Programs_NoRestorePointTitle"], Loc.Instance["Programs_NoRestorePointText"], Loc.Instance["Programs_Uninstall"]))
                    {
                        state = "";
                        return;
                    }
                    _restorePointReady = true;
                }
                var code = await ProgramUninstaller.RunAsync(command, services.Elevation);
                if (code is null)
                {
                    state = Loc.Instance["Programs_StartedAsUser"];
                    return;
                }
                await LoadProgramsAsync();
                // The list was read again: the row of the program (if it is still listed) is a new object.
                var still = _allPrograms.FirstOrDefault(r => r.Program.RegistryKey == row.Program.RegistryKey);
                if (still is not null)
                {
                    // Many uninstallers start a copy of themselves and end at once (exit code 0): it may still be running.
                    still.StateText = code == 0 ? Loc.Instance["Programs_MaybeStillRunning"] : Loc.Instance.Format("Programs_StillThere", code);
                }
                else
                {
                    ProgramsText = Loc.Instance.Format("Programs_RemovedName", row.Name);
                }
            });
        }
        finally
        {
            row.IsRemoving = false;
            if (state is not null) row.StateText = state;
            else if (row.StateText == Loc.Instance["Programs_Running"]) row.StateText = "";
        }
    }

    /// <summary>winget upgrade --all: every package winget knows an update for, from the winget source.</summary>
    [RelayCommand]
    private async Task UpdateAllAsync()
    {
        if (_winget is null)
        {
            Output = Loc.Instance["Apps_NoWinget"];
            return;
        }
        if (!dialogs.Ask(Loc.Instance["Apps_UpdateAllTitle"], Loc.Instance["Apps_UpdateAllText"], Loc.Instance["Apps_UpdateAll"])) return;
        IsUpdatingAll = true;
        var lines = new List<string>();
        var progress = new Progress<string>(l =>
        {
            lines.Add(l);
            if (lines.Count > 40) lines.RemoveAt(0);
            Output = string.Join("\n", lines);
        });
        try
        {
            var winget = _winget;
            await RunWorkAsync("winget upgrade all", async () =>
            {
                var code = await StreamingProcess.RunAsync(winget, Winget.UpgradeAllArguments, progress);
                lines.Add(Winget.IsSuccess(code) ? Loc.Instance["Apps_UpdateAllDone"] : Loc.Instance.Format("Apps_Failed", $"0x{code:X8}"));
                Output = string.Join("\n", lines);
            });
        }
        finally
        {
            IsUpdatingAll = false;
        }
    }

    // Elevated installs only use winget from the protected package folder; per-user installs run as the user and may use the alias.
    private string? _winget;
    private string? _wingetForUser;

    public ObservableCollection<AppRow> Apps { get; } = [];
    public ObservableCollection<DriverItem> Drivers { get; } = [];

    [ObservableProperty] private string _output = "";
    [ObservableProperty] private bool _wingetMissing;

    private IReadOnlyList<DriverRow>? _drivers;

    protected override async Task LoadAsync()
    {
        (_winget, _wingetForUser) = await Task.Run(() => (Winget.FindTrusted(), Winget.FindForUser(services.ProfilePath)));
        WingetMissing = _winget is null && _wingetForUser is null;
        ShowApps();
        await LoadProgramsAsync();
        _drivers = await Task.Run(DriverInventory.Read);
        ShowDrivers(_drivers);
    }

    // A language switch builds the rows from the programs and drivers already read.
    protected override Task RelabelAsync()
    {
        if (_drivers is null) return LoadAsync();
        ShowApps();
        FilterPrograms();
        ShowDrivers(_drivers);
        return Task.CompletedTask;
    }

    private void ShowApps()
    {
        var lang = Lang;
        var programs = Owner.Profile?.Extras?.Programs ?? [];
        Apps.Clear();
        string? last = null;
        foreach (var a in CatalogData.Current.Apps.Apps)
        {
            Apps.Add(new AppRow(a, lang, a.IsInstalled(programs)) { GroupHeader = a.Category == last ? null : Labels.Current.Get(lang, $"appCategory.{a.Category}") });
            last = a.Category;
        }
    }

    private void ShowDrivers(IReadOnlyList<DriverRow> drivers)
    {
        var vendor = CatalogData.Current.Bios.NormalizeVendor(Owner.Profile?.Firmware?.BoardManufacturer);
        var board = BiosAgeCheck.SupportUrl(vendor);
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
        if (row is null) return;
        if (row.PerUser)
        {
            if (_wingetForUser is null) return;
            var path = Winget.InstallForUser(_wingetForUser, row.App, services.Elevation);
            row.StateText = path == DeElevatedLauncher.Path.Failed ? Loc.Instance.Format("Result_Error", "winget") : Loc.Instance["Apps_StartedForUser"];
            return;
        }
        if (_winget is null)
        {
            row.StateText = Loc.Instance["Apps_NoWinget"];
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
            var winget = _winget;
            if (!await RunWorkAsync($"install {row.App.Id}", async () =>
                {
                    var code = await Winget.InstallAsync(winget, row.App, progress, CancellationToken.None);
                    row.Installed = Winget.IsSuccess(code);
                    row.StateText = row.Installed ? Loc.Instance["Apps_Installed"] : Loc.Instance.Format("Apps_Failed", $"0x{code:X8}");
                }))
                row.StateText = "";
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
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Path;

    public string Path { get; } = path;
    public string SizeText { get; } = CleanupViewModel.Size(bytes);
    public string Meta { get; } = meta;
    public bool CanDelete { get; } = canDelete;

    [ObservableProperty] private bool _selected;
}

public sealed record FolderRow(string Path, string SizeText, string Files)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Path;
}

public sealed partial class FeatureRow : SwitchRow
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Title;

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

    protected override Task<bool> ToggleAsync(bool on) => Switching.SetAsync(_services, _runner,
        enabled => Entry.Capability ? OptionalCapabilityAction.Tweak(Entry, enabled) : OptionalFeatureAction.Tweak(Entry, enabled), on);
}

public sealed record StepRow(string Text, bool Ok, string? Detail)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Text;
}

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

    public ObservableCollection<PriorityRow> PriorityRules { get; } = [];
    [ObservableProperty] private IReadOnlyList<FilterOption> _priorityChoices = BuildPriorityChoices();

    private static IReadOnlyList<FilterOption> BuildPriorityChoices() =>
    [
        new(nameof(CpuPriority.AboveNormal), Loc.Instance["Prio_AboveNormal"]),
        new(nameof(CpuPriority.High), Loc.Instance["Prio_High"]),
        new(nameof(CpuPriority.BelowNormal), Loc.Instance["Prio_BelowNormal"]),
        new(nameof(CpuPriority.Low), Loc.Instance["Prio_Low"]),
    ];

    /// <summary>After a language switch the page loads again: lists with texts are built again in the new language.</summary>
    private void RebuildTexts()
    {
        var key = NewPriority?.Key;
        PriorityChoices = BuildPriorityChoices();
        NewPriority = PriorityChoices.FirstOrDefault(o => o.Key == key) ?? PriorityChoices[0];
        QuickFixRows = QuickFixes.All.Select(f => new QuickFixRow(f)).ToList();
    }

    [ObservableProperty] private string _newPriorityExe = "";
    [ObservableProperty] private FilterOption? _newPriority;
    [ObservableProperty] private bool _newPriorityLowIo;
    [ObservableProperty] private string? _priorityNote;

    /// <summary>Reads the rules in the background: Image File Execution Options has a key per program.</summary>
    private async Task LoadPriorityRulesAsync()
    {
        NewPriority ??= PriorityChoices[0];
        var rules = await Task.Run(() => ProgramPriority.Read(services.Context.Registry));
        PriorityRules.Clear();
        foreach (var r in rules)
            PriorityRules.Add(new PriorityRow(r, r.Cpu is { } c ? PriorityChoices.First(o => o.Key == c.ToString()).Text
                    : r.OtherCpu is { } o ? Loc.Instance.Format("Prio_Other", o) : "",
                r.LowIo ? Loc.Instance["Prio_LowIo"] : null));
    }

    /// <summary>Adds a start priority rule through the engine (confirmation, backup, Changes page).</summary>
    [RelayCommand]
    private async Task AddPriorityRuleAsync()
    {
        var exe = NewPriorityExe.Trim();
        if (!ProgramPriority.IsValidExe(exe))
        {
            PriorityNote = Loc.Instance["Prio_InvalidName"];
            return;
        }
        var cpu = Enum.Parse<CpuPriority>((NewPriority ?? PriorityChoices[0]).Key);
        PriorityNote = null;
        if (await runner.ApplyAsync(ProgramPriority.Tweak(exe, cpu, NewPriorityLowIo))) NewPriorityExe = "";
        await LoadPriorityRulesAsync();
    }

    /// <summary>Undoes a rule this app set; a rule from elsewhere is removed with its own backup.</summary>
    [RelayCommand]
    private async Task RemovePriorityRuleAsync(PriorityRow? row)
    {
        if (row is null) return;
        var ours = ProgramPriority.Tweak(row.Rule.Exe, row.Rule.Cpu ?? CpuPriority.AboveNormal, row.Rule.LowIo);
        if (services.Store.Exists(ours.Id)) await runner.UndoAsync(ours);
        else await runner.ApplyAsync(ProgramPriority.RemoveTweak(row.Rule.Exe));
        await LoadPriorityRulesAsync();
    }

    /// <summary>Repairs that run documented Windows commands (restart a service or device, renew network state, rebuild a cache).</summary>
    [ObservableProperty] private IReadOnlyList<QuickFixRow> _quickFixRows = QuickFixes.All.Select(f => new QuickFixRow(f)).ToList();

    // The page shows the quick fixes in three groups: network, restart a part of Windows, repair Windows.
    private static readonly string[] NetworkFixes = ["IpRenew", "TcpIpReset"];
    private static readonly string[] RestartFixes = ["AudioRestart", "BluetoothRestart", "SearchRestart", "GraphicsRestart"];

    public IEnumerable<QuickFixRow> QuickNetworkRows => QuickFixRows.Where(r => NetworkFixes.Contains(r.Fix.Id));
    public IEnumerable<QuickFixRow> QuickRestartRows => QuickFixRows.Where(r => RestartFixes.Contains(r.Fix.Id));
    public IEnumerable<QuickFixRow> QuickRepairRows => QuickFixRows.Where(r => !NetworkFixes.Contains(r.Fix.Id) && !RestartFixes.Contains(r.Fix.Id));

    partial void OnQuickFixRowsChanged(IReadOnlyList<QuickFixRow> value)
    {
        OnPropertyChanged(nameof(QuickNetworkRows));
        OnPropertyChanged(nameof(QuickRestartRows));
        OnPropertyChanged(nameof(QuickRepairRows));
    }

    protected override async Task LoadAsync()
    {
        // Asking a drive whether it is ready can take seconds (a sleeping disk): off the UI thread.
        var drives = await Task.Run(() => DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed && d.IsReady).Select(d => d.RootDirectory.FullName).ToList());
        Drives.Clear();
        foreach (var d in drives) Drives.Add(d);
        SelectedDrive ??= Drives.FirstOrDefault();

        RebuildTexts();
        await LoadPriorityRulesAsync();
        _featureStates = await Task.Run(() => OptionalFeatureAction.ReadAll(services.Context.Processes)
            .Concat(OptionalCapabilityAction.ReadAll(services.Context.Processes)).ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase));
        ShowFeatures(_featureStates);
    }

    private Dictionary<string, string>? _featureStates;

    // A language switch builds the rows again without DISM (two lists that take seconds); the priority rules are a quick read.
    protected override async Task RelabelAsync()
    {
        if (_featureStates is null)
        {
            await LoadAsync();
            return;
        }
        RebuildTexts();
        ShowFeatures(_featureStates);
        await LoadPriorityRulesAsync();
    }

    private void ShowFeatures(Dictionary<string, string> states)
    {
        var lang = Lang;
        Features.Clear();
        foreach (var f in CatalogData.Current.Features.Features)
            if (states.TryGetValue(f.Name, out var s)) Features.Add(new FeatureRow(f, s == "Enabled", lang, services, runner));
        // DISM needs administrator rights to list features.
        FeaturesNote = Features.Count > 0 ? null : services.Elevation.IsElevated ? Loc.Instance["Feat_None"] : Loc.Instance["Feat_NeedsAdmin"];
    }

    [RelayCommand]
    private async Task ScanStorageAsync()
    {
        // One scan at a time: a second one (Scan pressed again, or the rescan after recycling) would mix its rows in.
        if (SelectedDrive is null || IsScanningStorage) return;
        // Game folders come from the PC scan; without it they could be offered for deletion.
        if (Owner.Profile?.Software is not { } software)
        {
            StorageStatus = Loc.Instance["Storage_WaitForScan"];
            return;
        }
        _scanCancel = new CancellationTokenSource();
        IsScanningStorage = true;
        LargestFiles.Clear();
        LargestFolders.Clear();
        Duplicates.Clear();
        try
        {
            // Read for every analysis: a scan since the page opened may have found a new library.
            _protected = await Task.Run(() => StorageAnalyzer.ProtectedRoots(software.GameLibraryPaths.Concat(software.OtherGameFolders)));
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
            _scanCancel.Dispose();
            _scanCancel = null;
        }
    }

    [RelayCommand]
    private void StopStorage() => _scanCancel?.Cancel();

    [RelayCommand]
    private async Task RecycleSelectedAsync()
    {
        if (IsScanningStorage) return; // the lists are being refilled
        var selected = LargestFiles.Concat(Duplicates).Where(f => f.Selected && f.CanDelete).Select(f => f.Path).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (selected.Count == 0) return;
        var text = Loc.Instance.Format("Storage_RecycleText", selected.Count, string.Join("\n", selected.Take(10)));
        // The Recycle Bin belongs to the account this process runs as. With a separate admin account (or Administrator
        // protection) that is not the signed-in user's bin: say so before deleting.
        if (services.Elevation is { UserMismatch: true } e) text += "\n\n" + Loc.Instance.Format("Storage_RecycleOtherAccount", e.ProcessUser);
        if (!dialogs.Ask(Loc.Instance["Storage_RecycleTitle"], text, Loc.Instance["Storage_Recycle"])) return;
        if (!await RunWorkAsync("recycle files", async () =>
            {
                var failed = await Task.Run(() => StorageAnalyzer.Recycle(selected, _protected));
                Owner.ShowResult(Loc.Instance.Format("Storage_Recycled", selected.Count - failed.Count, failed.Count),
                    failed.Count == 0 ? Wpf.Ui.Controls.InfoBarSeverity.Success : Wpf.Ui.Controls.InfoBarSeverity.Warning);
            }))
            return;
        await ScanStorageAsync();
    }

    [RelayCommand]
    private async Task RepairUpdateAsync()
    {
        if (!dialogs.Ask(Loc.Instance["Repair_Title"], Loc.Instance["Repair_Text"], Loc.Instance["Repair_Run"])) return;
        RepairSteps.Clear();
        var lang = Lang;
        var progress = new Progress<RepairStep>(s => RepairSteps.Add(new StepRow(Labels.Current.Get(lang, s.Key), s.Ok, s.Detail)));
        await RunWorkAsync("windows update repair", async () =>
        {
            var steps = await Task.Run(() => UpdateRepair.Run(services.Context.Processes, services.Context.Services, progress));
            var ok = steps.All(s => s.Ok);
            Owner.ShowResult(ok ? Loc.Instance["Repair_Done"] : Loc.Instance["Repair_Partial"], ok ? Wpf.Ui.Controls.InfoBarSeverity.Success : Wpf.Ui.Controls.InfoBarSeverity.Warning);
        });
    }

    [RelayCommand]
    private Task FlushDnsAsync() => RunWorkAsync("flush dns", async () =>
    {
        var (code, output) = await Task.Run(() => services.Context.Processes.Run("ipconfig.exe", "/flushdns"));
        QuickOutput = code == 0 ? Loc.Instance["Quick_DnsFlushed"] : Loc.Instance.Format("Result_Error", output.Trim());
    });

    [RelayCommand]
    private async Task RestartExplorerAsync()
    {
        if (!dialogs.Ask(Loc.Instance["Quick_ExplorerTitle"], Loc.Instance["Quick_ExplorerText"], Loc.Instance["Quick_Explorer"])) return;
        // Only Explorer of this session: the elevated taskkill would otherwise end it for every signed-in user, and it is
        // started again only for this one.
        int session;
        using (var self = Process.GetCurrentProcess()) session = self.SessionId;
        await RunWorkAsync("restart explorer", async () =>
        {
            await Task.Run(() => services.Context.Processes.Run("taskkill.exe", $"/f /im explorer.exe /fi \"SESSION eq {session}\""));
            await Task.Delay(1000);
            // Explorer must run as the signed-in user, never elevated.
            var path = DeElevatedLauncher.Open(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe"), null, services.Elevation);
            QuickOutput = path == DeElevatedLauncher.Path.Failed ? Loc.Instance["Quick_ExplorerManual"] : Loc.Instance["Quick_ExplorerRestarted"];
        });
    }

    [RelayCommand]
    private async Task RunQuickFixAsync(QuickFixRow row)
    {
        var fix = row.Fix;
        if (fix.Confirm && !dialogs.Ask(row.Title, Loc.Instance[$"Quick_{fix.Id}Confirm"], Loc.Instance["Cleanup_Run"])) return;
        QuickOutput = Loc.Instance.Format("Quick_Running", row.Title);
        if (!await RunWorkAsync($"quick fix {fix.Id}", async () =>
            {
                var (ok, output) = await Task.Run(() => QuickFixes.Run(services.Context.Processes, fix));
                QuickOutput = !ok ? Loc.Instance.Format("Result_Error", output.Trim())
                    : fix.ShowOutput ? Loc.Instance[$"Quick_{fix.Id}Done"] + "\n" + string.Join("\n", output.Trim().Split('\n').TakeLast(8))
                    : Loc.Instance[$"Quick_{fix.Id}Done"];
            }))
            QuickOutput = "";
    }

    [RelayCommand]
    private async Task WinsockResetAsync()
    {
        if (!dialogs.Ask(Loc.Instance["Quick_WinsockTitle"], Loc.Instance["Quick_WinsockText"], Loc.Instance["Quick_Winsock"])) return;
        await RunWorkAsync("winsock reset", async () =>
        {
            var (code, output) = await Task.Run(() => services.Context.Processes.Run("netsh.exe", "winsock reset"));
            QuickOutput = code == 0 ? Loc.Instance["Quick_WinsockDone"] : Loc.Instance.Format("Result_Error", output.Trim());
        });
    }
}

/// <summary>A start priority rule found in Image File Execution Options.</summary>
public sealed record PriorityRow(PriorityRule Rule, string CpuText, string? IoText)
{
    public string Exe => Rule.Exe;
    public string Details => IoText is null ? CpuText : CpuText.Length == 0 ? IoText : $"{CpuText}, {IoText}";
}

/// <summary>A quick fix card; title and hint come from the string resources in the current language.</summary>
public sealed record QuickFixRow(QuickFix Fix)
{
    public string Title => Loc.Instance[$"Quick_{Fix.Id}"];
    public string Hint => Loc.Instance[$"Quick_{Fix.Id}Hint"];
}

// ---------------- Health ----------------

public sealed record DiskRow(string Name, string Health, string Status, string Details)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;
}

public sealed record SensorRow(string Hardware, string Sensor, string Value)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => $"{Sensor}: {Value}";
}

public sealed record RunRow(string Label, string AvgFps, string Low, string Frames)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Label;
}

public sealed partial class HealthViewModel(MainViewModel owner, IDialogs dialogs) : PageViewModel(owner)
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

    protected override async Task LoadAsync()
    {
        BuildDisks();
        await RefreshProcessesAsync();
        CanInstallPawnIo = !Sensors.PawnIoInstalled;
        PawnIoText = Loc.Instance[Sensors.PawnIoInstalled ? "Sensors_PawnIoOn" : "Sensors_PawnIoOff"];
        if (Optimizer.Core.Tools.HealthStore.LoadThrottle(Optimizer.Core.Tools.HealthStore.DefaultFolder) is { } last) ThrottleResult = Describe(last);
    }

    /// <summary>The disk rows carry status brushes: built again for the new theme once the page was opened.</summary>
    public void OnThemeChanged()
    {
        if (Disks.Count > 0) BuildDisks();
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
        var token = _toolCancel.Token;
        IsToolRunning = true;
        var lines = new List<string>();
        var progress = new Progress<string>(l =>
        {
            OutputLines.Add(lines, l);
            ToolOutput = string.Join("\n", lines);
        });
        try
        {
            // SFC and DISM repair system files: no tweak, scan or update at the same time.
            await RunWorkAsync("system file repair", async () =>
            {
                try
                {
                    var code = await run(progress, token);
                    lines.Add(Loc.Instance.Format("Health_ExitCode", code));
                    ToolOutput = string.Join("\n", lines);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    ToolOutput += "\n" + Loc.Instance["Health_Stopped"];
                }
            });
        }
        finally
        {
            IsToolRunning = false;
            _toolCancel.Dispose();
            _toolCancel = null;
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
            var none = Loc.Instance["Throttle_NotReported"];
            var progress = new Progress<ThrottleSample>(s => ThrottleStatus = Loc.Instance.Format("Throttle_Sample", monitor.Samples.Count,
                s.CpuPerformanceLimit is { } l ? $"{l:0} %" : none, s.CpuUtility is { } u ? $"{u:0} %" : none,
                s.Gpus.FirstOrDefault()?.TempC is { } t ? $"{t} °C" : none));
            var result = await monitor.RunAsync(TimeSpan.FromSeconds(ThrottleSeconds), progress, _throttleCancel.Token);
            if (result.Samples >= 10)
            {
                Optimizer.Core.Tools.HealthStore.SaveThrottle(Optimizer.Core.Tools.HealthStore.DefaultFolder, result);
                ThrottleResult = Describe(result);
                // Updates the throttling finding; while a change or an update runs the next scan does it.
                if (Owner.ScanCommand.CanExecute(null)) await Owner.ScanAsync();
            }
            else
            {
                ThrottleResult = Loc.Instance["Throttle_TooShort"];
            }
        }
        catch (Exception ex)
        {
            ThrottleResult = Loc.Instance.Format("Result_Error", ex.Message);
        }
        finally
        {
            IsThrottleRunning = false;
            ThrottleStatus = "";
            _throttleCancel.Dispose();
            _throttleCancel = null;
        }
    }

    [RelayCommand] private void StopThrottle() => _throttleCancel?.Cancel();

    private static string Describe(ThrottleResult r) => Loc.Instance.Format("Throttle_Result",
        r.Measured.LocalDateTime.ToString("g"), r.Samples, $"{r.CpuBusyShare * 100:0}", $"{r.CpuLimitedShare * 100:0}",
        // The limit reasons come from NVML: both or neither are there.
        r.GpuReasonShare.TryGetValue("thermal", out var th) && r.GpuReasonShare.TryGetValue("powerLimit", out var pw)
            ? Loc.Instance.Format("Throttle_Gpu", $"{th * 100:0}", $"{pw * 100:0}")
            : Loc.Instance["Throttle_GpuNone"],
        r.CpuThrottled || r.GpuThermal || r.GpuSlowdown ? Loc.Instance["Throttle_Verdict_Bad"]
            : !r.CpuJudged ? Loc.Instance["Throttle_Verdict_NoLoad"] : Loc.Instance["Throttle_Verdict_Ok"]);

    // Benchmark

    /// <summary>Games found by the scan plus programs with a window; listing processes runs off the UI thread.</summary>
    [RelayCommand]
    private async Task RefreshProcessesAsync()
    {
        var selected = SelectedProcess;
        var games = (Owner.Profile?.Software?.Games ?? []).Select(g => g.Executable).OfType<string>().ToList();
        var names = await Task.Run(() =>
        {
            var set = new SortedSet<string>(games.Select(Path.GetFileName).OfType<string>(), StringComparer.OrdinalIgnoreCase);
            foreach (var p in Process.GetProcesses())
            {
                using (p)
                {
                    try
                    {
                        if (p.MainWindowHandle != IntPtr.Zero && p.Id != Environment.ProcessId) set.Add(p.ProcessName + ".exe");
                    }
                    catch (Exception)
                    {
                        // protected process
                    }
                }
            }
            return set.Where(PresentMon.IsSafeProcessName).ToList();
        });
        Processes.Clear();
        foreach (var n in names) Processes.Add(n);
        SelectedProcess = selected is not null && Processes.Contains(selected) ? selected : Processes.FirstOrDefault();
    }

    [RelayCommand] private Task CaptureBeforeAsync() => CaptureAsync(_before, "A");
    [RelayCommand] private Task CaptureAfterAsync() => CaptureAsync(_after, "B");

    private CancellationTokenSource? _captureCancel;

    [RelayCommand]
    private void StopCapture() => _captureCancel?.Cancel();

    private async Task CaptureAsync(List<FrameStats> set, string label)
    {
        if (SelectedProcess is null) return;
        IsCapturing = true;
        _captureCancel = new CancellationTokenSource();
        try
        {
            var exe = await Task.Run(() => PresentMon.Extract(AppServices.ToolsFolder));
            BenchmarkStatus = Loc.Instance.Format("Bench_Capturing", SelectedProcess, CaptureSeconds);
            var stats = await PresentMon.CaptureAsync(exe, SelectedProcess, CaptureSeconds, Optimizer.Core.Platform.DataPaths.Captures, null, _captureCancel.Token);
            if (stats is null)
            {
                BenchmarkStatus = Loc.Instance["Bench_NoFrames"];
                return;
            }
            set.Add(stats);
            Runs.Add(new RunRow($"{label}{set.Count}", Loc.Instance.Format("Bench_Fps", $"{stats.AverageFps:0.0}"), Loc.Instance.Format("Bench_Low", $"{stats.OnePercentLowFps:0.0}"), stats.Frames.ToString()));
            BenchmarkStatus = Loc.Instance["Bench_Done"];
            Compare();
        }
        catch (OperationCanceledException)
        {
            BenchmarkStatus = Loc.Instance["Bench_Stopped"];
        }
        catch (Exception ex)
        {
            BenchmarkStatus = Loc.Instance.Format("Result_Error", ex.Message);
        }
        finally
        {
            IsCapturing = false;
            _captureCancel.Dispose();
            _captureCancel = null;
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
            StartSensorsAsync().Forget("sensors");
        }
        else
        {
            _timer?.Stop();
            _timer = null;
            // A read still running keeps the library open; it is closed when that read ends.
            if (!_readingSensors) _sensors?.Dispose();
            _sensors = null;
            SensorRows.Clear();
        }
    }

    /// <summary>
    /// Opening the sensor library enumerates processor, board, drives, memory and battery and can take seconds: done off
    /// the UI thread. Switched off (or off and on again) meanwhile: the copy that is not needed is closed.
    /// </summary>
    private async Task StartSensorsAsync()
    {
        Sensors opened;
        try
        {
            opened = await Task.Run(() => new Sensors());
        }
        catch (Exception ex)
        {
            Owner.ShowResult(Loc.Instance.Format("Result_Error", ex.Message), Wpf.Ui.Controls.InfoBarSeverity.Error);
            SensorsOn = false;
            return;
        }
        if (!SensorsOn || _sensors is not null)
        {
            opened.Dispose();
            return;
        }
        _sensors = opened;
        _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => ReadSensorsAsync().Forget("sensor reading");
        _timer.Start();
        ReadSensorsAsync().Forget("sensor reading");
    }

    private bool _readingSensors;

    /// <summary>
    /// One read at a time and only while the Health page is open: a slow read (a sensor driver that hangs for a moment)
    /// is never overlapped by the next tick, and switching the sensors off during a read closes them after it.
    /// </summary>
    private async Task ReadSensorsAsync()
    {
        if (_sensors is not { } s || _readingSensors || Owner.CurrentPage != Page.Health) return;
        _readingSensors = true;
        IReadOnlyList<SensorReading> readings;
        try
        {
            readings = await Task.Run(s.Read);
        }
        catch (Exception ex)
        {
            Log.Warn("sensors", $"read failed: {ex.Message}");
            readings = [];
        }
        finally
        {
            _readingSensors = false;
            if (!ReferenceEquals(s, _sensors)) s.Dispose();
        }
        if (!ReferenceEquals(s, _sensors)) return;
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
        if (CatalogData.Current.Apps.Apps.FirstOrDefault(a => a.Id == "namazso.PawnIO") is not { } app) return;
        if (await Task.Run(() => Winget.FindTrusted()) is not { } winget)
        {
            PawnIoText = Loc.Instance["Apps_NoWinget"];
            return;
        }
        if (!dialogs.Ask(Loc.Instance["Sensors_PawnIoTitle"], Loc.Instance["Sensors_PawnIoText"], Loc.Instance["Apps_Install"])) return;
        await RunWorkAsync("install PawnIO", async () =>
        {
            var code = await Winget.InstallAsync(winget, app, null, CancellationToken.None);
            PawnIoText = Winget.IsSuccess(code) ? Loc.Instance["Sensors_PawnIoOn"] : Loc.Instance.Format("Apps_Failed", $"0x{code:X8}");
            CanInstallPawnIo = !Winget.IsSuccess(code);
        });
    }
}
