using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

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

    /// <summary>
    /// The MP the vehicle has spent in a continuous LOS to its target in this MPh, rounded up (C5.31 to C5.33, p. 172; pass 50): at most two is
    /// Case C2, up to three Case C1, more Case C; 99 for a LOS held since before the MPh. A map read, recorded with a Bounding First Fire shot.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? MpInLos
    {
        get; init;
    }

    /// <summary>
    /// Whether the vehicle fired its MA in this Player Turn's MPh as Bounding First Fire (D3.32, p. 200; pass 50): its AFPh shot on the ROF it kept
    /// uses Case C though it entered no new hex. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoundingFirer
    {
        get; init;
    }

    /// <summary>
    /// The vehicle's other weapons against this shot (D3.5, D3.51, pp. 200 and 201; pass 50). Null in a record made before pass 50, which then
    /// resolves as it was recorded.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceVehicleWeapons? Weapons
    {
        get; init;
    }

    /// <summary>
    /// The leadership modifier of the vehicle's Armor Leader on its MA's To Hit DR (D3.44, p. 200; pass 55, ruling R55.16), or the +1 of an
    /// Inexperienced Crew (D3.45; ruling R55.18). Null is none, which is every record made before pass 55.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? ArmorLeadership
    {
        get; init;
    }
}

/// <summary>
/// A tank's MA beside its other weapons (D3.5, D3.51, pp. 200 and 201; pass 50): whether a weapon of the vehicle fired in another fire phase of
/// this Player Turn, so that the MA has forfeited its shot; whether the vehicle's fire counter was placed by its other weapons alone, in this
/// fire phase, so that the MA has not fired; and the Case A DRM the first turret-mounted weapon to fire in this fire phase paid, which the MA's
/// first shot within the current TCA pays too.
/// </summary>
public sealed record OrdnanceVehicleWeapons(bool OtherPhase, bool MarkedByOthers, int? MaintainedCaseA)
{
    /// <summary>
    /// Whether the vehicle's Bounding Fire counter stands beside a shot of its MA in this MPh, an OVR among them (D3.3, D7.1; review of pass 50):
    /// the MA then fires again only on a ROF it kept. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? BoundingFireSpent
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
    /// <summary>Whether the target is in Bypass (D2.38, p. 198; pass 35, task 35.13 b): no beneficial TEM of the woods or building it Bypasses; null is false.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Bypass
    {
        get; init;
    }

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

    /// <summary>
    /// The Morale Level of the target's crew when it is its Armor Leader's (D3.42, p. 200; pass 55, ruling R55.17) or an Inexperienced Crew's 6
    /// (D3.45; ruling R55.18). Null is its nationality's (D5.1), which is every record made before pass 55; a record keeps it only when the
    /// hit made the crew check against it (D3.41).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? CrewMorale
    {
        get; init;
    }

    /// <summary>
    /// Whether the target's crew is CE and so Vulnerable to a Specific Collateral Attack (D5.3, D.8A; pass 60, ruling R60.7): a state fact, kept only
    /// on a shot that carries <see cref="OrdnanceShot.SpecificCollateral"/>. Null is false.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? CrewExposed
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
