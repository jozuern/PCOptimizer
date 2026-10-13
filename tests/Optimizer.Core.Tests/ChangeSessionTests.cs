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

/// <summary>The opt-in re-apply task: its own scheduled task (only in \Tunero\) and the run that applies reset changes again.</summary>
public class ReapplyTaskTests
{
    private static Facts Facts() => new Facts().Set("os.build", 26300).Set("elevated", true).Set("system.laptop", false).Set("os.insider", false);

    [Fact]
    public void TheTaskIsRegisteredAndUndoRemovesIt()
    {
        using var fx = new EngineFixture();
        var action = new Actions.OwnTaskAction { Path = ReapplyTask.TaskPath, Xml = ReapplyTask.Xml(@"C:\Program Files\Tunero\Tunero.exe", "S-1-5-21-1-2-3-1001") };
        var original = action.Read(fx.Context)!;
        Assert.False(original.Existed);
        action.Apply(fx.Context);
        Assert.Contains("<Arguments>--reapply</Arguments>", fx.Tasks.Xml[ReapplyTask.TaskPath]);
        Assert.Contains("<RunLevel>HighestAvailable</RunLevel>", fx.Tasks.Xml[ReapplyTask.TaskPath]);
        Assert.Equal(Actions.ActionState.Applied, action.State(fx.Context));
        action.Restore(fx.Context, original);
        Assert.False(fx.Tasks.Xml.ContainsKey(ReapplyTask.TaskPath));
    }

    [Theory]
    [InlineData(@"\Microsoft\Windows\Defrag\ScheduledDefrag")]
    [InlineData(@"\Tunero\")]
    [InlineData(@"\Tunero\Sub\Task")]
    [InlineData(@"\TuneroX\Task")]
    public void NoTaskOutsideTheAppsFolder(string path)
    {
        using var fx = new EngineFixture();
        var action = new Actions.OwnTaskAction { Path = path, Xml = "<Task />" };
        Assert.Throws<InvalidOperationException>(() => action.Apply(fx.Context));
        Assert.Empty(fx.Tasks.Xml);
    }

    [Fact]
    public void OnlyAnExeOnlyAdministratorsCanChangeGetsTheTask()
    {
        Assert.True(ReapplyTask.CanRegister(@"C:\Program Files\Tunero\Tunero.exe", _ => true));
        Assert.False(ReapplyTask.CanRegister(@"C:\Users\x\Downloads\Tunero.exe", _ => false));
        Assert.False(ReapplyTask.CanRegister(null, _ => true));
    }

    [Fact]
    public async Task TheRunAppliesWhatWindowsResetAndNothingElse()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var session = new ChangeSession(fx.Engine, fx.RestorePoints);
        var t = TweakCatalog.Current.Get("privacy.advertisingIdOff")!;
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), new ApplyOptions { ContinueWithoutRestorePoint = true });
        var nothing = await session.ReapplyResetAsync(new PcSnapshot(Facts(), [], new HashSet<string>([t.Id]), []));
        Assert.Same(BatchOutcome.Cancelled, nothing);

        Actions.RegistryValue.Delete(fx.Registry, Actions.Hive.Machine, @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy");
        // Without System Protection the run goes on: nobody is there to ask, and the change was chosen before.
        var outcome = await session.ReapplyResetAsync(new PcSnapshot(Facts(), [], new HashSet<string>(), []));
        Assert.Equal(1, outcome.Applied);
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(t, Facts()));
    }
}
