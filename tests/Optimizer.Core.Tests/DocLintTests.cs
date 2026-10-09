using Optimizer.Core.Docs;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Explanation lint (plan v4 §4.12): warning in Debug builds, error in Release builds.</summary>
public class DocLintTests(ITestOutputHelper output)
{
    [Fact]
    public void ExplanationsAreComplete()
    {
        var issues = DocLint.Run();
        foreach (var issue in issues) output.WriteLine(issue.ToString());
#if DEBUG
        // Debug: report only, development is not blocked.
        Assert.True(true);
#else
        Assert.Empty(issues);
#endif
    }

    [Fact]
    public void LabelsExistInBothLanguages() =>
        Assert.DoesNotContain(DocLint.Run(), i => i.DocId == "labels.json");

    /// <summary>Style rule: no middle dots, bullets, en or em dashes or ellipsis characters in anything the user reads.</summary>
    [Fact]
    public void UserVisibleTextHasNoBannedTypography()
    {
        char[] banned = ['–', '—', '•', '·', '…'];
        var offenders = new List<string>();
        foreach (var name in Catalog.CatalogData.ResourceNames("Catalog."))
        {
            var text = Catalog.CatalogData.ReadResourceText(name);
            // JSON comments ("_comment") are for maintainers, not shown in the app.
            var visible = name.EndsWith(".json", StringComparison.Ordinal)
                ? string.Join("\n", text.Split('\n').Where(l => !l.TrimStart().StartsWith("\"_comment\"", StringComparison.Ordinal)))
                : text;
            foreach (var c in banned.Where(visible.Contains)) offenders.Add($"{name}: U+{(int)c:X4}");
        }
        foreach (var o in offenders) output.WriteLine(o);
        Assert.Empty(offenders);
    }
}
