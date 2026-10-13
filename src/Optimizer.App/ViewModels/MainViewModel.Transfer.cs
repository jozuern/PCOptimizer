using System.IO;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Backup;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

/// <summary>
/// Export and import of the changes (Changes page): the catalog tweaks this app changed and the start priority rules,
/// as a file for another PC or a reinstall. The import applies through the same runner as Apply recommended.
/// </summary>
public sealed partial class MainViewModel
{
    /// <summary>Opt-in sections of the export (Changes page), remembered for the next export.</summary>
    public bool ExportApps
    {
        get => _settings.ExportApps;
        set { _settings.ExportApps = value; _settings.Save(); OnPropertyChanged(); }
    }

    public bool ExportServices
    {
        get => _settings.ExportServices;
        set { _settings.ExportServices = value; _settings.Save(); OnPropertyChanged(); }
    }

    public bool ExportStartup
    {
        get => _settings.ExportStartup;
        set { _settings.ExportStartup = value; _settings.Save(); OnPropertyChanged(); }
    }

    [RelayCommand]
    private async Task ExportChangesAsync()
    {
        try
        {
            var facts = _facts;
            var (apps, serviceTypes, startup) = (ExportApps, ExportServices, ExportStartup);
            var (file, pcSpecific) = await Task.Run(() => ChangeTransfer.Create(
                _services.Store.All().Select(b => b.TweakId), AppliedIds(), _services.Catalog,
                ProgramPriority.Read(_services.Context.Registry), AppInfo.Text, _services.Os.BuildString,
                apps || serviceTypes || startup ? TransferData.Sections(_services, facts, apps, serviceTypes, startup) : null));
            if (file.Count == 0)
            {
                ShowResult(Loc.Instance["Transfer_NothingToExport"]);
                return;
            }
            var name = $"Tunero-{Environment.MachineName}-{DateTime.Now:yyyy-MM-dd}{ChangeTransfer.Extension}";
            if (_dialogs.PickSaveFile(name, Loc.Instance["Transfer_FileType"], ChangeTransfer.Extension) is not { } path) return;
            await File.WriteAllTextAsync(path, ChangeTransfer.Serialize(file));
            Log.Info("transfer", "exported", new { tweaks = file.Tweaks.Count, priorities = file.Priorities.Count, apps = file.Apps?.Count, services = file.Services?.Count, startup = file.Startup?.Count, pcSpecific });
            ShowResult(Loc.Instance.Format("Transfer_Exported", file.Count, Path.GetFileName(path), pcSpecific), InfoBarSeverity.Success);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("transfer", $"export failed: {ex.Message}");
            ShowResult(Loc.Instance.Format("Result_Error", ex.Message), InfoBarSeverity.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanChange))]
    private async Task ImportChangesAsync()
    {
        if (_dialogs.PickOpenFile(Loc.Instance["Transfer_FileType"], ChangeTransfer.ImportPattern) is not { } path) return;
        TransferFile? file;
        try
        {
            file = new FileInfo(path).Length > ChangeTransfer.MaxFileBytes ? null : ChangeTransfer.Parse(await File.ReadAllTextAsync(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ShowResult(Loc.Instance.Format("Result_Error", ex.Message), InfoBarSeverity.Error);
            return;
        }
        if (file is null)
        {
            ShowResult(Loc.Instance["Transfer_Invalid"], InfoBarSeverity.Warning);
            return;
        }
        var profile = Profile;
        var targets = await Task.Run(() => TransferData.Targets(_services, file, profile));
        var plan = ChangeTransfer.Plan(file, _services.Catalog, _facts, AppliedIds(), targets);
        Log.Info("transfer", "import", new { together = plan.Together.Count, oneByOne = plan.OneByOne.Count, apps = plan.Apps.Count, plan.AlreadyOn, plan.NotForThisPc, plan.Unknown });
        var skipped = Loc.Instance.Format("Transfer_Skipped", plan.AlreadyOn, plan.NotForThisPc, plan.Unknown);
        if (plan.IsEmpty)
        {
            ShowResult(Loc.Instance["Transfer_Nothing"] + " " + skipped);
            return;
        }
        if (plan.Together.Count > 0)
        {
            var summary = Loc.Instance["Transfer_Intro"];
            if (plan.OneByOne.Count > 0) summary += " " + Loc.Instance.Format("Transfer_LaterOneByOne", plan.OneByOne.Count);
            var results = await Runner.ApplyBatchAsync(plan.Together, Loc.Instance["Transfer_Title"], summary);
            if (results.Count == 0) return; // cancelled
        }
        if (plan.OneByOne.Count > 0 &&
            _dialogs.Ask(Loc.Instance["Transfer_OneByOneTitle"], Loc.Instance.Format("Transfer_OneByOneText", plan.OneByOne.Count), Loc.Instance["Transfer_OneByOneGo"]))
        {
            // Each one with its own confirmation (preview, Expert and anti-cheat warnings); cancelling skips it.
            foreach (var t in plan.OneByOne) await Runner.ApplyAsync(t);
        }
        if (plan.Apps.Count > 0) await RemoveImportedAppsAsync(plan.Apps);
        if (plan.AlreadyOn + plan.NotForThisPc + plan.Unknown > 0) ShowResult(skipped);
    }

    /// <summary>
    /// The file's removed apps that this PC has: removing cannot be undone (the Store link stays for apps the Store
    /// offers), so they are asked for on their own, after the changes, with every name listed.
    /// </summary>
    private async Task RemoveImportedAppsAsync(IReadOnlyList<Optimizer.Core.Debloat.DebloatItem> apps)
    {
        var lang = Loc.Instance.Language;
        var names = string.Join(", ", apps.Select(a => a.Entry.Label(lang)));
        if (!_dialogs.Ask(Loc.Instance["Transfer_AppsTitle"], Loc.Instance.Format("Transfer_AppsText", apps.Count, names), Loc.Instance["Debloat_Remove"])) return;
        var removed = 0;
        var errors = new List<string>();
        await Runner.RunExclusiveAsync("import remove apps", async () =>
        {
            var service = new Optimizer.Core.Debloat.DebloatService(_services.Context.Processes, AppServices.DataFolder);
            foreach (var app in apps)
            {
                var error = await Task.Run(() => service.Remove(app.Installed, app.Entry));
                if (error is null) removed++;
                else errors.Add($"{app.Entry.Label(lang)}: {error}");
            }
        });
        ShowResult(Loc.Instance.Format("Transfer_AppsRemoved", removed) + (errors.Count > 0 ? " " + string.Join("; ", errors) : ""),
            errors.Count > 0 ? InfoBarSeverity.Warning : InfoBarSeverity.Success);
        await Debloat.ReloadAsync();
    }
}
