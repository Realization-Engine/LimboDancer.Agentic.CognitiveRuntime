using System.Text.Json.Serialization;

namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The Spotter of a mortar's shot (C9.3, C9.31; ruling R9.4): a Good Order Personnel unit of the firing side in the mortar's hex or an
/// adjacent one, whose LOS the shot is traced along. Its Location and LOS are the planner's map reads.
/// </summary>
public sealed record OrdnanceSpotter(string? UnitId, string? DefinitionId, bool? Broken, bool? Pinned);

/// <summary>
/// A Panzerfaust shot's own facts (C13.3 to C13.36; rulings R9.7, R9.8): the shots the side has taken this scenario and may take, whether
/// the firer is in a ground-level building Location (the Backblast's Case C3), and whether the firer is heroic.
/// </summary>
public sealed record OrdnancePanzerfaust(int? ShotsTaken, int? ShotsAllowed, bool? FromBuilding, bool? Heroic);

/// <summary>
/// The To Hit DR of an Area Target Type shot as judged for one unit of the target hex (C3.33, C3.331; ruling R9.3): the DRM that apply to
/// it alone, its Final DR, and whether the shot hit it.
/// </summary>
public sealed record OrdnanceAreaTarget(string UnitId, IReadOnlyList<FireModifier> Drm, int FinalDr, bool Hit)
{
    /// <summary>
    /// The highest Original To Hit DR that hits it (C3.6; pass 60, boundary review), for the proposal before the roll; below 2 it is hit only on an
    /// Original 2 and a subsequent dr. A Motion firer's lower dr (C5.35) is left out. Never recorded.
    /// </summary>
    [JsonIgnore]
    public int? NeededOriginal
    {
        get; init;
    }
}

/// <summary>
/// One Location of a mortar's target building hex that holds its targets (B23.32, p. 136; pass 60, ruling R60.11): its terrain and its LOS from the
/// firer or Spotter, whose Hindrance is its own Case R (C3.4), and the non-rooftop building levels above it, each +1 to the hit's IFT DR. Every
/// building hex is taken as not an Interior Building Hex, which the board data does not tell.
/// </summary>
public sealed record OrdnanceAreaLocation(string LocationId)
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TargetTerrain
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireLos? Los
    {
        get; init;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? LevelsAbove
    {
        get; init;
    }
}

/// <summary>
/// A mortar's hit on an AFV, resolved on the IFT and never on a To Kill Table (C1.55, C7.345, C9.1, p. 165; pass 60, ruling R60.10): the turret or
/// hull struck (C3.9, from the Original IFT DR; C7.7 note E, ruling R60.23), the FP (half the HE FP, or the full FP doubled by a Critical Hit, C3.72), the Effects DR with the AF and OT
/// DRM and the TEM that gives no HD, the Final DR of the column's K/# result, and the result: <c>burn</c> at most half that DR, <c>eliminated</c> on
/// any other KIA, <c>shock</c> (turret) or <c>immobilized</c> (hull) on that DR and the one above it, else <c>none</c>.
/// </summary>
public sealed record OrdnanceAreaVehicle(string VehicleId, string DefinitionId, string HitLocation, int Firepower, IReadOnlyList<int> Dice, IReadOnlyList<FireModifier> Drm,
    int FinalDr, int KillDr, string Result)
{
    /// <summary>Whether a Critical Hit fell on it (C3.72, C3.74); null when not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Critical
    {
        get; init;
    }

    /// <summary>The Crew Survival DR of an AFV destroyed and not burnt (D5.6); null when none is rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public OrdnanceCrewSurvival? CrewSurvival
    {
        get; init;
    }

    /// <summary>The crew's Immobilization TC after a hull hit immobilized it (D5.5; pass 65, ruling R65.21); null when none was taken.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FireCheck? CrewCheck
    {
        get; init;
    }

    /// <summary>Whether the crew Abandons the AFV after failing its TC (D5.5; pass 65); null when it does not.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? Abandoned
    {
        get; init;
    }

    /// <summary>The Survival DR of each Passenger and Rider of the AFV destroyed and not burnt (D6.9, D9.2; pass 65, ruling R65.21); null when none is rolled.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<PrcSurvival>? PrcSurvival
    {
        get; init;
    }
}

/// <summary>
/// A PF Check dr (C13.31; ruling R9.7): the dr, its drm, the final dr, and what it gave: <c>shot</c> (1 to 3), <c>no-shot</c> (4 or more),
/// or, on an Original 6, <c>pinned</c>, <c>broken</c> (already pinned), or <c>casualty-reduction</c> (berserk or heroic).
/// </summary>
public sealed record OrdnancePanzerfaustCheck(int Dr, IReadOnlyList<FireModifier> Drm, int FinalDr, string Outcome)
{
    public const string Shot = "shot";
    public const string NoShot = "no-shot";
    public const string Pinned = "pinned";
    public const string Broken = "broken";
    public const string CasualtyReduction = "casualty-reduction";
}

/// <summary>The names of the backlog pass 9 target type and weapon.</summary>
public static class OrdnanceTargetTypes
{
    /// <summary>The Area Target Type (C3.33).</summary>
    public const string Area = "area";

    /// <summary>The definition id of the rule-defined Panzerfaust (C13.3), which no counter represents.</summary>
    public const string Panzerfaust = "asl:panzerfaust";
}
