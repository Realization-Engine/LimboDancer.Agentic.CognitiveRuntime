"""Pass 40 task 40.9: the Rules tests and the Play tests of the small rows. Exact anchors, asserted once each."""
ROOT = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/"

RULES_TESTS = '''
    [Fact]
    public void GuardsAndUnarmedUnitsNeedNoLeaderAndAGuardSquadDeploysAtWill()
    {
        // A1.31, A1.32 (p. 45); A20.5 (p. 88).
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(true, false));
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, true));
        Assert.False(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false));
        Assert.True(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, true, false));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, true, true));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(false, true, false));
        Assert.False(ScenarioA1SupportWeaponRules.GuardDeploysAtWill(true, false, false));

        // A1.32: "at the start of any RPh".
        Assert.Null(ScenarioA1SupportWeaponRules.RecombineStartBar(false));
        Assert.StartsWith("play.recombine-start:", ScenarioA1SupportWeaponRules.RecombineStartBar(true), StringComparison.Ordinal);
    }

    [Fact]
    public void TheUnitSizeNumberIsOneMemberWithItsFourAndItsFive()
    {
        // A1.6 (p. 45).
        Assert.Equal(1, ScenarioA1Definitions.UnitSize(false, false));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize(false, true));
        Assert.Equal(3, ScenarioA1Definitions.UnitSize(true, false));
        Assert.Equal(4, ScenarioA1Definitions.UnitSize(false, false, fiveEighths: true));
        Assert.Equal(5, ScenarioA1Definitions.UnitSize(false, false, fiveEighths: true, large: true));
        Assert.Equal(3, ScenarioA1Definitions.UnitSize("asl:squad"));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:half-squad"));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:crew"));
        Assert.Equal(1, ScenarioA1Definitions.UnitSize("asl:leader"));
        Assert.Equal(3, ScenarioA1FireEligibility.GuardSize("asl:squad"));
    }

    [Fact]
    public void AButtedHalfHexIsInASetupAreaOnlyUnderBothBoardsNames()
    {
        // A2.3 (p. 45): two boards; the half-hex bd02:A1 is butted against bd04:GG10, and the later board, bd02, owns it.
        var card = new SetupCardFacts([], 2, null, false, null, null, null, null, []);
        SetupAreaFacts[] onFirst = [new("a", "hex-numbers", "bd04", 8, 10, null, null, null, null, null)];
        SetupAreaFacts[] onSecond = [new("b", "hex-numbers", "bd02", 1, 3, null, null, null, null, null)];
        SetupAreaFacts[] onBoth = [.. onFirst, .. onSecond];
        var full = new SetupLocationFacts("bd04:B9:0", "bd04", "B9", 9);
        var seam = new SetupLocationFacts("bd02:A1:0", "bd02", "A1", 1) { OtherHalf = new SetupLocationFacts("bd04:GG10:0", "bd04", "GG10", 10) };
        var edge = new SetupLocationFacts("bd02:A1:0", "bd02", "A1", 1);

        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onFirst, full));
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onSecond, full));

        // A unit limited to either board alone may not set up in the butted half-hex; one allowed on both may.
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onFirst, seam));
        Assert.False(ScenarioA1SetupCalculator.InAreas(card, onSecond, seam));
        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onBoth, seam));

        // "... or is not butted against another half-hex".
        Assert.True(ScenarioA1SetupCalculator.InAreas(card, onSecond, edge));
    }
}
'''
path = ROOT + "LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass40RulesTests.cs"
text = open(path, encoding="utf-8", newline="").read()
assert text.rstrip().endswith("}")
body = text.rstrip()[:-1].rstrip() + "\n" + RULES_TESTS
old_doc = "/// Pass 40, read directly in Rules. Task 40.5:"
assert body.count(old_doc) == 1
body = body.replace(old_doc, "/// Pass 40, read directly in Rules. Task 40.9: Deployment and Recombining without a leader, the US#, and a butted half-hex (A1.31, A1.32, A1.6, A2.3; p. 45). Task 40.5:")
open(path, "w", encoding="utf-8", newline="").write(body)

