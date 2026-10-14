using System.Runtime.CompilerServices;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// The base records in use of a volume's $MFT as parallel arrays instead of an object graph per record (a drive can hold
/// millions): 4 bytes per record of the table for the slot index, plus about 51 bytes per record in use and the one name
/// the tree shows (an $MFT keeps many free records, often more than half). Entries are added in record order, in pages
/// of <see cref="Paged{T}.Length"/> made as they fill: how many records are in use is not known before the whole table
/// is read, and pages never copy what they hold or keep more than one page of room. Per entry:
/// the record number, the parent reference of its best name (see <see cref="MftRecordParser.NameRank"/>), sequence
/// number, unnamed $DATA size and allocated size, last write (UTC ticks, 0 unknown), file attributes and
/// <see cref="Flags"/>. Extension records stay whole in <see cref="Extensions"/>; <see cref="MftTreeBuilder"/> folds them
/// in. A scan reserves the entries of a block of records in record order on one thread (<see cref="Reserve"/>), then
/// parses the block on several (<see cref="Set"/>, <see cref="Release"/> and <see cref="AddExtension"/>, each record
/// touching only its own entry and slot).
/// </summary>
internal sealed class MftTable
{
    public const byte InUse = 0x01, Folder = 0x02, HasData = 0x04, HasTag = 0x08, LinkTag = 0x10, Reached = 0x20;
    private const int RankShift = 6;
    /// <summary>Reparse tag bit of links (junction, mount point, symlink), as <see cref="EnumerationScanner.IsLink"/> tests it.</summary>
    private const uint NameSurrogate = 0x2000_0000;

    /// <summary>Entry index + 1 per record number, 0 for a record that is not a base record in use.</summary>
    private readonly int[] _slot;

    /// <summary>Entries (base records in use, and any released because their record could not be parsed).</summary>
    public int Count { get; private set; }
    public readonly Paged<uint> Number;
    public readonly Paged<ulong> Parent;
    public readonly Paged<ushort> Sequence;
    public readonly Paged<long> Size, Allocated, LastWrite;
    public readonly Paged<uint> Attributes;
    public readonly Paged<byte> Flags;
    public readonly Paged<string?> Name;
    public readonly List<MftRecord> Extensions = [];
    private readonly Lock _extensions = new();

    /// <param name="records">Records in the file table, the most entries it can have.</param>
    public MftTable(int records)
    {
        _slot = new int[records];
        Number = new(records);
        Parent = new(records);
        Sequence = new(records);
        Size = new(records);
        Allocated = new(records);
        LastWrite = new(records);
        Attributes = new(records);
        Flags = new(records);
        Name = new(records);
    }

    /// <summary>The entry of record <paramref name="record"/> (a record number, without sequence), -1 when there is none.</summary>
    public int IndexOf(ulong record) => record < (ulong)_slot.Length ? _slot[record] - 1 : -1;

    /// <summary>The rank of the name kept for entry <paramref name="i"/>, 0 when it has none.</summary>
    public byte Rank(int i) => (byte)(Flags[i] >> RankShift);

    /// <summary>
    /// The entry of base record <paramref name="number"/>, made at the end when it has none yet; -1 past the table. Not
    /// thread safe, and it may add pages: only while no record is being parsed.
    /// </summary>
    public int Reserve(uint number)
    {
        if (number >= (uint)_slot.Length) return -1;
        var i = _slot[number] - 1;
        if (i >= 0) return i;
        i = Count++;
        if (Paged<byte>.StartsPage(i))
        {
            Number.AddPage(i);
            Parent.AddPage(i);
            Sequence.AddPage(i);
            Size.AddPage(i);
            Allocated.AddPage(i);
            LastWrite.AddPage(i);
            Attributes.AddPage(i);
            Flags.AddPage(i);
            Name.AddPage(i);
        }
        _slot[number] = i + 1;
        return i;
    }

    /// <summary>
    /// A reserved entry whose record turned out unusable: no record number leads to it any more. Its fields stay empty
    /// (record 0, no name, no flags), so the builder passes over it.
    /// </summary>
    public void Release(uint number) => _slot[number] = 0;

    /// <summary>An extension record, from any thread.</summary>
    public void AddExtension(MftRecord record)
    {
        lock (_extensions) Extensions.Add(record);
    }

