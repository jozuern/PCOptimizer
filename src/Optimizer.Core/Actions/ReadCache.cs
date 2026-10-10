using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Optimizer.Core.Actions;

/// <summary>
/// Short-lived results of slow reads (DISM, PowerShell): a scan detects every tweak, then checks drift, and an apply
/// reads the same target several times (preview, state, backup, verify). Each read would start the tool again and
/// take seconds. Results are kept for a few seconds for a process runner that enabled the cache (the app's runner;
/// test fakes read every time) and dropped when the action writes.
/// </summary>
public static class ReadCache
{
    private sealed class Table
    {
        public required TimeSpan Lifetime { get; init; }
        public ConcurrentDictionary<string, (DateTime At, StoredValue? Value)> Values { get; } = new();
    }

    private static readonly ConditionalWeakTable<IProcessRunner, Table> Tables = new();

    /// <summary>Caches slow reads made through this runner for <paramref name="lifetime"/>.</summary>
    public static void Enable(IProcessRunner runner, TimeSpan lifetime) => Tables.AddOrUpdate(runner, new Table { Lifetime = lifetime });

    public static StoredValue? Get(ActionContext c, string key, Func<StoredValue?> read)
    {
        if (!Tables.TryGetValue(c.Processes, out var table)) return read();
        if (table.Values.TryGetValue(key, out var hit) && DateTime.UtcNow - hit.At < table.Lifetime) return hit.Value;
        var value = read();
        table.Values[key] = (DateTime.UtcNow, value);
        return value;
    }

    public static void Invalidate(ActionContext c, string key)
    {
        if (Tables.TryGetValue(c.Processes, out var table)) table.Values.TryRemove(key, out _);
    }
}
