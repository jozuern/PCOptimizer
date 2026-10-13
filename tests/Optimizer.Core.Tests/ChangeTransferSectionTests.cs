using Optimizer.Core.Actions;
using Optimizer.Core.Backup;
using Optimizer.Core.Debloat;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>The opt-in sections of the changes file: removed apps, service start types, startup entries turned off.</summary>
public class ChangeTransferSectionTests
{
    private static readonly TweakCatalog Catalog = TweakCatalog.Current;
    private static readonly Facts Pro = new Facts().Set("os.edition", "Professional").Set("os.build", 26300);

    private static TransferFile WithSections() => ChangeTransfer.Create([], new HashSet<string>(), Catalog, [], "1.0.25", "26300.1",
        new ExportSections(
            Apps: ["Microsoft.BingNews"],
            Services: [new TransferService("VendorUpdater", ServiceStart.Manual), new TransferService("SomeVendorSvc", ServiceStart.Disabled)],
            Startup: [new TransferStartup(StartupKind.RunKey, "Steam")])).File;

    [Fact]
    public void SectionsRoundTripAndOlderFilesStillRead()
    {
        var text = ChangeTransfer.Serialize(WithSections());
        var back = ChangeTransfer.Parse(text)!;
        Assert.Equal(["Microsoft.BingNews"], back.Apps);
        Assert.Equal(2, back.Services!.Count);
        Assert.Equal([new TransferStartup(StartupKind.RunKey, "Steam")], back.Startup);
        Assert.Equal(1, back.Format);

        // Without sections nothing new is written: a file a 1.0.0 copy reads the same way as before.
        var plain = ChangeTransfer.Serialize(ChangeTransfer.Create([], new HashSet<string>(), Catalog, [], "1.0.25", "26300.1").File);
        Assert.DoesNotContain("\"apps\"", plain);
        Assert.DoesNotContain("\"services\"", plain);
        Assert.DoesNotContain("\"count\"", plain);
    }

    [Theory]
    [InlineData("""{"kind":"pcoptimizer-changes","format":1,"appVersion":"x","windowsBuild":"x","created":"2026-10-13T00:00:00+00:00","tweaks":[],"priorities":[],"services":[{"name":"bad name;","start":"Disabled"}]}""")]
    [InlineData("""{"kind":"pcoptimizer-changes","format":1,"appVersion":"x","windowsBuild":"x","created":"2026-10-13T00:00:00+00:00","tweaks":[],"priorities":[],"startup":[{"kind":"RunKey","name":""}]}""")]
    [InlineData("""{"kind":"pcoptimizer-changes","format":1,"appVersion":"x","windowsBuild":"x","created":"2026-10-13T00:00:00+00:00","tweaks":[],"priorities":[],"apps":["../evil"]}""")]
    public void InvalidSectionsAreRejected(string text) => Assert.Null(ChangeTransfer.Parse(text));

    private static ServiceRow Row(string name, ServiceStart start, string? mode, string file) =>
        new(name, name, null, start, false, file, file, mode is null ? null : new ServiceNote { Mode = mode });

    [Fact]
    public void TheTargetsRulesDecide()
    {
        var file = WithSections();
        var targets = new TransferTargets(
            Services:
            [
                // A third-party service: Manual is allowed and safe, so it goes with the others.
                Row("VendorUpdater", ServiceStart.Automatic, null, @"C:\Program Files\Vendor\updater.exe"),
                // A Windows service the catalog does not explain: read-only here, so Disabled is not applied.
                Row("SomeVendorSvc", ServiceStart.Automatic, null, @"C:\Windows\System32\svc.dll"),
            ],
            Startup: [new StartupEntry(StartupKind.RunKey, "Steam", "steam.exe -silent", null, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", Hive.User, true, "run:steam")
                { Target = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run" }],
            Apps: []);
        var plan = ChangeTransfer.Plan(file, Catalog, Pro, new HashSet<string>(), targets);
        Assert.Contains(plan.Together, t => t.Actions.OfType<ServiceAction>().Any(a => a.Name == "VendorUpdater" && a.StartType == ServiceStart.Manual));
        Assert.DoesNotContain(plan.Together.Concat(plan.OneByOne), t => t.Actions.OfType<ServiceAction>().Any(a => a.Name == "SomeVendorSvc"));
        Assert.Contains(plan.Together, t => t.Actions.OfType<StartupApprovedAction>().Any(a => a.Name == "Steam" && !a.Enabled));
        // The removed app is not on this PC, and the Windows service may not be disabled here.
        Assert.Equal(2, plan.NotForThisPc);
        Assert.Empty(plan.Apps);
    }

    [Fact]
    public void WithoutTargetsSectionsAreNotForThisPc()
    {
        var plan = ChangeTransfer.Plan(WithSections(), Catalog, Pro, new HashSet<string>());
        Assert.True(plan.IsEmpty);
        Assert.Equal(4, plan.NotForThisPc);
    }

    [Fact]
    public void SectionsComeFromWhatThisAppChanged()
    {
        var service = ServiceManager.Change(Row("VendorUpdater", ServiceStart.Automatic, null, @"C:\Program Files\Vendor\updater.exe"), ServiceStart.Manual)!;
        var entry = new StartupEntry(StartupKind.RunKey, "Steam", "steam.exe", null, @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run", Hive.User, true, "run:steam")
            { Target = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run" };
        var off = StartupTweaks.Set(entry, enabled: false)!;
        Assert.Equal([new TransferService("VendorUpdater", ServiceStart.Manual)], ChangeTransfer.ServiceSection([service, off]));
        Assert.Equal([new TransferStartup(StartupKind.RunKey, "Steam")], ChangeTransfer.StartupSection([service, off]));
        var catalog = new AppxCatalog { Apps = [new AppxEntry { Name = "Microsoft.BingNews" }] };
        Assert.Equal(["Microsoft.BingNews"], ChangeTransfer.AppSection([new RemovedApp("Microsoft.BingNews", "Microsoft.BingNews_8wekyb3d8bbwe", "1.0", DateTimeOffset.Now)], catalog));
    }
}
