using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>The feature kind a terrain code belongs to. Paint order follows this order (Model Design section 5.3).</summary>
public enum TerrainKind
{
    Base,
    Area,
    Linear,
    Bridge,
    Building,
    Hexside,
}

/// <summary>Classifies catalog codes into feature kinds, for the vectorizer, authoring commands, and validation.</summary>
public static class TerrainKinds
{
    /// <summary>The kind of every code 0 to 255; codes absent from the catalog are area terrain.</summary>
    public static TerrainKind[] Classify(TerrainCatalog catalog, byte baseCode)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var kinds = new TerrainKind[256];
        Array.Fill(kinds, TerrainKind.Area);
        foreach (var type in catalog.Types)
        {
            kinds[type.Code] = type.Code == baseCode ? TerrainKind.Base : KindOf(type);
        }

        return kinds;
    }

    public static TerrainKind KindOf(TerrainType type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return type switch
        {
            { IsHexsideTerrain: true } => TerrainKind.Hexside,
            { IsBuilding: true } => TerrainKind.Building,
            { IsBridge: true } => TerrainKind.Bridge,
            _ when type.IsRoad || type.IsDepression || type.Category == LosCategory.Stream || IsLinearName(type.Name) => TerrainKind.Linear,
            _ => TerrainKind.Area,
        };
    }

    /// <summary>The kind a feature's codes must have; null for fidelity pins, which accept any code.</summary>
    public static TerrainKind? RequiredKind(Feature feature) => feature switch
    {
        AreaTerrainFeature => TerrainKind.Area,
        LinearTerrainFeature => TerrainKind.Linear,
        BridgeFeature => TerrainKind.Bridge,
        BuildingFeature => TerrainKind.Building,
        HexsideTerrainFeature => TerrainKind.Hexside,
        _ => null,
    };

    public static byte? CodeOf(Feature feature) => feature switch
    {
        AreaTerrainFeature area => area.Code,
        LinearTerrainFeature linear => linear.Code,
        BridgeFeature bridge => bridge.Code,
        BuildingFeature building => building.Code,
        HexsideTerrainFeature hexside => hexside.Code,
        FidelityPin pin => pin.Code,
        _ => null,
    };

    private static bool IsLinearName(string name) =>
        name.Contains("Railroad", StringComparison.Ordinal) || name.Contains("Runway", StringComparison.Ordinal)
        || name.Contains("Path", StringComparison.Ordinal) || name.Contains("Trail", StringComparison.Ordinal);
}
