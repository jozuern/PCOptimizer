using Optimizer.Core.Apps;

namespace Optimizer.Core.Tests;

/// <summary>Program Files folders no installed program uses: offered only when nothing points into them and they are quiet.</summary>
public class ProgramOrphansTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pco-test-" + Guid.NewGuid().ToString("N"));
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    public ProgramOrphansTests() => Directory.CreateDirectory(_root);

    public void Dispose() => TestFolders.Delete(_root);

    /// <summary>A folder with one file, everything last written <paramref name="daysAgo"/> days before <see cref="Now"/>.</summary>
    private string Folder(string pf, string name, int daysAgo)
    {
        var folder = Path.Combine(pf, name);
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "app.exe");
        File.WriteAllText(file, "x");
        var time = Now.AddDays(-daysAgo);
        File.SetLastWriteTimeUtc(file, time);
        Directory.SetLastWriteTimeUtc(folder, time);
        return folder;
    }

    [Fact]
    public void OnlyQuietFoldersNothingUsesAreOffered()
    {
        var pf = Path.Combine(_root, "Program Files");
        var startMenu = Path.Combine(_root, "Start Menu");
        Directory.CreateDirectory(startMenu);
        File.WriteAllText(Path.Combine(startMenu, "Linked.lnk"), "");
        var old = Folder(pf, "Old Tool", 400);
        Folder(pf, "Common Files", 400);           // shared by many programs
        Folder(pf, "NuGet", 400);                  // NuGet's machine-wide config (Visual Studio's fallback source): nothing points into it
        Folder(pf, "Contoso Studio", 400);         // named after an installed program
        var used = Folder(pf, "Used", 400);        // a service runs a file in it
        Folder(pf, "Fresh", 3);                    // changed recently: maybe just installed or a portable app in use
        var linked = Folder(pf, "Linked", 400);    // a Start menu shortcut points into it
        Folder(pf, "Ab", 400);                     // too short a name to tell

        var roots = new LeftoverRoots([pf], [startMenu], [Path.Combine(_root, "Keep")]);
        var evidence = new OrphanEvidence([Path.Combine(used, "svc.exe")], ["Contoso Studio 2026 (x64)", "Contoso Ltd."]);
        var found = ProgramOrphans.Find([pf, Path.Combine(_root, "missing")], roots, evidence, Now,
            lnk => lnk.EndsWith("Linked.lnk", StringComparison.OrdinalIgnoreCase) ? Path.Combine(linked, "app.exe") : null);

        var only = Assert.Single(found);
        Assert.Equal(old, only.Path);
        Assert.Equal(LeftoverKind.Folder, only.Kind);
        Assert.Equal(1, only.Bytes);
        Assert.Equal(Now.AddDays(-400), only.LastChangeUtc);
    }

    [Theory]
    [InlineData("7-Zip", "7-Zip 25.01 (x64 edition)", true)]
    [InlineData("Mozilla Firefox", "Mozilla Firefox (x64 de)", true)]
    [InlineData("Adobe", "Adobe Inc.", true)]
    [InlineData("NVIDIA Corporation", "NVIDIA", true)]
    [InlineData("Old Tool", "7-Zip 25.01 (x64 edition)", false)]
    [InlineData("Vendor App", "App", false)] // "app" is too short to count inside a folder name
    public void FolderNamesAreMatchedToInstalledPrograms(string folder, string installed, bool match) =>
        Assert.Equal(match, ProgramOrphans.MatchesInstalledName(folder, [ProgramOrphans.Normalize(installed)]));
}
