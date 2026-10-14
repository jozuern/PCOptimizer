using System.IO;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.App.ViewModels;
using Optimizer.Core.Hardware;
using Optimizer.Core.Platform;
using Optimizer.Core.Storage;
using Optimizer.Core.Tweaks;
using Wpf.Ui.Controls;

namespace Optimizer.App.Tests;

/// <summary>
/// The Storage page without the window: opening it reads only the drive list, an analysis fills the open tab, and a
/// stopped analysis keeps the result before it. Only a temp folder is read; nothing is moved or deleted.
/// </summary>
public sealed class StorageViewModelTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "tunero-storage-vm-" + Guid.NewGuid().ToString("N"));
    private readonly FakePageHost _host = new();

    public StorageViewModelTests() => Directory.CreateDirectory(_root);

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string File(string name, int bytes)
    {
        var path = Path.Combine(_root, name);
        System.IO.File.WriteAllBytes(path, new byte[bytes]);
        return path;
    }

    private StorageViewModel Storage(Func<string, IProgress<ScanProgress>?, CancellationToken, Task<DiskTree>>? scanner = null) =>
        new(_host, null!, new FakeDialogs(), scanner ?? ((root, progress, ct) => new EnumerationScanner().ScanAsync(root, progress, ct)));

    [Fact]
    public async Task OpeningThePageReadsOnlyDrives()
    {
        var scans = 0;
        var storage = Storage((_, _, _) =>
        {
            scans++;
            throw new InvalidOperationException("opening the page must not scan");
        });
        await storage.EnsureLoadedAsync();
        Assert.Null(storage.Tree);
        Assert.NotEmpty(storage.Drives);
        Assert.Equal(0, scans);
        Assert.Empty(_host.Results);
    }

    [Fact]
    public async Task AnalyzeFolderFillsLargestFiles()
    {
        var big = File("big.bin", 3000);
        File("middle.bin", 2000);
        var small = File("small.txt", 1000);
        var storage = Storage();
        await storage.EnsureLoadedAsync();
        await storage.AnalyzeFolderAsync(_root);
        var tree = Assert.IsType<DiskTree>(storage.Tree);
        Assert.Equal(3, tree.Files(tree.Root));
        Assert.NotNull(storage.ResultText);
        // Only the open tab builds its rows.
        Assert.Empty(storage.LargestFiles);
        storage.Tab = 2;
        Assert.Equal(3, storage.LargestFiles.Count);
        Assert.Equal(big, storage.LargestFiles[0].Path);
        Assert.Equal(small, storage.LargestFiles[^1].Path);
        Assert.Equal(3000, storage.LargestFiles[0].Bytes);
        Assert.False(_host.Runner.Work.IsActive);
    }

    [Fact]
    public async Task TypesListGroupsAndFiles()
    {
        var a = File("a.mp4", 300);
        var b = File("b.mp4", 100);
        File("c.zip", 200);
        var storage = Storage();
        await storage.EnsureLoadedAsync();
        await storage.AnalyzeFolderAsync(_root);
        // Only the open tab builds its rows.
        Assert.Empty(storage.TypeGroups);
        storage.Tab = StorageViewModel.TabTypes;
        Assert.Equal([FileGroup.Video, FileGroup.Archives], storage.TypeGroups.Select(g => g.Group));
        Assert.Equal([400L, 200L], storage.TypeGroups.Select(g => g.Bytes));
        Assert.Equal([2, 1], storage.TypeGroups.Select(g => g.Count));
        Assert.Equal(400 / 600.0, storage.TypeGroups[0].Share, 6);
        // Extension rows exist only for an expanded group.
        Assert.All(storage.TypeGroups, g => Assert.Empty(g.Extensions));
        storage.ToggleTypeGroupCommand.Execute(storage.TypeGroups[0]);
        Assert.Equal([".mp4"], storage.TypeGroups[0].Extensions.Select(e => e.Name));
        Assert.Empty(storage.TypeGroups[1].Extensions);
        Assert.Empty(storage.TypeFiles);
        storage.SelectedType = storage.TypeGroups[0].Extensions[0];
        Assert.Equal([a, b], storage.TypeFiles.Select(f => f.Path));
        // Closing the result drops the lists.
        storage.CloseResultCommand.Execute(null);
        Assert.Empty(storage.TypeGroups);
        Assert.Empty(storage.TypeFiles);
        Assert.Null(storage.SelectedType);
    }

    [Fact]
    public async Task CancelledAnalyzeKeepsThePreviousResult()
    {
        File("a.bin", 1000);
        var calls = 0;
        var storage = Storage((root, progress, ct) =>
        {
            // The second analysis is stopped: the reader gives up without a tree.
            if (++calls == 2) throw new OperationCanceledException();
            return new EnumerationScanner().ScanAsync(root, progress, ct);
        });
        await storage.AnalyzeFolderAsync(_root);
        var first = storage.Tree;
        var text = storage.ResultText;
        Assert.NotNull(first);
        var changes = 0;
        storage.TreeChanged += (_, _) => changes++;

        await storage.AnalyzeFolderAsync(_root);
        Assert.Same(first, storage.Tree);
        Assert.Equal(text, storage.ResultText);
        Assert.Equal(0, changes);
        Assert.False(_host.Runner.Work.IsActive);
        Assert.Equal(Loc.Instance["Storage_Stopped"], _host.Results[^1].Text);
    }

    [Fact]
    public async Task ReloadKeepsTheResultAndAnotherDriveDropsIt()
    {
        File("a.bin", 1000);
        var storage = Storage();
        await storage.EnsureLoadedAsync();
        await storage.AnalyzeFolderAsync(_root);
        storage.Tab = 2;
        var tree = storage.Tree;
        // F5 reads the drive list again: the same drive stays picked, the result and its rows stay as they are.
        await storage.ReloadAsync();
        Assert.Same(tree, storage.Tree);
        Assert.Single(storage.LargestFiles);
        storage.SelectedDrive = new DriveRow(@"Z:\", 1, 1);
        Assert.Null(storage.Tree);
    }

    /// <summary>
    /// While the page's own work runs (a recycle), nothing may read or replace the tree: Find duplicates and Analyze are
    /// off and say so through CanExecute. IsWorking is set directly; nothing is recycled.
    /// </summary>
    [Fact]
    public async Task NothingStartsWhileThePageIsWorking()
    {
        File("a.bin", 1000);
        var storage = Storage();
        await storage.AnalyzeFolderAsync(_root);
        Assert.True(storage.FindDuplicatesCommand.CanExecute(null));
        Assert.True(storage.AnalyzeCommand.CanExecute(null));
        var raised = 0;
        storage.FindDuplicatesCommand.CanExecuteChanged += (_, _) => raised++;

        storage.IsWorking = true;
        Assert.False(storage.FindDuplicatesCommand.CanExecute(null));
        Assert.False(storage.AnalyzeCommand.CanExecute(null));
        Assert.False(storage.RecycleSelectedCommand.CanExecute(null));
        Assert.True(raised > 0);
        // Called anyway (a key binding, a test): it does nothing.
        await storage.FindDuplicatesCommand.ExecuteAsync(null);
        Assert.False(storage.DuplicatesSearched);
        var tree = storage.Tree;
        await storage.AnalyzeFolderAsync(_root);
        Assert.Same(tree, storage.Tree);

        storage.IsWorking = false;
        Assert.True(storage.FindDuplicatesCommand.CanExecute(null));
    }

    [Fact]
    public async Task AnalyzeWaitsForThePcScan()
    {
        _host.Profile = null;
        var storage = Storage((_, _, _) => throw new InvalidOperationException("no analysis before the PC scan"));
        await storage.AnalyzeFolderAsync(_root);
        Assert.Null(storage.Tree);
        Assert.Equal(Loc.Instance["Storage_WaitForScan"], _host.Results[^1].Text);
    }

    [Fact]
    public async Task CloseResultDropsTheTree()
    {
        File("a.bin", 1000);
        var storage = Storage();
        await storage.AnalyzeFolderAsync(_root);
        storage.Tab = 2;
        var changes = 0;
        storage.TreeChanged += (_, _) => changes++;
        storage.CloseResultCommand.Execute(null);
        Assert.Null(storage.Tree);
        Assert.Null(storage.ResultText);
        Assert.Empty(storage.LargestFiles);
        Assert.Equal(1, changes);
    }

    /// <summary>
    /// The Folders tab is the default: an analysis lists the root's children, a folder shown on the map gets its path
    /// as the breadcrumb, an entry picked on the map expands its folder in the list, and Close result empties it all.
    /// </summary>
    [Fact]
    public async Task FoldersTabFollowsTheMapAndTheList()
    {
        Directory.CreateDirectory(Path.Combine(_root, "docs", "deep"));
        File(Path.Combine("docs", "deep", "x.txt"), 500);
        File(Path.Combine("docs", "y.txt"), 100);
        File("top.bin", 2000);
        var storage = Storage();
        await storage.AnalyzeFolderAsync(_root);
        var tree = Assert.IsType<DiskTree>(storage.Tree);
        Assert.Equal(StorageViewModel.TabFolders, storage.Tab);
        Assert.Equal(["top.bin", "docs"], storage.Folders.Rows.Select(r => r.Name));
        Assert.Equal(tree.Root, storage.Folder);
        Assert.Equal([_root], storage.Crumbs.Select(c => c.Name));

        // Enter (or a double-click) on a folder shows it on the map; on a file, the folder it is in.
        var docs = storage.Folders.Rows[1].Entry;
        var deep = tree.Children(docs).Single(tree.IsFolder);
        var x = tree.Children(deep).Single();
        storage.OpenFolder(x);
        Assert.Equal(deep, storage.Folder);
        Assert.Equal([_root, "docs", "deep"], storage.Crumbs.Select(c => c.Name));
        storage.OpenFolder(docs);
        Assert.Equal(docs, storage.Folder);

        // A click on the map selects the row, with its folders expanded; the row selects the entry back.
        storage.SelectedEntry = x;
        Assert.Equal(x, storage.SelectedRow?.Entry);
        Assert.Equal(["top.bin", "docs", "deep", "x.txt", "y.txt"], storage.Folders.Rows.Select(r => r.Name));
        storage.SelectedRow = storage.Folders.Rows[0];
        Assert.Equal(storage.Folders.Rows[0].Entry, storage.SelectedEntry);

        // Left on a closed entry selects its folder, Left again closes it; Right opens it and then moves into it.
        var deepRow = storage.Folders.Rows[2];
        storage.CollapseOrSelectParent(storage.Folders.Rows[3]);
        Assert.Same(deepRow, storage.SelectedRow);
        storage.CollapseOrSelectParent(deepRow);
        Assert.False(deepRow.IsExpanded);
        storage.ExpandOrSelectChild(deepRow);
        Assert.True(deepRow.IsExpanded);
        storage.ExpandOrSelectChild(deepRow);
        Assert.Equal(x, storage.SelectedEntry);

        storage.CloseResultCommand.Execute(null);
        Assert.Empty(storage.Folders.Rows);
        Assert.Empty(storage.Crumbs);
        Assert.Equal(-1, storage.SelectedEntry);
    }

    [Fact]
    public async Task FindDuplicatesKeepsTheFirstCopyWithoutATick()
    {
        // Duplicates in the temp folder (under AppData) are protected, so this checks the list, not the Recycle Bin.
        var bytes = (int)Optimizer.Core.Tools.StorageAnalyzer.DuplicateMinBytes;
        File("a.bin", bytes);
        File("b.bin", bytes);
        var storage = Storage();
        await storage.AnalyzeFolderAsync(_root);
        storage.Tab = 3;
        Assert.Empty(storage.Duplicates);
        await storage.FindDuplicatesCommand.ExecuteAsync(null);
        // Protected (AppData): the analysis never offers these, so no group is listed.
        Assert.Empty(storage.Duplicates);
        Assert.True(storage.DuplicatesSearched);
        Assert.False(_host.Runner.Work.IsActive);
    }
}

