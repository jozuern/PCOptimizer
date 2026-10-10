using Optimizer.Core.Catalog;
using Optimizer.Core.Findings;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Hardware.Probes;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tests;

public class OsGateTests
{
    [Theory]
    [InlineData(19045, CpuArchitecture.X64, OsGateResult.BlockedTooOld)]   // Windows 10 22H2
    [InlineData(22631, CpuArchitecture.X64, OsGateResult.BlockedTooOld)]   // Windows 11 23H2
    [InlineData(26100, CpuArchitecture.X64, OsGateResult.Supported)]
    [InlineData(26200, CpuArchitecture.X64, OsGateResult.Supported)]
    [InlineData(26300, CpuArchitecture.X64, OsGateResult.Supported)]       // 26H2 GA
    [InlineData(26340, CpuArchitecture.X64, OsGateResult.SupportedNotValidated)] // Experimental channel
    [InlineData(28000, CpuArchitecture.Arm64, OsGateResult.BlockedArchitecture)]
    public void Gate(int build, CpuArchitecture arch, OsGateResult expected) => Assert.Equal(expected, OsGate.Evaluate(build, arch));

    [Fact]
    public void EndOfUpdatesNoteOnlyFor24H2HomePro()
    {
        Assert.True(OsGate.Is24H2EndOfUpdates(26100, "Professional", new DateOnly(2026, 10, 13)));
        Assert.False(OsGate.Is24H2EndOfUpdates(26100, "Professional", new DateOnly(2026, 10, 12)));
        Assert.False(OsGate.Is24H2EndOfUpdates(26100, "Enterprise", new DateOnly(2026, 11, 1)));
        Assert.False(OsGate.Is24H2EndOfUpdates(26200, "Core", new DateOnly(2026, 11, 1)));
    }
}

public class CpuTests
{
    [Theory]
    [InlineData(new byte[] { 0xF8, 0, 0, 0 }, Vendor.Intel, 0xF8u)]                            // 26300, 4-byte layout
    [InlineData(new byte[] { 0, 0, 0, 0, 0x2F, 0x01, 0, 0 }, Vendor.Intel, 0x12Fu)]            // 8-byte MSR copy, Intel bytes 4 to 7
    [InlineData(new byte[] { 0x0A, 0x52, 0x40, 0x0B, 0, 0, 0, 0 }, Vendor.Amd, 0x0B40520Au)]    // AMD bytes 0 to 3
    public void Microcode(byte[] value, Vendor vendor, uint expected) => Assert.Equal(expected, CpuProbe.ParseMicrocodeBinary(value, vendor));

    [Fact]
    public void Identifier() => Assert.Equal((6, 158, 12), CpuProbe.ParseIdentifier("Intel64 Family 6 Model 158 Stepping 12"));

    private static CpuInfo Cpu(string name, Vendor vendor, int family, int model, uint? biosMicrocode, params long[] l3Mb) => new(
        name, vendor, family, model, 1, 16, 32, 4000, "", biosMicrocode, biosMicrocode, "test",
        l3Mb.Select((s, i) => new CacheDomain(3, s << 20, 0, i)).ToList(), new Dictionary<int, int> { [0] = 16 });

    [Fact]
    public void X3dAsymmetricIsMultiCcd() =>
        Assert.Equal(X3dLayout.MultiCcdAsymmetric, X3d.Classify(Cpu("AMD Ryzen 9 9950X3D 16-Core Processor", Vendor.Amd, 26, 68, null, 96, 32), CatalogData.Current));

    [Fact]
    public void X3dSymmetricDualIsOnlyViaModelList() =>
        Assert.Equal(X3dLayout.DualVCache, X3d.Classify(Cpu("AMD Ryzen 9 9950X3D2 16-Core Processor", Vendor.Amd, 26, 68, null, 96, 96), CatalogData.Current));

    [Fact]
    public void SymmetricUnknownCpuIsNotX3d() =>
        Assert.Equal(X3dLayout.None, X3d.Classify(Cpu("AMD Ryzen 9 11950X 16-Core Processor", Vendor.Amd, 27, 1, null, 48, 48), CatalogData.Current));

    [Fact]
    public void SingleCcdX3d() =>
        Assert.Equal(X3dLayout.SingleCcd, X3d.Classify(Cpu("AMD Ryzen 7 9850X3D 8-Core Processor", Vendor.Amd, 26, 68, null, 96), CatalogData.Current));

    private static HardwareProfile Profile(CpuInfo cpu) => new() { Os = TestData.Os(), Cpu = cpu };

