namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A declared Rally attempt for the Rally package (unit step 19). Every fact is supplied by the caller; the package never
/// reads a game. Nullable members are required: a missing one leaves the attempt undecided.
/// </summary>
public sealed record RallyAttempt(
    string? Phase,
    string? RallyingSide,
    RallyUnit? Unit,
    RallyLeader? Leader,
    string? LocationId,
    string? Terrain,
    bool? GoodOrderLeaderInLocation,
    bool? BrokenLeaderInLocation,
    bool? FirstMmcRallyOfOwnPlayerTurn,
    bool? EnemyGoodOrderInLosWithin16,
    RallyRolls? Rolls);

/// <summary>The broken unit attempting to rally, with its reviewed catalog definition.</summary>
public sealed record RallyUnit(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Disrupted,
    bool? Wounded,
    bool? DesperationMorale,
    bool? Concealed,
    bool? AttemptedThisPlayerTurn,
    bool? OtherActionThisPhase);

/// <summary>The Good Order leader attempting to rally the unit (A10.6); none for Self-Rally.</summary>
public sealed record RallyLeader(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Wounded,
    bool? Concealed);

/// <summary>Recorded rolls: the Rally DR, and a leader's Wound Severity dr after Fate (A10.64, A17.11).</summary>
public sealed record RallyRolls(IReadOnlyList<int>? Rally, int? WoundSeverity);

/// <summary>The attempt's arithmetic, as A10.6 to A10.64 resolve it.</summary>
public sealed record RallyArithmetic(
    string Kind,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int MoraleLevel,
    bool Rallied,
    bool Fate,
    bool HeatOfBattleNotTaken,
    bool LeaderCreationNotTaken);

/// <summary>What the attempt did to the unit, and which units lost "?" by it (A12.141).</summary>
public sealed record RallyEffect(
    string UnitId,
    string DefinitionId,
    string FinalDefinitionId,
    bool Rallied,
    bool Eliminated,
    bool Wounded,
    IReadOnlyList<string> ConcealmentLost,
    IReadOnlyList<string> Events);

/// <summary>The Rally package's answer: resolved with its arithmetic and effect, or Abstained or Indeterminate with reasons.</summary>
public sealed record RallyResolution(string Disposition, IReadOnlyList<string> Reasons, RallyArithmetic? Arithmetic, RallyEffect? Effect)
{
    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}
