using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 32.j (S10): setup, entry from off board, exit, the Victory Conditions, and concealment at setup, read directly in Rules: the card's diagnostics in
/// their order, the playable area and the setup areas, the setup's checks and fill, the entry's hexes and bars with their lazy reads, the exit's blocks,
/// VP and the immediate Victory, the Control fold over a reader, and the non-OB "?" scan that reads the LOS only within 16 hexes.
/// </summary>
public sealed class ScenarioA1SetupRulesTests
{
    private static readonly IReadOnlyList<string> Edges = ["top", "bottom", "left", "right"];

    // The card's rules.

    [Fact]
    public void TheCardsBoardsAndTurnsAreCheckedInTheirOrder()
    {
        var boards = ScenarioA1SetupCalculator.BoardDiagnostics([new CardBoardFacts("bd01", true, 0, 0), new CardBoardFacts("bd01", true, 0, 0)], "north", Edges).ToArray();
        Assert.Equal(["card.boards: two boards share a slot", "card.boards: a board is named twice", "card.north: North is the map's top, bottom, left, or right"], boards);
        Assert.Equal("card.boards: a card names its boards (A2.1)", ScenarioA1SetupCalculator.BoardDiagnostics([], "top", Edges).Single());
        Assert.Null(ScenarioA1SetupCalculator.PlayableAreaDiagnostic(enforced: true, hexrowsGiven: true, "A", "GG", boardNamed: false, boardCount: 1, boardOnCard: false));
        Assert.StartsWith("card.playable:", ScenarioA1SetupCalculator.PlayableAreaDiagnostic(true, true, "GG", "A", false, 1, false));
        Assert.Null(ScenarioA1SetupCalculator.PlayableAreaDiagnostic(false, false, null, null, false, 2, false));
        var turns = ScenarioA1SetupCalculator.TurnDiagnostics(["russian", "german"], 0, "german", null, null, [("russian", 2, -1), ("german", 2, 0)]).ToArray();
        Assert.Equal(4, turns.Length);
        Assert.StartsWith("card.turns: a card has 1 to 30", turns[0]);
        Assert.StartsWith("card.turns: the side that sets up first", turns[1]);
        Assert.StartsWith("card.setup: a sequential setup", turns[2]);
        Assert.StartsWith("card.ob: a group's", turns[3]);
    }

    [Fact]
    public void ASideIsCheckedInItsOrder()
    {
        var side = new CardSideFacts("axis-minor", 8, null, "top", "ssr", false, 3, "", [new CardLineFacts("x", false, false, 0, "a", false)],
            [new CardGroupFacts("Group", 6, [new CardAreaFacts("b", "building", ["A1"], false, false, null, null, null, null, null, null)], [new CardLineFacts("u", true, false, 0, "z", false)])]);
        var found = ScenarioA1SetupCalculator.SideDiagnostics(side, minimal: false, Edges, ["ssr", "entry", "setup", "manufactured", "none"], boardCount: 1, turnCount: 6).ToArray();
        Assert.StartsWith("card.san: axis-minor's SAN is 0 to 7", found[0]);
        Assert.StartsWith("card.nation: an Axis Minor side names its nation (romanian, hungarian", found[1]);
        Assert.StartsWith("card.elr: only a minimal card", found[2]);
        Assert.StartsWith("card.balance: axis-minor has its Balance", found[3]);
        Assert.StartsWith("card.balance: the Balance counter 'x'", found[4]);
        Assert.StartsWith("card.elr: 'Group' has an ELR", found[5]);
        Assert.StartsWith("card.setup: 'Group' area 'b' is not a valid building area", found[6]);
        Assert.Equal("card.ob: 'u' is not axis-minor", found[7]);
        Assert.Equal("card.ob: 'u' has a count of at least 1", found[8]);
        Assert.Equal("card.ob: 'u' names the unknown area 'z'", found[9]);
        Assert.Equal(10, found.Length);
    }

