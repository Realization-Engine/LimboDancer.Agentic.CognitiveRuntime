using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Passes 32.h and 32.i: night, weather, Starshells, and Snipers (S9), and the sequence of play with the A1 entry (S2), read directly in Rules: the SSR bar,
/// the weather's MF and MP, NVR and night sight, the Wind Change, the Starshell's bars and drift, the Sniper's trigger, target, candidates, and effects, who
/// ends a phase and owns a proposal, the request bars with their lazy reads, the next phase, the game's start and Balance, the entry's route and facts, and
/// the projector's markers, Stuns, lineage, and attempt verdicts.
/// </summary>
public sealed class ScenarioA1SequenceRulesTests
{
    // S9: night and weather.

    [Fact]
    public void TheSsrBarRefusesInItsOrder()
    {
        static bool Hip(string rule, string side) => true;
        Assert.StartsWith("play.hip-rule:", ScenarioA1NightAndWeather.RulesBar(["hip:russian"], Hip, true, true));
        Assert.StartsWith("play.weather-rule: 'weather:fog'", ScenarioA1NightAndWeather.RulesBar(["weather:fog"], Hip, true, true));
        Assert.Contains("do not combine", ScenarioA1NightAndWeather.RulesBar(["weather:mud", "weather:ground-snow"], Hip, true, true));
        Assert.Contains("Snow Chart", ScenarioA1NightAndWeather.RulesBar(["weather:extreme-winter"], Hip, true, true));
        Assert.Contains("month and year", ScenarioA1NightAndWeather.RulesBar(["weather:extreme-winter", "weather:ground-snow"], Hip, false, true));
        Assert.StartsWith("play.night-rule: one SSR", ScenarioA1NightAndWeather.RulesBar(["night:7"], Hip, true, true));
        Assert.Null(ScenarioA1NightAndWeather.RulesBar(["night:7", "weather:ground-snow"], Hip, true, true));
        Assert.Contains("NVR Table", ScenarioA1NightAndWeather.RulesBar(["night:3", "night-moon:new"], Hip, true, true));
        Assert.Equal("play.night-rule: a sky SSR needs 'night:n' too (E1.1)", ScenarioA1NightAndWeather.RulesBar(["night-clouds:scattered"], Hip, true, true));
        Assert.Null(ScenarioA1NightAndWeather.RulesBar(["night:3", "night-clouds:scattered", "weather:rain"], Hip, true, true));
    }

    [Fact]
    public void TheWeatherAddsHalfMfAndTakesTheRoadRate()
    {
        Assert.Equal((2, false), ScenarioA1NightAndWeather.InfantryWeatherHalfMf(night: true, rain: false, mud: false, groundSnow: false, deepSnow: false, plowedRoads: false, month: 7,
            pavedRoadCrossed: false, terrain: "woods", road: false, rise: 0));
        Assert.Equal((0, true), ScenarioA1NightAndWeather.InfantryWeatherHalfMf(true, false, false, false, false, false, 7, false, "woods", road: true, 0));
        Assert.Equal((1, false), ScenarioA1NightAndWeather.InfantryWeatherHalfMf(false, false, mud: true, false, false, false, 7, false, "open-ground", false, 0));
        Assert.Equal((2, false), ScenarioA1NightAndWeather.InfantryWeatherHalfMf(false, rain: true, false, false, false, false, 7, false, "open-ground", false, rise: 1));
        Assert.Equal((3, false), ScenarioA1NightAndWeather.InfantryWeatherHalfMf(false, false, false, false, deepSnow: true, false, 7, false, "open-ground", false, rise: 1));
        Assert.Equal(4, ScenarioA1NightAndWeather.VehicleWeatherHalfMp(night: true, rain: false, mud: false, groundSnow: false, deepSnow: true, type: "wheeled", terrain: "open-ground",
            paved: false, plowed: true, rise: 0));
        Assert.Equal(2, ScenarioA1NightAndWeather.VehicleWeatherHalfMp(false, false, false, false, true, "fully-tracked", "woods", false, false, 0));
    }

