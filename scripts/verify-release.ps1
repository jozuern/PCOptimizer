# Checks a published release the way the app's updater does, and prints the exe's SHA-256 (lowercase) when it passes.
#
#   pwsh scripts/verify-release.ps1 -Version 0.8.2
#
# Downloads Tunero.exe, Tunero.exe.sha256 and Tunero.exe.sig of the release, hashes the exe itself, compares with the
# .sha256 file and checks that one of the app's keys (src/Optimizer.Core/Updates/update-key*.pem: the release key and
# the offline backup key) signed exactly
# "PCOptimizer <version>\n<sha256>\n" (UpdateSignature.Statement, also used by release.yml, winget.yml and
# chocolatey.yml). The package scripts (chocolatey-package.ps1, scoop-manifest.ps1, winget-manifest.ps1) take the hash
# from here when none is given: a .sha256 file alone proves nothing, whoever can replace the exe can replace it too.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$Repository = "jozuern/Tunero",
    [string]$KeyFolder = (Join-Path (Split-Path -Parent $PSScriptRoot) "src\Optimizer.Core\Updates")
)
$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 0.8.2." }

$base = "https://github.com/$Repository/releases/download/v$Version/Tunero.exe"
$folder = Join-Path ([IO.Path]::GetTempPath()) "tunero-verify-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory $folder | Out-Null
try {
    $exe = Join-Path $folder "Tunero.exe"
    foreach ($suffix in "", ".sha256", ".sig") { Invoke-WebRequest "$base$suffix" -OutFile "$exe$suffix" -UseBasicParsing }
    $listed = ((Get-Content -Raw "$exe.sha256") -split '\s+')[0].ToLowerInvariant()
    $signature = [Convert]::FromBase64String((Get-Content -Raw "$exe.sig").Trim())
    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -ne $listed) { throw "Tunero.exe has SHA-256 $hash, the release lists $listed." }
    $statement = [Text.Encoding]::UTF8.GetBytes("PCOptimizer $Version`n$hash`n")
    $matched = $false
    foreach ($pem in Get-ChildItem $KeyFolder -Filter 'update-key*.pem') {
        $key = [Security.Cryptography.ECDsa]::Create()
        try {
            $key.ImportFromPem((Get-Content -Raw $pem.FullName))
            if ($key.VerifyData($statement, $signature, [Security.Cryptography.HashAlgorithmName]::SHA256)) { $matched = $true }
        } finally { $key.Dispose() }
    }
    if (-not $matched) { throw "The release signature matches none of the app's update keys for v$Version." }
    $hash
} finally {
    Remove-Item $folder -Recurse -Force -ErrorAction SilentlyContinue
}
