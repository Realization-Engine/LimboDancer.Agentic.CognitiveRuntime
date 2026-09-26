using System.Text.Json;
using System.Text.Json.Nodes;
using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Domain;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Maps.Terrain;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>
/// Governed writes (Governed Writes Design): setup, the sequence of play, and the reviewed empty-building entry, each
/// committed only through the Execution Gate, on a synthetic verified board 01 whose E4 is a wooden building.
/// </summary>
public sealed class PlayTests : IDisposable
{
    private static readonly Guid Tenant = Guid.Parse("7b1d2c3e-0000-4000-8000-00000000c303");
    private static readonly UnitVocabulary Vocabulary = UnitVocabulary.Asl();
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, Vocabulary)!.Catalog!;
    private static readonly UnitCatalog Synthetic = UnitCatalogs.Read(UnitCatalogs.ScenarioA1Synthetic, Vocabulary)!.Catalog!;
    private static readonly TerrainType Open = new() { Code = 1, Name = "Open Ground", Category = LosCategory.Open };
    private static readonly TerrainType Wooden = new() { Code = 2, Name = "Wooden Building", Category = LosCategory.Building };

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-" + Guid.NewGuid().ToString("N"));
    private readonly FileGameStore store;
    private readonly RecordingAudit audit = new();
    private TerrainType e4 = Wooden;

    public PlayTests() => store = new FileGameStore(root);

    private BoardHandle Board(BoardReadStatus status = BoardReadStatus.Verified)
    {
        var geometry = BoardGeometry.StandardGeomorphic;
        HexFacts[] hexes = [.. geometry.Hexes().Select(index =>
        {
            var name = geometry.NameOf(index);
            var level = new LocationFacts(0, name.ToString() == "E4" ? e4 : Open, null);
            HexsideFacts[] sides = [.. Enum.GetValues<HexsideDirection>().Select(side => new HexsideFacts(side, true, null, null, false, false, false, false, null))];
            return new HexFacts(name, index, 0, false, level, [level], sides, null, CenterTerrainSource.CenterSample);
        })];
        return new BoardHandle(BoardRef.Parse("bd01"), "v-test", status, "synthetic board for tests", new HexFactSet(geometry, "test", hexes));
    }

    private GamePlay Play(BoardReadStatus status = BoardReadStatus.Verified, TimeProvider? time = null) =>
        new(Planner(status, time), store, audit);

    private GamePlanner Planner(BoardReadStatus status = BoardReadStatus.Verified, TimeProvider? time = null) =>
        new(store, new InMemoryBoardCatalog([Board(status)]), Vocabulary, [Catalog, Synthetic], time);

    private static readonly LimboDancer.Abstractions.Execution.RuntimePrincipal Player =
        GamePlay.Principal("player", Tenant, GameActions.SetupPermission, GameActions.PlayPermission);

    private static readonly string[] Bd01 = ["bd01"];

    private static JsonElement Args(object value) => JsonSerializer.SerializeToElement(value);

    private static object Unit(string id, string definition, string side, string at, bool concealed = false, bool hidden = false) => new
    {
        id,
        kind = definition.Contains("leader", StringComparison.Ordinal) ? "asl:leader" : definition.Contains("half", StringComparison.Ordinal) ? "asl:half-squad" : "asl:squad",
        definition,
        side,
        position = new
        {
            at
        },
        conditions = new Dictionary<string, object>
        {
            ["asl:broken"] = false,
            ["asl:berserk"] = false,
            ["asl:captured"] = false,
            ["asl:melee"] = false,
            ["asl:ti"] = false,
            ["asl:disrupted"] = false,
            ["asl:concealed"] = concealed,
            ["asl:hidden"] = hidden,
        },
    };

    private static JsonElement SetupArgs(string attempt, long expected, object[] placements, string catalog = "asl-scenario-a1@1.1.0", bool start = true) =>
        Args(start
            ? new
            {
                gameId = "village",
                attemptId = attempt,
                expectedRevision = expected,
                start = new
                {
                    label = "Village test",
                    catalog,
                    boards = Bd01,
                    firstSide = "german",
                    sides = new[] { new { id = "german", nationality = "german" }, new { id = "russian", nationality = "russian" } },
                },
                placements,
            }
            : new
            {
                gameId = "village",
                attemptId = attempt,
                expectedRevision = expected,
                placements
            });

    private static async Task<PlayResult> Commit(GamePlay play, Abstractions.Actions.ActionDescriptor action, JsonElement arguments)
    {
        var proposed = await play.ProposeAsync(action, arguments, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        return await play.ConfirmAsync(action, arguments, Player, proposed.Correlation);
    }

    private GameState Current() => Planner().Replay(store.Read(new GameScope(Tenant, "village"))!.Events).Current!;

    /// <summary>A game with a German squad in D4 next to the wooden building in E4, set up and advanced to the German MPh.</summary>
    private async Task<GamePlay> GameInMph(params object[] extra)
    {
        var play = Play();
        Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.Setup,
            SetupArgs("setup-1", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0"), .. extra]))).Outcome);
        var revision = 1 + 1 + extra.Length;
        for (var step = 0; step < 2; step++)
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase,
                Args(new
                {
                    gameId = "village",
                    attemptId = $"advance-{step}",
                    expectedRevision = revision + step
                }))).Outcome);
        }

        Assert.Equal("mph", Current().Phase);
        return play;
    }

    private static JsonElement EntryArgs(string attempt, long expected, string unit = "g1", string location = "bd01:E4:0") =>
        Args(new
        {
            gameId = "village",
            attemptId = attempt,
            expectedRevision = expected,
            unitId = unit,
            location
        });

    [Fact]
    public async Task SetupNeedsConfirmationAndCommitsALiveGame()
    {
        var play = Play();
        var arguments = SetupArgs("setup-1", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0"), Unit("r1", "defender-squad", "russian", "bd01:G4:0")]);
        var proposed = await play.ProposeAsync(GameActions.Setup, arguments, Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        Assert.Null(store.Read(new GameScope(Tenant, "village")));

        var committed = await play.ConfirmAsync(GameActions.Setup, arguments, Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Committed, committed.Outcome);
        var record = store.Read(new GameScope(Tenant, "village"))!;
        Assert.False(record.Synthetic);
        Assert.Equal(["game-started", "instance-created", "instance-created"], record.Events.Select(item => item.Type));
        Assert.All(record.Events, item => Assert.Equal(LiveGames.Source, item.Source));
        var state = Current();
        Assert.False(state.Synthetic);
        Assert.Equal(("v-test", "german", "rph"), (state.Map.Boards[0].Version, state.FirstSide, state.Phase));
        Assert.Contains(audit.Events, item => item.EventType == AuditEventType.GateAuthorized);
        Assert.Contains(audit.Events, item => item.EventType == AuditEventType.ExecutorCompleted);
    }

    [Fact]
    public async Task ALiveGameIsRefusedAnywhereButItsLiveSource()
    {
        await GameInMph();
        var events = store.Read(new GameScope(Tenant, "village"))!.Events;
        var elsewhere = GameProjector.Project(events, Vocabulary, [Catalog]);
        Assert.Contains(elsewhere.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-017");
    }

    [Fact]
    public async Task HiddenAndConcealedPlacementsAreVisibleOnlyToTheirSide()
    {
        var play = Play();
        await Commit(play, GameActions.Setup, SetupArgs("setup-1", 0,
            [Unit("r1", "defender-squad", "russian", "bd01:G4:0", concealed: true), Unit("r2", "defender-leader", "russian", "bd01:G5:0", hidden: true),
                Unit("g1", "attacker-squad", "german", "bd01:D4:0")]));
        var record = store.Read(new GameScope(Tenant, "village"))!;
        Assert.Equal(["russian"], record.Events[1].Visibility);
        Assert.Equal(["russian"], record.Events[2].Visibility);
        Assert.Null(record.Events[3].Visibility);
    }

    public static TheoryData<string> SetupRefusals => ["synthetic-catalog", "unverified-board", "off-board", "no-permission", "unknown-definition"];

    [Theory]
    [MemberData(nameof(SetupRefusals))]
    public async Task SetupIsRefusedWhenItBreaksARule(string change)
    {
        var play = change == "unverified-board" ? Play(BoardReadStatus.Ingested) : Play();
        var arguments = change switch
        {
            "synthetic-catalog" => SetupArgs("s", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0")], "asl-scenario-a1-synthetic@1.0.0"),
            "off-board" => SetupArgs("s", 0, [Unit("g1", "attacker-squad", "german", "bd01:D20:0")]),
            "unknown-definition" => SetupArgs("s", 0, [Unit("g1", "no-such-squad", "german", "bd01:D4:0")]),
            _ => SetupArgs("s", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0")]),
        };
        var principal = change == "no-permission" ? GamePlay.Principal("guest", Tenant) : Player;
        var result = await play.ProposeAsync(GameActions.Setup, arguments, principal);
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Null(store.Read(new GameScope(Tenant, "village")));
    }

    [Fact]
    public async Task TheSequenceOfPlayFollowsA3AndAlternatesSides()
    {
        var play = Play();
        await Commit(play, GameActions.Setup, SetupArgs("setup-1", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0")]));
        var seen = new List<(int, string, string)>();
        for (var step = 0; step < 17; step++)
        {
            Assert.Equal(PlayOutcome.Committed, (await Commit(play, GameActions.AdvancePhase,
                Args(new
                {
                    gameId = "village",
                    attemptId = $"a{step}",
                    expectedRevision = 2 + step
                }))).Outcome);
            var state = Current();
            seen.Add((state.Turn, state.Phase, state.PhasingSide));
        }

        Assert.Equal((1, "ccph", "german"), seen[6]);
        Assert.Equal((1, "rph", "russian"), seen[7]);
        Assert.Equal((1, "ccph", "russian"), seen[14]);
        Assert.Equal((2, "rph", "german"), seen[15]);
    }

    [Fact]
    public async Task SetupClosesWhenPlayStarts()
    {
        await GameInMph();
        var result = await Play().ProposeAsync(GameActions.Setup, SetupArgs("late", 4, [Unit("g9", "attacker-squad", "german", "bd01:C4:0")], start: false), Player);
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.Contains(result.Reasons, reason => reason.StartsWith("play.setup-closed", StringComparison.Ordinal));
    }

    [Fact]
    public async Task TheReviewedEntryCommitsOnlyOnADefinitiveConclusion()
    {
        var play = await GameInMph();
        var proposed = await play.ProposeAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4), Player);
        Assert.Equal(PlayOutcome.NeedsConfirmation, proposed.Outcome);
        var review = proposed.Plan!.Entry!;
        Assert.All(review.Facts, fact => Assert.True(fact.Value, fact.Key));
        Assert.Equal(ConclusionDisposition.Definitive, review.Conclusion.Disposition);
        Assert.Equal(2, review.Conclusion.Value!.Value.GetProperty("entryMf").GetInt32());

        var committed = await play.ConfirmAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Committed, committed.Outcome);
        var state = Current();
        Assert.Equal("bd01:E4:0", state.Location("g1")!.Location.ToString());
        Assert.Equal(2, state.Unit("g1")!.MfSpent);
        var move = store.Read(new GameScope(Tenant, "village"))!.Events[^1];
        Assert.Equal(("instance-moved", ScenarioA1Package.Identity.ToString()), (move.Type, move.RulePackage));
    }

    public static TheoryData<string, LosCategory, bool> BuildingTerrain => new()
    {
        { "Stone Building", LosCategory.Building, true },
        { "Wooden Building, 1 Level", LosCategory.Building, true },
        { "Stone Building, 2 Level", LosCategory.Building, true },
        { "Wooden Factory, 1.5 Level", LosCategory.Factory, false },
    };

    /// <summary>The reviewed case covers the ground level of any ordinary wooden or stone building, as VASL boards name them.</summary>
    [Theory]
    [MemberData(nameof(BuildingTerrain))]
    public async Task TheGroundLevelOfAnyOrdinaryBuildingIsCovered(string terrain, LosCategory category, bool covered)
    {
        e4 = new TerrainType { Code = 3, Name = terrain, Category = category };
        var play = await GameInMph();
        var proposed = await play.ProposeAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4), Player);
        Assert.Equal(covered, proposed.Plan!.Entry!.Facts["isAdjacentGroundLevelOrdinaryBuilding"]);
        Assert.Equal(covered ? PlayOutcome.NeedsConfirmation : PlayOutcome.Denied, proposed.Outcome);
    }

    [Fact]
    public async Task ASecondConfirmationOfTheSameAttemptIsAReplay()
    {
        var play = await GameInMph();
        await Commit(play, GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4));
        var again = await Commit(play, GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4));
        Assert.Equal(PlayOutcome.Replay, again.Outcome);
        Assert.Equal(5, store.Read(new GameScope(Tenant, "village"))!.Events.Count);
    }

    public static TheoryData<string, string> EntryRefusals => new()
    {
        { "not-mph", "isAttackerMovementPhase" },
        { "occupied", "isDestinationKnownEmpty" },
        { "open-ground", "isAdjacentGroundLevelOrdinaryBuilding" },
        { "not-adjacent", "isAdjacentGroundLevelOrdinaryBuilding" },
        { "concealed", "isKnownGoodOrderInfantrySquad" },
    };

    [Theory]
    [MemberData(nameof(EntryRefusals))]
    public async Task TheEntryIsRefusedWhenAReviewedFactIsFalse(string change, string fact)
    {
        GamePlay play;
        long revision;
        var location = "bd01:E4:0";
        var unit = "g1";
        switch (change)
        {
            case "not-mph":
                play = Play();
                await Commit(play, GameActions.Setup, SetupArgs("setup-1", 0, [Unit("g1", "attacker-squad", "german", "bd01:D4:0")]));
                revision = 2;
                break;
            case "occupied":
                play = await GameInMph(Unit("r1", "defender-squad", "russian", "bd01:E4:0"));
                revision = 5;
                break;
            case "concealed":
                play = await GameInMph(Unit("g2", "attacker-squad", "german", "bd01:D4:0", concealed: true));
                revision = 5;
                unit = "g2";
                break;
            default:
                play = await GameInMph();
                revision = 4;
                location = change == "open-ground" ? "bd01:D5:0" : "bd01:F4:0";
                break;
        }

        var result = await play.ProposeAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter", revision, unit, location), Player);
        Assert.Equal(PlayOutcome.Denied, result.Outcome);
        Assert.False(result.Plan!.Entry!.Facts[fact]);
        Assert.NotEqual(ConclusionDisposition.Definitive, result.Plan.Entry.Conclusion.Disposition);
        Assert.Equal(revision, store.Read(new GameScope(Tenant, "village"))!.Events.Count);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public async Task AFirstLineSquadHasFourMovementFactors(int spent, bool enough)
    {
        // Every definition in the published catalog is 1st Line, so no live squad is Inexperienced (A4.11, p. 48).
        await GameInMph();
        var state = Current();
        var squad = state.Unit("g1")! with
        {
            MfSpent = spent
        };
        var facts = Planner().EntryFacts(state with
        {
            Units = [squad]
        }, squad, BoardLocation.Parse("bd01:E4:0"));
        Assert.Equal(enough, facts["hasEnoughMovementFactors"]);
    }

    [Fact]
    public async Task AnEntryAfterTwoMovementFactorsIsDefinitive()
    {
        // Step 7 left this case Indeterminate; with the squad's class known, its fourth MF is known too.
        await GameInMph();
        var state = Current();
        var squad = state.Unit("g1")! with
        {
            MfSpent = 2
        };
        var review = await Planner().ReviewEntryAsync(new GameScope(Tenant, "village"), state with
        {
            Units = [squad]
        }, squad, BoardLocation.Parse("bd01:E4:0"));
        Assert.Equal(ConclusionDisposition.Definitive, review.Conclusion.Disposition);
    }

    [Fact]
    public async Task AFactTheStateCannotEstablishKeepsTheCaseIndeterminate()
    {
        var play = await GameInMph();
        var planner = Planner();
        var state = Current();
        // A squad whose definition is not in the planner's catalogs has no known class, so after exactly 2 MF neither
        // allotment can be ruled out (A4.11, p. 48; A19.31, p. 86).
        var unclassed = state.Unit("g1")! with
        {
            MfSpent = 2,
            Definition = new DefinitionReference(new CatalogIdentity("other-catalog", "1.0.0", "none"), "attacker-squad")
        };
        var facts = planner.EntryFacts(state with
        {
            Units = [unclassed]
        }, unclassed, BoardLocation.Parse("bd01:E4:0"));
        Assert.Null(facts["hasEnoughMovementFactors"]);

        var unverified = new GamePlanner(store, new InMemoryBoardCatalog([Board(BoardReadStatus.Ingested)]), Vocabulary, [Catalog]);
        var unknownTerrain = unverified.EntryFacts(state, state.Unit("g1")!, BoardLocation.Parse("bd01:E4:0"));
        Assert.Null(unknownTerrain["isAdjacentGroundLevelOrdinaryBuilding"]);
        await Task.CompletedTask;
        Assert.NotNull(play);
    }

    [Fact]
    public async Task AProposalGoesStaleWhenTheGameMovesOn()
    {
        var play = await GameInMph();
        var proposed = await play.ProposeAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4), Player);
        await Commit(play, GameActions.AdvancePhase, Args(new
        {
            gameId = "village",
            attemptId = "advance-x",
            expectedRevision = 4
        }));
        var confirmed = await play.ConfirmAsync(GameActions.EnterEmptyBuilding, EntryArgs("enter-1", 4), Player, proposed.Correlation);
        Assert.Equal(PlayOutcome.Stale, confirmed.Outcome);
        Assert.Equal("bd01:D4:0", Current().Location("g1")!.Location.ToString());
    }

    [Fact]
    public void AnInvalidAppendWritesNothing()
    {
        var scope = new GameScope(Tenant, "village");
        var bogus = new GameEvent(scope, "x-1", 1, DateTimeOffset.UnixEpoch, LiveGames.Source, "phase-changed", new PhaseChanged(1, "mph", "german"), null, [], null);
        var result = store.Append(scope, "x", 0, [bogus], Planner().Replay);
        Assert.Equal(AppendStatus.Invalid, result.Status);
        Assert.Null(store.Read(scope));
        Assert.Equal(AppendStatus.Invalid, store.Append(scope with
        {
            Game = "Not A Slug"
        }, "x", 0, [bogus], Planner().Replay).Status);
    }

    [Fact]
    public void TheActionsAreRegisteredWritesThatNeedConfirmation()
    {
        Assert.Equal(["asl.game.setup", "asl.game.advance-phase", "asl.game.enter-empty-building", "asl.game.enter-building", "asl.game.declare-overrun"],
            GameActions.All.Select(action => action.Id.Value));
        Assert.All(GameActions.All, action => Assert.Equal(Abstractions.Actions.ActionReversibility.Irreversible, action.Risk.Reversibility));
        var precondition = Assert.Single(GameActions.EnterEmptyBuilding.Preconditions);
        Assert.Contains(ScenarioA1Package.Identity.ToString(), precondition.Parameters.GetRawText(), StringComparison.Ordinal);
        Assert.Equal(JsonValueKind.Object, JsonNode.Parse(GameActions.Setup.InputSchema.GetRawText())!.GetValueKind());
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class RecordingAudit : IAuditSink
    {
        public List<RuntimeAuditEvent> Events { get; } = [];

        public ValueTask WriteAsync(RuntimeAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            lock (Events)
            {
                Events.Add(auditEvent);
            }

            return ValueTask.CompletedTask;
        }
    }
}
