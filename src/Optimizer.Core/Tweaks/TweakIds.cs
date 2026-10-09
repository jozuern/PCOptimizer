using System.Security.Cryptography;
using System.Text;

namespace Optimizer.Core.Tweaks;

/// <summary>
/// Ids of tweaks built at runtime (startup entries, services, devices, games). The id names the backup file, so two
/// different subjects must never share one: a readable part plus a hash of the full subject.
/// </summary>
public static class TweakIds
{
    /// <summary>
    /// "runuserrun-1a2b3c4d": ASCII letters and digits of <paramref name="subject"/> (shortened to <paramref name="max"/>)
    /// and the first 8 hex digits of the SHA-256 of the whole subject (case-insensitive, as registry and service names are).
    /// </summary>
    public static string Slug(string subject, int max = 32)
    {
        var readable = new string(subject.Where(char.IsAsciiLetterOrDigit).Select(char.ToLowerInvariant).Take(max).ToArray());
        var hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(subject.ToUpperInvariant())))[..8];
        return readable.Length > 0 ? $"{readable}-{hash}" : hash;
    }
}
