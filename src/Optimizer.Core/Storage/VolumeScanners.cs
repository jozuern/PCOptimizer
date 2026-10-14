namespace Optimizer.Core.Storage;

/// <summary>Picks the reader for a scan. Today always the folder walk; the file table reader joins later.</summary>
public static class VolumeScanners
{
    public static Task<DiskTree> ScanAsync(string root, bool elevated, IProgress<ScanProgress>? progress, CancellationToken ct) =>
        new EnumerationScanner().ScanAsync(root, progress, ct);
}
