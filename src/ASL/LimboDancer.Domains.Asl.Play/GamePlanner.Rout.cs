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
        if (!ScenarioA1RoutCalculator.MustRoutCandidate(unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken), Is(unit, Conditions.Melee),
                Is(unit, Conditions.Captured), state.Aboard(unit.Id) is not null)
            || state.Location(unit.Id)?.Location is not { } at)
        {
            return null;
        }

        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.MustRout(scan, scan.Enemies(unit.Side), scan.Index(at), state.ScenarioMonth);
    }

    /// <summary>Whether a broken unit may rout (A10.5): it must, or it is under DM.</summary>
    public bool MayRout(GameState state, UnitInstance unit) =>
        ScenarioA1RoutCalculator.MayRout(MustRout(state, unit), unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken), Is(unit, Conditions.DesperationMorale),
            Is(unit, Conditions.Melee), Is(unit, Conditions.Captured));

    /// <summary>
    /// The rout's fact reader (pass 32.f; the pass 32 design, D4): a table of Locations by index, the Known enemy units of a side as Rules reads them, and
    /// the map and state reads the search and the scans make through it. The LOS keeps its cache in the planner.
    /// </summary>
    private sealed class RoutScan : IRoutFactReader
    {
        private readonly GamePlanner planner;
        private readonly GameState state;
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];
        private readonly Dictionary<string, IReadOnlyList<RoutEnemyFacts>> enemies = new(StringComparer.Ordinal);

        public RoutScan(GamePlanner planner, GameState state)
        {
            this.planner = planner;
            this.state = state;
        }

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        /// <summary>The Location at an index of the table.</summary>
        public BoardLocation At(int index) => locations[index];

        /// <summary>The Known enemy units of a side (A10.51, A10.533) as the rout reads them, in the state's order, read once a side.</summary>
        public IReadOnlyList<RoutEnemyFacts> Enemies(string side)
        {
            if (!enemies.TryGetValue(side, out var known))
            {
                enemies[side] = known = [.. KnownEnemies(state, side).Select(item => new RoutEnemyFacts(item.Unit.Id, Index(item.At), Armed(item.Unit),
                    Is(item.Unit, Conditions.Broken), Is(item.Unit, Conditions.Melee), LiveFire.IsVehicle(item.Unit), Is(item.Unit, Conditions.Cx),
                    Is(item.Unit, Conditions.Pinned), state.Encircled(item.Unit), NormalRange(state, item.Unit)))];
            }

            return known;
        }

        public string Name(int location) => locations[location].ToString();

        public IEnumerable<int> Neighbors(int location) => planner.Neighbors(state, locations[location]).Select(Index);

        public bool Playable(int location) => planner.PlayableBar(state, locations[location]) is null;

        public RoutLocationFacts? Location(int location) =>
            locations[location] is var at && planner.ReadLocation(state, at) is { } read ? new RoutLocationFacts(TerrainKey(read), HasSmoke(state, at)) : null;

        public RoutLosFacts? Los(int fromLocation, int toLocation) =>
            planner.Los(state, locations[fromLocation], locations[toLocation]) is { } los ? new RoutLosFacts(los.Status == LosStatus.Clear, los.Hindrance, los.Range) : null;

        public int? Distance(int one, int two) => planner.HexDistance(state, locations[one], locations[two]);

        public bool Adjacent(int one, int two) => planner.IsAdjacent(state, locations[one], locations[two]);

        public (InfantryEntry? Entry, string? Reason) Entry(int fromLocation, int toLocation) => planner.InfantryStep(state, locations[fromLocation], locations[toLocation]);
    }

    /// <summary>A Known enemy unit (A10.51, A10.533): not a Dummy, concealed, hidden, or a prisoner.</summary>
    private static IEnumerable<(UnitInstance Unit, BoardLocation At)> KnownEnemies(GameState state, string side) =>
        state.Units.Where(other => other.Status == InstanceStatus.Active && other.Side != side && KnownEnemy(other))
            .Select(other => (Unit: other, At: state.Location(other.Id)?.Location)).Where(item => item.At is not null).Select(item => (item.Unit, item.At!));

    /// <summary>A10.5, A10.62: an armed enemy unit is Personnel (a leader without a SW counts), or a vehicle not Abandoned.</summary>
    private static bool Armed(UnitInstance unit) => ScenarioA1RoutCalculator.Armed(LiveFire.IsVehicle(unit), Is(unit, Conditions.Abandoned));

    private bool AdjacentOrSame(GameState state, BoardLocation one, BoardLocation two) => one == two || IsAdjacent(state, one, two);

    /// <summary>A Known unbroken armed enemy unit ADJACENT to a Location or in it (A10.5), or null.</summary>
    private string? NearUnbrokenArmedEnemy(GameState state, string side, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.NearUnbrokenArmedEnemy(scan, scan.Enemies(side), scan.Index(at));
    }

    /// <summary>
    /// The Normal Range in hexes of a unit's fire (A10.532): the longest of its own printed range and the Normal Ranges of the functioning SW it
    /// possesses, at most 16; a leader without a SW has none. Vehicles and Guns have none here (backlog).
    /// </summary>
    private static int NormalRange(GameState state, UnitInstance unit)
    {
        var definitions = FireReference.Value.Definitions;
        var definition = unit.Definition is { } reference ? definitions.GetValueOrDefault(reference.Definition) : null;
        var own = ScenarioA1RoutCalculator.OwnRange(definition is not null, definition?.Kind == "asl:leader", definition?.Range);
        var weapons = state.Equipment.Where(item => item.Status == InstanceStatus.Active && item.Holding is { Role: HoldingRole.Possessed } holding
                && holding.Holder == unit.Id && !Is(item, Conditions.Malfunctioned) && !Is(item, Conditions.Dismantled))
            .Select(item => item.Definition is { } weapon ? definitions.GetValueOrDefault(weapon.Definition)?.Range ?? 0 : 0);
        return ScenarioA1RoutCalculator.NormalRange(own, weapons);
    }

    /// <summary>
    /// A Known unbroken enemy unit not in Melee in whose LOS and Normal Range a Location in Open Ground lies, with no Hindrance between (A10.5, A10.531), or null.
    /// </summary>
    private string? ExposedInOpenGround(GameState state, string side, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.ExposedInOpenGround(scan, scan.Enemies(side), scan.Index(at), state.ScenarioMonth);
    }

    /// <summary>The enemy unit able to Interdict a routing unit entering an Open Ground Location (A10.53, A10.532, A10.533; ruling R13.3), or null.</summary>
    private string? Interdictor(GameState state, string side, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.Interdictor(scan, scan.Enemies(side), scan.Index(at), state.ScenarioMonth);
    }

    /// <summary>Why a rout step from one Location to an ADJACENT one is not allowed (A10.5, A10.51; ruling R13.3), or null.</summary>
    private string? RoutStepBar(GameState state, string side, BoardLocation from, BoardLocation to, IReadOnlyCollection<string> seenBy)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.RoutStepBar(scan, scan.Enemies(side), scan.Index(from), scan.Index(to), seenBy);
    }

    /// <summary>The Known armed enemy units with a clear LOS to a Location (A10.51).</summary>
    private IEnumerable<string> SeenBy(GameState state, string side, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.SeenBy(scan, scan.Enemies(side), scan.Index(at));
    }

    /// <summary>The half MF a routing unit pays to step between ADJACENT Locations (A10.5, A7.7; ruling R12.11), or why it may not.</summary>
    private (int? HalfMf, bool AllMf, string? Reason) RoutEntry(GameState state, BoardLocation from, BoardLocation to, bool encircledFirst)
    {
        var scan = new RoutScan(this, state);
        var (halfMf, allMf, reason) = ScenarioA1RoutCalculator.RoutEntry(scan, scan.Index(from), scan.Index(to), encircledFirst);
        return (halfMf, allMf, reason);
    }

    /// <summary>A10.51: a woods or building Location is a rout destination.</summary>
    private bool RoutCover(GameState state, BoardLocation at)
    {
        var scan = new RoutScan(this, state);
        return ScenarioA1RoutCalculator.RoutCover(scan.Location(scan.Index(at)));
    }

    /// <summary>
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
        state.Location(unit.Id)?.Location is { } at && ScenarioA1RoutCalculator.CanRout(RoutReach(state, unit, at, SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit)).Count);

    /// <summary>Whether a broken unit ADJACENT to its captors can get away from every Known unbroken armed enemy unit only by Interdiction or Low Crawl (A20.21).</summary>
    private bool TrappedByInterdiction(GameState state, UnitInstance unit, BoardLocation start)
    {
        var scan = new RoutScan(this, state);
        var reach = ScenarioA1RoutCalculator.RoutReach(scan, scan.Enemies(unit.Side), scan.Index(start), SeenBy(state, unit.Side, start), 0, RoutHalfMf(unit), state.Encircled(unit),
            state.ScenarioMonth, avoidInterdiction: true);
        return ScenarioA1RoutCalculator.TrappedByInterdiction(scan, scan.Enemies(unit.Side), scan.Index(start), reach);
    }

    /// <summary>A10.5: the half MF a broken unit has in the RtPh, six MF, a wounded SMC three.</summary>
    private int RoutHalfMf(UnitInstance unit) => ScenarioA1RoutCalculator.RoutHalfMfAsPlanned(vocabulary.IsA(unit.Kind, "asl:smc"), Is(unit, Conditions.Wounded));

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

        if (ScenarioA1RoutCalculator.RoutPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        // A2.1 (ruling R20.6): a rout never leaves the card's playable area.
        if (route.Select(step => PlayableBar(state, step)).FirstOrDefault(bar => bar is not null) is { } outside)
        {
            return Refused(scope, label, expected, outside);
        }

        if (state.Unit(unitId) is not { Status: InstanceStatus.Active } unit
            || !ScenarioA1RoutCalculator.RoutUnitAllowed(Is(unit, Conditions.Broken), Is(unit, Conditions.Melee), Is(unit, Conditions.Captured))
            || state.Location(unit.Id)?.Location is not { } start)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutUnitText(unitId));
        }

        if (ScenarioA1RoutCalculator.RoutedBar(unit.Id, state.RoutedThisPhase.Contains(unit.Id)) is { } routedBar)
        {
            return Refused(scope, label, expected, routedBar);
        }

        if (ScenarioA1RoutCalculator.RoutPinnedBar(unit.Id, Is(unit, Conditions.Pinned)) is { } pinnedBar)
        {
            return Refused(scope, label, expected, pinnedBar);
        }

        if (ScenarioA1RoutCalculator.MayRoutBar(unit.Id, MayRout(state, unit)) is { } mayRoutBar)
        {
            return Refused(scope, label, expected, mayRoutBar);
        }

        // A10.5: the ATTACKER's broken units rout first, one at a time, then the DEFENDER's; one with no legal step does not hold the DEFENDER up.
        var attacker = state.PhasingSide;
        if (ScenarioA1RoutCalculator.RoutOrderBar(unit.Side == attacker, () => state.RoutedThisPhase.Any(id => state.Unit(id) is { } routed && routed.Side != attacker)) is { } orderBar)
        {
            return Refused(scope, label, expected, orderBar);
        }

        if (unit.Side != attacker && state.Units.FirstOrDefault(other => ScenarioA1RoutCalculator.AttackerMustRoutFirst(other.Side == attacker, state.RoutedThisPhase.Contains(other.Id),
            Is(other, Conditions.Pinned), () => MustRout(state, other) is not null, () => CanRout(state, other))) is { } first)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.AttackerFirstText(first.Id));
        }

        // E1.54 (backlog pass 16, ruling R16.6): at night a broken unit always Low Crawls, and surrenders only in CC.
        if (ScenarioA1RoutCalculator.NightRoutBar(unit.Id, state.Night, lowCrawl) is { } nightBar)
        {
            return Refused(scope, label, expected, nightBar);
        }

        // A20.21: a unit ADJACENT to its captors that is Disrupted, Encircled, or can get away only by Interdiction or Low Crawl surrenders instead.
        if (ScenarioA1RoutCalculator.SurrenderCandidate(state.Night, Is(unit, Conditions.Fanatic), state.NoQuarter.Contains(unit.Side, StringComparer.Ordinal))
            && Captors(state, unit) is { Count: > 0 } captors
            && ScenarioA1RoutCalculator.SurrenderCause(Is(unit, Conditions.Disrupted), state.Encircled(unit), () => TrappedByInterdiction(state, unit, start), captors) is { } cause)
        {
            return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutSurrenderText(unit.Id, cause, captors));
        }

        // A10.4 (read in the PDF, p. 66; ruling R31d.1): before it routs a broken unit leaves in its Location what it carries beyond its IPC, and routs
        // with the most PP it can carry within it; the choice is the owner's only among loads of equal PP. The rout does the leaving.
        IReadOnlyList<RoutLoadItem> left = [];
        IReadOnlyList<string> kept = [];
        if (RoutLoadOf(state, unit) is { Laden: true } load)
        {
            string[]? named = arguments.TryGetProperty("keep", out var keep) && keep.ValueKind == JsonValueKind.Array ? [.. Strings(arguments, "keep").Order(StringComparer.Ordinal)] : null;
            // Loads that differ only in which of two like counters is kept are one choice (the table player, pass 31d), and the game takes it.
            var choices = load.Choices;
            var chosen = ScenarioA1RoutCalculator.ChosenLoad(named, choices, load.BestLoads);
            if (chosen is null)
            {
                return Refused(scope, label, expected, ScenarioA1RoutCalculator.RoutLadenText(unit.Id, load.Total, load.Ipc, choices, load.Pp));
            }

            (kept, left) = (chosen, load.Left(chosen));
        }

        if (ScenarioA1RoutCalculator.RouteShapeBar(route.Count, lowCrawl) is { } shapeBar)
        {
            return Refused(scope, label, expected, shapeBar);
        }

        if (ScenarioA1RoutCalculator.LowCrawlOccupiedBar(lowCrawl, state.Night, () => state.At(start).OfType<UnitInstance>().Any(other => other.Status == InstanceStatus.Active
            && other.Side != unit.Side && other.Kind != UnitKinds.Dummy && !Is(other, Conditions.Captured))) is { } occupiedBar)
        {
            return Refused(scope, label, expected, occupiedBar);
        }

        // A10.51: each step, its cost, and the Known armed enemy units that have had the unit in their LOS; Rules walks the route over the scan (pass 32.f).
        var limit = RoutHalfMf(unit);
        var encircled = state.Encircled(unit);
        var scan = new RoutScan(this, state);
        var enemies = scan.Enemies(unit.Side);
        var seenBy = ScenarioA1RoutCalculator.SeenBy(scan, enemies, scan.Index(start)).ToHashSet(StringComparer.Ordinal);
        int[] routeIndexes = [.. route.Select(scan.Index)];
        var (walkBar, costs, _) = ScenarioA1RoutCalculator.RoutRouteWalk(scan, enemies, unit.Id, scan.Index(start), routeIndexes, lowCrawl, state.Night, limit, encircled, seenBy);
        if (walkBar is not null)
        {
            return Refused(scope, label, expected, walkBar);
        }

        // A10.51, A10.532: no ending ADJACENT to an armed enemy it began ADJACENT to; it must reach the nearest woods or building Location within its MF
        // this RtPh, not necessarily by a shortest route; with none, any legal route. Low Crawl moves one Location toward it.
        if (ScenarioA1RoutCalculator.RoutDestinationBar(scan, enemies, unit.Id, scan.Index(start), routeIndexes, lowCrawl, limit, encircled, state.ScenarioMonth) is { } destinationBar)
        {
            return Refused(scope, label, expected, destinationBar);
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
    private static int? BrokenMorale(UnitInstance unit)
    {
        var definition = unit.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition) : null;
        return ScenarioA1RoutCalculator.BrokenMorale(definition is not null, definition?.BrokenMorale, definition?.Morale, Is(unit, Conditions.Wounded));
    }

    /// <summary>Casualty Reduction (A7.302): a squad becomes its HS, a SMC is wounded, or eliminated if already wounded, and anything else is eliminated.</summary>
    private static (string Type, EventPayload Payload) CasualtyReduction(UnitInstance unit, string attemptId)
    {
        var half = unit.Kind == "asl:squad" && unit.Definition is { } squad ? ScenarioA1FireReference.HalfSquadOf(squad.Definition) : null;
        return ScenarioA1RoutCalculator.CasualtyReduction(half is not null, unit.Kind is "asl:leader" or "asl:hero", Is(unit, Conditions.Wounded)) switch
        {
            CasualtyOutcome.Reduced => ("lineage", new LineageRecorded(LineageAction.Reduced, [unit.Id],
                [new NewInstance($"{attemptId}-{unit.Id}", "asl:half-squad", half!, unit.Side, unit.Position, null,
                    new Dictionary<string, ConditionState>(unit.Conditions, StringComparer.Ordinal))])),
            CasualtyOutcome.Wounded => ("conditions-changed", new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Wounded] = ConditionState.True })),
            _ => ("instance-eliminated", new InstanceEliminated(unit.Id)),
        };
    }

    /// <summary>A10.62 (ruling R13.1): the broken units not under DM with a Known armed enemy unit ADJACENT to them or in their Location.</summary>
    private UnitInstance[] AdjacentDm(GameState state)
    {
        var scan = new RoutScan(this, state);
        return [.. state.Units.Where(unit => ScenarioA1RoutCalculator.DmCandidate(unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken),
                    Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Captured)) && state.Location(unit.Id)?.Location is { } at
                && ScenarioA1RoutCalculator.ArmedEnemyNear(scan, scan.Enemies(unit.Side), scan.Index(at)))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)];
    }

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
            Reasons = [.. plan.Reasons, ScenarioA1RoutCalculator.AdjacentDmReason(gaining.Select(unit => unit.Id))],
        };
    }

    /// <summary>A10.62 (ruling R13.1): at the start of the RtPh a broken unit in Open Ground in the LOS and Normal Range of a Known enemy unit comes under DM.</summary>
    private (UnitInstance Unit, string Why)[] RoutPhaseDm(GameState state)
    {
        var scan = new RoutScan(this, state);
        var gaining = new List<(UnitInstance, string)>();
        foreach (var unit in state.Units.Where(unit => ScenarioA1RoutCalculator.DmCandidate(unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken),
            Is(unit, Conditions.DesperationMorale), Is(unit, Conditions.Captured))).OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (state.Location(unit.Id)?.Location is { } at && ScenarioA1RoutCalculator.ExposedInOpenGround(scan, scan.Enemies(unit.Side), scan.Index(at), state.ScenarioMonth) is { } seen)
            {
                gaining.Add((unit, ScenarioA1RoutCalculator.RoutPhaseDmReason(seen)));
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
        // units alone (A10.533), so a route tells the routing side nothing it does not hold. Rules decides which places are offered (pass 32.f).
        var scan = new RoutScan(this, state);
        var found = new Dictionary<int, int[]>();
        var reach = ScenarioA1RoutCalculator.RoutReach(scan, scan.Enemies(unit.Side), scan.Index(at), SeenBy(state, unit.Side, at), 0, RoutHalfMf(unit), state.Encircled(unit),
            state.ScenarioMonth, routes: found);
        var (targets, canRout, routes) = ScenarioA1RoutCalculator.RoutAdvice(scan, scan.Enemies(unit.Side), scan.Index(at), reach, found);
        return ([.. targets.Select(scan.At)], canRout, routes.ToDictionary(item => scan.At(item.Key), item => (IReadOnlyList<BoardLocation>)[.. item.Value.Select(scan.At)]));
    }

    /// <summary>Whether a unit may keep its DM as the RPh ends (A10.62; ruling R13.1).</summary>
    public bool MayRetainDm(GameState state, string id) => RetainDmBar(state, id) is null;

    /// <summary>Why a unit may not keep its DM as the RPh ends (A10.62; ruling R13.1), or null: it is a broken unit under DM, not in woods or a building.</summary>
    private string? RetainDmBar(GameState state, string id)
    {
        var unit = state.Unit(id);
        return ScenarioA1RoutCalculator.RetainDmBar(id, unit is { Status: InstanceStatus.Active }, unit is not null && Is(unit, Conditions.Broken),
            unit is not null && Is(unit, Conditions.DesperationMorale), () => state.Location(unit!.Id)?.Location is not { } at || RoutCover(state, at));
    }

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
