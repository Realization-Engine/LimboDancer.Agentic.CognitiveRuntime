using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>The terrain catalog from <c>SharedBoardMetadata.xml</c>, or diagnostics explaining why there is none.</summary>
public sealed record SharedBoardMetadataResult(TerrainCatalog? Catalog, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Catalog is not null;

    /// <summary>The LOS scenario-specific rules (<c>LOSSSRules</c>), empty when the file has none.</summary>
    public LosSsRuleSet Rules { get; init; } = LosSsRuleSet.Empty;
}

/// <summary>
/// Reads the <c>terrainTypes</c> and <c>LOSSSRules</c> sections of VASL's <c>SharedBoardMetadata.xml</c>, following
/// <c>SharedBoardMetadata.parseTerrainTypes</c> and <c>AbstractMetadata.parseLOSSSRules</c>. Colors and counter rules
/// concern artwork and counters and are not read.
/// </summary>
public static class SharedBoardMetadataParser
{
    private static readonly Dictionary<string, LosSsRuleKind> RuleKinds = new(StringComparer.Ordinal)
    {
        ["ignore"] = LosSsRuleKind.Ignore,
        ["customCode"] = LosSsRuleKind.CustomCode,
        ["terrainMap"] = LosSsRuleKind.TerrainMap,
        ["elevationMap"] = LosSsRuleKind.ElevationMap,
        ["terrainToElevationMap"] = LosSsRuleKind.TerrainToElevationMap,
        ["elevationToTerrainMap"] = LosSsRuleKind.ElevationToTerrainMap,
        ["terrainToSelectElevationMap"] = LosSsRuleKind.TerrainToSelectElevationMap,
    };

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

        var rules = ReadRules(document.Root!.Element("LOSSSRules"), diagnostics);
        return diagnostics.Exists(diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error)
            ? new SharedBoardMetadataResult(null, diagnostics)
            : new SharedBoardMetadataResult(new TerrainCatalog(types), diagnostics) { Rules = rules };
    }

    // AbstractMetadata.parseLOSSSRules. VASL rejects the whole file for an invalid rule type; here the rule is skipped
    // with a warning, so the terrain catalog stays usable and only that rule is unavailable.
    private static LosSsRuleSet ReadRules(XElement? element, List<MapDiagnostic> diagnostics)
    {
        if (element is null)
        {
            return LosSsRuleSet.Empty;
        }

        var rules = new List<LosSsRule>();
        foreach (var rule in element.Elements("LOSSSRule"))
        {
            var name = (string?)rule.Attribute("name");
            var type = (string?)rule.Attribute("type");
            if (name is null || type is null || !RuleKinds.TryGetValue(type, out var kind))
            {
                diagnostics.Add(new MapDiagnostic("VASL-CAT-006", MapDiagnosticSeverity.Warning,
                    $"Invalid LOS scenario-specific rule type '{type}' for rule '{name}'."));
                continue;
            }

            rules.Add(new LosSsRule(name, kind, (string?)rule.Attribute("fromValue") ?? string.Empty, (string?)rule.Attribute("toValue") ?? string.Empty));
        }

        return new LosSsRuleSet(rules);
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

        if (JdomBoolean.TryParse(value, out var result))
        {
            return result;
        }

        diagnostics.Add(Error("VASL-CAT-005", $"Terrain '{label}' has non-boolean {attribute} '{value}'."));
        return false;
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);

    private static SharedBoardMetadataResult Failed(MapDiagnostic diagnostic) => new(null, [diagnostic]);
}
