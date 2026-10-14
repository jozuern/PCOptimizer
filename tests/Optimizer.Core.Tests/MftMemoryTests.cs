using System.Buffers.Binary;
using System.Text;
using Optimizer.Core.Storage;
using Optimizer.Core.Storage.Mft;
using Xunit.Abstractions;

namespace Optimizer.Core.Tests;

/// <summary>Runs alone: the managed heap is measured, and other tests allocating meanwhile would count.</summary>
[CollectionDefinition("Memory", DisableParallelization = true)]
public sealed class MemoryCollection;

/// <summary>
/// What the file table reader keeps in memory per entry, on a file table made in memory (no volume opened): 1,000
/// folders of 499 files each, every name 20 characters, the size of a typical drive's names. A drive's $MFT also holds
/// free records; each costs only its 4-byte slot.
/// </summary>
[Collection("Memory")]
public class MftMemoryTests(ITestOutputHelper output)
{
    private const int Folders = 1000, FilesPerFolder = 499, Entries = Folders * (1 + FilesPerFolder), FirstUser = 24;

    /// <summary>Writes each record when it is read: the root (5), the folders, then their files; records 0 to 23 else unused.</summary>
    private sealed class GeneratedSource : IMftSource
    {
        public int BytesPerRecord => 1024;
        public int BytesPerSector => 512;
        public long RecordCount => FirstUser + Entries;

        public int Read(long firstRecord, Span<byte> buffer)
        {
            var n = (int)Math.Min(buffer.Length / 1024, RecordCount - firstRecord);
            buffer[..(n * 1024)].Clear();
            for (var k = 0; k < n; k++)
            {
                var number = firstRecord + k;
                var r = buffer.Slice(k * 1024, 1024);
                if (number == 5) Write(r, folder: true, parent: 5, ".", 0);
                else if (number >= FirstUser && number < FirstUser + Folders)
                    Write(r, folder: true, parent: 5, $"folder-{number:D6}-name", 0);
                else if (number >= FirstUser + Folders)
                {
                    var file = number - FirstUser - Folders;
                    Write(r, folder: false, parent: (ulong)(FirstUser + file / FilesPerFolder), $"document-{file:D7}.txt", 4096 + file);
                }
            }
            return n;
        }

        public void Dispose() { }

        /// <summary>A FILE record with $STANDARD_INFORMATION, a Win32 $FILE_NAME and, for a file, a non-resident $DATA; fixups applied.</summary>
        private static void Write(Span<byte> r, bool folder, ulong parent, string name, long size)
        {
            "FILE"u8.CopyTo(r);
            BinaryPrimitives.WriteUInt16LittleEndian(r[0x04..], 0x30);
            BinaryPrimitives.WriteUInt16LittleEndian(r[0x06..], 3);
            BinaryPrimitives.WriteUInt16LittleEndian(r[0x10..], 1);
            BinaryPrimitives.WriteUInt16LittleEndian(r[0x14..], 0x38);
            BinaryPrimitives.WriteUInt16LittleEndian(r[0x16..], (ushort)(folder ? 3 : 1));
            BinaryPrimitives.WriteUInt32LittleEndian(r[0x1C..], 1024);
            var pos = 0x38;

            var si = r[pos..];
            BinaryPrimitives.WriteUInt32LittleEndian(si, 0x10);
            BinaryPrimitives.WriteUInt32LittleEndian(si[4..], 0x60);
            BinaryPrimitives.WriteUInt32LittleEndian(si[16..], 0x48);
            BinaryPrimitives.WriteUInt16LittleEndian(si[20..], 24);
            BinaryPrimitives.WriteInt64LittleEndian(si[(24 + 8)..], new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc).ToFileTimeUtc());
            BinaryPrimitives.WriteUInt32LittleEndian(si[(24 + 0x20)..], folder ? 0x10u : 0x20u);
            pos += 0x60;

