"""Pass 40 task 40.4: the Rules tests and the Play tests of the Infantry OVR, and the two existing tests it touches. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"


def apply(path, edits):
    text = open(ROOT + path, encoding="utf-8", newline="").read()
    for old, new in edits:
        assert text.count(old) == 1, (path, old[:80], text.count(old))
        text = text.replace(old, new)
    open(ROOT + path, "w", encoding="utf-8", newline="").write(text)


apply("LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1SequenceRulesTests.cs", [
    ("Assert.StartsWith(\"play.cc-overrun-first: the berserk Infantry OVR in bd01:G4:0\"", "Assert.StartsWith(\"play.cc-overrun-first: the Infantry OVR in bd01:G4:0\""),
])
apply("LimboDancer.Domains.Asl.Play.Tests/PlayTests.cs", [
    ("                \"asl.game.mop-up\"],\n", "                \"asl.game.mop-up\", \"asl.game.infantry-overrun-ntc\", \"asl.game.overrun-evade\"],\n"),
])

PLAY_TESTS = '''    [Fact]
    public async Task AnInfantryOvrOfALoneSmcNeedsItsNtcCostsDoubleAndHasItsCcAtOnce()
    {
        // Pass 40, task 40.4 (A4.15, A4.152, p. 49): the squad passes its NTC, enters the lone leader's Location at double the MF, and after the
        // DEFENDER's window the CC comes before anything else. The leader falls, so nothing is held in Melee and the move may end.
        await Setup(Squad("g1", L("C5"), "german"), Unit("rl", "asl:leader", "defender-leader-7-0", L("C4"), "russian"));
        Refused(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4"), ["overrun"] = true }), "play.move-overrun");
        Refused(await Move(G1, L("C4")), "play.move-occupied");
        Committed(await Do(GameActions.InfantryOverrunNtc, Once(1, 1), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4") }));
        Assert.True(Assert.Single(Current.InfantryOverrunNtcs).Passed);
        Refused(await Do(GameActions.InfantryOverrunNtc, Once(1, 1), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4") }), "play.overrun-ntc");
        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4"), ["overrun"] = true }));
        Assert.Equal(At("C4"), Current.Movement!.InfantryOverrun);
        Assert.Equal(2, Current.Unit("g1")!.MfSpent);
        await Pass();
        Assert.Equal(At("C4"), Planner().BerserkOverrunPending(Current));
        Refused(await EndMove(), "play.cc-overrun-first");
        Committed(await Do(GameActions.CloseCombat, Once(1, 1, 6, 6), new
        {
            location = L("C4"),
            attacks = new[] { new { attackers = G1, defenders = Rl }, new { attackers = Rl, defenders = G1 } },
        }));
        Assert.Null(Planner().BerserkOverrunPending(Current));
        Assert.False(Is(Current.Unit("g1")!, Conditions.Melee));
        Committed(await EndMove());
        NoReplayErrors();
    }

    [Fact]
    public async Task TheSmcMovesAwayFromAnInfantryOvrIntoALocationTheAttackerChooses()
    {
        // Pass 40, task 40.4 (A4.151, p. 49): before any Defensive First Fire the leader moves away; the ATTACKER names where; no CC is owed.
        await Setup(Squad("g1", L("C5"), "german"), Unit("rl", "asl:leader", "defender-leader-7-0", L("C4"), "russian"));
        Committed(await Do(GameActions.InfantryOverrunNtc, Once(1, 1), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4") }));
        Committed(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G1, ["to"] = L("C4"), ["overrun"] = true }));
        Assert.Equal("rl", Planner().OverrunEvader(Current, store.Read(Scope)!.Events));
        Committed(await Do(GameActions.OverrunEvade, NoRoll(), new Dictionary<string, object> { ["unitId"] = "rl" }));
        var choice = Current.Choice!;
        Assert.Equal(ChoicePending.OverrunEvade, choice.Kind);
        Assert.Equal("german", choice.Side);
        Assert.NotEmpty(choice.Options);
        Assert.DoesNotContain(L("C5"), choice.Options);
        Refused(await EndMove(), "play.choice");
        Committed(await Do(GameActions.Choose, NoRoll(), new Dictionary<string, object> { ["key"] = choice.Key, ["option"] = choice.Options[0] }));
        Assert.Equal(BoardLocation.Parse(choice.Options[0]), Current.Location("rl")!.Location);
        Assert.Null(Planner().OverrunEvader(Current, store.Read(Scope)!.Events));
        await Pass();
        Assert.Null(Planner().BerserkOverrunPending(Current));
        Committed(await EndMove());
        NoReplayErrors();
    }

    [Fact]
    public async Task ALeadersFailedNtcEndsHisStacksMphAndAMmcsOwnOnlyBarsItsOvr()
    {
        // Pass 40, task 40.4 (A4.15, p. 49): "if he fails, no unit in his stack may ... move or take any other action during that phase".
        await Setup(Squad("g1", L("C5"), "german"), Unit("l1", "asl:leader", "attacker-leader-8-0", L("C5"), "german"), Squad("g2", L("D5"), "german"),
            Unit("rl", "asl:leader", "defender-leader-7-0", L("C4"), "russian"));
        Committed(await Do(GameActions.InfantryOverrunNtc, Once(6, 6), new Dictionary<string, object> { ["unitIds"] = G1L1, ["to"] = L("C4") }));
        Assert.True(Current.Unit("g1")!.MovementEnded);
        Assert.True(Current.Unit("l1")!.MovementEnded);

        // The squad alone fails its own: no OVR there for it, and it may still move.
        Committed(await Do(GameActions.InfantryOverrunNtc, Once(6, 6), new Dictionary<string, object> { ["unitIds"] = G2, ["to"] = L("C4") }));
        Assert.False(Current.Unit("g2")!.MovementEnded);
        Refused(await Do(GameActions.Move, NoRoll(), new Dictionary<string, object> { ["unitIds"] = G2, ["to"] = L("C4"), ["overrun"] = true }), "play.move-overrun");
        Committed(await Move(G2, L("D4")));
        NoReplayErrors();
    }

    [Fact]
    public async Task ALoneBerserkLeaderEntersAnSmcsLocationWithoutAnOverrunCc()'''
path = "LimboDancer.Domains.Asl.Play.Tests/BacklogPass27Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task ALoneBerserkLeaderEntersAnSmcsLocationWithoutAnOverrunCc()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, PLAY_TESTS))

RULES_TESTS = '''
    private static InfantryOverrunMoverFacts OvrMover(string id, bool mmc = true, bool leader = false, bool goodOrder = true, bool berserk = false) =>
        new(id, mmc, leader, goodOrder, berserk, false, false);

    private static InfantryOverrunDefenderFacts OvrDefender(string id, bool known = true, bool smc = true, bool melee = false, bool unarmed = false, bool vehicle = false) =>
        new(id, known, smc, melee, unarmed, vehicle);

    [Fact]
    public void AnInfantryOvrsNtcIsItsLeadersForTheStackOrItsOneMmcs()
    {
        // A4.15 (p. 49).
        Assert.Equal("s1", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1")]).Taker);
        Assert.Equal("l", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1"), OvrMover("s2"), OvrMover("l", mmc: false, leader: true)]).Taker);
        Assert.Contains("name one MMC at a time", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1"), OvrMover("s2")]).Refusal, StringComparison.Ordinal);
        Assert.Contains("a SMC alone makes none", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("l", mmc: false, leader: true)]).Refusal, StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1", berserk: true)]).Refusal, StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1InfantryOverrun.NtcTaker([OvrMover("s1", goodOrder: false)]).Refusal);

        // The DRM is the TEM of the enemy's Location, and a hexside's when the LOS crosses it; the check passes at or below the Morale Level.
        Assert.Equal(3, ScenarioA1InfantryOverrun.NtcDrm(2, 1));
        Assert.Equal(1, ScenarioA1InfantryOverrun.NtcDrm(1, 0));
        Assert.True(ScenarioA1InfantryOverrun.NtcPassed(3, 3, 1, 7));
        Assert.False(ScenarioA1InfantryOverrun.NtcPassed(3, 4, 1, 7));
        Assert.True(ScenarioA1InfantryOverrun.EndsStackMove(true, false));
        Assert.False(ScenarioA1InfantryOverrun.EndsStackMove(false, false));
        Assert.False(ScenarioA1InfantryOverrun.EndsStackMove(true, true));
        Assert.StartsWith("play.overrun-ntc:", ScenarioA1InfantryOverrun.AlreadyTakenBar(true, "x"), StringComparison.Ordinal);
        Assert.Null(ScenarioA1InfantryOverrun.AlreadyTakenBar(false, "x"));
    }

    [Fact]
    public void AnInfantryOvrEntersOnlyALoneKnownSmcsLocationOnAPassedNtcAtDoubleTheMf()
    {
        // A4.15 (p. 49): "a Location containing only one Known enemy SMC (unless that SMC occupies an AFV)".
        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l")], "x"));
        Assert.Null(ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l"), OvrDefender("hidden", known: false, smc: false)], "x"));
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("s", smc: false)], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l"), OvrDefender("l2")], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([OvrDefender("l", melee: true)], "x"), StringComparison.Ordinal);
        Assert.StartsWith("play.overrun-target:", ScenarioA1InfantryOverrun.TargetBar([], "x"), StringComparison.Ordinal);

        Assert.Null(ScenarioA1InfantryOverrun.EntryBar(true, false, false, false, false, false, "x"));
        Assert.Contains("first passes its NTC", ScenarioA1InfantryOverrun.EntryBar(false, false, false, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("was failed", ScenarioA1InfantryOverrun.EntryBar(false, true, false, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("Minimum Move", ScenarioA1InfantryOverrun.EntryBar(true, false, true, false, false, false, "x"), StringComparison.Ordinal);
        Assert.Contains("berserk", ScenarioA1InfantryOverrun.EntryBar(true, false, false, false, false, true, "x"), StringComparison.Ordinal);
        Assert.Equal(8, ScenarioA1InfantryOverrun.EntryHalfMf(4));

        // A4.14 with the OVR: the Known SMC is no bar and is not drawn for a reveal; a concealed unit there still is (A12.15).
        UnitAtTargetFacts[] there = [new("l", true, "russian", false, true, false), new("q", true, "russian", false, false, false)];
        var barred = ScenarioA1MovementCalculator.EnemiesAtTarget(there, "german", false, false, false, false, false, "g1", "x");
        Assert.StartsWith("play.move-occupied:", barred.Refusal, StringComparison.Ordinal);
        var entered = ScenarioA1MovementCalculator.EnemiesAtTarget(there, "german", false, false, false, false, false, "g1", "x", knownEntered: true);
        Assert.Null(entered.Refusal);
        Assert.Equal(["q"], entered.EnemiesThere);
    }

    [Fact]
    public void UnarmedUnitsAreEnteredAndTheCcOfAnEntryIsOwedAtOnce()
    {
        // A20.54 (p. 88): "Unarmed units are not an obstacle to movement". A4.152, A20.54: the CC at once.
        Assert.True(ScenarioA1InfantryOverrun.OnlyUnarmed([OvrDefender("u", smc: false, unarmed: true)]));
        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([OvrDefender("u", smc: false, unarmed: true), OvrDefender("s", smc: false)]));
        Assert.False(ScenarioA1InfantryOverrun.OnlyUnarmed([]));
        Assert.True(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l")]));
        Assert.True(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("u", smc: false, unarmed: true), OvrDefender("u2", smc: false, unarmed: true)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(false, [OvrDefender("l")]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l", melee: true)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, [OvrDefender("l"), OvrDefender("q", known: false)]));
        Assert.False(ScenarioA1InfantryOverrun.CloseCombatOwed(true, []));
        var attacks = ScenarioA1VehicleCloseCombatRules.EntryAttacks(["g1", "g2"], ["l"], ["l"]);
        Assert.Equal(2, attacks.Count);
        Assert.Equal(["g1", "g2"], attacks[0].Attackers);
        Assert.Equal(["l"], attacks[1].Attackers);
    }

    [Fact]
    public void TheSmcMayMoveAwayBeforeAnyFireIntoALocationTheAttackerNames()
    {
        // A4.151 (p. 49).
        Assert.Null(ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, false, false, false, 2));
        Assert.Contains("before any Defensive First Fire", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, true, true, true, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("broken, pinned", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, true, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("broken, pinned", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, false, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("no adjacent Accessible Location", ScenarioA1InfantryOverrun.EvadeBar("l", true, true, false, true, true, false, false, false, 0), StringComparison.Ordinal);
        Assert.Contains("is not a SMC", ScenarioA1InfantryOverrun.EvadeBar("s", true, true, false, false, true, false, false, false, 2), StringComparison.Ordinal);
        Assert.Contains("is not a SMC", ScenarioA1InfantryOverrun.EvadeBar("l", false, true, false, true, true, false, false, false, 2), StringComparison.Ordinal);

        // "The ATTACKER may not force the SMC into ... [an] Open Ground hex if an alternate choice is available."
        Assert.Equal(["woods"], ScenarioA1InfantryOverrun.EvadeDestinations([("open", true), ("woods", false)]));
        Assert.Equal(["open", "open2"], ScenarioA1InfantryOverrun.EvadeDestinations([("open", true), ("open2", true)]));
        Assert.Empty(ScenarioA1InfantryOverrun.EvadeDestinations([]));
    }
}
'''
path = "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.7:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.4: Infantry OVR and the recapture of Unarmed units by entry (A4.15 to A4.152, A20.54; pp. 49, 88). Task 40.7:")
open(ROOT + path, "w", encoding="utf-8", newline="").write(body)
print("ok")
