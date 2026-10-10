using Optimizer.Core.Actions;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Hardware.Probes;
using Optimizer.Core.Interop;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tests;

/// <summary>
/// Regression tests for the check audit (October 2026): each test pins one bug that a mocked hardware profile exposed.
/// No test reads this PC: profiles, registry (sandbox) and clocks are injected.
/// </summary>
public class CheckEdgeCaseTests
{
    private static readonly CatalogData C = CatalogData.Current;

    private static HardwareProfile P(Func<HardwareProfile, HardwareProfile>? edit = null)
    {
        var p = new HardwareProfile { Os = TestData.Os(), Firmware = TestData.Firmware() };
        return edit is null ? p : edit(p);
    }

    private static Finding One(IFindingCheck check, HardwareProfile p) => check.Evaluate(p, C).Single();

    private static void AssertRenders(Finding f)
    {
        foreach (var lang in DocStore.Languages)
        {
            var page = DocStore.Get(f.Id, lang);
            Assert.NotNull(page);
            var md = DocStore.RenderFinding(page!, f, Labels.Current);
            Assert.DoesNotContain("{{", md);
            Assert.DoesNotContain(":::", md);
            foreach (var fact in f.Facts)
            {
                Assert.True(Labels.Current.Has(lang, fact.LabelKey), $"label {fact.LabelKey} missing ({lang})");
                if (fact.Value.StartsWith('@')) Assert.True(Labels.Current.Has(lang, "value." + fact.Value[1..]), $"value {fact.Value} missing ({lang})");
            }
        }
    }

    private static readonly SystemInfo LaptopSystem = new("Lenovo", "Legion", [10], true, true, false);

    private static GpuInfo Igpu(string name = "Intel(R) UHD Graphics 770") => TestData.Gpu(name, Vendor.Intel, null) with { Kind = GpuKind.Integrated, PnpDeviceId = @"PCI\VEN_8086&DEV_A780\3" };

    private static GpuInfo Dgpu(string name = "NVIDIA GeForce RTX 4070") => TestData.Gpu(name, Vendor.Nvidia, 16L << 30);

    private static DisplayInfo Monitor(string name, string adapter, bool primary = false, uint output = 10, Vendor vendor = Vendor.Nvidia) =>
        new($@"\\.\DISPLAY{name.Length}", name, "", DisplayProbe.IsInternalOutput(output), output, 2560, 1440, new RefreshRate(144000, 1000), 144,
            [new DisplayMode(2560, 1440, 144, 32)], "", vendor, adapter, false, false, null) { IsPrimary = primary };

    // ---------------- FindingEngine ----------------

    private sealed class ThrowingCheck(string id) : IFindingCheck
    {
        public IReadOnlyList<string> DocIds => [id];

        public IEnumerable<Finding> Evaluate(HardwareProfile profile, CatalogData catalog)
        {
            yield return new Finding { Id = id, Kind = FindingKind.Advisor, Status = FindingStatus.Problem, Impact = 5 };
            throw new InvalidOperationException("probe data broken");
        }
    }

    [Fact]
    public void ThrowingCheckYieldsOnlyOneUnknownOfItsKindAndOthersStillRun()
    {
        var results = FindingEngine.Evaluate(P(), [new ThrowingCheck("A.test"), new LowRamCheck()], C);
        var failed = Assert.Single(results, f => f.Id == "A.test");
        Assert.Equal((FindingStatus.Unknown, FindingKind.Advisor), (failed.Status, failed.Kind)); // no partial Problem
        Assert.DoesNotContain(results, f => f.Id == LowRamCheck.Id); // LowRamCheck ran (no memory data: nothing)
        Assert.Equal(FindingKind.Advisor, ((IFindingCheck)new SecureBootCertsCheck()).Kind);
        Assert.Equal(FindingKind.Finding, ((IFindingCheck)new RefreshRateCheck()).Kind);
        Assert.Equal(FindingKind.GameAccess, ((IFindingCheck)new GameAccessCheck()).Kind);
    }

    // ---------------- X1: embedded panels (F27, F2) ----------------

    [Theory]
    [InlineData(0x80000000u, true)]
    [InlineData(11u, true)]   // embedded DisplayPort
    [InlineData(13u, true)]   // embedded UDI
    [InlineData(10u, false)]  // external DisplayPort
    [InlineData(5u, false)]
    public void EmbeddedOutputsAreInternalPanels(uint output, bool expected)
    {
        Assert.Equal(expected, DisplayProbe.IsInternalOutput(output));
        Assert.Equal(expected, DisplayLink.Connection(output) == "internal");
    }

    [Fact]
    public void LaptopPanelOnEmbeddedDisplayPortIsFound()
    {
        var igpu = Igpu("Intel(R) Iris(R) Xe Graphics");
        var p = P(p => p with
        {
            System = LaptopSystem,
            Gpus = [igpu, Dgpu("NVIDIA GeForce RTX 4060 Laptop GPU")],
            Displays = [Monitor("Built-in", igpu.Name, primary: true, output: 11, vendor: Vendor.Intel)],
        });
        var f = One(new LaptopPanelCheck(), p);
        Assert.Equal(FindingStatus.Info, f.Status);
        AssertRenders(f);
    }

    // ---------------- X2: GPU classification ----------------