    [Fact]
    public void AreasAndConditionsAreValidByTheirKind()
    {
        Assert.True(ScenarioA1SetupCalculator.AreaValid(new CardAreaFacts("A1", "building", ["A1", "B2"], false, false, null, null, null, null, 2, 1), 1, 6, Edges));
        Assert.False(ScenarioA1SetupCalculator.AreaValid(new CardAreaFacts("A1", "building", ["A1", "B2"], false, false, null, null, null, null, 2, 3), 1, 6, Edges));
        Assert.True(ScenarioA1SetupCalculator.AreaValid(new CardAreaFacts("e", "entry", null, false, false, null, null, 2, "top", null, null), 1, 6, Edges));
        Assert.False(ScenarioA1SetupCalculator.AreaValid(new CardAreaFacts("e", "entry", null, false, false, null, null, 7, "top", null, null), 1, 6, Edges));
        Assert.True(ScenarioA1SetupCalculator.AreaValid(new CardAreaFacts("h", "hex-numbers", null, true, true, 0, 5, null, null, null, null), 1, 6, Edges));
        var condition = new CardConditionFacts("control-count", "russian", null, null, null, "b", 3, null, null, null);
        Assert.True(ScenarioA1SetupCalculator.ConditionValid(condition, ["russian", "german"], Edges, id => id == "b" ? 4 : null, _ => true));
        var five = condition with
        {
            AtLeast = 5
        };
        Assert.False(ScenarioA1SetupCalculator.ConditionValid(five, ["russian", "german"], Edges, id => id == "b" ? 4 : null, _ => true));
        Assert.True(ScenarioA1SetupCalculator.ConditionValid(new CardConditionFacts("exit-vp", "german", null, null, null, null, 10, null, "top", ["A1"]), ["russian", "german"], Edges, _ => null, _ => true));
        Assert.Equal(1.0, ScenarioA1SetupCalculator.SquadEquivalent("asl:squad"));
        Assert.Equal(0.5, ScenarioA1SetupCalculator.SquadEquivalent("asl:crew"));
        Assert.Equal(26, ScenarioA1SetupCalculator.IntegrityBpv([("asl:squad", 10, 2), ("asl:half-squad", 6, 1), ("asl:leader", 99, 1)]));
        Assert.True(ScenarioA1SetupCalculator.StartsOnTurnOne("entry", 1));
        Assert.False(ScenarioA1SetupCalculator.StartsOnTurnOne("entry", 2));
    }

    [Fact]
    public void ThePlayableAreaAndTheSetupAreasReadLocationTexts()
    {
        Assert.True(ScenarioA1SetupCalculator.Playable(false, null, null, null, 1, "bd01", "bd01", "A1"));
        Assert.True(ScenarioA1SetupCalculator.Playable(true, "A", "G", null, 1, "bd01", "bd01", "C5"));
        Assert.False(ScenarioA1SetupCalculator.Playable(true, "A", "G", null, 1, "bd01", "bd01", "H5"));
        Assert.True(ScenarioA1SetupCalculator.Playable(true, "A", "G", "bd01", 2, null, "bd02", "Z5"));
        Assert.Equal(32, ScenarioA1SetupCalculator.HexrowIndex("GG"));
        Assert.Null(ScenarioA1SetupCalculator.HexrowIndex("AB"));
        Assert.True(ScenarioA1SetupCalculator.Within("building", ["M9"], null, null, null, 1, "bd01", "bd01", "M9", 9));
        Assert.True(ScenarioA1SetupCalculator.Within("hex-numbers", null, "bd01", 1, 5, 1, "bd01", "bd01", "M3", 3));
        Assert.False(ScenarioA1SetupCalculator.Within("hex-numbers", null, "bd01", 1, 5, 1, "bd01", "bd02", "M3", 3));
    }

    // The setup's legality and fill.

    private static SetupCardFacts Card(int? dummies = null, bool? concealed = null) => new(
        [new SetupSideFacts("russian", null, true, [], [new SetupGroupFacts("russian-1", "Guards", null, dummies,
            [new SetupAreaFacts("area", "hex-numbers", null, "bd01", 1, 5, null, null, null, null, concealed)], [new SetupLineFacts("squad", 2, null)])]),
         new SetupSideFacts("german", null, false, [], [new SetupGroupFacts("german-1", "Grenadiers", null, null,
            [new SetupAreaFacts("entry", "entry", null, null, null, null, 1, "top", null, null, null)], [new SetupLineFacts("gsquad", 1, null)])])],
        1, "bd01", false, null, null, null, null, []);

    private static SetupCounterFacts Counter(string id, string side, string? group, string definition, string kind, string? at, bool concealed = false, bool dummy = false, bool offBoard = false,
        bool isNew = false, bool broken = false) =>
        new(id, side, group, definition, kind, at is null ? null : new SetupLocationFacts($"bd01:{at}:0", "bd01", at, int.Parse(at[1..], System.Globalization.CultureInfo.InvariantCulture)),
            concealed, false, dummy, false, isNew, offBoard, broken, false, null, null, false, false);

