using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class BoardFootprintTests
{
    private static readonly BoardRef Board = BoardRef.Parse("ab-footprint-test");
    private static readonly HexFactSet Facts = CreateFacts();

    private static HexFactSet CreateFacts()
    {
        var geometry = BoardGeometry.Standard(4, 4);
        var open = new TerrainType { Code = 0, Name = "Open Ground", Category = LosCategory.Open };
        var center = new LocationFacts(0, open, null);
        return new(geometry, "test", geometry.Hexes().Select(index => new HexFacts(geometry.NameOf(index), index,
            0, false, center, [center], [], null, CenterTerrainSource.CenterSample)).ToArray());
    }

    private static BoardHandle Handle(IEnumerable<HexName>? footprint = null) =>
        new(Board, "accepted-terrain", BoardReadStatus.AuthoredValid, "Test", Facts, locationHexes: footprint);

    [Fact]
    public void FootprintExcludesGuardLocationsAndNeighborsButRetainsAllTerrainFacts()
    {
        var allowed = new HashSet<HexName> { HexName.Parse("B2"), HexName.Parse("C2") };
        var handle = Handle(allowed);
        allowed.Add(HexName.Parse("A1")); // A caller cannot widen an accepted handle after construction.
        Assert.Same(Facts, handle.Facts);
        Assert.Equal("accepted-terrain", handle.Version);
        Assert.NotNull(handle.Resolve(BoardLocation.Parse("ab-footprint-test:B2:0")).Read);
        Assert.Null(handle.Resolve(BoardLocation.Parse("ab-footprint-test:A1:0")).Read);
        Assert.Null(handle.HexFacts(HexName.Parse("A1")));
        Assert.NotNull(handle.Facts[HexName.Parse("A1")]);
        Assert.Equal(HexName.Parse("C2"), handle.Neighbor(HexName.Parse("B2"), HexsideDirection.NorthEast));
        Assert.Null(handle.Neighbor(HexName.Parse("B2"), HexsideDirection.South));
        Assert.Null(handle.Neighbor(HexName.Parse("A1"), HexsideDirection.South));
        Assert.Equal(1, handle.Distance(HexName.Parse("B2"), HexName.Parse("C2")));
        Assert.Null(handle.Distance(HexName.Parse("A1"), HexName.Parse("B2")));
    }

    [Fact]
    public void OrdinaryBoardsRetainTheirCompleteLocationArea()
    {
        var handle = Handle();
        Assert.Null(handle.LocationHexes);
        Assert.All(Facts.Hexes, hex => Assert.NotNull(handle.HexFacts(hex.Hex)));
        Assert.Null(handle.HexFacts(HexName.Parse("Z99")));
    }

    [Fact]
    public void RefusesEmptyOrOutOfBoardFootprints()
    {
        Assert.Throws<ArgumentException>(() => Handle([]));
        Assert.Throws<ArgumentException>(() => Handle([HexName.Parse("Z99")]));
    }
}
