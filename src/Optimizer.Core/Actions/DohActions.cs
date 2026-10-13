using System.Net;

namespace Optimizer.Core.Actions;

/// <summary>
/// Automatic DNS over HTTPS for DoH servers Windows knows (Get- and Set-DnsClientDohServerAddress): with AutoUpgrade
/// on, Windows uses the server's DoH template whenever an adapter uses that server's address. Stored value: the
/// AutoUpgrade state of each listed server ("1.1.1.1=1;8.8.8.8=0"), so undo sets every server back to what it was.
/// </summary>
public sealed class DohAutoUpgradeAction : TweakAction
{
    public List<string> Servers { get; init; } = [];
    public bool AutoUpgrade { get; init; } = true;

    public override string TargetKey => "doh:autoupgrade:" + string.Join(",", Servers.Order(StringComparer.Ordinal)).ToLowerInvariant();
    public override TargetText TargetLabel(ActionContext c) => TargetText.Of("target.doh");
    public override EarlyRead EarlyRead => EarlyRead.WithScan;

    private IEnumerable<string> Valid => Servers.Where(s => IPAddress.TryParse(s, out _)).Order(StringComparer.Ordinal);

    public static string Format(IEnumerable<KeyValuePair<string, bool>> states) =>
        string.Join(";", states.OrderBy(s => s.Key, StringComparer.Ordinal).Select(s => $"{s.Key}={(s.Value ? 1 : 0)}"));

    public static Dictionary<string, bool> ParseStored(string? data) =>
        (data ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('='))
            .Where(p => p.Length == 2 && IPAddress.TryParse(p[0], out _))
            .ToDictionary(p => p[0], p => p[1] == "1", StringComparer.OrdinalIgnoreCase);

    public override StoredValue Desired(ActionContext c) => new(true, "doh", Format(Valid.Select(s => KeyValuePair.Create(s, AutoUpgrade))));

    /// <summary>Null when Windows cannot list the DoH servers or one of ours is not known to it.</summary>
    public override StoredValue? Read(ActionContext c)
    {
        // One list for every DoH action of a scan (the read is shared through the cache, the servers are filtered here).
        var list = ReadCache.Get(c, "doh:list", () =>
            c.Network.DohServers() is { } servers ? new StoredValue(true, "dohlist", Format(servers)) : null);
        if (list?.Data is not { } data) return null;
        var known = ParseStored(data);
        var states = new List<KeyValuePair<string, bool>>();
        foreach (var s in Valid)
        {
            if (!known.TryGetValue(s, out var on)) return null;
            states.Add(KeyValuePair.Create(s, on));
        }
        return new StoredValue(true, "doh", Format(states));
    }

    public override void Apply(ActionContext c) => Write(c, Valid.Select(s => KeyValuePair.Create(s, AutoUpgrade)));

    public override void Restore(ActionContext c, StoredValue original) => Write(c, ParseStored(original.Data).Where(s => Valid.Contains(s.Key, StringComparer.OrdinalIgnoreCase)));

    /// <summary>
    /// One PowerShell run per server with errors as terminating errors: in one script, only the last command's result
    /// would set the exit code. All servers are tried; the failures are reported together.
    /// </summary>
    private static void Write(ActionContext c, IEnumerable<KeyValuePair<string, bool>> states) => ReadCache.Writing(c, "doh:list", () =>
    {
        var errors = new List<string>();
        // Addresses are validated as IP addresses above, so nothing else reaches the script.
        foreach (var s in states)
        {
            var script = $"$ErrorActionPreference='Stop'; Set-DnsClientDohServerAddress -ServerAddress '{s.Key}' -AutoUpgrade ${(s.Value ? "true" : "false")}";
            var (code, output) = c.Processes.Run("powershell.exe", $"-NoProfile -NonInteractive -Command \"{script}\"");
            if (code != 0) errors.Add($"{s.Key} ({code}): {output.Trim()}");
        }
        if (errors.Count > 0) throw new InvalidOperationException("Set-DnsClientDohServerAddress failed for " + string.Join("; ", errors));
    });
}