    [Fact]
    public void TheSetupCheckFillsLinesAndRefusesInItsOrder()
    {
        var verdict = ScenarioA1SetupCalculator.Check(Card(dummies: 0), [Counter("s1", "russian", "russian-1", "squad", "asl:squad", "A3", concealed: true), Counter("s2", "russian", "russian-1", "squad", "asl:squad", "A9"),
            Counter("g1", "german", "german-1", "gsquad", "asl:squad", null, offBoard: true), Counter("b1", "russian", "russian-1", "squad", "asl:squad", "A3", broken: true)],
            _ => "woods", _ => true, _ => null, 7, null);
        Assert.StartsWith("play.setup-broken: b1 sets up broken", verdict.Reasons[0]);
        Assert.StartsWith("play.setup-area: s2 sets up at bd01:A9:0, outside the setup areas of Guards", verdict.Reasons[1]);
        Assert.StartsWith("play.setup-concealment: Guards uses 1 \"?\" at setup and has 0", verdict.Reasons[2]);
        // s1 and b1 fill the two squad lines (s2 outside the areas takes only the area reason), so nothing is left over.
        Assert.Equal(3, verdict.Reasons.Count);
        Assert.Equal(2, verdict.Groups.Count);
        Assert.True(verdict.Groups[0].SetsUp);
        Assert.False(verdict.Groups[1].SetsUp);
        Assert.True(verdict.Groups[1].Enters);
        Assert.Empty(verdict.Groups[1].OffBoard);
        Assert.True(verdict.Groups[0].Complete);
        Assert.Null(verdict.CurrentOrder);
    }

    [Fact]
    public void TheSetupStackingCountsSmcInTenthsBeyondFour()
    {
        SetupCounterFacts[] stack = [.. Enumerable.Range(0, 5).Select(index => Counter($"l{index}", "russian", "russian-1", "leader", "asl:leader", "A1")),
            Counter("s", "russian", "russian-1", "squad", "asl:squad", "A1"), Counter("h", "russian", "russian-1", "hs", "asl:half-squad", "A1")];
        Assert.Equal(2.0m, ScenarioA1SetupCalculator.SetupStackSquadEquivalents(stack, 5));
        Assert.True(ScenarioA1SetupCalculator.EntryStackOverstacked(2.0, 5));
        Assert.False(ScenarioA1SetupCalculator.EntryStackOverstacked(3.0, 4));
    }

    [Fact]
    public void TheStartOfPlayBarReadsTheLibraryOnlyBeforePlay()
    {
        var read = 0;
        Assert.Null(ScenarioA1SetupCalculator.SetupIncompleteBar(pastSetup: true, () => { read++; return true; }, () => "gone", () => null));
        Assert.Equal(0, read);
        Assert.Equal("gone", ScenarioA1SetupCalculator.SetupIncompleteBar(false, () => true, () => "gone", () => null));
        var open = new SetupGroupVerdict("russian", "russian-1", "Guards", 1, true, false, [new SetupNeedVerdict("russian-1", "area", "squad", 2)], 0, [], false);
        Assert.Equal("play.setup-incomplete: Guards (russian) has not finished setting up: 2 squad in area left (A2.9; ruling R19.2)", ScenarioA1SetupCalculator.SetupIncompleteBar(false, () => false, () => "gone", () => [open]));
        var waiting = open with
        {
            Complete = true,
            Remaining = [],
            OffBoard = [new SetupNeedVerdict("russian-1", "entry", "squad", 1)],
        };
        Assert.StartsWith("play.setup-incomplete: Guards (russian) still sets up off board to enter: 1 squad (A2.51", ScenarioA1SetupCalculator.SetupIncompleteBar(false, () => false, () => "gone", () => [waiting]));
        Assert.Equal(["russian-1"], ScenarioA1SetupCalculator.OutOfSight(false, false, "german", () => (1, [open])).ToArray());
        Assert.Empty(ScenarioA1SetupCalculator.OutOfSight(true, false, "german", () => throw new InvalidOperationException()));
    }

    // Entry from off board.

