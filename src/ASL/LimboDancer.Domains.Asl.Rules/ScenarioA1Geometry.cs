namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// The hex arithmetic the rules decide with (pass 32.a, slice S1). A facing is a hexspine as an int, 0 east and counterclockwise by 60 degrees
/// (Units' <c>UnitFacing</c> order); a hexside is an int, 0 north and clockwise (Maps' <c>HexsideDirection</c> order); a position around a hex
/// is one of twelve, hexspines even and hexsides odd. The callers cast their enums.
/// </summary>
public static class ScenarioA1Geometry
{
    /// <summary>The angle between two bearings in degrees, 0 to 180.</summary>
    public static double AngleBetween(double one, double two) => Math.Abs(((one - two) % 360 + 540) % 360 - 180);

    /// <summary>A vehicle's facing is a hexspine (D2.11), whose direction counterclockwise from east is 60 degrees per facing step.</summary>
    public static double FacingDegrees(int facing) => 60 * facing;

    /// <summary>The two VCAs of a vehicle entering across a hexside: its VCA holds the hex it enters, so it faces a hexspine 30 degrees either side of its travel (D2.11).</summary>
    public static int[] EntryFacings(int hexside) => [(10 - hexside) % 6, (11 - hexside) % 6];

    /// <summary>The fewest hexspines a VCA turns to point at a neighbor at a bearing: its VCA holds the two hexes 30 degrees either side; 6 when none.</summary>
    public static int VcaTurns(int facing, double bearing)
    {
        var turns = int.MaxValue;
        for (var candidate = 0; candidate < 6; candidate++)
        {
            if (Math.Abs(Math.Abs(((bearing - FacingDegrees(candidate) + 540) % 360) - 180) - 30) < 1)
            {
                var steps = Math.Abs(candidate - facing);
                turns = Math.Min(turns, Math.Min(steps, 6 - steps));
            }
        }

        return turns == int.MaxValue ? 6 : turns;
    }

    /// <summary>
    /// Whether LOS entries Encircle a hex (A7.7; ruling R12.11): two opposite hexspines, two opposite hexsides (exactly three vertices between them both
    /// ways), or three hexsides no two of which are adjacent.
    /// </summary>
    public static bool Encircles(IReadOnlyCollection<int> positions)
    {
        ArgumentNullException.ThrowIfNull(positions);
        var set = positions.ToHashSet();
        if (set.Any(position => set.Contains((position + 6) % 12)))
        {
            return true;
        }

        var sides = set.Where(position => position % 2 == 1).Select(position => (position - 1) / 2).ToHashSet();
        return sides.Any(side => sides.Contains((side + 2) % 6) && sides.Contains((side + 4) % 6));
    }
}
