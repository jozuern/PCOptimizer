using Optimizer.Core.Platform;

namespace Optimizer.Core.Tests;

/// <summary>The data folder from before the rename (PCOptimizer) moves to the new name once, with everything in it.</summary>
public class DataPathsRenameTests
{
    [Fact]
    public void OldFolderMovesWithItsBackups()
    {
        var baseFolder = TestFolders.Create("datapaths");
        try
        {
            var legacy = Path.Combine(baseFolder, DataPaths.LegacyFolderName);
            Directory.CreateDirectory(Path.Combine(legacy, "backups"));
            File.WriteAllText(Path.Combine(legacy, "backups", "a.json"), "{}");

            var root = DataPaths.ResolveRoot(baseFolder);

            Assert.Equal(Path.Combine(baseFolder, DataPaths.FolderName), root);
            Assert.True(File.Exists(Path.Combine(root, "backups", "a.json")));
            Assert.False(Directory.Exists(legacy));
        }
        finally
        {
            TestFolders.Delete(baseFolder);
        }
    }

    [Fact]
    public void NewFolderWinsAndTheOldOneStays()
    {
        var baseFolder = TestFolders.Create("datapaths");
        try
        {
            Directory.CreateDirectory(Path.Combine(baseFolder, DataPaths.FolderName));
            Directory.CreateDirectory(Path.Combine(baseFolder, DataPaths.LegacyFolderName));
            Assert.Equal(Path.Combine(baseFolder, DataPaths.FolderName), DataPaths.ResolveRoot(baseFolder));
            Assert.True(Directory.Exists(Path.Combine(baseFolder, DataPaths.LegacyFolderName)));
        }
        finally
        {
            TestFolders.Delete(baseFolder);
        }
    }

    [Fact]
    public void LinkInPlaceOfTheOldFolderIsNotMoved()
    {
        var baseFolder = TestFolders.Create("datapaths");
        try
        {
            var elsewhere = Path.Combine(baseFolder, "elsewhere");
            Directory.CreateDirectory(elsewhere);
            TestFolders.Junction(Path.Combine(baseFolder, DataPaths.LegacyFolderName), elsewhere);
            Assert.Equal(Path.Combine(baseFolder, DataPaths.FolderName), DataPaths.ResolveRoot(baseFolder));
            Assert.False(Directory.Exists(Path.Combine(baseFolder, DataPaths.FolderName)));
            Assert.True(Directory.Exists(elsewhere));
        }
        finally
        {
            TestFolders.Delete(baseFolder);
        }
    }

    /// <summary>
    /// Reading the data folder changes nothing: only the app's start moves the old folder. Test runs (which read
    /// DataPaths through the log) once renamed a developer's real folder.
    /// </summary>
    [Fact]
    public void LocatingTheFolderMovesNothing()
    {
        var baseFolder = TestFolders.Create("datapaths");
        try
        {
            var legacy = Path.Combine(baseFolder, DataPaths.LegacyFolderName);
            Directory.CreateDirectory(legacy);
            Assert.Equal(legacy, DataPaths.Locate(baseFolder));
            Assert.True(Directory.Exists(legacy));
            Assert.False(Directory.Exists(Path.Combine(baseFolder, DataPaths.FolderName)));
        }
        finally
        {
            TestFolders.Delete(baseFolder);
        }
    }

    [Fact]
    public void NoFolderYetGivesTheNewName()
    {
        var baseFolder = TestFolders.Create("datapaths");
        try
        {
            Assert.Equal(Path.Combine(baseFolder, DataPaths.FolderName), DataPaths.ResolveRoot(baseFolder));
        }
        finally
        {
            TestFolders.Delete(baseFolder);
        }
    }
}
