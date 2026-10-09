using System.Text.RegularExpressions;
using System.Xml.Linq;
using Optimizer.Core.Docs;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Explanation lint (plan v4 §4.12): warning in Debug builds, error in Release builds.</summary>
public partial class DocLintTests(ITestOutputHelper output)
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

    /// <summary>Style rule: no middle dots, bullets, en or em dashes, ellipsis characters or "(s)" plurals in anything the user reads.</summary>
    [Fact]
    public void UserVisibleTextHasNoBannedTypography()
    {
        var offenders = new List<string>();
        foreach (var name in Catalog.CatalogData.ResourceNames("Catalog."))
        {
            var text = Catalog.CatalogData.ReadResourceText(name);
            // JSON comments ("_comment") are for maintainers, not shown in the app.
            var visible = name.EndsWith(".json", StringComparison.Ordinal)
                ? string.Join("\n", text.Split('\n').Where(l => !l.TrimStart().StartsWith("\"_comment\"", StringComparison.Ordinal)))
                : text;
            offenders.AddRange(BannedTypography(name, visible));
        }
        // The app's own UI text: resource string values and literal text in XAML (comments are for maintainers).
        var app = AppSourceFolder();
        foreach (var file in Directory.EnumerateFiles(Path.Combine(app, "Resources"), "Strings*.resx"))
            offenders.AddRange(BannedTypography(Path.GetFileName(file),
                string.Join("\n", XDocument.Load(file).Descendants("data").Select(d => (string?)d.Element("value") ?? ""))));
        foreach (var file in Directory.EnumerateFiles(app, "*.xaml", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(app, file);
            if (relative.StartsWith("bin", StringComparison.OrdinalIgnoreCase) || relative.StartsWith("obj", StringComparison.OrdinalIgnoreCase)) continue;
            offenders.AddRange(BannedTypography(relative, XamlComment().Replace(File.ReadAllText(file), "")));
        }
        foreach (var o in offenders) output.WriteLine(o);
        Assert.Empty(offenders);
    }

    private static IEnumerable<string> BannedTypography(string name, string text)
    {
        char[] banned = ['–', '—', '•', '·', '…'];
        foreach (var c in banned.Where(text.Contains)) yield return $"{name}: U+{(int)c:X4}";
        foreach (Match m in ParenPlural().Matches(text)) yield return $"{name}: \"{m.Value}\" plural";
    }

    /// <summary>src/Optimizer.App, found by walking up from the test output folder.</summary>
    private static string AppSourceFolder()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var app = Path.Combine(dir.FullName, "src", "Optimizer.App");
            if (Directory.Exists(app)) return app;
        }
        throw new DirectoryNotFoundException("src/Optimizer.App not found above " + AppContext.BaseDirectory);
    }

    /// <summary>"value(s)", "Wert(e)", "Änderung(en)" and similar.</summary>
    [GeneratedRegex(@"\p{L}\((?:s|e|en|n|es)\)")]
    private static partial Regex ParenPlural();

    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex XamlComment();
}
