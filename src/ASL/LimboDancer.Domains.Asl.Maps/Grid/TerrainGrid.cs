using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Grid;

/// <summary>
/// The canonical terrain of a board (ASL-MAP-011): a terrain code and an elevation for every grid cell,
/// plus a stairway flag for every hex. Cells are stored column-major (index = x * GridHeight + y), the
/// order VASL writes in LOSData. The grid is a data structure, never shown except through SVG views.
/// </summary>
public sealed class TerrainGrid
{
    private readonly byte[] codes;
    private readonly sbyte[] elevations;
    private readonly bool[] stairways;

    public TerrainGrid(BoardGeometry geometry, ReadOnlySpan<byte> codes, ReadOnlySpan<sbyte> elevations, ReadOnlySpan<bool> stairways)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var cellCount = checked(geometry.GridWidth * geometry.GridHeight);
        if (codes.Length != cellCount || elevations.Length != cellCount)
        {
            throw new ArgumentException($"A {geometry.GridWidth} by {geometry.GridHeight} grid needs {cellCount} codes and elevations.");
        }

        if (stairways.Length != geometry.HexCount)
        {
            throw new ArgumentException($"The board has {geometry.HexCount} hexes but {stairways.Length} stairway flags were supplied.", nameof(stairways));
        }

        Geometry = geometry;
        this.codes = codes.ToArray();
        this.elevations = elevations.ToArray();
        this.stairways = stairways.ToArray();
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public int CellCount => codes.Length;

    /// <summary>Terrain codes in column-major order.</summary>
    public ReadOnlySpan<byte> Codes => codes;

    /// <summary>Elevations in levels, in column-major order.</summary>
    public ReadOnlySpan<sbyte> Elevations => elevations;

    /// <summary>Stairway flags in VASL hex order (<see cref="BoardGeometry.HexOrdinal"/>).</summary>
    public ReadOnlySpan<bool> Stairways => stairways;

    public byte CodeAt(int x, int y) => codes[CellIndex(x, y)];

    public sbyte ElevationAt(int x, int y) => elevations[CellIndex(x, y)];

    /// <summary>The code at a cell, or false when the cell is off the grid (VASL's <c>getGridTerrain</c> returns null).</summary>
    public bool TryGetCode(int x, int y, out byte code)
    {
        if (!Geometry.ContainsCell(x, y))
        {
            code = 0;
            return false;
        }

        code = codes[(x * Geometry.GridHeight) + y];
        return true;
    }

    public bool HasStairway(HexIndex hex) => stairways[Geometry.HexOrdinal(hex)];

    /// <summary>Every distinct terrain code used by the grid, in ascending order.</summary>
    public IReadOnlyList<byte> DistinctCodes()
    {
        var seen = new bool[256];
        foreach (var code in codes)
        {
            seen[code] = true;
        }

        var result = new List<byte>();
        for (var code = 0; code < seen.Length; code++)
        {
            if (seen[code])
            {
                result.Add((byte)code);
            }
        }

        return result;
    }

    private int CellIndex(int x, int y)
    {
        if (!Geometry.ContainsCell(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Cell ({x}, {y}) is outside the {Geometry.GridWidth} by {Geometry.GridHeight} grid.");
        }

        return (x * Geometry.GridHeight) + y;
    }
}
