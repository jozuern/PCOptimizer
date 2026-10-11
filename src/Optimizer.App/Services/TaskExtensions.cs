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
            Report();
        }
    }

    /// <summary>
    /// The message goes to the UI thread: a task forgotten on a background thread ends there, and the window may not be
    /// reached from it. Nothing may throw from here (async void ends the process on an exception).
    /// </summary>
    private static void Report()
    {
        try
        {
            if (System.Windows.Application.Current?.Dispatcher is not { } dispatcher) return;
            dispatcher.BeginInvoke(() =>
                (System.Windows.Application.Current?.MainWindow?.DataContext as ViewModels.MainViewModel)?.ShowResult(Loc.Instance["Error_Unexpected"], Wpf.Ui.Controls.InfoBarSeverity.Error));
        }
        catch (Exception ex)
        {
            Log.Warn("ui", $"error not shown: {ex.Message}");
        }
    }
}
