namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// The file table of a volume cannot be read whole: it cannot be opened (not elevated, not NTFS), a read failed, its
/// extents cannot be followed, or too many records are damaged. The scan then reads folder by folder instead; the
/// message is the reason for the log and never holds a file name.
/// </summary>
public sealed class MftUnavailableException(string reason) : Exception(reason);
