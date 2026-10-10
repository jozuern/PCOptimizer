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

    /// <summary>
    /// The data files are read leniently (maintainer "_comment" keys); a misspelled property would be ignored and turn a
    /// rule into a no-op ("recomendWhen" in profiles.json). Without the comments, every property must map.
    /// </summary>
    [Fact]
    public void DataFilesHaveNoUnknownProperties()
    {
        var types = new Dictionary<string, Type>
        {
            ["appx.json"] = typeof(Debloat.AppxCatalog), ["services.json"] = typeof(Services.ServiceCatalog), ["apps.json"] = typeof(Apps.AppCatalog),
            ["features.json"] = typeof(Tools.FeatureCatalog), ["profiles.json"] = typeof(Profiles.ProfileCatalog), ["gpu.json"] = typeof(Catalog.GpuCatalog),
            ["cpu.json"] = typeof(Catalog.CpuCatalog), ["ram.json"] = typeof(Catalog.RamCatalog), ["anticheat.json"] = typeof(Catalog.AntiCheatCatalog),
            ["bios.json"] = typeof(Catalog.BiosCatalog), ["storage.json"] = typeof(Catalog.StorageCatalog), ["extras.json"] = typeof(Catalog.ExtrasCatalog),
        };
        var files = Catalog.CatalogData.ResourceNames("Catalog.Data.").Select(n => n["Catalog.Data.".Length..]).Where(n => n != "labels.json").ToList();
        Assert.Equal(files.Order(), types.Keys.Order());
        var strict = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true, ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true,
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
        static void StripComments(System.Text.Json.Nodes.JsonNode? node)
        {
            if (node is System.Text.Json.Nodes.JsonObject o)
            {
                o.Remove("_comment");
                foreach (var (_, child) in o) StripComments(child);
            }
            else if (node is System.Text.Json.Nodes.JsonArray a)
            {
                foreach (var child in a) StripComments(child);
            }
        }
        var errors = new List<string>();
        foreach (var (file, type) in types)
        {
            var node = System.Text.Json.Nodes.JsonNode.Parse(Catalog.CatalogData.ReadResourceText("Catalog.Data." + file),
                documentOptions: new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true });
            StripComments(node);
            try
            {
                System.Text.Json.JsonSerializer.Deserialize(node, type, strict);
            }
            catch (System.Text.Json.JsonException ex)
            {
                errors.Add($"{file}: {ex.Message}");
            }
        }
        foreach (var e in errors) output.WriteLine(e);
        Assert.Empty(errors);
    }

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
    public void CatalogFilesHaveNoDuplicateKeys()
    {
        // System.Text.Json keeps the last of two equal keys without a word; a merge once left two "subject" keys per DNS preset.
        var problems = new List<string>();
        var catalog = Path.Combine(RepoPaths.Root, "src", "Optimizer.Core", "Catalog");
        var options = new System.Text.Json.JsonDocumentOptions { CommentHandling = System.Text.Json.JsonCommentHandling.Skip, AllowTrailingCommas = true };
        foreach (var file in Directory.EnumerateFiles(catalog, "*.json", SearchOption.AllDirectories))
        {
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(file), options);
            void Walk(System.Text.Json.JsonElement e, string at)
            {
                if (e.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    var here = e.TryGetProperty("id", out var id) && id.ValueKind == System.Text.Json.JsonValueKind.String ? id.GetString()! : at;
                    var seen = new HashSet<string>(StringComparer.Ordinal);
                    foreach (var p in e.EnumerateObject())
                    {
                        if (!seen.Add(p.Name)) problems.Add($"{Path.GetFileName(file)} {here}: \"{p.Name}\" twice");
                        Walk(p.Value, here);
                    }
                }
                else if (e.ValueKind == System.Text.Json.JsonValueKind.Array)
                    foreach (var item in e.EnumerateArray()) Walk(item, at);
            }
            Walk(doc.RootElement, "(root)");
        }
        AssertNone(problems);
    }

    [Fact]
    public void TweaksSharingAPageHaveDistinctSubjects()
    {
        // The title comes from the page, so without a subject the rows read the same (the DNS presets in the VM test).
        AssertNone(Tweaks.GroupBy(t => t.DocId).Where(g => g.Count() > 1)
            .SelectMany(g => g.GroupBy(t => t.Subject ?? "").Where(s => s.Count() > 1)
                .Select(s => $"{g.Key}: {string.Join(", ", s.Select(t => t.Id))} share the title, set \"subject\""))
            .ToList());
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