    [Theory]
    [InlineData("Intel(R) Arc(TM) B390 GPU", GpuKind.Integrated)]      // Panther Lake processor graphics
    [InlineData("Intel(R) Arc(TM) B370 GPU", GpuKind.Integrated)]
    [InlineData("Intel(R) Arc(TM) 140V GPU", GpuKind.Integrated)]      // Lunar Lake
    [InlineData("Intel(R) Arc(TM) Pro Graphics", GpuKind.Integrated)]  // Meteor Lake vPro processor graphics
    [InlineData("Intel(R) Arc(TM) Pro B50 Graphics", GpuKind.Discrete)]
    [InlineData("Intel(R) Arc(TM) Pro A60 Graphics", GpuKind.Discrete)]
    [InlineData("Intel(R) Arc(TM) B580 Graphics", GpuKind.Discrete)]
    [InlineData("Intel(R) Arc(TM) A770 Graphics", GpuKind.Discrete)]
    [InlineData("Intel(R) Arc(TM) A370M Graphics", GpuKind.Discrete)]
    public void IntelArcClassification(string name, GpuKind expected) =>
        Assert.Equal(expected, GpuProbe.Classify(name, @"PCI\VEN_8086&DEV_0000", Vendor.Intel, "oem1.inf", C));

    [Theory]
    [InlineData("Radeon RX Vega", GpuKind.Discrete)]
    [InlineData("AMD Radeon RX Vega 64", GpuKind.Discrete)]
    [InlineData("AMD Radeon R9 390 Series", GpuKind.Discrete)]
    [InlineData("AMD Radeon(TM) R7 370 Series", GpuKind.Discrete)]
    [InlineData("AMD Radeon HD 7970", GpuKind.Discrete)]
    [InlineData("Radeon 550 Series", GpuKind.Discrete)]
    [InlineData("AMD Radeon RX 7800 XT", GpuKind.Discrete)]
    [InlineData("AMD Radeon(TM) Graphics", GpuKind.Integrated)]          // Ryzen APUs
    [InlineData("AMD Radeon(TM) Vega 8 Graphics", GpuKind.Integrated)]
    [InlineData("AMD Radeon 780M Graphics", GpuKind.Integrated)]
    [InlineData("AMD Radeon HD 7660D", GpuKind.Integrated)]              // A10 APU
    [InlineData("AMD Radeon(TM) R7 Graphics", GpuKind.Integrated)]       // Kaveri APU
    public void AmdClassification(string name, GpuKind expected) =>
        Assert.Equal(expected, GpuProbe.Classify(name, @"PCI\VEN_1002&DEV_0000", Vendor.Amd, "oem1.inf", C));

    // ---------------- X3 / F25: virtual adapters, location permission ----------------

    private static NicDetail Nic(string instance, string description, bool up = true) => new("{" + description.Length.ToString("D8") + "-0000-0000-0000-000000000000}",
        description, description, "Ethernet", up, up ? 10_000_000_000 : 0, null, @"SYSTEM\x\0007",
        new Dictionary<string, string>(), new Dictionary<string, IReadOnlyList<string>>()) { DeviceInstanceId = instance, Characteristics = 0x4 }; // NCF_PHYSICAL, as cards and miniports report it

    private static Wlan.Connection Wifi24() => new("Intel(R) Wi-Fi 6E AX211", "Home", "AABBCCDDEEFF", 2_437_000, [2_437_000, 5_180_000]);

    [Theory]
    [InlineData(@"PCI\VEN_8086&DEV_15BC\3", true)]
    [InlineData(@"USB\VID_0BDA&PID_8153\000001", true)]
    [InlineData(@"ROOT\VMS_MP\0000", false)]   // Hyper-V vEthernet (WSL, Docker)
    [InlineData(@"SWD\MSRRAS\MS_NDISWANIP", false)]
    [InlineData(@"SWD\Wintun\{1}", false)]
    [InlineData(@"ROOT\KDNIC\0000", false)]
    [InlineData(null, false)]
    public void PhysicalNicDetection(string? instance, bool physical) => Assert.Equal(physical, Nic(instance!, "x").IsPhysical);

    [Fact]
    public void HyperVSwitchDoesNotHideTheWifiBandCheck()
    {
        var vEthernet = Nic(@"ROOT\VMS_MP\0000", "Hyper-V Virtual Ethernet Adapter");
        var f = One(new WifiBandCheck(), P(p => p with { Extras = new HardwareExtras { Wifi = [Wifi24()], Nics = [vEthernet] } }));
        Assert.Equal(FindingStatus.Problem, f.Status);
        // A real, connected Ethernet card still skips it.
        var cable = Nic(@"PCI\VEN_8086&DEV_15BC\3", "Intel(R) Ethernet Connection I219-V");
        Assert.Empty(new WifiBandCheck().Evaluate(P(p => p with { Extras = new HardwareExtras { Wifi = [Wifi24()], Nics = [vEthernet, cable] } }), C));
    }

    [Fact]
    public void WifiDetailsWithoutLocationPermissionAreUnknown()
    {
        var denied = new Wlan.Connection("Intel(R) Wi-Fi 6E AX211", "", "", null, []) { LocationDenied = true };
        var f = One(new WifiBandCheck(), P(p => p with { Extras = new HardwareExtras { Wifi = [denied] } }));
        Assert.Equal((FindingStatus.Unknown, "noLocation"), (f.Status, f.Variant));
        AssertRenders(f);
    }

    // ---------------- X4: ACLineStatus 255 (F29, F8, F19) ----------------

    private static PowerInfo Power(bool? onAc, bool energySaver = false, uint? cpMinCores = 10, PowerPersonality personality = PowerPersonality.Balanced) =>
        new(Guid.Empty, "Balanced", personality, 100, 5, 2, cpMinCores, onAc, energySaver, true, 55);

