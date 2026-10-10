using System.Globalization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Rout Phase and Desperation Morale (backlog pass 13, rulings R13.1 and R13.3; pass 32.f): who must or may rout, a rout's legal route to the
/// nearest woods or building Location, Low Crawl, Interdiction, Failure to Rout and surrender, and the DM a broken unit gains from an ADJACENT
/// Known armed enemy unit. Play reads the state and the map, hands the facts over through <see cref="IRoutFactReader"/>, and writes the events.
/// </summary>
public static class ScenarioA1RoutCalculator
{
    /// <summary>A10.5, A10.62: an armed enemy unit is Personnel (a leader without a SW counts), or a vehicle not Abandoned.</summary>
    public static bool Armed(bool vehicle, bool abandoned) => !vehicle || !abandoned;

    /// <summary>A10.532: a unit's own printed range counts toward its Normal Range; a leader's does not, and a unit the catalog does not know has none.</summary>
    public static int OwnRange(bool fromCatalog, bool leader, int? printedRange) => fromCatalog && !leader ? printedRange ?? 0 : 0;

    /// <summary>
    /// The Normal Range in hexes of a unit's fire (A10.532): the longest of its own range and the Normal Ranges of the functioning SW it possesses,
    /// at most 16.
    /// </summary>
    public static int NormalRange(int own, IEnumerable<int> weaponRanges)
    {
        ArgumentNullException.ThrowIfNull(weaponRanges);
        return Math.Min(16, weaponRanges.Append(own).Max());
    }

    /// <summary>D6.1 (ruling R26.2): a broken Passenger may stay aboard, free of rout requirements; a unit in Melee or captured need not rout.</summary>
    public static bool MustRoutCandidate(bool active, bool broken, bool melee, bool captured, bool aboard) => active && broken && !melee && !captured && !aboard;

    /// <summary>Why a broken unit must rout (A10.5; ruling R13.3), or null when it need not.</summary>
    public static string? MustRout(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth) =>
        NearUnbrokenArmedEnemy(reader, enemies, at) is { } enemy ? $"{enemy} is a Known unbroken armed enemy unit ADJACENT to it or in its Location"
            : ExposedInOpenGround(reader, enemies, at, scenarioMonth) is { } seen ? $"it is in Open Ground in the LOS and Normal Range of {seen}"
            : null;

    /// <summary>
    /// Whether a broken unit may rout (A10.5): it must, or it is under DM. A Disrupted unit stays put unless it must rout (A19.12; pass 35, task 35.2):
    /// its DM alone gives it no rout.
    /// </summary>
    public static bool MayRout(string? mustRout, bool active, bool broken, bool desperationMorale, bool melee, bool captured, bool disrupted) =>
        mustRout is not null || (active && broken && desperationMorale && !melee && !captured && !disrupted);

    /// <summary>Two Locations are the same or ADJACENT.</summary>
    public static bool AdjacentOrSame(IRoutFactReader reader, int one, int two)
    {
        ArgumentNullException.ThrowIfNull(reader);
        return one == two || reader.Adjacent(one, two);
    }

