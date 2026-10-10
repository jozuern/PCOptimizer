using System.Diagnostics;
using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Catalog;
using Optimizer.Core.Cleanup;
using Optimizer.Core.Debloat;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

public class DebloatServicesAppsTests
{
    [Fact]
    public void AppxListParsesSingleObjectAndArrays()
    {
        Assert.Single(DebloatService.ParseList("""{"Name":"Microsoft.BingNews","PackageFamilyName":"Microsoft.BingNews_8wekyb3d8bbwe","Version":"4.1","NonRemovable":false}"""));
        var list = DebloatService.ParseList("""[{"Name":"A","PackageFamilyName":"A_1","Version":"1","NonRemovable":false},{"Name":"B","PackageFamilyName":"B_1","Version":"2","NonRemovable":true}]""");
        Assert.True(list[1].NonRemovable);
    }

    [Fact]
    public void DebloatOnlyOffersCatalogAppsAndHonorsGuards()
    {
        var installed = new[]
        {
            new InstalledAppx("Microsoft.BingNews", "Microsoft.BingNews_8wekyb3d8bbwe", "1", false),
            new InstalledAppx("Microsoft.WindowsStore", "Microsoft.WindowsStore_8wekyb3d8bbwe", "1", true),
            new InstalledAppx("Microsoft.XboxGamingOverlay", "Microsoft.XboxGamingOverlay_8wekyb3d8bbwe", "1", false),
            new InstalledAppx("Some.Random.App", "x", "1", false),
        };
        var x3d = new CpuInfo("AMD Ryzen 9 9950X3D 16-Core Processor", Vendor.Amd, 26, 68, 0, 16, 32, 4300, "AM5", null, null, "t",
            [new CacheDomain(3, 96L << 20, 0, 0), new CacheDomain(3, 32L << 20, 0, 1)], new Dictionary<int, int> { [0] = 16 });
        var offered = DebloatService.Offer(CatalogData.Current.Appx, installed, new HardwareProfile { Os = TestData.Os(), Cpu = x3d }, CatalogData.Current);
        Assert.Equal(["Microsoft.BingNews", "Microsoft.XboxGamingOverlay"], offered.Select(o => o.Entry.Name).Order());
        Assert.Equal("block.appX3dGameBar", offered.Single(o => o.Entry.Name == "Microsoft.XboxGamingOverlay").BlockKey);
        Assert.True(DebloatService.IsSafeName("Microsoft.BingNews"));
        Assert.False(DebloatService.IsSafeName("x'; Remove-Item C:\\ -Recurse; '"));
        Assert.All(CatalogData.Current.Appx.Apps, a => Assert.False(CatalogData.Current.Appx.IsProtected(a.Name), a.Name));
    }

    [Fact]
    public void RemovedAppsKeepTheStoreLink()
    {
        // Records written before product IDs were known keep the (deprecated but working) package family name link.
        Assert.Equal("ms-windows-store://pdp/?PFN=Microsoft.BingNews_8wekyb3d8bbwe", new RemovedApp("Microsoft.BingNews", "Microsoft.BingNews_8wekyb3d8bbwe", "1", DateTimeOffset.Now).StoreLink);
        Assert.Equal("ms-windows-store://pdp/?ProductId=9WZDNCRFHVFW", new RemovedApp("Microsoft.BingNews", "Microsoft.BingNews_8wekyb3d8bbwe", "1", DateTimeOffset.Now, "9WZDNCRFHVFW").StoreLink);
        // Apps the Store no longer offers get no reinstall link.
        Assert.Null(new RemovedApp("Microsoft.WindowsMaps", "Microsoft.WindowsMaps_8wekyb3d8bbwe", "1", DateTimeOffset.Now, null, Reinstallable: false).StoreLink);
    }

    [Fact]
    public void EveryDebloatEntrySaysWhetherTheStoreStillOffersIt() =>
        Assert.All(CatalogData.Current.Appx.Apps, a => Assert.Contains(a.Reinstall, new[] { "store", "none" }));

