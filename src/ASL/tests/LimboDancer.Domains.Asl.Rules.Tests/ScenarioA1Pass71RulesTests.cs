using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 71, Infantry IV: Snipers and wounds. Increment 4a: the Sniper's targets and results (A14.21, A14.22, A14.3, A14.31, A14.33, D5.34, D5.341, D5.342,
/// A12.14; rulings R71.3 to R71.8), read in Rules through its public members.
/// </summary>
public sealed class ScenarioA1Pass71RulesTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private static readonly string[] OneButtoned = [ScenarioA1PrcRules.Buttoned];
    private static readonly string[] OneExposed = [ScenarioA1PrcRules.Exposed];
    private static readonly string[] OneOpen = [ScenarioA1PrcRules.Open];
    private static readonly string[] NoPrc = [];
    private static readonly int[] First = [0];

    private static SniperVehicleFacts Vehicle(IReadOnlyList<string> prc, bool unarmored = false, bool partiallyArmored = false, bool crewExposed = false, bool manned = true) =>
        new(unarmored, partiallyArmored, crewExposed, manned, prc);

    private static Func<bool> Never(string what) => () => throw new InvalidOperationException($"{what} is not read.");

    [Fact]
    public void AnAfvWithNoVulnerablePrcIsNoTargetAndACeOneOffersItsCrewAndItsExposedPassengers()
    {
        // A14.22 (p. 82; ruling R71.3): "All units are eligible Sniper targets except AFV with no Vulnerable PRC": a BU AFV's Passengers and crew are none.
        var buttoned = ScenarioA1Sniper.VehicleTargets(Vehicle(OneButtoned));
        Assert.Empty(buttoned.Prc);
        Assert.False(buttoned.Crew);
        Assert.False(buttoned.Vehicle);

        // A CE AFV: its crew and its CE Passengers.
        var exposed = ScenarioA1Sniper.VehicleTargets(Vehicle(OneExposed, crewExposed: true));
        Assert.Equal(First, exposed.Prc);
        Assert.True(exposed.Crew);
        Assert.False(exposed.Vehicle);

        // A Rider is always Vulnerable (D6.23), even on a BU AFV.
        Assert.Equal(First, ScenarioA1Sniper.VehicleTargets(Vehicle(OneOpen)).Prc);
    }

    [Fact]
    public void APartiallyArmoredAfvsPrcAndCrewAreVulnerableEvenWhenBu()
    {
        // A14.22: PRC "are Vulnerable to Sniper attack if they ... occupy a Partially Armored AFV"; A14.3 names its Inherent crew.
        var targets = ScenarioA1Sniper.VehicleTargets(Vehicle(OneButtoned, partiallyArmored: true));
        Assert.Equal(First, targets.Prc);
        Assert.True(targets.Crew);
        Assert.False(targets.Vehicle);
    }

    [Fact]
    public void AnUnarmoredVehicleIsATargetApartFromItsPrcButNotWhenEmpty()
    {
        // A14.22, A14.33 (rulings R71.3, R71.4): a manned truck offers its Passengers, its Inherent crew, and itself, each a separate target.
        var truck = ScenarioA1Sniper.VehicleTargets(Vehicle(OneOpen, unarmored: true));
        Assert.Equal(First, truck.Prc);
        Assert.True(truck.Crew);
        Assert.True(truck.Vehicle);

        // An Abandoned truck with a Passenger aboard is still a target; its crew is gone.
        var abandoned = ScenarioA1Sniper.VehicleTargets(Vehicle(OneOpen, unarmored: true, manned: false));
        Assert.False(abandoned.Crew);
        Assert.True(abandoned.Vehicle);

        // R71.4: an empty unarmored vehicle is no target.
        var empty = ScenarioA1Sniper.VehicleTargets(Vehicle(NoPrc, unarmored: true, manned: false));
        Assert.Empty(empty.Prc);
        Assert.False(empty.Crew);
        Assert.False(empty.Vehicle);
    }

    [Fact]
    public void WhoMayBeATargetAtAll()
    {
        // A14.22: prisoners and hidden units are no targets; a friendly unit neither.
        Assert.True(ScenarioA1Sniper.Eligible(true, true, hidden: false, captured: false, onMap: true));
        Assert.False(ScenarioA1Sniper.Eligible(true, true, hidden: true, captured: false, onMap: true));
        Assert.False(ScenarioA1Sniper.Eligible(true, true, hidden: false, captured: true, onMap: true));
        Assert.False(ScenarioA1Sniper.Eligible(true, attackedSide: false, false, false, true));
        Assert.False(ScenarioA1Sniper.Eligible(true, true, false, false, onMap: false));
    }

    [Fact]
    public void TheCatalogsVehiclesReadTheirPartiallyArmoredTrait()
    {
        // Ruling R71.3: the trait is a definition property, read like Unarmored; no vehicle of the scenario catalog has it.
        Assert.False(Ordnance.Fire.Definitions["attacker-tank"].PartiallyArmored);
        Assert.False(Ordnance.Fire.Definitions["attacker-truck"].PartiallyArmored);
        Assert.True(Ordnance.Fire.Definitions["attacker-truck"].Unarmored);
    }

    [Fact]
    public void ACeCrewOfAFullyArmoredAfvIsRecalledOnAOneAndStunnedOnATwo()
    {
        // A14.3, D5.34, D5.341 (pp. 82, 203, 204; ruling R71.5).
        Assert.Equal(ScenarioA1Sniper.Recalled, ScenarioA1Sniper.CrewResult(1, fullyArmored: true, stunRecovery: false, pinned: false));
        Assert.Equal(ScenarioA1Sniper.Stunned, ScenarioA1Sniper.CrewResult(2, fullyArmored: true, stunRecovery: false, pinned: false));

        // D5.342: "An AFV crew that suffers a second Stun result (e.g., while already under the effects of a '+1' counter) suffers Recall instead."
        Assert.Equal(ScenarioA1Sniper.Recalled, ScenarioA1Sniper.CrewResult(2, fullyArmored: true, stunRecovery: true, pinned: false));

        // The vehicle's conditions, as the Fire path writes them (D.7).
        Assert.Equal([(VehicleCondition.Stunned, true), (VehicleCondition.ButtonedUp, true), (VehicleCondition.Motion, false)], ScenarioA1Sniper.VehicleConditions(ScenarioA1Sniper.Stunned));
        Assert.Equal([(VehicleCondition.Recalled, true), (VehicleCondition.Stunned, false), (VehicleCondition.StunRecovery, false), (VehicleCondition.ButtonedUp, true),
            (VehicleCondition.Motion, false)], ScenarioA1Sniper.VehicleConditions(ScenarioA1Sniper.Recalled));
    }

    [Fact]
    public void AnUnprotectedInherentCrewBreaksOnAOneAndIsPinnedOnATwo()
    {
        // A14.3: "breaks Inherent crew of unarmored-vehicle/Partially-Armored-AFV" on dr 1; dr 2 pins it, with no effect on one already pinned.
        Assert.Equal(ScenarioA1Sniper.CrewBroken, ScenarioA1Sniper.CrewResult(1, fullyArmored: false, stunRecovery: false, pinned: false));
        Assert.Equal(ScenarioA1Sniper.CrewPinned, ScenarioA1Sniper.CrewResult(2, fullyArmored: false, stunRecovery: false, pinned: false));
        Assert.Equal(ScenarioA1Sniper.NoEffect, ScenarioA1Sniper.CrewResult(2, fullyArmored: false, stunRecovery: false, pinned: true));
        Assert.Equal([(VehicleCondition.Pinned, true)], ScenarioA1Sniper.VehicleConditions(ScenarioA1Sniper.CrewPinned));

        // The break is written by the Unprotected crew's events, not as a vehicle condition.
        Assert.Empty(ScenarioA1Sniper.VehicleConditions(ScenarioA1Sniper.CrewBroken));
    }

    [Fact]
    public void AnUnarmoredVehicleIsImmobilizedOnlyOnAOneAndOnlyOnce()
    {
        // A14.33 (p. 82): "can only immobilize it, and only if the Sniper dr were a '1'. There is no further effect vs an already-immobilized unarmored vehicle".
        Assert.Equal(ScenarioA1Sniper.Immobilized, ScenarioA1Sniper.VehicleResult(1, immobilized: false));
        Assert.Equal(ScenarioA1Sniper.NoEffect, ScenarioA1Sniper.VehicleResult(1, immobilized: true));
        Assert.Equal(ScenarioA1Sniper.NoEffect, ScenarioA1Sniper.VehicleResult(2, immobilized: false));
        Assert.Equal([(VehicleCondition.Immobilized, true), (VehicleCondition.Motion, false)], ScenarioA1Sniper.VehicleConditions(ScenarioA1Sniper.Immobilized));
    }

    [Fact]
    public void TheEnemySniperIsEliminatedOnAOnePinnedOnATwoAndCountsAsACandidateOfItsOwn()
    {
        // A14.3, A14.31 (ruling R71.6).
        Assert.Equal(ScenarioA1Sniper.SniperEliminated, ScenarioA1Sniper.SniperResult(1, pinned: false));
        Assert.Equal(ScenarioA1Sniper.SniperPinned, ScenarioA1Sniper.SniperResult(2, pinned: false));
        Assert.Equal(ScenarioA1Sniper.NoEffect, ScenarioA1Sniper.SniperResult(2, pinned: true));

        // A Sniper counter is never concealed: beside a concealed stack it is a candidate of its own (A14.23).
        var candidates = ScenarioA1Sniper.Candidates([false, true]);
        Assert.Equal(2, candidates.Count);
        Assert.Equal(First, candidates[0]);

        // A hex holding only the enemy Sniper counter is in the search with its distance like any other (A14.21).
        Assert.Equal(1, ScenarioA1Sniper.TargetHex([new SniperTargetHex(0, 3, 0, "E3"), new SniperTargetHex(1, 1, 0, "E7")]));
    }

    [Fact]
    public void SmokeInATargetsHexCountsInTheTemComparison()
    {
        // A14.21 (ruling R71.7): "the lowest (to a minimum of zero) in-hex TEM/SMOKE DRM": +2 a source, at most +3 (A24.2).
        Assert.Equal(0, ScenarioA1Sniper.TargetTem("open-ground", 0));
        Assert.Equal(2, ScenarioA1Sniper.TargetTem("open-ground", 1));
        Assert.Equal(4, ScenarioA1Sniper.TargetTem("woods", 2));

        // Two equidistant hexes: the woods (+1) is chosen over the Open Ground with a SMOKE grenade (+2).
        Assert.Equal(1, ScenarioA1Sniper.TargetHex([new SniperTargetHex(0, 2, ScenarioA1Sniper.TargetTem("open-ground", 1), "B3"),
            new SniperTargetHex(1, 2, ScenarioA1Sniper.TargetTem("woods", 0), "C3")]));
    }

    [Fact]
    public void AConcealedUnitAttackedByASniperKeepsItsQuestionMarkOnlyOutOfEveryGoodOrderEnemyLos()
    {
        // A12.14 (p. 77; ruling R71.8): a pin, or a dr of 1 or 2 with no effect, costs the "?" only in the LOS of a Good Order enemy ground unit.
        Assert.True(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.Pinned, () => false));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.Pinned, () => true));
        Assert.True(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.NoEffect, () => false));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.Stunned, () => true));

        // A break, a wound, or a Reduction costs it "even if out of the LOS of all enemy units": the LOS is not read.
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.Broken, Never("the LOS")));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.Wounded, Never("the LOS")));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.CasualtyReduced, Never("the LOS")));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(true, ScenarioA1Sniper.CrewBroken, Never("the LOS")));
        Assert.False(ScenarioA1Sniper.KeepsConcealment(false, ScenarioA1Sniper.Pinned, Never("the LOS")));

        // The pin of a unit that keeps its "?" leaves its concealment alone.
        Assert.Equal([(UnitCondition.Pinned, true)], ScenarioA1Sniper.PinnedConditions(keepsConcealment: true));
        Assert.Equal([(UnitCondition.Pinned, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)], ScenarioA1Sniper.PinnedConditions(keepsConcealment: false));
    }

    // Increment 4b: the SAN as state and Sniper Check (A14.4, E1.76; rulings R71.9, R71.10).
    private static SniperCheckCandidate Candidate(bool onFoot = true, bool goodOrder = true, bool pinned = false, bool ti = false, bool unarmed = false, bool hidden = false,
        bool firedOrMoved = false) => new(onFoot, goodOrder, pinned, ti, unarmed, hidden, firedOrMoved);

    private static SniperCheckParticipant Leader(int leadership, bool heroic = false) => new("asl:leader", leadership, heroic);

    private static readonly SniperCheckParticipant Squad = new("asl:squad", null, false);
    private static readonly SniperCheckParticipant HalfSquad = new("asl:half-squad", null, false);
    private static readonly SniperCheckParticipant Crew = new("asl:crew", null, false);
    private static readonly SniperCheckParticipant Hero = new("asl:hero", null, false);

    [Fact]
    public void TheActualSanIsThePrintedLessItsReductionsAndAtNightNeverBelowThePrinted()
    {
        // A14.4 (p. 83; ruling R71.9): each Sniper eliminated reduces the SAN by one; by day from the printed SAN.
        Assert.Equal(3, ScenarioA1Sniper.ActualSan(3, night: false, reductions: 0));
        Assert.Equal(2, ScenarioA1Sniper.ActualSan(3, night: false, reductions: 1));
        Assert.Equal(1, ScenarioA1Sniper.ActualSan(2, night: false, reductions: 1));

        // E1.76 (p. 225): at night the printed SAN plus two, at most 7, which "can never be reduced below the printed" SAN.
        Assert.Equal(6, ScenarioA1Sniper.ActualSan(4, night: true, reductions: 0));
        Assert.Equal(5, ScenarioA1Sniper.ActualSan(4, night: true, reductions: 1));
        Assert.Equal(4, ScenarioA1Sniper.ActualSan(4, night: true, reductions: 2));
        Assert.Equal(4, ScenarioA1Sniper.ActualSan(4, night: true, reductions: 3));
        Assert.Equal(6, ScenarioA1Sniper.ActualSan(6, night: true, reductions: 1));

        // The trigger reads the actual SAN: a DR equal to the old SAN calls nothing once it is reduced.
        Assert.True(ScenarioA1Sniper.Triggers(3, 3, night: false, reductions: 0));
        Assert.False(ScenarioA1Sniper.Triggers(3, 3, night: false, reductions: 1));
        Assert.True(ScenarioA1Sniper.Triggers(2, 3, night: false, reductions: 1));
    }

    [Fact]
    public void ASanBelowTwoRemovesTheCounter()
    {
        // A14.1, A14.4 (pp. 82, 83; ruling R71.9): "If the SAN is reduced to 1, remove the Sniper Target Selection counter from the game."
        Assert.True(ScenarioA1Sniper.CounterRemoved(1));
        Assert.True(ScenarioA1Sniper.CounterRemoved(0));
        Assert.False(ScenarioA1Sniper.CounterRemoved(2));

        // A recorded reduction names a side with a SAN and one of the two causes.
        Assert.Null(ScenarioA1Sniper.VerifySanReduced(true, ScenarioA1Sniper.SniperCheckCause));
        Assert.Null(ScenarioA1Sniper.VerifySanReduced(true, ScenarioA1Sniper.SniperAttackCause));
        Assert.NotNull(ScenarioA1Sniper.VerifySanReduced(false, ScenarioA1Sniper.SniperCheckCause));
        Assert.NotNull(ScenarioA1Sniper.VerifySanReduced(true, "fire"));
    }

    [Fact]
    public void TheSniperCheckDrmFollowsTheExamplesThreeCases()
    {
        // A14.4 EX (p. 83): "An 8-1 leader making a Sniper Check would not qualify for any DRM because a leader cannot modify his own DR."
        Assert.Equal(0, ScenarioA1Sniper.SniperCheckDrm([Leader(-1)]).Drm);

        // "An 8-1 leader with a HS would receive a -2 DRM (-1 for leadership and -1 for a HS taking the Sniper Check)."
        var (withHalfSquad, causes) = ScenarioA1Sniper.SniperCheckDrm([Leader(-1), HalfSquad]);
        Assert.Equal(-2, withHalfSquad);
        Assert.Equal(["leadership -1", "1 HS equivalent -1"], causes);

        // "An 8-1 leader, a squad, and a HS would receive a -4 DRM (-1 for leadership, -1 for the HS, and -2 for the squad)."
        Assert.Equal(-4, ScenarioA1Sniper.SniperCheckDrm([Leader(-1), Squad, HalfSquad]).Drm);
    }

    [Fact]
    public void ASniperCheckCountsEachHeroAndEachCrewAndAHeroicLeaderGivesOneOrTheOther()
    {
        // A14.4 (p. 83): -1 each participating hero; -1 each crew/HS equivalent.
        Assert.Equal(-1, ScenarioA1Sniper.SniperCheckDrm([Hero]).Drm);
        Assert.Equal(-2, ScenarioA1Sniper.SniperCheckDrm([Hero, Crew]).Drm);
        Assert.Equal(-2, ScenarioA1Sniper.SniperCheckDrm([Squad]).Drm);

        // A leader whose modifier helps nobody is not used: a 7-0's 0, a 6+1's +1.
        Assert.Equal(-2, ScenarioA1Sniper.SniperCheckDrm([Leader(0), Squad]).Drm);
        Assert.Equal(-2, ScenarioA1Sniper.SniperCheckDrm([Leader(1), Squad]).Drm);

        // A15.21 (p. 83): "A heroic leader may not combine his heroic DRM with his leadership DRM": a heroic 9-2 with a squad directs it (-2 - 2), and his hero's
        // -1 is not added; alone, he is a hero (-1).
        Assert.Equal(-4, ScenarioA1Sniper.SniperCheckDrm([Leader(-2, heroic: true), Squad]).Drm);
        Assert.Equal(-1, ScenarioA1Sniper.SniperCheckDrm([Leader(-2, heroic: true)]).Drm);
    }

    [Theory]
    [InlineData(2, ScenarioA1Sniper.SniperEliminated)]
    [InlineData(3, ScenarioA1Sniper.SniperPinned)]
    [InlineData(4, ScenarioA1Sniper.NoEffect)]
    public void ASniperCheckFinalDrOfTwoEliminatesTheSniperAndAThreePinsIt(int finalDr, string result)
    {
        // A14.4 (p. 82): "will do so on a Sniper Check Final DR of <= 2. A Sniper Check Final DR of 3 pins the Sniper."
        Assert.Equal(result, ScenarioA1Sniper.SniperCheckResult(finalDr, pinned: false));
        Assert.Equal(finalDr <= 2 ? ScenarioA1Sniper.SniperEliminated : ScenarioA1Sniper.NoEffect, ScenarioA1Sniper.SniperCheckResult(finalDr, pinned: true));
    }

    [Fact]
    public void OnlyUnpinnedNonTiArmedGoodOrderInfantryThatHasNeitherFiredNorMovedMayTakeTheSniperCheck()
    {
        // A14.4 (p. 82): "any unpinned, non-TI, armed Good Order Infantry/Cavalry unit in the target Location which has not yet fired/moved during that Player Turn".
        Assert.True(ScenarioA1Sniper.MayTakeSniperCheck(Candidate()));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(pinned: true)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(ti: true)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(goodOrder: false)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(firedOrMoved: true)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(unarmed: true)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(onFoot: false)));
        Assert.False(ScenarioA1Sniper.MayTakeSniperCheck(Candidate(hidden: true)));

        // A12.141 (p. 78): making a Sniper Check costs the "?" in the LOS of a Good Order enemy ground unit within 16 hexes; TI always.
        Assert.Equal([(UnitCondition.Ti, true)], ScenarioA1Sniper.SniperCheckConditions(losesConcealment: false));
        Assert.Equal([(UnitCondition.Ti, true), (UnitCondition.Concealed, false), (UnitCondition.Hidden, false)], ScenarioA1Sniper.SniperCheckConditions(losesConcealment: true));
    }

    // Increment 4c: the Sniper counter at setup (A14.01, A14.2; ruling R71.11). The hexes lie on a line, so two are as many hexes apart as their indexes.
    private static readonly Func<int, int, int?> Line = (one, two) => Math.Abs(one - two);

    private static SniperPlacementUnit[] Enemies(params int[] hexes) => [.. hexes.Select(hex => new SniperPlacementUnit(hex, true, false))];

    [Fact]
    public void ASniperGoesWithinSixHexesOfSixEnemyHexesWhenItCanAndOfFiveWhenNoHexHasSix()
    {
        // A14.2 (p. 82): "in an unoccupied hex within six hexes of >= six enemy-occupied ... hexes if possible. If this is not possible, ... >= five
        // enemy-occupied hexes, etc.". Seven enemy hexes, 0 to 6: only hex 7 is within six of six of them (1 to 6), and the count stops at six.
        var six = ScenarioA1Sniper.Placements(20, Enemies(0, 1, 2, 3, 4, 5, 6), Line);
        Assert.Equal(6, six.Tier);
        Assert.Equal([7], six.Legal);

        // Five enemy hexes, 0 to 4: hexes 5 and 6 are within six of all five; hex 7 is not (hex 0 is seven away).
        var five = ScenarioA1Sniper.Placements(20, Enemies(0, 1, 2, 3, 4), Line);
        Assert.Equal(5, five.Tier);
        Assert.Equal([5, 6], five.Legal);
        Assert.Null(ScenarioA1Sniper.PlacementBar(five.Legal.Contains(6), "russian", "[F6]", five.Tier));
        Assert.StartsWith("play.sniper-placement: [F7] is no hex for the russian Sniper counter: it goes in an unoccupied hex within six hexes of 5 or more enemy-occupied hexes",
            ScenarioA1Sniper.PlacementBar(five.Legal.Contains(7), "russian", "[F7]", five.Tier), StringComparison.Ordinal);
    }

    [Fact]
    public void ASniperGoesInAnUnoccupiedHexAndAnywhereWhenNoEnemyIsOnTheBoard()
    {
        // A14.2 (p. 82): "an unoccupied hex": a friendly unit in hex 5 leaves hex 6 alone within six hexes of the five enemy hexes.
        SniperPlacementUnit[] withFriend = [.. Enemies(0, 1, 2, 3, 4), new SniperPlacementUnit(5, false, false)];
        Assert.Equal([6], ScenarioA1Sniper.Placements(20, withFriend, Line).Legal);

        // "thus it may be set up anywhere if no enemy unit sets up onboard": every unoccupied hex, a hex whose distance is unread too.
        var anywhere = ScenarioA1Sniper.Placements(4, [new SniperPlacementUnit(1, false, false)], (_, _) => null);
        Assert.Equal(0, anywhere.Tier);
        Assert.Equal([0, 2, 3], anywhere.Legal);
    }

    [Fact]
    public void ASnipersPlacementCountsTheEnemyUnitsItsSideSeesAndNotTheHiddenOnes()
    {
        // A14.2 (p. 82; ruling R71.11): counted from the placing side's view. An enemy "?" is a unit it sees, so it occupies its hex and counts; a hidden
        // enemy unit is not on the board as it sees it (A12.3): with one in hex 10, hex 10 is unoccupied and the tier stays five, met by hexes 5 and 6.
        SniperPlacementUnit[] withHidden = [.. Enemies(0, 1, 2, 3, 4), new SniperPlacementUnit(10, true, true)];
        var verdict = ScenarioA1Sniper.Placements(20, withHidden, Line);
        Assert.Equal(5, verdict.Tier);
        Assert.Equal([5, 6], verdict.Legal);

        // Seen, the same unit in hex 10 makes hexes 5 and 6 within six of six enemy hexes, and hex 10 occupied.
        Assert.Equal(6, ScenarioA1Sniper.Placements(20, Enemies(0, 1, 2, 3, 4, 10), Line).Tier);
        Assert.Equal([5, 6], ScenarioA1Sniper.Placements(20, Enemies(0, 1, 2, 3, 4, 10), Line).Legal);
    }

    [Fact]
    public void EverySideWithASanOfTwoOrMorePlacesItsSniperCounterBeforePlayTheDefenderFirst()
    {
        // A14.01, A14.2 (p. 82; ruling R71.11): "each player (DEFENDER first) places one friendly Sniper Counter"; a SAN below 2 gives none.
        SniperSetupSide[] sides = [new("german", 3, false), new("russian", 2, false)];
        Assert.Equal(["russian", "german"], ScenarioA1Sniper.PlacementsDue(sides, "russian"));
        Assert.Equal(["german"], ScenarioA1Sniper.PlacementsDue([new("german", 3, false), new("russian", 2, true)], "russian"));
        Assert.Empty(ScenarioA1Sniper.PlacementsDue([new("german", 1, false), new("russian", null, false)], "russian"));
        Assert.StartsWith("play.setup-incomplete: the russian side has a SAN of 2 or more and places its Sniper counter before play starts",
            ScenarioA1Sniper.PlacementDueBar(["russian", "german"]), StringComparison.Ordinal);
        Assert.Null(ScenarioA1Sniper.PlacementDueBar([]));

        // The Sniper step: a card's game only, once both sides have set up, a side that owes one, the DEFENDER first.
        string[] due = ["russian", "german"];
        Assert.StartsWith("play.sniper-placement: in a game made without a card", ScenarioA1Sniper.PlacementTurnBar("russian", false, false, due), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: a Sniper counter is placed once both sides have set up", ScenarioA1Sniper.PlacementTurnBar("russian", true, true, due), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: the russian side, the DEFENDER, places its Sniper counter first", ScenarioA1Sniper.PlacementTurnBar("german", true, false, due), StringComparison.Ordinal);
        Assert.StartsWith("play.sniper-placement: the german side has no Sniper counter to place", ScenarioA1Sniper.PlacementTurnBar("german", true, false, []), StringComparison.Ordinal);
        Assert.Null(ScenarioA1Sniper.PlacementTurnBar("russian", true, false, due));
    }

    [Fact]
    public void AGameStartedWithoutASidesSniperCounterSaysItMakesNoSniperAttacks()
    {
        // A14.01 (p. 82; ruling R71.11): a game whose play started before the counter was given is never given one.
        Assert.Equal(["german"], ScenarioA1Sniper.NoCounter([new("german", 3, false), new("russian", 2, true)]));
        Assert.Empty(ScenarioA1Sniper.NoCounter([new("german", 1, false), new("russian", 2, true)]));
        Assert.Equal("The German side has a SAN but no Sniper counter in this game, so it makes no Sniper attacks (A14.01).", ScenarioA1Sniper.NoCounterText(["German"]));
        Assert.Equal("The German and the Russian sides have a SAN but no Sniper counter in this game, so they make no Sniper attacks (A14.01).",
            ScenarioA1Sniper.NoCounterText(["German", "Russian"]));
        Assert.Null(ScenarioA1Sniper.NoCounterText([]));
    }
}
