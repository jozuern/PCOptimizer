using System.Text.Json;

namespace Optimizer.Core.Logging;

public enum LogLevel { Debug, Info, Warning, Error }

/// <summary>
/// Structured JSON-lines log in the data folder's logs (%ProgramData%\PCOptimizer\logs when elevated). Creating that folder is the app's only write in M1.
/// Logging never throws: a failing log must not break a scan.
/// </summary>
public static class Log
{
    private static readonly Lock Gate = new();
    private static string? _file;

    public static string Directory { get; } =
        Path.Combine(Platform.DataPaths.Root, "logs");

    public static string? CurrentFile => _file;

    /// <summary>In-memory copy of this session's entries (shown in the UI and used by tests).</summary>
    public static List<LogEntry> Session { get; } = [];

    /// <summary>The last <paramref name="max"/> entries of this session, safe to read while other threads log.</summary>
    public static IReadOnlyList<LogEntry> Snapshot(int max)
    {
        lock (Gate) return Session.TakeLast(max).ToList();
    }

    public static void Debug(string source, string message, object? data = null) => Write(LogLevel.Debug, source, message, data);
    public static void Info(string source, string message, object? data = null) => Write(LogLevel.Info, source, message, data);
    public static void Warn(string source, string message, object? data = null) => Write(LogLevel.Warning, source, message, data);
    public static void Error(string source, string message, Exception? ex = null) =>
        Write(LogLevel.Error, source, message, ex is null ? null : new { type = ex.GetType().FullName, ex.Message, ex.StackTrace });

    private static void Write(LogLevel level, string source, string message, object? data)
    {
        var entry = new LogEntry(DateTimeOffset.Now, level, source, message, data is null ? null : JsonSerializer.Serialize(data));
        lock (Gate)
        {
            Session.Add(entry);
            try
            {
                if (_file is null)
                {
                    System.IO.Directory.CreateDirectory(Directory);
                    _file = Path.Combine(Directory, $"session-{DateTime.Now:yyyyMMdd-HHmmss}-{Environment.ProcessId}.jsonl");
                }
                File.AppendAllText(_file, JsonSerializer.Serialize(entry) + Environment.NewLine);
            }
            catch
            {
                // Not elevated or disk issue: keep the in-memory log only.
            }
        }
    }
}

public sealed record LogEntry(DateTimeOffset Time, LogLevel Level, string Source, string Message, string? Data);
