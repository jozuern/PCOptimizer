using System.Security.Cryptography;
using Optimizer.Core.Platform;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Tests;

/// <summary>The app runs as administrator: links, downloads and files in user-writable places must not be trusted blindly.</summary>
public class SecurityTests
{
    private const string Download = "https://github.com/jozuern/PCOptimizer/releases/download/v1.0.0/";

    [Theory]
    [InlineData(Download + "PCOptimizer.exe", true)]
    [InlineData(Download + "../../../../evil/repo/releases/download/v1.0.0/PCOptimizer.exe", false)]
    [InlineData(Download + "%2e%2e/%2e%2e/PCOptimizer.exe", false)]
    [InlineData(Download + "PCOptimizer.exe?x=1", false)]
    [InlineData(Download + "PCOptimizer.exe#x", false)]
    [InlineData("http://github.com/jozuern/PCOptimizer/releases/download/v1.0.0/PCOptimizer.exe", false)]
    [InlineData("https://github.com/jozuern/PCOptimizer/releases/download/v1.0.0\\..\\PCOptimizer.exe", false)]
    public void ReleaseDownloadsMustStayInThisRepository(string url, bool expected) =>
        Assert.Equal(expected, ReleaseCheck.IsReleaseDownload(url));

    [Fact]
    public void ReleaseAssetsWithDotSegmentsAreIgnored()
    {
        var json = $$"""
            {"tag_name":"v1.0.0","assets":[
              {"name":"PCOptimizer.exe","browser_download_url":"{{Download}}../../../../evil/x/releases/download/v1/PCOptimizer.exe"},
              {"name":"PCOptimizer.exe.sha256","browser_download_url":"{{Download}}PCOptimizer.exe.sha256"}]}
            """;
        Assert.Null(ReleaseCheck.Evaluate(new Version(0, 3, 0), json).Assets);
    }

    [Fact]
    public void UpdatedExeStartsOnlyWithTheCheckedHashAndCannotBeSwappedMeanwhile()
    {
        var folder = Path.Combine(Path.GetTempPath(), "pco-start-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var exe = Path.Combine(folder, "PCOptimizer.exe");
            File.WriteAllText(exe, "new exe");
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(exe)));

            var started = false;
            Exception? swap = null;
            Assert.True(Updater.StartVerified(exe, hash, _ =>
            {
                started = true;
                // While the process starts, another process cannot replace or rename the file.
                swap = Record.Exception(() => File.WriteAllText(exe, "planted"));
                swap ??= Record.Exception(() => File.Move(exe, exe + ".moved"));
            }));
            Assert.True(started);
            Assert.IsType<IOException>(swap);
            Assert.Equal("new exe", File.ReadAllText(exe));

            File.WriteAllText(exe, "planted");
            Assert.False(Updater.StartVerified(exe, hash, _ => throw new InvalidOperationException("must not start")));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [Theory]
    [InlineData("https://learn.microsoft.com/en-us/windows/", true)]
    [InlineData("http://example.com", true)]
    [InlineData("ms-windows-store://pdp/?PFN=Microsoft.WindowsCalculator_8wekyb3d8bbwe", true)]
    [InlineData(@"C:\Users\Public\evil.exe", false)]
    [InlineData("file:///C:/Windows/System32/cmd.exe", false)]
    [InlineData("ms-settings:display", false)]
    [InlineData("search-ms:query=x", false)]
    [InlineData("not a link", false)]
    public void OnlyWebAndStoreLinksAreOpened(string target, bool expected) =>
        Assert.Equal(expected, DeElevatedLauncher.IsLink(target));

    // ---------------- Data folder ----------------

