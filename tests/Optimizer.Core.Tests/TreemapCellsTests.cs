using Optimizer.Core.Storage;

namespace Optimizer.Core.Tests;

/// <summary>The treemap's hit test and lookup go down only into the folder that holds the point, with the same result as looking at every cell.</summary>
public class TreemapCellsTests
{
    // Entries: 1 folder A (left) with file 2 on top and folder 3 below it, which holds files 4 and 5; file 6 on the right;
    // folder 7 too small to hold anything, as a leaf.
    private static (TreemapCells Cells, List<(LayoutRect Rect, int Entry)> Flat) Sample()
    {
        var cells = new TreemapCells();
        var flat = new List<(LayoutRect, int)>();
        void Leaf(LayoutRect r, int e) { cells.AddLeaf(r, e); flat.Add((r, e)); }
        int Begin(LayoutRect r, int e) { flat.Add((r, e)); return cells.BeginFolder(r, e); }

        var a = Begin(new LayoutRect(0, 0, 60, 100), 1);
        Leaf(new LayoutRect(0, 0, 60, 50), 2);
        var b = Begin(new LayoutRect(0, 50, 60, 50), 3);
        Leaf(new LayoutRect(0, 50, 30, 50), 4);
        Leaf(new LayoutRect(30, 50, 30, 50), 5);
        cells.EndFolder(b);
        cells.EndFolder(a);
        Leaf(new LayoutRect(60, 0, 40, 90), 6);
        Leaf(new LayoutRect(60, 90, 40, 10), 7);
        return (cells, flat);
    }

    [Fact]
    public void HitFindsTheDeepestCellLikeAFullScan()
    {
        var (cells, flat) = Sample();
        for (var x = -5.0; x <= 105; x += 2.5)
            for (var y = -5.0; y <= 105; y += 2.5)
            {
                // The last cell in layout order that holds the point is the deepest.
                var expected = flat.LastOrDefault(c => x >= c.Rect.X && x < c.Rect.X + c.Rect.Width && y >= c.Rect.Y && y < c.Rect.Y + c.Rect.Height, (default, -1)).Item2;
                Assert.Equal(expected, cells.Hit(x, y));
            }
        Assert.Equal(5, cells.Hit(45, 75));
        Assert.Equal(-1, cells.Hit(100, 50));
    }

    [Fact]
    public void FindFollowsThePathDownToTheEntry()
    {
        var (cells, _) = Sample();
        Assert.Equal(new LayoutRect(30, 50, 30, 50), cells.Rect(cells.Find([1, 3, 5])));
        Assert.Equal(2, cells.Entry(cells.Find([1, 2])));
        Assert.Equal(6, cells.Entry(cells.Find([6])));
        Assert.Equal(7, cells.Entry(cells.Find([7])));
        // An entry with no cell, or a path that does not lead to it, finds nothing.
        Assert.Equal(-1, cells.Find([1, 3, 9]));
        Assert.Equal(-1, cells.Find([3, 5]));
        Assert.Equal(-1, cells.Find([7, 1]));
        Assert.Equal(-1, cells.Find([]));
        cells.Clear();
        Assert.Equal(0, cells.Count);
        Assert.Equal(-1, cells.Hit(10, 10));
    }
}
