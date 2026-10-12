using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 50, Armored combat I. Tasks 50.1 and 50.2: a tank's BMG and CMG on the IFT (D1.81, D1.82, D3.5, pp. 195 and 200), Case A as an IFT
/// DRM (D3.51, D3.52, C5.11, C5.13), a malfunction by weapon (D3.7, A9.71), the repair of each weapon and the Recall of a tank whose MA is
/// disabled (D3.7, D5.341), and the MA beside the vehicle's other weapons. Task 50.3: the MA while moving: Bounding First Fire with Cases C, C1, C2,
/// and C4 (C5.3 to C5.35, D3.3), the AFPh after it (D3.32), a Delay MP (D2.17), and the turret turned with an MP expenditure (D3.12).
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

    // A tank's MA shot at an enemy tank's front: the German PzKpfw IIIH (a T turret, ROF 2) or the Russian T-34 (a RST turret).
    private static OrdnanceShot MaShot(string phase = "MPh", string? kind = ScenarioA1FireCalculator.BoundingFirstFire, int? mpInLos = 99, bool inMotion = false, bool moved = false,
        bool boundingFirer = false, int turn = 0, string side = "phasing", bool russian = false, int[]? dice = null)
    {
        var id = russian ? "defender-tank" : "attacker-tank";
        return new OrdnanceShot(phase, side, russian ? "russian" : "german", new OrdnanceGun(id + "-1", id, false, 0, false, false),
            new OrdnanceCrew(id + "-1", id, false, false, false, false, false), At, 3, turn, false, true, 0,
            new FireAttack(phase, side, null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls(dice ?? [5, 6], null, null, null, null))
        {
            Vehicle = new OrdnanceVehicleFirer(true, inMotion, false, false, false)
            {
                MpInLos = kind is null ? null : mpInLos,
                Moved = moved ? true : null,
                BoundingFirer = boundingFirer ? true : null,
            },
            VehicleTarget = new OrdnanceVehicleTarget("target-1", russian ? "attacker-tank" : "defender-tank", "front", "front", false, false, false, false, true),
            Ammunition = "ap",
            ScenarioYear = 1942,
            FireKind = kind,
        };
    }

    private static OrdnanceResolution Shoot(OrdnanceShot shot) => ScenarioA1OrdnanceCalculator.Resolve(shot with { Rolls = shot.Rolls! with { ToKill = [6, 6] } }, Ordnance);

    private static (string Name, decimal Value) CaseOf(OrdnanceResolution result, string prefix = "case-c") =>
        Assert.Single(result.ToHit!.Drm.Where(item => item.Name.StartsWith(prefix, StringComparison.Ordinal) && !item.Name.StartsWith("case-c4", StringComparison.Ordinal))
            .Select(item => (item.Name, item.Value)));

    [Fact]
    public void ABoundingFirstFirersMaTakesCaseCAndItsSubcasesByTheMpInLos()
    {
        // C5.3: "it must add the +2 DRM of Case B to the applicable Gun Type DRM of Case C"; the EX: "a T or ST Gun Type must add a +4 DRM".
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot())));
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(russian: true))));

        // C5.33: more than three MP in a continuous LOS is Case C; C5.31: two and a half to three is C1; C5.32: at most two is C2. The MP are FRU.
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(mpInLos: 4))));
        Assert.Equal(("case-c1", 5m), CaseOf(Shoot(MaShot(mpInLos: 3))));
        Assert.Equal(("case-c2", 6m), CaseOf(Shoot(MaShot(mpInLos: 2))));
        Assert.Equal(("case-c2", 6m), CaseOf(Shoot(MaShot(mpInLos: 0))));

        // Case B is part of Case C and is not added again; C5.13: Case A never applies, and a shot that would turn the turret is outside.
        Assert.DoesNotContain(Shoot(MaShot()).ToHit!.Drm, item => item.Name == "case-b");
        Assert.Contains("asl.a1.ordnance.covered-arc-fixed", Shoot(MaShot(turn: 1)).Reasons);

        // D3.3: the phasing side's vehicle, in the MPh; a record that names no MP in LOS is incomplete.
        Assert.Contains("asl.a1.ordnance.phase-outside", Shoot(MaShot(side: "non-phasing")).Reasons);
        Assert.Contains("asl.a1.ordnance.phase-outside", Shoot(MaShot(phase: "PFPh")).Reasons);
        Assert.NotEqual(OrdnanceResolution.Resolved, Shoot(MaShot(mpInLos: null)).Disposition);

        // D3.3: "is marked with a Bounding Fire counter [EXC: if the only weapon fired did not exhaust its Multiple ROF]".
        Assert.Equal((false, "bounding-fire"), (Shoot(MaShot(dice: [5, 6])).Gun!.RateOfFireKept, Shoot(MaShot(dice: [5, 6])).Gun!.FireCounter));
        Assert.Equal((true, (string?)null), (Shoot(MaShot(dice: [2, 6])).Gun!.RateOfFireKept, Shoot(MaShot(dice: [2, 6])).Gun!.FireCounter));
        Assert.Equal(UnitCondition.BoundingFire, ScenarioA1OrdnanceEventRules.ShotMarker("MPh", "bounding-fire"));
        Assert.Equal(UnitCondition.FirstFire, ScenarioA1OrdnanceEventRules.ShotMarker("MPh", "first-fire"));
        Assert.Null(ScenarioA1OrdnanceEventRules.ShotMarker("MPh", null));
    }

    [Fact]
    public void AMotionFirerDoublesTheLowerDrOfItsToHitDr()
    {
        // C5.35 and its EX: Case C, C1, or C2 as applicable, and the lower dr doubled: a DR of 1 and 4 adds 1, of 2 and 2 adds 2.
        var moving = Shoot(MaShot(mpInLos: 2, inMotion: true, dice: [1, 4]));
        Assert.Equal(("case-c2", 6m), CaseOf(moving));
        Assert.Equal(1m, Assert.Single(moving.ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.Equal(5 + 6 + 1 + 1, moving.ToHit.FinalDr);
        Assert.Equal(2m, Assert.Single(Shoot(MaShot(inMotion: true, dice: [2, 2])).ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.DoesNotContain(Shoot(MaShot(dice: [1, 4])).ToHit!.Drm, item => item.Name == "case-c4:lower-dr");

        // D2.42: in its AFPh too, with Case C; D2.4: never in the PFPh; the DEFENDER's vehicle in Motion is not built.
        var afph = Shoot(MaShot(phase: "AFPh", kind: null, inMotion: true, dice: [3, 4]));
        Assert.Equal(("case-c", 4m), CaseOf(afph));
        Assert.Equal(3m, Assert.Single(afph.ToHit!.Drm, item => item.Name == "case-c4:lower-dr").Value);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Shoot(MaShot(phase: "PFPh", kind: null, inMotion: true)).Reasons);
        Assert.Contains("asl.a1.ordnance.vehicle-fire-outside", Shoot(MaShot(phase: "DFPh", kind: null, inMotion: true, side: "non-phasing")).Reasons);
    }

    [Fact]
    public void InTheAfphAVehicleThatMovedOrFiredInItsMphUsesCaseC()
    {
        // C5.3: "a vehicle ... which has entered a new hex/hexside during its MPh but does not fire until its AFPh is using Bounding Fire and always
        // uses Case C"; C5.2: one that did not takes Case B; D3.32: the shot on the ROF its Bounding First Fire kept uses Case C.
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null, moved: true))));
        Assert.DoesNotContain(Shoot(MaShot(phase: "AFPh", kind: null, moved: true)).ToHit!.Drm, item => item.Name == "case-b");
        Assert.Equal(("case-c", 4m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null, boundingFirer: true))));
        Assert.Equal(("case-b", 2m), CaseOf(Shoot(MaShot(phase: "AFPh", kind: null)), "case-b"));
        Assert.DoesNotContain(Shoot(MaShot(phase: "AFPh", kind: null)).ToHit!.Drm, item => item.Name.StartsWith("case-c", StringComparison.Ordinal));

        // D3.32: "if the vehicle did not exhaust its Multiple ROF during its MPh, and did not fire any other weapons ... it may fire that Multiple
        // ROF weapon (only) again once during its AFPh". A Bounding Fire counter says the ROF is spent.
        VehicleWeaponShotFacts[] ma = [new("ma", "mph", 0, false, 0)];
        Assert.False(ScenarioA1OrdnanceEligibility.VehicleWeapons(ma, "afph", "t").OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons(ma, "afph", "t", boundingFireMarked: true).OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons([.. ma, new(FireVehicleWeapon.CoaxialMg, "mph", 0, false)], "afph", "t").OtherPhase);
        Assert.True(ScenarioA1OrdnanceEligibility.VehicleWeapons([new("ma", "pfph", 0, false)], "afph", "t").OtherPhase);
    }

    [Fact]
    public void TheMphGivesAVehicleItsShotsADelayMpAndATurretTurnedWithAnMp()
    {
        // D3.3: the phasing side's vehicle fires as Bounding First Fire when it may; the other side at the moving stack while its window is open.
        Assert.Equal((ScenarioA1FireCalculator.BoundingFirstFire, (string?)null), ScenarioA1OrdnanceEligibility.MphShotKind(true, true, () => true, false, () => false));
        Assert.StartsWith("play.ordnance-bounding", ScenarioA1OrdnanceEligibility.MphShotKind(true, true, () => false, false, () => false).Refusal, StringComparison.Ordinal);
        Assert.Equal(ScenarioA1OrdnanceEligibility.MphWindowRefusal, ScenarioA1OrdnanceEligibility.MphShotKind(true, false, () => true, false, () => false).Refusal);
        Assert.Equal(("first-fire", (string?)null), ScenarioA1OrdnanceEligibility.MphShotKind(false, true, () => false, true, () => true));
        Assert.Equal(ScenarioA1OrdnanceEligibility.MphWindowRefusal, ScenarioA1OrdnanceEligibility.MphShotKind(false, false, () => false, false, () => true).Refusal);

        // C5.3: "may not fire again until it has expended another MP (even if only a Delay MP)".
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", null, 0));
        Assert.StartsWith("play.ordnance-bounding-mp", ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 0), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 1));
        Assert.Null(ScenarioA1OrdnanceEligibility.BoundingShotBar("de-tank", 0, 0, rofSpent: true));
        Assert.StartsWith("play.ordnance-bounding-arc", ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", true, 1), StringComparison.Ordinal);
        Assert.Null(ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", true, 0));
        Assert.Null(ScenarioA1OrdnanceMapRules.BoundingArcRefusal("de-tank", false, 2));

        // D2.17, D3.3: a Delay MP while stopped or before starting, for one MP of those left (in half MP).
        Assert.Null(ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", false, 0, 26));
        Assert.StartsWith("play.move-vehicle-delay", ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", true, 0, 26), StringComparison.Ordinal);
        Assert.StartsWith("play.move-vehicle-mp", ScenarioA1VehicleMovementCalculator.DelayBar("de-tank", false, 25, 26), StringComparison.Ordinal);

        // D3.12: the turret turns with a Delay, a stop, a VCA change, or an entry; a vehicle with a turret; not in woods, a building, or rubble.
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "delay", true, true, false));
        Assert.Null(ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "enter", true, true, false));
        Assert.Contains("no turret", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-ht", "stop", false, true, false), StringComparison.Ordinal);
        Assert.Contains("hexspine", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, false, false), StringComparison.Ordinal);
        Assert.Contains("Delay MP, a stop", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "start", true, true, false), StringComparison.Ordinal);
        Assert.Contains("doubles", ScenarioA1VehicleMovementCalculator.TurretStepBar("de-tank", "stop", true, true, true), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "rear")]
    [InlineData(2, "rear")]
    [InlineData(3, "side")]
    [InlineData(4, "side")]
    [InlineData(5, "front")]
    [InlineData(6, "front")]
    public void FromInsideTheTargetsHexTheColoredDrGivesTheTargetFacing(int colored, string facing)
    {
        // D3.2: "the colored dr of the To Hit DR determines the Target Facing: 1-2 Rear, 3-4 Side, 5-6 Front".
        var shot = MaShot(phase: "PFPh", kind: null) with
        {
            Range = 0,
            SameHex = true,
            VehicleTarget = new OrdnanceVehicleTarget("target-1", "defender-tank", null, null, false, false, false, false, true),
            Rolls = new OrdnanceRolls([colored, 1], null, null, null, null),
        };
        var result = Shoot(shot);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(facing, result.Kill!.TargetFacing);

        // C5.5: Case E, +2, doubled in woods, a building, or rubble; Case L does not apply to it.
        Assert.Equal(2m, Assert.Single(result.ToHit!.Drm, item => item.Name == "case-e").Value);
        Assert.Equal(4m, Assert.Single(Shoot(shot with { FirerInWoodsOrBuilding = true }).ToHit!.Drm, item => item.Name == "case-e").Value);
        Assert.DoesNotContain(result.ToHit.Drm, item => item.Name == "case-l");

        // A shot from inside the hex names no facing of its own, and one from outside it must.
        Assert.Contains("asl.a1.ordnance.vehicle-target-outside", Shoot(shot with { VehicleTarget = shot.VehicleTarget! with { HullFacing = "front" } }).Reasons);
        Assert.NotEqual(OrdnanceResolution.Resolved, Shoot(shot with { SameHex = null, Range = 1 }).Disposition);
    }
}
