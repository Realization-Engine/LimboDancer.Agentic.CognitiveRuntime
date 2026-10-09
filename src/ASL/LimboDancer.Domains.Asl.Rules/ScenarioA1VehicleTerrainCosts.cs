namespace LimboDancer.Domains.Asl.Rules;

/// <summary>
/// A vehicle's entry costs over the terrain (backlog pass 11, rulings R11.1 to R11.9; moved from Play in pass 32.e): the Terrain Chart's MP costs by
/// movement type, ALL and half-allotment entries, walls and hedges, levels, Bog DRM, Reverse, VBM, VCA changes, and the D2.6 TK DR of 5.
/// </summary>
public static class ScenarioA1VehicleTerrainCosts
{
    public static bool Tracked(string? movementType) => movementType is "fully-tracked" or "half-tracked";

    /// <summary>D2.21 (ruling R11.1): Reverse costs four times the entry for a tracked vehicle, three times for a truck.</summary>
    public static int ReverseMultiplier(string? movementType) => movementType == "truck" ? 3 : 4;

    /// <summary>
    /// The Bog DRM of a vehicle (D8.21; ruling R11.9) that belong to it, whatever the terrain: +1 Normal Ground Pressure (none printed), +2 High, 0 Low;
    /// +1 towing a Gun; +1 not fully-tracked; +1 truck-type MP.
    /// </summary>
    public static List<(string Cause, int Drm)> VehicleBogDrm(string? groundPressure, bool towing, string? movementType)
    {
        var drm = new List<(string, int)>();
        switch (groundPressure)
        {
            case "low":
                break;
            case "high":
                drm.Add(("high-ground-pressure", 2));
                break;
            default:
                drm.Add(("normal-ground-pressure", 1));
                break;
        }

        if (towing)
        {
            drm.Add(("towing", 1));
        }

        if (movementType != "fully-tracked")
        {
            drm.Add(("not-fully-tracked", 1));
        }

        if (movementType == "truck")
        {
            drm.Add(("truck-type-mp", 1));
        }

        return drm;
    }

