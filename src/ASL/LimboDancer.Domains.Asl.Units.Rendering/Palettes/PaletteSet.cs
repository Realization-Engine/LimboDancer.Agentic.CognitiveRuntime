using System.Text.Json;

namespace LimboDancer.Domains.Asl.Units.Rendering.Palettes;

/// <summary>A side's colors by role (Unit Display Design, section 7.4).</summary>
public sealed record SidePalette(string Fill, string FillMuted, string Ink, string Accent)
{
    public static readonly IReadOnlyList<string> Roles = ["fill", "fill-muted", "ink", "accent"];

    public string? Role(string role) => role switch
    {
        "fill" => Fill,
        "fill-muted" => FillMuted,
        "ink" => Ink,
        "accent" => Accent,
        _ => null,
    };
}

/// <summary>
/// A named set of side palettes, kept in its own file apart from style sheets. A side the set does not list uses
/// <see cref="Fallback"/>.
/// </summary>
public sealed record PaletteSet(string Name, string Version, string Label, IReadOnlyDictionary<string, SidePalette> Sides, SidePalette Fallback)
{
    public SidePalette For(string? side) => side is not null && Sides.TryGetValue(side, out var palette) ? palette : Fallback;
}

public sealed record PaletteSetResult(PaletteSet? Set, IReadOnlyList<string> Diagnostics);

public static class PaletteSetReader
{
    public static readonly IReadOnlyList<string> BuiltIn = ["asl-customary", "limbodancer"];

    public static PaletteSetResult Read(ReadOnlySpan<byte> json)
    {
        var diagnostics = new List<string>();
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json.ToArray());
        }
        catch (JsonException exception)
        {
            diagnostics.Add($"Not valid JSON: line {exception.LineNumber + 1}.");
            return new PaletteSetResult(null, diagnostics);
        }

        using (document)
        {
            var root = document.RootElement;
            string? Text(JsonElement element, string name) =>
                element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

            SidePalette? Palette(JsonElement element, string where)
            {
                var roles = SidePalette.Roles.Select(role => Text(element, role)).ToArray();
                for (var index = 0; index < roles.Length; index++)
                {
                    if (roles[index] is not { } color || !IsColor(color))
                    {
                        diagnostics.Add($"{where}: '{SidePalette.Roles[index]}' must be a #rrggbb color.");
                    }
                }

                return roles.All(role => role is not null && IsColor(role)) ? new SidePalette(roles[0]!, roles[1]!, roles[2]!, roles[3]!) : null;
            }

            var name = Text(root, "set");
            var version = Text(root, "version");
            if (name is null || version is null)
            {
                diagnostics.Add("'set' and 'version' are required.");
                return new PaletteSetResult(null, diagnostics);
            }

            var fallback = root.TryGetProperty("fallback", out var fallbackElement) ? Palette(fallbackElement, "fallback") : null;
            if (fallback is null)
            {
                diagnostics.Add("'fallback' is required.");
            }

            var sides = new SortedDictionary<string, SidePalette>(StringComparer.Ordinal);
            if (root.TryGetProperty("sides", out var sidesElement) && sidesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var side in sidesElement.EnumerateObject())
                {
                    if (Palette(side.Value, side.Name) is { } palette)
                    {
                        sides[side.Name] = palette;
                    }
                }
            }

            return diagnostics.Count > 0 || fallback is null
                ? new PaletteSetResult(null, diagnostics)
                : new PaletteSetResult(new PaletteSet(name, version, Text(root, "label") ?? name, sides, fallback), diagnostics);
        }
    }

    public static PaletteSet Embedded(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        using var stream = typeof(PaletteSetReader).Assembly.GetManifestResourceStream($"Palettes.{name}.palette.json")
            ?? throw new InvalidOperationException($"The {name} palette set is not embedded.");
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        var result = Read(buffer.ToArray());
        return result.Set ?? throw new InvalidDataException($"The {name} palette set is invalid: {string.Join("; ", result.Diagnostics)}");
    }

    internal static bool IsColor(string text) =>
        text.Length == 7 && text[0] == '#' && text.Skip(1).All(character => char.IsAsciiHexDigitLower(character) || char.IsAsciiDigit(character));
}
