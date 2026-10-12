using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>
/// Pass 40, task 40.2: the two target points of a hex entered at the road rate (A4.132, p. 48), on the rule's own example, board 5. The squad
/// enters the woods-road hex [I4] from [H3]; the building [I9] is entered by the road of [I10]. Runs against a local VASL checkout.
/// </summary>
public sealed class LosTargetPointTests
{
    private static readonly string[] AlongTheRoad = ["G2", "J3", "K3", "L2"];
    private static readonly string[] BehindTheWoods = ["I5", "J4"];
    private static readonly string[] BesideTheHexside = ["H4", "I3"];

    private static LosMap Board05()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var shared = vasl.ReadTerrainCatalog();
        var catalog = Assert.IsType<TerrainCatalog>(shared.Catalog);
        var board = Assert.IsType<IngestedBoard>(VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, "05"), catalog).Board);
        var handle = new BoardHandle(board.Board, board.Provenance.LosData.ContentBlob, BoardReadStatus.Verified, "VASL checkout", HexFactFidelity.Derive(board, catalog))
        {
            Los = new LosData(board.Grid, catalog, HexFactFidelity.Annotations(board.Metadata), shared.Rules),
        };
        return Assert.IsType<LosMap>(LosMap.ForBoard(handle).Map);
    }

    private static bool? Crosses(LosMap map, string from, string to, HexsideDirection? across) =>
        LosTargetPoints.CrossesTerrainInTargetHex(map, BoardLocation.Parse($"bd05:{from}:0"), BoardLocation.Parse($"bd05:{to}:0"), across);

    [VaslFact]
    public void TheWoodsRoadHexOfTheExampleIsSeenAlongItsRoadAndNotThroughItsWoods()
    {
        var map = Board05();

        // "A squad entering 5I4 at a cost of one MF would be subject to" FFMO "for fire from G2, J3, K3, or L2": the center dot is seen along the road.
        Assert.All(AlongTheRoad, firer => Assert.False(Crosses(map, firer, "I4", null), firer));

        // "Fire from I5 or J4 to I4 would never receive the -1 FFMO DRM because it must trace its LOS through the Woods depiction in I4":
        // neither the center dot nor the road's hexside is reached clear.
        Assert.All(BehindTheWoods, firer => Assert.True(Crosses(map, firer, "I4", null), firer));
        Assert.All(BehindTheWoods, firer => Assert.True(Crosses(map, firer, "I4", HexsideDirection.NorthWest), firer));

        // "Fire from H4 or I3 would qualify ... only if the unit entered I4 at the one MF rate across the H3-I4 hexside": the center dot is
        // behind the woods, and the point where the road crosses that hexside is not.
        Assert.All(BesideTheHexside, firer => Assert.True(Crosses(map, firer, "I4", null), firer));
        Assert.All(BesideTheHexside, firer => Assert.False(Crosses(map, firer, "I4", HexsideDirection.NorthWest), firer));
    }

    [VaslFact]
    public void TheBuildingOfTheExampleIsSeenAtItsRoadHexsideButNotFromBehind()
    {
        var map = Board05();

        // "Infantry could use the road in 5I10 to enter the building in I9 ... subject to the FFMO DRM ... for fire from any adjacent hex (except
        // I8, whose LOS cannot reach either the hex center dot or any point on the road depiction of the I9-I10 hexside without first
        // encountering the building obstacle)". The center dot is in the building for every firer.
        Assert.True(Crosses(map, "I8", "I9", null));
        Assert.True(Crosses(map, "I8", "I9", HexsideDirection.South));
        Assert.False(Crosses(map, "I10", "I9", HexsideDirection.South));
    }
}
