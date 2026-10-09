using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Optimizer.Core.Actions;
using Optimizer.Core.Interop;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// Real registry roots. "User" = HKU\&lt;SID of the signed-in session user&gt;, never Registry.CurrentUser: under
/// over-the-shoulder elevation or Administrator protection the process's HKCU belongs to another account (plan v4 §4.5).
/// </summary>
public sealed class SystemRegistryRoots(string? userSid) : IRegistryRoots
{
    public string? UserSid { get; } = userSid;

    public RegistryKey? Open(Hive hive, string path, bool writable, bool create = false)
    {
        RegistryKey root;
        if (hive == Hive.Machine)
        {
            root = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        }
        else
        {
            if (UserSid is null) throw new InvalidOperationException("The signed-in user could not be determined; user settings are not changed.");
            root = RegistryKey.OpenBaseKey(RegistryHive.Users, RegistryView.Registry64).OpenSubKey(UserSid, writable)
                   ?? throw new InvalidOperationException($"User hive {UserSid} is not loaded.");
        }
        using (root)
        {
            return create ? root.CreateSubKey(path, writable) : root.OpenSubKey(path, writable);
        }
    }

    public string DisplayRoot(Hive hive) => hive == Hive.Machine ? "HKLM" : $"HKU\\{UserSid}";
}

public sealed class SystemServiceManager : IServiceManager
{
    public ServiceStart? GetStartType(string name)
    {
        var path = $@"SYSTEM\CurrentControlSet\Services\{name}";
        if (Reg.HklmInt(path, "Start") is not { } start) return null;
        return start == 2 && Reg.HklmInt(path, "DelayedAutostart") == 1 ? ServiceStart.AutomaticDelayed : (ServiceStart)start;
    }

    public void SetStartType(string name, ServiceStart start)
    {
        var scm = NativeWrite.OpenSCManager(null, null, NativeWrite.ScManagerConnect);
        if (scm == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError());
        try
        {
            var svc = NativeWrite.OpenService(scm, name, NativeWrite.ServiceChangeConfig | NativeWrite.ServiceQueryConfig);
            if (svc == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Cannot open service {name}");
            try
            {
                var type = start == ServiceStart.AutomaticDelayed ? 2u : (uint)start;
                // ChangeServiceConfig sets the start type; protected services (e.g. WaaSMedicSvc) return access denied.
                if (!NativeWrite.ChangeServiceConfig(svc, NativeWrite.ServiceNoChange, type, NativeWrite.ServiceNoChange, null, null, IntPtr.Zero, null, null, null, null))
                    throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Cannot change start type of {name}");
                if (type == 2)
                {
                    var info = new NativeWrite.SERVICE_DELAYED_AUTO_START_INFO { fDelayedAutostart = start == ServiceStart.AutomaticDelayed ? 1 : 0 };
                    if (!NativeWrite.ChangeServiceConfig2(svc, NativeWrite.ServiceConfigDelayedAutoStartInfo, ref info))
                        throw new Win32Exception(Marshal.GetLastPInvokeError(), $"Cannot change the delayed start of {name}");
                }
            }
            finally
            {
                NativeWrite.CloseServiceHandle(svc);
            }
        }
        finally
        {
            NativeWrite.CloseServiceHandle(scm);
        }
    }
}

public sealed class SystemPowerManager(IProcessRunner processes) : IPowerManager
{
    public Guid ActiveScheme()
    {
        if (Native.PowerGetActiveScheme(IntPtr.Zero, out var ptr) != 0) return Guid.Empty;
        try
        {
            return Marshal.PtrToStructure<Guid>(ptr);
        }
        finally
        {
            Native.LocalFree(ptr);
        }
    }

    public uint? ReadAc(Guid scheme, Guid sub, Guid setting) => Native.PowerReadACValueIndex(IntPtr.Zero, scheme, sub, setting, out var v) == 0 ? v : null;
    public uint? ReadDc(Guid scheme, Guid sub, Guid setting) => Native.PowerReadDCValueIndex(IntPtr.Zero, scheme, sub, setting, out var v) == 0 ? v : null;

