using System.IO;
using System.Runtime.CompilerServices;
using Optimizer.Core.Logging;

namespace Optimizer.App.Tests;

internal static class TestSetup
{
    /// <summary>
    /// As in Optimizer.Core.Tests: the log stays in memory and the data folder points at a temp folder, so a test never
    /// writes session logs into the real data folder of this PC (where pruning would delete the user's older logs).
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        Optimizer.Core.Platform.DataPaths.UseRoot(Path.Combine(Path.GetTempPath(), "tunero-app-tests-" + Environment.ProcessId));
        Log.DisableFile();
    }
}
