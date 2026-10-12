using System.ComponentModel;
using System.Text.RegularExpressions;
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
        // The title bar lays out its parts after the window: center the search box once that is done.
        Loaded += (_, _) => Dispatcher.BeginInvoke(CenterSearchBox, System.Windows.Threading.DispatcherPriority.Loaded);
        Loaded += (_, _) => SetTitleBarIcon(System.Windows.Media.VisualTreeHelper.GetDpi(this).DpiScaleX);
        DpiChanged += (_, e) => SetTitleBarIcon(e.NewDpi.DpiScaleX);
        vm.PropertyChanged += OnViewModelChanged;
        SizeChanged += (_, _) =>
        {
            FitPanes();
            CenterSearchBox();
        };
        // Closing ends the process, and with it the threads of a change in progress: a batch, an undo, a winget run or
        // an app removal would stop halfway. The window stays open until the change has finished.
        Closing += (_, e) =>
        {
            if (!ChangeGate.Instance.Busy) return;
            e.Cancel = true;
            vm.ShowResult(Loc.Instance["Close_WhileBusy"], InfoBarSeverity.Warning);
        };
        Nav.PaneOpened += (_, _) => ShowGroupHeaders();
        Nav.PaneClosed += (_, _) => ShowGroupHeaders();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Escape && !vm.SearchResultsOpen) vm.SelectedItem = null;
            // Ctrl+F searches the whole app; F5 reads the page or the PC again.
            else if (e.Key == System.Windows.Input.Key.F && System.Windows.Input.Keyboard.Modifiers == System.Windows.Input.ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.F5)
            {
                vm.RefreshCommand.Execute(null);
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// The title bar puts its center content right after the title: the box is moved to the middle of the window, as in
    /// Windows 11 apps (never closer than 24 px to the title).
    /// </summary>
    private void SearchBox_SizeChanged(object sender, SizeChangedEventArgs e) => CenterSearchBox();

    private void CenterSearchBox()
    {
        if (!SearchBox.IsLoaded || ActualWidth <= 0) return;
        var left = SearchBox.TranslatePoint(new Point(0, 0), this).X - SearchBox.Margin.Left;
        var wanted = (ActualWidth - SearchBox.ActualWidth) / 2 - left;
        SearchBox.Margin = new Thickness(Math.Max(24, wanted), 6, 0, 6);
    }

    /// <summary>A new result message is announced by screen readers (the text is a live region).</summary>
    private void AnnounceResult()
    {
        if (System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(ResultMessage) is { } peer)
            peer.RaiseAutomationEvent(System.Windows.Automation.Peers.AutomationEvents.LiveRegionChanged);
    }

    /// <summary>In the search box: Down moves into the results, Enter opens the first one, Escape clears the search.</summary>
    private void SearchBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        switch (e.Key)
        {
            case System.Windows.Input.Key.Down when _vm.SearchResults.Count > 0:
                _vm.SearchResultsOpen = true;
                SearchList.SelectedIndex = 0;
                Dispatcher.BeginInvoke(() => (SearchList.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus(),
                    System.Windows.Threading.DispatcherPriority.Input);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Enter when _vm.SearchResults.Count > 0:
                OpenResult(_vm.SearchResults[0]);
                e.Handled = true;
                break;
            case System.Windows.Input.Key.Escape:
                _vm.GlobalSearch = "";
                _vm.SearchResultsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void SearchList_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter && SearchList.SelectedItem is SearchResultItem item)
        {
            OpenResult(item);
            e.Handled = true;
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            _vm.SearchResultsOpen = false;
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void SearchList_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (SearchList.SelectedItem is SearchResultItem item) OpenResult(item);
    }

    private void OpenResult(SearchResultItem item)
    {
        _vm.OpenSearchResultCommand.Execute(item);
        SearchList.SelectedItem = null;
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
        if (e.PropertyName == nameof(MainViewModel.ResultText) && _vm.ResultOpen) Dispatcher.BeginInvoke(AnnounceResult);
        if (e.PropertyName == nameof(MainViewModel.SelectedItem))
        {
            RenderDetails();
            FitPanes();
        }
        if (e.PropertyName == nameof(MainViewModel.CurrentPage) && !_syncing && PageTypes.TryGetValue(_vm.CurrentPage, out var type))
        {
            _syncing = true;
            Nav.Navigate(type);
            _syncing = false;
        }
    }

    /// <summary>Window width below which the open pane and the details pane together leave the page too narrow.</summary>
    private const double NarrowWidth = 1320;

    private bool _paneCollapsedForDetails;

    /// <summary>
    /// In a narrow window the open details pane takes the room of the navigation pane, which folds to the rail (as
    /// NavigationView does in WinUI); it opens again when the details close or the window gets wide enough.
    /// </summary>
    private void FitPanes()
    {
        FitHeight();
        // The details pane never leaves the page less room than it needs for a readable list.
        DetailsPane.Width = Math.Clamp(DetailsPane.Width, DetailsMin, DetailsMax);
        var crowded = _vm.SelectedItem is not null && ActualWidth > 0 && ActualWidth < NarrowWidth;
        if (crowded && Nav.IsPaneOpen)
        {
            Nav.IsPaneOpen = false;
            _paneCollapsedForDetails = true;
        }
        else if (!crowded && _paneCollapsedForDetails)
        {
            Nav.IsPaneOpen = true;
            _paneCollapsedForDetails = false;
        }
    }

    /// <summary>Below this height the open pane cannot show the group headings and every page; it drops the headings.</summary>
    private const double HeadingsHeight = 900;

    /// <summary>Below this height even the pane without headings would cut off pages: it folds to the rail, as in WinUI.</summary>
    private const double RailHeight = 790;

    private bool _paneCollapsedForHeight;

    /// <summary>
    /// The pane does not scroll: on a short window (a 768 px screen) its last pages would slide under the footer. First the
    /// group headings go, then the pane folds to the rail; both come back when the window is tall enough again.
    /// </summary>
    private void FitHeight()
    {
        if (ActualHeight <= 0) return;
        var tooShort = ActualHeight < RailHeight;
        if (tooShort && Nav.IsPaneOpen)
        {
            Nav.IsPaneOpen = false;
            _paneCollapsedForHeight = true;
        }
        else if (!tooShort && _paneCollapsedForHeight)
        {
            Nav.IsPaneOpen = true;
            _paneCollapsedForHeight = false;
        }
        ShowGroupHeaders();
    }

    private void ShowGroupHeaders()
    {
        var show = Nav.IsPaneOpen && ActualHeight >= HeadingsHeight ? Visibility.Visible : Visibility.Collapsed;
        HeaderOptimize.Visibility = HeaderCleanUp.Visibility = HeaderMaintain.Visibility = show;
    }

    private const double DetailsMin = 340;

    /// <summary>Widest details pane: the page keeps at least 520 px (rail and a list of readable width).</summary>
    private double DetailsMax => Math.Max(DetailsMin, ActualWidth - 520);

    /// <summary>Dragging the left edge of the details pane: to the left makes it wider.</summary>
    private void DetailsResize_DragDelta(object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e) =>
        DetailsPane.Width = Math.Clamp(DetailsPane.Width - e.HorizontalChange, DetailsMin, DetailsMax);

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
        if (!pending.IsCompleted) DetailsViewer.Document = null;
        var markdown = await pending;
        if (ReferenceEquals(_vm.SelectedItem, item)) Show(markdown);
    }

    private void Show(string markdown)
    {
        // The pane header shows the title (the row's, with its subject); the page's own heading would repeat it.
        markdown = LeadingTitle().Replace(markdown, "", 1);
        Brush B(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;
        DetailsViewer.Document = MarkdownFlow.Render(markdown,
            B("TextFillColorPrimaryBrush", Brushes.White),
            B("TextFillColorSecondaryBrush", Brushes.Gray),
            B("SubtleFillColorSecondaryBrush", Brushes.DimGray),
            B("DividerStrokeColorDefaultBrush", Brushes.DimGray));
    }

    [GeneratedRegex(@"\A\s*#[ \t][^\r\n]*(\r?\n)?")]
    private static partial Regex LeadingTitle();

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
