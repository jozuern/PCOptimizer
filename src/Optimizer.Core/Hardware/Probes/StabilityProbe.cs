using System.Diagnostics;
using Optimizer.Core.Platform;
using System.Globalization;
using System.Xml.Linq;

namespace Optimizer.Core.Hardware.Probes;

/// <param name="HypervisorPresent">Win32_ComputerSystem.HypervisorPresent: Windows runs a hypervisor (memory integrity, Hyper-V).</param>
/// <param name="CpuSupport">Win32_Processor.VMMonitorModeExtensions: the processor has Intel VT-x or AMD-V.</param>
/// <param name="FirmwareEnabled">Win32_Processor.VirtualizationFirmwareEnabled: the firmware turned the extensions on.</param>
public sealed record VirtualizationInfo(bool HypervisorPresent, bool CpuSupport, bool FirmwareEnabled)
{
    /// <summary>
    /// With a hypervisor running, Windows reports both processor flags as false (the hypervisor hides them), so a running
    /// hypervisor alone proves virtualization is on.
    /// </summary>
    public bool Enabled => HypervisorPresent || FirmwareEnabled;
}

/// <summary>One Kernel-Power event 41. BugcheckCode is the Stop error (0 = none recorded); PowerButton = held down to turn off.</summary>
public sealed record UnexpectedShutdown(DateTime Time, uint BugcheckCode, bool PowerButton);

/// <summary>Read-only stability data: virtualization (WMI) and unexpected shutdowns (System event log via wevtutil).</summary>
public static class StabilityProbe
{
    public static VirtualizationInfo ReadVirtualization()
    {
        var cpu = Wmi.Query("SELECT VMMonitorModeExtensions, VirtualizationFirmwareEnabled FROM Win32_Processor").FirstOrDefault() ?? [];
        var cs = Wmi.Query("SELECT HypervisorPresent FROM Win32_ComputerSystem").FirstOrDefault() ?? [];
        return new VirtualizationInfo(cs.Bool("HypervisorPresent") == true, cpu.Bool("VMMonitorModeExtensions") == true, cpu.Bool("VirtualizationFirmwareEnabled") == true);
    }

    /// <summary>Kernel-Power 41 of the last 30 days, at most 50, newest first.</summary>
    public static IReadOnlyList<UnexpectedShutdown> ReadUnexpectedShutdowns()
    {
        var wevtutil = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "wevtutil.exe");
        var start = new ProcessStartInfo(wevtutil)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        ProcessHardening.Apply(start);
        foreach (var arg in new[]
                 {
                     "qe", "System",
                     "/q:*[System[Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=41) and TimeCreated[timediff(@SystemTime) <= 2592000000]]]",
                     "/c:50", "/rd:true", "/f:xml",
                 })
            start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("wevtutil did not start");
        var output = process.StandardOutput.ReadToEndAsync();
        if (!process.WaitForExit(15_000))
        {
            ProcessHardening.KillTree(process);
            throw new TimeoutException("wevtutil did not finish");
        }
        if (process.ExitCode != 0) throw new InvalidOperationException($"wevtutil exit code {process.ExitCode}");
        return Parse(output.GetAwaiter().GetResult());
    }

    /// <summary>wevtutil /f:xml prints the events one after another without a root element.</summary>
    public static IReadOnlyList<UnexpectedShutdown> Parse(string xmlEvents)
    {
        if (string.IsNullOrWhiteSpace(xmlEvents)) return [];
        XNamespace ns = "http://schemas.microsoft.com/win/2004/08/events/event";
        var root = XElement.Parse($"<Events>{xmlEvents}</Events>");
        var list = new List<UnexpectedShutdown>();
        foreach (var e in root.Elements(ns + "Event"))
        {
            var time = e.Element(ns + "System")?.Element(ns + "TimeCreated")?.Attribute("SystemTime")?.Value;
            if (!DateTime.TryParse(time, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var when)) continue;
            string? Data(string name) => e.Element(ns + "EventData")?.Elements(ns + "Data").FirstOrDefault(d => (string?)d.Attribute("Name") == name)?.Value;
            var bugcheck = uint.TryParse(Data("BugcheckCode"), out var b) ? b : 0;
            var button = ulong.TryParse(Data("PowerButtonTimestamp"), out var p) && p != 0;
            list.Add(new UnexpectedShutdown(when, bugcheck, button));
        }
        return list.OrderByDescending(s => s.Time).ToList();
    }
}
