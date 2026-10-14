using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>
/// The steps the window and the command line share (<see cref="ChangeSession"/>): the restore point decision for one
/// tweak and for a set, and the counts of a batch and of undo all.
/// </summary>
public class ChangeSessionTests
{
    private static Facts Facts() => new Facts().Set("os.build", 26300).Set("elevated", true).Set("system.laptop", false)
        .Set("gpu.hasDiscrete", true).Set("gpu.supportsHags", true).Set("memory.totalGb", 32.0).Set("storage.ssdOnly", true)
        .Set("power.modernStandby", false).Set("cpu.x3dMultiCcd", false).Set("os.insider", false);

    private static TweakDefinition T(string id) => TweakCatalog.Current.Get(id) ?? throw new InvalidOperationException(id);

    private static ChangeSession Session(EngineFixture fx) => new(fx.Engine, fx.RestorePoints);

    [Fact]
    public async Task WithRestorePointsOnNobodyIsAsked()
    {
        using var fx = new EngineFixture();
        var asked = 0;
        var result = await Session(fx).ApplyAsync(T("gpu.hags"), Facts(), new HashSet<string>(), new ApplyOptions(), () => { asked++; return RestorePointChoice.Cancel; });
        Assert.Equal(ApplyOutcome.Applied, result!.Outcome);
        Assert.Equal(0, asked);
        Assert.Equal(1, fx.RestorePoints.Created);
    }

    [Fact]
    public async Task CancelAtTheRestorePointQuestionChangesNothing()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var t = T("gpu.hags");
        Assert.Null(await Session(fx).ApplyAsync(t, Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Cancel));
        Assert.Null(fx.Store.Get(t.Id));
    }

    [Fact]
    public async Task ContinueAppliesWithoutARestorePoint()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var result = await Session(fx).ApplyAsync(T("gpu.hags"), Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Continue);
        Assert.Equal(ApplyOutcome.Applied, result!.Outcome);
        Assert.False(fx.RestorePoints.Enabled);
    }

    [Fact]
    public async Task EnableTurnsOnSystemProtectionAndMakesTheRestorePoint()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var result = await Session(fx).ApplyAsync(T("gpu.hags"), Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Enable);
        Assert.Equal(ApplyOutcome.Applied, result!.Outcome);
        Assert.True(fx.RestorePoints.Enabled);
        Assert.True(fx.RestorePoints.Created > 0);
    }

    [Fact]
    public async Task BatchCountsAndUndoAll()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var session = Session(fx);
        var tweaks = new[] { T("gpu.hags"), T("explorer.fileExtensions") };
        var cancelled = await session.ApplyBatchAsync(tweaks, Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Cancel);
        Assert.Same(BatchOutcome.Cancelled, cancelled);
        Assert.Empty(fx.Store.All());

        var outcome = await session.ApplyBatchAsync(tweaks, Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Continue);
        Assert.False(outcome.StoppedForRestorePoint);
        Assert.Equal((2, 0, 0), (outcome.Applied, outcome.Failed, outcome.Skipped));

        var undone = await session.UndoAllAsync();
        Assert.Equal((2, 0), (undone.Undone, undone.Failed));
        Assert.Empty(fx.Store.All());
    }

    [Fact]
    public async Task EnableThatYieldsNoRestorePointStopsTheBatch()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        fx.RestorePoints.CreateFails = true;
        var outcome = await Session(fx).ApplyBatchAsync([T("gpu.hags")], Facts(), new HashSet<string>(), new ApplyOptions(), () => RestorePointChoice.Enable);
        Assert.True(outcome.StoppedForRestorePoint);
        Assert.Equal(0, outcome.Applied);
        Assert.Empty(fx.Store.All());
    }
}

