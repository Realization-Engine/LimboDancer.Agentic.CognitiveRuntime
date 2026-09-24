using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// Parses VASL's per-board <c>BoardMetadata.xml</c>, following
/// <c>VASL.build.module.map.boardArchive.BoardMetadata.parseBoardMetadataFile</c>.
/// </summary>
public static class BoardMetadataParser
{
    private static readonly string[] GeometryAttributeNames =
        ["A1CenterX", "A1CenterY", "hexWidth", "hexHeight", "altHexGrain", "snapScale", "HexGridConfig"];

    private static readonly HashSet<string> DeferredElementNames = new(StringComparer.Ordinal)
    {
        "colors",
        "colorSSRules",
        "overlaySSRules",
    };

    public static BoardMetadataResult Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        XDocument document;
        bool declaredXml11;
        try
        {
            document = VaslXml.Load(buffer.ToArray(), out declaredXml11);
        }
        catch (XmlException exception)
        {
            return new BoardMetadataResult(null, [Error("VASL-META-000", $"BoardMetadata.xml is not well-formed XML: {exception.Message}")]);
        }

        var result = Parse(document);
        return declaredXml11
            ? result with
            {
                Diagnostics = [new MapDiagnostic("VASL-META-006", MapDiagnosticSeverity.Info, "BoardMetadata.xml declares XML 1.1; it was read as XML 1.0."), .. result.Diagnostics],
            }
            : result;
    }

    public static BoardMetadataResult Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var root = document.Root;
        if (root is null || root.Name.LocalName != "boardMetadata")
        {
            return new BoardMetadataResult(null, [Error("VASL-META-000", "BoardMetadata.xml has no boardMetadata root element.")]);
        }

        var diagnostics = new List<MapDiagnostic>();
        var width = RequiredInt(root, "width", diagnostics);
        var height = RequiredInt(root, "height", diagnostics);
        var hasHills = RequiredBoolean(root, "hasHills", diagnostics);
        var geometryAttributes = GeometryAttributeNames
            .Where(name => root.Attribute(name) is not null)
            .ToDictionary(name => name, name => root.Attribute(name)!.Value, StringComparer.Ordinal);

        var buildingTypes = new List<BuildingTypeOverride>();
        var slopes = new List<HexsideFlags>();
        var embankments = new List<HexsideFlags>();
        var orchards = new List<HexsideFlags>();
        var deferred = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var element in root.Elements())
        {
            var name = element.Name.LocalName;
            switch (name)
            {
                case "buildingTypes":
                    ReadBuildingTypes(element, buildingTypes, diagnostics);
                    break;
                case "slopes":
                    ReadHexsideFlags(element, "slope", slopes, diagnostics);
                    break;
                case "rrembankments":
                    ReadHexsideFlags(element, "rrembankment", embankments, diagnostics);
                    break;
                case "partialorchards":
                    ReadHexsideFlags(element, "partialorchard", orchards, diagnostics);
                    break;
                default:
                    if (DeferredElementNames.Contains(name))
                    {
                        deferred[name] = element.ToString(SaveOptions.DisableFormatting);
                        diagnostics.Add(new MapDiagnostic("VASL-META-003", MapDiagnosticSeverity.Info,
                            $"<{name}> is preserved but not interpreted in version 1."));
                    }
                    else
                    {
                        diagnostics.Add(new MapDiagnostic("VASL-META-001", MapDiagnosticSeverity.Warning,
                            $"Unknown element <{name}> is not interpreted."));
                    }

                    break;
            }
        }

        if (diagnostics.Exists(diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error))
        {
            return new BoardMetadataResult(null, diagnostics);
        }

        var metadata = new BoardMetadata
        {
            Name = (string?)root.Attribute("name") ?? string.Empty,
            Version = (string?)root.Attribute("version") ?? string.Empty,
            VersionDate = (string?)root.Attribute("versionDate") ?? string.Empty,
            Author = (string?)root.Attribute("author") ?? string.Empty,
            BoardImageFileName = (string?)root.Attribute("boardImageFileName") ?? string.Empty,
            HasHills = hasHills,
            Width = width,
            Height = height,
            GeometryAttributes = geometryAttributes,
            BuildingTypes = LastWins(buildingTypes, item => item.Hex),
            Slopes = LastWins(slopes, item => item.Hex),
            RailroadEmbankments = LastWins(embankments, item => item.Hex),
            PartialOrchards = LastWins(orchards, item => item.Hex),
            DeferredElements = deferred,
        };
        return new BoardMetadataResult(metadata, diagnostics);
    }

    private static void ReadBuildingTypes(XElement element, List<BuildingTypeOverride> buildingTypes, List<MapDiagnostic> diagnostics)
    {
        foreach (var child in element.Elements("buildingType"))
        {
            var hexText = (string?)child.Attribute("hexName");
            var typeName = (string?)child.Attribute("buildingTypeName");
            if (!HexName.TryParse(hexText, out var hex) || string.IsNullOrEmpty(typeName))
            {
                diagnostics.Add(Error("VASL-META-005", $"buildingType has invalid hexName '{hexText}' or buildingTypeName '{typeName}'."));
                continue;
            }

            buildingTypes.Add(new BuildingTypeOverride(hex, typeName));
        }
    }

    // VASL reads at most the first six characters, each a digit naming one hexside; anything else rejects the metadata.
    private static void ReadHexsideFlags(XElement element, string childName, List<HexsideFlags> target, List<MapDiagnostic> diagnostics)
    {
        foreach (var child in element.Elements(childName))
        {
            var hexText = (string?)child.Attribute("hex");
            var sidesText = (string?)child.Attribute("hexsides") ?? string.Empty;
            if (!HexName.TryParse(hexText, out var hex))
            {
                diagnostics.Add(Error("VASL-META-005", $"<{childName}> has invalid hex '{hexText}'."));
                continue;
            }

            var sides = new SortedSet<HexsideDirection>();
            foreach (var character in sidesText.AsSpan(0, Math.Min(6, sidesText.Length)))
            {
                if (character is < '0' or > '5')
                {
                    diagnostics.Add(Error("VASL-META-005", $"<{childName} hex=\"{hexText}\"> has invalid hexsides '{sidesText}'."));
                    sides = null;
                    break;
                }

                sides.Add((HexsideDirection)(character - '0'));
            }

            if (sides is not null)
            {
                target.Add(new HexsideFlags(hex, [.. sides]));
            }
        }
    }

    private static List<T> LastWins<T>(List<T> items, Func<T, HexName> key)
    {
        var positions = new Dictionary<HexName, int>();
        var result = new List<T>();
        foreach (var item in items)
        {
            if (positions.TryGetValue(key(item), out var position))
            {
                result[position] = item;
            }
            else
            {
                positions[key(item)] = result.Count;
                result.Add(item);
            }
        }

        return result;
    }

    private static int RequiredInt(XElement root, string attribute, List<MapDiagnostic> diagnostics)
    {
        var value = ((string?)root.Attribute(attribute))?.Trim();
        if (value is not null && int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        diagnostics.Add(Error("VASL-META-005", $"boardMetadata has missing or non-integer {attribute} '{value}'."));
        return 0;
    }

    private static bool RequiredBoolean(XElement root, string attribute, List<MapDiagnostic> diagnostics)
    {
        var value = ((string?)root.Attribute(attribute))?.Trim();
        if (value is not null && JdomBoolean.TryParse(value, out var result))
        {
            return result;
        }

        diagnostics.Add(Error("VASL-META-005", $"boardMetadata has missing or non-boolean {attribute} '{value}'."));
        return false;
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);
}

/// <summary>JDOM <c>Attribute.getBooleanValue</c>: true/on/yes/1 and false/off/no/0, case-insensitive.</summary>
internal static class JdomBoolean
{
    public static bool TryParse(string value, out bool result)
    {
        var trimmed = value.Trim();
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("on", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase) || trimmed == "1")
        {
            result = true;
            return true;
        }

        result = false;
        return trimmed.Equals("false", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("off", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("no", StringComparison.OrdinalIgnoreCase) || trimmed == "0";
    }
}
