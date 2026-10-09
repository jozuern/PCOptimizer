namespace Optimizer.Core.Startup;

/// <summary>Turns autostart command lines and service image paths into the file that actually runs.</summary>
public static class CommandLine
{
    private static readonly string[] Hosts = ["rundll32.exe", "rundll32"];

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
