using System.Globalization;
using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// Canonical JSON (Model Design section 9.2): RFC 8785 rules for the values this model uses. Object keys are sorted by
/// UTF-16 code units, there is no whitespace, numbers are integers, and strings use the minimal JCS escapes. Values are
/// dictionaries, lists, integers, strings, booleans, or null.
/// </summary>
public static class CanonicalJson
{
    public static byte[] Serialize(object? value)
    {
        var builder = new StringBuilder();
        Write(builder, value);
        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void Write(StringBuilder builder, object? value)
    {
        switch (value)
        {
            case null:
                builder.Append("null");
                break;
            case bool flag:
                builder.Append(flag ? "true" : "false");
                break;
            case int number:
                builder.Append(number.ToString(CultureInfo.InvariantCulture));
                break;
            case long number:
                builder.Append(number.ToString(CultureInfo.InvariantCulture));
                break;
            case string text:
                WriteString(builder, text);
                break;
            case IDictionary<string, object?> map:
                builder.Append('{');
                var first = true;
                foreach (var key in map.Keys.Order(StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        builder.Append(',');
                    }

                    first = false;
                    WriteString(builder, key);
                    builder.Append(':');
                    Write(builder, map[key]);
                }

                builder.Append('}');
                break;
            case System.Collections.IEnumerable items:
                builder.Append('[');
                var firstItem = true;
                foreach (var item in items)
                {
                    if (!firstItem)
                    {
                        builder.Append(',');
                    }

                    firstItem = false;
                    Write(builder, item);
                }

                builder.Append(']');
                break;
            default:
                throw new ArgumentException($"Canonical JSON does not support {value.GetType().Name}.", nameof(value));
        }
    }

    private static void WriteString(StringBuilder builder, string text)
    {
        builder.Append('"');
        foreach (var character in text)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\b':
                    builder.Append("\\b");
                    break;
                case '\f':
                    builder.Append("\\f");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < 0x20)
                    {
                        builder.Append("\\u").Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
    }
}

/// <summary>
/// The <c>features.json</c> entry (Model Design section 9.1): a Feature Model in canonical JSON. Coordinates are raw
/// fixed-point integers (1/64 pixel); features are written in id order, so equal models give equal bytes.
/// </summary>
public static class FeatureModelJson
{
    public const int FormatVersion = 1;

    public static byte[] Serialize(FeatureModel model) => CanonicalJson.Serialize(ToJson(model));

    /// <summary>The model as canonical JSON values, for embedding in other canonical documents.</summary>
    public static Dictionary<string, object?> ToJson(FeatureModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new Dictionary<string, object?>
        {
            ["formatVersion"] = FormatVersion,
            ["geometry"] = GeometryJson(model.Geometry),
            ["catalogHash"] = model.CatalogHash,
            ["baseCode"] = (int)model.BaseCode,
            ["baseElevation"] = model.BaseElevation,
            ["features"] = model.Features.OrderBy(feature => feature.Id, StringComparer.Ordinal).Select(Feature).ToList(),
            ["annotations"] = Annotations(model.Geometry, model.Annotations),
            ["provenance"] = Provenance(model.Provenance),
        };
    }

    /// <summary>
    /// The geometry: kind "standard" with the board size for the standard hex, or kind "vasl" with every value of a VASL
    /// layout (a/b, BFP double-width, and Deluxe boards, Model Design section 3.6). Hex sizes are written as their
    /// shortest round-trip decimal text, since canonical JSON numbers are integers.
    /// </summary>
    public static Dictionary<string, object?> GeometryJson(BoardGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (IsStandard(geometry))
        {
            return new Dictionary<string, object?>
            {
                ["kind"] = "standard",
                ["widthInHexes"] = geometry.WidthInHexes,
                ["heightInHexes"] = geometry.HeightInHexes,
            };
        }

        return new Dictionary<string, object?>
        {
            ["kind"] = "vasl",
            ["widthInHexes"] = geometry.WidthInHexes,
            ["heightInHexes"] = geometry.HeightInHexes,
            ["hexWidth"] = geometry.HexWidth.ToString("R", CultureInfo.InvariantCulture),
            ["hexHeight"] = geometry.HexHeight.ToString("R", CultureInfo.InvariantCulture),
            ["gridWidth"] = geometry.GridWidth,
            ["gridHeight"] = geometry.GridHeight,
            ["columnLetterOffset"] = geometry.ColumnLetterOffset,
            ["rowNumberOffset"] = geometry.RowNumberOffset,
        };
    }

