namespace Optimizer.App.Tests;

/// <summary>Which starts keep to one window per session. The lock and the window itself are checked in the VM.</summary>
public sealed class SingleInstanceTests
{
    [Theory]
    [InlineData]
    [InlineData("--after-update")]
    [InlineData("--AFTER-UPDATE")]
    public void APlainStartAndTheUpdatedExeKeepToOneWindow(params string[] args) => Assert.True(SingleInstance.Applies(args));

    /// <summary>The installed copy started by the setup waits for the downloaded one to close, like an updated exe.</summary>
    [Fact]
    public void TheInstalledCopyWaitsForTheDownloadedOne()
    {
        string[] args = ["--installed-from", @"C:\Users\Test\Downloads\Tunero.exe"];
        Assert.True(SingleInstance.Applies(args));
        Assert.True(SingleInstance.WaitsForOther(args));
        Assert.True(SingleInstance.WaitsForOther(["--after-update"]));
        Assert.False(SingleInstance.WaitsForOther([]));
    }

    /// <summary>Uninstall from Windows Settings opens its own window beside an open one: the cleanup waits for both to end.</summary>
    [Fact]
    public void UninstallFromWindowsSettingsIsNotAPlainStart() => Assert.False(SingleInstance.Applies(["--uninstall"]));

    /// <summary>Developer switches, command line runs and the reapply task neither block an open window nor wait for it.</summary>
    [Theory]
    [InlineData("--screenshot", "shot.png")]
    [InlineData("--perf", "perf.txt")]
    [InlineData("--apply", "changes.json")]
    [InlineData("--reapply")]
    [InlineData("--export", "out.json")]
    [InlineData("--lang", "de")]
    [InlineData("--after-update", "--screenshot", "shot.png")]
    public void SwitchesStartBesideAnOpenWindow(params string[] args) => Assert.False(SingleInstance.Applies(args));
}
