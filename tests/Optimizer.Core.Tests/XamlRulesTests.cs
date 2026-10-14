using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Optimizer.Core.Tests;

/// <summary>Rules for the app's XAML that a screenshot cannot show.</summary>
public partial class XamlRulesTests
{
    private static readonly XNamespace Presentation = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

    /// <summary>
    /// A hand cursor set on a TabItem reaches the whole tab's content (its mouse events route through the TabItem), so every
    /// text and card on a page with tabs showed as clickable. The hand belongs on the header inside the tab's template.
    /// </summary>
    [Fact]
    public void TabItemsSetNoCursorForTheirContent()
    {
        var offenders = new List<string>();
        foreach (var file in Directory.EnumerateFiles(RepoPaths.App, "*.xaml", SearchOption.AllDirectories)
                     .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") && !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")))
        {
            var doc = XDocument.Load(file);
            var relative = Path.GetRelativePath(RepoPaths.App, file);
            foreach (var style in doc.Descendants(Presentation + "Style").Where(s => TabItemType().IsMatch((string?)s.Attribute("TargetType") ?? "")))
                if (style.Elements(Presentation + "Setter").Any(s => (string?)s.Attribute("Property") == "Cursor"))
                    offenders.Add($"{relative}: a TabItem style sets Cursor");
            foreach (var item in doc.Descendants(Presentation + "TabItem").Where(i => i.Attribute("Cursor") is not null))
                offenders.Add($"{relative}: a TabItem sets Cursor");
        }
        Assert.Empty(offenders);
    }

    /// <summary>
    /// The navigation rail uses NavItem and NavHeader (Views/NavItem.cs): WPF-UI's own item gives screen readers no way to
    /// open a page, and its heading is not read at all. Both need the switch that removes the ItemsControl's item wrapper.
    /// </summary>
    [Fact]
    public void TheNavigationRailIsReachableForScreenReaders()
    {
        var window = File.ReadAllText(Path.Combine(RepoPaths.App, "Views", "MainWindow.xaml"));
        Assert.DoesNotContain("<ui:NavigationViewItem ", window);
        Assert.DoesNotContain("<ui:NavigationViewItemHeader ", window);
        Assert.Contains("<v:NavItem ", window);
        Assert.Contains("<v:NavHeader ", window);
        var project = File.ReadAllText(Path.Combine(RepoPaths.App, "Optimizer.App.csproj"));
        Assert.Contains("\"Switch.System.Windows.Controls.ItemsControlDoesNotSupportAutomation\" Value=\"true\"", project);
    }

    [GeneratedRegex(@"^(\{x:Type\s+)?TabItem\}?$")]
    private static partial Regex TabItemType();
}