    public void WriteAc(Guid scheme, Guid sub, Guid setting, uint value) => Check(NativeWrite.PowerWriteACValueIndex(IntPtr.Zero, scheme, sub, setting, value), "PowerWriteACValueIndex");
    public void WriteDc(Guid scheme, Guid sub, Guid setting, uint value) => Check(NativeWrite.PowerWriteDCValueIndex(IntPtr.Zero, scheme, sub, setting, value), "PowerWriteDCValueIndex");
    public void SetActive(Guid scheme) => Check(NativeWrite.PowerSetActiveScheme(IntPtr.Zero, scheme), "PowerSetActiveScheme");

    public bool SchemeExists(Guid scheme) => Schemes().Any(s => s.Id == scheme);

    public Guid Duplicate(Guid source, string friendlyName)
    {
        var dest = IntPtr.Zero;
        Check(NativeWrite.PowerDuplicateScheme(IntPtr.Zero, source, ref dest), "PowerDuplicateScheme");
        try
        {
            var id = Marshal.PtrToStructure<Guid>(dest);
            var name = Encoding.Unicode.GetBytes(friendlyName + "\0");
            Check(NativeWrite.PowerWriteFriendlyName(IntPtr.Zero, id, IntPtr.Zero, IntPtr.Zero, name, (uint)name.Length), "PowerWriteFriendlyName");
            return id;
        }
        finally
        {
            Native.LocalFree(dest);
        }
    }

    public void Delete(Guid scheme) => Check(NativeWrite.PowerDeleteScheme(IntPtr.Zero, scheme), "PowerDeleteScheme");

    public IReadOnlyList<(Guid Id, string Name)> Schemes()
    {
        var list = new List<(Guid, string)>();
        for (uint i = 0; ; i++)
        {
            var buffer = new byte[16];
            uint size = 16;
            if (NativeWrite.PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, NativeWrite.AccessScheme, i, buffer, ref size) != 0) break;
            var id = new Guid(buffer);
            list.Add((id, FriendlyName(id)));
        }
        return list;
    }

    /// <summary>Throws when powercfg fails, so the backup never lists an export file that does not exist.</summary>
    public void Export(Guid scheme, string file)
    {
        var (code, output) = processes.Run("powercfg.exe", $"/export \"{file}\" {scheme}");
        if (code != 0 || !File.Exists(file)) throw new InvalidOperationException($"powercfg /export failed ({code}): {output}");
    }

    private static string FriendlyName(Guid scheme)
    {
        uint size = 0;
        Native.PowerReadFriendlyName(IntPtr.Zero, scheme, IntPtr.Zero, IntPtr.Zero, null, ref size);
        if (size == 0) return "";
        var buffer = new byte[size];
        return Native.PowerReadFriendlyName(IntPtr.Zero, scheme, IntPtr.Zero, IntPtr.Zero, buffer, ref size) == 0
            ? Encoding.Unicode.GetString(buffer).TrimEnd('\0')
            : "";
    }

    private static void Check(uint result, string api)
    {
        if (result != 0) throw new Win32Exception((int)result, $"{api} failed");
    }
}

/// <summary>bcdedit wrapper. Element identifiers are read from "/enum {current} /v" (identifiers are not localized).</summary>
public sealed class SystemBcdStore(IProcessRunner processes) : IBcdStore
{
    public IReadOnlyDictionary<string, string> CurrentValues()
    {
        var (code, output) = processes.Run("bcdedit.exe", "/enum {current} /v");
        // bcdedit needs admin rights; failing loudly makes BCD tweaks "unknown" instead of wrongly "applied".
        if (code != 0) throw new InvalidOperationException($"bcdedit /enum failed ({code})");
        return Parse(output);
    }

