using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Optimizer.Core.Startup;

public enum SignatureStatus { Signed, Unsigned, Invalid, NotFound, Error }

/// <summary>Authenticode result; <see cref="Catalog"/> = signed through a Windows catalog file (most Windows binaries).</summary>
public sealed record SignatureInfo(SignatureStatus Status, string? Publisher, bool Catalog)
{
    public bool IsMicrosoft => Status == SignatureStatus.Signed && Publisher is { } p &&
                               (p.StartsWith("Microsoft Windows", StringComparison.OrdinalIgnoreCase) || p.StartsWith("Microsoft Corporation", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Verifies files with WinVerifyTrust (WINTRUST_ACTION_GENERIC_VERIFY_V2), first the embedded signature, then the
/// system catalogs (CryptCATAdmin), like Autoruns' "Verify code signatures". Revocation is not checked online.
/// </summary>
public static class SignatureVerifier
{
    private static readonly ConcurrentDictionary<string, SignatureInfo> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Guid GenericVerifyV2 = new("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");
    private static readonly Guid DriverActionVerify = new("F750E6C3-38EE-11d1-85E5-00C04FC295EE");

    public static SignatureInfo Verify(string? path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new SignatureInfo(SignatureStatus.NotFound, null, false);
        // Keyed by the file's identity, not only its path, size and write time (which a program replacing the file can
        // keep): the NTFS file id changes when the file is swapped, and the change time when it is written in place.
        // Hashing every file for the key would double the work WinVerifyTrust does anyway.
        string key;
        try
        {
            key = $"{path}|{FileIdentity(path)}";
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new SignatureInfo(SignatureStatus.Error, null, false);
        }
        return Cache.GetOrAdd(key, _ =>
        {
            var p = path;
            try
            {
                var embedded = VerifyEmbedded(p, out var publisher);
                if (embedded == 0) return new SignatureInfo(SignatureStatus.Signed, publisher, false);
                // Not a signable file type (scripts, data): unsigned, not "invalid".
                if (embedded is TrustESubjectFormUnknown or TrustEProviderUnknown) return new SignatureInfo(SignatureStatus.Unsigned, null, false);
                if (embedded != TrustENoSignature) return new SignatureInfo(SignatureStatus.Invalid, publisher, false);
                return VerifyCatalog(p);
            }
            catch (Exception)
            {
                return new SignatureInfo(SignatureStatus.Error, null, false);
            }
        });
    }

    /// <summary>Volume serial, file id, size, last write and change time of the file, read through one handle.</summary>
    private static string FileIdentity(string path)
    {
        using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var handle = stream.SafeFileHandle;
        if (!GetFileInformationByHandle(handle, out var info)) throw new IOException($"cannot read the identity of {path}");
        var basic = new FILE_BASIC_INFO();
        var changed = GetFileInformationByHandleEx(handle, 0 /* FileBasicInfo */, out basic, (uint)Marshal.SizeOf<FILE_BASIC_INFO>()) ? basic.ChangeTime : 0;
        return $"{info.VolumeSerialNumber:X}:{info.FileIndexHigh:X}{info.FileIndexLow:X8}:{((long)info.FileSizeHigh << 32) | info.FileSizeLow}:" +
               $"{((long)info.LastWriteTime.dwHighDateTime << 32) | (uint)info.LastWriteTime.dwLowDateTime}:{changed}";
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime, LastAccessTime, LastWriteTime;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_BASIC_INFO
    {
        public long CreationTime, LastAccessTime, LastWriteTime, ChangeTime;
        public uint FileAttributes;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandle(Microsoft.Win32.SafeHandles.SafeFileHandle handle, out BY_HANDLE_FILE_INFORMATION info);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetFileInformationByHandleEx(Microsoft.Win32.SafeHandles.SafeFileHandle handle, int infoClass, out FILE_BASIC_INFO info, uint size);

    private const int TrustENoSignature = unchecked((int)0x800B0100);
    private const int TrustESubjectFormUnknown = unchecked((int)0x800B0003), TrustEProviderUnknown = unchecked((int)0x800B0001);
    private const uint WtdUiNone = 2, WtdRevokeNone = 0, WtdChoiceFile = 1, WtdChoiceCatalog = 2, WtdStateVerify = 1, WtdStateClose = 2;
    private const uint WtdCacheOnlyUrlRetrieval = 0x1000, WtdDisableMd2Md4 = 0x2000;

    private static int VerifyEmbedded(string path, out string? publisher)
    {
        publisher = null;
        var file = new WINTRUST_FILE_INFO { cbStruct = (uint)Marshal.SizeOf<WINTRUST_FILE_INFO>(), pcwszFilePath = path };
        var filePtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_FILE_INFO>());
        try
        {
            Marshal.StructureToPtr(file, filePtr, false);
            var data = NewData(WtdChoiceFile, filePtr);
            var action = GenericVerifyV2;
            var result = WinVerifyTrust(IntPtr.Zero, ref action, ref data);
            publisher = Publisher(data.hWVTStateData);
            data.dwStateAction = WtdStateClose;
            WinVerifyTrust(IntPtr.Zero, ref action, ref data);
            return result;
        }
        finally
        {
            Marshal.DestroyStructure<WINTRUST_FILE_INFO>(filePtr);
            Marshal.FreeHGlobal(filePtr);
        }
    }

    private static SignatureInfo VerifyCatalog(string path)
    {
        var sha256 = VerifyCatalog(path, "SHA256");
        // Older catalogs list only SHA-1 hashes: a file not found by its SHA-256 hash is looked up again by SHA-1.
        return sha256.Status is SignatureStatus.Unsigned or SignatureStatus.Error ? VerifyCatalog(path, "SHA1") is { Status: SignatureStatus.Signed } sha1 ? sha1 : sha256 : sha256;
    }

    private static SignatureInfo VerifyCatalog(string path, string algorithm)
    {
        var policy = DriverActionVerify;
        if (!CryptCATAdminAcquireContext2(out var admin, IntPtr.Zero, algorithm, IntPtr.Zero, 0))
            return new SignatureInfo(SignatureStatus.Error, null, false);
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var handle = stream.SafeFileHandle.DangerousGetHandle();
            uint size = 0;
            CryptCATAdminCalcHashFromFileHandle2(admin, handle, ref size, null, 0);
            if (size == 0) return new SignatureInfo(SignatureStatus.Unsigned, null, false);
            var hash = new byte[size];
            if (!CryptCATAdminCalcHashFromFileHandle2(admin, handle, ref size, hash, 0)) return new SignatureInfo(SignatureStatus.Unsigned, null, false);

            var catalog = CryptCATAdminEnumCatalogFromHash(admin, hash, size, 0, IntPtr.Zero);
            if (catalog == IntPtr.Zero) return new SignatureInfo(SignatureStatus.Unsigned, null, false);
            try
            {
                var info = new CATALOG_INFO { cbStruct = (uint)Marshal.SizeOf<CATALOG_INFO>() };
                if (!CryptCATCatalogInfoFromContext(catalog, ref info, 0)) return new SignatureInfo(SignatureStatus.Error, null, true);

                var member = Convert.ToHexString(hash);
                var cat = new WINTRUST_CATALOG_INFO
                {
                    cbStruct = (uint)Marshal.SizeOf<WINTRUST_CATALOG_INFO>(),
                    pcwszCatalogFilePath = info.wszCatalogFile,
                    pcwszMemberTag = member,
                    pcwszMemberFilePath = path,
                    hMemberFile = handle,
                    pbCalculatedFileHash = Marshal.AllocHGlobal((int)size),
                    cbCalculatedFileHash = size,
                    hCatAdmin = admin,
                };
                Marshal.Copy(hash, 0, cat.pbCalculatedFileHash, (int)size);
                var catPtr = Marshal.AllocHGlobal(Marshal.SizeOf<WINTRUST_CATALOG_INFO>());
                try
                {
                    Marshal.StructureToPtr(cat, catPtr, false);
                    var data = NewData(WtdChoiceCatalog, catPtr);
                    var action = GenericVerifyV2;
                    var result = WinVerifyTrust(IntPtr.Zero, ref action, ref data);
                    var publisher = result == 0 ? Publisher(data.hWVTStateData) : null;
                    data.dwStateAction = WtdStateClose;
                    WinVerifyTrust(IntPtr.Zero, ref action, ref data);
                    if (result != 0)
                    {
                        // WHQL driver catalogs are signed for driver verification, not code signing: use the driver
                        // policy with a DRIVER_VER_INFO block (zeroed = the running Windows version).
                        var verInfo = Marshal.AllocHGlobal(DriverVerInfoSize);
                        try
                        {
                            for (var i = 0; i < DriverVerInfoSize; i++) Marshal.WriteByte(verInfo, i, 0);
                            Marshal.WriteInt32(verInfo, 0, DriverVerInfoSize);
                            data = NewData(WtdChoiceCatalog, catPtr);
                            data.pPolicyCallbackData = verInfo;
                            action = policy;
                            result = WinVerifyTrust(IntPtr.Zero, ref action, ref data);
                            publisher = Publisher(data.hWVTStateData) ?? NullIfEmpty(Marshal.PtrToStringUni(verInfo + DriverVerSignedByOffset));
                            data.dwStateAction = WtdStateClose;
                            WinVerifyTrust(IntPtr.Zero, ref action, ref data);
                            var signerCert = Marshal.ReadIntPtr(verInfo, DriverVerSignerCertOffset);
                            if (signerCert != IntPtr.Zero) CertFreeCertificateContext(signerCert);
                        }
                        finally
                        {
                            Marshal.FreeHGlobal(verInfo);
                        }
                    }
                    return new SignatureInfo(result == 0 ? SignatureStatus.Signed : SignatureStatus.Invalid, publisher, true);
                }
                finally
                {
                    Marshal.DestroyStructure<WINTRUST_CATALOG_INFO>(catPtr);
                    Marshal.FreeHGlobal(catPtr);
                    Marshal.FreeHGlobal(cat.pbCalculatedFileHash);
                }
            }
            finally
            {
                CryptCATAdminReleaseCatalogContext(admin, catalog, 0);
            }
        }
        finally
        {
            CryptCATAdminReleaseContext(admin, 0);
        }
    }

    private static WINTRUST_DATA NewData(uint choice, IntPtr info) => new()
    {
        cbStruct = (uint)Marshal.SizeOf<WINTRUST_DATA>(),
        dwUIChoice = WtdUiNone,
        fdwRevocationChecks = WtdRevokeNone,
        dwUnionChoice = choice,
        pInfo = info,
        dwStateAction = WtdStateVerify,
        dwProvFlags = WtdCacheOnlyUrlRetrieval | WtdDisableMd2Md4,
    };

    /// <summary>Signer certificate subject (simple name) from the verification state.</summary>
    private static string? Publisher(IntPtr state)
    {
        if (state == IntPtr.Zero) return null;
        var provider = WTHelperProvDataFromStateData(state);
        if (provider == IntPtr.Zero) return null;
        var signer = WTHelperGetProvSignerFromChain(provider, 0, false, 0);
        if (signer == IntPtr.Zero) return null;
        var sgnr = Marshal.PtrToStructure<CRYPT_PROVIDER_SGNR>(signer);
        if (sgnr.csCertChain == 0 || sgnr.pasCertChain == IntPtr.Zero) return null;
        var cert = Marshal.PtrToStructure<CRYPT_PROVIDER_CERT>(sgnr.pasCertChain);
        if (cert.pCert == IntPtr.Zero) return null;
        using var x509 = new X509Certificate2(cert.pCert);
        return x509.GetNameInfo(X509NameType.SimpleName, false);
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    // DRIVER_VER_INFO (softpub.h, x64): cbStruct@0, dwReserved1@8, dwReserved2@16, dwPlatform@24, dwVersion@28,
    // wszVersion[260]@32, wszSignedBy[260]@552, pcSignerCertContext@1072, sOSVersionLow@1080, sOSVersionHigh@1088,
    // dwBuildNumberLow@1096, dwBuildNumberHigh@1100 => 1104 bytes.
    private const int DriverVerInfoSize = 1104, DriverVerSignedByOffset = 552, DriverVerSignerCertOffset = 1072;

    [DllImport("crypt32.dll")] private static extern bool CertFreeCertificateContext(IntPtr context);

    // ---------------- interop (wintrust.h, mscat.h) ----------------

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_FILE_INFO
    {
        public uint cbStruct;
        public string pcwszFilePath;
        public IntPtr hFile;
        public IntPtr pgKnownSubject;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WINTRUST_CATALOG_INFO
    {
        public uint cbStruct;
        public uint dwCatalogVersion;
        public string pcwszCatalogFilePath;
        public string pcwszMemberTag;
        public string pcwszMemberFilePath;
        public IntPtr hMemberFile;
        public IntPtr pbCalculatedFileHash;
        public uint cbCalculatedFileHash;
        public IntPtr pcCatalogContext;
        public IntPtr hCatAdmin;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WINTRUST_DATA
    {
        public uint cbStruct;
        public IntPtr pPolicyCallbackData;
        public IntPtr pSIPClientData;
        public uint dwUIChoice;
        public uint fdwRevocationChecks;
        public uint dwUnionChoice;
        public IntPtr pInfo;
        public uint dwStateAction;
        public IntPtr hWVTStateData;
        public IntPtr pwszURLReference;
        public uint dwProvFlags;
        public uint dwUIContext;
        public IntPtr pSignatureSettings;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CATALOG_INFO
    {
        public uint cbStruct;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string wszCatalogFile;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CRYPT_PROVIDER_SGNR
    {
        public uint cbStruct;
        public System.Runtime.InteropServices.ComTypes.FILETIME sftVerifyAsOf;
        public uint csCertChain;
        public IntPtr pasCertChain;
        public uint dwSignerType;
        public IntPtr psSigner;
        public uint dwError;
        public uint csCounterSigners;
        public IntPtr pasCounterSigners;
        public IntPtr pChainContext;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CRYPT_PROVIDER_CERT
    {
        public uint cbStruct;
        public IntPtr pCert;
    }

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode)]
    private static extern int WinVerifyTrust(IntPtr hwnd, ref Guid action, ref WINTRUST_DATA data);

    [DllImport("wintrust.dll")] private static extern IntPtr WTHelperProvDataFromStateData(IntPtr state);
    [DllImport("wintrust.dll")] private static extern IntPtr WTHelperGetProvSignerFromChain(IntPtr provider, uint signerIndex, bool counterSigner, uint counterSignerIndex);

    [DllImport("wintrust.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CryptCATAdminAcquireContext2(out IntPtr admin, IntPtr subsystem, string? hashAlgorithm, IntPtr strongHashPolicy, uint flags);

    [DllImport("wintrust.dll", SetLastError = true)]
    private static extern bool CryptCATAdminCalcHashFromFileHandle2(IntPtr admin, IntPtr file, ref uint hashSize, byte[]? hash, uint flags);

    [DllImport("wintrust.dll")] private static extern IntPtr CryptCATAdminEnumCatalogFromHash(IntPtr admin, byte[] hash, uint hashSize, uint flags, IntPtr previous);
    [DllImport("wintrust.dll", CharSet = CharSet.Unicode)] private static extern bool CryptCATCatalogInfoFromContext(IntPtr catalog, ref CATALOG_INFO info, uint flags);
    [DllImport("wintrust.dll")] private static extern bool CryptCATAdminReleaseCatalogContext(IntPtr admin, IntPtr catalog, uint flags);
    [DllImport("wintrust.dll")] private static extern bool CryptCATAdminReleaseContext(IntPtr admin, uint flags);
}
