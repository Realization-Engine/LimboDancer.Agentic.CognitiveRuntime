using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The map service keeps a bounded number of built maps (option K of the SVG memory analysis).</summary>
public sealed class MapCacheTests : IDisposable
{
    private static readonly BoardRef Board02 = BoardRef.Parse("bd02");

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-map-cache-tests-" + Guid.NewGuid().ToString("N"));
    private readonly MapService maps;

    public MapCacheTests()
    {
        maps = new MapService(new StudioOptions { CacheRoot = root, BoardsRoot = Path.Combine(root, "boards") }, new FakeVaslMapSource());
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void PlacedMapsStayBoundedAndASavedMapStays()
    {
        var saved = maps.Save("Kept", [new BoardPlacement(Board02)]).Ref!;
        Assert.NotNull(maps.Load(saved).Board);

        // Rectangles one to three boards wide (VASL's limit across) and one to fourteen rows tall: forty different maps, more than the
        // 32 the service keeps.
        var shapes = Enumerable.Range(1, 14).SelectMany(rows => Enumerable.Range(1, 3).Select(width => (rows, width))).Take(40).ToArray();
        foreach (var (rows, width) in shapes)
        {
            var placements = Enumerable.Range(0, rows).SelectMany(row => Enumerable.Range(0, width).Select(column => new BoardPlacement(Board02, column, row)));
            Assert.NotNull(maps.LoadPlacements([.. placements]).Board);
            Assert.True(maps.CachedCount <= 33, $"{maps.CachedCount} maps cached after the {rows} by {width} map.");
        }

        // The 33rd map cleared the first 32; maps 33 to 40 remain, with the saved map.
        Assert.NotNull(maps.Load(saved).Board);
        Assert.Equal(1 + 8, maps.CachedCount);
    }

    [Fact]
    public void SavingAgainDropsTheOldBuild()
    {
        var map = maps.Save("Changed", [new BoardPlacement(Board02)]).Ref!;
        maps.Load(map);
        Assert.Equal(1, maps.CachedCount);

        maps.Save("Changed", [new BoardPlacement(Board02), new BoardPlacement(BoardRef.Parse("bd03"), 1, 0)]);
        Assert.Equal(0, maps.CachedCount);
        maps.Load(map);
        Assert.Equal(1, maps.CachedCount);

        maps.Delete(map);
        Assert.Equal(0, maps.CachedCount);
    }
}
