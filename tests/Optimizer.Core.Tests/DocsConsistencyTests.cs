using System.Text;
using Optimizer.Core.Actions;
using Optimizer.Core.Catalog;
using Optimizer.Core.Cleanup;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

/// <summary>
/// Numbers and lists in the documents come from the catalog, so they cannot drift. The VM test plan's tweak tables are
/// generated: run the tests with PCO_UPDATE_DOCS=1 to rewrite them after a catalog change.
/// </summary>
public class DocsConsistencyTests
{
    private const string Begin = "<!-- Generated from the tweak catalog by DocsConsistencyTests. Do not edit by hand. -->";
    private const string End = "<!-- End of generated tables. -->";

    public static int VisibleTweaks => TweakCatalog.Current.Tweaks.Count(t => !t.Hidden);
    public static int Checks => FindingEngine.CreateChecks(null).SelectMany(c => c.DocIds).Distinct(StringComparer.Ordinal).Count();
    public static int InboxApps => CatalogData.Current.Appx.Apps.Count;
    public static int CleanupCategories => CleanupEngine.Categories(@"C:\Users\Example", "S-1-5-21-0-0-0-1001").Count;

    [Fact]
    public void ReadmeCountsMatchTheCatalog()
    {
        var readme = File.ReadAllText(Path.Combine(RepoPaths.Root, "README.md"));
        var missing = new[] { $"{VisibleTweaks} catalog tweaks", $"{Checks} read-only checks", $"{InboxApps} inbox apps", $"{CleanupCategories} cleanup categories" }
            .Where(phrase => !readme.Contains(phrase, StringComparison.Ordinal)).ToList();
        Assert.True(missing.Count == 0, "README.md must say: " + string.Join("; ", missing));
    }

    [Fact]
    public void VmTestPlanIsGeneratedFromTheCatalog()
    {
        var path = Path.Combine(RepoPaths.Root, "docs", "vm-test-plan.md");
        var text = File.ReadAllText(path).Replace("\r\n", "\n");
        var start = text.IndexOf(Begin, StringComparison.Ordinal);
        var end = text.IndexOf(End, StringComparison.Ordinal);
        Assert.True(start >= 0 && end > start, "vm-test-plan.md needs the generated-table markers");
        var expected = text[..start] + Begin + "\n\n" + GenerateTables() + End + text[(end + End.Length)..];
        if (Environment.GetEnvironmentVariable("PCO_UPDATE_DOCS") == "1" && expected != text) File.WriteAllText(path, expected);
        Assert.True(expected == text, "docs/vm-test-plan.md is out of date: run the tests once with PCO_UPDATE_DOCS=1");
        Assert.Contains($"Each of the {CleanupCategories} categories", text);
    }

    /// <summary>The tweak tables: Expert and boot-critical, restart or sign-out, everything else, and real hardware only.</summary>
    public static string GenerateTables()
    {
        var tweaks = TweakCatalog.Current.Tweaks.Where(t => !t.Hidden).ToList();
        static bool NeedsHardware(TweakDefinition t) => t.Actions.Any(a => a is NvidiaDrsAction or NicPropertyAction);
        var hardware = tweaks.Where(NeedsHardware).ToList();
        var expert = tweaks.Except(hardware).Where(t => t.EffectiveRisk == Risk.Expert || t.IsBootCritical).ToList();
        var restart = tweaks.Except(hardware).Except(expert).Where(t => t.Restart || t.SignOut).ToList();
        var other = tweaks.Except(hardware).Except(expert).Except(restart).ToList();

        var sb = new StringBuilder();
        sb.Append("The tables below cover all ").Append(tweaks.Count).Append(" catalog tweaks. Preview tweaks have not been tested on real Windows yet; test them first.\n\n");
        void Table(string heading, string intro, IEnumerable<TweakDefinition> list, bool hardwareColumn)
        {
            sb.Append("## ").Append(heading).Append("\n\n");
            if (intro.Length > 0) sb.Append(intro).Append("\n\n");
            sb.Append(hardwareColumn ? "| Tweak | Title | Risk | Needs | Actions | Result |\n|---|---|---|---|---|---|\n" : "| Tweak | Title | Risk | Then | Actions | Result |\n|---|---|---|---|---|---|\n");
            foreach (var t in list)
            {
                var third = hardwareColumn
                    ? t.Actions.Any(a => a is NvidiaDrsAction) ? "NVIDIA GPU" : "physical network adapter"
                    : t.Restart ? "restart" : t.SignOut ? "sign out" : "";
                var actions = string.Join(", ", t.Actions.Select(ActionType).Distinct());
                sb.Append("| `").Append(t.Id).Append("` | ").Append(Title(t)).Append(" | ").Append(RiskText(t)).Append(" | ").Append(third)
                  .Append(" | ").Append(actions).Append(" | |\n");
            }
            sb.Append('\n');
        }
        Table("2. Expert and boot-critical tweaks",
            "Turn on Expert mode. Take a checkpoint before **each** of these, apply it alone, restart, confirm Windows still starts, then undo and restart again.",
            expert, false);
        Table("3. Tweaks that need a restart or sign-out", "", restart, false);
        Table("4. Other tweaks", "", other, false);
        Table("5. Tweaks that need real hardware",
            "A VM has no NVIDIA GPU and only a synthetic network adapter. Test these on a spare real PC with the hardware named, one at a time, with a restore point first.",
            hardware, true);
        return sb.ToString();
    }

    private static string Title(TweakDefinition t) => (DocStore.Get(t.DocId, "en")?.Title ?? t.Id).Replace("|", "/");

    private static string RiskText(TweakDefinition t)
    {
        var parts = new List<string> { t.EffectiveRisk.ToString().ToLowerInvariant() };
        if (t.IsBootCritical) parts.Add("boot-critical");
        if (t.AntiCheatSensitive) parts.Add("anti-cheat sensitive");
        if (t.Preview) parts.Add("preview");
        return string.Join(", ", parts);
    }

    private static string ActionType(TweakAction a)
    {
        var name = a.GetType().Name.Replace("Action", "");
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