    [Fact]
    public void WingetScopeIsPassedOnlyWhenItIsAKnownValue()
    {
        Assert.Contains(" --scope machine ", Winget.InstallArguments("Microsoft.PowerToys", "machine"));
        Assert.DoesNotContain("--scope", Winget.InstallArguments("Microsoft.PowerToys", null));
        Assert.DoesNotContain("--scope", Winget.InstallArguments("Microsoft.PowerToys", "machine; calc"));
        Assert.Equal("machine", CatalogData.Current.Apps.Apps.Single(a => a.Id == "Microsoft.PowerToys").WingetScope);
    }

    [Fact]
    public void ServiceEditRules()
    {
        var c = CatalogData.Current.Services;
        ServiceRow Row(string name, string? file, SignatureInfo? sig = null) =>
            new(name, name, null, ServiceStart.Automatic, true, file, file, c.Find(ServiceManager.BaseName(name))) { Signature = sig };
        var microsoft = new SignatureInfo(SignatureStatus.Signed, "Microsoft Windows", true);
        var vendor = new SignatureInfo(SignatureStatus.Signed, "Logitech Inc", false);

        Assert.Equal(ServiceEdit.ReadOnly, Row("RpcSs", @"C:\Windows\system32\rpcss.dll", microsoft).Edit);
        Assert.Equal(ServiceEdit.ReadOnly, Row("vgc", @"C:\Program Files\Riot Vanguard\vgc.exe", vendor).Edit);
        Assert.Equal(ServiceEdit.ManualOnly, Row("MapsBroker", @"C:\Windows\System32\moshost.dll", microsoft).Edit);
        Assert.Equal(ServiceEdit.ReadOnly, Row("SomeUnknownWindowsSvc", @"C:\Windows\System32\x.dll", microsoft).Edit);
        Assert.Equal(ServiceEdit.Full, Row("LGHUBUpdaterService", @"C:\Program Files\LGHUB\updater.exe", vendor).Edit);
        Assert.Null(ServiceManager.Change(Row("MapsBroker", null, microsoft), ServiceStart.Disabled));
        Assert.NotNull(ServiceManager.Change(Row("MapsBroker", null, microsoft), ServiceStart.Manual));
        Assert.Equal("CDPUserSvc", ServiceManager.BaseName("CDPUserSvc_1a2b3c"));
        Assert.Equal("Spooler", ServiceManager.BaseName("Spooler"));
    }

    [Fact]
    public async Task ChangingAServiceSeveralTimesKeepsOneBackupAndNoFalseDrift()
    {
        using var fx = new EngineFixture();
        fx.Services.Start["LGHUBUpdaterService"] = ServiceStart.Automatic;
        var vendor = new SignatureInfo(SignatureStatus.Signed, "Logitech Inc", false);
        ServiceRow Row(ServiceStart s) => new("LGHUBUpdaterService", "LG HUB Updater", null, s, true, @"C:\Program Files\LGHUB\updater.exe", null,
            CatalogData.Current.Services.Find("LGHUBUpdaterService")) { Signature = vendor };
        var facts = new Facts().Set("os.build", 26300).Set("elevated", true);
        var options = new ApplyOptions { ContinueWithoutRestorePoint = true, ExpertMode = true };

        await fx.Engine.ApplyAsync(ServiceManager.Change(Row(ServiceStart.Automatic), ServiceStart.Manual)!, facts, new HashSet<string>(), options);
        await fx.Engine.ApplyAsync(ServiceManager.Change(Row(ServiceStart.Manual), ServiceStart.Disabled)!, facts, new HashSet<string>(), options);
        Assert.Equal(ServiceStart.Disabled, fx.Services.Start["LGHUBUpdaterService"]);

        var id = ServiceManager.ChangeId("LGHUBUpdaterService");
        Assert.Single(fx.Store.All(), b => b.TweakId.StartsWith("service.", StringComparison.Ordinal));
        Assert.Equal("Automatic", fx.Store.Get(id)!.Entries.Single().Original.Data);
        Assert.Empty(fx.Engine.CheckDrift(facts)); // the earlier choice (Manual) is not "reset by Windows"

        // Undo goes back to the true original.
        Assert.True(fx.Engine.Revert(fx.Engine.Resolve(id)!).Success);
        Assert.Equal(ServiceStart.Automatic, fx.Services.Start["LGHUBUpdaterService"]);
    }

