using System.IO;
using Optimizer.App.Services;
using Optimizer.Core.Platform;
using Wpf.Ui.Controls;

namespace Optimizer.App.Tests;

/// <summary>
/// The first start's setup (Install into Program Files, Start menu entry, Run from here) without the window: the install
/// steps, the Start menu and the launch are recorded, nothing changes on this PC.
/// </summary>
public sealed class SetupTests
{
    private const string Running = @"C:\Users\Test\Downloads\Tunero.exe";
    private const string Installed = @"C:\Program Files\Tunero\Tunero.exe";

    private readonly FakeShell _shell = new();
    private readonly AppSettings _settings = new();
    private readonly List<System.Diagnostics.ProcessStartInfo> _started = [];
    private readonly List<string> _startMenu = [];
    private readonly List<string> _undone = [];
    private int _installs;
    private int _saves;

    private InstallService Service(InstallResult? result = null, bool startMenuFails = false, bool launchFails = false) =>
        new(Running, _settings, () => _saves++, _shell,
            new InstallService.Steps(
                () =>
                {
                    _installs++;
                    return result ?? new InstallResult(Installed, null, null);
                },
                exe => _undone.Add(exe),
                exe =>
                {
                    if (startMenuFails) throw new IOException("no Start menu");
                    _startMenu.Add(exe);
                }),
            new Launch
            {
                Start = psi =>
                {
                    if (launchFails) throw new System.ComponentModel.Win32Exception(5);
                    _started.Add(psi);
                    return 4242;
                },
            });

    [Fact]
    public void RunFromHereIsRemembered()
    {
        Service().RunHere();
        Assert.True(_settings.SetupDeclined);
        Assert.True(_settings.StartMenuAsked);
        Assert.Equal(1, _saves);
        Assert.Equal(0, _installs);
        Assert.False(_shell.ShutDown);
    }

    [Fact]
    public void OnlyTheStartMenuAddsTheEntryForThisExe()
    {
        Assert.False(Service().Install(toProgramFiles: false, startMenu: true));
        Assert.Equal([Running], _startMenu);
        Assert.Equal(0, _installs);
        Assert.Empty(_started);
        Assert.True(_settings.SetupDeclined);
    }

    [Fact]
    public void NeitherBoxIsRunFromHere()
    {
        Assert.False(Service().Install(toProgramFiles: false, startMenu: false));
        Assert.Empty(_startMenu);
        Assert.True(_settings.SetupDeclined);
    }

    [Fact]
    public void InstallStartsTheInstalledCopyAndCloses()
    {
        Assert.True(Service().Install(toProgramFiles: true, startMenu: true));
        Assert.Equal([Installed], _startMenu);
        var start = Assert.Single(_started);
        Assert.Equal(Installed, start.FileName);
        Assert.Equal([InstallService.InstalledFrom, Running], start.ArgumentList);
        Assert.True(_shell.ShutDown);
        Assert.True(_settings.StartMenuAsked);
    }

    [Fact]
    public void AFailedInstallKeepsRunningFromHere()
    {
        Assert.False(Service(new InstallResult(null, InstallStep.Check, "not signed")).Install(toProgramFiles: true, startMenu: true));
        Assert.Empty(_startMenu);
        Assert.Empty(_started);
        Assert.False(_shell.ShutDown);
        var (text, severity) = Assert.Single(_shell.Results);
        Assert.Contains("not signed", text);
        Assert.Equal(InfoBarSeverity.Warning, severity);
        Assert.False(_settings.SetupDeclined);
    }

    [Fact]
    public void AFailedStartMenuEntryUndoesTheInstall()
    {
        Assert.False(Service(startMenuFails: true).Install(toProgramFiles: true, startMenu: true));
        Assert.Equal([Installed], _undone);
        Assert.Empty(_started);
        Assert.False(_shell.ShutDown);
        Assert.Single(_shell.Results);
    }

    [Fact]
    public void AFailedStartOfTheInstalledCopyUndoesTheInstall()
    {
        Assert.False(Service(launchFails: true).Install(toProgramFiles: true, startMenu: false));
        Assert.Equal([Installed], _undone);
        Assert.False(_shell.ShutDown);
    }
}
