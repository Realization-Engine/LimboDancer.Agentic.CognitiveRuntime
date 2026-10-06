namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// Infantry movement over the terrain of backlog pass 10 (rulings R10.1 to R10.3, R10.5, R10.7; pass 32.b, slice S3, from Play's planner): the cost
/// of one step between Locations, levels in buildings and on hills, walls and hedges on entry. The caller reads the map and the state and hands the
/// facts over; a SMOKE or weather cost it reads only when a function here asks, as the planner read it.
/// </summary>
public static class ScenarioA1TerrainCosts
{
    /// <summary>The wall or hedge on a hexside, from its hexside terrain's name, as <c>wall</c> or <c>hedge</c>; null for none; <c>other</c> for hexside terrain the pass does not review.</summary>
    public static string? WallOn(string? hexsideTerrain) => hexsideTerrain switch
    {
        null => null,
        "Wall" => "wall",
        "Hedge" => "hedge",
        _ => "other",
    };

    /// <summary>
    /// The half MF Infantry spend to enter a terrain (B15.4, B15.6; ruling R5.19): grain costs 1½ MF from April to September and is Open Ground
    /// otherwise, and a game with no scenario month does not decide it; null when the terrain is not a reviewed entry. The concealment rules read grain
    /// as Concealment Terrain from June to September (<see cref="ScenarioA1Definitions.IsConcealmentTerrain"/>): two seasons under one citation,
    /// kept apart for pass 45 (the pass 32 design, section 12).
    /// </summary>
    public static int? InfantryEntryHalfMf(string terrain, int? month)
    {
        if (terrain == "grain")
        {
            return month is not { } known ? null : known is >= 4 and <= 9 ? ScenarioA1ResultTables.EntryHalfMf["grain"] : ScenarioA1ResultTables.EntryHalfMf["open-ground"];
        }

        return ScenarioA1ResultTables.EntryHalfMf.TryGetValue(terrain, out var halfMf) ? halfMf : null;
    }

    /// <summary>
    /// The cost of an Infantry step from one Location to another (rulings R10.1 to R10.3, R10.5): up or down one level in a stairwell hex
    /// (B23.4, 1 MF); into an ADJACENT hex of the same building at an upper level (B23.421, 2 MF); or into an adjacent hex at ground level at
    /// its terrain's cost (A4.13), a road hexside's rate (A4.132), doubled one level up (A4.133, B10.4), with Abrupt Elevation Changes
    /// (B10.51), one MF more across a wall or hedge (B9.4), and one more into SMOKE or a burning wreck (A24.7, B25.141). Null with the reason
    /// when the step is not allowed or not reviewed. <paramref name="smokeHalfMf"/> is read for the Location entered where the planner read it.
    /// </summary>
    public static (InfantryEntry? Entry, string? Reason) InfantryStep(InfantryStepFacts facts, Func<int> smokeHalfMf, InfantryWeatherRead weather)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(smokeHalfMf);
        ArgumentNullException.ThrowIfNull(weather);
        if (facts.FromIsHexside || facts.ToIsHexside)
        {
            return (null, "play.move-step: a unit moves between Locations, not hexsides");
        }

        if (facts.SameHex)
        {
            // B23.4, B23.22, B23.23 (ruling R10.2): one level up or down within a building, only in a stairwell hex.
            if (facts.FromLevel == facts.ToLevel || Math.Abs(facts.FromLevel - facts.ToLevel) != 1 || facts.From is not { } fromLevel
                || facts.To is not { } toLevel || !ScenarioA1Definitions.IsBuildingTerrain(fromLevel.TerrainKey) || !ScenarioA1Definitions.IsBuildingTerrain(toLevel.TerrainKey))
            {
                return (null, $"play.move-level: {facts.ToText} is not the next level of the building Location {facts.FromText} (B23.4, B23.42)");
            }

            return !fromLevel.Stairway
                ? (null, $"play.move-stairwell: {facts.FromHexText} has no stairwell, so its levels do not connect (B23.23, B23.4)")
                // E1.51 (referee, pass 16): a building Location is Concealment Terrain, one MF more at night.
                : (new InfantryEntry(2 + smokeHalfMf() + (facts.Night ? 2 : 0), toLevel.TerrainKey!, false, false, false, true), null);
        }

        if (facts.From is not { } fromRead || facts.To is not { } toRead || !facts.Adjacent || facts.Crossed is not { } crossed)
        {
            return (null, $"play.move-step: {facts.ToText} is not an adjacent Location the map reads");
        }

