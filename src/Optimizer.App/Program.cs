using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Runtime.InteropServices;
using Optimizer.Core.Backup;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;

namespace Optimizer.App;

/// <summary>
/// Entry point. Runs before WPF loads anything: the data folder is secured first (the log and settings live there),
/// native libraries are tied to System32 and the protected folder, and a single-file exe moves its extracted
/// libraries out of %TEMP% (see <see cref="SingleFileRuntime"/>).
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // Assembly.Location is empty only inside a single-file bundle.
#pragma warning disable IL3000
        var bundle = string.IsNullOrEmpty(typeof(Program).Assembly.Location);
#pragma warning restore IL3000
        var runtimeBase = SingleFileRuntime.ExtractBase(DataPaths.Root);
        if (bundle)
        {
            NativeLibraryGuard.Install(SingleFileRuntime.TrustedFolders(AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES") as string, runtimeBase));
        }

        // Files the app creates must be owned by Administrators, or the data folder would distrust them (UAC off,
        // built-in Administrator, Administrator protection).
        if (DataPaths.ProcessIsElevated) DefaultOwner.SetAdministrators();
        if (DataPaths.ProcessIsElevated && !SecureFolder.PrepareRoot(DataPaths.Root))
        {
            ShowError(string.Format(Text("Startup_DataFolderUnsafe"), DataPaths.Root));
            return 1;
        }
        Log.EnableFile();

        var start = SingleFileRuntime.Decide(bundle, Environment.GetEnvironmentVariable(SingleFileRuntime.ExtractVariable), runtimeBase,
            Environment.GetEnvironmentVariable(SingleFileRuntime.RestartedVariable) == "1", DataPaths.ProcessIsElevated);
        if (start == RuntimeStart.Restart)
        {
            if (RunWithProtectedRuntime(args, runtimeBase) is { } exitCode) return exitCode;
            start = SingleFileRuntime.AfterFailedRestart(DataPaths.ProcessIsElevated);
        }
        if (start == RuntimeStart.Refuse)
        {
            Log.Error("app", "the native runtime folder is not the protected one; refusing to start elevated");
            ShowError(string.Format(Text("Startup_RuntimeFolderUnsafe"), SingleFileRuntime.ExtractVariable, SingleFileRuntime.RestartedVariable));
            return 1;
        }

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }

    /// <summary>
    /// Starts this exe again with the extraction folder inside the data folder and waits for it (developer switches
    /// such as --report wait for the exit code). Null when that is not possible.
    /// </summary>
    private static int? RunWithProtectedRuntime(string[] args, string runtimeBase)
    {
        if (Environment.ProcessPath is not { } exe) return null;
        try
        {
            Directory.CreateDirectory(runtimeBase);
            // Hold the exe without write or delete sharing while the new process starts, so it cannot be swapped.
            using var hold = new FileStream(exe, FileMode.Open, FileAccess.Read, FileShare.Read);
            var start = new ProcessStartInfo(exe) { UseShellExecute = false };
            foreach (var a in args) start.ArgumentList.Add(a);
            start.Environment[SingleFileRuntime.ExtractVariable] = runtimeBase;
            start.Environment[SingleFileRuntime.RestartedVariable] = "1";
            using var child = Process.Start(start);
            if (child is null) return null;
            hold.Dispose();
            child.WaitForExit();
            return child.ExitCode;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            Log.Error("app", "restart with the protected runtime folder failed", ex);
            return null;
        }
    }

    private static string Text(string key)
    {
        var resources = new ResourceManager("Optimizer.App.Resources.Strings", typeof(Program).Assembly);
        var german = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
        return resources.GetString(key, new System.Globalization.CultureInfo(german ? "de" : "en")) ?? key;
    }

    private static void ShowError(string text) => MessageBoxW(IntPtr.Zero, text, "PCOptimizer", 0x10 /* MB_ICONERROR */);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int MessageBoxW(IntPtr owner, string text, string caption, uint type);
}
