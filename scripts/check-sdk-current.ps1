# Checks that global.json pins the newest released SDK of its feature band (10.0.4xx), from Microsoft's release metadata.
#
#   pwsh scripts/check-sdk-current.ps1 [-WarnOnly]
#
# global.json pins the SDK exactly (rollForward "patch"), so the runtime inside the self-contained exe changes only with
# a commit, never because a build machine has a newer SDK. The other side: Microsoft's monthly runtime fixes, often
# security fixes, reach the exe only once the pin moves (Dependabot's dotnet-sdk pull request, then licenses.json and
# THIRD-PARTY-NOTICES.md). The release workflow runs this and stops; CI runs it with -WarnOnly and shows a warning.
param([switch]$WarnOnly)
$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$pinned = [version](Get-Content -Raw (Join-Path $root "global.json") | ConvertFrom-Json).sdk.version
$channel = "$($pinned.Major).$($pinned.Minor)"
$meta = Invoke-RestMethod "https://builds.dotnet.microsoft.com/dotnet/release-metadata/$channel/releases.json"
$band = [math]::Floor($pinned.Build / 100)
$newest = @($meta.releases | ForEach-Object { $_.sdks } | ForEach-Object { $_.version }) |
    Where-Object { $_ -match '^\d+\.\d+\.\d+$' } | ForEach-Object { [version]$_ } |
    Where-Object { $_.Major -eq $pinned.Major -and $_.Minor -eq $pinned.Minor -and [math]::Floor($_.Build / 100) -eq $band } |
    Sort-Object -Descending | Select-Object -First 1

if (-not $newest) { throw "Microsoft's release metadata lists no SDK $channel.$($band)xx" }
if ($newest -le $pinned) { "SDK $pinned is the newest of its feature band"; exit 0 }

$message = "global.json pins SDK $pinned, Microsoft has released $newest (runtime $($meta.'latest-runtime')): take the dotnet-sdk update and name the new runtime in licenses.json and THIRD-PARTY-NOTICES.md"
if ($WarnOnly) { "::warning::$message"; exit 0 }
Write-Error $message
exit 1
