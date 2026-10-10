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

    // Checked, not Command: a screen reader's select action and the arrow keys check a radio button without clicking it.
    // The binding checks the button of the current language too, which must not switch again.
    private void Language_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton { Tag: string lang } || DataContext is not MainViewModel vm) return;
        if (lang != Services.Loc.Instance.Language) vm.SetLanguageCommand.Execute(lang);
    }

    private void SaveKey_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm || string.IsNullOrWhiteSpace(VtKey.Password)) return;
        vm.SaveVirusTotalKey(VtKey.Password);
        VtKey.Password = "";
    }

    private void Licenses_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        new LicensesWindow(url => vm.OpenLinkCommand.Execute(url)) { Owner = Window.GetWindow(this) }.ShowDialog();
    }

    private void RemoveKey_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm) vm.SaveVirusTotalKey(null);
        VtKey.Password = "";
    }
}
