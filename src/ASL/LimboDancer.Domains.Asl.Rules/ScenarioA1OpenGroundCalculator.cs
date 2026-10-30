namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A10.531 Open Ground (p. 67), the one calculator of the definition (the A10.531 claim's design, ruling RA10.1): for rout determination, Dash,
/// concealment, and Interdiction, a hex is Open Ground to "the particular enemy unit(s)" that could apply the -1 FFMO DRM (A4.6, p. 51) in a
/// hypothetical Defensive First Fire. The definition judges the view alone; each reader keeps its own filter of the enemy units it asks (ruling RA10.3:
/// A12.14's Good Order ground unit within 16 hexes, A10.5's Known unit in Normal Range, A10.53's unbroken unit with FP in Normal Range). FFMO's own DRM
/// stays A4.6's (ruling RA10.4). Facts in, verdicts out.
/// </summary>
public static class ScenarioA1OpenGroundCalculator
{
    /// <summary>
    /// Whether a terrain is Open Ground by itself (B1.1, B1.11, p. 113; B15.6, p. 129; B12.3, B14.3, B15.3, B16.3): true for "open-ground" (a road hex
    /// reads so); for "grain", true outside June to September, when it does not exist, false in season, and null when the game names no month (ruling
    /// RA10.7); false for every other terrain, brush, orchard, marsh, and woods among them.
    /// </summary>
    public static bool? OpenGroundTerrain(string? terrainKey, int? month) => terrainKey switch
    {
        "open-ground" => true,
        "grain" => month is { } known ? known is < 6 or > 9 : null,
        _ => false,
    };

    /// <summary>
    /// Whether nothing on one enemy unit's LOS cancels FFMO (A4.6, p. 51; the A10.531 EX, p. 67; B1.16, B1.17, p. 113; B10.31, p. 125): no Hindrance
    /// of any kind along it (the map's, counted as the rout counts it, a vehicle's or wreck's, SMOKE), no wall or hedge TEM on the hexside crossed, no
    /// Height Advantage, and no wreck or AFV in the Location. Low Visibility is not a Hindrance here (A9.222 note, p. 110).
    /// </summary>
    public static bool NoFfmoCover(RoutCoverFacts cover, int losHindrance)
    {
        ArgumentNullException.ThrowIfNull(cover);
        return (cover.MapHindrance ?? losHindrance) + cover.OtherHindrance == 0 && !cover.HexsideTem && !cover.HeightAdvantage && !cover.InHexCover;
    }

    /// <summary>
    /// Whether a Location is Open Ground to one enemy unit (A10.531, p. 67): no SMOKE there and no protected entry (the EX), a clear LOS (a Location
    /// out of its LOS is not Open Ground to it, A4.63 EX, p. 51), and the terrain as that unit sees it Open Ground. For the rout, Dash, and
    /// Interdiction nothing on the LOS may cancel FFMO (<see cref="NoFfmoCover"/>); for concealment only a wreck or AFV in the Location counts, since a
    /// Hindrance, a hexside TEM, or Height Advantage along the LOS does not keep a "?" (ruling RA10.2; A12.14, p. 78). Null only when the terrain's
    /// answer is unknown (grain with no month) and nothing else already says no.
    /// </summary>
    public static bool? OpenGroundTo(OpenGroundLocationFacts location, OpenGroundViewFacts view, OpenGroundPurpose purpose)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(view.Cover);
        if (!Enum.IsDefined(purpose))
        {
            throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Not a purpose of A10.531.");
        }

        if (location.Smoke || location.ProtectedEntry || !view.LosClear)
        {
            return false;
        }

        var covered = purpose == OpenGroundPurpose.Concealment ? view.Cover.InHexCover : !NoFfmoCover(view.Cover, view.LosHindrance);
        return covered ? false : OpenGroundTerrain(view.TerrainAsSeen ?? location.TerrainKey, location.Month);
    }

    /// <summary>
    /// Whether a Location is Open Ground to "the particular enemy unit(s)" (A10.531, p. 67): true when it is to any one of them; null when it is to
    /// none and one answer is unknown; false otherwise, and when there is no enemy unit to ask.
    /// </summary>
    public static bool? OpenGroundToAny(IEnumerable<bool?> views)
    {
        ArgumentNullException.ThrowIfNull(views);
        var unknown = false;
        foreach (var view in views)
        {
            if (view == true)
            {
                return true;
            }

            unknown |= view is null;
        }

        return unknown ? null : false;
    }
}