    private static string TempFolder(string name)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"pco-{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>
    /// A standard user can plant backups or settings in the data folder before the first elevated start. The test
    /// process is not elevated, so the files it creates are owned by this user, as a planted file would be.
    /// </summary>
    [Fact]
    public void FilesNotCreatedByAnAdministratorAreRemoved()
    {
        if (DataPaths.ProcessIsElevated) return; // files created elevated are owned by Administrators
        var root = TempFolder("untrusted");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "backups"));
            File.WriteAllText(Path.Combine(root, "backups", "planted.json"), "{}");
            File.WriteAllText(Path.Combine(root, "settings.json"), "{}");
            Assert.False(Backup.SecureFolder.IsOwnedByAdmins(Path.Combine(root, "settings.json")));

            Backup.SecureFolder.RemoveUntrusted(root);

            Assert.Empty(Directory.EnumerateFileSystemEntries(root));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void RemoveLinksDeletesJunctionsButNotTheirTargets()
    {
        var root = TempFolder("links");
        var target = TempFolder("target");
        try
        {
            File.WriteAllText(Path.Combine(target, "keep.txt"), "x");
            Directory.CreateSymbolicLink(Path.Combine(root, "logs"), target);
        }
        catch (IOException)
        {
            return; // symbolic links need Developer Mode or admin rights
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }
        try
        {
            Backup.SecureFolder.RemoveLinks(root);
            Assert.False(Path.Exists(Path.Combine(root, "logs")));
            Assert.True(File.Exists(Path.Combine(target, "keep.txt")));
        }
        finally
        {
            Directory.Delete(root, true);
            Directory.Delete(target, true);
        }
    }

    [Fact]
    public void SafeDeleteHandlesPathsLongerThanTheOldBuffer()
    {
        var root = TempFolder("long");
        try
        {
            var dir = root;
            while (dir.Length < 1200) dir = Path.Combine(dir, new string('d', 60));
            Directory.CreateDirectory(dir);
            var file = Path.Combine(dir, "old.tmp");
            File.WriteAllText(file, "x");
            SafeDelete.DeleteFile(file);
            Assert.False(File.Exists(file));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    // ---------------- Native libraries ----------------

    [Fact]
    public void PlainDllNamesResolveToSystem32First()
    {
        var trusted = TempFolder("trusted");
        try
        {
            File.WriteAllText(Path.Combine(trusted, "wpfgfx_cor3.dll"), "");
            File.WriteAllText(Path.Combine(trusted, "powrprof.dll"), "");
            var system32 = Environment.SystemDirectory;
            Assert.Equal(Path.Combine(system32, "powrprof.dll"), NativeLibraryGuard.Locate("powrprof.dll", system32, [trusted]), ignoreCase: true);
            Assert.Equal(Path.Combine(system32, "powrprof.dll"), NativeLibraryGuard.Locate("powrprof", system32, [trusted]), ignoreCase: true);
            Assert.Equal(Path.Combine(trusted, "wpfgfx_cor3.dll"), NativeLibraryGuard.Locate("wpfgfx_cor3", system32, [trusted]));
            Assert.Null(NativeLibraryGuard.Locate(@"C:\Elsewhere\x.dll", system32, [trusted]));
            Assert.Null(NativeLibraryGuard.Locate("not-a-real-library-xyz", system32, [trusted]));
        }
        finally
        {
            Directory.Delete(trusted, true);
        }
    }

    [Fact]
    public void DllNextToTheExeIsDetected()
    {
        var app = TempFolder("app");
        try
        {
            File.WriteAllText(Path.Combine(app, "atiadlxx.dll"), "planted");
            Assert.True(NativeLibraryGuard.IsNextToExe("atiadlxx", app));
            Assert.True(NativeLibraryGuard.IsNextToExe("atiadlxx.dll", app));
            Assert.False(NativeLibraryGuard.IsNextToExe("nvml.dll", app));
            Assert.False(NativeLibraryGuard.IsNextToExe("atiadlxx.dll", null));
        }
        finally
        {
            Directory.Delete(app, true);
        }
    }

    [Theory]
    [InlineData(true, null, false, true)]
    [InlineData(true, @"C:\Users\me\AppData\Local\Temp", false, true)]
    [InlineData(true, @"C:\ProgramData\PCOptimizer\runtime", false, false)]
    [InlineData(true, @"C:\ProgramData\PCOptimizer\runtime\", false, false)]
    [InlineData(true, null, true, false)]
    [InlineData(false, null, false, false)]
    public void SingleFileExeRestartsOnceWithTheProtectedExtractionFolder(bool bundle, string? current, bool restarted, bool expected) =>
        Assert.Equal(expected, SingleFileRuntime.NeedsRestart(bundle, current, @"C:\ProgramData\PCOptimizer\runtime", restarted));

    [Fact]
    public void OnlyExtractionFoldersInsideTheDataFolderAreTrusted()
    {
        var list = SingleFileRuntime.TrustedFolders(
            @"C:\ProgramData\PCOptimizer\runtime\PCOptimizer\abc123\;C:\Users\me\Downloads\;C:\ProgramData\PCOptimizer\runtime-evil\x;relative\path",
            @"C:\ProgramData\PCOptimizer\runtime");
        Assert.Equal([@"C:\ProgramData\PCOptimizer\runtime\PCOptimizer\abc123"], list);
    }
}