    [Fact]
    public void NightSightReadsInTheOldOrder()
    {
        var reads = new List<string>();
        Assert.Equal(new NightSightVerdict(false, null), ScenarioA1NightAndWeather.NightSight(null, "A", "B", 3, () => true, () => true, () => true, []));
        Assert.Empty(reads);
        var verdict = ScenarioA1NightAndWeather.NightSight(2, "A", "B", 3, () => { reads.Add("lit"); return false; }, () => { reads.Add("flash"); return false; },
            () => { reads.Add("from"); return true; }, []);
        Assert.Equal(["lit", "flash", "from"], reads);
        Assert.StartsWith("play.night-illuminated: A is Illuminated", verdict.Reason);
        Assert.Equal(new NightSightVerdict(true, null), ScenarioA1NightAndWeather.NightSight(2, "A", "B", 5, () => false, () => true, () => false, []));
        Assert.Equal(new NightSightVerdict(false, null), ScenarioA1NightAndWeather.NightSight(2, "A", "B", 4, () => false, () => false, () => false, ["fully-tracked"]));
        Assert.StartsWith("play.night-nvr: B is 5 hexes away, beyond the NVR of 2", ScenarioA1NightAndWeather.NightSight(2, "A", "B", 5, () => false, () => false, () => false, ["wheeled"]).Reason);
        Assert.Equal(1, ScenarioA1NightAndWeather.NvrOf(3, buttonedUpVehicle: true));
        Assert.True(ScenarioA1NightAndWeather.Illuminated(true, [null, 3], []));
        Assert.False(ScenarioA1NightAndWeather.Illuminated(true, [4], [3]));
        Assert.True(ScenarioA1NightAndWeather.Gunflash([marker => false, marker => marker == UnitCondition.Melee]));
    }

    [Fact]
    public void ExtremeWinterCutsByNationAndDate()
    {
        Assert.Equal(1, ScenarioA1NightAndWeather.ExtremeWinterReduction(true, 1941, 3, "russian"));
        Assert.Null(ScenarioA1NightAndWeather.ExtremeWinterReduction(true, 1941, 4, "russian"));
        Assert.Equal(2, ScenarioA1NightAndWeather.ExtremeWinterReduction(true, 1942, 1, "german"));
        Assert.Null(ScenarioA1NightAndWeather.ExtremeWinterReduction(true, 1942, 1, "finnish"));
        Assert.Null(ScenarioA1NightAndWeather.ExtremeWinterReduction(false, 1941, 1, "russian"));
    }

    [Fact]
    public void TheWindChangeDrawsTheNvrDrOnlyUnderAScatteredMoon()
    {
        var drawn = 0;
        var verdict = ScenarioA1NightAndWeather.WindChange(6, 5, nvr: 3, starshellUsed: false, moonHalf: true, moonFull: false, scatteredClouds: true, snow: false, overcast: false,
            rain: false, heavyRain: false, fallingSnow: false, gusty: false, precipitation: null, () => { drawn++; return 6; });
        Assert.Equal(1, drawn);
        Assert.Equal(5, verdict.Nvr);
        Assert.Equal(["the Base NVR goes from 3 to 5"], verdict.Notes);
        var still = ScenarioA1NightAndWeather.WindChange(6, 2, 3, false, false, false, false, false, overcast: true, false, false, false, gusty: true, null, () => throw new InvalidOperationException());
        Assert.Equal(2, still.Nvr);
        Assert.Null(still.Precipitation);
        Assert.False(still.Gust);
        var heavy = ScenarioA1NightAndWeather.WindChange(5, 5, null, false, false, false, false, false, overcast: true, false, false, false, gusty: true, "rain", () => 1);
        Assert.Equal("heavy-rain", heavy.Precipitation);
        Assert.True(heavy.Gust);
        Assert.Equal(["heavy rain falls", "a Gust blows"], heavy.Notes);
        Assert.Equal("play.wind-change: the Wind Change DR is 5 + 5 = 10: heavy rain falls; a Gust blows (B25.65, E1.12, E3.51)", ScenarioA1NightAndWeather.WindChangeText(5, 5, 10, heavy.Notes));
        Assert.True(ScenarioA1NightAndWeather.WindChangeDue("rph", 2, "german", "russian", false, false, false, null, true, false, false));
        Assert.False(ScenarioA1NightAndWeather.WindChangeDue("rph", 1, "russian", "russian", true, false, false, null, false, false, false));
        Assert.Equal("UNIT-STATE-043", ScenarioA1NightAndWeather.VerifyWindChange("rph", true, true, night: false, nvr: 2, null)?.Code);
    }

