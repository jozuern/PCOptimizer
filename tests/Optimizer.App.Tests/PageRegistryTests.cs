using Optimizer.App.ViewModels;
using Optimizer.App.Views;

namespace Optimizer.App.Tests;

public sealed class PageRegistryTests
{
    /// <summary>Every page has one entry with a view of its own: the window navigates and the view model loads by it.</summary>
    [Fact]
    public void EveryPageIsRegisteredOnce()
    {
        Assert.Equal(Enum.GetValues<Page>().Order(), PageRegistry.All.Select(p => p.Id).Order());
        Assert.Equal(PageRegistry.All.Count, PageRegistry.All.Select(p => p.View).Distinct().Count());
        Assert.All(PageRegistry.All, p => Assert.True(typeof(System.Windows.FrameworkElement).IsAssignableFrom(p.View), p.Id.ToString()));
        Assert.Null(PageRegistry.ForView(typeof(string)));
    }
}
