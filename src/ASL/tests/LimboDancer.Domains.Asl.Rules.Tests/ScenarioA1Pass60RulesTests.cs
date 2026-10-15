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

        // In the open the PF's HEAT is refused at Infantry.
        Assert.Contains(HeatTargetOutside, Resolve(Latw("de-s:pf", OrdnanceTargetTypes.Panzerfaust, 1, "open-ground", Target("ru-s", "defender-squad")), [2, 2], pfCheck: 2).Reasons);
    }

    [Fact]
    public void APskAtInfantryInTheOpenIsRefusedAndInABuildingHitsEveryUnit()
    {
        // C13.48, C8.31: a PSK's IFT effect is 12, and HEAT goes only at Infantry behind a wall or in a building or rubble.
        Assert.Equal([HeatTargetOutside], Resolve(Latw("de-psk", "attacker-psk", 2, "open-ground", Target("ru-s", "defender-squad")), [2, 3]).Reasons);
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
        var result = Resolve(shot, DiceOf(need), ift: [3, 4]);
        Resolved(result);
        Assert.Equal([("de-s", true), ("de-c", false)], result.TargetHits!.Select(item => (item.UnitId, item.Hit)));
        Assert.Contains(result.TargetHits!.Single(item => item.UnitId == "de-c").Drm, item => item.Name == "case-k");
        Assert.Equal(["de-s"], new[] { result.Hit, result.CriticalHit }.OfType<FireResolution>().SelectMany(attack => attack.Effects).Select(effect => effect.UnitId));
    }
}
