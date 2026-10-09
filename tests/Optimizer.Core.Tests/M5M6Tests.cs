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

public class StartupTests
{
    private static readonly Facts Facts = new Facts().Set("os.build", 26300).Set("elevated", true);
    private static readonly ApplyOptions Options = new() { ExpertMode = true, ContinueWithoutRestorePoint = true };
    private const string Approved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved";

    [Fact]
    public async Task DisablingARunEntryLikeTaskManagerAndUndo()
    {
        using var fx = new EngineFixture();
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Run", "Discord", "string", "\"C:\\Users\\x\\Discord\\Update.exe\" --processStart Discord.exe");
        var scanner = new StartupScanner(fx.Registry, fx.Tasks, null);
        var entry = Assert.Single(scanner.RunKeys(), e => e.Name == "Discord");
        Assert.True(entry.Enabled); // no StartupApproved value = enabled

        var tweak = StartupTweaks.Set(entry, enabled: false)!;
        Assert.Equal(ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(tweak, Facts, new HashSet<string>(), Options)).Outcome);
        var raw = RegistryValue.Read(fx.Registry, Hive.User, $@"{Approved}\Run", "Discord");
        Assert.StartsWith("03000000", raw.Data);
        Assert.Equal(24, raw.Data!.Length); // 12 bytes
        Assert.False(Assert.Single(scanner.RunKeys(), e => e.Name == "Discord").Enabled);
        // The Run value itself is untouched.
        Assert.True(RegistryValue.Read(fx.Registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Run", "Discord").Existed);

        Assert.True(fx.Engine.Revert(tweak).Success);
        Assert.False(RegistryValue.Read(fx.Registry, Hive.User, $@"{Approved}\Run", "Discord").Existed);
    }

    [Fact]
    public void TaskManagerDisabledValuesAreRecognized()
    {
        Assert.False(StartupApprovedAction.IsEnabled(new StoredValue(true, "binary", "030000005A1B3C4D5E6F7081")));
        Assert.False(StartupApprovedAction.IsEnabled(new StoredValue(true, "binary", "010000000000000000000000")));
        Assert.True(StartupApprovedAction.IsEnabled(new StoredValue(true, "binary", "020000000000000000000000")));
        Assert.True(StartupApprovedAction.IsEnabled(new StoredValue(true, "binary", "060000000000000000000000")));
        Assert.True(StartupApprovedAction.IsEnabled(StoredValue.Missing));
    }

    [Fact]
    public async Task ShellExtensionIsBlockedByClsidAndUndone()
    {
        using var fx = new EngineFixture();
        const string clsid = "{23170F69-40C1-278A-1000-000100020000}";
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Classes\*\shellex\ContextMenuHandlers\7-Zip", "", "string", clsid);
        RegistryValue.Write(fx.Registry, Hive.Machine, $@"SOFTWARE\Classes\CLSID\{clsid}", "", "string", "7-Zip Shell Extension");
        var entry = Assert.Single(new StartupScanner(fx.Registry, fx.Tasks, null).ShellExtensions());
        Assert.Equal("7-Zip Shell Extension", entry.Name);
        var tweak = StartupTweaks.Set(entry, enabled: false)!;
        await fx.Engine.ApplyAsync(tweak, Facts, new HashSet<string>(), Options);
        Assert.True(RegistryValue.Read(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", clsid).Existed);
        Assert.False(Assert.Single(new StartupScanner(fx.Registry, fx.Tasks, null).ShellExtensions()).Enabled);
        Assert.True(fx.Engine.Revert(tweak).Success);
        Assert.False(RegistryValue.Read(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Shell Extensions\Blocked", clsid).Existed);
    }

    [Fact]
    public void WinlogonDefaultsAndIfeoDebuggers()
    {
        using var fx = new EngineFixture();
        RegistryValue.Write(fx.Registry, Hive.Machine, StartupScanner.Winlogon, "Shell", "string", "explorer.exe");
        RegistryValue.Write(fx.Registry, Hive.Machine, StartupScanner.Winlogon, "Userinit", "string", @"C:\Windows\system32\userinit.exe,C:\evil.exe");
        RegistryValue.Write(fx.Registry, Hive.Machine, $@"{StartupScanner.Ifeo}\taskmgr.exe", "Debugger", "string", @"C:\Tools\procexp64.exe");
        var scanner = new StartupScanner(fx.Registry, fx.Tasks, null);
        var winlogon = scanner.WinlogonEntries().ToList();
        Assert.False(winlogon.Single(e => e.Name == "Shell").Suspicious);
        Assert.True(winlogon.Single(e => e.Name == "Userinit").Suspicious);
        var ifeo = Assert.Single(scanner.ImageHijacks());
        Assert.Equal(Risk.Expert, StartupTweaks.Set(ifeo, enabled: false)!.EffectiveRisk);
        Assert.Null(StartupTweaks.Set(winlogon[0], enabled: false)); // read-only
    }

    [Fact]
    public void LogonTasksComeFromTheScheduler()
    {
        using var fx = new EngineFixture();
        fx.Tasks.Listed.Add(new ScheduledTaskInfo(@"\Vendor\Updater", true, "Vendor", @"C:\Vendor\up.exe", "/silent", true, false, null));
        fx.Tasks.Listed.Add(new ScheduledTaskInfo(@"\Vendor\Daily", true, "Vendor", @"C:\Vendor\up.exe", null, false, false, null));
        var task = Assert.Single(new StartupScanner(fx.Registry, fx.Tasks, null).LogonTasks());
        Assert.Equal(@"\Vendor\Updater", task.Target);
        Assert.IsType<ScheduledTaskAction>(StartupTweaks.Set(task, false)!.Actions.Single());
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\App\\app.exe\" -min", @"C:\Program Files\App\app.exe")]
    [InlineData(@"C:\Program Files\App\app.exe -min", @"C:\Program Files\App\app.exe")]
    [InlineData(@"rundll32.exe C:\Tools\helper.dll,Start", @"C:\Tools\helper.dll")]
    [InlineData(@"\??\C:\Drivers\x.sys", @"C:\Drivers\x.sys")]
    public void CommandLineImagePaths(string command, string expected)
    {
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { @"C:\Program Files\App\app.exe", @"C:\Tools\helper.dll", @"C:\Drivers\x.sys" };
        Assert.Equal(expected, CommandLine.ImagePath(command, s => s, files.Contains));
    }

    [Fact]
    public void VirusTotalParsingAndThresholds()
    {
        static string Json(int m, int s) =>
            "{\"data\":{\"attributes\":{\"last_analysis_stats\":{\"malicious\":" + m + ",\"suspicious\":" + s + ",\"undetected\":60,\"harmless\":0}}}}";
        Assert.Equal(VirusTotalVerdict.Clean, VirusTotalClient.Parse("x", Json(0, 0)).Verdict);
        Assert.Equal(VirusTotalVerdict.Suspicious, VirusTotalClient.Parse("x", Json(1, 0)).Verdict); // one engine: often a false positive
        var bad = VirusTotalClient.Parse("x", Json(12, 1));
        Assert.Equal((VirusTotalVerdict.Malicious, 73), (bad.Verdict, bad.Total));
    }

    [Fact]
    public void DpapiRoundTrip()
    {
        var protectedKey = Dpapi.Protect("secret-api-key");
        Assert.DoesNotContain("secret", protectedKey);
        Assert.Equal("secret-api-key", Dpapi.Unprotect(protectedKey));
        Assert.Null(Dpapi.Unprotect("not base64!"));
    }
}

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
    public void RemovedAppsKeepTheStoreLink() =>
        Assert.Equal("ms-windows-store://pdp/?PFN=Microsoft.BingNews_8wekyb3d8bbwe", new RemovedApp("Microsoft.BingNews", "Microsoft.BingNews_8wekyb3d8bbwe", "1", DateTimeOffset.Now).StoreLink);

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

public class CleanupStorageTests : IDisposable
{
    // Outside AppData on purpose: the storage analyzer never offers anything under AppData (program state).
    private readonly string _root = Path.Combine(Path.GetPathRoot(Path.GetTempPath())!, "pco-test-" + Guid.NewGuid().ToString("N"));

    public CleanupStorageTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try
        {
            // Remove the junction first so the delete never walks into its target.
            foreach (var d in Directory.GetDirectories(_root, "*", SearchOption.AllDirectories).Where(d => (System.IO.File.GetAttributes(d) & FileAttributes.ReparsePoint) != 0))
                Directory.Delete(d);
            Directory.Delete(_root, true);
        }
        catch (IOException)
        {
        }
    }

    private string File(string relative, int bytes, DateTime? written = null)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllBytes(path, new byte[bytes]);
        if (written is { } w)
        {
            System.IO.File.SetLastWriteTimeUtc(path, w);
            System.IO.File.SetCreationTimeUtc(path, w);
        }
        return path;
    }

    [Fact]
    public void CleanupNeverFollowsJunctionsAndRespectsAge()
    {
        var outside = Path.Combine(_root, "outside");
        var keep = File(@"outside\precious.txt", 10, DateTime.UtcNow.AddDays(-30));
        var temp = Path.Combine(_root, "temp");
        var old = File(@"temp\old.tmp", 100, DateTime.UtcNow.AddDays(-3));
        var fresh = File(@"temp\fresh.tmp", 100);
        File(@"temp\sub\old2.tmp", 50, DateTime.UtcNow.AddDays(-3));
        var ini = File(@"temp\desktop.ini", 5, DateTime.UtcNow.AddDays(-3));
        // Junction inside the cleaned folder pointing outside (no admin rights needed for junctions).
        var junction = Path.Combine(temp, "link");
        var mk = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{junction}\" \"{outside}\"") { CreateNoWindow = true, UseShellExecute = false })!;
        mk.WaitForExit();
        Assert.True(Directory.Exists(junction));

        var category = new CleanupCategory("cleanup.test", [temp], MinAge: CleanupEngine.TempMinAge);
        var scan = CleanupEngine.Scan(category);
        Assert.Equal((2, 150L), (scan.Files, scan.Bytes));
        var result = CleanupEngine.Clean(category);
        Assert.Equal(2, result.Deleted);
        Assert.False(System.IO.File.Exists(old));
        Assert.True(System.IO.File.Exists(fresh));
        Assert.True(System.IO.File.Exists(ini));
        Assert.True(System.IO.File.Exists(keep));        // never reached through the junction
        Assert.False(Directory.Exists(Path.Combine(temp, "sub"))); // emptied subfolder removed
        Assert.True(Directory.Exists(temp));             // root kept
    }

    private static void Junction(string link, string target)
    {
        var mk = Process.Start(new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"") { CreateNoWindow = true, UseShellExecute = false })!;
        mk.WaitForExit();
        Assert.True(Directory.Exists(link));
    }

    [Fact]
    public void CleanupSkipsARootThatIsOrLiesUnderAJunction()
    {
        // %LOCALAPPDATA%\Temp (or a parent like %LOCALAPPDATA%\NVIDIA) replaced by a junction to a protected folder.
        var keep = File(@"protected\program.dll", 10, DateTime.UtcNow.AddDays(-30));
        var keep2 = File(@"protected\DXCache\shader.bin", 10, DateTime.UtcNow.AddDays(-30));
        var temp = Path.Combine(_root, "Temp");
        Junction(temp, Path.Combine(_root, "protected"));
        var nvidia = Path.Combine(_root, "NVIDIA");
        Junction(nvidia, Path.Combine(_root, "protected"));

        var category = new CleanupCategory("cleanup.test", [temp, Path.Combine(nvidia, "DXCache")]);
        Assert.Equal(0, CleanupEngine.Scan(category).Files);
        Assert.Equal(0, CleanupEngine.Clean(category).Deleted);
        Assert.True(System.IO.File.Exists(keep));
        Assert.True(System.IO.File.Exists(keep2));
    }

    [Fact]
    public void BackupFolderLinksAreRemovedWithoutTouchingTargets()
    {
        // A standard user pre-created the backup root (or a subfolder) as a junction to a folder they control.
        var userFiles = Path.Combine(_root, "user");
        var planted = File(@"user\backups\x.json", 10);
        var root = Path.Combine(_root, "PCOptimizer");
        Junction(root, userFiles);
        Optimizer.Core.Backup.SecureFolder.RemoveLinks(root);
        Assert.False(Directory.Exists(root));
        Assert.True(System.IO.File.Exists(planted)); // the target is left alone

        Directory.CreateDirectory(root);
        Junction(Path.Combine(root, "backups"), Path.Combine(userFiles, "backups"));
        Optimizer.Core.Backup.SecureFolder.RemoveLinks(root);
        Assert.False(Directory.Exists(Path.Combine(root, "backups")));
        Assert.True(System.IO.File.Exists(planted));
    }

    [Fact]
    public void SafeDeleteRefusesAPathThatResolvesElsewhere()
    {
        // The scan saw temp\sub\old.tmp; then sub was swapped for a junction to a protected folder with the same file name.
        var victim = File(@"protected\old.tmp", 10);
        Directory.CreateDirectory(Path.Combine(_root, "temp"));
        var sub = Path.Combine(_root, @"temp\sub");
        Junction(sub, Path.Combine(_root, "protected"));
        Assert.Throws<IOException>(() => Optimizer.Core.Platform.SafeDelete.DeleteFile(Path.Combine(sub, "old.tmp")));
        Assert.True(System.IO.File.Exists(victim));
        Assert.False(Optimizer.Core.Platform.SafeDelete.HasNoLinks(Path.Combine(sub, "old.tmp")));

        // A normal read-only file is deleted.
        var ro = File(@"temp\readonly.tmp", 10);
        System.IO.File.SetAttributes(ro, FileAttributes.ReadOnly);
        Optimizer.Core.Platform.SafeDelete.DeleteFile(ro);
        Assert.False(System.IO.File.Exists(ro));
    }

    [Fact]
    public async Task StorageAnalyzerFindsLargestAndDuplicates()
    {
        var a = File(@"docs\a.bin", 2 << 20);
        var b = File(@"backup\a-copy.bin", 2 << 20);
        System.IO.File.WriteAllBytes(Path.Combine(_root, @"docs\different.bin"), [.. new byte[(2 << 20) - 1], 1]);
        File(@"docs\small.txt", 100);
        var report = await StorageAnalyzer.ScanAsync(_root, StorageAnalyzer.ProtectedRoots(null));
        Assert.Equal(4, report.ScannedFiles);
        var dup = Assert.Single(report.Duplicates);
        Assert.Equal(new[] { b, a }.Order(StringComparer.OrdinalIgnoreCase), dup.Paths);
        Assert.Equal(2L << 20, dup.Reclaimable);
        Assert.Contains(report.LargestFolders, f => f.Path.EndsWith("docs", StringComparison.OrdinalIgnoreCase));
        Assert.True(StorageAnalyzer.IsProtected(@"C:\Windows\System32\x.dll", StorageAnalyzer.ProtectedRoots(null)));
        Assert.True(StorageAnalyzer.IsProtected(@"C:\Users\x\AppData\Local\y.db", StorageAnalyzer.ProtectedRoots(null)));
        Assert.False(StorageAnalyzer.IsProtected(a, StorageAnalyzer.ProtectedRoots(null)));
        Assert.True(StorageAnalyzer.IsProtected(a, StorageAnalyzer.ProtectedRoots([Path.Combine(_root, "docs")]))); // game library
    }

    [Fact]
    public void UpdateRepairRenamesAndRestartsServices()
    {
        var win = Path.Combine(_root, "Windows");
        Directory.CreateDirectory(Path.Combine(win, "SoftwareDistribution"));
        Directory.CreateDirectory(Path.Combine(win, @"System32\catroot2"));
        var processes = new FakeProcesses { Handler = (f, a) => (a.StartsWith("stop bits", StringComparison.Ordinal) ? 2 : 0, "") };
        var steps = UpdateRepair.Run(processes, windowsDir: win, now: new DateTime(2026, 10, 9, 12, 0, 0));
        Assert.All(steps, s => Assert.True(s.Ok, s.Key));
        Assert.True(Directory.Exists(Path.Combine(win, "SoftwareDistribution.old-20261009-120000")));
        Assert.True(Directory.Exists(Path.Combine(win, @"System32\catroot2.old-20261009-120000")));
        Assert.Equal(4, processes.Calls.Count(c => c.Contains(" stop ")));
        Assert.Equal(3, processes.Calls.Count(c => c.Contains(" start ")));
        Assert.All(steps, s => Assert.True(Labels.Current.Has("en", s.Key) && Labels.Current.Has("de", s.Key), s.Key));
    }
}

