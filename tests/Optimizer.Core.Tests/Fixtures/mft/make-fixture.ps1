# Makes the NTFS fixture for MftTreeBuilderTests: a 64 MB VHDX formatted NTFS as T:, filled with the content below, its
# raw $MFT (test-volume.mft.gz) and a manifest of every entry from a folder walk (test-volume.json).
#
# RUNS ONLY INSIDE THE HYPER-V TEST VM PCO-Test, NEVER ON A REAL PC: it creates, formats and mounts a disk. From the
# host, over PowerShell Direct (Windows PowerShell 5.1 in the VM, run elevated):
#   $cred = Import-Clixml "$env:LOCALAPPDATA\pco-vm-cred.xml"
#   $s = New-PSSession -VMName PCO-Test -Credential $cred
#   Invoke-Command -Session $s -FilePath tests\Optimizer.Core.Tests\Fixtures\mft\make-fixture.ps1
#   Copy-Item -FromSession $s C:\pco-fixture\test-volume.mft.gz, C:\pco-fixture\test-volume.json -Destination tests\Optimizer.Core.Tests\Fixtures\mft
#   Invoke-Command -Session $s { Remove-Item C:\pco-fixture -Recurse -Force }
# The VHDX is dismounted and deleted before the script ends.

$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -ne 'PCO-TEST') { throw "make-fixture.ps1 runs only inside the test VM PCO-Test, not on $env:COMPUTERNAME." }

