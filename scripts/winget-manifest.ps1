# Writes the winget manifest of a published release for microsoft/winget-pkgs (package jozuern.PCOptimizer, portable).
#
#   pwsh scripts/winget-manifest.ps1 -Version 0.6.4 [-OutDir artifacts/winget]
#
# Reads the SHA-256 and the date of the GitHub release, writes the four files in the folder layout winget-pkgs expects
# (manifests/j/jozuern/PCOptimizer/<version>/) and validates them with "winget validate". Submit the folder as one pull
# request per version (doc/Authoring.md in winget-pkgs), for example with "wingetcreate submit <folder>".
# No "Commands": winget would rename the exe to the command name; without it the link in WinGet\Links is named
# after the exe, and "pcoptimizer" starts it all the same.
param(
    [Parameter(Mandatory)][string]$Version,
    [string]$OutDir = (Join-Path (Split-Path -Parent $PSScriptRoot) "artifacts\winget")
)
$ErrorActionPreference = "Stop"
if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Version must look like 0.6.4." }

$id = "jozuern.PCOptimizer"
$repo = "https://github.com/jozuern/PCOptimizer"
$tag = "v$Version"
$exeUrl = "$repo/releases/download/$tag/PCOptimizer.exe"
$release = Invoke-RestMethod "https://api.github.com/repos/jozuern/PCOptimizer/releases/tags/$tag"
if ($release.draft) { throw "$tag is still a draft. Publish it first." }
$sha = ((Invoke-RestMethod "$exeUrl.sha256") -split '\s+')[0].ToUpperInvariant()
if ($sha -notmatch '^[0-9A-F]{64}$') { throw "No SHA-256 in $exeUrl.sha256." }
$date = ([datetime]$release.published_at).ToUniversalTime().ToString("yyyy-MM-dd")

$dir = Join-Path $OutDir "manifests\j\jozuern\PCOptimizer\$Version"
New-Item -ItemType Directory -Force $dir | Out-Null
$schema = "1.12.0"
function Write-Manifest($name, $type, $body) {
    $text = "# yaml-language-server: `$schema=https://aka.ms/winget-manifest.$type.$schema.schema.json`n`n" +
        "PackageIdentifier: $id`nPackageVersion: $Version`n" + $body.Trim() + "`nManifestVersion: $schema`n"
    [IO.File]::WriteAllText((Join-Path $dir $name), $text, [Text.UTF8Encoding]::new($false))
}

Write-Manifest "$id.yaml" "version" @"
DefaultLocale: en-US
ManifestType: version
"@

Write-Manifest "$id.installer.yaml" "installer" @"
InstallerType: portable
MinimumOSVersion: 10.0.26100.0
ReleaseDate: $date
Installers:
- Architecture: x64
  InstallerUrl: $exeUrl
  InstallerSha256: $sha
ManifestType: installer
"@

$common = @"
Publisher: Joshua Zürn
PublisherUrl: https://github.com/jozuern
PublisherSupportUrl: $repo/issues
PrivacyUrl: $repo/blob/main/docs/PRIVACY.md
Author: Joshua Zürn
PackageName: PCOptimizer
PackageUrl: $repo
License: GPL-3.0-only
LicenseUrl: $repo/blob/main/LICENSE
Copyright: Copyright (c) 2026 Joshua Zürn
ReleaseNotesUrl: $repo/releases/tag/$tag
"@

Write-Manifest "$id.locale.en-US.yaml" "defaultLocale" @"
PackageLocale: en-US
$common
Moniker: pcoptimizer
ShortDescription: Windows 11 PC optimizer, gaming first, with usage profiles. It explains every check and change with sources, backs up what it changes and can undo it.
Description: |-
  PCOptimizer checks Windows 11 for settings that cost performance, privacy or battery life and offers tweaks with their impact, evidence and risk.
  Every finding and every change comes with an explanation and sources in English and German. Changes are backed up first, verified after applying and can be undone, also after a restart.
  It also covers debloat, cleanup, startup entries, services, NVIDIA driver settings, DNS and a frame time benchmark. It needs administrator rights because it changes system settings.
Tags:
- gaming
- optimizer
- tweaks
- windows-11
- privacy
- debloat
- startup
- services
- cleanup
- performance
InstallationNotes: Start it with pcoptimizer. On the first start PCOptimizer offers an entry in the Start menu, so Windows Search finds it.
ManifestType: defaultLocale
"@

Write-Manifest "$id.locale.de-DE.yaml" "locale" @"
PackageLocale: de-DE
$common
ShortDescription: PC-Optimierer für Windows 11, zuerst für Spiele, mit Nutzungsprofilen. Erklärt jede Prüfung und Änderung mit Quellen, sichert, was es ändert, und kann es rückgängig machen.
Tags:
- spiele
- optimierung
- tweaks
- windows-11
- datenschutz
- debloat
- autostart
- dienste
- bereinigung
InstallationNotes: Starte es mit pcoptimizer. Beim ersten Start bietet PCOptimizer einen Eintrag im Startmenü an, damit die Windows-Suche es findet.
ManifestType: locale
"@

Write-Host "Written: $dir"
if (Get-Command winget -ErrorAction SilentlyContinue) {
    winget validate --manifest $dir
    if ($LASTEXITCODE -ne 0) { throw "winget validate failed." }
}
