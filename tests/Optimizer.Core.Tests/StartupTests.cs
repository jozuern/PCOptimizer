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
    public void ListedOnlyLocationsAreReadWithTheirFiles()
    {
        using var fx = new EngineFixture();
        var sys = Environment.SystemDirectory;
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "Cleanup", "string", @"C:\Tools\cleanup.exe /once");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Active Setup\Installed Components\{AAAA}", "", "string", "Contoso Setup");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Active Setup\Installed Components\{AAAA}", "StubPath", "string", @"C:\Contoso\setup.exe /user");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Active Setup\Installed Components\{BBBB}", "StubPath", "string", @"C:\Old\old.exe");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Active Setup\Installed Components\{BBBB}", "IsInstalled", "dword", "0");
        RegistryValue.Write(fx.Registry, Hive.User, @"Software\Microsoft\Windows NT\CurrentVersion\Windows", "Load", "string", @"C:\Users\x\evil.exe");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Session Manager", "BootExecute", "multiString", "autocheck autochk *\nC:\\evil\\native.exe");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs", "kernel32", "string", "kernel32.dll");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs", "DllDirectory", "string", "%SystemRoot%\\system32");
        // PackedCatalogItem: the DLL path as a zero-terminated ANSI string, then binary data.
        var packed = Convert.ToHexString([.. System.Text.Encoding.ASCII.GetBytes(@"%SystemRoot%\system32\mswsock.dll"), 0, 0x41, 0x42]);
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\WinSock2\Parameters\Protocol_Catalog9\Catalog_Entries\000000000001", "PackedCatalogItem", "binary", packed);
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Print\Monitors\Local Port", "Driver", "string", "localspl.dll");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\Lsa", "Authentication Packages", "multiString", "msv1_0");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Control\NetworkProvider\Order", "ProviderOrder", "string", "RDPNP,LanmanWorkstation");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\NetworkProvider", "ProviderPath", "string", @"%SystemRoot%\System32\ntlanman.dll");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SYSTEM\CurrentControlSet\Services\LanmanWorkstation\NetworkProvider", "Name", "string", "Microsoft Windows Network");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Drivers32", "msacm.imaadpcm", "string", "imaadp32.acm");
        RegistryValue.Write(fx.Registry, Hive.Machine, @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Drivers32", "MidisrvTransferComplete", "dword", "1");

        var all = new StartupScanner(fx.Registry, fx.Tasks, null).ScanAll(includeServicesAndDrivers: false, includeWmi: false);
        StartupEntry One(StartupKind kind) => Assert.Single(all, e => e.Kind == kind);
        Assert.Equal(@"C:\Tools\cleanup.exe", One(StartupKind.RunOnce).ImagePath);
        Assert.Equal("Contoso Setup", One(StartupKind.ActiveSetup).Name); // {BBBB} is no longer installed
        Assert.True(One(StartupKind.LoadValue).Suspicious);
        var boot = all.Where(e => e.Kind == StartupKind.BootExecute).ToList();
        Assert.False(boot.Single(e => e.Command == StartupScanner.BootExecuteDefault).Suspicious);
        Assert.Equal(Path.Combine(sys, "autochk.exe"), boot.Single(e => e.Command == StartupScanner.BootExecuteDefault).ImagePath);
        Assert.True(boot.Single(e => e.Command!.Contains("native")).Suspicious);
        Assert.Equal(Path.Combine(sys, "kernel32.dll"), One(StartupKind.KnownDll).ImagePath);
        Assert.Equal(Environment.ExpandEnvironmentVariables(@"%SystemRoot%\system32\mswsock.dll"), One(StartupKind.WinsockProvider).ImagePath);
        Assert.Equal(Path.Combine(sys, "localspl.dll"), One(StartupKind.PrintMonitor).ImagePath);
        Assert.Equal(Path.Combine(sys, "msv1_0.dll"), One(StartupKind.LsaPackage).ImagePath);
        var providers = all.Where(e => e.Kind == StartupKind.NetworkProvider).ToList();
        Assert.Equal(["RDPNP", "Microsoft Windows Network"], providers.Select(p => p.Name));
        Assert.Equal(Path.Combine(sys, "imaadp32.acm"), One(StartupKind.Codec).ImagePath);
        // Listed only: none of them can be switched here.
        Assert.All(all.Where(e => e.Kind >= StartupKind.RunOnce), e => Assert.Null(StartupTweaks.Set(e, enabled: false)));
        Assert.Null(StartupScanner.PackedPath([0x01, 0x02, 0x00]));
    }

    [Fact]
    public void SnapshotShowsWhatAnInstallerAddedChangedAndRemoved()
    {
        StartupEntry Run(string name, string command) =>
            new(StartupKind.RunKey, name, command, null, @"HKCU\Run", Hive.User, true, $"run:User:Run:{name}");
        var before = new[] { Run("Discord", "discord.exe"), Run("Steam", "steam.exe -silent"), Run("Old", "old.exe") };
        var snapshot = StartupSnapshot.Take(before, DateTimeOffset.Parse("2026-10-01T10:00:00Z"));

        var folder = TestFolders.Create("snapshot");
        try
        {
            var file = Path.Combine(folder, "startup-snapshot.json");
            StartupSnapshot.Save(file, snapshot);
            var loaded = StartupSnapshot.Load(file)!;
            Assert.Equal(3, loaded.Entries.Count);

            var after = new[] { Run("Discord", "discord.exe"), Run("Steam", "steam.exe -silent -nofriendsui"), Run("Updater", "contoso-update.exe") };
            var diff = StartupSnapshot.Compare(loaded, after);
            Assert.Equal(["run:User:Run:Steam", "run:User:Run:Updater"], diff.NewKeys.Order());
            Assert.Equal(["Old", "Steam"], diff.Removed.Select(r => r.Name).Order());
            Assert.Equal(snapshot.TakenAt, diff.TakenAt);

            File.WriteAllText(file, "{ broken");
            Assert.Null(StartupSnapshot.Load(file));
        }
        finally
        {
            TestFolders.Delete(folder);
        }
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
