using System.Security.Cryptography;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using static LimboDancer.Domains.Asl.Maps.Tests.FeatureTestCatalog;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class FeatureCompilerTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void AnEmptyModelIsItsBase()
    {
        var model = FeatureModel.New(Geometry, "test-catalog") with
        {
            BaseCode = Code("Grain"),
            BaseElevation = 2
        };
        var grid = Compile(model);
        Assert.All(Enumerable.Range(0, grid.CellCount), cell => Assert.Equal(Code("Grain"), grid.Codes[cell]));
        Assert.All(Enumerable.Range(0, grid.CellCount), cell => Assert.Equal(2, grid.Elevations[cell]));
    }

    [Fact]
    public void StagesPaintInOrderRegardlessOfListOrder()
    {
        // A building listed first still paints over woods; within a kind, the higher layer wins.
        var grid = Compile(Model(Geometry,
            new BuildingFeature("b", 0, [Box(10, 10, 20, 20)], Code("Stone Building"), "B1"),
            new AreaTerrainFeature("w2", 1, Box(0, 0, 15, 15), Code("Grain")),
            new AreaTerrainFeature("w1", 0, Box(0, 0, 30, 30), Code("Woods"))));
        Assert.Equal(Code("Stone Building"), grid.CodeAt(12, 12));
        Assert.Equal(Code("Grain"), grid.CodeAt(5, 5));
        Assert.Equal(Code("Woods"), grid.CodeAt(25, 25));
        Assert.Equal(Code("Open Ground"), grid.CodeAt(35, 35));
    }

    [Fact]
    public void ElevationRegionsPaintInAscendingLevel()
    {
        var grid = Compile(Model(Geometry,
            new ElevationRegion("high", 0, Box(20, 20, 40, 40), 2),
            new ElevationRegion("low", 5, Box(0, 0, 60, 60), 1)));
        Assert.Equal(2, grid.ElevationAt(30, 30));
        Assert.Equal(1, grid.ElevationAt(5, 5));
        Assert.Equal(0, grid.ElevationAt(70, 70));
    }

    [Fact]
    public void LinearTerrainStrokesItsCenterlineOrFillsItsOutline()
    {
        var road = new LinearTerrainFeature("road", 0, Code("Paved Road"),
            new CenterlinePath(FixedVector.FromPixels(100, 100), [new PathSegment(FixedVector.FromPixels(200, 100))]), FixedPoint.FromPixels(8), null);
        var track = new LinearTerrainFeature("track", 0, Code("Paved Road"), null, FixedPoint.Zero, Box(300, 300, 310, 305));
        var grid = Compile(Model(Geometry, road, track));
        Assert.Equal(Code("Paved Road"), grid.CodeAt(150, 97));
        Assert.Equal(Code("Open Ground"), grid.CodeAt(150, 106));
        Assert.Equal(Code("Paved Road"), grid.CodeAt(305, 302));
    }

    [Fact]
    public void HexsideStrokesAreCenteredOnTheHexsideAndCoverBothEdgeSamples()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var side = new HexsideRef(hex, HexsideDirection.North);
        var neighbor = Geometry.Neighbor(hex, HexsideDirection.North)!.Value;
        var grid = Compile(Model(Geometry, new HexsideTerrainFeature("wall", 0, Code("Wall"), [new HexsideSpan(side)])));
        var inside = Geometry.EdgeSamplePoint(hex, HexsideDirection.North);
        var outside = Geometry.EdgeSamplePoint(neighbor, HexsideDirection.South);
        Assert.Equal(Code("Wall"), grid.CodeAt(inside.X, inside.Y));
        Assert.Equal(Code("Wall"), grid.CodeAt(outside.X, outside.Y));
        Assert.Equal(Code("Open Ground"), grid.CodeAt(inside.X, inside.Y + 4));

        var facts = Derive(grid);
        Assert.Equal("Wall", facts[hex].Hexsides[0].HexsideTerrain?.Name);
        Assert.Equal("Wall", facts[neighbor].Hexsides[3].HexsideTerrain?.Name);
    }

    [Fact]
    public void HexsideWidthAndExtentAreHonored()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var side = new HexsideRef(hex, HexsideDirection.North);
        var (from, to) = FeatureCompiler.HexsideEndpoints(Geometry, side);
        var y = from.Y / 64;
        var midX = (from.X + to.X) / 2 / 64;
        var wide = Compile(Model(Geometry, new HexsideTerrainFeature("wall", 0, Code("Wall"), [new HexsideSpan(side, Width: 12)])));
        Assert.Equal(Code("Wall"), wide.CodeAt(midX, y + 5));
        Assert.Equal(Code("Open Ground"), wide.CodeAt(midX, y + 7));

        var half = Compile(Model(Geometry, new HexsideTerrainFeature("wall", 0, Code("Wall"), [new HexsideSpan(side, 0, 32)])));
        Assert.Equal(Code("Wall"), half.CodeAt((from.X / 64) + 3, y));
        Assert.Equal(Code("Open Ground"), half.CodeAt((to.X / 64) - 3, y));

        // A width below the minimum is raised to 3 pixels.
        var thin = Compile(Model(Geometry, new HexsideTerrainFeature("wall", 0, Code("Wall"), [new HexsideSpan(side, Width: 1)])));
        Assert.Equal(Code("Wall"), thin.CodeAt(midX, y));
        Assert.Equal(Code("Wall"), thin.CodeAt(midX, y - 1));
    }

    [Fact]
    public void ExteriorFactoryWallsFollowTheEditorRule()
    {
        var grid = Compile(Model(Geometry,
            new BuildingFeature("f", 0, [Box(100, 100, 110, 110)], Code("Stone Factory, 1.5 Level"), "F1"),
            new BuildingFeature("r", 0, [Box(200, 100, 210, 110)], Code("Roofless Stone Factory, 1.5 Level"), "F2")));
        Assert.Equal(Code("Stone Factory Wall, 1.5 Level"), grid.CodeAt(100, 105));
        Assert.Equal(Code("Stone Factory Wall, 1.5 Level"), grid.CodeAt(109, 109));
        Assert.Equal(Code("Stone Factory, 1.5 Level"), grid.CodeAt(101, 101));
        Assert.Equal(Code("Stone Factory Wall, 1.5 Level"), grid.CodeAt(200, 100));
        Assert.Equal(Code("Roofless Stone Factory, 1.5 Level"), grid.CodeAt(205, 105));

        // The pre-pass grid keeps the footprint whole.
        var prePass = FeatureCompiler.Compile(Model(Geometry, new BuildingFeature("f", 0, [Box(100, 100, 110, 110)], Code("Stone Factory, 1.5 Level"), "F1")), Catalog).PrePassGrid;
        Assert.Equal(Code("Stone Factory, 1.5 Level"), prePass.CodeAt(100, 105));
    }

    [Fact]
    public void DepressionsAreOneLevelDown()
    {
        var grid = Compile(Model(Geometry,
            new ElevationRegion("hill", 0, Box(0, 0, 400, 400), 1),
            new LinearTerrainFeature("stream", 0, Code("Shallow Stream"), null, FixedPoint.Zero, Box(100, 50, 106, 300))));
        Assert.Equal(0, grid.ElevationAt(103, 200));
        Assert.Equal(1, grid.ElevationAt(120, 200));
    }

    [Fact]
    public void SunkenRoadsBecomeElevatedRoadsOnlyInLevelOneHexes()
    {
        var hill = Geometry.IndexOf(HexName.Parse("C3"));
        var flat = Geometry.IndexOf(HexName.Parse("H3"));
        var hillCenter = Geometry.CenterPoint(hill);
        var flatCenter = Geometry.CenterPoint(flat);
        var grid = Compile(Model(Geometry,
            new ElevationRegion("hill", 0, Box(hillCenter.X - 40, hillCenter.Y - 40, hillCenter.X + 40, hillCenter.Y + 40), 1),
            new LinearTerrainFeature("s1", 0, Code("Sunken Road"), null, FixedPoint.Zero, Box(hillCenter.X - 10, hillCenter.Y + 10, hillCenter.X + 10, hillCenter.Y + 14)),
            new LinearTerrainFeature("s2", 0, Code("Sunken Road"), null, FixedPoint.Zero, Box(flatCenter.X - 10, flatCenter.Y + 10, flatCenter.X + 10, flatCenter.Y + 14))));
        Assert.Equal(Code("Elevated Road"), grid.CodeAt(hillCenter.X, hillCenter.Y + 12));
        Assert.Equal(1, grid.ElevationAt(hillCenter.X, hillCenter.Y + 12));
        Assert.Equal(Code("Sunken Road"), grid.CodeAt(flatCenter.X, flatCenter.Y + 12));
        Assert.Equal(-1, grid.ElevationAt(flatCenter.X, flatCenter.Y + 12));
    }

    [Fact]
    public void CliffPixelsTakeTheBaseLevelOfTheHexTheEditorAssignsThem()
    {
        var upper = Geometry.IndexOf(HexName.Parse("E4"));
        var lower = Geometry.Neighbor(upper, HexsideDirection.South)!.Value;
        var center = Geometry.CenterPoint(upper);
        var model = Model(Geometry,
            new ElevationRegion("hill", 0, Box(center.X - 60, center.Y - 60, center.X + 60, center.Y + 30), 1),
            new HexsideTerrainFeature("cliff", 0, Code("Cliff"), [new HexsideSpan(new HexsideRef(upper, HexsideDirection.South), Width: 8)]));
        var result = FeatureCompiler.Compile(model, Catalog);
        var facts = Derive(result.PrePassGrid);
        var locator = new VaslHexLocator(Geometry);
        var levels = new HashSet<int>();
        for (var x = 0; x < Geometry.GridWidth; x++)
        {
            for (var y = 0; y < Geometry.GridHeight; y++)
            {
                if (result.Grid.CodeAt(x, y) == Code("Cliff") && locator.GridToHex(x, y) is { } hex)
                {
                    Assert.Equal(facts[hex].BaseLevel, result.Grid.ElevationAt(x, y));
                    levels.Add(result.Grid.ElevationAt(x, y));
                }
            }
        }

        Assert.Equal([0, 1], levels.Order());
        Assert.Equal(1, facts[upper].BaseLevel);
        Assert.Equal(0, facts[lower].BaseLevel);
    }

    [Fact]
    public void FidelityPinsPaintOverThePostPasses()
    {
        var grid = Compile(Model(Geometry,
            new BuildingFeature("f", 0, [Box(100, 100, 110, 110)], Code("Stone Factory, 1.5 Level"), "F1"),
            new FidelityPin("pin", 0, Box(100, 105, 101, 106), Code("Stone Factory, 1.5 Level"), 3)));
        Assert.Equal(Code("Stone Factory, 1.5 Level"), grid.CodeAt(100, 105));
        Assert.Equal(3, grid.ElevationAt(100, 105));
        Assert.Equal(Code("Stone Factory Wall, 1.5 Level"), grid.CodeAt(100, 106));
    }

    [Fact]
    public void StairwaysComeFromTheAnnotations()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var model = FeatureModel.New(Geometry, "test-catalog") with
        {
            Annotations = new HexAnnotations(new HashSet<HexIndex> { hex }, HexsideAnnotations.None)
        };
        var grid = Compile(model);
        Assert.True(grid.HasStairway(hex));
        Assert.False(grid.HasStairway(Geometry.IndexOf(HexName.Parse("E5"))));
    }

    [Fact]
    public void CompilationIsDeterministicAndPinnedByHash()
    {
        var model = SampleModel();
        var first = Compile(model);
        var second = Compile(model with
        {
            Features = [.. model.Features.Reverse()]
        });
        Assert.Equal(Hash(first), Hash(second));

        // Pinned so Windows and Linux runs in CI must agree byte for byte (Model Design section 5.4).
        const string Pinned = "0b809219dfb85ad3124eec9f76303a4bc841e7c432fb88fece49185de3fe2cd4";
        Assert.True(Hash(first) == Pinned, $"The compiled grid hash is {Hash(first)}.");
    }

    internal static FeatureModel SampleModel()
    {
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        return Model(Geometry,
            new ElevationRegion("hill", 0, Box(300, 100, 700, 400), 1),
            new AreaTerrainFeature("woods", 0, new FeatureShape([[FixedVector.FromPixels(50, 50), FixedVector.FromPixels(250, 80), FixedVector.FromPixels(180, 260)]]), Code("Woods")),
            new LinearTerrainFeature("road", 0, Code("Paved Road"), new CenterlinePath(FixedVector.FromPixels(0, 500),
                [new PathSegment(FixedVector.FromPixels(900, 450), FixedVector.FromPixels(300, 380), FixedVector.FromPixels(600, 560))]), FixedPoint.FromPixels(10), null),
            new LinearTerrainFeature("stream", 0, Code("Shallow Stream"), new CenterlinePath(FixedVector.FromPixels(1000, 0),
                [new PathSegment(FixedVector.FromPixels(1100, 645))]), FixedPoint.FromExactPixels(6.5), null),
            new BridgeFeature("bridge", 0, Box(1040, 280, 1070, 300), Code("Stone Bridge")),
            new BuildingFeature("house", 0, [Box(420, 180, 470, 230)], Code("Stone Building, 2 Level"), "B1"),
            new BuildingFeature("factory", 0, [Box(1300, 200, 1400, 280)], Code("Stone Factory, 1.5 Level"), "B2"),
            new HexsideTerrainFeature("wall", 0, Code("Wall"), [new HexsideSpan(new HexsideRef(hex, HexsideDirection.North)), new HexsideSpan(new HexsideRef(hex, HexsideDirection.NorthEast), 8, 56)]));
    }

    internal static string Hash(TerrainGrid grid)
    {
        var bytes = new byte[grid.CellCount * 2];
        grid.Codes.CopyTo(bytes);
        for (var cell = 0; cell < grid.CellCount; cell++)
        {
            bytes[grid.CellCount + cell] = unchecked((byte)grid.Elevations[cell]);
        }

        return Convert.ToHexStringLower(SHA256.HashData(bytes));
    }

    private static TerrainGrid Compile(FeatureModel model) => FeatureCompiler.Compile(model, Catalog).Grid;
}
