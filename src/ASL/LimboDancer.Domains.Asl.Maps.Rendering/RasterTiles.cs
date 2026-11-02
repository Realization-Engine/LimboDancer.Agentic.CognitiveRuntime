using System.Buffers;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>What a tile shows, from the Exact view's two layer switches (Raster Tiles Plan, section 2.6).</summary>
public enum TileVariant
{
    /// <summary>Terrain colours with the elevation tint over them: both layers shown.</summary>
    TerrainAndElevation,

    /// <summary>Terrain colours alone: the elevation layer hidden.</summary>
    Terrain,

    /// <summary>The elevation tint alone, transparent at level 0: the terrain layer hidden.</summary>
    Elevation,
}

/// <summary>
/// The tile pyramid of a grid (Raster Tiles Plan, section 2.2). At level n one tile pixel stands for 2^n by 2^n grid cells, and a tile covers
/// <see cref="TileSize"/> pixels each way; levels go up until the whole grid fits one tile.
/// </summary>
public static class TilePyramid
{
    public const int TileSize = 512;

    /// <summary>The number of levels: level 0 and every coarser one up to the first whose single tile holds the grid.</summary>
    public static int LevelCount(int gridWidth, int gridHeight, int tileSize = TileSize)
    {
        var level = 0;
        while ((long)tileSize << level < gridWidth || (long)tileSize << level < gridHeight)
        {
            level++;
        }

        return level + 1;
    }

    /// <summary>The cells one tile covers on each side at a level.</summary>
    public static int Span(int level, int tileSize = TileSize) => tileSize << level;

    public static int Columns(int gridWidth, int level, int tileSize = TileSize) => Ceiling(gridWidth, Span(level, tileSize));

    public static int Rows(int gridHeight, int level, int tileSize = TileSize) => Ceiling(gridHeight, Span(level, tileSize));

    /// <summary>A tile's size in pixels: a full tile, or the part of one an edge cuts.</summary>
    public static (int Width, int Height) PixelSize(int gridWidth, int gridHeight, int level, int column, int row, int tileSize = TileSize)
    {
        var span = Span(level, tileSize);
        var cellsWide = Math.Min(span, gridWidth - (column * span));
        var cellsHigh = Math.Min(span, gridHeight - (row * span));
        return (Ceiling(cellsWide, 1 << level), Ceiling(cellsHigh, 1 << level));
    }

    /// <summary>Whether a level, column, and row name a tile of the grid.</summary>
    public static bool Contains(int gridWidth, int gridHeight, int level, int column, int row, int tileSize = TileSize) =>
        level >= 0 && level < LevelCount(gridWidth, gridHeight, tileSize)
        && column >= 0 && column < Columns(gridWidth, level, tileSize)
        && row >= 0 && row < Rows(gridHeight, level, tileSize);

    private static int Ceiling(int value, int divisor) => (value + divisor - 1) / divisor;
}

/// <summary>
/// Paints raster tiles of the Exact view straight from the terrain grid (Raster Tiles Plan, section 2.1). The Exact view fills each cell with
/// its catalog colour (<c>#ff00ff</c> for a code the catalog does not know) and lays black, above level 0, or <c>#1f4e79</c>, below it, over
/// the cell at opacity min(0.5, 0.12 x |level|), all with crisp edges along cell boundaries; so at level 0 a tile pixel is that one colour,
/// and at level n it is the average of its cells.
/// </summary>
public static class TilePainter
{
    private static readonly TerrainColor Unknown = new(0xff, 0x00, 0xff);
    private static readonly TerrainColor Below = new(0x1f, 0x4e, 0x79);

    /// <summary>Bytes per pixel: RGBA for the elevation variant, RGB otherwise.</summary>
    public static int Channels(TileVariant variant) => variant == TileVariant.Elevation ? 4 : 3;

    /// <summary>The overlay's opacity at a level, as the SVG elevation layer draws it (two decimals).</summary>
    public static double Opacity(int level) => level == 0 ? 0 : Math.Round(Math.Min(0.5, 0.12 * Math.Abs(level)), 2);

