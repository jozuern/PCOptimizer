using System.Globalization;
using System.Text.Json;
using Microsoft.Win32;

namespace Optimizer.Core.Actions;

/// <summary>Sets or deletes one registry value. Hive "user" writes to the session user's hive, never the elevated account's.</summary>
public sealed class RegistryAction : TweakAction
{
    public Hive Hive { get; init; } = Hive.Machine;
    public string Path { get; init; } = "";

    /// <summary>Value name; empty = the key's default value.</summary>
    public string Name { get; init; } = "";

    /// <summary>dword, qword, string, expandString, multiString, binary.</summary>
    public string Kind { get; init; } = "dword";

    /// <summary>Number, string, hex string (binary) or string array (multiString).</summary>
    public JsonElement Value { get; init; }

    /// <summary>true = the tweak removes the value.</summary>
    public bool Delete { get; init; }

    /// <summary>Optional live refresh after writing: "mouse", "settings".</summary>
    public string? Notify { get; init; }

    /// <summary>What Windows assumes when the value is absent (e.g. Game Mode is on by default). Used for state only.</summary>
    public JsonElement? MissingMeans { get; init; }

    /// <summary>Key (relative to the hive) to delete on undo when the value did not exist before, for tweaks where the key's existence matters.</summary>
    public string? RemoveKeyOnUndo { get; init; }

    public override ActionState State(ActionContext c)
    {
        var current = Read(c);
        if (current is null) return ActionState.Unsupported;
        if (!current.Existed && MissingMeans is { } m && !Delete && Serialize(Kind, m) == Serialize(Kind, Value)) return ActionState.Applied;
        return current.SameAs(Desired(c)) ? ActionState.Applied : ActionState.NotApplied;
    }

    public override string TargetKey => $"reg:{Hive}:{Path}\\{Name}".ToLowerInvariant();

    public override string Describe(ActionContext c) => $"{c.Registry.DisplayRoot(Hive)}\\{Path}\\{(Name.Length == 0 ? "(Default)" : Name)}";

    public override StoredValue Desired(ActionContext c) => Delete ? StoredValue.Missing : new StoredValue(true, Kind, Serialize(Kind, Value));

    public override StoredValue? Read(ActionContext c) => RegistryValue.Read(c.Registry, Hive, Path, Name);

    public override void Apply(ActionContext c)
    {
        if (Delete) RegistryValue.Delete(c.Registry, Hive, Path, Name);
        else RegistryValue.Write(c.Registry, Hive, Path, Name, Kind, Serialize(Kind, Value));
        if (Notify is not null) c.Notify?.Invoke(Notify);
    }

    public override void Restore(ActionContext c, StoredValue original)
    {
        if (!original.Existed)
        {
            RegistryValue.Delete(c.Registry, Hive, Path, Name);
            if (RemoveKeyOnUndo is { Length: > 0 } key) RegistryValue.DeleteKey(c.Registry, Hive, key);
        }
        else
        {
            RegistryValue.Write(c.Registry, Hive, Path, Name, original.Kind ?? Kind, original.Data ?? "");
        }
        if (Notify is not null) c.Notify?.Invoke(Notify);
    }

    internal static string Serialize(string kind, JsonElement value) => kind switch
    {
        "multiString" when value.ValueKind == JsonValueKind.Array => string.Join("\n", value.EnumerateArray().Select(v => v.GetString())),
        "dword" when value.ValueKind == JsonValueKind.Number => value.GetUInt32().ToString(CultureInfo.InvariantCulture),
        "qword" when value.ValueKind == JsonValueKind.Number => value.GetUInt64().ToString(CultureInfo.InvariantCulture),
        "dword" or "qword" when value.ValueKind == JsonValueKind.String => ParseNumber(value.GetString()!).ToString(CultureInfo.InvariantCulture),
        _ => value.ValueKind == JsonValueKind.String ? value.GetString()! : value.GetRawText(),
    };

    private static ulong ParseNumber(string s) =>
        s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToUInt64(s[2..], 16) : ulong.Parse(s, CultureInfo.InvariantCulture);
}

/// <summary>Sets and/or clears bits in a DWORD (e.g. Tcpip6 DisabledComponents shared by several tweaks).</summary>
public sealed class RegistryBitsAction : TweakAction
{
    public Hive Hive { get; init; } = Hive.Machine;
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public uint Set { get; init; }
    public uint Clear { get; init; }

    public override string TargetKey => $"regbits:{Hive}:{Path}\\{Name}:{Set:X}:{Clear:X}".ToLowerInvariant();

    public override string Describe(ActionContext c) => $"{c.Registry.DisplayRoot(Hive)}\\{Path}\\{Name} (bits)";

    private uint Current(ActionContext c) =>
        RegistryValue.Read(c.Registry, Hive, Path, Name) is { Existed: true, Data: { } d } && uint.TryParse(d, out var v) ? v : 0;

    public override StoredValue Desired(ActionContext c) =>
        new(true, "dword", ((Current(c) | Set) & ~Clear).ToString(CultureInfo.InvariantCulture));

    public override StoredValue? Read(ActionContext c) => RegistryValue.Read(c.Registry, Hive, Path, Name);

    public override ActionState State(ActionContext c)
    {
        var v = Current(c);
        return (v & Set) == Set && (v & Clear) == 0 ? ActionState.Applied : ActionState.NotApplied;
    }

    public override void Apply(ActionContext c) => RegistryValue.Write(c.Registry, Hive, Path, Name, "dword", Desired(c).Data!);

