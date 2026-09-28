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
    public string? FireKind
    {
        get; init;
    }

    /// <summary>How the target stack moved, for FFNAM and FFMO (A4.6, A4.61); required in the MPh.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireMovement? TargetMovement
    {
        get; init;
    }

    /// <summary>The Residual FP counter attacking a moving unit (A8.2); required for <c>residual-fp</c>.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }

    /// <summary>The firing side's ELR, for the NMC FPF inflicts on its firers (A8.31, A19.13).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? FiringSideElr
    {
        get; init;
    }

    /// <summary>Whether no target is farther than the closest armed Known enemy unit (A8.3); required for Subsequent First Fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? WithinSubsequentFirstFireRange
    {
        get; init;
    }

    /// <summary>The leaders directing the group from its other Locations (A7.531); null when the group is in one Location.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireDirector>? OtherDirectors
    {
        get; init;
    }

    /// <summary>Whether every Location of a group spanning Locations is ADJACENT to another of them (A7.5, A.8).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirerLocationsAdjacent
    {
        get; init;
    }

    /// <summary>
    /// The target side's units in the target Location that the attack does not attack, such as those not moving with the stack
    /// in the MPh: a leader who goes berserk takes them with him on a passed NTC (A15.41). Null when there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireTarget>? Companions
    {
        get; init;
    }

    /// <summary>
    /// An ordnance hit on the Infantry Target Type resolved on the IFT (C3.3, C3.32, unit step 24): no firers, the Gun's HE FP
    /// column (C.6), and no TEM or Hindrance on the Effects DR (C.3) unless a Critical Hit reverses the TEM (C3.71). Null for
    /// Infantry fire.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireOrdnanceHit? OrdnanceHit
    {
        get; init;
    }

    /// <summary>
    /// The vehicles in the target Location (unit step 25): an unarmored one is attacked on the Vehicle line with the attack's IFT DR
    /// (A7.308), and an armored one is unharmed but its Vulnerable CE crew takes a General Collateral Attack (A7.307, D.8B). Null when
    /// there are none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireVehicle>? Vehicles
    {
        get; init;
    }

    /// <summary>
    /// A vehicle's MA MG firing on the IFT (D1.83, D3.5, unit step 25): no Infantry firers or leader, the MG's FP with its own
    /// modifications. Null for other fire.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireVehicleFire? VehicleFire
    {
        get; init;
    }

    /// <summary>Whether the target side is faced with No Quarter (A20.3): its units treat a Heat of Battle Surrender as Berserk; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? TargetSideNoQuarter
    {
        get; init;
    }

    /// <summary>
    /// The AFV or wreck whose +1 TEM the target Location's Infantry may claim (D9.3, D10.3; ruling R6.1): the game decides that it is a
    /// non-burning wreck, a friendly AFV, or an abandoned enemy AFV, not in Motion and not under the Case J clause; the package adds it
    /// when the terrain gives no positive TEM and the attack does not come from within the Location. Null when there is none.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AfvCover
    {
        get; init;
    }

    /// <summary>
    /// The manned Gun whose crew is the attack's target (C11; backlog pass 8, ruling R8.3): whether it is Emplaced, and whether its gunshield
    /// faces the attack. Null when the target Location holds no Gun.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireGunTarget? GunTarget
    {
        get; init;
    }

    /// <summary>Whether the firing side is faced with No Quarter, for the Heat of Battle of an FPF NMC (A8.31, A20.3); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FiringSideNoQuarter
    {
        get; init;
    }

    /// <summary>
    /// The owners' answers to the attack's options (ruling R5.8), <c>take</c> or <c>decline</c> by key: <c>battleHardening:&lt;unit&gt;</c>
    /// (A15.3; <c>&lt;unit&gt;:2</c> for a second Heat of Battle DR) and <c>unlikelyKill:&lt;vehicle&gt;</c> (A7.309). Null means every option is
    /// taken as before pass 5; otherwise an option the attack reaches with no answer leaves it undecided.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, string>? Choices
    {
        get; init;
    }
}

/// <summary>A vehicle in the target Location, with its reviewed catalog definition and its crew's state (D5.2, D5.3, D5.34).</summary>
public sealed record FireVehicle(
    string? VehicleId,
    string? DefinitionId,
    string? LocationId,
    bool? CrewExposed,
    bool? Stunned,
    bool? StunRecovery,
    bool? Immobilized)
{
    /// <summary>Whether the vehicle is concealed (A12.2; ruling R6.7): it is attacked on the column of the halved FP (A12.13); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Concealed
    {
        get; init;
    }
}

