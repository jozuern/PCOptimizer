using System.Reflection;
using System.Runtime.InteropServices;

namespace Optimizer.Core.Platform;

/// <summary>
/// Controls where native DLLs come from in the elevated process. By default .NET looks for a P/Invoke target such as
/// powrprof.dll or nvapi64.dll in the app's own folder before System32, and the app's folder is often Downloads, which
/// the signed-in user (and anything running as that user) can write to. A DLL planted there would run with
/// administrator rights. The guard resolves plain DLL names to System32 or to a trusted folder (the locked folder the
/// single-file exe extracts its WPF libraries to), and refuses a DLL of that name next to the exe.
/// </summary>
public static class NativeLibraryGuard
{
    private const uint LoadLibrarySearchSystem32 = 0x00000800, LoadLibrarySearchUserDirs = 0x00000400;

    private static IReadOnlyList<string> _trusted = [];
    private static string? _appFolder;

    /// <summary>
    /// Installs the guard for every assembly, loaded now or later. <paramref name="trustedFolders"/>: folders besides
    /// System32 that only administrators can write to and that hold the app's own native libraries.
    /// <paramref name="appFolder"/>: the exe's folder, where a library with a resolvable name is refused.
    /// </summary>
    public static void Install(IReadOnlyList<string> trustedFolders, string? appFolder)
    {
        _trusted = trustedFolders;
        _appFolder = appFolder;
        // LoadLibrary calls made by native code (WPF's native part, drivers' helper DLLs) search System32 and the
        // trusted folders instead of the exe's folder and the PATH.
        if (SetDefaultDllDirectories(LoadLibrarySearchSystem32 | LoadLibrarySearchUserDirs))
            foreach (var folder in trustedFolders) AddDllDirectory(folder);
        AppDomain.CurrentDomain.AssemblyLoad += (_, e) => Register(e.LoadedAssembly);
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) Register(assembly);
    }

    private static void Register(Assembly assembly)
    {
        if (assembly.IsDynamic) return;
        try
        {
            NativeLibrary.SetDllImportResolver(assembly, Resolve);
        }
        catch (InvalidOperationException)
        {
            // The assembly has its own resolver.
        }
    }

    private static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (Locate(name, Environment.SystemDirectory, _trusted) is { } path) return NativeLibrary.Load(path);
        if (IsNextToExe(name, _appFolder))
            throw new DllNotFoundException($"{name} next to the exe is not loaded: it is not in System32 or the app's protected folder");
        return IntPtr.Zero; // default search (absolute paths, libraries the runtime links in)
    }

    /// <summary>A plain DLL name ("powrprof.dll", "nvapi64") found in System32 or a trusted folder, in that order.</summary>
    public static string? Locate(string name, string system32, IReadOnlyList<string> trusted)
    {
        if (!IsPlainName(name)) return null;
        foreach (var folder in trusted.Prepend(system32))
        foreach (var candidate in Variants(name))
        {
            var path = Path.Combine(folder, candidate);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    public static bool IsNextToExe(string name, string? appFolder) =>
        appFolder is not null && IsPlainName(name) && Variants(name).Any(v => File.Exists(Path.Combine(appFolder, v)));

    private static bool IsPlainName(string name) =>
        name.Length > 0 && name.IndexOfAny(['\\', '/', ':']) < 0 && name is not ("." or "..");

    private static IEnumerable<string> Variants(string name) =>
        name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ? [name] : [name, name + ".dll"];

    [DllImport("kernel32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetDefaultDllDirectories(uint flags);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr AddDllDirectory(string folder);
}