    private static bool IsStandard(BoardGeometry geometry)
    {
        var standard = BoardGeometry.Standard(geometry.WidthInHexes, geometry.HeightInHexes);
        return geometry.HexWidth == standard.HexWidth && geometry.HexHeight == standard.HexHeight && geometry.A1CenterX == standard.A1CenterX
            && geometry.A1CenterY == standard.A1CenterY && geometry.GridWidth == standard.GridWidth && geometry.GridHeight == standard.GridHeight
            && geometry.ColumnLetterOffset == 0 && geometry.RowNumberOffset == 0;
    }

    public static Dictionary<string, object?> Provenance(FeatureProvenance provenance)
    {
        ArgumentNullException.ThrowIfNull(provenance);
        return new Dictionary<string, object?>
        {
            ["kind"] = provenance.Kind,
            ["sourceBoard"] = provenance.SourceBoard,
            ["sourceVersion"] = provenance.SourceVersion,
            ["vectorizerVersion"] = provenance.VectorizerVersion,
        };
    }

    public static FeatureModel Deserialize(ReadOnlySpan<byte> json)
    {
        using var document = JsonDocument.Parse(json.ToArray());
        return FromJson(document.RootElement);
    }

    public static FeatureModel FromJson(JsonElement root)
    {
        var version = root.GetProperty("formatVersion").GetInt32();
        if (version != FormatVersion)
        {
            throw new JsonException($"features.json format version {version} is not supported.");
        }

        var geometry = ReadGeometry(root.GetProperty("geometry"));
        return new FeatureModel(
            geometry,
            root.GetProperty("catalogHash").GetString()!,
            checked((byte)root.GetProperty("baseCode").GetInt32()),
            root.GetProperty("baseElevation").GetInt32(),
            root.GetProperty("features").EnumerateArray().Select(ReadFeature).ToArray(),
            ReadAnnotations(geometry, root.GetProperty("annotations")),
            ReadProvenance(root.GetProperty("provenance")));
    }

