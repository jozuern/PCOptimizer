using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using Optimizer.App.Services;
using Optimizer.App.ViewModels;
using Wpf.Ui.Controls;

namespace Optimizer.App.Views;

/// <summary>Main window: Fluent title bar, NavigationView rail, pages, details pane.</summary>
public partial class MainWindow : FluentWindow, IServiceProvider
{
    private readonly MainViewModel _vm;
    private readonly Dictionary<Type, FrameworkElement> _pages = [];

    public static readonly IReadOnlyDictionary<Page, Type> PageTypes = new Dictionary<Page, Type>
    {
        [Page.Overview] = typeof(OverviewPage),
        [Page.Tweaks] = typeof(TweaksPage),
        [Page.Advisor] = typeof(AdvisorPage),
        [Page.Network] = typeof(NetworkPage),
        [Page.Debloat] = typeof(DebloatPage),
        [Page.Cleanup] = typeof(CleanupPage),
        [Page.Startup] = typeof(StartupPage),
        [Page.Services] = typeof(ServicesPage),
        [Page.Apps] = typeof(AppsPage),
        [Page.Tools] = typeof(ToolsPage),
        [Page.Health] = typeof(HealthPage),
        [Page.Changes] = typeof(ChangesPage),
        [Page.Hardware] = typeof(HardwarePage),
        [Page.Settings] = typeof(SettingsPage),
    };

    private bool _syncing;

    public MainWindow(MainViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
        Nav.SetServiceProvider(this);
        Nav.Navigated += (_, e) =>
        {
            if (_syncing || e.Page is null) return;
            var page = PageTypes.FirstOrDefault(p => p.Value == e.Page.GetType()).Key;
            _syncing = true;
            vm.CurrentPage = page;
            _syncing = false;
        };
        Loaded += (_, _) => Nav.Navigate(typeof(OverviewPage));
        Loaded += (_, _) => SetTitleBarIcon(System.Windows.Media.VisualTreeHelper.GetDpi(this).DpiScaleX);
        DpiChanged += (_, e) => SetTitleBarIcon(e.NewDpi.DpiScaleX);
        vm.PropertyChanged += OnViewModelChanged;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape) vm.SelectedItem = null;
        };
    }

    /// <summary>Pages are created once and share the main view model.</summary>
    public object? GetService(Type serviceType)
    {
        if (!PageTypes.Values.Contains(serviceType)) return null;
        if (!_pages.TryGetValue(serviceType, out var page))
        {
            page = (FrameworkElement)Activator.CreateInstance(serviceType)!;
            page.DataContext = _vm;
            _pages[serviceType] = page;
        }
        return page;
    }

    private void OnViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SelectedItem)) RenderDetails();
        if (e.PropertyName == nameof(MainViewModel.CurrentPage) && !_syncing && PageTypes.TryGetValue(_vm.CurrentPage, out var type))
        {
            _syncing = true;
            Nav.Navigate(type);
            _syncing = false;
        }
    }

    /// <summary>
    /// Renders the selected item's explanation (re-run after theme changes so brushes match). The document is built in
    /// the background; the title shows at once, and a result for an item that is no longer selected is dropped.
    /// </summary>
    public void RenderDetails() => RenderDetailsAsync().Forget("inspector");

    private async Task RenderDetailsAsync()
    {
        if (_vm.SelectedItem is not { } item)
        {
            DetailsViewer.Document = null;
            return;
        }
        var pending = item.MarkdownAsync();
        if (!pending.IsCompleted) Show($"# {item.Title}");
        var markdown = await pending;
        if (ReferenceEquals(_vm.SelectedItem, item)) Show(markdown);
    }

    private void Show(string markdown)
    {
        Brush B(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;
        DetailsViewer.Document = MarkdownFlow.Render(markdown,
            B("TextFillColorPrimaryBrush", Brushes.White),
            B("TextFillColorSecondaryBrush", Brushes.Gray),
            B("SubtleFillColorSecondaryBrush", Brushes.DimGray),
            B("DividerStrokeColorDefaultBrush", Brushes.DimGray));
    }

    /// <summary>
    /// Title bar icon: the hand-tuned bitmap for the current scaling (16, 24 or 32 px for a 16 px slot), never a
    /// scaled-down large icon, which would look blurry at small sizes.
    /// </summary>
    private void SetTitleBarIcon(double scale)
    {
        var size = scale >= 1.75 ? 32 : scale >= 1.25 ? 24 : 16;
        var image = new System.Windows.Media.Imaging.BitmapImage(new Uri($"pack://application:,,,/Assets/icon-{size}.png"));
        TitleIcon.Source = image;
    }
}