    [Fact]
    public void UnknownAcLineIsNotOnBattery()
    {
        Assert.Equal((bool?)null, PowerProbe.AcLine(255));
        Assert.Equal((bool?)true, PowerProbe.AcLine(1));
        Assert.Equal((bool?)false, PowerProbe.AcLine(0));

        var laptop = P(p => p with { System = LaptopSystem, Power = Power(null, energySaver: true) });
        var f = One(new OnBatteryCheck(), laptop);
        Assert.Equal(FindingStatus.Unknown, f.Status);
        AssertRenders(f);
        // F8 and F19 do not judge a laptop whose power source is unknown.
        var overlay = laptop with { Extras = new HardwareExtras { PowerOverlay = FirmwareExtras.OverlayBetterBattery } };
        Assert.Empty(new PowerModeCheck().Evaluate(overlay, C));
        Assert.Empty(new EnergySaverCheck().Evaluate(laptop, C));
        Assert.Equal(FindingStatus.Problem, One(new EnergySaverCheck(), laptop with { Power = Power(true, energySaver: true) }).Status);
    }

    // ---------------- F3 ----------------

    private static HardwareProfile Desktop(params DisplayInfo[] displays) => P(p => p with { Gpus = [Igpu(), Dgpu()], Displays = displays });

    [Fact]
    public void SecondaryMonitorOnIgpuIsInfoPrimaryIsProblem()
    {
        var secondary = One(new MonitorOnIgpuCheck(), Desktop(Monitor("AOC 27G1G4", Dgpu().Name, primary: true), Monitor("DELL P2417H", Igpu().Name)));
        Assert.Equal((FindingStatus.Info, "secondary", 1), (secondary.Status, secondary.Variant, secondary.Impact));
        AssertRenders(secondary);

        var primary = One(new MonitorOnIgpuCheck(), Desktop(Monitor("AOC 27G1G4", Igpu().Name, primary: true), Monitor("DELL P2417H", Dgpu().Name)));
        Assert.Equal((FindingStatus.Problem, 5), (primary.Status, primary.Impact));
        AssertRenders(primary);

        // A single monitor is the primary one even when the position could not be read.
        Assert.Equal(FindingStatus.Problem, One(new MonitorOnIgpuCheck(), Desktop(Monitor("AOC 27G1G4", Igpu().Name))).Status);
        Assert.Equal(FindingStatus.Ok, One(new MonitorOnIgpuCheck(), Desktop(Monitor("AOC 27G1G4", Dgpu().Name, primary: true))).Status);
    }

    [Fact]
    public void UnmatchedAdapterIsUnknownAndBuiltInPanelsAreSkipped()
    {
        var unknown = One(new MonitorOnIgpuCheck(), Desktop(Monitor("AOC 27G1G4", null!, primary: true)));
        Assert.Equal(FindingStatus.Unknown, unknown.Status);
        AssertRenders(unknown);
        // All-in-one PC or laptop without a battery: the built-in panel on the iGPU is not a cabling mistake.
        Assert.Empty(new MonitorOnIgpuCheck().Evaluate(Desktop(Monitor("Built-in", Igpu().Name, primary: true, output: 0x80000000)), C));
        Assert.Empty(new MonitorOnIgpuCheck().Evaluate(Desktop(Monitor("AOC", Igpu().Name)) with { System = LaptopSystem }, C));
    }

    // ---------------- F4 ----------------

    [Fact]
    public void MissingGpuPreferenceIsInfoNotProblem()
    {
        using var registry = new SandboxRegistry();
        var games = new SoftwareInfo([], ["Steam"], []) { Games = [new InstalledGame("Cyberpunk 2077", "Steam", @"D:\Games\Cyberpunk", @"D:\Games\Cyberpunk\bin\x64\Cyberpunk2077.exe")] };
        var p = P(p => p with { Gpus = [Igpu(), Dgpu()], Software = games });
        var f = One(new GpuPreferenceCheck(registry), p);
        Assert.Equal((FindingStatus.Info, 2), (f.Status, f.Impact)); // the driver profile may already route it to the dGPU
        Assert.NotNull(f.Fix);
        Assert.Equal(2, f.Fix!.Impact.Gaming);
        AssertRenders(f);

        RegistryValue.Write(registry, Hive.User, @"Software\Microsoft\DirectX\UserGpuPreferences", @"D:\Games\Cyberpunk\bin\x64\Cyberpunk2077.exe", "string", "SwapEffectUpgradeEnable=1;GpuPreference=2;");
        Assert.Equal(FindingStatus.Ok, One(new GpuPreferenceCheck(registry), p).Status);
        Assert.Empty(new GpuPreferenceCheck(registry).Evaluate(p with { Gpus = [Dgpu()] }, C)); // not hybrid
    }

    // ---------------- F5 ----------------

    private static GpuInfo Basic(Vendor vendor) => TestData.Gpu("Microsoft Basic Display Adapter", vendor, null) with { Kind = GpuKind.Basic, PnpDeviceId = @"PCI\VEN_8086&DEV_4680\3" };

