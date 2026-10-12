using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Optimizer.Core.Tools;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Backup;

/// <summary>A start priority rule in a transfer file.</summary>
public sealed record TransferPriority(string Exe, CpuPriority Cpu, bool LowIo);

/// <summary>
/// The changes of one PC as a file, to apply them on another PC or after a reinstall. It holds only catalog tweak ids
/// and start priority rules (program name and class), never actions: a file can only select what this version of the
/// app offers, so a changed or foreign file cannot make the elevated app write anything else. Changes made for one PC
/// (startup entries, services, device tweaks, fixes of findings, NVIDIA profiles) are left out, and removed apps too,
/// because removing an app cannot be undone.
/// </summary>
public sealed record TransferFile(
    string Kind,
    int Format,
    string AppVersion,
    string WindowsBuild,
    DateTimeOffset Created,
    IReadOnlyList<string> Tweaks,
    IReadOnlyList<TransferPriority> Priorities);

/// <summary>What an import would do on this PC.</summary>
public sealed record ImportPlan(
    IReadOnlyList<TweakDefinition> Together,
    IReadOnlyList<TweakDefinition> OneByOne,
    int AlreadyOn,
    int NotForThisPc,
    int Unknown)
{
    public bool IsEmpty => Together.Count == 0 && OneByOne.Count == 0;
}

public static partial class ChangeTransfer
{
    public const string FileKind = "pcoptimizer-changes";
    public const int CurrentFormat = 1;
    public const string Extension = ".tunero.json";

    /// <summary>What the import dialog lists: every .json, so files saved before the rename (".pcoptimizer.json") still show.</summary>
    public const string ImportPattern = ".json";

    /// <summary>A transfer file is a few kilobytes; anything larger is not one.</summary>
    public const long MaxFileBytes = 1024 * 1024;

    private const int MaxEntries = 2000;

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._\-]{0,119}$")]
    private static partial Regex IdRegex();

    /// <summary>
    /// The file for the catalog tweaks this app changed and that are on now (<paramref name="backedUp"/> and
    /// <paramref name="applied"/>), and the start priority rules this app offers. Returns the file and how many changes
    /// were left out because they belong to this PC only.
    /// </summary>
    public static (TransferFile File, int PcSpecific) Create(IEnumerable<string> backedUp, IReadOnlySet<string> applied, TweakCatalog catalog,
        IEnumerable<PriorityRule> priorities, string appVersion, string windowsBuild)
    {
        var tweaks = new List<string>();
        var pcSpecific = 0;
        foreach (var id in backedUp.Distinct(StringComparer.Ordinal))
        {
            if (id.StartsWith("priority.", StringComparison.Ordinal)) continue;
            if (catalog.Get(id) is { Hidden: false } && applied.Contains(id)) tweaks.Add(id);
            else if (catalog.Get(id) is null) pcSpecific++;
        }
        var rules = priorities.Where(r => r.Cpu is not null && ProgramPriority.IsValidExe(r.Exe))
            .Select(r => new TransferPriority(r.Exe, r.Cpu!.Value, r.LowIo)).ToList();
        tweaks.Sort(StringComparer.Ordinal);
        return (new TransferFile(FileKind, CurrentFormat, appVersion, windowsBuild, DateTimeOffset.Now, tweaks, rules), pcSpecific);
    }

    public static string Serialize(TransferFile file) => JsonSerializer.Serialize(file, Json);

    /// <summary>Reads a transfer file; null when it is not one, is from a newer format, or holds anything invalid.</summary>
    public static TransferFile? Parse(string text)
    {
        if (text.Length > MaxFileBytes) return null;
        TransferFile? file;
        try
        {
            file = JsonSerializer.Deserialize<TransferFile>(text, Json);
        }
        catch (JsonException)
        {
            return null;
        }
        if (file is null || file.Kind != FileKind || file.Format is < 1 or > CurrentFormat) return null;
        if (file.Tweaks is null || file.Priorities is null || file.Tweaks.Count + file.Priorities.Count > MaxEntries) return null;
        if (file.Tweaks.Any(id => id is null || !IdRegex().IsMatch(id))) return null;
        if (file.Priorities.Any(p => p is null || p.Exe is null || !ProgramPriority.IsValidExe(p.Exe) || !Enum.IsDefined(p.Cpu))) return null;
        return file;
    }

    /// <summary>
    /// Sorts the file's changes for this PC: what can be applied together after one confirmation (the same rules as
    /// Apply recommended), what needs its own confirmation (previews, Expert, anti-cheat sensitive, boot-critical), and
    /// what is skipped because it is on already, does not apply here or is unknown to this version.
    /// </summary>
    public static ImportPlan Plan(TransferFile file, TweakCatalog catalog, Facts facts, IReadOnlySet<string> applied)
    {
        var together = new List<TweakDefinition>();
        var oneByOne = new List<TweakDefinition>();
        int on = 0, notHere = 0, unknown = 0;
        void Add(TweakDefinition t)
        {
            if (applied.Contains(t.Id)) on++;
            else if (!TweakEngine.AppliesTo(t, facts)) notHere++;
            else (t.IsBatchSafe ? together : oneByOne).Add(t);
        }
        foreach (var id in file.Tweaks.Distinct(StringComparer.Ordinal))
        {
            if (catalog.Get(id) is { Hidden: false } t) Add(t);
            else unknown++;
        }
        foreach (var p in file.Priorities.DistinctBy(p => p.Exe.ToLowerInvariant()))
            Add(ProgramPriority.Tweak(p.Exe, p.Cpu, p.LowIo));
        return new ImportPlan(together, oneByOne, on, notHere, unknown);
    }
}