    /// <summary>
    /// The cost of a vehicle's outright entry of a Location over the hexside it crosses (rulings R11.6 to R11.9), from the facts Play reads: the
    /// wreck and vehicle penalty, SMOKE, and the weather cost cross as delegates read where the old body read them.
    /// </summary>
    public static (VehicleTerrainEntry? Entry, string? Reason) EntryCost(string? type, string to, int toLevel, bool cliffOrSlope, string? terrainKey,
        string? terrainName, string? wall, string? hexsideName, bool roadHexside, int rise, int? month, bool mud, bool pavedRoad, bool plowedRule,
        int printedHalfMp, bool buttonedAfv, bool groundSnow, bool deepSnow, Func<bool, int> wreckHalfMp, Func<bool> smoke, bool towing,
        Func<string, bool, bool, bool, int> weatherHalfMp, bool reverse, bool allMp, List<(string Cause, int Drm)> own, bool besideMarsh = false)
    {
        if (type is null)
        {
            return (null, $"{to} is not an adjacent Location the map reads");
        }

        if (toLevel != 0)
        {
            return (null, "vehicles stay at ground level (B23.4)");
        }

        if (cliffOrSlope)
        {
            return (null, "cliffs and Continuous Slopes are not reviewed for vehicles (ruling R11.7)");
        }

        if (terrainKey is not { } terrain)
        {
            return (null, $"{terrainName ?? "that terrain"} is not a reviewed entry for vehicles (ruling R11.7)");
        }

        if (wall == "other")
        {
            return (null, $"{hexsideName} hexsides are not reviewed (ruling R11.7)");
        }

        var road = roadHexside && !ScenarioA1Definitions.IsRubbleTerrain(terrain);

        // C10.1 (p. 180; pass 35, task 35.12): no vehicle tows a Gun over a wall or hedge, or into rubble. A road crosses a wall or hedge by its gap.
        if (towing && wall is not null && !road)
        {
            return (null, $"a vehicle towing a Gun may not cross a {wall} (C10.1)");
        }

        if (towing && ScenarioA1Definitions.IsRubbleTerrain(terrain))
        {
            return (null, "a vehicle towing a Gun may not enter rubble (C10.1)");
        }

        // B10.52 (ruling R11.7): on a board whose elevations are hills, an Abrupt Elevation Change crosses two hillside Crest Lines, a Double-Crest
        // hexside, which a vehicle crosses only by road.
        if (Math.Abs(rise) >= 2 && !road)
        {
            return (null, "an Abrupt Elevation Change between hill levels is a Double-Crest hexside, crossed by a vehicle only along a road (B10.52; ruling R11.7)");
        }

        // B15.6: grain is Open Ground outside its season, April to September for MP.
        if (terrain == "grain")
        {
            if (month is not { } scenarioMonth)
            {
                return (null, "grain's MP cost depends on the season, and the game names no scenario month (B15.6)");
            }

            terrain = scenarioMonth is >= 4 and <= 9 ? "grain" : "open-ground";
        }

        // E3.6 (backlog pass 16, ruling R16.12; referee, pass 16): in Mud a vehicle using an unpaved road pays the Open Ground COT, whatever the hex holds.
        var paved = road && pavedRoad;
        var plowed = road && plowedRule;
        if (road && mud && !paved)
        {
            road = false;
            terrain = "open-ground";
        }

        // B13.41, B13.42, B24.4: ALL and half the allotment are of the printed allotment (D1.1), whatever ESB added.
        var allotment = printedHalfMp;
        var bog = new List<(string Cause, int Drm)>();
        var all = false;
        int cost;
        var woods = terrain == "woods";
        if (road)
        {
            // E3.724, E3.7331 (referee, pass 16): in Ground or Deep Snow a road entry costs at least one MP.
            cost = buttonedAfv || groundSnow || deepSnow ? 2 : 1;
        }
        else if (woods)
        {
            // B13.41, B13.42: ALL with a Bog DR, or for a fully-tracked vehicle half its allotment with a Bog DR at +3.
            if (type == "fully-tracked" && !allMp)
            {
                cost = allotment / 2;
                bog.Add(("woods-at-half-allotment", 3));
            }
            else
            {
                all = true;
                cost = allotment;
            }

            bog.Add(("woods", 0));
            if (rise > 0)
            {
                bog.Add(("gaining-elevation-into-woods", 1));
            }
        }
        else if (ScenarioA1Definitions.IsRubbleTerrain(terrain))
        {
            // B24.4: a fully-tracked vehicle only, at half its allotment, with a Bog DR at +3.
            if (type != "fully-tracked")
            {
                return (null, "only a fully-tracked vehicle enters rubble (B24.4)");
            }

            cost = allotment / 2;
            bog.Add(("rubble-at-half-allotment", 3));
        }
        else if (ScenarioA1Definitions.IsBuildingTerrain(terrain))
        {
            return (null, "a vehicle enters a building hex only by VBM here; the B23.41 entry of a fully-tracked AFV is not built (ruling R11.7)");
        }
        else if (!ScenarioA1ResultTables.VehicleTerrainHalfMp.TryGetValue((type, terrain), out cost))
        {
            return (null, $"{terrain} is not allowed to a {type} vehicle (Terrain Chart; ruling R11.7)");
        }

        // Terrain Chart note H, B10.4: 4 MP more for the level climbed, 2 by a road hexside; B10.51 (referee, pass 11): across an Abrupt Elevation Change
        // each intermediate level climbed costs 4 MP more and each descended 2 MP.
        if (rise > 0 && !all)
        {
            cost += (road ? 4 : 8) + ((rise - 1) * 8);
        }
        else if (rise <= -2 && !all)
        {
            cost += (-rise - 1) * 4;
        }

        // B9.4: a wall or hedge hexside, not crossed by the road through a gap in it: fully-tracked 1 + COT; a halftrack a hedge only, 2 + COT with a
        // Bog DR in the hex it leaves; a truck neither.
        var bogInLeft = false;
        if (wall is not null && !road)
        {
            switch (type, wall)
            {
                case ("fully-tracked", _):
                    cost += all ? 0 : 2;
                    break;
                case ("half-tracked", "hedge"):
                    cost += all ? 0 : 4;
                    bogInLeft = true;
                    break;
                default:
                    return (null, $"a {type} vehicle may not cross a {wall} (Terrain Chart; B9.4)");
            }
        }

        // D2.14, B13.41, R6.4: one more MP per wreck or vehicle there, two by a road hexside at the road rate, doubled in woods off the road; SMOKE
        // and towing add one MP each (A24.7, C10.1).
        var penalty = wreckHalfMp(road);
        if (woods && !road)
        {
            penalty += wreckHalfMp(false) - (smoke() ? 2 : 0);
        }

        var towingHalfMp = towing ? 2 : 0;
        cost = all ? cost : cost + penalty + towingHalfMp + weatherHalfMp(terrain, road, paved, plowed);
        if (reverse && !all)
        {
            cost *= ReverseMultiplier(type);
        }

        // B16.43 (p. 130; pass 35, task 35.7): a ground level or level -1 hex adjacent to a marsh is a Bog hex for a vehicle entering by a non-road hexside.
        if (besideMarsh && !road)
        {
            bog.Add(("beside-marsh", 0));
        }

        int? bogDrm = null;
        var causes = new List<string>();

        // D8.21 notes 2 and 3 (backlog pass 16, rulings R16.12, R16.13; referee, pass 16): +1 on mud or snow-covered ground, +1 more with Deep Snow, not in
        // a building (a Bog DR is never made by road).
        if (terrain is not ("wooden-building" or "stone-building") && (mud || groundSnow || deepSnow))
        {
            own.Add((mud ? "mud" : "snow", 1));
            if (deepSnow)
            {
                own.Add(("deep-snow", 1));
            }
        }
        if (bog.Count > 0 && !road)
        {
            var total = bog.Concat(own).ToArray();
            bogDrm = total.Sum(item => item.Drm);
            causes.AddRange(total.Select(item => item.Cause));
        }

        // B9.4 (referee, pass 11): a halftrack crossing a hedge into a Bog hex takes the hedge's Bog DR in the hex it leaves, then the hex's own.
        int? hedgeDrm = bogInLeft ? own.Sum(item => item.Drm) : null;
        if (bogInLeft)
        {
            causes.Insert(0, "hedge");
        }

        return (new VehicleTerrainEntry(cost, all, bogDrm, causes, terrain, road, hedgeDrm), null);
    }

