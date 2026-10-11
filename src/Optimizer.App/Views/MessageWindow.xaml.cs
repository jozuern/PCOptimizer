using System.Windows;
using System.Windows.Controls;
using Optimizer.App.Services;
using Wpf.Ui.Controls;

namespace Optimizer.App.Views;

/// <summary>Small question dialog. The last button is the primary (accent) one; the result is the clicked index, or -1.</summary>
public partial class MessageWindow : FluentWindow
{
    public MessageWindow(string title, string body, params string[] buttons)
    {
        InitializeComponent();
        TitleText.Text = title;
        BodyText.Text = body;
        // Buttons share the footer in equal columns, primary (accent) last, as in a Windows 11 content dialog.
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            Buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var primary = i == buttons.Length - 1;
            var b = new Wpf.Ui.Controls.Button
            {
                // Long labels (German, three buttons) wrap instead of being cut off.
                Content = new System.Windows.Controls.TextBlock { Text = buttons[i], TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(i == 0 ? 0 : 4, 0, primary ? 0 : 4, 0),
                Appearance = primary ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary,
            };
            Grid.SetColumn(b, i);
            b.Click += (_, _) =>
            {
                Result = index;
                DialogResult = true;
            };
            Buttons.Children.Add(b);
        }
        // Escape closes the dialog like the window's close button: no choice made (-1).
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key != System.Windows.Input.Key.Escape) return;
            e.Handled = true;
            DialogResult = false;
        };
    }

    public int Result { get; private set; } = -1;

    public static int Show(Window? owner, string title, string body, params string[] buttons)
    {
        var w = new MessageWindow(title, body, buttons) { Owner = owner };
        return w.ShowDialog() == true ? w.Result : -1;
    }
}

/// <summary>IDialogs implementation for the view model.</summary>
public sealed class Dialogs : IDialogs
{
    private static Window? Owner => Application.Current.MainWindow;

    public ConfirmResult? ConfirmApply(ConfirmRequest request)
    {
        var w = new ConfirmWindow(request) { Owner = Owner };
        return w.ShowDialog() == true ? new ConfirmResult(w.Acknowledged) : null;
    }

    public RestorePointChoice AskRestorePoint() =>
        MessageWindow.Show(Owner, Loc.Instance["Rp_AskTitle"], Loc.Instance["Rp_AskText"],
                Loc.Instance["Confirm_Cancel"], Loc.Instance["Rp_Continue"], Loc.Instance["Rp_Enable"]) switch
        {
            2 => RestorePointChoice.Enable,
            1 => RestorePointChoice.Continue,
            _ => RestorePointChoice.Cancel,
        };

    public bool ConfirmExpertMode() =>
        MessageWindow.Show(Owner, Loc.Instance["Expert_ConfirmTitle"], Loc.Instance["Expert_ConfirmText"],
            Loc.Instance["Confirm_Cancel"], Loc.Instance["Settings_Expert"]) == 1;

    public bool Ask(string title, string text, string primary) =>
        MessageWindow.Show(Owner, title, text, Loc.Instance["Confirm_Cancel"], primary) == 1;

    public bool ConfirmUndoAll(int count) =>
        MessageWindow.Show(Owner, Loc.Instance["UndoAll_Title"], Loc.Instance.Format("UndoAll_Text", count),
            Loc.Instance["Confirm_Cancel"], Loc.Instance["Changes_UndoAll"]) == 1;
}
