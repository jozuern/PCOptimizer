using System.Text.RegularExpressions;
using Optimizer.Core.Actions;
using Optimizer.Core.Docs;
using Optimizer.Core.Tweaks;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>
/// The catalog refers to other things by name: tweaks, findings, facts and labels. A typo there does not fail at load
/// time, it silently turns a rule into a no-op (a guard that never blocks, a reason that shows its key). These tests
/// check every reference.
/// </summary>
public partial class CatalogIntegrityTests(ITestOutputHelper output)
{
    private static readonly IReadOnlyList<TweakDefinition> Tweaks = TweakCatalog.Current.Tweaks;

    private static IEnumerable<Condition> Conditions(TweakDefinition t) => t.Conditions();

    /// <summary>Fact names FactsBuilder writes, read from its source, plus the per-finding facts.</summary>
    private static readonly HashSet<string> KnownFacts = FactNamePattern()
        .Matches(File.ReadAllText(Path.Combine(RepoPaths.Root, "src", "Optimizer.Core", "Tweaks", "FactsBuilder.cs")))
        .Select(m => m.Groups[1].Value).ToHashSet(StringComparer.Ordinal);

    private static readonly HashSet<string> FindingIds = DocLint.RequiredDocIds().ToHashSet(StringComparer.Ordinal);

    private static bool IsKnownFact(string fact)
    {
        if (KnownFacts.Contains(fact)) return true;
        var m = FindingFactPattern().Match(fact);
        return m.Success && FindingIds.Contains(m.Groups[1].Value);
    }

    private void AssertNone(List<string> problems)
    {
        foreach (var p in problems) output.WriteLine(p);
        Assert.Empty(problems);
    }

    [Fact]
    public void ReferencedTweaksAndFindingsExist()
    {
        // Catalog tweaks and the fixes built at run time (fix.powerMode and the like).
        var ids = Tweaks.Select(t => t.Id).Concat(Findings.Checks.RuntimeFixes.DocIds).ToHashSet(StringComparer.Ordinal);
        var problems = new List<string>();
        foreach (var t in Tweaks)
        {
            problems.AddRange(t.Requires.Where(r => !ids.Contains(r)).Select(r => $"{t.Id}: requires unknown tweak {r}"));
            problems.AddRange(t.ConflictsWith.Where(c => !ids.Contains(c)).Select(c => $"{t.Id}: conflicts with unknown tweak {c}"));
            problems.AddRange(t.Fixes.Where(f => !FindingIds.Contains(f)).Select(f => $"{t.Id}: fixes unknown finding {f}"));
        }
        AssertNone(problems);
    }

    [Fact]
    public void ConditionsUseFactsTheAppComputes()
    {
        Assert.True(KnownFacts.Count > 20, "FactsBuilder.cs was not parsed");
        var problems = Tweaks.SelectMany(t => Conditions(t).SelectMany(c => c.ReferencedFacts()).Where(f => !IsKnownFact(f)).Select(f => $"{t.Id}: unknown fact {f}")).ToList();
        foreach (var p in CatalogDataProfilesFacts()) problems.Add(p);
        AssertNone(problems);
    }

    private static IEnumerable<string> CatalogDataProfilesFacts() =>
        Catalog.CatalogData.Current.Profiles.Profiles.SelectMany(p =>
            (p.SuggestWhen?.ReferencedFacts() ?? []).Concat(p.Tweaks.SelectMany(pt => pt.RecommendWhen?.ReferencedFacts() ?? []))
                .Where(f => !IsKnownFact(f)).Select(f => $"profile {p.Id}: unknown fact {f}"));

    [Fact]
    public void LabelKeysExistInBothLanguages()
    {
        var keys = Tweaks.SelectMany(t =>
                new[] { t.RecommendReasonKey }
                    .Concat(t.ImpactOverrides.Select(o => o.ReasonKey))
                    .Concat(Conditions(t).Select(c => c.ReasonKey))
                    .Append($"category.{t.Category}")
                    .Concat(t.Impact.Effect.Select(e => $"effect.{e}")))
            .OfType<string>().Distinct();
        var problems = keys.SelectMany(k => DocStore.Languages.Where(l => !Labels.Current.Has(l, k)).Select(l => $"[{l}] missing label {k}")).ToList();
        AssertNone(problems);
    }

    [Fact]
    public void FreeTextFieldsUseKnownValues()
    {
        string[] registryKinds = ["dword", "qword", "string", "expandString", "multiString", "binary"];
        var problems = new List<string>();
        foreach (var t in Tweaks)
        {
            if (t.AppliesTo.FormFactor is not ("any" or "desktop" or "laptop")) problems.Add($"{t.Id}: formFactor {t.AppliesTo.FormFactor}");
            if (t.Verify is not ("actions" or "afterRestart")) problems.Add($"{t.Id}: verify {t.Verify}");
            foreach (var r in t.Actions.OfType<RegistryAction>().Where(r => !registryKinds.Contains(r.Kind))) problems.Add($"{t.Id}: registry kind {r.Kind}");
            foreach (var v in t.AppliesTo.GpuVendor ?? []) if (v is not ("nvidia" or "amd" or "intel")) problems.Add($"{t.Id}: gpuVendor {v}");
            foreach (var v in t.AppliesTo.CpuVendor ?? []) if (v is not ("intel" or "amd")) problems.Add($"{t.Id}: cpuVendor {v}");
        }
        AssertNone(problems);
    }

    [Fact]
    public void AGuardThatCannotBeCheckedBlocks()
    {
        var guard = new Condition { Fact = "power.modernStandby", Eq = System.Text.Json.JsonSerializer.SerializeToElement(true), ReasonKey = "block.modernStandby" };
        var t = new TweakDefinition
        {
            Id = "test.guarded", Category = "Test", Hidden = true, Impact = new ImpactInfo { Gaming = 0, Basis = "situational", Effect = ["none"] },
            Actions = [], BlockedWhen = [guard],
        };
        using var fx = new EngineFixture();
        var known = new Facts().Set("os.build", 26300).Set("elevated", true).Set("power.modernStandby", false);
        Assert.Empty(fx.Engine.Preflight(t, known, new HashSet<string>(), new ApplyOptions { ExpertMode = true }));
        var unknown = new Facts().Set("os.build", 26300).Set("elevated", true); // the power probe failed
        Assert.Contains(fx.Engine.Preflight(t, unknown, new HashSet<string>(), new ApplyOptions { ExpertMode = true }), b => b.ReasonKey == "block.cannotCheck");
    }

    [GeneratedRegex(@"\.Set\(""([\w.]+)""")]
    private static partial Regex FactNamePattern();

    [GeneratedRegex(@"^finding\.([\w.]+?)\.(problem|status)$")]
    private static partial Regex FindingFactPattern();
}
