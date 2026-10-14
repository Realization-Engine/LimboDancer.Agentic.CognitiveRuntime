"""Pass 32.f commit 2: RoutReach, RoutTargets, CanRout, TrappedByInterdiction, and RoutAdvice forward to Rules over a RoutScan."""
import pathlib

P = pathlib.Path(r"E:\Archive\GitHub\dlandi\LimboDancer.MCP\src\ASL\LimboDancer.Domains.Asl.Play\GamePlanner.Rout.cs")
text = P.read_bytes().decode("utf-8")
assert "\r\n" not in text


def replace_range(text, start_anchor, end_anchor, new):
    assert text.count(start_anchor) == 1, start_anchor[:60]
    assert text.count(end_anchor) == 1, end_anchor[:60]
    a = text.index(start_anchor)
    b = text.index(end_anchor) + len(end_anchor)
    assert a < b
    return text[:a] + new + text[b:]


SEARCH_START = "    /// <summary>\n    /// The least half MF, within the unit's allowance, to reach each Location from a start over legal rout steps (A10.51): the search follows which Known\n"
SEARCH_END = "        return !reach.Keys.Any(at => at != start && NearUnbrokenArmedEnemy(state, unit.Side, at) is null);\n    }\n"
SEARCH_NEW = '''    /// <summary>
    /// The least half MF, within the unit's allowance, to reach each Location from a start over legal rout steps (A10.51), as Rules searches it over the
    /// rout's fact reader; the Locations come back through the scan's table in the order the search reached them.
    /// </summary>
    private Dictionary<BoardLocation, int> RoutReach(GameState state, UnitInstance unit, BoardLocation start, IEnumerable<string> seen, int spent, int limit,
        bool avoidInterdiction = false, Dictionary<BoardLocation, BoardLocation[]>? routes = null)
    {
        var scan = new RoutScan(this, state);
        var found = routes is null ? null : new Dictionary<int, int[]>();
        var reach = ScenarioA1RoutCalculator.RoutReach(scan, scan.Enemies(unit.Side), scan.Index(start), seen, spent, limit, state.Encircled(unit), state.ScenarioMonth,
            avoidInterdiction, found);
        if (routes is not null)
        {
            foreach (var (at, route) in found!)
            {
                routes[scan.At(at)] = [.. route.Select(scan.At)];
            }
        }

        return reach.ToDictionary(item => scan.At(item.Key), item => item.Value);
    }

    /// <summary>The woods and building Locations a rout may make for (A10.51, A10.532 EXC); empty when none is reached.</summary>
    private BoardLocation[] RoutTargets(GameState state, UnitInstance unit, BoardLocation start, IReadOnlyDictionary<BoardLocation, int> reach)
    {
        var scan = new RoutScan(this, state);
        var targets = ScenarioA1RoutCalculator.RoutTargets(scan, scan.Enemies(unit.Side), scan.Index(start), reach.ToDictionary(item => scan.Index(item.Key), item => item.Value));
        return [.. targets.Select(scan.At)];
    }

    /// <summary>Whether a broken unit has any legal rout step (A10.5): a step or a Low Crawl the rules allow.</summary>
    private bool CanRout(GameState state, UnitInstance unit) =>
        state.Location(unit.Id)?.Location is { } at && ScenarioA1RoutCalculator.CanRout(RoutReach(state, unit, at, SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit)));

    /// <summary>Whether a broken unit ADJACENT to its captors can get away from every Known unbroken armed enemy unit only by Interdiction or Low Crawl (A20.21).</summary>
    private bool TrappedByInterdiction(GameState state, UnitInstance unit, BoardLocation start)
    {
        var scan = new RoutScan(this, state);
        var reach = ScenarioA1RoutCalculator.RoutReach(scan, scan.Enemies(unit.Side), scan.Index(start), SeenBy(state, unit.Side, start), 0, RoutHalfMf(unit), state.Encircled(unit),
            state.ScenarioMonth, avoidInterdiction: true);
        return ScenarioA1RoutCalculator.TrappedByInterdiction(scan, scan.Enemies(unit.Side), scan.Index(start), reach);
    }
'''

ADVICE_START = "        // Pass 31d (design D3): one least-cost route to each place the rout may end, from the search's own steps. The search reads Known enemy\n"
ADVICE_END = "            .ToDictionary(target => target, target => (IReadOnlyList<BoardLocation>)found[target]));\n    }\n"
ADVICE_NEW = '''        // Pass 31d (design D3): one least-cost route to each place the rout may end, from the search's own steps. The search reads Known enemy
        // units alone (A10.533), so a route tells the routing side nothing it does not hold. Rules decides which places are offered (pass 32.f).
        var scan = new RoutScan(this, state);
        var found = new Dictionary<int, int[]>();
        var reach = ScenarioA1RoutCalculator.RoutReach(scan, scan.Enemies(unit.Side), scan.Index(at), SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit), state.Encircled(unit),
            state.ScenarioMonth, routes: found);
        var (targets, canRout, routes) = ScenarioA1RoutCalculator.RoutAdvice(scan, scan.Enemies(unit.Side), scan.Index(at), reach, found);
        return ([.. targets.Select(scan.At)], canRout, routes.ToDictionary(item => scan.At(item.Key), item => (IReadOnlyList<BoardLocation>)[.. item.Value.Select(scan.At)]));
    }
'''

text = replace_range(text, SEARCH_START, SEARCH_END, SEARCH_NEW)
text = replace_range(text, ADVICE_START, ADVICE_END, ADVICE_NEW)
P.write_bytes(text.encode("utf-8"))
print("applied")
