using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>A hex of a Canister aim, as steps from the firer's hex across the two hexsides of its Covered Arc: so many to the left hex, then so many to the right.</summary>
public readonly record struct CanisterOffset(int Left, int Right);

/// <summary>
/// One of the five vertices a Canister shot may be fired at within a Covered Arc (C8.4 and its EX, p. 178; pass 55, ruling R55.8): the three hexes
/// that share it, and the hex or hexes adjacent to the firer that its LOS runs through (C8.41).
/// </summary>
public sealed record CanisterAim(int Aim, IReadOnlyList<CanisterOffset> Cluster, IReadOnlyList<CanisterOffset> Adjacent);

/// <summary>
/// Canister (C8.4 to C8.42, p. 178; pass 55, rulings R55.8 to R55.10): a Gun's shot that makes no To Hit DR. One IFT DR attacks every Location of
/// a three-hex cluster and of the adjacent hex on the LOS, each with its own TEM and Hindrance, and "also serves as the Ammo Depletion, armament
/// breakdown, and ROF checks usually resolved with the To Hit DR". This calculator decides the shot's own part: the aims, the FP, what halves it,
/// Case A on the IFT DR, the round's use (C8.9), the malfunction, and the ROF. The Fire calculator resolves each Location's attack.
/// </summary>
public static class ScenarioA1CanisterCalculator
{
    private const string Prefix = "asl.a1.ordnance.";

    /// <summary>The ammunition's name in a shot.</summary>
    public const string Ammunition = "canister";

    /// <summary>
    /// The five aims of a Covered Arc (ruling R55.8), left to right: "Canister is fired at a common vertex ... shared by three hexes which is two
    /// hexsides distant (excluding the firing hex)", and "only five possible vertex aiming points (and corresponding three-hex target clusters)
    /// exist in each CA" (the EX and its figure): the vertices whose three hexes all lie in the CA at range 2 or 3. The LOS to the first, second,
    /// fourth, and fifth runs through one of the two CA hexes adjacent to the firer; the LOS to the third runs along their common hexside (C8.41).
    /// </summary>
    public static IReadOnlyList<CanisterAim> Aims
    {
        get;
    } =
    [
        new(1, [new(2, 0), new(3, 0), new(2, 1)], [new(1, 0)]),
        new(2, [new(2, 0), new(1, 1), new(2, 1)], [new(1, 0)]),
        new(3, [new(1, 1), new(2, 1), new(1, 2)], [new(1, 0), new(0, 1)]),
        new(4, [new(0, 2), new(1, 1), new(1, 2)], [new(0, 1)]),
        new(5, [new(0, 2), new(0, 3), new(1, 2)], [new(0, 1)]),
    ];

    /// <summary>The aim of that number, or null when it is not 1 to 5.</summary>
    public static CanisterAim? Aim(int aim) => aim is >= 1 and <= 5 ? Aims[aim - 1] : null;

    /// <summary>
    /// The hexsides of the firer's hex that the two hexes of its Covered Arc adjacent to it lie across (C3.2): the left and the right of the
    /// hexspine it faces. A facing is the hexspine 60 degrees times its number counterclockwise from east (east 0, north-east 1, north-west 2,
    /// west 3, south-west 4, south-east 5); a hexside is numbered from north clockwise (north 0, north-east 1, south-east 2, south 3,
    /// south-west 4, north-west 5).
    /// </summary>
    public static (int Left, int Right) ArcSides(int facing) => ((((1 - facing) % 6) + 6) % 6, (((2 - facing) % 6) + 6) % 6);

    /// <summary>
    /// What a Canister shot attacks, of the Locations of its cluster and of the adjacent hex or hexes that it can reach (C8.4, C8.41, p. 178;
    /// the referee's review of pass 55): "Canister attacks all units in the three target hexes ... In addition, if firing at a same-level
    /// target, it attacks units in the hex adjacent to the firer along the LOS". The adjacent hex is attacked in addition to a cluster that is
    /// fired at: with no Location of the cluster to attack there is no shot, and the result is empty. The same holds for the hexes of an aim
    /// that the map has: an aim with no hex of its cluster on the map is not offered.
    /// </summary>
    public static IReadOnlyList<T> Attacked<T>(IReadOnlyList<T> cluster, IReadOnlyList<T> adjacent)
    {
        ArgumentNullException.ThrowIfNull(cluster);
        ArgumentNullException.ThrowIfNull(adjacent);
        return cluster.Count == 0 ? [] : [.. cluster, .. adjacent];
    }

