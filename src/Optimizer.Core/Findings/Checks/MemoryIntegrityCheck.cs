using Optimizer.Core.Catalog;
using Optimizer.Core.Hardware;

namespace Optimizer.Core.Findings.Checks;

/// <summary>
/// F31: memory integrity (HVCI) is running. Information, never a problem: it is Microsoft's secure default, and from
/// October 2026 Windows quality updates turn it on for more PCs, so a gamer may see lower frame rates without knowing why.
/// Measured: about 6 % average and 8 % in the 1 % lows in CPU-limited games (ComputerBase 2023, Ryzen 7 5800X), more on
/// processors without MBEC/GMET, which emulate it (variant "noMbec", impact 3). The fix is the Expert tweak
/// security.vbsOff with its warnings (it lists this check in "fixes").
/// </summary>
public sealed class MemoryIntegrityCheck : IFindingCheck
{
    public const string Id = "F31.memoryIntegrity";
    public IReadOnlyList<string> DocIds => [Id];

    public IEnumerable<Finding> Evaluate(HardwareProfile p, CatalogData c)
    {
        if (p.Firmware is not { } fw) yield break;
        var running = fw.VbsRunning && fw.HvciRunning;
        // VbsStatus < 0: Win32_DeviceGuard could not be read, so memory integrity is unknown, not off.
        var unknown = fw.VbsStatus < 0;
        yield return new Finding
        {
            Id = Id,
            Kind = FindingKind.Finding,
            Status = unknown ? FindingStatus.Unknown : running ? FindingStatus.Info : FindingStatus.Ok,
            Variant = running && !fw.MbecAvailable ? "noMbec" : null,
            Impact = fw.MbecAvailable ? 2 : 3,
            Effects = [Effect.Fps, Effect.Lows],
            Facts =
            [
                new("fact.hvci", unknown ? "@unknown" : running ? "@on" : "@off"),
                new("fact.mbec", fw.MbecAvailable ? "@yes" : "@no"),
            ],
        };
    }
}
