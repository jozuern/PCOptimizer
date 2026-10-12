using System.Windows;
using System.Windows.Controls;
using Optimizer.App.ViewModels;

namespace Optimizer.App.Views;

public partial class OverviewPage : UserControl
{
    public OverviewPage() => InitializeComponent();

    /// <summary>A number in the counts card: show the list it counts.</summary>
    private void Count_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || sender is not FrameworkElement { Tag: string target }) return;
        switch (target)
        {
            case "advisor":
                vm.CurrentPage = ViewModels.Page.Advisor;
                break;
            case "passed":
                vm.ShowPassed = true;
                FindingsHeader.BringIntoView();
                break;
            default:
                FindingsHeader.BringIntoView();
                break;
        }
    }
}
