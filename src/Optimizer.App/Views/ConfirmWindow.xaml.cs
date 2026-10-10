using System.Windows;
using Optimizer.App.Services;
using Wpf.Ui.Controls;

namespace Optimizer.App.Views;

/// <summary>Shows every exact change (target, current value, new value) before anything is written.</summary>
public partial class ConfirmWindow : FluentWindow
{
    private readonly bool _needsAck;

    public ConfirmWindow(ConfirmRequest request)
    {
        InitializeComponent();
        TitleText.Text = request.Title;
        SummaryText.Text = request.Summary;
        SummaryText.Visibility = string.IsNullOrWhiteSpace(request.Summary) ? Visibility.Collapsed : Visibility.Visible;
        BadgeText.Text = string.Join(", ", request.Badges);
        BadgeText.Visibility = request.Badges.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        WarningList.ItemsSource = request.AntiCheatWarning is null ? request.Warnings : request.Warnings.Append(request.AntiCheatWarning).ToList();
        ChangeList.ItemsSource = request.Changes
            .Select(c => c with { Before = Show(c.Before), After = Show(c.After) })
            .ToList();
        NewHeader.Text = Loc.Instance[request.IsUndo ? "Confirm_Restore" : "Confirm_New"];
        ApplyButton.Content = Loc.Instance[request.IsUndo ? "Confirm_Undo" : "Confirm_Apply"];
        _needsAck = request.AntiCheatWarning is not null;
        AntiCheatAck.Visibility = _needsAck ? Visibility.Visible : Visibility.Collapsed;
        ApplyButton.IsEnabled = !_needsAck;
    }

    /// <summary>Placeholders in the current language, also inside combined values ("Key=(not set), Key2=0").</summary>
    private static string Show(string value)
    {
        var lang = Loc.Instance.Language;
        var labels = Optimizer.Core.Docs.Labels.Current;
        return labels.Display(lang, value).Replace("(not set)", labels.Get(lang, "value.notSet"), StringComparison.Ordinal);
    }

    public bool Acknowledged => AntiCheatAck.IsChecked == true;

    private void AntiCheat_Changed(object sender, RoutedEventArgs e) => ApplyButton.IsEnabled = !_needsAck || Acknowledged;

    private void Apply_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
