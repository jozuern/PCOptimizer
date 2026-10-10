using System.Diagnostics;
using Optimizer.Core.Interop;

namespace Optimizer.Core.Platform;

public enum CpuArchitecture { X64, Arm64, Other }

/// <summary>Windows build facts. Read from the registry, never from version-lying APIs.</summary>
public sealed record BuildInfo(
    int Build,
    int Ubr,
    string DisplayVersion,
    string Edition,
    string ProductName,
    CpuArchitecture NativeArchitecture,
    bool FlightingActive,
    string? FlightingDetail)
{
    public string BuildString => $"{Build}.{Ubr}";

    public static BuildInfo Read()
    {
        const string cv = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";
        var build = int.TryParse(Reg.HklmString(cv, "CurrentBuild"), out var b) ? b : Environment.OSVersion.Version.Build;
        var ubr = Reg.HklmInt(cv, "UBR") ?? 0;
        var (flighting, detail) = ReadFlighting();
        return new BuildInfo(
            build,
            ubr,
            Reg.HklmString(cv, "DisplayVersion") ?? "",
            Reg.HklmString(cv, "EditionID") ?? "",
            Reg.HklmString(cv, "ProductName") ?? "",
            ReadArchitecture(),
            flighting,
            detail);
    }

    private static CpuArchitecture ReadArchitecture()
    {
        try
        {
            if (Native.IsWow64Process2(Process.GetCurrentProcess().Handle, out _, out var native))
            {
                return native switch
                {
                    Native.ImageFileMachineAmd64 => CpuArchitecture.X64,
                    Native.ImageFileMachineArm64 => CpuArchitecture.Arm64,
                    _ => CpuArchitecture.Other,
                };
            }
        }
        catch (EntryPointNotFoundException)
        {
        }
        return CpuArchitecture.Other;
    }

    /// <summary>
    /// Insider = any active flighting state. Channel names are never matched (they changed in 2026: Dev/Canary -> Experimental).
    /// On a non-Insider PC (checked on the Gaming PC, 26300.9550) Applicability holds only WNS/UI values.
    /// </summary>
    private static (bool, string?) ReadFlighting()
    {
        const string app = @"SOFTWARE\Microsoft\WindowsSelfHost\Applicability";
        var branch = Reg.HklmString(app, "BranchName");
        var ring = Reg.HklmString(app, "Ring");
        var enable = Reg.HklmInt(app, "EnablePreviewBuilds");
        var content = Reg.HklmString(app, "ContentType");
        // EnablePreviewBuilds = 0 is written when a user leaves the program; it does not mean "Insider".
        var active = (enable is > 0) || (!string.IsNullOrWhiteSpace(branch) && enable is not 0);
        var parts = new[] { branch is null ? null : $"BranchName={branch}", ring is null ? null : $"Ring={ring}",
                            enable is null ? null : $"EnablePreviewBuilds={enable}", content is null ? null : $"ContentType={content}" }
            .Where(p => p is not null);
        var detail = string.Join(", ", parts);
        return (active, detail.Length == 0 ? null : detail);
    }
}

public enum OsGateResult { Supported, SupportedNotValidated, BlockedTooOld, BlockedArchitecture }

/// <summary>OS gate: block below 26100 and non-x64; never block an unknown newer build.</summary>
public static class OsGate
{
    public const int MinimumBuild = 26100;

    /// <summary>Newest build the catalog was validated on. Above it the app runs with a "not validated" note.</summary>
    public const int NewestValidatedBuild = 26300;

    public static OsGateResult Evaluate(int build, CpuArchitecture arch)
    {
        if (arch != CpuArchitecture.X64) return OsGateResult.BlockedArchitecture;
        if (build < MinimumBuild) return OsGateResult.BlockedTooOld;
        return build > NewestValidatedBuild ? OsGateResult.SupportedNotValidated : OsGateResult.Supported;
    }

    /// <summary>24H2 Home/Pro end of updates (info note, no block).</summary>
    public static bool Is24H2EndOfUpdates(int build, string edition, DateOnly today) =>
        build == 26100 && today >= new DateOnly(2026, 10, 13) &&
        (edition.StartsWith("Core", StringComparison.OrdinalIgnoreCase) || edition.StartsWith("Professional", StringComparison.OrdinalIgnoreCase));
}
