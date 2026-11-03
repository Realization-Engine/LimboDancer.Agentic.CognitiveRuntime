using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>The lines painted into a terrain tile with it (Screen-Bounded Map Analysis, section 3.1, decision N1).</summary>
[Flags]
public enum TileLines
{
    None = 0,

    /// <summary>The hex grid.</summary>
    Grid = 1,

    /// <summary>The Formation boundaries of a Campaign game.</summary>
    Boundaries = 2,
}

/// <summary>
/// Paints the hex grid and the Formation boundaries into a tile's pixels, as the Map Lab's <c>tileslines</c> did (Map Lab Plan, section 8.1):
/// a pixel whose hex differs from the hex of the pixel to its right or below is a line pixel, a boundary where the two hexes' parents differ,
/// else the grid. Lines are one tile pixel wide at every level and are left out of a level where they would stand under
/// <see cref="MinimumPixels"/> apart, so far zoom is not a mesh.
/// </summary>
public static class TileLinePainter
{
    /// <summary>The fewest tile pixels across a hex (or a formation) at which its lines are painted.</summary>
    public const double MinimumPixels = 6;

    /// <summary>The value of a hex no parent owns, in a parent array.</summary>
    public const int Unowned = -1;

    // The Exact view's grid: black at opacity 0.45 (BoardRenderer.WriteGrid); the boundaries: the vector overlay's colour, opaque.
    private const int GridKeep = 55;
    private const byte BoundaryRed = 0x9b;
    private const byte BoundaryGreen = 0x5f;
    private const byte BoundaryBlue = 0x17;

    /// <summary>Which of the asked lines a level shows: the grid where a hex is wide enough, the boundaries where a formation is.</summary>
    public static TileLines Shown(BoardGeometry geometry, TileLines lines, int level, double hexesPerParent)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var hexPixels = geometry.HexWidth / (1 << level);
        var shown = TileLines.None;
        if (lines.HasFlag(TileLines.Grid) && hexPixels >= MinimumPixels)
        {
            shown |= TileLines.Grid;
        }

        if (lines.HasFlag(TileLines.Boundaries) && hexPixels * Math.Sqrt(Math.Max(1, hexesPerParent)) >= MinimumPixels)
        {
            shown |= TileLines.Boundaries;
        }

        return shown;
    }

    /// <summary>
    /// Paints the lines over an RGB tile of width by height pixels whose top left is (<paramref name="originX"/>, <paramref name="originY"/>)
    /// in cells, each pixel <paramref name="block"/> cells on a side. <paramref name="parents"/> holds each hex's parent by ordinal
    /// (<see cref="BoardGeometry.HexOrdinal"/>), or <see cref="Unowned"/>; it is read only for the boundaries.
    /// </summary>
    public static void Paint(BoardGeometry geometry, TileLines lines, ReadOnlySpan<int> parents, int originX, int originY, int block,
        Span<byte> pixels, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (lines == TileLines.None)
        {
            return;
        }

        var boundaries = lines.HasFlag(TileLines.Boundaries) && parents.Length == geometry.HexCount;
        var grid = lines.HasFlag(TileLines.Grid);
        var stride = width + 1;
        // The hex under each pixel's centre, one column and row beyond the tile, so a tile's last pixels compare with the next tile's first.
        var centres = new int[stride * (height + 1)];
        for (var py = 0; py <= height; py++)
        {
            var y = originY + ((py + 0.5) * block);
            for (var px = 0; px <= width; px++)
            {
                centres[(py * stride) + px] = OrdinalAt(geometry, originX + ((px + 0.5) * block), y);
            }
        }

        for (var py = 0; py < height; py++)
        {
            for (var px = 0; px < width; px++)
            {
                var here = centres[(py * stride) + px];
                var right = centres[(py * stride) + px + 1];
                var below = centres[((py + 1) * stride) + px];
                if (here == right && here == below)
                {
                    continue;
                }

                var at = ((py * width) + px) * 3;
                if (boundaries && (Differ(parents, here, right) || Differ(parents, here, below)))
                {
                    pixels[at] = BoundaryRed;
                    pixels[at + 1] = BoundaryGreen;
                    pixels[at + 2] = BoundaryBlue;
                }
                else if (grid)
                {
                    pixels[at] = (byte)(pixels[at] * GridKeep / 100);
                    pixels[at + 1] = (byte)(pixels[at + 1] * GridKeep / 100);
                    pixels[at + 2] = (byte)(pixels[at + 2] * GridKeep / 100);
                }
            }
        }
    }

    /// <summary>The ordinal of the hex whose centre dot is nearest the point, clamped to the board: the lattice arithmetic of
    /// <see cref="BoardGeometry.CenterDot"/> over the three nearest columns and rows, without the right edge's one-cell clamp.</summary>
    public static int OrdinalAt(BoardGeometry geometry, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var near = (int)Math.Round((x - geometry.A1CenterX) / geometry.HexWidth);
        var best = 0;
        var bestDistance = double.MaxValue;
        for (var column = Math.Max(0, near - 1); column <= Math.Min(geometry.WidthInHexes - 1, near + 1); column++)
        {
            var offset = geometry.HexHeight / 2 * (column % 2);
            var middle = (int)Math.Round((y - geometry.A1CenterY + offset) / geometry.HexHeight);
            var cx = geometry.A1CenterX + (column * geometry.HexWidth);
            for (var row = Math.Max(0, middle - 1); row <= Math.Min(geometry.RowCount(column) - 1, middle + 1); row++)
            {
                var cy = geometry.A1CenterY + (geometry.HexHeight * row) - offset;
                var distance = ((cx - x) * (cx - x)) + ((cy - y) * (cy - y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (column * geometry.HeightInHexes) + (column / 2) + row;
                }
            }
        }

        return best;
    }

    // A boundary lies between two hexes of different parents where at least one is owned: the perimeter of the owned area included.
    private static bool Differ(ReadOnlySpan<int> parents, int a, int b) =>
        parents[a] != parents[b] && (parents[a] != Unowned || parents[b] != Unowned);
}
