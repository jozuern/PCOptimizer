using System.Text;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Platform;

/// <summary>
/// Removing the app from this PC (Settings > Uninstall). The running exe, the runtime extracted into the data folder and
/// the open log cannot be deleted while the app runs, so a hidden Windows PowerShell, started with the app's
/// administrator rights and <see cref="ProcessHardening"/>, waits until every process with the exe's name has ended (the
/// restarted app and the start process that waits for it), then deletes the data folder and the exe. A copy installed by
/// a package manager is removed by it instead (<see cref="PackageInstall.UninstallArguments"/>), so its record stays right.
/// </summary>
public static class SelfUninstall
{
    /// <summary>
    /// Script for the cleanup. The data folder (admin-only) holds the backups, so it is deleted only when the app is gone:
    /// never while a process with the app's name runs (it did not end, or the user started it again), and for a copy a
    /// package manager removes (<paramref name="removedByPackageManager"/>) only once the package manager has removed
    /// <paramref name="exe"/> (at most <paramref name="packageManagerSeconds"/>, 15 minutes). When the package manager fails, the app stays and so do its backups.
    /// The folder is deleted with cmd.exe's rd, which removes a junction or symbolic link inside it without following it.
    /// A downloaded exe usually sits in a folder the signed-in user can write to: Remove-Item deletes a link put in its
    /// place, not the link's target; the ".old" exe a self-update leaves until the next start goes with it.
    /// <paramref name="waitForProcessId"/> is the elevated Chocolatey window: it inherits the app's TEMP inside the data
    /// folder and writes its download cache there, so the folder is deleted only after it has ended.
    /// </summary>
    public static string CleanupScript(string processName, string dataRoot, string exe, bool removedByPackageManager = false, int? waitForProcessId = null,
        int packageManagerSeconds = 900)
    {
        var files = removedByPackageManager ? "@()" : $"@({WingetPackage.Quote(exe)}, {WingetPackage.Quote(exe + ".old")})";
        var cmd = WingetPackage.Quote(Path.Combine(Environment.SystemDirectory, "cmd.exe"));
        var script = new StringBuilder()
            .AppendLine("$ErrorActionPreference = 'SilentlyContinue'")
            .AppendLine($"$name = {WingetPackage.Quote(processName)}")
            .AppendLine("Wait-Process -Name $name -Timeout 60")
            .AppendLine("if (Get-Process -Name $name) { exit 1 }")
            .AppendLine($"$root = {WingetPackage.Quote(dataRoot)}")
            .AppendLine($"$exe = {WingetPackage.Quote(exe)}");
        if (removedByPackageManager)
        {
            script
                .AppendLine($"for ($i = 0; $i -lt {packageManagerSeconds} -and (Test-Path -LiteralPath $exe); $i++) {{")
                .AppendLine("    if (Get-Process -Name $name) { exit 1 }")
                .AppendLine("    Start-Sleep -Seconds 1")
                .AppendLine("}")
                .AppendLine("if (Test-Path -LiteralPath $exe) { exit 1 }");
            if (waitForProcessId is { } id) script.AppendLine($"Wait-Process -Id {id} -Timeout 900");
        }
        return script
            .AppendLine($"$files = {files}")
            .AppendLine("for ($i = 0; $i -lt 30; $i++) {")
            .AppendLine("    if (Get-Process -Name $name) { exit 1 }")
            .AppendLine($"    if (Test-Path -LiteralPath $root) {{ & {cmd} /d /c rd /s /q $root }}")
            .AppendLine("    foreach ($f in $files) { if (Test-Path -LiteralPath $f) { Remove-Item -LiteralPath $f -Force } }")
            .AppendLine("    if (-not (Test-Path -LiteralPath $root) -and -not ($files | Where-Object { Test-Path -LiteralPath $_ })) { break }")
            .AppendLine("    Start-Sleep -Seconds 1")
            .AppendLine("}")
            .ToString();
    }

    /// <summary>Arguments for powershell.exe that run <paramref name="script"/> without quoting problems.</summary>
    public static string PowerShellArguments(string script) => PowerShellScript.Encoded(script);
}