    [Fact]
    public void TheEntryHexesWidenFourAGameTurnAndNeverPastARiver()
    {
        // Six edge hexes in a row; the card names hex 2; hex 4 is a river.
        static int? Distance(int one, int two) => Math.Abs(one - two);
        Assert.Equal([0, 1, 2, 3, 4, 5], ScenarioA1EntryCalculator.EntryHexesFor(6, false, _ => false, 3, 1, _ => false, Distance));
        Assert.Equal([2], ScenarioA1EntryCalculator.EntryHexesFor(6, true, at => at == 2, 1, 1, _ => throw new InvalidOperationException(), (_, _) => throw new InvalidOperationException()));
        Assert.Equal([0, 1, 2, 3], ScenarioA1EntryCalculator.EntryHexesFor(6, true, at => at == 2, 2, 1, at => at == 4, Distance));
        Assert.True(ScenarioA1EntryCalculator.IsRiver("Canal"));
        Assert.False(ScenarioA1EntryCalculator.IsRiver("Woods"));
        Assert.True(ScenarioA1EntryCalculator.EntryObstructor(true, true, false, false, true));
        Assert.False(ScenarioA1EntryCalculator.EntryObstructor(true, true, true, true, true));
        var cost = 0;
        Assert.Null(ScenarioA1EntryCalculator.EntryHexBar(false, "bd01:A1:0", vehicle: true, () => { cost++; return false; }));
        Assert.Equal(0, cost);
        Assert.StartsWith("play.entry-terrain: the entry cost of bd01:A1:0", ScenarioA1EntryCalculator.EntryHexBar(false, "bd01:A1:0", false, () => false));
        Assert.StartsWith("play.entry-occupied: bd01:A1:0 holds a Known enemy", ScenarioA1EntryCalculator.EntryHexBar(true, "bd01:A1:0", false, () => true));
    }

    [Fact]
    public void TheAphEndNamesWhoWaitsAndWhere()
    {
        var text = ScenarioA1EntryCalculator.EntryDue([new EntryWaitFacts("v", true, 1, () => throw new InvalidOperationException()), new EntryWaitFacts("a", false, 2, () => ["bd01:A1:0"]),
            new EntryWaitFacts("b", false, 1, () => ["bd01:A1:0", "bd01:A2:0"]), new EntryWaitFacts("c", false, 1, () => [])], 1);
        Assert.Equal("play.entry-due: b waits off board and must still enter this Game Turn: advance into a hex of the entry edge, such as bd01:A1:0, bd01:A2:0, before the APh ends (A2.5; rulings R20.5, R25.1)", text);
        Assert.Null(ScenarioA1EntryCalculator.EntryDue([], 1));
        var reads = new List<string>();
        var hexes = ScenarioA1EntryCalculator.AdvanceEntries(false, cx: true, () => 2, at => { reads.Add($"bar{at}"); return at == 0; }, at => { reads.Add($"edge{at}"); return true; },
            at => { reads.Add($"cost{at}"); return (4, false); }, half => { reads.Add($"hard{half}"); return true; });
        Assert.Empty(hexes);
        Assert.Equal(["bar0", "bar1", "edge1", "cost1", "hard4"], reads);
        Assert.Empty(ScenarioA1EntryCalculator.AdvanceEntries(true, false, () => throw new InvalidOperationException(), _ => false, _ => true, _ => null, _ => null));
        Assert.Equal([1], ScenarioA1EntryCalculator.OpenEntryHexes(1, 1, () => 2, at => at == 0));
        Assert.Empty(ScenarioA1EntryCalculator.OpenEntryHexes(2, 1, () => throw new InvalidOperationException(), _ => false));
    }

    [Fact]
    public void TheEnteringStackIsCheckedInItsOrder()
    {
        static OffBoardMoverFacts Mover(string id, int turn = 1, string edge = "top", IReadOnlyList<string>? named = null, bool vehicle = false, double squads = 1, bool smc = false) =>
            new(id, vehicle, squads, smc, (turn, edge, named));
        var (edge, side, reason) = ScenarioA1EntryCalculator.EntryCheck([Mover("a"), Mover("b")], "bd01:A1:0", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null);
        Assert.Equal(("top", 3, null), (edge, side, reason));
        Assert.StartsWith("play.entry-vehicle: v is a vehicle", ScenarioA1EntryCalculator.EntryCheck([Mover("v", vehicle: true)], "x", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null).Reason);
        Assert.StartsWith("play.entry-stacking:", ScenarioA1EntryCalculator.EntryCheck([Mover("a", squads: 2), Mover("b", squads: 2)], "x", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null).Reason);
        Assert.StartsWith("play.entry-turn: a enters on Game Turn 2, not 1", ScenarioA1EntryCalculator.EntryCheck([Mover("a", turn: 2)], "x", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null).Reason);
        Assert.StartsWith("play.entry-stack: a stack enters along one edge", ScenarioA1EntryCalculator.EntryCheck([Mover("a"), Mover("b", edge: "left")], "x", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null).Reason);
        Assert.StartsWith("play.entry-edge: a enter at ground level in a hex of the top edge, and x is not one", ScenarioA1EntryCalculator.EntryCheck([Mover("a")], "x", 1, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => null).Reason);
        Assert.Equal("outside", ScenarioA1EntryCalculator.EntryCheck([Mover("a")], "x", 0, 1, false, false, _ => 3, () => "outside", _ => true, _ => true, () => null).Reason);
        Assert.StartsWith("play.entry-hex: the card names A1 for a's entry, and none is a hex of the top edge", ScenarioA1EntryCalculator.EntryCheck([Mover("a", named: ["A1"])], "x", 0, 1, false, false, _ => 3, () => null, _ => false, _ => false, () => null).Reason);
        Assert.StartsWith("play.entry-hex: a was to enter by A1; on Game Turn 2 it enters within 4 hexes", ScenarioA1EntryCalculator.EntryCheck([Mover("a", named: ["A1"])], "x", 0, 2, false, false, _ => 3, () => null, _ => false, _ => true, () => null).Reason);
        Assert.Equal("barred", ScenarioA1EntryCalculator.EntryCheck([Mover("a")], "x", 0, 1, false, false, _ => 3, () => null, _ => true, _ => true, () => "barred").Reason);
        Assert.Null(ScenarioA1EntryCalculator.EntryCheck([Mover("a")], "x", 0, 1, advancing: true, false, _ => 3, () => null, _ => true, _ => true, () => "barred").Reason);
    }

