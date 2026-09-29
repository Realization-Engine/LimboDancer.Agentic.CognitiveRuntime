using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>
/// The records of unit steps 29 and 30 and their replay checks: Advance (UNIT-STATE-032), Ambush and CC rounds (UNIT-STATE-030),
/// Melee at the end of the CCPh (A11.15), and a pending surrender and its capture (UNIT-STATE-031). The synthetic fixture holds g1,
/// g2, and gh1 in D4 and the Russian r1 in E4; the CC and Ambush records are checked by a stub verifier.
/// </summary>
public sealed class CloseCombatRecordTests
{
    private static readonly Lazy<UnitCatalog> Catalog = new(() => UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitsTestData.Asl.Value)!.Catalog!);

    private static readonly JsonElement Facts = JsonSerializer.SerializeToElement(new { phase = "CCPh" });
    private static readonly BoardLocation E4 = BoardLocation.Parse("bd01:E4:0");
    private static readonly string[] G1 = ["g1"];
    private static readonly string[] R1 = ["r1"];

    private sealed class Verifier : ICloseCombatRecordVerifier
    {
        public string? VerifyAmbush(GameState state, AmbushRolled ambush, IReadOnlyDictionary<string, DiceRolled> rolls) => null;

        public string? Verify(GameState state, CloseCombatResolved combat, IReadOnlyDictionary<string, DiceRolled> rolls) => null;
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

    private static GameHistory Project(IReadOnlyList<GameEvent> events) =>
        GameProjector.Project(events, UnitsTestData.Asl.Value, [Catalog.Value], new FakeChains(), closeCombat: new Verifier());

    private static (string, string, EventPayload) Phase(string id, string phase, string side = "german") => (id, "phase-changed", new PhaseChanged(1, phase, side));

    private static (string, string, EventPayload) Advance(string id, string[] units, BoardLocation to) => (id, "advanced", new AdvanceMoved(units, to));

    private static (string, string, EventPayload) Combat(string id, string round, string[] attackers, string[] defenders) =>
        (id, "close-combat-resolved", new CloseCombatResolved(E4, round, attackers, defenders, new Dictionary<string, string>(), Facts, Facts));

    private static (string, string, EventPayload) Ambush(string id, string? ambusher) =>
        (id, "ambush-rolled", new AmbushRolled(E4, new Dictionary<string, string>(), ambusher, Facts, Facts));

    private static (string, string, EventPayload) Changed(string id, string unit, string condition, ConditionState value) =>
        (id, "conditions-changed", new ConditionsChanged(unit, new Dictionary<string, ConditionState> { [condition] = value }));

    private static void Refused(GameHistory history, string code) =>
        Assert.True(history.Diagnostics.Any(item => item.Code == code), string.Join(" ", history.Diagnostics));

    [Fact]
    public void AnAdvanceMovesUnitsOnceInTheAph()
    {
        var advanced = Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4)));
        Assert.False(advanced.HasErrors, string.Join(" ", advanced.Diagnostics));
        Assert.Equal(E4, advanced.Current!.Location("g1")!.Location);
        Assert.Equal([new AdvanceRecord("g1", E4)], advanced.Current.Advances);

        // A4.7: once per APh, only in the APh, never a broken unit (A15.431: nor a berserk one).
        Refused(Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Advance("a2", G1, BoardLocation.Parse("bd01:D4:0")))), "UNIT-STATE-032");
        Refused(Project(With(8, Advance("a1", G1, E4))), "UNIT-STATE-032");
        Refused(Project(With(8, Phase("p1", "aph"), Changed("b1", "g1", Conditions.Broken, ConditionState.True), Advance("a1", G1, E4))), "UNIT-STATE-032");
        Refused(Project(With(8, Phase("p1", "aph"), Changed("b1", "g1", Conditions.Berserk, ConditionState.True), Advance("a1", G1, E4))), "UNIT-STATE-032");
    }

    [Fact]
    public void ALocationsCcIsResolvedOnceAndUnitsLeftTogetherAreHeldInMelee()
    {
        // Ruling R14.2: a concealed unit is not held in Melee, so the Russian is Known here, as its own attack would have made it.
        var events = With(8, Changed("k1", "r1", Conditions.Concealed, ConditionState.False), Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"),
            Combat("c1", CloseCombatResolved.Simultaneous, G1, R1));
        var combat = Project(events);
        Assert.False(combat.HasErrors, string.Join(" ", combat.Diagnostics));
        Assert.True(combat.Current!.CloseCombats.Single().Closed);

        // A11.12: once per CCPh; no ambusher's round without an Ambush.
        Refused(Project(Next(events, Combat("c2", CloseCombatResolved.Simultaneous, R1, G1))), "UNIT-STATE-030");
        Refused(Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Combat("c1", CloseCombatResolved.AmbusherRound, G1, R1))),
            "UNIT-STATE-030");

        // A11.15: at the end of the CCPh both are held in Melee; the Melee ends when the Russian is gone.
        var melee = Project(Next(events, Phase("p3", "rph", "russian")));
        Assert.False(melee.HasErrors, string.Join(" ", melee.Diagnostics));
        Assert.True(GameState.Condition(melee.Current!.Unit("g1")!, Conditions.Melee) == ConditionState.True
            && GameState.Condition(melee.Current.Unit("r1")!, Conditions.Melee) == ConditionState.True);
        var freed = Project(Next(events, Phase("p3", "rph", "russian"), ("x1", "instance-eliminated", new InstanceEliminated("r1"))));
        Assert.Equal(ConditionState.False, GameState.Condition(freed.Current!.Unit("g1")!, Conditions.Melee));

        // A11.16: a broken unit left in Melee is eliminated before the next CCPh ends.
        Refused(Project(Next(events, Phase("p3", "rph", "russian"), Changed("b1", "r1", Conditions.Broken, ConditionState.True), Phase("p4", "ccph", "russian"),
            Phase("p5", "rph", "german"))), "UNIT-STATE-030");
    }

    [Fact]
    public void AnAmbushOrdersTheRoundsAndHoldsThePhaseUntilTheSecond()
    {
        var events = With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Ambush("m1", "german"), Combat("c1", CloseCombatResolved.AmbusherRound, G1, R1));
        var first = Project(events);
        Assert.False(first.HasErrors, string.Join(" ", first.Diagnostics));
        Refused(Project(Next(events, Phase("p3", "rph", "russian"))), "UNIT-STATE-030");
        var second = Project(Next(events, Combat("c2", CloseCombatResolved.AmbushedRound, R1, G1), Phase("p3", "rph", "russian")));
        Assert.False(second.HasErrors, string.Join(" ", second.Diagnostics));

        // The Ambush drs come before any CC in the Location.
        Refused(Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Combat("c1", CloseCombatResolved.Simultaneous, G1, R1), Ambush("m1", null))),
            "UNIT-STATE-030");
    }

    [Fact]
    public void APendingSurrenderHoldsThePhaseUntilItsCaptorTakesIt()
    {
        var events = With(8, Changed("b1", "r1", Conditions.Broken, ConditionState.True), ("s1", "surrender-pending", new SurrenderPending("r1", G1)));
        var pending = Project(events);
        Assert.False(pending.HasErrors, string.Join(" ", pending.Diagnostics));
        Refused(Project(Next(events, Phase("p1", "pfph"))), "UNIT-STATE-031");
        Refused(Project(Next(events, ("m1", "instance-moved", new InstanceMoved("r1", new MapPosition(BoardLocation.Parse("bd01:D4:0")))),
            ("k1", "instance-captured", new InstanceCaptured("r1", "g2")))), "UNIT-STATE-031");
        var taken = Project(Next(events, ("m1", "instance-moved", new InstanceMoved("r1", new MapPosition(BoardLocation.Parse("bd01:D4:0")))),
            ("k1", "instance-captured", new InstanceCaptured("r1", "g1")), Phase("p1", "pfph")));
        Assert.False(taken.HasErrors, string.Join(" ", taken.Diagnostics));
        Assert.Empty(taken.Current!.PendingSurrenders);

        // A20.53: the prisoner moves with its Guard.
        var moved = Project(Next(events, ("m1", "instance-moved", new InstanceMoved("r1", new MapPosition(BoardLocation.Parse("bd01:D4:0")))),
            ("k1", "instance-captured", new InstanceCaptured("r1", "g1")), Phase("p1", "aph"), Advance("a1", G1, BoardLocation.Parse("bd01:D5:0"))));
        Assert.False(moved.HasErrors, string.Join(" ", moved.Diagnostics));
        Assert.Equal(BoardLocation.Parse("bd01:D5:0"), moved.Current!.Location("r1")!.Location);
    }

    [Fact]
    public void ABrokenGuardInMeleeIsNotEliminatedAtTheEndOfTheCcph()
    {
        // Table-player review, item 4: A11.16 eliminates only a non-guard broken unit that cannot withdraw. r1 guards g2 and is
        // broken in Melee with g1 in E4.
        (string, string, EventPayload)[] melee = [("m2", "instance-moved", new InstanceMoved("g1", new MapPosition(E4))), Phase("p1", "aph"), Phase("p2", "ccph"),
            Changed("c1", "r1", Conditions.Melee, ConditionState.True), Changed("c2", "g1", Conditions.Melee, ConditionState.True),
            Changed("c3", "r1", Conditions.Broken, ConditionState.True), Phase("p3", "rph", "russian")];
        var guarded = Project(With(8, [Changed("b1", "g2", Conditions.Broken, ConditionState.True), ("s1", "surrender-pending", new SurrenderPending("g2", R1)),
            ("m1", "instance-moved", new InstanceMoved("g2", new MapPosition(E4))), ("k1", "instance-captured", new InstanceCaptured("g2", "r1")), .. melee]));
        Assert.False(guarded.HasErrors, string.Join(" ", guarded.Diagnostics));

        // Without its prisoner the broken unit in Melee is eliminated first.
        Refused(Project(With(8, melee)), "UNIT-STATE-030");
    }

    [Fact]
    public void AnAmbushWithNoAmbusherAwaitsItsRound()
    {
        // Table-player review, item 8: the refusal names the round still due, not an ambushed side's round.
        var history = Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Ambush("m1", null), Phase("p3", "rph", "russian")));
        Assert.Contains(history.Diagnostics, item => item.Code == "UNIT-STATE-030" && item.Message.Contains("after the Ambush drs", StringComparison.Ordinal));

        // A11.3: after an Ambush the ambusher may make a second record before the ambushed side's round.
        var sequential = Project(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Ambush("m1", "german"),
            Combat("c1", CloseCombatResolved.AmbusherRound, G1, R1), Combat("c2", CloseCombatResolved.AmbusherRound, ["g2"], ["r9"]),
            Combat("c3", CloseCombatResolved.AmbushedRound, R1, G1)));
        Assert.False(sequential.HasErrors, string.Join(" ", sequential.Diagnostics));
        Assert.True(sequential.Current!.CloseCombats.Single().Closed);
    }

    [Fact]
    public void TheCloseCombatRecordsRoundTrip()
    {
        var events = Next(With(8, Phase("p1", "aph"), Advance("a1", G1, E4), Phase("p2", "ccph"), Ambush("m1", "german"),
            Combat("c1", CloseCombatResolved.AmbusherRound, G1, R1)), ("s1", "surrender-pending", new SurrenderPending("r1", G1)));
        var text = GameEventWriter.Write(events[0].Scope, new GameRecord("live", true, events));
        var read = GameEventReader.Read(Encoding.UTF8.GetBytes(text));
        Assert.False(read.HasErrors, string.Join(" ", read.Diagnostics));
        Assert.Equal(text, GameEventWriter.Write(events[0].Scope, read.Record!));
        Assert.Equal("german", Assert.IsType<AmbushRolled>(read.Record!.Events[^3].Payload).Ambusher);
    }

    private static List<GameEvent> Next(List<GameEvent> events, params (string Id, string Type, EventPayload Payload)[] added)
    {
        List<GameEvent> next = [.. events];
        foreach (var (id, type, payload) in added)
        {
            next.Add(next[^1] with
            {
                EventId = id,
                Revision = next[^1].Revision + 1,
                Type = type,
                Payload = payload,
                Causes = [],
                Visibility = null
            });
        }

        return next;
    }
}
