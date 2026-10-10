using System.Runtime.CompilerServices;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Tests;

internal static class TestSetup
{
    /// <summary>Tests keep the log in memory: nothing may be written into the real data folder of this PC.</summary>
    [ModuleInitializer]
    internal static void Initialize() => Log.DisableFile();
}
