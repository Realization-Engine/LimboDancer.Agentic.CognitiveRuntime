using System.Text;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Map Lab Plan, task 5: the generator, the key, the drawing, and the tiles' place in the shared cache.</summary>
public sealed class MapLabTests
{
    private static readonly LabMap Small = LabMapGenerator.Generate(LabMapSpec.Clamped(1_038, 36, 7, LabTechnique.Paths));

    [Fact]
    public void GeneratesAboutTheAskedCountRepeatablyWithEveryHexInOneFormation()
    {
        Assert.InRange(Small.HexCount, 1_000, 1_100);
        Assert.Equal(Small.Geometry.HexCount, Small.HexCount);
        var again = LabMapGenerator.Generate(LabMapSpec.Clamped(1_038, 36, 7, LabTechnique.Elements));
        Assert.Equal(Small.Terrain, again.Terrain);
        Assert.Equal(Small.Levels, again.Levels);
        Assert.Equal(Small.Parents, again.Parents);
        Assert.NotEqual(Small.Terrain, LabMapGenerator.Generate(LabMapSpec.Clamped(1_038, 36, 8, LabTechnique.Paths)).Terrain);

        // Each hex has one formation; formations average about the group's size.
        Assert.Equal(Small.Formations, Small.Parents.Distinct().Count());
        Assert.InRange(Small.HexCount / (double)Small.Formations, 18, 54);
        Assert.All(Small.Levels, level => Assert.InRange(level, 0, 2));
        var single = LabMapGenerator.Generate(LabMapSpec.Clamped(100, 1, 7, LabTechnique.Paths));
        Assert.Equal(Enumerable.Range(0, single.HexCount).ToArray(), single.Parents);
    }

    [Theory]
    [InlineData("n10000-g36-s7-tiles", true)]
    [InlineData("n10000-g36-s7-tileslines", true)]
    [InlineData("n1000000-g1-s0-elements", true)]
    [InlineData("n1000001-g36-s7-paths", false)]
    [InlineData("n10000-g0-s7-paths", false)]
    [InlineData("n10000-g36-s7-canvas", false)]
    [InlineData("../n10000-g36-s7-paths", false)]
    public void ReadsOnlyKeysInRange(string key, bool valid)
    {
        Assert.Equal(valid, LabMapSpec.TryReadKey(key, out var spec));
        if (valid)
        {
            Assert.Equal(key, spec.Key);
        }
    }

    [Fact]
    public void ClampsTheFormToTheLimits()
    {
        Assert.Equal(LabMapSpec.MaxHexes, LabMapSpec.Clamped(5_000_000, 36, 1, LabTechnique.Tiles).Hexes);
        Assert.Equal(1, LabMapSpec.Clamped(100, -3, 1, LabTechnique.Tiles).Group);
    }

    [Fact]
    public void DrawsAnElementPerHexOrAFewPaths()
    {
        var elements = Small.With(Small.Spec with
        {
            Technique = LabTechnique.Elements
        });
        var terrain = XElement.Parse(LabMapRenderer.Fragment(elements, LabMapRenderer.TerrainLayer)!).Elements().Single();
        Assert.Equal("layer-exact-terrain", terrain.Attribute("id")!.Value);
        Assert.Equal(Small.HexCount, terrain.Elements().Count());

        var paths = XElement.Parse(LabMapRenderer.Fragment(Small, LabMapRenderer.TerrainLayer)!).Elements().Single();
        Assert.InRange(paths.Elements().Count(), 1, LabMap.Colours.Length);
        var grid = XElement.Parse(LabMapRenderer.Fragment(Small, LabMapRenderer.GridLayer)!).Elements().Single();
        Assert.Single(grid.Elements());
        Assert.NotNull(LabMapRenderer.Fragment(Small, LabMapRenderer.BoundariesLayer));
        Assert.Null(LabMapRenderer.Fragment(LabMapGenerator.Generate(LabMapSpec.Clamped(100, 1, 7, LabTechnique.Paths)), LabMapRenderer.BoundariesLayer));
    }

