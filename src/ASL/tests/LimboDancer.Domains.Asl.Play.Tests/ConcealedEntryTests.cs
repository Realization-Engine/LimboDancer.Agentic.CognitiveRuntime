using System.Text.Json;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// The one entry action over occupied and concealed buildings (Occupied and Concealed Entry Design, sections 6 to 8),
/// and acceptance scenarios U4 to U8, on board 01 as the committed Hex Fact oracle fixture reads it: a German squad in
/// the wooden building D4 attempts E4, a stone building whose type the pinned metadata states outright.
/// </summary>
public sealed class ConcealedEntryTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c308");
    private static readonly GameScope Scope = new(Tenant, "village");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly Perspective German = Perspective.Side("german");
    private static readonly string[] Bd01 = ["bd01"];

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-entry-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly IBoardCatalog boards = new InMemoryBoardCatalog([Board01Fixture.Handle()]);

    public ConcealedEntryTests() => store = new FileGameStore(root);

    private GamePlanner Planner() => new(store, boards, Vocabulary, [Catalog]);

    private GamePlay Play() => new(Planner(), store, new NullAudit());

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static Dictionary<string, object> Placement(string id, string definition, string side, string at, bool concealed = false, bool hidden = false,
        bool berserk = false, bool knownDisrupted = true)
    {
        var conditions = new Dictionary<string, object>
        {
            ["asl:broken"] = false,
            ["asl:berserk"] = berserk,
            ["asl:captured"] = false,
            ["asl:melee"] = false,
            ["asl:ti"] = false,
            ["asl:concealed"] = concealed,
            ["asl:hidden"] = hidden,
        };
        if (knownDisrupted)
        {
            conditions["asl:disrupted"] = false;
        }

        return new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : "asl:squad",
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new { at },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Entity(string id, string kind, string side, string at) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, object>(),
    };

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private long Revision => store.Read(Scope)?.Events.Count ?? 0;

    private GameHistory History() => Planner().Replay(store.Read(Scope)!.Events);

    /// <summary>A game with the German squad g1 in D4 and the given placements, advanced to the German MPh.</summary>
    private async Task<GamePlay> GameInMph(params Dictionary<string, object>[] extra)
    {
        var play = Play();
        var setup = Args(new
        {
            gameId = Scope.Game,
            attemptId = "setup-1",
            expectedRevision = 0,
            start = new
            {
                label = "Village test",
                catalog = "asl-scenario-a1@1.0.0",
                boards = Bd01,
                firstSide = "german",
                sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
            },
            placements = new[] { Placement("g1", "attacker-squad", "german", "bd01:D4:0") }.Concat(extra).ToArray(),
        });
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup, setup)).Outcome);
        for (var step = 0; step < 2; step++)
        {
            Assert.Equal(PlayOutcome.Committed,
                (await Commit(play, GameActions.AdvancePhase, Args(new
                {
                    gameId = Scope.Game,
                    attemptId = $"advance-{step}",
                    expectedRevision = Revision
                }))).Outcome);
        }

        Assert.Equal("mph", History().Current!.Phase);
        return play;
    }

    private JsonElement Entry(string attempt, string unit = "g1", string location = "bd01:E4:0", long? expected = null) =>
        Args(new
        {
            gameId = Scope.Game,
            attemptId = attempt,
            expectedRevision = expected ?? Revision,
            unitId = unit,
            location
        });

    [Theory]
    [InlineData(true, 4)]
    [InlineData(false, 3)]
    public async Task AnEntryIntoOneConcealedOrHiddenSquadRevealsItAndForcesTheMoverBack(bool hidden, int events)
    {
        // U7 and U4: the attacker sees nothing (hidden) or a sealed presence (concealed) before, and the squad after.
        var play = await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", concealed: !hidden, hidden: hidden));
        var before = GameView.Of(History(), Revision, German);
        Assert.DoesNotContain(before.Units, unit => unit.Id == "r1");
        Assert.Equal(hidden ? 0 : 1, before.Sealed.Count);

        var start = Revision;
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Equal([EntryDisclosure.ResolvedOnConfirmation], proposed.Plan!.Disclosure!.ReasonsForMover(proposed.Plan, confirmed: false));
        Assert.Equal(start, Revision);

        var committed = await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", expected: start), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Committed, committed.Outcome);
        var record = store.Read(Scope)!;
        string[] types = hidden
            ? ["entry-attempted", "conditions-changed", "conditions-changed", "entry-forced-back"]
            : ["entry-attempted", "conditions-changed", "entry-forced-back"];
        Assert.Equal(types, record.Events.Skip((int)start).Select(item => item.Type));
        Assert.Equal(events, types.Length);
        Assert.All(record.Events.Skip((int)start + 1), item => Assert.Contains(record.Events[(int)start].EventId, item.Causes));
        Assert.All(record.Events.Skip((int)start), item => Assert.Equal(ScenarioA1.ScenarioA1PostRevealPackage.Identity.ToString(), item.RulePackage));

        var state = History().Current!;
        var mover = state.Unit("g1")!;
        Assert.Equal(("bd01:D4:0", 2, true), (state.Location("g1")!.Location.ToString(), mover.MfSpent, mover.MovementEnded));
        var after = GameView.Of(History(), Revision, German);
        Assert.Contains(after.Units, unit => unit.Id == "r1");
        Assert.Empty(after.Sealed);

        // A second confirmation of the same attempt changes nothing.
        var again = await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", expected: start), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Replay, again.Outcome);
        Assert.Equal(start + events, Revision);
    }

    [Fact]
    public async Task AUnitForcedBackCannotMoveAgainThisPhase()
    {
        var play = await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", hidden: true));
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.EnterBuilding, Entry("enter-1"))).Outcome);
        var start = Revision;
        var again = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-2"), Player);
        Assert.Equal(PlayOutcome.Denied, again.Outcome);
        Assert.Contains("play.fact-false: canMoveThisPhase", again.Plan!.Disclosure!.MoverReasons);
        Assert.Equal(start, Revision);
    }

    [Fact]
    public async Task AnEntryIntoAKnownEnemySquadIsProhibitedByA414()
    {
        // U8: the attacker sees the squad, so the refusal and its reason are disclosed at once.
        var play = await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0"));
        var start = Revision;
        var result = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Equal(EntryRoute.KnownEnemy, result.Plan!.Disclosure!.Route);
        Assert.False(result.Plan.Disclosure.Withheld);
        Assert.Contains(result.Plan.Reasons, reason => reason.StartsWith("play.prohibited: A4.14", StringComparison.Ordinal));
        Assert.Equal(start, Revision);
    }

    [Theory]
    [InlineData("two-concealed", "play.outside-reviewed-cases")]
    [InlineData("concealed-leader", "play.outside-reviewed-cases")]
    [InlineData("return-wire", "play.return-hazard")]
    public async Task AnEntryNoReviewedCaseCoversIsRefusedWithoutNamingWhatIsHidden(string change, string reason)
    {
        var play = change switch
        {
            "two-concealed" => await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", concealed: true),
                Placement("r2", "defender-squad", "russian", "bd01:E4:0", hidden: true)),
            "concealed-leader" => await GameInMph(Placement("r1", "defender-leader", "russian", "bd01:E4:0", concealed: true)),
            _ => await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", hidden: true), Entity("w1", "asl:wire", "russian", "bd01:D4:0")),
        };
        var start = Revision;
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        var confirmed = await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", expected: start), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Denied, confirmed.Outcome);
        Assert.Equal([EntryDisclosure.CannotResolve], confirmed.Plan!.Disclosure!.ReasonsForMover(confirmed.Plan, confirmed: true));
        Assert.Contains(confirmed.Plan.Reasons, item => item.StartsWith(reason, StringComparison.Ordinal));
        Assert.Equal(start, Revision);
    }

    [Fact]
    public async Task AFriendlyOccupantIsRefusedOpenly()
    {
        var play = await GameInMph(Placement("g2", "attacker-squad", "german", "bd01:E4:0"));
        var result = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.False(result.Plan!.Disclosure!.Withheld);
        Assert.Contains(result.Plan.Reasons, reason => reason.StartsWith("play.outside-reviewed-cases", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("berserk", "play.fact-false: isKnownGoodOrderInfantrySquad")]
    [InlineData("disrupted-unknown", "play.a414-exception")]
    public async Task AMoverThatMayBeAnA414ExceptionIsRefused(string change, string reason)
    {
        var mover = change == "berserk"
            ? Placement("g2", "attacker-squad", "german", "bd01:F4:0", berserk: true)
            : Placement("g2", "attacker-squad", "german", "bd01:F4:0", knownDisrupted: false);
        var play = await GameInMph(mover, Placement("r1", "defender-squad", "russian", "bd01:E4:0", hidden: true));
        var start = Revision;
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1", unit: "g2"), Player);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation
            ? await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", unit: "g2", expected: start), Player, proposed.Correlation)
            : proposed;
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        // A Berserk unit is not in Good Order, so it is refused before A4.14 is reached.
        Assert.Contains(result.Plan!.Reasons, item => item.StartsWith(reason, StringComparison.Ordinal));
        Assert.Equal(start, Revision);
    }

    [Fact]
    public async Task AnEmptyBuildingIsEnteredThroughTheSameActionWithoutDisclosingItIsEmpty()
    {
        var play = await GameInMph();
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Equal([EntryDisclosure.ResolvedOnConfirmation], proposed.Plan!.Disclosure!.ReasonsForMover(proposed.Plan, confirmed: false));
        var committed = await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", expected: Revision), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Committed, committed.Outcome);
        Assert.Equal("bd01:E4:0", History().Current!.Location("g1")!.Location.ToString());
    }

    [Fact]
    public async Task AConfirmationAfterALaterChangeIsStale()
    {
        var play = await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", hidden: true));
        var start = Revision;
        var proposed = await play.ProposeAsync(GameActions.EnterBuilding, Entry("enter-1"), Player);
        Assert.Equal(PlayOutcome.Committed,
            (await Commit(play, GameActions.AdvancePhase, Args(new
            {
                gameId = Scope.Game,
                attemptId = "advance-late",
                expectedRevision = start
            }))).Outcome);
        var confirmed = await play.ConfirmAsync(GameActions.EnterBuilding, Entry("enter-1", expected: start), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Stale, confirmed.Outcome);
        Assert.Equal(start + 1, Revision);
    }

    [Fact]
    public async Task ACaseReadBeforeTheAttemptIsStaleAfterItAndStaysNondefinitiveForTheAttacker()
    {
        // U5 and U6 over a live game.
        var play = await GameInMph(Placement("r1", "defender-squad", "russian", "bd01:E4:0", concealed: true));
        var start = Revision;
        var reader = new CaseReader(new HistoryGameSource([History()]), boards, Vocabulary, [Catalog]);
        var read = reader.Read(new CaseRequest(Scope, "scenario-a1", "g1", BoardLocation.Parse("bd01:E4:0"), start, German));
        Assert.NotNull(read.Snapshot);
        Assert.False(read.Snapshot!.Occupancy.Complete);
        Assert.Null(read.Snapshot.Occupancy.SoleEnemy("russian").Unit);

        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.EnterBuilding, Entry("enter-1"))).Outcome);
        Assert.True(CaseReader.IsStale(read.Snapshot, History()));
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
