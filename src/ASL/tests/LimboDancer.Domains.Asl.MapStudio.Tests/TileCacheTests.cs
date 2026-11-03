using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Raster Tiles Plan, task 3: the tile cache keeps one version per board, a bound in bytes, and one painting per tile.</summary>
public sealed class TileCacheTests
{
    // The synthetic grid (113 by 129) fits one 512 tile, so it has one level and one tile per variant.
    private static readonly StudioBoard Board = FakeBoardProvider.Board;

    [Fact]
    public void HoldsOneVersionPerBoard()
    {
        var cache = new TileCache();
        cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0);
        cache.Get(Board, TileVariant.Terrain, 0, 0, 0);
        Assert.Equal(2, cache.Count);

        var next = Board with
        {
            Version = new string('1', 40)
        };
        Assert.NotNull(cache.Get(next, TileVariant.TerrainAndElevation, 0, 0, 0));
        Assert.Equal(1, cache.Count);
        Assert.Null(cache.Get(Board, TileVariant.TerrainAndElevation, 0, 1, 0));
    }

    [Fact]
    public void EvictsLeastRecentlyUsedByBytes()
    {
        var grid = Board.Render.Grid;
        long Size(TileVariant variant) => TilePainter.Png(grid, Board.Catalog, variant, 0, 0, 0).Length;
        // Room for the combined tile and either other one, never all three.
        var bound = Size(TileVariant.TerrainAndElevation) + Math.Max(Size(TileVariant.Terrain), Size(TileVariant.Elevation));
        var cache = new TileCache(bound: bound);

        cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0);
        cache.Get(Board, TileVariant.Terrain, 0, 0, 0);
        cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0);
        Assert.Equal(2, cache.Painted);

        // The elevation tile pushes out the least recently used one, the terrain tile.
        cache.Get(Board, TileVariant.Elevation, 0, 0, 0);
        Assert.Equal(3, cache.Painted);
        Assert.True(cache.Bytes <= bound);
        Assert.Equal(2, cache.Count);
        cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0);
        Assert.Equal(3, cache.Painted);
        cache.Get(Board, TileVariant.Terrain, 0, 0, 0);
        Assert.Equal(4, cache.Painted);
    }

    [Fact]
    public async Task PaintsATileOnceUnderConcurrentRequests()
    {
        var cache = new TileCache();
        using var start = new ManualResetEventSlim();
        var requests = Enumerable.Range(0, 16).Select(_ => Task.Run(() =>
        {
            start.Wait();
            return cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0);
        })).ToArray();
        start.Set();
        var tiles = await Task.WhenAll(requests);

        Assert.Equal(1, cache.Painted);
        Assert.All(tiles, tile => Assert.Same(tiles[0], tile));
    }

    [Fact]
    public void FileNamesReadBack()
    {
        var name = TileCache.FileName(TileVariant.Elevation, 3, 12, 7);
        Assert.Equal("tile-e-3-12-7.png", name);
        Assert.True(TileCache.TryReadFileName(name, out var variant, out var level, out var column, out var row));
        Assert.Equal((TileVariant.Elevation, 3, 12, 7), (variant, level, column, row));
        Assert.False(TileCache.TryReadFileName("tile-x-0-0-0.png", out _, out _, out _, out _));
        Assert.False(TileCache.TryReadFileName("tile-te-0-0.png", out _, out _, out _, out _));
    }

    // Screen-Bounded Map Analysis, section 3.1: tiles with lines name them, and keep apart from tiles without.
    [Fact]
    public void LinedFileNamesReadBackAndHoldTheirOwnTiles()
    {
        var name = TileCache.FileName(TileVariant.TerrainAndElevation, TileLines.Grid | TileLines.Boundaries, 2, 1, 0);
        Assert.Equal("tile-te+gb-2-1-0.png", name);
        Assert.True(TileCache.TryReadFileName(name, out var variant, out var lines, out var level, out var column, out var row));
        Assert.Equal((TileVariant.TerrainAndElevation, TileLines.Grid | TileLines.Boundaries, 2, 1, 0), (variant, lines, level, column, row));
        Assert.False(TileCache.TryReadFileName(name, out _, out _, out _, out _));
        Assert.Equal("tile-t+g-0-0-0.png", TileCache.FileName(TileVariant.Terrain, TileLines.Grid, 0, 0, 0));

        var cache = new TileCache();
        var plain = cache.Get(Board, TileVariant.TerrainAndElevation, 0, 0, 0)!;
        var lined = cache.Get(Board, TileVariant.TerrainAndElevation, TileLines.Grid, null, 0, 0, 0)!;
        Assert.NotEqual(plain.ETag, lined.ETag);
        Assert.Equal(2, cache.Count);
        // Boundaries without parents are left out, so the tile is the grid's.
        Assert.Same(lined, cache.Get(Board, TileVariant.TerrainAndElevation, TileLines.Grid | TileLines.Boundaries, null, 0, 0, 0));
    }
}
