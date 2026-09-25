using LimboDancer.Domains.Asl.Maps.Coordinates;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>
/// Something a side knows is at a location without knowing what (A12.11, p. 76): a concealed enemy unit, reduced to its
/// side and location. <see cref="PlacementId"/> is a key for the display, numbered by location, never the unit's id.
/// Every condition of the unit is withheld.
/// </summary>
public sealed record SealedPresence(string PlacementId, string Side, BoardLocation Location);

/// <summary>
/// A game state as one perspective may know it (ASL-UNIT-030, 031). A side's view leaves out what that side cannot
/// know rather than marking it for a consumer to hide: an enemy's hidden instances (A12.3, p. 80) and everything
/// inside or held by them are absent; an enemy's concealed units become <see cref="SealedPresence"/>s, and what they
/// hold is absent. The adjudicator's view is the whole state.
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
    /// <summary>The view of the state at a revision of a history, with the events the perspective is entitled to.</summary>
    public static GameView Of(GameHistory history, long revision, Perspective perspective)
    {
        ArgumentNullException.ThrowIfNull(history);
        var state = history.At(revision) ?? throw new ArgumentOutOfRangeException(nameof(revision), revision, "The history has no state at that revision.");
        return Of(state, perspective, history.EventsFor(perspective, revision));
    }

    public static GameView Of(GameState state, Perspective perspective, IReadOnlyList<GameEvent> events)
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

        var active = state.Objects.Where(item => item.Status == InstanceStatus.Active).ToArray();
        if (perspective.IsAdjudicator)
        {
            return new GameView(perspective, state.Stamp, state.Synthetic, state.Sides, state.Map, state.Turn, state.Phase, state.PhasingSide,
                [.. active.OfType<UnitInstance>()], [], [.. active.OfType<EquipmentInstance>()], [.. active.OfType<EntityInstance>()], events,
                LocationsOf(state, active));
        }

        bool Enemy(IGameObject item) => item.Side is not null && item.Side != perspective.Name;
        bool Is(IGameObject item, string condition) => GameState.Condition(item, condition) == ConditionState.True;

        // An instance is withheld if it, or anything it is inside or held by, is an enemy's hidden or concealed instance.
        bool Withheld(IGameObject item, bool sealAllowed)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (IGameObject? current = item; current is not null && seen.Add(current.Id);)
            {
                if (Enemy(current) && (Is(current, Conditions.Hidden) || (Is(current, Conditions.Concealed) && !(sealAllowed && current == item))))
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
        var sealedUnits = new List<(string Side, BoardLocation Location)>();
        foreach (var unit in active.OfType<UnitInstance>().Where(unit => !Withheld(unit, sealAllowed: true)))
        {
            if (Enemy(unit) && Is(unit, Conditions.Concealed))
            {
                if (state.Location(unit.Id) is { } location)
                {
                    sealedUnits.Add((unit.Side, location.Location));
                }

                continue;
            }

            units.Add(unit);
        }

        var sealedPresences = sealedUnits
            .OrderBy(item => item.Location.ToString(), StringComparer.Ordinal).ThenBy(item => item.Side, StringComparer.Ordinal)
            .Select((item, index) => new SealedPresence($"sealed-{index + 1}", item.Side, item.Location))
            .ToArray();
        EquipmentInstance[] equipment = [.. active.OfType<EquipmentInstance>().Where(item => !Withheld(item, sealAllowed: false))];
        EntityInstance[] entities = [.. active.OfType<EntityInstance>().Where(item => !Withheld(item, sealAllowed: false))];
        return new GameView(perspective, state.Stamp, state.Synthetic, state.Sides, state.Map, state.Turn, state.Phase, state.PhasingSide, units,
            sealedPresences, equipment, entities, events, LocationsOf(state, [.. units, .. equipment, .. entities]));
    }

    /// <summary>Where each visible instance is on the map; instances off map or not entered have no entry.</summary>
    private static Dictionary<string, MapPosition> LocationsOf(GameState state, IEnumerable<IGameObject> visible) =>
        visible.Select(item => (item.Id, Location: state.Location(item.Id))).Where(item => item.Location is not null)
            .ToDictionary(item => item.Id, item => item.Location!, StringComparer.Ordinal);
}
