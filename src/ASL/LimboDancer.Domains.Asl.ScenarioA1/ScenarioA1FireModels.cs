using System.Text.Json.Serialization;

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
    FireRolls? Rolls)
{
    /// <summary>
    /// The kind of Defensive fire in the MPh (unit step 22): <c>first-fire</c> (A8.1), <c>subsequent-first-fire</c> (A8.3),
    /// <c>final-protective-fire</c> (A8.31), or <c>residual-fp</c> (A8.2); null for the fire phases.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? FireKind { get; init; }

    /// <summary>How the target stack moved, for FFNAM and FFMO (A4.6, A4.61); required in the MPh.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireMovement? TargetMovement { get; init; }

    /// <summary>The Residual FP counter attacking a moving unit (A8.2); required for <c>residual-fp</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp { get; init; }

    /// <summary>The firing side's ELR, for the NMC FPF inflicts on its firers (A8.31, A19.13).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FiringSideElr { get; init; }

    /// <summary>Whether no target is farther than the closest armed Known enemy unit (A8.3); required for Subsequent First Fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WithinSubsequentFirstFireRange { get; init; }

    /// <summary>The leaders directing the group from its other Locations (A7.531); null when the group is in one Location.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireDirector>? OtherDirectors { get; init; }

    /// <summary>Whether every Location of a group spanning Locations is ADJACENT to another of them (A7.5, A.8).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirerLocationsAdjacent { get; init; }

    /// <summary>
    /// The target side's units in the target Location that the attack does not attack, such as those not moving with the stack
    /// in the MPh: a leader who goes berserk takes them with him on a passed NTC (A15.41). Null when there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? Companions { get; init; }
}

/// <summary>How a moving target stack moved in the MPh: Assault Movement avoids FFNAM but not FFMO (A4.61).</summary>
public sealed record FireMovement(bool? AssaultMovement);

/// <summary>A MG a firer uses in the attack (A7.35, A9.1), with its reviewed catalog definition.</summary>
public sealed record FireWeapon(string? EquipmentId, string? DefinitionId, bool? Malfunctioned, bool? FiredThisPlayerTurn, bool? FirstFireMarked);

/// <summary>A unit of the fire group, with its reviewed catalog definition.</summary>
public sealed record FireFirer(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? FiredThisPlayerTurn,
    bool? UsesSupportWeapon)
{
    /// <summary>The range from this firer's Location, when the group spans Locations (A7.52); otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Range { get; init; }

    /// <summary>Whether this firer is at the target's level, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SameLevel { get; init; }

    /// <summary>The LOS from this firer's Location, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireLos? Los { get; init; }

    /// <summary>Marked with a First Fire counter this MPh (A8.1); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirstFireMarked { get; init; }

    /// <summary>Marked with a Final Fire counter (A8.3, A8.31); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FinalFireMarked { get; init; }

    /// <summary>The MGs the firer uses (A7.35); null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeapon>? Weapons { get; init; }

    /// <summary>Whether the firer adds its inherent FP (A7.351); null is true. False for a MG firing again on its Multiple ROF.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UsesInherentFp { get; init; }

    /// <summary>Whether the firer is Fanatic (A10.8): no Cowering, and a higher Morale Level in an FPF NMC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>Whether a hero firer is wounded (A15.2: 1-3-8); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wounded { get; init; }

    /// <summary>Whether a Green or Conscript firer is Inexperienced (A19.2), for the Heat of Battle DRM of an FPF NMC; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced { get; init; }

    /// <summary>Whether a Known enemy unit is in the firer's LOS (A15.44), the planner's read; needed when an FPF NMC can reach Heat of Battle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos { get; init; }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the firer may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors { get; init; }
}

/// <summary>The leader directing the fire group (A7.53, A7.531).</summary>
public sealed record FireDirector(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Pinned,
    bool? Concealed,
    bool? DirectedThisPlayerTurn,
    bool? Wounded);

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
    bool? Dummy,
    bool? Wounded,
    bool? Disrupted)
{
    /// <summary>Whether the unit is Fanatic (A10.8): both Morale Levels one higher, never Disrupted; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>Whether a leader is heroic (A15.21): he is wounded, not broken, by a failed MC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic { get; init; }

    /// <summary>Whether a Green or Conscript MMC is Inexperienced (A19.2), for the Heat of Battle DRM; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced { get; init; }

    /// <summary>
    /// Whether the unit is berserk (A15.42): Morale Level 10, Casualty Reduction instead of breaking, never pinned, no leader
    /// loss checks, and no leadership from a friendly leader; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk { get; init; }

    /// <summary>Whether a Known enemy unit is in the unit's LOS (A15.44), the planner's read; needed before the attack when Heat of Battle can reach it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos { get; init; }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the unit may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors { get; init; }
}

