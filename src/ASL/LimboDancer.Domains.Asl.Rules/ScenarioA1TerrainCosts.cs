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
    /// kept apart for pass 35 (the pass 32 design, section 12).
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
        // B.2 (p. 112; pass 35, task 35.5): the COT is the hex's cost plus that of the Artificial Terrain in it, so SMOKE's MF is doubled with the rest
        // one level up (2 x 2 = 4 MF, not 3), but not across an Abrupt Elevation Change, which B.2 excepts.
        var smoke = smokeHalfMf();
        cost = rise switch
        {
            1 => 2 * (cost + smoke),
            >= 2 => (4 * (rise - 1)) + (2 * cost) + smoke,
            <= -2 => (2 * (-rise - 1)) + cost + smoke,
            _ => cost + smoke,
        };

        // B9.4 (ruling R10.1): one MF more across a wall or hedge, not across a road gap in it.
        if (wall is not null && !road)
        {
            cost += 2;
        }

        var (extra, roadRate) = allMf ? (0, road) : weather(terrain, road, rise);
        return (new InfantryEntry(cost + extra, terrain, roadRate && smoke == 0, allMf, minimumOnly, rise != 0), null);
    }

    /// <summary>
    /// The first step of a stack waiting off board (A2.51, A2.6; ruling R25.3): into a ground-level hex of its entry edge across the edge's hexside, as if from
    /// the mirror-image hex beyond it at the same level: the hex's own cost, at the road rate across a road hexside, one MF more across a wall or hedge,
    /// or in Bypass along one or two hexsides starting at a vertex of the edge's hexside. <paramref name="entryReadable"/> says the map read the hex and the
    /// edge's hexside; <paramref name="bypassStep"/> and <paramref name="groundEntry"/> are read by the caller for the step the verdict takes.
    /// </summary>
    public static (InfantryEntry? Entry, string? Reason) EntryStep(bool bypassGiven, bool entryReadable, string toText,
        Func<(InfantryEntry? Entry, string? Reason)> bypassStep, Func<(InfantryEntry? Entry, string? Reason)> groundEntry)
    {
        ArgumentNullException.ThrowIfNull(bypassStep);
        ArgumentNullException.ThrowIfNull(groundEntry);
        if (bypassGiven)
        {
            return entryReadable ? bypassStep() : (null, $"play.entry-terrain: the entry cost of {toText} is not decided (ruling R20.5)");
        }

        return groundEntry();
    }

    /// <summary>
    /// The cost of crossing a map edge's hexside into a ground-level hex, or out of it into the mirror-image hex beyond (A2.51, A2.6; rulings R25.3, R25.5):
    /// the hex's own terrain at the same level, at the road rate across a road hexside (the A2.6 EX's 2Y1), one MF more across a wall or hedge.
    /// <paramref name="terrain"/> is null when the map does not read the hex or admits no terrain there; <paramref name="crossed"/> null when it does not read the hexside.
    /// </summary>
    public static (InfantryEntry? Entry, string? Reason) EntryGround(string? terrain, CrossedHexsideFacts? crossed, string atText, int? month, Func<int> smokeHalfMf, InfantryWeatherRead weather) =>
        terrain is not null && crossed is not null
            ? GroundStep(crossed, terrain, 0, month, smokeHalfMf, weather)
            : (null, $"play.entry-terrain: the cost of crossing the map edge at {atText} is not decided (ruling R25.3)");

    /// <summary>
    /// A moving stack's step (rulings R10.1 to R10.3, R10.7): an ordinary step, a step into a woods or building hex in Bypass along one or two of its
    /// hexsides, or, from Bypass, a step out through the far vertex or into the obstacle itself. <paramref name="enteredSide"/> is read for the hexside the
    /// stack entered its Bypass by, <paramref name="acrossIsTarget"/> for whether the hex across a hexside of the origin is the target's hex, and
    /// <paramref name="smokeAtFrom"/> for the SMOKE cost of the obstacle, each where the planner read it.
    /// </summary>
    public static MoveEntryVerdict MoveEntry(MoveEntryFacts facts, Func<int?> enteredSide, Func<int, bool> acrossIsTarget, Func<int> smokeAtFrom)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(enteredSide);
        ArgumentNullException.ThrowIfNull(acrossIsTarget);
        ArgumentNullException.ThrowIfNull(smokeAtFrom);

        // A4.3, A4.31, A4.32 (ruling R10.7): a stack in Bypass leaves through the far vertex of its last hexside, or pays the obstacle's cost to occupy it.
        if (facts.StackInBypass && facts.CurrentAtFrom && facts.MoversAllInCurrent)
        {
            var lane = facts.Lane;

            // Table player, pass 10: the Bypassing stack moves on together, so its Bypass is kept for every member.
            if (facts.CurrentMemberLeftBehind)
            {
                return new MoveEntryVerdict("play.move-bypass: a stack in Bypass leaves it or occupies the obstacle together; splitting it there is not built (A4.3; ruling R10.7)", null, false, false);
            }

            if (facts.ToIsFrom && facts.EnemyAtFrom)
            {
                return new MoveEntryVerdict("play.move-bypass: occupying an obstacle that holds enemy units is not built (A4.14, A12.15; ruling R10.7)", null, false, false);
            }

            if (facts.BypassGiven)
            {
                return new MoveEntryVerdict("play.move-bypass: continuing Bypass around the same hex is not built; leave it or occupy the obstacle (A4.31; ruling R10.7)", null, false, false);
            }

            if (facts.ToIsFrom)
            {
                return facts.ObstacleTerrainKey is { } obstacle && (obstacle == "woods" || ScenarioA1Definitions.IsBuildingTerrain(obstacle))
                    ? new MoveEntryVerdict(null, new InfantryEntry(ScenarioA1ResultTables.EntryHalfMf[obstacle] + smokeAtFrom(), obstacle, false, false, false, false), false, false)
                    : new MoveEntryVerdict("play.move-bypass: the Bypassed hex has no woods or building to occupy", null, false, false);
            }

            if (enteredSide() is not { } entered)
            {
                return new MoveEntryVerdict("play.move-bypass: the hexside the stack entered its Bypass by cannot be read", null, false, false);
            }

            var turn = (lane[0] - entered + 6) % 6;
            var far = (lane[^1] + turn) % 6;
            if (!new[] { lane[^1], far }.Any(side => acrossIsTarget(side) && facts.ToIsGround))
            {
                return new MoveEntryVerdict("play.move-bypass: from Bypass the stack leaves only into a hex at the far vertex of its last hexside, or occupies the obstacle (A4.31, A4.32)", null, false, false);
            }

            return new MoveEntryVerdict(null, null, true, false);
        }

        if (facts.StackInBypass)
        {
            return new MoveEntryVerdict("play.move-bypass: while the stack is in Bypass, only it moves, together, out of the hex or into the obstacle (A4.32; ruling R10.7)", null, false, false);
        }

        if (!facts.BypassGiven)
        {
            return new MoveEntryVerdict(null, null, true, false);
        }

        // A4.3, A4.31 (ruling R10.7): into a woods or building hex along one or two contiguous hexsides the obstacle does not touch, starting at a vertex
        // of the hexside crossed.
        if (facts.FromLevel != 0 || !facts.FromReadable)
        {
            return new MoveEntryVerdict("play.move-bypass: Bypass moves at ground level along one or two hexsides of an adjacent woods or building hex (A4.3, A4.31)", null, false, false);
        }

        if (facts.SideTowardFrom is null)
        {
            return new MoveEntryVerdict($"play.move-step: {facts.ToText} is not an adjacent Location the map reads", null, false, false);
        }

        return new MoveEntryVerdict(null, null, false, true);
    }

    /// <summary>
    /// A Bypass step into a woods or building hex (A4.3, A4.31; ruling R10.7) across its hexside <see cref="BypassStepFacts.Crossed"/>: from an adjacent hex at
    /// <see cref="BypassStepFacts.FromBaseLevel"/>, or from off board across the map edge (ruling R25.3), with the wall or hedge on the hexside crossed.
    /// <paramref name="smokeHalfMf"/> is read for the target where the planner read it. The Bypass hexsides are the caller's when the entry is given.
    /// </summary>
    public static (InfantryEntry? Entry, string? Reason) BypassStep(BypassStepFacts facts, Func<int> smokeHalfMf, InfantryWeatherRead? weather = null)
    {
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(smokeHalfMf);
        var bypass = facts.Bypass;
        if (bypass.Count is < 1 or > 2 || facts.ToLevel != 0 || !facts.TargetReadable
            || facts.TargetTerrainKey is not { } obstacleKey || !(obstacleKey == "woods" || ScenarioA1Definitions.IsBuildingTerrain(obstacleKey)))
        {
            return (null, "play.move-bypass: Bypass moves at ground level along one or two hexsides of an adjacent woods or building hex (A4.3, A4.31)");
        }

        var direction = (bypass[0].Side - facts.Crossed + 6) % 6;
        if (direction is not (1 or 5) || (bypass.Count == 2 && (bypass[1].Side - bypass[0].Side + 6) % 6 != direction))
        {
            return (null, "play.move-bypass: the Bypassed hexsides are contiguous and start at a vertex of the hexside the stack crosses (A4.31)");
        }

        var laneCost = 0;
        var laneTerrain = "open-ground";
        foreach (var bypassed in bypass)
        {
            var name = bypassed.Terrain;
            if (!bypassed.Read || name is null || name == "Woods" || ScenarioA1Definitions.OrdinaryBuildings.Contains(name) || WallOn(bypassed.HexsideTerrain) == "other"
                || (ScenarioA1Definitions.FireTerrain.GetValueOrDefault(name) ?? (bypassed.Road ? "open-ground" : null)) is not { } key || InfantryEntryHalfMf(key, facts.Month) is not { } cost)
            {
                return (null, $"play.move-bypass: the obstacle touches the {bypassed.SideText} hexside, or its other terrain is not reviewed, so it may not be Bypassed there (A4.3, A4.31)");
            }

            if (cost >= laneCost)
            {
                (laneCost, laneTerrain) = (cost, key);
            }
        }

        // Table player, pass 10: a hex with a wall or hedge is not Bypassed until the vertex LOS for fire at a Bypassing stack is built, nor one
        // holding friendly units, whose TEM a Bypassing stack would share in the fire at the Location.
        if (facts.TargetHexsideTerrains.Any(item => WallOn(item) is not null) || facts.FriendlyUnitsAtTarget)
        {
            return (null, $"play.move-bypass: {facts.ToText} has a wall or hedge, or holds friendly units; that Bypass is not built (A4.34; ruling R10.7)");
        }

        // A4.3: no Bypass of an obstacle holding an armed Known enemy unit (rubble is not an obstacle here).
        if (facts.ArmedKnownEnemyAtTarget)
        {
            return (null, $"play.move-bypass: {facts.ToText} holds an armed Known enemy unit, so its obstacle may not be Bypassed (A4.3)");
        }

        var rise = facts.TargetBaseLevel - facts.FromBaseLevel;
        if (Math.Abs(rise) > 1)
        {
            return (null, "play.move-bypass: a Bypass across an Abrupt Elevation Change is not reviewed (B10.51; ruling R10.7)");
        }

        if (facts.EntryWall == "other")
        {
            return (null, "play.move-hexside: that hexside terrain is not reviewed (ruling R10.1)");
        }

        // E3.9 (p. 231; pass 35, task 35.14): the weather's MF are added per hexside Bypassed, after the total; a level's own cost once.
        var perHexside = weather?.Invoke(laneTerrain, false, 0).HalfMf ?? 0;
        var extra = (weather?.Invoke(laneTerrain, false, rise).HalfMf ?? 0) + ((bypass.Count - 1) * perHexside);
        // B.2 (p. 112; the referees' review): SMOKE's MF is part of the COT doubled one level up, in Bypass as in an entry.
        var smoke = smokeHalfMf();
        var halfMf = (rise == 1 ? 2 * (laneCost + smoke) : laneCost + smoke) + (facts.EntryWall is not null ? 2 : 0) + extra;
        return (new InfantryEntry(halfMf, laneTerrain, false, false, false, rise != 0), null);
    }

    /// <summary>
    /// The wall or hedge TEM a target claims against a group's LOS (B9.3, B9.31, B9.33, B9.35; rulings R10.5, R10.6): from the wall or hedge on
    /// the hexside each firer's LOS crosses into the target hex, or at a vertex on either hexside there or the hexspine leading away; reduced
    /// by one for each full level a firer's height above the target hex exceeds the range; none against a firer that holds Wall Advantage
    /// over that hexside. Every firer Location must give the same TEM. Null TEM with no reason when none applies.
    /// </summary>
    public static (FireHexsideTem? Tem, string? Reason) HexsideTemAt(WallTemFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        if (!facts.TargetReadable || !facts.TargetHexsideTerrains.Any(side => WallOn(side) is not null))
        {
            return (null, null);
        }

        if (facts.TargetHexsideTerrains.Any(side => WallOn(side) == "other"))
        {
            return (null, "play.fire-hexside: hexside terrain other than walls and hedges at the target is not reviewed (ruling R10.1)");
        }

        FireHexsideTem? common = null;
        var first = true;
        foreach (var firer in facts.Firers)
        {
            FireHexsideTem? tem = null;

            // B9.35: only a target at the base level of its hex, where its walls are, claims them.
            if (facts.TargetLevel == 0 && !firer.IsTarget)
            {
                if (firer.EntrySides is not { } sides)
                {
                    return (null, "play.fire-hexside: the bearing of the LOS into the target hex cannot be read");
                }

                var crossed = new List<(string Wall, LosEntrySideFacts? Side)>();
                foreach (var side in sides)
                {
                    if (side.Wall is { } wall)
                    {
                        crossed.Add((wall, side));
                    }
                }

                // B9.2, B9.3: an LOS through a vertex is also affected by the wall or hedge on the hexspine leading away from it.
                if (sides.Count == 2 && firer.SpineWall is { } spineWall)
                {
                    if (spineWall == "other")
                    {
                        return (null, "play.fire-hexside: hexside terrain other than walls and hedges on the LOS is not reviewed (ruling R10.1)");
                    }

                    crossed.Add((spineWall, null));
                }

                foreach (var (wall, side) in crossed.OrderByDescending(item => ScenarioA1FireReference.HexsideTem[item.Wall]))
                {
                    // B9.6 (ruling R10.5): a Hillside wall or hedge is not reviewed.
                    if (side is { BeyondBaseLevel: { } beyondLevel } && beyondLevel != facts.TargetBaseLevel)
                    {
                        return (null, "play.fire-hillside-wall: a wall or hedge between hexes of different levels is a Hillside wall, which is not reviewed (B9.6)");
                    }

                    // B9.3 (referee, pass 10): through a road gap in the wall, its TEM applies only to a unit that is not moving.
                    if (side is { Road: true } && facts.MovingStackAtTarget)
                    {
                        continue;
                    }

                    // B9.31, B9.321 EX (ruling R10.6): no wall TEM against a firer that holds Wall Advantage over the shared hexside; B9.32 (referee,
                    // pass 10): only a ground-level unit at the wall's level holds it.
                    if (side is { BeyondIsFirer: true } && firer.Level == 0)
                    {
                        var (holder, reason) = firer.WallAdvantage();
                        if (reason is not null)
                        {
                            return (null, reason);
                        }

                        if (holder == facts.FiringSide)
                        {
                            continue;
                        }
                    }

                    // B9.33: a higher firer lowers the TEM by one for each full level its height above the target hex exceeds the range.
                    var height = firer.BaseLevel is { } firerBase ? firerBase + firer.Level - facts.TargetBaseLevel : 0;
                    var value = Math.Max(0, ScenarioA1FireReference.HexsideTem[wall] - Math.Max(0, height - firer.Range));
                    tem = value > 0 ? new FireHexsideTem(wall, value) : null;
                    break;
                }
            }

            // A.5, A7.52 (table player, pass 10): a DRM that helps the target applies to the whole group, so the highest wall TEM stands.
            if (first || (tem?.Tem ?? 0) > (common?.Tem ?? 0))
            {
                common = tem;
            }

            first = false;
        }

        return (common, null);
    }

    /// <summary>
    /// Which side holds Wall Advantage over the hexside two ADJACENT Locations share (B9.32, B9.321, B9.41; ruling R10.6): of the Good Order
    /// units there, the one that entered its Location first; units there since setup give it to the Scenario Defender when named. Null holder
    /// with no reason when neither Location holds a unit that may claim it.
    /// </summary>
    public static (string? Side, string? Reason) WallAdvantageHolder(WallAdvantageFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);
        static int InHexTem(WallAdvantageLocationFacts at) => at.TerrainKey is { } key ? ScenarioA1FireReference.Tem.GetValueOrDefault(key) : 0;

        // B9.31, B9.321 EX, B9.323 (referee, pass 10): a unit keeping a positive in-hex TEM does not hold WA; one with none must claim it.
        static WallAdvantageUnitFacts[] Claimants(WallAdvantageLocationFacts at) => at.Level != 0 || InHexTem(at) > 0 ? []
            : [.. at.Units.Where(unit => unit.Active && !unit.Dummy && !unit.Vehicle && !unit.Captured && !unit.Broken)];
        var first = Claimants(facts.One);
        var second = Claimants(facts.Two);
        if (first.Length == 0 || second.Length == 0)
        {
            return (first.Length > 0 ? first[0].Side : second.Length > 0 ? second[0].Side : null, null);
        }

        if (!facts.HistoryKnown)
        {
            return (null, "play.fire-wall-advantage: which side holds Wall Advantage cannot be read here (ruling R10.6)");
        }

        var oneAt = first.Min(unit => facts.ArrivalOf(unit.Id));
        var twoAt = second.Min(unit => facts.ArrivalOf(unit.Id));
        if (oneAt != twoAt)
        {
            return (oneAt < twoAt ? first[0].Side : second[0].Side, null);
        }

        return facts.ScenarioDefender is { } defender
            ? (defender, null)
            : (null, "play.fire-wall-advantage: both sides' units have held their Locations since setup and no Scenario Defender is named, so which holds Wall Advantage is not decided (B9.32; ruling R10.6)");
    }

    /// <summary>
    /// Whether a target at a higher elevation than every firer may claim Height Advantage (B10.31; ruling R10.4): not a mover under Defensive
    /// First Fire whose step came from a lower hex across the hexside a firer's LOS crosses into its hex.
    /// </summary>
    public static bool HeightAdvantageAt(HeightAdvantageFacts facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        // A.5, A7.52 (referee, pass 10): a DRM that helps the target applies to the whole group when any firer is lower.
        var height = facts.TargetBaseLevel + facts.TargetLevel;
        var lower = facts.FirerLevels.Select((level, index) => (Level: level, Index: index)).Where(item => item.Level < height).ToArray();
        if (lower.Length == 0)
        {
            return false;
        }

        if (facts.MovementPhase && facts.MovingStackAtTarget && facts.LeftBaseLevel is { } leftLevel && leftLevel < facts.TargetBaseLevel && facts.ClimbedSide is { } climbed)
        {
            // A Snap Shot is traced to the climbed hexside itself (referee, pass 10).
            return !facts.SnapShot && lower.Any(item => facts.EntrySidesOf(item.Index) is { } sides && !sides.Contains(climbed));
        }

        return true;
    }
}
