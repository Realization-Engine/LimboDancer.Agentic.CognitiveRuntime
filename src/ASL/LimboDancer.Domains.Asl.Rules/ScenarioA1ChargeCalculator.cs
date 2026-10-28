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

    /// <summary>The movement model's refusal of a cliff hexside, which a berserk unit never charges through (A15.45).</summary>
    private const string CliffCode = "play.move-cliff:";

    /// <summary>
    /// The movement model's refusals of Locations the building rules do not connect (B23.4, B23.42; ruling R72.17): ordinary movement, which the shortest
    /// route already prices, so neither a rule bar nor a gap of the model.
    /// </summary>
    private static readonly string[] ConnectionCodes = ["play.move-level:", "play.move-stairwell:", "play.move-upper-level:"];

    /// <summary>The half MF a step across a rule bar is given in the search that ignores bars (ruling R72.17): one MF, as Open Ground (A4.1).</summary>
    private const int RelaxedBarHalfMf = 2;

    /// <summary>A15.43 (table player, pass 27): a Known enemy unit a charge may seek; an Abandoned vehicle is no unit to charge.</summary>
    public static bool ChargeTarget(bool known, bool vehicle, bool abandoned) => known && !(vehicle && abandoned);

    /// <summary>A4.5, A15.43: the half MF a unit has left this MPh, from its allotment (none when it has no allotment), the MF spent, and a half MF spent.</summary>
    public static int HalfMfLeft(int? allotment, int mfSpent, bool halfMfSpent) => allotment is { } allowance ? (allowance * 2) - (mfSpent * 2) - (halfMfSpent ? 1 : 0) : 0;

    /// <summary>A15.43: whether a berserk unit can afford a charge step with the half MF it has left.</summary>
    public static bool Affordable(int allotment, int mfSpent, bool halfMfSpent, int halfMf) => HalfMfLeft(allotment, mfSpent, halfMfSpent) >= halfMf;

    /// <summary>
    /// Whether a refused charge step is barred by A15.45's terrain (p. 84; pass 72, rulings R72.4, R72.17): a cliff hexside or an unbridged Water Obstacle
    /// (B21.1, p. 134), which a berserk unit never charges through; a Blaze is not built. A bridged hex is not barred, and since the movement model reads no
    /// bridge its refusal stays a gap. A building connection is no bar (<see cref="Connection"/>); any other refusal (terrain "not reviewed", ruling R10.1; a
    /// map the model cannot read) is a gap.
    /// </summary>
    public static bool RuleBar(ChargeEntryFacts entry) =>
        entry.HalfMf is null && (entry.Refusal?.StartsWith(CliffCode, StringComparison.Ordinal) == true
            || (!entry.Bridged && entry.Terrain is { } name && (WaterObstacles.Contains(name) || name.Contains("Pond", StringComparison.Ordinal))));

    /// <summary>
    /// Whether a refused charge step is ordinary movement the building rules do not allow (B23.4, B23.42, B23.8, B23.41; ruling R72.17): Locations of a building
    /// that do not connect, a Rooftop, or a Cellar. The shortest route goes round it, so it is neither a rule bar nor a gap of the model.
    /// </summary>
    private static bool Connection(ChargeEntryFacts entry) =>
        entry.HalfMf is null && ((entry.Refusal is { } refusal && ConnectionCodes.Any(code => refusal.StartsWith(code, StringComparison.Ordinal))) || entry.Terrain is "Rooftop" or "Cellar");

    /// <summary>A rule bar in words, for the verdict that names the bar met (A15.45).</summary>
    private static string BarText(ChargeEntryFacts entry) => entry.Refusal?.StartsWith(CliffCode, StringComparison.Ordinal) == true ? "a cliff" : "a Water Obstacle";

    /// <summary>
    /// The moves out of a node of a charge's route (A15.431, A4.3, A4.31; ruling R27.2): from a Location, each step the movement rules allow and each
    /// Bypass of an ADJACENT woods or building hex along one or two of its hexsides; from Bypass, the steps out through the far vertex. Each step the
    /// movement rules refuse is handed to <paramref name="refused"/> with the node it would enter.
    /// </summary>
    public static IEnumerable<(ChargeNodeFacts Node, ChargeMoveFacts Move)> Edges(IChargeFactReader reader, ChargeNodeFacts node, Action<ChargeNodeFacts, ChargeEntryFacts>? refused = null)
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
                    refused?.Invoke(new ChargeNodeFacts(exit, null, null), entry);
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
                refused?.Invoke(new ChargeNodeFacts(next, null, null), entry);
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
    /// One search from a node over the charge's moves (A15.431; rulings R27.2, R72.3, R72.17): by Dijkstra in half MF, the first moves of every shortest route
    /// to each node and the fewest hexes stepped on one, with the refusals met on its frontier: the rule bars (<see cref="RuleBar"/>) and whether a gap of the
    /// model refused a step. Each node's moves are read once and kept, so the search that ignores bars (<see cref="Search.RelaxedCost"/>) asks the reader again
    /// only beyond them. The <paramref name="terminals"/> (every Known enemy Location) are entered, never passed through.
    /// </summary>
    private static Search SearchFrom(IChargeFactReader reader, ChargeNodeFacts start, HashSet<int> terminals)
    {
        var search = new Search(reader, start, terminals);
        var done = new HashSet<ChargeNodeFacts>();
        var queue = new PriorityQueue<ChargeNodeFacts, int>();
        queue.Enqueue(start, 0);
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (!done.Add(node) || search.Stops(node))
            {
                continue;
            }

            foreach (var (next, move, _) in search.Out(node, frontier: true))
            {
                if (move is not { } taken)
                {
                    continue;
                }

                var total = cost + Math.Max(1, taken.HalfMf);
                var known = search.Best.GetValueOrDefault(next, int.MaxValue);
                if (total > known || (total == known && done.Contains(next)))
                {
                    continue;
                }

                HashSet<ChargeMoveFacts> via = node == start ? [taken] : search.Firsts[node];
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

    /// <summary>A move out of a node as <see cref="Search"/> keeps it: the node entered, the move (null for a step a rule bar refused), and its half MF.</summary>
    private readonly record struct ChargeOut(ChargeNodeFacts Node, ChargeMoveFacts? Move, int HalfMf);

    /// <summary>The answer of <see cref="SearchFrom"/>.</summary>
    private sealed class Search(IChargeFactReader reader, ChargeNodeFacts start, HashSet<int> terminals)
    {
        private readonly Dictionary<ChargeNodeFacts, List<ChargeOut>> outs = [];
        private Dictionary<ChargeNodeFacts, int>? relaxed;

        public Dictionary<ChargeNodeFacts, int> Best { get; } = new() { [start] = 0 };

        public Dictionary<ChargeNodeFacts, HashSet<ChargeMoveFacts>> Firsts { get; } = new() { [start] = [] };

        public Dictionary<ChargeNodeFacts, int> Hexes { get; } = new() { [start] = 0 };

        /// <summary>The rule bars met on the search's frontier, in words, in the order met.</summary>
        public List<string> Bars { get; } = [];

        /// <summary>Whether a gap of the model refused a step on the search's frontier.</summary>
        public bool Gap
        {
            get; private set;
        }

        /// <summary>Whether a node is a Known enemy Location other than the start: entered, never passed through.</summary>
        public bool Stops(ChargeNodeFacts node) => node != start && node.Lane is null && terminals.Contains(node.At);

        /// <summary>
        /// The moves out of a node, read once (the reader's entry and Bypass costs are its dearest calls). On the search's own <paramref name="frontier"/> each
        /// refusal is sorted: a rule bar is kept as a move the search that ignores bars may take, a building connection is ordinary movement, and any other is a gap.
        /// </summary>
        public List<ChargeOut> Out(ChargeNodeFacts node, bool frontier)
        {
            if (outs.TryGetValue(node, out var kept))
            {
                return kept;
            }

            var found = new List<ChargeOut>();
            void Refused(ChargeNodeFacts next, ChargeEntryFacts entry)
            {
                if (RuleBar(entry))
                {
                    found.Add(new ChargeOut(next, null, RelaxedBarHalfMf));
                    if (frontier && !Bars.Contains(BarText(entry)))
                    {
                        Bars.Add(BarText(entry));
                    }
                }
                else if (frontier && !Connection(entry))
                {
                    Gap = true;
                }
            }

            foreach (var (next, move) in Edges(reader, node, Refused))
            {
                found.Add(new ChargeOut(next, move, move.HalfMf));
            }

            outs[node] = found;
            return found;
        }

        /// <summary>The first moves of the shortest routes to a Location, or null when none reaches it.</summary>
        public HashSet<ChargeMoveFacts>? FirstMoves(int target) =>
            Firsts.TryGetValue(new ChargeNodeFacts(target, null, null), out var found) && found.Count > 0 ? found : null;

        public int Cost(int target) => Best[new ChargeNodeFacts(target, null, null)];

        public int HexesTo(int target) => Hexes[new ChargeNodeFacts(target, null, null)];

        /// <summary>
        /// A15.45 (ruling R72.17): the half MF of the shortest route to a Location when rule bars are ignored, each step across one taken at one MF; null when
        /// none reaches it. Searched once, on the first ask, over the moves already read.
        /// </summary>
        public int? RelaxedCost(int target)
        {
            if (relaxed is null)
            {
                relaxed = new()
                {
                    [start] = 0
                };
                var done = new HashSet<ChargeNodeFacts>();
                var queue = new PriorityQueue<ChargeNodeFacts, int>();
                queue.Enqueue(start, 0);
                while (queue.TryDequeue(out var node, out var cost))
                {
                    if (!done.Add(node) || Stops(node))
                    {
                        continue;
                    }

                    foreach (var (next, _, halfMf) in Out(node, frontier: false))
                    {
                        var total = cost + Math.Max(1, halfMf);
                        if (total < relaxed.GetValueOrDefault(next, int.MaxValue))
                        {
                            relaxed[next] = total;
                            queue.Enqueue(next, total);
                        }
                    }
                }
            }

            return relaxed.TryGetValue(new ChargeNodeFacts(target, null, null), out var found) ? found : null;
        }
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431, A15.45; rulings R27.2, R72.3): one search reads the
    /// first move of each shortest route in MF to every Known enemy Location, over the entries the game allows, Bypass lanes, stairwells, and upper levels,
    /// never through another Location holding enemy units. The targets are taken in groups by range in hexes, the nearest first; when no route reaches any
    /// unit of a group, the next group is charged (A15.45). When a rule bar lies across a target's route, the charge stays on it only while its route, counted
    /// in hexes stepped, is still closer than the next group with a reachable target (ruling R72.17; a tie gives both groups). Equidistant targets and routes
    /// are the ATTACKER's choice, so their steps are all allowed. With no Known enemy unit in LOS, the charge keeps to the Location it charged
    /// (<paramref name="previous"/>). <paramref name="start"/> gives the stack's node when a route is searched: its Location, or its Bypass lane (A4.32). When no
    /// target can be reached, the verdict is <see cref="UnreachableCode"/> if a Known enemy unit is in LOS and only rule bars kept them out of reach (A15.44,
    /// rulings R72.4, R72.17), else the charge is undecided and ends in place (ruling R30.5).
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

        // A15.431, A15.44 (ruling R72.17): a unit with no Known enemy unit in its LOS returns to normal, and is never Battle Hardened.
        var seen = inLos.Length > 0;

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
        int[][] reachable = [.. groups.Select(group => group.Targets.Where(target => search.FirstMoves(target) is not null).ToArray())];

        // Ruling R72.17: a rule bar lies across a target's would-be route when the route that ignores bars is cheaper than the real one.
        bool Barred(int target) => search.Bars.Count > 0 && search.RelaxedCost(target) is { } relaxed && relaxed < search.Cost(target);
        for (var index = 0; index < groups.Count; index++)
        {
            if (reachable[index].Length == 0)
            {
                continue;
            }

            // A15.45 (ruling R72.17): a barred nearest is charged by its alternate route only while that route, in hexes stepped, is still closer than the next
            // nearest group that has a reachable target (none: the nearest stays). At a tie the targets are equidistant, and the ATTACKER chooses among both
            // groups (A15.43); a target no bar crosses is charged by A15.43 and keeps the farther group out.
            var later = Enumerable.Range(index + 1, groups.Count - index - 1).FirstOrDefault(other => reachable[other].Length > 0, -1);
            var reached = reachable[index];
            if (later >= 0)
            {
                var range = groups[later].Range;
                int[] closer = [.. reached.Where(target => !Barred(target) || search.HexesTo(target) < range)];
                int[] tied = [.. reached.Where(target => Barred(target) && search.HexesTo(target) == range)];
                reached = closer.Length > 0 ? closer : tied.Length > 0 ? [.. tied, .. reachable[later]] : [];
            }

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

        // A15.45, A15.44 (ruling R72.17): the verdict needs a Known enemy unit in LOS, a rule bar on the frontier and no gap, and no Known enemy Location reached
        // at all (a target cut off only by other enemy Locations, or a Known enemy unit the charge can reach out of its LOS, keeps ruling R30.5).
        var nearest = groups[0].Targets[0];
        return (none, nearest, seen && search.Bars.Count > 0 && !search.Gap && !terminals.Any(terminal => search.FirstMoves(terminal) is not null)
            ? UnreachableText(search.Bars)
            : $"play.charge-no-route: the game allows a route to no Known enemy unit in its LOS (the nearest {reader.Name(nearest)}), so the charge ends in place (ruling R30.5)");
    }

    /// <summary>A15.45, A15.44 (rulings R72.4, R72.17), in words, naming the bars met; the unit and its Location are the caller's.</summary>
    private static string UnreachableText(List<string> bars) =>
        $"{UnreachableCode}: no route reaches a Known enemy unit in its LOS except across {(bars.Count > 0 ? string.Join(" or ", bars) : "terrain a berserk unit will not charge through")}; "
        + "at the end of the MPh it loses berserk status and is Battle Hardened unless its owner refuses (A15.45, A15.44, A15.3)";

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
        $"play.berserk-hardening: {string.Join(", ", units.Select(unit => $"{unit.UnitId} in {unit.LocationText}"))} can reach no Known enemy unit in its LOS except across terrain "
        + "a berserk unit will not charge through; as the MPh ends it loses berserk status, and its owner takes or refuses Battle Hardening (A15.45, A15.44, E1.533, A15.3)";

    /// <summary>
    /// The owner's Battle Hardening choice for a berserk unit that can reach no Known enemy unit, in words, the unit named after its hex as the hex UI rule has
    /// it, "[A8]: g1" (A15.44, A15.3; rulings R72.4, R72.17).
    /// </summary>
    public static string HardeningChoiceText(string? hex, string unitId) =>
        $"{(hex is null ? string.Empty : hex + ": ")}{unitId} is berserk and can reach no Known enemy unit in its LOS (A15.45), so it loses berserk status; its owner takes Battle Hardening or refuses it (A15.44, A15.3)";

    /// <summary>
    /// The answer to the Battle Hardening choice, in words (A15.44, A15.3; rulings R72.4, R72.17): the unit and its Location, taken or refused, and what it became,
    /// the unit of the next higher quality by its definition, or Fanatic.
    /// </summary>
    public static string HardeningText(string unitId, string? at, bool taken, string? hardenedDefinitionId, bool fanatic)
    {
        var unit = at is null ? unitId : $"{unitId} in {at}";
        return !taken ? $"play.berserk-hardening: {unit} loses berserk status and its owner refuses Battle Hardening, so it is Good Order (A15.44, A15.3)"
            : hardenedDefinitionId is { } next ? $"play.berserk-hardening: {unit} loses berserk status and is Battle Hardened: it is Replaced by an unbroken {next} (A15.44, A15.3)"
            : fanatic ? $"play.berserk-hardening: {unit} loses berserk status and is Battle Hardened; already of the best quality, it becomes Fanatic (A15.44, A15.3)"
            : $"play.berserk-hardening: {unit} loses berserk status; Battle Hardening leaves it as it is (A15.44, A15.3)";
    }
}
