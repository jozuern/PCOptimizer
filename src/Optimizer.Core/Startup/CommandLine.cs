namespace Optimizer.Core.Startup;

/// <summary>Turns autostart command lines and service image paths into the file that actually runs.</summary>
public static class CommandLine
{
    private static readonly string[] Hosts = ["rundll32.exe", "rundll32"];

    /// <summary>
    /// Built-in Windows programs that run scripts or other code given on the command line. They are signed by
    /// Microsoft, so the signature says nothing about what an autostart entry does with them.
    /// </summary>
    private static readonly HashSet<string> ScriptHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "powershell.exe", "pwsh.exe", "powershell_ise.exe", "cmd.exe", "wscript.exe", "cscript.exe", "mshta.exe", "regsvr32.exe",
        "rundll32.exe", "conhost.exe", "forfiles.exe", "msbuild.exe", "installutil.exe", "regasm.exe", "regsvcs.exe", "certutil.exe",
        "bitsadmin.exe", "wmic.exe", "hh.exe", "msxsl.exe", "bash.exe", "wsl.exe",
    };

    /// <summary>True when the image is a script host (PowerShell, cmd, mshta and similar).</summary>
    public static bool IsScriptHost(string? imagePath) =>
        imagePath is { Length: > 0 } && ScriptHosts.Contains(Path.GetFileName(imagePath.Trim().Trim('"')));

    /// <summary>
    /// rundll32 entry points that start another program, file or URL given as an argument (signed proxies for any
    /// program).
    /// </summary>
    private static readonly HashSet<string> ProxyExports = new(StringComparer.OrdinalIgnoreCase)
    {
        "ShellExec_RunDLL", "ShellExec_RunDLLA", "ShellExec_RunDLLW", "FileProtocolHandler", "OpenURL", "OpenURLA", "RouteTheCall",
    };

    /// <summary>
    /// A script host whose command could run anything: inline code (powershell -enc, cmd /c with no file), chained
    /// commands, a URL, another script host as an argument (cmd /c powershell -enc), or a script outside
    /// System32/SysWOW64. A host that only runs files from those protected Windows folders (Windows' own tasks: cmd /c
    /// %SystemRoot%\system32\x.cmd) is not flagged; its files are signed or writable only by TrustedInstaller. The host
    /// is the program the line starts (rundll32 itself, not its DLL); a rundll32 line is flagged when it uses an entry
    /// point that starts something else, or passes further files or URLs.
    /// </summary>
    public static bool RunsUnverifiedScript(string? command, Func<string, string>? expand = null)
    {
        if (string.IsNullOrWhiteSpace(command)) return false;
        expand ??= Environment.ExpandEnvironmentVariables;
        if (Program(command, expand) is not var (host, args) || !IsScriptHost(host)) return false;
        if (IsRundll(host)) return RundllStartsSomethingElse(args);
        if (args.IndexOfAny(['&', '|', '^', '`', ';']) >= 0 || args.Contains("://", StringComparison.Ordinal)) return true;
        var tokens = Tokens(args).ToList();
        if (tokens.Any(t => IsScriptHost(t))) return true;
        // PowerShell inline code: -Command / -EncodedCommand / -ec and their accepted prefixes ("-c", "-enc", "/e").
        if (Path.GetFileName(host).StartsWith("p", StringComparison.OrdinalIgnoreCase)
            && tokens.Any(t => t.Length >= 2 && t[0] is '-' or '/' && IsPrefixOf(t[1..], "command", "encodedcommand", "ec")))
            return true;
        var files = tokens.Select(t => t.Split(',')[0]).Where(t => t.Contains('\\')).ToList();
        if (files.Count == 0) return true; // inline code or a bare name looked up on PATH
        return !files.All(IsInProtectedSystemFolder);
    }

    /// <summary>
    /// rundll32 runs the DLL's entry point; the DLL itself is judged by its signature (<see cref="ImagePath"/>). Flagged
    /// only when the entry point is a known proxy or more files or URLs follow (rundll32 x.dll,Entry C:\Users\x.exe).
    /// </summary>
    private static bool RundllStartsSomethingElse(string args)
    {
        var tokens = Tokens(args).Where(t => !t.StartsWith('/') && !t.StartsWith('-')).ToList();
        if (tokens.Count == 0) return false;
        var parts = tokens[0].Split(',', 2);
        if (parts.Length == 2 && ProxyExports.Contains(parts[1].Trim())) return true;
        if (args.Contains("://", StringComparison.Ordinal)) return true;
        return tokens.Skip(1).Any(t => t.Contains('\\') && !IsInProtectedSystemFolder(t));
    }

    /// <summary>
    /// Folders below System32 and SysWOW64 that standard users can write to (print color profiles, task files, crash
    /// dumps, fax and machine key stores): a script there is not one of Windows' own.
    /// </summary>
    private static readonly string[] UserWritableSystemFolders =
    [
        @"spool\drivers\color", @"spool\PRINTERS", @"spool\SERVERS", "Tasks", "Tasks_Migrated", @"com\dmp", "FxsTmp",
        @"Microsoft\Crypto\RSA\MachineKeys", "LogFiles", @"config\systemprofile",
    ];

    /// <summary>A file in System32 or SysWOW64 (or a subfolder only administrators can write to), without "..".</summary>
    public static bool IsInProtectedSystemFolder(string file)
    {
        if (file.Contains("..", StringComparison.Ordinal)) return false;
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        foreach (var root in new[] { Path.Combine(windows, "System32"), Path.Combine(windows, "SysWOW64") })
        {
            if (!file.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase)) continue;
            var relative = file[(root.Length + 1)..];
            return !UserWritableSystemFolders.Any(w => relative.StartsWith(w + "\\", StringComparison.OrdinalIgnoreCase));
        }
        return false;
    }

    private static bool IsPrefixOf(string s, params string[] words) =>
        words.Any(w => w.StartsWith(s, StringComparison.OrdinalIgnoreCase));

    /// <summary>Arguments split at spaces, quoted parts kept together (quotes removed).</summary>
    private static IEnumerable<string> Tokens(string args)
    {
        var rest = args.Trim();
        while (rest.Length > 0)
        {
            var (first, tail) = SplitFirst(rest);
            if (first.Length > 0) yield return first;
            if (tail.Length >= rest.Length) yield break;
            rest = tail;
        }
    }

    /// <summary>
    /// Image file of a command line: quoted or unquoted first token, environment variables expanded, kernel prefixes
    /// (\SystemRoot\, \??\, System32\drivers) resolved, rundll32 lines resolved to their DLL. Null when empty.
    /// </summary>
    public static string? ImagePath(string? command, Func<string, string>? expand = null, Func<string, bool>? exists = null)
    {
        if (Parse(command, expand, exists) is not var (first, rest, system, ex)) return null;
        if (IsRundll(first) && RundllDll(rest) is { Length: > 0 } dll) return Qualify(dll, system, ex);
        return Qualify(first, system, ex);
    }

    /// <summary>
    /// The program a command line starts and its arguments, for running it (uninstall commands): unlike
    /// <see cref="ImagePath"/>, a rundll32 line gives rundll32 itself with the DLL and entry point as arguments. Null
    /// when empty.
    /// </summary>
    public static (string File, string Arguments)? Program(string? command, Func<string, string>? expand = null, Func<string, bool>? exists = null)
    {
        if (Parse(command, expand, exists) is not var (first, rest, system, ex)) return null;
        return (Qualify(first, system, ex), rest);
    }

    /// <summary>The DLL of a rundll32 line; switches such as "/d" (Windows' own Autochk task) come before it.</summary>
    private static string? RundllDll(string args) => Tokens(args).FirstOrDefault(t => !t.StartsWith('/') && !t.StartsWith('-'))?.Split(',')[0];

    private static bool IsRundll(string file) => Hosts.Any(h => Path.GetFileName(file).Equals(h, StringComparison.OrdinalIgnoreCase));

    /// <summary>First token (the program, unquoted paths with spaces resolved) and the rest of the line.</summary>
    private static (string First, string Tail, string Windows, Func<string, bool> Exists)? Parse(string? command, Func<string, string>? expand, Func<string, bool>? exists)
    {
        if (string.IsNullOrWhiteSpace(command)) return null;
        expand ??= Environment.ExpandEnvironmentVariables;
        exists ??= File.Exists;
        var s = expand(command.Trim());
        var system = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        if (s.StartsWith(@"\??\", StringComparison.Ordinal)) s = s[4..];
        if (s.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase)) s = Path.Combine(system, s[12..]);
        if (s.StartsWith(@"System32\", StringComparison.OrdinalIgnoreCase) || s.StartsWith(@"SysWOW64\", StringComparison.OrdinalIgnoreCase))
            s = Path.Combine(system, s);

        var (first, rest) = SplitFirst(s);
        // An unquoted path with spaces: take the longest prefix that exists (C:\Program Files\Apppp.exe -arg).
        if (!s.StartsWith('"') && !exists(first))
        {
            var parts = s.Split(' ');
            for (var n = parts.Length; n > 1; n--)
            {
                var candidate = string.Join(' ', parts.Take(n));
                if (exists(candidate) || exists(candidate + ".exe"))
                {
                    first = exists(candidate) ? candidate : candidate + ".exe";
                    rest = string.Join(' ', parts.Skip(n));
                    break;
                }
            }
        }
        return (first, rest, system, exists);
    }

    private static (string First, string Tail) SplitFirst(string s)
    {
        s = s.TrimStart();
        if (s.StartsWith('"'))
        {
            var end = s.IndexOf('"', 1);
            return end < 0 ? (s[1..], "") : (s[1..end], s[(end + 1)..].Trim());
        }
        var space = s.IndexOf(' ');
        return space < 0 ? (s, "") : (s[..space], s[(space + 1)..].Trim());
    }

    private static string Qualify(string file, string windows, Func<string, bool> exists)
    {
        if (Path.IsPathRooted(file)) return file;
        // Bare names (for example "ctfmon.exe") are found in System32 or the Windows folder like CreateProcess does.
        foreach (var dir in new[] { Path.Combine(windows, "System32"), windows })
        {
            var candidate = Path.Combine(dir, file);
            if (exists(candidate)) return candidate;
            if (!Path.HasExtension(file) && exists(candidate + ".exe")) return candidate + ".exe";
        }
        return file;
    }
}
