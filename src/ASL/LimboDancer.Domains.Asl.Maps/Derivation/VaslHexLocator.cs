using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>
/// VASL's pixel-to-hex lookup, <c>Map.gridToHex</c>. By default it runs as the LOS editor runs it when it creates
/// <c>LOSData</c>: the map is built with the "Normal" grid configuration (<c>LOSDataEditor.createNewLOSData</c>). The
/// compiler's cliff and sunken-road post-passes use it, so compiled grids follow the editor's conventions exactly. With
/// <c>runtime</c> set it runs as the game runtime's configuration <c>HalfHexWidthLeftHexFullHeight</c> does, which
/// skips the "Normal" branch; composition and scenario-specific rules use that. Quirks are kept: only the "grey area"
/// between columns tests polygon containment, and indexing errors return null.
/// </summary>
public sealed class VaslHexLocator
{
    private readonly BoardGeometry geometry;
    private readonly bool runtime;
    private readonly JavaPolygon[][] borders;
    private readonly JavaPolygon[][] extendedBorders;

    public VaslHexLocator(BoardGeometry geometry, bool runtime = false)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        this.geometry = geometry;
        this.runtime = runtime;
        var extension = geometry.HexWidth > 168 && geometry.HexHeight > 194 ? 2 : 1;
        borders = new JavaPolygon[geometry.WidthInHexes][];
        extendedBorders = new JavaPolygon[geometry.WidthInHexes][];
        for (var column = 0; column < geometry.WidthInHexes; column++)
        {
            borders[column] = new JavaPolygon[geometry.RowCount(column)];
            extendedBorders[column] = new JavaPolygon[geometry.RowCount(column)];
            for (var row = 0; row < geometry.RowCount(column); row++)
            {
                var hex = new HexIndex(column, row);
                borders[column][row] = new JavaPolygon(geometry.Border(hex));
                extendedBorders[column][row] = new JavaPolygon(ExtendedBorder(geometry.Vertices(hex), extension));
            }
        }
    }

    /// <summary>The hex VASL's LOS editor assigns to a pixel, or null where it finds none.</summary>
    public HexIndex? GridToHex(int x, int y)
    {
        if (!geometry.ContainsCell(x, y))
        {
            return null;
        }

        var hexWidth = geometry.HexWidth;
        var hexHeight = geometry.HexHeight;
        var z = (int)(x / (hexWidth / 3.0));
        int column;
        int row;
        if ((z - 1) % 3 == 0)
        {
            column = (int)Math.Ceiling((z - 1.0) / 3.0);
            row = (int)(column % 2 == 0 ? y / hexHeight : (y + (hexHeight / 2.0)) / hexHeight);
            if (!Exists(column, row))
            {
                return null;
            }

            if (Contains(column, row, x, y))
            {
                return new HexIndex(column, row);
            }

            if (column < Columns && row - 1 >= 0 && row - 1 < Rows(column) && Contains(column, row - 1, x, y))
            {
                return new HexIndex(column, row - 1);
            }

            if (column < Columns && row + 1 < Rows(column) && Contains(column, row + 1, x, y))
            {
                return new HexIndex(column, row + 1);
            }

            if (column + 1 < Columns && row < Rows(column + 1) && Contains(column + 1, row, x, y))
            {
                return new HexIndex(column + 1, row);
            }

            if (column + 1 < Columns && row - 1 >= 0 && row - 1 < Rows(column + 1) && Contains(column + 1, row - 1, x, y))
            {
                return new HexIndex(column + 1, row - 1);
            }

            if (column + 1 < Columns && row + 1 < Rows(column + 1) && Contains(column + 1, row + 1, x, y))
            {
                return new HexIndex(column + 1, row + 1);
            }

            if (column - 1 >= 0 && row < Rows(column - 1) && Contains(column - 1, row, x, y))
            {
                return new HexIndex(column - 1, row);
            }

            if (column - 1 >= 0 && row - 1 >= 0 && row - 1 < Rows(column - 1) && Contains(column - 1, row - 1, x, y))
            {
                return new HexIndex(column - 1, row - 1);
            }

            if (column - 1 >= 0 && row + 1 < Rows(column - 1))
            {
                if (Contains(column - 1, row + 1, x, y))
                {
                    return new HexIndex(column - 1, row + 1);
                }
            }
            else if (column % 2 == 0 && !runtime)
            {
                // The "Normal" configuration branch.
                if (column + 1 < Columns && row + 1 < Rows(column + 1))
                {
                    if (Contains(column + 1, row + 1, x, y))
                    {
                        return new HexIndex(column + 1, row + 1);
                    }

                    if (row < Rows(column + 1))
                    {
                        return Contains(column + 1, row, x, y) ? new HexIndex(column + 1, row) : null;
                    }
                }
            }
            else if (column + 1 < Columns && row - 1 >= 0 && row - 1 < Rows(column + 1))
            {
                if (Contains(column + 1, row - 1, x, y) || row == geometry.HeightInHexes)
                {
                    return new HexIndex(column + 1, row - 1);
                }

                if (row >= Rows(column + 1))
                {
                    return null;
                }

                if (Contains(column + 1, row, x, y))
                {
                    return new HexIndex(column + 1, row);
                }
            }

            return extendedBorders[column][row].Contains(x, y) ? new HexIndex(column, row) : null;
        }

        column = (int)Math.Ceiling(z / 3.0);
        row = (int)(column % 2 == 0 ? y / hexHeight : (y + (hexHeight / 2.0)) / hexHeight);
        return Exists(column, row) ? new HexIndex(column, row) : null;
    }

    private int Columns => borders.Length;

    private int Rows(int column) => borders[column].Length;

    private bool Exists(int column, int row) => column >= 0 && column < Columns && row >= 0 && row < Rows(column);

    private bool Contains(int column, int row, int x, int y) => borders[column][row].Contains(x, y);

    // Hex.initHexNew: the extended border truncates each vertex and moves it outward, two pixels on Deluxe hexes.
    private static GridPoint[] ExtendedBorder(IReadOnlyList<PixelPoint> vertices, int extension)
    {
        (int Dx, int Dy)[] offsets = [(-1, -1), (1, -1), (1, 0), (1, 1), (-1, 1), (-1, 0)];
        var points = new GridPoint[6];
        for (var index = 0; index < 6; index++)
        {
            points[index] = new GridPoint((int)vertices[index].X + (offsets[index].Dx * extension), (int)vertices[index].Y + (offsets[index].Dy * extension));
        }

        return points;
    }
}