    // Infantry leaving the map.

    [Fact]
    public void TheExitBlocksRefuseInTheirOrderAndReadLazily()
    {
        Assert.StartsWith("play.exit-phase:", ScenarioA1EntryCalculator.ExitPhaseBar("rtph"));
        Assert.Null(ScenarioA1EntryCalculator.ExitPhaseBar("aph"));
        Assert.StartsWith("play.exit-stack: every unit leaving", ScenarioA1EntryCalculator.ExitStackBar(2, 1, false));
        Assert.StartsWith("play.exit-stack: the stack leaves from one Location", ScenarioA1EntryCalculator.ExitFromBar(1, true));
        Assert.True(ScenarioA1EntryCalculator.PushingOn(false, true, true, true));
        Assert.Equal("play.exit-unit: a is not free to move this MPh (A4.1, A3.3)", ScenarioA1EntryCalculator.ExitUnableBar([("a", false, false, false, false, false, false, true, false, false)], false, false));
        Assert.Null(ScenarioA1EntryCalculator.ExitUnableBar([("a", false, false, false, false, false, false, true, false, false)], false, pushingOn: true));
        Assert.Equal("play.exit-unit: a is not free to advance this APh (A4.7)", ScenarioA1EntryCalculator.ExitUnableBar([("a", false, true, false, false, false, false, false, false, false)], true, false));
        Assert.Null(ScenarioA1EntryCalculator.ExitUnableBar([("a", false, false, false, false, false, false, false, false, true)], advancing: true, false));
        var qsu = 0;
        Assert.StartsWith("play.exit-unit: c mans g; it leaves the map only pushing it", ScenarioA1EntryCalculator.ExitGunBar("c", "g", null, false, [], true, true, false, () => { qsu++; return true; }));
        Assert.Equal("play.move-push: c pushes its Gun alone; leave d out of the stack (C10.3, C10.111; ruling R26.4)", ScenarioA1EntryCalculator.ExitGunBar("c", "g", "g", false, ["d"], true, true, false, () => { qsu++; return true; }));
        Assert.Equal(0, qsu);
        Assert.StartsWith("play.move-push: g is not QSU", ScenarioA1EntryCalculator.ExitGunBar("c", "g", "g", false, [], true, true, false, () => false));
        Assert.Null(ScenarioA1EntryCalculator.ExitGunBar("c", "g", "g", false, [], true, true, false, () => true));
        Assert.StartsWith("play.move-window:", ScenarioA1EntryCalculator.ExitMovementBar(true, true, true, true, []));
        Assert.StartsWith("play.move-order: x moved last", ScenarioA1EntryCalculator.ExitMovementBar(true, false, false, false, ["x"]));
        Assert.StartsWith("play.exit-stack: the whole moving stack", ScenarioA1EntryCalculator.ExitMovementBar(true, false, false, true, ["x"]));
        var near = 0;
        Assert.Null(ScenarioA1EntryCalculator.ExitEdgeBar(0, true, true, "f", "top", () => { near++; return ["n"]; }));
        Assert.Equal("play.exit-edge: f is not a ground-level hex of the top edge within the playable area (A2.6; rulings R20.6, R21.5); n is next to it", ScenarioA1EntryCalculator.ExitEdgeBar(0, false, true, "f", "top", () => { near++; return ["n"]; }));
        Assert.Equal(1, near);
        Assert.Equal((null, 2, "open-ground"), ScenarioA1EntryCalculator.BypassExit(0, 1, 2, [3], "top"));
        Assert.StartsWith("play.exit-bypass: from Bypass", ScenarioA1EntryCalculator.BypassExit(0, 1, 2, [0], "top").Refusal);
        Assert.Equal((null, 2, "open-ground", true), ScenarioA1EntryCalculator.ExitCost([(4, "woods", false, false), (2, "open-ground", true, false), (1, "marsh", false, true)], "f"));
        Assert.StartsWith("play.exit-terrain:", ScenarioA1EntryCalculator.ExitCost([(1, "marsh", false, true)], "f").Refusal);
        Assert.Equal((null, 8), ScenarioA1EntryCalculator.PushedExit("grain", 4, "f"));
        Assert.StartsWith("play.move-push-terrain:", ScenarioA1EntryCalculator.PushedExit("woods", 4, "f").Refusal);
        Assert.Equal("for 1.5 MF at the road rate from Bypass", ScenarioA1EntryCalculator.ExitHow(3, true, true));
        var (refusal, tiring) = ScenarioA1EntryCalculator.AdvanceExit([("a", false), ("b", true), ("c", false)], index => index == 2 ? null : true);
        Assert.StartsWith("play.advance-difficult-terrain: b is CX", refusal);
        Assert.Equal(["a"], tiring);
        Assert.Equal(2, ScenarioA1EntryCalculator.DoubleTimeExtraMf(true, 0, false, 0));
        Assert.Equal(1, ScenarioA1EntryCalculator.DoubleTimeExtraMf(true, 1, false, 0));
        Assert.Equal("play.move-mf: a has 1.5 MF left, and leaving the map from f costs 4 (A2.6, A4.11)", ScenarioA1EntryCalculator.ExitMfBar([("a", 2, true)], _ => 4, 8, "f"));
        Assert.Equal(-1, ScenarioA1EntryCalculator.PushOffDrm(2, true));
        var escort = 0;
        Assert.Equal(string.Empty, ScenarioA1EntryCalculator.ExitScoring(false, false, true, () => { escort++; return true; }));
        Assert.StartsWith("; it counts toward the side's Exit VP", ScenarioA1EntryCalculator.ExitScoring(true, true, true, () => { escort++; return true; }));
        Assert.Equal(0, escort);
        Assert.StartsWith("; it meets no exit condition, but a Guard", ScenarioA1EntryCalculator.ExitScoring(true, false, true, () => true));
        Assert.True(ScenarioA1EntryCalculator.ExitMeetsCondition([new CardConditionFacts("exit-vp", "russian", null, null, null, null, 5, null, "top", ["A1", "B2"])], "russian", "top", hex => hex == "B2"));
        Assert.False(ScenarioA1EntryCalculator.ExitMeetsCondition([new CardConditionFacts("exit-vp", "russian", null, null, null, null, 5, null, "top", ["A1"])], "russian", "left", _ => true));
    }

