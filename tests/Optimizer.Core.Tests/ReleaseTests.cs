using System.Net;
using System.Text.Json;
using Optimizer.Core.Updates;

namespace Optimizer.Core.Tests;

/// <summary>The opt-in release check and the license texts that ship in the exe.</summary>
public class ReleaseTests
{
    private static readonly Version Current = new(0, 3, 0, 0); // assembly versions have four parts

    [Theory]
    [InlineData("v0.4.0", ReleaseCheckStatus.NewerAvailable)]
    [InlineData("0.3.1", ReleaseCheckStatus.NewerAvailable)]
    [InlineData("v0.3.0", ReleaseCheckStatus.UpToDate)]
    [InlineData("v0.2.9", ReleaseCheckStatus.UpToDate)]
    [InlineData("v0.4.0-beta", ReleaseCheckStatus.Error)]
    [InlineData("latest", ReleaseCheckStatus.Error)]
    public void ComparesTagWithRunningVersion(string tag, ReleaseCheckStatus expected) =>
        Assert.Equal(expected, ReleaseCheck.Evaluate(Current, $$"""{"tag_name":"{{tag}}","draft":false,"prerelease":false}""").Status);

    [Fact]
    public void PreReleasesAndBrokenAnswersAreNotOffered()
    {
        Assert.Equal(ReleaseCheckStatus.NoRelease, ReleaseCheck.Evaluate(Current, """{"tag_name":"v9.0.0","prerelease":true}""").Status);
        Assert.Equal(ReleaseCheckStatus.Error, ReleaseCheck.Evaluate(Current, "not json").Status);
        Assert.Equal(ReleaseCheckStatus.Error, ReleaseCheck.Evaluate(Current, """{"tag_name":7}""").Status);
    }

    [Fact]
    public async Task AsksGitHubWithUserAgentAndHandlesMissingRelease()
    {
        var handler = new FakeHandler(HttpStatusCode.NotFound, "{}");
        using var check = new ReleaseCheck(handler);
        Assert.Equal(ReleaseCheckStatus.NoRelease, (await check.CheckAsync(Current)).Status);
        Assert.Equal("https://api.github.com/repos/jozuern/PCOptimizer/releases/latest", handler.Request!.RequestUri!.ToString());
        Assert.Equal("PCOptimizer/0.3.0", handler.Request.Headers.UserAgent.ToString());
    }

    [Fact]
    public async Task NewerReleaseIsReported()
    {
        using var check = new ReleaseCheck(new FakeHandler(HttpStatusCode.OK, """{"tag_name":"v1.0.0","html_url":"https://example.com/elsewhere"}"""));
        var result = await check.CheckAsync(Current);
        Assert.Equal((ReleaseCheckStatus.NewerAvailable, new Version(1, 0, 0)), (result.Status, result.Latest));
        // The link the app opens is fixed, never the URL from the answer.
        Assert.Equal("https://github.com/jozuern/PCOptimizer/releases/latest", ReleaseCheck.LatestReleaseUrl);
    }

    private sealed class FakeHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    // ---------------- Shipped licenses ----------------

    private sealed record Component(string Name, string? Package, string? Version, string License, string Source, List<string> Files);

    private static List<Component> Manifest() =>
        JsonSerializer.Deserialize<List<Component>>(File.ReadAllText(Path.Combine(RepoPaths.App, "Licenses", "licenses.json")),
            new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

    [Fact]
    public void EveryLicenseFileExists()
    {
        foreach (var file in Manifest().SelectMany(c => c.Files).Distinct())
        {
            // PCOptimizer.txt is the repository's LICENSE, embedded under that name.
            var path = file == "PCOptimizer.txt" ? Path.Combine(RepoPaths.Root, "LICENSE") : Path.Combine(RepoPaths.App, "Licenses", file);
            Assert.True(File.Exists(path), $"{file} is listed in licenses.json but missing");
        }
        Assert.Contains("PolyForm Strict License 1.0.0", File.ReadAllText(Path.Combine(RepoPaths.Root, "LICENSE")));
    }

    /// <summary>Every NuGet package the app ships needs its license in the exe: a new dependency fails here until it is added.</summary>
    [Fact]
    public void EveryShippedPackageHasItsLicense()
    {
        var assets = Path.Combine(RepoPaths.App, "obj", "project.assets.json");
        Assert.True(File.Exists(assets), "restore the app first (dotnet restore)");
        using var doc = JsonDocument.Parse(File.ReadAllText(assets));
        var listed = Manifest().Where(c => c.Package is not null).ToDictionary(c => c.Package!, c => c.Version, StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();
        foreach (var lib in doc.RootElement.GetProperty("libraries").EnumerateObject())
        {
            if (lib.Value.GetProperty("type").GetString() != "package") continue;
            var (id, version) = (lib.Name.Split('/')[0], lib.Name.Split('/')[1]);
            // runtime.<rid>.* packages carry native files for other platforms; the win-x64 exe does not contain them.
            if (id.StartsWith("runtime.", StringComparison.OrdinalIgnoreCase)) continue;
            if (!listed.TryGetValue(id, out var v) || v != version) missing.Add($"{id} {version}");
        }
        Assert.Empty(missing);
    }
}
