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
}
