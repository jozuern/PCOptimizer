using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Optimizer.Core.Tweaks;
using Optimizer.Core.Updates;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

public sealed partial class MainViewModel
{
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
}
