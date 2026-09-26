using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Placed boards and entry across a seam (Composed Maps Design, sections 6 and 7; U13). Board 01 from the oracle facts
/// sits in row 1 below a synthetic, verified bd02 of open ground with one wooden building at G10, so bd01's reviewed
/// wooden building G1 lies across the seam from bd02's G10, or from AA1 when bd02 is reversed.
/// </summary>
public sealed class SeamEntryTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c312");
    private static readonly GameScope Scope = new(Tenant, "seam");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-seam-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle(), Bd02()]);

    public SeamEntryTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    private GamePlay Play() => new(Planner(), store, new NullAudit());

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameState Current => Planner().Replay(store.Read(Scope)!.Events).Current!;

    private static object Placement(string id, string definition, string side, string at, bool concealed = false) => new
    {
        id,
        kind = "asl:squad",
        definition,
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, bool>
        {
            ["asl:broken"] = false,
            ["asl:berserk"] = false,
            ["asl:captured"] = false,
            ["asl:melee"] = false,
            ["asl:ti"] = false,
            ["asl:disrupted"] = false,
            ["asl:concealed"] = concealed,
            ["asl:hidden"] = false,
        },
    };

    private static object[] TwoBoards(bool reversed = false) =>
    [
        new { board = "bd02", column = 0, row = 0, reversed },
        new { board = "bd01", column = 0, row = 1 },
    ];

    private static JsonElement Setup(object[] boardList, params object[] placements) => Args(new
    {
        gameId = Scope.Game,
        attemptId = "setup-1",
        expectedRevision = 0,
        start = new
        {
            label = "Seam test",
            catalog = "asl-scenario-a1@1.0.0",
            boards = boardList,
            firstSide = "german",
            sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
        },
        placements,
    });

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        Assert.True(proposed.Outcome == PlayOutcome.NeedsConfirmation, string.Join(" | ", proposed.Plan?.Reasons ?? []));
        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    /// <summary>A game on the given boards and placements, advanced to the German MPh.</summary>
    private async Task InMph(object[] boardList, params object[] placements)
    {
        var play = Play();
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup, Setup(boardList, placements))).Outcome);
        for (var step = 0; step < 2; step++)
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase,
                Args(new
                {
                    gameId = Scope.Game,
                    attemptId = $"advance-{step}",
                    expectedRevision = Revision
                }))).Outcome);
        }
    }

    private JsonElement Entry(string location) =>
        Args(new
        {
            gameId = Scope.Game,
            attemptId = "enter-1",
            expectedRevision = Revision,
            unitId = "g1",
            location
        });

    [Theory]
    [InlineData(false, "bd02:G10:0")]
    [InlineData(true, "bd02:AA1:0")]
    public async Task ASquadAcrossTheSeamEntersABoard01BuildingAsFromBoard01(bool reversed, string from)
    {
        // U13: the concealed squad in G1 is revealed and the mover forced back to its hex on the other board.
        await InMph(TwoBoards(reversed), Placement("g1", "attacker-squad", "german", from),
            Placement("r1", "defender-squad", "russian", "bd01:G1:0", concealed: true));
        var map = Current.Map;
        Assert.True(map.IsPlaced);
        Assert.Equal(reversed ? "bd02@0,0/r bd01@0,1" : "bd02@0,0 bd01@0,1", map.Reference);

        var facts = Planner().EntryFacts(Current, Current.Unit("g1")!, BoardLocation.Parse("bd01:G1:0"));
        Assert.True(facts["isAdjacentGroundLevelOrdinaryBuilding"]);
        Assert.True(facts["hasNoRoadBypassElevationOrAdditionalTerrain"]);

        var start = Revision;
        Assert.Equal(PlayOutcome.Committed, (await Commit(Play(), GameActions.EnterBuilding, Entry("bd01:G1:0"))).Outcome);
        Assert.Equal(["entry-attempted", "conditions-changed", "entry-forced-back"], store.Read(Scope)!.Events.Skip((int)start).Select(item => item.Type));
        Assert.Equal((from, 2, true), (Current.Location("g1")!.Location.ToString(), Current.Unit("g1")!.MfSpent, Current.Unit("g1")!.MovementEnded));
        Assert.Equal(ConditionState.False, GameState.Condition(Current.Unit("r1")!, Conditions.Concealed));
    }

    [Fact]
    public async Task AnEntryAcrossTheSeamIntoAnotherBoardsBuildingIsRefused()
    {
        await InMph(TwoBoards(), Placement("g1", "attacker-squad", "german", "bd01:G1:0"),
            Placement("r1", "defender-squad", "russian", "bd02:G10:0", concealed: true));
        // The entry's outcome is withheld from the mover until confirmation, where it is refused.
        var start = Revision;
        var refused = await Commit(Play(), GameActions.EnterBuilding, Entry("bd02:G10:0"));
        Assert.NotEqual(PlayOutcome.Committed, refused.Outcome);
        Assert.Contains(refused.Plan!.Reasons, reason => reason.StartsWith("play.outside-reviewed-board", StringComparison.Ordinal));
        Assert.Equal(start, Revision);
    }

    [Theory]
    [InlineData("mixed")]
    [InlineData("gap")]
    [InlineData("shared-name")]
    public async Task SetupRefusesAPlacementThatCannotBePlayed(string change)
    {
        object[] boardList = change switch
        {
            "mixed" => [new { board = "bd02", column = 0, row = 0, reversed = false }, "bd01"],
            "gap" => [new { board = "bd02", column = 0, row = 0, reversed = false }, new { board = "bd01", column = 0, row = 2 }],
            _ => TwoBoards(),
        };

        // bd02's F10 is the half hex bd01, placed later, owns as F0.
        var at = change == "shared-name" ? "bd02:F10:0" : "bd01:G2:0";
        var proposed = await Play().ProposeAsync(GameActions.Setup, Setup(boardList, Placement("g1", "attacker-squad", "german", at)), Player);
        Assert.Equal(PlayOutcome.Denied, proposed.Outcome);
        Assert.Equal(0, Revision);
    }

    /// <summary>A verified synthetic board of open ground with a one-level wooden building at G10.</summary>
    private static BoardHandle Bd02()
    {
        var open = new TerrainType { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
        var wooden = new TerrainType { Code = 2, Name = "Wooden Building, 1 Level", Category = LosCategory.Open };
        var geometry = BoardGeometry.StandardGeomorphic;
        var hexes = geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index);
            var center = new LocationFacts(0, name.ToString() == "G10" ? wooden : open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            return new HexFacts(name, index, 0, false, center, [center], sides, null, CenterTerrainSource.CenterSample);
        }).ToArray();
        return new BoardHandle(BoardRef.Parse("bd02"), "synthetic-1", BoardReadStatus.Verified, "synthetic", new HexFactSet(geometry, "test", hexes));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class NullAudit : IAuditSink
    {
        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
