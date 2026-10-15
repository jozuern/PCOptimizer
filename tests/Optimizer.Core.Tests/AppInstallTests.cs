using Microsoft.Win32;
using Optimizer.Core.Actions;
using Optimizer.Core.Platform;
using Optimizer.Core.Startup;

namespace Optimizer.Core.Tests;

/// <summary>
/// Installing the downloaded exe into Program Files: when it is offered, the Installed apps entry, the install steps and
/// their undo. Registry in the sandbox, files in a temp folder, the signature check faked.
/// </summary>
public sealed class AppInstallTests : IDisposable
{
    private const string Subject = "CN=Open Source Developer Joshua Zürn, O=Open Source Developer, L=Neudenau, S=Baden-Württemberg, C=DE";

    private readonly SandboxRegistry _registry = new();

    public void Dispose()
    {
        _registry.Dispose();
        Directory.Delete(_temp, recursive: true);
    }

    [Theory]
    [InlineData(true, true, true, false, false, false, true, true)]
    [InlineData(false, true, true, false, false, false, true, false)]
    [InlineData(true, false, true, false, false, false, true, false)]
    [InlineData(true, true, false, false, false, false, true, false)]
    [InlineData(true, true, true, true, false, false, true, false)]
    [InlineData(true, true, true, false, true, false, true, false)]
    [InlineData(true, true, true, false, false, true, true, false)]
    [InlineData(true, true, true, false, false, false, false, false)]
    public void ShouldOfferOnlyADownloadedSignedCopy(bool published, bool elevated, bool plainStart, bool packageCopy, bool declined,
        bool installedHere, bool trusted, bool expected) =>
        Assert.Equal(expected, AppInstall.ShouldOffer(published, elevated, plainStart, packageCopy, declined, installedHere, trusted));

    [Fact]
    public void IsTrustedNeedsAValidSignatureAndTheExactPublisher()
    {
        var signed = new SignatureInfo(SignatureStatus.Signed, "Open Source Developer Joshua Zürn", false);
        Assert.True(AppInstall.IsTrusted(signed, Subject));
        Assert.False(AppInstall.IsTrusted(signed, "CN=Open Source Developer Joshua Zürn, O=Open Source Developer, L=Berlin, C=DE"));
        Assert.False(AppInstall.IsTrusted(signed, null));
        Assert.False(AppInstall.IsTrusted(signed with { Status = SignatureStatus.Invalid }, Subject));
        Assert.False(AppInstall.IsTrusted(signed with { Status = SignatureStatus.Unsigned }, Subject));
        Assert.False(AppInstall.IsTrusted(signed with { Catalog = true }, Subject));
    }

    [Fact]
    public void TheEntryHasTheValuesWindowsShows()
    {
        var exe = @"C:\Program Files\Tunero\Tunero.exe";
        var values = AppInstall.EntryValues(exe, "1.3.0", 67_500);
        Assert.Equal("Tunero", values["DisplayName"]);
        Assert.Equal("1.3.0", values["DisplayVersion"]);
        Assert.Equal("Joshua Zürn", values["Publisher"]);
        Assert.Equal(exe, values["DisplayIcon"]);
        Assert.Equal(@"C:\Program Files\Tunero", values["InstallLocation"]);
        Assert.Equal($"\"{exe}\" --uninstall", values["UninstallString"]);
        Assert.Equal("https://tunero.dev", values["URLInfoAbout"]);
        Assert.Equal(67_500, values["EstimatedSize"]);
        Assert.Equal(1, values["NoModify"]);
        Assert.Equal(1, values["NoRepair"]);
    }

    [Fact]
    public void WriteThenRemoveLeavesNoKey()
    {
        var exe = Path.Combine(Path.GetTempPath(), $"tunero-test-{Guid.NewGuid():N}.exe");
        File.WriteAllBytes(exe, [1, 2, 3]);
        try
        {
            AppInstall.WriteEntry(_registry, exe, "1.3.0", 3);
            using (var key = _registry.Open(Hive.Machine, AppInstall.KeyPath, writable: false))
            {
                Assert.NotNull(key);
                Assert.Equal(RegistryValueKind.DWord, key.GetValueKind("EstimatedSize"));
                Assert.Equal(RegistryValueKind.DWord, key.GetValueKind("NoModify"));
                Assert.Equal("1.3.0", key.GetValue("DisplayVersion"));
            }
            Assert.Equal(exe, AppInstall.InstalledExe(_registry));
            AppInstall.RemoveEntry(_registry);
            Assert.Null(_registry.Open(Hive.Machine, AppInstall.KeyPath, writable: false));
            Assert.Null(AppInstall.InstalledExe(_registry));
        }
        finally
        {
            File.Delete(exe);
        }
    }

    // Install steps: a temp folder stands in for Program Files\Tunero, the running exe is a file in another temp folder.

    private readonly string _temp = Directory.CreateTempSubdirectory("tunero-install-").FullName;

    private string Folder => Path.Combine(_temp, "Program Files", "Tunero");

    private string Running()
    {
        var downloads = Directory.CreateDirectory(Path.Combine(_temp, "Downloads")).FullName;
        var exe = Path.Combine(downloads, "Tunero.exe");
        File.WriteAllBytes(exe, [7, 7, 7, 7]);
        return exe;
    }

