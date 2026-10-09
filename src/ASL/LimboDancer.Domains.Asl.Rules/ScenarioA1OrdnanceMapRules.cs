namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The rule decisions of an ordnance shot's map reads (S7): the Covered Arc and the hexspines turned (C3.2, C5.1), the Target Facing (D3.2,
/// ruling R7.4), the level rule (C2.6, ruling R8.7), the SW firing-Location rules (B23.423, C13.8), the gunshield (C11.5), the MP spent in a
/// firer's LOS (C6.11, C6.15), and a Gun's target read (C3.2, C2.25; ruling R8.9). The bearings, LOS, ranges, and terrain are read by the caller.
/// </summary>
public static class ScenarioA1OrdnanceMapRules
{
    /// <summary>The angle in degrees, 0 to 180, between two bearings.</summary>
    public static double Off(double one, double two) => Math.Abs(((one - two) % 360 + 540) % 360 - 180);

    /// <summary>Whether a bearing lies within 30 degrees of a hexspine facing, the boundary rows included (C3.2).</summary>
    public static bool WithinArc(double bearing, int facing) => Off(bearing, facing * 60) <= 30 + 1e-6;

    /// <summary>C3.2, C5.1: the facing that brings the bearing into the Covered Arc by the fewest hexspines, and that number.</summary>
    public static (int Facing, int Steps) CoveredArcTurn(int facing, double bearing) =>
        Enumerable.Range(0, 6).Select(step => (Facing: (facing + step) % 6, Steps: Math.Min(step, 6 - step)))
            .Where(item => Off(bearing, item.Facing * 60) <= 30 + 1e-6).OrderBy(item => item.Steps).First();

    /// <summary>The hexspines a Gun turns to bring the bearing into its CA; 0 when none does.</summary>
    public static int GunTurnSteps(int facing, double bearing) =>
        Enumerable.Range(0, 6).Where(step => Off(bearing, facing * 60 + step * 60) <= 30 + 1e-6).Select(step => Math.Min(step, 6 - step)).DefaultIfEmpty(0).Min();

    /// <summary>C5.11: a non-turreted MA that must turn to fire is refused.</summary>
    public static string? VcaRefusal(string? tankId, bool turreted, int steps) =>
        tankId is not null && !turreted && steps > 0
            ? $"play.ordnance-vca: {tankId}'s MA is not in a turret, and pivoting the vehicle to fire is not reviewed (C5.11)"
            : null;

    /// <summary>D3.2 (ruling R7.4): front within 60 degrees, side to 120, rear beyond.</summary>
    public static string TargetFacing(double off) => off <= 60 + 1e-6 ? "front" : off <= 120 + 1e-6 ? "side" : "rear";

    /// <summary>
    /// D3.2, D2.32 (ruling R11.2): a vehicle's hull and turret Target Facings from the bearing back to the firer; a Bypass facing replaces both.
    /// The turret's facing is read lazily, as before.
    /// </summary>
    public static (string Hull, string? Turret) VehicleTargetFacings(double back, int hull, bool turreted, string? bypassFacing, Func<int?> turret) =>
        (bypassFacing ?? TargetFacing(Off(back, hull * 60)),
            turreted ? bypassFacing ?? (turret() is { } tca ? TargetFacing(Off(back, tca * 60)) : null) : null);

    /// <summary>C2.6 (ruling R8.7): a Gun fires at another level only if the range is at least the elevation difference.</summary>
    public static bool ElevationAllowed(int rise, int? range) => rise == 0 || range >= rise;

    /// <summary>B23.423, C13.8, C13.81 (rulings R9.8, R9.11): a mortar fires from no building, a PF or PSK from no building's upper level.</summary>
    public static string? SupportWeaponLocationRefusal(string? latwType, bool building, int level)
    {
        if (latwType is null && building)
        {
            return "play.ordnance-mortar-building: a mortar does not fire from a building Location (B23.423)";
        }

        if (latwType is "pf" or "psk" && building && level > 0)
        {
            return "play.panzerfaust-backblast: a PF or PSK is not fired from above a building's ground level; Desperation fire is not built (C13.8, C13.81; rulings R9.8, R9.11)";
        }

        return null;
    }

    /// <summary>Whether a terrain is a building Location.</summary>
    public static bool IsBuilding(string terrain) => terrain is "wooden-building" or "stone-building";

    /// <summary>C5.34, C13.8 (pp. 172, 185; pass 35, task 35.10): a PF or PSK fired from a building or from rubble has its Backblast to answer for.</summary>
    public static bool IsBackblastLocation(string terrain) => IsBuilding(terrain) || ScenarioA1Definitions.IsRubbleTerrain(terrain);

    /// <summary>C6.14: Infantry moving into Open Ground with no Hindrance.</summary>
    public static bool OpenGround(string? terrain, int? hindranceDrm) => terrain == "open-ground" && hindranceDrm == 0;

    /// <summary>
    /// D8.4 (p. 209; pass 35, task 35.13 j): whether a vehicle began its MPh bogged and has not left its Bog hex: its first MP expenditure of the MPh
    /// was Bog Removal and it has entered no hex since. The steps are this MPh's, oldest first.
    /// </summary>
    public static bool? InBogHex(IReadOnlyList<(string Kind, bool BogRemoval)> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Count > 0 && steps[0] is ("start", true) && !steps.Any(step => step.Kind is "enter" or "exit") ? true : null;
    }

    /// <summary>
    /// C6.11, C6.12, C6.15 (search): the MP spent in the firer's continuous LOS, counted back over the steps (oldest first) to the last one out of
    /// LOS; when every step is seen, all of it if the vehicle began out of LOS, else 99. The LOS reads are lazy and taken latest first.
    /// </summary>
    public static int MpInLos(IReadOnlyList<(int HalfMp, Func<bool> Clear)> steps, Func<bool> begunOutOfLos)
    {
        var halfMp = 0;
        for (var index = steps.Count - 1; index >= 0; index--)
        {
            if (!steps[index].Clear())
            {
                return (halfMp + 1) / 2;
            }

            halfMp += steps[index].HalfMp;
        }

        return begunOutOfLos() ? (halfMp + 1) / 2 : 99;
    }

    /// <summary>C11.5 (referee, pass 8): the gunshield of an AT or INF Gun counts for a Good Order crew not moving, facing a firer within its CA.</summary>
    public static bool Gunshield(string? gunType, bool withinCa, bool crewKind, bool moving) => gunType is "at" or "inf" && withinCa && crewKind && !moving;

    /// <summary>C3.2, C2.25, C2.6 (ruling R8.9): a Gun's read of a target, after its own Location and LOS are settled; bearing and levels read lazily.</summary>
    public static string GunTargetStatus(int range, int? maximum, Func<int?> rise, Func<double?> bearing, int facing)
    {
        if (maximum is { } most && range > most)
        {
            return $"beyond its range of {most} hexes";
        }

        if (rise() is { } levels && levels > range)
        {
            return $"refused: {levels} levels apart at range {range} (C2.6)";
        }

        if (bearing() is not { } read)
        {
            return $"range {range}; its CA cannot be read here";
        }

        var steps = GunTurnSteps(facing, read);
        return steps == 0 ? $"range {range}, in its CA" : $"range {range}, turn {steps} hexspine(s) (Case A)";
    }
}
