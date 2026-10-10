using System.Text.RegularExpressions;

namespace Optimizer.Core.Platform;

/// <summary>
/// Output of tools that redraw a progress line with carriage returns (sfc "Verification 12% complete.", the DISM progress
/// bar). Each redraw arrives as its own line; shown one after another they fill the output with repeats.
/// </summary>
public static partial class OutputLines
{
    /// <summary>
    /// Adds <paramref name="line"/>. A progress line replaces the line before it when that is the same progress line with
    /// another number, in any display language. Keeps at most <paramref name="max"/> lines.
    /// </summary>
    public static void Add(List<string> lines, string line, int max = 200)
    {
        if (lines.Count > 0 && IsProgress(line) && IsProgress(lines[^1]) && Shape(line) == Shape(lines[^1]))
            lines[^1] = line;
        else
            lines.Add(line);
        if (lines.Count > max) lines.RemoveAt(0);
    }

    private static bool IsProgress(string line) => Percent().IsMatch(line);

    /// <summary>The line without numbers, percent signs, bar characters and spaces.</summary>
    private static string Shape(string line) =>
        new(line.Where(c => !char.IsDigit(c) && c is not ('.' or ',' or '%' or '=' or ' ' or ' ')).ToArray());

    [GeneratedRegex(@"\d+(?:[.,]\d+)?\s?%")]
    private static partial Regex Percent();
}
