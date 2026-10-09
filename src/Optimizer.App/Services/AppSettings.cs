using System.IO;
using System.Text.Json;

namespace Optimizer.App.Services;

/// <summary>UI preferences in %ProgramData%\PCOptimizer\settings.json (app-owned data; elevated profiles differ per account).</summary>
public sealed class AppSettings
{
    public string? Language { get; set; }
    public string Theme { get; set; } = "System";

    /// <summary>Shows Expert tweaks (security trade-offs, boot configuration). Off by default.</summary>
    public bool ExpertMode { get; set; }

    /// <summary>VirusTotal API key, DPAPI-protected for the current user (never stored in clear text).</summary>
    public string? VirusTotalKey { get; set; }

    private static string FilePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "PCOptimizer", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            return File.Exists(FilePath) ? JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new() : new();
        }
        catch (Exception)
        {
            return new();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception)
        {
            // Preferences are a convenience; failing to save them must not break the app.
        }
    }
}
