using Optimizer.Core.Storage;

namespace Optimizer.Core.Tests;

/// <summary>The squarified treemap layout: proportional areas, containment, empty rects and aspect ratios.</summary>
public class SquarifiedLayoutTests
{
    private static readonly double[] Example = { 6, 6, 4, 3, 2, 2, 1 };
    private static readonly LayoutRect Area = new(0, 0, 600, 400);

    [Fact]
    public void RectsFillTheAreaInProportion()
    {
        var rects = SquarifiedLayout.Layout(Example, Area);

        Assert.Equal(Example.Length, rects.Count);
        double sum = 0;
        for (var i = 0; i < rects.Count; i++)
        {
            var area = rects[i].Width * rects[i].Height;
            Assert.InRange(area, Example[i] / 24 * 240000 - 0.5, Example[i] / 24 * 240000 + 0.5);
            sum += area;
        }
        Assert.InRange(sum, 239999, 240001);
    }

    [Fact]
    public void RectsStayInsideAndDoNotOverlap()
    {
        var rnd = new Random(12345);
        var weights = Enumerable.Range(0, 200).Select(_ => rnd.NextDouble() * 1000 + 1).OrderByDescending(w => w).ToArray();
        var area = new LayoutRect(10, 20, 800, 500);

        var rects = SquarifiedLayout.Layout(weights, area);

        const double eps = 1e-6;
        foreach (var r in rects)
        {
            Assert.True(r.X >= area.X - eps && r.Y >= area.Y - eps);
            Assert.True(r.X + r.Width <= area.X + area.Width + eps);
            Assert.True(r.Y + r.Height <= area.Y + area.Height + eps);
        }
        for (var i = 0; i < rects.Count; i++)
            for (var j = i + 1; j < rects.Count; j++)
            {
                var a = rects[i];
                var b = rects[j];
                var ox = Math.Min(a.X + a.Width, b.X + b.Width) - Math.Max(a.X, b.X);
                var oy = Math.Min(a.Y + a.Height, b.Y + b.Height) - Math.Max(a.Y, b.Y);
                Assert.False(ox > 1e-6 && oy > 1e-6, $"rects {i} and {j} overlap");
            }
    }

    [Fact]
    public void OneWeightTakesTheWholeArea()
    {
        var rects = SquarifiedLayout.Layout(new[] { 5.0 }, Area);

        Assert.Equal(Area, Assert.Single(rects));
    }

    [Fact]
    public void ZeroWeightsGetEmptyRects()
    {
        var rects = SquarifiedLayout.Layout(new[] { 4.0, 0, -3, 0 }, Area);

        Assert.Equal(4, rects.Count);
        Assert.Equal(Area, rects[0]);
        for (var i = 1; i < 4; i++)
            Assert.True(rects[i].Width == 0 || rects[i].Height == 0);
    }

    [Fact]
    public void EmptyInputAndEmptyAreaDoNotThrow()
    {
        Assert.Empty(SquarifiedLayout.Layout(Array.Empty<double>(), Area));
        foreach (var area in new[] { new LayoutRect(0, 0, 0, 400), new LayoutRect(0, 0, 600, 0) })
        {
            var rects = SquarifiedLayout.Layout(Example, area);
            Assert.Equal(Example.Length, rects.Count);
            Assert.All(rects, r => Assert.True(r.Width * r.Height == 0));
        }
    }

    [Fact]
    public void AspectRatiosAreReasonable()
    {
        var rects = SquarifiedLayout.Layout(Example, Area);

        var worst = rects.Max(r => Math.Max(r.Width / r.Height, r.Height / r.Width));
        Assert.True(worst < 3, $"worst aspect ratio {worst}");
    }
}
