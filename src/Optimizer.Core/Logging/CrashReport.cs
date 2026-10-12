using System.Text;
using System.Text.RegularExpressions;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Logging;

/// <summary>
/// Error reports without telemetry: an unexpected error is written to the data folder, and the app offers to open a
/// GitHub bug report filled in with the app version, the Windows build and the error. Nothing leaves the PC unless the
/// user submits that report on GitHub. The text names no user: profile paths become %USERPROFILE%, and the account and
/// computer names are replaced.
/// </summary>
public static partial class CrashReport
{
    /// <summary>The last report not offered yet (admin-only data folder when elevated).</summary>
    public static string FilePath => Path.Combine(DataPaths.Root, "last-error.txt");

    /// <summary>GitHub accepts long issue links, but browsers and proxies cut them; the error text is shortened to this.</summary>
    public const int MaxReportChars = 3500;

    public static string Describe(Exception error, string appVersion, string windowsBuild, DateTimeOffset at)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Tunero {appVersion}, Windows {windowsBuild}, {at:yyyy-MM-dd HH:mm:ss zzz}");
        for (var e = error; e is not null; e = e.InnerException)
        {
            sb.AppendLine($"{e.GetType().FullName}: {e.Message}");
            if (e.StackTrace is { } trace) sb.AppendLine(trace);
        }
        return Sanitize(sb.ToString(), Environment.UserName, Environment.MachineName);
    }

    /// <summary>Removes what identifies the user: profile paths, the account name and the computer name.</summary>
    public static string Sanitize(string text, string? userName, string? machineName)
    {
        text = ProfilePath().Replace(text, "%USERPROFILE%");
        if (userName is { Length: >= 3 }) text = Regex.Replace(text, Regex.Escape(userName), "<user>", RegexOptions.IgnoreCase);
        if (machineName is { Length: >= 3 }) text = Regex.Replace(text, Regex.Escape(machineName), "<pc>", RegexOptions.IgnoreCase);
        return text;
    }

    /// <summary>Writes the report for the next start; never throws (it runs while the app fails).</summary>
    public static void Save(string report)
    {
        try
        {
            Directory.CreateDirectory(DataPaths.Root);
            File.WriteAllText(FilePath, report, new UTF8Encoding(false));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("crash", $"error report not saved: {ex.Message}");
        }
    }

    /// <summary>The saved report, removed so it is offered once; null when there is none.</summary>
    public static string? Take()
    {
        try
        {
            if (!File.Exists(FilePath)) return null;
            var text = File.ReadAllText(FilePath);
            File.Delete(FilePath);
            return text.Length == 0 ? null : text;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("crash", $"error report not read: {ex.Message}");
            return null;
        }
    }

    /// <summary>The bug report form of the repository, filled in (field ids of .github/ISSUE_TEMPLATE/bug_report.yml).</summary>
    public static string IssueUrl(string repository, string appVersion, string windowsBuild, string what, string report)
    {
        if (report.Length > MaxReportChars) report = report[..MaxReportChars] + "\n(shortened)";
        return $"https://github.com/{repository}/issues/new?template=bug_report.yml&labels=bug,crash" +
               $"&version={Uri.EscapeDataString(appVersion)}&windows={Uri.EscapeDataString(windowsBuild)}" +
               $"&what={Uri.EscapeDataString(what)}&log={Uri.EscapeDataString(report)}";
    }

    [GeneratedRegex(@"[A-Za-z]:\\Users\\[^\\\r\n""]+", RegexOptions.IgnoreCase)]
    private static partial Regex ProfilePath();
}
