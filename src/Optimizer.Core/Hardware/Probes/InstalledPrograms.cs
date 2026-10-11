namespace Optimizer.Core.Hardware.Probes;

public sealed record InstalledProgram(string Name, string? Version, string? Publisher, bool PerUser);

/// <summary>
/// Installed programs for the checks: the same list as the Apps page (<see cref="Apps.Programs.Read"/>, machine 64/32-bit
/// and the session user's hive, without Windows components and updates), one entry per name. Never uses
/// Win32_Product: querying it triggers MSI self-repair.
/// </summary>
public static class InstalledPrograms
{
    public static IReadOnlyList<InstalledProgram> Read(string? userSid = null) =>
        Apps.Programs.Read(new Platform.SystemRegistryRoots(userSid))
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .Select(p => new InstalledProgram(p.Name, p.Version, p.Publisher, p.PerUser))
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
