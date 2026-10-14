using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Wpf.Ui.Controls;

namespace Optimizer.App.Views;

/// <summary>
/// A page in the navigation rail. WPF-UI's item reports itself to UI Automation without a pattern to act on (its peer is
/// a plain FrameworkElementAutomationPeer): Narrator could not tell which page is open, and Voice Access or another
/// assistive tool could not open a page by name. This one is a list item with Invoke and SelectionItem, as WinUI's own
/// NavigationViewItem is; both open the page the way a click does.
/// </summary>
public class NavItem : NavigationViewItem
{
    static NavItem() => DefaultStyleKeyProperty.OverrideMetadata(typeof(NavItem), new FrameworkPropertyMetadata(typeof(NavigationViewItem)));

    protected override AutomationPeer OnCreateAutomationPeer() => new NavItemPeer(this);

    /// <summary>Opens the page as a click does (ButtonBase.OnClick tells the NavigationView).</summary>
    private void Open() => OnClick();

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        // The open page changed: screen readers hear it from the selection state.
        if (e.Property == IsActiveProperty && UIElementAutomationPeer.FromElement(this) is NavItemPeer peer)
            peer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, (bool)e.OldValue, (bool)e.NewValue);
    }

    private sealed class NavItemPeer(NavItem owner) : FrameworkElementAutomationPeer(owner), IInvokeProvider, ISelectionItemProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

        protected override string GetClassNameCore() => nameof(NavItem);

        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Content as string ?? "";

        public override object? GetPattern(PatternInterface pattern) =>
            pattern is PatternInterface.Invoke or PatternInterface.SelectionItem ? this : base.GetPattern(pattern);

        public void Invoke()
        {
            if (!IsEnabled()) throw new ElementNotEnabledException();
            owner.Open();
        }

        public bool IsSelected => owner.IsActive;

        public IRawElementProviderSimple? SelectionContainer => null;

        public void Select() => Invoke();

        // One page is always open: another page is chosen with Select, nothing is added or removed.
        public void AddToSelection() => throw new InvalidOperationException();

        public void RemoveFromSelection() => throw new InvalidOperationException();
    }
}

/// <summary>
/// A group heading in the navigation rail ("Optimize"). WPF-UI's heading has no automation peer and its text is part of
/// its template, so without the ItemsControl's item wrapper (see the switch in Optimizer.App.csproj) screen readers
/// would not hear the groups at all. This one is a text element with the heading's text, marked as a heading by its
/// style, so Narrator can also jump from group to group.
/// </summary>
public class NavHeader : NavigationViewItemHeader
{
    static NavHeader() => DefaultStyleKeyProperty.OverrideMetadata(typeof(NavHeader), new FrameworkPropertyMetadata(typeof(NavigationViewItemHeader)));

    protected override AutomationPeer OnCreateAutomationPeer() => new NavHeaderPeer(this);

    private sealed class NavHeaderPeer(NavHeader owner) : FrameworkElementAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        protected override string GetClassNameCore() => nameof(NavHeader);

        protected override string GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : owner.Text ?? "";
    }
}