    [Fact]
    public void RaptorLakeOldBiosMicrocodeWarnsEvenIfOsLoadedNewer()
    {
        var cpu = Cpu("13th Gen Intel(R) Core(TM) i9-13900K", Vendor.Intel, 6, 183, 0x129) with { MicrocodeCurrent = 0x12F };
        var f = new MicrocodeCheck().Evaluate(Profile(cpu), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Problem, f.Status);
        Assert.True(f.Critical);
    }

    [Fact]
    public void RaptorLakeMobileIsNotFlagged() =>
        Assert.Empty(new MicrocodeCheck().Evaluate(Profile(Cpu("13th Gen Intel(R) Core(TM) i7-13700H", Vendor.Intel, 6, 186, 0x100)), CatalogData.Current));

    [Fact]
    public void CoffeeLakeIsNotFlagged() =>
        Assert.Empty(new MicrocodeCheck().Evaluate(Profile(Cpu("Intel(R) Core(TM) i7-9700K CPU @ 3.60GHz", Vendor.Intel, 6, 158, 0xF8)), CatalogData.Current));

    [Fact]
    public void ArrowLakeBelow114()
    {
        var f = new MicrocodeCheck().Evaluate(Profile(Cpu("Intel(R) Core(TM) Ultra 9 285K", Vendor.Intel, 6, 198, 0x113)), CatalogData.Current).Single();
        Assert.Equal("A.arrowlake", f.Id);
        Assert.Equal(FindingStatus.Problem, f.Status);
    }
}

public class RamTests
{
    [Theory]
    [InlineData("CMW32GX4M2E3200C16  ", 3200)]   // Gaming PC
    [InlineData("CMK32GX5M2B6000C36", 6000)]
    [InlineData("CMH32GX5M2B6000Z30K", 6000)]
    [InlineData("F4-3600C16D-32GTZNC", 3600)]
    [InlineData("F5-6000J3038F16GX2-TZ5RK", 6000)]
    [InlineData("KF432C16BBK2/32", 3200)]
    [InlineData("KF560C30BBEK2-32", 6000)]
    [InlineData("BL2K16G32C16U4B", 3200)]
    [InlineData("CP2K16G60C30U5B", 6000)]
    [InlineData("TF3D416G3200HC16F", 3200)]
    public void DecodesRatedSpeed(string part, int expected) => Assert.Equal(expected, RamSpeed.DecodeRated(part, CatalogData.Current));

    [Theory]
    [InlineData("M425R2GA3BB0-CWMOD")]   // Samsung JEDEC SODIMM: no decoder → Unknown
    [InlineData("")]
    public void UnknownPartNumbers(string part) => Assert.Null(RamSpeed.DecodeRated(part, CatalogData.Current));

    [Theory]
    [InlineData(1600, "DDR4", 3200)]
    [InlineData(3200, "DDR4", 3200)]
    [InlineData(2400, "DDR5", 4800)]
    [InlineData(6000, "DDR5", 6000)]
    public void NormalizesMhzReports(int value, string type, int expected) => Assert.Equal(expected, RamSpeed.NormalizeConfigured(value, type));

    private static MemoryModule Module(string locator, string bank, int configured, string part = "CMW32GX4M2E3200C16") =>
        new(16L << 30, 3200, configured, part, "Corsair", locator, bank, 26, 8);

    private static HardwareProfile Profile(params MemoryModule[] modules) =>
        new() { Os = TestData.Os(), Memory = new MemoryInfo(modules.Sum(m => m.CapacityBytes), modules), Firmware = TestData.Firmware() };

    [Fact]
    public void XmpOffIsProblemWithAsusPath()
    {
        var p = Profile(Module("ChannelA-DIMM2", "BANK 1", 2133), Module("ChannelB-DIMM2", "BANK 3", 2133)) with { Cpu = TestData.Cpu9700K() };
        var f = new XmpCheck().Evaluate(p, CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Problem, f.Status);
        Assert.Contains("Ai Overclock Tuner", f.Params["menuPath"]);
        // bios.json: the ASUS path is checked against ASUS's documentation, so the page shows no "not checked" note.
        Assert.Equal("", f.Params["menuUnverified"]);
        var md = Optimizer.Core.Docs.DocStore.RenderFinding(Optimizer.Core.Docs.DocStore.Get(f.Id, "en")!, f, Optimizer.Core.Docs.Labels.Current);
        Assert.DoesNotContain("not yet checked against the manual", md);
    }

    [Fact]
    public void XmpOnIsOk() =>
        Assert.Equal(FindingStatus.Ok, new XmpCheck().Evaluate(Profile(Module("ChannelA-DIMM2", "BANK 1", 3200), Module("ChannelB-DIMM2", "BANK 3", 3200)), CatalogData.Current).Single().Status);

