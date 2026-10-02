using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Fire and Close Combat packages as revised for backlog pass 11: a vehicle's OVR (ruling R11.11), Infantry's CC against a vehicle (R11.14),
/// and a vehicle's CC attack on Infantry (R11.15).
/// </summary>
public sealed class ScenarioA1Pass11Tests
{
    private static readonly ScenarioA1FireReference Fire = new ScenarioA1FirePackage().Reference;
    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private const string At = "bd01:F5:0";

    private static FireTarget Target(string id, bool concealed = false) => new(id, "defender-squad", At, false, false, concealed, false, false, false, false);

    private static FireAttack Overrun(string definition, int[] dice, bool crewExposed = false, bool immobile = false, string terrain = "open-ground", bool concealed = false,
        bool bmg = false) =>
        new("MPh", "phasing", null, At, At, null, null, 0, true, new FireLos(false, 0, true, false), 7, terrain, [Target("ru-s", concealed)], 3,
            new FireRolls(dice, null, null, null))
        {
            FireKind = ScenarioA1FireCalculator.OverrunFire,
            Overrun = new FireOverrun("de-v", definition, At, crewExposed, immobile, false, bmg, false),
        };

    private static FireResolution Complete(FireAttack attack)
    {
        var rolls = attack.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Fire);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            var key = reason["asl.a1.fire.roll-missing:".Length..];
            var split = key.IndexOf(':', StringComparison.Ordinal);
            var (kind, ids) = (key[..split], key[(split + 1)..].Split(','));
            rolls = kind switch
            {
                "checks" => rolls with { Checks = ids.ToDictionary(id => id, _ => (IReadOnlyList<int>)[2, 2]) },
                "weaponSelection" => rolls with { WeaponSelection = ids.Select((id, index) => (id, index)).ToDictionary(item => item.id, item => item.index == ids.Length - 1 ? 6 : 1) },
                _ => throw new InvalidOperationException(key),
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void ATankOverrunsWithFourPlusItsMachineGunsTripledAndHalved()
    {
        // D7.11: PzKpfw IIIH, MA 50mm manned: 4 + (3 BMG + 5 CMG) x 3 / 2 = 16; D7.15: FFMO -1 in Open Ground; D7.1: Bounding First Fire.
        var result = Complete(Overrun("attacker-tank", [3, 4]));
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal((16m, 16), (result.Arithmetic!.TotalFirepower, result.Arithmetic.ColumnFp));
        Assert.Contains(result.Arithmetic.Drm, item => item.Name == "ffmo" && item.Value == -1m);
        Assert.Equal("bounding-fire", result.FireCounter);
        Assert.Equal(["de-v"], result.FireCounterUnitIds);
        Assert.Null(result.Arithmetic.ResidualFp);
    }

    [Fact]
    public void TheOvrFpIsHalvedByImmobilityAndAgainstConcealedUnitsButNotByMotion()
    {
        // D7.11: the halftrack, CE: 2 + 3 x 3 / 2 = 6.5, halved when Immobile before the OVR resolves: 3.25; a malfunctioned BMG adds nothing.
        var ce = Complete(Overrun("attacker-halftrack", [4, 4], crewExposed: true));
        Assert.Equal(6.5m, ce.Arithmetic!.TotalFirepower);
        var bu = Complete(Overrun("attacker-halftrack", [4, 4]));
        Assert.Equal(2m, bu.Arithmetic!.TotalFirepower);
        var immobile = Complete(Overrun("attacker-halftrack", [4, 4], crewExposed: true, immobile: true));
        Assert.Equal(3.25m, immobile.Arithmetic!.TotalFirepower);
        var withoutBmg = Complete(Overrun("attacker-tank", [4, 4], bmg: true));
        Assert.Equal(11.5m, withoutBmg.Arithmetic!.TotalFirepower);

        // A12.13: a truck's 1 FP against a concealed squad is Area Fire, 0.5, below the 1 column.
        var truck = Complete(Overrun("attacker-truck", [4, 4], concealed: true));
        Assert.Equal(0.5m, truck.Arithmetic!.TotalFirepower);
        Assert.Equal("none", truck.Arithmetic.Result);
    }

    [Fact]
    public void AnOvrTakesTheTargetsTemAndNoFfmoInWoods()
    {
        var result = Complete(Overrun("attacker-tank", [4, 4], terrain: "woods"));
        Assert.Contains(result.Arithmetic!.Drm, item => item.Name == "tem:woods" && item.Value == 1m);
        Assert.DoesNotContain(result.Arithmetic.Drm, item => item.Name == "ffmo");
    }

    [Fact]
    public void AnOriginal12MalfunctionsAWeaponByRandomSelectionOrImmobilizesAnUnarmedVehicle()
    {
        // D7.17: the tank's MA, BMG, and CMG added FP; the highest selection dr (the last named, 6) malfunctions.
        var tank = Complete(Overrun("attacker-tank", [6, 6]));
        Assert.Equal([FireOverrunEffect.CoaxialMg], tank.OverrunEffect!.MalfunctionedWeapons);
        Assert.False(tank.OverrunEffect.Immobilized);

        // A truck has no weapon that can malfunction, so it is immobilized.
        var truck = Complete(Overrun("attacker-truck", [6, 6]));
        Assert.True(truck.OverrunEffect!.Immobilized);
        Assert.Null(Complete(Overrun("attacker-truck", [5, 6])).OverrunEffect);
    }

    [Fact]
    public void AnOvrOutsideItsOwnLocationIsRefused()
    {
        var attack = Overrun("attacker-tank", [3, 4]) with
        {
            TargetLocationId = "bd01:F6:0"
        };
        Assert.Contains("asl.a1.fire.overrun-outside", ScenarioA1FireCalculator.Precheck(attack, Fire));
        Assert.Contains("asl.a1.fire.phase-outside", ScenarioA1FireCalculator.Precheck(Overrun("attacker-tank", [3, 4]) with
        {
            Phase = "AFPh"
        }, Fire));
    }

    private static CloseCombatUnit Unit(string id, string definition, string side, bool pinned = false, bool broken = false, bool cx = false) =>
        new(id, definition, side, broken, pinned, false, false, false, false, false, false, false, false, false)
        {
            Cx = cx ? true : null,
            Inexperienced = false
        };

    private static VehicleCloseCombatVehicle Vehicle(string definition, bool crewExposed = false, bool motion = false, bool immobile = false, bool stunned = false) =>
        new("de-v", definition, "german", crewExposed, motion, immobile, stunned, false, false, false, false, false);

    private static VehicleCloseCombatFacts Attack(VehicleCloseCombatVehicle vehicle, CloseCombatUnit[] units, string[] attackers, int[] dice, int? unlikely = null,
        bool reaction = false) =>
        new(reaction ? "MPh" : "CCPh", At, vehicle, units, attackers, [], false, reaction, new VehicleCloseCombatRolls(dice) { UnlikelyKill = unlikely });

    [Fact]
    public void InfantryAttackAVehicleWithTheirCcvAsTheKillNumber()
    {
        // A11.5, A11.61: a 4-4-7 (CCV 5) against the CE, OT halftrack (-2 OT): an Original 6 is 4, which eliminates it; 5 immobilizes; at most 2 burns.
        var units = new[] { Unit("ru-s", "defender-squad", "russian") };
        var halftrack = Vehicle("attacker-halftrack", crewExposed: true);
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1VehicleCloseCombat.Resolve(Attack(halftrack, units, ["ru-s"], [3, 3]), CloseCombat).VehicleResult);
        Assert.Equal(VehicleCloseCombatResolution.Immobilized, ScenarioA1VehicleCloseCombat.Resolve(Attack(halftrack, units, ["ru-s"], [3, 4]), CloseCombat).VehicleResult);
        var burning = ScenarioA1VehicleCloseCombat.Resolve(Attack(halftrack, units, ["ru-s"], [2, 2]), CloseCombat);
        Assert.Equal((5, VehicleCloseCombatResolution.BurningWreck), (burning.KillNumber, burning.VehicleResult));
    }

