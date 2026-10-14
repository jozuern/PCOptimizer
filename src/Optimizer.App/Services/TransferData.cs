using Optimizer.Core.Backup;
using Optimizer.Core.Catalog;
using Optimizer.Core.Debloat;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tweaks;

namespace Optimizer.App.Services;

/// <summary>
/// What the opt-in sections of the changes file need from this PC, for the window and the command line alike: the
/// sections for an export, and for an import what this PC has (only for the sections the file brings). All of it reads
/// the system for seconds: call it off the UI thread.
/// </summary>
public static class TransferData
{
    /// <summary>The chosen sections: removed reviewed apps, and the service and startup changes of this app that are in place.</summary>
    public static ExportSections Sections(AppServices services, Facts facts, bool apps, bool serviceTypes, bool startup)
    {
        var applied = serviceTypes || startup
            ? services.Store.All().Select(services.Engine.Resolve).OfType<TweakDefinition>()
                .Where(t => services.Engine.DetectState(t, facts) is TweakState.Applied or TweakState.PendingRestart).ToList()
            : [];
        return new ExportSections(
            apps ? ChangeTransfer.AppSection(new DebloatService(services.Context.Processes, AppServices.DataFolder).Removed(), CatalogData.Current.Appx) : null,
            serviceTypes ? ChangeTransfer.ServiceSection(applied) : null,
            startup ? ChangeTransfer.StartupSection(applied) : null);
    }

    /// <summary>
    /// What this PC has for the file's sections; null when the file has none. A part that cannot be read (Get-AppxPackage
    /// failed) stays null, so its entries count as not for this PC and the rest of the file still applies.
    /// </summary>
    public static TransferTargets? Targets(AppServices services, TransferFile file, HardwareProfile? profile)
    {
        if (file.Apps is null && file.Services is null && file.Startup is null) return null;
        // The services also for a startup entry of a service: the import keeps to the Services page's rules for it.
        var rows = file.Services is { Count: > 0 } || file.Startup?.Any(s => s.Kind == StartupKind.Service) == true
            ? Read("services", () => ServiceManager.List(services.Context.Services, CatalogData.Current.Services)) : null;
        var entries = file.Startup is { Count: > 0 }
            ? Read("startup", () => new StartupScanner(services.Context.Registry, services.Context.Tasks, services.ProfilePath).ScanAll()) : null;
        var apps = file.Apps is { Count: > 0 } && profile is not null
            ? Read("apps", () => DebloatService.Offer(CatalogData.Current.Appx,
                new DebloatService(services.Context.Processes, AppServices.DataFolder).ListInstalled(allUsers: services.Elevation.IsElevated), profile, CatalogData.Current))
            : null;
        return new TransferTargets(rows, entries, apps);
    }

    private static IReadOnlyList<T>? Read<T>(string part, Func<IReadOnlyList<T>> read)
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.IO.IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Warn("transfer", $"{part} not read for the import: {ex.Message}");
            return null;
        }
    }
}