    public static BoardGeometry ReadGeometry(JsonElement element)
    {
        var kind = element.GetProperty("kind").GetString();
        var width = element.GetProperty("widthInHexes").GetInt32();
        var height = element.GetProperty("heightInHexes").GetInt32();
        return kind switch
        {
            "standard" => BoardGeometry.Standard(width, height),
            "vasl" => BoardGeometry.Vasl(width, height,
                double.Parse(element.GetProperty("hexWidth").GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                double.Parse(element.GetProperty("hexHeight").GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture),
                element.GetProperty("gridWidth").GetInt32(), element.GetProperty("gridHeight").GetInt32(),
                element.GetProperty("columnLetterOffset").GetInt32(), element.GetProperty("rowNumberOffset").GetInt32()),
            _ => throw new JsonException($"Geometry kind '{kind}' is not supported."),
        };
    }

    public static FeatureProvenance ReadProvenance(JsonElement element) => new(
        element.GetProperty("kind").GetString()!,
        OptionalString(element, "sourceBoard"),
        OptionalString(element, "sourceVersion"),
        OptionalString(element, "vectorizerVersion"));

    private static Dictionary<string, object?> Feature(Feature feature)
    {
        var json = new Dictionary<string, object?>
        {
            ["id"] = feature.Id,
            ["layer"] = feature.Layer,
        };
        if (feature.Notes is { } notes)
        {
            json["notes"] = notes;
        }

        switch (feature)
        {
            case ElevationRegion region:
                json["kind"] = "elevation";
                json["level"] = region.Level;
                json["shape"] = Shape(region.Shape);
                break;
            case AreaTerrainFeature area:
                json["kind"] = "area";
                json["code"] = (int)area.Code;
                json["shape"] = Shape(area.Shape);
                break;
            case LinearTerrainFeature linear:
                json["kind"] = "linear";
                json["code"] = (int)linear.Code;
                json["width"] = linear.Width.Raw;
                if (linear.Centerline is { } centerline)
                {
                    json["centerline"] = Centerline(centerline);
                }

                if (linear.Outline is { } outline)
                {
                    json["outline"] = Shape(outline);
                }

                break;
            case BridgeFeature bridge:
                json["kind"] = "bridge";
                json["code"] = (int)bridge.Code;
                json["shape"] = Shape(bridge.Shape);
                break;
            case BuildingFeature building:
                json["kind"] = "building";
                json["code"] = (int)building.Code;
                json["buildingId"] = building.BuildingId;
                json["footprints"] = building.Footprints.Select(Shape).ToList();
                break;
            case HexsideTerrainFeature hexside:
                json["kind"] = "hexside";
                json["code"] = (int)hexside.Code;
                json["spans"] = hexside.Spans.Select(span => new Dictionary<string, object?>
                {
                    ["column"] = span.Side.Hex.Column,
                    ["row"] = span.Side.Hex.Row,
                    ["side"] = (int)span.Side.Side,
                    ["from"] = span.From,
                    ["to"] = span.To,
                    ["width"] = span.Width,
                }).ToList();
                break;
            case FidelityPin pin:
                json["kind"] = "pin";
                json["code"] = (int)pin.Code;
                json["elevation"] = pin.Elevation;
                json["shape"] = Shape(pin.Shape);
                break;
            default:
                throw new NotSupportedException($"Unknown feature kind {feature.GetType().Name}.");
        }

        return json;
    }

    private static List<object?> Shape(FeatureShape shape) =>
        shape.Rings.Select(ring => (object?)ring.SelectMany(point => new object?[] { point.X, point.Y }).ToList()).ToList();

    private static Dictionary<string, object?> Centerline(CenterlinePath path) => new()
    {
        ["start"] = new List<object?> { path.Start.X, path.Start.Y },
        ["segments"] = path.Segments.Select(segment =>
        {
            var json = new Dictionary<string, object?> { ["end"] = new List<object?> { segment.End.X, segment.End.Y } };
            if (segment.Control1 is { } c1 && segment.Control2 is { } c2)
            {
                json["c1"] = new List<object?> { c1.X, c1.Y };
                json["c2"] = new List<object?> { c2.X, c2.Y };
            }

            return (object?)json;
        }).ToList(),
    };

    private static Dictionary<string, object?> Annotations(BoardGeometry geometry, HexAnnotations annotations) => new()
    {
        ["stairways"] = annotations.Stairways.Select(geometry.NameOf).Select(name => name.ToString()).Order(StringComparer.Ordinal).Select(name => (object?)name).ToList(),
        ["slopes"] = Flags(annotations.Hexsides.Slopes),
        ["railroadEmbankments"] = Flags(annotations.Hexsides.RailroadEmbankments),
        ["partialOrchards"] = Flags(annotations.Hexsides.PartialOrchards),
    };

    private static Dictionary<string, object?> Flags(IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> flags) =>
        flags.Where(pair => pair.Value.Count > 0).ToDictionary(
            pair => pair.Key.ToString(),
            pair => (object?)pair.Value.Select(side => (int)side).Order().Select(side => (object?)side).ToList(),
            StringComparer.Ordinal);

    private static Feature ReadFeature(JsonElement element)
    {
        var id = element.GetProperty("id").GetString()!;
        var layer = element.GetProperty("layer").GetInt32();
        Feature feature = element.GetProperty("kind").GetString() switch
        {
            "elevation" => new ElevationRegion(id, layer, ReadShape(element.GetProperty("shape")), element.GetProperty("level").GetInt32()),
            "area" => new AreaTerrainFeature(id, layer, ReadShape(element.GetProperty("shape")), Code(element)),
            "linear" => new LinearTerrainFeature(id, layer, Code(element),
                element.TryGetProperty("centerline", out var centerline) ? ReadCenterline(centerline) : null,
                FixedPoint.FromRaw(element.GetProperty("width").GetInt32()),
                element.TryGetProperty("outline", out var outline) ? ReadShape(outline) : null),
            "bridge" => new BridgeFeature(id, layer, ReadShape(element.GetProperty("shape")), Code(element)),
            "building" => new BuildingFeature(id, layer, element.GetProperty("footprints").EnumerateArray().Select(ReadShape).ToArray(), Code(element),
                element.GetProperty("buildingId").GetString()!),
            "hexside" => new HexsideTerrainFeature(id, layer, Code(element), element.GetProperty("spans").EnumerateArray().Select(span =>
                new HexsideSpan(
                    new HexsideRef(new HexIndex(span.GetProperty("column").GetInt32(), span.GetProperty("row").GetInt32()), (HexsideDirection)span.GetProperty("side").GetInt32()),
                    span.GetProperty("from").GetInt32(),
                    span.GetProperty("to").GetInt32(),
                    span.TryGetProperty("width", out var width) && width.ValueKind == JsonValueKind.Number ? width.GetInt32() : null)).ToArray()),
            "pin" => new FidelityPin(id, layer, ReadShape(element.GetProperty("shape")), Code(element),
                element.TryGetProperty("elevation", out var elevation) && elevation.ValueKind == JsonValueKind.Number ? elevation.GetInt32() : null),
            var kind => throw new JsonException($"Unknown feature kind '{kind}'."),
        };

        return element.TryGetProperty("notes", out var notes) ? feature with
        {
            Notes = notes.GetString()
        } : feature;
    }

    private static byte Code(JsonElement element) => checked((byte)element.GetProperty("code").GetInt32());

    private static FeatureShape ReadShape(JsonElement element) => new(element.EnumerateArray().Select(ring =>
    {
        var values = ring.EnumerateArray().Select(value => value.GetInt32()).ToArray();
        var points = new FixedVector[values.Length / 2];
        for (var index = 0; index < points.Length; index++)
        {
            points[index] = new FixedVector(values[2 * index], values[(2 * index) + 1]);
        }

        return (IReadOnlyList<FixedVector>)points;
    }).ToArray());

    private static CenterlinePath ReadCenterline(JsonElement element) => new(
        Point(element.GetProperty("start")),
        element.GetProperty("segments").EnumerateArray().Select(segment => new PathSegment(
            Point(segment.GetProperty("end")),
            segment.TryGetProperty("c1", out var c1) ? Point(c1) : null,
            segment.TryGetProperty("c2", out var c2) ? Point(c2) : null)).ToArray());

    private static FixedVector Point(JsonElement element) => new(element[0].GetInt32(), element[1].GetInt32());

    private static HexAnnotations ReadAnnotations(BoardGeometry geometry, JsonElement element) => new(
        element.GetProperty("stairways").EnumerateArray().Select(name => geometry.IndexOf(HexName.Parse(name.GetString()!))).ToHashSet(),
        new HexsideAnnotations(ReadFlags(element.GetProperty("slopes")), ReadFlags(element.GetProperty("railroadEmbankments")), ReadFlags(element.GetProperty("partialOrchards"))));

    private static Dictionary<HexName, IReadOnlySet<HexsideDirection>> ReadFlags(JsonElement element) =>
        element.EnumerateObject().ToDictionary(
            property => HexName.Parse(property.Name),
            property => (IReadOnlySet<HexsideDirection>)property.Value.EnumerateArray().Select(side => (HexsideDirection)side.GetInt32()).ToHashSet());

    private static string? OptionalString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
