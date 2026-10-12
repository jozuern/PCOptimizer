using System.Text.RegularExpressions;

namespace Optimizer.Core.Platform;

/// <summary>
/// Environment variables as a service or driver sees them: Windows' known folders and the machine variables
/// (HKLM\...\Session Manager\Environment), without the signed-in user's variables. This process inherits those too
/// (HKCU\Environment, which any program of the user can change): expanding a service's path with them could check a
/// different file than the one Windows starts.
/// </summary>
public static partial class MachineEnvironment
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Variables = new(Read);

    /// <summary>%name% replaced by the machine value; names without one stay as they are.</summary>
    public static string Expand(string text) => Expand(text, Variables.Value);

    /// <summary>
    /// Expansion as a program started for the signed-in user sees it: the machine variables plus that user's profile
    /// folders. The elevated app may run under another account (a separate admin account): its own %LOCALAPPDATA% would
    /// name a different file than the one the user's autostart entry starts. Names this does not know are expanded with
    /// this process's variables. Without a profile path: this process's variables, as before.
    /// </summary>
    public static Func<string, string> ForUser(string? profilePath)
    {
        if (string.IsNullOrEmpty(profilePath)) return Environment.ExpandEnvironmentVariables;
        var vars = new Dictionary<string, string>(Variables.Value, StringComparer.OrdinalIgnoreCase)
        {
            ["USERPROFILE"] = profilePath,
            ["HOMEDRIVE"] = Path.GetPathRoot(profilePath)!.TrimEnd('\\'),
            ["HOMEPATH"] = profilePath[Path.GetPathRoot(profilePath)!.TrimEnd('\\').Length..],
            ["APPDATA"] = Path.Combine(profilePath, @"AppData\Roaming"),
            ["LOCALAPPDATA"] = Path.Combine(profilePath, @"AppData\Local"),
            ["TEMP"] = Path.Combine(profilePath, @"AppData\Local\Temp"),
            ["TMP"] = Path.Combine(profilePath, @"AppData\Local\Temp"),
        };
        return text => Environment.ExpandEnvironmentVariables(Expand(text, vars));
    }

    internal static string Expand(string text, IReadOnlyDictionary<string, string> variables) =>
        VariableRegex().Replace(text, m => variables.TryGetValue(m.Groups[1].Value, out var v) ? v : m.Value);

    private static IReadOnlyDictionary<string, string> Read()
    {
        var windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        string Folder(Environment.SpecialFolder f) => Environment.GetFolderPath(f);
        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["SystemRoot"] = windows,
            ["windir"] = windows,
            ["SystemDrive"] = Path.GetPathRoot(windows)!.TrimEnd('\\'),
            ["ProgramFiles"] = Folder(Environment.SpecialFolder.ProgramFiles),
            ["ProgramW6432"] = Folder(Environment.SpecialFolder.ProgramFiles),
            ["ProgramFiles(x86)"] = Folder(Environment.SpecialFolder.ProgramFilesX86),
            ["CommonProgramFiles"] = Folder(Environment.SpecialFolder.CommonProgramFiles),
            ["CommonProgramW6432"] = Folder(Environment.SpecialFolder.CommonProgramFiles),
            ["CommonProgramFiles(x86)"] = Folder(Environment.SpecialFolder.CommonProgramFilesX86),
            ["ProgramData"] = Folder(Environment.SpecialFolder.CommonApplicationData),
            ["ALLUSERSPROFILE"] = Folder(Environment.SpecialFolder.CommonApplicationData),
        };
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Environment");
            foreach (var name in key?.GetValueNames() ?? [])
                if (!vars.ContainsKey(name) && key!.GetValue(name, null, Microsoft.Win32.RegistryValueOptions.DoNotExpandEnvironmentNames) is string value)
                    vars[name] = value;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            Logging.Log.Warn("environment", $"machine variables not readable: {ex.Message}");
        }
        // Machine values refer to each other (TEMP is %SystemRoot%\TEMP): one pass over the known folders resolves them.
        foreach (var name in vars.Keys.ToList()) vars[name] = Expand(vars[name], vars);
        return vars;
    }

    [GeneratedRegex("%([^%]+)%")]
    private static partial Regex VariableRegex();
}