    [Fact]
    public void UnusedIgpuOnBasicDriverIsInfoWithLowImpact()
    {
        var p = P(p => p with { Gpus = [Basic(Vendor.Intel), Dgpu()], Displays = [Monitor("AOC", Dgpu().Name, primary: true)] });
        var f = One(new BasicDisplayAdapterCheck(), p);
        Assert.Equal((FindingStatus.Info, "secondary", 1), (f.Status, f.Variant, f.Impact));
        AssertRenders(f);
        // The graphics card itself (NVIDIA) or the only GPU on the basic driver stays a problem with impact 5.
        var nv = One(new BasicDisplayAdapterCheck(), P(p => p with { Gpus = [Igpu(), Basic(Vendor.Nvidia)], Displays = [] }));
        Assert.Equal((FindingStatus.Problem, 5), (nv.Status, nv.Impact));
        Assert.Equal(FindingStatus.Problem, One(new BasicDisplayAdapterCheck(), P(p => p with { Gpus = [Basic(Vendor.Amd)], Displays = [] })).Status);
        // Display on the basic adapter: it is in use.
        var used = P(p => p with { Gpus = [Basic(Vendor.Intel), Dgpu()], Displays = [Monitor("AOC", "Microsoft Basic Display Adapter", primary: true)] });
        Assert.Equal(FindingStatus.Problem, One(new BasicDisplayAdapterCheck(), used).Status);
    }

    // ---------------- F10 ----------------

    private static CpuInfo Amd(string name, int family, int model, string socket, params long[] l3Mb) => new(name, Vendor.Amd, family, model, 0, 8, 16, 4000, socket,
        null, null, "test", l3Mb.Select((s, i) => new CacheDomain(3, s << 20, 0, i)).ToList(), new Dictionary<int, int> { [0] = 8 });

    [Fact]
    public void X3dWithCoreParkingOffIsProblemEvenOnBalanced()
    {
        var cpu = Amd("AMD Ryzen 9 9950X3D 16-Core Processor", 26, 68, "AM5", 96, 32);
        var off = One(new X3dCheck(), P(p => p with { Cpu = cpu, Power = Power(true, cpMinCores: 100) }));
        Assert.Equal((FindingStatus.Problem, "parkingOff"), (off.Status, off.Variant));
        AssertRenders(off);
        Assert.Equal(FindingStatus.Ok, One(new X3dCheck(), P(p => p with { Cpu = cpu, Power = Power(true, cpMinCores: 10) })).Status);
        Assert.Equal(FindingStatus.Problem, One(new X3dCheck(), P(p => p with { Cpu = cpu, Power = Power(true, personality: PowerPersonality.HighPerformance) })).Status);
        Assert.Equal(FindingStatus.Unknown, One(new X3dCheck(), P(p => p with { Cpu = cpu })).Status);
    }

    [Fact]
    public void Ryzen5500X3dIsSingleCcd() =>
        Assert.Equal(X3dLayout.SingleCcd, X3d.Classify(Amd("AMD Ryzen 5 5500X3D 6-Core Processor", 25, 33, "AM4", 96), C));

    // ---------------- F12 / F17 / F18 ----------------

