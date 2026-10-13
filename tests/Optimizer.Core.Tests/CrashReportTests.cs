using Optimizer.Core.Logging;

namespace Optimizer.Core.Tests;

/// <summary>Error reports leave out who the user is and fit into a GitHub issue link.</summary>
public class CrashReportTests
{
    [Fact]
    public void ReportNamesNoUserOrComputer()
    {
        var text = @"at Foo in C:\Users\Joshua Zürn\AppData\Local\x.cs, file D:\Games\joshua\save, host DESKTOP-ABC123";
        var clean = CrashReport.Sanitize(text, "Joshua", "DESKTOP-ABC123");
        Assert.Contains(@"%USERPROFILE%\AppData\Local\x.cs", clean);
        Assert.DoesNotContain("joshua", clean, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DESKTOP-ABC123", clean);
        Assert.Contains("<pc>", clean);
        // A registry error names the account's hive by its SID.
        Assert.Equal(@"HKU\<sid>\Software", CrashReport.Sanitize(@"HKU\S-1-5-21-1111111111-2222222222-3333333333-1001\Software", null, null));
    }

    [Fact]
    public void ShortNamesAreNotReplacedEverywhere()
    {
        // A two-letter account name would turn every "Al" in a stack trace into <user>.
        Assert.Equal("Alias", CrashReport.Sanitize("Alias", "Al", "PC"));
    }

    [Fact]
    public void DescribeIncludesInnerErrors()
    {
        Exception error;
        try
        {
            throw new InvalidOperationException("outer", new IOException("inner"));
        }
        catch (Exception ex)
        {
            error = ex;
        }
        var report = CrashReport.Describe(error, "1.2.3", "26100.1", new DateTimeOffset(2026, 10, 12, 18, 0, 0, TimeSpan.Zero));
        Assert.StartsWith("Tunero 1.2.3, Windows 26100.1, 2026-10-12 18:00:00", report);
        Assert.Contains("System.InvalidOperationException: outer", report);
        Assert.Contains("System.IO.IOException: inner", report);
    }

    [Fact]
    public void IssueLinkIsFilledAndShortened()
    {
        var url = CrashReport.IssueUrl("jozuern/Tunero", "1.2.3", "26100.1", "It stopped", new string('x', 10_000));
        Assert.StartsWith("https://github.com/jozuern/Tunero/issues/new?template=bug_report.yml", url);
        Assert.Contains("&version=1.2.3", url);
        Assert.Contains("&what=It%20stopped", url);
        Assert.True(url.Length < 4_200, $"link has {url.Length} characters");
        Assert.Contains(Uri.EscapeDataString("(shortened)"), url);
    }
}