/// <summary>
/// Recorded rolls. <see cref="Attack"/> is the IFT DR; <see cref="RandomSelection"/> holds one dr per target unit;
/// <see cref="Checks"/> holds the MC or NTC DR of a target unit; <see cref="LeaderLoss"/> holds its LLMC or LLTC DR;
/// <see cref="WoundSeverity"/> holds a wounded leader's Wound Severity dr (A17.11).
/// </summary>
public sealed record FireRolls(
    IReadOnlyList<int>? Attack,
    IReadOnlyDictionary<string, int>? RandomSelection,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? Checks,
    IReadOnlyDictionary<string, IReadOnlyList<int>>? LeaderLoss,
    IReadOnlyDictionary<string, int>? WoundSeverity = null)
{
    /// <summary>One dr per MG whose B# the IFT DR reached, when two or more did (A9.71).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WeaponSelection { get; init; }

    /// <summary>One dr per FPF firer, when a Casualty MC falls on two or more of them (A8.31).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? FirerSelection { get; init; }

    /// <summary>The Heat of Battle DR of each unit whose Original MC DR was 2 (A15.1).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? HeatOfBattle { get; init; }

    /// <summary>The NTC DR of each friendly unit a berserk leader tries to take berserk with him (A15.41).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? BerserkChecks { get; init; }
}

/// <summary>One modifier with its value and rule.</summary>
public sealed record FireModifier(string Name, decimal Value, string Rule);

/// <summary>One firer's FP arithmetic.</summary>
public sealed record FirerFirepower(string UnitId, int Printed, IReadOnlyList<FireModifier> Multipliers, decimal Firepower)
{
    /// <summary>The unit operating this MG (A7.35); null for a unit's inherent FP.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operator { get; init; }

    /// <summary>Whether this FP attacks the concealed targets of a mixed Location (A12.13); null for all targets.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? VsConcealed { get; init; }
}

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
    string Result)
{
    /// <summary>
    /// The column and result for the concealed targets of a Location that also holds known ones: the same DR on the
    /// column their halved FP gives (A12.13). Null when every target is of one kind.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireColumn? Concealed { get; init; }

    /// <summary>The Residual FP the attack leaves in the target Location (A8.2, A8.26, A7.372); null when it leaves none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp { get; init; }
}

/// <summary>A second column of one attack: its total FP, the column before and after Cowering, and the IFT result.</summary>
public sealed record FireColumn(decimal TotalFirepower, int? UnshiftedColumnFp, int? ColumnFp, string Result);

/// <summary>What the attack did to a MG: malfunction (A9.7, A9.71), Multiple ROF kept (A9.2), Sustained Fire (A9.3), and its fire counter.</summary>
public sealed record FireWeaponEffect(string EquipmentId, int BreakdownNumber, bool Malfunctioned, bool RateOfFireRetained, bool SustainedFire,
    string? FireCounter, int? SelectionDr);

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
    bool Wounded,
    bool Disrupted,
    bool ConcealmentLost,
    IReadOnlyList<string> Events,
    IReadOnlyList<FireCheck> Checks)
{
    /// <summary>The unit's Heat of Battle DR and results (A15.1); null when it rolled none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeatOfBattleOutcome? HeatOfBattle { get; init; }

    /// <summary>Whether the unit is Fanatic after the attack (A10.8); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>Whether a leader is heroic after the attack (A15.21); null when he is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic { get; init; }

    /// <summary>Whether the unit is berserk after the attack (A15.4, A15.41); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk { get; init; }
}

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
    /// <summary>What the attack did to each MG used in it; null when none was.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects { get; init; }

    /// <summary>The NMC FPF inflicts on its firers and directing leader (A8.31); null for other fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? FirerEffects { get; init; }

    /// <summary>The companions a berserk leader took berserk with him (A15.41); null when he took none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? CompanionEffects { get; init; }

    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}