    private InstallRequest Request(string running, Func<string, bool>? verify = null, string version = "1.3.0", Func<string, bool>? adminOnly = null) =>
        new(running, Folder, "1.3.0", verify ?? (_ => true), _ => version, adminOnly ?? (_ => true));

    [Fact]
    public void InstallCopiesChecksAndRegisters()
    {
        var running = Running();
        var result = AppInstall.Install(Request(running), _registry);
        var installed = Path.Combine(Folder, "Tunero.exe");
        Assert.Null(result.FailedAt);
        Assert.Equal(installed, result.InstalledExe);
        Assert.Equal(File.ReadAllBytes(running), File.ReadAllBytes(installed));
        Assert.False(File.Exists(installed + ".new"));
        Assert.Equal(installed, AppInstall.InstalledExe(_registry));
    }

    [Fact]
    public void InstallStopsWhenTheCopyIsNotTrusted()
    {
        var result = AppInstall.Install(Request(Running(), verify: _ => false), _registry);
        Assert.Equal(InstallStep.Check, result.FailedAt);
        Assert.False(Directory.Exists(Folder));
        Assert.Null(_registry.Open(Hive.Machine, AppInstall.KeyPath, writable: false));
    }

    [Fact]
    public void InstallStopsOnAnotherVersion()
    {
        var result = AppInstall.Install(Request(Running(), version: "1.2.9"), _registry);
        Assert.Equal(InstallStep.Check, result.FailedAt);
        Assert.False(Directory.Exists(Folder));
    }

    [Fact]
    public void InstallRefusesAFolderWithOtherFiles()
    {
        Directory.CreateDirectory(Folder);
        var planted = Path.Combine(Folder, "version.dll");
        File.WriteAllBytes(planted, [1]);
        var result = AppInstall.Install(Request(Running()), _registry);
        Assert.Equal(InstallStep.Folder, result.FailedAt);
        Assert.True(File.Exists(planted));
        Assert.False(File.Exists(Path.Combine(Folder, "Tunero.exe")));
    }

    [Fact]
    public void InstallRefusesAWeakFolder()
    {
        var result = AppInstall.Install(Request(Running(), adminOnly: _ => false), _registry);
        Assert.Equal(InstallStep.Folder, result.FailedAt);
        Assert.False(Directory.Exists(Folder));
    }

    [Fact]
    public void AFailedStepUndoesTheEarlierOnes()
    {
        var result = AppInstall.Install(Request(Running()), _registry);
        AppInstall.Undo(result.InstalledExe!, _registry);
        Assert.False(Directory.Exists(Folder));
        Assert.Null(_registry.Open(Hive.Machine, AppInstall.KeyPath, writable: false));
    }

    [Fact]
    public void InstallReplacesAnOlderInstalledExe()
    {
        Directory.CreateDirectory(Folder);
        var installed = Path.Combine(Folder, "Tunero.exe");
        File.WriteAllBytes(installed, [1, 2]);
        var running = Running();
        var result = AppInstall.Install(Request(running), _registry);
        Assert.Null(result.FailedAt);
        Assert.Equal(File.ReadAllBytes(running), File.ReadAllBytes(installed));
    }

    [Fact]
    public void AFailedCheckKeepsTheOlderInstalledExe()
    {
        Directory.CreateDirectory(Folder);
        var installed = Path.Combine(Folder, "Tunero.exe");
        File.WriteAllBytes(installed, [1, 2]);
        var result = AppInstall.Install(Request(Running(), verify: _ => false), _registry);
        Assert.Equal(InstallStep.Check, result.FailedAt);
        Assert.Equal([1, 2], File.ReadAllBytes(installed));
        Assert.Equal(["Tunero.exe"], Directory.GetFiles(Folder).Select(Path.GetFileName));
    }

    [Fact]
    public void TheOldCopyIsDeletedWhenItIsTheSameFile()
    {
        var running = Running();
        var installed = AppInstall.Install(Request(running), _registry).InstalledExe!;
        Assert.True(AppInstall.DeleteOldCopy(running, installed));
        Assert.False(File.Exists(running));
    }

    /// <summary>Another file put at the old path after the install is not the downloaded copy: it stays.</summary>
    [Fact]
    public void TheOldCopyIsKeptWhenItChanged()
    {
        var running = Running();
        var installed = AppInstall.Install(Request(running), _registry).InstalledExe!;
        File.WriteAllBytes(running, [9, 9]);
        Assert.False(AppInstall.DeleteOldCopy(running, installed));
        Assert.True(File.Exists(running));
    }

    /// <summary>A copy started from the install folder itself names that folder as its old place: nothing is deleted.</summary>
    [Fact]
    public void TheInstalledExeIsNeverTheOldCopy()
    {
        var installed = AppInstall.Install(Request(Running()), _registry).InstalledExe!;
        Assert.False(AppInstall.DeleteOldCopy(installed.ToUpperInvariant(), installed));
        Assert.True(File.Exists(installed));
    }

    [Fact]
    public void AMissingOldCopyIsNotAnError()
    {
        var running = Running();
        var installed = AppInstall.Install(Request(running), _registry).InstalledExe!;
        File.Delete(running);
        Assert.True(AppInstall.DeleteOldCopy(running, installed));
    }

    [Fact]
    public void AnEntryWhoseExeIsGoneNamesNoInstalledExe()
    {
        AppInstall.WriteEntry(_registry, @"C:\nowhere\Tunero.exe", "1.3.0", 3);
        Assert.Null(AppInstall.InstalledExe(_registry));
    }
}