    // VP and the immediate Victory.

    [Fact]
    public void VpReadTheLeaderAndTheVehicleOnlyWhenTheyAre()
    {
        Assert.Equal(2, ScenarioA1VictoryCalculator.VictoryPoints("asl:squad", () => throw new InvalidOperationException(), () => throw new InvalidOperationException()));
        Assert.Equal(3, ScenarioA1VictoryCalculator.VictoryPoints("asl:leader", () => -2, () => 0));
        Assert.Equal(1, ScenarioA1VictoryCalculator.VictoryPoints("asl:leader", () => 1, () => 0));
        Assert.Equal(7, ScenarioA1VictoryCalculator.VictoryPoints("asl:vehicle", () => null, () => 7));
        // One, the MA, three for an AF of 11, two for the crew.
        Assert.Equal(7, ScenarioA1VictoryCalculator.VehicleVictoryPoints(true, true, false, true, true, [8, 4, 11, null], () => true));
        Assert.Equal(1, ScenarioA1VictoryCalculator.VehicleVictoryPoints(true, false, false, true, false, [], () => false));
        Assert.Equal(2, ScenarioA1VictoryCalculator.VehicleVictoryPoints(false, false, false, false, true, [0], () => false));
        Assert.True(ScenarioA1VictoryCalculator.HasInherentCrew(true, false, () => false));
        Assert.False(ScenarioA1VictoryCalculator.HasInherentCrew(true, true, () => throw new InvalidOperationException()));
        Assert.True(ScenarioA1VictoryCalculator.VehicleArmed(false, false, false, true, false));
        Assert.Equal([0, 1], ScenarioA1VictoryCalculator.HexLevels([("Stone Building", 1), ("Rooftop", 2), ("Cellar", -1), ("Open Ground", 0)]));
        Assert.Equal([0], ScenarioA1VictoryCalculator.HexLevels(null));
        var card = 0;
        Assert.False(ScenarioA1VictoryCalculator.ImmediateVictoryRead(true, true, true, () => { card++; return true; }));
        Assert.Equal(0, card);
        Assert.True(ScenarioA1VictoryCalculator.ImmediateVictoryRead(true, false, true, () => true));
        Assert.False(ScenarioA1VictoryCalculator.NothingLeftOpen(true, 0, true, 0, false));
        Assert.True(ScenarioA1VictoryCalculator.NothingLeftOpen(true, 0, false, 0, false));
    }

