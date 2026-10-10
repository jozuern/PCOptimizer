using Optimizer.Core.Platform;
using Optimizer.Core.Tools;

namespace Optimizer.Core.Tests;

/// <summary>Quick fixes run only Windows tools from System32, in order, and stop at the first failing step.</summary>
public class QuickFixTests
{
    [Fact]
    public void EveryStepIsAWindowsTool()
    {
        foreach (var fix in QuickFixes.All)
        foreach (var step in fix.Steps)
            Assert.True(File.Exists(ProcessHardening.ResolveSystemTool(step.File)), $"{fix.Id}: {step.File} is not in System32");
    }

    [Fact]
    public void IdsAreUniqueAndSourcesAreLinks()
    {
        Assert.Equal(QuickFixes.All.Count, QuickFixes.All.Select(f => f.Id).Distinct().Count());
        Assert.All(QuickFixes.All, f => Assert.StartsWith("https://learn.microsoft.com/", f.Source));
    }

    [Fact]
    public void StepsRunInOrderAndStopAtTheFirstFailure()
    {
        var runner = new FakeProcesses();
        var (ok, _) = QuickFixes.Run(runner, QuickFixes.Get("IpRenew"));
        Assert.True(ok);
        Assert.Equal(["ipconfig.exe /release", "ipconfig.exe /renew"], runner.Calls);

        var failing = new FakeProcesses { Handler = (file, _) => file == "ipconfig.exe" ? (1, "no adapter") : null };
        var (ok2, output) = QuickFixes.Run(failing, QuickFixes.Get("IpRenew"));
        Assert.False(ok2);
        Assert.Equal("no adapter", output);
        Assert.Single(failing.Calls);
    }

    [Fact]
    public void DiskScanChecksTheSystemDriveOnline() =>
        Assert.Equal(Path.GetPathRoot(Environment.SystemDirectory)!.TrimEnd('\\') + " /scan", QuickFixes.Get("DiskScan").Steps.Single().Arguments);
}

/// <summary>DoH auto-upgrade through the documented cmdlets, with each server's previous state restored on undo.</summary>
public class DohTests
{
    private sealed class DohState
    {
        public Dictionary<string, bool> Servers { get; } = new() { ["1.1.1.1"] = false, ["8.8.8.8"] = true, ["9.9.9.9"] = false };

        public (int, string)? Handle(string file, string args)
        {
            if (args.Contains("Get-DnsClientDohServerAddress"))
                return (0, System.Text.Json.JsonSerializer.Serialize(Servers.Select(s => new { ServerAddress = s.Key, AutoUpgrade = s.Value })));
            if (!args.Contains("Set-DnsClientDohServerAddress")) return null;
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(args, @"-ServerAddress '([^']+)' -AutoUpgrade \$(true|false)"))
                Servers[m.Groups[1].Value] = m.Groups[2].Value == "true";
            return (0, "");
        }
    }

    [Fact]
    public async Task AutoUpgradeIsTurnedOnAndEachServerRestored()
    {
        using var fx = new EngineFixture();
        var state = new DohState();
        fx.Processes.Handler = state.Handle;
        var t = Tweaks.TweakCatalog.Current.Tweaks.Single(x => x.Id == "network.dohAutoUpgrade");
        var action = Assert.IsType<Actions.DohAutoUpgradeAction>(Assert.Single(t.Actions));
        var only = new Tweaks.TweakDefinition
        {
            Id = "test.doh", Category = "Test", Hidden = true, Impact = new Tweaks.ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Actions = [new Actions.DohAutoUpgradeAction { Servers = ["1.1.1.1", "8.8.8.8", "9.9.9.9"] }],
        };
        var facts = new Tweaks.Facts().Set("os.build", 26300).Set("elevated", true);
        var options = new Tweaks.ApplyOptions { ExpertMode = true, ContinueWithoutRestorePoint = true };
        Assert.Equal(Tweaks.ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(only, facts, new HashSet<string>(), options)).Outcome);
        Assert.All(state.Servers.Values, Assert.True);
        Assert.True(fx.Engine.Revert(only).Success);
        Assert.False(state.Servers["1.1.1.1"]);
        Assert.True(state.Servers["8.8.8.8"]); // was on before: stays on
        Assert.False(state.Servers["9.9.9.9"]);

        // A server Windows does not know: the tweak is unsupported instead of failing later.
        state.Servers.Remove("9.9.9.9");
        Assert.Null(only.Actions[0].Read(fx.Context));
        Assert.Contains("149.112.112.112", action.Servers);
        Assert.Equal("1.1.1.1=1;8.8.8.8=0", Actions.DohAutoUpgradeAction.Format(new Dictionary<string, bool> { ["8.8.8.8"] = false, ["1.1.1.1"] = true }));
    }
}

