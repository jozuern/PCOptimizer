namespace Optimizer.Core.Tests;

/// <summary>Source folders of the repository, found by walking up from the test output folder.</summary>
internal static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string App => Path.Combine(Root, "src", "Optimizer.App");

    private static string FindRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "Optimizer.App"))) return dir.FullName;
        throw new DirectoryNotFoundException("Repository root (with src/Optimizer.App) not found above " + AppContext.BaseDirectory);
    }
}
