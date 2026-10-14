using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Location chains for tests: every hex of bd01 has a ground level, E4 also a cellar and a first level, and Z9 is off the board.</summary>
internal sealed class FakeChains(string version = StateTests.Bd01Version) : ILocationChains
{
    public string? Version(BoardRef board) => board.Value == "bd01" ? version : null;

    public IReadOnlyList<int>? Levels(BoardRef board, HexName hex) =>
        board.Value != "bd01" || hex.ToString() == "Z9" ? null : hex.ToString() == "E4" ? [-1, 0, 1] : [0];

    public bool HasBridge(BoardRef board, HexName hex) => false;
}

/// <summary>The state model (State Model Design): replay, invariants, perspectives, staleness, and display projection.</summary>
public sealed class StateTests
{
    public const string Bd01Version = "8d77d26222b7bb21d8c1fdda6ba05b447f63c317";

    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static IReadOnlyList<GameEvent> Fixture()
    {
        var record = UnitGames.Read("a1-village.synthetic");
        Assert.NotNull(record);
        Assert.Empty(record.Diagnostics);
        Assert.True(record.Record!.Synthetic);
        return record.Record.Events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events, ILocationChains? chains = null) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], chains ?? new FakeChains());

    private static GameHistory Replayed()
    {
        var history = Project(Fixture());
        Assert.Empty(history.Diagnostics);
        return history;
    }

    /// <summary>The fixture's events through <paramref name="revision"/>, then <paramref name="next"/> as the following event.</summary>
    private static List<GameEvent> Then(int revision, EventPayload next, string type = "test", IReadOnlyList<string>? visibility = null)
    {
        var events = Fixture().Take(revision).ToList();
        events.Add(events[^1] with
        {
            EventId = "x",
            Revision = revision + 1,
            Type = type,
            Payload = next,
            Causes = [],
            Visibility = visibility
        });
        return events;
    }

    private static void AssertRefused(List<GameEvent> events, string code, ILocationChains? chains = null)
    {
        var history = Project(events, chains);
        Assert.True(history.HasErrors);
        Assert.Contains(history.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == UnitDiagnosticSeverity.Error);
        Assert.Null(history.Current);
        Assert.Equal(events.Count - 1, history.States.Count);
    }

    private static Dictionary<string, ConditionState> Clear() => new(StringComparer.Ordinal)
    {
        [Conditions.Broken] = ConditionState.False,
        [Conditions.Berserk] = ConditionState.False,
        [Conditions.Captured] = ConditionState.False,
        [Conditions.Melee] = ConditionState.False,
    };

    private static NewInstance Squad(string id, string side, string at, string definition = "attacker-squad") =>
        new(id, "asl:squad", definition, side, new MapPosition(BoardLocation.Parse(at)), null, Clear());

    [Fact]
    public void TheFixtureReplaysIntoOneStatePerRevision()
    {
        var history = Replayed();
        Assert.Equal(23, history.States.Count);
        var state = history.Current!;
        Assert.Equal(23, state.Revision);
        Assert.True(state.Synthetic);
        Assert.Equal("asl-scenario-a1", state.Catalog.Catalog);
        Assert.Equal(["german", "russian", "adjudicator"], state.Perspectives.Select(perspective => perspective.Name));
        Assert.Equal(3, state.Side("russian")!.Elr);
        Assert.Equal(("mph", 2), (state.Phase, state.Turn));
        Assert.Equal(1, state.Unit("gh1")!.MfSpent);
        Assert.Equal(0, history.At(19)!.Unit("gh1")!.MfSpent);
        Assert.Equal($"asl-scenario-a1@1.15.0+sha256:{Catalog.Value.Identity.Hash}#attacker-squad", state.Unit("g1")!.Definition!.ToString());
    }

    [Fact]
    public void LineageLinksProducedInstancesToWhatTheyConsumed()
    {
        var state = Replayed().Current!;
        Assert.Equal(InstanceStatus.Consumed, state.Unit("g2")!.Status);
        var produced = state.Unit("g2-hs")!;
        Assert.Equal(["g2"], produced.From);
        Assert.Equal("german", produced.Side);
        Assert.Equal("bd01:D4:0", state.Location("g2-hs")!.Location.ToString());
    }

    [Fact]
    public void EveryRevisionCanBeReadBack()
    {
        var history = Replayed();
        Assert.Equal(ConditionState.True, GameState.Condition(history.At(9)!.Unit("r1")!, Conditions.Concealed));
        Assert.Equal(ConditionState.False, GameState.Condition(history.At(10)!.Unit("r1")!, Conditions.Concealed));
        Assert.Equal(InstanceStatus.Active, history.At(11)!.Unit("g2")!.Status);
        Assert.Null(history.At(0));
        Assert.Null(history.At(24));
    }

    [Fact]
    public void PositionsFollowContainmentAndHolding()
    {
        var history = Replayed();
        Assert.Equal("bd01:D4:0", history.At(5)!.Location("g-lmg")!.Location.ToString());
        Assert.Equal("bd01:E4:0", history.Current!.Location("g-lmg")!.Location.ToString());
        Assert.Equal("bd01:E5:0", history.Current!.Location("r2")!.Location.ToString());
        Assert.Equal(["g1", "g2", "gh1", "g-lmg"], history.At(5)!.At(BoardLocation.Parse("bd01:D4:0")).Select(item => item.Id));
    }

    [Fact]
    public void GoodOrderIsDerivedNotStored()
    {
        var history = Replayed();
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.Equal(ConditionState.True, GameState.GoodOrder(history.Current!.Unit("g1")!, vocabulary));
        Assert.Equal(ConditionState.False, GameState.GoodOrder(history.At(14)!.Unit("r1")!, vocabulary));
        Assert.Equal(ConditionState.False, GameState.GoodOrder(history.Current!.Unit("r1")!, vocabulary));
        var unknown = history.Current!.Unit("g1")! with
        {
            Conditions = new Dictionary<string, ConditionState>()
        };
        Assert.Equal(ConditionState.Unknown, GameState.GoodOrder(unknown, vocabulary));
        Assert.Equal(ConditionState.Unknown, GameState.Condition(unknown, Conditions.Broken));
    }

    [Fact]
    public void CaptureRecordsTheCustodian()
    {
        var prisoner = Replayed().Current!.Unit("r1")!;
        Assert.Equal("g1", prisoner.Custodian);
        Assert.Equal(ConditionState.True, GameState.Condition(prisoner, Conditions.Captured));
    }

    [Fact]
    public void ASideDoesNotSeeHiddenUnitsAndSeesConcealedOnesAsSealedPresence()
    {
        var history = Replayed();
        var german = GameView.Of(history, 8, Perspective.Side("german"));
        Assert.DoesNotContain(german.Units, unit => unit.Side == "russian");
        var sealedPresence = Assert.Single(german.Sealed);
        Assert.Equal(("sealed-1", "russian", "bd01:E4:0"), (sealedPresence.PlacementId, sealedPresence.Side, sealedPresence.Location.ToString()));
        Assert.Contains(german.Entities, entity => entity.Id == "f1");
        Assert.DoesNotContain(german.Events, item => item.EventId is "e06" or "e08");

        // Filtering at the source: the Russian ids appear nowhere in what the German side receives.
        var serialized = JsonSerializer.Serialize(german);
        Assert.DoesNotContain("\"r1\"", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("\"r2\"", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("defender-leader", serialized, StringComparison.Ordinal);

        var russian = GameView.Of(history, 8, Perspective.Side("russian"));
        Assert.Equal(["r1", "r2"], russian.Units.Where(unit => unit.Side == "russian").Select(unit => unit.Id));

        // Pass 23 (A2.9; ruling R23.3): before play starts the German stack shows its top counter, and its other two are counted, not identified.
        Assert.Equal(["g1"], russian.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id));
        Assert.Equal(2, russian.Sealed.Count(presence => presence is { Side: "german", Uninspected: true } && presence.Location.ToString() == "bd01:D4:0"));
        Assert.Equal(russian.Sealed.Count, russian.Sealed.Count(presence => presence.Uninspected));
        var adjudicator = GameView.Of(history, 8, Perspective.Adjudicator);
        Assert.Equal(5, adjudicator.Units.Count);
        Assert.Equal(8, adjudicator.Events.Count);
    }

    [Fact]
    public void TheRussianSideDoesNotSeeHiddenGermansAndSeesConcealedOnesAsSealedPresence()
    {
        var history = Replayed();
        var russian = GameView.Of(history, 23, Perspective.Side("russian"));
        Assert.DoesNotContain(russian.Units, unit => unit.Id is "g3" or "gh2");
        var sealedPresence = Assert.Single(russian.Sealed);
        Assert.Equal(("sealed-1", "german", "bd01:C5:0"), (sealedPresence.PlacementId, sealedPresence.Side, sealedPresence.Location.ToString()));
        Assert.DoesNotContain(russian.Events, item => item.EventId is "e22" or "e23");
        Assert.Contains(russian.Units, unit => unit.Id == "g1");

        // Filtering at the source: the Russian side receives no German ids it may not know.
        var serialized = JsonSerializer.Serialize(russian);
        Assert.DoesNotContain("\"g3\"", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("\"gh2\"", serialized, StringComparison.Ordinal);

        var german = GameView.Of(history, 23, Perspective.Side("german"));
        Assert.Contains(german.Units, unit => unit.Id == "g3" && GameState.Condition(unit, Conditions.Concealed) == ConditionState.True);
        Assert.Contains(german.Units, unit => unit.Id == "gh2" && GameState.Condition(unit, Conditions.Hidden) == ConditionState.True);
        Assert.Empty(german.Sealed);
        Assert.Contains(german.Events, item => item.EventId == "e23");
    }

    [Fact]
    public void TheRussianDisplayReceivesOnlyItsView()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var russian = GameDocuments.For(GameView.Of(Replayed(), 23, Perspective.Side("russian")), vocabulary, [Catalog.Value]);
        var placeholder = Assert.Single(russian, document => document.Concealed);
        Assert.Equal(("sealed-1", "bd01:C5:0", "german"), (placeholder.Id, placeholder.Location, placeholder.Side));
        Assert.DoesNotContain(russian, document => document.Id is "g3" or "gh2");
        Assert.DoesNotContain(russian, document => document.Location == "bd01:C4:0");

        var german = GameDocuments.For(GameView.Of(Replayed(), 23, Perspective.Side("german")), vocabulary, [Catalog.Value]);
        Assert.Equal(["asl:concealed"], Assert.Single(german, document => document.Id == "g3").States);
        var hidden = Assert.Single(german, document => document.Id == "gh2");
        Assert.Equal("bd01:C4:0", hidden.Location);
        Assert.Equal(["asl:hidden"], hidden.States);
    }

    [Fact]
    public void ARevealShowsTheUnitToTheOtherSide()
    {
        var german = GameView.Of(Replayed(), 10, Perspective.Side("german"));
        Assert.Contains(german.Units, unit => unit.Id == "r1");
        Assert.Empty(german.Sealed);
        Assert.DoesNotContain(german.Units, unit => unit.Id == "r2");
    }

    [Fact]
    public void AnUnknownPerspectiveIsRefused()
    {
        var history = Replayed();
        Assert.Throws<ArgumentException>(() => GameView.Of(history, 8, Perspective.Side("american")));
        Assert.Throws<ArgumentException>(() => GameView.Of(history.At(8)!, Perspective.Side("german"), history.EventsFor(Perspective.Adjudicator, 8)));
    }

    [Fact]
    public void TheDisplayReceivesOnlyTheView()
    {
        var history = Replayed();
        var vocabulary = UnitsTestData.Asl.Value;
        var german = GameDocuments.For(GameView.Of(history, 8, Perspective.Side("german")), vocabulary, [Catalog.Value]);
        var placeholder = Assert.Single(german, document => document.Concealed);
        Assert.Equal(("sealed-1", "bd01:E4:0", "russian"), (placeholder.Id, placeholder.Location, placeholder.Side));
        Assert.Empty(placeholder.Faces);
        Assert.DoesNotContain(german, document => document.Id is "r1" or "r2");
        Assert.Equal(["g1", "g2", "gh1"], german.Where(document => document.Location == "bd01:D4:0").OrderBy(document => document.StackOrder).Select(document => document.Id));

        var russian = GameDocuments.For(GameView.Of(history, 18, Perspective.Side("russian")), vocabulary, [Catalog.Value]);
        var prisoner = Assert.Single(russian, document => document.Id == "r1");
        Assert.Equal(["asl:broken"], prisoner.States);
        Assert.Equal("broken", prisoner.ShownFace(vocabulary));
        Assert.Contains(russian, document => document.Id == "f1" && document.Kind == "asl:foxhole");
        var set = GameDocuments.PlacementSet(GameView.Of(history, 18, Perspective.Adjudicator), "game-test", "test", vocabulary, [Catalog.Value]);
        Assert.True(set.Synthetic);
        Assert.Equal(["bd01"], set.Boards.Select(board => board.Value));
    }

    [Fact]
    public void PossessedEquipmentIsDrawnWithItsHolderAndDroppedEquipmentAlone()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var held = GameDocuments.For(GameView.Of(Replayed(), 5, Perspective.Side("german")), vocabulary, [Catalog.Value]);
        var holder = Assert.Single(held, document => document.Id == "g1");
        Assert.Equal(("g-lmg", "asl:mg"), (Assert.Single(holder.Attached).Id, holder.Attached[0].Kind));
        Assert.DoesNotContain(held, document => document.Id == "g-lmg");

        var history = Project(Then(16, new InstanceEliminated("g1")));
        var dropped = GameDocuments.For(GameView.Of(history, 17, Perspective.Side("german")), vocabulary, [Catalog.Value]);
        Assert.DoesNotContain(dropped, document => document.Id == "g1");
        Assert.Equal("bd01:E4:0", Assert.Single(dropped, document => document.Id == "g-lmg").Location);
    }

    [Fact]
    public void AConclusionGoesStaleAfterALaterEvent()
    {
        var history = Replayed();
        var stamp = history.At(12)!.Stamp;
        Assert.Equal(StampStatus.Current, history.At(12)!.Check(stamp));
        Assert.Equal(StampStatus.Stale, history.At(13)!.Check(stamp));
        Assert.Equal(StampStatus.Stale, history.At(12)!.Check(stamp with
        {
            MapVersion = "other"
        }));
        Assert.Equal(StampStatus.OtherGame, history.At(12)!.Check(stamp with
        {
            Scope = stamp.Scope with
            {
                Game = "other"
            }
        }));
    }

    [Fact]
    public void WithoutLocationChainsPositionsAreNotCheckedAndThatIsSaid()
    {
        var history = GameProjector.Project(Fixture(), UnitsTestData.Asl.Value, [Catalog.Value]);
        Assert.False(history.HasErrors);
        Assert.Equal("UNIT-STATE-020", Assert.Single(history.Diagnostics).Code);
    }

    [Fact]
    public void EnvelopeRulesAreEnforced()
    {
        var events = Fixture().ToList();
        AssertRefused([.. events.Take(3), events[3] with { Revision = 5 }], "UNIT-STATE-003");
        AssertRefused([.. events.Take(3), events[3] with { EventId = "e02" }], "UNIT-STATE-003");
        AssertRefused([.. events.Take(3), events[3] with { Scope = events[3].Scope with { Game = "other" } }], "UNIT-STATE-002");
        AssertRefused([.. events.Take(3), events[3] with { Causes = ["e99"] }], "UNIT-STATE-016");
        AssertRefused([.. events.Take(3), events[3] with { Visibility = ["american"] }], "UNIT-STATE-005");
        AssertRefused([events[1] with { Revision = 1 }], "UNIT-STATE-004");
        AssertRefused([.. events.Take(3), events[0] with { EventId = "again", Revision = 4 }], "UNIT-STATE-004");
        var started = (GameStarted)events[0].Payload;
        AssertRefused([events[0] with { Payload = started with { Synthetic = false } }], "UNIT-STATE-017");
        AssertRefused([events[0] with { Payload = started with { Catalog = "asl-no-such-catalog@1.0.0" } }], "UNIT-STATE-008");
    }

    [Fact]
    public void BoardVersionsMustMatchTheChains() =>
        AssertRefused([Fixture()[0]], "UNIT-STATE-010", new FakeChains("another-version"));

    public static TheoryData<string, string> Refusals => new()
    {
        { "consumed-acts", "UNIT-STATE-007" },
        { "unknown-instance", "UNIT-STATE-006" },
        { "duplicate-instance", "UNIT-STATE-006" },
        { "unknown-condition", "UNIT-STATE-009" },
        { "exclusive-conditions", "UNIT-STATE-009" },
        { "hex-off-board", "UNIT-STATE-010" },
        { "level-not-in-chain", "UNIT-STATE-010" },
        { "board-not-in-map", "UNIT-STATE-010" },
        { "facing-without-facing", "UNIT-STATE-010" },
        { "container-not-fortification", "UNIT-STATE-011" },
        { "possessed-by-entity", "UNIT-STATE-012" },
        { "manned-from-elsewhere", "UNIT-STATE-012" },
        { "unit-with-holding", "UNIT-STATE-012" },
        { "deploy-counts", "UNIT-STATE-013" },
        { "lineage-side", "UNIT-STATE-013" },
        { "lineage-kinds", "UNIT-STATE-013" },
        { "custodian-same-side", "UNIT-STATE-014" },
        { "custodian-elsewhere", "UNIT-STATE-014" },
        { "phase", "UNIT-STATE-015" },
        { "turn-backwards", "UNIT-STATE-015" },
        { "unit-without-definition", "UNIT-STATE-008" },
        { "definition-kind", "UNIT-STATE-008" },
        { "unknown-side", "UNIT-STATE-005" },
    };

    [Theory]
    [MemberData(nameof(Refusals))]
    public void AnEventBreakingAnInvariantIsRefused(string change, string code)
    {
        var at = BoardLocation.Parse("bd01:D4:0");
        List<GameEvent> events = change switch
        {
            "consumed-acts" => Then(12, new InstanceMoved("g2", new MapPosition(at))),
            "unknown-instance" => Then(12, new InstanceMoved("nobody", new MapPosition(at))),
            "duplicate-instance" => Then(4, new InstanceCreated(Squad("g1", "german", "bd01:D4:0"))),
            "unknown-condition" => Then(4, new ConditionsChanged("g1", new Dictionary<string, ConditionState> { ["asl:sleepy"] = ConditionState.True })),
            "exclusive-conditions" => Then(4, new ConditionsChanged("g1", new Dictionary<string, ConditionState>
            {
                [Conditions.Broken] = ConditionState.True,
                [Conditions.Berserk] = ConditionState.True,
            })),
            "hex-off-board" => Then(4, new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd01:Z9:0")))),
            "level-not-in-chain" => Then(4, new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd01:D4:1")))),
            "board-not-in-map" => Then(4, new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd02:D4:0")))),
            "facing-without-facing" => Then(4, new InstanceMoved("g1", new MapPosition(at, Facing: Documents.UnitFacing.East))),
            "container-not-fortification" => Then(4, new InstanceMoved("gh1", new ContainedPosition("g1", ContainmentRole.InFortification))),
            "possessed-by-entity" => Then(7, new EquipmentTransferred("g-lmg", new Holding("f1", HoldingRole.Possessed), null)),
            "manned-from-elsewhere" => Then(7, new EquipmentTransferred("g-lmg", new Holding("g1", HoldingRole.Manned), new MapPosition(BoardLocation.Parse("bd01:E5:0")))),
            "unit-with-holding" => Then(4, new InstanceCreated(Squad("g9", "german", "bd01:D4:0") with { Holding = new Holding("g1", HoldingRole.Possessed) })),
            "deploy-counts" => Then(4, new LineageRecorded(LineageAction.Deployed, ["g1"], [Squad("g1a", "german", "bd01:D4:0") with { Kind = "asl:half-squad", Definition = "attacker-half-squad" }])),
            "lineage-side" => Then(4, new LineageRecorded(LineageAction.Reduced, ["g1"], [Squad("g1a", "russian", "bd01:D4:0") with { Kind = "asl:half-squad", Definition = "attacker-half-squad" }])),
            "lineage-kinds" => Then(4, new LineageRecorded(LineageAction.Reduced, ["gh1"], [Squad("g1a", "german", "bd01:D4:0")])),
            "custodian-same-side" => Then(10, new InstanceCaptured("g2", "g1")),
            "custodian-elsewhere" => Then(10, new InstanceCaptured("r1", "g1")),
            "phase" => Then(4, new PhaseChanged(1, "lunch", "german")),
            "turn-backwards" => Then(4, new PhaseChanged(0, "mph", "german")),
            "unit-without-definition" => Then(4, new InstanceCreated(Squad("g9", "german", "bd01:D4:0") with { Definition = null })),
            "definition-kind" => Then(4, new InstanceCreated(Squad("g9", "german", "bd01:D4:0", definition: "defender-leader"))),
            "unknown-side" => Then(4, new InstanceCreated(Squad("g9", "french", "bd01:D4:0"))),
            _ => throw new ArgumentOutOfRangeException(nameof(change)),
        };

        AssertRefused(events, code);
    }

    /// <summary>
    /// Pass 32.e commit 10b: a vehicle step out of the MPh is refused on its phase before the vehicle is read, so an unknown vehicle adds no
    /// UNIT-STATE-006.
    /// </summary>
    [Fact]
    public void AVehicleStepOutOfTheMphIsRefusedBeforeTheVehicleIsRead()
    {
        var states = Replayed().States;
        var revision = Enumerable.Range(0, states.Count).First(index => states[index].Phase is { } phase && phase != "mph") + 1;
        var events = Then(revision, new VehicleStepped("nobody", VehicleStepped.Start, BoardLocation.Parse("bd01:D4:0"), null, 2, 1));
        AssertRefused(events, "UNIT-STATE-034");
        Assert.DoesNotContain(Project(events).Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-006");
    }

    [Fact]
    public void EliminationLeavesHeldEquipmentWhereItsHolderWas()
    {
        var events = Then(16, new InstanceEliminated("g1"));
        var history = Project(events);
        Assert.False(history.HasErrors, string.Join("; ", history.Diagnostics));
        var equipment = history.Current!.Equipment.Single(item => item.Id == "g-lmg");
        Assert.Null(equipment.Holding);
        Assert.Equal("bd01:E4:0", history.Current.Location("g-lmg")!.Location.ToString());
        AssertRefused([.. events, events[^1] with { EventId = "y", Revision = 18, Payload = new InstanceMoved("g1", new MapPosition(BoardLocation.Parse("bd01:D4:0"))) }],
            "UNIT-STATE-007");
    }

    [Fact]
    public void ForeignNationalityDefinitionsWarn()
    {
        var history = Project(Then(4, new InstanceCreated(Squad("r9", "russian", "bd01:D4:0"))));
        Assert.False(history.HasErrors);
        Assert.Contains(history.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-008" && diagnostic.Severity == UnitDiagnosticSeverity.Warning);
    }

    // Pass 55 (D3.4 to D3.45, p. 200; rulings R55.15 to R55.18): a vehicle's Armor Leader and its Inexperienced Crew.

    private static NewInstance Tank(string id, ArmorLeader? leader, bool inexperiencedCrew = false, string kind = "asl:vehicle", string definition = "attacker-tank") =>
        new(id, kind, definition, "german", new MapPosition(BoardLocation.Parse("bd01:G6:0"), Facing: kind == "asl:vehicle" ? Documents.UnitFacing.East : null), null,
            inexperiencedCrew ? new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.InexperiencedCrew] = ConditionState.True } : Clear())
        {
            ArmorLeader = leader,
        };

    private static List<GameEvent> After(List<GameEvent> events, EventPayload next)
    {
        events.Add(events[^1] with
        {
            EventId = "x" + events.Count,
            Revision = events.Count + 1,
            Payload = next,
        });
        return events;
    }

    [Fact]
    public void AVehicleSetsUpWithAnArmorLeaderOfAListedGrade()
    {
        var history = Project(Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(9, -1, Revealed: true)))));
        Assert.False(history.HasErrors);

        // D3.41: the record never says he is revealed; a record that uses him does.
        Assert.Equal(new ArmorLeader(9, -1), history.Current!.Unit("t1")!.ArmorLeader);
        Assert.Null(history.Current.Unit("g1")!.ArmorLeader);

        // D3.4, H1.43: the four grades; D3.41: a vehicle; D3.45: not beside an Inexperienced Crew, which stands alone.
        AssertRefused(Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(10, -1)))), "UNIT-STATE-047");
        AssertRefused(Then(23, new InstanceCreated(Tank("s9", new ArmorLeader(9, -1), kind: "asl:squad", definition: "attacker-squad"))), "UNIT-STATE-047");
        AssertRefused(Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(9, -1), inexperiencedCrew: true))), "UNIT-STATE-047");
        var green = Project(Then(23, new InstanceCreated(Tank("t1", null, inexperiencedCrew: true))));
        Assert.False(green.HasErrors);
        Assert.Equal(ConditionState.True, GameState.Condition(green.Current!.Unit("t1")!, Conditions.InexperiencedCrew));
    }

    [Fact]
    public void AnArmorLeaderIsWrittenAndReadWithHisVehicle()
    {
        var events = Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(10, -2))), "instance-created");
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("armor leader", true, events));
        Assert.Contains("\"armorLeader\"", text, StringComparison.Ordinal);
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.Empty(read.Diagnostics);
        Assert.Equal(new ArmorLeader(10, -2), Assert.IsType<InstanceCreated>(read.Record!.Events[^1].Payload).Instance.ArmorLeader);
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record));

        // A record made before the pass names none, and is written as it was read.
        var plain = GameEventWriter.Write(events[0].Scope, new GameRecord("plain", true, Then(23, new InstanceCreated(Tank("t1", null)), "instance-created")));
        Assert.DoesNotContain("armorLeader", plain, StringComparison.Ordinal);
        Assert.DoesNotContain("armorLeader", GameEventWriter.Write(events[0].Scope, new GameRecord("fixture", true, Fixture())), StringComparison.Ordinal);
    }

    [Fact]
    public void TheEnemysViewDoesNotShowAnArmorLeaderNoRecordHasUsed()
    {
        // D3.41: "his presence is noted on a side record. The Armor Leader counter remains out of play until the owner needs to use his
        // morale/leadership factor".
        var history = Project(Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(9, -2)))));
        Assert.False(history.HasErrors);
        var russian = GameView.Of(history, 24, Perspective.Side("russian"));
        Assert.Null(Assert.Single(russian.Units, unit => unit.Id == "t1").ArmorLeader);
        Assert.Null(Assert.IsType<InstanceCreated>(Assert.Single(russian.Events, item => item.Payload is InstanceCreated { Instance.Id: "t1" }).Payload).Instance.ArmorLeader);
        foreach (var perspective in new[] { Perspective.Side("german"), Perspective.Adjudicator })
        {
            var view = GameView.Of(history, 24, perspective);
            Assert.Equal(new ArmorLeader(9, -2), Assert.Single(view.Units, unit => unit.Id == "t1").ArmorLeader);
            Assert.NotNull(Assert.IsType<InstanceCreated>(Assert.Single(view.Events, item => item.Payload is InstanceCreated { Instance.Id: "t1" }).Payload).Instance.ArmorLeader);
        }

        // The state and the record themselves are untouched by a view.
        Assert.NotNull(Assert.IsType<InstanceCreated>(history.Events[^1].Payload).Instance.ArmorLeader);
    }

    [Theory]
    [InlineData(Conditions.Recalled)]
    [InlineData(Conditions.Abandoned)]
    [InlineData("eliminated")]
    [InlineData("wrecked")]
    public void AnArmorLeaderIsLostWithARecallAWreckOrACrewThatLeaves(string loss)
    {
        // D5.341: "Recall eliminates any Armor Leader present in that AFV"; D3.43: he "ceases to exist once the crew takes counter form".
        var events = Then(23, new InstanceCreated(Tank("t1", new ArmorLeader(9, -1))));
        var history = Project(After(events, loss switch
        {
            "eliminated" => new InstanceEliminated("t1"),
            "wrecked" => new VehicleWrecked("t1", false),
            _ => new ConditionsChanged("t1", new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [loss] = ConditionState.True }),
        }));
        Assert.False(history.HasErrors, string.Join("; ", history.Diagnostics));
        Assert.NotNull(history.At(24)!.Unit("t1")!.ArmorLeader);
        Assert.Null(history.Current!.Unit("t1")!.ArmorLeader);

        // D3.45: "the quasi-Armor Leader can never be eliminated or removed from its crew": the condition stays.
        var green = Then(23, new InstanceCreated(Tank("t1", null, inexperiencedCrew: true)));
        var kept = Project(After(green, new ConditionsChanged("t1", new Dictionary<string, ConditionState>(StringComparer.Ordinal) { [Conditions.Recalled] = ConditionState.True })));
        Assert.Equal(ConditionState.True, GameState.Condition(kept.Current!.Unit("t1")!, Conditions.InexperiencedCrew));
    }

    [Theory]
    [InlineData("{")]
    [InlineData("""{ "schemaVersion": 1, "tenant": "not-a-guid", "game": "g", "label": "x", "events": [] }""")]
    [InlineData("""{ "schemaVersion": 1, "tenant": "3f6a9c1e-0000-4000-8000-00000000a101", "game": "g", "label": "x", "events": [ { "eventId": "e1", "revision": 1, "time": "2026-09-26T09:00:00Z", "source": "t", "type": "teleported", "payload": {} } ] }""")]
    [InlineData("""{ "schemaVersion": 1, "tenant": "3f6a9c1e-0000-4000-8000-00000000a101", "game": "g", "label": "x", "events": [ { "eventId": "e1", "revision": 1, "time": "2026-09-26T09:00:00Z", "source": "t", "type": "instance-moved", "payload": { "id": "a", "position": { "at": "E4" } } } ] }""")]
    public void AMalformedRecordIsRefused(string json)
    {
        var result = GameEventReader.Read(Encoding.UTF8.GetBytes(json));
        Assert.Null(result.Record);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-001");
    }
}
