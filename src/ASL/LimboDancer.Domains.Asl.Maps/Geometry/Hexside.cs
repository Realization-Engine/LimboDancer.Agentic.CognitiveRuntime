namespace LimboDancer.Domains.Asl.Maps.Geometry;

/// <summary>Hexsides of a flat-top hex, numbered clockwise from the top as in VASL.</summary>
public enum HexsideDirection
{
    North = 0,
    NorthEast = 1,
    SouthEast = 2,
    South = 3,
    SouthWest = 4,
    NorthWest = 5,
}

public static class HexsideDirections
{
    public static IReadOnlyList<HexsideDirection> All
    {
        get;
    } =
    [
        HexsideDirection.North,
        HexsideDirection.NorthEast,
        HexsideDirection.SouthEast,
        HexsideDirection.South,
        HexsideDirection.SouthWest,
        HexsideDirection.NorthWest,
    ];

    public static HexsideDirection Opposite(this HexsideDirection side) => (HexsideDirection)(((int)side + 3) % 6);

    public static bool IsDefined(int value) => value is >= 0 and <= 5;
}

/// <summary>One side of one hex. A shared hexside has two refs; see <see cref="BoardGeometry.Canonicalize"/>.</summary>
public readonly record struct HexsideRef(HexIndex Hex, HexsideDirection Side);