    /// <summary>A tile as a PNG.</summary>
    public static byte[] Png(TerrainGrid grid, TerrainCatalog catalog, TileVariant variant, int level, int column, int row, int tileSize = TilePyramid.TileSize)
    {
        ArgumentNullException.ThrowIfNull(grid);
        var geometry = grid.Geometry;
        var (width, height) = TilePyramid.PixelSize(geometry.GridWidth, geometry.GridHeight, level, column, row, tileSize);
        var length = width * height * Channels(variant);
        var pixels = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            Paint(grid, catalog, variant, level, column, row, pixels.AsSpan(0, length), tileSize);
            return PngEncoder.Encode(pixels.AsSpan(0, length), width, height, variant == TileVariant.Elevation);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(pixels);
        }
    }

    /// <summary>Paints a tile's pixels, rows top first, into a buffer of width x height x <see cref="Channels"/> bytes.</summary>
    public static void Paint(TerrainGrid grid, TerrainCatalog catalog, TileVariant variant, int level, int column, int row, Span<byte> pixels,
        int tileSize = TilePyramid.TileSize)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(catalog);
        var geometry = grid.Geometry;
        var gridWidth = geometry.GridWidth;
        var gridHeight = geometry.GridHeight;
        if (!TilePyramid.Contains(gridWidth, gridHeight, level, column, row, tileSize))
        {
            throw new ArgumentOutOfRangeException(nameof(level), $"No tile {level}/{column}/{row} on a {gridWidth} by {gridHeight} grid.");
        }

        var (width, height) = TilePyramid.PixelSize(gridWidth, gridHeight, level, column, row, tileSize);
        var channels = Channels(variant);
        if (pixels.Length < width * height * channels)
        {
            throw new ArgumentException($"A {width} by {height} tile needs {width * height * channels} bytes.", nameof(pixels));
        }

        // Every cell's colour depends only on its code and level, so each pair's colour is worked out once per tile: packed RGB for the
        // terrain variants, indexed by code x 256 + level; for the elevation variant, the overlay's colour premultiplied by its opacity in
        // 1/1024ths, indexed by level. The loop over the cells then only looks up and adds integers (a top tile reads every cell of the map).
        var table = ArrayPool<int>.Shared.Rent(65536);
        try
        {
            FillTable(table, catalog, variant);
            Sum(grid, variant, level, column, row, tileSize, table, pixels, width, height, channels);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(table);
        }
    }

    private const int OpacityScale = 1024;

    /// <summary>The most cells a tile pixel reads along each side; above level 3 it samples them evenly.</summary>
    public const int SamplesPerSide = 8;

    private static void FillTable(int[] table, TerrainCatalog catalog, TileVariant variant)
    {
        for (var value = -128; value < 128; value++)
        {
            var elevation = unchecked((byte)value);
            var opacity = Opacity(value);
            var overlay = value > 0 ? default : Below;
            if (variant == TileVariant.Elevation)
            {
                var weight = (int)Math.Round(opacity * OpacityScale);
                table[elevation] = weight;
                table[256 + elevation] = Pack(overlay.Red, overlay.Green, overlay.Blue);
                continue;
            }

            for (var code = 0; code < 256; code++)
            {
                var color = catalog.TryGet((byte)code, out var terrain) ? terrain.MapColor : Unknown;
                var keep = variant == TileVariant.Terrain ? 1.0 : 1.0 - opacity;
                var add = variant == TileVariant.Terrain ? 0.0 : opacity;
                table[(code << 8) | elevation] = Pack(
                    Byte((color.Red * keep) + (overlay.Red * add)),
                    Byte((color.Green * keep) + (overlay.Green * add)),
                    Byte((color.Blue * keep) + (overlay.Blue * add)));
            }
        }
    }

    private static void Sum(TerrainGrid grid, TileVariant variant, int level, int column, int row, int tileSize, int[] table, Span<byte> pixels,
        int width, int height, int channels)
    {
        var codes = grid.Codes;
        var elevations = grid.Elevations;
        var gridWidth = grid.Geometry.GridWidth;
        var gridHeight = grid.Geometry.GridHeight;
        var block = 1 << level;
        // Above level 3 a pixel averages an even 8 by 8 sample of its cells, not all of them: a top tile otherwise reads every cell of the
        // map (2.4 s for Goodwood), and at those zooms a screen pixel spans 16 cells or more (Raster Tiles Plan, deviation D1).
        var step = Math.Max(1, block / SamplesPerSide);
        var originX = column * TilePyramid.Span(level, tileSize);
        var originY = row * TilePyramid.Span(level, tileSize);
        var elevationOnly = variant == TileVariant.Elevation;
        for (var py = 0; py < height; py++)
        {
            var y0 = originY + (py * block);
            var y1 = Math.Min(y0 + block, gridHeight);
            for (var px = 0; px < width; px++)
            {
                var x0 = originX + (px * block);
                var x1 = Math.Min(x0 + block, gridWidth);
                long red = 0, green = 0, blue = 0, weight = 0;
                var count = 0;
                for (var x = x0; x < x1; x += step)
                {
                    var start = (x * gridHeight) + y0;
                    var cellCodes = codes.Slice(start, y1 - y0);
                    var cellElevations = elevations.Slice(start, y1 - y0);
                    for (var i = 0; i < cellCodes.Length; i += step)
                    {
                        count++;
                        var elevation = unchecked((byte)cellElevations[i]);
                        if (elevationOnly)
                        {
                            // Premultiplied, so a pixel half over a hill keeps the hill's colour at half its opacity.
                            var w = table[elevation];
                            var overlay = table[256 + elevation];
                            red += (overlay >> 16) * w;
                            green += ((overlay >> 8) & 0xff) * w;
                            blue += (overlay & 0xff) * w;
                            weight += w;
                            continue;
                        }

                        var packed = table[(cellCodes[i] << 8) | elevation];
                        red += packed >> 16;
                        green += (packed >> 8) & 0xff;
                        blue += packed & 0xff;
                    }
                }

                var at = ((py * width) + px) * channels;
                if (elevationOnly)
                {
                    pixels[at] = weight > 0 ? Byte((double)red / weight) : (byte)0;
                    pixels[at + 1] = weight > 0 ? Byte((double)green / weight) : (byte)0;
                    pixels[at + 2] = weight > 0 ? Byte((double)blue / weight) : (byte)0;
                    pixels[at + 3] = Byte(255.0 * weight / OpacityScale / count);
                }
                else
                {
                    pixels[at] = Byte((double)red / count);
                    pixels[at + 1] = Byte((double)green / count);
                    pixels[at + 2] = Byte((double)blue / count);
                }
            }
        }
    }

    private static int Pack(byte red, byte green, byte blue) => (red << 16) | (green << 8) | blue;

    private static byte Byte(double value) => (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);
}
