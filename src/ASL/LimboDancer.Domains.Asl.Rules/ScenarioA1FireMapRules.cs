namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The map's part of a fire attack (A4.34, A6.7, A7.21, A7.212, A7.5, A8.15, A8.2, A8.3, B9.3, B10.31, B15.6, B16.32, D7.22, D9.3; rulings R5.19,
/// R6.1, R6.6, R9.6, R10.1, R10.4 to R10.8, R10.13, R10.14; pass 32.c, slice S4, from the planner's FireMapFacts): one function a block, called in
/// the old order. The caller reads the map (the Locations, the hexsides, each LOS, the walls, the wrecks and SMOKE) and the state, and hands the facts
/// over; a read a function here asks for through a delegate is made where the old body made it. LOS and range stay in Maps (the design, D8).
/// </summary>
public static class ScenarioA1FireMapRules
{
    /// <summary>The target Location must be read, and its terrain must have a TEM in the Fire package.</summary>
    public static string? TargetTerrainBar(bool read, string? terrainKey, string? name) =>
        !read ? "play.fire-map: the target Location cannot be read"
        : terrainKey is null ? $"play.fire-terrain: the target's terrain ({name ?? "unknown"}) has no TEM in the Fire package"
        : null;

    /// <summary>Whether the attack is Infantry fire: not Residual FP, not an ordnance hit, not a vehicle's fire.</summary>
    public static bool IsDirectFire(FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        return attack.FireKind != ScenarioA1FireCalculator.ResidualFire && attack.OrdnanceHit is null && attack.VehicleFire is null;
    }

    /// <summary>Rulings R10.5, R10.6: walls and hedges are reviewed for Infantry fire; ordnance and a vehicle's fire keep refusing hexside terrain and cliffs at the target; Residual FP crosses no hexside (table player, pass 10).</summary>
    public static string? HexsideTerrainBar(bool direct, string? fireKind, bool hexsideTerrainOrCliffAtTarget) =>
        !direct && fireKind != ScenarioA1FireCalculator.ResidualFire && hexsideTerrainOrCliffAtTarget
            ? "play.fire-terrain: hexside terrain at the target is not reviewed for ordnance or a vehicle's fire (ruling R10.5)"
            : null;

    /// <summary>B15.6 (ruling R5.19): grain is Open Ground outside June to September; with no scenario month, fire at a moving unit in grain is not decided. The terrain as the attack reads it, or the refusal.</summary>
    public static (string? Refusal, string Terrain) GrainTerrain(string terrain, int? month, string? phase)
    {
        if (terrain == "grain")
        {
            if (month is not { } known)
            {
                if (phase == "mph")
                {
                    return ("play.fire-grain: grain is Open Ground outside its season, which the game does not name, so FFMO there is not decided (B15.6)", terrain);
                }
            }
            else if (known is < 6 or > 9)
            {
                return (null, "open-ground");
            }
        }

        return (null, terrain);
    }

    /// <summary>A4.3, A4.34, B23.31 (ruling R10.7): a stack in Bypass is in the other terrain of the hexsides it moves along, not in the obstacle; whether the attack reads those hexsides.</summary>
    public static bool UsesBypassLane(bool direct, string? fireKind, string? phase, bool movingStackBypassingAtTarget) =>
        (direct || fireKind == ScenarioA1FireCalculator.ResidualFire) && phase == "mph" && movingStackBypassingAtTarget;

