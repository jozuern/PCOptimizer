using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Optimizer.Core.Startup;

public enum VirusTotalVerdict { Clean, Suspicious, Malicious, NotFound, RateLimited, InvalidKey, Error }

public sealed record VirusTotalResult(string Sha256, VirusTotalVerdict Verdict, int Malicious, int Suspicious, int Total)
{
    /// <summary>The file's public report page.</summary>
    public string ReportUrl => $"https://www.virustotal.com/gui/file/{Sha256}";
}

/// <summary>
/// Opt-in VirusTotal lookup (API v3, GET /files/{sha256}). Only the SHA-256 hash is sent, never the file. Uses the
/// user's own API key; the free key allows 4 lookups per minute, so requests are spaced out.
/// </summary>
public sealed class VirusTotalClient(string apiKey, HttpMessageHandler? handler = null) : IDisposable
{
    public static readonly TimeSpan PublicApiSpacing = TimeSpan.FromSeconds(15.5);

    private readonly HttpClient _http = new(handler ?? new HttpClientHandler()) { BaseAddress = new Uri("https://www.virustotal.com/api/v3/"), Timeout = TimeSpan.FromSeconds(30) };
    private DateTime _last = DateTime.MinValue;

    public TimeSpan Spacing { get; init; } = PublicApiSpacing;

    public static string Sha256(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public async Task<VirusTotalResult> LookupAsync(string sha256, CancellationToken ct = default)
    {
        var wait = _last + Spacing - DateTime.UtcNow;
        if (wait > TimeSpan.Zero) await Task.Delay(wait, ct);
        _last = DateTime.UtcNow;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"files/{sha256}");
        request.Headers.Add("x-apikey", apiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, ct);
        }
        catch (HttpRequestException)
        {
            return new VirusTotalResult(sha256, VirusTotalVerdict.Error, 0, 0, 0);
        }
        using (response)
        {
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotFound: return new VirusTotalResult(sha256, VirusTotalVerdict.NotFound, 0, 0, 0);
                case HttpStatusCode.TooManyRequests: return new VirusTotalResult(sha256, VirusTotalVerdict.RateLimited, 0, 0, 0);
                case HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden: return new VirusTotalResult(sha256, VirusTotalVerdict.InvalidKey, 0, 0, 0);
            }
            if (!response.IsSuccessStatusCode) return new VirusTotalResult(sha256, VirusTotalVerdict.Error, 0, 0, 0);
            return Parse(sha256, await response.Content.ReadAsStringAsync(ct));
        }
    }

    /// <summary>data.attributes.last_analysis_stats: malicious, suspicious, undetected, harmless (+ others).</summary>
    public static VirusTotalResult Parse(string sha256, string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || !data.TryGetProperty("attributes", out var attr) ||
            !attr.TryGetProperty("last_analysis_stats", out var stats))
            return new VirusTotalResult(sha256, VirusTotalVerdict.Error, 0, 0, 0);
        int Get(string name) => stats.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : 0;
        var malicious = Get("malicious");
        var suspicious = Get("suspicious");
        var total = stats.EnumerateObject().Sum(p => p.Value.TryGetInt32(out var n) ? n : 0);
        // A single engine is a common false positive; two or more is worth a look.
        var verdict = malicious >= 2 ? VirusTotalVerdict.Malicious : malicious + suspicious >= 1 ? VirusTotalVerdict.Suspicious : VirusTotalVerdict.Clean;
        return new VirusTotalResult(sha256, verdict, malicious, suspicious, total);
    }

    public void Dispose() => _http.Dispose();
}

/// <summary>Protects secrets (the VirusTotal key) with DPAPI for the current user, so the settings file never holds it in clear text.</summary>
public static class Dpapi
{
    public static string Protect(string secret)
    {
        var data = Encoding.UTF8.GetBytes(secret);
        var input = new DataBlob(data);
        try
        {
            if (!CryptProtectData(ref input, null, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0x1 /* UI_FORBIDDEN */, out var output))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            return Convert.ToBase64String(output.ToArrayAndFree());
        }
        finally
        {
            input.Free();
        }
    }

    public static string? Unprotect(string? protectedBase64)
    {
        if (string.IsNullOrEmpty(protectedBase64)) return null;
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(protectedBase64);
        }
        catch (FormatException)
        {
            return null;
        }
        var input = new DataBlob(bytes);
        try
        {
            return CryptUnprotectData(ref input, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0x1, out var output)
                ? Encoding.UTF8.GetString(output.ToArrayAndFree())
                : null;
        }
        finally
        {
            input.Free();
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int cbData;
        public IntPtr pbData;

        public DataBlob(byte[] data)
        {
            cbData = data.Length;
            pbData = Marshal.AllocHGlobal(Math.Max(1, data.Length));
            Marshal.Copy(data, 0, pbData, data.Length);
        }

        public void Free() => Marshal.FreeHGlobal(pbData);

        public byte[] ToArrayAndFree()
        {
            var b = new byte[cbData];
            Marshal.Copy(pbData, b, 0, cbData);
            LocalFree(pbData);
            return b;
        }
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CryptProtectData(ref DataBlob input, string? description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("crypt32.dll", SetLastError = true)]
    private static extern bool CryptUnprotectData(ref DataBlob input, IntPtr description, IntPtr entropy, IntPtr reserved, IntPtr prompt, int flags, out DataBlob output);

    [DllImport("kernel32.dll")] private static extern IntPtr LocalFree(IntPtr mem);
}
