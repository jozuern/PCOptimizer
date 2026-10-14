using System.Runtime.CompilerServices;

namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// The base records in use of a volume's $MFT as parallel arrays instead of an object graph per record (a drive can hold
/// millions): 4 bytes per record of the table for the slot index, plus about 51 bytes per record in use and the one name
/// the tree shows (an $MFT keeps many free records, often more than half). Entries are added in record order. Per entry:
/// the record number, the parent reference of its best name (see <see cref="MftRecordParser.NameRank"/>), sequence
/// number, unnamed $DATA size and allocated size, last write (UTC ticks, 0 unknown), file attributes and
/// <see cref="Flags"/>. Extension records stay whole in <see cref="Extensions"/>; <see cref="MftTreeBuilder"/> folds them
/// in.
/// </summary>
internal sealed class MftTable
{
    public const byte InUse = 0x01, Folder = 0x02, HasData = 0x04, HasTag = 0x08, LinkTag = 0x10, Reached = 0x20;
    private const int RankShift = 6;
    /// <summary>Reparse tag bit of links (junction, mount point, symlink), as <see cref="EnumerationScanner.IsLink"/> tests it.</summary>
    private const uint NameSurrogate = 0x2000_0000;

    /// <summary>Entry index + 1 per record number, 0 for a record that is not a base record in use.</summary>
    private readonly int[] _slot;

    /// <summary>Entries (base records in use).</summary>
    public int Count { get; private set; }
    public uint[] Number;
    public ulong[] Parent;
    public ushort[] Sequence;
    public long[] Size, Allocated, LastWrite;
    public uint[] Attributes;
    public byte[] Flags;
    public string?[] Name;
    public readonly List<MftRecord> Extensions = [];

    /// <param name="records">Records in the file table.</param>
    /// <param name="inUse">Records in use when known (the $MFT bitmap): the entries get room for them at once. Otherwise
    /// they start with room for a quarter of all records and grow.</param>
    public MftTable(int records, long? inUse = null)
    {
        _slot = new int[records];
        var capacity = (int)Math.Min(records, inUse is { } known ? known + known / 64 + 64 : Math.Max(1024, records / 4));
        Number = new uint[capacity];
        Parent = new ulong[capacity];
        Sequence = new ushort[capacity];
        Size = new long[capacity];
        Allocated = new long[capacity];
        LastWrite = new long[capacity];
        Attributes = new uint[capacity];
        Flags = new byte[capacity];
        Name = new string?[capacity];
    }

    /// <summary>The entry of record <paramref name="record"/> (a record number, without sequence), -1 when there is none.</summary>
    public int IndexOf(ulong record) => record < (ulong)_slot.Length ? _slot[record] - 1 : -1;

    /// <summary>The rank of the name kept for entry <paramref name="i"/>, 0 when it has none.</summary>
    public byte Rank(int i) => (byte)(Flags[i] >> RankShift);

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public void Add(uint number, ushort sequence, bool folder, string? name, ulong parent, byte rank, bool hasData, long size,
        long allocated, long lastWriteTicks, uint attributes, uint reparseTag)
    {
        if (number >= (uint)_slot.Length) return;
        var i = _slot[number] - 1;
        if (i < 0)
        {
            if (Count == Number.Length) Grow(number);
            i = Count++;
            _slot[number] = i + 1;
        }
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

    /// <summary>
    /// Room for the entries still to come, estimated from the share in use so far (records arrive in order) with a
    /// tenth to spare, at least a quarter more: no doubling that leaves half the arrays empty.
    /// </summary>
    private void Grow(uint number)
    {
        var estimate = (long)((double)Count * _slot.Length / (number + 1) * 1.1);
        var capacity = (int)Math.Min(_slot.Length, Math.Max(estimate, Count + Count / 4 + 1));
        Array.Resize(ref Number, capacity);
        Array.Resize(ref Parent, capacity);
        Array.Resize(ref Sequence, capacity);
        Array.Resize(ref Size, capacity);
        Array.Resize(ref Allocated, capacity);
        Array.Resize(ref LastWrite, capacity);
        Array.Resize(ref Attributes, capacity);
        Array.Resize(ref Flags, capacity);
        Array.Resize(ref Name, capacity);
    }

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