    /// <summary>
    /// The terrain of a Bypassing stack (A4.34; ruling R10.7): the highest-TEM terrain of the hexsides it moves along, a road as Open Ground; refused
    /// when a hexside's terrain is not reviewed, for fire from within the hex or a Snap Shot at a Bypass step, and in a hex with a wall or hedge
    /// (the vertex LOS is not built).
    /// </summary>
    public static (string? Refusal, string? Terrain) BypassTerrain(IReadOnlyList<BypassHexsideFacts> hexsides, bool direct, bool firerInTarget, bool snapShot, bool wallOrHedgeOnHex)
    {
        ArgumentNullException.ThrowIfNull(hexsides);
        string?[] laneTerrain = [.. hexsides.Select(side => side.TerrainName is { } name ? ScenarioA1Definitions.FireTerrain.GetValueOrDefault(name) ?? (side.Road ? "open-ground" : null) : null)];
        if (laneTerrain.Length == 0 || laneTerrain.Any(key => key is null))
        {
            return ("play.fire-bypass: the terrain the Bypassing stack moves through is not reviewed (ruling R10.7)", null);
        }

        var terrain = laneTerrain.OrderByDescending(key => ScenarioA1FireReference.Tem[key!]).First()!;

        // Table player, pass 10: whether units in the obstacle and a stack Bypassing it share a Location for TPBF is not built, nor a Snap Shot at
        // a Bypass step.
        if (direct && (firerInTarget || snapShot))
        {
            return ("play.fire-bypass: fire from within the Bypassed hex, and a Snap Shot at a Bypass step, are not built (A4.34, A8.15; ruling R10.7)", null);
        }

        // A4.34 (referee, pass 10): a wall or hedge of the hex applies when the LOS crosses it, which the center LOS cannot tell.
        return wallOrHedgeOnHex
            ? ("play.fire-bypass: fire at a Bypassing stack in a hex with a wall or hedge needs the vertex LOS, which is not built (A4.34; ruling R10.7)", null)
            : (null, terrain);
    }

    /// <summary>D9.3, D10.3 (ruling R6.1): the side whose Infantry may claim a wreck's or AFV's +1 TEM at the target: the first target's side.</summary>
    public static string? InfantrySideOf(IEnumerable<string?> targetSides)
    {
        ArgumentNullException.ThrowIfNull(targetSides);
        return targetSides.FirstOrDefault(side => side is not null);
    }

    /// <summary>A8.2 (rulings R6.6, R9.6): Residual FP has no LOS Hindrance, but the SMOKE of its Location applies: a burning wreck's and each grenade counter's +2, at most +3, never the outgoing +1 (B25.2, A24.8).</summary>
    public static FireAttack ResidualMapFacts(FireAttack attack, string terrain, int smokeSourcesAtTarget)
    {
        ArgumentNullException.ThrowIfNull(attack);
        var smoke = Math.Min(3, 2 * smokeSourcesAtTarget);
        return attack with
        {
            TargetTerrain = terrain,
            Los = smoke > 0 ? new FireLos(false, smoke, true, false) : null,
        };
    }

    /// <summary>
    /// A8.15 (ruling R10.13): a Snap Shot is Infantry Defensive First Fire at the hexside the moving stack just crossed; one at a hexside of a hex with a
    /// wall, hedge, SMOKE, or rubble is not built (B9.42; referee, pass 10). <paramref name="eitherHexNotBuilt"/> is read once the first check passes.
    /// </summary>
    public static string? SnapShotBar(bool direct, string? phase, bool movementFromKnownAtTarget, bool crossedSideKnown, Func<bool> eitherHexNotBuilt)
    {
        ArgumentNullException.ThrowIfNull(eitherHexNotBuilt);
        if (!direct || phase != "mph" || !movementFromKnownAtTarget || !crossedSideKnown)
        {
            return "play.fire-snap-shot: a Snap Shot is Infantry Defensive First Fire at the hexside the moving stack just crossed (A8.15)";
        }

        return eitherHexNotBuilt() ? "play.fire-snap-shot: a Snap Shot at a hexside of a hex with a wall, hedge, SMOKE, or rubble is not built (A8.15; ruling R10.13)" : null;
    }

    /// <summary>The LOS of firers at their own Location (A7.21): range 0, same level, no Hindrance.</summary>
    public static FireLos OwnLocationLos => new(false, 0, true, false);

