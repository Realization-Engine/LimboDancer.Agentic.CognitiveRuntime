using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Something a side knows is at a location without knowing what (A12.11, p. 76): a concealed enemy unit, reduced to its
/// side and location. <see cref="PlacementId"/> is a key for the display, numbered by location, never the unit's id.
/// Every condition of the unit is withheld.
/// </summary>
public sealed record SealedPresence(string PlacementId, string Side, BoardLocation Location)
{
    /// <summary>
    /// A counter beneath the top of an enemy stack that is not under "?", before play starts: "No enemy stack ... may be inspected prior to the start of
    /// play" (A2.9; pass 23, ruling R23.3). The top counter is shown; the rest are counted, not identified.
    /// </summary>
    public bool Uninspected
    {
        get; init;
    }
}

/// <summary>
/// A game state as one perspective may know it (ASL-UNIT-030, 031). A side's view leaves out what that side cannot
/// know rather than marking it for a consumer to hide: an enemy's hidden instances (A12.3, p. 80) and everything
/// inside or held by them are absent; an enemy's concealed units become <see cref="SealedPresence"/>s, and what they
/// hold is absent. Before play starts, an enemy stack not under "?" shows its top counter and counts the rest (A2.9), and a side setting up now is
/// out of sight entirely (A12.12; pass 23, rulings R23.1 and R23.3). The adjudicator's view is the whole state.
/// </summary>
public sealed record GameView(
    Perspective Perspective,
    StateStamp Stamp,
    bool Synthetic,
    IReadOnlyList<SideState> Sides,
    MapInPlay Map,
    int Turn,
    string Phase,
    string PhasingSide,
    IReadOnlyList<UnitInstance> Units,
    IReadOnlyList<SealedPresence> Sealed,
    IReadOnlyList<EquipmentInstance> Equipment,
    IReadOnlyList<EntityInstance> Entities,
    IReadOnlyList<GameEvent> Events,
    IReadOnlyDictionary<string, MapPosition> Locations)
{
    /// <summary>
    /// The view of the state at a revision of a history, with the events the perspective is entitled to. <paramref name="outOfSight"/> names the OB
    /// groups setting up now, whose units a side's view leaves out (A12.12: the player setting up does so "out of vision of his opponent"; ruling R23.3).
    /// </summary>
    public static GameView Of(GameHistory history, long revision, Perspective perspective, IReadOnlySet<string>? outOfSight = null)
    {
        ArgumentNullException.ThrowIfNull(history);
        var state = history.At(revision) ?? throw new ArgumentOutOfRangeException(nameof(revision), revision, "The history has no state at that revision.");
        return Of(state, perspective, history.EventsFor(perspective, revision), outOfSight);
    }

    public static GameView Of(GameState state, Perspective perspective, IReadOnlyList<GameEvent> events, IReadOnlySet<string>? outOfSight = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(perspective);
        ArgumentNullException.ThrowIfNull(events);
        if (!state.Perspectives.Contains(perspective))
        {
            throw new ArgumentException($"'{perspective}' is not a perspective of {state.Scope}; use {string.Join(", ", state.Perspectives)}.", nameof(perspective));
        }

        if (events.Any(item => !item.IsVisibleTo(perspective)))
        {
            throw new ArgumentException($"The events include some '{perspective}' is not entitled to.", nameof(events));
        }

        // A wreck stays on the map for every perspective (D10.1; ruling R6.5).
        var active = state.Objects.Where(item => item.Status is InstanceStatus.Active or InstanceStatus.Wrecked).ToArray();
        if (perspective.IsAdjudicator)
        {
            return new GameView(perspective, state.Stamp, state.Synthetic, state.Sides, state.Map, state.Turn, state.Phase, state.PhasingSide,
                [.. active.OfType<UnitInstance>()], [], [.. active.OfType<EquipmentInstance>()], [.. active.OfType<EntityInstance>()], events,
                LocationsOf(state, active));
        }

        bool Enemy(IGameObject item) => item.Side is not null && item.Side != perspective.Name;
        bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

        // A Dummy is a "?" with nothing beneath it (A12.11), so it is always concealed to the enemy, whatever its condition says (referee, pass 23).
        bool Concealed(IGameObject item) => Is(item, Conditions.Concealed) || item is UnitInstance { Kind: UnitKinds.Dummy };

        // Ruling R23.3 (A2.9): before play starts, an enemy stack not under "?" shows only its top counter, so what its other counters hold is withheld too.
        var beforePlay = !state.SetupClosed;
        var tops = new HashSet<string>(StringComparer.Ordinal);
        var stacked = new HashSet<(string Side, BoardLocation Location)>();
        if (beforePlay)
        {
            foreach (var stack in active.OfType<UnitInstance>().Where(unit => Enemy(unit) && !Is(unit, Conditions.Hidden)
                && state.Location(unit.Id) is not null).GroupBy(unit => (unit.Side, state.Location(unit.Id)!.Location)))
            {
                stacked.Add(stack.Key);
                if (stack.FirstOrDefault(unit => !Concealed(unit)) is { } top)
                {
                    tops.Add(top.Id);
                }
            }
        }

        // Equipment on the map (a Gun) lies beneath the enemy's units in its Location, or is its own top when none is there; a fortification holds its
        // units beneath it, so it is a top counter.
        bool Beneath(IGameObject item) => item.Position is MapPosition && state.Location(item.Id) is { } at && stacked.Contains((item.Side!, at.Location));

        // An instance is withheld if it, or anything it is inside or held by, is an enemy's hidden or concealed instance, belongs to a side setting up
        // out of sight (ruling R23.3), or lies beneath the top of an enemy stack before play (A2.9).
        bool Withheld(IGameObject item, bool sealAllowed)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (IGameObject? current = item; current is not null && seen.Add(current.Id);)
            {
                if (Enemy(current) && (Is(current, Conditions.Hidden) || (Concealed(current) && !(sealAllowed && current == item))
                    || (current is UnitInstance { Group: { } group } && outOfSight?.Contains(group) == true)
                    || (beforePlay && current is UnitInstance && !tops.Contains(current.Id) && !(sealAllowed && current == item))
                    || (beforePlay && current is EquipmentInstance && Beneath(current))))
                {
                    return true;
                }

                current = current switch
                {
                    EquipmentInstance { Holding: { } holding } => state.Find(holding.Holder),
                    { Position: ContainedPosition contained } => state.Find(contained.Container),
                    _ => null,
                };
            }

            return false;
        }

        var units = new List<UnitInstance>();
        var sealedUnits = new List<(string Side, BoardLocation Location, bool Uninspected)>();
        foreach (var unit in active.OfType<UnitInstance>().Where(unit => !Withheld(unit, sealAllowed: true)))
        {
            var concealed = Enemy(unit) && Concealed(unit);
            if (concealed || (beforePlay && Enemy(unit) && !tops.Contains(unit.Id)))
            {
                if (state.Location(unit.Id) is { } location)
                {
                    sealedUnits.Add((unit.Side, location.Location, !concealed));
                }

                continue;
            }

            units.Add(unit);
        }

        var sealedPresences = sealedUnits
            .OrderBy(item => item.Location.ToString(), StringComparer.Ordinal).ThenBy(item => item.Side, StringComparer.Ordinal).ThenBy(item => item.Uninspected)
            .Select((item, index) => new SealedPresence($"sealed-{index + 1}", item.Side, item.Location) { Uninspected = item.Uninspected })
            .ToArray();
        EquipmentInstance[] equipment = [.. active.OfType<EquipmentInstance>().Where(item => !Withheld(item, sealAllowed: false))];
        EntityInstance[] entities = [.. active.OfType<EntityInstance>().Where(item => !Withheld(item, sealAllowed: false))];

        // Referee, pass 23: a setup event names what it creates, so a side reads only those of its own instances, of the instances it may see now, and of
        // instances no longer in play; the creation of a counter it may not see stays out of its list.
        var shown = units.Select(unit => unit.Id).Concat(equipment.Select(item => item.Id)).Concat(entities.Select(item => item.Id)).ToHashSet(StringComparer.Ordinal);
        var activeIds = active.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        GameEvent[] entitled = [.. events.Where(item => item.Payload is not InstanceCreated { Instance: var created } || created.Side == perspective.Name
            || shown.Contains(created.Id) || !activeIds.Contains(created.Id))];
        return new GameView(perspective, state.Stamp, state.Synthetic, state.Sides, state.Map, state.Turn, state.Phase, state.PhasingSide, units,
            sealedPresences, equipment, entities, entitled, LocationsOf(state, [.. units, .. equipment, .. entities]));
    }

    /// <summary>Where each visible instance is on the map; instances off map or not entered have no entry.</summary>
    private static Dictionary<string, MapPosition> LocationsOf(GameState state, IEnumerable<IGameObject> visible) =>
        visible.Select(item => (item.Id, Location: state.Location(item.Id))).Where(item => item.Location is not null)
            .ToDictionary(item => item.Id, item => item.Location!, StringComparer.Ordinal);
}
