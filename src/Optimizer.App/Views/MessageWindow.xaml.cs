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
        // Buttons share the footer in equal columns. The order of a Windows 11 content dialog: the primary (accent) button
        // first, then the secondary one, Cancel last. The callers list them the other way round (Cancel first, primary last).
        for (var i = 0; i < buttons.Length; i++)
        {
            var index = i;
            Buttons.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var primary = i == buttons.Length - 1;
            var column = buttons.Length - 1 - i;
            var b = new Wpf.Ui.Controls.Button
            {
                // Long labels (German, three buttons) wrap instead of being cut off.
                Content = new System.Windows.Controls.TextBlock { Text = buttons[i], TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center },
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch,
                Margin = new Thickness(column == 0 ? 0 : 4, 0, column == buttons.Length - 1 ? 0 : 4, 0),
                Appearance = primary ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary,
            };
            Grid.SetColumn(b, column);
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

    public string? PickSaveFile(string defaultName, string filterName, string extension)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = defaultName,
            Filter = $"{filterName} (*{extension})|*{extension}",
            DefaultExt = extension,
            AddExtension = true,
            OverwritePrompt = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public string? PickOpenFile(string filterName, string extension)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = $"{filterName} (*{extension})|*{extension}",
            CheckFileExists = true,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        return dialog.ShowDialog(Owner) == true ? dialog.FileName : null;
    }

    public bool ConfirmUndoAll(int count) =>
        MessageWindow.Show(Owner, Loc.Instance["UndoAll_Title"], Loc.Instance.Format("UndoAll_Text", count),
            Loc.Instance["Confirm_Cancel"], Loc.Instance["Changes_UndoAll"]) == 1;

    public UninstallChoice ConfirmUninstall(int activeChanges, string dataFolder)
    {
        var text = Loc.Instance.Format("Uninstall_Text", dataFolder);
        if (activeChanges == 0)
            return MessageWindow.Show(Owner, Loc.Instance["Uninstall_Title"], text, Loc.Instance["Confirm_Cancel"], Loc.Instance["Uninstall_Button"]) == 1
                ? UninstallChoice.Keep
                : UninstallChoice.Cancel;
        return MessageWindow.Show(Owner, Loc.Instance["Uninstall_Title"], text + "\n\n" + Loc.Instance.Format("Uninstall_TextChanges", activeChanges),
                Loc.Instance["Confirm_Cancel"], Loc.Instance["Uninstall_Keep"], Loc.Instance["Uninstall_UndoFirst"]) switch
        {
            2 => UninstallChoice.UndoFirst,
            1 => UninstallChoice.Keep,
            _ => UninstallChoice.Cancel,
        };
    }
}
