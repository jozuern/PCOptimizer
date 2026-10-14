using Optimizer.Core.Storage;
using Optimizer.Core.Tools;

namespace Optimizer.Core.Tests;

/// <summary>What the storage page may move to the Recycle Bin. Only refusals are tested: the bin is the real user's.</summary>
public sealed class RecycleRulesTests : IDisposable
{
    private readonly string _root = TestFolders.Create("recycle-rules");

    public void Dispose() => TestFolders.Delete(_root);

    [Fact]
    public void SystemFileIsRefused()
    {
        Assert.Equal(RecycleBlock.SystemFile, RecycleRules.Check(@"C:\pagefile.sys", folder: false, systemFile: true, [], [], appData: false));
    }

    [Fact]
    public void DriveRootIsKept()
    {
        var kept = RecycleRules.KeptFolders();
        var system = Path.GetPathRoot(Environment.SystemDirectory)!;
        Assert.Equal(RecycleBlock.KeptFolder, RecycleRules.Check(system, folder: true, systemFile: false, [], kept, appData: false));
    }

    [Fact]
    public void ProfileRootIsKeptButItsFilesAreNot()
    {
        var kept = RecycleRules.KeptFolders();
        var profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Assert.Equal(RecycleBlock.KeptFolder, RecycleRules.Check(profile, folder: true, systemFile: false, [], kept, appData: false));
        Assert.Equal(RecycleBlock.KeptFolder, RecycleRules.Check(Path.Combine(profile, "Downloads"), folder: true, systemFile: false, [], kept, appData: false));
        Assert.Null(RecycleRules.Check(Path.Combine(profile, "Downloads", "setup.exe"), folder: false, systemFile: false, [], kept, appData: false));
        Assert.Null(RecycleRules.Check(Path.Combine(profile, "Downloads", "old"), folder: true, systemFile: false, [], kept, appData: false));
    }

    [Fact]
    public void ProtectedPathIsRefusedBeforeContainment()
    {
        var games = Path.Combine(_root, "games") + "\\";
        Assert.Equal(RecycleBlock.Protected, RecycleRules.Check(Path.Combine(games, "g.pak"), folder: false, systemFile: false, [games], [], appData: false));
    }

    [Fact]
    public void FolderThatContainsAProtectedRootIsRefused()
    {
        var x = Path.Combine(_root, "x");
        var protectedRoots = new[] { Path.Combine(x, "games") + "\\" };
        Assert.Equal(RecycleBlock.ContainsProtected, RecycleRules.Check(x, folder: true, systemFile: false, protectedRoots, [], appData: false));
        Assert.Null(RecycleRules.Check(Path.Combine(x, "other"), folder: true, systemFile: false, protectedRoots, [], appData: false));
    }

    [Fact]
    public void FolderThatContainsAKeptFolderIsRefused()
    {
        var x = Path.Combine(_root, "x");
        var kept = new[] { Path.Combine(x, "profile") + "\\" };
        Assert.Equal(RecycleBlock.ContainsProtected, RecycleRules.Check(x, folder: true, systemFile: false, [], kept, appData: false));
        Assert.Null(RecycleRules.Check(Path.Combine(x, "other"), folder: true, systemFile: false, [], kept, appData: false));
    }

    [Fact]
    public void FolderWithAnAppDataFolderInsideIsRefusedOnlyWhenAppDataIsProtected()
    {
        var home = Path.Combine(_root, "home");
        Directory.CreateDirectory(Path.Combine(home, "AppData"));
        // %TEMP% lies in AppData itself, so with the check on the folder is Protected before the contents are looked at.
        Assert.NotNull(RecycleRules.Check(home, folder: true, systemFile: false, [], [], appData: true));
        Assert.Null(RecycleRules.Check(home, folder: true, systemFile: false, [], [], appData: false));
    }

    [Fact]
    public void RecycleItemsRefusesAFolderWithAJunctionAndAKeptFolder()
    {
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);
        File.WriteAllText(Path.Combine(target, "keep.txt"), "x");
        var b = Path.Combine(_root, "b");
        Directory.CreateDirectory(b);
        TestFolders.Junction(Path.Combine(b, "link"), target);
        var kept = Path.Combine(_root, "kept");
        Directory.CreateDirectory(kept);

        var failed = StorageAnalyzer.RecycleItems([(b, true), (kept, true)], [], [kept + "\\"], appData: false);

        Assert.Equal([b, kept], failed);
        Assert.True(Directory.Exists(b));
        Assert.True(Directory.Exists(kept));
        Assert.True(File.Exists(Path.Combine(target, "keep.txt")));
    }
}
