using System.IO;
using System.Text.Json;

namespace Optimizer.App.Services;

/// <summary>One shipped component and the license texts that must travel with it (Licenses/licenses.json).</summary>
public sealed record LicenseComponent(string Name, string? Package, string? Version, string License, string Source, IReadOnlyList<string> Files)
{
    // What screen readers announce for this item in a list or combo box.
    public override string ToString() => Name;

    public string Subtitle => Version is null ? License : $"{Version}, {License}";
}

/// <summary>License texts embedded in the exe (see the Licenses item group in the project file).</summary>
public static class Licenses
{
    private static IReadOnlyList<LicenseComponent>? _components;

    public static IReadOnlyList<LicenseComponent> Components => _components ??=
        JsonSerializer.Deserialize<List<LicenseComponent>>(Read("licenses.json"), new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? [];

    /// <summary>All texts of a component, in manifest order (license first, then third-party notices).</summary>
    public static string Text(LicenseComponent component) => string.Join("\n\n\n", component.Files.Select(Read));

    private static string Read(string file)
    {
        using var stream = typeof(Licenses).Assembly.GetManifestResourceStream("Licenses." + file)
            ?? throw new FileNotFoundException("License text not embedded", file);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd().Replace("\r\n", "\n");
    }
}
