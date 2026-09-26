using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// Placed boards in the game record (Composed Maps Design, section 5): slots round-trip, an unplaced record reads as
/// before, replay checks the placement, and a position in a shared half hex must use its owner's name. The synthetic
/// village game is replayed with bd02 (or bd21) placed above its bd01.
/// </summary>
public sealed class MapPlacementTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly Dictionary<string, ConditionState> Concealed = new()
    {
        [Conditions.Concealed] = ConditionState.True,
        [Conditions.Hidden] = ConditionState.False,
    };

    private static List<GameEvent> Placed(params PlacedBoard[] boards)
    {
        var events = UnitGames.Read("a1-village.synthetic")!.Record!.Events.ToList();
        var started = (GameStarted)events[0].Payload;
        events[0] = events[0] with
        {
            Payload = started with
            {
                Map = started.Map with
                {
                    Reference = "test map",
                    Boards = boards
                }
            }
        };
        return events;
    }

    private static PlacedBoard Bd01(int row = 1) => new(BoardRef.Parse("bd01"), StateTests.Bd01Version, new BoardSlot(0, row, false));

    private static PlacedBoard Top(string board = "bd02", bool reversed = false, int column = 0, int row = 0) =>
        new(BoardRef.Parse(board), "v1", new BoardSlot(column, row, reversed));

    private static List<GameEvent> With(List<GameEvent> events, string id, string at)
    {
        var squad = new NewInstance(id, "asl:squad", "defender-squad", "russian", new MapPosition(BoardLocation.Parse(at)), null, Concealed);
        events.Add(events[^1] with
        {
            EventId = "c-" + id,
            Revision = events[^1].Revision + 1,
            Type = "instance-created",
            Payload = new InstanceCreated(squad),
            Causes = [],
            Visibility = null,
        });
        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new PlacedChains());

    [Fact]
    public void APlacedMapRoundTripsAndAnUnplacedOneReadsAsBefore()
    {
        var events = Placed(Top(reversed: true), Bd01());
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("placed", true, events));
        Assert.Contains("\"reversed\": true", text, StringComparison.Ordinal);
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        var map = ((GameStarted)read.Record!.Events[0].Payload).Map;
        Assert.True(map.IsPlaced);
        Assert.Equal([new BoardSlot(0, 0, true), new BoardSlot(0, 1, false)], map.Boards.Select(board => board.Slot));
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));

        var unplaced = ((GameStarted)UnitGames.Read("a1-village.synthetic")!.Record!.Events[0].Payload).Map;
        Assert.False(unplaced.IsPlaced);
        Assert.Empty(unplaced.Placements());
    }

    [Fact]
    public void APlacedGameReplays()
    {
        var history = Project(Placed(Top(), Bd01()));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
    }

    [Theory]
    [InlineData("mixed")]
    [InlineData("twice")]
    [InlineData("gap")]
    [InlineData("taken")]
    public void ReplayRefusesAPlacementTheBuilderWouldReject(string change)
    {
        var events = change switch
        {
            "mixed" => Placed(Top(), Bd01() with { Slot = null }),
            "twice" => Placed(Top("bd01"), Bd01()),
            "gap" => Placed(Top(), Bd01(row: 2)),
            _ => Placed(Top(), Bd01(row: 0)),
        };
        Assert.Contains(Project(events).Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-010");
    }

    [Fact]
    public void APositionInASharedHalfHexUsesItsOwnersName()
    {
        // bd01, placed below and later, owns the half hex that is bd02's F10 and its own F0.
        Assert.False(Project(With(Placed(Top(), Bd01()), "r9", "bd01:F0:0")).HasErrors);
        var refused = Project(With(Placed(Top(), Bd01()), "r9", "bd02:F10:0"));
        Assert.Contains(refused.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-010" && diagnostic.Message.Contains("F0", StringComparison.Ordinal));
    }

    [Fact]
    public void AUnitOnAReversedBoardKeepsItsBoardRelativePosition()
    {
        // U3: bd21:N5 on a map with bd21 reversed.
        var history = Project(With(Placed(Top("bd21", reversed: true), Bd01()), "r9", "bd21:N5:0"));
        Assert.False(history.HasErrors, string.Join(" ", history.Diagnostics));
        Assert.Equal("bd21:N5:0", history.Current!.Location("r9")!.Location.ToString());
    }

    /// <summary>The test chains of bd01, and standard geomorphic boards bd02 and bd21 with a ground level everywhere.</summary>
    private sealed class PlacedChains : ILocationChains
    {
        private readonly FakeChains bd01 = new();

        public string? Version(BoardRef board) => board.Value is "bd02" or "bd21" ? "v1" : bd01.Version(board);

        public IReadOnlyList<int>? Levels(BoardRef board, HexName hex) =>
            board.Value is "bd02" or "bd21" ? BoardGeometry.StandardGeomorphic.TryGetIndex(hex, out _) ? [0] : null : bd01.Levels(board, hex);

        public bool HasBridge(BoardRef board, HexName hex) => false;

        public BoardGeometry? Geometry(BoardRef board) => Version(board) is null ? null : BoardGeometry.StandardGeomorphic;
    }
}
