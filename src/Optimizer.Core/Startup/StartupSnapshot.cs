using System.Text.Json;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Startup;

/// <summary>One saved autostart entry: enough to recognize it again (location key and command).</summary>
public sealed record SnapshotEntry(StartupKind Kind, string Name, string? Command, string Location, string Key);

/// <summary>The saved list and when it was taken.</summary>
public sealed record StartupSnapshotData(DateTimeOffset TakenAt, IReadOnlyList<SnapshotEntry> Entries);

/// <summary>Entries added or changed since the snapshot, and entries that are gone.</summary>
public sealed record SnapshotDiff(DateTimeOffset TakenAt, IReadOnlySet<string> NewKeys, IReadOnlyList<SnapshotEntry> Removed);

/// <summary>
/// Saves the autostart list and compares a later scan with it ("what did that installer add?"). An entry counts as
/// new when its location key is new or its command changed. The file lives in the protected data folder.
/// </summary>
public static class StartupSnapshot
{
    public static string DefaultFile => Path.Combine(DataPaths.Root, "startup-snapshot.json");

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static StartupSnapshotData Take(IEnumerable<StartupEntry> entries, DateTimeOffset now) =>
        new(now, entries.Select(e => new SnapshotEntry(e.Kind, e.Name, e.Command, e.Location, e.Key)).ToList());

    public static void Save(string file, StartupSnapshotData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var temp = file + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(data, Json));
        File.Move(temp, file, overwrite: true);
    }

    public static StartupSnapshotData? Load(string file)
    {
        if (!File.Exists(file)) return null;
        try
        {
            return JsonSerializer.Deserialize<StartupSnapshotData>(File.ReadAllText(file), Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string Identity(string key, string? command) => key + "\n" + (command ?? "");

    public static SnapshotDiff Compare(StartupSnapshotData snapshot, IEnumerable<StartupEntry> current)
    {
        var before = snapshot.Entries.Select(e => Identity(e.Key, e.Command)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var list = current.ToList();
        var now = list.Select(e => Identity(e.Key, e.Command)).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var added = list.Where(e => !before.Contains(Identity(e.Key, e.Command))).Select(e => e.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var removed = snapshot.Entries.Where(e => !now.Contains(Identity(e.Key, e.Command))).ToList();
        return new SnapshotDiff(snapshot.TakenAt, added, removed);
    }
}
