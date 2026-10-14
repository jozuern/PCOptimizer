using Optimizer.Core.Storage;

namespace Optimizer.Core.Tests;

public class EnumerationScannerTests
{
    private static Task<DiskTree> Scan(string root, CancellationToken ct = default) =>
        new EnumerationScanner().ScanAsync(root, null, ct);

    private static int Find(DiskTree tree, int parent, string name) =>
        tree.Children(parent).Single(c => tree.Name(c) == name);

    [Fact]
    public async Task ReadsSizesAndNestedFolders()
    {
        var root = TestFolders.Create("enum");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "a", "b"));
            File.WriteAllBytes(Path.Combine(root, "a", "b", "c.bin"), new byte[1000]);
            File.WriteAllBytes(Path.Combine(root, "d.txt"), new byte[10]);
            var tree = await Scan(root);
            Assert.Equal(1010, tree.Size(tree.Root));
            Assert.Equal(2, tree.Files(tree.Root));
            Assert.Equal(ScanReader.Folders, tree.Reader);
            var c = Find(tree, Find(tree, Find(tree, tree.Root, "a"), "b"), "c.bin");
            Assert.Equal(Path.Combine(root, "a", "b", "c.bin"), tree.FullPath(c));
            Assert.Equal(1000, tree.Size(c));
        }
        finally { TestFolders.Delete(root); }
    }

    [Fact]
    public async Task JunctionIsListedButNotEntered()
    {
        var root = TestFolders.Create("enum");
        var other = TestFolders.Create("enum-target");
        try
        {
            File.WriteAllBytes(Path.Combine(other, "big.bin"), new byte[5000]);
            TestFolders.Junction(Path.Combine(root, "j"), other);
            var tree = await Scan(root);
            var j = Find(tree, tree.Root, "j");
            Assert.Equal(EntryFlags.Folder | EntryFlags.ReparsePoint, tree.Flags(j));
            Assert.Equal(RecycleBlock.Link, RecycleRules.CheckEntry(tree, j));
            Assert.Empty(tree.Children(j));
            Assert.Equal(0, tree.Size(tree.Root));
        }
        finally { TestFolders.Delete(root); TestFolders.Delete(other); }
    }

    [Fact]
    public async Task HiddenAndSystemFilesCount()
    {
        var root = TestFolders.Create("enum");
        try
        {
            var hidden = Path.Combine(root, "h.dat");
            var system = Path.Combine(root, "s.dat");
            File.WriteAllBytes(hidden, new byte[7]);
            File.WriteAllBytes(system, new byte[9]);
            File.SetAttributes(hidden, FileAttributes.Hidden);
            File.SetAttributes(system, FileAttributes.System | FileAttributes.Hidden);
            var tree = await Scan(root);
            Assert.Equal(16, tree.Size(tree.Root));
            Assert.Equal(EntryFlags.None, tree.Flags(Find(tree, tree.Root, "h.dat")));
            Assert.True(tree.Flags(Find(tree, tree.Root, "s.dat")).HasFlag(EntryFlags.System));
        }
        finally
        {
            foreach (var f in Directory.GetFiles(root)) File.SetAttributes(f, FileAttributes.Normal);
            TestFolders.Delete(root);
        }
    }

    [Fact]
    public async Task LongPathsWork()
    {
        var root = TestFolders.Create("enum");
        try
        {
            var dir = root;
            var prefix = @"\\?\";
            while (dir.Length < 300) dir = Path.Combine(dir, "folder-with-a-long-name-0123456789");
            Directory.CreateDirectory(prefix + dir);
            var file = Path.Combine(dir, "deep.bin");
            File.WriteAllBytes(prefix + file, new byte[42]);
            var tree = await Scan(root);
            Assert.Equal(42, tree.Size(tree.Root));
            var i = tree.Root;
            while (tree.IsFolder(i) && tree.Children(i).Any()) i = tree.Children(i).First();
            Assert.Equal("deep.bin", tree.Name(i));
            Assert.Equal(file, tree.FullPath(i));
        }
        finally { TestFolders.Delete(root); }
    }

    [Fact]
    public async Task CancelledScanThrows()
    {
        var root = TestFolders.Create("enum");
        try
        {
            File.WriteAllBytes(Path.Combine(root, "x"), new byte[1]);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Scan(root, cts.Token));
        }
        finally { TestFolders.Delete(root); }
    }
}

public class EnumerationLinkTests
{
    private const uint Reparse = 0x400, Directory = 0x10;

    [Theory]
    [InlineData(0xA0000003u, true)]   // IO_REPARSE_TAG_MOUNT_POINT (junction, mount point)
    [InlineData(0xA000000Cu, true)]   // IO_REPARSE_TAG_SYMLINK
    [InlineData(0x9000001Au, false)]  // IO_REPARSE_TAG_CLOUD (OneDrive placeholder)
    [InlineData(0x9000301Au, false)]  // IO_REPARSE_TAG_CLOUD_3
    public void OnlyNameSurrogatesAreLinks(uint tag, bool link) =>
        Assert.Equal(link, EnumerationScanner.IsLink(Directory | Reparse, tag));

    [Fact]
    public void WithoutReparseAttributeNothingIsALink() =>
        Assert.False(EnumerationScanner.IsLink(Directory, 0xA0000003u));
}
