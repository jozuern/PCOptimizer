using Optimizer.Core.Actions;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>Engine round trips against a sandboxed registry and fake system APIs. Nothing on the real system changes.</summary>
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
        const string path = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "HwSchMode", "dword", "1");
        var t = T("gpu.hags");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);

        RegistryValue.Write(fx.Registry, Hive.Machine, path, "HwSchMode", "dword", "3"); // a feature update wrote a new value
        var undo = fx.Engine.Revert(t);

        Assert.Single(undo.AlreadyRevertedByWindows);
        Assert.Equal("3", Reg(fx, Hive.Machine, path, "HwSchMode").Data);
    }

    [Fact]
    public async Task DriftIsReportedAsRevertedByWindows()
    {
        using var fx = new EngineFixture();
        var t = T("privacy.advertisingIdOff");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal(TweakState.Applied, fx.Engine.DetectState(t, Facts()));

        RegistryValue.Delete(fx.Registry, Hive.Machine, @"SOFTWARE\Policies\Microsoft\Windows\AdvertisingInfo", "DisabledByGroupPolicy");
        Assert.Equal(TweakState.RevertedByWindows, fx.Engine.DetectState(t, Facts()));
    }

    [Fact]
    public async Task SharedBitmaskKeepsTheOtherTweaksBits()
    {
        using var fx = new EngineFixture();
        const string path = @"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters";
        // A second tweak owning another bit of the same value (0x08).
        var other = new TweakDefinition
        {
            Id = "test.otherBit", Category = "Network", Hidden = true, Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Actions = [new RegistryBitsAction { Hive = Hive.Machine, Path = path, Name = "DisabledComponents", Set = 8 }],
        };
        await fx.Engine.ApplyAsync(T("network.preferIpv4"), Facts(), new HashSet<string>(), Expert);
        await fx.Engine.ApplyAsync(other, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("40", Reg(fx, Hive.Machine, path, "DisabledComponents").Data); // 0x20 | 0x08

        fx.Engine.Revert(T("network.preferIpv4"));
        Assert.Equal("8", Reg(fx, Hive.Machine, path, "DisabledComponents").Data);
        fx.Engine.Revert(other);
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
        Assert.Contains(fx.Engine.Preflight(T("leftover.usePlatformClock"), Facts(), none, new ApplyOptions()), b => b.ReasonKey == "block.expertMode");
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
        fx.Bcd.Values["useplatformclock"] = "Yes";
        var t = T("leftover.usePlatformClock");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Single(fx.Bcd.Exports);
        Assert.False(fx.Bcd.Values.ContainsKey("useplatformclock"));
        fx.Engine.Revert(t);
        Assert.Equal("yes", fx.Bcd.Values["useplatformclock"]);
    }

    [Fact]
    public async Task BcdUndoWritesBackTheOriginalValue()
    {
        // "No" must come back as "no", not as a forced "yes" (that would turn HPET on).
        using var fx = new EngineFixture();
        fx.Bcd.Values["useplatformclock"] = "No";
        var t = T("leftover.usePlatformClock");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.False(fx.Bcd.Values.ContainsKey("useplatformclock"));
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal("no", fx.Bcd.Values["useplatformclock"]);
    }

    [Fact]
    public void BcdValueIsCompared()
    {
        // "disabledynamictick No" is present but not the desired "yes".
        using var fx = new EngineFixture();
        fx.Bcd.Values["disabledynamictick"] = "No";
        var a = new BcdAction { Element = "disabledynamictick", Value = "yes" };
        Assert.Equal(ActionState.NotApplied, a.State(fx.Context));
        fx.Bcd.Values["disabledynamictick"] = "Yes";
        Assert.Equal(ActionState.Applied, a.State(fx.Context));
    }

    [Fact]
    public void BcdOutputIsParsedWithValues()
    {
        var map = Platform.SystemBcdStore.Parse("Windows-Startladeprogramm\r\n-------------------------\r\nBezeichner              {current}\r\nuseplatformclock        No\r\nnx                      OptIn\r\n");
        Assert.Equal("No", map["useplatformclock"]);
        Assert.Equal("OptIn", map["nx"]);
        Assert.False(map.ContainsKey("Bezeichner"));
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
        Assert.Equal(2, fx.Engine.Expand(t).Count);
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("1", Reg(fx, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{BBBB}", "TcpAckFrequency").Data);
    }

    [Fact]
    public async Task UndoRestoresAdaptersThatAreNoLongerConnected()
    {
        // Applied with Ethernet and Wi-Fi up, undone with only Wi-Fi up: Ethernet values must still be restored.
        using var fx = new EngineFixture("{AAAA}", "{BBBB}");
        const string eth = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{AAAA}";
        RegistryValue.Write(fx.Registry, Hive.Machine, eth, "TcpAckFrequency", "dword", "2");
        var t = T("network.nagleOff");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal("1", Reg(fx, Hive.Machine, eth, "TcpAckFrequency").Data);

        var wifiOnly = new TweakEngine(WithInterfaces(fx.Context, "{BBBB}"), fx.Store, fx.RestorePoints, "test", 26300);
        Assert.Equal(TweakState.Applied, wifiOnly.DetectState(t, Facts())); // not "reset by Windows"
        var r = wifiOnly.Revert(t);
        Assert.True(r.Success);
        Assert.Equal("2", Reg(fx, Hive.Machine, eth, "TcpAckFrequency").Data);
        Assert.False(Reg(fx, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{BBBB}", "TcpAckFrequency").Existed);
        Assert.Null(fx.Store.Get(t.Id));
    }

    [Fact]
    public async Task UndoKeepsEntriesThatCannotBeRestored()
    {
        using var fx = new EngineFixture();
        fx.Services.Start["SvcA"] = ServiceStart.Automatic;
        fx.Services.Start["SvcB"] = ServiceStart.Automatic;
        var t = new TweakDefinition
        {
            Id = "test.twoServices", Category = "Services", Hidden = true, Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Actions = [new ServiceAction { Name = "SvcA", StartType = ServiceStart.Manual }, new ServiceAction { Name = "SvcB", StartType = ServiceStart.Manual }],
        };
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        fx.Services.FailOnWrite.Add("SvcA");
        var first = fx.Engine.Revert(t);
        Assert.False(first.Success);
        Assert.Equal(ServiceStart.Automatic, fx.Services.Start["SvcB"]);
        // Only SvcA is left in the backup; the retry restores it and does not call SvcB "reset by Windows".
        Assert.Single(fx.Store.Get(t.Id)!.Entries);
        fx.Services.FailOnWrite.Clear();
        var second = fx.Engine.Revert(t);
        Assert.True(second.Success);
        Assert.Empty(second.AlreadyRevertedByWindows);
        Assert.Equal(ServiceStart.Automatic, fx.Services.Start["SvcA"]);
        Assert.Null(fx.Store.Get(t.Id));
    }

    [Fact]
    public async Task FailedRollbackKeepsTheBackup()
    {
        using var fx = new EngineFixture();
        fx.Services.Start["SvcA"] = ServiceStart.Automatic;
        fx.Services.Start["SvcB"] = ServiceStart.Automatic;
        var t = new TweakDefinition
        {
            Id = "test.rollback", Category = "Services", Hidden = true, Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Actions = [new ServiceAction { Name = "SvcA", StartType = ServiceStart.Manual }, new ServiceAction { Name = "SvcB", StartType = ServiceStart.Manual }],
        };
        // SvcA is written, SvcB fails, and rolling back SvcA fails too (the fake throws on every later SvcA write).
        var services = new ThrowAfterFirstWrite(fx.Services, "SvcA", "SvcB");
        var engine = new TweakEngine(WithServices(fx.Context, services), fx.Store, fx.RestorePoints, "test", 26300);
        var r = await engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        Assert.Equal(ApplyOutcome.Failed, r.Outcome);
        Assert.Equal(ServiceStart.Manual, fx.Services.Start["SvcA"]);
        var backup = fx.Store.Get(t.Id);
        Assert.NotNull(backup);
        Assert.Equal("Automatic", backup!.Entry(new ServiceAction { Name = "SvcA" }.TargetKey)!.Original.Data);
        // Undo with working services restores it.
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal(ServiceStart.Automatic, fx.Services.Start["SvcA"]);
    }

    private sealed class ThrowAfterFirstWrite(FakeServices inner, string once, string never) : IServiceManager
    {
        private bool _written;
        public ServiceStart? GetStartType(string name) => inner.GetStartType(name);

        public void SetStartType(string name, ServiceStart start)
        {
            if (name.Equals(never, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("access denied");
            if (name.Equals(once, StringComparison.OrdinalIgnoreCase) && _written) throw new InvalidOperationException("access denied");
            _written = true;
            inner.SetStartType(name, start);
        }
    }

    private static ActionContext WithInterfaces(ActionContext c, params string[] nics) => Copy(c, c.Services, nics);
    private static ActionContext WithServices(ActionContext c, IServiceManager services) => Copy(c, services, c.NetworkInterfaceIds);

    private static ActionContext Copy(ActionContext c, IServiceManager services, IReadOnlyList<string> nics) => new()
    {
        Registry = c.Registry, Services = services, Power = c.Power, Bcd = c.Bcd, Tasks = c.Tasks, Displays = c.Displays, Processes = c.Processes,
        PowerMode = c.PowerMode, Devices = c.Devices, Network = c.Network, Nvidia = c.Nvidia, Notify = c.Notify, ExportFolder = c.ExportFolder,
        NetworkInterfaceIds = nics,
    };

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
        // 0x80000002: system managed, last access updates on (the Windows default on small volumes).
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "NtfsDisableLastAccessUpdate", "dword", "2147483650");
        var t = T("storage.lastAccessOff");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert);
        RegistryValue.Write(fx.Registry, Hive.Machine, path, "NtfsDisableLastAccessUpdate", "dword", "0");
        await fx.Engine.ApplyAsync(t, Facts(), new HashSet<string>(), Expert); // re-apply after drift

        Assert.Equal("2147483650", fx.Store.Get(t.Id)!.Entries.Single().Original.Data);
        fx.Engine.Revert(t);
        Assert.Equal("2147483650", Reg(fx, Hive.Machine, path, "NtfsDisableLastAccessUpdate").Data);
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
        Assert.Equal(0, TweakEngine.ImpactFor(T("gpu.hags"), Facts()).Impact);
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
