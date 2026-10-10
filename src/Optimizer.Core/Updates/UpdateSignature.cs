using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Optimizer.Core.Updates;

/// <summary>
/// Proves who published an update. The release workflow signs the statement "PCOptimizer {version}\n{sha256}\n" with
/// the project's ECDSA P-256 key (a repository secret that never reaches the build job) and attaches the signature as
/// PCOptimizer.exe.sig. The app holds only the public key (update-key.pem, embedded). A checksum alone proves that the
/// download is intact, not who made it: anyone who can edit a release could replace the exe and its checksum file.
/// Binding the version in also stops an older signed exe from being offered as a newer release.
/// </summary>
public static class UpdateSignature
{
    private static readonly Lazy<string?> Key = new(LoadKey);

    /// <summary>The embedded public key, or null when this build has none (self-update is then off).</summary>
    public static string? PublicKeyPem => Key.Value;

    public static bool IsConfigured => PublicKeyPem is not null;

    public static byte[] Statement(Version version, string sha256Hex) =>
        Encoding.UTF8.GetBytes($"PCOptimizer {version.ToString(3)}\n{sha256Hex.ToLowerInvariant()}\n");

    /// <summary>The base64 signature (IEEE P1363, SHA-256) over <see cref="Statement"/> made with the key behind <paramref name="publicKeyPem"/>.</summary>
    public static bool Verify(Version version, string sha256Hex, string signatureText, string? publicKeyPem = null)
    {
        publicKeyPem ??= PublicKeyPem;
        if (publicKeyPem is null) return false;
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

    private static string? LoadKey()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Updates.update-key.pem");
        if (stream is null) return null;
        using var reader = new StreamReader(stream);
        var pem = reader.ReadToEnd().Trim();
        return pem.Contains("-----BEGIN PUBLIC KEY-----", StringComparison.Ordinal) ? pem : null;
    }
}
