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

    /// <summary>The scenario's month when setup recorded it (B15: grain is a Hindrance June to September).</summary>
    public int? ScenarioMonth
    {
        get; init;
    }

    /// <summary>
    /// The vehicles that entered a new hex, or moved under a Motion counter, in the current Player Turn's MPh (C6.1 Case J, D9.3, D9.4;
    /// ruling R6.1): until the AFPh ends they, and their wrecks, give Infantry no TEM and form no Hindrance. Cleared at each new Player Turn.
    /// </summary>
    public IReadOnlyList<string> MovedVehicles { get; init; } = [];

    /// <summary>The fire attacks made in the current phase (A7.55); cleared at every phase change.</summary>
    public IReadOnlyList<FireRecord> FiresThisPhase { get; init; } = [];

    /// <summary>The units that attempted to rally this Player Turn (A10.6, p. 68): each may try once.</summary>
    public IReadOnlyList<string> RallyAttemptsThisPlayerTurn { get; init; } = [];

    /// <summary>The sides whose first MMC Rally attempt of their own Player Turn is spent (A18.11, p. 85).</summary>
    public IReadOnlyList<string> FirstMmcRallyTaken { get; init; } = [];

    /// <summary>The units that attempted a Repair this RPh, which may not also rally (A3.1, p. 47).</summary>
    public IReadOnlyList<string> RepairsThisPhase { get; init; } = [];

    /// <summary>The Residual FP in each Location this MPh (A8.2, p. 60); cleared at the end of the MPh.</summary>
    public IReadOnlyList<ResidualFire> ResidualFire { get; init; } = [];

    /// <summary>The CC Locations of the CCPh (A11.12; unit step 29); cleared at every phase change.</summary>
    public IReadOnlyList<CloseCombatLocation> CloseCombats { get; init; } = [];

    /// <summary>The units that advanced this Player Turn and where (A4.7), which an Ambush reads (A11.4).</summary>
    public IReadOnlyList<AdvanceRecord> Advances { get; init; } = [];

    /// <summary>Surrenders awaiting the captor's choice (A15.5); while one waits the phase may not change.</summary>
    public IReadOnlyList<PendingSurrender> PendingSurrenders { get; init; } = [];

    /// <summary>Each Gun's shots this fire phase and its Multiple ROF (C2.24; unit step 24); cleared at every phase change.</summary>
    public IReadOnlyList<OrdnanceShotRecord> OrdnanceShots { get; init; } = [];

    /// <summary>The Location each Gun has acquired and its Acquisition DRM (C6.5).</summary>
    public IReadOnlyList<GunAcquisition> Acquisitions { get; init; } = [];

    /// <summary>The choice the game waits for (ruling R5.8); while one waits nothing else may happen.</summary>
    public PendingChoice? Choice
    {
        get; init;
    }

    /// <summary>
    /// The answers given to the pending choices of a resolution not yet recorded (ruling R5.8), by key; the resolution's record must
    /// declare exactly these, and clears them.
    /// </summary>
    public IReadOnlyDictionary<string, string> ChoicesMade { get; init; } = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>The sides faced with No Quarter (A20.3, A20.4; rulings R5.6 and R5.7): their units never surrender.</summary>
    public IReadOnlyList<string> NoQuarter { get; init; } = [];

    /// <summary>The sides whose ELR a Massacre has raised (A20.4): it is raised once only.</summary>
    public IReadOnlyList<string> MassacreElrRaised { get; init; } = [];

    /// <summary>The units whose CX counter was removed at the start of this MPh, which may not Double Time in it (A4.51, ruling R5.3).</summary>
    public IReadOnlyList<string> NoDoubleTime { get; init; } = [];

    /// <summary>The stack moving now in the MPh, if any (unit step 22).</summary>
    public MovementState? Movement
    {
        get; init;
    }

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