    [Fact]
    public void TheCcDrmFollowTheVehicleAndItsEscort()
    {
        // A11.51: -3 unarmored and -1 with no manned MG (a truck); A11.61: the -1 Immobile is for an AFV only (referee, pass 11); +2 against a moving
        // vehicle; +2 per escorting squad; the BU halftrack's AAMG is not manned, -1.
        var squad = Unit("ru-s", "defender-squad", "russian");
        var truck = ScenarioA1VehicleCloseCombat.Resolve(Attack(Vehicle("attacker-truck", immobile: true), [squad], ["ru-s"], [4, 4]), CloseCombat);
        Assert.Equal(["vs-unarmored", "vs-no-manned-mg"], truck.Drm.Select(item => item.Name));
        var escorted = ScenarioA1VehicleCloseCombat.Resolve(Attack(Vehicle("attacker-tank", motion: true, immobile: true), [squad, Unit("de-s", "attacker-squad", "german")],
            ["ru-s"], [4, 4]), CloseCombat);
        Assert.Equal([("vs-immobile", -1m), ("escort:de-s", 2m), ("vs-motion", 2m)], escorted.Drm.Select(item => (item.Name, item.Value)));
        var bu = ScenarioA1VehicleCloseCombat.Resolve(Attack(Vehicle("attacker-halftrack"), [squad], ["ru-s"], [4, 4]), CloseCombat);
        Assert.Contains(bu.Drm, item => item.Name == "vs-no-manned-mg");
    }

