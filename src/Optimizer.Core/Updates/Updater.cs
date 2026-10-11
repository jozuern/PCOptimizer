using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Optimizer.Core.Platform;

namespace Optimizer.Core.Updates;

public enum UpdateOutcome { Ready, ChecksumMismatch, SignatureInvalid, DownloadFailed, NoAssets }

/// <param name="Sha256">Hash of the downloaded exe, checked against the release's signed SHA-256 file.</param>
public sealed record UpdateDownload(UpdateOutcome Outcome, string? FilePath = null, string? Sha256 = null);

/// <summary>
/// Self-update after the user confirms it: downloads the release exe, its SHA-256 file and the signature from this
/// repository's release (links checked by <see cref="ReleaseCheck"/>). The checksum counts only when the project's key
/// signed it for this version (<see cref="UpdateSignature"/>), and the exe only when it has that checksum; then it is
/// swapped for the running exe.
/// </summary>
/// <param name="publicKeyPem">The key that must have signed the release; null uses the key built into the app.</param>
public sealed partial class Updater(HttpMessageHandler? handler = null, string? publicKeyPem = null) : IDisposable
{
    private readonly HttpClient _http = new(handler ?? new HttpClientHandler()) { Timeout = TimeSpan.FromMinutes(10) };
    private readonly string? _key = publicKeyPem ?? UpdateSignature.PublicKeyPem;

    /// <param name="folder">Where the download goes: the app's protected data folder (updates).</param>
    /// <param name="progress">Percent, when the server sends the size.</param>
    public async Task<UpdateDownload> DownloadAsync(ReleaseCheckResult release, string folder, IProgress<int>? progress = null, CancellationToken ct = default)
    {
        if (release.Assets is not { } assets || release.Latest is not { } version || _key is null) return new(UpdateOutcome.NoAssets);
        var target = Path.Combine(folder, $"PCOptimizer-{version.ToString(3)}.exe");
        var partial = target + ".partial";
        try
        {
            if (ParseChecksum(await GetStringAsync(assets.ChecksumUrl, ct)) is not { } expected) return new(UpdateOutcome.DownloadFailed);
            if (!UpdateSignature.Verify(version, expected, await GetStringAsync(assets.SignatureUrl, ct), _key))
                return new(UpdateOutcome.SignatureInvalid);
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
    /// Puts the new exe in place of the running one. It is first copied next to it as ".new" (a failed copy, a full disk
    /// for example, leaves the running exe untouched); Windows lets a running exe be renamed, so the running one then
    /// moves to ".old" (deleted on the next start) and ".new" takes its name; on failure the old exe is put back. The
    /// folder is usually one the signed-in user can write to (Downloads): it stays held while the files move
    /// (<see cref="SafeDelete.Pin"/>), leftovers are deleted through <see cref="SafeDelete"/>, and ".new" is created new
    /// and checked for its real path, so no step follows a link the user put there.
    /// </summary>
    public static void Install(string newExe, string runningExe)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(runningExe))!;
        using var pin = SafeDelete.Pin(folder) ?? throw new IOException($"{folder} is a link or cannot be held: not updated");
        var old = runningExe + ".old";
        var staged = runningExe + ".new";
        DeleteIfThere(old);
        DeleteIfThere(staged);
        try
        {
            using (var source = File.OpenRead(newExe))
            using (var target = SafeDelete.CreateNew(staged))
                source.CopyTo(target);
        }
        catch
        {
            TryDelete(staged);
            throw;
        }
        File.Move(runningExe, old);
        try
        {
            File.Move(staged, runningExe);
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(runningExe)) SafeDelete.DeleteFile(runningExe);
                File.Move(old, runningExe);
            }
            catch (Exception restore) when (restore is IOException or UnauthorizedAccessException)
            {
                Logging.Log.Error("update", $"the previous exe could not be put back, it is {old}", restore);
            }
            TryDelete(staged);
            Logging.Log.Error("update", "install failed", ex);
            throw;
        }
    }

    private static void DeleteIfThere(string path)
    {
        if (File.Exists(path) || Directory.Exists(path)) SafeDelete.DeleteFile(path);
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

    /// <summary>Deletes without following links (the exe's folder is often one the signed-in user can write to).</summary>
    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) SafeDelete.DeleteFile(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    [GeneratedRegex(@"^\s*([0-9a-fA-F]{64})\b")]
    private static partial Regex ChecksumRegex();

    public void Dispose() => _http.Dispose();
}
