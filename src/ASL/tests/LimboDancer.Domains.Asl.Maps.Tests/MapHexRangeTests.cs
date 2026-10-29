using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class MapHexRangeTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;
    private static readonly MapLayout Normal = Layout(false);
    private static readonly MapLayout Reversed = Layout(true);

    [Theory]
    [InlineData("bd01:G4:0", "bd01:G4:3", 0)]
    [InlineData("bd01:G4:0", "bd01:G5:0", 1)]
    [InlineData("bd01:G4:-1", "bd01:G5:3", 1)]
    [InlineData("bd01:G4:0", "bd01:G6:0", 2)]
    public void SameHexAndAdjacentRangeIgnoreVerticalSeparation(string source, string target, int expected)
    {
        var from = BoardLocation.Parse(source);
        var to = BoardLocation.Parse(target);
        Assert.Equal(expected, MapHexRange.Read(from, to, null, Geometry));
        Assert.Equal(expected, MapHexRange.Read(to, from, null, Geometry));
    }

    [Theory]
    [InlineData("bd02:G10:0", "bd01:G1:0", 1)]
    [InlineData("bd02:G9:0", "bd01:G1:0", 2)]
    [InlineData("bd02:G10:2", "bd01:G1:-1", 1)]
    [InlineData("bd02:F10:0", "bd01:F0:0", 0)]
    public void PlacedRangeCrossesTheSeamAndRecognizesItsSharedHex(string source, string target, int expected)
    {
        // The same vertical layout as ComposedMapReadTests: bd02 above bd01.
        Assert.Equal(expected, MapHexRange.Read(BoardLocation.Parse(source), BoardLocation.Parse(target), Normal, null, placed: true));
    }

    [Fact]
    public void ReversedPlacementUsesBoardRelativeNamesBeforeMeasuringTheSeam()
    {
        // Reversing bd02 puts AA1 above bd01:G1, where G10 sits without reversal.
        var source = BoardLocation.Parse("bd02:AA1:2");
        var target = BoardLocation.Parse("bd01:G1:0");
        Assert.Equal(1, MapHexRange.Read(source, target, Reversed, null, placed: true));
        Assert.Equal(1, MapHexRange.Read(target, source, Reversed, null, placed: true));
        Assert.Equal(2, MapHexRange.Read(BoardLocation.Parse("bd02:AA2:0"), target, Reversed, null, placed: true));
        Assert.NotEqual(1, MapHexRange.Read(source, target, Normal, null, placed: true));
    }

    [Fact]
    public void MissingPlacedContextNeverFallsBackToLocalCoordinatesOrSameHex()
    {
        var source = BoardLocation.Parse("bd01:G4:0");
        Assert.Null(MapHexRange.Read(source, source, null, Geometry, placed: true));
        Assert.Null(MapHexRange.Read(source, BoardLocation.Parse("bd01:G5:0"), null, Geometry, placed: true));
        Assert.Null(MapHexRange.Read(source, BoardLocation.Parse("bd02:G4:0"), null, Geometry));
    }

    [Fact]
    public void MissingLocationsAndNamesOutsideTheResolvedGeometryHaveNoRange()
    {
        var source = BoardLocation.Parse("bd01:G4:0");
        var outside = BoardLocation.Parse("bd01:HH1:0");
        Assert.Null(MapHexRange.Read(null, source, null, Geometry));
        Assert.Null(MapHexRange.Read(source, null, Normal, null, placed: true));
        Assert.Null(MapHexRange.Read(source, outside, null, Geometry));
        Assert.Null(MapHexRange.Read(outside, source, null, Geometry));
        Assert.Null(MapHexRange.Read(outside, outside, null, Geometry));
        Assert.Null(MapHexRange.Read(source, outside, Normal, Geometry, placed: true));
        Assert.Null(MapHexRange.Read(source, BoardLocation.Parse("bd03:G4:0"), Normal, Geometry, placed: true));
    }

    [Fact]
    public void OnlyIdenticalUnplacedHexNamesHaveARangeWithoutGeometry()
    {
        var source = BoardLocation.Parse("bd01:G4:0");
        Assert.Equal(0, MapHexRange.Read(source, BoardLocation.Parse("bd01:G4:3"), null, null));
        Assert.Null(MapHexRange.Read(source, BoardLocation.Parse("bd01:G5:0"), null, null));
        Assert.Null(MapHexRange.Read(source, BoardLocation.Parse("bd02:G4:0"), null, null));
    }

    private static MapLayout Layout(bool reversedTop)
    {
        var result = MapLayout.Create(
        [
            (new BoardPlacement(BoardRef.Parse("bd02"), 0, 0, reversedTop, []), Geometry),
            (new BoardPlacement(BoardRef.Parse("bd01"), 0, 1), Geometry),
        ]);
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<MapLayout>(result.Layout);
    }
}
