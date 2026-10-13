using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Apps;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Platform;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.ViewModels;

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

    /// <summary>"Install Discord": what a screen reader says for the button (each row has one).</summary>
    public string InstallLabel => Loc.Instance.Format("Apps_InstallName", Name);

    public string UpdateLabel => Loc.Instance.Format("Apps_UpdateName", Name);

    /// <summary>Installed for the whole PC: winget can update it from here (per-user apps update in their own installer).</summary>
    public bool CanUpdate => Installed && !PerUser;

    partial void OnInstalledChanged(bool value) => OnPropertyChanged(nameof(CanUpdate));
    [ObservableProperty] private bool _isInstalling;
    [ObservableProperty] private string _stateText = installed ? Loc.Instance["Apps_Installed"] : "";
}

public sealed record DriverItem(string Device, string ClassText, string Provider, string Version, string DateText, string? UpdateUrl, bool Old)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Device;

    /// <summary>Class, provider and version; a driver without provider or version shows no empty part.</summary>
    public string MetaText => string.Join(", ", new[] { ClassText, $"{Provider} {Version}".Trim() }.Where(x => x.Length > 0));

    /// <summary>Heading above the first driver of an update source ("Updates from Intel"), with one link for the group.</summary>
    public string? GroupHeader { get; init; }

    public string? GroupUrl { get; init; }

    /// <summary>"10.08.2018, about 8 years old": the date in the app language and a rounded age. Placeholder dates say so.</summary>
    public static string Describe(DateTime? date, int? ageDays)
    {
        if (date is not { } d) return "";
        if (ageDays is not { } a) return Loc.Instance["Drv_NoRealDate"];
        var shown = d.ToString("d", System.Globalization.CultureInfo.CurrentCulture);
        // A date after today (some inbox drivers carry one) has no meaningful age.
        if (a < 0) return shown;
        var age = a < 60 ? Loc.Instance["Drv_AgeNew"]
            : a < 730 ? Loc.Instance.Format("Drv_AgeMonths", (int)Math.Round(a / 30.44))
            : Loc.Instance.Format("Drv_AgeYears", (int)Math.Round(a / 365.25));
        return $"{shown}, {age}";
    }
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

    /// <summary>"Uninstall Steam": what a screen reader says for the button (each row has one).</summary>
    public string UninstallLabel => Loc.Instance.Format("Programs_UninstallName", Name);
}

public sealed partial class AppsViewModel(MainViewModel owner, AppServices services, IDialogs dialogs) : PageViewModel(owner)
{
    /// <summary>The installed state of the catalog apps comes from the scan result (installed programs).</summary>
    protected override bool UsesProfile => true;
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

    [RelayCommand]
    private async Task LoadProgramsAsync()
    {
        var list = await Task.Run(() => Programs.Read(services.Context.Registry));
        _allPrograms = list.Select(p => new ProgramRow(p)).ToList();
        services.Search.UpdateRuntime(Optimizer.Core.Search.SearchKind.InstalledProgram, _allPrograms.Select(r => ("Debloat", r.Program.RegistryKey, r.Name, r.Meta)));
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
                var code = await StreamingProcess.RunAsync(winget, Winget.UpgradeAllArguments("machine"), progress);
                lines.Add(Winget.IsSuccess(code) ? Loc.Instance["Apps_UpdateAllDone"] : Loc.Instance.Format("Apps_UpdateFailed", $"0x{code:X8}"));
                // Per-user programs update as the signed-in user, in a window of their own.
                if (_wingetForUser is { } forUser && Winget.UpgradeAllForUser(forUser, services.Elevation) != DeElevatedLauncher.Path.Failed)
                    lines.Add(Loc.Instance["Apps_UpdateAllUser"]);
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
        // One link per update source instead of one per driver: most chipset rows lead to the same Intel page. Sources
        // keep the order of their first driver; drivers without a known source come last.
        var groups = drivers.GroupBy(d => DriverInventory.UpdateUrl(d, board) ?? "").OrderBy(g => g.Key.Length == 0);
        foreach (var g in groups)
        {
            var first = true;
            foreach (var d in g)
            {
                var url = g.Key.Length == 0 ? null : g.Key;
                Drivers.Add(new DriverItem(d.Device, Loc.Instance[$"DrvClass_{d.Class}"] is var c && c != $"DrvClass_{d.Class}" ? c : d.Class,
                    d.Provider ?? "", d.Version ?? "", DriverItem.Describe(d.Date, d.AgeDays(today)), url,
                    d.AgeDays(today) > 2 * 365 && d.Class is "DISPLAY" or "NET" or "MEDIA")
                {
                    GroupHeader = first ? GroupTitle(url) : null,
                    GroupUrl = first ? url : null,
                });
                first = false;
            }
        }
    }

    /// <summary>Who the update link leads to, for the group heading.</summary>
    private static string GroupTitle(string? url) =>
        url is null ? Loc.Instance["Drv_GroupOther"]
        : url.Contains("nvidia.", StringComparison.OrdinalIgnoreCase) ? Loc.Instance.Format("Drv_GroupFrom", "NVIDIA")
        : url.Contains("amd.", StringComparison.OrdinalIgnoreCase) ? Loc.Instance.Format("Drv_GroupFrom", "AMD")
        : url.Contains("intel.", StringComparison.OrdinalIgnoreCase) ? Loc.Instance.Format("Drv_GroupFrom", "Intel")
        : Loc.Instance["Drv_GroupBoard"];

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

    /// <summary>winget upgrade for one app: the newest version from the winget source, or "already up to date".</summary>
    [RelayCommand]
    private async Task UpdateAppAsync(AppRow? row)
    {
        if (row is null || !row.CanUpdate) return;
        if (_winget is null)
        {
            row.StateText = Loc.Instance["Apps_NoWinget"];
            return;
        }
        row.IsInstalling = true;
        row.StateText = Loc.Instance["Apps_Updating"];
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
            if (!await RunWorkAsync($"update {row.App.Id}", async () =>
                {
                    var code = await Winget.UpgradeAsync(winget, row.App, progress, CancellationToken.None);
                    row.StateText = code == Winget.UpdateNotApplicable ? Loc.Instance["Apps_UpToDate"]
                        : Winget.IsSuccess(code) ? Loc.Instance["Apps_Updated"]
                        : Loc.Instance.Format("Apps_UpdateFailed", $"0x{code:X8}");
                }))
                row.StateText = Loc.Instance["Apps_Installed"];
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
