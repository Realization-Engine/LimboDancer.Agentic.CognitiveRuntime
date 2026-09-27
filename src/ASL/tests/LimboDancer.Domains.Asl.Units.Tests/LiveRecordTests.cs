using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The live records of unit steps 19 to 23 and their replay checks: Rally (UNIT-STATE-027), Repair (UNIT-STATE-028),
/// hex-by-hex movement (UNIT-STATE-029), and Residual FP (UNIT-STATE-026). The synthetic fixture is in the German RPh
/// through revision 8 and the German MPh at revision 9; the Rally and fire records are checked by stub verifiers.
/// </summary>
public sealed class LiveRecordTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly JsonElement Facts = JsonSerializer.SerializeToElement(new { phase = "MPh" });
    private static readonly BoardLocation D5 = BoardLocation.Parse("bd01:D5:0");
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] G2 = ["g2"];

    private sealed class RallyVerifier(string? reason) : IRallyRecordVerifier
    {
        public string? Verify(GameState state, RallyAttempted rally, IReadOnlyDictionary<string, DiceRolled> rolls) => reason;
    }

    private sealed class FireVerifier : IFireRecordVerifier
    {
        public string? Verify(GameState state, FireResolved fire, IReadOnlyDictionary<string, DiceRolled> rolls) => null;
    }

    private static List<GameEvent> With(int take, params (string Id, string Type, EventPayload Payload)[] added)
    {
        List<GameEvent> events = [.. UnitGames.Read("a1-village.synthetic")!.Record!.Events.Take(take)];
        foreach (var (id, type, payload) in added)
        {
            events.Add(events[^1] with
            {
                EventId = id,
                Revision = events[^1].Revision + 1,
                Type = type,
                Payload = payload,
                Causes = [],
                Visibility = null
            });
        }

        return events;
    }

    private static GameHistory Project(IReadOnlyList<GameEvent> events, IRallyRecordVerifier? rally = null) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains(), fire: new FireVerifier(), rally: rally);

    private static (string, string, EventPayload) Roll(string id, params int[] values) =>
        ("r-" + id, "dice-rolled", new DiceRolled(id, "test", values.Length, 6, values, DiceRolled.SystemSource, "player"));

    private static (string, string, EventPayload) Rally(string id, string unit, string roll) =>
        (id, "rally-attempted", new RallyAttempted(unit, null, new Dictionary<string, string> { ["rally"] = roll }, Facts, Facts));

    private static (string, string, EventPayload) Malfunctioned(string id, string holder) =>
        ("c-" + id, "instance-created", new InstanceCreated(new NewInstance(id, "asl:mg", "attacker-lmg", "german", null,
            new Holding(holder, HoldingRole.Possessed), new Dictionary<string, ConditionState> { [Conditions.Malfunctioned] = ConditionState.True })));

    private static (string, string, EventPayload) Repair(string id, string unit, string equipment, string roll, int repairNumber, string result) =>
        (id, "repair-attempted", new RepairAttempted(unit, equipment, roll, repairNumber, result));

    private static (string, string, EventPayload) Step(string id, string[] movers, string to, int step, int halfMf = 2) =>
        (id, "movement-step", new MovementStepped(movers, BoardLocation.Parse(to), halfMf, false, step));

    private static (string, string, EventPayload) Fire(string id, string roll, int step, int residual) =>
        (id, "fire-resolved", new FireResolved(["r1"], null, "bd01:E4:0", "bd01:D5:0", new Dictionary<string, string> { ["attack"] = roll }, Facts,
            JsonSerializer.SerializeToElement(new
            {
                arithmetic = new
                {
                    residualFp = residual
                }
            }))
        {
            MovementStep = step
        });

    private static void Refused(GameHistory history, string code) =>
        Assert.Contains(history.Diagnostics, item => item.Code == code);

    [Fact]
    public void ARallyAttemptIsKeptForThePlayerTurnAndMadeOnce()
    {
        var once = Project(With(8, Roll("x-roll-1", 2, 3), Rally("a1", "g1", "x-roll-1")), new RallyVerifier(null));
        Assert.False(once.HasErrors, string.Join(" ", once.Diagnostics));
        Assert.Equal(G1, once.Current!.RallyAttemptsThisPlayerTurn);
        Assert.Contains("german", once.Current.FirstMmcRallyTaken);

        Refused(Project(With(8, Roll("x-roll-1", 2, 3), Rally("a1", "g1", "x-roll-1"), Roll("y-roll-1", 4, 4), Rally("a2", "g1", "y-roll-1")),
            new RallyVerifier(null)), "UNIT-STATE-027");
        Refused(Project(With(9, Roll("x-roll-1", 2, 3), Rally("a1", "g1", "x-roll-1")), new RallyVerifier(null)), "UNIT-STATE-027");
        Refused(Project(With(8, Roll("x-roll-1", 2, 3), Rally("a1", "g1", "x-roll-1")), new RallyVerifier("the resolution differs")), "UNIT-STATE-027");
        Refused(Project(With(8, Roll("x-roll-1", 2, 3), Rally("a1", "g1", "x-roll-1"))), "UNIT-STATE-023");
        Refused(Project(With(8, Rally("a1", "g1", "x-roll-1")), new RallyVerifier(null)), "UNIT-STATE-023");
    }

    [Fact]
    public void ARepairRecordMustAgreeWithTheRepairNumberAndTheDr()
    {
        var repaired = Project(With(8, Malfunctioned("mg9", "g2"), Roll("x-roll-1", 2), Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Repaired)));
        Assert.False(repaired.HasErrors, string.Join(" ", repaired.Diagnostics));
        Assert.Equal(G2, repaired.Current!.RepairsThisPhase);

        Assert.False(Project(With(8, Malfunctioned("mg9", "g2"), Roll("x-roll-1", 6),
            Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Eliminated))).HasErrors);
        Refused(Project(With(8, Malfunctioned("mg9", "g2"), Roll("x-roll-1", 3),
            Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Repaired))), "UNIT-STATE-028");
        Refused(Project(With(8, Malfunctioned("mg9", "g2"), Roll("x-roll-1", 2),
            Repair("p1", "g2", "mg9", "x-roll-1", 3, RepairAttempted.Repaired))), "UNIT-STATE-028");
        Refused(Project(With(8, Malfunctioned("mg9", "g1"), Roll("x-roll-1", 2),
            Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Repaired))), "UNIT-STATE-028");

        // A3.1: a unit that attempted to rally does not also repair.
        Refused(Project(With(8, Malfunctioned("mg9", "g2"), Roll("y-roll-1", 2, 3), Rally("a1", "g2", "y-roll-1"), Roll("x-roll-1", 2),
            Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Repaired)), new RallyVerifier(null)), "UNIT-STATE-028");
    }

    [Fact]
    public void AStackMovesOneStepPerDefenderWindow()
    {
        var stepped = Project(With(9, Step("m1", G1, "bd01:D5:0", 1)));
        Assert.False(stepped.HasErrors, string.Join(" ", stepped.Diagnostics));
        Assert.Equal(new MovementState(G1, D5, 2, 1, false, true), stepped.Current!.Movement! with
        {
            Movers = G1
        });
        Assert.Equal(D5, stepped.Current.Location("g1")!.Location);
        Assert.Equal(1, stepped.Current.Unit("g1")!.MfSpent);

        // The stack waits for the DEFENDER's window; another stack waits for this one to end; steps are numbered in order.
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Step("m2", G1, "bd01:D4:0", 2))), "UNIT-STATE-029");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("w1", "movement-window-closed", new MovementWindowClosed(1)),
            Step("m2", G2, "bd01:C5:0", 1))), "UNIT-STATE-029");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 2))), "UNIT-STATE-029");
        Refused(Project(With(8, Step("m1", G1, "bd01:D5:0", 1))), "UNIT-STATE-029");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("w1", "movement-window-closed", new MovementWindowClosed(2)))), "UNIT-STATE-029");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("e1", "movement-ended", new MovementEnded(G1)))), "UNIT-STATE-029");

        var ended = Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("w1", "movement-window-closed", new MovementWindowClosed(1)),
            ("e1", "movement-ended", new MovementEnded(G1))));
        Assert.False(ended.HasErrors, string.Join(" ", ended.Diagnostics));
        Assert.Null(ended.Current!.Movement);
        Assert.True(ended.Current.Unit("g1")!.MovementEnded);
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("w1", "movement-window-closed", new MovementWindowClosed(1)),
            ("e1", "movement-ended", new MovementEnded(G1)), Step("m2", G1, "bd01:D4:0", 1))), "UNIT-STATE-029");
    }

    [Fact]
    public void AMovingStackSplitsAndAMemberThatBreaksLeavesIt()
    {
        string[] stack = ["g1", "g2"];
        (string, string, EventPayload) Broken(string id) =>
            ("b-" + id, "conditions-changed", new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.True }));
        (string, string, EventPayload) Close(int step) => ("w" + step, "movement-window-closed", new MovementWindowClosed(step));

        // A4.2, A7.8: g1 breaks under fire and leaves the stack; g2 moves on alone, and nothing else moves until it ends.
        var split = Project(With(9, Step("m1", stack, "bd01:D5:0", 1), Broken("g1"), Close(1), Step("m2", G2, "bd01:C5:0", 2)));
        Assert.False(split.HasErrors, string.Join(" ", split.Diagnostics));
        Assert.Equal(G2, split.Current!.Movement!.Members);
        Assert.True(split.Current.Unit("g1")!.MovementEnded);
        Refused(Project(With(9, Step("m1", stack, "bd01:D5:0", 1), Broken("g1"), Close(1), Step("m2", G1, "bd01:C5:0", 2))), "UNIT-STATE-029");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Close(1), Step("m2", G2, "bd01:C5:0", 2))), "UNIT-STATE-029");

        // The ATTACKER ends one member; the stack's move is over only when the last one ends.
        var partly = Project(With(9, Step("m1", stack, "bd01:D5:0", 1), Close(1), ("e1", "movement-ended", new MovementEnded(G1))));
        Assert.False(partly.HasErrors, string.Join(" ", partly.Diagnostics));
        Assert.Equal(G2, partly.Current!.Movement!.Members);
        var over = Project(With(9, Step("m1", stack, "bd01:D5:0", 1), Close(1), ("e1", "movement-ended", new MovementEnded(G1)),
            ("e2", "movement-ended", new MovementEnded(G2))));
        Assert.False(over.HasErrors, string.Join(" ", over.Diagnostics));
        Assert.Null(over.Current!.Movement);

        // A member Reduced to a HS in Good Order is followed in the stack by its HS (A7.302).
        var reduced = Project(With(9, Step("m1", stack, "bd01:D5:0", 1), ("l1", "lineage", new LineageRecorded(LineageAction.Reduced, G1,
            [new NewInstance("g1-hs", "asl:half-squad", "attacker-half-squad", "german", null, null,
                new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.False })]))));
        Assert.False(reduced.HasErrors, string.Join(" ", reduced.Diagnostics));
        Assert.Equal(["g2", "g1-hs"], reduced.Current!.Movement!.Members);
        Assert.Equal(["g2", "g1-hs"], reduced.Current.Movement.Movers);

        // A stack whose only member broke is ended by naming the unit of its latest step.
        var broken = Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Broken("g1"), Close(1), ("e1", "movement-ended", new MovementEnded(G1))));
        Assert.False(broken.HasErrors, string.Join(" ", broken.Diagnostics));
        Assert.Null(broken.Current!.Movement);
    }

    [Fact]
    public void AReplacedOrCreatedUnitGainsNoFreshMfAndAReplacementKeepsTheSw()
    {
        var clear = new Dictionary<string, ConditionState> { [Conditions.Broken] = ConditionState.False };

        // A15.3, A4.2: a Battle Hardened mover is a Replacement: it has spent what g1 spent, and keeps g1's LMG.
        var hardened = Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("l1", "lineage", new LineageRecorded(LineageAction.Replaced, G1,
            [new NewInstance("g1-e", "asl:squad", "attacker-elite-squad", "german", null, null, clear)]))));
        Assert.False(hardened.HasErrors, string.Join(" ", hardened.Diagnostics));
        Assert.Equal((1, false), (hardened.Current!.Unit("g1-e")!.MfSpent, hardened.Current.Unit("g1-e")!.MovementEnded));
        Assert.Equal("g1-e", hardened.Current.Equipment.Single(item => item.Id == "g-lmg").Holding!.Holder);
        Assert.Equal(["g1-e"], hardened.Current.Movement!.Members);

        // A15.21: a hero created in his side's MPh moves no further that phase.
        var hero = Project(With(9, Step("m1", G1, "bd01:D5:0", 1), ("h1", "instance-created", new InstanceCreated(
            new NewInstance("g1-hero", "asl:hero", "attacker-hero", "german", new MapPosition(D5), null, clear)))));
        Assert.False(hero.HasErrors, string.Join(" ", hero.Diagnostics));
        Assert.True(hero.Current!.Unit("g1-hero")!.MovementEnded);
    }

    [Fact]
    public void ResidualFpIsTheFireRecordsValueAndOnlyALargerCounterReplacesIt()
    {
        var placed = Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Roll("x-roll-1", 3, 4), Fire("f1", "x-roll-1", 1, 4),
            ("p1", "residual-fp-placed", new ResidualFirePlaced("f1", D5, 4))));
        Assert.False(placed.HasErrors, string.Join(" ", placed.Diagnostics));
        Assert.Equal(new ResidualFire(D5, 4, "f1"), Assert.Single(placed.Current!.ResidualFire));

        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Roll("x-roll-1", 3, 4), Fire("f1", "x-roll-1", 1, 4),
            ("p1", "residual-fp-placed", new ResidualFirePlaced("f1", D5, 6)))), "UNIT-STATE-026");
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Roll("x-roll-1", 3, 4), Fire("f1", "x-roll-1", 1, 4),
            ("p1", "residual-fp-placed", new ResidualFirePlaced("f1", D5, 4)), ("p2", "residual-fp-placed", new ResidualFirePlaced("f1", D5, 4)))),
            "UNIT-STATE-026");

        // A fire record in the MPh answers the open window's step.
        Refused(Project(With(9, Step("m1", G1, "bd01:D5:0", 1), Roll("x-roll-1", 3, 4), Fire("f1", "x-roll-1", 2, 4))), "UNIT-STATE-024");
    }

    [Fact]
    public void TheLiveRecordsRoundTrip()
    {
        var events = With(9, Step("m1", G1, "bd01:D5:0", 1), Roll("x-roll-1", 3, 4), Fire("f1", "x-roll-1", 1, 4),
            ("p1", "residual-fp-placed", new ResidualFirePlaced("f1", D5, 4)), ("w1", "movement-window-closed", new MovementWindowClosed(1)),
            ("e1", "movement-ended", new MovementEnded(G1)));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("live", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record!));
        Assert.Equal(1, Assert.IsType<FireResolved>(read.Record!.Events[^4].Payload).MovementStep);

        var rallied = With(8, Malfunctioned("mg9", "g2"), Roll("x-roll-1", 2), Repair("p1", "g2", "mg9", "x-roll-1", 2, RepairAttempted.Repaired),
            Roll("y-roll-1", 2, 3), Rally("a1", "g1", "y-roll-1"));
        text = GameEventWriter.Write(rallied[0].Scope, new GameRecord("live", true, rallied));
        read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(text, GameEventWriter.Write(rallied[0].Scope, read.Record!));
    }
}
