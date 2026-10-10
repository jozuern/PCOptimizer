using Optimizer.Core.Logging;

namespace Optimizer.App.Services;

public static class TaskExtensions
{
    /// <summary>
    /// Starts work from a property setter or event handler that cannot await it. A failure is logged and reported
    /// instead of being lost as an unobserved task exception.
    /// </summary>
    public static async void Forget(this Task task, string what)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            Log.Error("ui", $"{what} failed", ex);
            (System.Windows.Application.Current?.MainWindow?.DataContext as ViewModels.MainViewModel)?.ShowResult(Loc.Instance["Error_Unexpected"], Wpf.Ui.Controls.InfoBarSeverity.Error);
        }
    }
}