PLAY_TESTS = '''    [Fact]
    public async Task AnUnarmedSquadDeploysAndUnarmedHalfSquadsRecombineWithNoLeader()
    {
        // Pass 40, task 40.9 (A1.31, A1.32, p. 45): "unarmed units (20.5) ... do not require leader direction to Deploy", and "unarmed HS do not
        // need leader presence to Recombine". The armed squad beside them still needs its leader.
        await SetupAt(0, "german", Unit("gu", "attacker-squad", "C3", "german", "asl:unarmed"), Unit("g1", "attacker-squad", "C3", "german"),
            Unit("h1", "attacker-half-squad", "D3", "german", "asl:unarmed"), Unit("h2", "attacker-half-squad", "D3", "german", "asl:unarmed"));
        Committed(await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = H1H2,
            attemptId = "rec"
        }));
        Assert.Equal("asl:squad", Current.Unit("rec-h1")!.Kind);
        Refused(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "g1"
        }), "play.deploy-leader");
        Committed(await Do(GameActions.Deploy, Once(1, 2), new
        {
            squadId = "gu",
            attemptId = "dep"
        }));
        Assert.Equal("asl:half-squad", Current.Unit("dep-gu-1")!.Kind);
        Assert.True(Is(Current.Unit("dep-gu-1")!, Conditions.Unarmed));
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task HalfSquadsRecombineOnlyAtTheStartOfTheirSidesRph()
    {
        // Pass 40, task 40.9 (A1.32, p. 45): "at the start of any RPh": not once a unit of their side has attempted a Deployment in it.
        await SetupAt(0, "german", Unit("h1", "attacker-half-squad", "C3", "german"), Unit("h2", "attacker-half-squad", "C3", "german"),
            Unit("gl", "attacker-leader-8-1", "C3", "german"), Unit("g2", "attacker-squad", "D3", "german"), Unit("gl2", "attacker-leader-8-1", "D3", "german"));
        Committed(await Do(GameActions.Deploy, Once(6, 6), new
        {
            squadId = "g2",
            leader = "gl2"
        }));
        Refused(await Do(GameActions.Recombine, NoRoll(), new
        {
            halfSquads = H1H2,
            leader = "gl"
        }), "play.recombine-start");
    }

    [Fact]
    public async Task AGuardSquadDeploysAtWillInAnyPhaseWithNoLeaderAndNoNtc()
    {
        // Pass 40, task 40.9 (A20.5, p. 88): "An unbroken Guard squad can Deploy into HS automatically at any time, regardless of nationality or
        // leader requirements." The Russian squad guards a German HS; it Deploys in the German PFPh, and a HS of it keeps the prisoner.
        await SetupAt(1, "german", Unit("r1", "defender-squad", "C3", "russian"), Unit("g9", "attacker-half-squad", "C3", "german", "asl:captured"),
            Unit("r2", "defender-squad", "F3", "russian"));
        Assert.Equal("pfph", Current.Phase);
        Assert.Equal("r1", Current.Unit("g9")!.Custodian);
        Refused(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "r2"
        }), "play.deploy-phase");
        Committed(await Do(GameActions.Deploy, NoRoll(), new
        {
            squadId = "r1",
            attemptId = "dep"
        }));
        Assert.Equal("asl:half-squad", Current.Unit("dep-r1-1")!.Kind);
        Assert.Contains(Current.Unit("g9")!.Custodian, new[] { "dep-r1-1", "dep-r1-2" });
        Assert.False(Planner().Replay(store.Read(Scope)!.Events).HasErrors);
    }

    [Fact]
    public async Task TwoHalfSquadsRecombineWithTheirLeader()'''
path = ROOT + "LimboDancer.Domains.Asl.Play.Tests/BacklogPass13Tests.cs"
text = open(path, encoding="utf-8", newline="").read()
anchor = "    [Fact]\n    public async Task TwoHalfSquadsRecombineWithTheirLeader()"
assert text.count(anchor) == 1
open(path, "w", encoding="utf-8", newline="").write(text.replace(anchor, PLAY_TESTS))
print("ok")
