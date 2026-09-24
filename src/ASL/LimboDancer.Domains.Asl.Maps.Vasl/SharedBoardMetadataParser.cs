using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>The terrain catalog from <c>SharedBoardMetadata.xml</c>, or diagnostics explaining why there is none.</summary>
public sealed record SharedBoardMetadataResult(TerrainCatalog? Catalog, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Catalog is not null;
}

/// <summary>
/// Reads the <c>terrainTypes</c> section of VASL's <c>SharedBoardMetadata.xml</c>, following
/// <c>VASL.build.module.map.boardArchive.SharedBoardMetadata.parseTerrainTypes</c>. Other sections
/// (colors, SSR rules, counters) are outside version 1 scope and are not read.
/// </summary>
public static class SharedBoardMetadataParser
{
    private static readonly Dictionary<string, LosCategory> Categories = new(StringComparer.Ordinal)
    {
        ["HEXSIDE"] = LosCategory.Hexside,
        ["BUILDING"] = LosCategory.Building,
        ["MARKETPLACE"] = LosCategory.Marketplace,
        ["FACTORY"] = LosCategory.Factory,
        ["OPEN"] = LosCategory.Open,
        ["ENTRENCHMENT"] = LosCategory.Entrenchment,
        ["BRIDGE"] = LosCategory.Bridge,
        ["TUNNEL"] = LosCategory.Tunnel,
        ["DEPRESSION"] = LosCategory.Depression,
        ["ROAD"] = LosCategory.Road,
        ["WOODS"] = LosCategory.Woods,
        ["STREAM"] = LosCategory.Stream,
        ["WATER"] = LosCategory.Water,
        ["OTHER"] = LosCategory.Other,
    };

    public static SharedBoardMetadataResult Parse(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        XDocument document;
        try
        {
            document = XDocument.Load(stream, LoadOptions.None);
        }
        catch (XmlException exception)
        {
            return Failed(Error("VASL-CAT-000", $"SharedBoardMetadata.xml is not well-formed XML: {exception.Message}"));
        }

        return Parse(document);
    }

    public static SharedBoardMetadataResult Parse(XDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var terrainTypes = document.Root?.Element("terrainTypes");
        if (terrainTypes is null)
        {
            return Failed(Error("VASL-CAT-000", "SharedBoardMetadata.xml has no terrainTypes element."));
        }

        var diagnostics = new List<MapDiagnostic>();
        var types = new List<TerrainType>();
        var codes = new HashSet<int>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in terrainTypes.Elements("terrainType"))
        {
            var type = ReadTerrainType(element, diagnostics);
            if (type is null)
            {
                continue;
            }

            if (!codes.Add(type.Code))
            {
                diagnostics.Add(Error("VASL-CAT-001", $"Duplicate terrain code {type.Code} ('{type.Name}')."));
                continue;
            }

            if (!names.Add(type.Name))
            {
                diagnostics.Add(Error("VASL-CAT-004", $"Duplicate terrain name '{type.Name}' (code {type.Code})."));
                continue;
            }

            types.Add(type);
        }

        return diagnostics.Exists(diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error)
            ? new SharedBoardMetadataResult(null, diagnostics)
            : new SharedBoardMetadataResult(new TerrainCatalog(types), diagnostics);
    }

    private static TerrainType? ReadTerrainType(XElement element, List<MapDiagnostic> diagnostics)
    {
        var name = (string?)element.Attribute("name");
        var label = name ?? "(unnamed)";
        var errorCount = diagnostics.Count;
        var code = ReadInt(element, "typeCode", label, diagnostics);
        var categoryText = (string?)element.Attribute("LOSCategory");
        LosCategory category = default;
        if (categoryText is null || !Categories.TryGetValue(categoryText, out category))
        {
            diagnostics.Add(Error("VASL-CAT-003", $"Terrain '{label}' has unknown LOSCategory '{categoryText}'."));
        }

        var type = new TerrainType
        {
            Code = code is >= 0 and <= 255 ? (byte)code : (byte)0,
            Name = name ?? string.Empty,
            Category = category,
            IsLosObstacle = ReadBoolean(element, "isLOSObstacle", label, diagnostics),
            IsLosHindrance = ReadBoolean(element, "isLOSHindrance", label, diagnostics),
            IsHalfLevelHeight = ReadBoolean(element, "isHalfLevelHeight", label, diagnostics),
            IsInherent = ReadBoolean(element, "isInherentTerrain", label, diagnostics),
            Split = ReadFloat(element, "split", label, diagnostics),
            IsLowerLosHindrance = ReadBoolean(element, "isLowerLOSHindrance", label, diagnostics),
            IsLowerLosObstacle = ReadBoolean(element, "isLowerLOSObstacle", label, diagnostics),
            Height = ReadInt(element, "height", label, diagnostics),
            MapColor = new TerrainColor(
                ReadColor(element, "mapColorRed", label, diagnostics),
                ReadColor(element, "mapColorGreen", label, diagnostics),
                ReadColor(element, "mapColorBlue", label, diagnostics)),
        };

        if (name is null)
        {
            diagnostics.Add(Error("VASL-CAT-005", "A terrainType element has no name attribute."));
        }

        if (code is < 0 or > 255)
        {
            diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has typeCode {code}, outside 0 to 255."));
        }

        return diagnostics.Count == errorCount ? type : null;
    }

    private static string? RequireAttribute(XElement element, string attribute, string label, List<MapDiagnostic> diagnostics)
    {
        var value = (string?)element.Attribute(attribute);
        if (value is null)
        {
            diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has no {attribute} attribute."));
        }

        return value?.Trim();
    }

    // Java Integer.parseInt on the trimmed value, as JDOM's getIntValue does.
    private static int ReadInt(XElement element, string attribute, string label, List<MapDiagnostic> diagnostics)
    {
        var value = RequireAttribute(element, attribute, label, diagnostics);
        if (value is null)
        {
            return 0;
        }

        if (int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has non-integer {attribute} '{value}'."));
        return 0;
    }

    private static byte ReadColor(XElement element, string attribute, string label, List<MapDiagnostic> diagnostics)
    {
        var value = ReadInt(element, attribute, label, diagnostics);
        if (value is >= 0 and <= 255)
        {
            return (byte)value;
        }

        diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has {attribute} {value}, outside 0 to 255."));
        return 0;
    }

    private static float ReadFloat(XElement element, string attribute, string label, List<MapDiagnostic> diagnostics)
    {
        var value = RequireAttribute(element, attribute, label, diagnostics);
        if (value is null)
        {
            return 0f;
        }

        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
        {
            return result;
        }

        diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has non-numeric {attribute} '{value}'."));
        return 0f;
    }

    // JDOM Attribute.getBooleanValue: true/on/yes/1 and false/off/no/0, case-insensitive, trimmed.
    private static bool ReadBoolean(XElement element, string attribute, string label, List<MapDiagnostic> diagnostics)
    {
        var value = RequireAttribute(element, attribute, label, diagnostics);
        if (value is null)
        {
            return false;
        }

        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value.Equals("on", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase) || value == "1")
        {
            return true;
        }

        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) || value.Equals("off", StringComparison.OrdinalIgnoreCase)
            || value.Equals("no", StringComparison.OrdinalIgnoreCase) || value == "0")
        {
            return false;
        }

        diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has non-boolean {attribute} '{value}'."));
        return false;
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);

    private static SharedBoardMetadataResult Failed(MapDiagnostic diagnostic) => new(null, [diagnostic]);
}