    // The Victory Conditions over a reader.

    private sealed class Reader(IReadOnlyList<IReadOnlyList<VictoryUnitFacts>> states) : IVictoryStateReader
    {
        public int Count => states.Count;

        public bool SetupClosed(int state) => state > 0;

        public IEnumerable<(string Gun, string HolderSide)> GunHolders(int state) => [];

        public IEnumerable<(string Building, string Side, IReadOnlyList<string> Locations)> NewlySecured(int state) => [];

        public IReadOnlyList<VictoryUnitFacts> Units(int state) => states[state];

        public IReadOnlyList<VictoryStandingUnitFacts> Standing(int state) => [.. states[state].Select(unit => new VictoryStandingUnitFacts(unit.Id, unit.Side, unit.Dummy, VictoryStatus.Active, unit.Captured, unit.Vehicle, () => 2, null))];

        public IReadOnlyList<VictoryGunFacts> Guns(int state) => [];

        public IReadOnlyList<VictoryExitFacts> Exits(int state) => [];
    }

    private static VictoryUnitFacts Unit(string id, string side, string hex, string kind = "asl:squad", bool broken = false, bool concealed = false) =>
        new(id, side, kind, false, hex, 0, false, false, broken, false, false, false, false, false, null, false, false, concealed, false);

    private static VictoryCardFacts VictoryCard() => new(["russian", "german"],
        [("russian", false, [new CardConditionFacts("control-margin", "russian", ["b"], ["b"], 1, null, null, null, null, null)])], "german", _ => null,
        [("german", [new SetupAreaFacts("b", "building", ["M9"], null, null, null, null, null, null, null, null)])], 1, "bd01",
        id => id == "b" ? [new VictoryHexFacts("bd01:M9", "bd01", "M9", 9)] : null, _ => [0, 1]);

    [Fact]
    public void ControlFoldsStateByStateAndStoresTheFoldWhereAsked()
    {
        var card = VictoryCard();
        var stored = new List<int>();
        var fold = new VictoryFoldFacts(null, null, 0, 2, (count, control, holders) => stored.Add(count));
        var verdict = ScenarioA1VictoryCalculator.Evaluate(card, new Reader([[Unit("g", "german", "bd01:M9")], [Unit("r", "russian", "bd01:M9")], [Unit("r", "russian", "bd01:M9", broken: true)]]), false, null, fold)!;
        Assert.Equal([2], stored);
        Assert.Equal(["b", "bd01:M9:0", "bd01:M9:1"], verdict.Control.Select(item => item.Id));
        Assert.Equal("russian", verdict.Control[0].Side);
        Assert.Equal("russian", verdict.AtEnd.Winner);
        Assert.Equal("russian Controls 1 of b and german 0 of b, a margin of at least 1 (A26.14)", verdict.AtEnd.Reason);
        Assert.Null(verdict.Immediate);
        // The Russian squad at ground level takes the building and its ground Location; level 1 stays German (ruling R24.7).
        Assert.Equal(["building b: russian", "Location bd01:M9 level 1 of building b: german", "russian: 0 CVP, 0 Exit VP, 0 unbroken squad-equivalents",
            "german: 0 CVP, 0 Exit VP, 0 unbroken squad-equivalents"], verdict.AtEnd.Facts);
        Assert.Equal("german", ScenarioA1VictoryCalculator.StartSide(card, [new VictoryHexFacts("bd01:M9", "bd01", "M9", 9)]));
        // No setup area holds N9; the German side alone sets up on its board.
        Assert.Equal("german", ScenarioA1VictoryCalculator.StartSide(card, [new VictoryHexFacts("bd01:N9", "bd01", "N9", 9)]));
        Assert.Null(ScenarioA1VictoryCalculator.StartSide(card, [new VictoryHexFacts("bd02:N9", "bd02", "N9", 9)]));
        // Read for the German side, a concealed Russian neither gains Control nor counts.
        var known = ScenarioA1VictoryCalculator.Evaluate(card, new Reader([[Unit("g", "german", "bd01:M9")], [Unit("r", "russian", "bd01:M9", concealed: true)]]), false, "german", new VictoryFoldFacts(null, null, 0, 0, null))!;
        Assert.Equal("german", known.Control[0].Side);
        Assert.Equal(0, known.Sides[0].UnbrokenSquads);
        Assert.Equal(0.5, ScenarioA1VictoryCalculator.UnbrokenSquads([Unit("c", "russian", "x", "asl:crew")], "russian", null));
        Assert.True(ScenarioA1VictoryCalculator.EscortEdge("top", null, "russian", "top"));
        Assert.Equal("bd01:M9 level 1", ScenarioA1VictoryCalculator.LocationName("bd01:M9", 1));
        Assert.Equal("bd01:M9 cellar", ScenarioA1VictoryCalculator.LocationName("bd01:M9", -1));
    }

