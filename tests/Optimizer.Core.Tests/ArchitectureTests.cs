using System.Text.RegularExpressions;

namespace Optimizer.Core.Tests;

/// <summary>
/// Rules that keep "tests never change the real PC" (CLAUDE.md) a property of the code instead of a convention: the real
/// system adapters are reached only through <see cref="ReadOnlyContext"/>, and only the listed tests start real tools.
/// </summary>
public partial class ArchitectureTests
{
    /// <summary>Tests that start a real process themselves, each reviewed: what they run changes nothing.</summary>
    private static readonly Dictionary<string, string> AllowedToolRuns = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BenchmarkHealthTests.cs"] = "cmd.exe starting ping to 127.0.0.1 (the output pipe test)",
        ["SelfUninstallTests.cs"] = "powershell.exe running the uninstall cleanup on temp folders only",
    };

    [Fact]
    public void TestsReachTheRealSystemOnlyReadOnly()
    {
        var problems = new List<string>();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepoPaths.Root, "tests", "Optimizer.Core.Tests"), "*.cs"))
        {
            var name = Path.GetFileName(file);
            if (name is "ReadOnlyContext.cs" or "ArchitectureTests.cs") continue;
            var text = File.ReadAllText(file);
            if (text.Contains("SystemNotify.CreateContext(", StringComparison.Ordinal)) problems.Add($"{name}: SystemNotify.CreateContext (use ReadOnlyContext.Create)");
            foreach (Match m in RealAdapter().Matches(text)) problems.Add($"{name}: {m.Value} (use ReadOnlyContext.Create)");
            if (text.Contains("StreamingProcess.RunAsync(", StringComparison.Ordinal) && !AllowedToolRuns.ContainsKey(name))
                problems.Add($"{name}: StreamingProcess.RunAsync (review it and list it in AllowedToolRuns)");
        }
        Assert.Empty(problems);
    }

    [Theory]
    [InlineData("bcdedit.exe", "/enum {current} /v", false)]
    [InlineData("dism.exe", "/Online /English /Get-ReservedStorageState", false)]
    [InlineData("powershell.exe", "-NoProfile -NonInteractive -Command \"(Get-MMAgent).MemoryCompression\"", false)]
    [InlineData("bcdedit.exe", "/set {current} useplatformclock true", true)]
    [InlineData("powercfg.exe", "/hibernate off", true)]
    [InlineData("dism.exe", "/Online /English /Set-ReservedStorageState /State:Disabled", true)]
    [InlineData("powershell.exe", "-NoProfile -NonInteractive -Command \"Disable-MMAgent -MemoryCompression\"", true)]
    [InlineData("net.exe", "stop wuauserv /y", true)]
    public void TheReadOnlyRunnerRefusesChanges(string file, string arguments, bool refused) =>
        Assert.Equal(refused, ReadOnlyProcesses.LooksLikeAWrite(file, arguments));

    [Fact]
    public void AnEncodedScriptIsCheckedToo()
    {
        static string Encoded(string script) => Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script));
        Assert.False(ReadOnlyProcesses.LooksLikeAWrite("powershell.exe", $"-NoProfile -EncodedCommand {Encoded("Get-AppxPackage -AllUsers | ConvertTo-Json")}"));
        Assert.True(ReadOnlyProcesses.LooksLikeAWrite("powershell.exe", $"-NoProfile -EncodedCommand {Encoded("Remove-AppxPackage x")}"));
    }

    /// <summary>The real adapter classes (SystemPowerManager, SystemBcdStore, SystemRegistryRoots and the others).</summary>
    [GeneratedRegex(@"new System[A-Z][A-Za-z]*(Manager|Store|Scheduler|Roots|Runner|Settings)\(")]
    private static partial Regex RealAdapter();
}
