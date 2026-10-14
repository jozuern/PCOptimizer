namespace Optimizer.Core.Storage;

/// <summary>Progress of a scan; <paramref name="Percent"/> is null when the used space of the volume is unknown.</summary>
public readonly record struct ScanProgress(double? Percent, int Files, long Bytes, ScanReader Reader);

/// <summary>Reads a drive or folder into a <see cref="DiskTree"/>. Read only; no partial tree on cancellation.</summary>
public interface IVolumeScanner
{
    Task<DiskTree> ScanAsync(string root, IProgress<ScanProgress>? progress, CancellationToken ct);
}