    public void Add(uint number, ushort sequence, bool folder, string? name, ulong parent, byte rank, bool hasData, long size,
        long allocated, long lastWriteTicks, uint attributes, uint reparseTag)
    {
        if (Reserve(number) is var i and >= 0)
            Set(i, number, sequence, folder, name, parent, rank, hasData, size, allocated, lastWriteTicks, attributes, reparseTag);
    }

    /// <summary>Fills entry <paramref name="i"/>; writes nothing else, so different entries can be filled at the same time.</summary>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Set(int i, uint number, ushort sequence, bool folder, string? name, ulong parent, byte rank, bool hasData, long size,
        long allocated, long lastWriteTicks, uint attributes, uint reparseTag)
    {
        Number[i] = number;
        Sequence[i] = sequence;
        Name[i] = name;
        Parent[i] = parent;
        LastWrite[i] = lastWriteTicks;
        Attributes[i] = attributes;
        Size[i] = hasData ? size : 0;
        Allocated[i] = hasData ? allocated : 0;
        Flags[i] = (byte)(InUse | (folder ? Folder : 0) | (hasData ? HasData : 0) | TagFlags(reparseTag) | rank << RankShift);
    }

    /// <summary>Takes a name of an extension record of entry <paramref name="i"/> when it ranks higher than the one kept.</summary>
    public void OfferName(int i, MftName name)
    {
        var rank = MftRecordParser.NameRank(name.Namespace);
        if (rank <= Rank(i)) return;
        Name[i] = name.Name;
        Parent[i] = name.ParentReference;
        Flags[i] = (byte)(Flags[i] & ((1 << RankShift) - 1) | rank << RankShift);
    }

    public static byte TagFlags(uint reparseTag) =>
        reparseTag == 0 ? (byte)0 : (byte)(HasTag | ((reparseTag & NameSurrogate) != 0 ? LinkTag : 0));

    public static DateTime Time(long ticks) => ticks == 0 ? default : new DateTime(ticks, DateTimeKind.Utc);

    /// <summary>The table of records parsed one by one (the tests and the fixture): sized by the highest record number.</summary>
    public static MftTable From(IReadOnlyList<MftRecord> records)
    {
        var count = 0;
        foreach (var r in records)
            if (r.Number < int.MaxValue) count = Math.Max(count, (int)r.Number + 1);
        // In record order, as a scan adds them; a later record of the same number replaces an earlier one.
        var bases = new MftRecord?[count];
        var table = new MftTable(count);
        foreach (var r in records)
        {
            if (r.Number >= count) continue;
            if (r.BaseReference != 0) table.Extensions.Add(r);
            else bases[r.Number] = r;
        }
        foreach (var r in bases)
        {
            if (r is null) continue;
            MftName? best = null;
            byte rank = 0;
            foreach (var name in r.Names)
            {
                var k = MftRecordParser.NameRank(name.Namespace);
                if (k > rank) (best, rank) = (name, k);
            }
            table.Add(r.Number, r.Sequence, r.IsFolder, best?.Name, best?.ParentReference ?? 0, rank, r.DataSize is not null,
                r.DataSize ?? 0, r.DataAllocated ?? -1, r.LastWriteUtc.Ticks, r.FileAttributes, r.ReparseTag);
        }
        return table;
    }
}

/// <summary>
/// An array in pages of <see cref="Length"/> elements (64 KB for 8-byte ones, under the large object heap's threshold),
/// each made by <see cref="AddPage"/> when the first element on it is reserved. Elements are reached by reference, as in
/// an array.
/// </summary>
internal readonly struct Paged<T>
{
    private const int Shift = 13;
    public const int Length = 1 << Shift;
    private const int Mask = Length - 1;
    private readonly T[]?[] _pages;

    /// <param name="capacity">The most elements it can hold; no page is made yet.</param>
    public Paged(int capacity) => _pages = new T[]?[(capacity + Mask) >> Shift];

    public ref T this[int i] => ref _pages[i >> Shift]![i & Mask];

    /// <summary>Whether element <paramref name="i"/> is the first on its page.</summary>
    public static bool StartsPage(int i) => (i & Mask) == 0;

    /// <summary>Makes the page that holds element <paramref name="i"/>.</summary>
    public void AddPage(int i) => _pages[i >> Shift] ??= new T[Length];
}
