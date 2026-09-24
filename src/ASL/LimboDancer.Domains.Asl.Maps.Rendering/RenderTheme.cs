using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>One stroke of a layered linear-terrain style: its color, width added to the feature width, and dash.</summary>
public sealed record ThemeStroke(string Color, double Extra, string? Dash);

/// <summary>A resolved terrain style. Area, building, and bridge styles fill; hexside styles stroke; linear styles stack strokes.</summary>
public sealed record TerrainStyle(string? Fill, string? Stroke, string? StrokeWidth, string? Dash, bool Shadow, IReadOnlyList<ThemeStroke> Strokes);

/// <summary>A pattern definition: a tile with a background and child elements written through the SVG writer.</summary>
public sealed record ThemePattern(string Id, int Width, int Height, string Background, IReadOnlyList<(string Name, IReadOnlyList<(string Name, string Value)> Attributes)> Elements);

/// <summary>
/// A versioned render theme (Architecture and Rendering Design, section 3.4), loaded from embedded JSON. Style rules
/// are tried in order; a rule matches when its <c>name</c>, <c>contains</c>, and <c>category</c> conditions all hold.
/// A code no rule matches is unstyled and falls back to its catalog color.
/// </summary>
public sealed class RenderTheme
{
    private readonly List<(string? Name, string? Contains, string? Category, TerrainStyle Style)> rules = [];

    private RenderTheme(string name, string version)
    {
        Name = name;
        Version = version;
    }

    public string Name
    {
        get;
    }

    public string Version
    {
        get;
    }

    public IReadOnlyDictionary<int, string> ElevationTints { get; private set; } = new Dictionary<int, string>();

    public TerrainStyle Crest { get; private set; } = new(null, "#6b4f2a", "1", null, false, []);

    public int HexsideWidth { get; private set; } = 4;

    public IReadOnlyList<ThemePattern> Patterns { get; private set; } = [];

    /// <summary>The shipped Styled view theme.</summary>
    public static RenderTheme Board { get; } = Load("board");

    public static RenderTheme Load(string name)
    {
        using var stream = typeof(RenderTheme).Assembly.GetManifestResourceStream($"Themes.{name}.json")
            ?? throw new ArgumentException($"No theme named {name} is embedded.", nameof(name));
        using var document = JsonDocument.Parse(stream);
        return Parse(document.RootElement);
    }

    public static RenderTheme Parse(JsonElement root)
    {
        var theme = new RenderTheme(root.GetProperty("name").GetString()!, root.GetProperty("version").GetString()!);
        theme.ElevationTints = root.GetProperty("elevationTints").EnumerateObject()
            .ToDictionary(property => int.Parse(property.Name, System.Globalization.CultureInfo.InvariantCulture), property => property.Value.GetString()!);
        var crest = root.GetProperty("crest");
        theme.Crest = new TerrainStyle(null, crest.GetProperty("stroke").GetString(), crest.GetProperty("strokeWidth").GetString(), null, false, []);
        theme.HexsideWidth = root.GetProperty("hexsideWidth").GetInt32();
        theme.Patterns = root.GetProperty("patterns").EnumerateArray().Select(pattern => new ThemePattern(
            pattern.GetProperty("id").GetString()!,
            pattern.GetProperty("width").GetInt32(),
            pattern.GetProperty("height").GetInt32(),
            pattern.GetProperty("background").GetString()!,
            pattern.GetProperty("elements").EnumerateArray().Select(element => (
                element.GetProperty("name").GetString()!,
                (IReadOnlyList<(string, string)>)element.GetProperty("attributes").EnumerateObject().Select(attribute => (attribute.Name, attribute.Value.GetString()!)).ToArray()))
                .ToArray())).ToArray();
        foreach (var rule in root.GetProperty("styles").EnumerateArray())
        {
            var strokes = rule.TryGetProperty("strokes", out var list)
                ? list.EnumerateArray().Select(stroke => new ThemeStroke(
                    stroke.GetProperty("color").GetString()!,
                    double.Parse(stroke.GetProperty("extra").GetString()!, System.Globalization.CultureInfo.InvariantCulture),
                    Optional(stroke, "dash"))).ToArray()
                : [];
            theme.rules.Add((Optional(rule, "name"), Optional(rule, "contains"), Optional(rule, "category"), new TerrainStyle(
                Optional(rule, "fill"), Optional(rule, "stroke"), Optional(rule, "strokeWidth"), Optional(rule, "dash"),
                rule.TryGetProperty("shadow", out var shadow) && shadow.GetBoolean(), strokes)));
        }

        return theme;
    }

    /// <summary>The style for a terrain type, or null when the theme does not style it.</summary>
    public TerrainStyle? StyleFor(TerrainType terrain)
    {
        ArgumentNullException.ThrowIfNull(terrain);
        foreach (var (name, contains, category, style) in rules)
        {
            if ((name is null || name == terrain.Name)
                && (contains is null || terrain.Name.Contains(contains, StringComparison.Ordinal))
                && (category is null || string.Equals(category, terrain.Category.ToString(), StringComparison.Ordinal)))
            {
                return style;
            }
        }

        return null;
    }

    /// <summary>The ground tint for an elevation level; levels beyond the table use its nearest end.</summary>
    public string Tint(int level)
    {
        if (ElevationTints.TryGetValue(level, out var tint))
        {
            return tint;
        }

        var keys = ElevationTints.Keys.Order().ToArray();
        return ElevationTints[level < keys[0] ? keys[0] : keys[^1]];
    }

    private static string? Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
