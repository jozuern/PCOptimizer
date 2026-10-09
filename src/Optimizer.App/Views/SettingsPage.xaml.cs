using System.Windows;
using System.Windows.Controls;
using Optimizer.App.ViewModels;

namespace Optimizer.App.Views;

public partial class SettingsPage : UserControl
{
    private bool _loading;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            _loading = true;
            var theme = (Application.Current as App)?.ThemeSetting ?? "System";
            ThemeSystem.IsChecked = theme == "System";
            ThemeLight.IsChecked = theme == "Light";
            ThemeDark.IsChecked = theme == "Dark";
            _loading = false;
        };
    }

    private void Theme_Checked(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not RadioButton { Tag: string theme }) return;
        (Application.Current as App)?.ApplyTheme(theme);
    }

    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || string.IsNullOrWhiteSpace(VtKey.Password)) return;
        vm.SaveVirusTotalKey(VtKey.Password);
        VtKey.Password = "";
    }

    private void RemoveKey_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SaveVirusTotalKey(null);
        VtKey.Password = "";
    }
}
