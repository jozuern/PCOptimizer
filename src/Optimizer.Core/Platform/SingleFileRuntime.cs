namespace Optimizer.Core.Platform;

/// <summary>
/// Where the single-file exe extracts its native libraries (the native part of WPF). The .NET host extracts them to
/// %TEMP%\.net unless DOTNET_BUNDLE_EXTRACT_BASE_DIR names another folder, and Microsoft's documentation says that
/// folder must not be writable by users or processes with fewer rights: %TEMP% is writable by every program the
/// signed-in user runs without elevation. The app therefore starts itself once more with the variable pointing to a
/// folder inside its locked data folder, before any of those libraries is loaded.
/// </summary>
public static class SingleFileRuntime
{
    public const string ExtractVariable = "DOTNET_BUNDLE_EXTRACT_BASE_DIR";

    /// <summary>
    /// Set for the second start, so a failed redirect can never loop. It only breaks the loop: the decision itself
    /// depends on the real extraction folder, because any program of the signed-in user can set this variable too.
    /// </summary>
    public const string RestartedVariable = "PCOPTIMIZER_RUNTIME_RESTARTED";

    public static string ExtractBase(string dataRoot) => Path.Combine(dataRoot, Path.GetFileName(DataPaths.Runtime));

    /// <summary>
    /// Continue when this is no single-file exe or it already extracts into <paramref name="wantedBase"/>. Otherwise
    /// restart once; when the folder still differs after that restart (or the variable was set from outside), an
    /// elevated process refuses to start instead of loading native libraries from a folder the user can write to.
    /// </summary>
    public static RuntimeStart Decide(bool isBundle, string? currentExtractBase, string wantedBase, bool restarted, bool elevated)
    {
        if (!isBundle || SameFolder(currentExtractBase, wantedBase)) return RuntimeStart.Continue;
        if (!restarted) return RuntimeStart.Restart;
        return elevated ? RuntimeStart.Refuse : RuntimeStart.Continue;
    }

    /// <summary>What to do when the restart could not be started.</summary>
    public static RuntimeStart AfterFailedRestart(bool elevated) => elevated ? RuntimeStart.Refuse : RuntimeStart.Continue;

    /// <summary>
    /// Folders from the runtime's native search list that lie inside <paramref name="wantedBase"/> (the extraction
    /// folder of this exe when the restart worked).
    /// </summary>
    public static IReadOnlyList<string> TrustedFolders(string? nativeSearchDirectories, string wantedBase)
    {
        var root = Normalize(wantedBase) + Path.DirectorySeparatorChar;
        return (nativeSearchDirectories ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(d => Path.IsPathFullyQualified(d) && (Normalize(d) + Path.DirectorySeparatorChar).StartsWith(root, StringComparison.OrdinalIgnoreCase))
            .Select(Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool SameFolder(string? a, string b) =>
        !string.IsNullOrWhiteSpace(a) && Path.IsPathFullyQualified(a) && string.Equals(Normalize(a), Normalize(b), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
}

public enum RuntimeStart
{
    Continue,
    Restart,
    Refuse,
}
