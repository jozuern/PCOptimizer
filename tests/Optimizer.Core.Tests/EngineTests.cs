using Optimizer.Core.Actions;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>Engine round trips against a sandboxed registry and fake system APIs (plan v4 §10.1). Nothing on the real system changes.</summary>
public class EngineTests
{
    private static readonly TweakCatalog Catalog = TweakCatalog.Current;

    private static Facts Facts(Action<Facts>? extra = null)
    {
        var f = new Facts().Set("os.build", 26300).Set("elevated", true).Set("system.laptop", false)
            .Set("gpu.hasDiscrete", true).Set("gpu.supportsHags", true).Set("memory.totalGb", 32.0).Set("storage.ssdOnly", true);
        extra?.Invoke(f);
        return f;
    }

    private static readonly ApplyOptions Expert = new() { ExpertMode = true, ContinueWithoutRestorePoint = true };

    private static TweakDefinition T(string id) => Catalog.Get(id) ?? throw new InvalidOperationException(id);

    private static StoredValue Reg(EngineFixture fx, Hive hive, string path, string name) => RegistryValue.Read(fx.Registry, hive, path, name);

    [Fact]
    public void CatalogLoadsWithUniqueIdsAndActions()
    {
        Assert.True(Catalog.Visible.Count() >= 45, $"only {Catalog.Visible.Count()} tweaks");
        Assert.All(Catalog.Tweaks, t => Assert.NotEmpty(t.Actions));
        Assert.All(Catalog.Tweaks.Where(t => t.IsBootCritical), t => Assert.Equal(Risk.Expert, t.EffectiveRisk));
    }

    [Fact]
    public async Task ApplyTwiceUndoOnceRestoresTrueOriginal()
    {
        using var fx = new EngineFixture();
        const string path = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "HwSchMode", "dword", "1");
        var t = T("gpu.hags");

