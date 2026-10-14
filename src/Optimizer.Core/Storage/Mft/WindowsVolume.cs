using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// A local volume (\\.\C:) opened for reading only: GENERIC_READ, shared for reading and writing so Windows and other
/// programs go on using the drive, without the cache (reads must be sector-aligned). Nothing is ever written through
/// this handle. Opening needs administrator rights; FSCTL_GET_NTFS_VOLUME_DATA fails on anything but NTFS.
/// </summary>
internal sealed unsafe partial class WindowsVolume : IVolumeDevice
{
    private const uint GenericRead = 0x8000_0000, FileShareRead = 0x1, FileShareWrite = 0x2, OpenExisting = 3,
        FileFlagNoBuffering = 0x2000_0000, FileFlagSequentialScan = 0x0800_0000;
    /// <summary>CTL_CODE(FILE_DEVICE_FILE_SYSTEM, 25, METHOD_BUFFERED, FILE_ANY_ACCESS).</summary>
    private const uint FsctlGetNtfsVolumeData = 0x0009_0064;
    private readonly SafeFileHandle _handle;

    public NtfsVolumeData VolumeData { get; }

    private WindowsVolume(SafeFileHandle handle, NtfsVolumeData data) => (_handle, VolumeData) = (handle, data);

    /// <summary>Opens the volume of a drive root such as "C:\".</summary>
    public static WindowsVolume Open(string root)
    {
        if (!VolumeScanners.IsDriveRoot(root)) throw new MftUnavailableException("not a drive root");
        var handle = CreateFileW(@"\\.\" + Path.GetFullPath(root)[..2], GenericRead, FileShareRead | FileShareWrite, IntPtr.Zero,
            OpenExisting, FileFlagNoBuffering | FileFlagSequentialScan, IntPtr.Zero);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            throw new MftUnavailableException($"the volume could not be opened (error {error})");
        }
        try
        {
            // NTFS_VOLUME_DATA_BUFFER is 96 bytes; NTFS_EXTENDED_VOLUME_DATA may follow.
            var buffer = stackalloc byte[128];
            if (!DeviceIoControl(handle, FsctlGetNtfsVolumeData, null, 0, buffer, 128, out var returned, null) || returned < 96)
                throw new MftUnavailableException($"not an NTFS volume (error {Marshal.GetLastPInvokeError()})");
            var b = new ReadOnlySpan<byte>(buffer, 96);
            var data = new NtfsVolumeData(
                BytesPerSector: BinaryPrimitives.ReadInt32LittleEndian(b[40..]),
                BytesPerCluster: BinaryPrimitives.ReadInt32LittleEndian(b[44..]),
                BytesPerFileRecord: BinaryPrimitives.ReadInt32LittleEndian(b[48..]),
                MftStartLcn: BinaryPrimitives.ReadInt64LittleEndian(b[64..]),
                MftValidDataLength: BinaryPrimitives.ReadInt64LittleEndian(b[56..]),
                TotalClusters: BinaryPrimitives.ReadInt64LittleEndian(b[16..]),
                FreeClusters: BinaryPrimitives.ReadInt64LittleEndian(b[24..]));
            return new WindowsVolume(handle, data);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public void Read(long offset, Span<byte> buffer)
    {
        fixed (byte* p = buffer)
        {
            var at = new NativeOverlapped { OffsetLow = (int)offset, OffsetHigh = (int)(offset >> 32) };
            if (!ReadFile(_handle, p, (uint)buffer.Length, out var read, &at))
                throw new MftUnavailableException($"reading the volume failed (error {Marshal.GetLastPInvokeError()})");
            if (read != buffer.Length) throw new MftUnavailableException($"reading the volume returned {read} of {buffer.Length} bytes");
        }
    }

    public void Dispose() => _handle.Dispose();

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    private static partial SafeFileHandle CreateFileW(string fileName, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeviceIoControl(SafeFileHandle device, uint code, void* input, uint inputSize, void* output, uint outputSize,
        out uint returned, NativeOverlapped* overlapped);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ReadFile(SafeFileHandle file, byte* buffer, uint toRead, out uint read, NativeOverlapped* overlapped);
}
