using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Optimizer.App.Services;
using Optimizer.Core.Logging;
using Optimizer.Core.Storage;
using Optimizer.Core.Tools;
using Wpf.Ui.Controls;

namespace Optimizer.App.ViewModels;

/// <summary>
/// The Storage page: a drive (or folder) read into a <see cref="DiskTree"/> on request only, shown in four tabs. Opening
/// the page reads the drive list and nothing else; Analyze and Find duplicates run under the progress banner with Stop.
/// Only the open tab builds its rows.
/// </summary>
/// <param name="scanner">Reads a root into a tree; tests pass their own. Default: <see cref="VolumeScanners.ScanAsync"/>.</param>
public sealed partial class StorageViewModel(IPageHost owner, AppServices services, IDialogs dialogs,
    Func<string, IProgress<ScanProgress>?, CancellationToken, Task<DiskTree>>? scanner = null) : PageViewModel(owner)
{
    public const int TabFolders = 0, TabTypes = 1, TabLargest = 2, TabDuplicates = 3;

    private readonly Func<string, IProgress<ScanProgress>?, CancellationToken, Task<DiskTree>> _scan =
        scanner ?? ((root, progress, ct) => VolumeScanners.ScanAsync(root, services.Elevation.IsElevated, progress, ct));

    /// <summary>Protected roots of the last analysis (Windows, programs, game libraries of that PC scan).</summary>
    private IReadOnlyList<string> _protected = [];

    public ObservableCollection<DriveRow> Drives { get; } = [];
    public ObservableCollection<StorageFileRow> LargestFiles { get; } = [];
    public ObservableCollection<StorageFileRow> Duplicates { get; } = [];

    [ObservableProperty] private DriveRow? _selectedDrive;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult), nameof(NoResult), nameof(CanFindDuplicates))]
    [NotifyCanExecuteChangedFor(nameof(FindDuplicatesCommand))]
    private DiskTree? _tree;

    /// <summary>"120 GB in 84,000 files, read folder by folder in 12.3 s"; null without a result.</summary>
    [ObservableProperty] private string? _resultText;

    /// <summary>The open tab: <see cref="TabFolders"/>, <see cref="TabTypes"/>, <see cref="TabLargest"/>, <see cref="TabDuplicates"/>.</summary>
    [ObservableProperty] private int _tab;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private bool _isAnalyzing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    private bool _isFindingDuplicates;

    /// <summary>Find duplicates ran on the current tree (the tab shows its list, or that there are none).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NoDuplicates))]
    private bool _duplicatesSearched;

    /// <summary>"Duplicates: 1.2 GB can be freed" after Find duplicates.</summary>
    [ObservableProperty] private string? _duplicatesText;

    /// <summary>Something is ticked: "Move selected to Recycle Bin" has work to do.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRecycle))]
    [NotifyCanExecuteChangedFor(nameof(RecycleSelectedCommand))]
    private bool _hasSelection;

    public bool HasResult => Tree is not null;
    public bool NoResult => Tree is null;
    public bool IsBusy => IsAnalyzing || IsFindingDuplicates;

    // Analyze, Find duplicates and Recycle exclude each other: the tree is not thread safe, and a recycle removes entries
    // from it while Find duplicates would read it on a worker thread.
    public bool CanAnalyze => !IsBusy && !IsWorking;
    public bool CanFindDuplicates => Tree is not null && !IsBusy && !IsWorking;
    public bool CanRecycle => HasSelection && !IsBusy && !IsWorking;

    partial void OnIsAnalyzingChanged(bool value) => BusyChanged();
    partial void OnIsFindingDuplicatesChanged(bool value) => BusyChanged();
    protected override void WorkingChanged() => BusyChanged();

    private void BusyChanged()
    {
        OnPropertyChanged(nameof(CanAnalyze));
        OnPropertyChanged(nameof(CanFindDuplicates));
        OnPropertyChanged(nameof(CanRecycle));
        AnalyzeCommand.NotifyCanExecuteChanged();
        FindDuplicatesCommand.NotifyCanExecuteChanged();
        RecycleSelectedCommand.NotifyCanExecuteChanged();
    }
    public bool NoDuplicates => DuplicatesSearched && Duplicates.Count == 0;

    /// <summary>After a scan, Close result and a removal: views drawn from the tree (folders, types) build again.</summary>
    public event EventHandler? TreeChanged;

    private CancellationTokenSource? _cancel;
    private bool _largestFilled;
    private IReadOnlyList<DuplicateGroup> _duplicateGroups = [];
    private Dictionary<string, int> _duplicateEntries = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Only the drive list (used and free space): nothing on a drive is read before Analyze.</summary>
    protected override async Task LoadAsync()
    {
        // Asking a drive whether it is ready can take seconds (a sleeping disk): off the UI thread.
        var drives = await Task.Run(ReadDrives);
        var selected = SelectedDrive?.Root;
        // The combo box sets its selection to null while its rows are replaced: that is no other drive.
        _refreshingDrives = true;
        try
        {
            Drives.Clear();
            foreach (var d in drives) Drives.Add(d);
            SelectedDrive = Drives.FirstOrDefault(d => string.Equals(d.Root, selected, StringComparison.OrdinalIgnoreCase)) ?? Drives.FirstOrDefault();
        }
        finally
        {
            _refreshingDrives = false;
        }
        if (selected is not null && !string.Equals(SelectedDrive?.Root, selected, StringComparison.OrdinalIgnoreCase)) SetTree(null);
        // A language switch (which reads the drive list again) builds the texts drawn from the tree in the new language.
        if (_textLanguage != Lang)
        {
            _textLanguage = Lang;
            Relabel();
        }
    }

    private bool _refreshingDrives;
    private string? _textLanguage;

    private void Relabel()
    {
        if (Tree is { } tree) ResultText = Summary(tree);
        if (DuplicatesSearched) ShowDuplicates();
        LargestFiles.Clear();
        _largestFilled = false;
        UpdateSelection();
        FillTab();
    }

    private static List<DriveRow> ReadDrives()
    {
        var rows = new List<DriveRow>();
        foreach (var d in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
        {
            try
            {
                if (d.IsReady) rows.Add(new DriveRow(d.RootDirectory.FullName, d.TotalSize, d.TotalFreeSpace));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
            }
        }
        return rows;
    }

    /// <summary>Another drive: the result of the last one no longer belongs to the picker (a language switch keeps it).</summary>
    partial void OnSelectedDriveChanged(DriveRow? oldValue, DriveRow? newValue)
    {
        if (!_refreshingDrives && Tree is not null && !string.Equals(oldValue?.Root, newValue?.Root, StringComparison.OrdinalIgnoreCase)) SetTree(null);
    }

    partial void OnTabChanged(int value) => FillTab();

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private Task AnalyzeAsync() => SelectedDrive is { } drive ? AnalyzeRootAsync(drive.Root) : Task.CompletedTask;

    /// <summary>Analyzes a folder instead of a drive (the --storage-scan developer switch and tests).</summary>
    public Task AnalyzeFolderAsync(string folder) => AnalyzeRootAsync(folder);

    private async Task AnalyzeRootAsync(string root)
    {
        // One analysis at a time; not while duplicates are hashed from the tree it would replace or files are recycled.
        if (!CanAnalyze) return;
        // Game folders come from the PC scan; without it they could be offered for deletion.
        if (Owner.Profile?.Software is not { } software)
        {
            Owner.ShowResult(Loc.Instance["Storage_WaitForScan"], InfoBarSeverity.Warning);
            return;
        }
        _cancel = new CancellationTokenSource();
        IsAnalyzing = true;
        try
        {
            // Read for every analysis: a scan since the page opened may have found a new library.
            var protectedRoots = await Task.Run(() => StorageAnalyzer.ProtectedRoots(software.GameLibraryPaths.Concat(software.OtherGameFolders)));
            // Only reads: no change gate, but the banner (with Stop) like every long run. The tree before stays until the
            // new one is complete, so a stopped analysis leaves the page as it was.
            using var work = Owner.Runner.Work.Begin(Loc.Instance["Work_StorageScan"], _cancel);
            var progress = new Progress<ScanProgress>(p => work.Report(p.Percent, Loc.Instance.Format("Storage_Progress", p.Files, CleanupViewModel.Size(p.Bytes))));
            var tree = await _scan(root, progress, _cancel.Token);
            _protected = protectedRoots;
            Log.Info("storage", "analysis done", new { reader = tree.Reader.ToString(), files = tree.Files(tree.Root), lost = tree.Lost, ms = (long)tree.Elapsed.TotalMilliseconds });
            SetTree(tree);
        }
        catch (OperationCanceledException)
        {
            Owner.ShowResult(Loc.Instance["Storage_Stopped"]);
        }
        catch (Exception ex)
        {
            // The page stays usable with the result before (a root that vanished, a reader that failed).
            Log.Error("storage", "analysis failed", ex);
            Owner.ShowResult(Loc.Instance.Format("Result_Error", ex.Message), InfoBarSeverity.Error);
        }
        finally
        {
            IsAnalyzing = false;
            _cancel.Dispose();
            _cancel = null;
        }
    }

    [RelayCommand]
    private void CloseResult() => SetTree(null);

    /// <summary>A new tree (or none): every list drawn from the old one goes, and the open tab builds its rows again.</summary>
    private void SetTree(DiskTree? tree)
    {
        Tree = tree;
        ResultText = tree is null ? null : Summary(tree);
        _duplicateGroups = [];
        _duplicateEntries = new(StringComparer.OrdinalIgnoreCase);
        Duplicates.Clear();
        DuplicatesSearched = false;
        DuplicatesText = null;
        RefreshFromTree();
    }

    /// <summary>The tree changed (new, closed, entries removed): lists are built again when their tab is open.</summary>
    private void RefreshFromTree()
    {
        LargestFiles.Clear();
        _largestFilled = false;
        UpdateSelection();
        FillTab();
        TreeChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string Summary(DiskTree tree) => Loc.Instance.Format("Storage_Result", CleanupViewModel.Size(tree.Size(tree.Root)), tree.Files(tree.Root),
        Loc.Instance[tree.Reader == ScanReader.FileTable ? "Storage_ReaderFileTable" : "Storage_ReaderFolders"], tree.Elapsed.TotalSeconds);

    private void FillTab()
    {
        if (Tab == TabLargest && !_largestFilled && Tree is { } tree)
        {
            _largestFilled = true;
            foreach (var i in tree.LargestFiles(100))
            {
                var path = tree.FullPath(i);
                var system = (tree.Flags(i) & EntryFlags.System) != 0;
                LargestFiles.Add(new StorageFileRow(path, tree.Size(i),
                    tree.LastWriteUtc(i).ToLocalTime().ToString("d", System.Globalization.CultureInfo.CurrentCulture),
                    !system && !StorageAnalyzer.IsProtected(path, _protected), i, UpdateSelection));
            }
        }
    }

    /// <summary>
    /// Hashes files of equal size from the tree (at least 1 MB, not system, not cloud-only, not protected) on a worker
    /// thread under the banner with Stop. Nothing is hashed before this is pressed.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanFindDuplicates))]
    private async Task FindDuplicatesAsync()
    {
        if (!CanFindDuplicates || Tree is not { } tree) return;
        var protectedRoots = _protected;
        using var stop = new CancellationTokenSource();
        IsFindingDuplicates = true;
        try
        {
            using var work = Owner.Runner.Work.Begin(Loc.Instance["Work_FindDuplicates"], stop);
            var entries = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var groups = await Task.Run(() => StorageAnalyzer.FindDuplicates(StorageAnalyzer.DuplicateCandidates(tree, protectedRoots, entries: entries, ct: stop.Token), stop.Token));
            Log.Info("storage", "duplicates found", new { groups = groups.Count, files = groups.Sum(g => g.Paths.Count) });
            // A new analysis or Close result meanwhile: these belong to the old tree.
            if (!ReferenceEquals(tree, Tree)) return;
            _duplicateGroups = groups.Take(50).ToList();
            _duplicateEntries = entries;
            DuplicatesSearched = true;
            ShowDuplicates();
        }
        catch (OperationCanceledException)
        {
            Owner.ShowResult(Loc.Instance["Storage_Stopped"]);
        }
        finally
        {
            IsFindingDuplicates = false;
        }
    }

    /// <summary>The grouped list: the first copy of each group without a tick, the others offered.</summary>
    private void ShowDuplicates()
    {
        Duplicates.Clear();
        foreach (var g in _duplicateGroups)
            foreach (var (p, i) in g.Paths.Select((p, i) => (p, i)))
                Duplicates.Add(new StorageFileRow(p, g.BytesEach, i == 0 ? Loc.Instance.Format("Storage_DuplicateOf", g.Paths.Count) : "", i > 0,
                    _duplicateEntries.GetValueOrDefault(p, -1), UpdateSelection));
        DuplicatesText = _duplicateGroups.Count == 0 ? null
            : Loc.Instance.Format("Storage_DuplicatesFound", CleanupViewModel.Size(_duplicateGroups.Sum(d => d.Reclaimable)));
        OnPropertyChanged(nameof(NoDuplicates));
        UpdateSelection();
    }

    private void UpdateSelection() => HasSelection = LargestFiles.Concat(Duplicates).Any(f => f.Selected && f.CanRecycle);

    /// <summary>
    /// Moves the ticked files to the Recycle Bin after a confirmation that lists them. Every file is checked again by
    /// <see cref="StorageAnalyzer.Recycle"/>; the ones moved leave the tree and the lists.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanRecycle))]
    private async Task RecycleSelectedAsync()
    {
        if (!CanRecycle || Tree is not { } tree) return;
        var rows = LargestFiles.Concat(Duplicates).Where(f => f.Selected && f.CanRecycle).DistinctBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToList();
        var selected = rows.Select(f => f.Path).ToList();
        if (selected.Count == 0) return;
        // The first copy of a duplicate group can also be picked under the largest files: one copy always stays.
        if (StorageAnalyzer.WholeGroupsSelected(selected, _duplicateGroups) is [var whole, ..])
        {
            Owner.ShowResult(Loc.Instance.Format("Storage_KeepOneCopy", whole.Paths[0]), InfoBarSeverity.Warning);
            return;
        }
        var text = Loc.Instance.Format("Storage_RecycleText", selected.Count, string.Join("\n", selected.Take(10)));
        // The Recycle Bin belongs to the account this process runs as. With a separate admin account (or Administrator
        // protection) that is not the signed-in user's bin: say so before deleting.
        if (services.Elevation is { UserMismatch: true } e) text += "\n\n" + Loc.Instance.Format("Storage_RecycleOtherAccount", e.ProcessUser);
        if (!dialogs.Ask(Loc.Instance["Storage_RecycleTitle"], text, Loc.Instance["Storage_Recycle"])) return;
        var protectedRoots = _protected;
        IReadOnlyList<string> failed = selected;
        await RunWorkAsync("recycle files", Loc.Instance["Work_Recycle"], async work =>
        {
            var steps = new Progress<int>(done => work.Step(done, selected.Count, selected[done]));
            failed = await Task.Run(() => StorageAnalyzer.Recycle(selected, protectedRoots, progress: steps));
            Owner.ShowResult(Loc.Instance.Format("Storage_Recycled", selected.Count - failed.Count, failed.Count),
                failed.Count == 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning);
        });
        if (!ReferenceEquals(tree, Tree)) return;
        var gone = rows.Where(r => !failed.Contains(r.Path, StringComparer.OrdinalIgnoreCase)).ToList();
        if (gone.Count == 0) return;
        foreach (var r in gone.Where(r => r.Entry >= 0)) tree.Remove(r.Entry);
        var goneSet = gone.Select(r => r.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _duplicateGroups = _duplicateGroups.Select(g => g with { Paths = g.Paths.Where(p => !goneSet.Contains(p)).ToList() }).Where(g => g.Paths.Count > 1).ToList();
        ResultText = Summary(tree);
        if (DuplicatesSearched) ShowDuplicates();
        RefreshFromTree();
    }
}