        if (toRead.TerrainKey is not { } terrain)
        {
            var name = toRead.TerrainName ?? "the terrain";
            return (null, name is "Rooftop" or "Cellar"
                ? $"play.move-terrain: a {name} is not a Location units enter (rooftops by SSR only, B23.8; cellars, B23.41)"
                : $"play.move-terrain: {name} is not a reviewed entry (ruling R10.1)");
        }

        if (facts.FromLevel != 0 || facts.ToLevel != 0)
        {
            // B23.421, B23.422 (ruling R10.2): at an upper level, only into the same level of an ADJACENT hex of the same building.
            var connected = facts.FromLevel == facts.ToLevel && facts.FromLevel > 0 && ScenarioA1Definitions.IsBuildingTerrain(fromRead.TerrainKey) && ScenarioA1Definitions.IsBuildingTerrain(terrain)
                && fromRead.BaseLevel == toRead.BaseLevel && crossed.HexsideTerrain is null && crossed.Terrain is { } shared
                && ScenarioA1Definitions.OrdinaryBuildings.Contains(shared);
            // E1.51 (backlog pass 16, ruling R16.5): a building Location is Concealment Terrain, one MF more at night.
            return connected
                ? (new InfantryEntry(ScenarioA1ResultTables.EntryHalfMf[terrain] + smokeHalfMf() + (facts.Night ? 2 : 0), terrain, false, false, false, false), null)
                : (null, "play.move-upper-level: from an upper level a unit moves only into the same level of an ADJACENT hex of the same building (B23.421, B23.422)");
        }

        return GroundStep(crossed, terrain, toRead.BaseLevel - fromRead.BaseLevel, facts.Month, smokeHalfMf, weather);
    }

    /// <summary>
    /// The cost of an Infantry step into a ground-level Location across a hexside (rulings R10.1, R10.3): the step of <see cref="InfantryStep"/> from an
    /// adjacent hex <paramref name="rise"/> levels lower, or the entry from off board across the map edge's hexside, from the mirror-image hex at the same
    /// level (A2.51, A2.6; ruling R25.3). <paramref name="smokeHalfMf"/> and <paramref name="weather"/> are read where the planner read them, after
    /// the refusals.
    /// </summary>
    public static (InfantryEntry? Entry, string? Reason) GroundStep(CrossedHexsideFacts crossed, string terrain, int rise, int? month, Func<int> smokeHalfMf, InfantryWeatherRead weather)
    {
        ArgumentNullException.ThrowIfNull(crossed);
        ArgumentNullException.ThrowIfNull(smokeHalfMf);
        ArgumentNullException.ThrowIfNull(weather);
        if (crossed.Cliff)
        {
            return (null, "play.move-cliff: a cliff hexside is crossed only by Climbing, which is not built (B11)");
        }

        if (crossed.Slope)
        {
            return (null, "play.move-slope: Continuous Slopes are not reviewed (ruling R10.1)");
        }

        var wall = WallOn(crossed.HexsideTerrain);
        if (wall == "other")
        {
            return (null, $"play.move-hexside: {crossed.HexsideTerrain!} hexsides are not reviewed (ruling R10.1)");
        }

        var road = crossed.Road && !ScenarioA1Definitions.IsRubbleTerrain(terrain);
        int cost;
        var allMf = false;
        var minimumOnly = false;
        if (terrain == "marsh")
        {
            // B16.4 (ruling R10.1): marsh takes the whole MF allotment; from a lower elevation only as a Minimum Move.
            allMf = true;
            minimumOnly = rise > 0;
            cost = 0;
        }
        else if (road)
        {
            cost = 2;
        }
        else if (InfantryEntryHalfMf(terrain, month) is { } halfMf)
        {
            cost = halfMf;
        }
        else
        {
            return (null, "play.move-grain: grain's MF cost depends on the season, and the game names no scenario month (B15.6)");
        }

        // A4.133, B10.4, B10.51 (ruling R10.3): one level up doubles the cost; each intermediate level of an Abrupt Elevation Change costs two MF up
        // or one down, and the last level its own cost, doubled up.
        cost = rise switch
        {
            >= 1 => (4 * (rise - 1)) + (2 * cost),
            <= -2 => (2 * (-rise - 1)) + cost,
            _ => cost,
        };

        // B9.4 (ruling R10.1): one MF more across a wall or hedge, not across a road gap in it.
        if (wall is not null && !road)
        {
            cost += 2;
        }

        var smoke = smokeHalfMf();
        var (extra, roadRate) = allMf ? (0, road) : weather(terrain, road, rise);
        return (new InfantryEntry(cost + smoke + extra, terrain, roadRate && smoke == 0, allMf, minimumOnly, rise != 0), null);
    }
}
