using Optimizer.Core.Actions;
using Optimizer.Core.Findings;
using Optimizer.Core.Hardware;
using Optimizer.Core.Interop;
using Optimizer.Core.Network;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

public class M4Tests
{
    private static CpuInfo Cpu(int cores, int threads, bool hybrid = false) => TestData.Cpu9700K() with
    {
        Cores = cores,
        Threads = threads,
        CoresByEfficiencyClass = hybrid ? new Dictionary<int, int> { [0] = 8, [1] = 8 } : new Dictionary<int, int> { [0] = cores },
    };

    [Theory]
    [InlineData(8, 8, false, 2)]     // i7-9700K: core 2
    [InlineData(8, 16, false, 4)]    // SMT: first thread of core 2
    [InlineData(16, 24, true, 4)]    // hybrid with SMT P-cores first
    [InlineData(24, 24, true, null)] // hybrid without SMT: interleaved numbering, no default
    [InlineData(2, 4, false, null)]  // too few cores
    public void AffinityTarget(int cores, int threads, bool hybrid, int? expected) =>
        Assert.Equal(expected, DeviceTweaks.AffinityTarget(Cpu(cores, threads, hybrid)));

    [Fact]
    public void DeviceTweaksOnlyForMsiCapableDevicesAndNvidiaGames()
    {
        var p = new HardwareProfile
        {
            Os = TestData.Os(),
            Cpu = TestData.Cpu9700K(),
            Gpus = [TestData.Gpu("NVIDIA GeForce RTX 2070", Vendor.Nvidia, null)],
            Software = new SoftwareInfo([], [], []) { Games = [new InstalledGame("Counter-Strike 2", "Steam", @"D:\cs2", @"D:\cs2\game\bin\win64\cs2.exe")] },
            Extras = new HardwareExtras
            {
                MsiDevices =
                [
                    new MsiDevice(@"PCI\VEN_10DE&DEV_1F02\4&1", "RTX 2070", "gpu", 0x3, 1, null),
                    new MsiDevice(@"PCI\VEN_8086&DEV_15BC\3", "I219-V", "nic", 0x1, null, null), // line-based only
                ],
            },
        };
        var tweaks = DeviceTweaks.Build(p);
        var msi = Assert.Single(tweaks, t => t.Id.StartsWith("device.msi.", StringComparison.Ordinal));
        Assert.True(msi.IsBootCritical);
        Assert.Equal(Risk.Expert, msi.EffectiveRisk);
        Assert.False(msi.IsBatchSafe);
        var reg = Assert.IsType<RegistryAction>(msi.Actions.Single());
        Assert.Equal(@"SYSTEM\CurrentControlSet\Enum\PCI\VEN_10DE&DEV_1F02\4&1\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties", reg.Path);

        var affinity = Assert.Single(tweaks, t => t.Id.StartsWith("device.affinity.", StringComparison.Ordinal));
        var mask = affinity.Actions.OfType<RegistryAction>().Single(a => a.Name == "AssignmentSetOverride");
        Assert.Equal("0400000000000000", mask.Value.GetString()); // 1 << 2, KAFFINITY little endian

        var game = Assert.Single(tweaks, t => t.Id.StartsWith("nvidia.game.", StringComparison.Ordinal));
        Assert.Equal("Counter-Strike 2", game.Subject);
        var drs = Assert.IsType<NvidiaDrsAction>(game.Actions.Single());
        Assert.Equal((Nvapi.SettingPreferredPState, Nvapi.PStatePreferMax), (drs.SettingId, drs.Value!.Value));
    }

    [Fact]
    public void DnsQueryPacketFollowsRfc1035()
    {
        var q = DnsBenchmark.BuildQuery(0x1234, "www.example.com");
        Assert.Equal([0x12, 0x34, 0x01, 0x00, 0, 1, 0, 0, 0, 0, 0, 0], q[..12]);
        Assert.Equal(3, q[12]);
        Assert.Equal("www"u8.ToArray(), q[13..16]);
        Assert.Equal([0, 0, 1, 0, 1], q[^5..]);
        Assert.True(DnsBenchmark.IsAnswerTo([0x12, 0x34, 0x81, 0x80, 0, 1, 0, 1, 0, 0, 0, 0], 0x1234));
        Assert.False(DnsBenchmark.IsAnswerTo([0x12, 0x34, 0x81, 0x83, 0, 1, 0, 0, 0, 0, 0, 0], 0x1234)); // NXDOMAIN
        Assert.False(DnsBenchmark.IsAnswerTo([0x12, 0x35, 0x81, 0x80, 0, 1, 0, 1, 0, 0, 0, 0], 0x1234));
        Assert.Equal(2.5, DnsBenchmark.Median([1, 2, 3, 4]));
    }

    [Fact]
    public void CatalogParsesNewActionTypes()
    {
        var c = TweakCatalog.Current;
        var nic = Assert.IsType<NicPropertyAction>(c.Get("network.nicPowerSavingOff")!.Actions.Single());
        Assert.Equal("ethernet", nic.Media);
        Assert.Equal("0", nic.Properties["*EEE"]);
        Assert.Equal(24u, Assert.IsType<NicPropertyAction>(c.Get("network.nicAllowPowerOffOff")!.Actions.Single()).Dwords["PnPCapabilities"]);
        var shader = Assert.IsType<NvidiaDrsAction>(c.Get("nvidia.shaderCacheUnlimited")!.Actions.Single());
        Assert.Equal((Nvapi.SettingShaderCacheMaxSize, Nvapi.ShaderCacheUnlimited), (shader.SettingId, shader.Value!.Value));
        var dns = c.Get("network.dns.cloudflare")!;
        Assert.Equal("{nic}", Assert.IsType<DnsAction>(dns.Actions.Single()).InterfaceGuid);
        Assert.Contains("network.dns.google", dns.ConflictsWith);
        Assert.Equal(Risk.Expert, c.Get("network.interruptModerationOff")!.EffectiveRisk);
    }

    [Fact]
    public void RecommendationPlanPutsFixesFirstAndExcludesUnsafe()
    {
        var fix = Findings.Checks.RuntimeFixes.PowerModeBestPerformance();
        var finding = new Finding { Id = "F8.powerMode", Kind = FindingKind.Finding, Status = FindingStatus.Problem, Impact = 3, Fix = fix };
        var safe = TweakCatalog.Current.Get("gpu.gameMode")!;
        var expert = TweakCatalog.Current.Get("network.interruptModerationOff")!;
        TweakStatus S(TweakDefinition t) => new(t, TweakState.NotApplied, t.Impact.Gaming, [], null, true, [], false);

        var plan = Recommendations.Build([S(safe), S(expert)], [finding]);
        Assert.Equal(["fix.powerMode", "gpu.gameMode"], plan.Items.Select(i => i.Tweak.Id));
        Assert.Same(finding, plan.Items[0].FixesFinding);
        Assert.Equal("network.interruptModerationOff", Assert.Single(plan.Excluded).Tweak.Id);
    }
}
