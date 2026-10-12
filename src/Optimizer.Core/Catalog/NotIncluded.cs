namespace Optimizer.Core.Catalog;

/// <summary>
/// Popular tweaks the app does not offer (Catalog/Data/notincluded.json), so a search for one explains why instead of
/// finding nothing. The full reasons with sources are in docs/not-included.md, at <see cref="Url"/>.
/// </summary>
public sealed class NotIncludedCatalog
{
    public string Url { get; init; } = "";
    public List<NotIncludedEntry> Entries { get; init; } = [];
}

/// <param name="Doc">The start of the row in docs/not-included.md this entry summarizes.</param>
public sealed class NotIncludedEntry
{
    public string Id { get; init; } = "";
    public string Doc { get; init; } = "";
    public Dictionary<string, string> Title { get; init; } = [];
    public Dictionary<string, string> Reason { get; init; } = [];
    public List<string> Keywords { get; init; } = [];

    public string TitleIn(string lang) => Title.GetValueOrDefault(lang) ?? Title.GetValueOrDefault("en") ?? Id;
    public string ReasonIn(string lang) => Reason.GetValueOrDefault(lang) ?? Reason.GetValueOrDefault("en") ?? "";
}
