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

    public void Dispose() => _registry.Dispose();

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

    [Fact]
    public void AnEntryWhoseExeIsGoneNamesNoInstalledExe()
    {
        AppInstall.WriteEntry(_registry, @"C:\nowhere\Tunero.exe", "1.3.0", 3);
        Assert.Null(AppInstall.InstalledExe(_registry));
    }
}
