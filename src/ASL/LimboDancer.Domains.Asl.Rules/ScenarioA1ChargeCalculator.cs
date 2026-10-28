namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The berserk charge's search (A15.43, A15.431, A15.45, A4.3, A4.31; rulings R27.2, R30.5, R72.3; pass 32.g, S6): the Known enemy units a charge seeks, the
/// moves out of a node of its route, one search by Dijkstra for the first moves of every shortest route to every Known enemy Location, and the steps the stack
/// may take toward the nearest Known enemy unit in its LOS it can reach. Play reads the state and the map through <see cref="IChargeFactReader"/>, with Locations
/// as indexes into its table, and marks the steps.
/// </summary>
public static class ScenarioA1ChargeCalculator
{
    /// <summary>The code of the verdict that no Known enemy unit in LOS can be reached past rule bars alone (A15.45, A15.44; pass 72, ruling R72.4).</summary>
    public const string UnreachableCode = "play.charge-unreachable";

    /// <summary>The key of the owner's choice of Battle Hardening for a berserk unit that can reach no Known enemy unit (A15.44, A15.3; ruling R72.4).</summary>
    public const string HardeningChoice = "berserkHardening";

    /// <summary>B21.1 (p. 134): the VASL terrains of a Water Obstacle (a pond, river, canal, lake, or ocean); a stream is none (B20).</summary>
    private static readonly HashSet<string> WaterObstacles = new(StringComparer.Ordinal) { "Water", "Shallow Water", "River", "Shallow River", "Canal", "Ocean", "Shallow Ocean" };

    /// <summary>The movement model's refusals that are rules, by code: a cliff hexside (A15.45), and Locations the building rules do not connect (B23.4, B23.42).</summary>
    private static readonly string[] BarCodes = ["play.move-cliff:", "play.move-level:", "play.move-stairwell:", "play.move-upper-level:"];

    /// <summary>A15.43 (table player, pass 27): a Known enemy unit a charge may seek; an Abandoned vehicle is no unit to charge.</summary>
    public static bool ChargeTarget(bool known, bool vehicle, bool abandoned) => known && !(vehicle && abandoned);

    /// <summary>A4.5, A15.43: the half MF a unit has left this MPh, from its allotment (none when it has no allotment), the MF spent, and a half MF spent.</summary>
    public static int HalfMfLeft(int? allotment, int mfSpent, bool halfMfSpent) => allotment is { } allowance ? (allowance * 2) - (mfSpent * 2) - (halfMfSpent ? 1 : 0) : 0;

    /// <summary>A15.43: whether a berserk unit can afford a charge step with the half MF it has left.</summary>
    public static bool Affordable(int allotment, int mfSpent, bool halfMfSpent, int halfMf) => HalfMfLeft(allotment, mfSpent, halfMfSpent) >= halfMf;

    /// <summary>
    /// Whether a refused charge step is barred by a rule (pass 72, ruling R72.4), not by a gap of the model: a cliff hexside or a Water Obstacle, which a berserk
    /// unit never charges through (A15.45, p. 84; B21.1, p. 134), or Locations the building rules do not connect (B23.4, B23.42, B23.8, B23.41). Any other
    /// refusal (terrain "not reviewed", ruling R10.1; a map the model cannot read) is a gap.
    /// </summary>
    public static bool RuleBar(ChargeEntryFacts entry)
    {
        if (entry.HalfMf is not null)
        {
            return false;
        }

        return (entry.Refusal is { } refusal && BarCodes.Any(code => refusal.StartsWith(code, StringComparison.Ordinal)))
            || (entry.Terrain is { } name && (WaterObstacles.Contains(name) || name.Contains("Pond", StringComparison.Ordinal) || name is "Rooftop" or "Cellar"));
    }

    /// <summary>
    /// The moves out of a node of a charge's route (A15.431, A4.3, A4.31; ruling R27.2): from a Location, each step the movement rules allow and each
    /// Bypass of an ADJACENT woods or building hex along one or two of its hexsides; from Bypass, the steps out through the far vertex. Each step the
    /// movement rules refuse is handed to <paramref name="refused"/>.
    /// </summary>
    public static IEnumerable<(ChargeNodeFacts Node, ChargeMoveFacts Move)> Edges(IChargeFactReader reader, ChargeNodeFacts node, Action<ChargeEntryFacts>? refused = null)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (node.Lane is { } key)
        {
            var lane = reader.Lane(key);
            var turn = (lane[0] - node.Entered!.Value + 6) % 6;
            var far = (lane[^1] + turn) % 6;
            foreach (var side in new[] { lane[^1], far }.Distinct())
            {
                if (reader.Across(node.At, side) is not { } exit || reader.Level(exit) != 0 || !reader.Playable(exit))
                {
                    continue;
                }

                var entry = reader.EntryCost(node.At, exit);
                if (entry.HalfMf is { } cost)
                {
                    yield return (new ChargeNodeFacts(exit, null, null), new ChargeMoveFacts(exit, null, cost));
                }
                else
                {
                    refused?.Invoke(entry);
                }
            }

            yield break;
        }

