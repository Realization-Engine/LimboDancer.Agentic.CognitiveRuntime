using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.MapStudio.Services.MapLab;
using LimboDancer.Domains.Asl.Units.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>Counters on a Map Lab map (the Goodwood Lab Techniques Analysis, section 5.2): the catalog's own units, placed by seed.</summary>
public sealed class MapLabUnitsTests
{
    private static readonly LabMap Map = LabMapGenerator.Generate(LabMapSpec.Clamped(10_000, 36, 7, LabTechnique.TilesLines));

    // Nothing is read from or written to this folder: the catalog and the built-in sheet are embedded.
    private static readonly UnitLibrary Library = new(new StudioOptions
    {
        CacheRoot = Path.Combine(Path.GetTempPath(), "asl-lab-units-unused"),
        BoardsRoot = Path.Combine(Path.GetTempPath(), "asl-lab-units-unused"),
    });

    [Fact]
    public void EachSideDrawsOnItsOwnCatalogInfantryAndSupportWeapons()
    {
        foreach (var side in LabUnits.Sides)
        {
            var pool = LabUnits.Pool(Library.CatalogChoices, side);
            Assert.NotEmpty(pool);
            Assert.All(pool, unit => Assert.Equal(side, unit.Side));
            Assert.Contains(pool, unit => unit.Kind == "asl:squad");
            Assert.Contains(pool, unit => unit.Kind == "asl:leader");
        }
    }

    [Fact]
    public void PlacesTheAskedCountOnTheSidesHalfRepeatablyAndContinuesTheNumbering()
    {
        var pool = LabUnits.Pool(Library.CatalogChoices, LabUnits.Russian);
        var first = LabUnits.Add(Map, pool, LabUnits.Russian, 0, 100);
        Assert.Equal(100, first.Count);
        Assert.Equal(first.Select(unit => unit.Location), LabUnits.Add(Map, pool, LabUnits.Russian, 0, 100).Select(unit => unit.Location));

        var target = UnitMapTarget.ForBoard(LabUnits.Board, Map.Geometry);
        var half = Map.Geometry.WidthInHexes / 2;
        Assert.All(first, unit =>
        {
            Assert.True(BoardLocation.TryParse(unit.Location, out var location));
            var hex = Assert.NotNull(target.Locate(location));
            Assert.InRange(hex.Column, Map.Geometry.WidthInHexes - half, Map.Geometry.WidthInHexes - 1);
        });

        var next = LabUnits.Add(Map, pool, LabUnits.Russian, 100, 50);
        Assert.Equal("lab-russian-101", next[0].Id);
        Assert.Equal(150, first.Concat(next).Select(unit => unit.Id).Distinct().Count());
    }

    [Fact]
    public void SpreadsTheUnitsOverTheWholeOfTheSidesHalf()
    {
        // Four by four parts of the west half: 1,000 units put about 62 in each. The first build, seeded with neighbouring numbers, drew its
        // hexes in bands and left whole parts empty.
        var units = LabUnits.Add(Map, LabUnits.Pool(Library.CatalogChoices, LabUnits.German), LabUnits.German, 0, 1_000);
        var target = UnitMapTarget.ForBoard(LabUnits.Board, Map.Geometry);
        var half = Map.Geometry.WidthInHexes / 2;
        var parts = new int[16];
        foreach (var unit in units)
        {
            Assert.True(BoardLocation.TryParse(unit.Location, out var location));
            var hex = Assert.NotNull(target.Locate(location));
            parts[(Math.Min(3, hex.Column * 4 / half) * 4) + Math.Min(3, hex.Row * 4 / Map.Geometry.HeightInHexes)]++;
        }

        Assert.All(parts, count => Assert.InRange(count, 25, 120));
    }

    [Fact]
    public void TheTileIndexCarriesTheHexLatticeTheViewportPlacesCountersBy()
    {
        using var index = System.Text.Json.JsonDocument.Parse(LabMapRenderer.TileIndex(Map));
        var root = index.RootElement;
        Assert.Equal(Map.Geometry.HexWidth, root.GetProperty("hexWidth").GetDouble());
        Assert.Equal(Map.Geometry.HexHeight, root.GetProperty("hexHeight").GetDouble());
        Assert.Equal(Map.Geometry.A1CenterX, root.GetProperty("a1x").GetDouble());
        Assert.Equal(Map.Geometry.A1CenterY, root.GetProperty("a1y").GetDouble());
        Assert.Equal(Map.Geometry.GridWidth, root.GetProperty("width").GetInt32());
    }

    [Fact]
    public void StopsAtTheLimitOfASide()
    {
        var pool = LabUnits.Pool(Library.CatalogChoices, LabUnits.German);
        Assert.Equal(10, LabUnits.Add(Map, pool, LabUnits.German, LabUnits.MaxPerSide - 10, 1000).Count);
        Assert.Empty(LabUnits.Add(Map, pool, LabUnits.German, LabUnits.MaxPerSide, 10));
        Assert.Empty(LabUnits.Add(Map, [], LabUnits.German, 0, 10));
    }

    [Fact]
    public void TheStudiosCounterLayerDrawsEveryPlacedUnit()
    {
        var units = LabUnits.Sides.SelectMany(side => LabUnits.Add(Map, LabUnits.Pool(Library.CatalogChoices, side), side, 0, 200)).ToArray();
        var renderer = Assert.IsType<UnitRenderer>(Library.Renderer(UnitLibrary.DefaultSheet));
        var overlay = UnitOverlayBuilder.Build(UnitMapTarget.ForBoard(LabUnits.Board, Map.Geometry), LabUnits.SetId, units, renderer);
        Assert.Empty(overlay.Diagnostics);
        Assert.Equal(400, overlay.Units.Count);
        Assert.Contains("data-unit-set=\"map-lab\"", overlay.Open, StringComparison.Ordinal);
    }
}
