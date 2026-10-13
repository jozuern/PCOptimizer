using Optimizer.App.ViewModels;
using Optimizer.Core.Findings;

namespace Optimizer.App.Tests;

public sealed class IgnoreKeyTests
{
    private static Finding F(string? instance, string? subject) =>
        new() { Id = "F12.gamesOnHdd", Kind = FindingKind.Finding, Status = FindingStatus.Problem, InstanceKey = instance, Subject = subject };

    /// <summary>One result is ignored, not the whole check: another library on a hard disk still shows.</summary>
    [Fact]
    public void TheKeyNamesTheOneResult()
    {
        Assert.Equal(@"F12.gamesOnHdd|E:\SteamLibrary", MainViewModel.IgnoreKey(F(null, @"E:\SteamLibrary")));
        Assert.NotEqual(MainViewModel.IgnoreKey(F(null, @"E:\SteamLibrary")), MainViewModel.IgnoreKey(F(null, @"F:\Games")));
        Assert.Equal("F12.gamesOnHdd#disk1", MainViewModel.IgnoreKey(F("disk1", "anything")));
        Assert.Equal("F12.gamesOnHdd", MainViewModel.IgnoreKey(F(null, null)));
    }
}
