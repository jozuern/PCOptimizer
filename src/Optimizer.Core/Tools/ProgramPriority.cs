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
public sealed record PriorityRule(string Exe, CpuPriority? Cpu, bool LowIo);

/// <summary>
/// Persistent start priorities without a background program: Windows reads PerfOptions when the program starts.
/// Microsoft documents the priority classes and Image File Execution Options, but not the PerfOptions values, so the
/// rules carry the undocumented badge and stay Preview until tested.
/// </summary>
public static partial class ProgramPriority
{
    private const string Ifeo = StartupScanner.Ifeo;

    public static bool IsValidExe(string name) => ExeRegex().IsMatch(name);

    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9 ._\-]{0,99}\.exe$", RegexOptions.IgnoreCase)]
    private static partial Regex ExeRegex();

    /// <summary>Every program with a CpuPriorityClass or an I/O priority in PerfOptions.</summary>
    public static IReadOnlyList<PriorityRule> Read(IRegistryRoots registry)
    {
        var list = new List<PriorityRule>();
        using var root = registry.Open(Hive.Machine, Ifeo, writable: false);
        foreach (var exe in root?.GetSubKeyNames() ?? [])
        {
            using var perf = root!.OpenSubKey($@"{exe}\PerfOptions");
            if (perf is null) continue;
            CpuPriority? cpu = perf.GetValue("CpuPriorityClass") is int c && Enum.IsDefined(typeof(CpuPriority), c) ? (CpuPriority)c : null;
            var lowIo = perf.GetValue("IoPriority") is int io && io < 2;
            if (cpu is not null || lowIo) list.Add(new PriorityRule(exe, cpu, lowIo));
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
    public static TweakDefinition Tweak(string exe, CpuPriority cpu, bool lowIo)
    {
        if (!IsValidExe(exe)) throw new ArgumentException($"invalid program name {exe}");
        var actions = new List<TweakAction> { Value(exe, "CpuPriorityClass", (uint)cpu) };
        if (lowIo) actions.Add(Value(exe, "IoPriority", 1));
        return new TweakDefinition
        {
            Id = $"priority.{TweakIds.Slug(exe.ToLowerInvariant())}",
            Docs = "priority.change",
            Subject = exe,
            Category = "Priorities",
            Impact = new ImpactInfo { Gaming = 0, Basis = "disputed", Effect = ["none"] },
            Risk = Risk.Moderate,
            Hidden = true,
            Preview = true,
            Undocumented = true,
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
            ],
            Sources = ["https://learn.microsoft.com/en-us/windows/win32/procthread/scheduling-priorities"],
        };
    }
}
