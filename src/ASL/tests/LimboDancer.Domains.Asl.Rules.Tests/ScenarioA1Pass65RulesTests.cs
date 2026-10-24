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

    // Increment 2d: loading and unloading completed (D6.4, D6.5, D5.43, A4.6, A4.11, A4.12, A4.42, A4.5, A7.821, D2.401; rulings R65.14, R65.15).
    private static readonly string[] SquadOnly = ["de-s"];

    /// <summary>
    /// A Russian squad's Defensive First Fire from the ADJACENT hex at a German truck in Open Ground and the 4-6-7 that unloads from it or boards it, with the
    /// stack's movement as given; an Original DR of 12, no effect.
    /// </summary>
    private static FireAttack AtEmbarking(FireEmbarking? embarking, bool assault = false) =>
        AtVehicle(Halftrack(false, [], "attacker-truck"), range: 1, from: "bd01:G6:0") with
        {
            Phase = "MPh",
            FiringSide = "non-phasing",
            FireKind = ScenarioA1FireCalculator.FirstFire,
            TargetMovement = new FireMovement(assault),
            Targets = [new FireTarget("de-s", "attacker-squad", At, false, false, false, false, false, false, false) { KnownEnemyInLos = true, Captors = [] }],
            Embarking = embarking,
        };

    private static string[] MovementDrm(FireAttack attack) =>
        [.. Fired(attack, [6, 6]).Arithmetic!.Drm.Where(item => item.Name is "ffnam" or "ffmo").Select(item => $"{item.Name} {item.Rule}")];

    [Fact]
    public void UnloadersTakeFfnamAndFfmoOnlyFromAnUnarmoredConveyance()
    {
        // D6.5: FFMO only for Passengers unloading from an unarmored conveyance in Open Ground; FFNAM always, Assault Movement or not.
        Assert.True(ScenarioA1PassengerCalculator.EmbarkingFfmo(true, true));
        Assert.False(ScenarioA1PassengerCalculator.EmbarkingFfmo(true, false));
        Assert.False(ScenarioA1PassengerCalculator.EmbarkingFfmo(false, true));
        Assert.Equal(["ffnam D6.5", "ffmo D6.5"], MovementDrm(AtEmbarking(new FireEmbarking(SquadOnly, true, true))));
        Assert.Equal(["ffnam D6.5"], MovementDrm(AtEmbarking(new FireEmbarking(SquadOnly, false, true))));
        Assert.Equal(["ffnam D6.5"], MovementDrm(AtEmbarking(new FireEmbarking(SquadOnly, true, false), assault: true)));

        // An attack recorded before the pass names no (un)loaders: Assault Movement avoids FFNAM, and Open Ground gives FFMO, as before.
        Assert.Equal(["ffnam A4.6", "ffmo A4.6"], MovementDrm(AtEmbarking(null)));
        Assert.Equal(["ffmo A4.6"], MovementDrm(AtEmbarking(null, assault: true)));
    }

    private static FireUnitEffect Effect(string id, bool eliminated = false, bool broken = false, bool pinned = false) =>
        new(id, "attacker-squad", "attacker-squad", null, eliminated, broken, pinned, false, false, false, [], []);

    [Fact]
    public void ALoadFailsOnAPinOrABreakAndAnUnloadDoesNot()
    {
        // D5.43: a loader left pinned or broken does not board; an eliminated one is gone; an unpinned Good Order one boards.
        FireUnitEffect[] effects = [Effect("de-p", pinned: true), Effect("de-b", broken: true), Effect("de-k", eliminated: true), Effect("de-s"), Effect("de-x", pinned: true)];
        Assert.Equal(["de-p", "de-b"], ScenarioA1PassengerCalculator.LoadFailures(new FireEmbarking(["de-p", "de-b", "de-k", "de-s"], false, true), effects));

        // "A Passenger/Rider attempting to unload ... does succeed"; an attack with no (un)loaders fails no load.
        Assert.Empty(ScenarioA1PassengerCalculator.LoadFailures(new FireEmbarking(["de-p", "de-b"], true, true), effects));
        Assert.Empty(ScenarioA1PassengerCalculator.LoadFailures(null, effects));
    }

    [Fact]
    public void AfterUnloadingFourMfNoDoubleTimeAndNoLeaderBonus()
    {
        // A4.11: four MF for a unit that rode or dismounted, a SMC's six cut to four; the usual allotment otherwise.
        Assert.Equal(4, ScenarioA1PassengerCalculator.ConveyedAllowance(true));
        Assert.Null(ScenarioA1PassengerCalculator.ConveyedAllowance(false));
        var leader = new MfAllotmentFacts(false, null, true, false, false);
        Assert.Equal(4, ScenarioA1MovementCalculator.MfAllotment(leader, 0, false, 0, 0, () => ScenarioA1PassengerCalculator.ConveyedAllowance(true) ?? 6, () => []));
        Assert.Equal(6, ScenarioA1MovementCalculator.MfAllotment(leader, 0, false, 0, 0, () => ScenarioA1PassengerCalculator.ConveyedAllowance(false) ?? 6, () => []));

        // A4.5: no Double Time after riding or dismounting; A4.12: no leader bonus.
        Assert.Contains("A4.5", ScenarioA1PassengerCalculator.ConveyedDoubleTimeBar(true, [("de-s", false), ("de-l", true)]));
        Assert.Null(ScenarioA1PassengerCalculator.ConveyedDoubleTimeBar(false, [("de-l", true)]));
        Assert.Null(ScenarioA1PassengerCalculator.ConveyedDoubleTimeBar(true, [("de-s", false)]));
        Assert.False(ScenarioA1PassengerCalculator.ConveyedLeaderBonus(true, true));
        Assert.True(ScenarioA1PassengerCalculator.ConveyedLeaderBonus(true, false));

        // D6.5, A7.821: the units disembarked that move on before any other: active, not done moving, Good Order and unpinned.
        Assert.Equal(["de-s"], ScenarioA1PassengerCalculator.InterimMembers([("de-s", true, false, false, false), ("de-p", true, false, true, false),
            ("de-b", true, false, false, true), ("de-e", true, true, false, false), ("de-k", false, false, false, false)]));
    }

    [Fact]
    public void AStackBoardsAtTheEndOfItsMoveWithItsLeadersIpc()
    {
        // D6.4, A4.2: the whole stack boards once the DEFENDER passes; no unit outside it boards while it moves; no stack moving, nothing to bar.
        Assert.Null(ScenarioA1PassengerCalculator.EndOfMoveLoadBar("de-t", false, false, [], SquadOnly));
        Assert.Null(ScenarioA1PassengerCalculator.EndOfMoveLoadBar("de-t", true, false, ["de-s", "de-l"], ["de-s", "de-l"]));
        Assert.Contains("A8.1", ScenarioA1PassengerCalculator.EndOfMoveLoadBar("de-t", true, true, SquadOnly, SquadOnly));
        Assert.Contains("de-l moves with the stack", ScenarioA1PassengerCalculator.EndOfMoveLoadBar("de-t", true, false, ["de-s", "de-l"], SquadOnly));
        Assert.Contains("de-h is not of the moving stack", ScenarioA1PassengerCalculator.EndOfMoveLoadBar("de-t", true, false, SquadOnly, ["de-s", "de-h"]));

        // A4.42: the laden MMC gains the leader's one PP of IPC, and the leader spends his own.
        Assert.Equal((1, -1, 0), (ScenarioA1PassengerCalculator.BoardingIpcBonus("de-s", "de-s", "de-l"), ScenarioA1PassengerCalculator.BoardingIpcBonus("de-l", "de-s", "de-l"),
            ScenarioA1PassengerCalculator.BoardingIpcBonus("de-h", "de-s", "de-l")));

        // D6.4's EX: a squad with five PP and a leader with none: IPC 4, so three MF; two spent entering Open Ground and boarding, the vehicle keeps a quarter.
        var squad = new MfAllotmentFacts(false, null, false, false, false);
        var allotment = ScenarioA1MovementCalculator.MfAllotment(squad, 0, false, 0, 1, () => ScenarioA1PassengerCalculator.ConveyanceAllowance, () => [5])!.Value;
        Assert.Equal(3, allotment);
        Assert.Equal(2, ScenarioA1PassengerCalculator.BoardingHalfMfLeft(allotment, 1, false));
        Assert.Equal(4, ScenarioA1PassengerCalculator.LoadKeptHalfMp(16, 1));
    }

    [Fact]
    public void ASwStaysAboardOnlyAHalftrackOrCarrierAndTimeAboardCounts()
    {
        // D6.4's EXC: a halftrack or Carrier keeps a SW its unloading Passenger held; a truck does not; a SW no unloader holds stays with its holder.
        Assert.Null(ScenarioA1PassengerCalculator.LeaveAboardBar("de-ht", "halftrack", [("de-lmg", true)]));
        Assert.Null(ScenarioA1PassengerCalculator.LeaveAboardBar("uk-c", "carrier", [("uk-bren", true)]));
        Assert.Null(ScenarioA1PassengerCalculator.LeaveAboardBar("de-t", "truck", []));
        Assert.Contains("neither", ScenarioA1PassengerCalculator.LeaveAboardBar("de-t", "truck", [("de-lmg", true)]));
        Assert.Contains("not held by a Passenger that unloads", ScenarioA1PassengerCalculator.LeaveAboardBar("de-ht", "halftrack", [("de-lmg", false)]));

        // D2.401: the vehicle's MP while the unit rode count, and its own one MF to board or get off.
        Assert.Equal((true, 4), ScenarioA1MotionAttempts.AboardStep(true, 4, false));
        Assert.Equal((false, 2), ScenarioA1MotionAttempts.AboardStep(false, 4, true));
        Assert.Equal(6, ScenarioA1MotionAttempts.HalfPointsInLos([ScenarioA1MotionAttempts.AboardStep(true, 4, false), ScenarioA1MotionAttempts.AboardStep(true, 4, true),
            ScenarioA1MotionAttempts.AboardStep(false, 2, false)]));
    }

    // Increment 2e: Riders and Bailing Out (D6.2, D6.21 to D6.24, B13.43; rulings R65.16, R65.17).

    [Fact]
    public void RiderCapacityFollowsTheVehicleClass()
    {
        // D6.2, D6.81: a tank 14 PP, a Carrier 8, a halftrack one SMC and his two PP, a truck none.
        Assert.Equal(new RiderCapacity(14, false), ScenarioA1RiderRules.Capacity("tank", false));
        Assert.Equal(new RiderCapacity(14, false), ScenarioA1RiderRules.Capacity("assault-gun", false));
        Assert.Equal(new RiderCapacity(8, false), ScenarioA1RiderRules.Capacity("carrier", false));
        Assert.Equal(new RiderCapacity(2, true), ScenarioA1RiderRules.Capacity("halftrack", false));
        Assert.Equal(new RiderCapacity(2, true), ScenarioA1RiderRules.Capacity("tankette", false));
        Assert.Null(ScenarioA1RiderRules.Capacity("truck", true));

        // A squad and a 9-1 ride a tank (10 PP); two squads do not fit beside a third; a halftrack takes the leader alone, not a squad.
        (string, bool, Func<int?>) Squad(string id) => (id, false, () => 10);
        (string, bool, Func<int?>) Leader(string id, int pp = 0) => (id, true, () => pp);
        Assert.Null(ScenarioA1RiderRules.CapacityBar("t", new RiderCapacity(14, false), [Squad("s"), Leader("l")]));
        Assert.Contains("would take 20 PP", ScenarioA1RiderRules.CapacityBar("t", new RiderCapacity(14, false), [Squad("s"), Squad("s2")]));
        Assert.Null(ScenarioA1RiderRules.CapacityBar("h", new RiderCapacity(2, true), [Leader("l", 2)]));
        Assert.Contains("one SMC", ScenarioA1RiderRules.CapacityBar("h", new RiderCapacity(2, true), [Squad("s")]));
        Assert.Contains("carries no Riders", ScenarioA1RiderRules.CapacityBar("k", null, [Leader("l")]));
    }

    [Fact]
    public void RidersAreRussianFrom1942AndEveryonesFrom1943()
    {
        // D6.2: a Russian in 1942 yes; a German in 1942 no; anyone in 1943; a game with no year refuses and says why.
        Assert.Null(ScenarioA1RiderRules.DateBar("t", 1942, "russian"));
        Assert.Contains("only from 1942 for the Russians", ScenarioA1RiderRules.DateBar("t", 1942, "german"));
        Assert.Null(ScenarioA1RiderRules.DateBar("t", 1943, "german"));
        Assert.Contains("1943", ScenarioA1RiderRules.DateBar("t", 1941, "russian"));
        Assert.Contains("names no scenario year", ScenarioA1RiderRules.DateBar("t", null, "russian"));
    }

    [Fact]
    public void RidersBailOutInWoodsButNotAcrossARoadInBypassOrInAStream()
    {
        // D6.21, B13.43: woods, orchard, a building, rubble, Water, bocage; not across a road hexside, in Bypass, or in a stream; a friendly VSE lets a building be.
        static RiderTerrainFacts Into(string? terrain, string? name = null, string? hexside = null) => new("open-ground", terrain, name, hexside, false, false, false, false);
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into("woods")));
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into("orchard")));
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into("stone-building")));
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into("wooden-rubble")));
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into(null, "Water")));
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(Into("open-ground", hexside: "Bocage")));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("open-ground")));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("grain")));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("woods") with
        {
            Road = true
        }));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("woods") with
        {
            Bypass = true
        }));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("woods") with
        {
            Stream = true
        }));
        Assert.False(ScenarioA1RiderRules.TerrainBailOut(Into("wooden-building") with
        {
            FriendlyVse = true
        }));

        // Leaving woods with Riders aboard makes them Bail Out too.
        Assert.True(ScenarioA1RiderRules.TerrainBailOut(new RiderTerrainFacts("woods", "open-ground", null, null, false, false, false, false)));
    }

    [Fact]
    public void ATurretedAfvsTcaChangeMakesItsRidersBailOut()
    {
        // D6.21: a TCA change of a turreted AFV; no change, or no turret, keeps them aboard.
        Assert.True(ScenarioA1RiderRules.TurretBailOut(true, 0, 1));
        Assert.False(ScenarioA1RiderRules.TurretBailOut(true, 1, 1));
        Assert.False(ScenarioA1RiderRules.TurretBailOut(true, 1, null));
        Assert.False(ScenarioA1RiderRules.TurretBailOut(false, 0, 1));

        // D6.23: a pin or a break makes a Rider Bail Out; an eliminated one, or a Passenger, does not.
        Assert.True(ScenarioA1RiderRules.CombatBailOut(true, false, true, false));
        Assert.True(ScenarioA1RiderRules.CombatBailOut(true, false, false, true));
        Assert.False(ScenarioA1RiderRules.CombatBailOut(true, true, false, true));
        Assert.False(ScenarioA1RiderRules.CombatBailOut(false, false, true, false));
    }

    [Fact]
    public void ABailOutsNmcAndItsSwDrs()
    {
        // D6.24, A10.3, A7.8, A10.31: against Morale 7, a 6 passes, a 7 pins, an 8 breaks, a 12 Reduces as it breaks; a broken unit is Reduced on a fail and
        // eliminated on a 12.
        Assert.Equal(ScenarioA1RiderRules.Passed, ScenarioA1RiderRules.Check(3, 3, 7, false));
        Assert.Equal(ScenarioA1RiderRules.Pinned, ScenarioA1RiderRules.Check(3, 4, 7, false));
        Assert.Equal(ScenarioA1RiderRules.Broken, ScenarioA1RiderRules.Check(4, 4, 7, false));
        Assert.Equal(ScenarioA1RiderRules.Reduced, ScenarioA1RiderRules.Check(6, 6, 7, false));
        Assert.Equal(ScenarioA1RiderRules.Reduced, ScenarioA1RiderRules.Check(4, 4, 7, true));
        Assert.Equal(ScenarioA1RiderRules.Eliminated, ScenarioA1RiderRules.Check(6, 6, 7, true));

        // A SW's dr: 1 to 3 kept, 4 to 6 malfunctioned, an X# SW eliminated; every SW eliminated when its carrier breaks.
        Assert.Equal(ScenarioA1RiderRules.Kept, ScenarioA1RiderRules.WeaponFate(3, false, false));
        Assert.Equal(ScenarioA1RiderRules.Malfunctioned, ScenarioA1RiderRules.WeaponFate(4, false, false));
        Assert.Equal(ScenarioA1RiderRules.Eliminated, ScenarioA1RiderRules.WeaponFate(4, true, false));
        Assert.Equal(ScenarioA1RiderRules.Eliminated, ScenarioA1RiderRules.WeaponFate(null, false, true));
        Assert.True(ScenarioA1RiderRules.LeavesBroken(ScenarioA1RiderRules.Pinned, true));
        Assert.False(ScenarioA1RiderRules.LeavesBroken(ScenarioA1RiderRules.Pinned, false));
    }

    [Fact]
    public void ARiderFiresAtHalfAQuarterOnceItsAfvMovedAndNoSw()
    {
        // D6.22: half its inherent FP, a quarter once the AFV moved or is in Motion, whatever the phase; it uses no SW; a BU tank does not stop it.
        var riding = ScenarioA1PassengerActionRules.MountedFire(Mounted("defender-tank", crewExposed: false) with
        {
            Rider = true
        });
        Assert.Null(riding.Refusal);
        Assert.Equal((0.5m, "D6.22"), (Times(riding), riding.Multipliers[0].Rule));
        Assert.Equal(0.25m, Times(ScenarioA1PassengerActionRules.MountedFire(Mounted("defender-tank", moved: true, phase: "AFPh") with
        {
            Rider = true
        })));
        Assert.Equal(0.25m, Times(ScenarioA1PassengerActionRules.MountedFire(Mounted("defender-tank", motion: true) with
        {
            Rider = true
        })));
        Assert.Contains("Riders use no SW", ScenarioA1PassengerActionRules.MountedFire(Mounted("defender-tank", weapon: "defender-lmg") with
        {
            Rider = true
        }).Refusal);

        // A11.611: a Rider's part in CC is apart, +1 to its DR.
        Assert.Equal(ScenarioA1PassengerActionRules.Apart, ScenarioA1PassengerActionRules.CloseCombat(FirePrc.Rider, true, true, false).Part);
    }

    // Increment 2f: Carriers (D6.8 to D6.84, A26.213; ruling R65.18).

    [Fact]
    public void ACarrierIsNeverBuAndItsCrewAndPassengersStayCeWhenStunned()
    {
        // D6.84: a Carrier's BU counter is refused, at setup too; asking for CE is not; a halftrack's is not decided here.
        Assert.Contains("never BU", ScenarioA1CarrierRules.ButtonUpBar("uk-bren", "carrier", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CarrierRules.ButtonUpBar("uk-bren", "carrier", false));
        Assert.Null(ScenarioA1CarrierRules.ButtonUpBar("de-ht", "halftrack", true));
        Assert.StartsWith("play.setup-carrier", ScenarioA1CarrierRules.SetupBar("uk-bren", "carrier", true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CarrierRules.SetupBar("de-ht", "halftrack", true));

        // Its crew is CE where its markers would say BU (stunned or shocked); a halftrack's follows its markers.
        Assert.True(ScenarioA1CarrierRules.CrewExposed("carrier", false));
        Assert.False(ScenarioA1CarrierRules.CrewExposed("halftrack", false));
        Assert.True(ScenarioA1CarrierRules.CrewExposed("halftrack", true));

        // Its Passengers are CE with a stunned crew; a halftrack's are spared (D.8).
        var passenger = new FirePrc("uk-l", "british-leader-8-1", FirePrc.Passenger, false, false);
        Assert.Equal(ScenarioA1PrcRules.Exposed, ScenarioA1PrcRules.Exposure(passenger, Ordnance.Fire.Definitions["campaign-british-bren-carrier"], false, true));
        Assert.Equal(ScenarioA1PrcRules.Spared, ScenarioA1PrcRules.Exposure(passenger, Ordnance.Fire.Definitions["attacker-halftrack"], false, true));
    }

    [Fact]
    public void ACarrierCarriesFourPassengerPpAndEightRiderPp()
    {
        // D6.81: four Passenger PP, none with a mortar, FT, or 6 FP AAMG as MA; eight Rider PP. Any other vehicle keeps its printed capacity.
        var bren = Ordnance.Fire.Definitions["campaign-british-bren-carrier"];
        Assert.Equal(4, ScenarioA1CarrierRules.PassengerCapacity(bren.VehicleClass, bren.PassengerCapacity, bren.MainArmament, bren.Caliber, bren.AntiAircraftMg));
        Assert.Null(ScenarioA1CarrierRules.PassengerCapacity("carrier", 4, "ft", null, null));
        Assert.Null(ScenarioA1CarrierRules.PassengerCapacity("carrier", 4, null, 76, null));
        Assert.Null(ScenarioA1CarrierRules.PassengerCapacity("carrier", 4, "aamg", null, 6));
        Assert.Equal(4, ScenarioA1CarrierRules.PassengerCapacity("carrier", 4, "aamg", null, 4));
        Assert.Equal(15, ScenarioA1CarrierRules.PassengerCapacity("halftrack", 15, "aamg", null, 3));
        Assert.Equal(new RiderCapacity(8, false), ScenarioA1RiderRules.Capacity(bren.VehicleClass, bren.Unarmored == true));
    }

    [Fact]
    public void ACarriersCrewLeavesAsAHalfSquadUnlessItHasAFtOr76mmMortarOrTowsAGun()
    {
        // D6.82: the Bren carrier's crew is the British 2-4-7 HS, not the elite 2-4-8; a FT or 76mm mortar Carrier, or one towing a Gun, has a crew.
        var bren = Ordnance.Fire.Definitions["campaign-british-bren-carrier"];
        Assert.True(ScenarioA1CarrierRules.InherentHalfSquad(bren.VehicleClass, bren.MainArmament, bren.Caliber, false));
        Assert.False(ScenarioA1CarrierRules.InherentHalfSquad("carrier", "ft", null, false));
        Assert.False(ScenarioA1CarrierRules.InherentHalfSquad("carrier", null, 76, false));
        Assert.True(ScenarioA1CarrierRules.InherentHalfSquad("carrier", null, 51, false));
        Assert.False(ScenarioA1CarrierRules.InherentHalfSquad("carrier", "mg", null, true));
        Assert.False(ScenarioA1CarrierRules.InherentHalfSquad("halftrack", "aamg", null, false));

        var definitions = Ordnance.Fire.Definitions.Values;
        Assert.Equal(("british-half-squad", "asl:half-squad"), ScenarioA1CarrierRules.LeavingCrew(definitions, "british", true));
        Assert.Equal(("attacker-half-squad", "asl:half-squad"), ScenarioA1CarrierRules.LeavingCrew(definitions, "german", true));
        Assert.Equal(("british-vehicle-crew", "asl:vehicle-crew"), ScenarioA1CarrierRules.LeavingCrew(definitions, "british", false));

        // A nationality with no 2-4-7 HS (the Italians' is 1-3-7) leaves its vehicle crew; one with neither, nothing.
        Assert.Equal(("italian-vehicle-crew", "asl:vehicle-crew"), ScenarioA1CarrierRules.LeavingCrew(definitions, "italian", true));
        Assert.Null(ScenarioA1CarrierRules.LeavingCrew(definitions, "japanese", true));

        // D6.83: only a Carrier's inherent HS unloads as Passengers do, and once.
        Assert.Null(ScenarioA1CarrierRules.CrewUnloadBar("uk-bren", true, false));
        Assert.Contains("already left", ScenarioA1CarrierRules.CrewUnloadBar("uk-bren", true, true), StringComparison.Ordinal);
        Assert.Contains("only a Carrier's inherent HS", ScenarioA1CarrierRules.CrewUnloadBar("de-ht", false, false), StringComparison.Ordinal);
    }

    [Fact]
    public void ACarriersInherentHalfSquadIsWorthTwoVpAndOneOnceItLeaves()
    {
        // A26.213: the Bren carrier, its MG MA, AF 1, and its inherent HS: 1 + 1 + 1 + 2. Once the HS is a counter of its own it is a HS, one VP (A26.211),
        // and it no longer counts in the Carrier.
        Assert.Equal(5, ScenarioA1VictoryCalculator.VehicleVictoryPoints(true, false, false, false, true, [1, 1], () => true));
        Assert.Equal(3, ScenarioA1VictoryCalculator.VehicleVictoryPoints(true, false, false, false, true, [1, 1], () => false));
        Assert.Equal(1, ScenarioA1VictoryCalculator.VictoryPoints("asl:half-squad", () => null, () => 0));
        Assert.True(ScenarioA1VictoryCalculator.LeftItsVehicle("asl:half-squad"));
        Assert.True(ScenarioA1VictoryCalculator.LeftItsVehicle("asl:vehicle-crew"));
        Assert.False(ScenarioA1VictoryCalculator.LeftItsVehicle("asl:squad"));
    }

    // Increment 2g: the crew's own acts (D5.41, D5.411, D5.42, D5.341, D5.8, D6.631, D3.43, A21.22; ruling R65.20).

    [Fact]
    public void ACrewAbandonsOnlyAnUnmovedUnfiredArmedVehicleInItsOwnMph()
    {
        // D5.41: in its own MPh, not moved or fired that Player Turn; D5.1: an unarmed vehicle's Inherent Driver never leaves.
        Assert.Null(ScenarioA1CrewActsRules.AbandonBar("de-tank", true, true, true, false, false, false, false));
        Assert.Contains("only in its own MPh", ScenarioA1CrewActsRules.AbandonBar("de-tank", false, true, true, false, false, false, false), StringComparison.Ordinal);
        Assert.Contains("Inherent Driver", ScenarioA1CrewActsRules.AbandonBar("de-truck", true, true, false, false, false, false, false), StringComparison.Ordinal);
        Assert.Contains("already Abandoned", ScenarioA1CrewActsRules.AbandonBar("de-tank", true, true, true, true, false, false, false), StringComparison.Ordinal);
        Assert.Contains("a unit is moving", ScenarioA1CrewActsRules.AbandonBar("de-tank", true, true, true, false, true, false, false), StringComparison.Ordinal);
        Assert.Contains("has moved this Player Turn", ScenarioA1CrewActsRules.AbandonBar("de-tank", true, true, true, false, false, true, false), StringComparison.Ordinal);
        Assert.Contains("has fired this Player Turn", ScenarioA1CrewActsRules.AbandonBar("de-tank", true, true, true, false, false, false, true), StringComparison.Ordinal);
    }

    [Fact]
    public void OnlyAnInherentCrewDestroysItsVehicleAndAWreckWaitsForThePassengers()
    {
        // D5.411: a wreck or every MG and the MA malfunctioned, by an Inherent crew alone; PRC unload before a wreck.
        Assert.Null(ScenarioA1CrewActsRules.SelfDestructionBar("de-tank", null, false, true));
        Assert.Null(ScenarioA1CrewActsRules.SelfDestructionBar("de-tank", ScenarioA1CrewActsRules.Wreck, true, false));
        Assert.Null(ScenarioA1CrewActsRules.SelfDestructionBar("de-ht", ScenarioA1CrewActsRules.Malfunction, true, true));
        Assert.Contains("Temporary crew", ScenarioA1CrewActsRules.SelfDestructionBar("de-tank", ScenarioA1CrewActsRules.Malfunction, false, false), StringComparison.Ordinal);
        Assert.Contains("unload before", ScenarioA1CrewActsRules.SelfDestructionBar("de-ht", ScenarioA1CrewActsRules.Wreck, true, true), StringComparison.Ordinal);
        Assert.Contains("is not a self-destruction", ScenarioA1CrewActsRules.SelfDestructionBar("de-ht", "burn", true, false), StringComparison.Ordinal);
        Assert.Equal([(UnitCondition.Malfunctioned, true), (UnitCondition.BmgMalfunctioned, true)], ScenarioA1CrewActsRules.SelfDestructionConditions(true, true, false));
    }

    [Fact]
    public void AReCrewingIsBarredTheTurnTheCrewLeftAndForARecalledCrew()
    {
        // D5.42: in its own MPh, in the Location since the MPh began, unpinned and in Good Order (D5.43), not in the Player Turn the old crew left; D5.341:
        // a Recalled crew never re-crews its own AFV.
        static RecrewFacts Facts() => new("de-crew", "de-tank", true, true, true, true, true, false, true, false, true, false);
        Assert.Null(ScenarioA1CrewActsRules.RecrewBar(Facts()));
        Assert.Contains("only in its own MPh", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            OwnMph = false
        }), StringComparison.Ordinal);
        Assert.Contains("not an Abandoned vehicle", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            VehicleAbandoned = false
        }), StringComparison.Ordinal);
        Assert.Contains("the enemy's", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            SameSide = false
        }), StringComparison.Ordinal);
        Assert.Contains("before it spends any MF", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            MovedThisPhase = true
        }), StringComparison.Ordinal);
        Assert.Contains("Good Order", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            GoodOrder = false
        }), StringComparison.Ordinal);
        Assert.Contains("left it this Player Turn", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            LeftThisPlayerTurn = true
        }), StringComparison.Ordinal);
        Assert.Contains("Recalled crew", ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            Recalled = true
        }), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CrewActsRules.RecrewBar(Facts() with
        {
            Recalled = true,
            OwnCrew = false
        }));

        // A21.22: a squad is a Temporary crew; a vehicle or infantry crew, or the vehicle's own crew (a Carrier's HS), is not.
        Assert.True(ScenarioA1CrewActsRules.TemporaryCrew("asl:squad", false));
        Assert.False(ScenarioA1CrewActsRules.TemporaryCrew("asl:vehicle-crew", false));
        Assert.False(ScenarioA1CrewActsRules.TemporaryCrew("asl:crew", false));
        Assert.False(ScenarioA1CrewActsRules.TemporaryCrew("asl:half-squad", true));

        // D3.43: a crew counter may carry its Armor Leader, dormant; any other unit not a vehicle may not.
        Assert.Null(ScenarioA1ArmorLeaders.AssignmentRefusal("de-crew", false, 9, -1, false, crew: true));
        Assert.NotNull(ScenarioA1ArmorLeaders.AssignmentRefusal("de-squad", false, 9, -1, false));
    }

    [Fact]
    public void ALeavingCrewIsMarkedPrepOrFinalFireAfterItsVehicleFiredOrMoved()
    {
        // D5.8: Prep Fire in its own Player Turn, Final Fire in the enemy's, when the vehicle fired or was a moving target (moved, or in Motion).
        Assert.Null(ScenarioA1CrewActsRules.CrewFireCounter(false, false, false, true));
        Assert.Equal(UnitCondition.PrepFire, ScenarioA1CrewActsRules.CrewFireCounter(true, false, false, true));
        Assert.Equal(UnitCondition.PrepFire, ScenarioA1CrewActsRules.CrewFireCounter(false, true, false, true));
        Assert.Equal(UnitCondition.FinalFire, ScenarioA1CrewActsRules.CrewFireCounter(false, false, true, false));
        Assert.Equal(UnitCondition.FinalFire, ScenarioA1CrewActsRules.CrewFireCounter(true, false, false, false));
    }

    [Fact]
    public void AnArmoredHalftracksMgOfThreeFpOrLessIsRemovedAsTheNationalitysLmg()
    {
        // D6.631: the SPW 251/1's 3 FP AAMG MA is Removed as the German LMG (3 FP); a heavier MG, a tank, another nationality's crew, a SMC, a crew
        // forced out by a failed TC (D5.5), or a Disabled mount Removes none.
        var spw = Ordnance.Fire.Definitions["attacker-halftrack"];
        var removal = ScenarioA1CrewActsRules.MgRemoval("de-ht", spw.VehicleClass, spw.Unarmored == true, spw.Nationality, "german", false, false, spw.MainArmament,
            spw.AntiAircraftMg, spw.BowMg, false, false);
        Assert.Equal<(string?, int, string?)>((ScenarioA1CrewActsRules.MainArmament, 3, null), removal);
        Assert.Equal("attacker-lmg", ScenarioA1CrewActsRules.RemovedMgDefinition(Ordnance.Fire.Definitions.Values, "german", removal.Fp));
        Assert.Contains("3 FP or less", ScenarioA1CrewActsRules.MgRemoval("de-ht", "halftrack", false, "german", "german", false, false, "aamg", 4, null, false, false).Bar,
            StringComparison.Ordinal);
        Assert.Contains("armored halftrack", ScenarioA1CrewActsRules.MgRemoval("de-tank", "tank", false, "german", "german", false, false, null, null, 2, false, false).Bar,
            StringComparison.Ordinal);
        Assert.Contains("own nationality", ScenarioA1CrewActsRules.MgRemoval("de-ht", "halftrack", false, "german", "russian", false, false, "aamg", 3, null, false, false).Bar,
            StringComparison.Ordinal);
        Assert.Contains("lone SMC", ScenarioA1CrewActsRules.MgRemoval("de-ht", "halftrack", false, "german", "german", true, false, "aamg", 3, null, false, false).Bar,
            StringComparison.Ordinal);
        Assert.Contains("failed Immobilization TC", ScenarioA1CrewActsRules.MgRemoval("de-ht", "halftrack", false, "german", "german", false, true, "aamg", 3, null, false, false).Bar,
            StringComparison.Ordinal);
        Assert.Contains("no MG left", ScenarioA1CrewActsRules.MgRemoval("de-ht", "halftrack", false, "german", "german", false, false, "aamg", 3, null, true, false).Bar,
            StringComparison.Ordinal);
        Assert.Equal(UnitCondition.Disabled, ScenarioA1CrewActsRules.RemovedCondition(ScenarioA1CrewActsRules.MainArmament));
        Assert.Equal(UnitCondition.BmgDisabled, ScenarioA1CrewActsRules.RemovedCondition(ScenarioA1CrewActsRules.BowMg));
    }

    // Increment 2h: the Immobilization TC's causes, Crew Survival, the red CS#, Brew Up (D5.5, D5.6, D5.7, D8.11, D8.22, D9.2; ruling R65.21).

    /// <summary>The T-34's AP at the BU PzKpfw IIIH's front, its hull and turret.</summary>
    private static OrdnanceShot AtTank(int[] toKill, IReadOnlyList<FirePrc>? prc)
    {
        var shot = TankAt("attacker-tank", toKill, prc, crewExposed: false, ammunition: "ap");
        return shot with
        {
            VehicleTarget = shot.VehicleTarget! with
            {
                TurretFacing = "front"
            },
        };
    }

    /// <summary>The T-34's AP at the PzKpfw IIIH's front, the target already immobilized and its crew rolling the given TC.</summary>
    private static OrdnanceShot AtImmobile(int[] toKill, IReadOnlyList<FirePrc>? prc, int[] crewCheck, bool immobile = true)
    {
        var shot = AtTank(toKill, prc);
        return shot with
        {
            VehicleTarget = shot.VehicleTarget! with
            {
                Immobile = immobile ? true : null
            },
            Rolls = shot.Rolls! with
            {
                CrewCheck = crewCheck
            },
        };
    }

    [Fact]
    public void AnImmobileTanksCrewTakesTheTcWhenAnOriginalFiveWouldHaveKilledIt()
    {
        // D5.5, D8.11 (pp. 204, 208): the AP's TK DR two above the Final TK# does nothing (one above is a possible Shock, C7.41), but an Original 5 is below
        // the Final TK#, an elimination or a burn: the crew of the immobilized PzKpfw IIIH takes the TC, and a DR of 11 against its Morale Level fails it.
        var probe = Shot(AtImmobile([6, 5], [], [5, 6], immobile: false));
        Assert.True(probe.Kill is not null, string.Join("; ", probe.Reasons));
        var finalTk = probe.Kill.FinalTk;
        Assert.InRange(finalTk, 6, 9);
        var dr = finalTk + 2;
        int[] toKill = [dr - 5, 5];
        var kill = Shot(AtImmobile(toKill, [], [5, 6])).Kill!;
        Assert.Equal(OrdnanceKill.None, kill.Result);
        Assert.Equal((true, true, "TC", 11, false), (kill.SecondCause, kill.Abandoned, kill.CrewCheck!.Kind, kill.CrewCheck.FinalDr, kill.CrewCheck.Passed));

        // Not immobile, or a shot recorded before the pass (no PRC fact): no TC.
        Assert.Null(Shot(AtImmobile(toKill, [], [5, 6], immobile: false)).Kill!.CrewCheck);
        Assert.Null(Shot(AtImmobile(toKill, null, [5, 6])).Kill!.CrewCheck);

        // Only a burn, an elimination, or an automatic Shock on an Original 5 counts; a possible Shock does not, nor a result that already did something.
        Assert.True(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.None, _ => OrdnanceKill.Burn, true, true));
        Assert.True(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.Immobilized, _ => OrdnanceKill.Shock, true, true));
        Assert.False(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.None, _ => OrdnanceKill.PossibleShock, true, true));
        Assert.False(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.Shock, _ => OrdnanceKill.Burn, true, true));
        Assert.False(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.None, _ => OrdnanceKill.Eliminated, false, true));
        Assert.False(ScenarioA1CrewSurvivalRules.SecondCause(OrdnanceKill.None, _ => OrdnanceKill.Eliminated, true, false));
        Assert.Equal(5, ScenarioA1CrewSurvivalRules.SecondCauseDr);
    }

    [Fact]
    public void CrewSurvivalTakesPlusOneForABrokenStunnedOrShockedCrewAndNoneForAnAbandonedVehicle()
    {
        // D5.6: +1 if broken, Stunned (a Recall too), Shocked (an Unconfirmed Kill too), or under a "+1" counter; at most the CS# survives.
        Assert.Equal(0, ScenarioA1CrewSurvivalRules.SurvivalDrm(false, false, false, false));
        Assert.Equal(1, ScenarioA1CrewSurvivalRules.SurvivalDrm(true, false, false, false));
        Assert.Equal(1, ScenarioA1CrewSurvivalRules.SurvivalDrm(false, true, false, false));
        Assert.Equal(1, ScenarioA1CrewSurvivalRules.SurvivalDrm(false, false, true, false));
        Assert.Equal(1, ScenarioA1CrewSurvivalRules.SurvivalDrm(false, false, false, true));
        Assert.Equal(1, ScenarioA1CrewSurvivalRules.SurvivalDrm(true, true, true, true));
        var survival = ScenarioA1CrewSurvivalRules.Survival(6, CheckDice, 1);
        Assert.Equal((5, 6, true), (survival.FinalDr, survival.CrewSurvival, survival.Survived));
        Assert.False(ScenarioA1CrewSurvivalRules.Survival(4, CheckDice, 1).Survived);

        // The PzKpfw IIIH's CS# 6; an Abandoned vehicle has no crew to survive (D5.41); a truck's cs# serves its PRC alone.
        var tank = Ordnance.Armor.Vehicles["attacker-tank"];
        Assert.Equal(6, ScenarioA1CrewSurvivalRules.CrewSurvival(tank, false));
        Assert.Null(ScenarioA1CrewSurvivalRules.CrewSurvival(tank, true));
        Assert.Null(ScenarioA1CrewSurvivalRules.CrewSurvival(Ordnance.Armor.Vehicles["attacker-truck"], false));

        // D9.2: a killed tank whose crew was Shocked rolls with the +1; an Abandoned one rolls none, but its PRC still roll theirs.
        var finalTk = Shot(AtTank([6, 5], [])).Kill!.FinalTk;
        var dr = finalTk - 1;
        Assert.True(dr * 2 > finalTk);
        int[] toKill = [dr / 2, dr - (dr / 2)];
        var shocked = AtTank(toKill, []);
        shocked = shocked with
        {
            VehicleTarget = shocked.VehicleTarget! with
            {
                CrewImpaired = true
            }
        };
        var killed = Shot(shocked).Kill!;
        Assert.Equal((OrdnanceKill.Eliminated, 1, 13), (killed.Result, killed.CrewSurvival!.Drm, killed.CrewSurvival.FinalDr));
        var abandoned = AtTank(toKill, [Aboard()]);
        abandoned = abandoned with
        {
            VehicleTarget = abandoned.VehicleTarget! with
            {
                Abandoned = true
            }
        };
        var left = Shot(abandoned, new Dictionary<string, int[]> { ["de-s"] = [2, 2] }).Kill!;
        Assert.Null(left.CrewSurvival);
        Assert.True(Assert.Single(left.PrcSurvival!).Survived);
    }

    [Fact]
    public void ARedCsNumberBurnsOneDrHigherAndChangesNothingElse()
    {
        // D5.7, C7.7 note A: -1 to the Final TK DR for burning only. Against a Final TK# of 12, a DR of 7 burns a red CS# AFV (6 is half of 12) but merely
        // eliminates any other; a DR of 8 eliminates both, and nothing turns an elimination into anything but a burn.
        static bool Burns(int dr) => dr * 2 <= 12;
        Assert.True(ScenarioA1CrewSurvivalRules.BurnsRed(true, 7, Burns));
        Assert.False(ScenarioA1CrewSurvivalRules.BurnsRed(false, 7, Burns));
        Assert.False(ScenarioA1CrewSurvivalRules.BurnsRed(true, 8, Burns));

        // A FT's or MOL's burn is a DR below its TK#: with a red CS# an equal DR burns too.
        Assert.True(ScenarioA1CrewSurvivalRules.BurnsRed(true, 8, lower => lower < 8));

        // A synthetic red CS# PzKpfw IIIH burns on its own DR of 7 against 12; no catalog vehicle prints one (catalog 1.18.0, trait asl:cs-red).
        var red = Ordnance.Armor.Vehicles["attacker-tank"] with
        {
            CsRed = true
        };
        Assert.True(ScenarioA1CrewSurvivalRules.BurnsRed(red.CsRed, 7, Burns));
        Assert.All(Ordnance.Armor.Vehicles.Values, item => Assert.False(item.CsRed));
        Assert.All(Ordnance.Fire.Definitions.Values.Where(item => item.IsVehicle), item => Assert.NotEqual(true, item.CsRed));
    }

    // Increment 2i: captured vehicles (A21.2, A21.21, A21.22, D5.343, A26.12; ruling R65.22).

    private static readonly ScenarioA1CloseCombatReference CloseCombat = new ScenarioA1CloseCombatPackage().Reference;
    private static readonly int[] CaptureDice = [3, 3];
    private static readonly string[] UnmannedDrm = ["vs-no-manned-mg", "vs-ce-closed-topped", "vs-immobile"];

    private static CaptureUnitFacts Here(string id, string side, bool personnel = true, bool goodOrder = true, bool smc = false) => new(id, side, personnel, goodOrder, smc);

    [Fact]
    public void AnAbandonedAfvIsCapturedAsTheCcphEndsWhenNoneOfItsSidesPersonnelIsThere()
    {
        // A21.2: Good Order Russian Infantry alone with the Abandoned German AFV capture it; a German crew beneath it, broken or not, prevents it, as does a
        // manned or unarmed vehicle, one marked captured before the pass (R11.16), or no Good Order captor.
        Assert.True(ScenarioA1CapturedVehicleRules.AutomaticCaptureDue("ccph"));
        Assert.False(ScenarioA1CapturedVehicleRules.AutomaticCaptureDue("aph"));
        Assert.Equal("russian", ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, true, false, [Here("ru-s", "russian")]));
        Assert.Null(ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, true, false, [Here("ru-s", "russian"), Here("de-c", "german", goodOrder: false)]));
        Assert.Null(ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, false, false, [Here("ru-s", "russian")]));
        Assert.Null(ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", false, true, false, [Here("ru-s", "russian")]));
        Assert.Null(ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, true, true, [Here("ru-s", "russian")]));
        Assert.Null(ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, true, false, [Here("ru-s", "russian", goodOrder: false)]));
        Assert.Equal("russian", ScenarioA1CapturedVehicleRules.AutomaticCaptor("german", true, true, false, [Here("ru-s", "russian"), Here("de-v", "german", personnel: false)]));

        // A21.21: a MMC captor's Temporary Driver costs nothing; SMC captors alone give up the first of them to drive it.
        Assert.Null(ScenarioA1CapturedVehicleRules.TemporaryDriver([Here("ru-s", "russian"), Here("ru-l", "russian", smc: true)]));
        Assert.Equal("ru-a", ScenarioA1CapturedVehicleRules.TemporaryDriver([Here("ru-b", "russian", smc: true), Here("ru-a", "russian", smc: true)]));

        // A11.52, A21.2: a capture attempt is made on an Abandoned AFV only.
        Assert.Null(ScenarioA1CapturedVehicleRules.CaptureAttemptBar("de-v", true, true));
        Assert.Contains("only an Abandoned AFV", ScenarioA1CapturedVehicleRules.CaptureAttemptBar("de-v", true, false), StringComparison.Ordinal);
        Assert.Contains("(A21.2)", ScenarioA1CapturedVehicleRules.CapturedText("de-v", "russian", At, ScenarioA1CapturedVehicleRules.Automatic, null), StringComparison.Ordinal);
    }

    [Fact]
    public void ACaptureAttemptCapturesTheAbandonedTankWhereAnAttackWouldEliminateIt()
    {
        // A21.2, A11.51, A11.61: the Russian squad's CCV 5 against the Abandoned PzKpfw IIIH at -1 (no manned MG), -1 (assumed CE), -1 (assumed Immobile): a
        // DR of 6 is a Final 3, below the CCV, so the capture attempt captures it where an attack to eliminate would eliminate it.
        var units = new[] { new CloseCombatUnit("ru-s", "defender-squad", "russian", false, false, false, false, false, false, false, false, false, false, false) { Inexperienced = false } };
        var tank = new VehicleCloseCombatVehicle("de-v", "attacker-tank", "german", false, false, false, false, false, true, false, false, false);
        VehicleCloseCombatFacts Facts(bool? capture, bool? unmanned) => new("CCPh", At, tank, units, ["ru-s"], [], false, false, new VehicleCloseCombatRolls(CaptureDice))
        {
            Capture = capture,
            Unmanned = unmanned,
        };

        var captured = ScenarioA1VehicleCloseCombat.Resolve(Facts(true, true), CloseCombat);
        Assert.Equal((VehicleCloseCombatResolution.Captured, 3), (captured.VehicleResult, captured.FinalDr));
        Assert.Equal(UnmannedDrm, captured.Drm.Select(item => item.Name));
        Assert.Equal(VehicleCloseCombatResolution.Eliminated, ScenarioA1VehicleCloseCombat.Resolve(Facts(null, true), CloseCombat).VehicleResult);

        // A record made before the pass has no unmanned fact: -1 alone, a Final 5 equal to the CCV, which immobilizes; a capture attempt then does nothing.
        Assert.Equal(VehicleCloseCombatResolution.Immobilized, ScenarioA1VehicleCloseCombat.Resolve(Facts(null, null), CloseCombat).VehicleResult);
        Assert.Equal(VehicleCloseCombatResolution.None, ScenarioA1VehicleCloseCombat.Resolve(Facts(true, null), CloseCombat).VehicleResult);

        // A manned AFV is never the target of a capture attempt (A11.52).
        Assert.Contains("asl.a1.cc-vehicle.capture-outside", ScenarioA1VehicleCloseCombat.Precheck(Facts(true, true) with
        {
            Vehicle = tank with
            {
                Abandoned = false
            }
        }, CloseCombat));
    }

    [Fact]
    public void ATemporaryCrewsPenaltiesFollowWhoMansTheVehicle()
    {
        // A21.22: in a captured vehicle a vehicle crew, an infantry crew, a HS, or a SMC is a Temporary crew; in a friendly one a MMC other than a crew or a SMC
        // is; the vehicle's own crew, and a crew of its nationality, never is. A squad Deploys first.
        Assert.Equal(ScenarioA1CapturedVehicleRules.VehicleCrew, ScenarioA1CapturedVehicleRules.Manning("asl:vehicle-crew", false, false, true));
        Assert.Equal(ScenarioA1CapturedVehicleRules.InfantryCrew, ScenarioA1CapturedVehicleRules.Manning("asl:crew", false, false, true));
        Assert.Equal(ScenarioA1CapturedVehicleRules.HalfSquad, ScenarioA1CapturedVehicleRules.Manning("asl:half-squad", false, false, true));
        Assert.Equal(ScenarioA1CapturedVehicleRules.Smc, ScenarioA1CapturedVehicleRules.Manning("asl:leader", true, false, true));
        Assert.Null(ScenarioA1CapturedVehicleRules.Manning("asl:vehicle-crew", false, false, false));
        Assert.Null(ScenarioA1CapturedVehicleRules.Manning("asl:crew", false, false, false));
        Assert.Null(ScenarioA1CapturedVehicleRules.Manning("asl:half-squad", false, true, false));
        Assert.Contains("Deploys first", ScenarioA1CapturedVehicleRules.SquadBar("ru-s", "de-v", "asl:squad", ScenarioA1CapturedVehicleRules.HalfSquad), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CapturedVehicleRules.SquadBar("ru-h", "de-v", "asl:half-squad", ScenarioA1CapturedVehicleRules.HalfSquad));

        // Halved MP for a HS or an infantry crew: the PzKpfw IIIH's 11 MP (22 half MP) become 5 1/2; the start dr, +1 for a Russian captor: 4 or less starts,
        // 5 no movement, 6 or more Bogs.
        Assert.Equal(11, ScenarioA1CapturedVehicleRules.AllotmentHalfMp(22, 11, ScenarioA1CapturedVehicleRules.InfantryCrew));
        Assert.Equal(11, ScenarioA1CapturedVehicleRules.AllotmentHalfMp(22, 11, ScenarioA1CapturedVehicleRules.HalfSquad));
        Assert.Equal(22, ScenarioA1CapturedVehicleRules.AllotmentHalfMp(22, 11, ScenarioA1CapturedVehicleRules.VehicleCrew));
        Assert.Equal(22, ScenarioA1CapturedVehicleRules.AllotmentHalfMp(22, 11, null));
        Assert.True(ScenarioA1CapturedVehicleRules.StartDrRequired(ScenarioA1CapturedVehicleRules.InfantryCrew));
        Assert.False(ScenarioA1CapturedVehicleRules.StartDrRequired(ScenarioA1CapturedVehicleRules.VehicleCrew));
        Assert.Equal((1, 1, 0), (ScenarioA1CapturedVehicleRules.StartDrm("russian"), ScenarioA1CapturedVehicleRules.StartDrm("japanese"), ScenarioA1CapturedVehicleRules.StartDrm("german")));
        Assert.Equal((ScenarioA1ResultTables.CheckPassed, ScenarioA1ResultTables.CheckNoMove, ScenarioA1ResultTables.CheckBogged),
            (ScenarioA1ResultTables.VehicleCheck(ScenarioA1ResultTables.TemporaryStartCheck, 4), ScenarioA1ResultTables.VehicleCheck(ScenarioA1ResultTables.TemporaryStartCheck, 5),
                ScenarioA1ResultTables.VehicleCheck(ScenarioA1ResultTables.TemporaryStartCheck, 6)));
        Assert.True(ScenarioA1CapturedVehicleRules.StartStops(ScenarioA1ResultTables.CheckNoMove));

        // A vehicle crew in a captured vehicle takes red MP and the captured-weapon penalty; a HS doubles it there; a SMC and an infantry crew take it once.
        Assert.True(ScenarioA1CapturedVehicleRules.RedMp(ScenarioA1CapturedVehicleRules.VehicleCrew, true));
        Assert.False(ScenarioA1CapturedVehicleRules.RedMp(ScenarioA1CapturedVehicleRules.InfantryCrew, true));
        Assert.Equal((1, 1, 2, 1, (int?)null), (ScenarioA1CapturedVehicleRules.WeaponPenalty(ScenarioA1CapturedVehicleRules.VehicleCrew, true),
            ScenarioA1CapturedVehicleRules.WeaponPenalty(ScenarioA1CapturedVehicleRules.InfantryCrew, true), ScenarioA1CapturedVehicleRules.WeaponPenalty(ScenarioA1CapturedVehicleRules.HalfSquad, true),
            ScenarioA1CapturedVehicleRules.WeaponPenalty(ScenarioA1CapturedVehicleRules.Smc, true), ScenarioA1CapturedVehicleRules.WeaponPenalty(null, false)));
    }

    [Fact]
    public void ATemporaryCrewsOrdnanceFiresOnTheRedNumbersWithCaseHAndALowerBreakdown()
    {
        // A21.12, A21.13: a HS in a captured T-34 doubles the penalty: Case H +4, the B# four lower.
        var shot = AtTank(CaptureDice, []);
        var plain = Shot(shot);
        var captured = Shot(shot with
        {
            CapturedPenalty = 2
        });
        Assert.True(captured.ToHit is not null, string.Join("; ", captured.Reasons));
        Assert.Equal("red", captured.ToHit.Color);
        Assert.Equal(4, (int)captured.ToHit.Drm.Single(item => item.Name == "case-h:captured").Value);
        Assert.Equal(plain.Gun!.BreakdownNumber - 4, captured.Gun!.BreakdownNumber);
        Assert.DoesNotContain(plain.ToHit!.Drm, item => item.Name == "case-h:captured");
    }

    [Fact]
    public void ASmcCrewIsWoundedByAStunAndEliminatedByARecall()
    {
        // D5.343: Wounded on a Stun; eliminated on a Recall, but one with a Disabled MA (D3.7).
        Assert.Equal((true, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(true, false, false, false));
        Assert.Equal((false, true), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(false, true, false, false));
        Assert.Equal((false, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(false, true, true, false));
        Assert.Equal((false, false), ScenarioA1CapturedVehicleRules.SmcCrewOutcome(false, false, false, false));

        // A21.22: CE, the AAMG alone and no move; BU, the BMG or CMG; never both move and fire in one Player Turn.
        Assert.Null(ScenarioA1CapturedVehicleRules.SmcFireBar("de-ht", true, "aamg", false, false));
        Assert.Contains("only its AAMG", ScenarioA1CapturedVehicleRules.SmcFireBar("de-ht", true, "bmg", false, false), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CapturedVehicleRules.SmcFireBar("de-v", false, "bmg", false, false));
        Assert.Contains("BMG or CMG", ScenarioA1CapturedVehicleRules.SmcFireBar("de-v", false, "ma", false, false), StringComparison.Ordinal);
        Assert.Contains("both move and fire", ScenarioA1CapturedVehicleRules.SmcFireBar("de-v", false, "cmg", false, true), StringComparison.Ordinal);
        Assert.Contains("Immobile while he is CE", ScenarioA1CapturedVehicleRules.SmcMoveBar("de-v", true, false), StringComparison.Ordinal);
        Assert.Contains("both move and fire", ScenarioA1CapturedVehicleRules.SmcMoveBar("de-v", false, true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1CapturedVehicleRules.SmcMoveBar("de-v", false, false));
    }

    [Fact]
    public void AMannedCapturedVehicleHoldsControlForItsCaptor()
    {
        // A26.12 (ruling R65.22): a vehicle captured by the pass's record is its captor's, unmarked, and armed once manned; one marked captured under ruling
        // R11.16 is not (R24.5), nor an Abandoned one with no crew yet.
        VictoryUnitFacts Vehicle(bool captured, bool abandoned, bool? crew) =>
            new("de-v", "russian", "asl:vehicle", false, "bd01:G5", 0, false, false, false, false, captured, false, abandoned, true, crew, false, false, false, false);
        Assert.True(ScenarioA1VictoryCalculator.Armed(Vehicle(false, false, true)));
        Assert.False(ScenarioA1VictoryCalculator.Armed(Vehicle(true, true, false)));
        Assert.False(ScenarioA1VictoryCalculator.Armed(Vehicle(false, true, false)));
    }
}
