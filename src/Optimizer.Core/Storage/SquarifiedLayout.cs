namespace Optimizer.Core.Storage;

/// <summary>A rectangle in the treemap's coordinate space (device independent pixels).</summary>
public readonly record struct LayoutRect(double X, double Y, double Width, double Height);

/// <summary>
/// Squarified treemap layout (Bruls, Huizing, van Wijk 2000): rows are laid along the shorter side of what is left and
/// an item joins the row as long as the worst aspect ratio does not get worse. One level only; the caller recurses.
/// </summary>
public static class SquarifiedLayout
{
    /// <summary>
    /// Places one rectangle per weight inside <paramref name="area"/>, areas in proportion to the weights. Weights
    /// should come sorted descending (that gives the best ratios); the result index matches the input index. Zero and
    /// negative weights, and an area without width or height, give empty rectangles.
    /// </summary>
    public static IReadOnlyList<LayoutRect> Layout(IReadOnlyList<double> weights, LayoutRect area)
    {
        var n = weights.Count;
        var result = new LayoutRect[n];
        if (n == 0 || !(area.Width > 0) || !(area.Height > 0)) return result;

        // Only items with a positive weight take part; the others keep their empty default rectangle.
        var idx = new int[n];
        var m = 0;
        double total = 0;
        for (var i = 0; i < n; i++)
        {
            var w = weights[i];
            if (w > 0 && !double.IsInfinity(w)) { idx[m++] = i; total += w; }
        }
        if (m == 0) return result;

        var scale = area.Width * area.Height / total;
        double x = area.X, y = area.Y, rw = area.Width, rh = area.Height;
        var start = 0;
        while (start < m)
        {
            var side = Math.Min(rw, rh);
            var side2 = side * side;
            var end = start;
            double sum = 0, min = 0, max = 0, worst = double.MaxValue;
            while (end < m)
            {
                var a = weights[idx[end]] * scale;
                var nsum = sum + a;
                var nmin = end == start ? a : Math.Min(min, a);
                var nmax = end == start ? a : Math.Max(max, a);
                var nworst = Math.Max(side2 * nmax / (nsum * nsum), nsum * nsum / (side2 * nmin));
                if (end > start && nworst > worst) break;
                sum = nsum; min = nmin; max = nmax; worst = nworst;
                end++;
            }

            var last = end == m;
            var vertical = rw >= rh; // the row is a column along the left edge when the area is wider than tall
            var thickness = last ? (vertical ? rw : rh) : sum / side;
            var offset = 0.0;
            for (var k = start; k < end; k++)
            {
                var len = k == end - 1 ? side - offset : weights[idx[k]] * scale / thickness;
                result[idx[k]] = vertical
                    ? new LayoutRect(x, y + offset, thickness, len)
                    : new LayoutRect(x + offset, y, len, thickness);
                offset += len;
            }

            if (vertical) { x += thickness; rw -= thickness; }
            else { y += thickness; rh -= thickness; }
            start = end;
        }
        return result;
    }
}
