namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of leaving the playing area and the Recall (S8; A2.6, D5.341, rulings R5.17 and R5.18): who must leave, the exit's VCA and cost,
/// and the shortest route in MP off the Friendly Board Edge, by Dijkstra over the caller's nodes. The caller reads the map and the terrain, keeps the
/// caches, and writes the events.
/// </summary>
public static class ScenarioA1RecallCalculator
{
    /// <summary>D5.341: a vehicle must leave by its Friendly Board Edge when Recalled with its counter on the Recall; +1 side, unless immobilized or Abandoned.</summary>
    public static bool MustLeave(bool vehicle, bool recalled, bool recallShown, bool immobilized, bool abandoned) =>
        vehicle && recalled && recallShown && !immobilized && !abandoned;

    /// <summary>D2.11: an exit direction lies within the VCA when it is 30 degrees from the vehicle's facing either way.</summary>
    public static bool ExitWithinVca(double directionDegrees, double facingDegrees) =>
        Math.Abs(Math.Abs(((directionDegrees - facingDegrees + 540) % 360) - 180) - 30) < 1;

    /// <summary>A2.6, B15.6: the terrain an exit pays for: grain is Open Ground outside April to September; null when the month is not known.</summary>
    public static string? ExitTerrain(string terrain, int? month) =>
        terrain != "grain" ? terrain : month is not { } known ? null : known is >= 4 and <= 9 ? "grain" : "open-ground";

    /// <summary>
    /// D5.341 (p. 203; pass 35, task 35.13 g): whether a leaving AFV may make an expenditure that is not on its shortest route: only a Stop, and only
    /// while it carries Passengers or Riders, which it "may Stop (or remain Stopped) long enough to unload".
    /// </summary>
    public static bool StopsToUnload(string kind, bool carrying) => kind == "stop" && carrying;

    /// <summary>
    /// A2.6, D2.16: the half MP of an exit: its hex's own terrain cost, or the road rate when a road crosses the hexside: a full MP for a BU AFV, and
    /// in Ground or Deep Snow (E3.724, E3.7331), as an entry by road pays (pass 35, task 35.13 a; the pass 32 design's section 12, item 15).
    /// </summary>
    public static int ExitHalfMp(int terrainHalfMp, bool road, bool buttonedUpAfv, bool snow = false) => road ? (buttonedUpAfv || snow ? 2 : 1) : terrainHalfMp;

    /// <summary>
    /// D5.341 (ruling R5.17): the first moves of a shortest route in half MP from <paramref name="origin"/> off the map, or why it is undecided: an exact
    /// search over the moves the review admits, and a lower bound that also enters undecided terrain; the route is undecided when there is none or the
    /// bound is shorter. The first moves are the exits from the origin at the route's cost, then each move that starts a shortest route.
    /// </summary>
    public static (IReadOnlyList<TMove> Moves, int? HalfMp, bool Undecided) Route<TNode, TMove>(TNode origin,
        Func<TNode, bool, IEnumerable<(TMove Move, int HalfMp, TNode To)>> moves, Func<TNode, int?> exit, Func<int, IEnumerable<TMove>> exitsAt)
        where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(moves);
        ArgumentNullException.ThrowIfNull(exit);
        ArgumentNullException.ThrowIfNull(exitsAt);
        var exact = Distance(origin, false, moves, exit);
        var lower = Distance(origin, true, moves, exit);
        if (exact is not { } route || lower < route)
        {
            return ([], exact, true);
        }

        var first = new List<TMove>();
        if (exit(origin) is { } here && here == route)
        {
            first.AddRange(exitsAt(here));
        }

        foreach (var (move, halfMp, to) in moves(origin, false))
        {
            if (Distance(to, false, moves, exit) is { } rest && halfMp + rest == route)
            {
                first.Add(move);
            }
        }

        return (first, route, false);
    }

    /// <summary>Dijkstra's least half MP from <paramref name="start"/> off the map, or null when no exit is reached.</summary>
    public static int? Distance<TNode, TMove>(TNode start, bool lowerBound, Func<TNode, bool, IEnumerable<(TMove Move, int HalfMp, TNode To)>> moves,
        Func<TNode, int?> exit)
        where TNode : notnull
    {
        ArgumentNullException.ThrowIfNull(moves);
        ArgumentNullException.ThrowIfNull(exit);
        var best = new Dictionary<TNode, int> { [start] = 0 };
        var queue = new PriorityQueue<TNode, int>();
        queue.Enqueue(start, 0);
        int? goal = null;
        while (queue.TryDequeue(out var node, out var cost))
        {
            if (goal is { } found && cost >= found)
            {
                break;
            }

            if (cost > best[node])
            {
                continue;
            }

            if (exit(node) is { } leave && cost + leave < (goal ?? int.MaxValue))
            {
                goal = cost + leave;
            }

            foreach (var (_, halfMp, to) in moves(node, lowerBound))
            {
                var total = cost + halfMp;
                if (total < best.GetValueOrDefault(to, int.MaxValue))
                {
                    best[to] = total;
                    queue.Enqueue(to, total);
                }
            }
        }

        return goal;
    }
}
