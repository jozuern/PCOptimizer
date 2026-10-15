# Signs a release with the maintainer's Certum code signing certificate (SimplySign cloud) and hands it back to the
# release workflow. Run it on the maintainer's PC while the release is a draft (CONTRIBUTING.md, Code signing).
#
#   pwsh scripts/sign-release.ps1 -Version 1.2.3
#
# Before: the release workflow built the tag, attested the build and created a draft release with only
# Tunero-unsigned.exe. SimplySign Desktop is connected (Connect to SimplySign, token from the SimplySign Mobile app), so
# the certificate is in the user's certificate store.
#
# The script downloads Tunero-unsigned.exe from the draft, checks with `gh attestation verify` that the release workflow
# of this repository built it from this tag on GitHub's runners (nothing else gets signed), signs a copy as Tunero.exe
# with SHA-256 and Certum's timestamp, checks the signature and that only the signature was added
# (check-signed-build.ps1), uploads Tunero.exe to the draft and starts the release workflow again on the tag. That run
# waits for the approval of the environment "release", checks the same again, attests the signed exe, adds .sha256 and
# .sig and removes Tunero-unsigned.exe. Then check the draft and publish it.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Repository = "jozuern/Tunero",
    # The certificate's SHA-1 thumbprint, when the store holds more than one Certum code signing certificate.
    [string]$Thumbprint,
    [string]$TimestampUrl = "http://time.certum.pl",
    # Stop after the upload instead of starting the release workflow.
    [switch]$NoDispatch
)
$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 1.2.3." }
$tag = "v$Version"
$check = Join-Path $PSScriptRoot "check-signed-build.ps1"

function Invoke-Native([string]$what) {
    if ($LASTEXITCODE -ne 0) { throw "$what failed (exit code $LASTEXITCODE)." }
}

# gh writes UTF-8. PowerShell reads a program's output with the console's code page (850 on a German Windows), which
# turns the "ü" in the certificate's subject into "Ã¼", and then no certificate matches.
function Read-Gh {
    $saved = [Console]::OutputEncoding
    [Console]::OutputEncoding = [Text.Encoding]::UTF8
    try { gh @args } finally { [Console]::OutputEncoding = $saved }
}

# The draft: still a draft, with the unsigned build and nothing signed yet.
$release = Read-Gh release view $tag --repo $Repository --json "isDraft,assets" | ConvertFrom-Json
Invoke-Native "gh release view $tag"
if (-not $release.isDraft) { throw "$tag is published already: a published release cannot be changed (immutable releases)." }
$names = @($release.assets.name)
if ($names -notcontains "Tunero-unsigned.exe") { throw "The draft $tag has no Tunero-unsigned.exe. Is CODE_SIGNING_SUBJECT set, and did the release workflow finish?" }

# The publisher the release workflow expects: the repository variable that switches signing on.
$expected = (Read-Gh variable get CODE_SIGNING_SUBJECT --repo $Repository)
Invoke-Native "gh variable get CODE_SIGNING_SUBJECT"
$expected = "$expected".Trim()
if (-not $expected) { throw "The repository variable CODE_SIGNING_SUBJECT is empty." }

# The certificate: a code signing certificate in the user's store with that subject, valid today.
$now = Get-Date
$certificates = @(Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Where-Object {
    $_.Subject -eq $expected -and $_.NotBefore -le $now -and $_.NotAfter -gt $now -and
        (-not $Thumbprint -or $_.Thumbprint -eq $Thumbprint.ToUpperInvariant())
})
if ($certificates.Count -eq 0) {
    throw "No valid code signing certificate with the subject '$expected' in Cert:\CurrentUser\My. Connect SimplySign Desktop (Connect to SimplySign) and check the subject: Get-ChildItem Cert:\CurrentUser\My -CodeSigningCert | Format-List Subject, NotAfter, Thumbprint"
}
if ($certificates.Count -gt 1) { throw "More than one certificate matches; choose one with -Thumbprint: $($certificates.Thumbprint -join ', ')" }
$certificate = $certificates[0]
if ($certificate.NotAfter -lt $now.AddDays(30)) { Write-Warning "The certificate expires on $($certificate.NotAfter.ToString('yyyy-MM-dd')): renew it with Certum." }

# signtool from the newest Windows SDK (Visual Studio Installer or the Windows SDK installer, "Windows SDK Signing Tools").
$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
    Sort-Object { [version]$_.Directory.Parent.Name } -ErrorAction SilentlyContinue | Select-Object -Last 1
if (-not $signtool) { throw "signtool.exe not found: install the Windows SDK signing tools (Windows SDK installer, feature 'Windows SDK Signing Tools for Desktop Apps')." }

$folder = Join-Path ([IO.Path]::GetTempPath()) "tunero-sign-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $folder | Out-Null
try {
    gh release download $tag --repo $Repository --pattern "Tunero-unsigned.exe" --dir $folder
    Invoke-Native "gh release download"
    $unsigned = Join-Path $folder "Tunero-unsigned.exe"

    # Only what the release workflow built from this tag on GitHub's runners gets signed.
    gh attestation verify $unsigned --repo $Repository --signer-workflow "$Repository/.github/workflows/release.yml" --source-ref "refs/tags/$tag" --deny-self-hosted-runners
    Invoke-Native "gh attestation verify (Tunero-unsigned.exe was not built by the release workflow from $tag)"

    $exe = Join-Path $folder "Tunero.exe"
    Copy-Item $unsigned $exe
    # /d names the program in the UAC prompt; SimplySign may ask for the card's PIN.
    & $signtool.FullName sign /sha1 $certificate.Thumbprint /fd sha256 /tr $TimestampUrl /td sha256 /d "Tunero" /du "https://tunero.dev" /v $exe
    Invoke-Native "signtool sign"
    & $signtool.FullName verify /pa /tw /v $exe
    Invoke-Native "signtool verify"

    $signature = Get-AuthenticodeSignature $exe
    if ($signature.Status -ne 'Valid') { throw "The signature is not valid: $($signature.Status) $($signature.StatusMessage)" }
    if ($signature.SignerCertificate.Subject -ne $expected) { throw "Signed by '$($signature.SignerCertificate.Subject)', expected '$expected'." }
    if (-not $signature.TimeStamperCertificate) { throw "The signature has no timestamp: it would stop being valid when the certificate expires." }
    & $check -Unsigned $unsigned -Signed $exe

    gh release upload $tag $exe --repo $Repository --clobber
    Invoke-Native "gh release upload"
    "Uploaded the signed Tunero.exe to the draft $tag (SHA-256 $((Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()))."

    if ($NoDispatch) {
        "Next: gh workflow run release.yml --repo $Repository --ref $tag, approve the environment 'release', then check and publish the draft."
    } else {
        gh workflow run release.yml --repo $Repository --ref $tag
        Invoke-Native "gh workflow run release.yml"
        "Started the release workflow on $tag. Approve the environment 'release' (Actions > the run > Review deployments), then check the draft and publish it."
    }
} finally {
    Remove-Item -Recurse -Force $folder -ErrorAction SilentlyContinue
}