public class M6Tests
{
    private static IEnumerable<string> Csv(params double[] ms) =>
        ["Application,ProcessID,PresentMode,MsBetweenPresents", .. ms.Select(m => $"game.exe,1,Hardware: Independent Flip,{m.ToString(System.Globalization.CultureInfo.InvariantCulture)}")];

    [Fact]
    public void PresentMonStats()
    {
        var times = Enumerable.Repeat(10.0, 990).Concat(Enumerable.Repeat(30.0, 10)).ToArray();
        var s = PresentMon.Parse(Csv(times))!;
        Assert.Equal(1000, s.Frames);
        Assert.Equal(1000 / 10.2, s.AverageFps, 3);      // 10.2 s for 1000 frames
        Assert.InRange(s.OnePercentLowFps, 33.3, 100.0); // p99 lies at the edge of the slow frames
        Assert.Equal("Hardware: Independent Flip", s.PresentMode);
        Assert.Null(PresentMon.Parse(Csv(16.6, 16.6)));  // too few frames
        Assert.True(PresentMon.IsSafeProcessName("cs2.exe"));
        Assert.False(PresentMon.IsSafeProcessName("x.exe\" --output_file C:\\Windows\\a"));
    }

    [Fact]
    public void BenchmarkComparisonNeedsSeparatedRanges()
    {
        FrameStats R(double fps) => new(1000, 10, fps, fps * 0.7, 1000 / (fps * 0.7), null);
        Assert.Equal(Comparison.NotEnoughRuns, PresentMon.Compare([R(100), R(101)], [R(110), R(111), R(112)]).Result);
        Assert.Equal(Comparison.NoMeasurableDifference, PresentMon.Compare([R(100), R(104), R(102)], [R(103), R(106), R(105)]).Result);
        var better = PresentMon.Compare([R(100), R(101), R(102)], [R(108), R(109), R(110)]);
        Assert.Equal(Comparison.Better, better.Result);
        Assert.Equal(7.9, better.ChangePercent, 1); // (109 - 101) / 101
    }