        foreach (var next in reader.Neighbors(node.At).Where(reader.Playable))
        {
            var entry = reader.EntryCost(node.At, next);
            if (entry.HalfMf is { } cost)
            {
                yield return (new ChargeNodeFacts(next, null, null), new ChargeMoveFacts(next, null, cost));
            }
            else
            {
                refused?.Invoke(entry);
            }

            if (reader.Level(node.At) != 0 || reader.Level(next) != 0 || !reader.Reads(node.At) || reader.SideToward(next, node.At) is not { } side)
            {
                continue;
            }

            foreach (var turn in new[] { 1, 5 })
            {
                foreach (var length in new[] { 1, 2 })
                {
                    List<int> lane = [.. Enumerable.Range(1, length).Select(index => (side + (turn * index)) % 6)];
                    if (reader.BypassHalfMf(next, side, lane, node.At) is { } halfMf)
                    {
                        yield return (new ChargeNodeFacts(next, side, reader.LaneKey(lane)), new ChargeMoveFacts(next, reader.LaneKey(lane), halfMf));
                    }
                }
            }
        }
    }

    /// <summary>
    /// One search from a node over the charge's moves (A15.431; rulings R27.2, R72.3): by Dijkstra in half MF, the first moves of every shortest route to each
    /// node, the fewest hexes stepped on one, and the least cost at which a rule bar (<see cref="RuleBar"/>) or a gap of the model refused a step. The
    /// <paramref name="terminals"/> (every Known enemy Location) are entered, never passed through.
    /// </summary>
    private static Search SearchFrom(IChargeFactReader reader, ChargeNodeFacts start, HashSet<int> terminals)
    {
        var search = new Search(start);
        var done = new HashSet<ChargeNodeFacts>();
        var queue = new PriorityQueue<ChargeNodeFacts, int>();
        queue.Enqueue(start, 0);
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (!done.Add(node) || (node != start && node.Lane is null && terminals.Contains(node.At)))
            {
                continue;
            }

            void Refused(ChargeEntryFacts entry)
            {
                if (RuleBar(entry))
                {
                    search.BarCost ??= cost;
                }
                else
                {
                    search.GapCost ??= cost;
                }
            }

            foreach (var (next, move) in Edges(reader, node, Refused))
            {
                var total = cost + Math.Max(1, move.HalfMf);
                var known = search.Best.GetValueOrDefault(next, int.MaxValue);
                if (total > known || (total == known && done.Contains(next)))
                {
                    continue;
                }

                HashSet<ChargeMoveFacts> via = node == start ? [move] : search.Firsts[node];
                var hexes = search.Hexes[node] + (reader.Distance(node.At, next.At) == 0 ? 0 : 1);
                if (total < known)
                {
                    search.Best[next] = total;
                    search.Firsts[next] = [.. via];
                    search.Hexes[next] = hexes;
                    queue.Enqueue(next, total);
                }
                else
                {
                    search.Firsts[next].UnionWith(via);
                    search.Hexes[next] = Math.Min(search.Hexes[next], hexes);
                }
            }
        }

        return search;
    }

    /// <summary>The answer of <see cref="SearchFrom"/>.</summary>
    private sealed class Search(ChargeNodeFacts start)
    {
        public Dictionary<ChargeNodeFacts, int> Best { get; } = new() { [start] = 0 };

        public Dictionary<ChargeNodeFacts, HashSet<ChargeMoveFacts>> Firsts { get; } = new() { [start] = [] };

        public Dictionary<ChargeNodeFacts, int> Hexes { get; } = new() { [start] = 0 };

        public int? BarCost
        {
            get; set;
        }

        public int? GapCost
        {
            get; set;
        }

        /// <summary>The first moves of the shortest routes to a Location, or null when none reaches it.</summary>
        public HashSet<ChargeMoveFacts>? FirstMoves(int target) =>
            Firsts.TryGetValue(new ChargeNodeFacts(target, null, null), out var found) && found.Count > 0 ? found : null;

        public int Cost(int target) => Best[new ChargeNodeFacts(target, null, null)];

        public int HexesTo(int target) => Hexes[new ChargeNodeFacts(target, null, null)];
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431, A15.45; rulings R27.2, R72.3): one search reads the
    /// first move of each shortest route in MF to every Known enemy Location, over the entries the game allows, Bypass lanes, stairwells, and upper levels,
    /// never through another Location holding enemy units. The targets are taken in groups by range in hexes, the nearest first; when no route reaches any
    /// unit of a group, the next group is charged (A15.45). Past a rule bar the charge stays on a unit of the group only while its route, counted in hexes
    /// stepped, is no longer than the next group's range (a tie stays). Equidistant targets and routes are the ATTACKER's choice, so their steps are all allowed.
    /// With no Known enemy unit in LOS, the charge keeps to the Location it charged (<paramref name="previous"/>). <paramref name="start"/> gives the stack's
    /// node when a route is searched: its Location, or its Bypass lane (A4.32). When no target can be reached, the verdict is
    /// <see cref="UnreachableCode"/> if only rule bars refused a step (A15.44, ruling R72.4), else the charge is undecided and ends in place (ruling R30.5).
    /// </summary>
    public static (IReadOnlyDictionary<int, ChargeSearchStep> Steps, int? Target, string? Undecided) Steps(IChargeFactReader reader, int from, IReadOnlyList<int> enemies,
        int? previous, Func<ChargeNodeFacts> start)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(enemies);
        ArgumentNullException.ThrowIfNull(start);
        var none = new Dictionary<int, ChargeSearchStep>();
        if (enemies.Contains(from))
        {
            return (none, from, null);
        }

        var inLos = enemies.Where(location => reader.LosClear(from, location)).ToArray();

        // A15.431: a charge keeps to its Location until a closer Known enemy unit comes into its LOS (ruling R30.5).
        if (previous is { } charged && reader.Distance(from, charged) is { } kept)
        {
            var closer = inLos.Select(location => (location, Distance: reader.Distance(from, location))).Where(item => item.Distance < kept).ToArray();
            inLos = [.. closer.Select(item => item.location)];
        }

        var distances = inLos.Select(location => (location, Distance: reader.Distance(from, location))).ToArray();
        if (distances.Any(item => item.Distance is null))
        {
            return (none, null, "play.charge-undecided: a distance to a Known enemy unit cannot be read");
        }

        List<(int Range, int[] Targets)> groups = [.. distances.GroupBy(item => item.Distance!.Value).OrderBy(group => group.Key)
            .Select(group => (group.Key, group.Select(item => item.location).ToArray()))];
        if (previous is { } held && !inLos.Contains(held))
        {
            groups.Add((reader.Distance(from, held) ?? int.MaxValue, [held]));
        }

        if (groups.Count == 0)
        {
            return (none, null, null);
        }

        // Ruling R72.3: one search for every target; A4.32 (ruling R27.2): a stack in Bypass starts from its lane.
        HashSet<int> terminals = [.. enemies];
        if (previous is { } goal)
        {
            terminals.Add(goal);
        }

        var search = SearchFrom(reader, start(), terminals);
        for (var index = 0; index < groups.Count; index++)
        {
            var next = index + 1 < groups.Count ? groups[index + 1].Range : (int?)null;

            // A15.45: past a rule bar met before the target, an alternate route stays on it only while no longer in hexes than the next nearest's range.
            int[] reached = [.. groups[index].Targets.Where(target => search.FirstMoves(target) is not null
                && (next is not { } range || search.BarCost is not { } bar || bar >= search.Cost(target) || search.HexesTo(target) <= range))];
            if (reached.Length == 0)
            {
                continue;
            }

            var steps = new Dictionary<int, ChargeSearchStep>();
            foreach (var target in reached)
            {
                foreach (var move in search.FirstMoves(target)!.OrderBy(item => reader.Name(item.To), StringComparer.Ordinal))
                {
                    var known = steps.GetValueOrDefault(move.To);
                    IReadOnlyList<string> lanes = known?.Lanes ?? [];
                    if (move.Lane is { } key && !lanes.Contains(key))
                    {
                        lanes = [.. lanes, key];
                    }

                    steps[move.To] = new ChargeSearchStep(known?.Target ?? target, Math.Min(known?.HalfMf ?? int.MaxValue, move.HalfMf), (known?.Plain ?? false) || move.Lane is null, lanes);
                }
            }

            return (steps, reached.Length == 1 ? reached[0] : null, null);
        }

        var nearest = groups[0].Targets[0];
        return (none, nearest, search.GapCost is null && search.BarCost is not null
            ? UnreachableText(reader.Name(from))
            : $"play.charge-no-route: no route the game allows leads from {reader.Name(from)} to {reader.Name(nearest)}; the charge ends in place (A15.431)");
    }

    /// <summary>A15.45, A15.44 (ruling R72.4), in words.</summary>
    private static string UnreachableText(string fromText) =>
        $"{UnreachableCode}: from {fromText} no route reaches a Known enemy unit in LOS but across a cliff or a Water Obstacle, so the unit stays in place; at the end of the MPh "
        + "it loses berserk status and is Battle Hardened unless its owner refuses (A15.45, A15.44, A15.3)";

    /// <summary>Whether a charge's verdict is that no Known enemy unit in LOS can be reached past rule bars alone (A15.45, A15.44; ruling R72.4).</summary>
    public static bool NoneReachable(string? undecided) => undecided?.StartsWith(UnreachableCode + ":", StringComparison.Ordinal) == true;

    /// <summary>
    /// A15.45, A15.44, E1.533 (pp. 84, 224; ruling R72.4): at the end of its MPh a berserk unit not in Melee whose charge can reach no Known enemy unit in its LOS,
    /// past rule bars alone, loses berserk status and is offered Battle Hardening; the charge is read only then.
    /// </summary>
    public static bool HardeningDue(string? phase, bool active, bool phasing, bool berserk, bool melee, bool onMap, Func<string?> undecided)
    {
        ArgumentNullException.ThrowIfNull(undecided);
        return phase == "mph" && active && phasing && berserk && !melee && onMap && NoneReachable(undecided());
    }

    /// <summary>
    /// A15.3 (p. 83; ruling R72.4): what Battle Hardening makes of a unit: the next higher quality, or, for an elite MMC or the best leader not yet Fanatic, the
    /// Fanatic status; neither for a Commissar (ruling R15.6) or a unit the reference does not review.
    /// </summary>
    public static (string? HardenedDefinitionId, bool Fanatic) Hardening(string definitionId, bool fanatic) =>
        ScenarioA1FireReference.HardenedOf(definitionId) is { } next ? (next, false) : (null, ScenarioA1FireReference.IsHighestQuality(definitionId) && !fanatic);

    /// <summary>The berserk units whose Battle Hardening their owner is to take or refuse as the MPh ends, in words (A15.45, A15.44, E1.533; ruling R72.4).</summary>
    public static string HardeningDueText(IReadOnlyList<(string UnitId, string LocationText)> units) =>
        $"play.berserk-hardening: {string.Join(", ", units.Select(unit => $"{unit.UnitId} in {unit.LocationText}"))} can reach no Known enemy unit in its LOS but across a cliff or a Water Obstacle; as the MPh ends it loses "
        + "berserk status, and its owner takes or refuses Battle Hardening (A15.45, A15.44, E1.533, A15.3)";

    /// <summary>The owner's Battle Hardening choice for a berserk unit that can reach no Known enemy unit, in words (A15.44, A15.3; ruling R72.4).</summary>
    public static string HardeningChoiceText(string hex) =>
        $"The berserk unit in {hex} can reach no Known enemy unit in its LOS (A15.45), so it loses berserk status; its owner takes Battle Hardening or refuses it (A15.44, A15.3)";

    /// <summary>The answer to the Battle Hardening choice, in words (A15.44, A15.3; ruling R72.4).</summary>
    public static string HardeningText(string unitId, bool taken, bool hardened, bool fanatic) =>
        !taken ? $"play.berserk-hardening: {unitId} loses berserk status and its owner refuses Battle Hardening, so it is Good Order (A15.44, A15.3)"
        : hardened ? $"play.berserk-hardening: {unitId} loses berserk status and is Battle Hardened, Replaced by an unbroken unit of the next higher quality (A15.44, A15.3)"
        : fanatic ? $"play.berserk-hardening: {unitId} loses berserk status and is Battle Hardened; already of the best quality, it becomes Fanatic (A15.44, A15.3)"
        : $"play.berserk-hardening: {unitId} loses berserk status; Battle Hardening leaves it as it is (A15.44, A15.3)";
}
