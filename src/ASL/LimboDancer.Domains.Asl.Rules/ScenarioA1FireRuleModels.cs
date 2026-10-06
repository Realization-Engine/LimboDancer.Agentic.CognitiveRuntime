namespace LimboDancer.Domains.Asl.Rules;

// The fact records of the Fire rules that moved from Play and Units in pass 32.c (slice S4). None is recorded in a game: the recorded records of
// an attack stay in ScenarioA1FireModels.cs and keep their shape (the pass 32 design, D5).

/// <summary>A unit against the bars on firing (A15.432, A7.25, A11.15, A20.5, A20.52): its kind, side, conditions, whether it is a vehicle, the phase and phasing side, and the kinds of the prisoners in its custody.</summary>
public sealed record FireBarFacts(
    string Kind,
    string? Side,
    bool Berserk,
    bool BoundingFire,
    bool Melee,
    bool Captured,
    bool Unarmed,
    bool Vehicle,
    string? Phase,
    string? PhasingSide,
    IReadOnlyList<string> PrisonerKinds);

/// <summary>A weapon a unit possesses, as the read of its usable MGs finds it: its id, whether it has malfunctioned or is dismantled (true only when its condition is True), its kind, and its LATW type, read from the catalog only when asked.</summary>
public sealed record PossessedWeaponFacts(string Id, bool Malfunctioned, bool Dismantled, string Kind, Func<string?> LatwType);

/// <summary>A phasing vehicle against Bounding First Fire (D3.3): the phase, whether it is an active vehicle of the phasing side, the moving stack (a vehicle's, its window open, the vehicle among its movers), and whether the vehicle's move has ended.</summary>
public sealed record BoundingFireFacts(string? Phase, bool ActiveVehicleOfPhasingSide, bool MovementExists, bool MovementVehicle, bool WindowOpen, bool AmongMovers, bool MovementEnded);

/// <summary>A vehicle's crew state (D5.3, D5.34, D5.2, C7.42): its BU condition as three values, its Stun, Recall, Shock, and Unconfirmed Kill, and whether it is closed-topped.</summary>
public sealed record CrewExposedFacts(RuleState ButtonedUp, bool Stunned, bool Recalled, bool Shocked, bool UnconfirmedKill, bool ClosedTopped);

/// <summary>A vehicle in a target Location as the state has it (A7.307, A7.308, D.8B): its id, definition, Location text, crew exposure, and conditions.</summary>
public sealed record VehicleTargetFacts(string Id, string? Definition, string At, bool CrewExposed, bool Stunned, bool Recalled, bool StunRecovery, bool Immobilized, bool Concealed, bool Hidden);

/// <summary>A firing vehicle as the state has it (D2.42, A7.82, D3.7, C2.24): its id, definition, Location text, crew exposure, Motion counter, whether it moves unstopped as the moving vehicle, its conditions, its fire counters, and whether its last shot this phase kept its Multiple ROF.</summary>
public sealed record VehicleFirerFacts(
    string Id,
    string? Definition,
    string At,
    bool CrewExposed,
    bool Motion,
    bool MovingUnstopped,
    bool Pinned,
    bool Stunned,
    bool Recalled,
    bool Shocked,
    bool UnconfirmedKill,
    bool StunRecovery,
    bool Malfunctioned,
    bool Fired,
    bool FirstFire,
    bool BoundingFire,
    bool RateOfFireKept);

/// <summary>A unit in a target Location as the state has it (A10.8, A15.21, A19.2, A22.4): its id, definition, Location text, conditions, Dummy, its group's ELR (null outside a group), its Green Inexperience, and the FT it possesses.</summary>
public sealed record TargetUnitFacts(
    string Id,
    string? Definition,
    string At,
    bool Broken,
    bool Pinned,
    bool Concealed,
    bool Hidden,
    bool Dummy,
    bool Wounded,
    bool Disrupted,
    int? Elr,
    bool Fanatic,
    bool Heroic,
    bool Berserk,
    bool? Inexperienced,
    int? Flamethrowers);

/// <summary>A unit against the spending of its fire for the phase (A7.1, A8.4, A9.2, C2.24, D3.5, A7.351, A22.3): the phase, its fire and First Fire counters, whether it is a vehicle; and three reads made only when asked, as the planner made them: whether its last shot kept its Multiple ROF, whether it has used a SW this phase, and whether it holds an unfired, working MG, FT, or ATR.</summary>
public sealed record FireSpentFacts(string? Phase, bool Fired, bool FirstFire, bool Vehicle, Func<bool> RateOfFireKept, Func<bool> SupportWeaponUsed, Func<bool> UnfiredWeaponHeld);