    // S9: Starshells and Snipers.

    [Fact]
    public void TheStarshellBarsReadTheScansOnlyBeforeTheFirst()
    {
        Assert.NotNull(ScenarioA1Starshells.FirerBar("g1", leader: false, afv: true, mmc: false, buttonedUp: true, stunned: false, shocked: false, goodOrder: true, pinned: false, ti: false, captured: false));
        Assert.Null(ScenarioA1Starshells.FirerBar("g1", true, false, false, false, false, false, true, false, false, false));
        Assert.Null(ScenarioA1Starshells.PhaseBar("mph", phasing: false));
        Assert.NotNull(ScenarioA1Starshells.PhaseBar("mph", phasing: true));
        var scanned = false;
        Assert.Null(ScenarioA1Starshells.FirstBar(true, () => { scanned = true; return false; }, () => false));
        Assert.False(scanned);
        Assert.NotNull(ScenarioA1Starshells.FirstBar(false, () => false, () => false));
        Assert.NotNull(ScenarioA1Starshells.TimingBar(true, "1|german", "2|german", leader: false, "pfph", acted: true));
        Assert.Null(ScenarioA1Starshells.TimingBar(true, "1|german", "2|german", leader: true, "pfph", acted: true));
        Assert.Equal(4, ScenarioA1Starshells.UsageNeed(true));
        Assert.Equal(1, ScenarioA1Starshells.PlacementExtent("own-hex", [3]));
        Assert.Equal(2, ScenarioA1Starshells.PlacementExtent("at-target", [1, 3]));
        Assert.Equal(5, ScenarioA1Starshells.PlacementExtent("three-hexes", [1, 5]));
        Assert.Equal("UNIT-STATE-044", ScenarioA1Starshells.VerifyStarshell(true, true, true, true, landed: true, starshellNamed: true, passed: false, attemptedFromHex: false)?.Code);
    }

    [Fact]
    public void TheSniperTriggersOnTheSanAndFindsItsTarget()
    {
        Assert.Equal(("leaderLoss", "g1"), ScenarioA1Sniper.RollKey("leaderLoss:g1#2"));
        Assert.Equal("german", ScenarioA1Sniper.RollMaker("attack", "german", () => throw new InvalidOperationException()));
        Assert.Equal("russian", ScenarioA1Sniper.RollMaker("checks", "german", () => ("russian", false)));
        Assert.Null(ScenarioA1Sniper.RollMaker("checks", "german", () => ("russian", true)));
        Assert.True(ScenarioA1Sniper.Triggers(7, 5, night: true));
        Assert.False(ScenarioA1Sniper.Triggers(8, 6, night: true));
        Assert.Equal(2, ScenarioA1Sniper.TargetHex([new SniperTargetHex(0, null, 0, "A1"), new SniperTargetHex(1, 2, 1, "C1"), new SniperTargetHex(2, 2, 1, "B1")]));
        Assert.Equal(1, ScenarioA1Sniper.TargetLocation([new SniperTargetLocation(0, 1, 0), new SniperTargetLocation(1, 2, 1)]));
        var candidates = ScenarioA1Sniper.Candidates([false, true, false, true]);
        Assert.Equal(3, candidates.Count);
        Assert.Equal([0], candidates[0]);
        Assert.Equal([2], candidates[1]);
        Assert.Equal([1, 3], candidates[2]);
        Assert.Equal([1, 2], ScenarioA1Sniper.Tied([2, 6, 6]));
        Assert.Equal(SniperEffect.CasualtyReduction, ScenarioA1Sniper.UnitEffect(smc: false, 1, broken: true, berserk: false, pinned: false));
        Assert.Equal(SniperEffect.Pinned, ScenarioA1Sniper.UnitEffect(false, 2, false, false, false));
        Assert.Equal(SniperEffect.WoundSeverity, ScenarioA1Sniper.UnitEffect(true, 2, false, false, false));
        Assert.True(ScenarioA1Sniper.Mortal(4, alreadyWounded: true));
        Assert.Equal([(UnitCondition.Broken, true), (UnitCondition.Pinned, false), (UnitCondition.DesperationMorale, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)],
            ScenarioA1Sniper.BrokenConditions());
    }

    // S2: the sequence of play.

