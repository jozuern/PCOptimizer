# Creates the ECDSA P-256 key that signs releases for the in-app update (see src/Optimizer.Core/Updates/UpdateSignature.cs).
#
#   pwsh scripts/new-update-key.ps1 [-PrivateKeyFile <path>]
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
    [switch]$Force
)
$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$publicFile = Join-Path $repo "src\Optimizer.Core\Updates\update-key.pem"

if ((Test-Path $PrivateKeyFile) -and -not $Force) { throw "$PrivateKeyFile exists. Use -Force to replace the key." }
# Another -PrivateKeyFile would otherwise replace the committed public key silently, and installed versions would then
# reject every release signed with the old key.
if ((Test-Path $publicFile) -and -not $Force) { throw "$publicFile exists. Use -Force to replace the app's update key." }
$ecdsa = [System.Security.Cryptography.ECDsa]::Create([System.Security.Cryptography.ECCurve+NamedCurves]::nistP256)
try {
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $PrivateKeyFile) | Out-Null
    Set-Content -Path $PrivateKeyFile -Value $ecdsa.ExportPkcs8PrivateKeyPem() -Encoding ascii -NoNewline
    # Only the current user may read the private key.
    $acl = Get-Acl $PrivateKeyFile
    $acl.SetAccessRuleProtection($true, $false)
    $acl.Access | ForEach-Object { [void]$acl.RemoveAccessRule($_) }
    $me = [System.Security.Principal.WindowsIdentity]::GetCurrent().User
    $acl.AddAccessRule([System.Security.AccessControl.FileSystemAccessRule]::new($me, "FullControl", "Allow"))
    Set-Acl $PrivateKeyFile $acl
    Set-Content -Path $publicFile -Value $ecdsa.ExportSubjectPublicKeyInfoPem() -Encoding ascii -NoNewline
} finally {
    $ecdsa.Dispose()
}
"Public key:  $publicFile (commit it)"
"Private key: $PrivateKeyFile (store it as the UPDATE_SIGNING_KEY secret, keep an offline copy, never commit it)"
