using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 60, Armored combat III. Increment 2a: HE Equivalency (C8.31, p. 178; rulings R60.2, R60.4 to R60.6): the table, AP, APCR, and HEAT at
/// Infantry on the Infantry Target Type, HEAT's target bar, a PF and a PSK at Personnel (C13.31, C13.9), and AP, HEAT, and an ATR at a Gun
/// (C11.52, C13.23, C11.6 note 5). The Russian 45mm PTP obr. 32 and the German 7.5cm leIG 18 with their crews, the PSK, the PF, and the ATR.
/// </summary>
public sealed class ScenarioA1Pass60RulesTests
{
    private static readonly ScenarioA1OrdnanceReference Reference = new ScenarioA1OrdnancePackage().Reference;
    private const string At = "bd01:G5:0";
    private const string HeatTargetOutside = "asl.a1.ordnance.heat-target-outside";

    private static FireTarget Target(string id, string definition) =>
        new(id, definition, At, false, false, false, false, false, false, false)
        {
            KnownEnemyInLos = true,
            Captors = [],
        };

    private static FireAttack Hit(string terrain, FireTarget[] targets, int month = 7) =>
        new("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), month, terrain, targets, 3, null);

    /// <summary>The Russian 45mm AT Gun and its crew at German Infantry five hexes away, in July 1943, with the named ammunition.</summary>
    private static OrdnanceShot Russian(string? ammunition, string terrain = "open-ground", FireTarget[]? targets = null, int year = 1943) =>
        new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("ru-gun", "defender-at-gun", false, 0, false, false),
            new OrdnanceCrew("ru-crew", "defender-crew", false, false, false, false, false), At, 5, 0, false, true, 0,
            Hit(terrain, targets ?? [Target("de-s", "attacker-squad")]), new OrdnanceRolls(null, null, null, null, null))
        {
            Ammunition = ammunition,
            ScenarioYear = year,
        };