    /// <summary>C10.1 (p. 180; pass 35, task 35.12): a vehicle towing a Gun may not use Bypass Movement. The Narrow Streets exception (B31.124) is not built.</summary>
    public static string? TowingBypassBar(bool towing) => towing ? "a vehicle towing a Gun may not use Bypass Movement (C10.1)" : null;

    /// <summary>
    /// A VBM step's cost in half MP (D2.3, D2.31; ruling R11.2): twice the Open Ground cost with a level climbed and SMOKE, the wreck and vehicle penalty
    /// when the obstacle is a new hex, and the Reverse multiplier.
    /// </summary>
    public static int BypassHalfMp(int open, int rise, bool smoke, bool newHex, Func<int> wreckHalfMp, bool reverse, string? movementType)
    {
        var smokeHalfMp = smoke ? 2 : 0;
        var cost = 2 * (open + (rise > 0 ? rise * 8 : 0) + smokeHalfMp);
        if (newHex)
        {
            cost += wreckHalfMp() - smokeHalfMp;
        }

        if (reverse)
        {
            cost *= ReverseMultiplier(movementType);
        }

        return cost;
    }

    /// <summary>
    /// A VCA change in a woods, building, or rubble hex off its road (D2.11, B13.41; ruling R11.8): two MP, with a Bog DR in woods and rubble; one MP
    /// elsewhere.
    /// </summary>
    public static (int HalfMp, int? BogDrm, IReadOnlyList<string> Causes) TurnCost(string terrain, Func<List<(string Cause, int Drm)>> bogDrm)
    {
        var obstacle = terrain == "woods" || ScenarioA1Definitions.IsBuildingTerrain(terrain) || ScenarioA1Definitions.IsRubbleTerrain(terrain);
        if (!obstacle)
        {
            return (2, null, []);
        }

        var drm = bogDrm();
        return terrain is "woods" || ScenarioA1Definitions.IsRubbleTerrain(terrain)
            ? (4, drm.Sum(item => item.Drm), [terrain, .. drm.Select(item => item.Cause)])
            : (4, null, []);
    }

    /// <summary>The Target Facing of an angle off a VCA or TCA (D3.2; referee, pass 11).</summary>
    public static string Facing(double off) => off <= 60 + 1e-6 ? "front" : off <= 120 + 1e-6 ? "side" : "rear";

    /// <summary>
    /// D2.6 (ruling R11.6): whether AP at range 0 with an Original TK DR of 5 destroys or Shocks the AFV: the Final TK# exceeds 5 against the hull AF
    /// (+1 at the rear), or equals 5 against a turret.
    /// </summary>
    public static bool KillsWithFive(int basic, int caseD, int? hullAf, string hullFacing, int? turretAf, string turretFacing) =>
        (hullAf is { } h && basic + caseD + (hullFacing == "rear" ? 1 : 0) - h > 5)
        || (turretAf is { } t && basic + caseD + (turretFacing == "rear" ? 1 : 0) - t >= 5);

    /// <summary>The extra MP a vehicle pays to enter a hex for its wrecks and vehicles, doubled by a road entry, and a Blaze's smoke (D2.14, B25.141).</summary>
    public static int WreckEntryHalfMp(int wrecks, int vehicles, bool road, bool smoke) => ((wrecks + vehicles) * (road ? 2 : 1) + (smoke ? 1 : 0)) * 2;

    /// <summary>The extra half MF Infantry pay to enter a SMOKE Location: a burning wreck's or a grenade's (B25.141, A24.7; ruling R9.6).</summary>
    public static int BlazeEntryHalfMf(bool smoke) => smoke ? 2 : 0;
}