    /// <summary>"element    value" lines; the localized header lines start with a capital letter and are skipped.</summary>
    public static IReadOnlyDictionary<string, string> Parse(string output)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in output.Split('\n').Select(l => l.Trim()))
        {
            if (line.Length == 0 || !char.IsAsciiLetterLower(line[0])) continue;
            var parts = line.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries);
            map.TryAdd(parts[0], parts.Length > 1 ? parts[1].Trim() : "");
        }
        return map;
    }

    public void Set(string element, string value) => Run($"/set {{current}} {element} {value}");
    public void Delete(string element) => Run($"/deletevalue {{current}} {element}");
    public void Export(string file) => Run($"/export \"{file}\"");

    private void Run(string args)
    {
        var (code, output) = processes.Run("bcdedit.exe", args);
        if (code != 0) throw new InvalidOperationException($"bcdedit {args} failed ({code}): {output}");
    }
}

/// <summary>Task Scheduler 2.0 via its COM API (late-bound).</summary>
public sealed class SystemTaskScheduler : ITaskScheduler
{
    public bool? IsEnabled(string path)
    {
        try
        {
            return (bool)GetTask(path).Enabled;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public void SetEnabled(string path, bool enabled) => GetTask(path).Enabled = enabled;

    // TASK_TRIGGER_BOOT = 8, TASK_TRIGGER_LOGON = 9, TASK_ACTION_EXEC = 0 (taskschd.h)
    private const int TriggerBoot = 8, TriggerLogon = 9, ActionExec = 0;

    public IReadOnlyList<ScheduledTaskInfo> List()
    {
        var list = new List<ScheduledTaskInfo>();
        var type = Type.GetTypeFromProgID("Schedule.Service") ?? throw new InvalidOperationException("Task Scheduler not available");
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        var stack = new Stack<dynamic>();
        stack.Push(service.GetFolder("\\"));
        while (stack.Count > 0)
        {
            var folder = stack.Pop();
            try
            {
                foreach (var sub in folder.GetFolders(0)) stack.Push(sub);
                foreach (var task in folder.GetTasks(1 /* TASK_ENUM_HIDDEN */))
                {
                    try
                    {
                        list.Add(Describe(task));
                    }
                    catch (Exception ex)
                    {
                        Log.Warn("tasks", $"unreadable task: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn("tasks", $"unreadable folder: {ex.Message}");
            }
        }
        return list;
    }

    private static ScheduledTaskInfo Describe(dynamic task)
    {
        var definition = task.Definition;
        bool logon = false, boot = false;
        foreach (var trigger in definition.Triggers)
        {
            int t = trigger.Type;
            if (t == TriggerLogon) logon = true;
            if (t == TriggerBoot) boot = true;
        }
        string? command = null, arguments = null;
        foreach (var action in definition.Actions)
        {
            if ((int)action.Type != ActionExec) continue;
            command = action.Path;
            arguments = action.Arguments;
            break;
        }
        DateTime lastRun = task.LastRunTime;
        return new ScheduledTaskInfo((string)task.Path, (bool)task.Enabled, (string?)definition.RegistrationInfo.Author, command, arguments, logon, boot,
            lastRun.Year < 2000 ? null : lastRun);
    }

    private static dynamic GetTask(string path)
    {
        var type = Type.GetTypeFromProgID("Schedule.Service") ?? throw new InvalidOperationException("Task Scheduler not available");
        dynamic service = Activator.CreateInstance(type)!;
        service.Connect();
        var i = path.LastIndexOf('\\');
        var folder = service.GetFolder(i <= 0 ? "\\" : path[..i]);
        return folder.GetTask(path[(i + 1)..]);
    }
}

public sealed class SystemDisplayManager : IDisplayManager
{
    public int? CurrentRefresh(string gdiName)
    {
        var dm = new Native.DEVMODE { dmDeviceName = "", dmFormName = "", dmSize = (ushort)Marshal.SizeOf<Native.DEVMODE>() };
        return Native.EnumDisplaySettingsEx(gdiName, -1 /* ENUM_CURRENT_SETTINGS */, ref dm, 0) ? (int)dm.dmDisplayFrequency : null;
    }

    public void SetMode(string gdiName, int width, int height, int refreshHz)
    {
        var dm = new Native.DEVMODE { dmDeviceName = "", dmFormName = "", dmSize = (ushort)Marshal.SizeOf<Native.DEVMODE>() };
        if (!Native.EnumDisplaySettingsEx(gdiName, -1, ref dm, 0)) throw new InvalidOperationException($"Display {gdiName} not found");
        dm.dmPelsWidth = (uint)width;
        dm.dmPelsHeight = (uint)height;
        dm.dmDisplayFrequency = (uint)refreshHz;
        dm.dmFields = NativeWrite.DmPelsWidth | NativeWrite.DmPelsHeight | NativeWrite.DmDisplayFrequency;
        var result = NativeWrite.ChangeDisplaySettingsEx(gdiName, ref dm, IntPtr.Zero, NativeWrite.CdsUpdateRegistry, IntPtr.Zero);
        if (result is not (NativeWrite.DispChangeSuccessful or NativeWrite.DispChangeRestart))
            throw new InvalidOperationException($"ChangeDisplaySettingsEx failed ({result})");
    }
}

/// <summary>Runs a system tool without a window and logs the command line, exit code and output.</summary>
public sealed class SystemProcessRunner : IProcessRunner
{
    public (int ExitCode, string Output) Run(string file, string arguments, TimeSpan? timeout = null)
    {
        var path = Path.IsPathRooted(file) ? file : Path.Combine(Environment.SystemDirectory, file);
        if (file.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase))
            path = Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");
        var psi = new ProcessStartInfo(path, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
        };
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"Cannot start {file}");
        var stdout = p.StandardOutput.ReadToEndAsync();
        var stderr = p.StandardError.ReadToEndAsync();
        if (!p.WaitForExit(timeout ?? TimeSpan.FromSeconds(60)))
        {
            p.Kill(true);
            throw new TimeoutException($"{file} {arguments} timed out");
        }
        var output = stdout.Result + stderr.Result;
        Log.Info("command", $"{file} {arguments}", new { exitCode = p.ExitCode, output = output.Length > 2000 ? output[..2000] : output });
        return (p.ExitCode, output);
    }
}

public static class SystemNotify
{
    /// <summary>Live refresh after user-scope changes. Mouse values: SPI without SPIF_UPDATEINIFILE (the hive was written directly).</summary>
    public static void Handle(IRegistryRoots registry, string what)
    {
        try
        {
            if (what == "mouse")
            {
                int Get(string name) => int.TryParse(RegistryValue.Read(registry, Hive.User, @"Control Panel\Mouse", name).Data, out var v) ? v : 0;
                NativeWrite.SystemParametersInfo(NativeWrite.SpiSetMouse, 0, [Get("MouseThreshold1"), Get("MouseThreshold2"), Get("MouseSpeed")], 0);
            }
            NativeWrite.SendMessageTimeout((IntPtr)NativeWrite.HwndBroadcast, NativeWrite.WmSettingChange, IntPtr.Zero, "Environment",
                NativeWrite.SmtoAbortIfHung, 1000, out _);
        }
        catch (Exception ex)
        {
            Log.Warn("notify", $"live refresh '{what}' failed: {ex.Message}");
        }
    }

    public static ActionContext CreateContext(string? userSid, string exportFolder, IReadOnlyList<string>? networkInterfaceIds = null)
    {
        var processes = new SystemProcessRunner();
        var registry = new SystemRegistryRoots(userSid);
        return new ActionContext
        {
            Registry = registry,
            Services = new SystemServiceManager(),
            Power = new SystemPowerManager(processes),
            Bcd = new SystemBcdStore(processes),
            Tasks = new SystemTaskScheduler(),
            Displays = new SystemDisplayManager(),
            Processes = processes,
            PowerMode = new SystemPowerModeManager(),
            Devices = new SystemDeviceManager(),
            Network = new SystemNetworkManager(),
            Nvidia = new SystemNvidiaSettings(),
            Notify = what => Handle(registry, what),
            ExportFolder = exportFolder,
            NetworkInterfaceIds = networkInterfaceIds ?? [],
        };
    }
}
