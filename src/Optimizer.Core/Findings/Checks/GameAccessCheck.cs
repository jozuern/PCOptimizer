using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings.Checks;

/// <summary>
/// Game access panel (plan v4 §5.3): anti-cheat readiness, shown separately from ⚡. Read-only; never advises
/// disabling VT-x/SVM/VT-d/IOMMU. Requirements per anti-cheat come from anticheat.json: "required" parts block games
/// when missing (Problem), "sometimes" parts are enforced only in some modes or rollout waves (Info).
/// </summary>
public sealed class GameAccessCheck : IFindingCheck
{
    public const string Id = "G.access";
    public IReadOnlyList<string> DocIds => [Id];

    /// <summary>Requirement ids used in anticheat.json.</summary>
    public static readonly string[] Parts = ["uefi", "secureBoot", "tpm2", "hvci", "iommu"];

    public static TriState PartState(FirmwareInfo? fw, string part)
    {
        if (fw is null) return TriState.Unknown;
        return part switch
        {
            "uefi" => fw.IsUefi ? TriState.Yes : TriState.No,
            "secureBoot" => fw.SecureBoot,
            "tpm2" => fw.TpmPresent switch
            {
                TriState.Yes => fw.TpmSpecVersion?.TrimStart().StartsWith('2') == true ? TriState.Yes : TriState.No,
                TriState.No => TriState.No,
                _ => TriState.Unknown,
            },
            "hvci" => fw.HvciRunning ? TriState.Yes : TriState.No,
            // Kernel DMA protection proves the IOMMU is on; its absence does not prove it is off.
            "iommu" => fw.DmaProtectionAvailable ? TriState.Yes : TriState.Unknown,
            _ => TriState.Unknown,
        };
    }

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        var fw = p.Firmware;
        var installed = p.Software?.AntiCheats ?? [];
        var signatures = installed
            .Select(a => (Presence: a, Sig: c.AntiCheat.AntiCheats.FirstOrDefault(s => s.Id == a.Id)))
            .Where(x => x.Sig is not null)
            .ToList();

        static string Tri(TriState t) => t switch { TriState.Yes => "@yes", TriState.No => "@no", _ => "@unknown" };

        // Missing parts per installed anti-cheat.
        var missingRequired = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var missingSometimes = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var unknownRequired = false;
        foreach (var (presence, sig) in signatures)
        {
            foreach (var part in sig!.Required)
            {
                var state = PartState(fw, part);
                if (state == TriState.No) Add(missingRequired, part, presence.DisplayName);
                else if (state == TriState.Unknown) unknownRequired = true;
            }
            foreach (var part in sig.Sometimes.Where(part => PartState(fw, part) == TriState.No))
                Add(missingSometimes, part, presence.DisplayName);
        }

        // Baseline for strict kernel anti-cheats when none (or only lenient ones) is installed.
        TriState[] baselineParts = [PartState(fw, "uefi"), PartState(fw, "secureBoot"), PartState(fw, "tpm2")];
        var baseline = baselineParts.Contains(TriState.No) ? TriState.No : baselineParts.Contains(TriState.Unknown) ? TriState.Unknown : TriState.Yes;

        FindingStatus status;
        string? variant = null;
        if (missingRequired.Count > 0) status = FindingStatus.Problem;
        else if (unknownRequired) status = FindingStatus.Unknown;
        else if (missingSometimes.Count > 0) { status = FindingStatus.Info; variant = "sometimes"; }
        else if (signatures.Any(x => x.Sig!.Required.Count > 0)) status = FindingStatus.Ok;
        else status = baseline switch
        {
            TriState.Yes => FindingStatus.Ok,
            TriState.No => FindingStatus.Info,
            _ => FindingStatus.Unknown,
        };
        if (status == FindingStatus.Info && variant is null) variant = "baseline";

        var parameters = new Dictionary<string, string>
        {
            ["antiCheats"] = string.Join(", ", installed.Select(a => a.DisplayName)),
            ["mbr"] = fw?.SystemDiskPartitionStyle == PartitionStyle.Mbr ? "yes" : "",
        };
        foreach (var (presence, sig) in signatures)
        {
            parameters[presence.Id] = "yes";
            // Catalog entries not yet checked against the vendor's own documentation say so on the page.
            if (!sig!.Verified) parameters[$"unverified_{presence.Id}"] = "yes";
        }
        foreach (var part in Parts)
        {
            if (missingRequired.ContainsKey(part) || missingSometimes.ContainsKey(part)) parameters[$"missing_{part}"] = "yes";
        }

        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.GameAccess,
            Status = status,
            Variant = variant,
            Impact = null,
            Effects = [Effect.Prerequisite],
            Facts =
            [
                new("fact.uefi", fw is null ? "@unknown" : fw.IsUefi ? "@yes" : "@no"),
                new("fact.secureBoot", fw is null ? "@unknown" : Tri(fw.SecureBoot)),
                new("fact.tpm", fw is null ? "@unknown" : fw.TpmPresent == TriState.Yes ? $"{fw.TpmSpecVersion}" : Tri(fw.TpmPresent)),
                new("fact.tpmReady", fw is null ? "@unknown" : Tri(fw.TpmReady)),
                new("fact.vbs", fw is null ? "@unknown" : fw.VbsStatus switch { 2 => "@running", 1 => "@configured", 0 => "@off", _ => "@unknown" }),
                new("fact.hvci", fw is null ? "@unknown" : fw.HvciRunning ? "@running" : "@off"),
                new("fact.iommu", fw is null ? "@unknown" : fw.DmaProtectionAvailable ? "@available" : "@unknown"),
                new("fact.systemDiskStyle", fw?.SystemDiskPartitionStyle.ToString().ToUpperInvariant() ?? "@unknown"),
                new("fact.antiCheats", installed.Count == 0 ? "@none" : string.Join(", ", installed.Select(a => a.DisplayName))),
                .. missingRequired.Select(kv => new Fact($"fact.required.{kv.Key}", string.Join(", ", kv.Value))),
                .. missingSometimes.Select(kv => new Fact($"fact.sometimes.{kv.Key}", string.Join(", ", kv.Value))),
            ],
            Params = parameters,
        };
    }

    private static void Add(Dictionary<string, List<string>> map, string part, string name)
    {
        if (!map.TryGetValue(part, out var list)) map[part] = list = [];
        if (!list.Contains(name)) list.Add(name);
    }
}