    [Fact]
    public void WhoEndsAPhaseAndOwnsAProposal()
    {
        Assert.Equal("german", ScenarioA1SequenceCalculator.PhaseEndedBy("rph", "german", "russian", "russian"));
        Assert.Equal("russian", ScenarioA1SequenceCalculator.PhaseEndedBy("dfph", "german", "russian", "german"));
        Assert.Equal("german", ScenarioA1SequenceCalculator.PhaseEndedBy("mph", "russian", "russian", "german"));
        var owner = ScenarioA1SequenceCalculator.Owner("asl.game.choose", () => throw new InvalidOperationException(), "russian", () => "german", () => throw new InvalidOperationException());
        Assert.Equal(new ProposalOwner("german", "answers this choice"), owner);
        Assert.Equal("play.not-your-action: the german side answers this choice, not the russian side (ruling R31.6)", ScenarioA1SequenceCalculator.NotYourActionBar(owner, "russian"));
        Assert.Null(ScenarioA1SequenceCalculator.RoutPhaseEndBar("asl.game.advance-phase", "rph", () => throw new InvalidOperationException()));
        Assert.Contains("hand over to the german side first", ScenarioA1SequenceCalculator.RoutPhaseEndBar("asl.game.advance-phase", "rtph", () => "german"));
        Assert.Equal(["unitId", "equipmentId"], ScenarioA1SequenceCalculator.ActorArguments["asl.game.repair"]);
    }

    [Fact]
    public void TheRequestBarsReadOnlyWhenChecked()
    {
        var read = false;
        Assert.Null(ScenarioA1SequenceCalculator.SetupOnlyBar("asl.game.setup", true, () => { read = true; return "refused"; }));
        Assert.False(read);
        Assert.Equal("refused", ScenarioA1SequenceCalculator.SetupOnlyBar("asl.game.move", true, () => "refused"));
        Assert.Null(ScenarioA1SequenceCalculator.PassengerBar("asl.game.button-up", true, () => "aboard"));
        Assert.Equal("aboard", ScenarioA1SequenceCalculator.PassengerBar("asl.game.fire", true, () => "aboard"));
        Assert.Null(ScenarioA1SequenceCalculator.OverrunCloseCombatFirstBar("asl.game.close-combat", true, () => "bd01:G4:0"));
        Assert.StartsWith("play.cc-overrun-first: the berserk Infantry OVR in bd01:G4:0", ScenarioA1SequenceCalculator.OverrunCloseCombatFirstBar("asl.game.fire", true, () => "bd01:G4:0"));
        Assert.Equal("play.game-over: the game ended after Game Turn 6 (A3.9; ruling R20.1)", ScenarioA1SequenceCalculator.GameOverBar(6));
        Assert.Null(ScenarioA1SequenceCalculator.ChoicePendingBar(false, "asl.game.fire", () => ("german", "x")));
        Assert.Equal("play.choice-pending: the german side answers first: x", ScenarioA1SequenceCalculator.ChoicePendingBar(true, "asl.game.fire", () => ("german", "x")));
        Assert.False(ScenarioA1SequenceCalculator.FollowUpsApply("asl.game.setup"));
    }

    [Fact]
    public void TheNextPhaseAndTheGameTurn()
    {
        Assert.Equal((1, "pfph", "german"), ScenarioA1SequenceCalculator.NextPhase("rph", 1, "german", "russian", "german"));
        Assert.Equal((1, "rph", "russian"), ScenarioA1SequenceCalculator.NextPhase("ccph", 1, "german", "russian", "german"));
        Assert.Equal((2, "rph", "german"), ScenarioA1SequenceCalculator.NextPhase("ccph", 1, "russian", "german", "german"));
        Assert.True(ScenarioA1SequenceCalculator.LastPhase("ccph"));
        Assert.Equal("russian", ScenarioA1SequenceCalculator.BerserkMassacringSide("dfph", "german", "russian"));
        Assert.Null(ScenarioA1SequenceCalculator.BerserkMassacringSide("pfph", "german", "russian"));
        Assert.Equal(UnitCondition.FinalFire, ScenarioA1SequenceCalculator.MassacreMarker("dfph"));
        Assert.Equal((ChoiceKind.UnlikelyKill, "german"), ScenarioA1SequenceCalculator.PendingChoice("unlikelyKill", "russian", "german", "russian"));
        Assert.Equal((ChoiceKind.BattleHardening, "russian"), ScenarioA1SequenceCalculator.PendingChoice("battleHardening", null, "german", "russian"));
        Assert.True(ScenarioA1SequenceCalculator.BerserkReturns("mph", true, true, true, false, true, () => false));
        Assert.False(ScenarioA1SequenceCalculator.BerserkReturns("mph", true, true, true, false, true, () => null));
        Assert.Equal("last-game-turn", ScenarioA1SequenceCalculator.GameEndReason(true));
    }

