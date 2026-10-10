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

    // ---------------- Self-update ----------------

    private const string Download = "https://github.com/jozuern/PCOptimizer/releases/download/v1.0.0/";

    private static string ReleaseJson(string exeUrl, string shaUrl, string? sigUrl = Download + "PCOptimizer.exe.sig") => $$"""
        {"tag_name":"v1.0.0","assets":[
          {"name":"PCOptimizer.exe","browser_download_url":"{{exeUrl}}"},
          {{(sigUrl is null ? "" : $$"""{"name":"PCOptimizer.exe.sig","browser_download_url":"{{sigUrl}}"},""")}}
          {"name":"PCOptimizer.exe.sha256","browser_download_url":"{{shaUrl}}"}]}
        """;

    /// <summary>A throwaway release key; the tests never use the project's real key.</summary>
    private static (string PublicPem, Func<byte[], string> Sign) TestKey()
    {
        var key = System.Security.Cryptography.ECDsa.Create(System.Security.Cryptography.ECCurve.NamedCurves.nistP256);
        return (key.ExportSubjectPublicKeyInfoPem(), data => Convert.ToBase64String(key.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256)));
    }

    [Fact]
    public void TheAppCarriesAP256ReleaseKey()
    {
        Assert.True(UpdateSignature.IsConfigured, "src/Optimizer.Core/Updates/update-key.pem is missing (scripts/new-update-key.ps1)");
        using var key = System.Security.Cryptography.ECDsa.Create();
        key.ImportFromPem(UpdateSignature.PublicKeyPem);
        Assert.Equal(256, key.KeySize);
    }

    [Fact]
    public void SignatureBindsKeyVersionAndChecksum()
    {
        var (pem, sign) = TestKey();
        var (otherPem, _) = TestKey();
        var hash = new string('a', 64);
        var version = new Version(1, 2, 3);
        var signature = sign(UpdateSignature.Statement(version, hash));
        // The release workflow signs exactly this text, with the lowercase hash.
        Assert.Equal("PCOptimizer 1.2.3\n" + hash + "\n", System.Text.Encoding.UTF8.GetString(UpdateSignature.Statement(version, hash.ToUpperInvariant())));
        var workflow = File.ReadAllText(Path.Combine(RepoPaths.Root, ".github", "workflows", "release.yml"));
        Assert.Contains("GetBytes(\"PCOptimizer $version`n$hash`n\")", workflow);
        Assert.Contains(".Hash.ToLowerInvariant()", workflow);
        Assert.True(UpdateSignature.Verify(version, hash, signature, pem));
        Assert.True(UpdateSignature.Verify(version, hash.ToUpperInvariant(), signature + "\n", pem));
        Assert.False(UpdateSignature.Verify(version, hash, signature, otherPem));            // another key
        Assert.False(UpdateSignature.Verify(new Version(1, 2, 4), hash, signature, pem));      // an older release offered as newer
        Assert.False(UpdateSignature.Verify(version, new string('b', 64), signature, pem));   // another exe
        Assert.False(UpdateSignature.Verify(version, hash, "not base64", pem));
    }

    [Fact]
    public void AReleaseWithoutSignatureOffersNoSelfUpdate() =>
        Assert.Null(ReleaseCheck.Evaluate(Current, ReleaseJson(Download + "PCOptimizer.exe", Download + "PCOptimizer.exe.sha256", sigUrl: null)).Assets);

    [Fact]
    public void AssetsAreTakenOnlyFromThisRepository()
    {
        var ok = ReleaseCheck.Evaluate(Current, ReleaseJson(Download + "PCOptimizer.exe", Download + "PCOptimizer.exe.sha256"));
        Assert.Equal(Download + "PCOptimizer.exe", ok.Assets?.ExeUrl);
        var elsewhere = ReleaseCheck.Evaluate(Current, ReleaseJson("https://example.com/PCOptimizer.exe", Download + "PCOptimizer.exe.sha256"));
        Assert.Null(elsewhere.Assets);
    }

    [Fact]
    public void ReadsSha256sumFormat()
    {
        var hash = new string('a', 64);
        Assert.Equal(hash, Updater.ParseChecksum($"{hash}  PCOptimizer.exe\n"));
        Assert.Null(Updater.ParseChecksum("not a hash"));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task KeepsTheDownloadOnlyWhenSignedAndTheChecksumMatches(bool matching, bool signedByReleaseKey)
    {
        var exe = "new exe bytes"u8.ToArray();
        var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(matching ? exe : [1, 2, 3])).ToLowerInvariant();
        var release = ReleaseCheck.Evaluate(Current, ReleaseJson(Download + "PCOptimizer.exe", Download + "PCOptimizer.exe.sha256"));
        var (pem, sign) = TestKey();
        var (_, signOther) = TestKey();
        var signature = (signedByReleaseKey ? sign : signOther)(UpdateSignature.Statement(new Version(1, 0, 0), hash));
        var folder = Path.Combine(Path.GetTempPath(), "pco-update-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var updater = new Updater(new RoutingHandler(new()
            {
                [Download + "PCOptimizer.exe"] = exe,
                [Download + "PCOptimizer.exe.sha256"] = System.Text.Encoding.ASCII.GetBytes($"{hash}  PCOptimizer.exe\n"),
                [Download + "PCOptimizer.exe.sig"] = System.Text.Encoding.ASCII.GetBytes(signature),
            }), pem);
            var result = await updater.DownloadAsync(release, folder);
            if (!signedByReleaseKey)
            {
                Assert.Equal(UpdateOutcome.SignatureInvalid, result.Outcome);
                Assert.False(Directory.Exists(folder) && Directory.EnumerateFiles(folder).Any());
            }
            else if (matching)
            {
                Assert.Equal(UpdateOutcome.Ready, result.Outcome);
                Assert.Equal(exe, File.ReadAllBytes(result.FilePath!));
            }
            else
            {
                Assert.Equal(UpdateOutcome.ChecksumMismatch, result.Outcome);
                Assert.Empty(Directory.EnumerateFiles(folder));
            }
        }
        finally
        {
            TestFolders.Delete(folder);
        }
    }

    [Fact]
    public void InstallSwapsTheExeAndCleanUpRemovesTheOldOne()
    {
        var folder = Path.Combine(Path.GetTempPath(), "pco-install-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var running = Path.Combine(folder, "PCOptimizer.exe");
            var downloaded = Path.Combine(folder, "PCOptimizer-1.0.0.exe");
            File.WriteAllText(running, "old");
            File.WriteAllText(downloaded, "new");
            Updater.Install(downloaded, running);
            Assert.Equal("new", File.ReadAllText(running));
            Assert.Equal("old", File.ReadAllText(running + ".old"));
            Updater.CleanUp(running, folder);
            Assert.False(File.Exists(running + ".old"));
            Assert.False(File.Exists(downloaded));
            Assert.True(File.Exists(running));
        }
        finally
        {
            TestFolders.Delete(folder);
        }
    }

    private sealed class RoutingHandler(Dictionary<string, byte[]> files) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(files.TryGetValue(request.RequestUri!.ToString(), out var body)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(body) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
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

    /// <summary>
    /// Every NuGet package the app ships needs its license in the exe and a line in THIRD-PARTY-NOTICES.md: a new
    /// dependency fails here until both are added. Read from the committed lock file, which restore keeps current.
    /// </summary>
    [Fact]
    public void EveryShippedPackageHasItsLicense()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(RepoPaths.App, "packages.lock.json")));
        var listed = Manifest().Where(c => c.Package is not null).ToDictionary(c => c.Package!, c => c.Version, StringComparer.OrdinalIgnoreCase);
        var notices = File.ReadAllText(Path.Combine(RepoPaths.Root, "docs", "THIRD-PARTY-NOTICES.md"));
        var missing = new List<string>();
        foreach (var target in doc.RootElement.GetProperty("dependencies").EnumerateObject())
        foreach (var lib in target.Value.EnumerateObject())
        {
            if (lib.Value.GetProperty("type").GetString() == "Project") continue;
            var (id, version) = (lib.Name, lib.Value.GetProperty("resolved").GetString());
            // runtime.<rid>.* packages carry native files for other platforms; the win-x64 exe does not contain them.
            if (id.StartsWith("runtime.", StringComparison.OrdinalIgnoreCase)) continue;
            if (!listed.TryGetValue(id, out var v) || v != version) missing.Add($"{id} {version} (licenses.json)");
            // The id as a whole name: "WPF-UI" inside "WPF-UI.Abstractions" does not count for WPF-UI.
            if (!System.Text.RegularExpressions.Regex.IsMatch(notices, $@"(?<![\w.-]){System.Text.RegularExpressions.Regex.Escape(id)}(?![\w-]|\.[A-Za-z])",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                missing.Add($"{id} (THIRD-PARTY-NOTICES.md)");
        }
        // The self-contained runtime comes from the SDK that global.json pins, not from a package reference.
        var runtime = $"{Environment.Version.Major}.{Environment.Version.Minor}.{Environment.Version.Build}";
        foreach (var pack in new[] { "Microsoft.NETCore.App", "Microsoft.WindowsDesktop.App" })
            if (listed.GetValueOrDefault(pack) != runtime) missing.Add($"{pack} {runtime} (licenses.json)");
        if (!notices.Contains($"runtime {runtime}", StringComparison.Ordinal)) missing.Add($".NET runtime {runtime} (THIRD-PARTY-NOTICES.md)");
        Assert.Empty(missing);
    }
}
