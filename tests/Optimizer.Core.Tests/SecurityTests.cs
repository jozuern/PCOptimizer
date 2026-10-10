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
            TestFolders.Delete(folder);
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

    [Fact]
    public void ElevatedToolsStartWithoutUserControlledCodePaths()
    {
        var psi = new System.Diagnostics.ProcessStartInfo(ProcessHardening.ResolveSystemTool("powershell.exe"));
        psi.Environment["PSModulePath"] = @"C:\Users\me\Documents\WindowsPowerShell\Modules";
        psi.Environment["COR_ENABLE_PROFILING"] = "1";
        psi.Environment["COR_PROFILER_PATH"] = @"C:\Users\me\evil.dll";
        psi.Environment["DOTNET_STARTUP_HOOKS"] = @"C:\Users\me\hook.dll";
        psi.Environment["PATH_KEEP"] = "x";
        ProcessHardening.Apply(psi);
        Assert.Equal(ProcessHardening.PowerShellModules, psi.Environment["PSModulePath"]);
        Assert.False(psi.Environment.ContainsKey("COR_ENABLE_PROFILING"));
        Assert.False(psi.Environment.ContainsKey("COR_PROFILER_PATH"));
        Assert.False(psi.Environment.ContainsKey("DOTNET_STARTUP_HOOKS"));
        Assert.Equal("x", psi.Environment["PATH_KEEP"]);
        Assert.Equal(Environment.SystemDirectory, psi.WorkingDirectory);
        Assert.StartsWith(Environment.SystemDirectory, psi.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine(Environment.SystemDirectory, "bcdedit.exe"), ProcessHardening.ResolveSystemTool("bcdedit.exe"));
    }

    // ---------------- Data folder ----------------

    private static string TempFolder(string name) => TestFolders.Create(name);

    /// <summary>
    /// A standard user can plant backups or settings in the data folder before the first elevated start. Such files
    /// are owned by the user; when the test runs elevated (files would be owned by Administrators) the owner is set
    /// to the user explicitly.
    /// </summary>
    [Fact]
    public void FilesNotCreatedByAnAdministratorAreRemoved()
    {
        var root = TempFolder("untrusted");
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "backups"));
            var planted = Path.Combine(root, "backups", "planted.json");
            var settings = Path.Combine(root, "settings.json");
            File.WriteAllText(planted, "{}");
            File.WriteAllText(settings, "{}");
            if (DataPaths.ProcessIsElevated)
                foreach (var f in new[] { planted, settings }) OwnByUser(f);
            Assert.False(Backup.SecureFolder.IsOwnedByAdmins(settings));

            Backup.SecureFolder.RemoveUntrusted(root);

            Assert.Empty(Directory.EnumerateFileSystemEntries(root));
        }
        finally
        {
            TestFolders.Delete(root);
        }
    }

    private static void OwnByUser(string file)
    {
        var info = new FileInfo(file);
        var security = info.GetAccessControl();
        security.SetOwner(System.Security.Principal.WindowsIdentity.GetCurrent().User!);
        info.SetAccessControl(security);
    }

    [Fact]
    public void RemoveLinksDeletesJunctionsButNotTheirTargets()
    {
        var root = TempFolder("links");
        var target = TempFolder("target");
        try
        {
            File.WriteAllText(Path.Combine(target, "keep.txt"), "x");
            TestFolders.Junction(Path.Combine(root, "logs"), target);
            Backup.SecureFolder.RemoveLinks(root);
            Assert.False(Path.Exists(Path.Combine(root, "logs")));
            Assert.True(File.Exists(Path.Combine(target, "keep.txt")));
        }
        finally
        {
            TestFolders.Delete(root);
            TestFolders.Delete(target);
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
            TestFolders.Delete(root);
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
            TestFolders.Delete(trusted);
        }
    }

    private const string ProtectedRuntime = @"C:\ProgramData\PCOptimizer\runtime";

    [Theory]
    [InlineData(true, null, false, false, RuntimeStart.Restart)]
    [InlineData(true, @"C:\Users\me\AppData\Local\Temp", false, true, RuntimeStart.Restart)]
    [InlineData(true, ProtectedRuntime, false, true, RuntimeStart.Continue)]
    [InlineData(true, ProtectedRuntime + @"\", true, true, RuntimeStart.Continue)]
    [InlineData(false, null, false, true, RuntimeStart.Continue)]
    // The restart marker alone never skips the check: set from outside (or after a restart that did not take), an
    // elevated process refuses to start; an unelevated one gains nothing from the folder and continues.
    [InlineData(true, null, true, true, RuntimeStart.Refuse)]
    [InlineData(true, @"C:\Users\me\Planted", true, true, RuntimeStart.Refuse)]
    [InlineData(true, @"C:\Users\me\Planted", true, false, RuntimeStart.Continue)]
    public void SingleFileExeRestartsOnceWithTheProtectedExtractionFolder(bool bundle, string? current, bool restarted, bool elevated, RuntimeStart expected) =>
        Assert.Equal(expected, SingleFileRuntime.Decide(bundle, current, ProtectedRuntime, restarted, elevated));

    [Fact]
    public void AFailedRestartStopsAnElevatedProcess()
    {
        Assert.Equal(RuntimeStart.Refuse, SingleFileRuntime.AfterFailedRestart(elevated: true));
        Assert.Equal(RuntimeStart.Continue, SingleFileRuntime.AfterFailedRestart(elevated: false));
    }

    [Fact]
    public void OnlyExtractionFoldersInsideTheDataFolderAreTrusted()
    {
        var list = SingleFileRuntime.TrustedFolders(
            @"C:\ProgramData\PCOptimizer\runtime\PCOptimizer\abc123\;C:\Users\me\Downloads\;C:\ProgramData\PCOptimizer\runtime-evil\x;relative\path",
            @"C:\ProgramData\PCOptimizer\runtime");
        Assert.Equal([@"C:\ProgramData\PCOptimizer\runtime\PCOptimizer\abc123"], list);
    }
}