    [Fact]
    public void XmpUnknownPartIsUnknown() =>
        Assert.Equal(FindingStatus.Unknown, new XmpCheck().Evaluate(Profile(Module("A", "B", 2400, "XYZ123")), CatalogData.Current).Single().Status);

    [Fact]
    public void DualChannelOk() =>
        Assert.Equal(FindingStatus.Ok, new DualChannelCheck().Evaluate(Profile(Module("ChannelA-DIMM2", "BANK 1", 3200), Module("ChannelB-DIMM2", "BANK 3", 3200)), CatalogData.Current).Single().Status);

    [Fact]
    public void SameChannelIsProblem()
    {
        var f = new DualChannelCheck().Evaluate(Profile(Module("ChannelA-DIMM1", "BANK 0", 3200), Module("ChannelA-DIMM2", "BANK 1", 3200)), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Problem, f.Status);
        Assert.Equal("sameChannel", f.Variant);
    }

    [Fact]
    public void SingleDimmIsProblem() =>
        Assert.Equal(FindingStatus.Problem, new DualChannelCheck().Evaluate(Profile(Module("ChannelA-DIMM2", "BANK 1", 3200)), CatalogData.Current).Single().Status);

    [Fact]
    public void UnknownLocatorsAreUnknown() =>
        Assert.Equal(FindingStatus.Unknown, new DualChannelCheck().Evaluate(Profile(Module("Slot 1", "", 3200), Module("Slot 2", "", 3200)), CatalogData.Current).Single().Status);
}

public class DisplayGpuTests
{
    [Theory]
    [InlineData(143980u, 1000u, 144, false)]   // 143.98 Hz on a 144 Hz mode: OK
    [InlineData(59940u, 1000u, 60, false)]
    [InlineData(60000u, 1000u, 144, true)]
    [InlineData(60u, 1u, 60, false)]
    [InlineData(60u, 1u, 0, false)]            // unknown maximum never reports a problem
    public void RefreshCompare(uint num, uint den, int max, bool expected) =>
        Assert.Equal(expected, RefreshRateCheck.IsBelowMax(new RefreshRate(num, den), max));

    [Fact]
    public void MixedRefreshSetupFlagsOnlyTheSlowHighRefreshPanel()
    {
        var p = new HardwareProfile
        {
            Os = TestData.Os(),
            Displays =
            [
                TestData.Display("AOC 27G1G4", 60, 144),
                TestData.Display("DELL P2417H", 60, 60),
            ],
        };
        var results = new RefreshRateCheck().Evaluate(p, CatalogData.Current).ToList();
        Assert.Equal(FindingStatus.Problem, results.Single(f => f.Subject == "AOC 27G1G4").Status);
        Assert.Equal(FindingStatus.Ok, results.Single(f => f.Subject == "DELL P2417H").Status);
    }

    [Fact]
    public void NvidiaVersionMapping()
    {
        Assert.Equal("560.94", GpuInfo.NvidiaVersion("32.0.15.6094"));
        Assert.Equal("617.42", GpuInfo.NvidiaVersion("32.0.16.1742"));
    }

    [Theory]
    [InlineData("NVIDIA GeForce RTX 2070", false)]
    [InlineData("NVIDIA GeForce RTX 5080", true)]
    [InlineData("AMD Radeon RX 7900 XTX", true)]
    [InlineData("Intel(R) Arc(TM) B580 Graphics", true)]
    public void RebarTable(string name, bool supported)
    {
        var vendor = name.StartsWith("NVIDIA") ? Vendor.Nvidia : name.StartsWith("AMD") ? Vendor.Amd : Vendor.Intel;
        var rule = GpuProbe.RebarSupport(TestData.Gpu(name, vendor, null), CatalogData.Current);
        Assert.NotNull(rule);
        Assert.Equal(supported, rule.Supported);
    }

    [Fact]
    public void Rtx2070IsUnsupportedWithoutBiosAdvice()
    {
        var p = new HardwareProfile { Os = TestData.Os(), Gpus = [TestData.Gpu("NVIDIA GeForce RTX 2070", Vendor.Nvidia, 256L << 20)], Firmware = TestData.Firmware() };
        var f = new RebarCheck().Evaluate(p, CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Unsupported, f.Status);
        Assert.Equal("unsupported", f.Variant);
    }