$work = 'C:\pco-fixture'
$vhd = Join-Path $work 'test-volume.vhdx'
$letter = 'T'
$drive = "${letter}:"
$root = "${letter}:\"
if (Test-Path $root) { throw "Drive $drive is already in use." }
New-Item -ItemType Directory -Force $work | Out-Null
if (Test-Path $vhd) { throw "$vhd exists from an earlier run: dismount and delete it first." }

Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class FixtureNative
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool DeviceIoControl(SafeFileHandle h, uint code, IntPtr inBuf, int inSize, byte[] outBuf, int outSize, out int returned, IntPtr overlapped);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool GetFileInformationByHandle(SafeFileHandle h, out BY_HANDLE_FILE_INFORMATION info);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern uint GetCompressedFileSizeW(string name, out uint high);

    // FILETIME is two DWORDs: 4-byte packing keeps the longs where the native struct has them.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    struct BY_HANDLE_FILE_INFORMATION
    {
        public uint Attributes; public long Created, Accessed, Written;
        public uint Serial, SizeHigh, SizeLow, Links, IndexHigh, IndexLow;
    }

    const uint GenericRead = 0x80000000, GenericWrite = 0x40000000, ShareAll = 7, OpenExisting = 3, BackupSemantics = 0x02000000;
    const uint FsctlGetNtfsVolumeData = 0x00090064, FsctlLockVolume = 0x00090018, FsctlUnlockVolume = 0x0009001C;

    /// <summary>File ID (record number and sequence) and link count; no reparse point is followed.</summary>
    public static ulong[] FileId(string path)
    {
        using (var h = CreateFileW(path, 0x80 /* FILE_READ_ATTRIBUTES */, ShareAll, IntPtr.Zero, OpenExisting, BackupSemantics | 0x00200000, IntPtr.Zero))
        {
            if (h.IsInvalid) throw new IOException("open " + path, Marshal.GetHRForLastWin32Error());
            BY_HANDLE_FILE_INFORMATION i;
            if (!GetFileInformationByHandle(h, out i)) throw new IOException("info " + path, Marshal.GetHRForLastWin32Error());
            return new ulong[] { ((ulong)i.IndexHigh << 32) | i.IndexLow, i.Links };
        }
    }

    public static long OnDisk(string path)
    {
        uint high;
        uint low = GetCompressedFileSizeW(path, out high);
        if (low == 0xFFFFFFFF && Marshal.GetLastWin32Error() != 0) throw new IOException("size " + path);
        return ((long)high << 32) | low;
    }

    /// <summary>
    /// Locks the volume when it can (which flushes every cached metadata page to disk), reads the $MFT's clusters from
    /// the runs of record 0's unnamed $DATA, cut at the MFT valid data length, and returns the raw bytes and the volume
    /// data.
    /// </summary>
    public static byte[] ReadMft(string volume, out long[] data, out bool locked)
    {
        using (var h = CreateFileW(volume, GenericRead | GenericWrite, ShareAll, IntPtr.Zero, OpenExisting, 0, IntPtr.Zero))
        {
            if (h.IsInvalid) throw new IOException("open " + volume, Marshal.GetHRForLastWin32Error());
            int got;
            // NTFS answers this only while the volume is not locked.
            var v = new byte[128];
            if (!DeviceIoControl(h, FsctlGetNtfsVolumeData, IntPtr.Zero, 0, v, v.Length, out got, IntPtr.Zero))
                throw new IOException("volume data: error " + Marshal.GetLastWin32Error());
            long bytesPerSector = BitConverter.ToUInt32(v, 40), cluster = BitConverter.ToUInt32(v, 44);
            long recordSize = BitConverter.ToUInt32(v, 48), validLength = BitConverter.ToInt64(v, 56);
            long mftStart = BitConverter.ToInt64(v, 64), totalClusters = BitConverter.ToInt64(v, 16);
            data = new long[] { bytesPerSector, cluster, recordSize, validLength, mftStart, totalClusters };

            // Best effort: a lock fails while another process (the indexer, Defender) holds a handle on the volume; the
            // caller flushed the volume before, so the records are on disk either way.
            locked = false;
            for (int attempt = 0; attempt < 20 && !locked; attempt++)
            {
                locked = DeviceIoControl(h, FsctlLockVolume, IntPtr.Zero, 0, null, 0, out got, IntPtr.Zero);
                if (!locked) System.Threading.Thread.Sleep(500);
            }
            try
            {
                // The stream does not own the handle: the unlock below still needs it.
                using (var fs = new FileStream(new SafeFileHandle(h.DangerousGetHandle(), false), FileAccess.Read, 4096, false))
                {
                    var record = new byte[recordSize];
                    fs.Position = mftStart * cluster;
                    ReadExactly(fs, record);
                    ApplyFixups(record);
                    var runs = DataRuns(record);
                    var mft = new MemoryStream();
                    foreach (var run in runs)
                    {
                        var bytes = new byte[run[1] * cluster];
                        fs.Position = run[0] * cluster;
                        ReadExactly(fs, bytes);
                        mft.Write(bytes, 0, bytes.Length);
                    }
                    if (mft.Length < validLength) throw new IOException("runs shorter than the valid data length");
                    mft.SetLength(validLength);
                    return mft.ToArray();
                }
            }
            finally
            {
                if (locked) DeviceIoControl(h, FsctlUnlockVolume, IntPtr.Zero, 0, null, 0, out got, IntPtr.Zero);
            }
        }
    }

    static void ReadExactly(Stream s, byte[] buffer)
    {
        int done = 0;
        while (done < buffer.Length)
        {
            int n = s.Read(buffer, done, buffer.Length - done);
            if (n <= 0) throw new EndOfStreamException();
            done += n;
        }
    }

    static void ApplyFixups(byte[] r)
    {
        int usa = BitConverter.ToUInt16(r, 4), count = BitConverter.ToUInt16(r, 6);
        for (int i = 1; i < count; i++)
        {
            if (r[i * 512 - 2] != r[usa] || r[i * 512 - 1] != r[usa + 1]) throw new IOException("record 0 fixup");
            r[i * 512 - 2] = r[usa + 2 * i]; r[i * 512 - 1] = r[usa + 2 * i + 1];
        }
    }

    /// <summary>LCN and cluster count of each run of record 0's unnamed non-resident $DATA.</summary>
    static System.Collections.Generic.List<long[]> DataRuns(byte[] r)
    {
        int pos = BitConverter.ToUInt16(r, 0x14);
        while (BitConverter.ToUInt32(r, pos) != 0xFFFFFFFF)
        {
            uint type = BitConverter.ToUInt32(r, pos);
            int length = BitConverter.ToInt32(r, pos + 4);
            if (type == 0x80 && r[pos + 8] != 0 && r[pos + 9] == 0)
            {
                var result = new System.Collections.Generic.List<long[]>();
                int p = pos + BitConverter.ToUInt16(r, pos + 0x20);
                long lcn = 0;
                while (r[p] != 0)
                {
                    int lengthSize = r[p] & 0xF, offsetSize = r[p] >> 4;
                    p++;
                    long clusters = 0;
                    for (int i = lengthSize - 1; i >= 0; i--) clusters = (clusters << 8) | r[p + i];
                    p += lengthSize;
                    if (offsetSize == 0) throw new IOException("sparse run in $MFT");
                    long offset = (sbyte)r[p + offsetSize - 1];
                    for (int i = offsetSize - 2; i >= 0; i--) offset = (offset << 8) | r[p + i];
                    p += offsetSize;
                    lcn += offset;
                    result.Add(new long[] { lcn, clusters });
                }
                return result;
            }
            pos += length;
        }
        throw new IOException("no $DATA in record 0");
    }
}
'@

function Write-Bytes([string]$path, [int]$count, [byte]$seed) {
    $bytes = New-Object byte[] $count
    for ($i = 0; $i -lt $count; $i++) { $bytes[$i] = [byte](($i * 31 + $seed) % 251) }
    [IO.File]::WriteAllBytes($path, $bytes)
}