    // S2: the game's start.

    [Fact]
    public void TheStartRollsRerollTiesAndTheBalanceNamesItsPlayers()
    {
        var attempts = new List<int>();
        Assert.Equal(1, ScenarioA1GameStart.Higher(attempt => { attempts.Add(attempt); return attempt < 3 ? [4, 4] : [2, 5]; }));
        Assert.Equal([1, 2, 3], attempts);
        Assert.Equal(0, ScenarioA1GameStart.Higher(_ => [3, 3]));
        var balance = ScenarioA1GameStart.Balance(["german", "russian"], null, [("Ann", "german"), ("Bo", "german")]);
        Assert.Null(balance.Reason);
        Assert.Equal("russian", balance.Balance);
        Assert.Equal(new BalanceRollFacts("german", "Ann", "Bo", "russian"), balance.Roll);
        Assert.Equal([("Bo", "german"), ("Ann", "russian")], ScenarioA1GameStart.BalancePlayers(false, balance.Roll!));
        Assert.StartsWith("play.balance: the Balance names two players", ScenarioA1GameStart.Balance(["german", "russian"], null, [("Ann", "german"), ("Ann", "russian")]).Reason);
        Assert.Equal("play.balance: 'finnish' is not a side of the card (A26.4; ruling R20.3)", ScenarioA1GameStart.Balance(["german", "russian"], "finnish", null).Reason);
        Assert.Equal("the second player", ScenarioA1GameStart.PlayerName("", 1));
        Assert.Equal("play.scenario: the card 'a1' has changed since it was read; read it again (ruling R18.2)",
            ScenarioA1GameStart.CardStartBar("a1", "x", "1.13.0", () => "y", () => throw new InvalidOperationException(), () => true, true, () => "note"));
        Assert.Equal("play.scenario: note The game rolls it as it starts, so the start names no winner (A3.9; ruling R20.2)",
            ScenarioA1GameStart.CardStartBar("a1", "x", "1.13.0", () => "x", () => true, () => true, true, () => "note"));
        Assert.True(ScenarioA1GameStart.NeedsGroup(false, "asl:squad", false, false, true, false));
        Assert.False(ScenarioA1GameStart.NeedsGroup(false, "asl:hero", false, false, true, false));
    }

    // S2: the A1 entry.

