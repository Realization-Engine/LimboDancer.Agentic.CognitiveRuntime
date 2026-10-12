using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 50, Armored combat I. Tasks 50.1 and 50.2: a tank's BMG and CMG on the IFT (D1.81, D1.82, D3.5, pp. 195 and 200), Case A as an IFT
/// DRM (D3.51, D3.52, C5.11, C5.13), a malfunction by weapon (D3.7, A9.71), the repair of each weapon and the Recall of a tank whose MA is
/// disabled (D3.7, D5.341), and the MA beside the vehicle's other weapons.
/// </summary>
public sealed class ScenarioA1Pass50RulesTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";
    private const string Outside = "asl.a1.fire.vehicle-fire-outside";
    private static readonly string[] Both = [FireVehicleWeapon.BowMg, FireVehicleWeapon.CoaxialMg];

    private static FireVehicleWeapon Mg(string weapon, int turn = 0, bool malfunctioned = false, bool fired = false, int? maintained = null) =>
        new(weapon, malfunctioned, fired)
        {
            Turn = turn,
            MaintainedCaseA = maintained,
        };

    private static FireVehicleFire Tank(FireVehicleWeapon[] weapons, string definition = "attacker-tank", bool crewExposed = false, bool woods = false,
        bool otherPhase = false) =>
        new("de-tank", definition, From, crewExposed, false, false, false, false, false, false, false)
        {
            Weapons = weapons,
            InWoodsOrBuilding = woods ? true : null,
            OtherPhase = otherPhase ? true : null,
        };

    private static FireAttack Attack(FireVehicleFire vehicle, int[] dice, int range = 2, string phase = "PFPh", string? kind = null) =>
        new FireAttack(phase, phase is "PFPh" or "AFPh" || kind == ScenarioA1FireCalculator.BoundingFirstFire ? "phasing" : "non-phasing", true, From, At, [], null, range, true,
            new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("enemy", vehicle.DefinitionId == "defender-tank" ? "attacker-squad" : "defender-squad", At, false, false, false, false, false, false, false)], 3,
            new FireRolls(dice, null, null, null))
        {
            VehicleFire = vehicle,
            FireKind = kind,
        };

    private static FireResolution Resolve(FireAttack attack) => ScenarioA1FireCalculator.Resolve(attack, Reference);

    [Fact]
    public void ATanksBmgAndCmgAddTheirFpEachByItsOwnNormalRange()
    {
        // D3.5: "the FP of a vehicle's various MG/IFE armaments may all be added together for one attack". The PzKpfw IIIH prints 3/5.
        var near = Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6]));
        Assert.Equal(FireResolution.Resolved, near.Disposition);
        Assert.Equal(8m, near.Arithmetic!.TotalFirepower);
        Assert.Equal(["de-tank BMG", "de-tank CMG"], near.Arithmetic.Firers.Select(item => item.UnitId));

        // D1.81, D1.82: a BMG's Normal Range is eight hexes and a CMG's twelve, so at nine hexes only the BMG is halved.
        Assert.Equal(6.5m, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6], range: 9)).Arithmetic!.TotalFirepower);

        // A7.22: twice the Normal Range of each: a CMG alone reaches 24 hexes, and with the BMG the attack ends at 16.
        Assert.Equal(FireResolution.Resolved, Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6], range: 24)).Disposition);
        Assert.Contains("asl.a1.fire.out-of-range", Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6], range: 25)).Reasons);
        Assert.Contains("asl.a1.fire.out-of-range", Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6], range: 17)).Reasons);

        // D1.83: neither needs a CE crew, and a BU tank fires both.
        Assert.Equal(FireResolution.Resolved, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg)], crewExposed: false), [5, 6])).Disposition);
    }

    [Fact]
    public void ACmgFiredOutsideItsTcaAddsCaseAToTheIftDrAndABmgFiresOnlyInsideTheVca()
    {
        FireModifier CaseA(FireResolution result) => Assert.Single(result.Arithmetic!.Drm, item => item.Name == "case-a:de-tank");

        // D3.52: "CMG = T or ST depending on Gun Type". A T turret pays 1 a hexspine, a RST turret 2 and then 1 (C5.11's table).
        Assert.Equal((1m, "D3.52"), (CaseA(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, turn: 1)]), [5, 6]))).Value, "D3.52"));
        Assert.Equal(2m, CaseA(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, turn: 2)]), [5, 6]))).Value);
        Assert.Equal(3m, CaseA(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, turn: 2)], "defender-tank"), [5, 6]))).Value);

        // C5.11: "the Case A DRM is doubled if the firer is in woods/building/rubble".
        Assert.Equal(2m, CaseA(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, turn: 1)], woods: true), [5, 6]))).Value);

        // D3.51: a CMG that fires within a TCA the MA changed in this fire phase pays "the same CA Change penalty as the first weapon which fired".
        Assert.Equal(2m, CaseA(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, maintained: 2)]), [5, 6]))).Value);

        // Within its CA and with nothing to maintain there is no DRM.
        Assert.DoesNotContain(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6])).Arithmetic!.Drm, item => item.Name.StartsWith("case-a", StringComparison.Ordinal));

        // D1.81: a BMG "may fire only at a target that lies within its vehicle's VCA"; C5.13: "a Bounding First Firer must always fire within its current CA".
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg, turn: 1)]), [5, 6])).Reasons);
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, turn: 1)]), [5, 6], phase: "MPh", kind: ScenarioA1FireCalculator.BoundingFirstFire)).Reasons);
        Assert.Equal(FireResolution.Resolved, Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6], phase: "MPh", kind: ScenarioA1FireCalculator.BoundingFirstFire)).Disposition);
    }

    [Fact]
    public void EachMgFiresOnceAPlayerTurnInTheFirePhaseOfTheVehiclesOtherWeapons()
    {
        // D3.5: "may make only one fire attack per Player Turn"; "the remaining weapons must fire in the same phase or forfeit".
        Assert.Contains("asl.a1.fire.firer-already-fired", Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg, fired: true)]), [5, 6])).Reasons);
        Assert.Contains("asl.a1.fire.firer-already-fired", Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg)], otherPhase: true), [5, 6])).Reasons);

        // A weapon named twice, a weapon the counter does not print, and a malfunctioned one put the attack outside.
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.BowMg)]), [5, 6])).Reasons);
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg)], "attacker-halftrack"), [5, 6])).Reasons);
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg, malfunctioned: true)]), [5, 6])).Reasons);

        // D1.321: the CMG of a CE T-34 (a RST turret) does not fire; its BMG does.
        Assert.Contains(Outside, Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)], "defender-tank", crewExposed: true), [5, 6])).Reasons);
        Assert.Equal(FireResolution.Resolved, Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg)], "defender-tank", crewExposed: true), [5, 6])).Disposition);
    }

    [Fact]
    public void AnOriginalTwelveMalfunctionsTheMgAndOfTwoRandomSelectionFindsIt()
    {
        // D3.7: "MG/FT/IFE malfunctions on an Original IFT/CC DR of 12"; the vehicle's fire counter is carried once.
        var one = Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [6, 6]));
        var effect = Assert.Single(one.WeaponEffects!);
        Assert.Equal(("de-tank", FireVehicleWeapon.CoaxialMg, true, false, "prep-fire"), (effect.EquipmentId, effect.VehicleWeapon, effect.Malfunctioned, effect.RateOfFireRetained, effect.FireCounter));
        Assert.All(Resolve(Attack(Tank([Mg(FireVehicleWeapon.CoaxialMg)]), [5, 6])).WeaponEffects!, item => Assert.False(item.Malfunctioned));

        // A9.71: "this rule also applies to multiple MG ... attacks by the same AFV": the roll is asked for, and the highest dr malfunctions.
        var asked = Resolve(Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.CoaxialMg)]), [6, 6]));
        Assert.NotEqual(FireResolution.Resolved, asked.Disposition);
        Assert.Contains(asked.Reasons, reason => reason.StartsWith("asl.a1.fire.roll-missing:weaponSelection:de-tank:bmg,de-tank:cmg", StringComparison.Ordinal));

        var attack = Attack(Tank([Mg(FireVehicleWeapon.BowMg), Mg(FireVehicleWeapon.CoaxialMg)]), [6, 6]);
        var chosen = Resolve(attack with
        {
            Rolls = attack.Rolls! with
            {
                WeaponSelection = new Dictionary<string, int> { ["de-tank:bmg"] = 2, ["de-tank:cmg"] = 5 }
            }
        });
        Assert.Equal(FireResolution.Resolved, chosen.Disposition);
        Assert.Equal([(FireVehicleWeapon.BowMg, false, "prep-fire"), (FireVehicleWeapon.CoaxialMg, true, (string?)null)],
            chosen.WeaponEffects!.Select(item => (item.VehicleWeapon!, item.Malfunctioned, item.FireCounter)));

        // The condition each sets is its own Malfunction counter.
        Assert.Contains((UnitCondition.CmgMalfunctioned, true), ScenarioA1FireFollowUps.WeaponEffectConditions(chosen.WeaponEffects![1], () => false));
        Assert.Contains((UnitCondition.BmgMalfunctioned, true), ScenarioA1FireFollowUps.WeaponEffectConditions(chosen.WeaponEffects[0] with { Malfunctioned = true }, () => false));
    }

    [Fact]
    public void TheVehiclesShotsOfThePlayerTurnDecideWhatEachWeaponMayStillDo()
    {
        // A.15: Defensive First Fire and Final Fire are one fire phase for the side that is not phasing.
        Assert.Equal(("defensive", "defensive", "mph", "afph"), (ScenarioA1FireEligibility.FirePhase("mph", false), ScenarioA1FireEligibility.FirePhase("dfph", false),
            ScenarioA1FireEligibility.FirePhase("mph", true), ScenarioA1FireEligibility.FirePhase("afph", true)));

        var plain = new FireVehicleFire("de-tank", "attacker-tank", From, false, false, false, false, false, false, true, false);
        VehicleWeaponShotFacts[] cmgFirst = [new(FireVehicleWeapon.CoaxialMg, "pfph", 2, false)];

        // The BMG after the CMG, in the same fire phase: unfired, and nothing to maintain (it is not turret-mounted).
        var bmg = ScenarioA1FireEligibility.WithVehicleWeapons(plain, new VehicleWeaponsFacts([FireVehicleWeapon.BowMg], false, false, "pfph", cmgFirst, "t"));
        Assert.Equal((false, (int?)null, (bool?)null), (Assert.Single(bmg.Weapons!).Fired, bmg.Weapons![0].MaintainedCaseA, bmg.OtherPhase));

        // The CMG again: it has fired. In the AFPh: another fire phase.
        Assert.True(Assert.Single(ScenarioA1FireEligibility.WithVehicleWeapons(plain, new VehicleWeaponsFacts([FireVehicleWeapon.CoaxialMg], false, false, "pfph", cmgFirst, "t")).Weapons!).Fired);
        Assert.True(ScenarioA1FireEligibility.WithVehicleWeapons(plain, new VehicleWeaponsFacts([FireVehicleWeapon.BowMg], false, false, "afph", cmgFirst, "t")).OtherPhase);

        // D3.51: the CMG after the MA turned the turret two hexspines pays the MA's Case A; doubled where the MA's was.
        VehicleWeaponShotFacts[] maFirst = [new("ma", "pfph", 2, true)];
        Assert.Equal(4, Assert.Single(ScenarioA1FireEligibility.WithVehicleWeapons(plain, new VehicleWeaponsFacts([FireVehicleWeapon.CoaxialMg], false, false, "pfph", maFirst, "t")).Weapons!).MaintainedCaseA);

        // The MA beside them: a counter the MGs placed is not the MA's; its first shot pays the CMG's Case A; a shot in another phase forfeits it.
        Assert.Equal(new OrdnanceVehicleWeapons(false, true, 2), ScenarioA1OrdnanceEligibility.VehicleWeapons(cmgFirst, "pfph", "t"));
        Assert.Equal(new OrdnanceVehicleWeapons(true, true, null), ScenarioA1OrdnanceEligibility.VehicleWeapons(cmgFirst, "afph", "t"));
        Assert.Equal(new OrdnanceVehicleWeapons(false, false, null), ScenarioA1OrdnanceEligibility.VehicleWeapons([.. cmgFirst, new("ma", "pfph", 0, false)], "pfph", "t"));
        Assert.Equal(new OrdnanceVehicleWeapons(false, false, null), ScenarioA1OrdnanceEligibility.VehicleWeapons([], "pfph", "t"));

        // The words the page lists a MG under.
        Assert.Equal(["is disabled", "has malfunctioned", "has fired", "the vehicle fired in another phase (D3.5)", null],
            new[]
            {
                ScenarioA1FireEligibility.VehicleMgBar(true, true, false, false, false), ScenarioA1FireEligibility.VehicleMgBar(true, false, false, false, false),
                ScenarioA1FireEligibility.VehicleMgBar(false, false, true, false, false), ScenarioA1FireEligibility.VehicleMgBar(false, false, false, true, false),
                ScenarioA1FireEligibility.VehicleMgBar(false, false, false, false, false),
            });
        Assert.NotNull(ScenarioA1FireEligibility.VehicleWeaponsNamedBar("de-tank", false, 0));
        Assert.Null(ScenarioA1FireEligibility.VehicleWeaponsNamedBar("de-ht", true, 0));
        Assert.StartsWith("play.fire-vehicle-vca", ScenarioA1FireEligibility.VehicleWeaponArcBar("de-tank", FireVehicleWeapon.BowMg, 1, false), StringComparison.Ordinal);
        Assert.StartsWith("play.fire-vehicle-tca", ScenarioA1FireEligibility.VehicleWeaponArcBar("de-tank", FireVehicleWeapon.CoaxialMg, 1, true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1FireEligibility.VehicleWeaponArcBar("de-tank", FireVehicleWeapon.CoaxialMg, 1, false));
    }

    [Fact]
    public void TheMaFiresBesideTheMgsOfItsFirePhase()
    {
        OrdnanceShot Shot(OrdnanceVehicleWeapons? weapons, bool marked, int turn = 0) =>
            new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("attacker-tank-1", "attacker-tank", false, 0, false, marked),
                new OrdnanceCrew("attacker-tank-1", "attacker-tank", false, false, false, false, false), At, 3, turn, false, true, 0,
                new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
                new OrdnanceRolls([2, 3], null, null, null, null))
            {
                Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false) { Weapons = weapons },
                VehicleTarget = new OrdnanceVehicleTarget("defender-tank-1", "defender-tank", "front", "front", false, false, false, false, true),
                Ammunition = "ap",
                ScenarioYear = 1942,
            };
        OrdnanceResolution Fire(OrdnanceShot shot) => ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = shot.Rolls! with { ToKill = [6, 6] } }, Ordnance);

        // D3.5: "it makes no difference which weapon fires first": a counter the MGs placed in this fire phase does not bar the MA.
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Fire(Shot(null, marked: true)).Reasons);
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Fire(Shot(new OrdnanceVehicleWeapons(false, false, null), marked: true)).Reasons);
        Assert.DoesNotContain("asl.a1.ordnance.gun-already-fired", Fire(Shot(new OrdnanceVehicleWeapons(false, true, null), marked: true)).Reasons);

        // A weapon fired in another fire phase forfeits the MA's shot.
        Assert.Contains("asl.a1.ordnance.gun-already-fired", Fire(Shot(new OrdnanceVehicleWeapons(true, false, null), marked: false)).Reasons);

        // D3.51: the MA's first shot within the TCA its CMG changed pays the CMG's Case A; a turn of its own pays its own instead.
        var maintained = Fire(Shot(new OrdnanceVehicleWeapons(false, true, 2), marked: true));
        Assert.Equal(("D3.51", 2m), Assert.Single(maintained.ToHit!.Drm.Where(item => item.Name == "case-a:maintained").Select(item => (item.Rule, item.Value))));
        var turned = Fire(Shot(new OrdnanceVehicleWeapons(false, true, 2), marked: true, turn: 1));
        Assert.DoesNotContain(turned.ToHit!.Drm, item => item.Name == "case-a:maintained");
        Assert.Contains(turned.ToHit.Drm, item => item.Name == "case-a:1");
    }

    [Fact]
    public void ATanksWeaponIsRepairedByItsOwnDrAndADisabledMaRecallsIt()
    {
        // D3.7: "by any inherent crew that is not shocked or stunned", once a RPh for each weapon; a disabled weapon is not repaired.
        Assert.Null(ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "bmg", true, false, false, false));
        Assert.StartsWith("play.repair-weapon", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "bmg", false, false, false, false), StringComparison.Ordinal);
        Assert.StartsWith("play.repair-weapon", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "ma", true, true, false, false), StringComparison.Ordinal);
        Assert.StartsWith("play.repair-unit", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "cmg", true, false, true, false), StringComparison.Ordinal);
        Assert.StartsWith("play.repair-unit", ScenarioA1RallyRules.VehicleWeaponRepairBar("de-tank", "cmg", true, false, false, true), StringComparison.Ordinal);

        // D5.341: "Recall is treated the same as a Stun" until its counter is flipped as the Player Turn ends.
        Assert.True(ScenarioA1RallyRules.VehicleCrewImpaired(false, false, false, true, false));
        Assert.False(ScenarioA1RallyRules.VehicleCrewImpaired(false, false, false, true, true));
        Assert.True(ScenarioA1RallyRules.VehicleCrewImpaired(false, true, false, false, false));

        // A 1 removes the weapon's own Malfunction counter; a 6 disables it.
        Assert.Equal([(UnitCondition.BmgMalfunctioned, false)], ScenarioA1RallyRules.VehicleWeaponRepairChanges("bmg", RepairOutcome.Repaired, false));
        Assert.Equal([(UnitCondition.CmgDisabled, true)], ScenarioA1RallyRules.VehicleWeaponRepairChanges("cmg", RepairOutcome.Eliminated, false));
        Assert.Empty(ScenarioA1RallyRules.VehicleWeaponRepairChanges("ma", RepairOutcome.NoChange, false));

        // D3.7: a vehicle whose MA is disabled "is immediately Recalled (5.341) unless that vehicle has Passenger/Towing capability".
        var recalled = ScenarioA1RallyRules.VehicleWeaponRepairChanges("ma", RepairOutcome.Eliminated, false);
        Assert.Contains((UnitCondition.Disabled, true), recalled);
        Assert.Contains((UnitCondition.Recalled, true), recalled);
        Assert.Equal([(UnitCondition.Disabled, true)], ScenarioA1RallyRules.VehicleWeaponRepairChanges("ma", RepairOutcome.Eliminated, true));
        Assert.Contains("Recalls", ScenarioA1RallyRules.VehicleWeaponRepairSummary("de-tank", "ma", true), StringComparison.Ordinal);
    }
}