    /// <summary>The Canister FP IFT Equivalency Table (C8.4, p. 178): 12 for a 37mm Gun, 20 for a 75mm, 24 for a 105mm; null for any other Gun Size.</summary>
    public static int? Firepower(int caliber) => caliber switch
    {
        37 => 12,
        75 => 20,
        105 => 24,
        _ => null,
    };

    /// <summary>
    /// What halves a Canister attack's FP in a Location (C8.4, C8.41, D3.31, D2.42, A7.81; ruling R55.9), each multiplying the others ("halved
    /// again"): the adjacent hex on the LOS, "as Area Fire with no doubling for PBF", or, where the LOS runs along the hexside of two adjacent
    /// hexes, a quarter in each; Bounding (First) Fire; a firer in Motion or not stopped ("quartered if both Motion and Bounding Fire penalties
    /// apply"); Intensive Fire; a pinned crew. An AFPh shot by a Gun that did not move is not halved (ruling R55.10): C8.4 halves it only "if
    /// Infantry move to and man a Gun during the MPh and then fire Canister in the AFPh". A concealed target halves it again on the IFT.
    /// </summary>
    public static IReadOnlyList<FireModifier> Multipliers(int? adjacentHexes, bool boundingFirstFire, bool motion, bool intensiveFire, bool pinnedCrew)
    {
        var multipliers = new List<FireModifier>();
        if (adjacentHexes == 1)
        {
            multipliers.Add(new FireModifier("canister-adjacent-hex", 0.5m, "C8.41"));
        }
        else if (adjacentHexes == 2)
        {
            multipliers.Add(new FireModifier("canister-adjacent-hexside", 0.25m, "C8.41"));
        }

        if (boundingFirstFire)
        {
            multipliers.Add(new FireModifier("bounding-fire", 0.5m, "D3.31"));
        }

        if (motion)
        {
            multipliers.Add(new FireModifier("motion-fire", 0.5m, "D2.42"));
        }

        if (intensiveFire)
        {
            multipliers.Add(new FireModifier("intensive-fire", 0.5m, "C8.4"));
        }

        if (pinnedCrew)
        {
            multipliers.Add(new FireModifier("pinned-crew", 0.5m, "A7.81"));
        }

        return multipliers;
    }

    /// <summary>The same for an attack's Canister fact.</summary>
    public static IReadOnlyList<FireModifier> Multipliers(FireCanister canister)
    {
        ArgumentNullException.ThrowIfNull(canister);
        return Multipliers(canister.AdjacentHexes, canister.BoundingFirstFire == true, canister.Motion == true, canister.IntensiveFire == true, canister.PinnedCrew == true);
    }

    /// <summary>
    /// The DRM a CA change adds to a Canister shot's IFT DR (C8.4: "hexspine changes in the CA"; C3.21: "See D3.52 for IFE and Canister"; D3.52:
    /// "a DRM to its IFT DR equal to the pertinent To Hit DRM of Case A"): by the Gun's mount (C5.1), doubled in woods, a building, or rubble
    /// (C5.11).
    /// </summary>
    public static int CaseA(string? maType, int hexspines, bool inWoodsOrBuilding) => ScenarioA1ArmorCalculator.CaseA(maType, hexspines) * (inWoodsOrBuilding ? 2 : 1);

    /// <summary>
    /// The Canister fact of a shot's attack on one of its Locations (ruling R55.9): the FP of the Gun Size, what the shot's own facts halve it by,
    /// and its Case A. Bounding First Fire and Motion are a vehicle's MA's (C8.4, D3.31, D2.42); no Gun of the catalog has them.
    /// </summary>
    public static FireCanister AttackFact(OrdnanceShot shot, GunDefinition gun, OrdnanceCanisterLocation location)
    {
        ArgumentNullException.ThrowIfNull(shot);
        ArgumentNullException.ThrowIfNull(gun);
        ArgumentNullException.ThrowIfNull(location);
        var caseA = CaseA(gun.MaType, shot.HexspinesToTurn ?? 0, shot.FirerInWoodsOrBuilding == true);
        return new FireCanister(shot.Gun!.GunId, Firepower(gun.Caliber))
        {
            AdjacentHexes = location.AdjacentHexes,
            BoundingFirstFire = shot.FireKind == ScenarioA1FireCalculator.BoundingFirstFire ? true : null,
            Motion = shot.Vehicle?.InMotion == true ? true : null,
            IntensiveFire = shot.IntensiveFire == true ? true : null,
            PinnedCrew = ScenarioA1OrdnanceParticipationCalculator.Pinned(shot) ? true : null,
            CaseA = caseA > 0 ? caseA : null,
        };
    }