    [Fact]
    public void TheEntryRouteAndItsFacts()
    {
        var mmc = new EntryOccupantFacts(true, false, true, false, true, false, false);
        Assert.Equal(ScenarioA1EntryRoute.KnownEnemy, ScenarioA1EntryRules.Route([mmc]));
        Assert.Equal(ScenarioA1EntryRoute.Concealed, ScenarioA1EntryRules.Route([mmc with { Visible = false, Concealed = true }]));
        Assert.Equal(ScenarioA1EntryRoute.LoneSmc, ScenarioA1EntryRules.Route([new EntryOccupantFacts(true, false, false, true, false, true, false)]));
        Assert.Equal(ScenarioA1EntryRoute.Outside, ScenarioA1EntryRules.Route([new EntryOccupantFacts(true, false, false, true, false, false, true)]));
        Assert.Equal(ScenarioA1EntryRoute.RandomSelection, ScenarioA1EntryRules.Route([mmc with { Visible = false, Concealed = true }, mmc with { Visible = false, Hidden = true }]));
        Assert.Equal(ScenarioA1EntryRoute.Outside, ScenarioA1EntryRules.Route([mmc, new EntryOccupantFacts(false, false, false, false, true, false, false)]));
        Assert.True(ScenarioA1EntryRules.A414Exception(null, false, true));
        Assert.Null(ScenarioA1EntryRules.A414Exception(null, false, false));
        Assert.True(ScenarioA1EntryRules.VisibleTo(false, false, null));
        Assert.False(ScenarioA1EntryRules.VisibleTo(false, null, false));
        Assert.Equal(["b"], ScenarioA1EntryRules.SampleBranch([("a", false), ("b", true)]));
        Assert.Equal(["a", "c"], ScenarioA1EntryRules.SampleBranch([("a", false), ("c", false), ("d", false)]));
        var facts = ScenarioA1EntryRules.EntryFacts(new EntryFactInputs(true, true, false, false, true, false, false, null, false, false, true, true, 0, false, "Wooden Building", 0, true, true, true,
            true, true, true, 4, 1, 0));
        Assert.True(facts["isKnownGoodOrderInfantrySquad"]);
        Assert.Null(facts["canMoveThisPhase"]);
        Assert.True(facts["isAdjacentGroundLevelOrdinaryBuilding"]);
        Assert.True(facts["hasEnoughMovementFactors"]);
        Assert.Equal(["play.fact-unknown: canMoveThisPhase"], ScenarioA1EntryRules.MoverReasons(facts));
        Assert.Equal(3, ScenarioA1EntryRules.NtcTem("stone"));
        Assert.Equal("A1-ovr-ntc-mf-insufficient", ScenarioA1EntryRules.Election(3).CaseId);
        Assert.Equal([ScenarioA1EntryRules.ResolvedOnConfirmation], ScenarioA1EntryRules.ReasonsForMover(true, ["x"], [], false, true));
    }

    // S2: the projector.

    [Fact]
    public void TheProjectorsMarkersStunsLineageAndAttempts()
    {
        Assert.Equal([UnitCondition.IntensiveFire], ScenarioA1SequenceProjection.ClearedMarkers("dfph", night: true));
        Assert.Equal([UnitCondition.Pinned, UnitCondition.Ti], ScenarioA1SequenceProjection.ClearedMarkers("ccph", night: false));
        Assert.Equal([(UnitCondition.Stunned, false), (UnitCondition.StunRecovery, true)], ScenarioA1SequenceProjection.StunEnds(true, false, false, true).Conditions);
        Assert.Empty(ScenarioA1SequenceProjection.StunEnds(true, true, true, false).Conditions);
        Assert.Equal((1, 2), ScenarioA1SequenceProjection.LineageCounts("Deployed"));
        Assert.Equal("UNIT-STATE-013", ScenarioA1SequenceProjection.LineageCountRefusal("Recombined", 1, 1)?.Code);
        Assert.Equal(("asl:half-squad", "asl:squad"), ScenarioA1SequenceProjection.LineageKinds("Recombined"));
        Assert.True(ScenarioA1SequenceProjection.LineageKeepsPossessions("Reduced"));
        Assert.Equal("g", ScenarioA1SequenceProjection.InheritedGroup(null, [new GroupSourceFacts("h", 4), new GroupSourceFacts("g", 2)]));
        Assert.Equal("'g1' can attempt an entry only in the MPh of its own side.", ScenarioA1SequenceProjection.AttemptRefusal(true, "pfph", true, false, false, 2, "g1")?.Text);
        Assert.Equal("UNIT-STATE-010", ScenarioA1SequenceProjection.AttemptRefusal(true, "mph", true, false, false, -1, "g1")?.Code);
        Assert.Null(ScenarioA1SequenceProjection.AttemptRefusal(true, "mph", true, false, false, 2, "g1"));
        Assert.Equal(["b", "c"], ScenarioA1SequenceProjection.Revealing(["a", "b", "c"], [1, 5, 5]));
        Assert.Equal("UNIT-STATE-045", ScenarioA1SequenceProjection.EndRefusal(2, 2, 0, false, 1, false)?.Code);
        Assert.True(ScenarioA1SequenceProjection.HungariansVersusRomanians(["hungarian", "romanian"]));
        Assert.Null(ScenarioA1SequenceProjection.ChoicesMismatch(new Dictionary<string, string> { ["k"] = "take" }, new Dictionary<string, string> { ["k"] = "take" }));
        Assert.Contains("declares the choices k=take, but the choosing sides answered none", ScenarioA1SequenceProjection.ChoicesMismatch(new Dictionary<string, string> { ["k"] = "take" }, new Dictionary<string, string>()));
    }
}
