using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.State;

/// <summary>Whether a stamp still describes the state (ASL-UNIT-041).</summary>
public enum StampStatus
{
    Current,

    /// <summary>A later event or a different map version: conclusions drawn at the stamp must be drawn again.</summary>
    Stale,

    /// <summary>The stamp belongs to another tenant or game.</summary>
    OtherGame,
}

/// <summary>
/// The state of a game at one revision (ASL-UNIT-020): a projection of its events, never edited directly (ASL-UNIT-040).
/// It holds everything; what a side may see comes from <see cref="GameView"/>.
/// </summary>
public sealed record GameState(
    GameScope Scope,
    long Revision,
    DateTimeOffset Time,
    bool Synthetic,
    IReadOnlyList<SideState> Sides,
    MapInPlay Map,
    CatalogIdentity Catalog,
    int Turn,
    string Phase,
    string PhasingSide,
    IReadOnlyList<UnitInstance> Units,
    IReadOnlyList<EquipmentInstance> Equipment,
    IReadOnlyList<EntityInstance> Entities)
{
    /// <summary>The SSRs in force, as <c>game-started</c> named them; empty means none.</summary>
    public IReadOnlyList<string> SpecialRules { get; init; } = [];

    /// <summary>The side that had the first Player Turn: a new Game Turn starts when it is phasing again.</summary>
    public string FirstSide { get; init; } = string.Empty;

    /// <summary>Entry attempts not yet resolved; while any is open the phase may not change.</summary>
    public IReadOnlyList<OpenAttempt> OpenAttempts { get; init; } = [];

    /// <summary>The source of the game's first event: <c>fixture</c>, or an accepted live source.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>The closed set of perspectives for this game: each side, then the adjudicator (ASL-UNIT-030).</summary>
    public IReadOnlyList<Perspective> Perspectives => [.. Sides.Select(side => Perspective.Side(side.Id)), Perspective.Adjudicator];

    public StateStamp Stamp => new(Scope, Revision, Map.Version);

    public IEnumerable<IGameObject> Objects => Units.Cast<IGameObject>().Concat(Equipment).Concat(Entities);

    public IGameObject? Find(string id) => Objects.FirstOrDefault(item => item.Id == id);

    public UnitInstance? Unit(string id) => Units.FirstOrDefault(unit => unit.Id == id);

    public SideState? Side(string id) => Sides.FirstOrDefault(side => side.Id == id);

    public static ConditionState Condition(IGameObject item, string name)
    {
        ArgumentNullException.ThrowIfNull(item);
        return item.Conditions.TryGetValue(name, out var state) ? state : ConditionState.Unknown;
    }

    /// <summary>
    /// Where an instance is on the map, following containment and holding: a passenger is where its vehicle is, a
    /// possessed SW where its holder is. Null when it, or what holds it, is off map or not entered.
    /// </summary>
    public MapPosition? Location(string id)
    {
        ArgumentNullException.ThrowIfNull(id);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var item = Find(id); item is not null && seen.Add(item.Id);)
        {
            if (item is EquipmentInstance { Holding: { Role: not HoldingRole.Manned } holding })
            {
                item = Find(holding.Holder);
                continue;
            }

            switch (item.Position)
            {
                case MapPosition map:
                    return map;
                case ContainedPosition contained:
                    item = Find(contained.Container);
                    continue;
                default:
                    return null;
            }
        }

        return null;
    }

    /// <summary>The active objects whose location is this board location: a stack (ASL-UNIT-025).</summary>
    public IReadOnlyList<IGameObject> At(BoardLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return [.. Objects.Where(item => item.Status == InstanceStatus.Active && Location(item.Id)?.Location == location)];
    }

    /// <summary>
    /// Good Order, derived and never stored (ASL-UNIT-023): a Personnel unit neither broken, berserk, captured, nor held
    /// in Melee (Index, Good Order, p. 23). Unknown while any of those is not known; inapplicable to other kinds, since
    /// vehicular crews' stun and shock are not yet modelled.
    /// </summary>
    public static ConditionState GoodOrder(UnitInstance unit, UnitVocabulary vocabulary)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(vocabulary);
        if (!vocabulary.IsA(unit.Kind, "asl:personnel"))
        {
            return ConditionState.Inapplicable;
        }

        var states = new[] { Conditions.Broken, Conditions.Berserk, Conditions.Captured, Conditions.Melee }.Select(name => Condition(unit, name)).ToArray();
        return states.Contains(ConditionState.True) ? ConditionState.False
            : states.All(state => state == ConditionState.False) ? ConditionState.True
            : ConditionState.Unknown;
    }

    /// <summary>Whether a conclusion stamped earlier still describes this state (ASL-UNIT-041).</summary>
    public StampStatus Check(StateStamp stamp)
    {
        ArgumentNullException.ThrowIfNull(stamp);
        return stamp.Scope != Scope ? StampStatus.OtherGame
            : stamp.Revision == Revision && stamp.MapVersion == Map.Version ? StampStatus.Current
            : StampStatus.Stale;
    }
}

/// <summary>
/// A game's events and its state after each one. <see cref="States"/>[n] is the state at revision n + 1. When the
/// events break a rule of the model, the history stops before the offending event and carries its diagnostics.
/// </summary>
public sealed record GameHistory(IReadOnlyList<GameEvent> Events, IReadOnlyList<GameState> States, IReadOnlyList<UnitDiagnostic> Diagnostics)
{
    public bool HasErrors => Diagnostics.Any(diagnostic => diagnostic.Severity == UnitDiagnosticSeverity.Error);

    public GameState? Current => HasErrors || States.Count == 0 ? null : States[^1];

    public GameState? At(long revision) =>
        HasErrors || revision < 1 || revision > States.Count ? null : States[(int)(revision - 1)];

    /// <summary>The events a perspective is entitled to, through a revision (ASL-UNIT-031).</summary>
    public IReadOnlyList<GameEvent> EventsFor(Perspective perspective, long throughRevision)
    {
        ArgumentNullException.ThrowIfNull(perspective);
        return [.. Events.Where(item => item.Revision <= throughRevision && item.IsVisibleTo(perspective))];
    }
}