/// <summary>
/// A vehicle's MA MG attack (D1.83, D3.5): the vehicle, its definition, and the facts that modify its FP and DR: Motion (D2.42), a
/// pinned crew (A7.82), a Stun +1 (D5.34), whether its crew is CE (D5.3), and whether it fires again on its Multiple ROF (C2.24, A9.2).
/// </summary>
public sealed record FireVehicleFire(
    string? VehicleId,
    string? DefinitionId,
    string? LocationId,
    bool? CrewExposed,
    bool? InMotion,
    bool? Pinned,
    bool? Stunned,
    bool? StunRecovery,
    bool? Malfunctioned,
    bool? FiredThisPlayerTurn,
    bool? RateOfFireShot);

/// <summary>An ordnance hit's IFT attack (C.6, C3.71): the Gun, its HE FP column, and whether the hit is Critical.</summary>
public sealed record FireOrdnanceHit(string? GunId, int? Firepower, bool? CriticalHit);

/// <summary>How a moving target stack moved in the MPh: Assault Movement avoids FFNAM but not FFMO (A4.61).</summary>
public sealed record FireMovement(bool? AssaultMovement);

/// <summary>A MG a firer uses in the attack (A7.35, A9.1), with its reviewed catalog definition.</summary>
public sealed record FireWeapon(string? EquipmentId, string? DefinitionId, bool? Malfunctioned, bool? FiredThisPlayerTurn, bool? FirstFireMarked);

/// <summary>A unit of the fire group, with its reviewed catalog definition.</summary>
/// <summary>
/// A manned Gun in the target Location (C11.2, C11.5; ruling R8.3): its crew, whether it is Emplaced (set up manned and never moved), and
/// whether its gunshield protects the crew from this attack (an AT or INF Gun, the attack from within its CA and not from its own hex).
/// </summary>
public sealed record FireGunTarget(string? GunId, string? DefinitionId, string? CrewUnitId, bool? Emplaced, bool? Gunshield);

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
    /// <summary>Whether this crew fired its Gun this Player Turn, which costs it its inherent FP (A7.352; ruling R8.4); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? GunFired
    {
        get; init;
    }

    /// <summary>The range from this firer's Location, when the group spans Locations (A7.52); otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Range
    {
        get; init;
    }

    /// <summary>Whether this firer is at the target's level, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? SameLevel
    {
        get; init;
    }

    /// <summary>The LOS from this firer's Location, when the group spans Locations; otherwise the attack's.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireLos? Los
    {
        get; init;
    }

    /// <summary>Marked with a First Fire counter this MPh (A8.1); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FirstFireMarked
    {
        get; init;
    }

    /// <summary>Marked with a Final Fire counter (A8.3, A8.31); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? FinalFireMarked
    {
        get; init;
    }

    /// <summary>The MGs the firer uses (A7.35); null is none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireWeapon>? Weapons
    {
        get; init;
    }

    /// <summary>Whether the firer adds its inherent FP (A7.351); null is true. False for a MG firing again on its Multiple ROF.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UsesInherentFp
    {
        get; init;
    }

    /// <summary>Whether the firer is Fanatic (A10.8): no Cowering, and a higher Morale Level in an FPF NMC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a hero firer is wounded (A15.2: 1-3-8); null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Wounded
    {
        get; init;
    }

    /// <summary>Whether a Green or Conscript firer is Inexperienced (A19.2), for the Heat of Battle DRM of an FPF NMC; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced
    {
        get; init;
    }

    /// <summary>Whether a Known enemy unit is in the firer's LOS (A15.44), the planner's read; needed when an FPF NMC can reach Heat of Battle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos
    {
        get; init;
    }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the firer may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors
    {
        get; init;
    }

    /// <summary>Whether the firer is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cx
    {
        get; init;
    }
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
    bool? Wounded)
{
    /// <summary>Whether the directing leader is CX (A4.51): +1 to the attack's IFT DR; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Cx
    {
        get; init;
    }
}

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
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a leader is heroic (A15.21): he is wounded, not broken, by a failed MC; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic
    {
        get; init;
    }

    /// <summary>Whether a Green or Conscript MMC is Inexperienced (A19.2), for the Heat of Battle DRM; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced
    {
        get; init;
    }

    /// <summary>
    /// Whether the unit is berserk (A15.42): Morale Level 10, Casualty Reduction instead of breaking, never pinned, no leader
    /// loss checks, and no leadership from a friendly leader; null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }

    /// <summary>Whether a Known enemy unit is in the unit's LOS (A15.44), the planner's read; needed before the attack when Heat of Battle can reach it.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos
    {
        get; init;
    }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the unit may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors
    {
        get; init;
    }
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
    public IReadOnlyDictionary<string, int>? WeaponSelection
    {
        get; init;
    }

    /// <summary>One dr per FPF firer, when a Casualty MC falls on two or more of them (A8.31).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? FirerSelection
    {
        get; init;
    }

    /// <summary>The Heat of Battle DR of each unit whose Original MC DR was 2 (A15.1).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? HeatOfBattle
    {
        get; init;
    }

    /// <summary>The NTC DR of each friendly unit a berserk leader tries to take berserk with him (A15.41).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? BerserkChecks
    {
        get; init;
    }

    /// <summary>The MC or NTC DR of each armored vehicle's Vulnerable crew in a General Collateral Attack (D.8B, D5.34).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? CrewChecks
    {
        get; init;
    }

    /// <summary>The Unlikely Kill dr of each unarmored vehicle whose Original IFT DR of 2 did not otherwise harm it (A7.309).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? UnlikelyKill
    {
        get; init;
    }
}

