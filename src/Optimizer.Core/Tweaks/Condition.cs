using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Optimizer.Core.Tweaks;

/// <summary>
/// Structured condition: {"all":[...]}, {"any":[...]}, {"not":{...}} or {"fact":"x", "eq"|"ne"|"gt"|"gte"|"lt"|"lte"|"in"|"exists": ...}.
/// No expression parser, so conditions are testable and cannot execute anything.
/// </summary>
public sealed class Condition
{
    public List<Condition>? All { get; init; }
    public List<Condition>? Any { get; init; }
    public Condition? Not { get; init; }
    public string? Fact { get; init; }
    public JsonElement? Eq { get; init; }
    public JsonElement? Ne { get; init; }
    public double? Gt { get; init; }
    public double? Gte { get; init; }
    public double? Lt { get; init; }
    public double? Lte { get; init; }
    public List<JsonElement>? In { get; init; }
    public bool? Exists { get; init; }

    /// <summary>Optional label key explaining a block (shown in the UI when a blockedWhen condition matches).</summary>
    public string? ReasonKey { get; init; }

    public bool Evaluate(Facts facts)
    {
        if (All is not null) return All.All(c => c.Evaluate(facts));
        if (Any is not null) return Any.Any(c => c.Evaluate(facts));
        if (Not is not null) return !Not.Evaluate(facts);
        if (Fact is null) return true;

        var value = facts.Get(Fact);
        if (Exists is { } exists) return (value is not null) == exists;
        if (value is null) return false; // unknown facts never match (Unknown never produces advice)
        if (Eq is { } eq) return Same(value, eq);
        if (Ne is { } ne) return !Same(value, ne);
        if (In is not null) return In.Any(v => Same(value, v));
        if (ToNumber(value) is { } n)
        {
            if (Gt is { } gt) return n > gt;
            if (Gte is { } gte) return n >= gte;
            if (Lt is { } lt) return n < lt;
            if (Lte is { } lte) return n <= lte;
        }
        return false;
    }

    /// <summary>
    /// Every fact this condition compares is known. "exists" tests are about missing facts and always count as known.
    /// A safety guard that cannot be evaluated must not let the change through (see TweakEngine.Preflight).
    /// </summary>
    public bool CanEvaluate(Facts facts)
    {
        if (All is not null) return All.All(c => c.CanEvaluate(facts));
        if (Any is not null) return Any.All(c => c.CanEvaluate(facts));
        if (Not is not null) return Not.CanEvaluate(facts);
        return Fact is null || Exists is not null || facts.Get(Fact) is not null;
    }

    /// <summary>Facts referenced anywhere in this condition (used by the catalog lint).</summary>
    public IEnumerable<string> ReferencedFacts()
    {
        if (Fact is not null) yield return Fact;
        foreach (var c in (All ?? []).Concat(Any ?? []).Concat(Not is null ? [] : [Not]))
            foreach (var f in c.ReferencedFacts())
                yield return f;
    }

    private static bool Same(object value, JsonElement expected) => expected.ValueKind switch
    {
        JsonValueKind.True => value is true,
        JsonValueKind.False => value is false,
        JsonValueKind.Number => ToNumber(value) is { } n && Math.Abs(n - expected.GetDouble()) < 1e-9,
        JsonValueKind.String => string.Equals(Convert.ToString(value, CultureInfo.InvariantCulture), expected.GetString(), StringComparison.OrdinalIgnoreCase),
        _ => false,
    };

    private static double? ToNumber(object value) => value switch
    {
        int i => i,
        long l => l,
        uint u => u,
        double d => d,
        float f => f,
        string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p) => p,
        _ => null,
    };
}

/// <summary>Named facts about this PC for conditions. Values: bool, number or string; missing = unknown.</summary>
public sealed class Facts
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public IReadOnlyDictionary<string, object?> All => _values;

    public object? Get(string name) => _values.GetValueOrDefault(name);

    public Facts Set(string name, object? value)
    {
        _values[name] = value;
        return this;
    }
}
