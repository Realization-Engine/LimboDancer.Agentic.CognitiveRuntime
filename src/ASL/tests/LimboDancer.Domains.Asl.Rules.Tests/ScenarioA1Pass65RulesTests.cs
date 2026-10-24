using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 65, Passengers, Riders, and crews. Increment 2a: the crew kind (A1.123, D5.1; ruling R65.2), the vehicle crews of catalog 1.18.0
/// (p. 695; ruling R65.3), the relaxed catalog check (ruling R65.4), and the definition a crew that leaves its vehicle is placed as (ruling R65.19).
/// </summary>
public sealed class ScenarioA1Pass65RulesTests
{
    private static readonly ScenarioA1OrdnanceReference Ordnance = new ScenarioA1OrdnancePackage().Reference;

    [Fact]
    public void AVehicleCrewIsACrewAndASquadIsNot()
    {
        Assert.True(ScenarioA1Definitions.IsCrew("asl:crew"));
        Assert.True(ScenarioA1Definitions.IsCrew("asl:vehicle-crew"));
        Assert.False(ScenarioA1Definitions.IsCrew("asl:squad"));
        Assert.False(ScenarioA1Definitions.IsCrew("asl:half-squad"));
        Assert.False(ScenarioA1Definitions.IsCrew(null));
    }

    [Fact]
    public void AVehicleCrewStacksCountsAndFightsAsACrew()
    {
        // A1.123, D5.1: all of a crew's capabilities but its Strength Factor: a HS-equivalent in stacking, 2 VP, US# 2, CCV 4's group, an MMC.
        Assert.Equal(StackUnitKind.HalfSquadOrCrew, ScenarioA1Stacking.KindOf("asl:vehicle-crew"));
        Assert.Equal(2, ScenarioA1VictoryCalculator.VictoryPoints("asl:vehicle-crew", () => null, () => 0));
        Assert.Equal(2, ScenarioA1Definitions.UnitSize("asl:vehicle-crew"));
        Assert.Equal(0.5, ScenarioA1SetupCalculator.SquadEquivalent("asl:vehicle-crew"));
        Assert.True(ScenarioA1SetupCalculator.Mmc("asl:vehicle-crew"));
        Assert.Contains("asl:vehicle-crew", ScenarioA1Definitions.MmcKinds);

        // C11.2, C11.3: a vehicle crew manning a Gun keeps it Emplaced and is a qualified crew.
        Assert.True(ScenarioA1OrdnanceEligibility.Emplaced("asl:vehicle-crew", () => false));
        Assert.Null(ScenarioA1OrdnanceEligibility.NonQualified(true, "asl:vehicle-crew", false));
        Assert.False(ScenarioA1OrdnanceProjection.LosesEmplacement("asl:gun", "asl:vehicle-crew"));
    }

    [Fact]
    public void TheCatalogHoldsTheEightVehicleCrewsOfTheChart()
    {
        // The National Capabilities Chart (p. 695), the design's R65.3 table: FP-range-morale, broken morale.
        (string Nationality, int Morale, int Broken)[] crews =
            [("german", 7, 7), ("russian", 7, 7), ("british", 7, 7), ("american", 6, 7), ("finnish", 7, 7), ("italian", 6, 5), ("axis-minor", 6, 5), ("french", 7, 6)];
        foreach (var (nationality, morale, broken) in crews)
        {
            var crew = Ordnance.Fire.Definitions[nationality + "-vehicle-crew"];
            Assert.Equal(("asl:vehicle-crew", nationality, 1, 2, morale, broken, true), (crew.Kind, crew.Nationality, crew.Firepower, crew.Range, crew.Morale,
                crew.BrokenMorale, crew.SelfRally));
        }

        // D6.81 (p. 206): the Bren carrier carries four PP.
        Assert.Equal(4, Ordnance.Fire.Definitions["campaign-british-bren-carrier"].PassengerCapacity);
    }

