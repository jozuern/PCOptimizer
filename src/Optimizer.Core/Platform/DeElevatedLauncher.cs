using System.Diagnostics;
using System.Runtime.InteropServices;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Platform;

/// <summary>
/// Starts user-facing processes (browser links, launchers) as the signed-in user, never elevated (plan v4 §4.5).
/// 1) Shell technique: ask the desktop's Explorer to ShellExecute (Raymond Chen). Works when Explorer runs as the
///    same user as this process.
/// 2) Fallback: a one-shot scheduled task for the session user with an interactive token (Administrator protection,
///    over-the-shoulder elevation), deleted after it started.
/// 3) If both fail, the caller shows the link for the user to open.
/// </summary>
public static class DeElevatedLauncher
{
    public enum Path { NotElevated, Shell, ScheduledTask, Failed }

    public static Path Open(string target, string? arguments, ElevationInfo? elevation)
    {
        if (elevation is { IsElevated: false })
        {
            Process.Start(new ProcessStartInfo(target, arguments ?? "") { UseShellExecute = true });
            return Path.NotElevated;
        }
        // Under Administrator protection or a separate admin account, Explorer belongs to another user: use the task.
        if (elevation is not { UserMismatch: true } && TryShell(target, arguments)) return Log(Path.Shell, target);
        if (elevation?.SessionUser is { } user && TryTask(target, arguments, user)) return Log(Path.ScheduledTask, target);
        return Log(Path.Failed, target);
    }

    private static Path Log(Path path, string target)
    {
        Logging.Log.Info("launcher", $"open {target}", new { path = path.ToString() });
        return path;
    }

    // ---------------- shell technique ----------------

    private static bool TryShell(string target, string? arguments)
    {
        try
        {
            var shellWindows = (IShellWindows)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"))!)!;
            object loc = 0; // CSIDL_DESKTOP
            object empty = Type.Missing;
            var dispatch = shellWindows.FindWindowSW(ref loc, ref empty, 8 /* SWC_DESKTOP */, out _, 1 /* SWFO_NEEDDISPATCH */);
            var sid = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837"); // SID_STopLevelBrowser
            var iid = typeof(IShellBrowser).GUID;
            ((IServiceProvider)dispatch).QueryService(ref sid, ref iid, out var browserObj);
            var browser = (IShellBrowser)browserObj;
            browser.QueryActiveShellView(out var view);
            var iidDispatch = new Guid("00020400-0000-0000-C000-000000000046");
            view.GetItemObject(0 /* SVGIO_BACKGROUND */, ref iidDispatch, out var folderView);
            dynamic app = ((dynamic)folderView).Application;
            app.ShellExecute(target, arguments ?? "", "", "open", 1);
            return true;
        }
        catch (Exception ex)
        {
            Logging.Log.Warn("launcher", $"shell technique failed: {ex.Message}");
            return false;
        }
    }

    // ---------------- scheduled-task fallback ----------------

    private static bool TryTask(string target, string? arguments, string user)
    {
        var name = $"PCOptimizer-open-{Guid.NewGuid():N}";
        dynamic? folder = null;
        try
        {
            dynamic service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")!)!;
            service.Connect();
            folder = service.GetFolder("\\");
            dynamic def = service.NewTask(0);
            def.Principal.UserId = user;
            def.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN: runs in the user's session with the user's token
            def.Principal.RunLevel = 0;  // TASK_RUNLEVEL_LUA: not elevated
            def.Settings.DisallowStartIfOnBatteries = false;
            def.Settings.ExecutionTimeLimit = "PT1M";
            dynamic action = def.Actions.Create(0); // TASK_ACTION_EXEC
            var isUrl = target.Contains("://", StringComparison.Ordinal);
            action.Path = isUrl ? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "explorer.exe") : target;
            action.Arguments = isUrl ? $"\"{target}\"" : arguments ?? "";
            dynamic task = folder.RegisterTaskDefinition(name, def, 6 /* CREATE_OR_UPDATE */, null, null, 3 /* INTERACTIVE_TOKEN */, null);
            task.Run(null);
            Thread.Sleep(1500);
            return true;
        }
        catch (Exception ex)
        {
            Logging.Log.Warn("launcher", $"task fallback failed: {ex.Message}");
            return false;
        }
        finally
        {
            try
            {
                folder?.DeleteTask(name, 0);
            }
            catch (Exception)
            {
                // the task may not exist if registration failed
            }
        }
    }

    // ---------------- COM interfaces (vtable order matters) ----------------

    [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsDual)]
    private interface IShellWindows
    {
        int Count { get; }
        [return: MarshalAs(UnmanagedType.IDispatch)] object Item(object index);
        [return: MarshalAs(UnmanagedType.IUnknown)] object _NewEnum();
        int Register([MarshalAs(UnmanagedType.IDispatch)] object pid, int hwnd, int swClass);
        int RegisterPending(int threadId, ref object loc, ref object locRoot, int swClass);
        void Revoke(int cookie);
        void OnNavigate(int cookie, ref object loc);
        void OnActivated(int cookie, [MarshalAs(UnmanagedType.VariantBool)] bool active);
        [return: MarshalAs(UnmanagedType.IDispatch)] object FindWindowSW(ref object loc, ref object locRoot, int swClass, out int hwnd, int swfwOptions);
    }

    [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IServiceProvider
    {
        void QueryService(ref Guid service, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object obj);
    }

    [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellBrowser
    {
        void GetWindow(out IntPtr hwnd);
        void ContextSensitiveHelp(int enterMode);
        void InsertMenusSB(IntPtr hmenuShared, IntPtr widths);
        void SetMenuSB(IntPtr hmenuShared, IntPtr holemenu, IntPtr hwndActiveObject);
        void RemoveMenusSB(IntPtr hmenuShared);
        void SetStatusTextSB(IntPtr text);
        void EnableModelessSB(int enable);
        void TranslateAcceleratorSB(IntPtr msg, short id);
        void BrowseObject(IntPtr pidl, uint flags);
        void GetViewStateStream(uint mode, out IntPtr stream);
        void GetControlWindow(uint id, out IntPtr hwnd);
        void SendControlMsg(uint id, uint msg, IntPtr wParam, IntPtr lParam, out IntPtr result);
        void QueryActiveShellView(out IShellView view);
        void OnViewWindowActive(IShellView view);
        void SetToolbarItems(IntPtr buttons, uint count, uint flags);
    }

    [ComImport, Guid("000214E3-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellView
    {
        void GetWindow(out IntPtr hwnd);
        void ContextSensitiveHelp(int enterMode);
        void TranslateAccelerator(IntPtr msg);
        void EnableModeless(int enable);
        void UIActivate(uint state);
        void Refresh();
        void CreateViewWindow(IShellView previous, IntPtr folderSettings, IShellBrowser browser, IntPtr rect, out IntPtr hwnd);
        void DestroyViewWindow();
        void GetCurrentInfo(IntPtr folderSettings);
        void AddPropertySheetPages(uint reserved, IntPtr callback, IntPtr lParam);
        void SaveViewState();
        void SelectItem(IntPtr pidl, uint flags);
        void GetItemObject(uint item, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out object obj);
    }
}
