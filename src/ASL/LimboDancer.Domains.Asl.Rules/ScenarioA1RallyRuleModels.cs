namespace LimboDancer.Domains.Asl.Rules;

/// <summary>The unit of a Rally attempt as the state finds it (unit step 19; pass 32.f), before Rules decides what the attempt declares.</summary>
public sealed record RallyUnitStateFacts(
    string Id,
    string DefinitionId,
    string Location,
    bool Broken,
    bool Disrupted,
    bool Wounded,
    bool DesperationMorale,
    bool Concealed,
    bool AttemptedThisPlayerTurn,
    bool RepairedThisPhase,
    bool TookRallyPhaseAction,
    bool Fanatic,
    bool? Inexperienced);

/// <summary>The rallying leader as the state finds it: its Location may be none.</summary>
public sealed record RallyLeaderStateFacts(string Id, string DefinitionId, string? Location, bool Broken, bool Wounded, bool Concealed);

/// <summary>
/// The state's part of a Rally attempt (A10.6, A10.63, A10.71, A15.41, A19.3, E3.742; rulings R15.6, R15.10, R16.14): the phase and the phasing side,
/// the unit and its leader, whether each other friendly leader in the Location is broken, the side's first MMC Rally, the planner's map and LOS
/// reads, the companions, No Quarter, the Commissar, and Extreme Winter.
/// </summary>
public sealed record RallyAttemptStateFacts(
    string? Phase,
    bool Phasing,
    RallyUnitStateFacts Unit,
    RallyLeaderStateFacts? Leader,
    string? Terrain,
    IReadOnlyList<bool> OtherLeadersBroken,
    bool FirstMmcRallyTaken,
    bool? EnemyGoodOrderInLosWithin16,
    bool? KnownEnemyInLos,
    IReadOnlyList<string>? Captors,
    IReadOnlyList<RallyCompanion> Companions,
    bool NoQuarter,
    string? CommissarId,
    bool ExtremeWinter);

/// <summary>What a Repair dr does (A9.72, D3.7): repairs the weapon, changes nothing, or eliminates or disables it.</summary>
public enum RepairOutcome
{
    Repaired,
    NoChange,
    Eliminated,
}

/// <summary>Which Self-Rally rule bars a unit without the capability (A10.63, A18.11, A10.71): none, not its own side's RPh, the side's one MMC Self-Rally used, or a broken leader in its Location.</summary>
public enum SelfRallyBar
{
    None,
    NotOwnPhase,
    Used,
    BrokenLeader,
}

/// <summary>
/// What the Rally package asks for next (ruling R5.8): the attempt is resolved; or an option its owner answers (the choice's key); or a roll (its key,
/// the dice count, the roll's purpose); or the package left the attempt undecided (its reasons joined).
/// </summary>
public sealed record RallyNextStep(bool Resolved, string? ChoiceKey, string? RollKey, int Count, string? Purpose, string? Undecided);