    [Fact]
    public void Rtx5080WithSmallBarIsProblem()
    {
        var p = new HardwareProfile { Os = TestData.Os(), Gpus = [TestData.Gpu("NVIDIA GeForce RTX 5080", Vendor.Nvidia, 256L << 20)], Firmware = TestData.Firmware() };
        Assert.Equal(FindingStatus.Problem, new RebarCheck().Evaluate(p, CatalogData.Current).Single().Status);
    }

    [Theory]
    [InlineData(16, 8, 16, PcieLinkCheck.LinkVerdict.TrainedDown)]   // x16 card at x8 in an x16 slot
    [InlineData(16, 4, 4, PcieLinkCheck.LinkVerdict.SlotLimited)]    // x16 card in an x4 slot
    [InlineData(16, 16, 16, PcieLinkCheck.LinkVerdict.Ok)]
    [InlineData(8, 8, 16, PcieLinkCheck.LinkVerdict.Ok)]             // x8 card (4060/5060 class) is normal
    public void PcieVerdict(int cardMax, int current, int slotMax, PcieLinkCheck.LinkVerdict expected) =>
        Assert.Equal(expected, PcieLinkCheck.Judge(new PcieLink(4, current, 4, cardMax), new PcieLink(4, current, 4, slotMax)));

    [Fact]
    public void GpuClassification()
    {
        var c = CatalogData.Current;
        Assert.Equal(GpuKind.Integrated, GpuProbe.Classify("Intel(R) UHD Graphics 630", "PCI\\VEN_8086&DEV_3E98", Vendor.Intel, "oem1.inf", c));
        Assert.Equal(GpuKind.Integrated, GpuProbe.Classify("Intel(R) Arc(TM) Graphics", "PCI\\VEN_8086&DEV_7D55", Vendor.Intel, "oem1.inf", c));
        Assert.Equal(GpuKind.Discrete, GpuProbe.Classify("Intel(R) Arc(TM) B580 Graphics", "PCI\\VEN_8086&DEV_E20B", Vendor.Intel, "oem1.inf", c));
        Assert.Equal(GpuKind.Integrated, GpuProbe.Classify("AMD Radeon(TM) Graphics", "PCI\\VEN_1002&DEV_164E", Vendor.Amd, "oem1.inf", c));
        Assert.Equal(GpuKind.Basic, GpuProbe.Classify("Microsoft Basic Display Adapter", "PCI\\VEN_10DE&DEV_1F02", Vendor.Nvidia, "display.inf", c));
        Assert.Equal(GpuKind.Virtual, GpuProbe.Classify("Parsec Virtual Display Adapter", "ROOT\\DISPLAY\\0000", Vendor.Unknown, "oem9.inf", c));
    }

    [Fact]
    public void EdidParsing()
    {
        // Minimal EDID 1.4 block: header, manufacturer "AOC" (0x05E3), product 0x2701, range limits 48 to 146 Hz, name descriptor.
        var e = new byte[128];
        byte[] header = [0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00];
        header.CopyTo(e, 0);
        e[8] = 0x05; e[9] = 0xE3; e[10] = 0x01; e[11] = 0x27;
        // descriptor 2 at 72: range limits
        e[72 + 3] = 0xFD; e[72 + 5] = 48; e[72 + 6] = 146;
        // descriptor 3 at 90: name
        e[90 + 3] = 0xFC;
        System.Text.Encoding.ASCII.GetBytes("27G1G4\n     ").CopyTo(e, 95);
        var info = DisplayProbe.ParseEdid(e);
        Assert.NotNull(info);
        Assert.Equal("AOC", info.ManufacturerId);
        Assert.Equal(0x2701, info.ProductCode);
        Assert.Equal(146, info.MaxVHz);
        Assert.Equal("27G1G4", info.Name);
    }

    [Fact]
    public void MonitorPathToInstance() =>
        Assert.Equal(@"DISPLAY\AOC2701\5&123&0&UID4352",
            DisplayProbe.MonitorPathToInstanceId(@"\\?\DISPLAY#AOC2701#5&123&0&UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}"));
}

public class ScoreTests
{
    [Fact]
    public void ScoreCountsOnlyProblemsAndIgnoresGameAccess()
    {
        Finding F(FindingKind k, FindingStatus s, int? i) => new() { Id = "x", Kind = k, Status = s, Impact = i };
        var score = ReadinessScore.Compute(
        [
            F(FindingKind.Finding, FindingStatus.Problem, 5),
            F(FindingKind.Advisor, FindingStatus.Problem, 3),
            F(FindingKind.Finding, FindingStatus.Ok, 5),
            F(FindingKind.GameAccess, FindingStatus.Problem, null),
        ]);
        Assert.Equal(100 - 20 - 7, score);
    }
}