    [Fact]
    public void PresentMonResourceMatchesItsHash()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pco-pm-" + Guid.NewGuid().ToString("N"));
        try
        {
            var exe = PresentMon.Extract(dir);
            Assert.True(File.Exists(exe));
            Assert.Equal(exe, PresentMon.Extract(dir)); // second call keeps the verified file
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void ThrottleSummaryCountsOnlyBusySamples()
    {
        var now = DateTime.UtcNow;
        ThrottleSample S(double limit, double util, ulong reasons = 0) =>
            new(now, limit, 100, util, [new Interop.Nvml.Sample("RTX", 70, 1800, 99, 200, 4, 16, 4, reasons)]);
        var samples = new List<ThrottleSample>();
        samples.AddRange(Enumerable.Range(0, 8).Select(_ => S(100, 90)));
        samples.AddRange(Enumerable.Range(0, 2).Select(_ => S(80, 95, Interop.Nvml.ReasonSwThermal)));
        samples.AddRange(Enumerable.Range(0, 10).Select(_ => S(50, 5))); // idle: ignored for CPU
        var r = ThrottleMonitor.Summarize(samples);
        Assert.Equal(0.2, r.CpuLimitedShare, 3);
        Assert.Equal(80, r.CpuLowestLimit);
        Assert.True(r.CpuThrottled);
        Assert.Equal(0.1, r.GpuReasonShare["thermal"], 3);
        Assert.False(r.GpuThermal); // exactly 10 % is not above the threshold
    }

    private static HardwareProfile P(HardwareExtras extras, bool laptop = false) => new()
    {
        Os = TestData.Os(),
        Extras = extras,
        System = laptop ? new SystemInfo("x", "y", [10], true, true, false) : null,
    };

    private static void AssertRenders(Finding f)
    {
        foreach (var lang in DocStore.Languages)
        {
            var md = DocStore.RenderFinding(DocStore.Get(f.Id, lang)!, f, Labels.Current);
            Assert.DoesNotContain("{{", md);
            Assert.DoesNotContain(":::", md);
            foreach (var fact in f.Facts.Where(x => x.Value.StartsWith('@')))
                Assert.True(Labels.Current.Has(lang, "value." + fact.Value[1..]), fact.Value);
        }
    }

    [Fact]
    public void M5M6FindingsRender()
    {
        var throttle = new ThrottleResult(DateTimeOffset.Now, 120, 0.4, 72, 0.8, new Dictionary<string, double> { ["thermal"] = 0, ["powerLimit"] = 0.9 }, 76);
        var f23 = new ThrottleCheck().Evaluate(P(new HardwareExtras { LastThrottle = throttle }, laptop: true), CatalogData.Current).Single();
        Assert.Equal((FindingStatus.Problem, "cpu"), (f23.Status, f23.Variant));
        AssertRenders(f23);
        var gpuPowerOnly = throttle with { CpuLimitedShare = 0 };
        Assert.Equal(FindingStatus.Info, new ThrottleCheck().Evaluate(P(new HardwareExtras { LastThrottle = gpuPowerOnly }), CatalogData.Current).Single().Status);
        Assert.Empty(new ThrottleCheck().Evaluate(P(new HardwareExtras()), CatalogData.Current));

        var disks = new[] { new DiskHealth("Samsung SSD 970 EVO Plus", "SSD", "NVMe", "Healthy", 40, 70, 93, 0, 0, 12000), new DiskHealth("ST2000", "HDD", "SATA", "Unhealthy", 35, 50, null, 10, 2, 40000) };
        var f28 = new DiskHealthCheck().Evaluate(P(new HardwareExtras { DiskHealth = disks }), CatalogData.Current).ToList();
        Assert.All(f28, f => Assert.Equal(FindingStatus.Problem, f.Status));
        Assert.False(f28[0].Critical);
        Assert.True(f28[1].Critical);
        f28.ForEach(AssertRenders);

        var f16 = new StartupCountCheck().Evaluate(P(new HardwareExtras { StartupPrograms = Enumerable.Range(1, 9).Select(i => $"App {i}").ToList() }), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Info, f16.Status);
        AssertRenders(f16);

        var sample = new[] { new ProcessCpu("MsMpEng", 1, 0.12, 1, null), new ProcessCpu("OneDrive", 2, 0.06, 1, null), new ProcessCpu("dwm", 3, 0.2, 1, null), new ProcessCpu("tiny", 4, 0.01, 1, null) };
        var f11 = new BackgroundCpuCheck().Evaluate(P(new HardwareExtras { BackgroundCpu = sample }), CatalogData.Current).Single();
        Assert.Equal(FindingStatus.Info, f11.Status);
        Assert.Equal("MsMpEng, OneDrive", f11.Params["names"]);
        Assert.Equal("yes", f11.Params["defender"]);
        AssertRenders(f11);
    }
}

public class RuntimeTweakPersistenceTests
{
    [Fact]
    public async Task RuntimeTweakCanBeUndoneAfterRestart()
    {
        using var fx = new EngineFixture();
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Run", "Updater", "string", @"C:\Vendor\updater.exe");
        var entry = new StartupScanner(fx.Registry, fx.Tasks, null).RunKeys().Single();
        var tweak = StartupTweaks.Set(entry, enabled: false)!;
        var facts = new Facts().Set("os.build", 26300).Set("elevated", true);
        await fx.Engine.ApplyAsync(tweak, facts, new HashSet<string>(), new ApplyOptions { ContinueWithoutRestorePoint = true });

        // A new engine on the same backup store = the app after a restart, without the startup page.
        var engine = new TweakEngine(fx.Context, new Backup.BackupStore(fx.BackupRoot, secure: false), fx.RestorePoints, "test", 26300);
        var restored = engine.Resolve(tweak.Id);
        Assert.NotNull(restored);
        Assert.IsType<StartupApprovedAction>(restored!.Actions.Single());
        Assert.Equal(entry.Name, restored.Subject);
        // Also reverts the restore point frequency tweak the engine applied before the first change.
        var all = engine.RevertAll();
        Assert.All(all, r => Assert.True(r.Result.Success));
        Assert.Contains(all, r => r.Tweak.Id == tweak.Id);
        Assert.False(RegistryValue.Read(fx.Registry, Hive.User, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Updater").Existed);
    }

    [Fact]
    public async Task DriftAfterWindowsUpdateIsReportedWithBothVersions()
    {
        using var fx = new EngineFixture();
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\Run", "Updater", "string", @"C:\Vendor\updater.exe");
        var entry = new StartupScanner(fx.Registry, fx.Tasks, null).RunKeys().Single();
        var tweak = StartupTweaks.Set(entry, enabled: false)!;
        var facts = new Facts().Set("os.build", 26300).Set("elevated", true);
        var before = new TweakEngine(fx.Context, new Backup.BackupStore(fx.BackupRoot, secure: false), fx.RestorePoints, "test", 26300, "26300.9000");
        await before.ApplyAsync(tweak, facts, new HashSet<string>(), new ApplyOptions { ContinueWithoutRestorePoint = true });
        Assert.Empty(before.CheckDrift(facts));

        // The update puts the entry back to enabled.
        RegistryValue.Delete(fx.Registry, Hive.User, @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", "Updater");

        var after = new TweakEngine(fx.Context, new Backup.BackupStore(fx.BackupRoot, secure: false), fx.RestorePoints, "test", 26300, "26300.9550");
        var drift = Assert.Single(after.CheckDrift(facts));
        Assert.Equal(tweak.Id, drift.Tweak.Id);
        Assert.Equal("26300.9000", drift.AppliedOn);
        Assert.Equal("26300.9550", drift.Current);
        Assert.True(drift.WindowsUpdatedSince);

        // Same version: reset by something else, not by an update.
        var same = Assert.Single(before.CheckDrift(facts));
        Assert.False(same.WindowsUpdatedSince);
    }

    [Theory]
    [InlineData("26300.9000", "26300.9550", true)]
    [InlineData("26300.9550", "26300.9550", false)]
    [InlineData("26300", "26300.9550", false)] // older backup knows the build only
    [InlineData("26100", "26300.9550", true)]
    [InlineData(null, "26300.9550", false)]
    public void WindowsUpdatedSinceComparesWhatTheBackupKnows(string? appliedOn, string current, bool expected)
    {
        var item = new DriftItem(RuntimeFixes.PowerModeBestPerformance(), new Backup.TweakBackup { TweakId = "x" }, appliedOn, current);
        Assert.Equal(expected, item.WindowsUpdatedSince);
    }

    [Fact]
    public void RuntimeDefinitionsRoundTripThroughJson()
    {
        var defs = new List<TweakDefinition>
        {
            OptionalFeatureAction.Tweak(new FeatureEntry { Name = "SMB1Protocol", Title = "SMB 1.0" }, false),
            RuntimeFixes.PowerModeBestPerformance(),
            RuntimeFixes.NvidiaGlobalReset([Interop.Nvapi.SettingFrameRateLimiter]),
            DeviceTweaks.MsiMode(new MsiDevice(@"PCI\VEN_10DE&DEV_1F02\4&1", "RTX 2070", "gpu", 3, 1, null)),
        };
        foreach (var d in defs)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(d, TweakCatalog.JsonOptions);
            var back = System.Text.Json.JsonSerializer.Deserialize<TweakDefinition>(json, TweakCatalog.JsonOptions)!;
            Assert.Equal(d.Id, back.Id);
            Assert.Equal(d.Actions.Select(a => a.GetType()), back.Actions.Select(a => a.GetType()));
            Assert.Equal(d.Actions.Select(a => a.TargetKey), back.Actions.Select(a => a.TargetKey));
            Assert.Equal(d.IsBootCritical, back.IsBootCritical);
        }
    }
}
