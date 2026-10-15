using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Optimizer.App.Views;

public partial class SetupView : UserControl
{
    public SetupView()
    {
        InitializeComponent();
        // The screen covers the window: the keyboard starts on Install, not on a control under it.
        IsVisibleChanged += (_, e) =>
        {
            if (e.NewValue is true) Dispatcher.BeginInvoke(() => InstallButton.Focus(), DispatcherPriority.Input);
        };
    }
}
