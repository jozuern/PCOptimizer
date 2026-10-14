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
    public void PathWithDotDotIsCheckedWhereItLeads()
    {
        // Pure Check test: the roots are fake and nothing is touched. Both spellings lead into games\.
        var games = Path.Combine(_root, "games") + "\\";
        var sneaky = Path.Combine(_root, "x", "..", "games", "g.pak");
        Assert.Equal(RecycleBlock.Protected, RecycleRules.Check(sneaky, folder: false, systemFile: false, [games], [], appData: false));
        Assert.Equal(RecycleBlock.Protected, RecycleRules.Check(Path.Combine(_root, "x", "..", "games"), folder: true, systemFile: false, [games], [], appData: false));
        Assert.EndsWith(@"\games\g.pak", RecycleRules.Normalize(sneaky));
    }

    [Fact]
    public void RootsAndKeptFoldersInAnotherSpellingStillMatch()
    {
        // The same folders spelled with a ".." segment and other case (an 8.3 name cannot be forced here).
        var games = Path.Combine(_root, "x", "..", "GAMES") + "\\";
        var kept = Path.Combine(_root, "y", "..", "KEPT") + "\\";
        Assert.Equal(RecycleBlock.Protected, RecycleRules.Check(Path.Combine(_root, "games", "g.pak"), folder: false, systemFile: false, [games], [], appData: false));
        Assert.Equal(RecycleBlock.KeptFolder, RecycleRules.Check(Path.Combine(_root, "kept"), folder: true, systemFile: false, [], [kept], appData: false));
        Assert.Equal(RecycleBlock.ContainsProtected, RecycleRules.Check(_root, folder: true, systemFile: false, [games], [], appData: false));
    }

    [Fact]
    public void ShortNameOfAKeptFolderIsStillKept()
    {
        // The long name of the temp folder differs from its 8.3 form on most machines; where it does not, the test still holds.
        var kept = Path.Combine(_root, "Long Folder Name For Kept");
        Directory.CreateDirectory(kept);
        var set = new[] { RecycleRules.Normalize(kept) + "\\" };
        Assert.Equal(RecycleBlock.KeptFolder, RecycleRules.Check(kept, folder: true, systemFile: false, [], set, appData: false));
        Assert.Equal(RecycleRules.Normalize(kept), RecycleRules.Normalize(kept + @"\sub\.."));
    }

    [Fact]
    public void HoldsFolderLinkFindsAJunctionAtAnyDepth()
    {
        var target = Path.Combine(_root, "target");
        Directory.CreateDirectory(target);
        var plain = Path.Combine(_root, "plain", "inner");
        Directory.CreateDirectory(plain);
        var direct = Path.Combine(_root, "direct");
        Directory.CreateDirectory(direct);
        TestFolders.Junction(Path.Combine(direct, "link"), target);
        var deep = Path.Combine(_root, "deep", "a", "b");
        Directory.CreateDirectory(deep);
        TestFolders.Junction(Path.Combine(deep, "link"), target);

        Assert.False(StorageAnalyzer.HoldsFolderLink(Path.Combine(_root, "plain")));
        Assert.True(StorageAnalyzer.HoldsFolderLink(direct));
        Assert.True(StorageAnalyzer.HoldsFolderLink(Path.Combine(_root, "deep")));
        Assert.True(Directory.Exists(target));
    }

    [Fact]
    public void RecycleItemsRefusesKeptProtectedAndDotDotPathsBeforeAnyMove()
    {
        // Every item is refused by Check before MoveToRecycleBin can be reached: nothing here may reach the Recycle Bin.
        var kept = Path.Combine(_root, "kept");
        var games = Path.Combine(_root, "games");
        Directory.CreateDirectory(kept);
        Directory.CreateDirectory(games);
        var gameFile = Path.Combine(games, "g.pak");
        File.WriteAllText(gameFile, "x");
        var sneaky = Path.Combine(_root, "kept", "..", "games", "g.pak");
        var roots = new[] { games + "\\" };

        var failed = StorageAnalyzer.RecycleItems([(kept, true), (gameFile, false), (sneaky, false), (games, true)], roots, [kept + "\\"], appData: false);

        Assert.Equal([kept, gameFile, sneaky, games], failed);
        Assert.True(Directory.Exists(kept));
        Assert.True(File.Exists(gameFile));
    }
}