/// <summary>One modifier with its value and rule.</summary>
public sealed record FireModifier(string Name, decimal Value, string Rule);

/// <summary>One firer's FP arithmetic.</summary>
public sealed record FirerFirepower(string UnitId, int Printed, IReadOnlyList<FireModifier> Multipliers, decimal Firepower)
{
    /// <summary>The unit operating this MG (A7.35); null for a unit's inherent FP.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Operator
    {
        get; init;
    }

    /// <summary>Whether this FP attacks the concealed targets of a mixed Location (A12.13); null for all targets.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? VsConcealed
    {
        get; init;
    }
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
    public FireColumn? Concealed
    {
        get; init;
    }

    /// <summary>The Residual FP the attack leaves in the target Location (A8.2, A8.26, A7.372); null when it leaves none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ResidualFp
    {
        get; init;
    }
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
    public HeatOfBattleOutcome? HeatOfBattle
    {
        get; init;
    }

    /// <summary>The unit's second Heat of Battle DR in the attack, after a second Original MC DR of 2 (A15.1, ruling R5.10); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeatOfBattleOutcome? SecondHeatOfBattle
    {
        get; init;
    }

    /// <summary>Whether the unit is Fanatic after the attack (A10.8); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic
    {
        get; init;
    }

    /// <summary>Whether a leader is heroic after the attack (A15.21); null when he is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic
    {
        get; init;
    }

    /// <summary>Whether the unit is berserk after the attack (A15.4, A15.41); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk
    {
        get; init;
    }
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
    public IReadOnlyList<FireWeaponEffect>? WeaponEffects
    {
        get; init;
    }

    /// <summary>The NMC FPF inflicts on its firers and directing leader (A8.31); null for other fire.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? FirerEffects
    {
        get; init;
    }

    /// <summary>The companions a berserk leader took berserk with him (A15.41); null when he took none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireUnitEffect>? CompanionEffects
    {
        get; init;
    }

    /// <summary>What the attack did to each vehicle in the target Location and its crew (A7.307 to A7.309, D.8B); null when there were none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<FireVehicleEffect>? VehicleEffects
    {
        get; init;
    }

    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}

/// <summary>
/// What one attack did to a vehicle (A7.307 to A7.309) and to its Vulnerable crew (D.8B, D5.34, D5.341, A7.82). An unarmored vehicle's
/// Vehicle line result is <c>none</c>, <c>immobilized</c>, <c>eliminated</c>, or <c>burning-wreck</c>; an armored one is never harmed.
/// </summary>
public sealed record FireVehicleEffect(
    string VehicleId,
    string DefinitionId,
    string Result,
    int? KillNumber,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int? UnlikelyKillDr,
    FireCheck? CrewCheck,
    string CrewResult)
{
    /// <summary>Whether the firer declined the Unlikely Kill dr it could make (A7.309, ruling R5.8); null when it made it or could not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? UnlikelyKillDeclined
    {
        get; init;
    }

    public const string None = "none";
    public const string Immobilized = "immobilized";
    public const string Eliminated = "eliminated";
    public const string BurningWreck = "burning-wreck";
    public const string Stunned = "stunned";
    public const string Recalled = "recalled";
    public const string Pinned = "pinned";
    public const string NotVulnerable = "not-vulnerable";
}
