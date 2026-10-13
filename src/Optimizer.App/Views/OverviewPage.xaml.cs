using System.Windows;
using System.Windows.Controls;
using Optimizer.App.ViewModels;

namespace Optimizer.App.Views;

public partial class OverviewPage : UserControl
{
    public OverviewPage() => InitializeComponent();

    /// <summary>"To change by hand": the Manual checks page lists what to change in the BIOS or on the hardware.</summary>
    private void Advisor_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.CurrentPage = ViewModels.Page.Advisor;
    }
}
