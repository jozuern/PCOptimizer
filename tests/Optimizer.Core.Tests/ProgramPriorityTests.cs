using Optimizer.Core.Actions;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>Start priority rules in Image File Execution Options, applied and undone in the sandbox.</summary>
public class ProgramPriorityTests
{
    private static readonly Facts Facts = new Facts().Set("os.build", 26300).Set("elevated", true);
    private static readonly ApplyOptions Options = new() { ExpertMode = true, ContinueWithoutRestorePoint = true, AcknowledgeAntiCheat = true };

    [Theory]
    [InlineData("game.exe", true)]
    [InlineData("Cyberpunk2077.exe", true)]
    [InlineData("My Game.exe", true)]
    [InlineData(@"..\evil.exe", false)]
    [InlineData(@"C:\Games\game.exe", false)]
    [InlineData("game.bat", false)]
    [InlineData("", false)]
    public void OnlyPlainProgramNames(string name, bool valid) => Assert.Equal(valid, ProgramPriority.IsValidExe(name));

    [Fact]
    public async Task RuleIsWrittenReadAndRemovedWithItsKeys()
    {
        using var fx = new EngineFixture();
        var t = ProgramPriority.Tweak("game.exe", CpuPriority.AboveNormal, lowIo: false);
        Assert.Equal(ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(t, Facts, new HashSet<string>(), Options)).Outcome);
        var rule = Assert.Single(ProgramPriority.Read(fx.Registry));
        Assert.Equal(new PriorityRule("game.exe", CpuPriority.AboveNormal, false), rule);
        // Not flagged as a program redirect: no Debugger value.
        Assert.Empty(new StartupScanner(fx.Registry, fx.Tasks, null).ImageHijacks());

        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Empty(ProgramPriority.Read(fx.Registry));
        using (var key = fx.Registry.Open(Hive.Machine, $@"{StartupScanner.Ifeo}\game.exe", writable: false)) Assert.Null(key);

        var low = ProgramPriority.Tweak("backup.exe", CpuPriority.Low, lowIo: true);
        await fx.Engine.ApplyAsync(low, Facts, new HashSet<string>(), Options);
        Assert.Equal(new PriorityRule("backup.exe", CpuPriority.Low, true), Assert.Single(ProgramPriority.Read(fx.Registry)));
        Assert.True(low.Undocumented && low.Preview && low.AntiCheatSensitive);
        Assert.DoesNotContain(Enum.GetValues<CpuPriority>(), p => (int)p == 4); // never realtime
    }
}
