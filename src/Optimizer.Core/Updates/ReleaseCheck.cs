using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Optimizer.Core.Updates;

public enum ReleaseCheckStatus { UpToDate, NewerAvailable, NoRelease, Error }

public sealed record ReleaseCheckResult(ReleaseCheckStatus Status, Version? Latest = null);

/// <summary>
/// Opt-in check for a newer version: asks GitHub for the latest published release of this repository and compares its
/// tag with the running version. Nothing is downloaded or run. The app only links to the release page (built here, never
/// taken from the response), which opens de-elevated in the browser.
/// </summary>
public sealed partial class ReleaseCheck(HttpMessageHandler? handler = null) : IDisposable
{
    public const string Repository = "jozuern/PCOptimizer";
    public const string LatestReleaseUrl = "https://github.com/" + Repository + "/releases/latest";

    private readonly HttpClient _http = new(handler ?? new HttpClientHandler()) { BaseAddress = new Uri("https://api.github.com/"), Timeout = TimeSpan.FromSeconds(15) };

    public async Task<ReleaseCheckResult> CheckAsync(Version current, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"repos/{Repository}/releases/latest");
        // GitHub rejects API requests without a User-Agent.
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PCOptimizer", Normalize(current).ToString(3)));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        try
        {
            using var response = await _http.SendAsync(request, ct);
            // 404: no published release yet (drafts and pre-releases are not "latest").
            if (response.StatusCode == HttpStatusCode.NotFound) return new(ReleaseCheckStatus.NoRelease);
            if (!response.IsSuccessStatusCode) return new(ReleaseCheckStatus.Error);
            return Evaluate(current, await response.Content.ReadAsStringAsync(ct));
        }
        catch (HttpRequestException)
        {
            return new(ReleaseCheckStatus.Error);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new(ReleaseCheckStatus.Error); // timeout
        }
    }

    /// <summary>Reads tag_name ("v0.4.0") from a GitHub release object and compares it with the running version.</summary>
    public static ReleaseCheckResult Evaluate(Version current, string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(ReleaseCheckStatus.Error);
            if (Flag(root, "draft") || Flag(root, "prerelease")) return new(ReleaseCheckStatus.NoRelease);
            if (!root.TryGetProperty("tag_name", out var tag) || ParseTag(tag.ValueKind == JsonValueKind.String ? tag.GetString() : null) is not { } latest)
                return new(ReleaseCheckStatus.Error);
            return new(latest > Normalize(current) ? ReleaseCheckStatus.NewerAvailable : ReleaseCheckStatus.UpToDate, latest);
        }
        catch (JsonException)
        {
            return new(ReleaseCheckStatus.Error);
        }
    }

    /// <summary>"v1.2.3" or "1.2.3"; anything else (suffixes, four parts) is not a release tag of this app.</summary>
    public static Version? ParseTag(string? tag) =>
        tag is not null && TagRegex().Match(tag) is { Success: true } m
            ? new Version(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), int.Parse(m.Groups[3].Value))
            : null;

    /// <summary>Assembly versions have four parts (0.3.0.0); release tags have three.</summary>
    private static Version Normalize(Version v) => new(v.Major, v.Minor, Math.Max(0, v.Build));

    private static bool Flag(JsonElement root, string name) =>
        root.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.True;

    [GeneratedRegex(@"^v?(\d{1,5})\.(\d{1,5})\.(\d{1,5})$")]
    private static partial Regex TagRegex();

    public void Dispose() => _http.Dispose();
}