            var valueLength = 0x42 + name.Length * 2;
            var length = (24 + valueLength + 7) & ~7;
            var fn = r[pos..];
            BinaryPrimitives.WriteUInt32LittleEndian(fn, 0x30);
            BinaryPrimitives.WriteUInt32LittleEndian(fn[4..], (uint)length);
            BinaryPrimitives.WriteUInt32LittleEndian(fn[16..], (uint)valueLength);
            BinaryPrimitives.WriteUInt16LittleEndian(fn[20..], 24);
            BinaryPrimitives.WriteUInt64LittleEndian(fn[24..], 1UL << 48 | parent);
            fn[24 + 0x40] = (byte)name.Length;
            fn[24 + 0x41] = 1; // Win32
            Encoding.Unicode.GetBytes(name, fn[(24 + 0x42)..]);
            pos += length;

            if (!folder)
            {
                var data = r[pos..];
                BinaryPrimitives.WriteUInt32LittleEndian(data, 0x80);
                BinaryPrimitives.WriteUInt32LittleEndian(data[4..], 0x48);
                data[8] = 1;
                BinaryPrimitives.WriteUInt16LittleEndian(data[0x20..], 0x40);
                BinaryPrimitives.WriteInt64LittleEndian(data[0x28..], (size + 4095) / 4096 * 4096);
                BinaryPrimitives.WriteInt64LittleEndian(data[0x30..], size);
                BinaryPrimitives.WriteInt64LittleEndian(data[0x38..], size);
                pos += 0x48;
            }
            BinaryPrimitives.WriteUInt32LittleEndian(r[pos..], 0xFFFF_FFFF);
            BinaryPrimitives.WriteUInt32LittleEndian(r[0x18..], (uint)(pos + 8));

            BinaryPrimitives.WriteUInt16LittleEndian(r[0x30..], 1); // update sequence number
            for (var i = 1; i <= 2; i++)
            {
                r.Slice(i * 512 - 2, 2).CopyTo(r[(0x30 + 2 * i)..]);
                BinaryPrimitives.WriteUInt16LittleEndian(r[(i * 512 - 2)..], 1);
            }
        }
    }

    [Fact]
    public void TwoMillionEntriesStayUnder250MegabytesInTheTable()
    {
        var before = GC.GetTotalMemory(true);
        var table = MftScanner.ReadTable(new GeneratedSource(), 4 << 20, null, CancellationToken.None);
        var withTable = GC.GetTotalMemory(true);
        Assert.Equal(Entries + 1, table.Count);

        var tree = MftTreeBuilder.Build(table, @"X:\", null);
        var withBoth = GC.GetTotalMemory(true);
        table = null!;
        var withTree = GC.GetTotalMemory(true);
        Assert.Equal(Entries + 1, tree.Count);
        Assert.Equal(Folders * FilesPerFolder, tree.Files(tree.Root));

        double tablePerEntry = (double)(withTable - before) / Entries, treePerEntry = (double)(withTree - before) / Entries,
            bothPerEntry = (double)(withBoth - before) / Entries;
        // A 20-character name is a string of 64 bytes (header, length, characters and terminator, rounded to 8).
        output.WriteLine($"table {tablePerEntry:0} B per entry ({tablePerEntry * 2_000_000 / (1 << 20):0} MB for 2 million), " +
            $"tree {treePerEntry:0} B ({treePerEntry * 2_000_000 / (1 << 20):0} MB), both alive {bothPerEntry:0} B ({bothPerEntry * 2_000_000 / (1 << 20):0} MB), names included");
        GC.KeepAlive(tree);

        // The reader's own table, names included, with 1.5 free records per record in use as in the test VM's $MFT (4 bytes
        // each): under 250 MB for 2 million entries.
        Assert.True((tablePerEntry + 6) * 2_000_000 < 250 << 20, $"{tablePerEntry:0} bytes per entry");
    }
}
