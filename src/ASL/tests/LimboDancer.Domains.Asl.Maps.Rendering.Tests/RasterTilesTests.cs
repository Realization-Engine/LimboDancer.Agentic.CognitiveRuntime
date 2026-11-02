using System.Buffers.Binary;
using System.IO.Compression;
using LimboDancer.Domains.Asl.Maps.Grid;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

/// <summary>Raster Tiles Plan, task 2: tiles painted from the grid, and their PNG.</summary>
public sealed class RasterTilesTests
{
    // The synthetic grid is 113 by 129 cells: one 512 tile, or 4 by 5 tiles of 32.
    private static readonly TerrainGrid Grid = SyntheticBoard.Grid();

    [Fact]
    public void Level0MatchesTheExactFormulaPerCell()
    {
        var pixels = Paint(TileVariant.TerrainAndElevation, 0, 0, 0, 512, out var width);
        // Woods at level 0: its catalog colour, untouched.
        Assert.Equal(new byte[] { 0x2e, 0x7d, 0x32 }, Pixel(pixels, width, 30, 20, 3));
        // Open ground on the hill (level 1): 0.88 of a8 b8 60, with black at 0.12 over it.
        Assert.Equal(new byte[] { 148, 162, 84 }, Pixel(pixels, width, 110, 70, 3));
        // Shallow stream at level -1: 6c a6 cd with 1f 4e 79 at 0.12 over it.
        Assert.Equal(new byte[] { 99, 155, 195 }, Pixel(pixels, width, 5, 92, 3));
    }

    [Fact]
    public void LevelNAveragesItsCells()
    {
        // Level 0 whole; level 1 of 64 tiles, whose first tile covers 128 by 128 cells, cut to 113 wide: 57 by 64 pixels.
        var fine = Paint(TileVariant.TerrainAndElevation, 0, 0, 0, 512, out var fineWidth);
        var coarse = Paint(TileVariant.TerrainAndElevation, 1, 0, 0, 64, out var coarseWidth);
        Assert.Equal(57, coarseWidth);
        var height = Grid.Geometry.GridHeight;
        for (var py = 0; py < 64; py++)
        {
            for (var px = 0; px < coarseWidth; px++)
            {
                for (var channel = 0; channel < 3; channel++)
                {
                    double sum = 0;
                    var count = 0;
                    for (var x = px * 2; x < Math.Min(px * 2 + 2, fineWidth); x++)
                    {
                        for (var y = py * 2; y < Math.Min(py * 2 + 2, height); y++)
                        {
                            sum += fine[((y * fineWidth) + x) * 3 + channel];
                            count++;
                        }
                    }

                    var painted = coarse[((py * coarseWidth) + px) * 3 + channel];
                    Assert.InRange(painted, Math.Floor(sum / count) - 1, Math.Ceiling(sum / count) + 1);
                }
            }
        }

        // A pixel wholly inside the woods keeps the woods' colour.
        Assert.Equal(new byte[] { 0x2e, 0x7d, 0x32 }, Pixel(coarse, coarseWidth, 15, 10, 3));
    }

    [Fact]
    public void AboveLevel3SamplesItsCellsEvenly()
    {
        // Tiles of 8 give the small grid six levels. At level 4 a pixel spans 16 by 16 cells and reads every second one: pixel (1, 1)
        // covers cells 16 to 31 each way, and its sampled columns 16 and 18 are open ground, 20 to 30 woods. A quarter of a8 b8 60 and
        // three quarters of 2e 7d 32.
        var pixels = Paint(TileVariant.Terrain, 4, 0, 0, 8, out var width);
        Assert.Equal(8, width);
        Assert.Equal(new byte[] { 77, 140, 62 }, Pixel(pixels, width, 1, 1, 3));
    }