/// <summary>Windows capabilities through DISM: English state names, add and remove, undo.</summary>
public class CapabilityTests
{
    [Fact]
    public async Task CapabilityIsRemovedAndAddedBack()
    {
        using var fx = new EngineFixture();
        var installed = new HashSet<string> { "OpenSSH.Client~~~~0.0.1.0" };
        fx.Processes.Handler = (file, args) =>
        {
            if (file != "dism.exe") return null;
            var name = System.Text.RegularExpressions.Regex.Match(args, "/CapabilityName:(\\S+)").Groups[1].Value;
            if (args.Contains("/Get-Capabilities"))
                return (0, "Capability Identity | State\n------------------- | -----\nOpenSSH.Client~~~~0.0.1.0 | " + (installed.Contains("OpenSSH.Client~~~~0.0.1.0") ? "Installed" : "Not Present") + "\nWMIC~~~~ | Not Present\n");
            if (args.Contains("/Get-CapabilityInfo")) return (0, $"Capability Identity : {name}\nState : {(installed.Contains(name) ? "Installed" : "Not Present")}\n");
            if (args.Contains("/Remove-Capability")) installed.Remove(name);
            if (args.Contains("/Add-Capability")) installed.Add(name);
            return (0, "The operation completed successfully.");
        };
        var all = OptionalCapabilityAction.ReadAll(fx.Processes);
        Assert.Equal("Enabled", all["OpenSSH.Client~~~~0.0.1.0"]);
        Assert.Equal("Disabled", all["WMIC~~~~"]);

        var entry = Catalog.CatalogData.Current.Features.Features.Single(f => f.Name == "OpenSSH.Client~~~~0.0.1.0");
        Assert.True(entry.Capability);
        var off = OptionalCapabilityAction.Tweak(entry, installed: false);
        var facts = new Tweaks.Facts().Set("os.build", 26300).Set("elevated", true);
        var options = new Tweaks.ApplyOptions { ExpertMode = true, ContinueWithoutRestorePoint = true };
        Assert.Equal(Tweaks.ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(off, facts, new HashSet<string>(), options)).Outcome);
        Assert.DoesNotContain("OpenSSH.Client~~~~0.0.1.0", installed);
        Assert.True(fx.Engine.Revert(off).Success);
        Assert.Contains("OpenSSH.Client~~~~0.0.1.0", installed);

        // Reserved storage: off and back on; DISM refusing while an update uses the space is a failed apply.
        var reserved = true;
        var inUse = false;
        fx.Processes.Handler = (file, args) =>
        {
            if (args.Contains("/Get-ReservedStorageState")) return (0, $"Reserved storage is {(reserved ? "enabled" : "disabled")}.");
            if (!args.Contains("/Set-ReservedStorageState")) return null;
            if (inUse) return (1, "This operation is not supported when reserved storage is in use.");
            reserved = args.Contains("/State:Enabled");
            return (0, "The operation completed successfully.");
        };
        var rs = Tweaks.TweakCatalog.Current.Tweaks.Single(x => x.Id == "storage.reservedStorageOff");
        Assert.Equal(Tweaks.ApplyOutcome.Applied, (await fx.Engine.ApplyAsync(rs, facts, new HashSet<string>(), options)).Outcome);
        Assert.False(reserved);
        Assert.True(fx.Engine.Revert(rs).Success);
        Assert.True(reserved);
        inUse = true;
        Assert.Equal(Tweaks.ApplyOutcome.Failed, (await fx.Engine.ApplyAsync(rs, facts, new HashSet<string>(), options)).Outcome);
        Assert.True(reserved);

        Assert.Null(OptionalCapabilityAction.MapState("Unknown"));
        Assert.Equal("Enabled", OptionalCapabilityAction.MapState("Install Pending"));
        Assert.False(OptionalCapabilityAction.IsSafeName("x; rm"));
        Assert.All(Catalog.CatalogData.Current.Features.Features.Where(f => f.Capability), f => Assert.True(OptionalCapabilityAction.IsSafeName(f.Name), f.Name));
    }
}
