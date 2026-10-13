using System.Text;

namespace Optimizer.Core.Platform;

/// <summary>
/// Command lines for Windows PowerShell 5.1 (powershell.exe in System32, see <see cref="ProcessHardening.ResolveSystemTool"/>),
/// in one place. Always -NoProfile: no profile script of the user runs in a PowerShell this app starts, elevated or not.
/// -NonInteractive for scripts whose output the app reads, never for a window that waits for Enter. The execution
/// policy is lifted only where a script file runs (Scoop, which is a .ps1) or a script is encoded.
/// </summary>
public static class PowerShellScript
{
    public const string Exe = "powershell.exe";

    /// <summary>
    /// Arguments that run <paramref name="script"/> as an -EncodedCommand (UTF-16, Base64): no quoting of the script can
    /// break the command line or change what runs.
    /// </summary>
    public static string Encoded(string script, bool interactive = false) =>
        Flags(interactive, bypassPolicy: true) + " -EncodedCommand " + Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

    /// <summary>
    /// Arguments that run <paramref name="script"/> as a -Command in double quotes, so a window shows the command it runs.
    /// A double quote would end the argument: such a script must go <see cref="Encoded"/>.
    /// </summary>
    public static string Command(string script, bool interactive = false, bool bypassPolicy = false) =>
        script.Contains('"')
            ? throw new ArgumentException("A -Command script cannot contain a double quote; use Encoded.", nameof(script))
            : Flags(interactive, bypassPolicy) + " -Command \"" + script + "\"";

    private static string Flags(bool interactive, bool bypassPolicy) =>
        "-NoProfile" + (interactive ? "" : " -NonInteractive") + (bypassPolicy ? " -ExecutionPolicy Bypass" : "");
}
