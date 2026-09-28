using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.ScenarioA1;

/// <summary>
/// A tank firing its MA (backlog pass 7, ruling R7.10): whether its crew is BU (Case I, C5.9), whether it is in Motion (D2.4), and whether
/// its crew is Stunned, Shocked, or Recalled (D5.34, C7.42, D5.341), none of which may fire.
/// </summary>
public sealed record OrdnanceVehicleFirer(bool? ButtonedUp, bool? InMotion, bool? Stunned, bool? Shocked, bool? Recalled)
{
    /// <summary>Whether the AFV is under a "+1" counter after a Stun (D5.34): +1 to its To Hit DR.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StunRecovery
    {
        get; init;
    }

    /// <summary>Whether the AFV entered a new hex in its MPh this Player Turn (C5.3 Case C, not built: its AFPh shot is outside).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Moved
    {
        get; init;
    }
}

/// <summary>
/// The vehicle a Vehicle Target Type shot fires at (C3.31; rulings R7.2 to R7.9): its Target Facing to the firer for a hull hit (VCA) and a
/// turret hit (TCA), <c>front</c>, <c>side</c>, or <c>rear</c> (D3.2); whether it is a moving target (C.8, Case J), Non-Stopped or in Motion
/// (Case L), concealed (Case K); whether its crew is Stunned, Shocked, or broken for Crew Survival (D5.6); and whether its crew may take an
/// Immobilization TC (D5.5).
/// </summary>
public sealed record OrdnanceVehicleTarget(
    string? VehicleId,
    string? DefinitionId,
    string? HullFacing,
    string? TurretFacing,
    bool? Moving,
    bool? NonStopped,
    bool? Concealed,
    bool? CrewImpaired,
    bool? CrewMayTakeTc)
{
    /// <summary>Whether the target is Abandoned (D5.41): it has no crew to survive its elimination (D5.6).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Abandoned
    {
        get; init;
    }

    /// <summary>Whether the target is under a "+1" counter after a Stun (D5.34): +1 to its crew's NTC, TC, and Crew Survival DR.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? StunRecovery
    {
        get; init;
    }
}

/// <summary>
/// A To Kill resolution (C7; rulings R7.4 to R7.9): the ammunition, where the hit struck and its Target Facing, the Basic, Modified, and Final
/// TK# with their modifications and the AF, the TK DR, its result, and the crew's checks: the NTC of a possible Shock (C7.41), the
/// Immobilization TC (D5.5), and the Crew Survival DR (D5.6).
/// </summary>
public sealed record OrdnanceKill(
    string Ammunition,
    string HitLocation,
    string TargetFacing,
    int? BasicTk,
    IReadOnlyList<FireModifier> Modifications,
    int? ModifiedTk,
    int? ArmorFactor,
    int FinalTk,
    IReadOnlyList<int> Dice,
    int OriginalDr,
    string Result)
{
    public const string Dud = "dud";
    public const string None = "none";
    public const string Burn = "burn";
    public const string Eliminated = "eliminated";
    public const string Immobilized = "immobilized";
    public const string Shock = "shock";
    public const string PossibleShock = "possible-shock";

    /// <summary>The crew's NTC of a possible Shock (C7.41); null when none was taken.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireCheck? ShockCheck
    {
        get; init;
    }

    /// <summary>Whether the AFV is Shocked by the hit (C7.41, C7.42); null when it is not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Shocked
    {
        get; init;
    }

    /// <summary>The crew's Immobilization TC (D5.5); null when none was taken.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireCheck? CrewCheck
    {
        get; init;
    }

    /// <summary>Whether the crew Abandons the immobilized AFV after failing its TC (D5.5); null when it does not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Abandoned
    {
        get; init;
    }

    /// <summary>The Crew Survival DR (D5.6); null when none is rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCrewSurvival? CrewSurvival
    {
        get; init;
    }
}

/// <summary>A Crew Survival DR (D5.6): the dice, +1 when the crew was Stunned, Shocked, or broken, the Final DR, the CS#, and whether it survives.</summary>
public sealed record OrdnanceCrewSurvival(IReadOnlyList<int> Dice, int Drm, int FinalDr, int CrewSurvival, bool Survived);

/// <summary>An armored or unarmored vehicle as the To Kill resolution reads its reviewed catalog definition (D1.2, D1.6, D1.7, D5.6).</summary>
public sealed record ArmorDefinition(
    string Id,
    string Nationality,
    bool Unarmored,
    int? FrontAf,
    int? SideAf,
    string? TurretFront,
    string? TurretSide,
    string? MaType,
    string? TargetSize,
    int? CrewSurvival,
    bool CrewSurvivalPassengersOnly,
    bool OpenTopped)
{
    /// <summary>Whether the vehicle has a turret (D1.31 to D1.322).</summary>
    public bool Turreted => MaType is "t" or "st" or "rst" or "1mt";
}
