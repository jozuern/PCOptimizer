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
