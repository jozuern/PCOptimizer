using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using Optimizer.Core.Actions;
using Optimizer.Core.Startup;

namespace Optimizer.Core.Platform;

/// <summary>
/// Installs the downloaded exe into Program Files on the first start (the owner's decision, 2026-10-15): there only
/// administrators can change it, so the reapply task may start it, and Windows lists it under Installed apps. Only a copy
/// signed by Tunero's publisher is installed: the downloaded file sits in a folder the signed-in user can change.
/// </summary>
public static class AppInstall
{
    /// <summary>The Installed apps entry, under HKLM (64-bit view).</summary>
    public const string KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Tunero";

    public const string ExeName = "Tunero.exe";

    /// <summary>
    /// Certificate subjects accepted as Tunero's publisher, exactly as Windows shows them. A renewed certificate with
    /// another subject is added here in the release before it signs one (CONTRIBUTING, Code signing).
    /// </summary>
    public static IReadOnlyList<string> TrustedPublishers { get; } =
    [
        "CN=Open Source Developer Joshua Zürn, O=Open Source Developer, L=Neudenau, S=Baden-Württemberg, C=DE",
    ];

    /// <summary>C:\Program Files\Tunero (the 64-bit Program Files folder).</summary>
    public static string DefaultFolder => Path.Combine(
        Environment.GetEnvironmentVariable("ProgramW6432") is { Length: > 0 } programFiles ? programFiles : Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        "Tunero");

    /// <summary>
    /// The setup screen shows for the published exe, run elevated from a plain start, signed by Tunero's publisher, not a
    /// package manager's copy, not installed already and not declined before.
    /// </summary>
    public static bool ShouldOffer(bool publishedExe, bool elevated, bool plainStart, bool packageCopy, bool declined, bool installedHere,
        bool trustedSignature) =>
        publishedExe && elevated && plainStart && trustedSignature && !packageCopy && !declined && !installedHere;

    /// <summary>A valid embedded signature (not a catalog's) whose certificate subject is one of <see cref="TrustedPublishers"/>.</summary>
    public static bool IsTrusted(SignatureInfo signature, string? subject) =>
        signature is { Status: SignatureStatus.Signed, Catalog: false } && subject is not null && TrustedPublishers.Contains(subject, StringComparer.Ordinal);

    /// <summary>
    /// <see cref="IsTrusted(SignatureInfo, string?)"/> for a file. Check a file only where it cannot change between the two
    /// reads (in the admin-only install folder); for a file in Downloads the answer only decides whether to offer.
    /// </summary>
    public static bool IsTrustedFile(string path)
    {
        var signature = SignatureVerifier.Verify(path);
        if (signature.Status != SignatureStatus.Signed) return false;
        return IsTrusted(signature, SignerSubject(path));
    }

    private static string? SignerSubject(string path)
    {
        try
        {
#pragma warning disable SYSLIB0057 // reads the signer's certificate from the file's signature, not a certificate file
            using var certificate = X509Certificate.CreateFromSignedFile(path);
#pragma warning restore SYSLIB0057
            return certificate.Subject;
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>The values of the Installed apps entry for <paramref name="exe"/>.</summary>
    public static IReadOnlyDictionary<string, object> EntryValues(string exe, string version, long sizeKb) => new Dictionary<string, object>
    {
        ["DisplayName"] = "Tunero",
        ["DisplayVersion"] = version,
        ["Publisher"] = "Joshua Zürn",
        ["DisplayIcon"] = exe,
        ["InstallLocation"] = Path.GetDirectoryName(exe)!,
        ["UninstallString"] = $"\"{exe}\" --uninstall",
        ["URLInfoAbout"] = "https://tunero.dev",
        ["EstimatedSize"] = (int)Math.Clamp(sizeKb, 0, int.MaxValue),
        ["NoModify"] = 1,
        ["NoRepair"] = 1,
    };

    /// <summary>Writes (or refreshes, after an update) the Installed apps entry.</summary>
    public static void WriteEntry(IRegistryRoots registry, string exe, string version, long sizeKb)
    {
        using var key = registry.Open(Hive.Machine, KeyPath, writable: true, create: true)
            ?? throw new IOException($"HKLM\\{KeyPath} could not be created.");
        foreach (var (name, value) in EntryValues(exe, version, sizeKb))
            key.SetValue(name, value, value is int ? RegistryValueKind.DWord : RegistryValueKind.String);
    }

    public static void RemoveEntry(IRegistryRoots registry)
    {
        using var parent = registry.Open(Hive.Machine, Path.GetDirectoryName(KeyPath)!, writable: true);
        parent?.DeleteSubKeyTree(Path.GetFileName(KeyPath), throwOnMissingSubKey: false);
    }

    /// <summary>The exe the entry names, when it exists; null without an entry.</summary>
    public static string? InstalledExe(IRegistryRoots registry)
    {
        using var key = registry.Open(Hive.Machine, KeyPath, writable: false);
        return key?.GetValue("DisplayIcon") is string exe && File.Exists(exe) ? exe : null;
    }
}
