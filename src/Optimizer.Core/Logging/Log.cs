using System.Text.Json;

namespace Optimizer.Core.Logging;

public enum LogLevel { Debug, Info, Warning, Error }

/// <summary>
/// Structured JSON-lines log in the data folder's logs (%ProgramData%\PCOptimizer\logs when elevated). Logging never
/// throws: a failing log must not break a scan.
/// </summary>
public static class Log
{
    private static readonly Lock Gate = new();
    private static string? _file;

    public static string Directory { get; } =
        Platform.DataPaths.Logs;

    public static string? CurrentFile => _file;

    /// <summary>
    /// Whether entries also go to the log file. An elevated process keeps them in memory until the data folder has been
    /// secured (<see cref="Backup.SecureFolder.PrepareRoot"/>), so no write can follow a planted link.
    /// </summary>
    public static bool FileEnabled { get; private set; } = !Platform.DataPaths.ProcessIsElevated;

    private static bool _fileDisabled;

    /// <summary>Starts writing the log file, including the entries logged so far.</summary>
    public static void EnableFile()
    {
        lock (Gate)
        {
            if (FileEnabled || _fileDisabled) return;
            FileEnabled = true;
            foreach (var entry in Session) Append(entry);
        }
    }

    /// <summary>Keeps every entry in memory only, for the rest of the process (tests must not write into the real data folder).</summary>
    public static void DisableFile()
    {
        lock (Gate)
        {
            _fileDisabled = true;
            FileEnabled = false;
        }
    }

    /// <summary>Entries kept in memory; older ones are dropped (the file keeps them).</summary>
    public const int MaxEntries = 10_000;

    private static readonly Queue<LogEntry> Session = new();

    /// <summary>The last <paramref name="max"/> entries of this session, safe to read while other threads log.</summary>
    public static IReadOnlyList<LogEntry> Snapshot(int max)
    {
        lock (Gate) return Session.TakeLast(max).ToList();
    }

    /// <summary>The last <paramref name="max"/> entries that match <paramref name="filter"/>, newest first.</summary>
    public static IReadOnlyList<LogEntry> Newest(Func<LogEntry, bool> filter, int max)
    {
        lock (Gate) return Session.Where(filter).Reverse().Take(max).ToList();
    }

    public static void Debug(string source, string message, object? data = null) => Write(LogLevel.Debug, source, message, data);
    public static void Info(string source, string message, object? data = null) => Write(LogLevel.Info, source, message, data);
    public static void Warn(string source, string message, object? data = null) => Write(LogLevel.Warning, source, message, data);
    public static void Error(string source, string message, Exception? ex = null) =>
        Write(LogLevel.Error, source, message, ex is null ? null : new { type = ex.GetType().FullName, ex.Message, ex.StackTrace });

    private static void Write(LogLevel level, string source, string message, object? data)
    {
        string? json;
        try
        {
            json = data is null ? null : JsonSerializer.Serialize(data);
        }
        catch (Exception ex)
        {
            json = JsonSerializer.Serialize(new { unserializable = data!.GetType().FullName, ex.Message });
        }
        var entry = new LogEntry(DateTimeOffset.Now, level, source, message, json);
        lock (Gate)
        {
            Session.Enqueue(entry);
            while (Session.Count > MaxEntries) Session.Dequeue();
            if (FileEnabled) Append(entry);
        }
    }

    /// <summary>Session logs kept: every start writes one, so older ones are removed (the newest are kept for bug reports).</summary>
    public const int KeptFiles = 30;

    private static void Prune()
    {
        try
        {
            foreach (var old in new DirectoryInfo(Directory).GetFiles("session-*.jsonl").OrderByDescending(f => f.Name, StringComparer.Ordinal).Skip(KeptFiles))
                old.Delete();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An older log in use or not removable: try again at the next start.
        }
    }

    private static void Append(LogEntry entry)
    {
        try
        {
            if (_file is null)
            {
                System.IO.Directory.CreateDirectory(Directory);
                _file = Path.Combine(Directory, $"session-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.jsonl");
                Prune();
            }
            File.AppendAllText(_file, JsonSerializer.Serialize(entry) + Environment.NewLine);
        }
        catch
        {
            // Disk issue: keep the in-memory log only.
        }
    }
}

public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Source, string Message, string? Data);
