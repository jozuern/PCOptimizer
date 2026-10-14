using CommunityToolkit.Mvvm.ComponentModel;
using Optimizer.App.Services;

namespace Optimizer.App.ViewModels;

/// <summary>A fixed drive in the picker: its root and how full it is (read from DriveInfo only).</summary>
public sealed record DriveRow(string Root, long Total, long Free)
{
    /// <summary>Used share of the drive, 0 to 1, for the bar under its name.</summary>
    public double UsedShare => Total > 0 ? Math.Clamp((double)(Total - Free) / Total, 0, 1) : 0;

    /// <summary>"120 GB free of 931 GB", in the language the row was built in.</summary>
    public string SpaceText { get; } = Loc.Instance.Format("Storage_DriveSpace", CleanupViewModel.Size(Free), CleanupViewModel.Size(Total));

    // What screen readers announce for this item in a combo box.
    public override string ToString() => $"{Root}, {SpaceText}";
}

/// <summary>A file in the Largest files or Duplicates list; <see cref="Entry"/> is its index in the scanned tree.</summary>
public sealed partial class StorageFileRow(string path, long bytes, string meta, bool canRecycle, int entry, Action? selectionChanged = null) : ObservableObject
{
    partial void OnSelectedChanged(bool value) => selectionChanged?.Invoke();

    // What screen readers announce for this item in a list.
    public override string ToString() => Path;

    public string Path { get; } = path;
    public long Bytes { get; } = bytes;
    public string SizeText { get; } = CleanupViewModel.Size(bytes);
    public string Meta { get; } = meta;
    public bool CanRecycle { get; } = canRecycle;
    public int Entry { get; } = entry;

    [ObservableProperty] private bool _selected;
}