    /// <summary>What halves a Canister attack's FP, in a proposal's or a record's words, by the multiplier's name.</summary>
    public static string HalvingWords(string multiplier) => multiplier switch
    {
        "canister-adjacent-hex" => "half in the adjacent hex, C8.41",
        "canister-adjacent-hexside" => "a quarter in each of the two adjacent hexes, C8.41",
        "bounding-fire" => "half in Bounding First Fire, D3.31",
        "motion-fire" => "half in Motion, D2.42",
        "intensive-fire" => "half for Intensive Fire, C8.4",
        "pinned-crew" => "half for a pinned crew, A7.81",
        "area-fire-concealed-target" => "half against concealed units, C8.4",
        _ => multiplier,
    };

    /// <summary>An aim in words for a proposal or a record: "aim 4 of the CA facing west".</summary>
    public static string AimWords(int aim, string facing) => $"aim {aim.ToString(CultureInfo.InvariantCulture)} of the CA facing {facing}";

    /// <summary>The facts a Canister shot needs beyond the Gun's and its crew's.</summary>
    internal static void Missing(OrdnanceShot shot, Action<object?, string> need)
    {
        need(shot.Canister, "canister");
        need(shot.Canister?.Aim, "canister.aim");
        need(shot.Canister?.Facing, "canister.facing");
        need(shot.Canister?.Locations, "canister.locations");
        foreach (var (location, index) in (shot.Canister?.Locations ?? []).Select((item, index) => (item, index)))
        {
            need(location.LocationId, $"canister.locations[{index.ToString(CultureInfo.InvariantCulture)}].locationId");
        }

        need(shot.ScenarioYear, "scenarioYear");
    }

    /// <summary>
    /// The reasons a Canister shot is outside (C8.4, C8.9): a Gun whose counter lists no Canister or whose Gun Size has no FP on the table; none
    /// at the scenario's date, or none left; an aim that is not one of the five; a Location named twice or as no hex of the aim can be; a vehicle
    /// as the target ("ineffective vs armored targets", and the Vehicle Target Type is another shot); and the MPh, which is not built.
    /// </summary>
    internal static void Outside(OrdnanceShot shot, GunDefinition gun, List<string> outside)
    {
        var canister = shot.Canister!;
        if (!ScenarioA1ArmorCalculator.Lists(gun, Ammunition) || Firepower(gun.Caliber) is null)
        {
            outside.Add(Prefix + "canister-gun-outside");
            return;
        }

        if (ScenarioA1ArmorCalculator.ShotDepletion(shot, gun) is null)
        {
            outside.Add(Prefix + "ammunition-unavailable" + ScenarioA1ArmorCalculator.StartMonthDetail(shot, gun));
        }

        if (shot.Gun!.Depleted?.Contains(Ammunition, StringComparer.Ordinal) == true)
        {
            outside.Add(Prefix + "ammunition-depleted");
        }

        // Ruling R55.10 gives Canister FFMO and FFNAM in Defensive First Fire; the shot in the MPh is not built in this pass.
        if (shot.FireKind is not null)
        {
            outside.Add(Prefix + "canister-movement-phase");
        }

        var locations = canister.Locations!;
        var aim = Aim(canister.Aim!.Value);
        if (aim is null || shot.TargetType is not null || shot.Spotter is not null || shot.Director is not null || shot.SameHex == true
            || shot.Hit!.Targets is { Count: > 0 } || shot.Hit.GunTarget is not null
            || locations.Select(item => item.LocationId).Distinct(StringComparer.Ordinal).Count() != locations.Count
            || locations.Count(item => item.AdjacentHexes is null) > 3
            || locations.Any(item => item.AdjacentHexes is not (null or 1 or 2) || (item.AdjacentHexes is { } adjacent && adjacent != aim.Adjacent.Count)))
        {
            outside.Add(Prefix + "canister-outside");
        }

        if (shot.Rolls is { Canister: { } dice } && (dice.Count != 2 || dice.Any(die => die is < 1 or > 6)))
        {
            outside.Add(Prefix + "roll-malformed");
        }

        if (shot.Rolls is { } rolls && (rolls.ToHit is not null || rolls.Subsequent is not null || rolls.CriticalSelection is not null || rolls.Hit is not null
            || rolls.CriticalHit is not null || rolls.ToKill is not null))
        {
            outside.Add(Prefix + "roll-malformed");
        }
    }

