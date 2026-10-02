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
public sealed record OrdnanceAreaTarget(string UnitId, IReadOnlyList<FireModifier> Drm, int FinalDr, bool Hit);

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
