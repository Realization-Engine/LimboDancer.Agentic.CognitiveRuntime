namespace LimboDancer.Domains.Asl.Units.Documents;

/// <summary>
/// The hexspine a Gun's barrel points at, which defines its Covered Arc (C3.2, p. 169). ASL hexes are flat-topped, so
/// the six hexspines lie east, north-east, north-west, west, south-west, and south-east of the hex center.
/// </summary>
public enum UnitFacing
{
    East,
    NorthEast,
    NorthWest,
    West,
    SouthWest,
    SouthEast,
}

public static class UnitFacings
{
    private static readonly string[] Names = ["east", "north-east", "north-west", "west", "south-west", "south-east"];

    /// <summary>The name documents use, such as <c>north-east</c>.</summary>
    public static string Name(this UnitFacing facing) => Names[(int)facing];

    public static string Label(this UnitFacing facing) => Names[(int)facing];

    public static bool TryParse(string? text, out UnitFacing facing)
    {
        var index = Array.IndexOf(Names, text);
        facing = index < 0 ? UnitFacing.East : (UnitFacing)index;
        return index >= 0;
    }

    /// <summary>The direction in SVG degrees: clockwise from east, since SVG's y axis points down.</summary>
    public static int Degrees(this UnitFacing facing) => facing switch
    {
        UnitFacing.East => 0,
        UnitFacing.NorthEast => -60,
        UnitFacing.NorthWest => -120,
        UnitFacing.West => 180,
        UnitFacing.SouthWest => 120,
        _ => 60,
    };

    public static IReadOnlyList<string> All => Names;
}

/// <summary>A hexside of a flat-topped hex, which a roadblock's arrow points at (B29.1, p. 150).</summary>
public enum UnitHexside
{
    North,
    NorthEast,
    SouthEast,
    South,
    SouthWest,
    NorthWest,
}

public static class UnitHexsides
{
    private static readonly string[] Names = ["north", "north-east", "south-east", "south", "south-west", "north-west"];

    public static string Name(this UnitHexside hexside) => Names[(int)hexside];

    public static bool TryParse(string? text, out UnitHexside hexside)
    {
        var index = Array.IndexOf(Names, text);
        hexside = index < 0 ? UnitHexside.North : (UnitHexside)index;
        return index >= 0;
    }

    /// <summary>The direction of the hexside's midpoint from the hex center, in SVG degrees (clockwise from east).</summary>
    public static int Degrees(this UnitHexside hexside) => -90 + ((int)hexside * 60);

    public static IReadOnlyList<string> All => Names;
}
