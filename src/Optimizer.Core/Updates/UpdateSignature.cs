using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Optimizer.Core.Updates;

/// <summary>
/// Proves who published an update. The release workflow signs the statement "PCOptimizer {version}\n{sha256}\n" with
/// the project's ECDSA P-256 key (a repository secret that never reaches the build job) and attaches the signature as
/// Tunero.exe.sig. The app holds only the public key (update-key.pem, embedded). A checksum alone proves that the
/// download is intact, not who made it: anyone who can edit a release could replace the exe and its checksum file.
/// Binding the version in also stops an older signed exe from being offered as a newer release.
/// <para>
/// A second public key (update-key-backup.pem) belongs to a key pair kept offline and never used while the release key
/// works: if the release key is ever lost or leaked, releases signed with the backup key still reach every installed
/// copy. A key can only be added for copies that already carry it, which is why both ship in every version.
/// </para>
/// </summary>
public static class UpdateSignature
{
    private static readonly Lazy<string?> ReleaseKey = new(() => LoadKey("Updates.update-key.pem"));
    private static readonly Lazy<IReadOnlyList<string>> Keys = new(() =>
        new[] { ReleaseKey.Value, LoadKey("Updates.update-key-backup.pem") }.OfType<string>().ToList());

    /// <summary>The embedded release key, or null when this build has none (self-update is then off).</summary>
    public static string? PublicKeyPem => ReleaseKey.Value;

    /// <summary>Every key a release may be signed with: the release key and the offline backup key.</summary>
    public static IReadOnlyList<string> PublicKeyPems => Keys.Value;

    public static bool IsConfigured => Keys.Value.Count > 0;

    public static byte[] Statement(Version version, string sha256Hex) =>
        Encoding.UTF8.GetBytes($"PCOptimizer {version.ToString(3)}\n{sha256Hex.ToLowerInvariant()}\n");

    /// <summary>
    /// The base64 signature (IEEE P1363, SHA-256) over <see cref="Statement"/> made with the key behind
    /// <paramref name="publicKeyPem"/>, or, without it, with one of the embedded keys.
    /// </summary>
    public static bool Verify(Version version, string sha256Hex, string signatureText, string? publicKeyPem = null) =>
        publicKeyPem is not null
            ? VerifyWith(version, sha256Hex, signatureText, publicKeyPem)
            : Keys.Value.Any(key => VerifyWith(version, sha256Hex, signatureText, key));

    private static bool VerifyWith(Version version, string sha256Hex, string signatureText, string publicKeyPem)
    {
        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(signatureText.Trim());
        }
        catch (FormatException)
        {
            return false;
        }
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportFromPem(publicKeyPem);
            if (ecdsa.KeySize != 256) return false;
            return ecdsa.VerifyData(Statement(version, sha256Hex), signature, HashAlgorithmName.SHA256);
        }
        catch (Exception ex) when (ex is ArgumentException or CryptographicException)
        {
            return false;
        }
    }

    private static string? LoadKey(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        var pem = reader.ReadToEnd().Trim();
        return pem.Contains("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal) ? pem : null;
    }
}