    private static StorageInfo Storage(string media, bool smr = false, double systemFree = 0.5, double dataFree = 0.5) => new(
        [new PhysicalDisk(0, "Samsung SSD 990 PRO", "SSD", "NVMe", 2_000_000_000_000, "Healthy", false),
         new PhysicalDisk(1, "ST2000DM008", media, "SATA", 2_000_000_000_000, "Healthy", smr)],
        [new Volume(@"C:\", "", 1_000_000_000_000, (long)(1_000_000_000_000 * systemFree), 0, true),
         new Volume(@"D:\", "Games", 2_000_000_000_000, (long)(2_000_000_000_000 * dataFree), 1, false),
         new Volume(@"E:\", "Backup", 2_000_000_000_000, 100_000_000_000, 1, false)]);

    [Fact]
    public void SmrLibraryHasTheSameImpactAsOtherHardDisks()
    {
        var software = new SoftwareInfo([], ["Steam"], [@"D:\SteamLibrary"]);
        var smr = One(new GamesOnHddCheck(), P(p => p with { Storage = Storage("HDD", smr: true), Software = software }));
        Assert.Equal((FindingStatus.Problem, "smr", 3), (smr.Status, smr.Variant, smr.Impact));
        AssertRenders(smr);
    }

    [Fact]
    public void SsdLibrarySummaryDoesNotSayHardDisk()
    {
        var software = new SoftwareInfo([], ["Steam"], [@"D:\SteamLibrary"]);
        var ssd = One(new GamesOnHddCheck(), P(p => p with { Storage = Storage("SSD"), Software = software }));
        Assert.Equal(FindingStatus.Ok, ssd.Status);
        AssertRenders(ssd);
        Assert.DoesNotContain("hard disk", DocStore.Summary(DocStore.Get(ssd.Id, "en")!, ssd), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Festplatte", DocStore.Summary(DocStore.Get(ssd.Id, "de")!, ssd), StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(DocStore.Summary(DocStore.Get(ssd.Id, "en")!, ssd));
    }

    [Fact]
    public void LowSpaceOnADataDriveIsInfoOnSystemAndGameDrivesProblem()
    {
        var software = new SoftwareInfo([], ["Steam"], [@"D:\SteamLibrary"]);
        var results = new LowDiskSpaceCheck().Evaluate(P(p => p with { Storage = Storage("HDD", systemFree: 0.05, dataFree: 0.05), Software = software }), C).ToList();
        Assert.Equal(FindingStatus.Problem, results.Single(f => f.Subject == @"C:\").Status);
        Assert.Equal(FindingStatus.Problem, results.Single(f => f.Subject == @"D:\").Status);
        var backup = results.Single(f => f.Subject == @"E:\"); // 5 % free, no game library
        Assert.Equal(FindingStatus.Info, backup.Status);
        foreach (var f in results) AssertRenders(f);
    }

    [Fact]
    public void MissingTrimValueMeansTrimOn()
    {
        HardwareProfile T(TrimSetting? trim) => P(p => p with { Storage = Storage("SSD"), Extras = new HardwareExtras { Trim = trim } });
        var ok = One(new TrimCheck(), T(new TrimSetting(null)));
        Assert.Equal(FindingStatus.Ok, ok.Status); // NTFS default: TRIM on unless an administrator disables it
        AssertRenders(ok);
        Assert.Equal(FindingStatus.Ok, One(new TrimCheck(), T(new TrimSetting(0))).Status);
        Assert.Equal(FindingStatus.Problem, One(new TrimCheck(), T(new TrimSetting(1))).Status);
        Assert.Equal(FindingStatus.Unknown, One(new TrimCheck(), T(null)).Status);
    }

    // ---------------- F15 / A.biosAge clocks, F21 BCD ----------------

    [Fact]
    public void DriverAgeUsesInjectedClockAndSkipsUnusedDesktopIgpu()
    {
        var today = new DateTime(2026, 10, 10);
        var old = Dgpu() with { DriverDate = today.AddDays(-184) };
        var p = P(p => p with { Gpus = [Igpu() with { DriverDate = today.AddDays(-900) }, old], Displays = [Monitor("AOC", old.Name, primary: true)] });
        var f = Assert.Single(new GpuDriverAgeCheck(() => today).Evaluate(p, C)); // the unused iGPU gets no driver advice
        Assert.Equal((FindingStatus.Problem, "184"), (f.Status, f.Facts.Single(x => x.LabelKey == "fact.driverAgeDays").Value));
        AssertRenders(f);
        Assert.Equal(FindingStatus.Ok, new GpuDriverAgeCheck(() => today.AddDays(-100)).Evaluate(p, C).Single().Status);
        // On a laptop the iGPU drives the panel: it is checked.
        Assert.Equal(2, new GpuDriverAgeCheck(() => today).Evaluate(p with { System = LaptopSystem }, C).Count());
    }

    [Fact]
    public void BiosAgeUsesInjectedClock()
    {
        var p = P(p => p with { Firmware = TestData.Firmware() with { BiosDate = new DateTime(2025, 1, 1) } });
        Assert.Equal(FindingStatus.Info, One(new BiosAgeCheck(() => new DateTime(2026, 10, 10)), p).Status);
        Assert.Equal(FindingStatus.Ok, One(new BiosAgeCheck(() => new DateTime(2025, 6, 1)), p).Status);
    }

    [Fact]
    public void LeftoverCheckWithInjectedBcd()
    {
        var problem = One(new LeftoverCheck(() => new HashSet<string> { "useplatformclock", "description" }), P());
        Assert.Equal(FindingStatus.Problem, problem.Status);
        AssertRenders(problem);
        Assert.Equal(FindingStatus.Ok, One(new LeftoverCheck(() => new HashSet<string> { "description" }), P()).Status);
        Assert.Equal(FindingStatus.Unknown, One(new LeftoverCheck(() => null), P()).Status);
        Assert.Equal(FindingStatus.Unknown, One(new LeftoverCheck(() => throw new UnauthorizedAccessException()), P()).Status);
    }

    // ---------------- G.access / A.virtualization ----------------

    private static HardwareProfile Ac(string? id, string name, Func<FirmwareInfo, FirmwareInfo>? fw = null) => P(p => p with
    {
        Firmware = fw?.Invoke(TestData.Firmware()) ?? TestData.Firmware(),
        Software = new SoftwareInfo(id is null ? [] : [new AntiCheatPresence(id, name, ["x"])], [], []),
    });

    [Fact]
    public void UnreadableDeviceGuardIsUnknownNotOff()
    {
        var f = One(new GameAccessCheck(), Ac("vanguard", "Riot Vanguard", fw => fw with { VbsStatus = -1, HvciRunning = false }));
        Assert.False(f.Params.ContainsKey("missing_hvci"));
        Assert.Equal("@unknown", f.Facts.Single(x => x.LabelKey == "fact.hvci").Value);
        Assert.Equal(FindingStatus.Ok, f.Status);
        Assert.Equal(TriState.Unknown, GameAccessCheck.PartState(TestData.Firmware() with { VbsStatus = -1 }, "vbs"));
        AssertRenders(f);
    }

    [Fact]
    public void FaceitAsksForVirtualizationBasedSecurity()
    {
        // VBS running without memory integrity: FACEIT's IOMMU requirement is met by VBS, memory integrity is per account.
        var vbsOn = One(new GameAccessCheck(), Ac("faceit", "FACEIT Anti-Cheat", fw => fw with { VbsStatus = 2, HvciRunning = false }));
        Assert.False(vbsOn.Params.ContainsKey("missing_vbs"));
        var vbsOff = One(new GameAccessCheck(), Ac("faceit", "FACEIT Anti-Cheat", fw => fw with { VbsStatus = 0, HvciRunning = false }));
        Assert.Equal((FindingStatus.Info, "sometimes", "yes"), (vbsOff.Status, vbsOff.Variant, vbsOff.Params["missing_vbs"]));
        Assert.Contains(vbsOff.Facts, x => x.LabelKey == "fact.sometimes.vbs");
        AssertRenders(vbsOff);

        var virt = One(new VirtualizationCheck(), Ac("faceit", "FACEIT Anti-Cheat") with { Extras = new HardwareExtras { Virtualization = new VirtualizationInfo(false, true, false) } });
        Assert.Equal((FindingStatus.Problem, "antiCheat"), (virt.Status, virt.Variant));
    }

    [Fact]
    public void NoAntiCheatOkHasItsOwnVariant()
    {
        var f = One(new GameAccessCheck(), Ac(null, ""));
        Assert.Equal((FindingStatus.Ok, "none"), (f.Status, f.Variant));
        AssertRenders(f);
        Assert.Null(One(new GameAccessCheck(), Ac("vanguard", "Riot Vanguard", fw => fw with { HvciRunning = true })).Variant);
    }

    [Fact]
    public void JavelinRequirementsAreOnlySometimes()
    {
        // EA: "Some EA games require Secure Boot"; TPM 2.0 for Battlefield 6. Not every Javelin game is blocked.
        var f = One(new GameAccessCheck(), Ac("javelin", "EA Javelin", fw => fw with { SecureBoot = TriState.No, TpmPresent = TriState.No }));
        Assert.Equal((FindingStatus.Info, "sometimes"), (f.Status, f.Variant));
        Assert.Equal(("yes", "yes"), (f.Params["missing_secureBoot"], f.Params["missing_tpm2"]));
        AssertRenders(f);
    }

    // ---------------- A.raptorlake / A.rebar data ----------------

    private static CpuInfo Intel(string name, int model, uint microcode) => new(name, Vendor.Intel, 6, model, 2, 10, 16, 2500, "LGA1700",
        microcode, microcode, "test", [new CacheDomain(3, 20L << 20, 0xFFFF, 0)], new Dictionary<int, int> { [0] = 4, [1] = 6 });

    [Fact]
    public void RaptorLakeModel191IsNotFlagged()
    {
        // Model 191 (06-bf) uses the 0x3x microcode series: it can never reach 0x12F.
        Assert.Empty(new MicrocodeCheck().Evaluate(P(p => p with { Cpu = Intel("13th Gen Intel(R) Core(TM) i5-13400F", 191, 0x3E) }), C));
        var f = One(new MicrocodeCheck(), P(p => p with { Cpu = Intel("13th Gen Intel(R) Core(TM) i5-13600K", 183, 0x12B) }));
        Assert.Equal((FindingStatus.Problem, true), (f.Status, f.Critical));
        Assert.Equal(FindingStatus.Ok, One(new MicrocodeCheck(), P(p => p with { Cpu = Intel("13th Gen Intel(R) Core(TM) i5-13600K", 183, 0x12F) })).Status);

        // BIOS revision unknown: a running revision that is too low proves the problem; a high one may come from Windows.
        var unknownBios = Intel("13th Gen Intel(R) Core(TM) i5-13600K", 183, 0x12F) with { MicrocodeBios = null };
        Assert.Equal(FindingStatus.Unknown, One(new MicrocodeCheck(), P(p => p with { Cpu = unknownBios })).Status);
        Assert.Equal(FindingStatus.Problem, One(new MicrocodeCheck(), P(p => p with { Cpu = unknownBios with { MicrocodeCurrent = 0x12B } })).Status);
    }

    [Theory]
    [InlineData("NVIDIA Quadro RTX 4000", Vendor.Nvidia, false)]   // Turing workstation card: no ReBAR
    [InlineData("Quadro RTX 3000", Vendor.Nvidia, false)]
    [InlineData("NVIDIA GeForce RTX 3080", Vendor.Nvidia, true)]
    [InlineData("NVIDIA GeForce RTX 4060 Laptop GPU", Vendor.Nvidia, true)]
    [InlineData("AMD Radeon RX 5700 XT", Vendor.Amd, true)]        // AMD GD-178: RX 5000 compatible
    [InlineData("AMD Radeon RX 580 Series", Vendor.Amd, false)]
    [InlineData("Intel(R) Arc(TM) Pro B50 Graphics", Vendor.Intel, true)]
    public void RebarRules(string name, Vendor vendor, bool supported)
    {
        var rule = GpuProbe.RebarSupport(TestData.Gpu(name, vendor, null), C);
        Assert.NotNull(rule);
        Assert.Equal(supported, rule.Supported);
    }

    [Fact]
    public void WorkstationRtxWithoutGeForceGetsNoAdvice() =>
        Assert.Null(GpuProbe.RebarSupport(TestData.Gpu("NVIDIA RTX 4000 Ada Generation", Vendor.Nvidia, null), C));

    [Fact]
    public void QuadroGetsNoBiosAdvice()
    {
        var f = One(new RebarCheck(), P(p => p with { Gpus = [TestData.Gpu("NVIDIA Quadro RTX 5000", Vendor.Nvidia, 256L << 20)] }));
        Assert.Equal((FindingStatus.Unsupported, "unsupported"), (f.Status, f.Variant));
    }

    [Fact]
    public void Rx5000WithSmallBarIsProblemWithImpact2()
    {
        var f = One(new RebarCheck(), P(p => p with { Cpu = TestData.Cpu13700K(), Gpus = [TestData.Gpu("AMD Radeon RX 5700 XT", Vendor.Amd, 256L << 20)] }));
        Assert.Equal((FindingStatus.Problem, "off", 2), (f.Status, f.Variant, f.Impact));
        AssertRenders(f);
        var arc = One(new RebarCheck(), P(p => p with { Gpus = [TestData.Gpu("Intel(R) Arc(TM) B580 Graphics", Vendor.Intel, 256L << 20)] }));
        Assert.Equal(5, arc.Impact); // Intel calls ReBAR required for Arc
    }

    [Fact]
    public void RebarOffOnAnOlderPlatformIsInfo()
    {
        HardwareProfile R(CpuInfo cpu) => P(p => p with { Cpu = cpu, Gpus = [TestData.Gpu("NVIDIA GeForce RTX 4070", Vendor.Nvidia, 256L << 20)] });
        // NVIDIA names Intel Core 10th gen and newer, AMD names Ryzen 3000 and newer.
        var coffeeLake = One(new RebarCheck(), R(TestData.Cpu9700K()));
        Assert.Equal((FindingStatus.Info, "platform"), (coffeeLake.Status, coffeeLake.Variant));
        AssertRenders(coffeeLake);
        Assert.Equal("platform", One(new RebarCheck(), R(Amd("AMD Ryzen 7 2700X Eight-Core Processor", 23, 8, "AM4", 16))).Variant);
        Assert.Equal("platform", One(new RebarCheck(), R(Amd("AMD Ryzen 5 3400G with Radeon Vega Graphics", 23, 24, "AM4", 4))).Variant);
        Assert.Equal("off", One(new RebarCheck(), R(Amd("AMD Ryzen 5 3600 6-Core Processor", 23, 113, "AM4", 32))).Variant);
        Assert.Equal("off", One(new RebarCheck(), R(TestData.Cpu9700K() with { Name = "Intel(R) Core(TM) i5-10400F CPU @ 2.90GHz" })).Variant);
        Assert.Equal("off", One(new RebarCheck(), R(TestData.Cpu9700K() with { Name = "Intel(R) Core(TM) Ultra 7 265K" })).Variant);
    }

    [Fact]
    public void LaptopRebarOffIsInfoWithoutBiosAdvice()
    {
        var f = One(new RebarCheck(), P(p => p with { System = LaptopSystem, Gpus = [TestData.Gpu("NVIDIA GeForce RTX 4060 Laptop GPU", Vendor.Nvidia, 256L << 20)] }));
        Assert.Equal((FindingStatus.Info, "laptop", "yes"), (f.Status, f.Variant, f.Params["laptop"]));
        AssertRenders(f);
    }

    [Fact]
    public void RebarBiosPathUsesThePlatform()
    {
        HardwareProfile R(CpuInfo cpu) => P(p => p with
        {
            Cpu = cpu,
            Firmware = TestData.Firmware("ASRock") with { BoardProduct = "B650M Pro RS" },
            Gpus = [TestData.Gpu("NVIDIA GeForce RTX 4070", Vendor.Nvidia, 256L << 20)],
        });
        var amd = One(new RebarCheck(), R(Amd("AMD Ryzen 7 7800X3D 8-Core Processor", 25, 97, "AM5", 96)));
        Assert.Equal(C.Bios.Find("ASRock", "amd", "rebar")!.Path, amd.Params["menuPath"]);
        var intel = One(new RebarCheck(), R(TestData.Cpu9700K()));
        Assert.Contains("Chipset Configuration", intel.Params["menuPath"]);
        Assert.Equal("*", RebarCheck.BiosPlatform(null));
    }

    // ---------------- A.pcieLink / A.amdAdrenalin ----------------

    [Fact]
    public void LaptopGpuLinkIsDesignNotAProblem()
    {
        var gpu = Dgpu("NVIDIA GeForce RTX 4070 Laptop GPU") with { CardLink = new PcieLink(4, 8, 4, 16), PlatformPortLink = new PcieLink(4, 8, 4, 8) };
        var f = One(new PcieLinkCheck(), P(p => p with { System = LaptopSystem, Gpus = [gpu] }));
        Assert.Equal((FindingStatus.Info, "designLimited"), (f.Status, f.Variant));
        AssertRenders(f);
        Assert.Equal(FindingStatus.Problem, One(new PcieLinkCheck(), P(p => p with { Gpus = [gpu] })).Status);
    }

    [Fact]
    public void RadeonProGetsNoAdrenalinSteps()
    {
        Assert.Empty(new AmdAdrenalinCheck().Evaluate(P(p => p with { Gpus = [TestData.Gpu("AMD Radeon PRO W7800", Vendor.Amd, null)] }), C));
        Assert.Empty(new AmdAdrenalinCheck().Evaluate(P(p => p with { Gpus = [TestData.Gpu("AMD Radeon(TM) Pro WX 3200 Series", Vendor.Amd, null)] }), C));
        var f = One(new AmdAdrenalinCheck(), P(p => p with { Gpus = [TestData.Gpu("AMD Radeon RX 7800 XT", Vendor.Amd, null)] }));
        Assert.Equal(FindingStatus.Info, f.Status);
        AssertRenders(f);
    }

    // ---------------- A.apo ----------------

    [Fact]
    public void ApoOnLaptopsPointsToTheLaptopMaker()
    {
        var hx = One(new ApoCheck(), P(p => p with { System = LaptopSystem, Cpu = TestData.Cpu9700K() with { Name = "Intel(R) Core(TM) i9-14900HX" }, Extras = new HardwareExtras() }));
        Assert.Equal((FindingStatus.Info, "yes"), (hx.Status, hx.Params["laptop"]));
        AssertRenders(hx);
        Assert.Contains("laptop maker", DocStore.RenderFinding(DocStore.Get(hx.Id, "en")!, hx, Labels.Current));
        var k = One(new ApoCheck(), P(p => p with { Cpu = TestData.Cpu9700K() with { Name = "Intel(R) Core(TM) i5-14600K" }, Extras = new HardwareExtras() }));
        Assert.Equal("", k.Params["laptop"]);
        Assert.Empty(new ApoCheck().Evaluate(P(p => p with { Cpu = TestData.Cpu9700K() with { Name = "13th Gen Intel(R) Core(TM) i5-13600K" } }), C));
    }

    // ---------------- A.amdFtpm ----------------

    [Fact]
    public void MobileAgesaFallsBackToTheBiosDate()
    {
        Assert.False(AmdFtpmCheck.IsAm4(Amd("AMD Ryzen 7 5800H with Radeon Graphics", 25, 80, "FP6", 16), false)); // mini PC: no battery
        Assert.False(AmdFtpmCheck.IsAm4(Amd("AMD Ryzen 5 5625U with Radeon Graphics", 25, 80, "", 16), false));
        Assert.True(AmdFtpmCheck.IsAm4(Amd("AMD Ryzen 7 5700G with Radeon Graphics", 25, 80, "AM4", 16), false));

        var cpu = Amd("AMD Ryzen 7 5700G with Radeon Graphics", 25, 80, "AM4", 16);
        HardwareProfile F(string source, Version agesa) => P(p => p with
        {
            Cpu = cpu,
            Firmware = TestData.Firmware() with { BiosDate = new DateTime(2023, 1, 1) },
            Extras = new HardwareExtras { TpmManufacturer = "AMD", Agesa = agesa, AgesaSource = source },
        });
        var mobile = One(new AmdFtpmCheck(), F("AGESA CezannePI-FP6 1.0.1.1", new Version(1, 0, 1, 1)));
        Assert.Equal(("date", FindingStatus.Ok), (mobile.Variant, mobile.Status)); // 1.0.1.1 is not below AMD's desktop 1.2.0.7
        var desktop = One(new AmdFtpmCheck(), F("AGESA ComboAM4v2PI 1.2.0.3c", new Version(1, 2, 0, 3)));
        Assert.Equal(("agesa", FindingStatus.Problem), (desktop.Variant, desktop.Status));
    }

    [Fact]
    public void FindsAgesaWithItsSourceString()
    {
        var found = ExtrasProbe.FindAgesa(["American Megatrends", "AGESA!V9 ComboAM4v2PI 1.2.0.7", "Default string"]);
        Assert.Equal((new Version(1, 2, 0, 7), "AGESA!V9 ComboAM4v2PI 1.2.0.7"), found);
        Assert.Null(ExtrasProbe.FindAgesa(["American Megatrends"]));
    }

    // ---------------- memory: A.ryzenMemory, A.dualChannel, A.xmp ----------------

    private static MemoryModule Module(int configured, string part, int type, int formFactor = 8, string locator = "DIMM_A1") =>
        new(16L << 30, configured, configured, part, "Corsair", locator, "BANK 0", type, formFactor);

    private static HardwareProfile Mem(CpuInfo cpu, params MemoryModule[] modules) =>
        P(p => p with { Cpu = cpu, Memory = new MemoryInfo(modules.Sum(m => m.CapacityBytes), modules) });

    [Fact]
    public void RyzenMemoryBelowRangeWithUnknownKitIsUnknown()
    {
        var am4 = Amd("AMD Ryzen 7 5800X 8-Core Processor", 25, 33, "AM4", 32);
        var f = One(new RyzenMemoryCheck(), Mem(am4, Module(2666, "M378A2K43DB1-CTD", 26), Module(2666, "M378A2K43DB1-CTD", 26)));
        Assert.Equal(FindingStatus.Unknown, f.Status); // XMP off or a slow kit: cannot tell
        AssertRenders(f);
        var slow = One(new RyzenMemoryCheck(), Mem(am4, Module(2666, "CMK16GX4M2A2666C16", 26), Module(2666, "CMK16GX4M2A2666C16", 26)));
        Assert.Equal((FindingStatus.Info, "slowKit"), (slow.Status, slow.Variant));
    }

    [Fact]
    public void Ddr5At5200IsWithinAmdSpecAndFourModulesGetNoUpgradeAdvice()
    {
        var am5 = Amd("AMD Ryzen 7 7800X3D 8-Core Processor", 25, 97, "AM5", 96);
        var spec = One(new RyzenMemoryCheck(), Mem(am5, Module(5200, "CMK32GX5M2B5200C40", 34), Module(5200, "CMK32GX5M2B5200C40", 34)));
        Assert.Equal(FindingStatus.Ok, spec.Status);
        var four = Enumerable.Range(0, 4).Select(_ => Module(4800, "CMK64GX5M4B4800C40", 34)).ToArray();
        Assert.Equal(FindingStatus.Ok, One(new RyzenMemoryCheck(), Mem(am5, four)).Status);
    }

    [Theory]
    [InlineData(8, FindingStatus.Problem)]   // DIMM
    [InlineData(12, FindingStatus.Problem)]  // SODIMM
    [InlineData(7, FindingStatus.Unknown)]   // SIMM per Win32_PhysicalMemory, not a proof of one channel
    [InlineData(0, FindingStatus.Unknown)]   // unknown, may be soldered
    [InlineData(11, FindingStatus.Unknown)]
    public void SingleModuleNeedsASocketedFormFactor(int formFactor, FindingStatus expected) =>
        Assert.Equal(expected, One(new DualChannelCheck(), Mem(TestData.Cpu9700K(), Module(3200, "CMK16GX4M1E3200C16", 26, formFactor))).Status);

    [Fact]
    public void FourDdr5ModulesOnRyzenRunSlowerByDesign()
    {
        var am5 = Amd("AMD Ryzen 9 7950X 16-Core Processor", 25, 97, "AM5", 32, 32);
        var four = new[] { "DIMM_A1", "DIMM_A2", "DIMM_B1", "DIMM_B2" }.Select(l => Module(4800, "CMK64GX5M4B6000C30", 34, locator: l)).ToArray();
        var f = One(new XmpCheck(), Mem(am5, four));
        Assert.Equal((FindingStatus.Info, "fourDimms"), (f.Status, f.Variant));
        AssertRenders(f);
        // Two modules at the same speed: EXPO is off.
        Assert.Equal(FindingStatus.Problem, One(new XmpCheck(), Mem(am5, four[0], four[2])).Status);
    }
}