    /// <summary>
    /// A firer's Location (B16.32, A7.212, A7.21, D7.22; rulings R10.1, R10.14): it must be read; no fire from marsh; a unit whose Location holds a Known
    /// enemy unit fires at nothing else; only Infantry fire as TPBF at units in their own Location; in the MPh, fire at a moving vehicle in the firer's
    /// own Location is Non-CC Reaction Fire, made only after its OVR there.
    /// </summary>
    public static FirerLocationVerdict FirerLocation(FirerLocationFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.Read)
        {
            return new FirerLocationVerdict("play.fire-map: a firer's Location cannot be read", false);
        }

        if (facts.Direct)
        {
            // B16.32: fire from a marsh hex is limited and Area Fire, which is not built.
            if (facts.TerrainKey == "marsh")
            {
                return new FirerLocationVerdict("play.fire-marsh: fire from a marsh hex is limited to some weapons and resolved as Area Fire, which is not built (B16.32; ruling R10.1)", false);
            }

            // A7.212 (ruling R10.14): a unit whose Location holds a Known enemy unit fires at nothing else.
            if (!facts.IsTarget && facts.KnownArmedEnemyThere())
            {
                return new FirerLocationVerdict($"play.fire-target-limit: {facts.Location} holds a Known enemy unit, so its units fire only at their own Location (A7.212)", false);
            }
        }

        // A7.21 (ruling R10.14): TPBF at the enemy units of the firer's own Location.
        if (facts.IsTarget)
        {
            if (!facts.Direct || facts.SnapShot)
            {
                return new FirerLocationVerdict("play.fire-own-location: only Infantry fire as TPBF at units in their own Location (A7.21)", false);
            }

            // D7.22 (table-player finding, pass 11): in the MPh, fire at a moving vehicle in the firer's own Location is Non-CC Reaction Fire, made
            // only after its OVR there; CC Reaction Fire is the vehicle CC action (D7.21).
            if (facts.Phase == "mph" && facts.MovingVehicleThereBeforeOverrun())
            {
                return new FirerLocationVerdict("play.fire-reaction: a DEFENDER unit fires at a moving vehicle in its own Location only as Reaction Fire after the vehicle's OVR there (D7.22); CC Reaction Fire is the vehicle CC action (D7.21)", false);
            }

            return new FirerLocationVerdict(null, true);
        }

