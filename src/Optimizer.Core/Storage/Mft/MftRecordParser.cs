using System.Buffers.Binary;
using System.Text;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// One $FILE_NAME of a record. <paramref name="ParentReference"/> is the full file reference (record number in the low
/// 48 bits, sequence number in the high 16). Namespace: 0 POSIX, 1 Win32, 2 DOS, 3 Win32 and DOS.
/// </summary>
public sealed record MftName(ulong ParentReference, string Name, byte Namespace);

/// <summary>
/// What the storage scan needs from one in-use MFT file record. <paramref name="BaseReference"/> is 0 for a base record
/// and the base record's file reference for an extension record. Data sizes are null when the record holds no unnamed
/// $DATA with sizes (a folder, or the stream lives in an extension record); resident data occupies no clusters, so its
/// allocated size is 0. <paramref name="LastWriteUtc"/> is default when the record has no valid
/// $STANDARD_INFORMATION time. <paramref name="ExtensionReferences"/> are the other records a resident
/// $ATTRIBUTE_LIST names for $FILE_NAME or unnamed $DATA. <paramref name="ReparseTag"/> is the tag of a resident
/// $REPARSE_POINT in this record, 0 when there is none or it is non-resident.
/// </summary>
public sealed record MftRecord(uint Number, ulong BaseReference, bool IsFolder, IReadOnlyList<MftName> Names, long? DataSize,
    long? DataAllocated, DateTime LastWriteUtc, uint FileAttributes, IReadOnlyList<ulong> ExtensionReferences, uint ReparseTag);

/// <summary>
/// Parses one raw NTFS 3.x file record segment (FILE_RECORD_SEGMENT_HEADER and its attributes). The bytes come from a
/// volume that may be damaged, so every offset and length is checked against the record before use and anything
/// malformed returns null instead of throwing. No LINQ and no allocation per attribute: this runs for every record of a
/// drive.
/// </summary>
public static class MftRecordParser
{
    /// <summary>NTFS protects multisector structures in strides of 512 bytes whatever the sector size (MULTI_SECTOR_HEADER).</summary>
    private const int Stride = 512;
    private const ulong RecordNumberMask = 0x0000_FFFF_FFFF_FFFF;
    private const uint StandardInformation = 0x10, AttributeList = 0x20, FileName = 0x30, Data = 0x80, ReparsePoint = 0xC0,
        End = 0xFFFF_FFFF;
    private const ushort InUse = 0x0001, Directory = 0x0002;
    private const ushort CompressionMask = 0x00FF, Sparse = 0x8000;
    private static readonly long MaxFileTime = DateTime.MaxValue.ToFileTimeUtc();

    /// <summary>
    /// Parses <paramref name="record"/> (exactly one file record) and applies the update sequence fixups in place. Null
    /// when the record has no FILE signature, is not in use, fails the fixups or is malformed. The fixup stride is 512
    /// bytes; <paramref name="bytesPerSector"/> is accepted as the stride only when the record's update sequence count
    /// fits it and not 512.
    /// </summary>
    public static MftRecord? Parse(Span<byte> record, int bytesPerSector, uint number)
    {
        // Header fields up to the base reference (0x20) and the next attribute id (0x28).
        if (record.Length < 0x2A) return null;
        if (record[0] != (byte)'F' || record[1] != (byte)'I' || record[2] != (byte)'L' || record[3] != (byte)'E') return null;
        var flags = U16(record, 0x16);
        if ((flags & InUse) == 0) return null;
        if (!ApplyFixups(record, bytesPerSector)) return null;

        var baseReference = U64(record, 0x20);
        var isFolder = (flags & Directory) != 0;
        List<MftName>? names = null;
        List<ulong>? extensions = null;
        long? dataSize = null, dataAllocated = null;
        DateTime lastWrite = default;
        uint attributes = 0, reparseTag = 0;

        var pos = (int)U16(record, 0x14);
        if (pos < 0x2A) return null; // inside the header
        while (true)
        {
            if (pos > record.Length - 4) return null; // no room left for the end marker
            var type = U32(record, pos);
            if (type == End) break;
            if (pos > record.Length - 16) return null;
            var length = U32(record, pos + 4);
            // A length of 0 would never move on; past the end would read outside the record.
            if (length < 16 || length > (uint)(record.Length - pos)) return null;
            var a = record.Slice(pos, (int)length);
            var nonResident = a[8] != 0;
            var nameLength = a[9];

            if (!nonResident)
            {
                if (a.Length < 24) return null;
                var valueLength = U32(a, 16);
                var valueOffset = U16(a, 20);
                if (valueOffset > a.Length || valueLength > (uint)(a.Length - valueOffset)) return null;
                var value = a.Slice(valueOffset, (int)valueLength);
                switch (type)
                {
                    case StandardInformation:
                        // Last write at 0x08, file attributes at 0x20 (ATTR_STD_INFO).
                        if (value.Length < 0x24) return null;
                        lastWrite = FromFileTime(I64(value, 0x08));
                        attributes = U32(value, 0x20);
                        break;
                    case AttributeList:
                        if (!ReadAttributeList(value, number, ref extensions)) return null;
                        break;
                    case FileName:
                        // FILE_NAME: parent at 0x00, name length (characters) at 0x40, namespace at 0x41, name at 0x42.
                        if (value.Length < 0x42) return null;
                        var chars = value[0x40];
                        if (0x42 + chars * 2 > value.Length) return null;
                        (names ??= new List<MftName>(2)).Add(new MftName(U64(value, 0),
                            Encoding.Unicode.GetString(value.Slice(0x42, chars * 2)), value[0x41]));
                        break;
                    case ReparsePoint:
                        // REPARSE_DATA_BUFFER: the tag, the data length and a reserved word come first.
                        if (value.Length < 8) return null;
                        reparseTag = U32(value, 0);
                        break;
                    case Data when nameLength == 0 && dataSize is null:
                        dataSize = valueLength;
                        dataAllocated = 0;
                        break;
                }
            }
            else
            {
                if (a.Length < 0x40) return null;
                // Sizes are valid only in the segment that starts at VCN 0; named $DATA is an alternate stream.
                if (type == Data && nameLength == 0 && dataSize is null && I64(a, 0x10) == 0)
                {
                    var allocated = I64(a, 0x28);
                    var size = I64(a, 0x30);
                    if (size < 0 || allocated < 0) return null;
                    // Compressed or sparse: the clusters really in use, when the header carries the field before the runs.
                    var attributeFlags = U16(a, 0x0C);
                    if ((attributeFlags & (CompressionMask | Sparse)) != 0 && a.Length >= 0x48 && U16(a, 0x20) >= 0x48)
                    {
                        var total = I64(a, 0x40);
                        if (total < 0) return null;
                        allocated = total;
                    }
                    dataSize = size;
                    dataAllocated = allocated;
                }
            }
            pos += (int)length;
        }

        return new MftRecord(number, baseReference, isFolder, (IReadOnlyList<MftName>?)names ?? [], dataSize, dataAllocated,
            lastWrite, attributes, (IReadOnlyList<ulong>?)extensions ?? [], reparseTag);
    }

