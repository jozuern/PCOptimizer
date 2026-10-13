using System.Runtime.CompilerServices;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Tests;

internal static class TestSetup
{
    /// <summary>
    /// Tests keep the log in memory, and the data folder points at a temp folder: nothing may be written into the real
    /// data folder of this PC (its backups, the change lock, the crash report).
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Optimizer.Core.Platform.DataPaths.UseRoot(Path.Combine(Path.GetTempPath(), "tunero-tests-" + Environment.ProcessId));
        Log.DisableFile();
    }
}

/// <summary>
/// Test classes that change a process-wide setting for a moment (ProcessHardening.TempFolder, StreamingProcess.OutputGrace)
/// run alone, after the others: a parallel test would otherwise see the changed value.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class GlobalStateCollection
{
    public const string Name = "Global state";
}
