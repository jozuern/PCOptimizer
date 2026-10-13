using Optimizer.Core.Apps;

namespace Optimizer.Core.Tests;

/// <summary>Searching winget for any package: a safe query, and the table winget prints read in any display language.</summary>
public class WingetSearchTests
{
    // Printed by winget 1.29 in the VM for "notepad" (lines shortened to a few).
    private const string English = """
        Name                 Id                             Version    Match
        ---------------------------------------------------------------------------------------
        AkelPad              AkelPad.AkelPad                4.9.9      Tag: notepad
        NoteTab Light        FookesHolding.NoteTabLight     7.2        Tag: notepad
        Notepad++            Notepad++.Notepad++            8.9.8.1    Tag: notepad
        Programmer's Notepad SimonSteele.ProgrammersNotepad 2.4.2
        Notepad4 (AVX2)      zufuliu.notepad4.AVX2          26.08r6282
        """;

    [Fact]
    public void ResultsAreReadByColumnPosition()
    {
        var found = Winget.ParseSearch(English);
        Assert.Equal(5, found.Count);
        Assert.Equal(new WingetSearchResult("NoteTab Light", "FookesHolding.NoteTabLight", "7.2"), found[1]);
        Assert.Equal(new WingetSearchResult("Notepad++", "Notepad++.Notepad++", "8.9.8.1"), found[2]);
        Assert.Equal(new WingetSearchResult("Programmer's Notepad", "SimonSteele.ProgrammersNotepad", "2.4.2"), found[3]);
        Assert.Equal("26.08r6282", found[4].Version);
    }

    [Fact]
    public void TranslatedHeadersSpinnersShortenedIdsAndNotesAreHandled()
    {
        const string german = "\r   - \r   \\ \rName      ID                 Version Übereinstimmung Quelle\n" +
                              "-------------------------------------------------------------\n" +
                              "7-Zip     7zip.7zip          26.04                   winget\n" +
                              "Long app  Vendor.VeryLongPac\u2026 1.0                   winget\n" +
                              "Weitere Ergebnisse sind verfügbar\n";
        var found = Winget.ParseSearch(german);
        Assert.Equal(new WingetSearchResult("7-Zip", "7zip.7zip", "26.04"), Assert.Single(found));
        Assert.Empty(Winget.ParseSearch("No package found matching input criteria."));
        Assert.Empty(Winget.ParseSearch(""));
    }

    [Theory]
    [InlineData("notepad", true)]
    [InlineData("Visual Studio Code", true)]
    [InlineData("C++ Redist", true)]
    [InlineData("Übersetzer", true)]
    [InlineData("a", false)]
    [InlineData("x\" --source evil", false)]
    [InlineData("a & calc", false)]
    [InlineData("a|b", false)]
    public void QueriesAreLimitedToSafeCharacters(string query, bool ok)
    {
        Assert.Equal(ok, Winget.IsSafeQuery(query));
        if (ok) Assert.Equal($"search --query \"{query}\" --source winget --count 50 --accept-source-agreements --disable-interactivity", Winget.SearchArguments(query));
        else Assert.Throws<ArgumentException>(() => Winget.SearchArguments(query));
    }
}
