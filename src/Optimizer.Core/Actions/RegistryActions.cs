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

    /// <summary>
    /// For tweaks where the key's existence matters: on undo, when the value did not exist before, the keys from
    /// <see cref="Path"/> up to this key (relative to the hive) are removed, each only if it is empty by then. Keys that
    /// held other values or subkeys before the change stay.
    /// </summary>
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
            if (RemoveKeyOnUndo is { Length: > 0 } key) RegistryValue.DeleteEmptyKeys(c.Registry, Hive, Path, key);
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

/// <summary>
/// Clears and/or sets bits in a REG_BINARY value byte by byte (e.g. UserPreferencesMask, where visual effects share
/// the value with accessibility options). Only the bits in the masks are written or compared; the rest of the value
/// stays as Windows or the user set it.
/// </summary>
public sealed class RegistryBinaryBitsAction : TweakAction
{
    public Hive Hive { get; init; } = Hive.User;
    public string Path { get; init; } = "";
    public string Name { get; init; } = "";

    /// <summary>Hex bytes, byte 0 first: bits to turn on.</summary>
    public string Set { get; init; } = "";

    /// <summary>Hex bytes, byte 0 first: bits to turn off.</summary>
    public string Clear { get; init; } = "";

    public override string TargetKey => $"regbinbits:{Hive}:{Path}\\{Name}:{Set}:{Clear}".ToLowerInvariant();

    public override string Describe(ActionContext c) => $"{c.Registry.DisplayRoot(Hive)}\\{Path}\\{Name} (bits)";

    private byte[]? Current(ActionContext c) =>
        RegistryValue.Read(c.Registry, Hive, Path, Name) is { Existed: true, Kind: "binary", Data: { } d } ? Convert.FromHexString(d) : null;

    private static byte[] Mask(string hex) => hex.Length == 0 ? [] : Convert.FromHexString(hex);

    private static byte At(byte[] bytes, int i) => i < bytes.Length ? bytes[i] : (byte)0;

    private byte[] Combine(byte[] current)
    {
        var set = Mask(Set);
        var clear = Mask(Clear);
        var result = new byte[Math.Max(current.Length, Math.Max(set.Length, clear.Length))];
        for (var i = 0; i < result.Length; i++) result[i] = (byte)((At(current, i) | At(set, i)) & ~At(clear, i));
        return result;
    }

    /// <summary>A value that does not exist or is not binary is not changed (Unsupported).</summary>
    public override StoredValue? Read(ActionContext c) => Current(c) is { } bytes ? new StoredValue(true, "binary", Convert.ToHexString(bytes)) : null;

    public override StoredValue Desired(ActionContext c) =>
        new(true, "binary", Convert.ToHexString(Combine(Current(c) ?? [])));

    public override ActionState State(ActionContext c)
    {
        if (Current(c) is not { } bytes) return ActionState.Unsupported;
        var set = Mask(Set);
        var clear = Mask(Clear);
        for (var i = 0; i < Math.Max(set.Length, clear.Length); i++)
            if ((At(bytes, i) & At(set, i)) != At(set, i) || (At(bytes, i) & At(clear, i)) != 0) return ActionState.NotApplied;
        return ActionState.Applied;
    }

    public override void Apply(ActionContext c)
    {
        if (Current(c) is not { } bytes) throw new InvalidOperationException($"{Describe(c)} does not exist");
        RegistryValue.Write(c.Registry, Hive, Path, Name, "binary", Convert.ToHexString(Combine(bytes)));
    }

    /// <summary>Only our bits count: Windows or the user may have changed other bits of the same value since.</summary>
    public override bool IsStillApplied(ActionContext c, StoredValue applied) => State(c) == ActionState.Applied;

    /// <summary>Puts back only the masked bits from the original; every other bit keeps its current state.</summary>
    public override void Restore(ActionContext c, StoredValue original)
    {
        if (Current(c) is not { } bytes || original is not { Existed: true, Data: { } data }) return;
        var before = Convert.FromHexString(data);
        var set = Mask(Set);
        var clear = Mask(Clear);
        var result = (byte[])bytes.Clone();
        for (var i = 0; i < result.Length; i++)
        {
            var mask = (byte)(At(set, i) | At(clear, i));
            result[i] = (byte)((result[i] & ~mask) | (At(before, i) & mask));
        }
        RegistryValue.Write(c.Registry, Hive, Path, Name, "binary", Convert.ToHexString(result));
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

    /// <summary>
    /// Deletes <paramref name="path"/> and its parents up to <paramref name="stopAt"/> (inclusive), each only while it
    /// has no values and no subkeys. <paramref name="stopAt"/> must be <paramref name="path"/> or one of its parents.
    /// </summary>
    public static void DeleteEmptyKeys(IRegistryRoots roots, Hive hive, string path, string stopAt)
    {
        var current = path.TrimEnd('\\');
        var stop = stopAt.TrimEnd('\\');
        if (!current.Equals(stop, StringComparison.OrdinalIgnoreCase) && !current.StartsWith(stop + "\\", StringComparison.OrdinalIgnoreCase)) return;
        while (current.Length >= stop.Length)
        {
            using (var key = roots.Open(hive, current, writable: false))
            {
                if (key is not null && (key.ValueCount > 0 || key.SubKeyCount > 0)) return;
            }
            var i = current.LastIndexOf('\\');
            if (i <= 0) return;
            using (var parent = roots.Open(hive, current[..i], writable: true))
                parent?.DeleteSubKey(current[(i + 1)..], throwOnMissingSubKey: false);
            current = current[..i];
        }
    }

    public static void Delete(IRegistryRoots roots, Hive hive, string path, string name)
    {
        using var key = roots.Open(hive, path, writable: true);
        if (key?.GetValue(name) is not null) key.DeleteValue(name, throwOnMissingValue: false);
    }
}
