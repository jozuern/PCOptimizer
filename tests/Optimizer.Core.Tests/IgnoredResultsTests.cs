using Optimizer.Core.Findings;

namespace Optimizer.Core.Tests;

public sealed class IgnoredResultsTests
{
    private static Finding F(string? instance, string? subject, FindingStatus status = FindingStatus.Problem, bool critical = false) =>
        new() { Id = "F12.gamesOnHdd", Kind = FindingKind.Finding, Status = status, InstanceKey = instance, Subject = subject, Critical = critical };

    /// <summary>One result is ignored, not the whole check: another library on a hard disk still shows.</summary>
    [Fact]
    public void TheKeyNamesTheOneResult()
    {
        Assert.Equal(@"F12.gamesOnHdd|E:\SteamLibrary@Problem", IgnoredResults.Key(F(null, @"E:\SteamLibrary")));
        Assert.NotEqual(IgnoredResults.Key(F(null, @"E:\SteamLibrary")), IgnoredResults.Key(F(null, @"F:\Games")));
        Assert.Equal("F12.gamesOnHdd#disk1|Samsung SSD@Problem", IgnoredResults.Key(F("disk1", "Samsung SSD")));
        Assert.Equal("F12.gamesOnHdd|@Problem", IgnoredResults.Key(F(null, null)));
    }

    /// <summary>A disk number or display name can move to another device: the device's name keeps them apart.</summary>
    [Fact]
    public void AnotherDeviceAtTheSamePositionShows()
    {
        string[] keys = [IgnoredResults.Key(F("disk1", "Old HDD"))];
        Assert.True(IgnoredResults.IsIgnored(F("disk1", "Old HDD"), keys));
        Assert.False(IgnoredResults.IsIgnored(F("disk1", "New HDD"), keys));
    }

    /// <summary>An ignored "could not be checked" never hides a real problem later, nor a problem one that turned critical.</summary>
    [Fact]
    public void AChangedStatusShowsAgain()
    {
        string[] unknown = [IgnoredResults.Key(F("disk1", "Disk", FindingStatus.Unknown))];
        Assert.True(IgnoredResults.IsIgnored(F("disk1", "Disk", FindingStatus.Unknown), unknown));
        Assert.False(IgnoredResults.IsIgnored(F("disk1", "Disk"), unknown));
        string[] problem = [IgnoredResults.Key(F("disk1", "Disk"))];
        Assert.False(IgnoredResults.IsIgnored(F("disk1", "Disk", critical: true), problem));
    }

    [Fact]
    public void PassedChecksAndGameAccessAreNeverIgnored()
    {
        var ok = F(null, "x", FindingStatus.Ok);
        Assert.False(IgnoredResults.IsIgnored(ok, [IgnoredResults.Key(ok)]));
        var access = F(null, "x") with { Kind = FindingKind.GameAccess };
        Assert.False(IgnoredResults.IsIgnored(access, [IgnoredResults.Key(access)]));
        var shown = F(null, "y");
        Assert.Equal([shown], IgnoredResults.Without([F(null, "x"), shown], [IgnoredResults.Key(F(null, "x"))]));
    }
}