    /// <summary>
    /// Checks that every stride ends with the update sequence number, then puts the saved words back. All are checked
    /// before any is replaced, so a torn record is left as read.
    /// </summary>
    private static bool ApplyFixups(Span<byte> record, int bytesPerSector)
    {
        int usaOffset = U16(record, 0x04), usaCount = U16(record, 0x06);
        // The array is the sequence number plus one saved word per stride, and ends before the first stride's last word.
        if ((usaOffset & 1) != 0 || usaCount < 2 || usaOffset < 0x08 || usaOffset + 2 * usaCount > Stride - 2) return false;
        var strides = usaCount - 1;
        int stride;
        if (strides * Stride == record.Length) stride = Stride;
        else if (bytesPerSector > Stride && (long)strides * bytesPerSector == record.Length) stride = bytesPerSector;
        else return false;

        var usn = U16(record, usaOffset);
        for (var i = 1; i <= strides; i++)
            if (U16(record, i * stride - 2) != usn) return false;
        for (var i = 1; i <= strides; i++)
            record.Slice(usaOffset + 2 * i, 2).CopyTo(record.Slice(i * stride - 2, 2));
        return true;
    }

    /// <summary>
    /// ATTRIBUTE_LIST_ENTRY records: type at 0x00, length at 0x04, name length at 0x06, segment reference at 0x10. Keeps
    /// the distinct other records that hold a $FILE_NAME or the unnamed $DATA.
    /// </summary>
    private static bool ReadAttributeList(ReadOnlySpan<byte> list, uint number, ref List<ulong>? extensions)
    {
        var pos = 0;
        while (pos < list.Length)
        {
            if (list.Length - pos < 0x1A) return false;
            int length = U16(list, pos + 4);
            if (length < 0x1A || length > list.Length - pos) return false;
            var type = U32(list, pos);
            var reference = U64(list, pos + 0x10);
            if ((type == FileName || (type == Data && list[pos + 6] == 0)) && (reference & RecordNumberMask) != number)
            {
                extensions ??= new List<ulong>(2);
                if (!extensions.Contains(reference)) extensions.Add(reference);
            }
            pos += length;
        }
        return true;
    }

    private static DateTime FromFileTime(long fileTime) =>
        fileTime > 0 && fileTime <= MaxFileTime ? DateTime.FromFileTimeUtc(fileTime) : default;

    private static ushort U16(ReadOnlySpan<byte> s, int at) => BinaryPrimitives.ReadUInt16LittleEndian(s.Slice(at, 2));
    private static uint U32(ReadOnlySpan<byte> s, int at) => BinaryPrimitives.ReadUInt32LittleEndian(s.Slice(at, 4));
    private static ulong U64(ReadOnlySpan<byte> s, int at) => BinaryPrimitives.ReadUInt64LittleEndian(s.Slice(at, 8));
    private static long I64(ReadOnlySpan<byte> s, int at) => BinaryPrimitives.ReadInt64LittleEndian(s.Slice(at, 8));
}
