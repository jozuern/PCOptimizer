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
    /// A script host whose command could run anything: inline code (powershell -enc, cmd /c with no file), chained
    /// commands, a URL, or a script outside System32/SysWOW64. A host that only runs files from those protected Windows
    /// folders (Windows' own tasks: cmd /c %SystemRoot%\system32\x.cmd) is not flagged; its files are signed or
    /// writable only by TrustedInstaller.
    /// </summary>
    public static bool RunsUnverifiedScript(string? command, string? imagePath, Func<string, string>? expand = null)
    {
        if (!IsScriptHost(imagePath) || string.IsNullOrWhiteSpace(command)) return false;
        expand ??= Environment.ExpandEnvironmentVariables;
        var args = SplitFirst(expand(command.Trim())).Tail;
        if (args.IndexOfAny(['&', '|', '^', '`', ';']) >= 0 || args.Contains("://", StringComparison.Ordinal)) return true;
        var tokens = Tokens(args).ToList();
        // PowerShell inline code: -Command / -EncodedCommand / -ec and their accepted prefixes ("-c", "-enc", "/e").
        if (Path.GetFileName(imagePath!).StartsWith("p", StringComparison.OrdinalIgnoreCase)
            && tokens.Any(t => t.Length >= 2 && t[0] is '-' or '/' && IsPrefixOf(t[1..], "command", "encodedcommand", "ec")))
            return true;
        var files = tokens.Select(t => t.Split(',')[0]).Where(t => t.Contains('\\')).ToList();
        if (files.Count == 0) return true; // inline code or a bare name looked up on PATH
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var protectedDirs = new[] { Path.Combine(windows, "System32") + "\\", Path.Combine(windows, "SysWOW64") + "\\" };
        return !files.All(f => !f.Contains("..", StringComparison.Ordinal) && protectedDirs.Any(d => f.StartsWith(d, StringComparison.OrdinalIgnoreCase)));
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
        // An unquoted path with spaces: take the longest prefix that exists (C:\Program Files\App\app.exe -arg).
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

        if (Hosts.Any(h => Path.GetFileName(first).Equals(h, StringComparison.OrdinalIgnoreCase)) && rest.Length > 0)
        {
            var dll = SplitFirst(rest).First.Split(',')[0];
            return Qualify(dll, system, exists);
        }
        return Qualify(first, system, exists);
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
