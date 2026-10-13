using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace Optimizer.App.Views;

/// <summary>
/// The app's list of items: an ItemsControl whose rows screen readers can enter. WPF's own peer reports every row as a
/// data item named by ToString() and without children, because the row's container (a ContentPresenter) has no peer:
/// the switches and buttons in a row were missing from UI Automation (Narrator's scan mode, Voice Access; found in the
/// VM). This peer lists what the rows show, from the visual tree. Not a Tab stop itself: the controls in the rows are.
/// </summary>
public class ItemList : ItemsControl
{
    static ItemList() => FocusableProperty.OverrideMetadata(typeof(ItemList), new FrameworkPropertyMetadata(false));

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}