    /// <summary>The German 7.5cm leIG 18 (H7, no AP) and its crew at Russian Infantry five hexes away, in July 1943.</summary>
    private static OrdnanceShot German(string? ammunition, string terrain = "open-ground", FireTarget[]? targets = null) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("de-gun", "attacker-inf-gun", false, 0, false, false),
            new OrdnanceCrew("de-crew", "attacker-crew", false, false, false, false, false), At, 5, 0, false, true, 0,
            Hit(terrain, targets ?? [Target("ru-s", "defender-squad")]), new OrdnanceRolls(null, null, null, null, null))
        {
            Ammunition = ammunition,
            ScenarioYear = 1943,
        };

    /// <summary>A German squad's LATW (a PF or the PSK) at Russian Infantry with no vehicle, in July 1944.</summary>
    private static OrdnanceShot Latw(string gun, string definition, int range, string terrain, params FireTarget[] targets) =>
        new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun(gun, definition, false, 0, false, false),
            new OrdnanceCrew("de-s", "attacker-squad", false, false, false, false, false), At, range, 0, false, true, 0, Hit(terrain, targets),
            new OrdnanceRolls(null, null, null, null, null))
        {
            Ammunition = "heat",
            ScenarioYear = 1944,
            Panzerfaust = definition == OrdnanceTargetTypes.Panzerfaust ? new OrdnancePanzerfaust(0, 4, false, false) : new OrdnancePanzerfaust(null, null, false, null),
        };

    /// <summary>A shot at a Gun manned by its crew alone, or with the given others beside it (C11).</summary>
    private static OrdnanceShot AtGun(OrdnanceShot shot, string gun, string definition, string crew, string crewDefinition, params FireTarget[] others) => shot with
    {
        Hit = shot.Hit! with
        {
            Targets = [Target(crew, crewDefinition), .. others],
            GunTarget = new FireGunTarget(gun, definition, crew, false, false),
        },
    };

    private static OrdnanceResolution Resolve(OrdnanceShot shot, int[] toHit, int[]? ift = null, IReadOnlyDictionary<string, int>? selection = null, int? pfCheck = null,
        ScenarioA1OrdnanceReference? reference = null)
    {
        var rolls = shot.Rolls! with
        {
            ToHit = toHit,
            CriticalSelection = selection,
            PanzerfaustCheck = pfCheck
        };
        var last = string.Empty;
        for (var step = 0; step < 40; step++)
        {
            var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
            {
                Rolls = rolls
            }, reference ?? Reference);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.ordnance.roll-missing:", StringComparison.Ordinal))
            {
                return result;
            }

            var key = reason["asl.a1.ordnance.roll-missing:".Length..];
            last = key;
            if (key.StartsWith("areaSurvival:", StringComparison.Ordinal))
            {
                // C1.55, D5.6 (pass 60): the Crew Survival DR of an AFV a mortar destroyed.
                rolls = rolls with
                {
                    AreaSurvival = new Dictionary<string, IReadOnlyList<int>>(rolls.AreaSurvival ?? new Dictionary<string, IReadOnlyList<int>>()) { [key["areaSurvival:".Length..]] = [3, 4] }
                };
                continue;
            }

            var critical = key.StartsWith("critical-hit:", StringComparison.Ordinal);
            var inner = key[(key.IndexOf(':', StringComparison.Ordinal) + 1)..];
            var unit = inner.Contains(':', StringComparison.Ordinal) ? inner[(inner.IndexOf(':', StringComparison.Ordinal) + 1)..] : string.Empty;
            var fire = (critical ? rolls.CriticalHit : rolls.Hit) ?? new FireRolls(null, null, null, null);
            fire = inner.StartsWith("attack", StringComparison.Ordinal) ? fire with
            {
                Attack = ift ?? [3, 4]
            }
                : inner.StartsWith("randomSelection:", StringComparison.Ordinal) ? fire with
                {
                    RandomSelection = unit.Split(',').ToDictionary(id => id, _ => 3)
                }
                : inner.StartsWith("checks:", StringComparison.Ordinal) ? fire with
                {
                    Checks = new Dictionary<string, IReadOnlyList<int>>(fire.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = [3, 4] }
                }
                : inner.StartsWith("crewCheck:", StringComparison.Ordinal) ? fire with
                {
                    CrewChecks = new Dictionary<string, IReadOnlyList<int>>(fire.CrewChecks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = [2, 2] }
                }
                : fire with
                {
                    WoundSeverity = new Dictionary<string, int> { [unit] = 2 }
                };
            rolls = critical ? rolls with
            {
                CriticalHit = fire
            } : rolls with
            {
                Hit = fire
            };
        }

        throw new InvalidOperationException("The package kept asking for rolls: " + last);
    }

    private static void Resolved(OrdnanceResolution result) => Assert.True(result.Disposition == OrdnanceResolution.Resolved, string.Join("; ", result.Reasons));

    [Fact]
    public void ApBelow37mmIsOneFp() => Assert.Equal(1, ScenarioA1HeEquivalency.Firepower("ap", ScenarioA1HeEquivalency.Gun, 20, 1));

    [Fact]
    public void Ap37mmAndUpIsTwoFp() => Assert.Equal((2, 2), (ScenarioA1HeEquivalency.Firepower("ap", ScenarioA1HeEquivalency.Gun, 37, 2),
        ScenarioA1HeEquivalency.Firepower("ap", ScenarioA1HeEquivalency.Gun, 88, 16)));

    [Fact]
    public void EveryApcrApdsAndAtrIsOneFp() => Assert.Equal((1, 1, 1), (ScenarioA1HeEquivalency.Firepower("apcr", ScenarioA1HeEquivalency.Gun, 75, 12),
        ScenarioA1HeEquivalency.Firepower("apds", ScenarioA1HeEquivalency.Gun, 57, 6), ScenarioA1HeEquivalency.Firepower("ap", ScenarioA1HeEquivalency.AntiTankRifle, 14, null)));

    [Fact]
    public void ABazookaOrPiatIsEightFp() => Assert.Equal((8, 8), (ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Bazooka, 60, null),
        ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Piat, 88, null)));

    [Fact]
    public void APskPfkOrBracketedHeatIsTwelveFp() => Assert.Equal((12, 12, 12), (ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Panzerschreck, 88, null),
        ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.PanzerfaustKlein, 0, null), ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.BracketedHeat, 37, 2)));

    [Fact]
    public void APfIsSixteenFp() => Assert.Equal(16, ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Panzerfaust, 0, null));

    [Fact]
    public void OtherOrdnanceHeatIsOneColumnLeftOfItsHeFp()
    {
        // "Use IFT column to the left of Gun's normal HE FP": 12 is 8, 4 is 2, 6 is 4; a Gun with no HE, or HE of 1, has none to step left from.
        Assert.Equal((8, 2, 4), (ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Gun, 75, 12), ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Gun, 45, 4),
            ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Gun, 57, 6)));
        Assert.Null(ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Gun, 57, null));
        Assert.Null(ScenarioA1HeEquivalency.Firepower("heat", ScenarioA1HeEquivalency.Gun, 20, 1));

        // HE and an unnamed round are no equivalency.
        Assert.Null(ScenarioA1HeEquivalency.Firepower("he", ScenarioA1HeEquivalency.Gun, 75, 12));
        Assert.Null(ScenarioA1HeEquivalency.Firepower(null, ScenarioA1HeEquivalency.Gun, 75, 12));
    }

    [Fact]
    public void ApAtInfantryAttacksByItsHeEquivalency()
    {
        // C8.31: "AP is also given HE Equivalency for normal A-P use": the 45mm's AP attacks on the 2 column, HE in the open as Case Q (none here).
        var result = Resolve(Russian("ap"), [3, 3], ift: [5, 6]);
        Resolved(result);
        Assert.True(result.ToHit!.Hit);
        Assert.Equal((2, 2), (result.HeEquivalency, (result.Hit ?? result.CriticalHit)!.Arithmetic!.ColumnFp / (result.CriticalHit is null ? 1 : 2)));
        Assert.Null(result.AmmunitionUse);

        // An unnamed round at Infantry stays HE, with no equivalency.
        var he = Resolve(Russian(null), [3, 3], ift: [5, 6]);
        Resolved(he);
        Assert.Null(he.HeEquivalency);
        Assert.Equal(4, (he.Hit ?? he.CriticalHit)!.Arithmetic!.ColumnFp / (he.CriticalHit is null ? 1 : 2));
    }

    [Fact]
    public void ApcrIsOneFpAndUsesItsDepletionNumber()
    {
        // C8.31 note 2: "Includes all APCR/APDS/ATR"; C8.9: the 45mm's A5 in 1943, used below it (pass 55's steps), none before its A4 of 1942.
        var result = Resolve(Russian("apcr"), [3, 1], ift: [5, 6]);
        Resolved(result);
        Assert.Equal((1, "used"), (result.HeEquivalency, result.AmmunitionUse));
        Assert.Contains("asl.a1.ordnance.ammunition-unavailable", Resolve(Russian("apcr", year: 1941), [3, 1]).Reasons);
        Assert.Contains("asl.a1.ordnance.ammunition-outside", Resolve(Russian("apds"), [3, 1]).Reasons);
    }

    [Fact]
    public void HeatAtInfantryNeedsAWallBuildingOrRubbleTem()
    {
        // C8.31: "HEAT may only be fired at a vehicle or Gun, or at Infantry/Cavalry receiving a wall/building/rubble/pillbox TEM". The leIG 18's
        // HE is 12, so its HEAT is 8.
        var building = Resolve(German("heat", "stone-building"), [2, 3], ift: [5, 6]);
        Resolved(building);
        Assert.Equal((8, "used"), (building.HeEquivalency, building.AmmunitionUse));
        Assert.Contains(building.ToHit!.Drm, item => item.Name == "case-q:stone-building");

        Assert.Equal([HeatTargetOutside], Resolve(German("heat"), [2, 3]).Reasons);
        Resolved(Resolve(German("heat") with
        {
            WallTem = true
        }, [2, 3], ift: [5, 6]));
        Resolved(Resolve(German("heat", "wooden-rubble"), [2, 3], ift: [5, 6]));

        // The leIG 18 fires no AP (C2.21).
        Assert.Contains("asl.a1.ordnance.ammunition-outside", Resolve(German("ap"), [2, 3]).Reasons);
    }

    [Fact]
    public void AGunWithNoHeHasNoHeatToFireAtInfantry()
    {
        // C8.31: a Gun's HEAT steps one column left of its HE FP; a made-up Gun that lists HEAT and no HE has none to step from.
        var made = new GunDefinition("made-up", "russian", "at", 57, "l", 3, 12, 225, true, false)
        {
            SpecialAmmo = ["H7"],
        };
        var shot = Russian("heat", "stone-building") with
        {
            Gun = new OrdnanceGun("ru-gun", "made-up", false, 0, false, false)
        };
        Assert.Contains("asl.a1.ordnance.heat-no-he", Resolve(shot, [2, 3], reference: Reference.WithGun(made)).Reasons);
    }

    [Fact]
    public void APfAtASquadInABuildingTakesThePlusOneAndHitsOneUnitByRandomSelection()
    {
        // C13.31: +1 to the PF Check "if it is fired at other than an AFV"; a PF "may not directly affect more than one unit ... unless the Random
        // Selection DR indicates several units are affected". A dr of 2 is a final 3: a shot. The PF's 16 FP (C8.31) on the one unit picked.
        var shot = Latw("de-s:pf", OrdnanceTargetTypes.Panzerfaust, 1, "stone-building", Target("ru-s", "defender-squad"), Target("ru-s2", "defender-squad"));
        var result = Resolve(shot, [2, 2], ift: [6, 6], selection: new Dictionary<string, int> { ["ru-s"] = 5, ["ru-s2"] = 2 }, pfCheck: 2);
        Resolved(result);
        Assert.Contains(result.PanzerfaustCheck!.Drm, item => item.Name == "not-afv" && item.Value == 1);
        Assert.Equal(OrdnancePanzerfaustCheck.Shot, result.PanzerfaustCheck.Outcome);
        Assert.Equal((16, 16), (result.HeEquivalency, result.Hit!.Arithmetic!.ColumnFp));
        Assert.Equal(["ru-s"], result.Hit.Effects.Select(item => item.UnitId));
        Assert.Equal((0, (string?)null), (result.Gun!.Acquisition, result.Gun.AcquiredLocationId));

        // Asked for before the IFT DR: the Random Selection dr of each unit.
        Assert.Equal(["asl.a1.ordnance.roll-missing:criticalSelection:ru-s,ru-s2"], ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = new OrdnanceRolls([2, 2], null, null, null, null) { PanzerfaustCheck = 2 }
        }, Reference).Reasons);

        // In the open the PF's HEAT is refused at Infantry, its detail telling a weapon with no HE or AP.
        Assert.Contains(HeatTargetOutside + ":latw", Resolve(Latw("de-s:pf", OrdnanceTargetTypes.Panzerfaust, 1, "open-ground", Target("ru-s", "defender-squad")), [2, 2], pfCheck: 2).Reasons);
    }

    [Fact]
    public void APskAtInfantryInTheOpenIsRefusedAndInABuildingHitsEveryUnit()
    {
        // C13.48, C8.31: a PSK's IFT effect is 12, and HEAT goes only at Infantry behind a wall or in a building or rubble.
        Assert.Equal([HeatTargetOutside + ":latw"], Resolve(Latw("de-psk", "attacker-psk", 2, "open-ground", Target("ru-s", "defender-squad")), [2, 3]).Reasons);
        var result = Resolve(Latw("de-psk", "attacker-psk", 2, "wooden-building", Target("ru-s", "defender-squad"), Target("ru-s2", "defender-squad")), [2, 3], ift: [6, 6]);
        Resolved(result);
        Assert.Equal(12, result.HeEquivalency);
        Assert.Equal(["ru-s", "ru-s2"], new[] { result.Hit, result.CriticalHit }.OfType<FireResolution>().SelectMany(item => item.Effects).Select(item => item.UnitId)
            .Order(StringComparer.Ordinal));
        Assert.Null(result.CriticalTarget);
    }

    [Fact]
    public void AnApKAtAGunEliminatesTheGun()
    {
        // C11.52, the Gun Destruction Table's note 5: "K result = Elim if AP was fired". The 45mm's AP at the leIG 18's crew: 2 FP, IFT DR 2: K/1.
        var result = Resolve(AtGun(Russian("ap"), "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew"), [3, 3], ift: [1, 1]);
        Resolved(result);
        Assert.Equal(("K/1", "destroyed"), (result.Hit!.Arithmetic!.Result, result.GunTargetFate));
    }

    [Fact]
    public void HeatsKAtAGunMalfunctionsIt()
    {
        // C11.51: "HEAT destroys a Gun using the same mechanics as an HE hit": a K malfunctions it (C11.4). The leIG 18's HEAT at the 45mm's crew,
        // which needs no wall: it is a Gun. 8 FP, IFT DR 3: K/2.
        var result = Resolve(AtGun(German("heat"), "ru-gun", "defender-at-gun", "ru-crew", "defender-crew"), [3, 3], ift: [1, 2]);
        Resolved(result);
        Assert.Equal(("K/2", "malfunctioned"), (result.Hit!.Arithmetic!.Result, result.GunTargetFate));
    }

    [Fact]
    public void AnAtrAtAGunAttacksOnlyTheGunsCrewAtOneFp()
    {
        // C13.23, C11.52 (ruling R60.6): an ATR "can be used vs Guns as per 11.52", at its 1 FP; a 14mm ATR attacks only the Gun and its manning
        // crew, the squad beside them untouched.
        var atr = new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("ru-atr", "defender-atr", false, 0, false, false),
            new OrdnanceCrew("ru-s", "defender-squad", false, false, false, false, false), At, 2, 0, false, true, 0,
            Hit("open-ground", [Target("de-s", "attacker-squad")]), new OrdnanceRolls(null, null, null, null, null))
        {
            Ammunition = "ap",
            ScenarioYear = 1943,
        };
        var result = Resolve(AtGun(atr, "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew", Target("de-s", "attacker-squad")), [3, 3], ift: [1, 1]);
        Resolved(result);
        Assert.Equal((1, 1), (result.HeEquivalency, result.Hit!.Arithmetic!.ColumnFp));
        Assert.Equal(["de-crew"], result.Hit.Effects.Select(item => item.UnitId));

        // At Personnel with no Gun it is Small Arms, not ordnance (C13.24).
        Assert.Contains("asl.a1.ordnance.target-type-outside", Resolve(atr, [3, 3]).Reasons);
    }

    // Increment 2b: the Specific Collateral Attack (D.8A, p. 192; ruling R60.7), the Infantry Target Type at a Location holding a vehicle (C3.32, C3.41;
    // ruling R60.8), and Known and concealed units together (C3.4, A12.13; ruling R60.9).

    /// <summary>
    /// A tank's MA at an enemy tank's front three hexes away, a hull hit (the colored dr above the white), in 1942: the German PzKpfw IIIH at the
    /// Russian T-34 M41, or the T-34 at the PzKpfw IIIH. The target's crew is CE unless told otherwise, and the shot carries pass 60's fact unless told.
    /// </summary>
    private static OrdnanceShot TankAtTank(bool german, string ammunition, int[] toKill, bool crewExposed = true, bool fact = true, int[]? shock = null,
        int[]? collateral = null)
    {
        var (gun, nationality, target) = german ? ("attacker-tank", "german", "defender-tank") : ("defender-tank", "russian", "attacker-tank");
        return new OrdnanceShot("PFPh", "phasing", nationality, new OrdnanceGun("firer", gun, false, 0, false, false),
            new OrdnanceCrew("firer", gun, false, false, false, false, false), At, 3, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls([4, 2], null, null, null, null) { ToKill = toKill, ShockCheck = shock, CollateralCheck = collateral })
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false),
            VehicleTarget = new OrdnanceVehicleTarget("struck", target, "front", "front", false, false, false, false, false) { CrewExposed = crewExposed ? true : null },
            Ammunition = ammunition,
            ScenarioYear = 1942,
            SpecificCollateral = fact ? true : null,
        };
    }

    private static int[] DiceOf(int sum) => [sum / 2, sum - (sum / 2)];

    private static FireVehicle Vehicle(string id, string definition, bool crewExposed = false, bool concealed = false) =>
        new(id, definition, At, crewExposed, false, false, false)
        {
            Concealed = concealed ? true : null
        };

    /// <summary>The Russian 45mm's HE at the target Location, holding the given units and vehicles.</summary>
    private static OrdnanceShot AtLocation(FireTarget[] targets, params FireVehicle[] vehicles)
    {
        var shot = Russian(null, targets: targets);
        return shot with
        {
            Hit = shot.Hit! with
            {
                Vehicles = vehicles.Length > 0 ? vehicles : null
            }
        };
    }

    [Fact]
    public void ASpecificCollateralAttackFollowsAMissedTkDrAtACeTank()
    {
        // D.8A: the PzKpfw IIIH's 50mm HE does not penetrate the T-34's front on a TK DR of 3; its CE crew takes the 50mm's HE FP on the same DR,
        // "the only DRM which applies is that for any applicable CE status" (+2, D5.31).
        var result = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(true, "he", [1, 2], collateral: [2, 2]), Reference);
        Resolved(result);
        Assert.Equal(OrdnanceKill.None, result.Kill!.Result);
        var collateral = result.SpecificCollateral!;
        var fp = Reference.HeFirepower(50);
        Assert.Equal((fp, 5), (collateral.Firepower, collateral.Effect.FinalDr));
        Assert.Equal(["crew-exposed"], collateral.Effect.Drm.Select(item => item.Name));
        Assert.Equal(Reference.Fire.Result(5, Array.IndexOf(ScenarioA1FireReference.ColumnFp, fp)), collateral.Result);
        Assert.Equal("struck", collateral.Effect.VehicleId);
    }

    [Fact]
    public void ASpecificCollateralAttackByApTakesItsHeEquivalency()
    {
        // D.8: "the attacking weapon's/ammo type's IFT FP (which includes HE Equivalency; C8.31)": the 50mm's AP is 2 FP.
        var result = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(true, "ap", [3, 4], collateral: [2, 2]), Reference);
        Resolved(result);
        Assert.Equal(OrdnanceKill.None, result.Kill!.Result);
        Assert.Equal((2, 9), (result.SpecificCollateral!.Firepower, result.SpecificCollateral.Effect.FinalDr));
    }

    [Fact]
    public void NoSpecificCollateralAttackAtABuTankOrInAnOlderRecord()
    {
        // D5.3: a BU crew is not Vulnerable. R60.3: a shot without pass 60's fact, as every one recorded before it, resolves as it did.
        Assert.Null(ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(true, "he", [1, 2], crewExposed: false, collateral: [2, 2]), Reference).SpecificCollateral);
        var older = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(true, "he", [1, 2], fact: false, collateral: [2, 2]), Reference);
        Resolved(older);
        Assert.Null(older.SpecificCollateral);
        Assert.Equal(OrdnanceKill.None, older.Kill!.Result);
    }

    [Fact]
    public void NoSpecificCollateralAttackOnADudOrAKill()
    {
        // C7.35 and the Collateral Attack Table's note 4: none on a Dud; D.8: none "if the vehicle is destroyed outright". The T-34's AP at the
        // PzKpfw IIIH's front: a TK DR of 2 is at most half its Final TK#.
        var dud = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(false, "ap", [6, 6], collateral: [2, 2]), Reference);
        Assert.Equal((OrdnanceKill.Dud, (OrdnanceCollateral?)null), (dud.Kill!.Result, dud.SpecificCollateral));
        var burnt = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(false, "ap", [1, 1], collateral: [2, 2]), Reference);
        Assert.Equal((OrdnanceKill.Burn, (OrdnanceCollateral?)null), (burnt.Kill!.Result, burnt.SpecificCollateral));
    }

    [Fact]
    public void NoSpecificCollateralAttackAfterAShock()
    {
        // C7.42: the crew of a shocked AFV "are not subject to any Collateral Attack". A TK DR one above the Final TK# is a possible Shock: a failed
        // NTC Shocks it; a passed one leaves the crew to the attack.
        var finalTk = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(false, "ap", [6, 6]), Reference).Kill!.FinalTk;
        var shocked = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(false, "ap", DiceOf(finalTk + 1), shock: [6, 6], collateral: [2, 2]), Reference);
        Resolved(shocked);
        Assert.Equal((OrdnanceKill.PossibleShock, true), (shocked.Kill!.Result, shocked.Kill.Shocked));
        Assert.Null(shocked.SpecificCollateral);
        var passed = ScenarioA1OrdnanceCalculator.Resolve(TankAtTank(false, "ap", DiceOf(finalTk + 1), shock: [1, 1], collateral: [2, 2]), Reference);
        Resolved(passed);
        Assert.Equal(finalTk + 3, passed.SpecificCollateral!.Effect.FinalDr);
    }

    [Fact]
    public void AnHeHitAtASquadAndATruckAttacksBothOnOneDr()
    {
        // C3.32 and its EX: each target is hit on its own Final TH#, and "all that are hit are then attacked on the IFT with a single Effects DR",
        // the truck on the Vehicle line (A7.308).
        var shot = AtLocation([Target("de-s", "attacker-squad")], Vehicle("de-truck", "attacker-truck"));
        var result = Resolve(shot, [2, 3], ift: [3, 4], selection: new Dictionary<string, int> { ["de-s"] = 5, ["de-truck"] = 2 });
        Resolved(result);
        Assert.Equal([("de-s", true), ("de-truck", true)], result.TargetHits!.Select(item => (item.UnitId, item.Hit)));
        var attacks = new[] { result.Hit, result.CriticalHit }.OfType<FireResolution>().ToArray();
        Assert.Contains(attacks, attack => attack.Effects.Any(effect => effect.UnitId == "de-s"));
        var truck = attacks.SelectMany(attack => attack.VehicleEffects ?? []).Single();
        Assert.Equal("de-truck", truck.VehicleId);
        Assert.NotNull(truck.KillNumber);
        Assert.Equal(7, attacks.Select(attack => attack.Arithmetic!.OriginalDr).Distinct().Single());
    }

    [Fact]
    public void ACeAfvBesideASquadIsImmuneAndItsCrewIsAttacked()
    {
        // C3.32: "All AFV (but not their Vulnerable PRC) in the target Location are immune"; D.8B: its CE crew takes the hit's DR with +2 for CE.
        var shot = AtLocation([Target("de-s", "attacker-squad")], Vehicle("de-tank", "attacker-tank", crewExposed: true));
        var result = Resolve(shot, [2, 3], ift: [3, 4], selection: new Dictionary<string, int> { ["de-s"] = 5 });
        Resolved(result);
        var crew = new[] { result.Hit, result.CriticalHit }.OfType<FireResolution>().SelectMany(attack => attack.VehicleEffects ?? []).Single();
        Assert.Equal(("de-tank", (int?)null, FireVehicleEffect.None), (crew.VehicleId, crew.KillNumber, crew.Result));
        Assert.Contains(crew.Drm, item => item.Name == "crew-exposed");

        // A BU AFV is not fired at at all; a Location of AFVs alone is the Vehicle Target Type's (C3.41).
        Assert.Null(Resolve(AtLocation([Target("de-s", "attacker-squad")], Vehicle("de-tank", "attacker-tank")), [2, 3], ift: [3, 4]).TargetHits);
        Assert.Contains("asl.a1.ordnance.target-outside", Resolve(AtLocation([], Vehicle("de-tank", "attacker-tank", crewExposed: true)), [2, 3]).Reasons);
    }

    [Fact]
    public void ADrThatHitsOnlyTheKnownSquadLeavesTheConcealedOneAlone()
    {
        // C3.4, the A12.13 EX: the Known squad is hit on a Final DR the concealed one, with Case K's +2, is not: it is not attacked and keeps "?".
        var shot = Russian(null, targets: [Target("de-s", "attacker-squad"), Target("de-c", "attacker-squad") with { Concealed = true }]);
        var preview = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Rolls = shot.Rolls! with
            {
                ToHit = [6, 6]
            }
        }, Reference);
        var known = preview.TargetHits!.Single(item => item.UnitId == "de-s");
        var need = preview.ToHit!.ModifiedToHit - (known.FinalDr - 12);

        // C3.6 (the boundary review): Rules gives each target's needed Original DR for the proposal, Case K's +2 two lower.
        Assert.Equal((need, need - 2), (known.NeededOriginal, preview.TargetHits!.Single(item => item.UnitId == "de-c").NeededOriginal));
        var result = Resolve(shot, DiceOf(need), ift: [3, 4]);
        Resolved(result);
        Assert.Equal([("de-s", true), ("de-c", false)], result.TargetHits!.Select(item => (item.UnitId, item.Hit)));
        Assert.Contains(result.TargetHits!.Single(item => item.UnitId == "de-c").Drm, item => item.Name == "case-k");
        Assert.Equal(["de-s"], new[] { result.Hit, result.CriticalHit }.OfType<FireResolution>().SelectMany(attack => attack.Effects).Select(effect => effect.UnitId));
    }

    // Increment 2c: Indirect Fire (C1.55, C3.332, C3.72, C9.1, B23.32, B13.3, C6.521, C11.4, C3.71; rulings R60.10 to R60.14). The Russian 50mm
    // mortar's 6 HE FP: a hit on the 2 column (half), a Critical Hit on the 12 column (full, doubled).

    private const string Upstairs = "bd01:G5:1";

    /// <summary>The Russian 50mm mortar's squad on the Area Target Type at German targets five hexes away (Modified TH# 7), in July 1943.</summary>
    private static OrdnanceShot Mortar(string terrain, FireTarget[] targets, params FireVehicle[] vehicles)
    {
        var shot = new OrdnanceShot("PFPh", "phasing", "russian", new OrdnanceGun("ru-mtr", "defender-light-mortar", false, 0, false, false),
            new OrdnanceCrew("ru-s", "defender-squad", false, false, false, false, false), At, 5, 0, false, true, 0, Hit(terrain, targets),
            new OrdnanceRolls(null, null, null, null, null))
        {
            TargetType = OrdnanceTargetTypes.Area,
        };
        return shot with
        {
            Hit = shot.Hit! with
            {
                Vehicles = vehicles.Length > 0 ? vehicles : null
            }
        };
    }

    [Fact]
    public void AMortarsHitOnATruckIsOnTheVehicleLine()
    {
        // C1.55, C3.332: a truck hit by HE on the Area Target Type is resolved on the Vehicle line of the hit's column (2 FP, Kill Number 4): an IFT DR of
        // 3 eliminates it, and the squad beside it takes the same DR.
        var result = Resolve(Mortar("open-ground", [Target("de-s", "attacker-squad")], Vehicle("de-truck", "attacker-truck")), [2, 3], ift: [1, 2]);
        Resolved(result);
        Assert.Equal([("de-s", true), ("de-truck", true)], result.AreaTargets!.Select(item => (item.UnitId, item.Hit)));
        var truck = result.Hit!.VehicleEffects!.Single();
        Assert.Equal(("de-truck", 4, FireVehicleEffect.Eliminated), (truck.VehicleId, truck.KillNumber, truck.Result));
        Assert.Equal(["de-s"], result.Hit.Effects.Select(item => item.UnitId));
        Assert.Null(result.AreaVehicles);
    }

    [Fact]
    public void AMortarsHitOnAPzKpfwIiiHIsOnTheIftAtHalfFp()
    {
        // C1.55, C9.1: never a To Kill DR; the 2 column's K/# is a DR of 2, and its AF (hull 6 front, so not all 4 or less) give no DRM. C7.7 note E
        // (ruling R60.23): the Original IFT DR's colored dr below the white strikes the turret (C3.9), whatever the To Hit DR's dice: a DR of 2 or 3
        // Shocks it; otherwise the hull, which they immobilize.
        var turret = Resolve(Mortar("open-ground", [], Vehicle("de-tank", "attacker-tank")), [4, 2], ift: [1, 2]);
        Resolved(turret);
        var shocked = turret.AreaVehicles!.Single();
        Assert.Equal(("turret", 2, 3, 2, OrdnanceKill.Shock), (shocked.HitLocation, shocked.Firepower, shocked.FinalDr, shocked.KillDr, shocked.Result));
        Assert.Empty(shocked.Drm);
        var hull = Resolve(Mortar("open-ground", [], Vehicle("de-tank", "attacker-tank")), [2, 4], ift: [2, 1]);
        Assert.Equal(("hull", 3, OrdnanceKill.Immobilized), (hull.AreaVehicles!.Single().HitLocation, hull.AreaVehicles![0].FinalDr, hull.AreaVehicles[0].Result));

        // A MC or PTC does nothing to the AFV.
        Assert.Equal(OrdnanceKill.None, Resolve(Mortar("open-ground", [], Vehicle("de-tank", "attacker-tank")), [4, 2], ift: [2, 2]).AreaVehicles!.Single().Result);
    }

    [Fact]
    public void AMortarsHitOnAnOtHalftrackTakesTheAfAndOtDrm()
    {
        // C1.55: the SPW 251/1's every AF is 1 (-1) and it is OT (-1): an IFT DR of 4 is a Final 2, the K/# DR, a hull hit, immobilizing it.
        var result = Resolve(Mortar("open-ground", [], Vehicle("de-ht", "attacker-halftrack", crewExposed: true)), [2, 3], ift: [2, 2]);
        Resolved(result);
        var halftrack = result.AreaVehicles!.Single();
        Assert.Equal(["all-af-4-or-less", "open-topped"], halftrack.Drm.Select(item => item.Name));
        Assert.Equal((2, OrdnanceKill.Immobilized), (halftrack.FinalDr, halftrack.Result));

        // D.8B, C3.332: the standing AFV's CE crew is attacked Collaterally on the same DR with its CE DRM alone, not the AFV's AF and OT DRM.
        var crew = result.Hit!.VehicleEffects!.Single();
        Assert.Equal(["crew-exposed"], crew.Drm.Select(item => item.Name));
        Assert.Equal(6, crew.FinalDr);
    }

    [Fact]
    public void ACriticalHitOnAnAfvIsAtDoubledFullFpWithNoTem()
    {
        // C3.72, C9.5: the 12 column (K/# a DR of 3), the stone rubble's +3 left out; an IFT DR of 2 is a KIA but not at most half of 3: the PzKpfw
        // IIIH is a wreck its crew may survive (D5.6). A normal hit there takes the +3 on the 2 column.
        var critical = Resolve(Mortar("stone-rubble", [], Vehicle("de-tank", "attacker-tank")), [1, 1], ift: [1, 1]);
        Resolved(critical);
        var made = critical.AreaVehicles!.Single();
        Assert.Equal((true, 12, 3, OrdnanceKill.Eliminated), (made.Critical, made.Firepower, made.KillDr, made.Result));
        Assert.Empty(made.Drm);
        Assert.Null(critical.Hit);
        var normal = Resolve(Mortar("stone-rubble", [], Vehicle("de-tank", "attacker-tank")), [2, 3], ift: [1, 1]).AreaVehicles!.Single();
        Assert.Equal((2, "tem:stone-rubble", 5), (normal.Firepower, normal.Drm.Single().Name, normal.FinalDr));
    }

    [Fact]
    public void AnOtHalftrackInWoodsIsHitAsUnarmoredAndACeTanksCrewTakesTheAirBurst()
    {
        // D5.311, B13.3: an OT AFV hit by Air Bursts is attacked as unarmored, on the Vehicle line, with no Air Burst TEM: an IFT DR of 4 equals the
        // 2 column's Kill Number and immobilizes it.
        var halftrack = Resolve(Mortar("woods", [], Vehicle("de-ht", "attacker-halftrack", crewExposed: true)), [2, 2], ift: [2, 2]);
        Resolved(halftrack);
        Assert.Null(halftrack.AreaVehicles);
        var line = halftrack.Hit!.VehicleEffects!.Single();
        Assert.Equal((4, FireVehicleEffect.Immobilized), (line.FinalDr, line.Result));
        Assert.DoesNotContain(line.Drm, item => item.Name == "air-burst");

        // A CT AFV takes nothing from the woods; its CE crew takes the -1 with its +2 (B13.3, D5.31).
        var tank = Resolve(Mortar("woods", [], Vehicle("de-tank", "attacker-tank", crewExposed: true)), [2, 2], ift: [2, 2]);
        Resolved(tank);
        Assert.Empty(tank.AreaVehicles!.Single().Drm);
        Assert.Equal(["air-burst", "crew-exposed"], tank.Hit!.VehicleEffects!.Single().Drm.Select(item => item.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void AnEmplacedGunInWoodsTakesItsEmplacementWithTheAirBurst()
    {
        // B13.3 (the foxhole EX's rule; no entrenchment is built): the -1 "is always applicable", even with "emplacements": +2 -1.
        var shot = AtGun(Mortar("woods", []), "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew");
        shot = shot with
        {
            Hit = shot.Hit! with
            {
                GunTarget = new FireGunTarget("de-gun", "attacker-inf-gun", "de-crew", true, true)
            }
        };
        var result = Resolve(shot, [2, 3], ift: [3, 4]);
        Resolved(result);
        Assert.Equal([("air-burst", -1m), ("emplacement:de-gun", 2m)], result.Hit!.Arithmetic!.Drm.Select(item => (item.Name, item.Value)));
    }

    [Fact]
    public void AGunshieldGivesPlusOneAgainstAMortarAndAKMalfunctionsTheGun()
    {
        // C11.4, C11.5: a Near Miss on the crew of the leIG 18 in the open takes its gunshield's +1, not +2, against Indirect Fire.
        var shot = AtGun(Mortar("open-ground", []), "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew");
        shot = shot with
        {
            Hit = shot.Hit! with
            {
                GunTarget = new FireGunTarget("de-gun", "attacker-inf-gun", "de-crew", false, true)
            }
        };
        var near = Resolve(shot, [1, 2], ift: [3, 4]);
        Resolved(near);
        Assert.Contains(near.Hit!.Arithmetic!.Drm, item => item.Name == "gunshield:de-gun" && item.Value == 1);
        Assert.Null(near.GunTargetFate);

        // A K (the 2 column's DR of 2) is a Direct Hit: the Gun malfunctions; a Critical Hit destroys it.
        Assert.Equal("malfunctioned", Resolve(shot, [1, 2], ift: [1, 1]).GunTargetFate);
        Assert.Equal("destroyed", Resolve(shot, [1, 1], ift: [3, 4]).GunTargetFate);
    }

    [Fact]
    public void ATwoLevelStoneBuildingIsHitAtEachLevelWithPlusOneBelow()
    {
        // B23.32: one IFT DR attacks both levels, the ground level with +1 for the level above it (its +3 a +4, as the EX's Level 1 squad of a stone
        // Multi-Story Building), Level 1 with the building's +3.
        var shot = Mortar("stone-building", [Target("de-a", "attacker-squad"), Target("de-b", "attacker-squad") with { LocationId = Upstairs }]) with
        {
            AreaLocations = [new OrdnanceAreaLocation(At) { TargetTerrain = "stone-building", Los = new FireLos(false, 0, true, false), LevelsAbove = 1 },
                new OrdnanceAreaLocation(Upstairs) { TargetTerrain = "stone-building", Los = new FireLos(false, 0, true, false) }],
        };
        var result = Resolve(shot, [2, 3], ift: [3, 4]);
        Resolved(result);
        Assert.Equal([("de-a", true), ("de-b", true)], result.AreaTargets!.Select(item => (item.UnitId, item.Hit)));
        Assert.Equal(["de-a"], result.Hit!.Effects.Select(item => item.UnitId));
        Assert.Equal(11, result.Hit.Arithmetic!.FinalDr);
        Assert.Contains(result.Hit.Arithmetic.Drm, item => item.Name == "tem:levels-above" && item.Value == 1);
        var upper = result.LevelHits!.Single();
        Assert.Equal(["de-b"], upper.Effects.Select(item => item.UnitId));
        Assert.Equal(10, upper.Arithmetic!.FinalDr);

        // C3.33 (ruling R60.24): a Location out of the firer's LOS is hit when the hardest target in LOS is, on the same Effects DR with its own TEM;
        // C3.4: one in LOS with a Hindrance has its own Case R.
        var blocked = Resolve(shot with
        {
            AreaLocations = [shot.AreaLocations![0], shot.AreaLocations[1] with { Los = new FireLos(true, 0, true, false) }],
        }, [2, 3], ift: [3, 4]);
        Assert.Equal([("de-a", true), ("de-b", true)], blocked.AreaTargets!.Select(item => (item.UnitId, item.Hit)));
        Assert.Equal(["de-b"], blocked.LevelHits!.Single().Effects.Select(item => item.UnitId));
        Assert.Equal(10, blocked.LevelHits!.Single().Arithmetic!.FinalDr);

        // A miss on the hardest target in LOS leaves the Location out of LOS unharmed.
        var missed = Resolve(shot with
        {
            AreaLocations = [shot.AreaLocations![0], shot.AreaLocations[1] with { Los = new FireLos(true, 0, true, false) }],
        }, [6, 6], ift: [3, 4]);
        Assert.Equal([("de-a", false), ("de-b", false)], missed.AreaTargets!.Select(item => (item.UnitId, item.Hit)));
        var hindered = Resolve(shot with
        {
            AreaLocations = [shot.AreaLocations![0], shot.AreaLocations[1] with { Los = new FireLos(false, 3, true, false) }],
        }, [2, 3], ift: [3, 4]);
        Assert.Equal([("de-a", true), ("de-b", false)], hindered.AreaTargets!.Select(item => (item.UnitId, item.Hit)));
    }

    [Fact]
    public void AreaAcquisitionCoversAnotherLocationOfTheHex()
    {
        // C6.521: the -1 Area Acquired counter on the hex applies at its other Location in LOS; the shot steps it to -2 there. A shot with no such fact,
        // as one recorded before the pass, has none.
        var shot = Mortar("open-ground", [Target("de-s", "attacker-squad")]) with
        {
            AreaAcquisition = -1
        };
        var result = Resolve(shot, [3, 4], ift: [3, 4]);
        Resolved(result);
        Assert.Contains(result.AreaTargets!.Single().Drm, item => item.Name == "case-n" && item.Value == -1);
        Assert.Equal((-2, At), (result.Gun!.Acquisition, result.Gun.AcquiredLocationId));
        Assert.DoesNotContain(Resolve(shot with
        {
            AreaAcquisition = null
        }, [3, 4], ift: [3, 4]).AreaTargets!.Single().Drm, item => item.Name == "case-n");
    }

    [Fact]
    public void ACriticalHitUphillReversesHeightAdvantageOnlyWithThePassesFact()
    {
        // C3.71: "any positive TEM ... which that target would normally be entitled to for TH ... purposes is reversed": the 45mm's Critical Hit
        // (an Original 2) on a squad that claimed Height Advantage takes -1 on the IFT DR. A shot without the fact resolves as before the pass.
        var shot = Russian(null) with
        {
            HeightAdvantage = true,
            ReversesHeightAdvantage = true,
            Rolls = new OrdnanceRolls(null, 1, null, null, null),
        };
        var result = Resolve(shot, [1, 1], ift: [3, 4]);
        Resolved(result);
        Assert.Contains(result.ToHit!.Drm, item => item.Name == "case-q:height-advantage");
        Assert.Contains(result.CriticalHit!.Arithmetic!.Drm, item => item.Name == "critical-hit-tem:height-advantage" && item.Value == -1);
        var older = Resolve(shot with
        {
            ReversesHeightAdvantage = null
        }, [1, 1], ift: [3, 4]);
        Assert.DoesNotContain(older.CriticalHit!.Arithmetic!.Drm, item => item.Name == "critical-hit-tem:height-advantage");
    }

    // Increment 2d: FT, MOL, and DC at an AFV (A22.34, A22.612, A23.5, C7.344, C7.346, C7.7, D.8A; rulings R60.15, R60.16). A Russian squad's FT, MOL, or
    // DC at the German PzKpfw IIIH (AF 6 front, 3 side, turret 4 both; worst 3, Aerial AF 2) or the OT SPW 251/1 (AF 1).
    private const string FiredFrom = "bd01:F5:0";

    private static FireFirer Squad(string id = "ru-1") => new(id, "defender-squad", FiredFrom, false, false, false, false, false)
    {
        KnownEnemyInLos = true,
        Captors = [],
    };

    private static FireFirer FtSquad() => Squad() with
    {
        Weapons = [new FireWeapon("ru-ft", "defender-ft", false, false, false)],
        UsesInherentFp = false,
        UsesSupportWeapon = true,
    };

    private static OrdnanceVehicleTarget Named(string facing = "rear", bool ce = true, string vehicle = "attacker-tank") =>
        new("de-afv", vehicle, facing, vehicle == "attacker-tank" ? facing : null, false, false, false, false, true)
        {
            CrewExposed = ce,
        };

    private static FireAttack AtAfv(FireFirer firer, OrdnanceVehicleTarget named, int range = 1, FireTarget[]? others = null) =>
        new FireAttack("PFPh", "phasing", true, FiredFrom, At, [firer], null, range, true, new FireLos(false, 0, true, false), 7, "open-ground", others ?? [], 3,
            new FireRolls(null, null, null, null))
        {
            Vehicles = [new FireVehicle("de-afv", named.DefinitionId, At, named.CrewExposed, false, false, false)],
            TargetVehicle = named,
        };

    private static FireAttack Charge(string mode, OrdnanceVehicleTarget named, FireTarget[]? others = null, string user = "defender-elite-squad", string dc = "defender-dc") =>
        new FireAttack(mode == FireDemolitionCharge.Placed ? "AFPh" : "PFPh", "phasing", null, FiredFrom, At, null, null, 1, true, null, 7, "open-ground", others ?? [], 3,
            new FireRolls(null, null, null, null))
        {
            DemolitionCharge = new FireDemolitionCharge("ru-dc", dc, mode, "ru-1", user, false, false, mode == FireDemolitionCharge.Placed ? false : null, null),
            Vehicles = [new FireVehicle("de-afv", named.DefinitionId, At, named.CrewExposed, false, false, false)],
            TargetVehicle = named,
        };

    /// <summary>Resolves an attack on the given IFT DR, Position DR, and check DR, every check DR 2, 2 unless given.</summary>
    private static FireResolution Named(FireAttack attack, int[] dice, int[]? position = null, int molCheck = 1, int[]? checks = null)
    {
        IReadOnlyList<int> check = checks ?? [2, 2];
        var rolls = new FireRolls(dice, null, null, null)
        {
            MolCheck = attack.Firers?.Any(item => item.Mol == true) == true ? molCheck : null,
            Position = position,
        };
        for (var step = 0; step < 10; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Reference.Fire);
            rolls = result.Reasons switch
            {
                ["asl.a1.fire.roll-missing:crewSurvival"] => rolls with { CrewSurvival = check },
                ["asl.a1.fire.roll-missing:taskCheck"] => rolls with { TaskCheck = check },
                ["asl.a1.fire.roll-missing:collateralCheck"] => rolls with { CollateralCheck = check },
                ["asl.a1.fire.roll-missing:crewCheck:de-afv"] => rolls with { CrewChecks = new Dictionary<string, IReadOnlyList<int>> { ["de-afv"] = check } },
                [var other, ..] when other.StartsWith("asl.a1.fire.roll-missing:checks:", StringComparison.Ordinal) => rolls with
                {
                    Checks = new Dictionary<string, IReadOnlyList<int>>(rolls.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [other["asl.a1.fire.roll-missing:checks:".Length..]] = check }
                },
                _ => null!,
            };
            if (rolls is null)
            {
                Assert.Equal(FireResolution.Resolved, result.Disposition);
                return result;
            }
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void AFlamethrowersTkNumberIsEightHalvedAtLongRangeFirstThenCeOtAndRear()
    {
        // C7.344, A22.34 (p. 177, 89): 8, +1 CE, +1 through the rear: 10; at Long Range halved first: 4 +1 = 5 from the front; at the OT halftrack
        // +2 OT and +1 CE: 11 from the side; never an AF.
        var rear = Named(AtAfv(FtSquad(), Named()), [6, 6]).VehicleKill!;
        Assert.Equal((8, 10, (int?)null), (rear.BasicTk!.Value, rear.FinalTk, rear.ArmorFactor));
        Assert.Equal(["crew-exposed", "case-a"], rear.Modifications.Select(item => item.Name));
        var far = Named(AtAfv(FtSquad(), Named("front"), range: 2), [6, 6]).VehicleKill!;
        Assert.Equal(5, far.FinalTk);
        Assert.Equal(["long-range", "crew-exposed"], far.Modifications.Select(item => item.Name));
        var halftrack = Named(AtAfv(FtSquad(), Named("side", vehicle: "attacker-halftrack")), [6, 6]).VehicleKill!;
        Assert.Equal(11, halftrack.FinalTk);
    }

    [Fact]
    public void TheFtMolColumnBurnsBelowEliminatesOnEqualAndLeavesTheCrewItsSpecificCollateralAttack()
    {
        // C7.7 (p. 190), C7.6: below the Final TK# of 10 burns, equal eliminates with Crew Survival, one above does nothing, an Original 12 is a Dud.
        var attack = AtAfv(FtSquad(), Named());
        var burns = Named(attack, [4, 5]);
        Assert.Equal(OrdnanceKill.Burn, burns.VehicleKill!.Result);
        Assert.Null(burns.SpecificCollateral);
        Assert.Empty(burns.Effects);
        var eliminated = Named(attack, [4, 6]);
        Assert.Equal(OrdnanceKill.Eliminated, eliminated.VehicleKill!.Result);
        Assert.NotNull(eliminated.VehicleKill.CrewSurvival);
        Assert.Null(eliminated.SpecificCollateral);
        Assert.Equal(OrdnanceKill.Dud, Named(attack, [6, 6]).VehicleKill!.Result);
        Assert.Null(Named(attack, [6, 6]).SpecificCollateral);

        // D.8A: the AFV stands on 11, and its CE crew takes the FT's 24 FP on that DR, +2 for CE alone: 13. Not CE, no attack.
        var stands = Named(attack, [5, 6]);
        Assert.Equal(OrdnanceKill.None, stands.VehicleKill!.Result);
        Assert.Equal((24, 13), (stands.SpecificCollateral!.Firepower, stands.SpecificCollateral.Effect.FinalDr));
        Assert.Equal(Reference.Fire.Result(13, Array.IndexOf(ScenarioA1FireReference.ColumnFp, 24)), stands.SpecificCollateral.Result);
        Assert.Null(stands.VehicleEffects);
        Assert.Null(Named(AtAfv(FtSquad(), Named(ce: false)), [5, 6]).SpecificCollateral);

        // At Long Range, 12 FP: a Final TK# of 5 from the front; a DR of 7 leaves it standing, and the crew takes 12 FP on 9.
        var far = Named(AtAfv(FtSquad(), Named("front"), range: 2), [3, 4]);
        Assert.Equal((12, 9), (far.SpecificCollateral!.Firepower, far.SpecificCollateral.Effect.FinalDr));
    }

    [Fact]
    public void AMolsOneDrIsItsToKillDrTheCrewsAttackWithFourMoreFpAndTheOthersAttack()
    {
        // A22.612 (p. 89, 90): 6, +1 CE (closed-topped), +1 rear: 8. The squad beside the AFV takes the squad's own 8 FP (PBF) without the MOL's
        // four; the crew 12 FP with +2 CE. The MOL Check takes no non-AFV drm.
        var squad = Target("de-s", "attacker-squad");
        var attack = AtAfv(Squad() with
        {
            Mol = true
        }, Named(), others: [squad]);
        var standing = Named(attack, [4, 5]);
        Assert.Equal((8, OrdnanceKill.None), (standing.VehicleKill!.FinalTk, standing.VehicleKill.Result));
        Assert.Equal(8m, standing.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(standing.MolCheck!.Drm, item => item.Name == "non-afv");
        Assert.Equal((12, 11), (standing.SpecificCollateral!.Firepower, standing.SpecificCollateral.Effect.FinalDr));
        Assert.Single(standing.Effects);
        Assert.Equal(OrdnanceKill.Burn, Named(attack, [3, 4]).VehicleKill!.Result);

        // C.8, D2.41: -2 against a moving AFV, the D2.41 +2 being a DC's alone; +2 OT replaces +1 CE.
        var moving = Named(attack with
        {
            TargetVehicle = Named() with
            {
                Moving = true
            }
        }, [4, 5]);
        Assert.Equal(6, moving.VehicleKill!.FinalTk);
        var open = Named(AtAfv(Squad() with
        {
            Mol = true
        }, Named("side", vehicle: "attacker-halftrack")), [4, 5]);
        Assert.Equal(["open-topped"], open.VehicleKill!.Modifications.Select(item => item.Name));

        // A failed Check: no To Kill DR; the crew takes a General Collateral Attack with the others, without the four FP.
        var failed = Named(attack, [4, 5], molCheck: 5);
        Assert.False(failed.MolCheck!.Passed);
        Assert.Null(failed.VehicleKill);
        Assert.Equal("de-afv", failed.VehicleEffects!.Single().VehicleId);
    }

    [Fact]
    public void ADcsPositionDrPicksTheAfAndItsDcColumnBurnsKillsImmobilizesAndShocks()
    {
        // C7.346 (p. 177): a Placed DC through the hull side, the crew BU: no DRM. Position 1, 2 (3): optimal, a turret hit (colored below white), the
        // Aerial AF 2: TK# 14. The DC column: 7 or less burns, below 14 eliminates, 14 and 15 Shock a turret.
        var placed = Charge(FireDemolitionCharge.Placed, Named("side", ce: false));
        var optimal = Named(placed, [3, 4], position: [1, 2]);
        Assert.Equal((FirePosition.Optimal, "turret"), (optimal.Position!.Result, optimal.Position.HitLocation));
        Assert.Equal((2, 14), (optimal.VehicleKill!.ArmorFactor!.Value, optimal.VehicleKill.FinalTk));
        Assert.Equal(OrdnanceKill.Burn, optimal.VehicleKill.Result);
        Assert.Equal(OrdnanceKill.Eliminated, Named(placed, [6, 5], position: [1, 2]).VehicleKill!.Result);

        // Position 4, 3 (7): positioned, a hull hit against the side AF 3: TK# 13; a 7 eliminates, an Original 12 is a Dud.
        var hull = Named(placed, [6, 1], position: [4, 3]);
        Assert.Equal((FirePosition.Positioned, "hull"), (hull.Position!.Result, hull.Position.HitLocation));
        Assert.Equal((3, 13, OrdnanceKill.Eliminated), (hull.VehicleKill!.ArmorFactor!.Value, hull.VehicleKill.FinalTk, hull.VehicleKill.Result));
        Assert.Equal(OrdnanceKill.Dud, Named(placed, [6, 6], position: [4, 3]).VehicleKill!.Result);

        // Through the hull front (+1 on the Position DR), AF 6: TK# 10. 10 and 11 immobilize a hull hit, and the crew takes its TC: a 12 Abandons it.
        var front = Charge(FireDemolitionCharge.Placed, Named("front", ce: false));
        var immobilized = Named(front, [5, 5], position: [4, 3]);
        Assert.Equal((10, OrdnanceKill.Immobilized), (immobilized.VehicleKill!.FinalTk, immobilized.VehicleKill.Result));
        Assert.True(immobilized.VehicleKill.CrewCheck!.Passed);
        var abandoned = Named(front, [5, 6], position: [4, 3], checks: [6, 6]);
        Assert.Equal((OrdnanceKill.Immobilized, true), (abandoned.VehicleKill!.Result, abandoned.VehicleKill.Abandoned));

        // Through the rear the TK# rises by one and the Position DR falls by one.
        var rear = Named(Charge(FireDemolitionCharge.Placed, Named("rear", ce: false)), [6, 1], position: [4, 4]);
        Assert.Contains(rear.Position!.Drm, item => item.Name == "hull-rear" && item.Value == -1);
        Assert.Equal(14, rear.VehicleKill!.FinalTk);

        // A German DC at the T-34's turret front (AF 11, inferior turret 8): TK# 8; 8 and 9 Shock a turret hit, 7 eliminates, 4 burns.
        var german = Charge(FireDemolitionCharge.Placed, Named("front", ce: false, vehicle: "defender-tank"), user: "attacker-squad", dc: "attacker-dc");
        var shocked = Named(german, [4, 5], position: [2, 3]);
        Assert.Equal(("turret", 8), (shocked.Position!.HitLocation, shocked.VehicleKill!.FinalTk));
        Assert.Equal((OrdnanceKill.Shock, true), (shocked.VehicleKill.Result, shocked.VehicleKill.Shocked));
        Assert.Equal(OrdnanceKill.Eliminated, Named(german, [3, 4], position: [2, 3]).VehicleKill!.Result);
        Assert.Equal(OrdnanceKill.Burn, Named(german, [2, 2], position: [2, 3]).VehicleKill!.Result);
    }

    [Fact]
    public void ADcPoorlyPositionedAttacksTheCrewOnTheSecondDrAndOneOfTwelveOrMoreTheOthersAsAreaFire()
    {
        // C7.346: a Thrown DC at a CE AFV through the hull front: +2 Thrown, +1 front, +1 CE. Position 2, 3 (5 + 4 = 9): poor; the crew alone takes
        // 30 FP on the IFT DR 3, 4, +2 CE: 9. The squad beside it is spared.
        var squad = Target("de-s", "attacker-squad");
        var thrown = Charge(FireDemolitionCharge.Thrown, Named("front"), [squad]);
        var poor = Named(thrown, [3, 4], position: [2, 3]);
        Assert.Equal((9, FirePosition.Poor), (poor.Position!.FinalDr, poor.Position.Result));
        Assert.Equal(["thrown-dc", "hull-front", "crew-exposed"], poor.Position.Drm.Select(item => item.Name));
        Assert.Null(poor.VehicleKill);
        Assert.Equal((30, 9), (poor.SpecificCollateral!.Firepower, poor.SpecificCollateral.Effect.FinalDr));
        Assert.False(poor.Effects.Single().Broken || poor.Effects.Single().Pinned || poor.Effects.Single().Eliminated);

        // Position 4, 4 (12): no effect on the AFV or its crew; the squad takes 15 FP as Area Fire, with no Thrown DRM on the IFT DR.
        var none = Named(thrown, [2, 2], position: [4, 4]);
        Assert.Equal(FirePosition.None, none.Position!.Result);
        Assert.Null(none.SpecificCollateral);
        Assert.Null(none.VehicleEffects);
        Assert.Equal(15m, none.Arithmetic!.TotalFirepower);
        Assert.DoesNotContain(none.Arithmetic.Drm, item => item.Name == "thrown-dc");

        // A23.4: an Original Position DR of 12 malfunctions the DC; nothing is attacked.
        var dud = Named(thrown, [2, 2], position: [6, 6]);
        Assert.True(dud.DemolitionChargeMalfunctioned);
        Assert.Null(dud.VehicleKill);
        Assert.False(dud.Effects.Single().Broken);

        // OT and Immobile are -2 each; the AFPh +1 for a Thrown DC but by an Opportunity Firer.
        var halftrack = Named(Charge(FireDemolitionCharge.Thrown, Named("side", vehicle: "attacker-halftrack") with
        {
            Immobile = true
        }) with
        {
            Phase = "AFPh"
        }, [3, 4], position: [3, 3]);
        Assert.Equal(["thrown-dc", "crew-exposed", "thrown-dc-afph", "immobile", "open-topped"], halftrack.Position!.Drm.Select(item => item.Name));
    }

    [Fact]
    public void OnlyAFtMolOrDcNamesAnEnemyArmoredVehicleInTheTargetLocation()
    {
        // R60.15: a squad's own fire names none; a vehicle not in the Location, a friendly one, or one beside concealed units is refused.
        Assert.Contains("asl.a1.fire.target-vehicle-outside", ScenarioA1FireCalculator.Resolve(AtAfv(Squad(), Named()), Reference.Fire).Reasons);
        Assert.Contains("asl.a1.fire.target-vehicle-outside", ScenarioA1FireCalculator.Resolve(AtAfv(FtSquad(), Named()) with
        {
            Vehicles = null
        }, Reference.Fire).Reasons);
        Assert.Contains("asl.a1.fire.target-vehicle-outside", ScenarioA1FireCalculator.Resolve(AtAfv(FtSquad(), Named(vehicle: "defender-tank")), Reference.Fire).Reasons);
        Assert.Contains("asl.a1.fire.target-vehicle-concealed", ScenarioA1FireCalculator.Resolve(AtAfv(FtSquad(), Named(),
            others: [Target("de-s", "attacker-squad") with { Concealed = true }]), Reference.Fire).Reasons);

        // The proposal states the Final TK# and its modifiers.
        Assert.Contains("Final TK# 10", ScenarioA1FireCalculator.NamedVehicleTerms(AtAfv(FtSquad(), Named()), Reference.Fire), StringComparison.Ordinal);
    }

    // Increment 2e: Random SW and Gun Destruction (A9.74, C8.42, C11.6, pp. 65, 178, 182; rulings R60.3, R60.17), an unattended Gun as a target (C3.41,
    // C11.4; ruling R60.18), and a DC at a manned Gun (C11.6). Two Russian squads ADJACENT fire 16 FP (PBF) at a German squad in the open.

    private static FireHeldWeapon Held(string id, string definition, string kind = "asl:mg", bool malfunctioned = false) =>
        new(id, kind, definition)
        {
            Malfunctioned = malfunctioned ? true : null
        };

    private static FireAttack Volley(bool fact, params FireHeldWeapon[] held) =>
        new FireAttack("PFPh", "phasing", true, FiredFrom, At, [Squad(), Squad("ru-2")], null, 1, true, new FireLos(false, 0, true, false), 7, "open-ground",
            [Target("de-s", "attacker-squad") with { HeldWeapons = held.Length > 0 ? held : null }], 3, new FireRolls(null, null, null, null))
        {
            RandomDestruction = fact ? true : null,
        };

    /// <summary>Resolves a fire attack on the given IFT DR, each weapon's Random SW/Gun Destruction dr as given, every other dr 3 and DR 3, 4.</summary>
    private static FireResolution Ift(FireAttack attack, int[] dice, int weaponDr = 3)
    {
        var rolls = new FireRolls(dice, null, null, null);
        for (var step = 0; step < 20; step++)
        {
            var result = ScenarioA1FireCalculator.Resolve(attack with
            {
                Rolls = rolls
            }, Reference.Fire);
            if (result.Reasons is not [{ } reason] || !reason.StartsWith("asl.a1.fire.roll-missing:", StringComparison.Ordinal))
            {
                Assert.True(result.Disposition == FireResolution.Resolved, string.Join("; ", result.Reasons));
                return result;
            }

            var key = reason["asl.a1.fire.roll-missing:".Length..];
            var unit = key[(key.IndexOf(':', StringComparison.Ordinal) + 1)..];
            rolls = key.Split(':')[0] switch
            {
                "randomSelection" => rolls with { RandomSelection = unit.Split(',').ToDictionary(id => id, _ => 3) },
                "weaponDestruction" => rolls with { WeaponDestruction = new Dictionary<string, int>(rolls.WeaponDestruction ?? new Dictionary<string, int>()) { [unit] = weaponDr } },
                "checks" => rolls with { Checks = new Dictionary<string, IReadOnlyList<int>>(rolls.Checks ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = [3, 4] } },
                "leaderLoss" => rolls with { LeaderLoss = new Dictionary<string, IReadOnlyList<int>>(rolls.LeaderLoss ?? new Dictionary<string, IReadOnlyList<int>>()) { [unit] = [3, 4] } },
                _ => rolls with { WoundSeverity = new Dictionary<string, int>(rolls.WoundSeverity ?? new Dictionary<string, int>()) { [unit] = 3 } },
            };
        }

        throw new InvalidOperationException("The attack asked for too many rolls.");
    }

    [Fact]
    public void TheGunDestructionTableReadsEachColumn()
    {
        // C11.6 (p. 182): ordnance: a KIA or a CH eliminates, a K malfunctions (eliminates when AP was fired, note 5); small arms: a KIA is A9.74's,
        // a K and a CH NA; a DC: a KIA eliminates, a K malfunctions.
        Assert.Equal(("destroyed", "malfunctioned", "destroyed", "destroyed"), (ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.Ordnance, "1KIA", false),
            ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.Ordnance, "K/2", false), ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.Ordnance, "K/2", false, true),
            ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.Ordnance, "2MC", true)));
        Assert.Equal(("random-destruction", (string?)null), (ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.SmallArms, "2KIA", false),
            ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.SmallArms, "K/3", true)));
        Assert.Equal(("destroyed", "malfunctioned", (string?)null), (ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.DemolitionCharge, "3KIA", false),
            ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.DemolitionCharge, "K/4", false), ScenarioA1GunDestruction.Fate(ScenarioA1GunDestruction.DemolitionCharge, "1MC", false)));
    }

    [Fact]
    public void AKiaMakesTheVictimsWeaponsRollOnTheSameColumn()
    {
        // A9.74 (p. 65): 16 FP, IFT DR 3: 1KIA eliminates the squad; its LMG's dr is read on the 16 FP column with no DRM: 3 is a 1KIA (destroyed),
        // 4 a K/3 (malfunctioned), 5 a 3MC (unharmed).
        var lmg = Held("de-lmg", "attacker-lmg");
        var destroyed = Ift(Volley(true, lmg), [1, 2], weaponDr: 3);
        Assert.True(destroyed.Effects.Single().Eliminated);
        var fate = destroyed.WeaponDestruction!.Single();
        Assert.Equal(("de-lmg", "de-s", 3, "1KIA", "destroyed", "A9.74"), (fate.EquipmentId, fate.HolderId, fate.Dr!.Value, fate.Result, fate.Fate, fate.Rule));
        Assert.Equal(("K/3", "malfunctioned"), (Ift(Volley(true, lmg), [1, 2], weaponDr: 4).WeaponDestruction!.Single().Result,
            Ift(Volley(true, lmg), [1, 2], weaponDr: 4).WeaponDestruction!.Single().Fate));
        Assert.Equal("none", Ift(Volley(true, lmg), [1, 2], weaponDr: 5).WeaponDestruction!.Single().Fate);

        // A K on a malfunctioned MG, or on a SW with an X# (the PSK), eliminates it.
        Assert.Equal("destroyed", Ift(Volley(true, Held("de-lmg", "attacker-lmg", malfunctioned: true)), [1, 2], weaponDr: 4).WeaponDestruction!.Single().Fate);
        Assert.Equal("destroyed", Ift(Volley(true, Held("de-psk", "attacker-psk", "asl:latw")), [1, 2], weaponDr: 4).WeaponDestruction!.Single().Fate);
    }

    [Fact]
    public void NoDrOnAKOrCasualtyReductionNorWithoutThePassesFact()
    {
        // IFT DR 4: K/3 Casualty Reduces the squad, which is no KIA; and a record without the pass's fact destroys nothing on its KIA.
        var lmg = Held("de-lmg", "attacker-lmg");
        var reduced = Ift(Volley(true, lmg), [1, 3]);
        Assert.Equal("K/3", reduced.Arithmetic!.Result);
        Assert.Null(reduced.WeaponDestruction);
        var older = Ift(Volley(false, lmg), [1, 2]);
        Assert.True(older.Effects.Single().Eliminated);
        Assert.Null(older.WeaponDestruction);
    }

    [Fact]
    public void ACanisterKiaDestroysASwAsAnHeKiaDoes()
    {
        // C8.42 (p. 178): "A Canister KIA result has the same chance of destroying a SW in the hex as an HE KIA result". The 37LL's 20 FP of Canister
        // at the adjacent hex, as Area Fire with no doubling (C8.41): 10 FP, the 8 FP column; IFT DR 2: 1KIA; the LMG's dr 2: 1KIA, destroyed.
        var canister = new FireAttack("PFPh", "phasing", null, FiredFrom, At, null, null, 1, true, new FireLos(false, 0, true, false), 7, "open-ground",
            [Target("de-s", "attacker-squad") with { HeldWeapons = [Held("de-lmg", "attacker-lmg")] }], 3, new FireRolls(null, null, null, null))
        {
            Canister = new FireCanister("ru-gun", 20) { AdjacentHexes = 1 },
            RandomDestruction = true,
        };
        var result = Ift(canister, [1, 1], weaponDr: 2);
        Assert.True(result.Effects.Single().Eliminated);
        Assert.Equal(("1KIA", "destroyed"), (result.WeaponDestruction!.Single().Result, result.WeaponDestruction!.Single().Fate));
    }

    [Fact]
    public void AnUnattendedGunIsATargetItsKiaDestroysItAndItsKMalfunctionsIt()
    {
        // C3.41, C11.4 (ruling R60.18): the leIG 18's HE at the Russian 45mm no unit mans: Case P for its small size, no Emplacement or gunshield; the IFT
        // DR on its 12 FP column (C.6) decides: 2 is a 1KIA, destroyed; 3 a K/3, malfunctioned. Without a Gun the empty Location stays refused.
        var lone = German(null, targets: []) with
        {
            Hit = German(null, targets: []).Hit! with
            {
                GunTarget = new FireGunTarget("ru-gun", "defender-at-gun", null, false, false)
            }
        };
        var destroyed = Resolve(lone, [2, 3], ift: [1, 1]);
        Resolved(destroyed);
        Assert.Contains(destroyed.ToHit!.Drm, item => item.Name.StartsWith("case-p:", StringComparison.Ordinal));
        Assert.DoesNotContain(destroyed.ToHit.Drm, item => item.Name == "case-q:emplacement");
        Assert.Equal(("1KIA", "destroyed"), (destroyed.Hit!.Arithmetic!.Result, destroyed.GunTargetFate));
        var malfunctioned = Resolve(lone, [2, 3], ift: [1, 2]);
        Assert.Equal(("K/3", "malfunctioned"), (malfunctioned.Hit!.Arithmetic!.Result, malfunctioned.GunTargetFate));
        Assert.Contains("asl.a1.ordnance.target-outside", Resolve(German(null, targets: []), [2, 3]).Reasons);
    }

    [Fact]
    public void ADcDestroysTheGunItsTargetsManOnAKiaAndMalfunctionsItOnAK()
    {
        // C11.6's DC column: a Placed DC at the Russian 45mm's crew. 30 FP: a Final DR of 3 is a 3KIA, the crew and the Gun eliminated, no A9.74 dr
        // for the Gun; 6 a K/4, the Gun malfunctioned; 7 a 4MC, the Gun untouched.
        var charge = new FireAttack("AFPh", "phasing", null, FiredFrom, At, null, null, 1, true, null, 7, "open-ground",
            [Target("ru-crew", "defender-crew") with { HeldWeapons = [Held("ru-gun", "defender-at-gun", "asl:gun")] }], 3, new FireRolls(null, null, null, null))
        {
            DemolitionCharge = new FireDemolitionCharge("de-dc", "attacker-dc", FireDemolitionCharge.Placed, "de-1", "attacker-squad", false, false, false, null),
            RandomDestruction = true,
        };
        var killed = Ift(charge, [1, 2]);
        Assert.Equal((3, "3KIA"), (killed.Arithmetic!.FinalDr, killed.Arithmetic.Result));
        Assert.True(killed.Effects.Single().Eliminated);
        Assert.Equal(("ru-gun", (int?)null, "destroyed", "C11.6"), (killed.WeaponDestruction!.Single().EquipmentId, killed.WeaponDestruction!.Single().Dr,
            killed.WeaponDestruction!.Single().Fate, killed.WeaponDestruction!.Single().Rule));
        var struck = Ift(charge, [2, 4]);
        Assert.Equal(("K/4", "malfunctioned"), (struck.Arithmetic!.Result, struck.WeaponDestruction!.Single().Fate));
        Assert.Null(Ift(charge, [3, 4]).WeaponDestruction);
    }

    // Increment 2f: a MG's To Kill attack on an AFV (A9.6, A9.61, A9.611, D3.54, pp. 64, 65, 201; ruling R60.19), and CC Reaction Fire's Fire Lane and
    // CC counter (A9.223, D7.21; ruling R60.20).

    /// <summary>
    /// A Russian squad's HMG (6 FP, Normal Range 12, ROF 3, B12) at the German SPW 251/1 (AF 1/1, OT, small, CS# 5) in the DFPh: the facing named, a hull
    /// hit (the colored dr above the white) unless told, its crew CE.
    /// </summary>
    private static OrdnanceShot HmgAtHalftrack(int range, int[] toKill, string facing = "front", string phase = "DFPh", int[]? toHit = null, int[]? collateral = null,
        string target = "attacker-halftrack", string? turretFacing = null)
    {
        return new OrdnanceShot(phase, phase == "DFPh" ? "non-phasing" : "phasing", "russian", new OrdnanceGun("ru-hmg", "defender-hmg", false, 0, false, false),
            new OrdnanceCrew("ru-s", "defender-squad", false, false, false, false, false), At, range, 0, false, true, 0,
            new FireAttack(phase, phase == "DFPh" ? "non-phasing" : "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls(toHit ?? [4, 2], null, null, null, null) { ToKill = toKill, CollateralCheck = collateral, CrewSurvival = [6, 6] })
        {
            VehicleTarget = new OrdnanceVehicleTarget("de-ht", target, facing, turretFacing, false, false, false, false, false) { CrewExposed = true },
            Ammunition = ScenarioA1MachineGunKill.Ammunition,
            ScenarioYear = 1942,
            SpecificCollateral = true,
        };
    }

    [Fact]
    public void AMgsToKillNumberIsFourWithCaseDsRowOfTwentyFiveMmLessTheAf()
    {
        // The D1.22 example: "Base 4 + 1 Case D - 1 AF" at range 3 to 6; Case D is +2 at 0 to 1, +1 at 2, none at 7 to 12; the black Vehicle row.
        var three = ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [3, 4], collateral: [2, 2]), Reference);
        Resolved(three);
        Assert.Equal(("black", 10, 4, 1, 4), (three.ToHit!.Color, three.ToHit.BasicToHit, three.Kill!.BasicTk!.Value, three.Kill.ArmorFactor!.Value, three.Kill.FinalTk));
        Assert.Empty(three.ToHit.Modifications);
        Assert.Equal(["case-d"], three.Kill.Modifications.Select(item => item.Name));
        Assert.Equal(5, ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(1, [3, 4], collateral: [2, 2]), Reference).Kill!.FinalTk);
        Assert.Equal(4, ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(2, [3, 4], collateral: [2, 2]), Reference).Kill!.FinalTk);
        Assert.Equal(3, ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(8, [3, 4], collateral: [2, 2]), Reference).Kill!.FinalTk);

        // C7.21, Case A: +1 through the rear.
        var rear = ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [3, 4], "rear", collateral: [2, 2]), Reference);
        Assert.Equal(5, rear.Kill!.FinalTk);
        Assert.Equal(["case-a", "case-d"], rear.Kill.Modifications.Select(item => item.Name));
    }

    [Fact]
    public void AMgsToKillDrBurnsEliminatesStunsOrDoesNothingAndATwelveIsADud()
    {
        // A9.61: at most half the Final TK# of 4 burns, below it eliminates, equal Stuns (D5.34), above it nothing; C7.35: an Original 12 is a Dud.
        OrdnanceKill Kill(int[] dice) => ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, dice, collateral: [2, 2]), Reference).Kill!;
        Assert.Equal(OrdnanceKill.Burn, Kill([1, 1]).Result);
        var eliminated = Kill([1, 2]);
        Assert.Equal((OrdnanceKill.Eliminated, 12, 5, false), (eliminated.Result, eliminated.CrewSurvival!.FinalDr, eliminated.CrewSurvival.CrewSurvival, eliminated.CrewSurvival.Survived));
        Assert.Equal(OrdnanceKill.Stun, Kill([2, 2]).Result);
        Assert.Equal(OrdnanceKill.None, Kill([2, 3]).Result);
        Assert.Equal(OrdnanceKill.Dud, Kill([6, 6]).Result);

        // D5.34: the Stun leaves the AFV Stunned, BU, and stopped.
        Assert.Equal([(UnitCondition.Stunned, true), (UnitCondition.ButtonedUp, true), (UnitCondition.Motion, false)],
            ScenarioA1OrdnanceEventRules.KillConditions(Kill([2, 2]), false));
    }

    [Fact]
    public void ACeCrewTakesTheMgsFpOnTheSameDrUnlessTheAttackStuns()
    {
        // A9.611 and the D1.22 example: the HMG's 6 IFT FP on the Original TK DR, +2 for CE; a 5 becomes a 7 on the 6 FP column. None after the Stun,
        // which buttons the crew up at once (D5.34).
        var missed = ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [2, 3], collateral: [2, 2]), Reference);
        Resolved(missed);
        Assert.Equal((6, 7), (missed.SpecificCollateral!.Firepower, missed.SpecificCollateral.Effect.FinalDr));
        Assert.Equal(Reference.Fire.Result(7, Array.IndexOf(ScenarioA1FireReference.ColumnFp, 6)), missed.SpecificCollateral.Result);
        Assert.Null(ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [2, 2], collateral: [2, 2]), Reference).SpecificCollateral);
    }

    [Fact]
    public void AMgScoresNoCriticalHitAndATurretHitMeetsTheTurretsAf()
    {
        // A9.61, C3.8: an Original 2 is a hit, never a Critical Hit, with no subsequent dr. C3.9: the colored dr below the white strikes the T-34's
        // turret, its front AF of 11 inferior (8); the HMG at range 1, front: 4 + 2 - 8.
        var two = ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [3, 4], toHit: [1, 1], collateral: [2, 2]), Reference);
        Resolved(two);
        Assert.Equal((true, false, (int?)null), (two.ToHit!.Hit, two.ToHit.CriticalHit, two.ToHit.SubsequentDr));
        var turret = ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(1, [3, 4], phase: "PFPh", toHit: [1, 3], collateral: [2, 2], target: "defender-tank", turretFacing: "front") with
        {
            FiringNationality = "german",
            Gun = new OrdnanceGun("de-hmg", "attacker-hmg", false, 0, false, false),
            Crew = new OrdnanceCrew("de-s", "attacker-squad", false, false, false, false, false),
        }, Reference);
        Resolved(turret);
        Assert.Equal(("turret", 8, -2), (turret.Kill!.HitLocation, turret.Kill.ArmorFactor!.Value, turret.Kill.FinalTk));
    }

    [Fact]
    public void AVehiclesMaMgMakesTheAttackAndItsBmgMayNot()
    {
        // D3.54: the SPW 251/1's MA AAMG (3 FP) at the T-34 M41's side, CE; the Bren Carrier's MA, a plain MG, fires its nationality's LMG FP (2).
        var halftrack = Reference.Weapon("attacker-halftrack")!;
        Assert.Equal((ScenarioA1MachineGunKill.GunType, 3, 8, 1, 12), (halftrack.GunType, halftrack.Firepower!.Value, halftrack.RangeMaximum!.Value, halftrack.RateOfFire!.Value, halftrack.Breakdown));
        Assert.Equal((2, 8), (Reference.Weapon("campaign-british-bren-carrier")!.Firepower!.Value, Reference.Weapon("campaign-british-bren-carrier")!.RangeMaximum!.Value));
        var shot = new OrdnanceShot("PFPh", "phasing", "german", new OrdnanceGun("de-ht", "attacker-halftrack", false, 0, false, false),
            new OrdnanceCrew("de-ht", "attacker-halftrack", false, false, false, false, false), At, 2, 0, false, true, 0,
            new FireAttack("PFPh", "phasing", null, null, At, [], null, null, true, new FireLos(false, 0, true, false), 7, "open-ground", [], 2, null),
            new OrdnanceRolls([4, 2], null, null, null, null) { ToKill = [3, 4], CollateralCheck = [2, 2] })
        {
            Vehicle = new OrdnanceVehicleFirer(false, false, false, false, false),
            VehicleTarget = new OrdnanceVehicleTarget("ru-t", "defender-tank", "side", "side", false, false, false, false, false) { CrewExposed = true },
            Ammunition = ScenarioA1MachineGunKill.Ammunition,
            ScenarioYear = 1942,
            SpecificCollateral = true,
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Resolved(result);
        Assert.Equal((OrdnanceKill.None, -1, 3, 9), (result.Kill!.Result, result.Kill.FinalTk, result.SpecificCollateral!.Firepower, result.SpecificCollateral.Effect.FinalDr));

        // A BU AFV's AAMG does not fire (D1.83); a tank's BMG or CMG makes no To Kill attack (D3.54), and a tank's MA is a Gun.
        Assert.Contains("asl.a1.ordnance.mg-to-kill-outside", ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            Vehicle = new OrdnanceVehicleFirer(true, false, false, false, false)
        }, Reference).Reasons);
        Assert.NotNull(ScenarioA1MachineGunKill.VehicleWeaponRefusal("de-tank", "bmg"));
        Assert.Null(ScenarioA1MachineGunKill.VehicleWeaponRefusal("de-tank", "ma"));
        Assert.NotEqual(ScenarioA1MachineGunKill.GunType, Reference.Weapon("attacker-tank")!.GunType);
    }

    [Fact]
    public void AMgsToKillAttackIsRefusedInTheAfphAtAnUnarmoredVehicleAndBeyondNormalRange()
    {
        Assert.Contains("asl.a1.ordnance.mg-to-kill-outside", ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [3, 4], phase: "AFPh"), Reference).Reasons);
        Assert.Contains("asl.a1.ordnance.mg-unarmored-target", ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(3, [3, 4], target: "attacker-truck"), Reference).Reasons);
        Assert.Contains("asl.a1.ordnance.out-of-range", ScenarioA1OrdnanceCalculator.Resolve(HmgAtHalftrack(13, [3, 4]), Reference).Reasons);
        var pinned = HmgAtHalftrack(3, [3, 4]);
        Assert.Contains("asl.a1.ordnance.mg-to-kill-outside", ScenarioA1OrdnanceCalculator.Resolve(pinned with
        {
            Crew = pinned.Crew! with
            {
                Pinned = true
            }
        }, Reference).Reasons);
    }

    [Fact]
    public void CcReactionFireCancelsItsMgsFireLaneAndMarksItsWeapons()
    {
        // A9.223 (the narrow reading, R60.20): the lane goes when its manning Infantry makes CC Reaction Fire, not when another unit does. D7.21: the SW
        // and Guns of an attacker the vehicle survived carry the CC counter.
        Assert.True(ScenarioA1FireEligibility.FireLaneCancelledByCcReaction(true, true));
        Assert.False(ScenarioA1FireEligibility.FireLaneCancelledByCcReaction(true, false));
        Assert.False(ScenarioA1FireEligibility.FireLaneCancelledByCcReaction(false, true));
        Assert.Equal([(UnitCondition.CcReaction, true)], ScenarioA1VehicleCloseCombatRules.ReactionWeaponConditions(false));
        Assert.Empty(ScenarioA1VehicleCloseCombatRules.ReactionWeaponConditions(true));
        Assert.NotNull(ScenarioA1OrdnanceEligibility.CcCounterRefusal("ru-hmg", true, true));
        Assert.Null(ScenarioA1OrdnanceEligibility.CcCounterRefusal("ru-hmg", true, false));
    }

    // The fixes of the pass's reviews (the boundary review, the two referees, the table player).

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ARecordNamesNoWeaponItsTargetsHoldAndWritesNoNullMembers()
    {
        // The boundary review: both sides read a record, so a concealed unit's SW are never written; the verifier reads them from the state.
        var target = Target("de-s", "attacker-squad") with { HeldWeapons = [Held("de-lmg", "attacker-lmg")] };
        Assert.DoesNotContain("heldWeapons", JsonSerializer.Serialize(target, Web), StringComparison.Ordinal);
        Assert.DoesNotContain("malfunctioned", JsonSerializer.Serialize(Held("de-lmg", "attacker-lmg"), Web), StringComparison.Ordinal);
        Assert.DoesNotContain("\"dr\"", JsonSerializer.Serialize(new FireWeaponDestruction("ru-gun", "ru-crew", "3KIA", "destroyed", "C11.6"), Web), StringComparison.Ordinal);
        Assert.Equal("{\"locationId\":\"bd01:G5:1\"}", JsonSerializer.Serialize(new OrdnanceAreaLocation(Upstairs), Web));
    }

    [Fact]
    public void TheBoundaryPredicatesAreRules()
    {
        // C11.3 (R60.26): a Gun manned by a non-crew unit as setup ends loses its Emplacement; a crew's manning keeps it.
        Assert.Equal((true, false, false), (ScenarioA1OrdnanceProjection.LosesEmplacement("asl:gun", "asl:squad"),
            ScenarioA1OrdnanceProjection.LosesEmplacement("asl:gun", "asl:crew"), ScenarioA1OrdnanceProjection.LosesEmplacement("asl:mg", "asl:squad")));

        // C6.521 (R60.13): with no Acquisition of the Location itself, a mortar's Acquisition of another Location of its hex.
        Assert.Equal(((int?)-1, (int?)null, (int?)null), (ScenarioA1OrdnanceEligibility.AreaAcquisition(true, 0, [-1]),
            ScenarioA1OrdnanceEligibility.AreaAcquisition(true, -1, [-2]), ScenarioA1OrdnanceEligibility.AreaAcquisition(false, 0, [-1])));

        // C3.41, C11.4 (R60.18): the unattended Gun is one no one holds.
        var lone = ScenarioA1OrdnanceEligibility.UnattendedGun([("b-gun", "asl:gun", "defender-at-gun", false), ("a-gun", "asl:gun", "defender-at-gun", true), ("mg", "asl:mg", "defender-lmg", false)]);
        Assert.Equal(("b-gun", (string?)null, false, false), (lone!.GunId, lone.CrewUnitId, lone.Emplaced!.Value, lone.Gunshield!.Value));

        // A23.3, A23.5 (R60.16): no DC Placement where an unarmored or a friendly vehicle is.
        Assert.Equal(("asl.a1.fire.dc-placement-vehicle", "asl.a1.fire.dc-placement-vehicle", (string?)null), (ScenarioA1FireEligibility.DcPlacementVehicleRefusal([(false, false)]),
            ScenarioA1FireEligibility.DcPlacementVehicleRefusal([(true, true)]), ScenarioA1FireEligibility.DcPlacementVehicleRefusal([(true, false)])));

        // A9.74 (R60.17): a SW possessed or a Gun manned; never a towed Gun, nor anything in an OVR.
        Assert.Equal((true, true, false, false), (ScenarioA1GunDestruction.Checks("possessed", false), ScenarioA1GunDestruction.Checks("manned", false),
            ScenarioA1GunDestruction.Checks("towed", false), ScenarioA1GunDestruction.Checks(null, true)));
    }

    [Fact]
    public void TheAreaAcquisitionIsHeldToTheStateForARecordThatCarriesIt()
    {
        // The boundary review: the recorded Area Acquisition is a state read; a record that carries one is compared with the state's, and an older
        // record with none keeps none.
        var state = Mortar("open-ground", [Target("de-s", "attacker-squad")]) with
        {
            AreaAcquisition = -1
        };
        int? Merged(int? recorded) => ScenarioA1OrdnanceEligibility.WithRecomputedStateFacts(state, state with { AreaAcquisition = recorded }, false, 0, () => 0, _ => null).AreaAcquisition;
        Assert.Equal(((int?)-1, (int?)null), (Merged(-2), Merged(null)));
    }

    [Fact]
    public void AGunsGunshieldProtectsItsCrewAloneNotAVehicleBesideIt()
    {
        // C11.5 (p. 182): "the Good Order manning crew (only)": the leIG 18's crew takes the gunshield's +1 against the mortar; the truck beside it,
        // hit on the same DR, takes none of it on the Vehicle line.
        var shot = AtGun(Mortar("open-ground", [], Vehicle("de-truck", "attacker-truck")), "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew");
        shot = shot with
        {
            Hit = shot.Hit! with
            {
                GunTarget = new FireGunTarget("de-gun", "attacker-inf-gun", "de-crew", false, true),
                RandomDestruction = true,
            }
        };
        var result = Resolve(shot, [1, 2], ift: [3, 4]);
        Resolved(result);
        Assert.Contains(result.Hit!.Arithmetic!.Drm, item => item.Name == "gunshield:de-gun");
        Assert.DoesNotContain(result.Hit.VehicleEffects!.Single().Drm, item => item.Name.StartsWith("gunshield:", StringComparison.Ordinal));
    }

    [Fact]
    public void ACriticalHitOnTheTruckBesideAGunLeavesTheGunToItsCrewsHit()
    {
        // C3.74 (the first referee): Random Selection puts the Critical Hit on the truck; the Gun's fate is its crew's normal hit, 12 on the IFT: none.
        // Put on the crew, it destroys the Gun (C11.4).
        var shot = AtGun(AtLocation([], Vehicle("de-truck", "attacker-truck")), "de-gun", "attacker-inf-gun", "de-crew", "attacker-crew") with
        {
            Rolls = new OrdnanceRolls(null, 1, null, null, null)
        };
        var onTruck = Resolve(shot, [1, 1], ift: [6, 6], selection: new Dictionary<string, int> { ["de-crew"] = 2, ["de-truck"] = 5 });
        Resolved(onTruck);
        Assert.Equal(("de-truck", (string?)null), (onTruck.CriticalTarget, onTruck.GunTargetFate));
        var onCrew = Resolve(shot, [1, 1], ift: [6, 6], selection: new Dictionary<string, int> { ["de-crew"] = 5, ["de-truck"] = 2 });
        Assert.Equal(("de-crew", "destroyed"), (onCrew.CriticalTarget, onCrew.GunTargetFate));
    }

    [Fact]
    public void ACriticalHitReversesAnUnarmoredVehiclesOwnTem()
    {
        // C3.71 (p. 170): the truck in the woods, picked by the Critical Hit's Random Selection, takes -1 for the woods on its Vehicle line.
        var shot = Russian(null, "woods", [Target("de-s", "attacker-squad")]) with
        {
            Rolls = new OrdnanceRolls(null, 1, null, null, null)
        };
        shot = shot with
        {
            Hit = shot.Hit! with
            {
                Vehicles = [Vehicle("de-truck", "attacker-truck")]
            }
        };
        var result = Resolve(shot, [1, 1], ift: [3, 4], selection: new Dictionary<string, int> { ["de-s"] = 1, ["de-truck"] = 6 });
        Resolved(result);
        Assert.Contains(result.CriticalHit!.VehicleEffects!.Single().Drm, item => item.Name == "critical-hit-tem:woods" && item.Value == -1);
    }

    [Fact]
    public void AnOriginalTwoOnAPsksOwnTableIsACriticalHit()
    {
        // C3.7 (p. 170): "on an Original TH DR of 2 ... on a LATW TH Table": the PSK at two hexes (9) at a squad in a stone building (+3): Final DR 5,
        // not below half of 9, and still a Critical Hit with no subsequent dr.
        var result = Resolve(Latw("de-psk", "attacker-psk", 2, "stone-building", Target("ru-s", "defender-squad")), [1, 1], ift: [3, 4]);
        Resolved(result);
        Assert.Equal((true, (int?)null), (result.ToHit!.CriticalHit, result.ToHit.SubsequentDr));
    }

    [Fact]
    public void ANonTwentyMmAtrMakesNoCollateralAttack()
    {
        // C8.31 note 1, C13.24: the Russian 14.5mm ATR has no HE Equivalency, so its hit on the CE halftrack that leaves it standing attacks no crew.
        var shot = HmgAtHalftrack(2, [6, 5], collateral: [2, 2]) with
        {
            Gun = new OrdnanceGun("ru-atr", "defender-atr", false, 0, false, false),
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Resolved(result);
        Assert.Equal(OrdnanceKill.None, result.Kill!.Result);
        Assert.Null(result.SpecificCollateral);
    }

    [Fact]
    public void AMgBeyondTwelveHexesTakesTheSmallCaliberModifications()
    {
        // C4.2 (p. 171): "Any weapon (including MG) <= 57mm" takes -1 beyond 12 hexes, and another for <= 40mm: the German HMG (Normal Range 16) at 14.
        var shot = HmgAtHalftrack(14, [3, 4], phase: "PFPh", collateral: [2, 2], target: "defender-tank", turretFacing: "front") with
        {
            FiringNationality = "german",
            Gun = new OrdnanceGun("de-hmg", "attacker-hmg", false, 0, false, false),
            Crew = new OrdnanceCrew("de-s", "attacker-squad", false, false, false, false, false),
        };
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot, Reference);
        Resolved(result);
        Assert.Equal(["caliber-57mm", "caliber-40mm"], result.ToHit!.Modifications.Select(item => item.Name));
        Assert.Equal(result.ToHit.BasicToHit - 2, result.ToHit.ModifiedToHit);
    }

    [Fact]
    public void ASwMgInItsTargetsHexTakesNoCaseE()
    {
        // The chart marks Case E "S", not with a SW (the second referee): the HMG at the halftrack in its own hex takes neither Case E nor Case L.
        // D3.2: in its own hex the colored dr gives the Target Facing, so the shot names none.
        var shot = HmgAtHalftrack(0, [3, 4], collateral: [2, 2]);
        var result = ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            SameHex = true,
            VehicleTarget = shot.VehicleTarget! with
            {
                HullFacing = null
            }
        }, Reference);
        Resolved(result);
        Assert.DoesNotContain(result.ToHit!.Drm, item => item.Name is "case-e" or "case-l");
    }

    [Fact]
    public void AMgsToKillAttackAtAConcealedAfvIsRefused()
    {
        // R60.27: the IFT would halve the MG's FP at a concealed target (A7.23), which A9.61 does not allow.
        var shot = HmgAtHalftrack(3, [3, 4]);
        Assert.Contains("asl.a1.ordnance.mg-to-kill-concealed", ScenarioA1OrdnanceCalculator.Resolve(shot with
        {
            VehicleTarget = shot.VehicleTarget! with
            {
                Concealed = true
            }
        }, Reference).Reasons);
        Assert.Equal(("no To Kill attack in the AFPh (A9.61)", "its possessor is pinned (A9.61)", (string?)null),
            (ScenarioA1MachineGunKill.Bar("afph", false, false, false, false, false), ScenarioA1MachineGunKill.Bar("PFPh", false, true, false, false, false),
                ScenarioA1MachineGunKill.Bar("dfph", false, false, false, false, false)));
    }
}
