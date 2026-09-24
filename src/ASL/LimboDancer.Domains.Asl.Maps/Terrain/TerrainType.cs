namespace LimboDancer.Domains.Asl.Maps.Terrain;

/// <summary>VASL's LOS categories (<c>VASL.LOS.Map.Terrain.LOSCategories</c>).</summary>
public enum LosCategory
{
    Hexside,
    Building,
    Marketplace,
    Factory,
    Open,
    Entrenchment,
    Bridge,
    Tunnel,
    Depression,
    Road,
    Woods,
    Stream,
    Water,
    Other,
}

public readonly record struct TerrainColor(byte Red, byte Green, byte Blue)
{
    /// <summary>Lowercase <c>#rrggbb</c>, for SVG output.</summary>
    public string ToHex() => $"#{Red:x2}{Green:x2}{Blue:x2}";
}

/// <summary>
/// One terrain type from VASL's shared board metadata. Classification predicates reproduce
/// <c>VASL.LOS.Map.Terrain</c> exactly, including the rules VASL makes by exact terrain name.
/// </summary>
public sealed record TerrainType
{
    private static readonly HashSet<string> RowhouseFactoryWallOrBreachNames = new(StringComparer.Ordinal)
    {
        "Rowhouse Wall",
        "Rowhouse Wall, 1 Level",
        "Rowhouse Wall, 2 Level",
        "Rowhouse Wall, 3 Level",
        "Rowhouse Wall, 4 Level",
        "Breach",
        "Interior Factory Wall, 1 Level",
        "Interior Factory Wall, 2 Level",
    };

    private static readonly HashSet<string> OutsideFactoryWallNames = new(StringComparer.Ordinal)
    {
        "Stone Factory Wall, 1.5 Level",
        "Stone Factory Wall, 2.5 Level",
        "Wooden Factory Wall, 1.5 Level",
        "Wooden Factory Wall, 2.5 Level",
    };

    private static readonly HashSet<string> StreamNames = new(StringComparer.Ordinal)
    {
        "Dry Stream",
        "Shallow Stream",
        "Deep Stream",
        "Flooded Stream",
    };

    public required byte Code
    {
        get; init;
    }

    public required string Name
    {
        get; init;
    }

    public required LosCategory Category
    {
        get; init;
    }

    public bool IsLosObstacle
    {
        get; init;
    }

    public bool IsLosHindrance
    {
        get; init;
    }

    public bool IsLowerLosObstacle
    {
        get; init;
    }

    public bool IsLowerLosHindrance
    {
        get; init;
    }

    public bool IsHalfLevelHeight
    {
        get; init;
    }

    public bool IsInherent
    {
        get; init;
    }

    public int Height
    {
        get; init;
    }

    /// <summary>The split level (VASL stores it as a Java <c>float</c>); nonzero only for marketplace-style terrain.</summary>
    public float Split
    {
        get; init;
    }

    public TerrainColor MapColor
    {
        get; init;
    }

    public bool HasSplit => Split != 0f;

    public bool IsBuilding => Category is LosCategory.Building or LosCategory.Factory or LosCategory.Marketplace;

    /// <summary>VASL's <c>isBuildingTerrain</c>, which unlike <see cref="IsBuilding"/> excludes factories.</summary>
    public bool IsBuildingTerrain => Category is LosCategory.Marketplace or LosCategory.Building;

    public bool IsHexsideTerrain => Category == LosCategory.Hexside || IsRowhouseFactoryWallOrBreach;

    public bool IsOpen => Category is LosCategory.Open or LosCategory.Road or LosCategory.Water;

    public bool IsEntrenchment => Category == LosCategory.Entrenchment;

    public bool IsWater => Category == LosCategory.Water;

    public bool IsBridge => Category == LosCategory.Bridge;

    public bool IsTunnel => Category == LosCategory.Tunnel;

    public bool IsRoad => Category == LosCategory.Road;

    public bool IsDepression => Category == LosCategory.Depression;

    public bool IsFactory => Category == LosCategory.Factory;

    public bool IsRowhouseFactoryWallOrBreach => RowhouseFactoryWallOrBreachNames.Contains(Name);

    public bool IsOutsideFactoryWall => OutsideFactoryWallNames.Contains(Name);

    public bool IsStream => StreamNames.Contains(Name);

    public bool IsCellar => Name.Contains("Cellar", StringComparison.Ordinal);

    public bool IsRooftop => Name.Contains("Rooftop", StringComparison.Ordinal);

    public bool IsRoofless => Name.Contains("Roofless", StringComparison.Ordinal) || Name.Contains("Gutted", StringComparison.Ordinal);

    public bool IsCliff => Name.Contains("Cliff", StringComparison.Ordinal);
}