        return new FirerLocationVerdict(null, false);
    }

    /// <summary>
    /// A8.15 (ruling R10.13): a Snap Shot is traced to both ends of the hexside the stack crossed: both must be definite and clear; the LOS with the
    /// greater Hindrance counts, at the lesser of the ranges to the target hex and the hex left; never at a unit entering the firer's hex. The LOS
    /// taken (the first when true, else the second) and the range, or the refusal.
    /// </summary>
    public static (string? Refusal, bool First, int Range) SnapShotLos(LosReadFacts? first, LosReadFacts? second, int? rangeToTarget, int? rangeToLeft)
    {
        if (first is not { Definite: true } || second is not { Definite: true })
        {
            return ("play.fire-los: the LOS read to the crossed hexside gives no definitive answer", false, 0);
        }

        if (first.Blocked || second.Blocked)
        {
            return ("play.fire-snap-shot: the firer has no LOS to the whole hexside crossed (A8.15)", false, 0);
        }

        var takeFirst = first.Hindrances.Sum(item => item.Value) >= second.Hindrances.Sum(item => item.Value);
        var range = Math.Min(rangeToTarget ?? int.MaxValue, rangeToLeft ?? int.MaxValue);
        return range == int.MaxValue || rangeToTarget == 0
            ? ("play.fire-snap-shot: a Snap Shot is not taken at a unit entering the firer's hex, and its range must be read (A8.15)", false, 0)
            : (null, takeFirst, range);
    }

    /// <summary>The LOS to the target must be read and definite (Clear or Blocked).</summary>
    public static string? LosBar(LosReadFacts? los) =>
        los is null ? "play.fire-los: the board has no LOS data to read"
        : !los.Definite ? $"play.fire-los: the LOS read gives no definitive answer ({los.StatusText}: {los.Reason})"
        : null;

    /// <summary>A4.34 (ruling R10.7): LOS to a Bypassing stack's hex center must cross a hexside it Bypasses; the vertex LOS is not built. <paramref name="entrySides"/> is null when the entry cannot be read.</summary>
    public static string? BypassLosBar(bool lane, IReadOnlyList<int>? entrySides, IReadOnlyList<int> bypassedSides)
    {
        ArgumentNullException.ThrowIfNull(bypassedSides);
        return lane && (entrySides is null || !entrySides.Any(bypassedSides.Contains))
            ? "play.fire-bypass-los: the LOS to the Bypassing stack does not cross a hexside it Bypasses; LOS to its vertices is not built (A4.34; ruling R10.7)"
            : null;
    }

    /// <summary>
    /// A6.7: the largest Hindrance at each range counts; brush always, grain June to September (B15.2), marsh at the same level (B16.2); and an AFV or
    /// wreck where the map has none at that range, and a burning wreck's smoke (D9.4, B25.2; rulings R6.2, R6.3), which <paramref name="vehicleHindrance"/>
    /// reads for the ranges with a map Hindrance. The Location's LOS facts, or the vehicle read's refusal.
    /// </summary>
    public static (string? Refusal, FireLos? Los) LocationLos(LosReadFacts los, int? month, bool sameLevel, Func<HashSet<int>, (int Drm, string? Reason)> vehicleHindrance)
    {
        ArgumentNullException.ThrowIfNull(los);
        ArgumentNullException.ThrowIfNull(vehicleHindrance);
        var inSeason = month is >= 6 and <= 9;
        var attributed = los.Hindrances.All(entry => entry.Terrains.Count > 0 && entry.Terrains.All(item => item is "Brush" or "Grain" or "Marsh"));
        var mapRanges = los.Hindrances.Where(entry => entry.Terrains.Contains("Brush") || (sameLevel && entry.Terrains.Contains("Marsh")) || (inSeason && entry.Terrains.Contains("Grain")))
            .Select(entry => entry.Range).ToHashSet();
        var grain = los.Hindrances.Any(entry => entry.Terrains.Contains("Grain"));
        var (vehicleDrm, vehicleReason) = vehicleHindrance(mapRanges);
        // B.10 (p. 113; pass 35, task 35.5): any combination of SMOKE, weather, and terrain Hindrance of +6 or more blocks the LOS, so the terrain's count
        // with the vehicles', wrecks', and SMOKE's is tested here; the map's own total was tested alone until now.
        var total = mapRanges.Count + vehicleDrm;
        return vehicleReason is not null ? (vehicleReason, null) : (null, new FireLos(los.Blocked || HindranceBlocks(total), total, attributed, grain));
    }

    /// <summary>B.10: a LOS Hindrance DRM of +6 or more blocks the LOS completely.</summary>
    public static bool HindranceBlocks(int hindranceDrm) => hindranceDrm >= 6;

    /// <summary>
    /// The group's map facts from its Locations (A7.5): the first Location's range, level, and LOS for the group; each firer's own when the group spans
    /// Locations, and whether every Location is ADJACENT to another of them, read pair by pair.
    /// </summary>
    public static FireAttack GroupMapFacts(FireAttack attack, string terrain, int targetHeight,
        IReadOnlyDictionary<string, (int Range, bool SameLevel, FireLos Los, int Height)> perLocation, Func<string, string, bool> adjacent)
    {
        ArgumentNullException.ThrowIfNull(attack);
        ArgumentNullException.ThrowIfNull(perLocation);
        ArgumentNullException.ThrowIfNull(adjacent);
        var firstLocation = perLocation[attack.FirerLocationId!];
        var facts = attack with
        {
            Range = firstLocation.Range,
            SameLevel = firstLocation.SameLevel,
            TargetLevelAbove = firstLocation.SameLevel ? null : targetHeight - firstLocation.Height,
            Los = firstLocation.Los,
            TargetTerrain = terrain,
        };
        if (perLocation.Count > 1)
        {
            // A7.5: every Location of the group ADJACENT to another of them.
            var locations = perLocation.Keys.ToArray();
            facts = facts with
            {
                Firers = [.. attack.Firers!.Select(firer => firer with
                {
                    Range = perLocation[firer.LocationId!].Range,
                    SameLevel = perLocation[firer.LocationId!].SameLevel,
                    TargetLevelAbove = perLocation[firer.LocationId!].SameLevel ? null : targetHeight - perLocation[firer.LocationId!].Height,
                    Los = perLocation[firer.LocationId!].Los,
                })],
                FirerLocationsAdjacent = locations.All(one => locations.Any(two => two != one && adjacent(one, two))),
            };
        }

        return facts;
    }

    /// <summary>B9.3 to B9.41 (rulings R10.5, R10.6): the wall or hedge TEM is read for Infantry fire from Locations at range, not for a Snap Shot, TPBF, or a Bypassing stack.</summary>
    public static bool WantsWallTem(bool direct, bool snapShot, bool lane, int sources, int locations) =>
        direct && !snapShot && !lane && sources == locations && sources > 0;

    /// <summary>B10.31 (ruling R10.4): Height Advantage over every firer, read when every Location is at range; null otherwise and when the read says no.</summary>
    public static bool? HeightAdvantage(int sources, int locations, Func<bool> read)
    {
        ArgumentNullException.ThrowIfNull(read);
        return sources == locations && read() ? true : null;
    }

    /// <summary>A4.62 (ruling R10.8; referee, pass 10): only the crew pushing its Gun into the target Location this MPh is under Hazardous Movement, when it is every target.</summary>
    public static bool? HazardousMovement(string? phase, bool pushedGunAtTarget, string? gunHolder, FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        return phase == "mph" && pushedGunAtTarget && gunHolder is not null && attack.Targets!.Count > 0 && attack.Targets.All(item => item.UnitId == gunHolder) ? true : null;
    }

    /// <summary>A8.3: Subsequent First Fire, and FPF by a firer not yet marked Final Fire, reach no farther than the closest armed, Known enemy unit from each firer's Location.</summary>
    public static bool ReadsSubsequentFirstFireRange(FireAttack attack)
    {
        ArgumentNullException.ThrowIfNull(attack);
        return attack.FireKind == ScenarioA1FireCalculator.SubsequentFirstFire
            || (attack.FireKind == ScenarioA1FireCalculator.FinalProtectiveFire && attack.Firers!.Any(item => item.FinalFireMarked != true));
    }

    /// <summary>
    /// Whether no target is farther than the closest armed, Known enemy unit from each firer's Location (A8.3): for each Location at range, the least
    /// LOS range to an enemy Location (the search, with the fact reader of the design's D4) is at least the Location's range to the target.
    /// </summary>
    public static bool WithinSubsequentFirstFireRange(IReadOnlyList<(int Location, int Range)> firerLocations, IReadOnlyList<int> enemyLocations, ILosFactReader los)
    {
        ArgumentNullException.ThrowIfNull(firerLocations);
        ArgumentNullException.ThrowIfNull(enemyLocations);
        ArgumentNullException.ThrowIfNull(los);
        return firerLocations.All(pair => enemyLocations
            .Select(enemy => pair.Range == 0 ? 0 : los.Los(pair.Location, enemy)?.Range ?? int.MaxValue).DefaultIfEmpty(int.MaxValue).Min() >= pair.Range);
    }

    // Encirclement and Fire Lanes (GamePlanner.FireExtensions of Play, backlog pass 12, rulings R12.7 and R12.11; pass 32.c).

    /// <summary>
    /// Where an LOS enters a target hex, as a position on its perimeter (A7.7; ruling R12.11): hexside i is 2i + 1, the hexspine between hexsides i and
    /// i + 1 is 2i + 2, counting 12 positions clockwise from the north vertex of the north hexside; null when the entry cannot be read.
    /// </summary>
    public static int? PerimeterPosition(IReadOnlyList<int> sides)
    {
        ArgumentNullException.ThrowIfNull(sides);
        return sides switch
        {
            [var one] => (2 * one) + 1,
            [var a, var b] when (a + 1) % 6 == b => ((2 * a) + 2) % 12,
            [var a, var b] when (b + 1) % 6 == a => ((2 * b) + 2) % 12,
            _ => null,
        };
    }

    /// <summary>
    /// An attack's share of an Encirclement (A7.7; ruling R12.11): the units firing inherent FP or a SW at no more than Normal Range, when its FP could
    /// inflict at least a NMC allowing for Cowering, with the positions where their LOS enters the target hex; none for other fire. <paramref name="firers"/>
    /// gives, for each firer of the attack in order, whether its Location is known and its LOS entry read.
    /// </summary>
    public static (int Units, List<int> Entries) EncirclementShare(FireAttack facts, FireArithmetic? arithmetic, bool targetKnown, ScenarioA1FireReference reference,
        IReadOnlyList<EncirclementFirerFacts> firers)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(firers);
        if (facts.VehicleFire is not null || facts.Overrun is not null || facts.OrdnanceHit is not null || facts.FireKind == ScenarioA1FireCalculator.ResidualFire
            || facts.Firers is not { Count: > 0 } firing || arithmetic is null || !targetKnown)
        {
            return (0, []);
        }

        var could = ScenarioA1ResultTables.CouldCauseNmc(facts, arithmetic);
        if (!could(false) && !could(true))
        {
            return (0, []);
        }

        var units = 0;
        var entries = new List<int>();
        foreach (var (firer, index) in firing.Select((firer, index) => (firer, index)))
        {
            var definition = reference.Definitions.GetValueOrDefault(firer.DefinitionId ?? string.Empty);
            var weaponRanges = (firer.Weapons ?? []).Select(weapon => reference.Definitions.GetValueOrDefault(weapon.DefinitionId ?? string.Empty)?.Range).OfType<int>();
            int? normal = firer.UsesInherentFp == false ? weaponRanges.DefaultIfEmpty(0).Max() : definition?.Range;
            var range = firer.Range ?? facts.Range;
            if (normal is not { } limit || range is not { } distance || distance < 1 || distance > limit || !firers[index].LocationKnown)
            {
                continue;
            }

            units++;
            if (firers[index].EntrySides() is { } sides && PerimeterPosition(sides) is { } position)
            {
                entries.Add(position);
            }
        }

        return (units, entries);
    }

    /// <summary>
    /// The side this attack Encircles in its target Location, or null (A7.7; ruling R12.11): with the side's earlier attacks this phase on that Location,
    /// made consecutively (none of its attacks at another Location between; a Gun's shot at another Location breaks the sequence too), by at least two
    /// counting units whose LOS entries Encircle it. <paramref name="unitSide"/> reads a target's side; <paramref name="encirclementExists"/> whether the
    /// Location already Encircles a side; <paramref name="earlier"/> is the phase's attacks, latest first, each read when reached.
    /// </summary>
    public static string? EncirclementSeal(string? phase, FireAttack facts, bool targetKnown, Func<string, string?> unitSide, Func<string, bool> encirclementExists,
        ScenarioA1FireReference reference, IReadOnlyList<EncirclementFirerFacts> firers, IEnumerable<EarlierAttackFacts> earlier, string firingSide)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(unitSide);
        ArgumentNullException.ThrowIfNull(encirclementExists);
        ArgumentNullException.ThrowIfNull(earlier);
        if (phase is not ("pfph" or "dfph" or "afph") || facts.SprayingFire == true || !targetKnown)
        {
            return null;
        }

        var targetSide = (facts.Targets ?? []).Where(item => item.Friendly != true && item.Dummy != true && item.GuardId is null)
            .Select(item => unitSide(item.UnitId!)).FirstOrDefault(side => side is not null);
        if (targetSide is null || encirclementExists(targetSide))
        {
            return null;
        }

        var (units, entries) = EncirclementShare(facts, ScenarioA1FireCalculator.Preview(facts, reference), targetKnown, reference, firers);
        if (units == 0)
        {
            return null;
        }

        foreach (var attack in earlier)
        {
            if (attack.Ordnance)
            {
                if (attack.Side == firingSide && !attack.SameTarget)
                {
                    break;
                }

                continue;
            }

            if (attack.Side != firingSide)
            {
                continue;
            }

            if (!attack.SameTarget)
            {
                break;
            }

            if (attack.Read() is not { } record || record.Blocked || record.Recorded.SprayingFire == true)
            {
                continue;
            }

            var (more, seen) = EncirclementShare(record.Recorded, record.Arithmetic, targetKnown, reference, record.Firers);
            units += more;
            entries.AddRange(seen);
        }

        return units >= 2 && ScenarioA1Geometry.Encircles(entries) ? targetSide : null;
    }

    /// <summary>
    /// The Locations of a Fire Lane (A9.22; ruling R12.7): along the Hex Grain from the MG's hex through the target hex to the counter's hex, each at the
    /// MG's level, within its Normal Range and in its manning Infantry's LOS, with its Fire Lane Residual FP (the IFT column left of the MG's FP, doubled
    /// ADJACENT) and the LOS Hindrance DRM from the MG; null with the reason when the lane is not on a Hex Grain. A search over the map through the
    /// fact reader (the design's D4); the MG's, the target's, and the counter's Locations are table indexes.
    /// </summary>
    public static (IReadOnlyList<FireLaneLocation>? Entries, string? Reason) FireLaneEntries(int from, int fromLevel, int target, int to, string targetText, string toText,
        int normalRange, int firepower, IFireLaneFactReader map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var column = Array.FindLastIndex(ScenarioA1FireReference.ColumnFp, fp => fp <= firepower) - 1;
        if (column < 0)
        {
            return (null, "play.fire-lane: the MG's FP has no IFT column to its left, so its Fire Lane has no Residual FP (A9.22)");
        }

        var laneFp = ScenarioA1FireReference.ColumnFp[column];
        int? fromHeight = map.BaseLevel(from) is { } fromBase ? fromBase + fromLevel : null;
        for (var direction = 0; direction < 6; direction++)
        {
            var path = new List<int>();
            var at = from;
            for (var step = 1; step <= normalRange && map.Across(at, direction) is { } next; step++)
            {
                at = next;
                path.Add(at);
            }

            var targetIndex = path.FindIndex(item => map.HexOf(item) == map.HexOf(target));
            var toIndex = path.FindIndex(item => map.HexOf(item) == map.HexOf(to));
            if (targetIndex < 0 || toIndex < targetIndex)
            {
                continue;
            }

            var entries = new List<FireLaneLocation>();
            foreach (var (location, index) in path.Take(toIndex + 1).Select((item, index) => (item, index)))
            {
                if (map.BaseLevel(location) is not { } level || level != fromHeight || map.Los(from, location) is not { Blocked: false } los)
                {
                    continue;
                }

                entries.Add(new FireLaneLocation(location, index == 0 ? 2 * laneFp : laneFp, los.Hindrance));
            }

            return entries.Count == 0 ? (null, "play.fire-lane: no Location of the lane is at the MG's level and in its LOS (A9.22)") : (entries, null);
        }

        return (null, $"play.fire-lane: {toText} is not on the Hex Grain through the MG's hex and {targetText}, within the MG's Normal Range (A9.22)");
    }
}
