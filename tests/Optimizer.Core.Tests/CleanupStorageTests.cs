using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Catalog;
using Optimizer.Core.Cleanup;
using Optimizer.Core.Debloat;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Findings.Checks;
using Optimizer.Core.Hardware;
using Optimizer.Core.Services;
using Optimizer.Core.Startup;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tests;

public class CleanupStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pco-test-" + Guid.NewGuid().ToString("N"));

    public CleanupStorageTests() => Directory.CreateDirectory(_root);

    public void Dispose() => TestFolders.Delete(_root);

    private string File(string relative, int bytes, DateTime? written = null)
    {
        var path = Path.Combine(_root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        System.IO.File.WriteAllBytes(path, new byte[bytes]);
        if (written is { } w)
        {
            System.IO.File.SetLastWriteTimeUtc(path, w);
            System.IO.File.SetCreationTimeUtc(path, w);
        }
        return path;
    }

    [Fact]
    public void CleanupNeverFollowsJunctionsAndRespectsAge()
    {
        var outside = Path.Combine(_root, "outside");
        var keep = File(@"outside\precious.txt", 10, DateTime.UtcNow.AddDays(-30));
        var temp = Path.Combine(_root, "temp");
        var old = File(@"temp\old.tmp", 100, DateTime.UtcNow.AddDays(-3));
        var fresh = File(@"temp\fresh.tmp", 100);
        File(@"temp\sub\old2.tmp", 50, DateTime.UtcNow.AddDays(-3));
        var ini = File(@"temp\desktop.ini", 5, DateTime.UtcNow.AddDays(-3));
        // Junction inside the cleaned folder pointing outside (no admin rights needed for junctions).
        var junction = Path.Combine(temp, "link");
        Junction(junction, outside);

        var category = new CleanupCategory("cleanup.test", [temp], MinAge: CleanupEngine.TempMinAge);
        var scan = CleanupEngine.Scan(category);
        Assert.Equal((2, 150L), (scan.Files, scan.Bytes));
        var result = CleanupEngine.Clean(category);
        Assert.Equal(2, result.Deleted);
        Assert.False(System.IO.File.Exists(old));
        Assert.True(System.IO.File.Exists(fresh));
        Assert.True(System.IO.File.Exists(ini));
        Assert.True(System.IO.File.Exists(keep));        // never reached through the junction
        Assert.False(Directory.Exists(Path.Combine(temp, "sub"))); // emptied subfolder removed
        Assert.True(Directory.Exists(temp));             // root kept
    }

    private static void Junction(string link, string target) => TestFolders.Junction(link, target);

    [Fact]
    public void CleanupSkipsARootThatIsOrLiesUnderAJunction()
    {
        // %LOCALAPPDATA%\Temp (or a parent like %LOCALAPPDATA%\NVIDIA) replaced by a junction to a protected folder.
        var keep = File(@"protected\program.dll", 10, DateTime.UtcNow.AddDays(-30));
        var keep2 = File(@"protected\DXCache\shader.bin", 10, DateTime.UtcNow.AddDays(-30));
        var temp = Path.Combine(_root, "Temp");
        Junction(temp, Path.Combine(_root, "protected"));
        var nvidia = Path.Combine(_root, "NVIDIA");
        Junction(nvidia, Path.Combine(_root, "protected"));

        var category = new CleanupCategory("cleanup.test", [temp, Path.Combine(nvidia, "DXCache")]);
        Assert.Equal(0, CleanupEngine.Scan(category).Files);
        Assert.Equal(0, CleanupEngine.Clean(category).Deleted);
        Assert.True(System.IO.File.Exists(keep));
        Assert.True(System.IO.File.Exists(keep2));
    }

    [Fact]
    public void BackupFolderLinksAreRemovedWithoutTouchingTargets()
    {
        // A standard user pre-created the backup root (or a subfolder) as a junction to a folder they control.
        var userFiles = Path.Combine(_root, "user");
        var planted = File(@"user\backups\x.json", 10);
        var root = Path.Combine(_root, "PCOptimizer");
        Junction(root, userFiles);
        Optimizer.Core.Backup.SecureFolder.RemoveLinks(root);
        Assert.False(Directory.Exists(root));
        Assert.True(System.IO.File.Exists(planted)); // the target is left alone

        Directory.CreateDirectory(root);
        Junction(Path.Combine(root, "backups"), Path.Combine(userFiles, "backups"));
        Optimizer.Core.Backup.SecureFolder.RemoveLinks(root);
        Assert.False(Directory.Exists(Path.Combine(root, "backups")));
        Assert.True(System.IO.File.Exists(planted));
    }

    [Fact]
    public void SafeDeleteRefusesAPathThatResolvesElsewhere()
    {
        // The scan saw temp\sub\old.tmp; then sub was swapped for a junction to a protected folder with the same file name.
        var victim = File(@"protected\old.tmp", 10);
        Directory.CreateDirectory(Path.Combine(_root, "temp"));
        var sub = Path.Combine(_root, @"temp\sub");
        Junction(sub, Path.Combine(_root, "protected"));
        Assert.Throws<IOException>(() => Optimizer.Core.Platform.SafeDelete.DeleteFile(Path.Combine(sub, "old.tmp")));
        Assert.True(System.IO.File.Exists(victim));
        Assert.False(Optimizer.Core.Platform.SafeDelete.HasNoLinks(Path.Combine(sub, "old.tmp")));
        Assert.False(Optimizer.Core.Platform.SafeDelete.ResolvesToItself(Path.Combine(sub, "old.tmp")));
        Assert.True(Optimizer.Core.Platform.SafeDelete.ResolvesToItself(victim));
        // The storage analyzer refuses to recycle it as well.
        Assert.Equal([Path.Combine(sub, "old.tmp")], StorageAnalyzer.Recycle([Path.Combine(sub, "old.tmp")], []));
        Assert.True(System.IO.File.Exists(victim));

        // A normal read-only file is deleted.
        var ro = File(@"temp\readonly.tmp", 10);
        System.IO.File.SetAttributes(ro, FileAttributes.ReadOnly);
        Optimizer.Core.Platform.SafeDelete.DeleteFile(ro);
        Assert.False(System.IO.File.Exists(ro));
    }

    [Fact]
    public async Task StorageAnalyzerFindsLargestAndDuplicates()
    {
        var a = File(@"docs\a.bin", 2 << 20);
        var b = File(@"backup\a-copy.bin", 2 << 20);
        System.IO.File.WriteAllBytes(Path.Combine(_root, @"docs\different.bin"), [.. new byte[(2 << 20) - 1], 1]);
        File(@"docs\small.txt", 100);
        // The test folder lies in %TEMP% under AppData, which the analyzer protects; that rule is checked below.
        var report = await StorageAnalyzer.ScanAsync(_root, StorageAnalyzer.ProtectedRoots(null), protectAppData: false);
        Assert.Equal(4, report.ScannedFiles);
        var dup = Assert.Single(report.Duplicates);
        Assert.Equal(new[] { b, a }.Order(StringComparer.OrdinalIgnoreCase), dup.Paths);
        Assert.Equal(2L << 20, dup.Reclaimable);
        Assert.Contains(report.LargestFolders, f => f.Path.EndsWith("docs", StringComparison.OrdinalIgnoreCase));
        Assert.True(StorageAnalyzer.IsProtected(@"C:\Windows\System32\x.dll", StorageAnalyzer.ProtectedRoots(null)));
        Assert.True(StorageAnalyzer.IsProtected(@"C:\Users\x\AppData\Local\y.db", StorageAnalyzer.ProtectedRoots(null)));
        Assert.True(StorageAnalyzer.IsProtected(a, StorageAnalyzer.ProtectedRoots(null)));
        Assert.False(StorageAnalyzer.IsProtected(a, StorageAnalyzer.ProtectedRoots(null), appData: false));
        Assert.True(StorageAnalyzer.IsProtected(a, StorageAnalyzer.ProtectedRoots([Path.Combine(_root, "docs")]), appData: false)); // game library
    }

    [Fact]
    public void UpdateRepairRenamesAndRestartsServices()
    {
        var win = Path.Combine(_root, "Windows");
        Directory.CreateDirectory(Path.Combine(win, "SoftwareDistribution"));
        Directory.CreateDirectory(Path.Combine(win, @"System32\catroot2"));
        var processes = new FakeProcesses { Handler = (f, a) => (a.StartsWith("stop bits", StringComparison.Ordinal) ? 2 : 0, "") };
        var steps = UpdateRepair.Run(processes, windowsDir: win, now: new DateTime(2026, 10, 9, 12, 0, 0));
        Assert.All(steps, s => Assert.True(s.Ok, s.Key));
        Assert.True(Directory.Exists(Path.Combine(win, "SoftwareDistribution.old-20261009-120000")));
        Assert.True(Directory.Exists(Path.Combine(win, @"System32\catroot2.old-20261009-120000")));
        Assert.Equal(4, processes.Calls.Count(c => c.Contains(" stop ")));
        Assert.Equal(3, processes.Calls.Count(c => c.Contains(" start ")));
        Assert.All(steps, s => Assert.True(Labels.Current.Has("en", s.Key) && Labels.Current.Has("de", s.Key), s.Key));
    }
}
