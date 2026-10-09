using System.Management;

namespace Optimizer.Core.Hardware;

/// <summary>Small read-only WMI wrapper. Returns property bags so probes stay testable and free of COM objects.</summary>
public static class Wmi
{
    public static List<Dictionary<string, object?>> Query(string wql, string scope = @"root\cimv2")
    {
        var rows = new List<Dictionary<string, object?>>();
        using var searcher = new ManagementObjectSearcher(new ManagementScope(scope), new ObjectQuery(wql));
        searcher.Options.Timeout = TimeSpan.FromSeconds(20);
        foreach (var obj in searcher.Get())
        {
            using (obj)
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var p in obj.Properties) row[p.Name] = p.Value;
                rows.Add(row);
            }
        }
        return rows;
    }

    public static string Str(this Dictionary<string, object?> row, string name) => row.TryGetValue(name, out var v) ? v?.ToString()?.Trim() ?? "" : "";

    public static long? Long(this Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var v) && v is not null && long.TryParse(v.ToString(), out var l) ? l : null;

    public static int? Int(this Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var v) && v is not null && int.TryParse(v.ToString(), out var i) ? i : null;

    public static bool? Bool(this Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var v) && v is bool b ? b : null;

    public static int[] IntArray(this Dictionary<string, object?> row, string name) =>
        row.TryGetValue(name, out var v) && v is Array a ? a.Cast<object>().Select(o => Convert.ToInt32(o)).ToArray() : [];

    /// <summary>WMI CIM_DATETIME (yyyymmddHHMMSS.mmmmmmsUUU) -> DateTime.</summary>
    public static DateTime? Date(this Dictionary<string, object?> row, string name)
    {
        var s = row.Str(name);
        if (s.Length < 8) return null;
        try
        {
            return ManagementDateTimeConverter.ToDateTime(s);
        }
        catch (Exception)
        {
            return DateTime.TryParseExact(s[..8], "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
        }
    }
}