# 1. The volume: a dynamic 64 MB VHDX, MBR, NTFS with 4 KB clusters (1 KB file records by default).
"create vdisk file=`"$vhd`" maximum=64 type=expandable" | Set-Content (Join-Path $work 'create.txt') -Encoding Ascii
diskpart /s (Join-Path $work 'create.txt') | Out-Null
if ($LASTEXITCODE -ne 0) { throw "diskpart failed: $LASTEXITCODE" }
Mount-DiskImage -ImagePath $vhd | Out-Null
try {
    $disk = Get-DiskImage -ImagePath $vhd | Get-Disk
    Initialize-Disk -Number $disk.Number -PartitionStyle MBR
    New-Partition -DiskNumber $disk.Number -UseMaximumSize -DriveLetter $letter | Out-Null
    Format-Volume -DriveLetter $letter -FileSystem NTFS -AllocationUnitSize 4096 -NewFileSystemLabel 'PCOFIXTURE' -Confirm:$false | Out-Null
    # Short (8.3) names on, so long names get a separate DOS name the tree must drop.
    fsutil 8dot3name set "${letter}:" 0 | Out-Null

    # 2. The content (spec section 4).
    $deep = $root
    for ($i = 1; $i -le 12; $i++) { $deep = Join-Path $deep ('level-{0:D2}-folder' -f $i); New-Item -ItemType Directory $deep | Out-Null }
    Write-Bytes (Join-Path $deep 'bottom-of-the-tree.txt') 1500 1
    Set-Content (Join-Path $root 'level-01-folder\level-02-folder\note.txt') 'twelve levels below the root' -Encoding Ascii -NoNewline

    New-Item -ItemType Directory "$drive\links\first", "$drive\links\second", "$drive\links\third" | Out-Null
    Write-Bytes "$drive\links\first\shared-original.dat" 20000 2
    fsutil hardlink create "$drive\links\second\shared-link-two.dat" "$drive\links\first\shared-original.dat" | Out-Null
    fsutil hardlink create "$drive\links\third\SHARED.DAT" "$drive\links\first\shared-original.dat" | Out-Null

    New-Item -ItemType Directory "$drive\streams" | Out-Null
    Write-Bytes "$drive\streams\with-alternate-stream.txt" 300 3
    $stream = New-Object byte[] 4096
    for ($i = 0; $i -lt 4096; $i++) { $stream[$i] = [byte]($i % 256) }
    Set-Content -Path "$drive\streams\with-alternate-stream.txt" -Stream 'extra' -Value $stream -Encoding Byte
    if ((Get-Item "$drive\streams\with-alternate-stream.txt" -Stream extra).Length -ne 4096) { throw 'stream size' }

    New-Item -ItemType Directory "$drive\sparse" | Out-Null
    $sparse = "$drive\sparse\sparse-ten-megabytes.bin"
    New-Item -ItemType File $sparse | Out-Null
    fsutil sparse setflag $sparse | Out-Null
    fsutil file seteof $sparse 10485760 | Out-Null
    fsutil sparse setrange $sparse 0 10485760 | Out-Null
    $chunk = New-Object byte[] 65536
    for ($i = 0; $i -lt 65536; $i++) { $chunk[$i] = [byte](($i * 7 + 5) % 253) }
    $fs = [IO.File]::Open($sparse, 'Open', 'Write')
    try { $fs.Position = 4194304; $fs.Write($chunk, 0, $chunk.Length) } finally { $fs.Dispose() }

    New-Item -ItemType Directory "$drive\compressed" | Out-Null
    compact /c "$drive\compressed" | Out-Null
    $line = [Text.Encoding]::ASCII.GetBytes("The same line again and again compresses well. 0123456789`r`n")
    $text = New-Object byte[] 1048576
    for ($i = 0; $i -lt $text.Length; $i++) { $text[$i] = $line[$i % $line.Length] }
    [IO.File]::WriteAllBytes("$drive\compressed\compressible-one-megabyte.txt", $text)
    compact /c "$drive\compressed\compressible-one-megabyte.txt" | Out-Null

    # 120 names: more $FILE_NAME attributes than one 1 KB record holds, so the file gets an attribute list.
    New-Item -ItemType Directory "$drive\many" | Out-Null
    Write-Bytes "$drive\many\one-file-with-many-names.bin" 50000 4
    for ($i = 1; $i -le 119; $i++) {
        fsutil hardlink create ("$drive\many\another-name-for-the-same-file-{0:D3}.bin" -f $i) "$drive\many\one-file-with-many-names.bin" | Out-Null
    }

    New-Item -ItemType Directory "$drive\junction-target" | Out-Null
    Write-Bytes "$drive\junction-target\reached-through-the-junction.txt" 700 5
    cmd /c mklink /J "$drive\junction-to-target" "$drive\junction-target" | Out-Null

    Write-Bytes "$drive\empty.txt" 0 0
    New-Item -ItemType Directory "$drive\deleted" | Out-Null
    Write-Bytes "$drive\deleted\removed-before-the-copy.txt" 5000 6
    Remove-Item "$drive\deleted\removed-before-the-copy.txt"

    Write-VolumeCache -DriveLetter $letter

    # 3. The manifest: a folder walk that lists links (junctions) but does not enter them. Paths are relative to the root.
    $entries = New-Object System.Collections.Generic.List[object]
    $stack = New-Object System.Collections.Generic.Stack[string]
    $stack.Push($root)
    $unreadable = New-Object System.Collections.Generic.List[string]
    while ($stack.Count -gt 0) {
        $dir = $stack.Pop()
        try { $items = ([IO.DirectoryInfo]$dir).GetFileSystemInfos() }
        catch { $unreadable.Add($dir.Substring($root.Length)); continue }
        foreach ($e in $items) {
            $relative = $e.FullName.Substring($root.Length)
            $attributes = [int]$e.Attributes
            if ($e -is [IO.DirectoryInfo]) {
                $entries.Add([ordered]@{ path = $relative; folder = $true; attributes = $attributes })
                if (($e.Attributes -band [IO.FileAttributes]::ReparsePoint) -eq 0) { $stack.Push($e.FullName) }
            } else {
                $id = [FixtureNative]::FileId($e.FullName)
                $entries.Add([ordered]@{
                    path = $relative; folder = $false; size = $e.Length; onDisk = [FixtureNative]::OnDisk($e.FullName)
                    fileId = ('0x{0:X16}' -f $id[0]); links = [int]$id[1]; attributes = $attributes })
            }
        }
    }

    # fsutil and compact do not stop the script on failure: check what they should have made.
    $byPath = @{}; foreach ($e in $entries) { $byPath[$e.path] = $e }
    if ($byPath['many\one-file-with-many-names.bin'].links -ne 120) { throw "120 links expected, found $($byPath['many\one-file-with-many-names.bin'].links)" }
    if ($byPath['links\third\SHARED.DAT'].links -ne 3) { throw "3 links expected, found $($byPath['links\third\SHARED.DAT'].links)" }
    if ($byPath['sparse\sparse-ten-megabytes.bin'].onDisk -ne 65536) { throw 'sparse file: 64 KB on disk expected' }
    if ($byPath['compressed\compressible-one-megabyte.txt'].onDisk -ge 1048576) { throw 'compressed file not compressed' }
    if (($byPath['junction-to-target'].attributes -band 0x400) -eq 0) { throw 'junction expected' }

    # 4. The raw $MFT, read after the flush above, through the volume locked when nothing else holds it.
    $data = $null; $locked = $false
    $mft = [FixtureNative]::ReadMft("\\.\$drive", [ref]$data, [ref]$locked)
    $gz = Join-Path $work 'test-volume.mft.gz'
    $out = [IO.File]::Create($gz)
    try {
        $zip = New-Object IO.Compression.GZipStream($out, [IO.Compression.CompressionLevel]::Optimal)
        try { $zip.Write($mft, 0, $mft.Length) } finally { $zip.Dispose() }
    } finally { $out.Dispose() }

    $os = Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion'
    $manifest = [ordered]@{
        made = [ordered]@{
            by = 'tests/Optimizer.Core.Tests/Fixtures/mft/make-fixture.ps1 in the Hyper-V VM PCO-Test'
            date = (Get-Date).ToString('yyyy-MM-dd')
            windows = ('{0} build {1}.{2}' -f $os.DisplayVersion, $os.CurrentBuildNumber, $os.UBR)
            volume = '64 MB dynamic VHDX, MBR, NTFS, 8.3 names on'
            bytesPerSector = $data[0]; bytesPerCluster = $data[1]; bytesPerFileRecord = $data[2]
            mftValidDataLength = $data[3]; mftStartLcn = $data[4]; totalClusters = $data[5]
            volumeLockedWhileRead = $locked
        }
        # Folders the walk could not open (System Volume Information): the test leaves them out of the comparison.
        unreadable = @($unreadable)
        deleted = @('deleted\removed-before-the-copy.txt')
        entries = $entries
    }
    $manifest | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $work 'test-volume.json') -Encoding UTF8
    "MFT bytes: $($mft.Length), gzip bytes: $((Get-Item $gz).Length), entries: $($entries.Count)"
}
finally {
    # 5. Clean up: the VHDX is dismounted and deleted whatever happened above.
    Dismount-DiskImage -ImagePath $vhd | Out-Null
    Remove-Item $vhd -Force
    Remove-Item (Join-Path $work 'create.txt') -Force -ErrorAction SilentlyContinue
}
