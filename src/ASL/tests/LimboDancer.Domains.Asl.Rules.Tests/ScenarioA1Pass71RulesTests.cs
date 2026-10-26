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
}
