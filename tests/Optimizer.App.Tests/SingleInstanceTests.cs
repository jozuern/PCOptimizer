namespace Optimizer.App.Tests;

/// <summary>Which starts keep to one window per session. The lock and the window itself are checked in the VM.</summary>
public sealed class SingleInstanceTests
{
    [Theory]
    [InlineData]
    [InlineData("--after-update")]
    [InlineData("--AFTER-UPDATE")]
    public void APlainStartAndTheUpdatedExeKeepToOneWindow(params string[] args) => Assert.True(SingleInstance.Applies(args));

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