    [Fact]
    public void GridDrawsEverySharedEdgeOnce()
    {
        // With every hex its own parent, the edges are the grid's: a hex's six, less one for each neighbour pair.
        var geometry = BoardGeometry.Standard(4, 3);
        var own = Enumerable.Range(0, geometry.HexCount).ToArray();
        var path = new StringBuilder();
        CampaignMapBoundaries.AppendEdges(path, geometry, own);
        var pairs = geometry.Hexes().Sum(hex => Enumerable.Range(0, 6).Count(side => geometry.Neighbor(hex, (HexsideDirection)side) is not null)) / 2;
        var degenerate = geometry.Hexes().Sum(hex =>
        {
            var vertices = geometry.Vertices(hex);
            return Enumerable.Range(0, 6).Count(side => vertices[side] == vertices[(side + 1) % 6]);
        });
        Assert.Equal((geometry.HexCount * 6) - pairs - degenerate, path.ToString().Count(character => character == 'M'));
    }

    [Fact]
    public void FindsTheHexOfAPointAsTheGeometryDoes()
    {
        var geometry = Small.Geometry;
        var random = new Random(3);
        for (var i = 0; i < 2_000; i++)
        {
            var x = random.NextDouble() * (geometry.GridWidth - 1);
            var y = random.NextDouble() * (geometry.GridHeight - 1);
            var hex = geometry.HexAt(x, y)!.Value;
            var expected = geometry.HexOrdinal(hex);
            var found = LabMapRenderer.OrdinalAt(geometry, x, y);
            // On a border two centres are equally near, and the geometry moves a right-edge centre in by a pixel: either answer is right there.
            if (found != expected)
            {
                var a = geometry.CenterDot(hex);
                var b = geometry.CenterDot(geometry.Hexes().ElementAt(found));
                Assert.True(Math.Abs(Distance(a, x, y) - Distance(b, x, y)) < 1.0, $"({x}, {y}): {expected} against {found}");
            }
        }
    }

    [Fact]
    public void PaintsTilesOfThePyramidIntoTheSharedCache()
    {
        var tiles = Small.With(Small.Spec with
        {
            Technique = LabTechnique.Tiles
        });
        Assert.True(LabMapRenderer.HasTile(tiles, 0, 0, 0));
        var cache = new TileCache();
        var painted = 0;
        byte[] Paint()
        {
            painted++;
            return LabMapRenderer.TilePng(tiles, 0, 0, 0);
        }

        var first = cache.Get(LabMapEndpoints.TileSource, tiles.Spec.Key, TileVariant.Terrain, 0, 0, 0, Paint);
        var second = cache.Get(LabMapEndpoints.TileSource, tiles.Spec.Key, TileVariant.Terrain, 0, 0, 0, Paint);
        Assert.Same(first, second);
        Assert.Equal(1, painted);
        Assert.Equal(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G' }, first.Png[..4]);

        // Another map's tiles replace this one's.
        cache.Get(LabMapEndpoints.TileSource, "n100-g1-s0", TileVariant.Terrain, 0, 0, 0, Paint);
        Assert.Equal(1, cache.Count);
    }

    [Fact]
    public void PaintsLinesIntoTilesOnlyWhereTheyCanBeSeen()
    {
        // Near: the grid and the boundaries change the tile, and only lines are added, never vector layers.
        var plain = Small.With(Small.Spec with
        {
            Technique = LabTechnique.Tiles
        });
        var lined = Small.With(Small.Spec with
        {
            Technique = LabTechnique.TilesLines
        });
        Assert.NotEqual(LabMapRenderer.TilePng(plain, 0, 0, 0), LabMapRenderer.TilePng(lined, 0, 0, 0));
        Assert.Equal(new[] { LabMapRenderer.TerrainLayer, LabMapRenderer.LabelsLayer }, LabMapRenderer.Layers(lined.Spec));
        Assert.Null(LabMapRenderer.Fragment(lined, LabMapRenderer.GridLayer));

        // Far: a hex under six pixels across draws no grid, so single hexes paint the plain tile.
        var large = LabMapGenerator.Generate(LabMapSpec.Clamped(100_000, 1, 7, LabTechnique.TilesLines));
        Assert.True(large.Geometry.HexWidth / 16 < LabMapRenderer.GridMinimumPixels);
        Assert.Equal(LabMapRenderer.TilePng(large.With(large.Spec with
        {
            Technique = LabTechnique.Tiles
        }), 4, 0, 0), LabMapRenderer.TilePng(large, 4, 0, 0));
    }

    private static double Distance(PixelPoint centre, double x, double y) => Math.Sqrt(((centre.X - x) * (centre.X - x)) + ((centre.Y - y) * (centre.Y - y)));
}
