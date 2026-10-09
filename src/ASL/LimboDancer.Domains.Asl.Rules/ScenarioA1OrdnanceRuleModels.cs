namespace LimboDancer.Domains.Asl.Rules;

// The fact records of the Ordnance, Gun, and SW rules that moved from Play and Units in pass 32.d (slice S7). None is recorded in a game: the
// recorded records of a shot stay in ScenarioA1OrdnanceModels.cs and its neighbours and keep their shape (the pass 32 design, D5).

/// <summary>A vehicle target as the state has it (C6.1 Case J, C.8, D5.5, D5.6): its id, definition, and the conditions that are True.</summary>
public sealed record OrdnanceVehicleTargetStateFacts(
    string Id,
    string? Definition,
    bool Motion,
    bool MovedThisPlayerTurn,
    bool Concealed,
    bool Hidden,
    bool Stunned,
    bool Shocked,
    bool UnconfirmedKill,
    bool Recalled,
    bool Abandoned,
    bool Immobilized,
    bool StunRecovery,
    bool Bypass = false,
    bool MovingUnstopped = false,
    bool Bogged = false);

/// <summary>
/// A squad against the use of a SW (A7.351, C13.31): whether it is a squad, its id, its First Fire mark and whether it has fired (a Prep or Final
/// Fire counter), whether it fired this phase and with which weapon, and whether it used a SW this phase.
/// </summary>
public sealed record SquadSupportFacts(string Id, bool Squad, bool FirstFire, bool Fired, bool FiredHere, string? FiredHereWeapon, bool UsedHere);

/// <summary>
/// A named Spotter of a light mortar (C9.3, C9.31, A7.352) as the state has it: whether it is active with a definition, its definition, whether it
/// is of the firer's side, Captured, in Melee, Broken, Pinned, a squad, has fired or is marked First Fire, has spotted this phase for this or
/// another mortar, and its facts against using a SW.
/// </summary>
public sealed record OrdnanceSpotterFacts(
    string Id,
    bool ActiveWithDefinition,
    string? Definition,
    bool SameSide,
    bool Captured,
    bool Melee,
    bool Broken,
    bool Pinned,
    bool Squad,
    bool FiredOrFirstFire,
    bool SpottedOtherMortar,
    bool SpottedThisPhase,
    SquadSupportFacts Support);

/// <summary>
/// A named leader directing a SW (A7.531, A7.53) as the state has it: whether it is active with a definition, its definition, whether it is of the
/// firer's side, its Location's text and whether that is the firer's, its conditions, whether it has fired or is marked First Fire, and whether it
/// directs this SW already this phase.
/// </summary>
public sealed record OrdnanceDirectorFacts(
    string Id,
    bool ActiveWithDefinition,
    string? Definition,
    bool SameSide,
    string? At,
    bool AtFirer,
    bool Broken,
    bool Pinned,
    bool Concealed,
    bool FiredOrFirstFire,
    bool Continuing,
    bool Wounded);
