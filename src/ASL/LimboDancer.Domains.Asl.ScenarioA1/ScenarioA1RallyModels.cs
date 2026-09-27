using System.Text.Json.Serialization;

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
    RallyRolls? Rolls)
{
    /// <summary>Whether a Known enemy unit is in the unit's LOS (A15.44), the planner's read; needed when a leader's rally can reach Heat of Battle.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? KnownEnemyInLos { get; init; }

    /// <summary>The ADJACENT Known Good Order armed enemy Infantry the unit may surrender to (A15.5), the planner's read; needed as above.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? Captors { get; init; }

    /// <summary>The other friendly units in the Location, which a leader who goes berserk tries to take with him (A15.41); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RallyCompanion>? Companions { get; init; }
}

/// <summary>Another friendly unit in the rallying unit's Location.</summary>
public sealed record RallyCompanion(string? UnitId, string? DefinitionId, bool? Broken, bool? Wounded, bool? Fanatic, bool? Heroic, bool? Berserk);

/// <summary>A companion's NTC to go berserk with a berserk leader (A15.41).</summary>
public sealed record RallyBerserkCheck(string UnitId, IReadOnlyList<int> Dice, int OriginalDr, IReadOnlyList<FireModifier> Drm, int FinalDr, int MoraleLevel, bool Passed);

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
    bool? OtherActionThisPhase)
{
    /// <summary>Whether the unit is Fanatic (A10.8): its broken Morale Level is one higher; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>Whether a Green or Conscript MMC is Inexperienced (A19.2), for the Heat of Battle DRM; required for those classes.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Inexperienced { get; init; }
}

/// <summary>The Good Order leader attempting to rally the unit (A10.6); none for Self-Rally.</summary>
public sealed record RallyLeader(
    string? UnitId,
    string? DefinitionId,
    string? LocationId,
    bool? Broken,
    bool? Wounded,
    bool? Concealed);

/// <summary>
/// Recorded rolls: the Rally DR, a leader's Wound Severity dr after Fate (A10.64, A17.11), the Heat of Battle DR after a
/// leader's rally on an Original 2 (A15.1), and the Leader Creation dr after the first MMC Self-Rally's Original 2 (A18.11).
/// </summary>
public sealed record RallyRolls(IReadOnlyList<int>? Rally, int? WoundSeverity)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<int>? HeatOfBattle { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LeaderCreation { get; init; }

    /// <summary>The NTC DR of each companion a berserk leader tries to take with him (A15.41).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, IReadOnlyList<int>>? BerserkChecks { get; init; }
}

/// <summary>The Leader Creation dr and its drm (A18.2), and the leader it created; null when the Final dr created none.</summary>
public sealed record LeaderCreationOutcome(int Dr, IReadOnlyList<FireModifier> Drm, int FinalDr, string? LeaderDefinitionId);

/// <summary>The attempt's arithmetic, as A10.6 to A10.64 resolve it.</summary>
public sealed record RallyArithmetic(
    string Kind,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    IReadOnlyList<FireModifier> Drm,
    int FinalDr,
    int MoraleLevel,
    bool Rallied,
    bool Fate)
{
    /// <summary>The Heat of Battle DR after a leader's rally on an Original 2 (A15.1); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public HeatOfBattleOutcome? HeatOfBattle { get; init; }

    /// <summary>The Leader Creation dr after the first MMC Self-Rally's Original 2 (A18.11, A18.2); null when none was rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public LeaderCreationOutcome? LeaderCreation { get; init; }

    /// <summary>The companions' NTC after a leader went berserk (A15.41); null when none was taken.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<RallyBerserkCheck>? BerserkChecks { get; init; }
}

/// <summary>What the attempt did to the unit, and which units lost "?" by it (A12.141).</summary>
public sealed record RallyEffect(
    string UnitId,
    string DefinitionId,
    string FinalDefinitionId,
    bool Rallied,
    bool Eliminated,
    bool Wounded,
    IReadOnlyList<string> ConcealmentLost,
    IReadOnlyList<string> Events)
{
    /// <summary>Whether the unit is Fanatic after the attempt (A10.8); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Fanatic { get; init; }

    /// <summary>Whether a rallied leader became heroic (A15.21); null when he did not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Heroic { get; init; }

    /// <summary>The hero the attempt created (A15.21); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HeroDefinitionId { get; init; }

    /// <summary>The leader Field Promotion created (A18.11); null when none.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CreatedLeaderDefinitionId { get; init; }

    /// <summary>Whether the unit went berserk (A15.4); null when it did not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Berserk { get; init; }

    /// <summary>Whether a Surrender result Disrupted the unit (A15.5); null when it did not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Disrupted { get; init; }

    /// <summary>The companions that went berserk with a berserk leader (A15.41); null when none did.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? BerserkCompanions { get; init; }
}

/// <summary>The Rally package's answer: resolved with its arithmetic and effect, or Abstained or Indeterminate with reasons.</summary>
public sealed record RallyResolution(string Disposition, IReadOnlyList<string> Reasons, RallyArithmetic? Arithmetic, RallyEffect? Effect)
{
    public const string Resolved = "resolved";
    public const string Abstained = "abstained";
    public const string Indeterminate = "indeterminate";
}