    [Fact]
    public void ACombiningLeaderAddsOneCcvAndHisLeadershipAndAPinAndAReactionMarkerSubtractOne()
    {
        // A11.5: 5 + 1 (SMC) - 1 (pinned) - 1 (D7.213 fire marker) = 4; the 8-1's -1 applies because he combines.
        var units = new[] { Unit("ru-s", "defender-squad", "russian", pinned: true), Unit("ru-l", "defender-leader-8-1", "russian") };
        var result = ScenarioA1VehicleCloseCombat.Resolve(Attack(Vehicle("attacker-tank"), units, ["ru-s", "ru-l"], [4, 4], reaction: true) with
        {
            FireMarked = ["ru-s"]
        },
            CloseCombat);
        Assert.Contains("asl.a1.cc-vehicle.reaction-outside", result.Reasons);
        var ccph = ScenarioA1VehicleCloseCombat.Resolve(Attack(Vehicle("attacker-tank"), units, ["ru-s", "ru-l"], [4, 4]), CloseCombat);
        Assert.Equal(5, ccph.KillNumber);
        Assert.Contains(ccph.Drm, item => item.Name == "leadership:ru-l" && item.Value == -1m);
    }

    [Fact]
    public void AnOriginalTwoRollsTheUnlikelyKillAndAnOriginal12ReducesTheAttackers()
    {
        // A11.501: an Original 2 against the moving tank (+2) is 4, below 5: eliminated; a subsequent dr of 1 burns it.
        var units = new[] { Unit("ru-s", "defender-squad", "russian") };
        var tank = Vehicle("attacker-tank", motion: true);
        Assert.Contains("asl.a1.cc-vehicle.roll-missing:unlikelyKill", ScenarioA1VehicleCloseCombat.Resolve(Attack(tank, units, ["ru-s"], [1, 1]), CloseCombat).Reasons);
        Assert.Equal(VehicleCloseCombatResolution.BurningWreck, ScenarioA1VehicleCloseCombat.Resolve(Attack(tank, units, ["ru-s"], [1, 1], 1), CloseCombat).VehicleResult);
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1VehicleCloseCombat.Resolve(Attack(tank, units, ["ru-s"], [1, 1], 5), CloseCombat).VehicleResult);

        // A11.621: an Original 12 against the crewed tank Casualty Reduces the squad to its HS.
        var twelve = ScenarioA1VehicleCloseCombat.Resolve(Attack(tank, units, ["ru-s"], [6, 6]), CloseCombat);
        Assert.Equal("defender-half-squad", Assert.Single(twelve.Effects).FinalDefinitionId);
    }

    [Fact]
    public void AVehicleAttacksInfantryWithItsCmgAndCeAamgAgainstTheirCcv()
    {
        // A11.62: the tank's CMG 5 against a squad (CCV 5) and a leader defending with it (+1): 5-6, the 1-2 column, Kill Number 4; the BMG never.
        var units = new[] { Unit("ru-s", "defender-squad", "russian"), Unit("ru-l", "defender-leader-8-1", "russian") };
        var facts = new VehicleCloseCombatFacts("CCPh", At, Vehicle("attacker-tank"), units, [], ["ru-s", "ru-l"], true, false, new VehicleCloseCombatRolls([2, 1]));
        var result = ScenarioA1VehicleCloseCombat.Resolve(facts, CloseCombat);
        Assert.Equal((5m, 6m, "1-2", 4), (result.AttackFirepower, result.DefenseValue, result.Odds, result.KillNumber));
        Assert.All(result.Defending!, item => Assert.Equal(CloseCombatDefenderResult.Eliminated, item.Result));

        // A leader alone defends with a CCV of 2 (referee, pass 11); a BU halftrack has no CC armament.
        var alone = ScenarioA1VehicleCloseCombat.Resolve(facts with
        {
            Defenders = ["ru-l"]
        }, CloseCombat);
        Assert.Equal(2m, alone.DefenseValue);
        Assert.Contains("asl.a1.cc-vehicle.no-cc-armament",
            ScenarioA1VehicleCloseCombat.Resolve(facts with
            {
                Vehicle = Vehicle("attacker-halftrack")
            }, CloseCombat).Reasons);
    }
}