        Assert.Equal(ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert)).Outcome);
        Assert.Equal("2", Reg(fx, Hive.Machine, path, "HwSchMode").Data);
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert); // second apply: nothing to do, original stays

        var undo = fx.Engine.Revert(t);
        Assert.True(undo.Success);
        Assert.Equal("1", Reg(fx, Hive.Machine, path, "HwSchMode").Data);
        Assert.Null(fx.Store.Get(t.Id));
    }

    [Fact]
    public async Task OriginalThatDidNotExistIsDeletedOnUndo()
    {
        using var fx = new EngineFixture();
        var t = T("power.throttlingOff");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.True(Reg(fx, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff").Existed);
        fx.Engine.Revert(t);
        Assert.False(Reg(fx, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff").Existed);
    }

    [Fact]
    public async Task FailureRollsBackEarlierActionsOfTheSameTweak()
    {
        using var fx = new EngineFixture();
        fx.Services.Start["DiagTrack"] = ServiceStart.Automatic;
        fx.Services.FailOnWrite.Add("DiagTrack");
        var t = T("privacy.telemetryOff"); // registry first, then the service (fails)

        var result = await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);

        Assert.Equal(ApplyOutcome.Failed, result.Outcome);
        Assert.False(Reg(fx, Hive.Machine, @"SOFTWARE\Policies\Microsoft\Windows\DataCollection", "AllowTelemetry").Existed);
        Assert.Equal(ServiceStart.Automatic, fx.Services.Start["DiagTrack"]);
    }

    [Fact]
    public async Task UndoLeavesValuesWindowsAlreadyChanged()
    {
        using var fx = new EngineFixture();
        const string path = @"SYSTEM\CurrentControlSet\Control\PriorityControl";
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "Win32PrioritySeparation", "dword", "2");
        var t = T("latency.win32PrioritySeparation");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);

        RegistryValue.Write(fx.Registry, Hive.Machine, path, "Win32PrioritySeparation", "dword", "24"); // feature update wrote a new default
        var undo = fx.Engine.Revert(t);

        Assert.Single(undo.AlreadyRevertedByWindows);
        Assert.Equal("24", Reg(fx, Hive.Machine, path, "Win32PrioritySeparation").Data);
    }

    [Fact]
    public async Task DriftIsReportedAsRevertedByWindows()
    {
        using var fx = new EngineFixture();
        var t = T("network.throttlingIndex");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(t, Facts()));

        RegistryValue.Delete(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile", "NetworkThrottlingIndex");
        Assert.Equal(TweakState.RevertedByWindows, fx.Engine.DetectState(t, Facts()));
    }

    [Fact]
    public async Task SharedBitmaskKeepsTheOtherTweaksBits()
    {
        using var fx = new EngineFixture();
        const string path = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        await fx.Engine.ApplyAsync(T("network.preferIpv4"), Facts(), new HashSet<string>(), Expert);
        await fx.Engine.ApplyAsync(T("network.teredoOff"), Facts(), new HashSet<string>(), Expert);
        Assert.Equal("40", Reg(fx, Hive.Machine, path, "DisabledComponents").Data); // 0x20 | 0x08

        fx.Engine.Revert(T("network.preferIpv4"));
        Assert.Equal("8", Reg(fx, Hive.Machine, path, "DisabledComponents").Data);
        fx.Engine.Revert(T("network.teredoOff"));
        var final = Reg(fx, Hive.Machine, path, "DisabledComponents");
        Assert.True(!final.Existed || final.Data == "0"); // 0 = Windows default (all IPv6 components enabled)
    }

    [Fact]
    public async Task TokenListKeepsOtherTokens()
    {
        using var fx = new EngineFixture();
        const string path = @"Software\Microsoft\DirectX\UserGpuPreferences";
        RegistryValue.Write(fx.Registry, Hive.User, path, "DirectXUserGlobalSettings", "string", "AutoHDREnable=1;");
        var t = T("gpu.windowedOptimizations");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("AutoHDREnable=1;SwapEffectUpgradeEnable=1;", Reg(fx, Hive.User, path, "DirectXUserGlobalSettings").Data);
        fx.Engine.Revert(t);
        Assert.Equal("AutoHDREnable=1;", Reg(fx, Hive.User, path, "DirectXUserGlobalSettings").Data);
    }

    [Fact]
    public void GuardRulesBlock()
    {
        using var fx = new EngineFixture();
        var none = new HashSet<string>();
        var opts = new ApplyOptions { ExpertMode = true };

        Assert.Contains(fx.Engine.Preflight(T("security.vbsOff"), Facts(f => f.Set("anticheat.strict", true)), none, opts), b => b.ReasonKey == "block.antiCheat");
        Assert.DoesNotContain(fx.Engine.Preflight(T("security.vbsOff"), Facts(f => f.Set("anticheat.strict", true)), none, new ApplyOptions { ExpertMode = true, AcknowledgeAntiCheat = true }), b => b.ReasonKey == "block.antiCheat");
        Assert.Contains(fx.Engine.Preflight(T("power.gamingPlan"), Facts(f => f.Set("cpu.x3dMultiCcd", true)), none, opts), b => b.ReasonKey == "block.x3dBalanced");
        Assert.Contains(fx.Engine.Preflight(T("power.coreParkingOff"), Facts(f => f.Set("cpu.x3dMultiCcd", true)), none, opts), b => b.ReasonKey == "block.x3dParking");
        Assert.Contains(fx.Engine.Preflight(T("network.teredoOff"), Facts(f => f.Set("xbox.used", true)), none, opts), b => b.ReasonKey == "block.xboxTeredo");
        Assert.Contains(fx.Engine.Preflight(T("latency.disableDynamicTick"), Facts(), none, new ApplyOptions()), b => b.ReasonKey == "block.expertMode");
        Assert.Contains(fx.Engine.Preflight(T("gpu.hags"), Facts(f => f.Set("elevated", false)), none, opts), b => b.ReasonKey == "block.notElevated");
        Assert.Contains(fx.Engine.Preflight(T("power.gamingPlan"), Facts(), new HashSet<string> { "power.ultimatePlan" }, opts), b => b.ReasonKey == "block.conflict");
        Assert.Contains(fx.Engine.Preflight(T("power.hibernateOff"), Facts(f => f.Set("power.modernStandby", true).Set("system.laptop", true)), none, opts), b => b.ReasonKey == "block.modernStandbyHibernate");
    }

    [Fact]
    public async Task RestorePointOncePerSessionAndFrequencyIsBackedUp()
    {
        using var fx = new EngineFixture();
        var opts = new ApplyOptions { ExpertMode = true };
        await fx.Engine.ApplyAsync(T("gpu.hags"), Facts(), new HashSet<string>(), opts);
        await fx.Engine.ApplyAsync(T("power.throttlingOff"), Facts(), new HashSet<string>(), opts);

        Assert.Equal(1, fx.RestorePoints.Created);
        Assert.Equal("0", Reg(fx, Hive.Machine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\SystemRestore", "SystemRestorePointCreationFrequency").Data);
        Assert.NotNull(fx.Store.Get(TweakEngine.RestorePointFrequencyTweak));
    }

    [Fact]
    public async Task DisabledSystemProtectionNeedsADecision()
    {
        using var fx = new EngineFixture();
        fx.RestorePoints.Enabled = false;
        var t = T("gpu.hags");
        Assert.Equal(ApplyOutcome.NeedsRestorePointDecision, (await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), new ApplyOptions())).Outcome);
        Assert.Equal(ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), new ApplyOptions { ContinueWithoutRestorePoint = true })).Outcome);
    }

    [Fact]
    public async Task PowerSettingUndoTargetsTheChangedScheme()
    {
        using var fx = new EngineFixture();
        var sub = PowerAliases.Resolve("SUB_PROCESSOR");
        var max = PowerAliases.Resolve("PROCTHROTTLEMAX");
        fx.Power.Ac[(FakePower.Balanced, sub, max)] = 99;
        var t = T("power.turboRestore");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal(100u, fx.Power.Ac[(FakePower.Balanced, sub, max)]);
        Assert.NotEmpty(fx.Power.Exports);

        fx.Power.Active = FakePower.High; // user switched plans in between
        fx.Engine.Revert(t);
        Assert.Equal(99u, fx.Power.Ac[(FakePower.Balanced, sub, max)]);
    }

    [Fact]
    public async Task GamingPlanIsCreatedAndRemovedOnUndo()
    {
        using var fx = new EngineFixture();
        var t = T("power.gamingPlan");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("PCOptimizer Gaming", fx.Power.SchemeNames[fx.Power.Active]);
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(t, Facts()));

        fx.Engine.Revert(t);
        Assert.Equal(FakePower.Balanced, fx.Power.Active);
        Assert.DoesNotContain("PCOptimizer Gaming", fx.Power.SchemeNames.Values);
    }

    [Fact]
    public async Task BootCriticalExportsBcdFirst()
    {
        using var fx = new EngineFixture();
        fx.Bcd.Elements.Add("useplatformclock");
        var t = T("leftover.usePlatformClock");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Single(fx.Bcd.Exports);
        Assert.DoesNotContain("useplatformclock", fx.Bcd.Elements);
        fx.Engine.Revert(t);
        Assert.Contains("useplatformclock", fx.Bcd.Elements);
    }

    [Fact]
    public void MissingValueMeansDefaultForGameMode()
    {
        using var fx = new EngineFixture();
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(T("gpu.gameMode"), Facts()));
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", "dword", "0");
        Assert.Equal(TweakState.Partial, fx.Engine.DetectState(T("gpu.gameMode"), Facts()));
    }

    [Fact]
    public async Task PerInterfaceValuesExpandToEachNic()
    {
        using var fx = new EngineFixture("{AAAA}", "{BBBB}");
        var t = T("network.nagleOff");
        Assert.Equal(4, fx.Engine.Expand(t).Count);
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("1", Reg(fx, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{BBBB}", "TCPNoDelay").Data);
    }

    [Fact]
    public async Task ClassicContextMenuKeyIsRemovedOnUndo()
    {
        using var fx = new EngineFixture();
        var t = T("explorer.classicContextMenu");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.NotNull(fx.Registry.Open(Hive.User, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}\InprocServer32", false));
        fx.Engine.Revert(t);
        Assert.Null(fx.Registry.Open(Hive.User, @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}", false));
    }

    [Fact]
    public async Task FirstOriginalIsNeverOverwritten()
    {
        using var fx = new EngineFixture();
        const string path = @"SYSTEM\CurrentControlSet\Control\FileSystem";
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "NtfsDisable8dot3NameCreation", "dword", "2");
        var t = T("storage.8dot3Off");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "NtfsDisable8dot3NameCreation", "dword", "0");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert); // re-apply after drift

        Assert.Equal("2", fx.Store.Get(t.Id)!.Entries.Single().Original.Data);
        fx.Engine.Revert(t);
        Assert.Equal("2", Reg(fx, Hive.Machine, path, "NtfsDisable8dot3NameCreation").Data);
    }

    [Fact]
    public async Task MemoryCompressionUsesMmAgent()
    {
        using var fx = new EngineFixture();
        var t = T("memory.compressionOff");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.False(fx.Processes.MemoryCompression);
        fx.Engine.Revert(t);
        Assert.True(fx.Processes.MemoryCompression);
    }

    [Fact]
    public void NotApplicableOnSmallRam() =>
        Assert.Equal(TweakState.NotApplicable, new EngineFixture().Engine.DetectState(T("memory.compressionOff"), Facts(f => f.Set("memory.totalGb", 8.0))));

    [Fact]
    public void ImpactOverrideForFrameGeneration()
    {
        Assert.Equal(1, TweakEngine.ImpactFor(T("gpu.hags"), Facts()).Impact);
        var (impact, effects, _) = TweakEngine.ImpactFor(T("gpu.hags"), Facts(f => f.Set("gpu.supportsFrameGeneration", true)));
        Assert.Equal(3, impact);
        Assert.Contains("prerequisite", effects);
    }

    [Fact]
    public async Task RevertAllUndoesEverythingReversible()
    {
        using var fx = new EngineFixture();
        foreach (var id in new[] { "gpu.hags", "storage.lastAccessOff", "explorer.fileExtensions" })
            await fx.Engine.ApplyAsync(T(id), Facts(), new HashSet<string>(), Expert);
        var results = fx.Engine.RevertAll();
        Assert.True(results.Count >= 3);
        Assert.All(results, r => Assert.True(r.Result.Success));
        Assert.Empty(fx.Store.All());
    }
}
