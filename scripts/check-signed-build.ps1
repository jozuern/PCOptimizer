# Checks that a signed exe is the unsigned build plus an Authenticode signature and nothing else.
#
#   pwsh scripts/check-signed-build.ps1 -Unsigned Tunero-unsigned.exe -Signed Tunero.exe
#
# Signing on the maintainer's PC (scripts/sign-release.ps1) appends the certificate table to the exe, pads the file to
# 8 bytes before it, and changes two fields of the PE header: the checksum and the security directory entry that points
# to the table. This script checks exactly that: every other byte equals the build that release.yml attested. The
# release workflow runs it before it hashes and attests the signed exe, so a file changed before signing (on that PC or
# on the way) never becomes a release. It does not check the signature itself: Get-AuthenticodeSignature does that.
param(
    [Parameter(Mandatory)][string]$Unsigned,
    [Parameter(Mandatory)][string]$Signed
)
$ErrorActionPreference = "Stop"

function Read-UInt32([byte[]]$bytes, [int]$offset) {
    if ($offset -lt 0 -or $offset + 4 -gt $bytes.Length) { throw "The PE header is cut off." }
    [BitConverter]::ToUInt32($bytes, $offset)
}

# Offsets of the checksum and of the security directory entry (data directory 4) in the PE header.
function Get-PeLayout([byte[]]$bytes, [string]$name) {
    if ($bytes.Length -lt 64 -or $bytes[0] -ne 0x4D -or $bytes[1] -ne 0x5A) { throw "$name is not an exe (no MZ header)." }
    $pe = [int](Read-UInt32 $bytes 0x3C)
    if ((Read-UInt32 $bytes $pe) -ne 0x00004550) { throw "$name has no PE signature." }
    $optional = $pe + 24
    $magic = [BitConverter]::ToUInt16($bytes, $optional)
    $directories = switch ($magic) {
        0x20B { $optional + 112 }   # PE32+
        0x10B { $optional + 96 }    # PE32
        default { throw "$name has an unknown optional header (magic 0x$($magic.ToString('X')))." }
    }
    $count = Read-UInt32 $bytes ($directories - 4)
    if ($count -lt 5) { throw "$name has no security directory." }
    [pscustomobject]@{ Checksum = $optional + 64; Security = $directories + 4 * 8 }
}

$u = [IO.File]::ReadAllBytes((Resolve-Path $Unsigned))
$s = [IO.File]::ReadAllBytes((Resolve-Path $Signed))
$layout = Get-PeLayout $u "The unsigned exe"
$signedLayout = Get-PeLayout $s "The signed exe"
if ($layout.Checksum -ne $signedLayout.Checksum -or $layout.Security -ne $signedLayout.Security) { throw "The PE headers differ." }

if ((Read-UInt32 $u $layout.Security) -ne 0 -or (Read-UInt32 $u ($layout.Security + 4)) -ne 0) { throw "The unsigned exe already carries a signature." }
$tableOffset = [long](Read-UInt32 $s $layout.Security)
$tableSize = [long](Read-UInt32 $s ($layout.Security + 4))
if ($tableOffset -eq 0 -or $tableSize -eq 0) { throw "The signed exe has no signature." }
if ($tableOffset + $tableSize -ne $s.Length) { throw "The signed exe has data after its signature." }
if ($tableOffset -lt $u.Length -or $tableOffset - $u.Length -ge 8) { throw "The signature does not start where the build ends (build $($u.Length) bytes, signature at $tableOffset)." }
for ($i = $u.Length; $i -lt $tableOffset; $i++) { if ($s[$i] -ne 0) { throw "The padding before the signature is not empty." } }

# Everything up to the end of the build is equal, apart from the checksum (4 bytes) and the security entry (8 bytes):
# clear those in both copies and compare hashes (a byte loop takes minutes on a 100 MB exe in PowerShell).
foreach ($bytes in $u, $s) {
    [Array]::Clear($bytes, $layout.Checksum, 4)
    [Array]::Clear($bytes, $layout.Security, 8)
}
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $built = [Convert]::ToHexString($sha.ComputeHash($u, 0, $u.Length))
    $kept = [Convert]::ToHexString($sha.ComputeHash($s, 0, $u.Length))
} finally { $sha.Dispose() }
if ($built -ne $kept) { throw "The signed exe differs from the build, not only by its signature." }
"The signed exe is the build plus its signature ($tableSize bytes)."
