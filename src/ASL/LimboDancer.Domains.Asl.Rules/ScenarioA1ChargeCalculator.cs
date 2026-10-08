namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The berserk charge's search (A15.43, A15.431, A4.3, A4.31; rulings R27.2, R30.5; pass 32.g, S6): the Known enemy units a charge seeks, the moves out
/// of a node of its route, every shortest route's first moves by Dijkstra, and the steps the stack may take toward the nearest Known enemy unit in its LOS.
/// Play reads the state and the map through <see cref="IChargeFactReader"/>, with Locations as indexes into its table, and marks the steps.
/// </summary>
public static class ScenarioA1ChargeCalculator
{
    /// <summary>A15.43 (table player, pass 27): a Known enemy unit a charge may seek; an Abandoned vehicle is no unit to charge.</summary>
    public static bool ChargeTarget(bool known, bool vehicle, bool abandoned) => known && !(vehicle && abandoned);

    /// <summary>A4.5, A15.43: the half MF a unit has left this MPh, from its allotment (none when it has no allotment), the MF spent, and a half MF spent.</summary>
    public static int HalfMfLeft(int? allotment, int mfSpent, bool halfMfSpent) => allotment is { } allowance ? (allowance * 2) - (mfSpent * 2) - (halfMfSpent ? 1 : 0) : 0;

    /// <summary>A15.43: whether a berserk unit can afford a charge step with the half MF it has left.</summary>
    public static bool Affordable(int allotment, int mfSpent, bool halfMfSpent, int halfMf) => HalfMfLeft(allotment, mfSpent, halfMfSpent) >= halfMf;

