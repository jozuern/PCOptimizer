using System.Diagnostics;
using Optimizer.Core.Platform;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Tests;

/// <summary>Settings > Uninstall: the cleanup after the app has ended, and the hand-over to winget.</summary>
public class SelfUninstallTests
{
    [Fact]
    public void CleanupScriptQuotesEveryPath()
    {
        var script = SelfUninstall.CleanupScript("PCOptimizer (1)", @"C:\ProgramData\PCOptimizer", @"C:\Users\O'Brien\Downloads\PCOptimizer (1).exe");
        Assert.Contains("Wait-Process -Name 'PCOptimizer (1)' -Timeout 60", script);
        Assert.Contains(@"$root = 'C:\ProgramData\PCOptimizer'", script);
        Assert.Contains(@"$files = @('C:\Users\O''Brien\Downloads\PCOptimizer (1).exe', 'C:\Users\O''Brien\Downloads\PCOptimizer (1).exe.old')", script);
        Assert.Contains($"& '{Path.Combine(Environment.SystemDirectory, "cmd.exe")}' /d /c rd /s /q $root", script);
    }

    [Fact]
    public void WingetCopyKeepsItsExeForWinget()
    {
        var script = SelfUninstall.CleanupScript("PCOptimizer", @"C:\ProgramData\PCOptimizer", null);
        Assert.Contains("$files = @()", script);
        Assert.DoesNotContain(".exe", script.Replace("cmd.exe", "", StringComparison.Ordinal));
    }

    [Fact]
    public void WingetUninstallWaitsForTheAppAndRemovesThisPackage()
    {
        var args = WingetPackage.UninstallArguments("jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe", "PCOptimizer", "Removing", "Failed \"here\"");
        Assert.StartsWith("-NoProfile -Command \"", args);
        Assert.EndsWith("\"", args);
        Assert.Equal(2, args.Count(c => c == '"')); // only the quotes around the command
        var wait = args.IndexOf("Wait-Process -Name 'PCOptimizer'", StringComparison.Ordinal);
        var remove = args.IndexOf("winget uninstall --product-code 'jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe' ", StringComparison.Ordinal);
        Assert.True(wait > 0 && remove > wait);
        Assert.DoesNotContain("Start-Process", args);
    }

    [Theory]
    [InlineData(@"C:\Users\a\AppData\Local\Microsoft\WinGet\Packages\jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe\PCOptimizer.exe", "jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe")]
    [InlineData(@"C:\Users\a\AppData\Local\Microsoft\WinGet\Packages\jozuern.PCOptimizer__DefaultSource\PCOptimizer.exe", "jozuern.PCOptimizer__DefaultSource")]
    [InlineData(@"C:\Users\a\Downloads\PCOptimizer.exe", null)]
    public void ProductCodeIsThePackageFolder(string exe, string? expected) => Assert.Equal(expected, WingetPackage.PackageFolder(exe));

    /// <summary>
    /// Runs the real script on temp folders only (listed in <see cref="ArchitectureTests"/>): the data folder and the exe
    /// go, and a junction inside the data folder is removed without deleting what it points to.
    /// </summary>
    [Fact]
    public void CleanupDeletesDataFolderAndExeButNotWhatALinkPointsTo()
    {
        var folder = TestFolders.Create("uninstall");
        try
        {
            var data = Path.Combine(folder, "data");
            var outside = Path.Combine(folder, "outside");
            Directory.CreateDirectory(Path.Combine(data, "logs"));
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(data, "logs", "app.log"), "log");
            File.WriteAllText(Path.Combine(outside, "keep.txt"), "keep");
            TestFolders.Junction(Path.Combine(data, "link"), outside);
            var exe = Path.Combine(folder, "PCOptimizer.exe");
            File.WriteAllBytes(exe, []);
            File.WriteAllBytes(exe + ".old", []);

            var script = SelfUninstall.CleanupScript("PCOptimizerTestNoSuchProcess", data, exe);
            var psi = new ProcessStartInfo(ProcessHardening.ResolveSystemTool("powershell.exe"), SelfUninstall.PowerShellArguments(script))
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            ProcessHardening.Apply(psi); // as the app starts it
            using (var p = Process.Start(psi)!)
                Assert.True(p.WaitForExit(60_000), "the cleanup script did not finish");

            Assert.False(Directory.Exists(data));
            Assert.False(File.Exists(exe));
            Assert.False(File.Exists(exe + ".old"));
            Assert.True(File.Exists(Path.Combine(outside, "keep.txt")));
        }
        finally
        {
            TestFolders.Delete(folder);
        }
    }
}
