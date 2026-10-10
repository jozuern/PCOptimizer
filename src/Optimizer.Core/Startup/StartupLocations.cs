using System.Text;
using Optimizer.Core.Actions;

namespace Optimizer.Core.Startup;

/// <summary>
/// Autostart locations that are only listed (Autoruns-style): RunOnce, Active Setup, the Load value, Boot Execute,
/// KnownDLLs, Winsock providers, print monitors, LSA packages, network providers, codecs and WMI event consumers.
/// The program that owns an entry changes it; the page shows where it is and whether the file is signed.
/// </summary>
public sealed partial class StartupScanner
{
    private static string Root(Hive hive) => hive == Hive.Machine ? "HKLM" : "HKCU";

    /// <summary>
    /// A DLL named without a folder ("localspl.dll", "msv1_0") is loaded from System32; for the 32-bit registrations
    /// (WOW6432Node, the 32-bit Winsock catalog) from SysWOW64, and a path into System32 is redirected there too.
    /// </summary>
    public static string? SystemFile(string? name, bool addDll = true, bool wow64 = false)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var expanded = Environment.ExpandEnvironmentVariables(name.Trim().Trim('"'));
        var system32 = Environment.SystemDirectory;
        var syswow64 = Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
        if (Path.IsPathRooted(expanded))
            return wow64 && syswow64.Length > 0 && expanded.StartsWith(system32 + "\\", StringComparison.OrdinalIgnoreCase)
                ? Path.Combine(syswow64, expanded[(system32.Length + 1)..])
                : expanded;
        if (addDll && !Path.HasExtension(expanded)) expanded += ".dll";
        return Path.Combine(wow64 && syswow64.Length > 0 ? syswow64 : system32, expanded);
    }

    private static string[] Strings(object? value, bool split) => value switch
    {
        string[] multi => multi,
        string single when split => single.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries),
        string single => [single],
        _ => [],
    };

    /// <summary>RunOnce runs a command once at the next sign-in; RunOnceEx is used by installers.</summary>
    public IEnumerable<StartupEntry> RunOnceEntries()
    {
        foreach (var (hive, path) in new[]
                 {
                     (Hive.Machine, @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"),
                     (Hive.Machine, @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce"),
                     (Hive.User, @"Software\Microsoft\Windows\CurrentVersion\RunOnce"),
                 })
        {
            using var key = Open(hive, path);
            foreach (var name in key?.GetValueNames().Where(n => n.Length > 0) ?? [])
            {
                var command = key!.GetValue(name, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
                yield return new StartupEntry(StartupKind.RunOnce, name, command, CommandLine.ImagePath(command), $@"{Root(hive)}\{path}", hive, null, $"runonce:{hive}:{path}:{name}");
            }
        }
        const string ex = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx";
        using var exKey = Open(Hive.Machine, ex);
        foreach (var sub in exKey?.GetSubKeyNames() ?? [])
        {
            using var s = exKey!.OpenSubKey(sub);
            foreach (var name in s?.GetValueNames().Where(n => n.Length > 0) ?? [])
            {
                var command = s!.GetValue(name)?.ToString();
                yield return new StartupEntry(StartupKind.RunOnce, $@"{sub}\{name}", command, CommandLine.ImagePath(command?.Split('|')[0]), $@"HKLM\{ex}\{sub}", Hive.Machine, null, $"runonceex:{sub}:{name}");
            }
        }
    }

    /// <summary>Active Setup: a command each user runs once at sign-in until the component version is recorded for that user.</summary>
    public IEnumerable<StartupEntry> ActiveSetup()
    {
        foreach (var path in new[] { @"SOFTWARE\Microsoft\Active Setup\Installed Components", @"SOFTWARE\WOW6432Node\Microsoft\Active Setup\Installed Components" })
        {
            using var key = Open(Hive.Machine, path);
            foreach (var id in key?.GetSubKeyNames() ?? [])
            {
                using var c = key!.OpenSubKey(id);
                if (c?.GetValue("StubPath", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString() is not { Length: > 0 } stub) continue;
                if (c.GetValue("IsInstalled") is int installed && installed == 0) continue;
                var name = c.GetValue("")?.ToString() is { Length: > 0 } n ? ResolveIndirect(n) ?? n : id;
                yield return new StartupEntry(StartupKind.ActiveSetup, name, stub, CommandLine.ImagePath(stub), $@"HKLM\{path}\{id}", Hive.Machine, null, $"activesetup:{path}:{id}");
            }
        }
    }

    /// <summary>The old "Load" and "Run" values under Windows NT\CurrentVersion\Windows of the user; normally empty.</summary>
    public IEnumerable<StartupEntry> LoadValues()
    {
        const string path = @"Software\Microsoft\Windows NT\CurrentVersion\Windows";
        using var key = Open(Hive.User, path);
        foreach (var name in new[] { "Load", "Run" })
        foreach (var item in Strings(key?.GetValue(name), split: true))
            yield return new StartupEntry(StartupKind.LoadValue, $"{name}: {Path.GetFileName(item)}", item, CommandLine.ImagePath(item), $@"HKCU\{path}\{name}", Hive.User, null, $"load:{name}:{item}")
            {
                Suspicious = true,
            };
    }

    public const string BootExecuteDefault = "autocheck autochk *";

    /// <summary>Native programs the Session Manager runs before Windows starts (normally only the disk check).</summary>
    public IEnumerable<StartupEntry> BootExecute()
    {
        const string path = @"SYSTEM\CurrentControlSet\Control\Session Manager";
        using var key = Open(Hive.Machine, path);
        foreach (var name in new[] { "BootExecute", "SetupExecute", "Execute", "S0InitialCommand" })
        foreach (var line in Strings(key?.GetValue(name), split: false).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            // "autocheck autochk *": autocheck marks a program that may be skipped; the program is the next word.
            var program = parts.Length > 1 && parts[0].Equals("autocheck", StringComparison.OrdinalIgnoreCase) ? parts[1] : parts[0];
            yield return new StartupEntry(StartupKind.BootExecute, $"{name}: {program}", line, SystemFile(Path.HasExtension(program) ? program : program + ".exe"),
                $@"HKLM\{path}\{name}", Hive.Machine, null, $"bootexecute:{name}:{line}")
            {
                Suspicious = !(name == "BootExecute" && string.Equals(line.Trim(), BootExecuteDefault, StringComparison.OrdinalIgnoreCase)),
            };
        }
    }

    /// <summary>DLLs Windows maps from System32 for every process; a file other than Windows' own here is loaded into everything.</summary>
    public IEnumerable<StartupEntry> KnownDlls()
    {
        const string path = @"SYSTEM\CurrentControlSet\Control\Session Manager\KnownDLLs";
        using var key = Open(Hive.Machine, path);
        foreach (var name in key?.GetValueNames().Where(n => n.Length > 0 && !n.StartsWith("DllDirectory", StringComparison.OrdinalIgnoreCase)) ?? [])
        {
            if (key!.GetValue(name) is not string { Length: > 0 } file) continue;
            yield return new StartupEntry(StartupKind.KnownDll, name, file, SystemFile(file), $@"HKLM\{path}", Hive.Machine, null, $"knowndll:{name}");
        }
    }

    /// <summary>Winsock transport and name space providers: DLLs loaded into every program that uses the network.</summary>
    public IEnumerable<StartupEntry> WinsockProviders()
    {
        foreach (var (catalog, value) in new[]
                 {
                     (@"Protocol_Catalog9\Catalog_Entries", "PackedCatalogItem"), (@"Protocol_Catalog9\Catalog_Entries64", "PackedCatalogItem"),
                     (@"NameSpace_Catalog5\Catalog_Entries", "LibraryPath"), (@"NameSpace_Catalog5\Catalog_Entries64", "LibraryPath"),
                 })
        {
            var path = $@"SYSTEM\CurrentControlSet\Services\WinSock2\Parameters\{catalog}";
            using var key = Open(Hive.Machine, path);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in key?.GetSubKeyNames() ?? [])
            {
                using var e = key!.OpenSubKey(id);
                var dll = e?.GetValue(value, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) switch
                {
                    byte[] packed => PackedPath(packed),
                    string s => s,
                    _ => null,
                };
                if (dll is null || !seen.Add(dll)) continue;
                var name = e!.GetValue("DisplayString")?.ToString() is { Length: > 0 } d ? ResolveIndirect(d) ?? d : Path.GetFileName(dll);
                // Catalog_Entries is the 32-bit catalog (loaded into 32-bit programs), Catalog_Entries64 the 64-bit one.
                var wow64 = !catalog.EndsWith("64", StringComparison.Ordinal);
                yield return new StartupEntry(StartupKind.WinsockProvider, name, dll, SystemFile(dll, wow64: wow64), $@"HKLM\{path}", Hive.Machine, null, $"winsock:{catalog}:{dll}");
            }
        }
    }

    /// <summary>
    /// PackedCatalogItem starts with the provider DLL path as a zero-terminated ANSI string. Read as Latin-1, so a path
    /// with umlauts keeps its letters instead of turning into "?".
    /// </summary>
    public static string? PackedPath(byte[] packed)
    {
        var end = Array.IndexOf(packed, (byte)0);
        if (end <= 0) return null;
        var text = Encoding.Latin1.GetString(packed, 0, end).Trim();
        return text.Length > 0 && !text.Any(char.IsControl) ? text : null;
    }

    /// <summary>Print monitors: DLLs the print spooler (a SYSTEM service) loads.</summary>
    public IEnumerable<StartupEntry> PrintMonitors()
    {
        const string path = @"SYSTEM\CurrentControlSet\Control\Print\Monitors";
        using var key = Open(Hive.Machine, path);
        foreach (var name in key?.GetSubKeyNames() ?? [])
        {
            using var m = key!.OpenSubKey(name);
            if (m?.GetValue("Driver") is not string { Length: > 0 } dll) continue;
            yield return new StartupEntry(StartupKind.PrintMonitor, name, dll, SystemFile(dll), $@"HKLM\{path}\{name}", Hive.Machine, null, $"printmonitor:{name}");
        }
    }

    /// <summary>Authentication, notification and security packages that LSASS loads (sign-in and password handling).</summary>
    public IEnumerable<StartupEntry> LsaPackages()
    {
        foreach (var (path, values) in new[]
                 {
                     (@"SYSTEM\CurrentControlSet\Control\Lsa", new[] { "Authentication Packages", "Notification Packages", "Security Packages" }),
                     (@"SYSTEM\CurrentControlSet\Control\Lsa\OSConfig", new[] { "Security Packages" }),
                 })
        {
            using var key = Open(Hive.Machine, path);
            foreach (var value in values)
            foreach (var item in Strings(key?.GetValue(value), split: true).Select(i => i.Trim().Trim('"')).Where(i => i.Length > 0))
                yield return new StartupEntry(StartupKind.LsaPackage, $"{value}: {item}", item, SystemFile(item), $@"HKLM\{path}\{value}", Hive.Machine, null, $"lsa:{path}:{value}:{item}");
        }
    }

    /// <summary>Network providers in the order Windows asks them (Windows network, WebDAV, and those of VPN or file sharing software).</summary>
    public IEnumerable<StartupEntry> NetworkProviders()
    {
        using var order = Open(Hive.Machine, @"SYSTEM\CurrentControlSet\Control\NetworkProvider\Order");
        foreach (var name in (order?.GetValue("ProviderOrder")?.ToString() ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var path = $@"SYSTEM\CurrentControlSet\Services\{name}\NetworkProvider";
            using var p = Open(Hive.Machine, path);
            var dll = p?.GetValue("ProviderPath", null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames)?.ToString();
            var display = p?.GetValue("Name")?.ToString() is { Length: > 0 } n ? n : name;
            yield return new StartupEntry(StartupKind.NetworkProvider, display, dll, SystemFile(dll), $@"HKLM\{path}", Hive.Machine, null, $"netprovider:{name}");
        }
    }

    /// <summary>Audio and video codecs registered for the older multimedia interfaces (loaded by programs that play media).</summary>
    public IEnumerable<StartupEntry> Codecs()
    {
        foreach (var path in new[] { @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Drivers32", @"SOFTWARE\WOW6432Node\Microsoft\Windows NT\CurrentVersion\Drivers32" })
        {
            using var key = Open(Hive.Machine, path);
            foreach (var name in key?.GetValueNames().Where(n => n.Length > 0) ?? [])
            {
                // Only file names: the key also holds DWORD settings (MidisrvTransferComplete).
                if (key!.GetValue(name) is not string { Length: > 0 } dll) continue;
                var wow64 = path.Contains("WOW6432Node", StringComparison.OrdinalIgnoreCase);
                yield return new StartupEntry(StartupKind.Codec, name, dll, SystemFile(dll, wow64: wow64), $@"HKLM\{path}", Hive.Machine, null, $"codec:{path}:{name}");
            }
        }
    }

    /// <summary>
    /// Permanent WMI event consumers that run a command or a script (root\subscription). Windows itself rarely uses
    /// them and malware does, so each one is marked for a look.
    /// </summary>
    public static IEnumerable<StartupEntry> WmiConsumers()
    {
        var list = new List<StartupEntry>();
        // A damaged WMI repository can hang a query: give up after 15 seconds instead of blocking the page.
        var options = new System.Management.EnumerationOptions { Timeout = TimeSpan.FromSeconds(15), ReturnImmediately = true, Rewindable = false };
        using var searcher = new System.Management.ManagementObjectSearcher(@"root\subscription", "SELECT * FROM __EventConsumer", options);
        using var results = searcher.Get();
        foreach (var o in results.OfType<System.Management.ManagementObject>())
        {
            using (o)
            {
                var cls = o["__CLASS"]?.ToString() ?? "";
                if (cls is not ("CommandLineEventConsumer" or "ActiveScriptEventConsumer")) continue;
                var name = o["Name"]?.ToString() ?? cls;
                var command = cls == "CommandLineEventConsumer"
                    ? o["CommandLineTemplate"]?.ToString() ?? o["ExecutablePath"]?.ToString()
                    : o["ScriptFileName"]?.ToString() is { Length: > 0 } file ? file
                    : o["ScriptText"]?.ToString() is { Length: > 0 } text ? "(script) " + text.ReplaceLineEndings(" ")[..Math.Min(120, text.ReplaceLineEndings(" ").Length)]
                    : "(script)";
                list.Add(new StartupEntry(StartupKind.WmiConsumer, name, command, CommandLine.ImagePath(command), $@"WMI root\subscription\{cls}", Hive.Machine, null, $"wmi:{cls}:{name}")
                {
                    Suspicious = true,
                });
            }
        }
        return list;
    }
}