    [Fact]
    public void EdgeTileIsCutToTheGrid()
    {
        Assert.Equal(4, TilePyramid.LevelCount(113, 129, 32));
        Assert.Equal(4, TilePyramid.Columns(113, 0, 32));
        Assert.Equal(5, TilePyramid.Rows(129, 0, 32));
        Assert.Equal((17, 1), TilePyramid.PixelSize(113, 129, 0, 3, 4, 32));
        Assert.Equal((25, 32), TilePyramid.PixelSize(113, 129, 1, 1, 0, 32));
        Assert.Equal((15, 17), TilePyramid.PixelSize(113, 129, 3, 0, 0, 32));
        Assert.False(TilePyramid.Contains(113, 129, 0, 4, 0, 32));
        Assert.False(TilePyramid.Contains(113, 129, 4, 0, 0, 32));

        var edge = Paint(TileVariant.Terrain, 0, 3, 0, 32, out var width);
        Assert.Equal(17, width);
        // Cell (100, 5) is the wall: its catalog colour, at x 4 of the fourth tile.
        Assert.Equal(new byte[] { 0x5a, 0x5a, 0x5a }, Pixel(edge, width, 4, 5, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => TilePainter.Paint(Grid, SyntheticBoard.Catalog, TileVariant.Terrain, 0, 4, 0, new byte[3], 32));
    }

    [Fact]
    public void ElevationVariantIsTransparentAtLevelZero()
    {
        var pixels = Paint(TileVariant.Elevation, 0, 0, 0, 512, out var width);
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, Pixel(pixels, width, 30, 20, 4));
        // 0.12 x 255 = 30.6: black above level 0, 1f 4e 79 below it.
        Assert.Equal(new byte[] { 0, 0, 0, 31 }, Pixel(pixels, width, 110, 70, 4));
        Assert.Equal(new byte[] { 0x1f, 0x4e, 0x79, 31 }, Pixel(pixels, width, 5, 92, 4));

        var terrain = Paint(TileVariant.Terrain, 0, 0, 0, 512, out _);
        Assert.Equal(new byte[] { 0xa8, 0xb8, 0x60 }, Pixel(terrain, width, 110, 70, 3));
    }

    [Fact]
    public void PngRoundTripReadsBackTheSamePixels()
    {
        foreach (var variant in new[] { TileVariant.TerrainAndElevation, TileVariant.Elevation })
        {
            var pixels = Paint(variant, 0, 0, 0, 512, out var width);
            var height = Grid.Geometry.GridHeight;
            var png = TilePainter.Png(Grid, SyntheticBoard.Catalog, variant, 0, 0, 0);
            var (readWidth, readHeight, colorType, read) = Decode(png);
            Assert.Equal(width, readWidth);
            Assert.Equal(height, readHeight);
            Assert.Equal(variant == TileVariant.Elevation ? 6 : 2, colorType);
            Assert.Equal(pixels, read);
        }
    }

    private static byte[] Paint(TileVariant variant, int level, int column, int row, int tileSize, out int width)
    {
        var geometry = Grid.Geometry;
        (width, var height) = TilePyramid.PixelSize(geometry.GridWidth, geometry.GridHeight, level, column, row, tileSize);
        var pixels = new byte[width * height * TilePainter.Channels(variant)];
        TilePainter.Paint(Grid, SyntheticBoard.Catalog, variant, level, column, row, pixels, tileSize);
        return pixels;
    }

    private static byte[] Pixel(byte[] pixels, int width, int x, int y, int channels) =>
        pixels.AsSpan(((y * width) + x) * channels, channels).ToArray();

    // Reads the PNG's header and image data back, checking every chunk's CRC, and undoes the "Sub" filter.
    private static (int Width, int Height, int ColorType, byte[] Pixels) Decode(byte[] png)
    {
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }, png[..8]);
        int width = 0, height = 0, colorType = 0;
        using var data = new MemoryStream();
        var at = 8;
        while (at < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(at));
            var type = System.Text.Encoding.ASCII.GetString(png, at + 4, 4);
            var body = png.AsSpan(at + 8, length);
            Assert.Equal(Crc(png.AsSpan(at + 4, length + 4)), BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(at + 8 + length)));
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(body);
                height = BinaryPrimitives.ReadInt32BigEndian(body[4..]);
                colorType = body[9];
            }
            else if (type == "IDAT")
            {
                data.Write(body);
            }

            at += 12 + length;
        }

        data.Position = 0;
        using var inflated = new MemoryStream();
        using (var zlib = new ZLibStream(data, CompressionMode.Decompress))
        {
            zlib.CopyTo(inflated);
        }

        var channels = colorType == 6 ? 4 : 3;
        var stride = width * channels;
        var raw = inflated.ToArray();
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            Assert.Equal(1, raw[y * (stride + 1)]);
            for (var i = 0; i < stride; i++)
            {
                var left = i >= channels ? pixels[(y * stride) + i - channels] : 0;
                pixels[(y * stride) + i] = unchecked((byte)(raw[(y * (stride + 1)) + 1 + i] + left));
            }
        }

        return (width, height, colorType, pixels);
    }

    private static uint Crc(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xffffffffu;
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xedb88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return crc ^ 0xffffffffu;
    }
}