    /// <summary>A Known unbroken armed enemy unit ADJACENT to a Location or in it (A10.5), or null.</summary>
    public static string? NearUnbrokenArmedEnemy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && !item.Broken && AdjacentOrSame(reader, item.Location, at))
            .Select(item => item.Id).Order(StringComparer.Ordinal).FirstOrDefault();
    }

    /// <summary>The Known armed enemy units ADJACENT to a Location or in it (A10.51, A10.62).</summary>
    public static IEnumerable<RoutEnemyFacts> ArmedEnemiesNear(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && AdjacentOrSame(reader, item.Location, at));
    }

    /// <summary>
    /// Open Ground (A10.531; ruling R13.3): a hex where the enemy could apply the FFMO DRM, read as Open Ground or road terrain, or grain outside June to
    /// September (B15.6; ruling R5.19), with no SMOKE there.
    /// </summary>
    public static bool OpenGround(RoutLocationFacts? read, int? scenarioMonth) =>
        read is { } location && !location.Smoke
            && (location.TerrainKey == "open-ground" || (location.TerrainKey == "grain" && scenarioMonth is < 6 or > 9));

    /// <summary>
    /// The cover facts of one LOS (A10.531; pass 35, task 35.4): fire's count of the map Hindrance stands when fire can attribute every Hindrance on
    /// the LOS to its terrain; otherwise, or when the read of the vehicles on the LOS was refused, the map's own total does.
    /// </summary>
    public static RoutCoverFacts CoverFacts(bool hindranceRead, bool attributed, int fireHindrance, int vehiclesAndSmoke, bool hexsideTem, bool heightAdvantage, bool inHexCover) =>
        new(hindranceRead && attributed ? fireHindrance - vehiclesAndSmoke : null, hindranceRead ? vehiclesAndSmoke : 0, hexsideTem, heightAdvantage, inHexCover);

    /// <summary>
    /// Whether the enemy unit in a Location could apply the FFMO DRM to a unit in an Open Ground Location (A10.531, A10.53; pass 35, task 35.4): a clear
    /// LOS within the range given, no Hindrance of any kind along it (the map's, a vehicle's or wreck's, SMOKE), and no TEM the unit could claim against
    /// that enemy: a wall or hedge on the hexside crossed, Height Advantage, or a wreck or AFV in the Location. The cover is read only for a clear LOS in range.
    /// A unit entering from <paramref name="steppedFrom"/> has no Height Advantage against a LOS that crosses the Crest Line hexside it climbs (B1.14, p. 113).
    /// </summary>
    public static bool CouldApplyFfmo(IRoutFactReader reader, int enemyLocation, int at, int range, int? steppedFrom = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (reader.Los(enemyLocation, at) is not { Clear: true } los || los.Range > range)
        {
            return false;
        }

        var cover = reader.Cover(enemyLocation, at, steppedFrom);
        return (cover.MapHindrance ?? los.Hindrance) + cover.OtherHindrance == 0 && !cover.HexsideTem && !cover.HeightAdvantage && !cover.InHexCover;
    }

    /// <summary>
    /// The range within which a unit may Interdict (A10.532; pass 35, task 35.4): its Normal Range, but for a SMC with no other SMC to man a weapon with
    /// him, whose SW fires at half FP and so does not Interdict: he has his own range alone, which for a leader is none. A hero is not such a SMC:
    /// he "uses a MG (at full FP)" alone (A15.23, p. 83), so he Interdicts at his weapons' range.
    /// </summary>
    public static int InterdictionRange(bool smc, bool anotherSmcThere, int own, IEnumerable<int> weaponRanges, bool hero = false) =>
        smc && !anotherSmcThere && !hero ? Math.Min(16, own) : NormalRange(own, weaponRanges);

    /// <summary>
    /// A Known unbroken enemy unit not in Melee in whose LOS and Normal Range a Location in Open Ground lies, with no Hindrance between (A10.5, A10.531), or null.
    /// </summary>
    public static string? ExposedInOpenGround(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        if (!OpenGround(reader.Location(at), scenarioMonth))
        {
            return null;
        }

        // A10.531 (table player, pass 13): only an enemy unit that could fire applies FFMO, so broken ones do not count.
        foreach (var enemy in enemies.Where(item => !item.Melee && !item.Broken).OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (enemy.NormalRange is > 0 and var range && CouldApplyFfmo(reader, enemy.Location, at, range))
            {
                return enemy.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// The enemy unit able to Interdict a routing unit entering an Open Ground Location (A10.53, A10.532, A10.533; ruling R13.3), or null: Known unbroken
    /// Infantry not CX, pinned, Encircled, in Melee, or a prisoner, that could apply the FFMO DRM there (<see cref="CouldApplyFfmo"/>) within the range
    /// it may Interdict at (<see cref="InterdictionRange"/>). Vehicles, and units whose FP is halved for other reasons, are not read (backlog).
    /// </summary>
    public static string? Interdictor(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, int? scenarioMonth, int? steppedFrom = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        if (!OpenGround(reader.Location(at), scenarioMonth))
        {
            return null;
        }

        foreach (var enemy in enemies.Where(item => !item.Vehicle && !item.Broken && !item.Cx && !item.Pinned && !item.Melee && !item.Encircled)
            .OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if ((enemy.InterdictionRange ?? enemy.NormalRange) is > 0 and var range && CouldApplyFfmo(reader, enemy.Location, at, range, steppedFrom))
            {
                return enemy.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Why a rout step from one Location to an ADJACENT one is not allowed (A10.5, A10.51; ruling R13.3), or null: it enters a Location holding a Known
    /// enemy unit, or a Location ADJACENT to one unless it leaves that unit's Location, or it decreases the range to a Known armed enemy unit that has had
    /// the routing unit in its LOS this rout.
    /// </summary>
    public static string? RoutStepBar(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int fromLocation, int toLocation, IReadOnlyCollection<string> seenBy)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(seenBy);
        foreach (var enemy in enemies)
        {
            if (enemy.Location == toLocation)
            {
                return $"play.rout-step: a routing unit never enters {reader.Name(toLocation)}, which holds the Known enemy unit {enemy.Id} (A10.51)";
            }

            if (enemy.Location != fromLocation && reader.Adjacent(enemy.Location, toLocation))
            {
                return $"play.rout-step: a routing unit never moves ADJACENT to the Known enemy unit {enemy.Id} unless it is leaving its Location (A10.51)";
            }

            if (enemy.Armed && seenBy.Contains(enemy.Id) && reader.Distance(enemy.Location, toLocation) is { } near && reader.Distance(enemy.Location, fromLocation) is { } far && near < far)
            {
                return $"play.rout-step: a routing unit never moves closer to the Known armed enemy unit {enemy.Id}, which has had it in its LOS (A10.51)";
            }
        }

        return null;
    }

    /// <summary>
    /// A10.533 (pass 35, task 35.4): a rout step into a Location holding concealed or hidden enemy units. With a real unit among them the routing unit
    /// is repulsed to the Location it came from, where its rout ends: the hidden units first go beneath a "?", and one real unit loses its "?", by
    /// Random Selection when there are several (A.9, as an ordinary entry draws: ruling R10.11). With Dummies alone, they are removed and the rout goes on.
    /// </summary>
    public static RoutRepulseVerdict RoutRepulse(IReadOnlyList<MoveRevealUnitFacts> unknownThere)
    {
        ArgumentNullException.ThrowIfNull(unknownThere);
        string[] real = [.. unknownThere.Where(unit => !unit.Dummy).OrderBy(unit => unit.Id, StringComparer.Ordinal).Select(unit => unit.Id)];
        return real.Length > 0
            ? new RoutRepulseVerdict(true, [.. unknownThere.Where(unit => unit.Hidden).Select(unit => unit.Id)], real, real.Length > 1, [])
            : new RoutRepulseVerdict(false, [], [], false, [.. unknownThere.Select(unit => unit.Id)]);
    }

    /// <summary>A10.533, A.9: the units that lose their "?" in repulsing a rout: the only real unit, or with dice the highest dr, ties all.</summary>
    public static IReadOnlyList<string> RoutRepulseShown(IReadOnlyList<string> pool, IReadOnlyList<int>? dice)
    {
        ArgumentNullException.ThrowIfNull(pool);
        return dice is null ? [.. pool.Take(1)] : [.. pool.Where((_, index) => dice[index] == dice.Take(pool.Count).Max())];
    }

    /// <summary>The Known armed enemy units with a clear LOS to a Location (A10.51), read as they are enumerated.</summary>
    public static IEnumerable<string> SeenBy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        return enemies.Where(item => item.Armed && reader.Los(item.Location, at) is { Clear: true }).Select(item => item.Id);
    }

    /// <summary>
    /// The half MF a routing unit pays to step between ADJACENT Locations (A10.5): the Infantry entry cost, without Bypass or Road Bonus, doubled for the
    /// first Location an Encircled unit enters (A7.7; ruling R12.11); or why it may not.
    /// </summary>
    public static RoutStepCost RoutEntry(IRoutFactReader reader, int fromLocation, int toLocation, bool encircledFirst)
    {
        ArgumentNullException.ThrowIfNull(reader);
        var (entry, reason) = reader.Entry(fromLocation, toLocation);
        return entry switch
        {
            null => new RoutStepCost(null, false, reason ?? $"play.rout-step: the step from {reader.Name(fromLocation)} to {reader.Name(toLocation)} is not one the game allows"),
            { MinimumMoveOnly: true } => new RoutStepCost(null, false, $"play.rout-step: {reader.Name(toLocation)} is entered only by Minimum Move, which a rout does not use (A10.5)"),
            { AllMf: true } => new RoutStepCost(null, true, null),
            _ => new RoutStepCost(entry.HalfMf * (encircledFirst ? 2 : 1), false, null),
        };
    }

    /// <summary>A10.51: a woods or building Location is a rout destination.</summary>
    public static bool RoutCover(RoutLocationFacts? read) => read is { } location && location.TerrainKey is "woods" or "wooden-building" or "stone-building";

    /// <summary>
    /// A10.5, A17.2: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three. One member for the planner and the projector (pass 35, the
    /// Rules boundary review; the pass 32 design's section 12, item 7); each hands over whether the unit is a SMC as it reads kinds.
    /// </summary>
    public static int RoutHalfMf(bool smc, bool wounded) => smc && wounded ? 6 : 12;

    /// <summary>
    /// The broken Morale Level (A10.4), one lower for a wounded SMC (A17.3) and one higher for a Fanatic unit (A10.8; pass 35, task 35.4), never above
    /// 10 (A.18), from the catalog; null for a unit the catalog does not know.
    /// </summary>
    public static int? BrokenMorale(bool fromCatalog, int? brokenMorale, int? morale, bool wounded, bool fanatic = false) =>
        fromCatalog && (brokenMorale ?? morale) is { } level ? ScenarioA1Definitions.MoraleCeiling(level - (wounded ? 1 : 0) + (fanatic ? 1 : 0)) : null;

    /// <summary>
    /// Casualty Reduction (A7.302, A17.11; pass 35, task 35.1): a squad with a HS becomes it; a SMC is wounded, and its Wound Severity dr says whether the
    /// wound is mortal (<see cref="ScenarioA1Wounds.Mortal"/>), a man already wounded staying wounded on a minor one; anything else is eliminated.
    /// </summary>
    public static CasualtyOutcome CasualtyReduction(bool squadWithHalfSquad, bool leaderOrHero, bool wounded, int? severityDr) =>
        squadWithHalfSquad ? CasualtyOutcome.Reduced
            : !ScenarioA1Wounds.SeverityDue(leaderOrHero) ? CasualtyOutcome.Eliminated
            : severityDr is { } dr ? ScenarioA1Wounds.Mortal(dr, wounded) ? CasualtyOutcome.Eliminated : CasualtyOutcome.Wounded
            : throw new ArgumentNullException(nameof(severityDr), "A SMC's Casualty Reduction needs its Wound Severity dr (A17.11).");

    /// <summary>A10.62 (ruling R13.1): a broken unit not under DM and not a prisoner can come under DM.</summary>
    public static bool DmCandidate(bool active, bool broken, bool desperationMorale, bool captured) => active && broken && !desperationMorale && !captured;

    /// <summary>A10.62 (ruling R13.1): whether a Known armed enemy unit is ADJACENT to a Location or in it.</summary>
    public static bool ArmedEnemyNear(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at) => ArmedEnemiesNear(reader, enemies, at).Any();

    /// <summary>A10.62 (ruling R13.1): why the broken units a plan's events put ADJACENT to a Known armed enemy unit come under DM.</summary>
    public static string AdjacentDmReason(IEnumerable<string> gaining)
    {
        ArgumentNullException.ThrowIfNull(gaining);
        return $"play.dm: {string.Join(", ", gaining)} come under DM from an ADJACENT Known armed enemy unit (A10.62)";
    }

    /// <summary>A10.62 (ruling R13.1): why a broken unit in Open Ground comes under DM as the RtPh starts.</summary>
    public static string RoutPhaseDmReason(string seen) => $"in Open Ground in the LOS and Normal Range of {seen}";

    /// <summary>
    /// Why a unit may not keep its DM as the RPh ends (A10.62; ruling R13.1), or null: it is a broken unit under DM, not in woods or a building; the
    /// Location is read only when the unit qualifies.
    /// </summary>
    public static string? RetainDmBar(string id, bool active, bool broken, bool desperationMorale, Func<bool> unplacedOrInCover)
    {
        ArgumentNullException.ThrowIfNull(unplacedOrInCover);
        return !active || !broken || !desperationMorale
            ? $"play.retain-dm: '{id}' is not a broken unit under DM (A10.62)"
            : unplacedOrInCover()
                ? $"play.retain-dm: {id} is in a woods or building Location, where DM is not retained (A10.62)"
                : null;
    }

    /// <summary>
    /// The least half MF, within the unit's allowance, to reach each Location from a start over legal rout steps (A10.51): the search follows which Known
    /// armed enemy units have had the unit in their LOS, since it never moves closer to them afterwards. An entry that costs all MF is a first step only.
    /// The Locations are reached in the order the dictionary lists them; a caller that asks for the routes is given one least-cost route to each.
    /// </summary>
    public static Dictionary<int, int> RoutReach(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int start, IEnumerable<string> seen, int spent, int limit,
        bool encircled, int? scenarioMonth, bool avoidInterdiction = false, Dictionary<int, int[]>? routes = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(seen);
        // Pass 31d (design D3): when the caller asks for the routes, the search keeps where each node was reached from, and gives one least-cost
        // route to each Location it reaches.
        var came = new Dictionary<(int, long), (int At, long Mask)>();
        var reached = new Dictionary<int, (int At, long Mask)>();
        string[] armed = [.. enemies.Where(item => item.Armed).Select(item => item.Id).Order(StringComparer.Ordinal)];
        var sight = new Dictionary<int, long>();
        long Mask(IEnumerable<string> ids) => ids.Select(id => Array.IndexOf(armed, id)).Where(index => index is >= 0 and < 63).Aggregate(0L, (mask, index) => mask | (1L << index));
        long Sight(int at) => sight.TryGetValue(at, out var mask) ? mask : sight[at] = Mask(SeenBy(reader, enemies, at));
        string[] Seen(long mask) => [.. armed.Where((_, index) => index < 63 && (mask & (1L << index)) != 0)];

        var bars = new Dictionary<(int, int, long), bool>();
        var interdicted = new Dictionary<(int, int), bool>();
        var entries = new Dictionary<(int, int, bool), RoutStepCost>();
        var best = new Dictionary<(int, long), int>();
        var reach = new Dictionary<int, int> { [start] = spent };
        var queue = new PriorityQueue<(int At, long Mask), int>();
        var first = (start, Mask(seen) | Sight(start));
        best[first] = spent;
        queue.Enqueue(first, spent);
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (cost > best[node])
            {
                continue;
            }

            // A2.1 (ruling R20.6; referee, pass 20): a rout never leaves the playable area, so its targets lie within it.
            foreach (var next in reader.Neighbors(node.At).Where(reader.Playable))
            {
                // Each LOS read is costly on a real board, so the search keeps what it has read.
                if (!bars.TryGetValue((node.At, next, node.Mask), out var barred))
                {
                    bars[(node.At, next, node.Mask)] = barred = RoutStepBar(reader, enemies, node.At, next, Seen(node.Mask)) is not null;
                }

                if (barred)
                {
                    continue;
                }

                if (avoidInterdiction && (interdicted.TryGetValue((node.At, next), out var open) ? open
                    : interdicted[(node.At, next)] = Interdictor(reader, enemies, next, scenarioMonth, node.At) is not null))
                {
                    continue;
                }

                var firstStep = encircled && cost == spent && node.At == start;
                if (!entries.TryGetValue((node.At, next, firstStep), out var entry))
                {
                    entries[(node.At, next, firstStep)] = entry = RoutEntry(reader, node.At, next, firstStep);
                }

                var (halfMf, allMf, _) = entry;
                int step;
                if (allMf)
                {
                    if (cost != 0)
                    {
                        continue;
                    }

                    step = limit;
                }
                else if (halfMf is { } value)
                {
                    step = value;
                }
                else
                {
                    continue;
                }

                var total = cost + step;
                if (total > limit)
                {
                    continue;
                }

                var key = (next, node.Mask | Sight(next));
                if (total < best.GetValueOrDefault(key, int.MaxValue))
                {
                    best[key] = total;
                    came[key] = node;
                    if (total < reach.GetValueOrDefault(next, int.MaxValue))
                    {
                        reached[next] = key;
                    }

                    reach[next] = Math.Min(reach.GetValueOrDefault(next, int.MaxValue), total);
                    queue.Enqueue(key, total);
                }
            }
        }

        if (routes is not null)
        {
            foreach (var (at, last) in reached)
            {
                var steps = new List<int>();
                for (var node = last; node != first && steps.Count <= limit; node = came[node])
                {
                    steps.Add(node.At);
                }

                steps.Reverse();
                routes[at] = [.. steps];
            }
        }

        return reach;
    }

    /// <summary>
    /// The woods and building Locations a rout may make for (A10.51, A10.532 EXC): the nearest in MF within its allowance, calculated at the start of its
    /// rout, and any no nearer than that which is no farther from a Known enemy unit than the start, which the unit may prefer; empty when none is reached.
    /// </summary>
    public static int[] RoutTargets(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int start, IReadOnlyDictionary<int, int> reach)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(reach);
        bool Ignorable(int at) => enemies.Any(enemy => reader.Distance(enemy.Location, at) is { } there && reader.Distance(enemy.Location, start) is { } here && there <= here);
        var covers = reach.Where(item => item.Key != start && RoutCover(reader.Location(item.Key))).ToArray();
        var binding = covers.Where(item => !Ignorable(item.Key)).ToArray();
        if (binding.Length == 0)
        {
            return [];
        }

        var nearest = binding.Min(item => item.Value);
        return [.. covers.Where(item => item.Value <= nearest).Select(item => item.Key)];
    }

    /// <summary>Whether a broken unit has any legal rout step (A10.5): the search reaches a Location beyond its own.</summary>
    public static bool CanRout(int reachedCount) => reachedCount > 1;

    /// <summary>
    /// Whether a broken unit ADJACENT to its captors can get away from every Known unbroken armed enemy unit only by Interdiction or Low Crawl (A20.21):
    /// no Location within its MF, reached without entering an Open Ground Location an enemy unit could Interdict, is clear of them.
    /// </summary>
    public static bool TrappedByInterdiction(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int start, IReadOnlyDictionary<int, int> reachAvoidingInterdiction)
    {
        ArgumentNullException.ThrowIfNull(reachAvoidingInterdiction);
        return !reachAvoidingInterdiction.Keys.Any(at => at != start && NearUnbrokenArmedEnemy(reader, enemies, at) is null);
    }

    /// <summary>
    /// What a player needs before routing a broken unit (A10.5, A10.51; table player, pass 13): the woods or building Locations it must reach, whether it
    /// has any legal step, and one least-cost route to each place the rout may end (pass 31d, design D3). A place the unit may not end in, since it began
    /// ADJACENT to a Known armed enemy unit that is ADJACENT to that place too (A10.51), is given no route; nor is a way that leaves cover again.
    /// </summary>
    public static (IReadOnlyList<int> Targets, bool CanRout, IReadOnlyDictionary<int, IReadOnlyList<int>> Routes) RoutAdvice(IRoutFactReader reader,
        IReadOnlyList<RoutEnemyFacts> enemies, int at, IReadOnlyDictionary<int, int> reach, IReadOnlyDictionary<int, int[]> found)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(reach);
        ArgumentNullException.ThrowIfNull(found);
        var targets = RoutTargets(reader, enemies, at, reach);
        var near = ArmedEnemiesNear(reader, enemies, at).ToArray();
        // A least-cost way to one place may run through another and out into the open again, which a rout may not do once it has reached woods or a
        // building (A10.51): such a way is not offered (found by the sweep of The Tractor Works, pass 31d).
        bool Holds(int[] route) => Array.FindIndex(route, targets.Contains) is var reached && route.Skip(reached + 1).All(step => RoutCover(reader.Location(step)));
        return (targets, CanRout(reach.Count), targets.Where(target => found.TryGetValue(target, out var route) && Holds(route) && !near.Any(item => AdjacentOrSame(reader, item.Location, target)))
            .ToDictionary(target => target, target => (IReadOnlyList<int>)found[target]));
    }

    /// <summary>A10.5: broken units rout in the RtPh.</summary>
    public static string? RoutPhaseBar(string? phase) => phase != "rtph" ? "play.rout-phase: broken units rout in the RtPh (A10.5)" : null;

    /// <summary>A10.5: a rout is by a broken unit outside Melee that is not a prisoner.</summary>
    public static bool RoutUnitAllowed(bool broken, bool melee, bool captured) => broken && !melee && !captured;

    /// <summary>A10.5: why a unit may not rout: it is not a broken unit on the map outside Melee.</summary>
    public static string RoutUnitText(string unitId) => $"play.rout-unit: '{unitId}' is not a broken unit on the map outside Melee (A10.5)";

    /// <summary>A10.5: a unit routs once a RtPh.</summary>
    public static string? RoutedBar(string unitId, bool routedThisPhase) => routedThisPhase ? $"play.rout-unit: {unitId} has routed this RtPh (A10.5)" : null;

    /// <summary>A10.53: a pinned unit routs no further.</summary>
    public static string? RoutPinnedBar(string unitId, bool pinned) => pinned ? $"play.rout-unit: {unitId} is pinned and routs no further this RtPh (A10.53)" : null;

    /// <summary>A10.5: a unit that need not rout and is not under DM may not rout.</summary>
    public static string? MayRoutBar(string unitId, bool mayRout) => mayRout ? null : $"play.rout-not-allowed: {unitId} need not rout and is not under DM, so it may not rout (A10.5)";

    /// <summary>A10.5: the ATTACKER's broken units rout first, one at a time, then the DEFENDER's; the DEFENDER's routs are read only for an ATTACKER's unit.</summary>
    public static string? RoutOrderBar(bool attackerUnit, Func<bool> defenderBegan)
    {
        ArgumentNullException.ThrowIfNull(defenderBegan);
        return attackerUnit && defenderBegan() ? "play.rout-order: the DEFENDER's units have begun to rout, so the ATTACKER's may not (A10.5)" : null;
    }

    /// <summary>
    /// A10.5, A20.21 (pass 35, task 35.17): whether a broken unit still has a rout to make this RtPh: it has not routed, is not pinned, must rout, has a
    /// legal step, and does not surrender instead of routing. A unit bound to surrender (A20.21: it "will surrender ... instead") owes no rout, so it
    /// holds up neither the rout order nor the phase's end. The last three are read lazily, in that order.
    /// </summary>
    public static bool RoutStillOwed(bool routedThisPhase, bool pinned, Func<bool> mustRout, Func<bool> canRout, Func<bool> surrendersInstead)
    {
        ArgumentNullException.ThrowIfNull(mustRout);
        ArgumentNullException.ThrowIfNull(canRout);
        ArgumentNullException.ThrowIfNull(surrendersInstead);
        return !routedThisPhase && !pinned && mustRout() && canRout() && !surrendersInstead();
    }

    /// <summary>A10.5: an ATTACKER's unit that still owes a rout (<see cref="RoutStillOwed"/>) routs before the DEFENDER's.</summary>
    public static bool AttackerMustRoutFirst(bool ofAttacker, bool routedThisPhase, bool pinned, Func<bool> mustRout, Func<bool> canRout, Func<bool> surrendersInstead) =>
        ofAttacker && RoutStillOwed(routedThisPhase, pinned, mustRout, canRout, surrendersInstead);

    /// <summary>A10.5: the DEFENDER's unit waits for the ATTACKER's.</summary>
    public static string AttackerFirstText(string firstId) => $"play.rout-order: the ATTACKER's {firstId} must rout first (A10.5)";

    /// <summary>E1.54 (backlog pass 16, ruling R16.6): at night a broken unit always Low Crawls, and surrenders only in CC.</summary>
    public static string? NightRoutBar(string unitId, bool night, bool lowCrawl) =>
        night && !lowCrawl ? $"play.night-rout: at night {unitId} does not rout normally but Low Crawls (lowCrawl) (E1.54)" : null;

    /// <summary>
    /// A20.21: a unit surrenders to ADJACENT captors by day, unless Fanatic, under No Quarter, or a Commissar (A25.22; pass 35, task 35.4), who never
    /// surrenders by the RtPh method. The other kinds the rule names (Partisans, Gurkhas, SS facing Russians, Japanese) have no counters.
    /// </summary>
    public static bool SurrenderCandidate(bool night, bool fanatic, bool noQuarter, bool commissar = false) => !night && !fanatic && !noQuarter && !commissar;

    /// <summary>A20.21: why a unit ADJACENT to its captors surrenders instead of routing: Disrupted, Encircled, or trapped (read last), or null.</summary>
    public static string? SurrenderCause(bool disrupted, bool encircled, Func<bool> trappedByInterdiction, IReadOnlyList<string> captors)
    {
        ArgumentNullException.ThrowIfNull(trappedByInterdiction);
        return disrupted ? "is Disrupted" : encircled ? "is Encircled"
            : trappedByInterdiction() ? $"can get away from {string.Join(" or ", captors)} only by Interdiction or Low Crawl" : null;
    }

    /// <summary>
    /// A19.12 (p. 86), A20.21, and the Comprehensive Rout Example (p. 69): whether a unit bound to surrender has done so before the other side's units
    /// rout. A Disrupted unit surrenders "at the start of any RtPh", before any rout; an ATTACKER's unit surrenders in its own turn among the
    /// ATTACKER's routs, which all come before the DEFENDER's. A DEFENDER's unit that is not Disrupted still stands while the ATTACKER routs. The
    /// surrender is recorded as the phase ends (ruling R35.2); for the other side's routes the unit is a prisoner from the moment the page has it
    /// surrender. Whether it is bound to surrender is read last.
    /// </summary>
    public static bool SurrenderedBeforeTheOtherSideRouts(bool disrupted, bool attacker, Func<bool> boundToSurrender)
    {
        ArgumentNullException.ThrowIfNull(boundToSurrender);
        return (disrupted || attacker) && boundToSurrender();
    }

    /// <summary>
    /// What the Rout panel says of where a broken unit's rout may end (A10.5, A10.51; pass 35, the Rules boundary review): no legal step; places
    /// within reach that all lie where it may not end, so that the route passes through them; the places it must end in; or none within reach.
    /// The places come worded, each with the way to it.
    /// </summary>
    public static string RoutEndAdvice(bool canRout, bool must, IReadOnlyList<string> places, bool anyPlaceMayEndTheRout)
    {
        ArgumentNullException.ThrowIfNull(places);
        return !canRout
            ? must ? ". It has no legal rout step, so it is eliminated for Failure to Rout, or surrenders, when the RtPh ends (A10.5)." : ". It has no legal rout step, so it stays where it is."
            : places.Count > 0 && !anyPlaceMayEndTheRout
                ? $". It may not end its rout in or ADJACENT to the Location of the enemy unit it began with or beside (A10.5, A10.51): its route passes through {string.Join(" or ", places)} and ends in woods or a building beyond."
            : places.Count > 0 ? $". Its route must end in {string.Join(" or ", places)}."
            : ". No woods or building is within its reach, so any legal route will do.";
    }

    /// <summary>A20.21: the refusal of a rout by a unit that surrenders instead.</summary>
    public static string RoutSurrenderText(string unitId, string cause, IReadOnlyList<string> captors) =>
        $"play.rout-surrender: {unitId} {cause}, so it surrenders to {string.Join(" or ", captors)} as the RtPh ends instead of routing (A20.21)";

    /// <summary>A20.21 (pass 35, task 35.17): what the Rout panel says of a unit that surrenders instead of routing; it is offered no route.</summary>
    public static string RoutSurrenderAdvice(string unitId, string cause, IReadOnlyList<string> captors) =>
        $"{unitId} does not rout: it {cause}, so it surrenders to {string.Join(" or ", captors)} as the RtPh ends ({(cause == "is Disrupted" ? "A19.12, " : string.Empty)}A20.21).";

    /// <summary>A19.12 (pass 35, task 35.2): a Disrupted unit may not use Low Crawl, but at night (E1.54).</summary>
    public static string? DisruptedLowCrawlBar(string unitId, bool disrupted, bool lowCrawl, bool night) =>
        disrupted && lowCrawl && !night ? $"play.rout-low-crawl: {unitId} is Disrupted and may not use Low Crawl; it routs normally if it must rout, and otherwise stays (A19.12)" : null;

    /// <summary>
    /// A10.4 (ruling R31d.1): the load a laden unit routs with: the one named, among the best loads; or the only choice when none is named; or null.
    /// </summary>
    public static IReadOnlyList<string>? ChosenLoad(IReadOnlyList<string>? named, IReadOnlyList<IReadOnlyList<string>> choices, IReadOnlyList<IReadOnlyList<string>> bestLoads)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(bestLoads);
        return named is null ? (choices.Count == 1 ? choices[0] : null)
            : bestLoads.FirstOrDefault(best => best.Order(StringComparer.Ordinal).SequenceEqual(named, StringComparer.Ordinal));
    }

    /// <summary>A10.4: the owner chooses among loads of equal PP.</summary>
    public static string RoutLadenText(string unitId, int total, int ipc, IReadOnlyList<IReadOnlyList<string>> choices, Func<string, int> pp)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(pp);
        var loads = string.Join(", or ", choices.Select(best => best.Count == 0 ? "no SW" : string.Join(" with ", best.Select(id => $"{id} ({pp(id)} PP)"))));
        return $"play.rout-laden: {unitId} carries {total} PP and routs with at most {ipc} PP; its owner chooses what it keeps: {loads}; it leaves the rest (A10.4)";
    }

    /// <summary>A10.52: a rout names at least one Location, and Low Crawl exactly one.</summary>
    public static string? RouteShapeBar(int routeCount, bool lowCrawl) =>
        routeCount == 0 || (lowCrawl && routeCount != 1) ? "play.rout-route: a rout names at least one Location, and Low Crawl exactly one (A10.52)" : null;

    /// <summary>A10.52: Low Crawl does not leave an enemy-occupied Location by day; the occupants are read lazily.</summary>
    public static string? LowCrawlOccupiedBar(bool lowCrawl, bool night, Func<bool> enemyOccupied)
    {
        ArgumentNullException.ThrowIfNull(enemyOccupied);
        return lowCrawl && !night && enemyOccupied() ? "play.rout-low-crawl: Low Crawl does not leave an enemy-occupied Location (A10.52)" : null;
    }

    /// <summary>
    /// A10.51: the walk of a rout's route: each step ADJACENT and allowed, Low Crawl's limits, the entry cost, an entry that takes all MF as the one step,
    /// the MF limit; the Known armed enemy units that see each step join the seen set. The costs of the steps walked are given with the first refusal.
    /// </summary>
    public static (string? Refusal, IReadOnlyList<int> Costs, int Spent) RoutRouteWalk(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, string unitId, int start,
        IReadOnlyList<int> route, bool lowCrawl, bool night, int limit, bool encircled, HashSet<string> seenBy)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(seenBy);
        var costs = new List<int>();
        var spent = 0;
        var from = start;
        foreach (var next in route)
        {
            if (!reader.Neighbors(from).Contains(next))
            {
                return ($"play.rout-route: {reader.Name(next)} is not ADJACENT to {reader.Name(from)}", costs, spent);
            }

            if (RoutStepBar(reader, enemies, from, next, seenBy) is { } bar)
            {
                return (bar, costs, spent);
            }

            if (lowCrawl && !night && reader.Location(next) is { TerrainKey: "marsh" })
            {
                return ("play.rout-low-crawl: Low Crawl does not enter marsh (A10.52)", costs, spent);
            }

            var (halfMf, allMf, why) = RoutEntry(reader, from, next, encircled && spent == 0);
            if (halfMf is null && !allMf)
            {
                return (why!, costs, spent);
            }

            if (allMf && spent > 0)
            {
                return ($"play.rout-route: entering {reader.Name(next)} takes all of {unitId}'s MF, so it is its only step (A4.134)", costs, spent);
            }

            var cost = lowCrawl || allMf ? limit - spent : halfMf!.Value;
            spent += cost;
            if (spent > limit)
            {
                return ($"play.rout-mf: the route costs more than {unitId}'s {limit / 2} MF in the RtPh (A10.5)", costs, spent);
            }

            costs.Add(cost);
            seenBy.UnionWith(SeenBy(reader, enemies, next));
            from = next;
        }

        return (null, costs, spent);
    }

    /// <summary>
    /// A10.51, A10.532: a unit ADJACENT to a Known armed enemy unit does not end its rout ADJACENT to that same unit; it must reach the nearest woods or
    /// building Location within its MF this RtPh, not necessarily by a shortest route, and go on only into cover; with none, any legal route. Low Crawl
    /// moves one Location toward it (A10.52).
    /// </summary>
    public static string? RoutDestinationBar(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, string unitId, int start, IReadOnlyList<int> route, bool lowCrawl,
        int limit, bool encircled, int? scenarioMonth)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(route);
        var near = ArmedEnemiesNear(reader, enemies, start).ToArray();
        if (near.FirstOrDefault(item => AdjacentOrSame(reader, item.Location, route[^1])) is { } still)
        {
            return $"play.rout-route: {unitId} began ADJACENT to {still.Id} and may not end its rout ADJACENT to it (A10.51)";
        }

        var startSeen = SeenBy(reader, enemies, start).ToArray();
        var targets = RoutTargets(reader, enemies, start, RoutReach(reader, enemies, start, startSeen, 0, limit, encircled, scenarioMonth));
        if (targets.Length > 0)
        {
            if (lowCrawl)
            {
                var whole = RoutReach(reader, enemies, start, startSeen, 0, 4 * limit, encircled, scenarioMonth);
                var after = RoutReach(reader, enemies, route[0], startSeen.Concat(SeenBy(reader, enemies, route[0])), 0, 4 * limit, encircled, scenarioMonth);
                if (!targets.Any(target => after.TryGetValue(target, out var rest) && whole.TryGetValue(target, out var all) && rest < all))
                {
                    return $"play.rout-destination: Low Crawl moves toward the nearest woods or building Location, {string.Join(" or ", targets.Select(reader.Name))} (A10.52)";
                }
            }
            else
            {
                var reached = route.ToList().FindIndex(targets.Contains);
                if (reached < 0)
                {
                    return $"play.rout-destination: {unitId} must rout to the nearest woods or building Location, {string.Join(" or ", targets.Select(reader.Name))} (A10.51)";
                }

                if (route.Skip(reached + 1).Where(step => !RoutCover(reader.Location(step))).Select(step => (int?)step).FirstOrDefault() is { } beyond)
                {
                    return $"play.rout-destination: having reached {reader.Name(route[reached])}, {unitId} goes on only into woods or building Locations, not {reader.Name(beyond)} (A10.51)";
                }
            }
        }

        return null;
    }

    /// <summary>A10.5: a leader wounded on the way routs on only with the MF a wounded SMC has; a step beyond them ends the rout.</summary>
    public static bool StepExceedsMf(int used, int cost, int halfMf) => used + cost > halfMf;

    /// <summary>A10.53: Interdiction as a unit enters an Open Ground hex without Low Crawl, once per hex; the Interdictor is read only then.</summary>
    public static string? InterdictionDue(bool lowCrawl, bool firstEntryOfHex, Func<string?> interdictor)
    {
        ArgumentNullException.ThrowIfNull(interdictor);
        return lowCrawl || !firstEntryOfHex ? null : interdictor();
    }

    /// <summary>A10.53: the Morale Level an Interdiction NMC is taken against; a unit the catalog does not know has none.</summary>
    public static int InterdictionMorale(int? brokenMorale) => brokenMorale ?? 0;

    /// <summary>A10.53, A10.31: what an Interdiction result does to the routing unit: it routs on, is pinned and routs no further, is Casualty Reduced, or is eliminated.</summary>
    public static InterdictionOutcome InterdictionEffect(string result) =>
        result == ScenarioA1ResultTables.InterdictionPassed ? InterdictionOutcome.Passed
            : result == ScenarioA1ResultTables.InterdictionPinned ? InterdictionOutcome.Pinned
            : result == ScenarioA1ResultTables.InterdictionReduced ? InterdictionOutcome.Reduced
            : InterdictionOutcome.Eliminated;

    /// <summary>A10.53: a HS left by Casualty Reduction, or a wounded SMC, routs on; an eliminated unit does not.</summary>
    public static bool RoutsOn(bool eliminated) => !eliminated;

    /// <summary>
    /// The plan's words (A10.52, A10.53, A10.4, A4.431): the steps and their costs, which steps an enemy unit could Interdict, and what is left behind.
    /// </summary>
    public static string[] RoutSummary(string unitId, bool lowCrawl, IReadOnlyList<(string Step, int HalfMf)> steps, IReadOnlyList<(string Step, string By)> threatened,
        IReadOnlyList<(string Weapon, int Pp)> left, IReadOnlyList<string> kept, string start)
    {
        ArgumentNullException.ThrowIfNull(steps);
        ArgumentNullException.ThrowIfNull(threatened);
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(kept);
        var route = string.Join(", ", steps.Select(item => $"{item.Step} ({(item.HalfMf / 2.0).ToString("0.#", CultureInfo.InvariantCulture)} MF)"));
        string[] threats = [.. threatened.Select(item => $"{item.Step} by {item.By}")];
        var interdiction = lowCrawl ? "Low Crawl is never Interdicted (A10.52)"
            : threats.Length == 0 ? "no step enters Open Ground an enemy unit could Interdict (A10.53)"
            : $"Interdicted as it enters {string.Join(", ", threats)}, a NMC each (A10.53)";
        string[] leaves = left.Count == 0 ? []
            : [$"play.rout-leaves: {unitId} leaves {string.Join(" and ", left.Select(item => $"{item.Weapon} ({item.Pp} PP)"))} in {start}, unpossessed, and routs with "
                + $"{(kept.Count == 0 ? "no SW" : string.Join(" and ", kept))} (A10.4, A4.431)"];
        return [$"play.rout: {unitId} routs{(lowCrawl ? " by Low Crawl" : "")} to {route}; {interdiction}", .. leaves];
    }

    /// <summary>E1.54 (backlog pass 16, ruling R16.6): no unit is eliminated for Failure to Rout at night.</summary>
    public static bool NoFailureToRout(bool night) => night;

    /// <summary>A10.5, A10.53: Failure to Rout falls on a broken Personnel unit not in Melee, not a prisoner, and not aboard a vehicle.</summary>
    public static bool FailureToRoutCandidate(bool active, bool broken, bool melee, bool captured, bool vehicle, bool aboard) =>
        active && broken && !melee && !captured && !vehicle && !aboard;

    /// <summary>
    /// Why the end of the RtPh eliminates a broken unit for Failure to Rout (A10.5, A10.53; ruling R13.3), or null: ADJACENT to or in the Location of a
    /// Known unbroken armed enemy unit; or, having not routed and not pinned, in Open Ground in the LOS and Normal Range of a Known enemy unit.
    /// </summary>
    public static string? FailureToRoutWhy(IRoutFactReader reader, IReadOnlyList<RoutEnemyFacts> enemies, int at, bool routedThisPhase, bool pinned, int? scenarioMonth) =>
        NearUnbrokenArmedEnemy(reader, enemies, at) is { } enemy ? $"it is ADJACENT to or in the Location of the Known unbroken armed enemy unit {enemy}"
            : !routedThisPhase && !pinned && ExposedInOpenGround(reader, enemies, at, scenarioMonth) is { } seen
                ? $"it did not rout from Open Ground in the LOS and Normal Range of {seen}" : null;

    /// <summary>
    /// A20.21: a unit with captors surrenders instead of failing to rout, unless Fanatic, under No Quarter, already rejected this phase, or a Commissar
    /// (A25.22; pass 35, task 35.4), who is eliminated rather than surrender. A unit repulsed this RtPh from a concealed unit's Location (A10.533,
    /// p. 68) is eliminated too: its enemy takes it prisoner only by giving up its "?" before the rout enters.
    /// </summary>
    public static bool SurrendersInstead(bool fanatic, bool noQuarter, bool rejected, bool commissar = false, bool repulsed = false) =>
        !fanatic && !noQuarter && !rejected && !commissar && !repulsed;

    /// <summary>A20.551 (ruling R31.8): a SMC that is free (no Custodian, not captured) and still Unarmed is Armed again.</summary>
    public static bool FreedUnarmedSmc(bool active, bool noCustodian, bool smc, bool unarmed, bool captured) => active && noCustodian && smc && unarmed && !captured;

    /// <summary>A20.551 (ruling R31.8): a plan's events are followed by the arming, unless there are none or the last ends the game or awaits a choice or a surrender.</summary>
    public static bool ArmsFreedSmc(int eventCount, bool lastEndsOrWaits) => eventCount > 0 && !lastEndsOrWaits;

    /// <summary>A20.551 (ruling R31.8): why the freed SMC are Armed again.</summary>
    public static string SmcArmedReason(IReadOnlyList<string> freed)
    {
        ArgumentNullException.ThrowIfNull(freed);
        return $"play.smc-armed: {string.Join(", ", freed)} {(freed.Count == 1 ? "is" : "are")} free and so Armed again (A20.551; ruling R31.8)";
    }
}
