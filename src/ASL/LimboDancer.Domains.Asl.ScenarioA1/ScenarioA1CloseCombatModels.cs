using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A unit in a Close Combat Location, as the Close Combat package reads it (unit step 29). Every fact is supplied by the
/// caller; the package never reads a game. Nullable members are required unless marked optional.
/// </summary>
public sealed record CloseCombatUnit(
    string? UnitId,
    string? DefinitionId,
    string? Side,
    bool? Broken,
    bool? Pinned,
    bool? Wounded,
    bool? Disrupted,
    bool? Berserk,
    bool? Fanatic,
    bool? Heroic,
    bool? Concealed,
    bool? Captured,
    bool? Advanced,
    bool? InMelee)
{
    /// <summary>Whether a Green or Conscript MMC is Inexperienced (A19.2), so Lax for Ambush (A11.18, A19.36); optional otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced { get; init; }

    /// <summary>The MMC a SMC is stacked with, declared before either side designates its attacks (A11.14); null when it is alone.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StackedWith { get; init; }

    /// <summary>The SW the unit possesses (A11.13); null or empty when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Weapons { get; init; }

    /// <summary>
    /// The ADJACENT Location a unit held in Melee attempts to Withdraw to (A11.2, A11.21), declared before the attacks; null when it
    /// stands.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WithdrawingTo { get; init; }
}

/// <summary>One declared CC attack (A11.12): the units attacking together, the units they attack, and the leader directing it.</summary>
public sealed record CloseCombatDeclaration(IReadOnlyList<string>? Attackers, IReadOnlyList<string>? Defenders)
{
    /// <summary>The attacking leader who applies his leadership DRM (A11.141); null when none directs.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Director { get; init; }
}

/// <summary>
/// Recorded rolls: each attack's CC DR by attack index, the Random Selection dr of a Partial Kill by "index:unit", Wound
/// Severity by unit, the Leader Creation dr of an Original 2 by attack index (A18.12), and each SW's loss dr (A11.13).
/// </summary>
public sealed record CloseCombatRolls(IReadOnlyDictionary<string, IReadOnlyList<int>>? Attacks)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? RandomSelection { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WoundSeverity { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? LeaderCreation { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, int>? WeaponLoss { get; init; }
}

/// <summary>
/// The CC of one Location in one round (A11.11, A11.12): the CCPh, the Location and its terrain, the ATTACKER's side, the
/// round (simultaneous, or the ambusher's then the ambushed side's, A11.32), the units there, the units that attacked or
/// were attacked earlier this CCPh, the declared attacks, and the rolls.
/// </summary>
public sealed record CloseCombatFacts(
    string? Phase,
    string? LocationId,
    string? Terrain,
    string? AttackerSide,
    string? Round,
    string? Ambusher,
    IReadOnlyList<CloseCombatUnit>? Units,
    IReadOnlyList<string>? AttackedBefore,
    IReadOnlyList<string>? AttackingBefore,
    IReadOnlyList<CloseCombatDeclaration>? Attacks,
    CloseCombatRolls? Rolls)
{
    public const string Simultaneous = "simultaneous";
    public const string AmbusherRound = "ambusher";
    public const string AmbushedRound = "ambushed";
}

/// <summary>A defending unit's Final DR in one attack, with its own DRM (A11.16), and the result for it.</summary>
public sealed record CloseCombatDefenderResult(string UnitId, IReadOnlyList<FireModifier> Drm, int FinalDr, string Result)
{
    public const string Eliminated = "eliminated";
    public const string PartialKill = "partial-kill";
    public const string NoEffect = "no-effect";

    /// <summary>The Random Selection dr of a Partial Kill among several defenders (A11.11); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? RandomSelectionDr { get; init; }

    /// <summary>Whether the Partial Kill Casualty Reduces this unit.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CasualtyReduced { get; init; }
}

/// <summary>One CC attack's arithmetic (A11.11): the FP of each side, the odds column, its black Kill Number, and the DR.</summary>
public sealed record CloseCombatAttackArithmetic(
    int Index,
    string Side,
    IReadOnlyList<string> Attackers,
    IReadOnlyList<string> Defenders,
    IReadOnlyList<FireModifier> FirepowerModifiers,
    decimal AttackFirepower,
    decimal DefenseFirepower,
    string Odds,
    int KillNumber,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    IReadOnlyList<CloseCombatDefenderResult> Defending)
{
    /// <summary>The Leader Creation dr after an Original 2 by an attacking MMC (A18.12); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderCreationOutcome? LeaderCreation { get; init; }
}

/// <summary>What the round did to one unit: eliminated, Reduced to its HS, wounded, or returned from berserk (A15.46).</summary>
public sealed record CloseCombatUnitEffect(string UnitId, string DefinitionId, string FinalDefinitionId, bool Eliminated, bool Wounded, IReadOnlyList<string> Events)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BerserkEnded { get; init; }

    /// <summary>The Location a withdrawing unit that was neither eliminated nor Reduced withdraws to (A11.2); null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? WithdrewTo { get; init; }
}

/// <summary>
/// A leader created by an Original 2 (A18.12): he joins the attack that created him and defends with its MMC. His id in the
/// resolution is <see cref="PlaceholderPrefix"/> and the attack index; the game gives him his own.
/// </summary>
public sealed record CloseCombatCreatedLeader(int Attack, string UnitId, string DefinitionId, string Side, string StackedWith, bool Eliminated, bool Wounded)
{
    public const string PlaceholderPrefix = "created-leader:";
}

/// <summary>A SW of a unit eliminated in CC with an Original colored dr of 1: its loss dr against the black Kill Number (A11.13).</summary>
public sealed record CloseCombatWeaponEffect(string EquipmentId, string HolderId, int Dr, bool Eliminated);

/// <summary>The Close Combat package's answer: resolved with each attack's arithmetic and the effects, or Abstained or Indeterminate.</summary>
public sealed record CloseCombatResolution(
    string Disposition,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<CloseCombatAttackArithmetic> Attacks,
    IReadOnlyList<CloseCombatUnitEffect> Effects,
    IReadOnlyList<CloseCombatCreatedLeader> CreatedLeaders,
    IReadOnlyList<CloseCombatWeaponEffect> WeaponEffects)
{
    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}

/// <summary>
/// Whether an Ambush occurs where Infantry advanced into CC (A11.4): the Location, its terrain, the ATTACKER's side, the
/// units there, and each side's Ambush dr.
/// </summary>
public sealed record AmbushFacts(
    string? Phase,
    string? LocationId,
    string? Terrain,
    string? AttackerSide,
    IReadOnlyList<CloseCombatUnit>? Units,
    IReadOnlyDictionary<string, int>? Rolls);

/// <summary>One side's Ambush dr, its drm, and its Final dr.</summary>
public sealed record AmbushSide(string Side, int Dr, IReadOnlyList<FireModifier> Drm, int FinalDr);

/// <summary>The Ambush drs and the side that ambushes, if either does (A11.4).</summary>
public sealed record AmbushResolution(string Disposition, IReadOnlyList<string> Reasons, IReadOnlyList<AmbushSide> Sides, string? Ambusher);