    [Fact]
    public void ACrewLeavesItsVehicleAsItsNationalitysVehicleCrew()
    {
        var definitions = Ordnance.Fire.Definitions.Values;
        Assert.Equal("german-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "german"));
        Assert.Equal("italian-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "italian"));
        Assert.Equal("finnish-vehicle-crew", ScenarioA1Definitions.LeavingCrewDefinition(definitions, "finnish"));
        Assert.Null(ScenarioA1Definitions.LeavingCrewDefinition(definitions, "japanese"));
        Assert.Null(ScenarioA1Definitions.LeavingCrewDefinition(definitions, null));
    }

    [Fact]
    public void APackageReadsItsCatalogAtAnOlderMinorVersion()
    {
        const string catalog = "asl-scenario-a1";
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.18.0"));
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.17.0"));
        Assert.True(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.1.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "1.19.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "2.0.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", "asl-scenario-a1-synthetic", "1.0.0"));
        Assert.False(ScenarioA1Definitions.CatalogServes(catalog, "1.18.0", catalog, "not-a-version"));
        Assert.Null(ScenarioA1OrdnanceEligibility.CatalogRefusal(catalog, "1.18.0", catalog, "1.17.0"));
        Assert.StartsWith("play.cc-catalog", ScenarioA1CloseCombatFactBuilders.CatalogBar(catalog, "2.0.0", catalog, "1.18.0"), StringComparison.Ordinal);
    }

    // Increment 2b: Passengers and Riders as targets (D.8, D.8A, D.8B, D5.31, D6.61 to D6.66, D6.7, D6.9, A7.211; rulings R65.5 to R65.9). Two Russian
    // squads fire at a German SPW 251/1 carrying a 4-6-7 squad, or a Russian T-34 fires at it or at a German truck.
    private const string At = "bd01:G5:0";
    private const string FiredFrom = "bd01:G7:0";
    private static readonly int[] CheckDice = [2, 2];

    private static FirePrc Aboard(string id = "de-s", string definition = "attacker-squad", bool broken = false, bool pinned = false) =>
        new(id, definition, FirePrc.Passenger, broken, pinned)
        {
            Elr = 3
        };

    private static FireFirer Squad(string id, string from) => new(id, "defender-squad", from, false, false, false, false, false)
    {
        KnownEnemyInLos = true,
        Captors = [],
    };

    private static FireVehicle Halftrack(bool crewExposed, IReadOnlyList<FirePrc>? prc, string definition = "attacker-halftrack") =>
        new("de-ht", definition, At, crewExposed, false, false, false)
        {
            Prc = prc
        };

    /// <summary>Two Russian squads' 8 FP at the vehicle's Location, open ground unless given, from two hexes away at its level unless given.</summary>
    private static FireAttack AtVehicle(FireVehicle vehicle, string terrain = "open-ground", int range = 2, string from = FiredFrom, int? levelAbove = null) =>
        new FireAttack("PFPh", "phasing", true, from, At, [Squad("ru-1", from), Squad("ru-2", from)], null, range, levelAbove is null, new FireLos(false, 0, true, false), 7,
            terrain, [], 3, null)
        {
            Vehicles = [vehicle],
            TargetLevelAbove = levelAbove,
        };

    private static FireRolls Rolled(FireRolls rolls, string key, IReadOnlyDictionary<string, int[]>? survival)
    {
        var split = key.IndexOf(':', StringComparison.Ordinal);
        var (kind, unit) = (key[..split], key[(split + 1)..]);
        return kind switch
        {
            "checks" => rolls with { Checks = new Dictionary<string, IReadOnlyList<int>>(rolls.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = CheckDice } },
            "crewCheck" => rolls with { CrewChecks = new Dictionary<string, IReadOnlyList<int>>(rolls.CrewChecks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = CheckDice } },
            "randomSelection" => rolls with { RandomSelection = unit.Split(',').ToDictionary(id => id, _ => 3) },
            "prcSurvival" => rolls with
            {
                PrcSurvival = new Dictionary<string, IReadOnlyList<int>>(rolls.PrcSurvival ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = survival![unit] }
            },
            _ => throw new InvalidOperationException("An unexpected roll: " + key),
        };
    }

    private static FireResolution Fired(FireAttack attack, int[] dice)
    {
        var rolls = new FireRolls(dice, null, null, null);
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Ordnance.Fire);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            rolls = Rolled(rolls, reason["asl.a1.fire.roll-missing:".Length..], null);
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    /// <summary>The Russian T-34's HE at the vehicle's front three hexes away, hitting its hull; the shot carries pass 60's and pass 65's facts.</summary>
    private static OrdnanceShot TankAt(string definition, int[] toKill, IReadOnlyList<FirePrc>? prc, bool crewExposed = true, string ammunition = "he") =>
        new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("firer", "defender-tank", false, 0, false, false),
            new OrdnanceCrew("firer", "defender-tank", false, false, false, false, false), At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 3, null),
            new OrdnanceRolls([2, 1], null, null, null, null) { ToKill = toKill, CollateralCheck = CheckDice, CrewSurvival = [6, 6] })
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
            VehicleTarget = new OrdnanceVehicleTarget("de-v", definition, "front", null, false, false, false, false, false)
            {
                CrewExposed = crewExposed ? true : null,
                Prc = prc
            },
            Ammunition = ammunition,
            ScenarioYear = 1942,
            SpecificCollateral = true,
        };

