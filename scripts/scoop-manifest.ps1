# Writes the Scoop manifest of a published release (app "tunero") for the bucket github.com/jozuern/scoop-bucket.
#
#   pwsh scripts/scoop-manifest.ps1 -Version 0.8.0 [-OutDir artifacts/scoop] [-Sha256 <hash>]
#
# The bucket's own workflow ("Tunero update") writes each new version after checking the release signature; this
# script writes the manifest for a fix by hand. It has no "checkver" or "autoupdate", because Scoop's Excavator would
# take the hash from the release's .sha256 file unchecked. Without -Sha256 the hash comes from verify-release.ps1
# (checked against the release signature). Copy the written tunero.json to bucket/ in the bucket repo.
# Users: scoop bucket add tunero https://github.com/jozuern/scoop-bucket, then scoop install tunero/tunero.
# Scoop creates the Start menu entry (Scoop Apps\Tunero PC Optimizer: Windows Search matches only an entry's name, and
# people search for "optimizer") from "shortcuts" and removes it with the app; the app does not offer its own entry
# for a Scoop copy. No "bin": the exe asks for administrator rights on every start, which a console shim would only get
# in the way of.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$OutDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\scoop"),
    [string]$Sha256
)
$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 0.8.0." }

$repo = "https://github.com/jozuern/Tunero"
$exeUrl = "$repo/releases/download/v$Version/Tunero.exe"
if (-not $Sha256) { $Sha256 = & (Join-Path $PSScriptRoot "verify-release.ps1") -Version $Version }
$Sha256 = $Sha256.ToLowerInvariant()
if ($Sha256 -notmatch '^[0-9a-f]{64}$') { throw "No SHA-256 for $exeUrl." }

# The field order of Scoop's contributing guide.
$manifest = [ordered]@{
    version      = $Version
    description  = "PC optimizer for Windows 11, gaming first. Explains every check and change with sources, backs up what it changes and can undo it."
    homepage     = $repo
    license      = "GPL-3.0-only"
    notes        = @(
        "Tunero asks for administrator rights on every start, because it changes system settings.",
        "Your changes and their backups are kept in %ProgramData%\Tunero. Undo them on the Changes page before 'scoop uninstall tunero', or use Settings > Uninstall in the app, which does both."
    )
    architecture = [ordered]@{
        "64bit" = [ordered]@{ url = $exeUrl; hash = $Sha256 }
    }
    shortcuts    = @(, @("Tunero.exe", "Tunero PC Optimizer"))
    # No checkver or autoupdate: the bucket's Excavator would take any new release's .sha256 unchecked, and Scoop
    # installs what the manifest names. Every release gets a manifest with a hash verified against the signature.
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$path = Join-Path $OutDir "tunero.json"
[IO.File]::WriteAllText($path, ($manifest | ConvertTo-Json -Depth 6) + "`n", [Text.UTF8Encoding]::new($false))
Write-Host "Written: $path"
