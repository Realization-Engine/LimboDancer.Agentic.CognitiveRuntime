using System.Globalization;
using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Rout Phase and Desperation Morale (backlog pass 13, rulings R13.1 and R13.3): who must or may rout, a rout's legal route to the nearest
/// woods or building Location, Low Crawl, Interdiction, Failure to Rout and surrender, and the DM a broken unit gains from an ADJACENT Known armed
/// enemy unit, at the start of the RtPh, and by retaining it as the RPh ends. Terrain Blazes are not built (a Wreck Blaze does not bar movement,
/// B25.141), so no unit routs for a Blaze.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Why a broken unit must rout (A10.5; ruling R13.3), or null when it need not.</summary>
    public string? MustRout(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        // D6.1 (ruling R26.2): a broken Passenger may stay aboard, free of rout requirements.
        if (unit.Status != InstanceStatus.Active || !Is(unit, Conditions.Broken) || Is(unit, Conditions.Melee) || Is(unit, Conditions.Captured)
            || state.Aboard(unit.Id) is not null || state.Location(unit.Id)?.Location is not { } at)
        {
            return null;
        }

        return NearUnbrokenArmedEnemy(state, unit.Side, at) is { } enemy ? $"{enemy} is a Known unbroken armed enemy unit ADJACENT to it or in its Location"
            : ExposedInOpenGround(state, unit.Side, at) is { } seen ? $"it is in Open Ground in the LOS and Normal Range of {seen}"
            : null;
    }

    /// <summary>Whether a broken unit may rout (A10.5): it must, or it is under DM.</summary>
    public bool MayRout(GameState state, UnitInstance unit) =>
        MustRout(state, unit) is not null || (unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && Is(unit, Conditions.DesperationMorale)
            && !Is(unit, Conditions.Melee) && !Is(unit, Conditions.Captured));

    /// <summary>A Known enemy unit (A10.51, A10.533): not a Dummy, concealed, hidden, or a prisoner.</summary>
    private static IEnumerable<(UnitInstance Unit, BoardLocation At)> KnownEnemies(GameState state, string side) =>
        state.Units.Where(other => other.Status == InstanceStatus.Active && other.Side != side && KnownEnemy(other))
            .Select(other => (Unit: other, At: state.Location(other.Id)?.Location)).Where(item => item.At is not null).Select(item => (item.Unit, item.At!));

    /// <summary>A10.5, A10.62: an armed enemy unit is Personnel (a leader without a SW counts), or a vehicle not Abandoned.</summary>
    private static bool Armed(UnitInstance unit) => !LiveFire.IsVehicle(unit) || !Is(unit, Conditions.Abandoned);

    private bool AdjacentOrSame(GameState state, BoardLocation one, BoardLocation two) => one == two || IsAdjacent(state, one, two);

    /// <summary>A Known unbroken armed enemy unit ADJACENT to a Location or in it (A10.5), or null.</summary>
    private string? NearUnbrokenArmedEnemy(GameState state, string side, BoardLocation at) =>
        KnownEnemies(state, side).Where(item => Armed(item.Unit) && !Is(item.Unit, Conditions.Broken) && AdjacentOrSame(state, item.At, at))
            .Select(item => item.Unit.Id).Order(StringComparer.Ordinal).FirstOrDefault();

    /// <summary>
    /// Open Ground (A10.531; ruling R13.3): a hex where the enemy could apply the FFMO DRM, read as Open Ground or road terrain, or grain outside June to
    /// September (B15.6; ruling R5.19), with no SMOKE there.
    /// </summary>
    private bool OpenGround(GameState state, BoardLocation at) =>
        ReadLocation(state, at) is { } read && !HasSmoke(state, at)
            && (TerrainKey(read) == "open-ground" || (TerrainKey(read) == "grain" && state.ScenarioMonth is < 6 or > 9));

    /// <summary>
    /// The Normal Range in hexes of a unit's fire (A10.532): the longest of its own printed range and the Normal Ranges of the functioning SW it
    /// possesses, at most 16; a leader without a SW has none. Vehicles and Guns have none here (backlog).
    /// </summary>
    private static int NormalRange(GameState state, UnitInstance unit)
    {
        var definitions = FireReference.Value.Definitions;
        var own = unit.Definition is { } reference && definitions.GetValueOrDefault(reference.Definition) is { } definition && definition.Kind != "asl:leader"
            ? definition.Range ?? 0 : 0;
        var weapons = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && !Is(item, Conditions.Malfunctioned) && !Is(item, Conditions.Dismantled))
            .Select(item => item.Definition is { } weapon ? definitions.GetValueOrDefault(weapon.Definition)?.Range ?? 0 : 0);
        return Math.Min(16, weapons.Append(own).Max());
    }

    /// <summary>
    /// A Known unbroken enemy unit not in Melee in whose LOS and Normal Range a Location in Open Ground lies, with no Hindrance between (A10.5, A10.531), or null.
    /// </summary>
    private string? ExposedInOpenGround(GameState state, string side, BoardLocation at)
    {
        if (!OpenGround(state, at))
        {
            return null;
        }

        // A10.531 (table player, pass 13): only an enemy unit that could fire applies FFMO, so broken ones do not count.
        foreach (var (enemy, there) in KnownEnemies(state, side).Where(item => !Is(item.Unit, Conditions.Melee) && !Is(item.Unit, Conditions.Broken))
            .OrderBy(item => item.Unit.Id, StringComparer.Ordinal))
        {
            if (NormalRange(state, enemy) is > 0 and var range && Los(state, there, at) is { Status: LosStatus.Clear, Hindrance: 0 } los && los.Range <= range)
            {
                return enemy.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// The enemy unit able to Interdict a routing unit entering an Open Ground Location (A10.53, A10.532, A10.533; ruling R13.3), or null: Known unbroken
    /// Infantry not CX, pinned, Encircled, in Melee, or a prisoner, with the Location in its LOS and Normal Range and no Hindrance between. Vehicles, and
    /// units whose FP is halved for other reasons, are not read (backlog).
    /// </summary>
    private string? Interdictor(GameState state, string side, BoardLocation at)
    {
        if (!OpenGround(state, at))
        {
            return null;
        }

        foreach (var (enemy, there) in KnownEnemies(state, side).Where(item => !LiveFire.IsVehicle(item.Unit) && !Is(item.Unit, Conditions.Broken)
            && !Is(item.Unit, Conditions.Cx) && !Is(item.Unit, Conditions.Pinned) && !Is(item.Unit, Conditions.Melee) && !state.Encircled(item.Unit))
            .OrderBy(item => item.Unit.Id, StringComparer.Ordinal))
        {
            if (NormalRange(state, enemy) is > 0 and var range && Los(state, there, at) is { Status: LosStatus.Clear, Hindrance: 0 } los && los.Range <= range)
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
    private string? RoutStepBar(GameState state, string side, BoardLocation from, BoardLocation to, IReadOnlyCollection<string> seenBy)
    {
        foreach (var (enemy, at) in KnownEnemies(state, side))
        {
            if (at == to)
            {
                return $"play.rout-step: a routing unit never enters {to}, which holds the Known enemy unit {enemy.Id} (A10.51)";
            }

            if (at != from && IsAdjacent(state, at, to))
            {
                return $"play.rout-step: a routing unit never moves ADJACENT to the Known enemy unit {enemy.Id} unless it is leaving its Location (A10.51)";
            }

            if (Armed(enemy) && seenBy.Contains(enemy.Id) && HexDistance(state, at, to) is { } near && HexDistance(state, at, from) is { } far && near < far)
            {
                return $"play.rout-step: a routing unit never moves closer to the Known armed enemy unit {enemy.Id}, which has had it in its LOS (A10.51)";
            }
        }

        return null;
    }

    /// <summary>The Known armed enemy units with a clear LOS to a Location (A10.51).</summary>
    private IEnumerable<string> SeenBy(GameState state, string side, BoardLocation at) =>
        KnownEnemies(state, side).Where(item => Armed(item.Unit) && Los(state, item.At, at) is { Status: LosStatus.Clear }).Select(item => item.Unit.Id);

    /// <summary>
    /// The half MF a routing unit pays to step between ADJACENT Locations (A10.5): the Infantry entry cost, without Bypass or Road Bonus, doubled for the
    /// first Location an Encircled unit enters (A7.7; ruling R12.11); or why it may not.
    /// </summary>
    private (int? HalfMf, bool AllMf, string? Reason) RoutEntry(GameState state, BoardLocation from, BoardLocation to, bool encircledFirst)
    {
        var (entry, reason) = InfantryStep(state, from, to);
        return entry switch
        {
            null => (null, false, reason ?? $"play.rout-step: the step from {from} to {to} is not one the game allows"),
            { MinimumMoveOnly: true } => (null, false, $"play.rout-step: {to} is entered only by Minimum Move, which a rout does not use (A10.5)"),
            { AllMf: true } => (null, true, null),
            _ => (entry.HalfMf * (encircledFirst ? 2 : 1), false, null),
        };
    }

    /// <summary>A10.51: a woods or building Location is a rout destination.</summary>
    private bool RoutCover(GameState state, BoardLocation at) =>
        ReadLocation(state, at) is { } read && TerrainKey(read) is "woods" or "wooden-building" or "stone-building";

    /// <summary>
    /// The least half MF, within the unit's allowance, to reach each Location from a start over legal rout steps (A10.51): the search follows which Known
    /// armed enemy units have had the unit in their LOS, since it never moves closer to them afterwards. An entry that costs all MF is a first step only.
    /// </summary>
    private Dictionary<BoardLocation, int> RoutReach(GameState state, UnitInstance unit, BoardLocation start, IEnumerable<string> seen, int spent, int limit,
        bool avoidInterdiction = false, Dictionary<BoardLocation, BoardLocation[]>? routes = null)
    {
        // Pass 31d (design D3): when the caller asks for the routes, the search keeps where each node was reached from, and gives one least-cost
        // route to each Location it reaches.
        var came = new Dictionary<(BoardLocation, long), (BoardLocation At, long Mask)>();
        var reached = new Dictionary<BoardLocation, (BoardLocation At, long Mask)>();
        string[] enemies = [.. KnownEnemies(state, unit.Side).Where(item => Armed(item.Unit)).Select(item => item.Unit.Id).Order(StringComparer.Ordinal)];
        var sight = new Dictionary<BoardLocation, long>();
        long Mask(IEnumerable<string> ids) => ids.Select(id => Array.IndexOf(enemies, id)).Where(index => index is >= 0 and < 63).Aggregate(0L, (mask, index) => mask | (1L << index));
        long Sight(BoardLocation at) => sight.TryGetValue(at, out var mask) ? mask : sight[at] = Mask(SeenBy(state, unit.Side, at));
        string[] Seen(long mask) => [.. enemies.Where((_, index) => index < 63 && (mask & (1L << index)) != 0)];

        var encircled = state.Encircled(unit);
        var bars = new Dictionary<(BoardLocation, BoardLocation, long), bool>();
        var interdicted = new Dictionary<BoardLocation, bool>();
        var entries = new Dictionary<(BoardLocation, BoardLocation, bool), (int? HalfMf, bool AllMf, string? Reason)>();
        var best = new Dictionary<(BoardLocation, long), int>();
        var reach = new Dictionary<BoardLocation, int> { [start] = spent };
        var queue = new PriorityQueue<(BoardLocation At, long Mask), int>();
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
            foreach (var next in Neighbors(state, node.At).Where(at => PlayableBar(state, at) is null))
            {
                // Each LOS read is costly on a real board, so the search keeps what it has read.
                if (!bars.TryGetValue((node.At, next, node.Mask), out var barred))
                {
                    bars[(node.At, next, node.Mask)] = barred = RoutStepBar(state, unit.Side, node.At, next, Seen(node.Mask)) is not null;
                }

                if (barred)
                {
                    continue;
                }

                if (avoidInterdiction && (interdicted.TryGetValue(next, out var open) ? open : interdicted[next] = Interdictor(state, unit.Side, next) is not null))
                {
                    continue;
                }

                var firstStep = encircled && cost == spent && node.At == start;
                if (!entries.TryGetValue((node.At, next, firstStep), out var entry))
                {
                    entries[(node.At, next, firstStep)] = entry = RoutEntry(state, node.At, next, firstStep);
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
                var steps = new List<BoardLocation>();
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
    private BoardLocation[] RoutTargets(GameState state, UnitInstance unit, BoardLocation start, IReadOnlyDictionary<BoardLocation, int> reach)
    {
        var enemies = KnownEnemies(state, unit.Side).ToArray();
        bool Ignorable(BoardLocation at) => enemies.Any(enemy => HexDistance(state, enemy.At, at) is { } there && HexDistance(state, enemy.At, start) is { } here && there <= here);
        var covers = reach.Where(item => item.Key != start && RoutCover(state, item.Key)).ToArray();
        var binding = covers.Where(item => !Ignorable(item.Key)).ToArray();
        if (binding.Length == 0)
        {
            return [];
        }

        var nearest = binding.Min(item => item.Value);
        return [.. covers.Where(item => item.Value <= nearest).Select(item => item.Key)];
    }

    /// <summary>Whether a broken unit has any legal rout step (A10.5): a step or a Low Crawl the rules allow.</summary>
    private bool CanRout(GameState state, UnitInstance unit) =>
        state.Location(unit.Id)?.Location is { } at && RoutReach(state, unit, at, SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit)).Count > 1;

    /// <summary>
    /// Whether a broken unit ADJACENT to its captors can get away from every Known unbroken armed enemy unit only by Interdiction or Low Crawl (A20.21):
    /// no Location within its MF, reached without entering an Open Ground Location an enemy unit could Interdict, is clear of them.
    /// </summary>
    private bool TrappedByInterdiction(GameState state, UnitInstance unit, BoardLocation start)
    {
        var reach = RoutReach(state, unit, start, SeenBy(state, unit.Side, start), 0, RoutHalfMf(unit), avoidInterdiction: true);
        return !reach.Keys.Any(at => at != start && NearUnbrokenArmedEnemy(state, unit.Side, at) is null);
    }

    /// <summary>A10.5: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three.</summary>
    private int RoutHalfMf(UnitInstance unit) => vocabulary.IsA(unit.Kind, "asl:smc") && Is(unit, Conditions.Wounded) ? 6 : 12;

    private GamePlan PlanRout(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label, string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a rout names the broken unit and its route");
        }

        var lowCrawl = arguments.TryGetProperty("lowCrawl", out var crawl) && crawl.ValueKind == JsonValueKind.True;
        var route = new List<BoardLocation>();
        foreach (var text in Strings(arguments, "route"))
        {
            if (!BoardLocation.TryParse(text, out var step))
            {
                return Refused(scope, label, expected, $"play.invalid-arguments: '{text}' is not a Location such as bd01:E4:0");
            }

            route.Add(step);
        }

        if (state.Phase != "rtph")
        {
            return Refused(scope, label, expected, "play.rout-phase: broken units rout in the RtPh (A10.5)");
        }

        // A2.1 (ruling R20.6): a rout never leaves the card's playable area.
        if (route.Select(step => PlayableBar(state, step)).FirstOrDefault(bar => bar is not null) is { } outside)
        {
            return Refused(scope, label, expected, outside);
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit || !Is(unit, Conditions.Broken) || Is(unit, Conditions.Melee)
            || Is(unit, Conditions.Captured) || state.Location(unit.Id)?.Location is not { } start)
        {
            return Refused(scope, label, expected, $"play.rout-unit: '{unitId}' is not a broken unit on the map outside Melee (A10.5)");
        }

        if (state.RoutedThisPhase.Contains(unit.Id))
        {
            return Refused(scope, label, expected, $"play.rout-unit: {unit.Id} has routed this RtPh (A10.5)");
        }

        if (Is(unit, Conditions.Pinned))
        {
            return Refused(scope, label, expected, $"play.rout-unit: {unit.Id} is pinned and routs no further this RtPh (A10.53)");
        }

        if (!MayRout(state, unit))
        {
            return Refused(scope, label, expected, $"play.rout-not-allowed: {unit.Id} need not rout and is not under DM, so it may not rout (A10.5)");
        }

        // A10.5: the ATTACKER's broken units rout first, one at a time, then the DEFENDER's; one with no legal step does not hold the DEFENDER up.
        var attacker = state.PhasingSide;
        if (unit.Side == attacker && state.RoutedThisPhase.Any(id => state.Unit(id) is { } routed && routed.Side != attacker))
        {
            return Refused(scope, label, expected, "play.rout-order: the DEFENDER's units have begun to rout, so the ATTACKER's may not (A10.5)");
        }

        if (unit.Side != attacker && state.Units.FirstOrDefault(other => other.Side == attacker && !state.RoutedThisPhase.Contains(other.Id)
            && !Is(other, Conditions.Pinned) && MustRout(state, other) is not null && CanRout(state, other)) is { } first)
        {
            return Refused(scope, label, expected, $"play.rout-order: the ATTACKER's {first.Id} must rout first (A10.5)");
        }

        // E1.54 (backlog pass 16, ruling R16.6): at night a broken unit always Low Crawls, and surrenders only in CC.
        if (state.Night && !lowCrawl)
        {
            return Refused(scope, label, expected, $"play.night-rout: at night {unit.Id} does not rout normally but Low Crawls (lowCrawl) (E1.54)");
        }

        // A20.21: a unit ADJACENT to its captors that is Disrupted, Encircled, or can get away only by Interdiction or Low Crawl surrenders instead.
        if (!state.Night && !Is(unit, Conditions.Fanatic) && !state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) && Captors(state, unit) is { Count: > 0 } captors
            && (Is(unit, Conditions.Disrupted) ? "is Disrupted" : state.Encircled(unit) ? "is Encircled"
                : TrappedByInterdiction(state, unit, start) ? $"can get away from {string.Join(" or ", captors)} only by Interdiction or Low Crawl" : null) is { } cause)
        {
            return Refused(scope, label, expected, $"play.rout-surrender: {unit.Id} {cause}, so it surrenders to {string.Join(" or ", captors)} as the RtPh ends instead of routing (A20.21)");
        }

        // A10.4 (read in the PDF, p. 66; ruling R31d.1): before it routs a broken unit leaves in its Location what it carries beyond its IPC, and routs
        // with the most PP it can carry within it; the choice is the owner's only among loads of equal PP. The rout does the leaving.
        IReadOnlyList<RoutLoadItem> left = [];
        IReadOnlyList<string> kept = [];
        if (RoutLoadOf(state, unit) is { Laden: true } load)
        {
            string[]? named = arguments.TryGetProperty("keep", out var keep) && keep.ValueKind == JsonValueKind.Array ? [.. Strings(arguments, "keep").Order(StringComparer.Ordinal)] : null;
            // Loads that differ only in which of two like counters is kept are one choice (the table player, pass 31d), and the game takes it.
            var chosen = named is null ? (load.Choices.Count == 1 ? load.Choices[0] : null)
                : load.BestLoads.FirstOrDefault(best => best.Order(StringComparer.Ordinal).SequenceEqual(named, StringComparer.Ordinal));
            if (chosen is null)
            {
                var loads = string.Join(", or ", load.Choices.Select(best => best.Count == 0 ? "no SW" : string.Join(" with ", best.Select(id => $"{id} ({load.Pp(id)} PP)"))));
                return Refused(scope, label, expected, $"play.rout-laden: {unit.Id} carries {load.Total} PP and routs with at most {load.Ipc} PP; its owner chooses what it keeps: {loads}; it leaves the rest (A10.4)");
            }

            (kept, left) = (chosen, load.Left(chosen));
        }

        if (route.Count == 0 || (lowCrawl && route.Count != 1))
        {
            return Refused(scope, label, expected, "play.rout-route: a rout names at least one Location, and Low Crawl exactly one (A10.52)");
        }

        if (lowCrawl && !state.Night && state.At(start).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active && other.Side != unit.Side
            && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured)))
        {
            return Refused(scope, label, expected, "play.rout-low-crawl: Low Crawl does not leave an enemy-occupied Location (A10.52)");
        }

        // A10.51: each step, its cost, and the Known armed enemy units that have had the unit in their LOS.
        var limit = RoutHalfMf(unit);
        var encircled = state.Encircled(unit);
        var seenBy = SeenBy(state, unit.Side, start).ToHashSet(StringComparer.Ordinal);
        var costs = new List<int>();
        var spent = 0;
        var from = start;
        foreach (var to in route)
        {
            if (!Neighbors(state, from).Contains(to))
            {
                return Refused(scope, label, expected, $"play.rout-route: {to} is not ADJACENT to {from}");
            }

            if (RoutStepBar(state, unit.Side, from, to, seenBy) is { } bar)
            {
                return Refused(scope, label, expected, bar);
            }

            if (lowCrawl && !state.Night && ReadLocation(state, to) is { } crawled && TerrainKey(crawled) == "marsh")
            {
                return Refused(scope, label, expected, "play.rout-low-crawl: Low Crawl does not enter marsh (A10.52)");
            }

            var (halfMf, allMf, why) = RoutEntry(state, from, to, encircled && spent == 0);
            if (halfMf is null && !allMf)
            {
                return Refused(scope, label, expected, why!);
            }

            if (allMf && spent > 0)
            {
                return Refused(scope, label, expected, $"play.rout-route: entering {to} takes all of {unit.Id}'s MF, so it is its only step (A4.134)");
            }

            var cost = lowCrawl || allMf ? limit - spent : halfMf!.Value;
            spent += cost;
            if (spent > limit)
            {
                return Refused(scope, label, expected, $"play.rout-mf: the route costs more than {unit.Id}'s {limit / 2} MF in the RtPh (A10.5)");
            }

            costs.Add(cost);
            seenBy.UnionWith(SeenBy(state, unit.Side, to));
            from = to;
        }

        // A10.51: a unit ADJACENT to a Known armed enemy unit does not end its rout ADJACENT to that same unit.
        var near = KnownEnemies(state, unit.Side).Where(item => Armed(item.Unit) && AdjacentOrSame(state, item.At, start)).ToArray();
        if (near.FirstOrDefault(item => AdjacentOrSame(state, item.At, route[^1])) is { Unit: { } still })
        {
            return Refused(scope, label, expected, $"play.rout-route: {unit.Id} began ADJACENT to {still.Id} and may not end its rout ADJACENT to it (A10.51)");
        }

        // A10.51, A10.532: it must reach the nearest woods or building Location within its MF this RtPh, not necessarily by a shortest route; with none,
        // any legal route. Low Crawl moves one Location toward it.
        var startSeen = SeenBy(state, unit.Side, start).ToArray();
        var targets = RoutTargets(state, unit, start, RoutReach(state, unit, start, startSeen, 0, limit));
        if (targets.Length > 0)
        {
            if (lowCrawl)
            {
                var whole = RoutReach(state, unit, start, startSeen, 0, 4 * limit);
                var after = RoutReach(state, unit, route[0], startSeen.Concat(SeenBy(state, unit.Side, route[0])), 0, 4 * limit);
                if (!targets.Any(target => after.TryGetValue(target, out var rest) && whole.TryGetValue(target, out var all) && rest < all))
                {
                    return Refused(scope, label, expected, $"play.rout-destination: Low Crawl moves toward the nearest woods or building Location, {string.Join(" or ", targets)} (A10.52)");
                }
            }
            else
            {
                var reached = route.FindIndex(targets.Contains);
                if (reached < 0)
                {
                    return Refused(scope, label, expected, $"play.rout-destination: {unit.Id} must rout to the nearest woods or building Location, {string.Join(" or ", targets)} (A10.51)");
                }

                if (route.Skip(reached + 1).FirstOrDefault(step => !RoutCover(state, step)) is { } beyond)
                {
                    return Refused(scope, label, expected, $"play.rout-destination: having reached {route[reached]}, {unit.Id} goes on only into woods or building Locations, not {beyond} (A10.51)");
                }
            }
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var events = new List<GameEvent>();
            var routing = unit;
            var rolls = 0;
            var used = 0;

            // A10.4, A4.431: what it leaves is unpossessed in the Location it routs from, written as a drop is.
            foreach (var item in left)
            {
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "equipment-transferred", new EquipmentTransferred(item.Weapon, null, new MapPosition(start)), package, null));
            }

            for (var index = 0; index < route.Count; index++)
            {
                // A10.5: a leader wounded on the way routs on only with the MF a wounded SMC has.
                if (used + costs[index] > RoutHalfMf(routing))
                {
                    break;
                }

                var to = route[index];
                used += costs[index];
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "rout-stepped", new RoutStepped(routing.Id, to, costs[index], lowCrawl), null, null));
                var after = Replay([.. existing, .. events]).Current!;
                AddAdjacentDm(scope, attemptId, expected, after, events);

                // A10.53: Interdiction as it enters an Open Ground hex without Low Crawl, once per hex.
                if (lowCrawl || route.IndexOf(to) != index || Interdictor(after, routing.Side, to) is not { } interdictor)
                {
                    continue;
                }

                var drawn = draw(new RollRequest(2, 6));
                rolls++;
                var rollId = $"{attemptId}-roll-{rolls.ToString(CultureInfo.InvariantCulture)}";
                var current = after.Unit(routing.Id)!;
                var morale = BrokenMorale(current) ?? 0;
                var original = drawn.Values[0] + drawn.Values[1];
                var result = RoutInterdicted.For(original, original, morale);
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "dice-rolled",
                    new DiceRolled(rollId, "interdiction", 2, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null));
                var record = EventId(attemptId, events.Count + 1);
                events.Add(Event(scope, attemptId, events.Count + 1, expected, "rout-interdicted", new RoutInterdicted(routing.Id, to, rollId, morale, 0, result) { Interdictor = interdictor }, package, null));
                if (result == RoutInterdicted.Passed)
                {
                    continue;
                }

                if (result == RoutInterdicted.Pinned)
                {
                    // A10.53: pinned, it routs no further this RtPh.
                    events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                        new ConditionsChanged(routing.Id, new Dictionary<string, ConditionState> { [Conditions.Pinned] = ConditionState.True }), package, null, [record]));
                    break;
                }

                var (type, payload) = result == RoutInterdicted.Reduced ? CasualtyReduction(current, attemptId) : ("instance-eliminated", new InstanceEliminated(routing.Id));
                events.Add(Event(scope, attemptId, events.Count + 1, expected, type, payload, package, null, [record]));

                // A10.53: a HS left by Casualty Reduction, or a wounded SMC, routs on; an eliminated unit does not.
                if (payload is InstanceEliminated)
                {
                    break;
                }

                routing = Replay([.. existing, .. events]).Current!.Unit(payload is LineageRecorded { Produced: [{ } half] } ? half.Id : routing.Id)!;
            }

            return events;
        }

        var steps = string.Join(", ", route.Select((step, index) => $"{step} ({(costs[index] / 2.0).ToString("0.#", CultureInfo.InvariantCulture)} MF)"));
        string[] threatened = lowCrawl ? [] : [.. route.Distinct().Select(step => (step, By: Interdictor(state, unit.Side, step))).Where(item => item.By is not null)
            .Select(item => $"{item.step} by {item.By}")];
        var interdiction = lowCrawl ? "Low Crawl is never Interdicted (A10.52)"
            : threatened.Length == 0 ? "no step enters Open Ground an enemy unit could Interdict (A10.53)"
            : $"Interdicted as it enters {string.Join(", ", threatened)}, a NMC each (A10.53)";
        string[] leaves = left.Count == 0 ? []
            : [$"play.rout-leaves: {unit.Id} leaves {string.Join(" and ", left.Select(item => $"{item.Weapon} ({item.Pp} PP)"))} in {start}, unpossessed, and routs with "
                + $"{(kept.Count == 0 ? "no SW" : string.Join(" and ", kept))} (A10.4, A4.431)"];
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [], [$"play.rout: {unit.Id} routs{(lowCrawl ? " by Low Crawl" : "")} to {steps}; {interdiction}", .. leaves])
        {
            Roll = new PlannedRoll("rout", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }

    /// <summary>The broken Morale Level (A10.4), one lower for a wounded SMC (A17.3), from the catalog.</summary>
    private static int? BrokenMorale(UnitInstance unit) =>
        unit.Definition is { } reference && FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) is { } definition
            ? (definition.BrokenMorale ?? definition.Morale) - (Is(unit, Conditions.Wounded) ? 1 : 0)
            : null;

    /// <summary>Casualty Reduction (A7.302): a squad becomes its HS, a SMC is wounded, or eliminated if already wounded, and anything else is eliminated.</summary>
    private static (string Type, EventPayload Payload) CasualtyReduction(UnitInstance unit, string attemptId)
    {
        if (unit.Kind == "asl:squad" && unit.Definition is { } squad && ScenarioA1FireReference.HalfSquadOf(squad.Definition) is { } half)
        {
            return ("lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", "asl:half-squad", half, unit.Side, unit.Position, null,
                    new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal))]));
        }

        return unit.Kind is "asl:leader" or "asl:hero" && !Is(unit, Conditions.Wounded)
            ? ("conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Wounded] = ConditionState.True }))
            : ("instance-eliminated", new InstanceEliminated(unit.Id));
    }

    /// <summary>A10.62 (ruling R13.1): the broken units not under DM with a Known armed enemy unit ADJACENT to them or in their Location.</summary>
    private UnitInstance[] AdjacentDm(GameState state) =>
        [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.DesperationMorale)
                && !Is(unit, Conditions.Captured) && state.Location(unit.Id)?.Location is { } at
                && KnownEnemies(state, unit.Side).Any(item => Armed(item.Unit) && AdjacentOrSame(state, item.At, at)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];

    /// <summary>Adds the DM that Known armed enemy units ADJACENT to broken units give them (A10.62; ruling R13.1), after the events so far.</summary>
    private void AddAdjacentDm(GameScope scope, string attemptId, long expected, GameState state, List<GameEvent> events)
    {
        foreach (var unit in AdjacentDm(state))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.DesperationMorale] = ConditionState.True }), null, null));
        }
    }

    /// <summary>
    /// A plan whose events are followed by the DM they give broken units (A10.62; ruling R13.1): a unit that moves, advances, or is revealed ADJACENT to
    /// a broken enemy unit, or a phase change, puts it under DM; a plan that rolls adds it once its rolls are drawn.
    /// </summary>
    private GamePlan WithAdjacentDm(GamePlan plan, GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected)
    {
        if (plan.Status != GamePlanStatus.Ready)
        {
            return plan;
        }

        if (plan.Roll is { } roll)
        {
            return plan with
            {
                Roll = roll with
                {
                    Build = draw =>
                    {
                        var events = roll.Build(draw).ToList();
                        if (Replay([.. existing, .. events]) is { HasErrors: false, Current: { } rolled })
                        {
                            AddAdjacentDm(scope, attemptId, expected, rolled, events);
                        }

                        return events;
                    },
                },
            };
        }

        if (plan.Events.Count == 0 || Replay([.. existing, .. plan.Events]) is not { HasErrors: false, Current: { } after } || AdjacentDm(after) is not { Length: > 0 } gaining)
        {
            return plan;
        }

        var events = plan.Events.ToList();
        AddAdjacentDm(scope, attemptId, expected, after, events);
        return plan with
        {
            Events = events,
            Reasons = [.. plan.Reasons, $"play.dm: {string.Join(", ", gaining.Select(unit => unit.Id))} come under DM from an ADJACENT Known armed enemy unit (A10.62)"],
        };
    }

    /// <summary>A10.62 (ruling R13.1): at the start of the RtPh a broken unit in Open Ground in the LOS and Normal Range of a Known enemy unit comes under DM.</summary>
    private (UnitInstance Unit, string Why)[] RoutPhaseDm(GameState state)
    {
        var gaining = new List<(UnitInstance, string)>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.DesperationMorale)
            && !Is(unit, Conditions.Captured)).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (state.Location(unit.Id)?.Location is { } at && ExposedInOpenGround(state, unit.Side, at) is { } seen)
            {
                gaining.Add((unit, $"in Open Ground in the LOS and Normal Range of {seen}"));
            }
        }

        return [.. gaining];
    }

    /// <summary>
    /// What a player needs before routing a broken unit (A10.5, A10.51; table player, pass 13): the woods or building Locations it must reach, and
    /// whether it has any legal step.
    /// </summary>
    public (IReadOnlyList<BoardLocation> Targets, bool CanRout, IReadOnlyDictionary<BoardLocation, IReadOnlyList<BoardLocation>> Routes) RoutAdvice(GameState state,
        UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        if (state.Location(unit.Id)?.Location is not { } at)
        {
            return ([], false, new Dictionary<BoardLocation, IReadOnlyList<BoardLocation>>());
        }

        // Pass 31d (design D3): one least-cost route to each place the rout may end, from the search's own steps. The search reads Known enemy
        // units alone (A10.533), so a route tells the routing side nothing it does not hold. A place the unit may not end in, since it began
        // ADJACENT to a Known armed enemy unit that is ADJACENT to that place too (A10.51), is given no route.
        var found = new Dictionary<BoardLocation, BoardLocation[]>();
        var reach = RoutReach(state, unit, at, SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit), routes: found);
        var targets = RoutTargets(state, unit, at, reach);
        var near = KnownEnemies(state, unit.Side).Where(item => Armed(item.Unit) && AdjacentOrSame(state, item.At, at)).ToArray();
        // A least-cost way to one place may run through another and out into the open again, which a rout may not do once it has reached woods or a
        // building (A10.51): such a way is not offered (found by the sweep of The Tractor Works, pass 31d).
        bool Holds(BoardLocation[] route) => Array.FindIndex(route, targets.Contains) is var reached && route.Skip(reached + 1).All(step => RoutCover(state, step));
        return (targets, reach.Count > 1, targets.Where(target => found.TryGetValue(target, out var route) && Holds(route) && !near.Any(item => AdjacentOrSame(state, item.At, target)))
            .ToDictionary(target => target, target => (IReadOnlyList<BoardLocation>)found[target]));
    }

    /// <summary>Whether a unit may keep its DM as the RPh ends (A10.62; ruling R13.1).</summary>
    public bool MayRetainDm(GameState state, string id) => RetainDmBar(state, id) is null;

    /// <summary>Why a unit may not keep its DM as the RPh ends (A10.62; ruling R13.1), or null: it is a broken unit under DM, not in woods or a building.</summary>
    private string? RetainDmBar(GameState state, string id) =>
        state.Unit(id) is not { Status: InstanceStatus.Active } unit || !Is(unit, Conditions.Broken) || !Is(unit, Conditions.DesperationMorale)
            ? $"play.retain-dm: '{id}' is not a broken unit under DM (A10.62)"
            : state.Location(unit.Id)?.Location is not { } at || RoutCover(state, at)
                ? $"play.retain-dm: {id} is in a woods or building Location, where DM is not retained (A10.62)"
                : null;

    /// <summary>
    /// The broken units the end of the RtPh eliminates for Failure to Rout (A10.5, A10.53; ruling R13.3), with why: ADJACENT to or in the Location of a
    /// Known unbroken armed enemy unit; or, having not routed, in Open Ground in the LOS and Normal Range of a Known enemy unit. Those with captors
    /// (A20.21) surrender instead, unless Fanatic, under No Quarter, or already rejected.
    /// </summary>
    private (UnitInstance Unit, string Why, IReadOnlyList<string>? Captors)[] FailureToRout(GameState state, IReadOnlyList<GameEvent> existing)
    {
        // E1.54 (backlog pass 16, ruling R16.6): no unit is eliminated for Failure to Rout at night.
        if (state.Night)
        {
            return [];
        }

        var start = existing.Select((item, index) => (item, index)).LastOrDefault(pair => pair.item.Payload is PhaseChanged).index;
        var rejected = existing.Skip(start).Select(item => item.Payload).OfType<SurrenderRejected>().Select(item => item.Unit).ToHashSet(StringComparer.Ordinal);
        var failed = new List<(UnitInstance, string, IReadOnlyList<string>?)>();
        foreach (var unit in state.Units.Where(unit => unit.Status == InstanceStatus.Active && Is(unit, Conditions.Broken) && !Is(unit, Conditions.Melee)
            && !Is(unit, Conditions.Captured) && !LiveFire.IsVehicle(unit) && state.Aboard(unit.Id) is null).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (state.Location(unit.Id)?.Location is not { } at)
            {
                continue;
            }

            var why = NearUnbrokenArmedEnemy(state, unit.Side, at) is { } enemy ? $"it is ADJACENT to or in the Location of the Known unbroken armed enemy unit {enemy}"
                : !state.RoutedThisPhase.Contains(unit.Id) && !Is(unit, Conditions.Pinned) && ExposedInOpenGround(state, unit.Side, at) is { } seen
                    ? $"it did not rout from Open Ground in the LOS and Normal Range of {seen}" : null;
            if (why is null)
            {
                continue;
            }

            var surrenders = !Is(unit, Conditions.Fanatic) && !state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal) && !rejected.Contains(unit.Id)
                && Captors(state, unit) is { Count: > 0 } captors ? captors : null;
            failed.Add((unit, why, surrenders));
        }

        return [.. failed];
    }
}
