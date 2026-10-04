using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// LOS in the Studio (LOS Design, section 6) on a verified synthetic geomorphic board of open ground with woods
/// filling E4: E2 sees E3, and woods in E4 block E2 from E6.
/// </summary>
public sealed class StudioLosTests : IDisposable
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    private static readonly TerrainCatalog Catalog = new(
    [
        new TerrainType { Code = 0, Name = "Open Ground", Category = LosCategory.Open },
        new TerrainType { Code = 60, Name = "Woods", Category = LosCategory.Woods, IsLosObstacle = true, IsLowerLosObstacle = true, Height = 1 },
    ]);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-los-" + Guid.NewGuid().ToString("N"));
    private readonly StudioLos los;

    public StudioLosTests()
    {
        var options = new StudioOptions { CacheRoot = root, BoardsRoot = root };
        var maps = new MapService(options, new FakeVaslMapSource());
        los = new StudioLos(new BuildingBoards(maps), maps, options);
    }

    [Fact]
    public void ACheckReportsClearOrBlockedAndDrawsTheLine()
    {
        var board = Board(BoardStatus.Verified);
        var clear = los.Check(board, "E2", "ab-los:E3:0");
        Assert.Equal((LosStatus.Clear, false), (clear.Result!.Status, clear.Result.IsBlocked));
        Assert.StartsWith("Clear, range 1", clear.Summary, StringComparison.Ordinal);
        Assert.Contains("class=\"los-line\"", los.Layer(board, clear), StringComparison.Ordinal);

        var blocked = los.Check(board, "E2:0", "E6");
        Assert.Equal(LosStatus.Blocked, blocked.Result!.Status);
        Assert.Equal("Blocked at ab-los:E4 (225, 194), range 4: Terrain is higher than both the source and target (A6.2)", blocked.Summary);
        var layer = los.Layer(board, blocked);
        Assert.Contains("id=\"los-blocked-hex\"", layer, StringComparison.Ordinal);
        Assert.Contains("class=\"los-line-blocked\"", layer, StringComparison.Ordinal);
    }

    // Pass 31c (design section 14): Range, a sibling of LOS. The hexes between two Locations are read without the LOS (A6.7).
    [Fact]
    public void ARangeIsTheHexesBetweenTwoLocationsWhateverTheLos()
    {
        var board = Board(BoardStatus.Verified);

        // The woods in E4 block E2 from E6, and the range is 4 all the same.
        var range = StudioLos.Range(board, "E2", "ab-los:E6:0");
        Assert.Equal(4, range.Hexes);
        Assert.Equal("[E2] to [E6]: 4 hexes.", range.Summary(1));
        Assert.Equal("[E2] to [E3]: 1 hex.", StudioLos.Range(board, "E2", "E3").Summary(1));
        Assert.Equal("[E2] to [E2]: the same Location.", StudioLos.Range(board, "E2", "E2").Summary(1));
        Assert.Equal("[E2] to [E2], level 1: the same hex.", StudioLos.Range(board, "E2", "E2:1").Summary(1));

        var unread = StudioLos.Range(board, "nowhere", "E6");
        Assert.Null(unread.Hexes);
        Assert.Contains("board:hex:level", unread.Summary(1), StringComparison.Ordinal);
    }

    [Fact]
    public void TheRangeIsDrawnInTheLosLayerUnderTheLosLine()
    {
        var board = Board(BoardStatus.Verified);
        var range = StudioLos.Range(board, "E2", "E6");
        var squad = new RangeRow("u1", "4-4-7 squad R5", "ab-los:E2:0", 2, 4, "Long Range, FP x1/2 (A7.22)", LimboDancer.Domains.Asl.Rules.FireRangeBand.LongRange, false);

        // The range line with its hex count, and the outlines of Normal Range (solid) and of the farthest range (dashed), each with its label.
        var layer = los.Layer(board, null, range, squad);
        Assert.StartsWith("<g id=\"layer-los\"><g id=\"range-marks\" data-hexes=\"4\"", layer, StringComparison.Ordinal);
        Assert.Contains("class=\"range-line\"", layer, StringComparison.Ordinal);
        Assert.Contains("class=\"range-reach\" data-reach=\"2\"", layer, StringComparison.Ordinal);
        Assert.Contains("class=\"range-reach\" data-reach=\"4\"", layer, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(layer, "stroke-dasharray=\"10 7\""));
        Assert.Equal(["2", "4", "4"], [.. System.Text.RegularExpressions.Regex.Matches(layer, ">(\\d+)</text>").Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal)]);

        // An LOS check for the same ends is drawn over the range marks, in the one group the viewport takes.
        var both = los.Layer(board, los.Check(board, "E2", "E6"), range, squad);
        Assert.StartsWith("<g id=\"layer-los\" data-status=\"Blocked\"><g id=\"range-marks\"", both, StringComparison.Ordinal);
        Assert.True(both.IndexOf("range-marks", StringComparison.Ordinal) < both.IndexOf("class=\"los-line\"", StringComparison.Ordinal));

        // With no unit chosen the line alone is drawn; a weapon with no Long Range has one outline; and nothing read draws nothing.
        Assert.DoesNotContain("range-reach", los.Layer(board, null, range, null), StringComparison.Ordinal);
        var rifle = squad with
        {
            NormalRange = 3,
            Limit = 3
        };
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(los.Layer(board, null, range, rifle), "class=\"range-reach\""));
        Assert.Equal("<g id=\"layer-los\"></g>", los.Layer(board, null, null, null));
    }

    [Fact]
    public void AnUnverifiedBoardIsNotDefinitiveAndABadLocationIsExplained()
    {
        var unverified = los.Check(Board(BoardStatus.Ingested), "E2", "E6");
        Assert.Equal(LosStatus.Nondefinitive, unverified.Result!.Status);
        Assert.StartsWith("Not definitive on an unverified board: blocked at ab-los:E4", unverified.Summary, StringComparison.Ordinal);

        var board = Board(BoardStatus.Verified);
        Assert.Null(los.Check(board, "nowhere", "E6").Result);
        var off = los.Check(board, "E2", "ab-los:Z99:0");
        Assert.Null(off.Result);
        Assert.Contains("not on the map", off.Problem, StringComparison.Ordinal);
        Assert.Equal("<g id=\"layer-los\"></g>", los.Layer(board, off));
    }

    [Fact]
    public void TheSummaryCarriesTheHindranceBreakdown()
    {
        var result = new LosResult(LosStatus.Clear, false, 4, 3, null, string.Empty)
        {
            Hindrances = [new LosHindrance(1, 1.0), new LosHindrance(3, 2.0)],
            FirstHindranceAt = new LosHindranceAt(new GridPoint(225, 160), BoardRef.Parse("ab-los"), HexName.Parse("E3")),
        };
        var check = new LosCheck("E2", "E6", result, null, null, null);
        Assert.Equal("Hindrances 1 at range 1, 2 at range 3; first at ab-los:E3", check.Breakdown);
        Assert.Equal("Clear, range 4, hindrance 3. Hindrances 1 at range 1, 2 at range 3; first at ab-los:E3", check.Summary);
    }

    [Fact]
    public void AHexsideSourceIsAimedAtEitherVertex()
    {
        var board = Board(BoardStatus.Verified);
        var first = los.Check(board, "E2:0/2", "E6");
        var next = los.Check(board, "E2:0/2", "E6", LosAim.AuxiliaryPoint);
        Assert.NotNull(first.Result);
        Assert.NotNull(next.Result);
        Assert.NotEqual(first.From, next.From);
        Assert.Equal(first.To, next.To);
    }

    [Fact]
    public void AComposedMapIsAsDefinitiveAsItsBoards()
    {
        // A map built from placements that match no oracle scenario is only Ingested itself; its boards decide.
        var verified = new StudioLos(new Boards(BoardStatus.Verified), new MapService(new StudioOptions { CacheRoot = root, BoardsRoot = root }, new FakeVaslMapSource()),
            new StudioOptions { CacheRoot = root, BoardsRoot = root });
        Assert.Equal(LosStatus.Blocked, verified.Check(Composed(), "bd02:E2:0", "bd02:E6:0").Result!.Status);

        var ingested = new StudioLos(new Boards(BoardStatus.Ingested), new MapService(new StudioOptions { CacheRoot = root, BoardsRoot = root }, new FakeVaslMapSource()),
            new StudioOptions { CacheRoot = root, BoardsRoot = root });
        Assert.Equal(LosStatus.Nondefinitive, ingested.Check(Composed(), "bd02:E2:0", "bd02:E6:0").Result!.Status);
    }

    /// <summary>bd02 above bd01, both the synthetic board, built as a map the Studio marks Ingested.</summary>
    private static StudioBoard Composed()
    {
        var grid = Board(BoardStatus.Verified).Render.Grid;
        BoardPlacement[] placements = [new(BoardRef.Parse("bd02"), 0, 0), new(BoardRef.Parse("bd01"), 0, 1)];
        var map = VaslMapBuilder.Build([.. placements.Select(placement => new PlacedBoard(placement, grid, HexsideAnnotations.None))], Catalog, new LosSsRuleSet([])).Map!;
        var reference = BoardRef.Parse("map-placed-test");
        var render = BoardRenderInput.Create(reference, "Placed test map", map.Grid, Catalog, map.Facts);
        return new StudioBoard(reference, "v-map", "Placed test map", BoardStatus.Ingested, render, Catalog, null, null, null, [])
        {
            Composition = new StudioMap(placements, map),
        };
    }

    /// <summary>Loads bd01 and bd02 as the synthetic board with one status.</summary>
    private sealed class Boards(BoardStatus status) : IBoardProvider
    {
        public string? SourceDescription => "Synthetic";

        public string? CatalogBlob => null;

        public IReadOnlyList<BoardListing> List() => [];

        public BoardLoadResult Load(BoardRef board) => new(Board(status) with
        {
            Ref = board
        }, []);

        public BoardLoadResult? Cached(BoardRef board) => Load(board);
    }

    /// <summary>The synthetic board; each status is its own version, so the LOS maps are cached apart.</summary>
    private static StudioBoard Board(BoardStatus status)
    {
        var codes = new byte[Geometry.GridWidth * Geometry.GridHeight];
        for (var x = 188; x < 263; x++)
        {
            for (var y = 194; y < 258; y++)
            {
                codes[(x * Geometry.GridHeight) + y] = 60;
            }
        }

        var grid = new TerrainGrid(Geometry, codes, new sbyte[codes.Length], new bool[Geometry.HexCount]);
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, Catalog, HexsideAnnotations.None);
        var reference = BoardRef.Parse("ab-los");
        return new StudioBoard(reference, "v-" + status, "Synthetic LOS board", status, BoardRenderInput.Create(reference, "Synthetic LOS board", grid, Catalog, facts),
            Catalog, null, null, null, []);
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
