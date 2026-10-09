using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Optimizer.App.Services;
using Optimizer.App.ViewModels;
using Optimizer.App.Views;
using Optimizer.Core.Catalog;
using Optimizer.Core.Docs;
using Optimizer.Core.Findings;
using Optimizer.Core.Hardware;
using Optimizer.Core.Logging;
using Optimizer.Core.Platform;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Optimizer.App;

public partial class App : Application
{
    private AppSettings _settings = new();
    private bool _watching;

    // The signed-in user, whose Windows app mode counts. With a separate admin account (or Administrator protection)
    // this process's HKCU is the admin's, and WPF-UI's theme watcher would follow that account instead.
    private string? _sessionSid;
    private bool _userMismatch;
    private bool _preferenceHooked;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, ex) =>
        {
            // Log the root cause too: XAML and reflection errors wrap the real exception.
            Log.Error("ui", "unhandled exception", ex.Exception);
            if (ex.Exception.GetBaseException() is { } root && !ReferenceEquals(root, ex.Exception)) Log.Error("ui", "root cause", root);
            ex.Handled = true;
        };

        var args = new CliArgs(e.Args);
        _settings = AppSettings.Load();
        Loc.Instance.SetLanguage(args.Value("--lang") ?? _settings.Language ?? Loc.Instance.Language);
        Log.Info("app", "start", new { version = typeof(App).Assembly.GetName().Version?.ToString(), args = e.Args });

        // OS gate (plan v4 §2): block below 26100 and non-x64 before scanning anything.
        var os = BuildInfo.Read();
        var gate = OsGate.Evaluate(os.Build, os.NativeArchitecture);
        if (gate is OsGateResult.BlockedTooOld or OsGateResult.BlockedArchitecture)
        {
            var text = gate == OsGateResult.BlockedTooOld ? Loc.Instance.Format("Gate_TooOld", os.BuildString) : Loc.Instance["Gate_Arch"];
            Log.Warn("app", "blocked by OS gate", new { os.Build, os.NativeArchitecture });
            if (args.Value("--report") is null) System.Windows.MessageBox.Show(text, Loc.Instance["Gate_Title"], System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            Shutdown(2);
            return;
        }

        var services = new AppServices();
        _sessionSid = services.UserSid;
        _userMismatch = services.Elevation.UserMismatch;

        if (args.Value("--report") is { } reportPath)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            Shutdown(await WriteReportAsync(reportPath, Loc.Instance.Language, services));
            return;
        }

        if (args.Value("--expert") is "on") _settings.ExpertMode = true;
        // Developer aid for screenshots: start with this profile for the session (never saved).
        var vm = new MainViewModel(_settings, services, new Dialogs()) { ProfileOverride = args.Value("--profile") };
        var window = new MainWindow(vm);
        MainWindow = window;
        ApplyTheme(args.Value("--theme") ?? _settings.Theme, save: false);
        window.Show();
        await vm.ScanAsync();
        // Opt-in release check (off by default); screenshots stay offline and reproducible.
        if (args.Value("--screenshot") is null) _ = vm.CheckForUpdatesAsync(atStart: true);

        if (args.Value("--screenshot") is { } shot)
        {
            if (Enum.TryParse<Page>(args.Value("--page"), true, out var page)) vm.CurrentPage = page;
            if (args.Value("--select") is { } id)
                vm.SelectedItem = vm.Findings.Concat(vm.AdvisorItems).Append(vm.GameAccess).FirstOrDefault(i => i?.Finding.Id == id);
            if (args.Value("--preview-drift") is "on") vm.PreviewDrift();
            if (args.Value("--category") is { } cat) vm.SelectedCategory = vm.Categories.FirstOrDefault(c => c.Key == cat) ?? vm.SelectedCategory;
            await Task.Delay(1500); // pages that load their own data (startup, services, apps)
            if (args.Value("--select") is { } tid && vm.Tweaks.FirstOrDefault(t => t.Tweak.Id == tid) is { } tweak) vm.SelectedItem = tweak;
            await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            // Developer aid: show the bottom of long pages (Settings > About).
            if (args.Value("--scroll") is "end")
            {
                foreach (var sv in Descendants<System.Windows.Controls.ScrollViewer>(window)) sv.ScrollToEnd();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            }
            await Task.Delay(400);
            SaveScreenshot(window, shot);

            // Developer aid: render the confirmation dialog of a tweak without applying anything.
            if (args.Value("--confirm") is { } confirmId && services.Catalog.Get(confirmId) is { } ct && args.Value("--confirm-shot") is { } confirmShot)
            {
                var cpage = DocStore.Get(ct.DocId, Loc.Instance.Language);
                var dlg = new ConfirmWindow(new ConfirmRequest(cpage?.Title ?? ct.Id, cpage?.Section(DocHeadings.Summary(Loc.Instance.Language))?.Body ?? "",
                    services.Engine.Preview(ct), [Labels.Current.Get(Loc.Instance.Language, $"risk.{ct.EffectiveRisk}")],
                    ct.IsBootCritical ? [Labels.Current.Get(Loc.Instance.Language, "undo.bootCritical")] : [], null, false)) { Owner = window };
                dlg.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await Task.Delay(400);
                SaveScreenshot(dlg, confirmShot);
                dlg.Close();
            }

            // Developer aid: render the Licenses window (Settings > About).
            if (args.Value("--licenses-shot") is { } licensesShot)
            {
                var lic = new LicensesWindow(_ => { }) { Owner = window };
                lic.Show();
                await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                await Task.Delay(400);
                SaveScreenshot(lic, licensesShot);
                lic.Close();
            }
            Shutdown(0);
        }
    }

    /// <summary>"System", "Dark" or "Light" as chosen in Settings.</summary>
    public string ThemeSetting => _settings.Theme;

    /// <summary>
    /// Applies the Windows 11 Fluent theme (WPF-UI) with Mica and the Windows accent color. "System" follows the
    /// Windows app mode, also when it changes while the app runs.
    /// </summary>
    public void ApplyTheme(string theme, bool save = true)
    {
        // Case-insensitive so "--theme light" works like "--lang de"; the saved value keeps the canonical spelling.
        var setting = new[] { "Light", "Dark" }.FirstOrDefault(t => t.Equals(theme, StringComparison.OrdinalIgnoreCase)) ?? "System";
        var dark = setting == "Dark" || (setting == "System" && !SystemUsesLightTheme(_sessionSid));
        ApplicationThemeManager.Apply(dark ? ApplicationTheme.Dark : ApplicationTheme.Light, WindowBackdropType.Mica, true);
        ApplicationAccentColorManager.ApplySystemAccent();
        // A gray Windows accent would leave switches, buttons and progress colorless: use the Fluent default blue then.
        var accent = ApplicationAccentColorManager.GetColorizationColor();
        if (Saturation(accent) < 0.2)
            ApplicationAccentColorManager.Apply(Color.FromRgb(0x00, 0x78, 0xD4), dark ? ApplicationTheme.Dark : ApplicationTheme.Light);
        if (MainWindow is Window w)
        {
            // The watcher only accepts loaded windows; at startup the window is not shown yet.
            void Watch()
            {
                if (_userMismatch)
                {
                    FollowSessionUserTheme();
                    return;
                }
                if (setting == "System") SystemThemeWatcher.Watch(w, WindowBackdropType.Mica, true);
                else if (_watching) SystemThemeWatcher.UnWatch(w);
                _watching = setting == "System";
            }
            if (w.IsLoaded) Watch();
            else w.Loaded += (_, _) => Watch();
        }
        _settings.Theme = setting;
        if (save) _settings.Save();
        // Status colors and the explanation document read theme brushes when they are built.
        if (MainWindow is MainWindow main)
        {
            (main.DataContext as MainViewModel)?.Rebuild();
            main.RenderDetails();
        }
    }

    private static double Saturation(Color c)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        return max == min ? 0 : l > 0.5 ? (max - min) / (2 - max - min) : (max - min) / (max + min);
    }

    /// <summary>
    /// Separate admin account: theme changes are broadcast to every window in the session, so re-read the signed-in
    /// user's app mode on each change instead of WPF-UI's watcher (which reads this process's own HKCU).
    /// </summary>
    private void FollowSessionUserTheme()
    {
        if (_preferenceHooked) return;
        _preferenceHooked = true;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category is not (Microsoft.Win32.UserPreferenceCategory.General or Microsoft.Win32.UserPreferenceCategory.Color)) return;
            Dispatcher.BeginInvoke(() =>
            {
                if (_settings.Theme == "System") ApplyTheme("System", save: false);
            });
        };
    }

    /// <summary>Windows "app mode" (Settings -> Personalization -> Colors) of the signed-in user. Read-only.</summary>
    private static bool SystemUsesLightTheme(string? sessionSid)
    {
        const string path = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        try
        {
            using var key = sessionSid is not null
                ? Microsoft.Win32.Registry.Users.OpenSubKey($@"{sessionSid}\{path}") ?? Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path)
                : Microsoft.Win32.Registry.CurrentUser.OpenSubKey(path);
            return key?.GetValue("AppsUseLightTheme") is int v && v == 1;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Headless scan -> Markdown report (developer aid for checking detection on real hardware).</summary>
    private static async Task<int> WriteReportAsync(string path, string lang, AppServices services)
    {
        try
        {
            var catalog = CatalogData.Current;
            var profile = await new HardwareScanner(catalog).ScanAsync();
            var findings = new FindingEngine(catalog, services.Context.Registry).Evaluate(profile);
            var sb = new StringBuilder();
            sb.AppendLine($"# PCOptimizer scan report ({DateTime.Now:yyyy-MM-dd HH:mm})").AppendLine();
            sb.AppendLine($"Readiness score: **{ReadinessScore.Compute(findings)}/100**, Windows {profile.Os.DisplayVersion} build {profile.Os.BuildString}, elevated: {profile.Elevation?.IsElevated}").AppendLine();
            foreach (var f in findings)
            {
                var item = new FindingItemViewModel(f, lang);
                sb.AppendLine($"## [{f.Status}] impact {f.Impact?.ToString() ?? "n/a"}/5: {item.Title}").AppendLine();
                sb.AppendLine(DocStore.Get(f.Id, lang) is { } page ? DocStore.RenderFinding(page, f, Labels.Current).Replace("\n## ", "\n### ") : "(no page)");
            }
            sb.AppendLine("## System info").AppendLine();
            foreach (var s in HardwareReport.Build(profile, Loc.Instance))
            {
                sb.AppendLine($"### {s.Title}");
                foreach (var i in s.Items) sb.AppendLine($"- {i.Label}: {i.Value.Replace("\n", "; ")}");
                sb.AppendLine();
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
            return 0;
        }
        catch (Exception ex)
        {
            Log.Error("app", "report failed", ex);
            return 1;
        }
    }

    private static IEnumerable<T> Descendants<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) yield return match;
            foreach (var deeper in Descendants<T>(child)) yield return deeper;
        }
    }

    private static void SaveScreenshot(Window window, string path)
    {
        if (window.Content is not FrameworkElement root) return;
        var dpi = VisualTreeHelper.GetDpi(window);
        var w = (int)(root.ActualWidth * dpi.DpiScaleX);
        var h = (int)(root.ActualHeight * dpi.DpiScaleY);
        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            var background = window.TryFindResource("ApplicationBackgroundBrush") as Brush ?? Brushes.Black;
            dc.DrawRectangle(background, null, new Rect(0, 0, root.ActualWidth, root.ActualHeight));
            dc.DrawRectangle(new VisualBrush(root), null, new Rect(0, 0, root.ActualWidth, root.ActualHeight));
        }
        var bitmap = new RenderTargetBitmap(w, h, dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(path);
        encoder.Save(stream);
    }

    private sealed class CliArgs(string[] args)
    {
        public string? Value(string name)
        {
            var i = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }
    }
}
