namespace Optimizer.Core.Storage.Mft;

/// <summary>
/// Decodes the mapping pairs (data runs) of a non-resident attribute into extents on the volume. Each run starts with
/// a byte whose low nibble is the size of the cluster count and whose high nibble is the size of the signed LCN offset
/// from the previous run's start; a 0 byte ends the list (ATTRIBUTE_RECORD_HEADER remarks, Linux ntfs3 run_unpack).
/// </summary>
public static class RunList
{
    /// <summary>
    /// The runs in order; a sparse run (no offset bytes) has Lcn -1 and does not move the current LCN. A malformed list
    /// (a run running past the span, a size over 8 bytes, a run of 0 clusters, an LCN below 0 or overflowing) returns
    /// an empty list rather than the runs before the damage, so a caller never reads half a file as if it were whole.
    /// </summary>
    public static IReadOnlyList<(long Lcn, long Clusters)> Parse(ReadOnlySpan<byte> runs)
    {
        List<(long, long)>? result = null;
        long lcn = 0;
        var pos = 0;
        while (pos < runs.Length && runs[pos] != 0)
        {
            int lengthSize = runs[pos] & 0x0F, offsetSize = runs[pos] >> 4;
            pos++;
            if (lengthSize == 0 || lengthSize > 8 || offsetSize > 8 || lengthSize + offsetSize > runs.Length - pos) return [];

            ulong clusters = 0;
            for (var i = lengthSize - 1; i >= 0; i--) clusters = clusters << 8 | runs[pos + i];
            pos += lengthSize;
            if (clusters == 0 || clusters > long.MaxValue) return [];

            long start = -1;
            if (offsetSize > 0)
            {
                // Signed, little endian: sign-extend from the top byte.
                long delta = (sbyte)runs[pos + offsetSize - 1];
                for (var i = offsetSize - 2; i >= 0; i--) delta = delta << 8 | runs[pos + i];
                pos += offsetSize;
                if (delta == 0 || (delta > 0 && lcn > long.MaxValue - delta)) return [];
                lcn += delta;
                if (lcn < 0) return [];
                start = lcn;
            }
            (result ??= []).Add((start, (long)clusters));
        }
        return (IReadOnlyList<(long, long)>?)result ?? [];
    }
}
