# Creates the ECDSA P-256 key that signs releases for the in-app update (see src/Optimizer.Core/Updates/UpdateSignature.cs).
#
#   pwsh scripts/new-update-key.ps1 [-PrivateKeyFile <path>]
#   pwsh scripts/new-update-key.ps1 -Backup -PrivateKeyFile E:\tunero-update-backup-key.pem
#
# -Backup creates the offline backup key instead: its public half goes to update-key-backup.pem (commit it; every
# version then accepts releases signed with either key), its private half only to the path you give, for example a USB
# stick you keep somewhere safe. It is never used while the release key works, never stored as a secret and never
# copied to this PC: write it straight to the offline place. If the release key is ever lost or leaked, store the
# backup key as UPDATE_SIGNING_KEY, release, and create a new backup key.
#
# Writes the public key to src/Optimizer.Core/Updates/update-key.pem (commit it) and the private key to a file
# outside the repository (default: %USERPROFILE%\.pcoptimizer\update-signing-key.pem). Then store the private key as
# a secret of the "release" environment (Settings > Environments > release: deployment tags v*.*.*, required reviewer),
# not as a repository secret, and keep an offline copy:
#
#   gh secret set UPDATE_SIGNING_KEY --env release < "$env:USERPROFILE\.pcoptimizer\update-signing-key.pem"
#
# A new key makes releases signed with the old key fail the check in versions that carry the new public key, and
# versions with the old public key reject releases signed with the new one: change it only when the key is lost or
# leaked, and then tell users to download the next version by hand.
param(
    [string]$PrivateKeyFile = (Join-Path $env:USERPROFILE ".pcoptimizer\update-signing-key.pem"),
    [switch]$Backup,
    [switch]$Force
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$publicFile = Join-Path $repo ("src\Optimizer.Core\Updates\" + ($(if ($Backup) { "update-key-backup.pem" } else { "update-key.pem" })))
if ($Backup -and -not $PSBoundParameters.ContainsKey('PrivateKeyFile')) {
    throw "Give -PrivateKeyFile on the offline drive (for example E:\tunero-update-backup-key.pem): the backup key must not stay on this PC."
}

if ((Test-Path $PrivateKeyFile) -and -not $Force) { throw "$PrivateKeyFile exists. Use -Force to replace the key." }
# Another -PrivateKeyFile would otherwise replace the committed public key silently, and installed versions would then
# reject every release signed with the old key.
if ((Test-Path $publicFile) -and -not $Force) { throw "$publicFile exists. Use -Force to replace the app's update key." }
$ecdsa = [System.Security.Cryptography.ECDsa]::Create([System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $PrivateKeyFile) | Out-Null
    Set-Content -Path $PrivateKeyFile -Value $ecdsa.ExportPkcs8PrivateKeyPem() -Encoding ascii -NoNewline
    # Only the current user may read the private key (a USB stick with FAT32 or exFAT has no permissions to set).
    try {
        $acl = Get-Acl $PrivateKeyFile
        $acl.SetAccessRuleProtection($true, $false)
        $acl.Access | ForEach-Object { [void]$acl.RemoveAccessRule($_) }
        $me = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
        $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new($me, "FullControl", "Allow"))
        Set-Acl $PrivateKeyFile $acl
    } catch {
        Write-Warning "Permissions not set on $PrivateKeyFile ($($_.Exception.Message)); keep the drive itself safe."
    }
    Set-Content -Path $publicFile -Value $ecdsa.ExportSubjectPublicKeyInfoPem() -Encoding ascii -NoNewline
} finally {
    $ecdsa.Dispose()
}
"Public key:  $publicFile (commit it)"
if ($Backup) {
    "Private key: $PrivateKeyFile (the offline backup key: keep it only there, never as a secret, never on this PC, never commit it)"
} else {
    "Private key: $PrivateKeyFile (store it as the UPDATE_SIGNING_KEY secret, keep an offline copy, never commit it)"
}
