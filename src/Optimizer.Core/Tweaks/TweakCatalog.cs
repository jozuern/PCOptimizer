using System.Text.Json;
using System.Text.Json.Serialization;
using Optimizer.Core.Catalog;

namespace Optimizer.Core.Tweaks;

/// <summary>Loads the embedded tweak catalog (Catalog/Tweaks/*.json, one file per category).</summary>
public sealed class TweakCatalog
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        AllowOutOfOrderMetadataProperties = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// For the embedded catalog only: an unknown property is an error (a typo like "restat" would otherwise be ignored
    /// and the tweak would silently miss its restart flag). Stored definitions in backups keep <see cref="JsonOptions"/>,
    /// so a backup written by an older version stays readable.
    /// </summary>
    public static readonly JsonSerializerOptions CatalogJsonOptions = new(JsonOptions) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };

    private static readonly Lazy<TweakCatalog> Lazy = new(Load);
    public static TweakCatalog Current => Lazy.Value;

    /// <summary>Bumped whenever catalog content changes; stored with every backup record.</summary>
    public const string Version = "2026.10.3";

    public required IReadOnlyList<TweakDefinition> Tweaks { get; init; }

    public TweakDefinition? Get(string id) => Tweaks.FirstOrDefault(t => t.Id == id);

    public IEnumerable<TweakDefinition> Visible => Tweaks.Where(t => !t.Hidden);

    public static TweakCatalog Load()
    {
        var list = new List<TweakDefinition>();
        foreach (var name in CatalogData.ResourceNames("Catalog.Tweaks.").OrderBy(n => n, StringComparer.Ordinal))
        {
            var file = JsonSerializer.Deserialize<CatalogFile>(CatalogData.ReadResourceText(name), CatalogJsonOptions)
                       ?? throw new InvalidDataException($"Empty catalog file {name}");
            list.AddRange(file.Tweaks);
        }
        var duplicate = list.GroupBy(t => t.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Duplicate tweak id {duplicate.Key}");
        return new TweakCatalog { Tweaks = list };
    }

    private sealed class CatalogFile
    {
        /// <summary>A note for maintainers at the top of a file.</summary>
        [JsonPropertyName("_comment")]
        public string? Comment { get; init; }

        public List<TweakDefinition> Tweaks { get; init; } = [];
    }
}
