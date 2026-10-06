namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// What follows a fire attack and its records (A7.7, A8.2, A9.22, A9.223, A10.62, A15.21, A23.6, C6.5; pass 32.c, slice S4, from the planner's Fire,
/// FireExtensions, and Acquisition files and the Units projector): a Fire Lane's attacks, the Encirclement placed, the effects on units and vehicles
/// as condition changes, the follow-ups of an attack, and the Acquisition that follows its units. The caller reads the state and the records and hands
/// the facts over; it writes the events in the order the verdicts give them.
/// </summary>
public static class ScenarioA1FireFollowUps
{
    /// <summary>
    /// A Fire Lane's attack on the moving stack in one of its Locations (A9.22, A9.222; ruling R12.7): Residual FP, never reduced, with no CX, leader, or
    /// hero DRM and no Cowering, taking the Hindrance of the LOS from its MG added to the Location's own.
    /// </summary>
    public static FireAttack FireLaneAttack(FireAttack facts, int laneHindranceDrm)
    {
        ArgumentNullException.ThrowIfNull(facts);
        return facts with
        {
            FireLane = true,
            Los = (facts.Los ?? new FireLos(false, 0, true, false)) with
            {
                HindranceDrm = (facts.Los?.HindranceDrm ?? 0) + laneHindranceDrm
            },
        };
    }

    /// <summary>A9.22, A9.223: a Fire Lane attacks the moving stack still in its Location, once the other attacks there are made, while the lane is in place.</summary>
    public static bool FireLaneAttacks(bool laneInPlace, bool movingStackInLocation, int movers) => laneInPlace && movingStackInLocation && movers > 0;

    /// <summary>A9.223: an Original DR at least the MG's Breakdown Number (12 when the catalog gives none; two less for a captured MG, A21.11) malfunctions the MG, which ends the lane.</summary>
    public static bool FireLaneMalfunctions(int originalDr, int? breakdown, bool captured) => originalDr >= (breakdown ?? 12) - (captured ? 2 : 0);

    // The concealment gained as a Player Turn ends (GamePlanner.ConcealmentGains and AddConcealmentGains of Play, backlog pass 12, ruling R12.5; pass 32.c).

    /// <summary>
    /// The units that gain "?" as their Player Turn ends (A12.12, A12.121, A12.122, the Concealment Table; ruling R12.5): each active phasing Personnel
    /// unit from the catalog, in id order, not concealed, broken, berserk, in Melee, a prisoner, manning a Gun, or sharing its Location with enemy units
    /// (about to be held in Melee, A11.15); not in the LOS of an active, unbroken, uncaptured enemy within 16 hexes (at night, within its NVR or
    /// Illuminated, E1.101), nor beyond 16 out of Concealment Terrain; with the Final Concealment dr modifier it needs (+US#, + the best Good Order
    /// leader's Leadership in the Location, - the Location's TEM, -2 for SMOKE in the hex; A12.122, A6.7), or null when it gains "?" with no dr (at
    /// night, E1.32). A search through the fact reader (the design's D4).
    /// </summary>
    public static List<ConcealmentGain> ConcealmentGains(IEnumerable<ConcealmentCandidateFacts> units, IEnumerable<WatchingEnemyFacts> watchers, bool night, int? month,
        IConcealmentFactReader map)
    {
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(watchers);
        ArgumentNullException.ThrowIfNull(map);
        var gains = new List<ConcealmentGain>();
        var enemies = watchers.Where(unit => unit.Active && unit.Enemy && !unit.Broken && !unit.Captured && unit.Location is not null).ToArray();
        foreach (var unit in units.Where(unit => unit.Active && unit.PhasingSide && !unit.Vehicle && unit.Personnel && !unit.Dummy && unit.HasDefinition)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (unit.Location is not { } at || unit.Concealed || unit.Hidden || unit.Broken || unit.Berserk || unit.Melee || unit.Captured || unit.HoldsGun() || unit.EnemyInLocation())
            {
                // A unit sharing its Location with enemy units is about to be held in Melee (A11.15), and gains no "?" (a reading).
                continue;
            }

            var inLosNear = false;
            var inLosFar = false;
            var within16 = false;
            foreach (var enemy in enemies)
            {
                var range = map.Range(enemy.Location!.Value, at);
                within16 |= range <= 16;
                // E1.101 (backlog pass 16, ruling R16.4): at night an enemy sees a Location within its NVR or Illuminated.
                if (map.LosOpen(enemy.Location.Value, at) && (!night || range <= map.Nvr(enemy.Index) || map.Illuminated(at)))
                {
                    inLosNear |= range <= 16;
                    inLosFar |= range > 16;
                }
            }

            var terrain = unit.TerrainKey();
            var inSeason = month is >= 6 and <= 9;
            var concealmentTerrain = terrain is "brush" or "woods" or "orchard" or "marsh" or "wooden-building" or "stone-building" or "wooden-rubble" or "stone-rubble"
                || (terrain == "grain" && inSeason);
            if (inLosNear || (inLosFar && !concealmentTerrain))
            {
                continue;
            }

            // E1.32 (backlog pass 16, ruling R16.4): at night what would need a Concealment dr gains "?" without one.
            var needsDr = !night && (inLosFar || (!concealmentTerrain && within16));
            if (!needsDr)
            {
                gains.Add(new ConcealmentGain(unit.Id, null));
                continue;
            }

            // A12.122: +US#, + the best Good Order leader's Leadership in the Location unless alone, - the Location's TEM and in-hex Hindrance.
            var size = unit.Squad ? 3 : unit.HalfSquad || unit.Kind == "asl:crew" ? 2 : 1;
            var leadership = unit.BestLeadership();
            var tem = terrain is not null && ScenarioA1FireReference.Tem.TryGetValue(terrain, out var value) ? value : 0;
            // A6.7 (referee, pass 12): only SMOKE in the Location hinders its own units; brush, grain, orchard, and marsh do not.
            var smoke = unit.SmokeInHex() ? 2 : 0;
            gains.Add(new ConcealmentGain(unit.Id, size + leadership - tem - smoke));
        }

        return gains;
    }

    /// <summary>A12.122: a Final Concealment dr of 5 or less gains "?"; the sentence says the dr, its modifier, and the result.</summary>
    public static (bool Gains, string Reason) ConcealmentDr(string id, int dr, int modifier)
    {
        var final = dr + modifier;
        return (final <= 5, $"play.concealment: {id}'s Final Concealment dr is {dr}{modifier:+0;-0;+0} = {final}: {(final <= 5 ? "gains" : "no")} \"?\" (A12.122)");
    }

    /// <summary>A12.12, the Concealment Table: the sentence of a unit that gains "?" with no dr.</summary>
    public static string ConcealmentWithoutDr(string id) => $"play.concealment: {id} gains \"?\" (A12.12, the Concealment Table)";
}