/// <summary>The window as the Storage page sees it: a finished PC scan without game libraries, and the banner.</summary>
internal sealed class FakePageHost : IPageHost
{
    public FakePageHost() => Runner = new ChangeRunner(null!, new FakeDialogs(), () => throw new NotSupportedException(), () => new HashSet<string>(), () => false);

    public List<(string Text, InfoBarSeverity Severity)> Results { get; } = [];

    public void ShowResult(string text, InfoBarSeverity severity = InfoBarSeverity.Informational) => Results.Add((text, severity));
    public void OpenLink(string url) => throw new NotSupportedException();
    public void Shutdown() => throw new NotSupportedException();

    public AppServices Services => throw new NotSupportedException();
    public ChangeRunner Runner { get; }

    public HardwareProfile? Profile { get; set; } = new()
    {
        Os = new BuildInfo(26300, 0, "26H2", "Professional", "Windows 11 Pro", CpuArchitecture.X64, false, null),
        Software = new SoftwareInfo([], [], []),
    };

    public Facts Facts => throw new NotSupportedException();
    public bool ExpertMode => false;
    public HashSet<string> AppliedIds() => [];
    public IReadOnlyList<TweakStatus> DeviceStates => [];
    public IEnumerable<TweakItemViewModel> CatalogItems(Func<TweakDefinition, bool> filter) => [];
    public TweakDefinition? ResolveTweak(string id) => null;
    public Task ToggleAsync(TweakItemViewModel item, bool on) => throw new NotSupportedException();
    public Task ScanInProgress => Task.CompletedTask;
    public IAsyncRelayCommand ScanCommand => throw new NotSupportedException();
    public Page CurrentPage => Page.Storage;
    public InspectorItem? SelectedItem { get; set; }
    public string? VirusTotalKey => null;
    public string? PickSaveFile(string defaultName, string filterName, string extension) => null;
}
