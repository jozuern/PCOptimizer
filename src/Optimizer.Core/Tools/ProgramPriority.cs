using System.Text.Json;
using System.Text.RegularExpressions;
using Optimizer.Core.Actions;
using Optimizer.Core.Startup;
using Optimizer.Core.Tweaks;

namespace Optimizer.Core.Tools;

/// <summary>
/// CPU priority classes Windows applies at program start from Image File Execution Options\&lt;exe&gt;\PerfOptions
/// (CpuPriorityClass). Realtime is never offered: it can starve Windows itself.
/// </summary>
public enum CpuPriority { Low = 1, BelowNormal = 5, AboveNormal = 6, High = 3 }

/// <summary>A start priority rule for one program file name.</summary>
/// <param name="OtherCpu">A CpuPriorityClass this app does not offer (Normal, Realtime), set by another program.</param>
public sealed record PriorityRule(string Exe, CpuPriority? Cpu, bool LowIo, int? OtherCpu = null)
{
    /// <summary>A memory priority below normal (PagePriority): its pages are trimmed from memory before others.</summary>
    public bool LowMemory { get; init; }
}

/// <summary>
/// Persistent start priorities without a background program: Windows reads PerfOptions when the program starts.
/// Microsoft documents the priority classes and Image File Execution Options, but not the PerfOptions values, so the
/// rules carry the undocumented badge; a VM test proved that Windows applies them (<see cref="Proof"/>).
/// </summary>
public static partial class ProgramPriority
{
    private const string Ifeo = StartupScanner.Ifeo;

    /// <summary>The test that showed Windows applies the PerfOptions values (CONTRIBUTING, "Undocumented values").</summary>
    public const string Proof = "VM test 2026-10-13, Windows 11 build 26300: PING.EXE with Below normal and low disk priority started with priority " +
        "class BelowNormal and I/O priority 1 (NtQueryInformationProcess), and with Normal and 2 again after Undo.";

    /// <summary>The test that showed Windows applies PagePriority (the memory priority of the process).</summary>
    public const string MemoryProof = "VM test 2026-10-13, Windows 11 build 26300.9550: PING.EXE with PagePriority 1, 2 and 3 started with memory " +
        "priority 1, 2 and 3 (GetProcessInformation, ProcessMemoryPriority), and with 5 (normal) again when the value was removed.";

    /// <summary>MEMORY_PRIORITY_LOW: trimmed from the working set before normal programs, but not as early as very low.</summary>
    public const int LowMemoryPriority = 2;

    public static bool IsValidExe(string name) => ExeRegex().IsMatch(name);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 ._\-]{0,99}\.exe$", RegexOptions.IgnoreCase)]
    private static partial Regex ExeRegex();

    /// <summary>
    /// Every program with a CpuPriorityClass or an I/O priority in PerfOptions. Rules from other programs with a class
    /// this app does not offer (Realtime) are listed too, so they can be seen and removed. Names the app cannot handle
    /// (characters outside <see cref="IsValidExe"/>) are left out.
    /// </summary>
    public static IReadOnlyList<PriorityRule> Read(IRegistryRoots registry)
    {
        var list = new List<PriorityRule>();
        using var root = registry.Open(Hive.Machine, Ifeo, writable: false);
        foreach (var exe in root?.GetSubKeyNames() ?? [])
        {
            if (!IsValidExe(exe)) continue;
            using var perf = root!.OpenSubKey($@"{exe}\PerfOptions");
            if (perf is null) continue;
            var raw = perf.GetValue("CpuPriorityClass") as int?;
            CpuPriority? cpu = raw is { } c && Enum.IsDefined(typeof(CpuPriority), c) ? (CpuPriority)c : null;
            int? other = raw is not null && cpu is null ? raw : null;
            var lowIo = perf.GetValue("IoPriority") is int io && io < 2;
            var lowMemory = perf.GetValue("PagePriority") is int page && page is >= 1 and < 5;
            if (cpu is not null || other is not null || lowIo || lowMemory) list.Add(new PriorityRule(exe, cpu, lowIo, other) { LowMemory = lowMemory });
        }
        return list.OrderBy(r => r.Exe, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static RegistryAction Value(string exe, string name, uint value) => new()
    {
        Hive = Hive.Machine,
        Path = $@"{Ifeo}\{exe}\PerfOptions",
        Name = name,
        Kind = "dword",
        Value = JsonSerializer.SerializeToElement(value),
    };

    /// <summary>Engine tweak that sets the rule (backup and undo like every other change).</summary>
    public static TweakDefinition Tweak(string exe, CpuPriority cpu, bool lowIo, bool lowMemory = false)
    {
        if (!IsValidExe(exe)) throw new ArgumentException($"invalid program name {exe}");
        var actions = new List<TweakAction> { Value(exe, "CpuPriorityClass", (uint)cpu) };
        // Without low disk or memory priority, a value left from an earlier rule is removed (undo puts it back).
        actions.Add(lowIo ? Value(exe, "IoPriority", 1)
            : new RegistryAction { Hive = Hive.Machine, Path = $@"{Ifeo}\{exe}\PerfOptions", Name = "IoPriority", Delete = true });
        actions.Add(lowMemory ? Value(exe, "PagePriority", LowMemoryPriority)
            : new RegistryAction { Hive = Hive.Machine, Path = $@"{Ifeo}\{exe}\PerfOptions", Name = "PagePriority", Delete = true });
        return new TweakDefinition
        {
            Id = $"priority.{TweakIds.Slug(exe.ToLowerInvariant())}",
            Docs = "priority.change",
            Subject = exe,
            Category = "Priorities",
            Impact = new ImpactInfo { Gaming = 0, Basis = "disputed", Effect = ["none"] },
            Risk = Risk.Moderate,
            Hidden = true,
            Undocumented = true,
            Proof = [Proof, MemoryProof],
            AntiCheatSensitive = true,
            Actions = actions,
            Sources = ["https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"],
        };
    }

    /// <summary>Removes a rule this app did not create (its values are backed up, so this is undoable too).</summary>
    public static TweakDefinition RemoveTweak(string exe)
    {
        if (!IsValidExe(exe)) throw new ArgumentException($"invalid program name {exe}");
        return new TweakDefinition
        {
            Id = $"priority.remove.{TweakIds.Slug(exe.ToLowerInvariant())}",
            Docs = "priority.change",
            Subject = exe,
            Category = "Priorities",
            Impact = new ImpactInfo { Gaming = 0, Basis = "disputed", Effect = ["none"] },
            Risk = Risk.Moderate,
            Hidden = true,
            Actions =
            [
                new RegistryAction { Hive = Hive.Machine, Path = $@"{Ifeo}\{exe}\PerfOptions", Name = "CpuPriorityClass", Delete = true },
                new RegistryAction { Hive = Hive.Machine, Path = $@"{Ifeo}\{exe}\PerfOptions", Name = "IoPriority", Delete = true },
                new RegistryAction { Hive = Hive.Machine, Path = $@"{Ifeo}\{exe}\PerfOptions", Name = "PagePriority", Delete = true },
            ],
            Sources = ["https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"],
        };
    }
}
