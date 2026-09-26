namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A declared fire attack for the Fire package (unit step 17). Every fact is supplied by the caller; the package never
/// reads a game. Nullable members are required: a missing one leaves the attack undecided.
/// </summary>
public sealed record FireAttack(
    string? Phase,
    string? FiringSide,
    bool? FireGroupComplete,
    string? FirerLocationId,
    string? TargetLocationId,
    IReadOnlyList<FireFirer>? Firers,
    FireDirector? Director,
    int? Range,
    bool? SameLevel,
    FireLos? Los,
    int? ScenarioMonth,
    string? TargetTerrain,
    IReadOnlyList<FireTarget>? Targets,
    int? TargetSideElr,
    FireRolls? Rolls);

/// <summary>A unit of the fire group, with its reviewed catalog definition.</summary>
public sealed record FireFirer(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? FiredThisPlayerTurn,
    bool? UsesSupportWeapon);

/// <summary>The leader directing the fire group (A7.53, A7.531).</summary>
public sealed record FireDirector(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? DirectedThisPlayerTurn);

/// <summary>The LOS of unit step 16 from the firers' Location to the target Location.</summary>
public sealed record FireLos(bool? Blocked, int? HindranceDrm, bool? HindranceAttributed, bool? GrainInLos);

/// <summary>A unit in the target Location.</summary>
public sealed record FireTarget(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? Hidden,
    bool? Dummy);

/// <summary>
/// Recorded rolls. <see cref="Attack"/> is the IFT DR; <see cref="RandomSelection"/> holds one dr per target unit;
/// <see cref="Checks"/> holds the MC or NTC DR of a target unit; <see cref="LeaderLoss"/> holds its LLMC or LLTC DR.
/// </summary>
public sealed record FireRolls(
    IReadOnlyList<int>? Attack,
    IReadOnlyDictionary<string, int>? RandomSelection,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? Checks,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? LeaderLoss);

/// <summary>One modifier with its value and rule.</summary>
public sealed record FireModifier(string Name, decimal Value, string Rule);

/// <summary>One firer's FP arithmetic.</summary>
public sealed record FirerFirepower(string UnitId, int Printed, IReadOnlyList<FireModifier> Multipliers, decimal Firepower);

/// <summary>The attack's arithmetic, as A7.3 resolves it.</summary>
public sealed record FireArithmetic(
    IReadOnlyList<FirerFirepower> Firers,
    decimal TotalFirepower,
    int? UnshiftedColumnFp,
    int ColumnShift,
    bool Cowered,
    int? ColumnFp,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    string Result);

/// <summary>A MC, NTC, LLMC, or LLTC taken by a target unit.</summary>
public sealed record FireCheck(
    string Kind,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int MoraleLevel,
    bool Passed,
    string Consequence);

/// <summary>What the attack did to one target unit.</summary>
public sealed record FireUnitEffect(
    string UnitId,
    string DefinitionId,
    string FinalDefinitionId,
    int? RandomSelectionDr,
    bool Eliminated,
    bool Broken,
    bool Pinned,
    bool ConcealmentLost,
    IReadOnlyList<string> Events,
    IReadOnlyList<FireCheck> Checks);

/// <summary>The Fire package's answer: resolved with its arithmetic and effects, or Abstained or Indeterminate with reasons.</summary>
public sealed record FireResolution(
    string Disposition,
    IReadOnlyList<string> Reasons,
    FireArithmetic? Arithmetic,
    IReadOnlyList<FireUnitEffect> Effects,
    IReadOnlyList<string> FireCounterUnitIds,
    string? FireCounter,
    IReadOnlyList<string> FirerConcealmentLost)
{
    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}
