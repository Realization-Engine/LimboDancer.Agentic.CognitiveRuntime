using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

/// <summary>
/// The MapHexIndex's pure function (the MapHexIndex Design, ruling RMH.2), on a synthetic geomorphic board: every terrain and feature, with its hexes in
/// the board's order; a Location counted once; off-map hexsides skipped; and the saved index's search.
/// </summary>
public sealed class MapHexIndexTests
{
    private static readonly TerrainType Open = new() { Code = 0, Name = "Open Ground", Category = LosCategory.Open };
    private static readonly TerrainType Woods = new() { Code = 60, Name = "Woods", Category = LosCategory.Woods };
    private static readonly TerrainType Building = new() { Code = 42, Name = "Stone Building, 2 Level", Category = LosCategory.Building, Height = 2 };
    private static readonly TerrainType Wall = new() { Code = 72, Name = "Wall", Category = LosCategory.Hexside };
    private static readonly TerrainType Gully = new() { Code = 33, Name = "Gully", Category = LosCategory.Depression };
    private static readonly TerrainType Bridge = new() { Code = 84, Name = "Stone Bridge", Category = LosCategory.Bridge };
    private static readonly TerrainType Road = new() { Code = 66, Name = "Paved Road", Category = LosCategory.Road };

    private static HexFactSet Board()
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index).ToString();
            var center = new LocationFacts(0, name switch
            {
                "C3" => Woods,
                "D4" => Building,
                _ => Open
            }, name == "E5" ? Gully : null);
            IReadOnlyList<LocationFacts> locations = name == "D4" ? [center, new LocationFacts(1, Building, null), new LocationFacts(2, Building, null)] : [center];
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side,
                OnMap: !(name == "A1" && side == HexsideDirection.North), name == "F6" && side == HexsideDirection.North ? Road : Open,
                (name == "C3" && side == HexsideDirection.North) || (name == "A1" && side == HexsideDirection.North) ? Wall : null,
                Cliff: name == "G7" && side == HexsideDirection.North, false, false, false, null))];
            return new HexFacts(geometry.NameOf(index), index, name == "H8" ? 1 : 0, Stairway: name == "D4", center, locations, sides,
                name == "J9" ? new BridgeFacts(Bridge, 0) : null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new HexFactSet(geometry, "test", hexes);
    }

    private static MapHexFeature Feature(IReadOnlyList<MapHexFeature> features, string id) => Assert.Single(features, feature => feature.Id == id);

    [Fact]
    public void EveryTerrainAndFeatureIsListedWithItsHexes()
    {
        var features = MapHexIndexer.Features(Board());
        Assert.Equal(features.Select(feature => feature.Id).Order(StringComparer.Ordinal), features.Select(feature => feature.Id));
        Assert.Equal(["C3"], Feature(features, "terrain:Woods").Hexes);
        Assert.Equal(("terrain", "Woods"), (Feature(features, "terrain:Woods").Kind, Feature(features, "terrain:Woods").Name));
        Assert.Equal(["D4"], Feature(features, "terrain:Stone Building, 2 Level").Hexes);
        Assert.Equal(["E5"], Feature(features, "depression:Gully").Hexes);
        Assert.Equal(["F6"], Feature(features, "terrain:Paved Road").Hexes);
        Assert.Equal(["C3"], Feature(features, "hexside:Wall").Hexes);
        Assert.Equal(["G7"], Feature(features, "flag:cliff").Hexes);
        Assert.Equal(["D4"], Feature(features, "flag:stairway").Hexes);
        Assert.Equal(["H8"], Feature(features, "level:1").Hexes);
        Assert.Equal(["J9"], Feature(features, "bridge:Stone Bridge").Hexes);
        Assert.DoesNotContain(features, feature => feature.Id == "flag:slope");

        // Every hex is Open Ground somewhere (a hexside), each hex listed once, in the board's order.
        var open = Feature(features, "terrain:Open Ground").Hexes;
        Assert.Equal(open.Count, open.Distinct().Count());
        Assert.Equal(Board().Hexes[0].Hex.ToString(), open[0]);
    }

    [Fact]
    public void TheSavedIndexFindsTheBoardsThatHoldEveryFeatureAsked()
    {
        var features = MapHexIndexer.Features(Board());
        var index = new MapHexIndex(MapHexIndexer.Version, "test", "commit", "blob", "fidelity-20261028T000000Z",
        [
            new MapHexIndexBoard("bd01", "verified", "los1", features),
            new MapHexIndexBoard("bd02", "verified", "los2", [.. features.Where(feature => feature.Id != "flag:cliff")]),
            new MapHexIndexBoard("bd79", "failed", null, []),
        ]);
        var read = MapHexIndex.FromJson(index.ToJson())!;
        Assert.Equal(["bd01"], read.BoardsWith(["flag:cliff", "terrain:Woods"]).Select(board => board.Board));
        Assert.Equal(["bd01", "bd02"], read.BoardsWith(["terrain:Woods"]).Select(board => board.Board));
        Assert.Contains("hexside:Wall", read.FeatureIds());
        Assert.Equal(["C3"], read.Boards[0].Features.Single(feature => feature.Id == "terrain:Woods").Hexes);
    }
}
