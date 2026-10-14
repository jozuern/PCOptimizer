namespace Optimizer.Core.Storage;

/// <summary>
/// The rectangles of a treemap layout in the order they were laid out, each folder before what is in it. A folder's cell
/// knows where the cells inside it end, so a hit test or a lookup goes down only into the folder that holds the point or
/// the entry: a drive's map holds a few hundred thousand cells, and the pointer moves over it all the time.
/// </summary>
public sealed class TreemapCells
{
    private readonly List<(LayoutRect Rect, int Entry, int End)> _cells = [];

    public int Count => _cells.Count;
    public LayoutRect Rect(int cell) => _cells[cell].Rect;
    public int Entry(int cell) => _cells[cell].Entry;

    public void Clear() => _cells.Clear();

    /// <summary>A file, or a folder with nothing laid out inside it.</summary>
    public void AddLeaf(LayoutRect rect, int entry) => _cells.Add((rect, entry, _cells.Count + 1));

    /// <summary>A folder whose cells follow; <see cref="EndFolder"/> with the returned cell once they are added.</summary>
    public int BeginFolder(LayoutRect rect, int entry)
    {
        _cells.Add((rect, entry, _cells.Count + 1));
        return _cells.Count - 1;
    }

    public void EndFolder(int cell) => _cells[cell] = _cells[cell] with { End = _cells.Count };

    /// <summary>The entry of the deepest cell under the point; -1 outside every cell. Siblings do not overlap.</summary>
    public int Hit(double x, double y)
    {
        var hit = -1;
        var limit = _cells.Count;
        for (var i = 0; i < limit;)
        {
            var (r, entry, end) = _cells[i];
            if (x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height)
            {
                // Only the cells inside this one are left to look at.
                hit = entry;
                limit = end;
                i++;
            }
            else i = end;
        }
        return hit;
    }

    /// <summary>
    /// The cell of an entry, given the entries from the top of the map down to it (the folders it lies in below the shown
    /// folder, the entry last); -1 when it got none (too small, or not on this map).
    /// </summary>
    public int Find(IReadOnlyList<int> path)
    {
        var i = 0;
        var limit = _cells.Count;
        for (var level = 0; level < path.Count; level++)
        {
            while (i < limit && _cells[i].Entry != path[level]) i = _cells[i].End;
            if (i >= limit) return -1;
            if (level == path.Count - 1) return i;
            limit = _cells[i].End;
            i++;
        }
        return -1;
    }
}