    // Concealment at setup.

    private sealed class NonObReader(IReadOnlyList<NonObUnitFacts> units, Func<int, int?> distance, Func<int, NonObLosFacts?> los) : INonObConcealmentReader
    {
        public IReadOnlyList<NonObUnitFacts> Units => units;

        public int? Distance(int location) => distance(location);

        public NonObLosFacts? Los(int location) => los(location);
    }

    [Fact]
    public void TheNonObScanReadsTheLosOnlyWithinSixteenHexesAndStopsAtTheFirstSeeing()
    {
        var reads = new List<string>();
        var reader = new NonObReader([new NonObUnitFacts(true, true, false, false, false, false, 0), new NonObUnitFacts(true, true, false, true, false, false, 1), new NonObUnitFacts(true, true, false, false, false, false, 2),
            new NonObUnitFacts(true, true, false, false, false, false, 3)],
            at => { reads.Add($"d{at}"); return at == 0 ? 20 : 5; }, at => { reads.Add($"los{at}"); return at == 2 ? new NonObLosFacts(true, false, 5) : new NonObLosFacts(false, true, 5); });
        var bar = ScenarioA1Concealment.NonObLocationBar([(false, false, false, false, false)], "russian", "bd01:A1:0", reader);
        Assert.Equal("play.non-ob-concealment: bd01:A1:0 is in the LOS of an unbroken enemy ground unit within 16 hexes (A12.12; ruling R23.6)", bar);
        Assert.Equal(["d0", "d2", "los2", "d3", "los3"], reads);
        Assert.StartsWith("play.non-ob-concealment: russian has no unit at bd01:A1:0", ScenarioA1Concealment.NonObLocationBar([], "russian", "bd01:A1:0", reader));
        Assert.StartsWith("play.non-ob-concealment: a unit of russian at bd01:A1:0 is broken or berserk", ScenarioA1Concealment.NonObLocationBar([(true, false, false, false, false)], "russian", "bd01:A1:0", reader));
        Assert.StartsWith("play.non-ob-concealment: bd01:A1:0 already holds", ScenarioA1Concealment.NonObLocationBar([(false, false, false, true, false)], "russian", "bd01:A1:0", reader));
        Assert.Equal(["bd01:A1:0", "bd01:B2:0"], ScenarioA1Concealment.NonObConcealment(() => false, () => ["bd01:B2:0", "bd01:C3:0", "bd01:A1:0"], at => at == 1));
        Assert.Empty(ScenarioA1Concealment.NonObConcealment(() => true, () => throw new InvalidOperationException(), _ => false));
        Assert.StartsWith("play.non-ob-concealment: play has started", ScenarioA1Concealment.NonObConcealmentBar(true, () => throw new InvalidOperationException()));
        Assert.StartsWith("play.non-ob-concealment: a non-OB \"?\" is placed only after both sides have set up", ScenarioA1Concealment.NonObConcealmentBar(false, () => true));
        Assert.Equal("play.non-ob-concealment: the russian side places 1 unit under \"?\" (A12.12; ruling R23.6)", ScenarioA1Concealment.NonObConcealmentText("russian", ["e1"]));
        Assert.Equal([(UnitCondition.Hidden, false), (UnitCondition.Concealed, true)], ScenarioA1Concealment.PlaceHiddenConditions());
        Assert.StartsWith("play.place-hidden: the units are hidden units of one side", ScenarioA1Concealment.PlaceHiddenUnitsBar(2, false, 2));
        Assert.Equal("bd01:A1:0=1?;bd01:B2:0=0?sq/3+1", ScenarioA1Concealment.PlanLook([("bd01:B2:0", false, false, "sq", "3"), ("bd01:A1:0", true, false, "sq", null), ("bd01:B2:0", false, false, "hs", null)]));
        var verdict = ScenarioA1Concealment.SetupConceal();
        Assert.Equal([(UnitCondition.Concealed, true)], verdict.Conditions);
        Assert.True(verdict.RecordNonOb);
    }
}
