using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The read contract (ASL-UNIT-060, 061) and the map read API (ASL-MAP-080), over the synthetic fixture and a
/// synthetic board 01 whose hexes all have a ground level, E4 also a cellar and a first level.
/// </summary>
public sealed class CaseReadTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    public static BoardHandle Board(BoardReadStatus status = BoardReadStatus.Verified, string version = StateTests.Bd01Version)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        HexFacts[] hexes = [.. geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index);
            LocationFacts[] levels = name.ToString() == "E4"
                ? [new(-1, null, null), new(0, null, null), new(1, null, null)]
                : [new(0, null, null)];
            return new HexFacts(name, index, 0, false, levels[name.ToString() == "E4" ? 1 : 0], levels, [], null, CenterTerrainSource.CenterSample);
        })];
        return new BoardHandle(BoardRef.Parse("bd01"), version, status, "synthetic board for tests", new HexFactSet(geometry, "test", hexes));
    }

    private static GameHistory History(int throughRevision = 21)
    {
        var events = UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(throughRevision).ToArray();
        var history = GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());
        Assert.False(history.HasErrors);
        return history;
    }

    private static CaseReader Reader(GameHistory? history = null, BoardHandle? board = null) =>
        new(new HistoryGameSource([history ?? History()]), new InMemoryBoardCatalog([board ?? Board()]), UnitsTestData.Asl.Value, [Catalog.Value]);

    private static CaseRequest Request(string attacker, string at, long revision = 21, string perspective = Perspective.AdjudicatorName, GameScope? scope = null) =>
        new(scope ?? History().Events[0].Scope, "asl-scenario-a1-test-package", attacker, BoardLocation.Parse(at), revision, new Perspective(perspective));

    [Fact]
    public void TheMapReadApiResolvesALocationWithItsVersionAndEvidence()
    {
        var board = Board();
        var read = board.Resolve(BoardLocation.Parse("bd01:E4:-1")).Read!;
        Assert.Equal(-1, read.Level.Level);
        Assert.Equal(StateTests.Bd01Version, read.BoardVersion);
        Assert.True(read.IsDefinitive);
        Assert.Equal($"board:bd01@{StateTests.Bd01Version}#E4:-1", read.Evidence);
        Assert.Equal("MAP-READ-002", Assert.Single(board.Resolve(BoardLocation.Parse("bd01:D4:1")).Diagnostics).Code);
        Assert.Equal("MAP-READ-002", Assert.Single(board.Resolve(BoardLocation.Parse("bd02:D4:0")).Diagnostics).Code);
        Assert.Equal(1, board.Distance(HexName.Parse("D4"), HexName.Parse("E4")));
        Assert.NotNull(board.Neighbor(HexName.Parse("D4"), HexsideDirection.North));
        Assert.False(Board(BoardReadStatus.Ingested).Resolve(BoardLocation.Parse("bd01:E4:0")).Read!.IsDefinitive);

        var catalog = new InMemoryBoardCatalog([board]);
        Assert.Same(board, catalog.TryGetBoard(BoardRef.Parse("bd01")).Board);
        Assert.Null(catalog.TryGetBoard(BoardRef.Parse("bd01"), "another-version").Board);
        Assert.Equal("MAP-READ-001", Assert.Single(catalog.TryGetBoard(BoardRef.Parse("bd02")).Diagnostics).Code);
    }

    [Fact]
    public void AnAdjudicatorReadIsOneConsistentSnapshot()
    {
        var result = Reader().Read(Request("gh1", "bd01:E5:0"));
        Assert.Equal(CaseReadStatus.Definitive, result.Status);
        var snapshot = result.Snapshot!;
        Assert.Equal((21L, StateTests.Bd01Version), (snapshot.Stamp.Revision, snapshot.Stamp.MapVersion));
        Assert.Equal(("fixture", 2, "mph", "german"), (snapshot.Source, snapshot.Turn, snapshot.Phase, snapshot.PhasingSide));
        Assert.Equal("asl-scenario-a1-test-package", snapshot.Request.Package);
        Assert.Equal("attacker-half-squad", snapshot.Attacker.Definition.Id);
        Assert.Equal(ConditionState.True, snapshot.Attacker.GoodOrder);
        Assert.Equal(1, snapshot.Attacker.MfSpent);
        Assert.Equal("bd01:D5:0", snapshot.Previous.Location.ToString());
        Assert.Equal("bd01:E5:0", snapshot.Target.Location.ToString());
        Assert.Equal($"board:bd01@{StateTests.Bd01Version}#E5:0", snapshot.Target.Evidence);
        Assert.Equal(["r2"], snapshot.Occupancy.Units.Select(unit => unit.Id));
        Assert.Equal(["f1"], snapshot.Occupancy.Entities.Select(entity => entity.Id));
        Assert.True(snapshot.Occupancy.Complete);
        Assert.Equal("r2", snapshot.Occupancy.SoleEnemy("german").Unit!.Id);

        // The phase's events that concern the case, in order, with the reveal among them.
        Assert.Equal(["e20", "e21"], snapshot.Events.Select(item => item.EventId));
        Assert.Equal("e21", Assert.Single(snapshot.Reveals).EventId);
    }

    [Fact]
    public void ASideNeverGetsASoleDefenderFromIncompleteInformation()
    {
        var german = Reader().Read(Request("gh1", "bd01:E5:0", perspective: "german"));
        Assert.Equal(CaseReadStatus.Definitive, german.Status);
        var occupancy = german.Snapshot!.Occupancy;
        Assert.Equal(["r2"], occupancy.Units.Select(unit => unit.Id));
        Assert.False(occupancy.Complete);
        var (unit, reason) = occupancy.SoleEnemy("german");
        Assert.Null(unit);
        Assert.Contains("hidden units (A12.3, p. 80)", reason, StringComparison.Ordinal);
    }

    [Fact]
    public void AConcealedOccupantIsOnlyASealedPresence()
    {
        var history = History(9);
        var german = Reader(history).Read(Request("g1", "bd01:E4:0", revision: 9, perspective: "german"));
        var occupancy = german.Snapshot!.Occupancy;
        Assert.Empty(occupancy.Units);
        Assert.Equal("sealed-1", Assert.Single(occupancy.Sealed).PlacementId);
        Assert.Contains("concealed presence", occupancy.IncompleteReason, StringComparison.Ordinal);

        var adjudicator = Reader(history).Read(Request("g1", "bd01:E4:0", revision: 9));
        Assert.Equal(["r1"], adjudicator.Snapshot!.Occupancy.Units.Select(item => item.Id));
        Assert.Equal(ConditionState.True, GameState.Condition(adjudicator.Snapshot.Occupancy.Units[0], Conditions.Concealed));
    }

    [Fact]
    public void AHiddenUnitCannotBeTheAttackerOfAnotherSidesRead()
    {
        var german = Reader(History(9)).Read(Request("r2", "bd01:E4:0", revision: 9, perspective: "german"));
        Assert.Equal((CaseReadStatus.Unavailable, "CASE-006"), (german.Status, german.Code));
        Assert.Null(german.Snapshot);
        var nobody = Reader(History(9)).Read(Request("nobody", "bd01:E4:0", revision: 9, perspective: "german"));
        Assert.Equal(german.Code, nobody.Code);
        Assert.Equal(german.Reason.Replace("r2", "nobody", StringComparison.Ordinal), nobody.Reason);
    }

    [Fact]
    public void EveryFailureIsAnExplicitNondefinitiveResult()
    {
        var scope = History().Events[0].Scope;
        Assert.Equal("CASE-001", Reader().Read(Request("gh1", "bd01:E5:0", scope: scope with
        {
            Tenant = Guid.Empty
        })).Code);
        Assert.Equal("CASE-002", Reader().Read(Request("gh1", "bd01:E5:0", scope: scope with
        {
            Game = "missing"
        })).Code);
        Assert.Equal("CASE-004", Reader().Read(Request("gh1", "bd01:E5:0", perspective: "american")).Code);
        var stale = Reader().Read(Request("gh1", "bd01:E5:0", revision: 20));
        Assert.Equal((CaseReadStatus.Stale, "CASE-005"), (stale.Status, stale.Code));
        Assert.Equal("CASE-006", Reader().Read(Request("g2", "bd01:E5:0")).Code);
        Assert.Equal("CASE-009", Reader().Read(Request("gh1", "bd02:E5:0")).Code);
        Assert.Equal("CASE-009", Reader().Read(Request("gh1", "bd01:E5:1")).Code);
        Assert.Equal(CaseReadStatus.Unavailable, Reader(board: Board(version: "another-version")).Read(Request("gh1", "bd01:E5:0")).Status);

        var unverified = Reader(board: Board(BoardReadStatus.Ingested)).Read(Request("gh1", "bd01:E5:0"));
        Assert.Equal((CaseReadStatus.Unverified, "CASE-010"), (unverified.Status, unverified.Code));
        Assert.NotNull(unverified.Snapshot);
        Assert.False(unverified.IsDefinitive);

        var broken = History().Events.ToList();
        broken[5] = broken[5] with
        {
            Revision = 99
        };
        var failed = GameProjector.Project(broken, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());
        var inconsistent = new CaseReader(new HistoryGameSource([failed]), new InMemoryBoardCatalog([Board()]), UnitsTestData.Asl.Value, [Catalog.Value])
            .Read(Request("gh1", "bd01:E5:0"));
        Assert.Equal((CaseReadStatus.Inconsistent, "CASE-003"), (inconsistent.Status, inconsistent.Code));
    }

    [Fact]
    public void ASnapshotGoesStaleOnlyWhenALaterEventAffectsIt()
    {
        var at16 = Reader(History(16)).Read(Request("g1", "bd01:E4:0", revision: 16)).Snapshot!;
        var history = History();
        Assert.True(CaseReader.IsStale(at16, history));
        Assert.True(CaseReader.Affects(at16, history.Events[16]));

        // At revision 20 the case is gh1 into E5; the reveal of r2 at revision 21 concerns E5, so it goes stale.
        var at20 = Reader(History(20)).Read(Request("gh1", "bd01:E5:0", revision: 20)).Snapshot!;
        Assert.True(CaseReader.Affects(at20, history.Events[20]));

        // A later event about another unit and other locations leaves an unrelated case current.
        var farCase = Reader(History(20)).Read(Request("g1", "bd01:E4:0", revision: 20)).Snapshot!;
        Assert.False(CaseReader.IsStale(farCase, history));
    }

    [Fact]
    public void StalenessIsJudgedOnlyFromEventsThePerspectiveMaySee()
    {
        var events = History().Events.Take(20).ToList();
        events.Add(events[^1] with
        {
            EventId = "secret",
            Revision = 21,
            Visibility = ["russian"],
            Payload = new ConditionsChanged("r2", new Dictionary<string, ConditionState> { [Conditions.Hidden] = ConditionState.False }),
        });
        var history = GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains());
        var german = Reader(History(20)).Read(Request("gh1", "bd01:E5:0", revision: 20, perspective: "german")).Snapshot!;
        Assert.False(CaseReader.IsStale(german, history));
        var adjudicator = Reader(History(20)).Read(Request("gh1", "bd01:E5:0", revision: 20)).Snapshot!;
        Assert.True(CaseReader.IsStale(adjudicator, history));
    }
}
