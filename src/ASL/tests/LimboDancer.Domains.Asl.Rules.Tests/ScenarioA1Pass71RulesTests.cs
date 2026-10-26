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
        Assert.Equal([1], ScenarioA1Sniper.TargetHexes([new SniperTargetHex(0, 3, 0, "E3"), new SniperTargetHex(1, 1, 0, "E7")]));
    }

    [Fact]
    public void SmokeInATargetsHexCountsInTheTemComparison()
    {
        // A14.21 (ruling R71.7): "the lowest (to a minimum of zero) in-hex TEM/SMOKE DRM": +2 a source, at most +3 (A24.2).
        Assert.Equal(0, ScenarioA1Sniper.TargetTem("open-ground", 0));
        Assert.Equal(2, ScenarioA1Sniper.TargetTem("open-ground", 1));
        Assert.Equal(4, ScenarioA1Sniper.TargetTem("woods", 2));

        // Two equidistant hexes: the woods (+1) is chosen over the Open Ground with a SMOKE grenade (+2).
        Assert.Equal([1], ScenarioA1Sniper.TargetHexes([new SniperTargetHex(0, 2, ScenarioA1Sniper.TargetTem("open-ground", 1), "B3"),
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

    // The hidden-information fixes (rulings R71.18 to R71.22).
    private static readonly int[] TheSquadAndTheCrew = [0, 2];

    [Fact]
    public void AConcealedVehiclesPrcCrewAndItselfAreItsStackAndOneWithNoEligibleTargetIsStillOneCandidate()
    {
        // A12.2, A14.23 (pp. 79, 82; ruling R71.20): a PRC of a concealed vehicle shares its "?", so it is in the concealed stack though not concealed itself.
        Assert.True(ScenarioA1Sniper.InConcealedStack(concealedOrDummy: false, vehicleConcealed: true));
        Assert.True(ScenarioA1Sniper.InConcealedStack(concealedOrDummy: true, vehicleConcealed: false));
        Assert.False(ScenarioA1Sniper.InConcealedStack(concealedOrDummy: false, vehicleConcealed: false));

        // A concealed BU AFV holds no eligible target, and still counts as one "?" candidate; unconcealed, it is no target, and one with a target is that target.
        var buttoned = ScenarioA1Sniper.VehicleTargets(Vehicle(OneButtoned));
        Assert.True(ScenarioA1Sniper.CountsAsConcealedVehicle(true, buttoned));
        Assert.False(ScenarioA1Sniper.CountsAsConcealedVehicle(false, buttoned));
        Assert.False(ScenarioA1Sniper.CountsAsConcealedVehicle(true, ScenarioA1Sniper.VehicleTargets(Vehicle(OneButtoned, crewExposed: true))));

        // A14.23: the owner declares the eligible targets only: neither a Dummy nor such a vehicle; a stack of Dummies alone is a Dummy stack, and one holding
        // the vehicle is not.
        var (eligible, dummyStack) = ScenarioA1Sniper.StackTargets([(ScenarioA1Sniper.UnitTarget, false), (ScenarioA1Sniper.UnitTarget, true), (ScenarioA1Sniper.CrewTarget, false),
            (ScenarioA1Sniper.ConcealedVehicle, false)]);
        Assert.Equal(TheSquadAndTheCrew, eligible);
        Assert.False(dummyStack);
        Assert.True(ScenarioA1Sniper.StackTargets([(ScenarioA1Sniper.UnitTarget, true), (ScenarioA1Sniper.UnitTarget, true)]).DummyStack);
        var vehicleOnly = ScenarioA1Sniper.StackTargets([(ScenarioA1Sniper.ConcealedVehicle, false), (ScenarioA1Sniper.UnitTarget, true)]);
        Assert.Empty(vehicleOnly.Eligible);
        Assert.False(vehicleOnly.DummyStack);
    }

    [Fact]
    public void ATargetThatKeepsItsQuestionMarkHasOneNeutralPublicResult()
    {
        // A12.14, A14.23 (ruling R71.22): a pin, a Stun, or no effect on a target that keeps its "?" reads the same in public.
        Assert.Equal(ScenarioA1Sniper.ConcealedKept, ScenarioA1Sniper.PublicResult(true, ScenarioA1Sniper.Pinned));
        Assert.Equal(ScenarioA1Sniper.ConcealedKept, ScenarioA1Sniper.PublicResult(true, ScenarioA1Sniper.Stunned));
        Assert.Equal(ScenarioA1Sniper.ConcealedKept, ScenarioA1Sniper.PublicResult(true, ScenarioA1Sniper.NoEffect));
        Assert.Equal(ScenarioA1Sniper.Pinned, ScenarioA1Sniper.PublicResult(false, ScenarioA1Sniper.Pinned));
    }

    [Fact]
    public void TheSniperCheckIsOfferedWheneverAUnitMayTakeItOrAQuestionMarkStaysInTheLocation()
    {
        // A14.4, A14.23, A12.141 (ruling R71.18): the offer tells the Sniper side nothing of a "?" stack.
        Assert.True(ScenarioA1Sniper.SniperCheckOffered(1, concealedLeft: false));
        Assert.True(ScenarioA1Sniper.SniperCheckOffered(0, concealedLeft: true));
        Assert.False(ScenarioA1Sniper.SniperCheckOffered(0, concealedLeft: false));
        Assert.Equal("play.sniper-check: no unit in [E6] may take the Sniper Check, so none is made and nothing is rolled (A14.4; ruling R71.18)",
            ScenarioA1Sniper.NoSniperCheckerText("[E6]"));
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

    // Increment 4d: the Sniper player's choices (A14.2, A14.21; ruling R71.12).

    [Fact]
    public void EquidistantHexesOfTheLowestTemAreTheSnipersChoiceAndAnyOtherTieIsNot()
    {
        // A14.21 (p. 82): "Should ≥ two such hexes be equidistant, the Location with the lowest TEM is the target ... If the target hex is still undetermined,
        // the Sniper player chooses which of those equidistant hexes to attack": both Open Ground hexes at two, by name; the woods at two and the closer hex
        // at one are not among them.
        Assert.Equal([2, 0], ScenarioA1Sniper.TargetHexes([new SniperTargetHex(0, 2, 0, "M5"), new SniperTargetHex(1, 2, 1, "K5"), new SniperTargetHex(2, 2, 0, "L4")]));
        Assert.Equal([3], ScenarioA1Sniper.TargetHexes([new SniperTargetHex(0, 2, 0, "M5"), new SniperTargetHex(3, 1, 3, "N5")]));
        Assert.False(ScenarioA1Sniper.ChoiceOffered(1));
        Assert.True(ScenarioA1Sniper.ChoiceOffered(2));
    }

    [Fact]
    public void BeforeRandomSelectionTheSniperMayChooseTheEnemySniperACrewOrAnUnarmoredVehicleButNoConcealedStack()
    {
        // A14.2 EXC (p. 82): "prior to Random Selection, the Sniper may choose ... the enemy Sniper counter; a Vulnerable Inherent Crew; or an unarmored
        // vehicle"; a unit is not among them, nor is a concealed stack (A14.23), whose contents the Sniper player does not know.
        SniperCandidate[] candidates = [new(ScenarioA1Sniper.UnitTarget, false), new(ScenarioA1Sniper.CrewTarget, false), new(ScenarioA1Sniper.VehicleTarget, false),
            new(ScenarioA1Sniper.SniperTarget, false), new(ScenarioA1Sniper.UnitTarget, true)];
        Assert.Equal([1, 2, 3], ScenarioA1Sniper.PriorTargets(candidates));
        Assert.Empty(ScenarioA1Sniper.PriorTargets([new(ScenarioA1Sniper.UnitTarget, false), new(ScenarioA1Sniper.CrewTarget, true)]));

        // With one candidate there is no Random Selection, so nothing to choose before it.
        Assert.Empty(ScenarioA1Sniper.PriorTargets([new(ScenarioA1Sniper.SniperTarget, false)]));
        Assert.Equal("crew:de-tk", ScenarioA1Sniper.TargetOption(ScenarioA1Sniper.CrewTarget, "de-tk"));
    }

    [Fact]
    public void TheSnipersChoicesAreWordedByTheirStepAndCiteTheRule()
    {
        // Ruling R71.12: each choice says what is chosen, with the dr where it matters, and cites A14.2 or A14.21.
        Assert.Equal("the german Sniper's dr of 2 is effective: it attacks, or forfeits the attack and its counter is repositioned within the setup's placement requirements (A14.2)",
            ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.ForfeitStep, "german", null, 2));
        Assert.EndsWith("(A14.21)", ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.HexStep, "german", null, 1), StringComparison.Ordinal);
        Assert.Contains("attacks [M5]: the german side chooses which of its Locations", ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.LocationStep, "german", "[M5]", 1), StringComparison.Ordinal);
        Assert.Contains("before Random Selection", ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.TargetStep, "german", "[M5], level 1", 1), StringComparison.Ordinal);
        Assert.Contains("chooses which takes the dr of 1", ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.FirstStep, "german", "[M5]", 1), StringComparison.Ordinal);
        Assert.EndsWith("(A14.2)", ScenarioA1Sniper.ChoiceText(ScenarioA1Sniper.RepositionStep, "german", null, 1), StringComparison.Ordinal);
    }

    // Increment 4e: every A14.1 trigger (ruling R71.13).

    private static (string? Side, bool Captured)? Unread() => throw new InvalidOperationException("The unit is not read.");

    private static string? NoVehicle() => throw new InvalidOperationException("The vehicle is not read.");

    [Fact]
    public void AToHitACanisterAndAnHeHitsIftDrAreTheFiringSidesAndTheirNestedChecksTheUnits()
    {
        // A14.1 (p. 82; ruling R71.13): "a TH, MC, TC, non-OBA IFT ... Original DR": an ordnance To Hit DR, a Canister shot's IFT DR, and the HE hit's IFT DR
        // are the firing side's; the HE hit's and a Critical Hit's MCs, and a PRC's, are the unit's, read inside their nested keys.
        Assert.Equal(("toHit", string.Empty), ScenarioA1Sniper.RollKey("toHit"));
        Assert.Equal(("attack", string.Empty), ScenarioA1Sniper.RollKey("hit:attack"));
        Assert.Equal(("checks", "r1"), ScenarioA1Sniper.RollKey("hit:checks:r1"));
        Assert.Equal(("leaderLoss", "r2"), ScenarioA1Sniper.RollKey("critical-hit:leaderLoss:r2#2"));
        Assert.Equal(("checks", "de-p"), ScenarioA1Sniper.RollKey("prc:checks:de-p"));
        foreach (var kind in new[] { "toHit", "canister", "attack" })
        {
            Assert.Equal("russian", ScenarioA1Sniper.RollMaker(kind, false, "russian", Unread, NoVehicle));
        }

        Assert.Equal("german", ScenarioA1Sniper.RollMaker("checks", true, "russian", () => ("german", false), NoVehicle));

        // C8.4: a Canister shot's Location attack names no firer; its DR is its Gun's side's, read after a DC's user and before Residual FP.
        Assert.Equal("american", ScenarioA1Sniper.FiringSide(false, NoVehicle, static () => null, static () => "american", static () => true, static () => "german"));
        Assert.Equal("german", ScenarioA1Sniper.FiringSide(false, NoVehicle, static () => null, static () => null, static () => true, static () => "german"));

        // Not A14.1's kinds: To Kill, Shock, Crew Survival, a selection, a dr.
        foreach (var kind in new[] { "toKill", "shockCheck", "crewSurvival", "areaSurvival", "randomSelection", "subsequent", "panzerfaustCheck", "woundSeverity" })
        {
            Assert.Null(ScenarioA1Sniper.RollMaker(kind, true, "russian", Unread, NoVehicle));
        }
    }

    [Fact]
    public void AVehicleCrewsCheckIsItsVehiclesSideAndEveryOtherCheckItsUnitsButAPrisoners()
    {
        // A14.1: a Collateral Attack's MC, an immobilization TC, and an ordnance crew check name no unit: they are the target vehicle's side's; a mortar's crew
        // TC names its vehicle. A PAATC, an OVR NTC, and a Bail Out NMC are their unit's, "[EXC: those made for Prisoners]".
        foreach (var kind in new[] { "collateralCheck", "taskCheck", "crewCheck" })
        {
            Assert.Equal("german", ScenarioA1Sniper.RollMaker(kind, false, "russian", Unread, () => "german"));
        }

        Assert.Equal("german", ScenarioA1Sniper.RollMaker("crewCheck", true, "russian", () => ("german", false), NoVehicle));
        Assert.Equal("german", ScenarioA1Sniper.RollMaker("areaCrewCheck", true, "russian", () => ("german", false), NoVehicle));
        foreach (var kind in new[] { ScenarioA1Sniper.PaatcRoll, ScenarioA1Sniper.NtcRoll, ScenarioA1Sniper.BailOutRoll })
        {
            Assert.Equal("russian", ScenarioA1Sniper.RollMaker(kind, true, null, () => ("russian", false), NoVehicle));
            Assert.Null(ScenarioA1Sniper.RollMaker(kind, true, null, () => ("russian", true), NoVehicle));
        }

        // The older overload reads as before the increment.
        Assert.Equal("russian", ScenarioA1Sniper.RollMaker("checks", "german", () => ("russian", false)));
        Assert.Null(ScenarioA1Sniper.RollMaker("collateralCheck", "german", Unread));
    }

    // Increment 4f: wounds (A17.2, A17.11, D5.343) and Field Promotions (A18.2); rulings R71.14, R71.15, R71.17.

    [Fact]
    public void ASmcWoundedInTheMphAfterMoreThanThreeMfIsPinned()
    {
        // A17.2 (p. 85; ruling R71.14): "A SMC who becomes wounded (e.g., by Defensive First Fire) after already expending > 3 MF is pinned": 3 1/2 counts.
        Assert.False(ScenarioA1Wounds.PinnedByWound(true, "mph", 3, false));
        Assert.True(ScenarioA1Wounds.PinnedByWound(true, "mph", 3, true));
        Assert.True(ScenarioA1Wounds.PinnedByWound(true, "mph", 4, false));

        // Only in the MPh (the RtPh's wound is the backlog's), and only a SMC.
        Assert.False(ScenarioA1Wounds.PinnedByWound(true, "rtph", 4, false));
        Assert.False(ScenarioA1Wounds.PinnedByWound(true, "dfph", 4, false));
        Assert.False(ScenarioA1Wounds.PinnedByWound(false, "mph", 4, false));

        // A Sniper's wound pins him with it.
        Assert.Contains((UnitCondition.Pinned, true), ScenarioA1Sniper.WoundedConditions(true));
        Assert.DoesNotContain((UnitCondition.Pinned, true), ScenarioA1Sniper.WoundedConditions());
    }

    [Fact]
    public void ASmcCrewsStunWoundTakesItsSeverityDrAndARecallStillEliminatesHim()
    {
        // D5.343 (p. 204), A17.11 (p. 85; ruling R71.15): a Stun wounds a SMC Temporary crew; his Wound Severity dr of 5 or 6 is mortal, +1 when already
        // wounded, and any other leaves him wounded.
        Assert.Equal((true, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false, severityDr: 1));
        Assert.Equal((false, true), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false, severityDr: 5));
        Assert.Equal((true, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false, alreadyWounded: true, severityDr: 3));
        Assert.Equal((false, true), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false, alreadyWounded: true, severityDr: 4));

        // With no dr in the record, the outcome of before the pass: a second wound eliminates him.
        Assert.Equal((false, true), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false, alreadyWounded: true));
        Assert.Equal((true, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false));

        // A Recall eliminates him with no dr; one with a Disabled MA leaves him (D5.343 EXC). The dr is due only for a wound.
        Assert.Equal((false, true), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(false, true, false, false, severityDr: 1));
        Assert.True(ScenarioA1CapturedVehicleRules.SmcCrewSeverityDue(true, false, false, false));
        Assert.False(ScenarioA1CapturedVehicleRules.SmcCrewSeverityDue(false, true, false, false));
        Assert.False(ScenarioA1CapturedVehicleRules.SmcCrewSeverityDue(false, true, true, false));
        Assert.False(ScenarioA1CapturedVehicleRules.SmcCrewSeverityDue(false, false, false, false));
    }

    // Increment 4g: carrying a wounded man (A17.2); ruling R71.16.

    private static CarryFacts Carry(bool manMoving = true, bool wounded = true, bool manGoodOrder = true, bool holdsSw = false, bool carrierMoving = true, bool mmc = true,
        bool carrierGoodOrder = true, string man = "de-l", string carrier = "de-s") =>
        new(man, carrier, manMoving, wounded, manGoodOrder, holdsSw, carrierMoving, mmc, carrierGoodOrder);

    private static readonly (string Man, string Carrier)[] NoEarlierCarry = [];
    private static readonly string[] TheSquadAndTheLeader = ["de-s", "de-l"];
    private static readonly string[] TheLeaderAlone = ["de-l"];
    private static readonly int[] One = [1];
    private static readonly int[] OneAndFive = [1, 5];

    [Fact]
    public void AWoundedManIsCarriedOnlyInGoodOrderWithNoSwByAGoodOrderMmcOfHisStack()
    {
        // A17.2 (p. 85; ruling R71.16): "A Good Order wounded man who is not portaging a SW may be carried by any Good Order MMC at a cost of five PP".
        Assert.Null(ScenarioA1Wounds.CarryBar([Carry()], NoEarlierCarry));
        Assert.Contains("two units of the moving stack", ScenarioA1Wounds.CarryBar([Carry(carrierMoving: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("two units of the moving stack", ScenarioA1Wounds.CarryBar([Carry(manMoving: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("two units of the moving stack", ScenarioA1Wounds.CarryBar([Carry(carrier: "de-l")], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("de-l is not wounded", ScenarioA1Wounds.CarryBar([Carry(wounded: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("de-l is not in Good Order", ScenarioA1Wounds.CarryBar([Carry(manGoodOrder: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("de-l portages a SW", ScenarioA1Wounds.CarryBar([Carry(holdsSw: true)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("de-s is no MMC", ScenarioA1Wounds.CarryBar([Carry(mmc: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("de-s is not in Good Order", ScenarioA1Wounds.CarryBar([Carry(carrierGoodOrder: false)], NoEarlierCarry), StringComparison.Ordinal);
        Assert.StartsWith("play.move-carry: ", ScenarioA1Wounds.CarryBar([Carry(holdsSw: true)], NoEarlierCarry), StringComparison.Ordinal);

        // One man to a carrier, one carrier to a man, the move's earlier carries counted; the same carry again stands.
        Assert.Contains("carries one wounded man", ScenarioA1Wounds.CarryBar([Carry(), Carry(man: "de-h")], NoEarlierCarry), StringComparison.Ordinal);
        Assert.Contains("carries one wounded man", ScenarioA1Wounds.CarryBar([Carry(carrier: "de-t")], [("de-l", "de-s")]), StringComparison.Ordinal);
        Assert.Null(ScenarioA1Wounds.CarryBar([Carry()], [("de-l", "de-s")]));

        // A man carried earlier in the move moves on only with his carrier: without it he was set down.
        Assert.Null(ScenarioA1Wounds.CarriedMoveBar([("de-l", "de-s")], TheSquadAndTheLeader));
        Assert.Contains("set down", ScenarioA1Wounds.CarriedMoveBar([("de-l", "de-s")], TheLeaderAlone), StringComparison.Ordinal);
    }

    [Fact]
    public void ACarriersFivePpReduceItsMfByThePortageRulesAndTheManSpendsNone()
    {
        // A17.2, A4.42 (p. 50): the five PP join the carrier's portage, so a squad's 4 MF fall to 2 (two PP beyond its IPC of 3); with a wounded leader's bonus,
        // which still applies, to 4; a SW already carried adds its own PP.
        var squad = new MfAllotmentFacts(false, null, false, false, false);
        int[] none = [];
        Assert.Equal(2, ScenarioA1MovementCalculator.MfAllotment(squad, 0, false, 0, 0, () => 4, () => ScenarioA1Wounds.Portage(none, true)));
        Assert.Equal(4, ScenarioA1MovementCalculator.MfAllotment(squad, 0, false, 2, 0, () => 4, () => ScenarioA1Wounds.Portage(none, true)));
        Assert.Equal(4, ScenarioA1MovementCalculator.MfAllotment(squad, 0, false, 0, 0, () => 4, () => ScenarioA1Wounds.Portage(none, false)));
        Assert.Equal(OneAndFive, ScenarioA1Wounds.Portage(One, true));
        Assert.True(ScenarioA1MovementCalculator.Laden(false, false, false, ScenarioA1Wounds.Portage(none, true)));
        Assert.Null(ScenarioA1Wounds.Portage(null, true));

        // The man carried spends no MF and is not held to his own 3 MF.
        Assert.Equal(0, ScenarioA1Wounds.MoverHalfMf(true, 2));
        Assert.Equal(2, ScenarioA1Wounds.MoverHalfMf(false, 2));
        MfLeftMoverFacts[] movers = [new("de-s", false, 0, false, false, false, 4), new("de-l", false, 0, false, false, false, 6) { Carried = true }];
        int?[] allotments = [4, 3];
        var (refusal, left) = ScenarioA1MovementCalculator.MfCheck(movers, false, false, false, false, false, null, false, 2, (null, null), (index, _, _, _, _) => allotments[index]);
        Assert.Null(refusal);
        Assert.Equal(2, left);
    }

    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private static readonly int[] SnakeEyes = [1, 1];
    private static readonly string[] RussianSquad = ["ru-1"];

    [Theory]
    [InlineData("attacker-tank", true, "defender-leader-7-0")]
    [InlineData("attacker-tank", null, "defender-leader-6-plus-1")]
    [InlineData("attacker-truck", true, "defender-leader-6-plus-1")]
    public void LeaderCreationInCcAgainstAnAfvTakesMinusOne(string vehicle, bool? afv, string created)
    {
        // A18.2 (p. 85; ruling R71.17): "-1: CC vs AFV". A Russian 4-4-7's Original 2 against the PzKpfw IIIH and a Leader Creation dr of 5: +1 Russian, -1
        // against the AFV, Final 5, a 7-0. A record that does not declare the AFV (made before the pass) takes no -1: Final 6, a 6+1; nor does a truck.
        CloseCombatUnit[] units = [new("ru-1", "defender-squad", "russian", false, false, false, false, false, false, false, false, false, false, false)];
        var facts = new VehicleCloseCombatFacts("CCPh", "bd03:F4:0", new VehicleCloseCombatVehicle("de-v", vehicle, "german", false, false, false, false, false, false, false, false, false),
            units, RussianSquad, [], false, false, new VehicleCloseCombatRolls(SnakeEyes) { LeaderCreation = 5 })
        {
            LeaderCreation = true,
            AfvDrm = afv,
        };
        var creation = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat).LeaderCreation!;
        Assert.Equal(created, creation.LeaderDefinitionId);
        Assert.Equal(vehicle == "attacker-tank" && afv == true, creation.Drm.Any(item => item.Name == "cc-vs-afv" && item.Value == -1m && item.Rule == "A18.2"));
    }
}
