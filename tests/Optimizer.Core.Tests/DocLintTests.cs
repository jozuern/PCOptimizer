using System.Text.RegularExpressions;
using System.Xml.Linq;
using Optimizer.Core.Docs;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Explanation lint: warning in Debug builds, error in Release builds.</summary>
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
        var app = RepoPaths.App;
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

    /// <summary>The same rule for the repository's own documents, and for code (comments included), minus the plural rule there.</summary>
    [Fact]
    public void DocumentsAndCodeHaveNoBannedTypography()
    {
        var offenders = new List<string>();
        foreach (var file in RepositoryFiles("*.md").Where(f => !f.Replace('\\', '/').Contains("/Catalog/Docs/", StringComparison.Ordinal)))
            offenders.AddRange(BannedTypography(Path.GetRelativePath(RepoPaths.Root, file), File.ReadAllText(file)));
        foreach (var file in RepositoryFiles("*.cs").Concat(RepositoryFiles("*.csproj")).Concat(RepositoryFiles("*.yml")))
            offenders.AddRange(BannedCharacters(Path.GetRelativePath(RepoPaths.Root, file), File.ReadAllText(file)));
        foreach (var o in offenders) output.WriteLine(o);
        Assert.Empty(offenders);
    }

    /// <summary>Filler words in everything the user reads besides the explanation pages (which the docs lint covers).</summary>
    [Fact]
    public void UserVisibleTextHasNoMarketingWords()
    {
        var offenders = new List<string>();
        void Check(string name, string text) => offenders.AddRange(DocLint.FindBannedWords(text).Select(w => $"{name}: {w}"));
        foreach (var name in Catalog.CatalogData.ResourceNames("Catalog.Data."))
            Check(name, Catalog.CatalogData.ReadResourceText(name));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(RepoPaths.App, "Resources"), "Strings*.resx"))
            Check(Path.GetFileName(file), string.Join("\n", XDocument.Load(file).Descendants("data").Select(d => (string?)d.Element("value") ?? "")));
        foreach (var file in Directory.EnumerateFiles(RepoPaths.App, "*.xaml", SearchOption.AllDirectories).Where(f => !IsBuildOutput(f)))
            Check(Path.GetFileName(file), XamlComment().Replace(File.ReadAllText(file), ""));
        // The style guide quotes the banned words as examples.
        foreach (var file in RepositoryFiles("*.md").Where(f => !f.Replace('\\', '/').Contains("/Catalog/Docs/", StringComparison.Ordinal) && !f.EndsWith("explanation-style-guide.md", StringComparison.Ordinal)))
            Check(Path.GetRelativePath(RepoPaths.Root, file), File.ReadAllText(file));
        foreach (var o in offenders) output.WriteLine(o);
        Assert.Empty(offenders);
    }

    [Theory]
    [InlineData("A seamless experience", true)]
    [InlineData("Ultimate Performance power plan", false)]
    [InlineData("Ultimate gaming", true)]
    [InlineData("The process runs elevated", false)]
    [InlineData("Unlock more FPS", true)]
    [InlineData("Aggressive at guaranteed", false)]
    [InlineData("Eine nahtlose Erfahrung", true)]
    [InlineData("nahtlos integriert", true)]
    public void BannedWordsMatchWholeWordsOnly(string text, bool banned) =>
        Assert.Equal(banned, DocLint.FindBannedWords(text).Any());

    private static readonly char[] Banned = ['\u2013', '\u2014', '\u2022', '\u00B7', '\u2026'];

    private static IEnumerable<string> BannedTypography(string name, string text)
    {
        foreach (var hit in BannedCharacters(name, text)) yield return hit;
        foreach (Match m in ParenPlural().Matches(text)) yield return $"{name}: \"{m.Value}\" plural";
    }

    private static IEnumerable<string> BannedCharacters(string name, string text) =>
        Banned.Where(text.Contains).Select(c => $"{name}: U+{(int)c:X4}");

    private static bool IsBuildOutput(string path)
    {
        var p = path.Replace('\\', '/');
        return p.Contains("/bin/", StringComparison.OrdinalIgnoreCase) || p.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Tracked-looking files of the repository: build output, .git and the local audit folder are skipped.</summary>
    private static IEnumerable<string> RepositoryFiles(string pattern) =>
        Directory.EnumerateFiles(RepoPaths.Root, pattern, SearchOption.AllDirectories).Where(f =>
        {
            var relative = Path.GetRelativePath(RepoPaths.Root, f).Replace('\\', '/');
            return !IsBuildOutput("/" + relative) && !relative.StartsWith(".git/", StringComparison.Ordinal) && !relative.StartsWith(".audit/", StringComparison.Ordinal)
                   && !relative.StartsWith(".claude/", StringComparison.Ordinal) && !relative.StartsWith("artifacts/", StringComparison.Ordinal)
                   && !relative.Equals("CLAUDE.md", StringComparison.Ordinal);
        });

    /// <summary>"value(s)", "Wert(e)", "Änderung(en)" and similar.</summary>
    [GeneratedRegex(@"\p{L}\((?:s|e|en|n|es)\)")]
    private static partial Regex ParenPlural();

    [GeneratedRegex("<!--.*?-->", RegexOptions.Singleline)]
    private static partial Regex XamlComment();
}