    [Theory]
    [InlineData(@"powershell.exe -w hidden -enc SQBFAFgA", true)]
    [InlineData(@"powershell.exe -ExecutionPolicy Bypass -c ""iex (irm x)""", true)]
    [InlineData(@"powershell.exe -enc SQBFAFgA C:\Windows\System32\x.ps1", true)]
    [InlineData(@"powershell.exe -ExecutionPolicy Bypass -File C:\Windows\System32\x.ps1", false)]
    [InlineData(@"""C:\Windows\system32\cmd.exe"" /d /c C:\Windows\system32\hpatchmonTask.cmd", false)]
    [InlineData(@"cmd.exe /c C:\Users\x\AppData\Roaming\evil.bat", true)]
    [InlineData(@"cmd.exe /c C:\Windows\System32\a.cmd & calc.exe", true)]
    [InlineData(@"cmd.exe /c C:\Windows\System32\..\Temp\x.cmd", true)]
    [InlineData(@"mshta.exe https://example.invalid/x.hta", true)]
    [InlineData(@"wscript.exe ""C:\Users\x\AppData\Roaming\x.vbs""", true)]
    [InlineData(@"C:\Program Files\App\app.exe --minimized", false)]
    public void ScriptHostEntriesAreFlaggedUnlessTheyOnlyRunSystemFiles(string command, bool flagged)
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var c = command.Replace(@"C:\Windows", windows, StringComparison.OrdinalIgnoreCase);
        var image = CommandLine.ImagePath(c);
        Assert.Equal(flagged, CommandLine.RunsUnverifiedScript(c, image));
    }

    [Fact]
    public void RundllTasksResolveToTheirDll()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var image = CommandLine.ImagePath($@"""{windows}\system32\rundll32.exe"" %windir%\system32\AppxDeploymentClient.dll,AppxPreStageCleanupRunTask");
        Assert.Equal(Path.Combine(windows, @"system32\AppxDeploymentClient.dll"), image, ignoreCase: true);
        Assert.False(CommandLine.IsScriptHost(image));
    }

    [Fact]
    public void RuntimeIdsDoNotCollide()
    {
        // Non-ASCII names and long names that only differ at the end got the same id before.
        StartupEntry Run(string name) => new(StartupKind.RunKey, name, "x.exe", null, "HKCU Run", Hive.User, true, $@"run:user:Run\{name}") { Target = @"Software\Microsoft\Windows\CurrentVersion\Run" };
        var a = StartupTweaks.Set(Run("微信"), false)!.Id;
        var b = StartupTweaks.Set(Run("钉钉"), false)!.Id;
        Assert.NotEqual(a, b);
        var longA = StartupTweaks.Set(Run(new string('a', 60) + "1"), false)!.Id;
        var longB = StartupTweaks.Set(Run(new string('a', 60) + "2"), false)!.Id;
        Assert.NotEqual(longA, longB);
        Assert.Equal(TweakIds.Slug("Spooler"), TweakIds.Slug("SPOOLER")); // service and registry names are case-insensitive
        Assert.NotEqual(ServiceManager.ChangeId("Svc"), ServiceManager.ChangeId("Svc_1"));
        Assert.NotEqual(TweakIds.Slug(@"PCI\VEN_10DE&DEV_1F02&SUBSYS_00000000&REV_A1\4&1&0&0008"), TweakIds.Slug(@"PCI\VEN_10DE&DEV_1F02&SUBSYS_00000000&REV_A1\4&1&0&0009"));
    }

    [Fact]
    public void ElevatedWingetComesOnlyFromTheProtectedPackageFolder()
    {
        var apps = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "WindowsApps");
        Assert.Equal(new Version(1, 29, 380, 0), Winget.PackageVersion("Microsoft.DesktopAppInstaller_1.29.380.0_x64__8wekyb3d8bbwe"));
        Assert.Null(Winget.PackageVersion("Microsoft.DesktopAppInstaller_1.29.380.0_neutral_split.language-de_8wekyb3d8bbwe"));
        // A folder outside Program Files\WindowsApps (for example in the user's profile) is never used.
        var userFolder = Path.Combine(Path.GetTempPath(), "Microsoft.DesktopAppInstaller_9.0.0.0_x64__8wekyb3d8bbwe");
        Directory.CreateDirectory(userFolder);
        System.IO.File.WriteAllText(Path.Combine(userFolder, "winget.exe"), "");
        try
        {
            Assert.Null(Winget.FindTrusted([("Microsoft.DesktopAppInstaller_9.0.0.0_x64__8wekyb3d8bbwe", userFolder)]));
        }
        finally
        {
            Directory.Delete(userFolder, true);
        }
        // On this PC (read-only): if App Installer is registered, the result is its package folder, never the alias.
        if (Winget.FindTrusted() is { } found)
        {
            Assert.StartsWith(apps + "\\", found, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(@"\AppData\", found, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void WingetSafetyAndCatalog()
    {
        Assert.All(CatalogData.Current.Apps.Apps, a => Assert.True(Winget.IsSafeId(a.Id), a.Id));
        Assert.False(Winget.IsSafeId("Valve.Steam & calc"));
        Assert.True(Winget.IsSuccess(0));
        Assert.True(Winget.IsSuccess(unchecked((int)0x8A15002B)));
        Assert.False(Winget.IsSuccess(1));
        var steam = CatalogData.Current.Apps.Apps.Single(a => a.Id == "Valve.Steam");
        Assert.True(steam.IsInstalled([new Hardware.Probes.InstalledProgram("Steam", "2.10", "Valve", false)]));
        Assert.False(steam.IsInstalled([new Hardware.Probes.InstalledProgram("Steam Link", "1", "Valve", false)]));
        Assert.Equal(new DateTime(2024, 5, 17), DriverInventory.ParseCimDate("20240517000000.******+000"));
    }

    [Fact]
    public async Task OptionalFeatureStateFromEnglishDismOutput()
    {
        using var fx = new EngineFixture();
        var state = "Disabled";
        fx.Processes.Handler = (file, args) =>
        {
            if (!file.Equals("dism.exe", StringComparison.OrdinalIgnoreCase)) return null;
            if (args.Contains("/Get-FeatureInfo")) return args.Contains("Missing") ? (87, "Error: 0x800f080c") : (0, $"Feature Name : SMB1Protocol\r\nState : {state}\r\n");
            if (args.Contains("/Enable-Feature")) { state = "Enable Pending"; return (3010, "restart required"); }
            if (args.Contains("/Disable-Feature")) { state = "Disabled"; return (0, ""); }
            return null;
        };
        var smb = new FeatureEntry { Name = "SMB1Protocol", Title = "SMB 1.0" };
        var t = OptionalFeatureAction.Tweak(smb, enabled: true);
        Assert.Equal(TweakState.NotApplied, fx.Engine.DetectState(t, new Facts().Set("os.build", 26300)));
        var r = await fx.Engine.ApplyAsync(t, new Facts().Set("os.build", 26300).Set("elevated", true), new HashSet<string>(), new ApplyOptions { ContinueWithoutRestorePoint = true });
        Assert.Equal(ApplyOutcome.Applied, r.Outcome);
        Assert.Equal("Enabled", OptionalFeatureAction.ReadState(fx.Processes, "SMB1Protocol")); // pending counts as the target state
        Assert.Null(OptionalFeatureAction.ReadState(fx.Processes, "Missing"));
        Assert.True(fx.Engine.Revert(t).Success);
        Assert.Equal("Disabled", state);
    }
}
