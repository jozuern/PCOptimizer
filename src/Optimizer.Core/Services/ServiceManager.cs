using Optimizer.Core.Actions;
using Optimizer.Core.Hardware;
using Optimizer.Core.Startup;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Services;

public sealed class ServiceCatalog
{
    public List<ServiceNote> Services { get; init; } = [];

    public ServiceNote? Find(string name) => Services.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
}

public sealed class ServiceNote
{
    public string Name { get; init; } = "";

    /// <summary>protected | manualOk | warn.</summary>
    public string Mode { get; init; } = "protected";

    public string En { get; init; } = "";
    public string De { get; init; } = "";

    public string Text(string lang) => lang == "de" && De.Length > 0 ? De : En;
}

public enum ServiceEdit { ReadOnly, ManualOnly, Full }

public sealed record ServiceRow(
    string Name,
    string DisplayName,
    string? Description,
    ServiceStart? Start,
    bool Running,
    string? ImagePath,
    string? File,
    ServiceNote? Note)
{
    /// <summary>Filled when the signature check ran.</summary>
    public SignatureInfo? Signature { get; init; }

    /// <summary>
    /// A valid signature decides. Without one (not checked yet, file not found, check failed, not signed), a file in
    /// the Windows folder counts as a Windows service, so a failed check never unlocks Disabled for it.
    /// </summary>
    public bool IsMicrosoft => Signature is { Status: SignatureStatus.Signed } s
        ? s.IsMicrosoft
        : File?.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.Windows), StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// What the manager allows: catalog "protected" and unknown Microsoft services are read-only; Microsoft services the
    /// catalog explains can go to Manual; third-party services can be set to Manual or Disabled.
    /// </summary>
    public ServiceEdit Edit => Note?.Mode switch
    {
        "protected" => ServiceEdit.ReadOnly,
        "manualOk" or "warn" => ServiceEdit.ManualOnly,
        _ => IsMicrosoft ? ServiceEdit.ReadOnly : ServiceEdit.Full,
    };
}

/// <summary>Service list for the services manager: WMI for state and paths, the registry for the exact start type.</summary>
public static class ServiceManager
{
    public static IReadOnlyList<ServiceRow> List(IServiceManager services, ServiceCatalog catalog)
    {
        var rows = new List<ServiceRow>();
        foreach (var s in Wmi.Query("SELECT Name, DisplayName, Description, State, PathName, ServiceType FROM Win32_Service"))
        {
            var name = s.Str("Name");
            if (name.Length == 0) continue;
            var image = s.Str("PathName");
            var file = CommandLine.ImagePath(image);
            if (file is not null && Path.GetFileName(file).Equals("svchost.exe", StringComparison.OrdinalIgnoreCase))
                file = CommandLine.ImagePath(Platform.Reg.HklmValue($@"SYSTEM\CurrentControlSet\Services\{name}\Parameters", "ServiceDll")?.ToString()) ?? file;
            rows.Add(new ServiceRow(name, s.Str("DisplayName"), NullIfEmpty(s.Str("Description")), services.GetStartType(name),
                string.Equals(s.Str("State"), "Running", StringComparison.OrdinalIgnoreCase), NullIfEmpty(image), file, catalog.Find(BaseName(name))));
        }
        return rows.OrderBy(r => r.DisplayName, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>Per-user services have a suffix (CDPUserSvc_1a2b3c); the catalog uses the template name.</summary>
    public static string BaseName(string name)
    {
        var i = name.LastIndexOf('_');
        return i > 0 && name.Length - i - 1 is >= 4 and <= 8 && name[(i + 1)..].All(char.IsAsciiHexDigit) ? name[..i] : name;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    /// <summary>
    /// The service whose start type counts. A per-user service instance (SERVICE_USERSERVICE_INSTANCE, 0x80 in its
    /// type) is created from its template at each sign-in with a new suffix: Windows reads the start type from the
    /// template, so a change and its backup belong there.
    /// </summary>
    public static string StartTypeOwner(string serviceName) =>
        Platform.Reg.HklmInt($@"SYSTEM\CurrentControlSet\Services\{serviceName}", "Type") is { } type && (type & 0x80) != 0 ? BaseName(serviceName) : serviceName;

    /// <summary>
    /// One id per service, whatever start type is chosen: the backup keeps the true original across several changes,
    /// and only the latest choice counts for detection (an earlier choice is not reported as reset by Windows).
    /// </summary>
    public static string ChangeId(string serviceName) => $"service.start.{Tweaks.TweakIds.Slug(serviceName)}";

    /// <summary>Engine tweak for a start type change (backup and undo). Null when the manager does not allow it.</summary>

    public static TweakDefinition? Change(ServiceRow row, ServiceStart start)
    {
        var allowed = row.Edit switch
        {
            ServiceEdit.ManualOnly => start == ServiceStart.Manual,
            ServiceEdit.Full => start is ServiceStart.Manual or ServiceStart.Disabled or ServiceStart.Automatic or ServiceStart.AutomaticDelayed,
            _ => false,
        };
        if (!allowed || row.Start == start) return null;
        var owner = StartTypeOwner(row.Name);
        return new TweakDefinition
        {
            Id = ChangeId(owner),
            Docs = "service.change",
            Subject = row.DisplayName,
            Category = "Services",
            Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Risk = row.Note?.Mode == "warn" || start == ServiceStart.Disabled ? Risk.Moderate : Risk.Safe,
            // Disabling a service nobody reviewed can break the program it belongs to.
            Preview = start == ServiceStart.Disabled,
            Hidden = true,
            Actions = [new ServiceAction { Name = owner, StartType = start }],
            Sources = ["https://learn.microsoft.com/en-us/windows/win32/api/winsvc/nf-winsvc-changeserviceconfigw"],
        };
    }
}
