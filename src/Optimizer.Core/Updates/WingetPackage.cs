namespace Optimizer.Core.Updates;

/// <summary>
/// PCOptimizer installed with winget (portable package): updates go through winget, so its record of the installed
/// version stays right, instead of the app replacing its own exe.
/// </summary>
public static class WingetPackage
{
    public const string Id = "jozuern.PCOptimizer";

    /// <summary>
    /// winget keeps a portable package in a folder named after the package id, with the source appended for a source
    /// ("jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe"), under a root its settings can move. Started through
    /// the command link in WinGet\Links, the path is the link's: it is followed to the file first.
    /// </summary>
    public static bool IsInstalledWithWinget(string exePath)
    {
        var path = exePath;
        try
        {
            if (File.ResolveLinkTarget(exePath, returnFinalTarget: true) is { } target) path = target.FullName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
        return IsPackageFolder(Path.GetFileName(Path.GetDirectoryName(path)));
    }

    public static bool IsPackageFolder(string? folder) =>
        folder is not null && (folder.Equals(Id, StringComparison.OrdinalIgnoreCase) || folder.StartsWith(Id + "_", StringComparison.OrdinalIgnoreCase));

    public static string PowerShell => Path.Combine(Environment.SystemDirectory, @"WindowsPowerShell\v1.0\powershell.exe");

    /// <summary>
    /// PowerShell arguments for the update, started as the signed-in user (never elevated): wait until this process has
    /// ended (winget cannot replace a running exe), run winget, keep the window open with <paramref name="failedText"/>
    /// when it did not succeed, then start PCOptimizer again (Windows asks for administrator rights as on any start).
    /// </summary>
    public static string UpdateArguments(int processId, string exePath, string runningText, string failedText) =>
        "-NoProfile -Command \"" +
        $"Write-Host {Quote(runningText)}; " +
        $"Wait-Process -Id {processId} -Timeout 60 -ErrorAction SilentlyContinue; " +
        $"winget upgrade --id {Id} --exact --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity; " +
        $"if ($LASTEXITCODE -ne 0) {{ Read-Host {Quote(failedText)} }}; " +
        $"Start-Process -FilePath {Quote(exePath)}\"";

    /// <summary>
    /// A PowerShell string in single quotes. PowerShell also ends such a string at the typographic single quotes, so every
    /// one of them is doubled; double quotes would end the -Command argument and are left out.
    /// </summary>
    public static string Quote(string text)
    {
        var sb = new System.Text.StringBuilder("'");
        foreach (var c in text)
        {
            if (c == '"') continue;
            if (c is '\'' or '‘' or '’' or '‚' or '‛') sb.Append(c);
            sb.Append(c);
        }
        return sb.Append('\'').ToString();
    }
}
