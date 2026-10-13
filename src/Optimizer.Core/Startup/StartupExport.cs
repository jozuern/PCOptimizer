using System.Text;

namespace Optimizer.Core.Startup;

/// <summary>One line of the exported list: the entry as the page shows it, in the UI language.</summary>
public sealed record StartupExportRow(string Name, string Kind, string State, string Publisher, string Signature, string? Command, string? File, string Location);

/// <summary>
/// The Startup list as a CSV file (Autoruns can save its list too), for a forum post or to compare by hand. UTF-8 with a
/// byte order mark and semicolons when the UI language uses a decimal comma, so Excel opens it in columns. A cell that
/// starts like a formula (=, +, -, @) gets a leading apostrophe, so a spreadsheet never runs a command line as one.
/// </summary>
public static class StartupExport
{
    public static string ToCsv(IEnumerable<StartupExportRow> rows, IReadOnlyList<string> headers, char separator)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(separator, headers.Select(h => Cell(h, separator))));
        foreach (var r in rows)
            sb.AppendLine(string.Join(separator, new[] { r.Name, r.Kind, r.State, r.Publisher, r.Signature, r.Command ?? "", r.File ?? "", r.Location }.Select(c => Cell(c, separator))));
        return sb.ToString();
    }

    public static string Cell(string value, char separator)
    {
        var text = value.ReplaceLineEndings(" ");
        if (text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t') text = "'" + text;
        return text.IndexOfAny([separator, '"', ',', ';']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    /// <summary>The separator Excel expects for the culture: a semicolon where the decimal separator is a comma.</summary>
    public static char SeparatorFor(System.Globalization.CultureInfo culture) =>
        culture.NumberFormat.NumberDecimalSeparator == "," ? ';' : ',';
}
