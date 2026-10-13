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

    /// <summary>
    /// The embedded catalog's content as a short hash, stored with every backup record: it changes with every catalog
    /// change by itself (a hand-kept number was forgotten for weeks).
    /// </summary>
    public static string Version => Current.ContentVersion;

    public required IReadOnlyList<TweakDefinition> Tweaks { get; init; }

    /// <summary>SHA-256 of the catalog files, first 12 hex digits; empty for a catalog built in code.</summary>
    public string ContentVersion { get; init; } = "";

    private Dictionary<string, TweakDefinition>? _byId;

    public TweakDefinition? Get(string id) => (_byId ??= Tweaks.ToDictionary(t => t.Id, StringComparer.Ordinal)).GetValueOrDefault(id);

    public IEnumerable<TweakDefinition> Visible => Tweaks.Where(t => !t.Hidden);

    public static TweakCatalog Load()
    {
        var list = new List<TweakDefinition>();
        using var hash = System.Security.Cryptography.IncrementalHash.CreateHash(System.Security.Cryptography.HashAlgorithmName.SHA256);
        foreach (var name in CatalogData.ResourceNames("Catalog.Tweaks.").OrderBy(n => n, StringComparer.Ordinal))
        {
            var text = CatalogData.ReadResourceText(name);
            hash.AppendData(System.Text.Encoding.UTF8.GetBytes(text));
            var file = JsonSerializer.Deserialize<CatalogFile>(text, CatalogJsonOptions)
                       ?? throw new InvalidDataException($"Empty catalog file {name}");
            list.AddRange(file.Tweaks);
        }
        var duplicate = list.GroupBy(t => t.Id).FirstOrDefault(g => g.Count() > 1);
        if (duplicate is not null) throw new InvalidDataException($"Duplicate tweak id {duplicate.Key}");
        return new TweakCatalog { Tweaks = list, ContentVersion = Convert.ToHexString(hash.GetHashAndReset())[..12].ToLowerInvariant() };
    }

    private sealed class CatalogFile
    {
        /// <summary>A note for maintainers at the top of a file.</summary>
        [JsonPropertyName("_comment")]
        public string? Comment { get; init; }

        public List<TweakDefinition> Tweaks { get; init; } = [];
    }
}
