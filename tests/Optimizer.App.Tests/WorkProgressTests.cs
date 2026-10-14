using Optimizer.App.Services;

namespace Optimizer.App.Tests;

/// <summary>The progress banner: count and bar from the steps, a run started meanwhile takes it over, Stop.</summary>
public sealed class WorkProgressTests
{
    [Fact]
    public void StepsFillTheBarAndCountFromOne()
    {
        var work = new WorkProgress();
        using (var scope = work.Begin("Applying changes"))
        {
            Assert.True(work.IsShown); // no WPF application in tests: no delay
            Assert.True(work.IsIndeterminate);
            Assert.Null(work.Count);
            scope.Step(3, 12, "Game Mode");
            Assert.Equal((false, 25.0, "Game Mode"), (work.IsIndeterminate, work.Value, work.Detail));
            Assert.Equal(Loc.Instance.Format("Work_Count", 4, 12), work.Count);
            // The restore point: the bar moves, the count stays.
            scope.Indeterminate("Creating a restore point");
            Assert.True(work.IsIndeterminate);
            Assert.NotNull(work.Count);
            Assert.False(work.CanStop);
        }
        Assert.False(work.IsShown);
        Assert.False(work.IsActive);
        // A moving bar animates while collapsed too: it keeps the window from ever being idle.
        Assert.False(work.IsIndeterminate);
    }

    [Fact]
    public void NoRunNoMovingBar() => Assert.False(new WorkProgress().IsIndeterminate);

    [Fact]
    public void ALaterRunTakesTheBannerUntilItEnds()
    {
        var work = new WorkProgress();
        using var first = work.Begin("Analyzing storage");
        first.Report(40, "Files: 1000");
        using (var second = work.Begin("Applying a change"))
        {
            first.Report(50, "Files: 2000"); // not shown while the change runs
            Assert.Equal(("Applying a change", true), (work.Title, work.IsIndeterminate));
        }
        Assert.Equal(("Analyzing storage", 50.0, "Files: 2000"), (work.Title, work.Value, work.Detail));
        Assert.True(work.IsShown);
    }

    [Fact]
    public void StopCancelsAndSaysItEndsAfterTheStep()
    {
        var work = new WorkProgress();
        using var cts = new CancellationTokenSource();
        using var scope = work.Begin("Cleaning up", cts);
        Assert.True(work.CanStop);
        work.StopCommand.Execute(null);
        Assert.True(cts.IsCancellationRequested);
        Assert.False(work.CanStop);
        // A step reported after Stop does not replace the note.
        scope.Step(1, 3, "Temporary files");
        Assert.Equal(Loc.Instance["Work_Stopping"], work.Detail);
    }
}