    /// <summary>
    /// The shot's resolution from its IFT DR (C8.4, C8.9; rulings R55.9, R55.7, R55.25). Below the Depletion Number the round is used, at it it is
    /// the last; above it "the firer had no such ammunition and is considered not to have fired yet for any purpose unless Gun Malfunction ...
    /// occurs": no Location is attacked, no fire counter is placed, and the ROF is kept. The Original DR at or above the B# malfunctions the Gun
    /// (C2.28, C5.62), and the colored dr at most the ROF keeps it (C2.24; one lower after a CA change, C2.5; none for a pinned crew, A7.81, or in
    /// the AFPh, C5.2). The Gun loses any Acquisition and gains none (C6.5: "or fires SMOKE, canister or IFE").
    /// </summary>
    internal static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference) => Run(shot, gun, reference, null);

    internal static OrdnanceResolution Run(OrdnanceShot shot, GunDefinition gun, ScenarioA1OrdnanceReference reference, VehicleBreakdownUseFacts? breakdownFacts)
    {
        if (shot.Rolls!.Canister is not { } dice)
        {
            return new OrdnanceResolution(OrdnanceResolution.Indeterminate, [Prefix + "roll-missing:canister"], null, null);
        }

        var canister = shot.Canister!;
        var original = dice[0] + dice[1];
        var breakdownOutcome = breakdownFacts is null ? null
            : ScenarioA1VehicleBreakdownResolutionCalculator.Resolve(breakdownFacts, VehicleBreakdownUse.Canister, original);
        var breakdown = breakdownOutcome?.Numbers.BNumber ?? ScenarioA1OrdnanceCalculator.Breakdown(shot, gun, reference);
        var failed = breakdownOutcome?.Failed ?? original >= breakdown;
        var malfunctioned = breakdownOutcome is null ? failed : breakdownOutcome.Outcome == VehicleBreakdownOutcome.Malfunction;
        var depletion = ScenarioA1ArmorCalculator.ShotDepletion(shot, gun)!.Value;
        var use = ScenarioA1ArmorCalculator.Use(original, depletion);
        var firepower = Firepower(gun.Caliber)!.Value;
        var caseA = CaseA(gun.MaType, shot.HexspinesToTurn!.Value, shot.FirerInWoodsOrBuilding == true);
        OrdnanceCanisterResolution Block(IReadOnlyList<OrdnanceCanisterAttack> attacks) =>
            new(canister.Aim!.Value, canister.Facing!, firepower, [.. dice], original, caseA, depletion, attacks);

        if (use == "none" && !(breakdownOutcome?.CountsAsFiredOnDepletionFailure ?? failed))
        {
            return new OrdnanceResolution(OrdnanceResolution.Resolved, [], null, new OrdnanceGunEffect(breakdown, false, gun.RateOfFire ?? 0, true, null, shot.Acquisition!.Value, null))
            {
                AmmunitionUse = use,
                Canister = Block([]),
            };
        }

        // A21.12 (W-A21.13): the penalty multiple lowers the Multiple ROF one each; a shot recorded before the wiring was not lowered.
        var rof = ScenarioA1OrdnanceCalculator.UnloweredRateOfFire(shot, gun);
        if (shot.HexspinesToTurn > 0 && shot.Vehicle is null)
        {
            rof--;
        }

        if (ScenarioA1OrdnanceParticipationCalculator.Pinned(shot) || ScenarioA1OrdnanceCalculator.AfphFire(shot)
            || ScenarioA1OrdnanceParticipationCalculator.HeroGunLimited(shot, gun, reference))
        {
            rof = 0;
        }

        var (kept, counter) = ScenarioA1OrdnanceCalculator.Counter(shot, failed, rof, dice[0]);
        OrdnanceCanisterAttack[] attacks = use == "none" ? [] : [.. canister.Locations!.Select(location =>
        {
            var multipliers = Multipliers(AttackFact(shot, gun, location));
            return new OrdnanceCanisterAttack(location.LocationId!, multipliers, multipliers.Aggregate((decimal)firepower, (value, item) => value * item.Value));
        })];
        // A.11 (W-A21.13): under a penalty multiple the Original B# is the X#, and a DR at or above it eliminates the Gun.
        var eliminated = breakdownOutcome is null ? ScenarioA1OrdnanceCalculator.Eliminated(shot, gun, original, failed) : null;
        return new OrdnanceResolution(OrdnanceResolution.Resolved, [], null,
            new OrdnanceGunEffect(breakdown, malfunctioned && eliminated != true, kept ? Math.Max(rof, 0) : shot.IntensiveFire == true ? 0 : Math.Max(rof, 0), kept, counter, 0, null)
            {
                VehicleBreakdown = breakdownOutcome,
                Eliminated = eliminated,
            })
        {
            // A12.14 (ruling R8.5): the "?" is lost only in the view of a Good Order enemy ground unit within 16 hexes.
            CrewConcealmentLost = shot.Crew!.Concealed == true && shot.CrewSeen != false ? true : null,
            AmmunitionUse = use,
            Canister = Block(attacks),
        };
    }
}
