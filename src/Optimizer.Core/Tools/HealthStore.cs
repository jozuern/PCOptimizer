using System.Text.Json;
using Optimizer.Core.Logging;

namespace Optimizer.Core.Tools;

/// <summary>Results of on-demand measurements (throttle check, benchmarks) kept between scans in the app's data folder.</summary>
public static class HealthStore
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static string DefaultFolder => Platform.DataPaths.Root;

    public static ThrottleResult? LoadThrottle(string folder)
    {
        var file = Path.Combine(folder, "throttle.json");
        try
        {
            return File.Exists(file) ? JsonSerializer.Deserialize<ThrottleResult>(File.ReadAllText(file)) : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            Log.Warn("health", $"throttle result unreadable: {ex.Message}");
            return null;
        }
    }

    public static void SaveThrottle(string folder, ThrottleResult result)
    {
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "throttle.json"), JsonSerializer.Serialize(result, Json));
    }
}
