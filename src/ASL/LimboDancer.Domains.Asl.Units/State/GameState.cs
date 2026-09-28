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

    /// <summary>The scenario's year when setup recorded it (C8.1, C8.3: Special Ammunition by year; ruling R7.6).</summary>
    public int? ScenarioYear
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

    /// <summary>The Turret Covered Arcs that differ from their VCA (D3.12; ruling R7.10).</summary>
    public IReadOnlyList<TurretFacing> TurretFacings { get; init; } = [];

    /// <summary>The Special Ammunition each Gun or AFV has run out of (C8.9; ruling R7.6).</summary>
    public IReadOnlyList<DepletedAmmunition> DepletedAmmunition { get; init; } = [];

    /// <summary>The crews that fired their Gun this Player Turn, which costs them their inherent FP (A7.352; ruling R8.4).</summary>
    public IReadOnlyList<string> GunCrewsFired { get; init; } = [];

    /// <summary>
    /// Each Gun's Defensive First Fire shots at the moving stack in its present Location (C6.17; ruling R8.1); cleared whenever the stack
    /// enters a new Location and at every phase change.
    /// </summary>
    public IReadOnlyList<OrdnanceShotRecord> OrdnanceShotsHere { get; init; } = [];

    /// <summary>The Scenario Defender's side, when setup names it (C6.41).</summary>
    public string? ScenarioDefender
    {
        get; init;
    }

    /// <summary>The Bore Sighted Locations recorded at setup (C6.42; ruling R8.8).</summary>
    public IReadOnlyList<BoreSighted> BoreSights { get; init; } = [];

    /// <summary>The Guns that changed their CA without firing this phase, and fire no more in it (C3.22); cleared at every phase change.</summary>
    public IReadOnlyList<string> GunsTurnedThisPhase { get; init; } = [];

    /// <summary>The Guns and crews that may not move this Player Turn after a CA change in the PFPh (C3.22; ruling R8.9).</summary>
    public IReadOnlyList<string> NoMoveThisPlayerTurn { get; init; } = [];

    /// <summary>The Guns whose Emplacement is lost: they moved, were hooked up, or set up otherwise (C11.3; ruling R8.3).</summary>
    public IReadOnlyList<string> UnemplacedGuns { get; init; } = [];

    /// <summary>The id suffix of a SMOKE grenade counter, which leaves at the end of its MPh (A24.11; ruling R9.5).</summary>
    public const string SmokeGrenadeSuffix = "-smoke-grenade";

    /// <summary>The units that attempted to place SMOKE grenades this MPh (A24.1; ruling R9.5); cleared at every phase change.</summary>
    public IReadOnlyList<string> SmokeAttempts { get; init; } = [];

    /// <summary>Each light mortar's Spotter (C9.3; ruling R9.4): the unit that spots for it until it is broken, eliminated, or captured.</summary>
    public IReadOnlyList<MortarSpotter> MortarSpotters { get; init; } = [];

    /// <summary>The PF shots each side has taken this scenario (C13.31; ruling R9.7), by side id.</summary>
    public IReadOnlyDictionary<string, int> PanzerfaustShots { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>
    /// Each side's Personnel at the end of setup in half-squad equivalents, a squad two and a HS or crew one, for the PF usage limit (C13.31;
    /// ruling R9.7), by side id.
    /// </summary>
    public IReadOnlyDictionary<string, int> SetupHalfSquads { get; init; } = new Dictionary<string, int>(StringComparer.Ordinal);

    /// <summary>Whether setup is over: an event other than game-started, instance-created, or bore-sighted has been recorded.</summary>
    public bool SetupClosed
    {
        get; init;
    }

    /// <summary>
    /// The squads whose only fire this phase is one SW's use, with that SW (A7.351; rulings R9.2, R9.4, R9.7): a light mortar's shot, a Spotter's
    /// spotting, or a PF Check. Such a squad may still fire its inherent FP; a second SW costs it. Cleared at every phase change.
    /// </summary>
    public IReadOnlyList<SupportWeaponUse> SupportWeaponUses { get; init; } = [];

    /// <summary>
    /// The units that fired in a fire record this phase, with the number of SW each used (A7.351; table player, pass 9); cleared at every phase
    /// change.
    /// </summary>
    public IReadOnlyList<SupportWeaponUse> PhaseFirers { get; init; } = [];

    /// <summary>The Spotters that spotted this phase, each for one mortar (C9.31 EX; table player, pass 9); cleared at every phase change.</summary>
    public IReadOnlyList<MortarSpotter> SpottedThisPhase { get; init; } = [];

    /// <summary>The Location a SMOKE attempt just placed its counter in, until the counter is created (A24.1; table player, pass 9).</summary>
    public Maps.Coordinates.BoardLocation? SmokePending
    {
        get; init;
    }

    /// <summary>The light mortars carried to a new Location in this Player Turn's MPh, which do not fire in its AFPh (A4.41; referee, pass 9).</summary>
    public IReadOnlyList<string> MovedWeapons { get; init; } = [];

    /// <summary>
    /// The leader directing each SW's To Hit DR this phase (A7.53; referee, pass 9), who may go on directing its further ROF shots; cleared at every
    /// phase change.
    /// </summary>
    public IReadOnlyList<SupportWeaponDirector> SupportWeaponDirectors { get; init; } = [];

    /// <summary>
    /// The units that passed a PAATC against a vehicle this phase, as "unit|vehicle" (A11.6, D7.21; ruling R11.13): they need no other against it in
    /// the phase. Cleared at every phase change.
    /// </summary>
    public IReadOnlyList<string> PaatcPassed { get; init; } = [];

    /// <summary>The vehicles whose Shock or Unconfirmed Kill dr was made this RPh (C7.42; ruling R7.8); cleared at every phase change.</summary>
    public IReadOnlyList<string> ShockRollsThisPhase { get; init; } = [];

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
