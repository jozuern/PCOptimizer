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
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            var b = new Wpf.Ui.Controls.Button
            {
                Content = buttons[i],
                MinWidth = 100,
                Margin = new Thickness(8, 0, 0, 0),
                Appearance = i == buttons.Length - 1 ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary,
            };
            b.Click += (_, _) =>
            {
                Result = index;
                DialogResult = true;
            };
            Buttons.Children.Add(b);
        }
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
