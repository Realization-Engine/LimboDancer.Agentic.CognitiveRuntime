"""Pass 40 task 40.7: the Rules tests and the Play test, and the reworded AFPh refusal. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"

# The refusal reads better with the weapon first.
path = "LimboDancer.Domains.Asl.Rules/ScenarioA1FireEligibility.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
old = "$\"play.fire-weapon-moved: {weapon.Id} is a MMG or HMG that moved in the MPh, so it does not fire in the AFPh (A4.41)\""
new = "$\"play.fire-weapon-moved: {weapon.Id} moved in the MPh, and a MMG or HMG that moved does not fire in the AFPh (A4.41)\""
assert text.count(old) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(old, new))

PLAY_TEST = '''    [Fact]
    public async Task PortageIsNotRecoupedByADropAndNoItemIsPortagedTwiceInAPhase()
    {
        // Pass 40, task 40.7 (A4.4, A4.42 and its example, p. 50): a squad with a MMG (4 PP) has three MF. It carries the MMG one hex and drops
        // it; the MF do not come back, so after two more MF it has none. The squad that Recovers the MMG may not carry it on in this phase.
        await SetupAt(2, "german", Unit("g1", "attacker-squad", "C3", "german"), Mg("gm", "attacker-mmg", "g1", "german"), Unit("g2", "attacker-squad", "C4", "german"));
        Committed(await Do(GameActions.Move, NoRoll(), new { unitIds = G1, to = L("C4") }));
        Assert.Equal([("g1", "gm")], Current.PortagedThisPhase);
        Committed(await Do(GameActions.Drop, NoRoll(), new { unitId = "g1", equipmentId = "gm" }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new { }));
        Committed(await Do(GameActions.Move, NoRoll(), new { unitIds = G1, to = L("C5") }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new { }));
        Committed(await Do(GameActions.Move, NoRoll(), new { unitIds = G1, to = L("C6") }));
        Committed(await Do(GameActions.PassFire, NoRoll(), new { }));
        Refused(await Do(GameActions.Move, NoRoll(), new { unitIds = G1, to = L("C7") }), "0 MF left");
        Committed(await Do(GameActions.EndMove, NoRoll(), new { }));
        Committed(await Do(GameActions.Recover, Once(2), new { unitId = "g2", equipmentId = "gm" }));
        Refused(await Do(GameActions.Move, NoRoll(), new { unitIds = G2, to = L("C5") }), "play.move-portage");
        Assert.Contains("gm", Current.MovedWeapons);
    }

    [Fact]
    public async Task ABrokenEnemyDoesNotMakeALocationOpenGround()'''
path = "tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass13Tests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task ABrokenEnemyDoesNotMakeALocationOpenGround()"
assert text.count(anchor) == 1
open(ROOT + path, "w", encoding="utf-8", newline="").write(text.replace(anchor, PLAY_TEST))

RULES_TESTS = '''
    private static MovingUnitFacts Mover(string id, bool leader = false) =>
        new(id, "german", false, !leader, leader, false, false, false, "german", 0, false, null);

    [Fact]
    public void TheLeadersIpcGoesToTheLadenMmcTheMoverNames()
    {
        // A4.42 (p. 50; ruling R40.11): "one SMC can add its IPC to that of any one Good Order Infantry unit".
        MovingUnitFacts[] stack = [Mover("s1"), Mover("s2"), Mover("l", leader: true)];
        Assert.Null(ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, _ => true).Recipient);
        Assert.Equal(("s2", "l"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, _ => true, "s2"));
        Assert.Equal(("s1", "l"), ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0));
        Assert.Null(ScenarioA1MovementCalculator.LeaderIpcRecipient(stack, index => index == 0, "s2").Recipient);

        // The choice holds for the whole move, and names a laden MMC with the leader.
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar(null, "s1", "s1"));
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar("s1", "s1", "s1"));
        Assert.Null(ScenarioA1MovementCalculator.IpcChoiceBar("s1", null, "s1"));
        Assert.StartsWith("play.move-ipc:", ScenarioA1MovementCalculator.IpcChoiceBar("s2", "s1", "s1"), StringComparison.Ordinal);
        Assert.StartsWith("play.move-ipc:", ScenarioA1MovementCalculator.IpcChoiceBar("s2", null, null), StringComparison.Ordinal);
    }

    [Fact]
    public void AnItemIsPortagedOnceAPhaseAndItsCostIsNotRecouped()
    {
        // A4.4 (p. 50).
        var after = ScenarioA1MovementCalculator.Portaged([("g1", "mmg")], [("g1", "mmg"), ("g2", "lmg")]);
        Assert.Equal([("g1", "mmg"), ("g2", "lmg")], after);
        Assert.Equal([("g1", "mmg")], ScenarioA1MovementCalculator.Portaged([("g1", "mmg")], [("g2", "mmg")]));
        Assert.Null(ScenarioA1MovementCalculator.PortagedTwiceBar([("mmg", "g1", "g1"), ("lmg", "g2", null)]));
        Assert.StartsWith("play.move-portage:", ScenarioA1MovementCalculator.PortagedTwiceBar([("mmg", "g2", "g1")]), StringComparison.Ordinal);
    }

    [Fact]
    public void AssaultMovementAllowsMfSpentInsideALocationBesideItsOneChangeOfLocation()
    {
        // A4.61 and its EXC (p. 51): one change of Location; MF spent within a Location, before or after it, do not prevent Assault Movement.
        Assert.Null(ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: false, stepChangesLocation: true));
        Assert.Null(ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: true, stepChangesLocation: false));
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, true, true, false, null, [], currentChangedLocation: true, stepChangesLocation: true),
            StringComparison.Ordinal);
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, true, false, false, null, [], currentChangedLocation: false, stepChangesLocation: true),
            StringComparison.Ordinal);
        Assert.StartsWith("play.move-assault:", ScenarioA1MovementCalculator.MoveTiming(true, false, true, false, null, [], currentChangedLocation: false, stepChangesLocation: true),
            StringComparison.Ordinal);

        // "Which breaks, or becomes berserk ... is no longer considered using Assault Movement."
        Assert.True(ScenarioA1FireEligibility.AssaultHolds(true, [(false, false)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(true, [(true, false)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(true, [(false, false), (false, true)]));
        Assert.False(ScenarioA1FireEligibility.AssaultHolds(false, [(false, false)]));
    }

    [Fact]
    public void AMovedMmgOrHmgDoesNotFireInTheAfph()
    {
        // A4.41 (p. 50): a LMG may; the phase is the AFPh alone.
        Assert.Equal("medium", new ScenarioA1FirePackage().Reference.Definitions["attacker-mmg"].Size);
        Assert.StartsWith("play.fire-weapon-moved:", ScenarioA1FireEligibility.MovedWeaponBar("afph", [("mmg", true, "medium")]), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-weapon-moved:", ScenarioA1FireEligibility.MovedWeaponBar("afph", [("lmg", true, "light"), ("hmg", true, "heavy")]), StringComparison.Ordinal);
        Assert.Null(ScenarioA1FireEligibility.MovedWeaponBar("afph", [("lmg", true, "light"), ("mmg", false, "medium")]));
        Assert.Null(ScenarioA1FireEligibility.MovedWeaponBar("dfph", [("mmg", true, "medium")]));
    }

    [Fact]
    public void ABypassingUnitRecoversNothingAndAUnitThatMountsHasFourMf()
    {
        // A4.44 (p. 50); A4.11 (p. 48) and A4.5 (p. 51).
        Assert.StartsWith("play.recover-bypass:", ScenarioA1SupportWeaponRules.RecoveryBypassBar("g1", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1SupportWeaponRules.RecoveryBypassBar("g1", false));
        Assert.Equal(4, ScenarioA1PassengerCalculator.ConveyanceAllowance);
        Assert.StartsWith("play.load:", ScenarioA1PassengerCalculator.BoardingDoubleTimeBar("g1", 2), StringComparison.Ordinal);
        Assert.Null(ScenarioA1PassengerCalculator.BoardingDoubleTimeBar("g1", 0));
    }
}
'''
path = "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(ROOT + path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.3:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.7: the portage and movement corrections (A4.4, A4.41, A4.42, A4.44, A4.61; pp. 48 to 51). Task 40.3:")
open(ROOT + path, "w", encoding="utf-8", newline="").write(body)
print("ok")
