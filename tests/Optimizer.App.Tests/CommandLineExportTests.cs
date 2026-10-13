using System.IO;
using Optimizer.App.Services;

namespace Optimizer.App.Tests;

public sealed class CommandLineExportTests
{
    /// <summary>A typo in --include stops the export before it reads the PC or writes a file.</summary>
    [Theory]
    [InlineData("apps,drivers")]
    [InlineData("service")]
    public async Task AnUnknownPartStopsTheExport(string include)
    {
        var path = Path.Combine(Path.GetTempPath(), $"tunero-test-{Guid.NewGuid():N}.json");
        Assert.Equal(CommandLineApply.UnknownSection, await CommandLineApply.ExportAsync(path, null!, null, include));
        Assert.False(File.Exists(path));
    }
}
