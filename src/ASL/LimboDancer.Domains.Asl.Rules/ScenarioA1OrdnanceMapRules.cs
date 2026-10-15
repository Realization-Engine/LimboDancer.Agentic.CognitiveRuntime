using System.Globalization;

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

    /// <summary>
    /// C2.6 (p. 168; ruling R8.7; pass 55): "a Gun may fire-at/affect a different-level target only if the range is >= the elevation difference
    /// between them", up or down; "Only mortars, AA Guns and Guns capable of using AA fire" fire at a higher target nearer than that; the limit is
    /// "not [applied] to SW [EXC: they do apply to MTR/INF/RCL ordnance SW]", so a LATW is free of it and a light mortar is held to it firing down.
    /// <paramref name="targetAbove"/> is the target's elevation less the firer's. The AA Guns' leave is not built (no counter has it).
    /// </summary>
    public static bool ElevationAllowed(int targetAbove, int? range, string? gunType = null) =>
        targetAbove == 0 || gunType == "latw" || (gunType == "mortar" && targetAbove > 0) || range >= Math.Abs(targetAbove);

    /// <summary>
    /// The refusal of a shot <see cref="ElevationAllowed"/> does not allow (C2.6), with the levels and the range the map gave, or null: "3 levels
    /// apart at range 2".
    /// </summary>
    public static string? ElevationRefusal(string firer, int targetAbove, int? range, string? gunType = null)
    {
        var levels = Math.Abs(targetAbove);
        return ElevationAllowed(targetAbove, range, gunType) ? null
            : $"play.ordnance-elevation: {firer} and its target are {levels.ToString(CultureInfo.InvariantCulture)} level{(levels == 1 ? string.Empty : "s")} apart at range "
                + $"{(range ?? 0).ToString(CultureInfo.InvariantCulture)}, and a Gun fires at another level only at a range of at least the elevation difference between them (C2.6)";
    }

    /// <summary>
    /// C2.6 (p. 168; pass 55): "A Gun in a building hex may not fire at a lower-level target in its own hex, and may fire at an higher-level target
    /// in its hex only if it is an AA Gun of &lt;= 40mm. These restrictions also apply to non-AA vehicular MG". At range 0 the general limit refuses
    /// every other hex with levels too, so a Gun and a vehicle's weapons fire at no other level of their own hex.
    /// </summary>
    public static string? OwnHexLevelRefusal(string firer, bool sameHex, bool sameLocation) =>
        sameHex && !sameLocation
            ? $"play.ordnance-own-hex: {firer} does not fire at another level of its own hex: a Gun or a vehicle's weapon fires at another level only at a range of at least the elevation difference (C2.6)"
            : null;

    /// <summary>
    /// B11.31, B11.32 (p. 126; pass 55), C2.6's exception: "No vehicle armament, IFE, or ordnance may fire through a cliff hexside to an adjacent
    /// lower level Location [EXC: LATW other than PIAT or MTR with minimum range of 1 hex]", and to an adjacent higher one "Only an AA weapon, MG
    /// counter, SW ATR, PIAT, MTR with minimum range of 1 hex and/or inherent Small Arms Fire". The AA weapons' leave is not built.
    /// <paramref name="targetAbove"/> is the target's elevation less the firer's.
    /// </summary>
    public static string? CliffRefusal(bool cliffHexside, int? range, int targetAbove, string? gunType, string? latwType, int? rangeMinimum)
    {
        if (!cliffHexside || range != 1 || targetAbove == 0 || (gunType == "mortar" && rangeMinimum <= 1))
        {
            return null;
        }

        if (gunType == "latw" && (targetAbove < 0 ? latwType != "piat" : latwType is "atr" or "piat"))
        {
            return null;
        }

        return targetAbove < 0
            ? "play.ordnance-cliff: ordnance does not fire through a cliff hexside to an adjacent lower Location (B11.31)"
            : "play.ordnance-cliff: ordnance does not fire through a cliff hexside to an adjacent higher Location (B11.32)";
    }

    /// <summary>
    /// B10.31, C6.8, D4.22 (pp. 125, 175, 202; pass 55): the Height Advantage the shot's map read found, carried to the To Hit DR as Case Q. A target
    /// behind a wall or hedge the LOS crosses has that TEM to claim and so no Height Advantage; the wall's own TEM on a To Hit DR is not built.
    /// The Area Target Type takes no Case Q (C6.8), and a mortar's fire is Indirect Fire (C.1). Null when there is none.
    /// </summary>
    public static bool? HeightAdvantage(bool? read, int? hexsideTem, bool areaTargetType = false) => read == true && hexsideTem is not > 0 && !areaTargetType ? true : null;

    /// <summary>
    /// C8.31 (p. 178; pass 60, ruling R60.4): whether a HEAT shot with no vehicle named is at Infantry "receiving a wall ... TEM" from the firer, as the
    /// fire probe's read of the hexside found it (B9.3). Null for every other shot, so that no record gains the fact but a HEAT shot at Infantry.
    /// </summary>
    public static bool? HeatWallTem(bool vehicleTarget, string? ammunition, string? hexsideTerrain) =>
        !vehicleTarget && ammunition == "heat" && hexsideTerrain == "wall" ? true : null;

    /// <summary>
    /// C13.8 (p. 185), its last sentence: "These weapons [PF/PFk, BAZ, PSK, RCL] also may not fire at a target two or more levels higher in an
    /// adjacent hex, or directly above them in the same hex." C2.6 frees a LATW of its own limit, so this one is the weapon's. An ATR is not
    /// one of "these weapons". <paramref name="targetAbove"/> is the target's elevation less the firer's.
    /// </summary>
    public static string? BackblastElevationRefusal(string firer, string? latwType, int? range, bool sameHex, int targetAbove) =>
        latwType is not ("pf" or "psk" or "baz") || targetAbove <= 0 ? null
        : sameHex ? $"play.latw-elevation: {firer} does not fire a PF, PSK, or BAZ at a target directly above it in its own hex (C13.8)"
        : range == 1 && targetAbove >= 2
            ? $"play.latw-elevation: {firer} does not fire a PF, PSK, or BAZ at a target {targetAbove.ToString(CultureInfo.InvariantCulture)} levels higher in an adjacent hex: "
                + "two or more levels is too steep (C13.8)"
        : null;

    /// <summary>
    /// B23.423, C13.8, C13.81 (rulings R9.8, R9.11): a mortar fires from no building, a PF or PSK from no building's upper level. A MG fired as ordnance
    /// (<paramref name="machineGun"/>; pass 60) is held to neither.
    /// </summary>
    public static string? SupportWeaponLocationRefusal(string? latwType, bool building, int level, bool machineGun = false)
    {
        if (latwType is null && building && !machineGun)
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
    /// D8.4 (p. 209; pass 35, task 35.13 j): whether a vehicle began its MPh bogged and has not left its Bog hex: it has entered no hex this MPh,
    /// and it is still bogged or has made a Bog Removal attempt. Its MP may have gone on anything else first, such as unloading (C.8, p. 162; D8.5).
    /// The steps are this MPh's, oldest first.
    /// </summary>
    public static bool? InBogHex(IReadOnlyList<(string Kind, bool BogRemoval)> steps, bool bogged = false)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return steps.Count > 0 && (bogged || steps.Any(step => step is ("start", true))) && !steps.Any(step => step.Kind is "enter" or "exit" or "overrun") ? true : null;
    }

    /// <summary>D3.2, D2.32, C5.5 (review of pass 50): a shot within the firer's own Location at or by a vehicle in Bypass has its own Target Facing and LOS; not built.</summary>
    public static string? SameHexBypassRefusal(bool firerInBypass, bool targetInBypass) =>
        firerInBypass || targetInBypass
            ? "play.ordnance-own-hex: a shot within its own hex by or at a vehicle in Bypass is not built: its Target Facing and LOS are read from the CAFP (D2.32, C5.5)"
            : null;

    /// <summary>C5.13 (p. 172; pass 50): "a Bounding First Firer must always fire within its current CA; it may change that CA only via MP expenditure".</summary>
    public static string? BoundingArcRefusal(string? vehicleId, bool boundingFirstFire, int steps) =>
        boundingFirstFire && steps > 0
            ? $"play.ordnance-bounding-arc: {vehicleId} fires its MA within its current TCA in Bounding First Fire; turn the turret with an MP expenditure first (C5.13, D3.12)"
            : null;

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
            return $"refused: {levels} level{(levels == 1 ? string.Empty : "s")} apart at range {range}, and the range must be at least the elevation difference (C2.6)";
        }

        if (bearing() is not { } read)
        {
            return $"range {range}; its CA cannot be read here";
        }

        var steps = GunTurnSteps(facing, read);
        return steps == 0 ? $"range {range}, in its CA" : $"range {range}, turn {steps} hexspine(s) (Case A)";
    }
}
