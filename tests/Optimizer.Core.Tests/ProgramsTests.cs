using Optimizer.Core.Actions;
using Optimizer.Core.Apps;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Tests;

/// <summary>The uninstall list and which uninstallers may run elevated.</summary>
public class ProgramsTests
{
    private const string Uninstall = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    private static void App(SandboxRegistry r, Hive hive, string key, params (string Name, string Kind, string Value)[] values)
    {
        foreach (var (name, kind, value) in values) RegistryValue.Write(r, hive, $@"{Uninstall}\{key}", name, kind, value);
    }

    [Fact]
    public void ListsWhatSettingsShowsAndSkipsComponentsAndUpdates()
    {
        using var r = new SandboxRegistry();
        App(r, Hive.Machine, "7-Zip", ("DisplayName", "string", "7-Zip 24.08 (x64)"), ("Publisher", "string", "Igor Pavlov"), ("DisplayVersion", "string", "24.08"),
            ("UninstallString", "string", @"""C:\Program Files\7-Zip\Uninstall.exe"""), ("EstimatedSize", "dword", "5800"), ("InstallDate", "string", "20260901"));
        App(r, Hive.Machine, "{11111111-2222-3333-4444-555555555555}", ("DisplayName", "string", "Contoso Tool"), ("WindowsInstaller", "dword", "1"),
            ("UninstallString", "string", "MsiExec.exe /I{11111111-2222-3333-4444-555555555555}"));
        App(r, Hive.Machine, "Component", ("DisplayName", "string", "Hidden part"), ("SystemComponent", "dword", "1"), ("UninstallString", "string", "x.exe"));
        App(r, Hive.Machine, "KB123", ("DisplayName", "string", "Update for Contoso"), ("ParentKeyName", "string", "Contoso"));
        App(r, Hive.User, "Discord", ("DisplayName", "string", "Discord"), ("UninstallString", "string", @"""C:\Users\me\AppData\Local\Discord\Update.exe"" --uninstall"));

        var list = Programs.Read(r);
        Assert.Equal(["7-Zip 24.08 (x64)", "Contoso Tool", "Discord"], list.Select(p => p.Name));
        var zip = list[0];
        Assert.Equal(5800, zip.SizeKb);
        Assert.Equal(new DateTime(2026, 9, 1), zip.InstallDate);
        Assert.False(zip.PerUser);
        Assert.Equal("{11111111-2222-3333-4444-555555555555}", list[1].ProductCode);
        Assert.True(list[2].PerUser);
    }

    [Fact]
    public void OnlyUninstallersInAdminOnlyFoldersRunElevated()
    {
        var msi = new DesktopProgram("Contoso", null, null, null, null, "MsiExec.exe /I{11111111-2222-3333-4444-555555555555}", "{11111111-2222-3333-4444-555555555555}", false, "k");
        var cmd = Programs.Command(msi, _ => false)!;
        Assert.Equal(UninstallMode.WindowsInstaller, cmd.Mode);
        Assert.Equal(Path.Combine(Environment.SystemDirectory, "msiexec.exe"), cmd.File, ignoreCase: true);
        Assert.Equal("/x {11111111-2222-3333-4444-555555555555}", cmd.Arguments); // never /I from the registry

        // msiexec named in UninstallString without the WindowsInstaller flag: still System32's msiexec with /x.
        var msiString = msi with { ProductCode = null };
        Assert.Equal("/x {11111111-2222-3333-4444-555555555555}", Programs.Command(msiString, _ => false)!.Arguments);
        Assert.Null(Programs.Command(msi with { ProductCode = null, UninstallString = "MsiExec.exe /I{not-a-guid}" }, _ => true));

        var machine = new DesktopProgram("7-Zip", null, null, null, null, @"""C:\Program Files\7-Zip\Uninstall.exe"" /S", null, false, "k");
        var trusted = Programs.Command(machine, _ => true)!;
        Assert.Equal(UninstallMode.Elevated, trusted.Mode);
        Assert.Equal(@"C:\Program Files\7-Zip\Uninstall.exe", trusted.File);
        Assert.Equal("/S", trusted.Arguments);
        Assert.Equal(UninstallMode.AsUser, Programs.Command(machine, _ => false)!.Mode);

        // A per-user program never runs elevated, even from a protected folder.
        Assert.Equal(UninstallMode.AsUser, Programs.Command(machine with { PerUser = true }, _ => true)!.Mode);
        Assert.Null(Programs.Command(machine with { UninstallString = "" }, _ => true));
    }

    [Theory]
    [InlineData(@"""C:\A B\u.exe"" /x /y", @"C:\A B\u.exe", "/x /y")]
    [InlineData(@"C:\Tools\u.exe --remove", @"C:\Tools\u.exe", "--remove")]
    [InlineData(@"C:\Tools\u.exe", @"C:\Tools\u.exe", "")]
    // Unquoted path with spaces, and a path written without .exe.
    [InlineData(@"C:\Program Files\App X\unins000.exe /SILENT", @"C:\Program Files\App X\unins000.exe", "/SILENT")]
    [InlineData(@"C:\Program Files\App X\unins000 /SILENT", @"C:\Program Files\App X\unins000.exe", "/SILENT")]
    public void ArgumentsFollowThePath(string command, string exe, string expected)
    {
        var program = new DesktopProgram("App", null, null, null, null, command, null, false, "k");
        var cmd = Programs.Command(program, _ => true, f => f.Equals(@"C:\Program Files\App X\unins000.exe", StringComparison.OrdinalIgnoreCase))!;
        Assert.Equal(exe, cmd.File);
        Assert.Equal(expected, cmd.Arguments);
    }

    /// <summary>A planted HKCU entry must not make an elevated msiexec remove a program installed for the whole PC.</summary>
    [Fact]
    public void PerUserWindowsInstallerEntriesRunAsTheUser()
    {
        var msi = new DesktopProgram("Contoso", null, null, null, null, "MsiExec.exe /X{11111111-2222-3333-4444-555555555555}", "{11111111-2222-3333-4444-555555555555}", true, "k");
        Assert.Equal(UninstallMode.AsUser, Programs.Command(msi, _ => true)!.Mode);
        Assert.Equal(UninstallMode.AsUser, Programs.Command(msi with { ProductCode = null }, _ => true)!.Mode);
    }

    /// <summary>rundll32 lines run rundll32 with the DLL as argument; elevated only when the DLL is admin-only too.</summary>
    [Fact]
    public void RundllUninstallersRunRundllWithTheirArguments()
    {
        var line = @"RunDll32 C:\PROGRA~2\COMMON~1\INSTAL~1\Ctor.dll,LaunchSetup ""C:\Program Files (x86)\App\setup.exe"" -removeonly";
        var program = new DesktopProgram("Old app", null, null, null, null, line, null, false, "k");
        var rundll = Path.Combine(Environment.SystemDirectory, "RunDll32.exe");
        var cmd = Programs.Command(program, _ => true)!;
        Assert.Equal(rundll, cmd.File, ignoreCase: true);
        Assert.Equal(@"C:\PROGRA~2\COMMON~1\INSTAL~1\Ctor.dll,LaunchSetup ""C:\Program Files (x86)\App\setup.exe"" -removeonly", cmd.Arguments);
        Assert.Equal(UninstallMode.Elevated, cmd.Mode);
        // The DLL in a user-writable folder: not elevated, although rundll32 itself is in System32.
        Assert.Equal(UninstallMode.AsUser, Programs.Command(program, f => f.Equals(rundll, StringComparison.OrdinalIgnoreCase))!.Mode);
    }

    [Fact]
    public void System32IsTrustedAndTempIsNot()
    {
        Assert.True(TrustedPath.IsAdminOnlyWritable(Path.Combine(Environment.SystemDirectory, "notepad.exe")));
        var folder = TestFolders.Create("trusted");
        try
        {
            var file = Path.Combine(folder, "uninstall.exe");
            File.WriteAllText(file, "x");
            Assert.False(TrustedPath.IsAdminOnlyWritable(file));
            Assert.False(TrustedPath.IsAdminOnlyWritable(Path.Combine(folder, "missing.exe")));
        }
        finally
        {
            TestFolders.Delete(folder);
        }
    }
}
