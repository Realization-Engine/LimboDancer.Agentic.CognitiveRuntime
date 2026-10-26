namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Street Fighting (A11.8, p. 76; pass 70, ruling R70.33): "Any vehicle in a road hex and ADJACENT to a building hex on both sides of that road is especially
/// vulnerable to CC in that hex. All Infantry advancing into that road Location from ground level of one or more of those adjacent building hexes qualify for
/// a Streetfighting Ambush -1/+1 DRM in CC vs the vehicle ... unless actual Ambush occurs"; "following any CC attack [they are] returned to the same Locations
/// they came from ... or at their option remain in the road Location. If broken or pinned or on top of Wire, they would not be returned". Play reads the
/// road's sides from the map (the Maps read of a hex's road flanks), where each unit advanced from, and its state; the vehicle CC package applies the DRM.
/// Reaction Fire from the buildings (D7.211) is pass 105's, and Street Fighting in Bypass pass 100's; Wire and mines on the return are not built.
/// </summary>
public static class ScenarioA1StreetFighting
{
    /// <summary>
    /// Whether a road hex is open to Street Fighting: of the sides of its road (the runs of its non-road hexsides, each with its ADJACENT building hexes), at
    /// least two hold a building. A hex where three roads meet has three sides; any two of them with buildings do (a reading).
    /// </summary>
    public static bool Flanked(IReadOnlyList<IReadOnlyList<string>> sides)
    {
        ArgumentNullException.ThrowIfNull(sides);
        return sides.Count(side => side.Count > 0) >= 2;
    }

    /// <summary>
    /// Whether a unit qualifies in the road Location (A11.8): an enemy vehicle is there (or its wreck, once the CC destroyed it), and the unit advanced into it
    /// this APh from the ground level of one of the ADJACENT building hexes of a road hex open to Street Fighting.
    /// </summary>
    public static bool Qualifies(IReadOnlyList<IReadOnlyList<string>> sides, bool advancedHere, bool enemyVehicleThere, string? advancedFromHex, int? advancedFromLevel)
    {
        ArgumentNullException.ThrowIfNull(sides);
        return advancedHere && enemyVehicleThere && advancedFromLevel == 0 && advancedFromHex is not null && Flanked(sides)
            && sides.Any(side => side.Contains(advancedFromHex, StringComparer.Ordinal));
    }

    /// <summary>
    /// Why a unit may not return to the building Location it advanced from (A11.8), or null: in the CCPh, once the CC of the road Location is over, a unit
    /// that qualified there and is still there returns unless it is broken, pinned, or a prisoner; it may instead stay, which needs no step.
    /// </summary>
    public static string? ReturnBar(string unit, string? phase, bool qualified, bool stillThere, bool closed, bool broken, bool pinned, bool captured) =>
        phase != "ccph" ? $"play.street-fighting: {unit} returns to the building it advanced from in the CCPh, after the CC of its road Location (A11.8)"
        : !qualified || !stillThere ? $"play.street-fighting: {unit} did not advance into a vehicle's road Location from the ground level of a building ADJACENT to it on a side of the road, so it has nowhere to return to (A11.8)"
        : !closed ? $"play.street-fighting: the CC of {unit}'s Location is not over; it returns following its CC attacks (A11.8)"
        : broken || pinned || captured ? $"play.street-fighting: {unit} is {(captured ? "a prisoner" : broken ? "broken" : "pinned")}, so it is not returned to its building Location and stays (A11.8)"
        : null;

    /// <summary>
    /// Whether Street Fighting's -1/+1 applies to the units of an attack on the vehicle, or to the defenders of its attack (A11.8): every one qualifies, and
    /// no actual Ambush occurred in the Location ("not cumulative with the normal -1/+1 CC DRM for Ambush").
    /// </summary>
    public static bool Applies(string? ambusher, IReadOnlyList<string>? fighters, IEnumerable<string> units)
    {
        ArgumentNullException.ThrowIfNull(units);
        return ambusher is null && fighters is { Count: > 0 } && units.All(unit => fighters.Contains(unit, StringComparer.Ordinal));
    }

    /// <summary>The proposal's words for Street Fighting's DRM in an attack on the vehicle, or in the vehicle's attack on the units that qualify.</summary>
    public static string DrmText(bool byVehicle) => byVehicle
        ? "; the units that advanced from an ADJACENT building qualify for Street Fighting, so the vehicle's attack on them takes +1 (A11.8)"
        : "; the attackers advanced from an ADJACENT building, so Street Fighting gives their attack on the vehicle -1 (A11.8)";

    /// <summary>The proposal's line for a return: "[F4]: 4-4-7 squad R1 returns to [F3], the building Location it advanced from (A11.8)".</summary>
    public static string ReturnText(string unit, string at, string to) =>
        $"play.street-fighting: {unit} in {at} returns to {to}, the building Location it advanced from, its CC with the vehicle over; it could stay instead (A11.8)";
}