    private static OrdnanceResolution Shot(OrdnanceShot shot, IReadOnlyDictionary<string, int[]>? survival = null)
    {
        var rolls = shot.Rolls!;
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
            {
                Rolls = rolls
            }, Ordnance);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.ordnance.roll-missing:prc:", StringComparison.Ordinal))
            {
                return result;
            }

            rolls = rolls with
            {
                Prc = Rolled(rolls.Prc ?? new FireRolls(null, null, null, null), reason["asl.a1.ordnance.roll-missing:prc:".Length..], survival)
            };
        }

        throw new InvalidOperationException("The shot asked for too many rolls.");
    }

    private static int Column(int fp) => Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp);

    [Fact]
    public void AGeneralCollateralAttackReachesTheCePassengersOfAHalftrack()
    {
        // D.8B, D5.31, D6.62: the squads' Original DR of 6 on the 8 column, +2 for CE, against the 4-6-7 aboard the CE SPW 251/1, as against its crew.
        var result = Fired(AtVehicle(Halftrack(true, [Aboard()])), [3, 3]);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var effect = Assert.Single(result.VehicleEffects!);
        var attack = Assert.Single(effect.PrcAttacks!);
        Assert.Equal((FirePrcAttack.General, 8, Ordnance.Fire.Result(8, Column(result.Arithmetic!.ColumnFp!.Value))), (attack.Kind, attack.FinalDr, attack.Result));
        Assert.Equal(["crew-exposed"], attack.Drm.Select(item => item.Name));
        Assert.Equal(["de-s"], attack.Effects.Select(item => item.UnitId));
        Assert.Equal(effect.FinalDr, attack.FinalDr);

        // An older record carries no PRC: none is attacked.
        Assert.Null(Assert.Single(Fired(AtVehicle(Halftrack(true, null)), [3, 3]).VehicleEffects!).PrcAttacks);
    }

    [Fact]
    public void ABuHalftracksPassengersAreAttackedOnlyFromAboveOrByTpbf()
    {
        // D6.61: a BU halftrack's Passengers are not attacked from two hexes away at its level.
        var bu = Assert.Single(Fired(AtVehicle(Halftrack(false, [Aboard()])), [3, 3]).VehicleEffects!);
        Assert.Null(bu.PrcAttacks);

        // A7.211: TPBF from within their Location, with the +2 CE DRM.
        var tpbf = Assert.Single(Assert.Single(Fired(AtVehicle(Halftrack(false, [Aboard()]), range: 0, from: At), [3, 3]).VehicleEffects!).PrcAttacks!);
        Assert.Equal((8, 2m), (tpbf.FinalDr, tpbf.Drm.Single(item => item.Name == "crew-exposed").Value));

        // D6.61: from three levels above at a range of one, an advantage two levels beyond the range: +2 less 2, so no CE DRM.
        var above = Assert.Single(Assert.Single(Fired(AtVehicle(Halftrack(false, [Aboard()]), range: 1, levelAbove: -3), [3, 3]).VehicleEffects!).PrcAttacks!);
        Assert.Equal(6, above.FinalDr);
        Assert.DoesNotContain(above.Drm, item => item.Name == "crew-exposed");

        // D6.62, A7.821: a pinned or broken Passenger of a CE halftrack is BU too; D6.66: a broken one is put under DM by FP that could break a CE one.
        var broken = Assert.Single(Fired(AtVehicle(Halftrack(true, [Aboard(broken: true)])), [3, 3]).VehicleEffects!);
        Assert.Null(broken.PrcAttacks);
        Assert.Equal(["de-s"], broken.PrcDesperation!);
    }

    [Fact]
    public void ATrucksPassengersTakeTheTemAndNoCeDrm()
    {
        // D6.7, A7.308: a truck's PRC are always Vulnerable; in woods they take its +1 TEM, never a CE DRM. The truck itself is on the Vehicle line.
        var result = Fired(AtVehicle(Halftrack(false, [Aboard("de-h", "attacker-half-squad")], "attacker-truck"), terrain: "woods"), [5, 6]);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        var effect = Assert.Single(result.VehicleEffects!);
        Assert.Equal(FireVehicleEffect.None, effect.Result);
        var attack = Assert.Single(effect.PrcAttacks!);
        Assert.Equal(["tem:woods"], attack.Drm.Select(item => item.Name));
        Assert.Equal(12, attack.FinalDr);
    }

    [Fact]
    public void TheCeDrmTakesNeitherAPositiveTemNorHeightAdvantageWithTheFact()
    {
        // D5.31 (ruling R65.7): with the PRC fact, an AFV in woods is attacked (ruling R25.6 lifted), its crew with +2 alone.
        Assert.Contains("asl.a1.fire.vehicle-outside", ScenarioA1FireCalculator.Precheck(AtVehicle(Halftrack(true, null), terrain: "woods"), Ordnance.Fire));
        var woods = Assert.Single(Fired(AtVehicle(Halftrack(true, []), terrain: "woods"), [3, 3]).VehicleEffects!);
        Assert.Equal(["crew-exposed"], woods.Drm.Select(item => item.Name));

        // B10.31: the crew of a vehicle above its firers is no longer given Height Advantage on top of its CE DRM; an older record still is.
        var climbing = AtVehicle(Halftrack(true, []), levelAbove: 1) with
        {
            HeightAdvantage = true
        };
        Assert.Equal(["crew-exposed"], Assert.Single(Fired(climbing, [3, 3]).VehicleEffects!).Drm.Select(item => item.Name));
        var older = climbing with
        {
            Vehicles = [Halftrack(true, null)]
        };
        Assert.Equal(["height-advantage", "crew-exposed"], Assert.Single(Fired(older, [3, 3]).VehicleEffects!).Drm.Select(item => item.Name));
    }

    [Fact]
    public void ASpecificCollateralAttackReachesThePassengersAfterAMissedTkDr()
    {
        // D.8A: the T-34's HE does not destroy the SPW 251/1 on a TK DR of 11; its CE Passengers take the HE FP on the same DR with +2 alone.
        var result = Shot(TankAt("attacker-halftrack", [5, 6], [Aboard()]));
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(OrdnanceKill.None, result.Kill!.Result);
        var attack = Assert.Single(result.SpecificCollateral!.Effect.PrcAttacks!);
        Assert.Equal((FirePrcAttack.Specific, 13), (attack.Kind, attack.FinalDr));
        Assert.Equal(["crew-exposed"], attack.Drm.Select(item => item.Name));

        // A truck's PRC take it with no DRM (D6.71); an older record carries none and makes no attack at a truck.
        // C7.311, C7.321: the 76mm's AP has a Final TK# of 9 at a truck; a TK DR of 10 is a miss.
        var truck = Shot(TankAt("attacker-truck", [5, 5], [Aboard("de-h", "attacker-half-squad")], crewExposed: false, ammunition: "ap"));
        Assert.Equal((OrdnanceKill.None, 9), (truck.Kill!.Result, truck.Kill.FinalTk));
        var onTruck = Assert.Single(truck.SpecificCollateral!.Effect.PrcAttacks!);
        Assert.Empty(onTruck.Drm);
        Assert.Equal(10, onTruck.FinalDr);
        Assert.Null(Shot(TankAt("attacker-truck", [5, 5], null, crewExposed: false, ammunition: "ap")).SpecificCollateral);
    }

    [Fact]
    public void NoCollateralAttackAfterAKillButEachPassengerRollsForSurvival()
    {
        // D.8: none "if the vehicle is destroyed outright"; D6.9: each PRC rolls its own Survival DR against the truck's cs# 6, +1 if broken. C7.7: a TK DR
        // below the Final TK# and more than half of it eliminates the truck without burning it.
        var finalTk = Shot(TankAt("attacker-truck", [6, 6], null, crewExposed: false)).Kill!.FinalTk;
        var dr = finalTk - 1;
        Assert.True(dr * 2 > finalTk);
        var survival = new Dictionary<string, int[]> { ["de-h"] = [3, 3], ["de-s"] = [3, 3] };
        var result = Shot(TankAt("attacker-truck", [dr / 2, dr - (dr / 2)], [Aboard("de-h", "attacker-half-squad"), Aboard(broken: true)], crewExposed: false), survival);
        Assert.Equal(OrdnanceResolution.Resolved, result.Disposition);
        Assert.Equal(OrdnanceKill.Eliminated, result.Kill!.Result);
        Assert.Null(result.SpecificCollateral);
        var rolled = result.Kill.PrcSurvival!;
        Assert.Equal([("de-h", 0, 6, true), ("de-s", 1, 7, false)], rolled.Select(item => (item.UnitId, item.Drm, item.FinalDr, item.Survived)));
        Assert.All(rolled, item => Assert.Equal(6, item.CrewSurvival));

        // A burning wreck takes its PRC with it: no Survival DR (A7.308, D5.6).
        Assert.Null(Shot(TankAt("attacker-truck", [1, 1], [Aboard("de-h", "attacker-half-squad")], crewExposed: false), survival).Kill!.PrcSurvival);
    }

    // Increment 2c: Passenger action (D6.1, D6.31, D6.5, D6.62 to D6.65, D6.651, D7.11, D7.16, A11.611, A11.71; rulings R65.10 to R65.13).

    private static FireDefinition Definition(string id) => Ordnance.Fire.Definitions[id];

    private static MountedFireFacts Mounted(string vehicle, bool crewExposed = true, bool moved = false, bool motion = false, bool bypass = false, string phase = "PFPh",
        string? weapon = null) =>
        new("de-s", "de-v", Definition(vehicle), crewExposed, true, moved, motion, bypass, phase,
            weapon is null ? [] : [new MountedWeaponFacts(weapon, Ordnance.Fire.Definitions.GetValueOrDefault(weapon))]);

    private static decimal Times(MountedFireVerdict verdict) => verdict.Multipliers.Aggregate(1m, (value, item) => value * item.Value);

    [Fact]
    public void ATrucksPassengersFireAtHalfAndAtAQuarterWhenItMoves()
    {
        // D6.1, D6.72: halved for Mounted Fire; halved again for Bounding First Fire in its MPh after it moved, and for Motion.
        var stopped = ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck"));
        Assert.Null(stopped.Refusal);
        Assert.Equal((0.5m, "mounted-fire", "D6.1"), (Times(stopped), stopped.Multipliers[0].Name, stopped.Multipliers[0].Rule));
        Assert.Equal(0.25m, Times(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", moved: true, phase: "MPh"))));
        Assert.Equal(0.25m, Times(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", motion: true))));

        // The AFPh's halving is A7.24's Advancing Fire, which the Fire package applies to every Infantry firer: Mounted Fire adds only its own.
        Assert.Equal(0.5m, Times(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", moved: true, phase: "AFPh"))));
    }

    [Fact]
    public void ACeHalftracksPassengersFireAtFullUnlessItMovedAndABuOnesDoNot()
    {
        // D6.63: never halved for Mounted Fire; halved as Bounding First Fire when it moved; D6.62: only while CE.
        var unmoved = ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-halftrack"));
        Assert.Equal((null, 0), (unmoved.Refusal, unmoved.Multipliers.Count));
        var moved = ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-halftrack", moved: true, phase: "MPh"));
        Assert.Equal((0.5m, "D6.63"), (Times(moved), moved.Multipliers[0].Rule));
        Assert.StartsWith("play.mounted-fire: de-s rides de-v BU", ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-halftrack", crewExposed: false)).Refusal);
        Assert.Contains("D6.62", ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-halftrack") with
        {
            PassengerExposed = false
        }).Refusal);

        // A truck's Passengers have no CE state to lose; a vehicle in Bypass fires from its CAFP, which is pass 100's.
        Assert.Null(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", crewExposed: false)).Refusal);
        Assert.Contains("Bypass", ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-halftrack", bypass: true)).Refusal);
    }

    [Fact]
    public void APassengerFiresOnlyALmgAPiatOrADc()
    {
        // D6.1 (p. 205): "LMG, PIAT, and Thrown DC are the only SW which can be fired by Passengers".
        Assert.Null(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", weapon: "attacker-lmg")).Refusal);
        Assert.Null(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", weapon: "attacker-dc")).Refusal);
        Assert.Equal("play.mounted-fire: de-s may not fire attacker-mmg from de-v: a Passenger fires only a LMG, a PIAT, or a Thrown DC (D6.1)",
            ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", weapon: "attacker-mmg")).Refusal);
        Assert.NotNull(ScenarioA1PassengerActionRules.MountedFire(Mounted("attacker-truck", weapon: "attacker-ft")).Refusal);
        Assert.True(ScenarioA1PassengerActionRules.Allowed(new FireDefinition("british-piat", "asl:latw", "british", null, null, null, null, null, null, null) { LatwType = "piat" }));
        Assert.False(ScenarioA1PassengerActionRules.Allowed(null));
    }

    [Fact]
    public void ABrokenPassengerRoutsBeneathAStoppedVehicleAndMustWhenItsCrewIsGone()
    {
        // D6.1 (p. 204): it may stay aboard or rout beneath a Stopped vehicle; it must when the inherent crew is eliminated, breaks, or Abandons.
        Assert.Equal(ScenarioA1RoutCalculator.MayRoutBeneath, ScenarioA1RoutCalculator.RoutBeneath(true, true, false, false, false));
        Assert.Equal(ScenarioA1RoutCalculator.StaysAboard, ScenarioA1RoutCalculator.RoutBeneath(true, false, false, false, false));
        Assert.Equal(ScenarioA1RoutCalculator.StaysAboard, ScenarioA1RoutCalculator.RoutBeneath(false, true, false, false, true));
        Assert.Equal(ScenarioA1RoutCalculator.MustRoutBeneath, ScenarioA1RoutCalculator.RoutBeneath(true, true, false, false, true));
        Assert.Equal(ScenarioA1RoutCalculator.MustRoutBeneath, ScenarioA1RoutCalculator.RoutBeneath(true, false, true, false, false));
        Assert.Equal(ScenarioA1RoutCalculator.MustRoutBeneath, ScenarioA1RoutCalculator.RoutBeneath(true, true, false, true, false));

        // D5.311: all its MF, so no route and no Low Crawl; and only a Passenger routs beneath.
        Assert.Null(ScenarioA1RoutCalculator.RoutBeneathBar("de-s", "de-v", ScenarioA1RoutCalculator.MayRoutBeneath, 0, false));
        Assert.NotNull(ScenarioA1RoutCalculator.RoutBeneathBar("de-s", "de-v", ScenarioA1RoutCalculator.MayRoutBeneath, 1, false));
        Assert.NotNull(ScenarioA1RoutCalculator.RoutBeneathBar("de-s", "de-v", ScenarioA1RoutCalculator.StaysAboard, 0, false));
        Assert.StartsWith("play.rout-beneath: de-s is not a Passenger", ScenarioA1RoutCalculator.RoutBeneathBar("de-s", null, ScenarioA1RoutCalculator.MayRoutBeneath, 0, false));
    }

    [Fact]
    public void D664GroupsPassengersWithTheirVehicleAndACeHalftracksWithInfantry()
    {
        FireGroupMemberFacts OnFoot(string id) => new(id, null, null, false);
        FireGroupMemberFacts Truck(string id, string vehicle = "de-truck") => new(id, vehicle, FirePrc.Passenger, false);
        FireGroupMemberFacts Halftrack(string id, string vehicle = "de-ht") => new(id, vehicle, FirePrc.Passenger, true);
        string? Bar(FireGroupMemberFacts[] firers, params FireGroupLeaderFacts[] leaders) => ScenarioA1PassengerActionRules.FireGroupBar(firers, leaders, []);

        // D6.64: Passengers of one truck together; not with Infantry, nor with another truck's.
        Assert.Null(Bar([Truck("a"), Truck("b")]));
        Assert.StartsWith("play.fire-group-prc: a rides de-truck", Bar([Truck("a"), OnFoot("c")]));
        Assert.NotNull(Bar([Truck("a"), Truck("b", "de-truck-2")]));

        // A CE halftrack's Passengers with Infantry and another CE halftrack's Passengers, not with a truck's.
        Assert.Null(Bar([Halftrack("a"), OnFoot("c"), Halftrack("b", "de-ht-2")]));
        Assert.NotNull(Bar([Halftrack("a"), Truck("b")]));

        // Riders only with the Riders of their vehicle.
        Assert.NotNull(Bar([new FireGroupMemberFacts("r", "de-tank", FirePrc.Rider, false), OnFoot("c")]));

        // D6.65: a CE Passenger leader directs his halftrack's Passengers, and the hex's others only while it has not moved; BU, nobody.
        var leader = new FireGroupLeaderFacts("de-l", "de-ht", true, true, false, false);
        Assert.Null(Bar([Halftrack("a"), OnFoot("c")], leader));
        Assert.Contains("moved this phase", Bar([Halftrack("a"), OnFoot("c")], leader with
        {
            Moved = true
        }));
        Assert.Null(Bar([Halftrack("a")], leader with
        {
            Moved = true
        }));
        Assert.Contains("BU", Bar([Halftrack("a")], leader with
        {
            Exposed = false
        }));
        Assert.NotNull(Bar([Truck("a"), Truck("b")], new FireGroupLeaderFacts("de-l", "de-truck-2", true, false, false, false)));

        // D3.4, D6.65: an Armor Leader only inside his halftrack's group; a leader on foot directs a CE halftrack's Passengers, not a truck's.
        var armor = new FireGroupLeaderFacts("al", "de-ht", true, true, false, true);
        Assert.NotNull(Bar([Halftrack("a")], armor));
        Assert.Null(ScenarioA1PassengerActionRules.FireGroupBar([Halftrack("a")], [armor], ["de-ht"]));
        var onFoot = new FireGroupLeaderFacts("de-l", null, true, false, false, false);
        Assert.Null(Bar([Halftrack("a"), OnFoot("c")], onFoot));
        Assert.NotNull(Bar([Truck("a")], onFoot));
    }

    [Fact]
    public void APassengerLeadersReachIsHisVehicle()
    {
        // D6.651: Rally, MC, TC, and LLMC reach the other Passengers of his vehicle; others only from a CE halftrack that has not moved.
        Assert.True(ScenarioA1PassengerActionRules.LeaderReaches("de-ht", false, "de-ht", true));
        Assert.False(ScenarioA1PassengerActionRules.LeaderReaches("de-ht", false, null, false));
        Assert.True(ScenarioA1PassengerActionRules.LeaderReaches("de-ht", true, null, false));
        Assert.False(ScenarioA1PassengerActionRules.LeaderReaches("de-truck", false, "de-ht", false));

        // A leader on foot reaches the Passengers of a vehicle that has not moved.
        Assert.True(ScenarioA1PassengerActionRules.LeaderReaches(null, false, "de-ht", false));
        Assert.False(ScenarioA1PassengerActionRules.LeaderReaches(null, false, "de-ht", true));
    }

    [Fact]
    public void AnOvrAddsItsPassengersShareTripledWithOneLeadershipModifier()
    {
        // D7.11: a CE halftrack's 4-6-7 adds half of 4, tripled: 6 FP; a truck's adds a quarter, tripled: 3 FP; a BU halftrack's adds none.
        OverrunPassengerFacts[] squad = [new("de-s", Definition("attacker-squad"), false)];
        Assert.Equal(6m, Assert.Single(ScenarioA1PassengerActionRules.OverrunFirepower(squad, false, true, [])).Firepower);
        Assert.Equal(3m, Assert.Single(ScenarioA1PassengerActionRules.OverrunFirepower(squad, true, false, [])).Firepower);
        Assert.Equal(0m, Assert.Single(ScenarioA1PassengerActionRules.OverrunFirepower(squad, false, false, [])).Firepower);

        // D7.16: one leadership modifier, the 9-1 Passenger leader's -1 over no Armor Leader, the Armor Leader's -2 over his; none for the leader alone.
        OverrunPassengerFacts[] led = [.. squad, new("de-l", Definition("attacker-leader-9-1"), false)];
        Assert.Equal(("leadership:de-l", -1m), (ScenarioA1PassengerActionRules.OverrunLeadership(led, true, null)!.Name, ScenarioA1PassengerActionRules.OverrunLeadership(led, true, null)!.Value));
        Assert.Equal(-2m, ScenarioA1PassengerActionRules.OverrunLeadership(led, true, -2)!.Value);
        Assert.Null(ScenarioA1PassengerActionRules.OverrunLeadership([new("de-l", Definition("attacker-leader-9-1"), false)], true, null));

        // Through the Fire package: the SPW 251/1's OVR with the squad aboard has 6 FP more than without, and an OVR recorded with no Passengers is as before.
        static FireAttack Ovr(IReadOnlyList<FireOverrunPassenger>? passengers) =>
            new FireAttack("MPh", "phasing", null, At, At, null, null, 0, true, new FireLos(false, 0, true, false), 7, "open-ground",
                [new FireTarget("ru-s", "defender-squad", At, false, false, false, false, false, false, false)], 3, new FireRolls([3, 4], null, null, null))
            {
                FireKind = ScenarioA1FireCalculator.OverrunFire,
                Overrun = new FireOverrun("de-ht", "attacker-halftrack", At, true, false, false, false, false) { Passengers = passengers },
            };
        FireResolution Resolved(FireAttack attack)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack, Ordnance.Fire);
            return result.Reasons is [{ } missing] && missing.StartsWith("asl.a1.fire.roll-missing:checks:", StringComparison.Ordinal)
                ? ScenarioA1FireCalculator.Resolve(attack with
                {
                    Rolls = attack.Rolls! with
                    {
                        Checks = new Dictionary<string, IReadOnlyList<int>> { ["ru-s"] = CheckDice }
                    }
                }, Ordnance.Fire)
                : result;
        }

        var without = Resolved(Ovr(null)).Arithmetic!.TotalFirepower;
        var carried = Resolved(Ovr([new FireOverrunPassenger("de-s", "attacker-squad"), new FireOverrunPassenger("de-l", "attacker-leader-9-1")])).Arithmetic!;
        Assert.Equal(without + 6m, carried.TotalFirepower);
        Assert.Contains(carried.Drm, item => item.Name == "leadership:de-l" && item.Rule == "D7.16");
    }

    [Fact]
    public void PassengersInCcAPassedSwAndAnUnloadAfterFire()
    {
        // A11.611, D6.62, A11.71: a BU halftrack's Passengers do not attack; a Rider attacks apart; a Mobile vehicle's PRC are not held in Melee.
        Assert.Equal((ScenarioA1PassengerActionRules.Idle, false), ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Passenger, true, false, true));
        Assert.Equal((ScenarioA1PassengerActionRules.Attacks, true), ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Passenger, true, true, false));
        Assert.Equal((ScenarioA1PassengerActionRules.Apart, false), ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, false, false, true));

        // D6.31: a Passenger passes a SW to Infantry in its Location or a Passenger of its vehicle; Infantry passes none to a Passenger.
        Assert.Null(ScenarioA1PassengerActionRules.TransferBar("de-s", "de-ht", "de-h", null));
        Assert.Null(ScenarioA1PassengerActionRules.TransferBar("de-s", "de-ht", "de-h", "de-ht"));
        Assert.Contains("D6.31", ScenarioA1PassengerActionRules.TransferBar("de-s", "de-ht", "de-h", "de-truck"));
        Assert.Contains("only as it loads", ScenarioA1PassengerActionRules.TransferBar("de-h", null, "de-s", "de-ht"));

        // D6.5, D.3: a Passenger that fired, or added its FP to the OVR, does not unload in that phase.
        Assert.Null(ScenarioA1PassengerActionRules.UnloadAfterFireBar("de-s", false, false));
        Assert.Contains("D6.5", ScenarioA1PassengerActionRules.UnloadAfterFireBar("de-s", true, false));
        Assert.Contains("OVR", ScenarioA1PassengerActionRules.UnloadAfterFireBar("de-s", false, true));
    }
}
