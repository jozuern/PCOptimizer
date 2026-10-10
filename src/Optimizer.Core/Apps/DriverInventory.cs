using System.Globalization;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Apps;

public sealed record DriverRow(string Device, string Class, string? Provider, string? Manufacturer, string? Version, DateTime? Date, string? Inf, string DeviceId)
{
    public int? AgeDays(DateTime today) => Date is { } d ? (int)(today - d.Date).TotalDays : null;

    /// <summary>Third-party driver package (oemNN.inf) rather than one that ships with Windows.</summary>
    public bool IsOem => Inf?.StartsWith("oem", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Drivers that matter for gaming (graphics, network, audio, storage controllers, Bluetooth, chipset packages) with
/// their age and where to update them. Read-only; the app never installs drivers itself.
/// </summary>
public static class DriverInventory
{
    private static readonly string[] Classes = ["DISPLAY", "NET", "MEDIA", "SCSIADAPTER", "HDC", "BLUETOOTH", "SYSTEM"];

    public static IReadOnlyList<DriverRow> Read()
    {
        var where = string.Join(" OR ", Classes.Select(c => $"DeviceClass = '{c}'"));
        var rows = new List<DriverRow>();
        foreach (var r in Wmi.Query($"SELECT DeviceName, DeviceClass, DriverProviderName, Manufacturer, DriverVersion, DriverDate, InfName, DeviceID FROM Win32_PnPSignedDriver WHERE {where}"))
        {
            var name = r.Str("DeviceName");
            var cls = r.Str("DeviceClass");
            var inf = r.Str("InfName");
            if (name.Length == 0) continue;
            // SYSTEM has hundreds of inbox devices; only third-party packages (chipset, ME, serial IO) are interesting there.
            if (cls == "SYSTEM" && !inf.StartsWith("oem", StringComparison.OrdinalIgnoreCase)) continue;
            // Virtual adapters (VPN, Hyper-V) are not hardware.
            var id = r.Str("DeviceID");
            if (cls == "NET" && !(id.StartsWith(@"PCI\", StringComparison.OrdinalIgnoreCase) || id.StartsWith(@"USB\", StringComparison.OrdinalIgnoreCase))) continue;
            rows.Add(new DriverRow(name, cls, NullIfEmpty(r.Str("DriverProviderName")), NullIfEmpty(r.Str("Manufacturer")), NullIfEmpty(r.Str("DriverVersion")),
                ParseCimDate(r.Str("DriverDate")), NullIfEmpty(inf), id));
        }
        return rows.DistinctBy(d => (d.Device, d.Version)).OrderBy(d => Array.IndexOf(Classes, d.Class)).ThenBy(d => d.Device, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>Driver dates are calendar dates ("20240517000000.******+000"): take the date part, no time zone shift.</summary>
    public static DateTime? ParseCimDate(string? cim) =>
        cim is { Length: >= 8 } && DateTime.TryParseExact(cim[..8], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;

    /// <summary>Where to get a newer driver: the chip vendor for graphics, else the mainboard or laptop maker.</summary>
    public static string? UpdateUrl(DriverRow d, string? boardSupportUrl)
    {
        var who = $"{d.Provider} {d.Manufacturer}";
        if (d.Class == "DISPLAY")
        {
            if (who.Contains("NVIDIA", StringComparison.OrdinalIgnoreCase)) return "https://www.nvidia.com/en-us/drivers/";
            if (who.Contains("AMD", StringComparison.OrdinalIgnoreCase) || who.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
                return "https://www.amd.com/en/support/download/drivers.html";
            if (who.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "https://www.intel.com/content/www/us/en/support/detect.html";
        }
        if (who.Contains("Intel", StringComparison.OrdinalIgnoreCase)) return "https://www.intel.com/content/www/us/en/support/detect.html";
        if (who.Contains("AMD", StringComparison.OrdinalIgnoreCase) || who.Contains("Advanced Micro Devices", StringComparison.OrdinalIgnoreCase))
            return "https://www.amd.com/en/support/download/drivers.html";
        return string.IsNullOrEmpty(boardSupportUrl) ? null : boardSupportUrl;
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
