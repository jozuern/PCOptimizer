namespace Optimizer.Core.Tweaks;

/// <summary>
/// The sections of the Tweaks page, in this order: the catalog's categories (two dozen, some with one or two tweaks)
/// combined by topic, gaming first. The categories stay in the catalog, the explanation pages and the tests; a test
/// checks that every category of the catalog belongs to a section.
/// </summary>
public static class TweakSections
{
    public static readonly IReadOnlyList<(string Key, string[] Categories)> All =
    [
        ("performance", ["Power", "Graphics", "Input", "Memory", "Storage", "Battery", "Quiet", "Leftovers", "Priorities", "Fixes"]),
        ("network", ["Network"]),
        ("background", ["Background", "Focus", "Updates", "Services", "Startup"]),
        ("privacy", ["Privacy", "Permissions", "AI"]),
        ("security", ["Security", "System", "Expert", "Features"]),
        ("desktop", ["Explorer", "Personalize", "Visual", "Productivity"]),
        ("browser", ["Browser"]),
    ];

    /// <summary>The section a category belongs to; an unknown one goes to Security and system (the test reports it).</summary>
    public static int IndexOf(string category)
    {
        for (var i = 0; i < All.Count; i++)
            if (All[i].Categories.Contains(category)) return i;
        return KnownIndexOf("security");
    }

    public static bool Contains(string category) => All.Any(s => s.Categories.Contains(category));

    private static int KnownIndexOf(string key)
    {
        for (var i = 0; i < All.Count; i++)
            if (All[i].Key == key) return i;
        return 0;
    }
}
