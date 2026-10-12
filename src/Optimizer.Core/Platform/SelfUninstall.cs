using System.Text;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Platform;

/// <summary>
/// Removing the app from this PC (Settings > Uninstall). The running exe, the runtime extracted into the data folder and
/// the open log cannot be deleted while the app runs, so a hidden Windows PowerShell, started with the app's
/// administrator rights and <see cref="ProcessHardening"/>, waits until every process with the exe's name has ended (the
/// restarted app and the start process that waits for it), then deletes the data folder and the exe. A copy winget
/// installed is removed by winget instead (<see cref="WingetPackage.UninstallArguments"/>), so its record stays right.
/// </summary>
public static class SelfUninstall
{
    /// <summary>
    /// Script for the cleanup. The data folder (admin-only) is deleted with cmd.exe's rd, which removes a junction or
    /// symbolic link inside it without following it. The exe usually sits in a folder the signed-in user can write to:
    /// Remove-Item deletes a link put in its place, not the link's target. <paramref name="exe"/> is null for a copy
    /// winget removes; the ".old" exe a self-update leaves until the next start goes with it.
    /// </summary>
    public static string CleanupScript(string processName, string dataRoot, string? exe)
    {
        var files = exe is null ? "@()" : $"@({WingetPackage.Quote(exe)}, {WingetPackage.Quote(exe + ".old")})";
        var cmd = WingetPackage.Quote(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        return new StringBuilder()
            .AppendLine("$ErrorActionPreference = 'SilentlyContinue'")
            .AppendLine($"Wait-Process -Name {WingetPackage.Quote(processName)} -Timeout 60")
            .AppendLine($"$root = {WingetPackage.Quote(dataRoot)}")
            .AppendLine($"$files = {files}")
            .AppendLine("for ($i = 0; $i -lt 30; $i++) {")
            .AppendLine($"    if (Test-Path -LiteralPath $root) {{ & {cmd} /d /c rd /s /q $root }}")
            .AppendLine("    foreach ($f in $files) { if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force } }")
            .AppendLine("    if (-not (Test-Path -LiteralPath $root) -and -not ($files | Where-Object { Test-Path -LiteralPath $_ })) { break }")
            .AppendLine("    Start-Sleep -Seconds 1")
            .AppendLine("}")
            .ToString();
    }

    /// <summary>Arguments for powershell.exe that run <paramref name="script"/> without quoting problems.</summary>
    public static string PowerShellArguments(string script) =>
        $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand {Convert.ToBase64String(Encoding.Unicode.GetBytes(script))}";
}
