using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Optimizer.Core.Updates;

public enum UpdateOutcome { Ready, ChecksumMismatch, DownloadFailed, NoAssets }

/// <param name="Sha256">Hash of the downloaded exe, checked against the release's SHA-256 file.</param>
public sealed record UpdateDownload(UpdateOutcome Outcome, string? FilePath = null, string? Sha256 = null);

/// <summary>
/// Self-update after the user confirms it: downloads the release exe and its SHA-256 file from this repository's
/// release (links checked by <see cref="ReleaseCheck"/>), keeps the exe only when its hash matches, and swaps it for
/// the running exe. The exe is not code signed yet, so the checksum is the integrity check.
/// </summary>
public sealed partial class Updater(HttpMessageHandler? handler = null) : IDisposable
{
    private readonly HttpClient _http = new(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromMinutes(10) };

    /// <param name="folder">Where the download goes: the app's protected data folder (updates).</param>
    /// <param name="progress">Percent, when the server sends the size.</param>
    public async Task<UpdateDownload> DownloadAsync(ReleaseCheckResult release, string folder, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (release.Assets is not { } assets || release.Latest is not { } version) return new(UpdateOutcome.NoAssets);
        var target = Path.Combine(folder, $"PCOptimizer-{version.ToString(3)}.exe");
        var partial = target + ".partial";
        try
        {
            if (ParseChecksum(await GetStringAsync(assets.ChecksumUrl, ct)) is not { } expected) return new(UpdateOutcome.DownloadFailed);
            Directory.CreateDirectory(folder);
            string actual;
            using (var request = Request(assets.ExeUrl))
            using (var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct))
            {
                response.EnsureSuccessStatusCode();
                var total = response.Content.Headers.ContentLength;
                await using var source = await response.Content.ReadAsStreamAsync(ct);
                await using var file = File.Create(partial);
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                long done = 0;
                int read;
                while ((read = await source.ReadAsync(buffer, ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    hash.AppendData(buffer, 0, read);
                    done += read;
                    if (total > 0) progress?.Report((int)(done * 100 / total.Value));
                }
                actual = Convert.ToHexString(hash.GetHashAndReset());
            }
            if (!actual.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(partial);
                return new(UpdateOutcome.ChecksumMismatch);
            }
            File.Move(partial, target, overwrite: true);
            return new(UpdateOutcome.Ready, target, actual);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException ||
                                   (ex is TaskCanceledException && !ct.IsCancellationRequested))
        {
            TryDelete(partial);
            return new(UpdateOutcome.DownloadFailed);
        }
    }

    /// <summary>"&lt;64 hex&gt;  PCOptimizer.exe" (sha256sum format) or just the hash.</summary>
    public static string? ParseChecksum(string text) =>
        ChecksumRegex().Match(text) is { Success: true } m ? m.Groups[1].Value : null;

    /// <summary>
    /// Puts the new exe in place of the running one. Windows lets a running exe be renamed, so the running one moves to
    /// ".old" (deleted on the next start) and the new one takes its name; on failure the old exe is put back.
    /// </summary>
    public static void Install(string newExe, string runningExe)
    {
        var old = runningExe + ".old";
        if (File.Exists(old)) File.Delete(old);
        File.Move(runningExe, old);
        try
        {
            File.Copy(newExe, runningExe);
        }
        catch
        {
            File.Move(old, runningExe);
            throw;
        }
    }

    /// <summary>
    /// Starts the installed exe only if it still has the checked hash. The exe usually sits in a folder the signed-in
    /// user can write to (Downloads), and the new process inherits this process's administrator rights without a UAC
    /// prompt: the file stays open without write or delete sharing from the hash check until the process has started,
    /// so it cannot be swapped in between. Returns false (nothing started) when the hash differs.
    /// </summary>
    public static bool StartVerified(string exe, string sha256, Action<string> start)
    {
        using var hold = new FileStream(exe, FileMode.Open, FileAccess.Read, FileShare.Read);
        var actual = Convert.ToHexString(SHA256.HashData(hold));
        if (!actual.Equals(sha256, StringComparison.OrdinalIgnoreCase)) return false;
        start(exe);
        return true;
    }

    /// <summary>After an update: removes the previous exe and the downloaded files.</summary>
    public static void CleanUp(string runningExe, string folder)
    {
        TryDelete(runningExe + ".old");
        try
        {
            if (Directory.Exists(folder))
                foreach (var file in Directory.EnumerateFiles(folder, "PCOptimizer-*.exe*")) TryDelete(file);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private async Task<string> GetStringAsync(string url, CancellationToken ct)
    {
        using var request = Request(url);
        using var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static HttpRequestMessage Request(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("PCOptimizer", "update"));
        return request;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [GeneratedRegex(@"^\s*([0-9a-fA-F]{64})\b")]
    private static partial Regex ChecksumRegex();

    public void Dispose() => _http.Dispose();
}
