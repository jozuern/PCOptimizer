using Optimizer.Core.Platform;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Tests;

/// <summary>The update through winget for a winget install, and the Start menu entry.</summary>
public class WingetPackageTests
{
    [Theory]
    [InlineData(@"C:\Users\a\AppData\Local\Microsoft\WinGet\Packages\jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe\PCOptimizer.exe", true)]
    [InlineData(@"C:\Program Files\WinGet\Packages\jozuern.PCOptimizer_Microsoft.Winget.Source_8wekyb3d8bbwe\PCOptimizer.exe", true)]
    [InlineData(@"D:\Portable\jozuern.PCOptimizer\PCOptimizer.exe", true)] // winget's package root moved in its settings
    [InlineData(@"C:\Users\a\Downloads\PCOptimizer.exe", false)]
    [InlineData(@"C:\Users\a\AppData\Local\Microsoft\WinGet\Packages\jozuern.PCOptimizerBeta_x\PCOptimizer.exe", false)]
    [InlineData(@"C:\Users\a\AppData\Local\Microsoft\WinGet\Packages\Other.App_Microsoft.Winget.Source_8wekyb3d8bbwe\PCOptimizer.exe", false)]
    public void RecognizesTheWingetPackageFolder(string exe, bool expected) =>
        Assert.Equal(expected, WingetPackage.IsInstalledWithWinget(exe));

    [Theory]
    [InlineData("plain", "'plain'")]
    [InlineData("it's", "'it''s'")]
    [InlineData("it\u2019s", "'it\u2019\u2019s'")]
    [InlineData("say \"hi\"", "'say hi'")]
    [InlineData(@"C:\Users\O'Brien\PCOptimizer (1).exe", @"'C:\Users\O''Brien\PCOptimizer (1).exe'")]
    public void QuotesForPowerShell(string text, string expected) => Assert.Equal(expected, WingetPackage.Quote(text));

    [Fact]
    public void UpdateWaitsForTheAppThenRunsWingetForThisPackageAndStartsTheAppAgain()
    {
        var args = WingetPackage.UpdateArguments(4242, @"C:\Apps\PCOptimizer.exe", "Updating", "Failed \"here\"");
        Assert.StartsWith("-NoProfile -Command \"", args);
        Assert.EndsWith("\"", args);
        Assert.Equal(2, args.Count(c => c == '"')); // only the quotes around the command
        var wait = args.IndexOf("Wait-Process -Id 4242", StringComparison.Ordinal);
        var upgrade = args.IndexOf("winget upgrade --id jozuern.PCOptimizer --exact --source winget", StringComparison.Ordinal);
        var start = args.IndexOf(@"Start-Process -FilePath 'C:\Apps\PCOptimizer.exe'", StringComparison.Ordinal);
        Assert.True(wait > 0 && upgrade > wait && start > upgrade);
    }

    [Fact]
    public void StartMenuEntryIsCreatedPointsToTheExeAndIsRemoved()
    {
        var folder = TestFolders.Create("startmenu");
        try
        {
            var exe = Path.Combine(folder, "PCOptimizer.exe");
            File.WriteAllBytes(exe, []);
            Assert.False(StartMenuShortcut.Exists(folder));
            Assert.Null(StartMenuShortcut.Target(folder));

            StartMenuShortcut.Create(folder, exe, "Test");
            Assert.True(StartMenuShortcut.Exists(folder));
            Assert.Equal(exe, StartMenuShortcut.Target(folder), ignoreCase: true);

            StartMenuShortcut.Remove(folder);
            Assert.False(StartMenuShortcut.Exists(folder));
            StartMenuShortcut.Remove(folder); // nothing left to remove
        }
        finally
        {
            TestFolders.Delete(folder);
        }
    }

    [Fact]
    public void StartMenuFolderOfAnotherAdminAccountIsTheSignedInUsers()
    {
        var mismatch = new ElevationInfo(true, "admin", "S-1-5-21-1-500", "a", "S-1-5-21-1-1001", false);
        Assert.Equal(@"C:\Users\a\AppData\Roaming\Microsoft\Windows\Start Menu\Programs", StartMenuShortcut.ProgramsFolder(mismatch, @"C:\Users\a"));
        Assert.Null(StartMenuShortcut.ProgramsFolder(mismatch, null));
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.Programs), StartMenuShortcut.ProgramsFolder(null, @"C:\Users\a"));
    }

    /// <summary>
    /// Administrator protection needs the policy (TypeOfAdminApprovalMode 2) and the ADMIN_ account: an ordinary account
    /// that happens to be called ADMIN_x is a separate admin account, which gets the banner.
    /// </summary>
    [Theory]
    [InlineData("ADMIN_joshua", @"PC\ADMIN_joshua", 2, true)]
    [InlineData("joshua", @"PC\ADMIN_k3xq", 2, true)]
    [InlineData("ADMIN_tools", @"PC\ADMIN_tools", 1, false)]
    [InlineData("ADMIN_tools", @"PC\ADMIN_tools", null, false)]
    [InlineData("admin", @"PC\admin", 2, false)]
    public void AdministratorProtectionNeedsThePolicyAndTheSystemManagedAccount(string profile, string process, int? policy, bool expected) =>
        Assert.Equal(expected, ElevationInfo.IsAdministratorProtection(profile, process, policy));
}