    /// <summary>Other tweaks may own other bits of the same value; only our bits count.</summary>
    public override bool IsStillApplied(ActionContext c, StoredValue applied) => State(c) == ActionState.Applied;

    /// <summary>Only our bits go back: other tweaks may own other bits of the same value.</summary>
    public override void Restore(ActionContext c, StoredValue original)
    {
        var originalBits = original is { Existed: true, Data: { } d } && uint.TryParse(d, out var o) ? o : 0u;
        var mask = Set | Clear;
        var restored = (Current(c) & ~mask) | (originalBits & mask);
        if (!original.Existed && restored == 0) RegistryValue.Delete(c.Registry, Hive, Path, Name);
        else RegistryValue.Write(c.Registry, Hive, Path, Name, "dword", restored.ToString(CultureInfo.InvariantCulture));
    }
}

/// <summary>Sets one "Key=Value;" token in a semicolon list (e.g. DirectXUserGlobalSettings), keeping the other tokens.</summary>
public sealed class RegistryTokenAction : TweakAction
{
    public Hive Hive { get; init; } = Hive.User;
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";
    public string Token { get; init; } = "";
    public string Value { get; init; } = "";

    public override string TargetKey => $"regtoken:{Hive}:{Path}\\{Name}:{Token}".ToLowerInvariant();

    public override string Describe(ActionContext c) => $"{c.Registry.DisplayRoot(Hive)}\\{Path}\\{Name}, token {Token}";

    public static Dictionary<string, string> Parse(string? s) =>
        (s ?? "").Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0], StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Last()[1], StringComparer.OrdinalIgnoreCase);

    public static string Format(Dictionary<string, string> tokens) => string.Concat(tokens.Select(t => $"{t.Key}={t.Value};"));

    private string? CurrentString(ActionContext c) => RegistryValue.Read(c.Registry, Hive, Path, Name) is { Existed: true } v ? v.Data : null;

    public override StoredValue? Read(ActionContext c)
    {
        var tokens = Parse(CurrentString(c));
        return tokens.TryGetValue(Token, out var v) ? new StoredValue(true, "token", v) : StoredValue.Missing;
    }

    public override StoredValue Desired(ActionContext c) => new(true, "token", Value);

    public override void Apply(ActionContext c)
    {
        var tokens = Parse(CurrentString(c));
        tokens[Token] = Value;
        RegistryValue.Write(c.Registry, Hive, Path, Name, "string", Format(tokens));
    }

    public override void Restore(ActionContext c, StoredValue original)
    {
        var tokens = Parse(CurrentString(c));
        if (original.Existed) tokens[Token] = original.Data ?? "";
        else tokens.Remove(Token);
        if (tokens.Count == 0) RegistryValue.Delete(c.Registry, Hive, Path, Name);
        else RegistryValue.Write(c.Registry, Hive, Path, Name, "string", Format(tokens));
    }
}

/// <summary>Typed registry read/write shared by the registry actions.</summary>
public static class RegistryValue
{
    public static StoredValue Read(IRegistryRoots roots, Hive hive, string path, string name)
    {
        using var key = roots.Open(hive, path, writable: false);
        if (key is null) return StoredValue.Missing;
        var value = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (value is null) return StoredValue.Missing;
        var kind = key.GetValueKind(name);
        return kind switch
        {
            RegistryValueKind.DWord => new StoredValue(true, "dword", unchecked((uint)(int)value).ToString(CultureInfo.InvariantCulture)),
            RegistryValueKind.QWord => new StoredValue(true, "qword", unchecked((ulong)(long)value).ToString(CultureInfo.InvariantCulture)),
            RegistryValueKind.ExpandString => new StoredValue(true, "expandString", (string)value),
            RegistryValueKind.MultiString => new StoredValue(true, "multiString", string.Join("\n", (string[])value)),
            RegistryValueKind.Binary => new StoredValue(true, "binary", Convert.ToHexString((byte[])value)),
            _ => new StoredValue(true, "string", value.ToString()),
        };
    }

    public static void Write(IRegistryRoots roots, Hive hive, string path, string name, string kind, string data)
    {
        using var key = roots.Open(hive, path, writable: true, create: true)
                        ?? throw new InvalidOperationException($"Cannot open {roots.DisplayRoot(hive)}\\{path} for writing");
        switch (kind)
        {
            case "dword":
                key.SetValue(name, unchecked((int)uint.Parse(data, CultureInfo.InvariantCulture)), RegistryValueKind.DWord);
                break;
            case "qword":
                key.SetValue(name, unchecked((long)ulong.Parse(data, CultureInfo.InvariantCulture)), RegistryValueKind.QWord);
                break;
            case "expandString":
                key.SetValue(name, data, RegistryValueKind.ExpandString);
                break;
            case "multiString":
                key.SetValue(name, data.Split('\n'), RegistryValueKind.MultiString);
                break;
            case "binary":
                key.SetValue(name, Convert.FromHexString(data.Replace(" ", "")), RegistryValueKind.Binary);
                break;
            default:
                key.SetValue(name, data, RegistryValueKind.String);
                break;
        }
    }

    public static void DeleteKey(IRegistryRoots roots, Hive hive, string path)
    {
        var i = path.LastIndexOf('\\');
        if (i <= 0) return;
        using var parent = roots.Open(hive, path[..i], writable: true);
        parent?.DeleteSubKeyTree(path[(i + 1)..], throwOnMissingSubKey: false);
    }

    public static void Delete(IRegistryRoots roots, Hive hive, string path, string name)
    {
        using var key = roots.Open(hive, path, writable: true);
        if (key?.GetValue(name) is not null) key.DeleteValue(name, throwOnMissingValue: false);
    }
}