    /// <summary>
    /// The moves out of a node of a charge's route (A15.431, A4.3, A4.31; ruling R27.2): from a Location, each step the movement rules allow and each
    /// Bypass of an ADJACENT woods or building hex along one or two of its hexsides; from Bypass, the steps out through the far vertex.
    /// </summary>
    public static IEnumerable<(ChargeNodeFacts Node, ChargeMoveFacts Move)> Edges(IChargeFactReader reader, ChargeNodeFacts node)
    {
        ArgumentNullException.ThrowIfNull(reader);
        if (node.Lane is { } key)
        {
            var lane = reader.Lane(key);
            var turn = (lane[0] - node.Entered!.Value + 6) % 6;
            var far = (lane[^1] + turn) % 6;
            foreach (var side in new[] { lane[^1], far }.Distinct())
            {
                if (reader.Across(node.At, side) is { } exit && reader.Level(exit) == 0 && reader.Playable(exit) && reader.EntryCost(node.At, exit) is { } cost)
                {
                    yield return (new ChargeNodeFacts(exit, null, null), new ChargeMoveFacts(exit, null, cost));
                }
            }

            yield break;
        }

        foreach (var next in reader.Neighbors(node.At).Where(reader.Playable))
        {
            if (reader.EntryCost(node.At, next) is { } cost)
            {
                yield return (new ChargeNodeFacts(next, null, null), new ChargeMoveFacts(next, null, cost));
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
    /// The first moves of every shortest route in MF from a node to a target Location (A15.431; ruling R27.2), by Dijkstra over the charge's moves;
    /// Locations holding other enemy units are not passed through. Null when no route reaches the target.
    /// </summary>
    public static HashSet<ChargeMoveFacts>? FirstMoves(IChargeFactReader reader, ChargeNodeFacts start, int target, HashSet<int> blocked)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(blocked);
        var best = new Dictionary<ChargeNodeFacts, int> { [start] = 0 };
        var firsts = new Dictionary<ChargeNodeFacts, HashSet<ChargeMoveFacts>> { [start] = [] };
        var done = new HashSet<ChargeNodeFacts>();
        var queue = new PriorityQueue<ChargeNodeFacts, int>();
        queue.Enqueue(start, 0);
        var goal = new ChargeNodeFacts(target, null, null);
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (!done.Add(node) || node == goal || (node != start && node.Lane is null && blocked.Contains(node.At)))
            {
                continue;
            }

            foreach (var (next, move) in Edges(reader, node))
            {
                var total = cost + Math.Max(1, move.HalfMf);
                HashSet<ChargeMoveFacts> via = node == start ? [move] : firsts[node];
                if (total < best.GetValueOrDefault(next, int.MaxValue))
                {
                    best[next] = total;
                    firsts[next] = [.. via];
                    queue.Enqueue(next, total);
                }
                else if (total == best[next] && !done.Contains(next))
                {
                    firsts[next].UnionWith(via);
                }
            }
        }

        return firsts.TryGetValue(goal, out var found) && found.Count > 0 ? found : null;
    }

    /// <summary>
    /// The steps a berserk stack may take toward the nearest Known enemy unit in its LOS (A15.43, A15.431): the first move of each shortest route
    /// in MF to that unit's Location, over the entries the game allows, Bypass lanes, stairwells, and upper levels (ruling R27.2), around other
    /// Locations holding enemy units. Equidistant targets and routes are the ATTACKER's choice, so their steps are all allowed. With no Known enemy
    /// unit in LOS, the charge keeps to the Location it charged (<paramref name="previous"/>). <paramref name="start"/> gives the stack's node when a
    /// route is searched: its Location, or its Bypass lane (A4.32).
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
        int[] targets;

        // A15.431: a charge keeps to its Location until a closer Known enemy unit comes into its LOS (ruling R30.5).
        if (previous is { } charged && reader.Distance(from, charged) is { } kept)
        {
            var closer = inLos.Select(location => (location, Distance: reader.Distance(from, location))).Where(item => item.Distance < kept).ToArray();
            inLos = [.. closer.Select(item => item.location)];
        }

        if (inLos.Length > 0)
        {
            var distances = inLos.Select(location => (location, Distance: reader.Distance(from, location))).ToArray();
            if (distances.Any(item => item.Distance is null))
            {
                return (none, null, "play.charge-undecided: a distance to a Known enemy unit cannot be read");
            }

            var nearest = distances.Min(item => item.Distance!.Value);
            targets = [.. distances.Where(item => item.Distance == nearest).Select(item => item.location)];
        }
        else if (previous is { } held)
        {
            targets = [held];
        }
        else
        {
            return (none, null, null);
        }

        // A4.32 (ruling R27.2): a stack in Bypass starts from its lane.
        var origin = start();
        var steps = new Dictionary<int, ChargeSearchStep>();
        var undecided = new List<string>();
        foreach (var target in targets)
        {
            var blocked = enemies.Where(location => location != target).ToHashSet();
            if (FirstMoves(reader, origin, target, blocked) is not { } moves)
            {
                undecided.Add($"play.charge-no-route: no route the game allows leads from {reader.Name(from)} to {reader.Name(target)}; the charge ends in place (A15.431)");
                continue;
            }

            foreach (var move in moves.OrderBy(item => reader.Name(item.To), StringComparer.Ordinal))
            {
                // A step the model cannot take leaves the charge undecided, so it may end in place (ruling R30.5).
                var occupants = reader.Occupants(move.To);
                if (ScenarioA1CloseCombatRules.ChargeBarred(occupants.Crew, occupants.EnemyVehicle, occupants.EnemyInfantry, reader.Name(move.To)) is { } barred)
                {
                    undecided.Add(barred);
                    continue;
                }

                var known = steps.GetValueOrDefault(move.To);
                IReadOnlyList<string> lanes = known?.Lanes ?? [];
                if (move.Lane is { } key && !lanes.Contains(key))
                {
                    lanes = [.. lanes, key];
                }

                steps[move.To] = new ChargeSearchStep(known?.Target ?? target, Math.Min(known?.HalfMf ?? int.MaxValue, move.HalfMf), (known?.Plain ?? false) || move.Lane is null, lanes);
            }
        }

        return steps.Count == 0 && undecided.Count > 0
            ? (steps, targets[0], undecided[0])
            : (steps, targets.Length == 1 ? targets[0] : null, null);
    }
}
