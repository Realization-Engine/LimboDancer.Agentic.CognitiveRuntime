using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Infantry movement and fire over the terrain of backlog pass 10 (rulings R10.1 to R10.7): the cost of one step between Locations, levels in
/// buildings and on hills, walls and hedges on entry and as TEM at the target, Wall Advantage, and Height Advantage.
/// </summary>
public sealed partial class GamePlanner
{
    // One Infantry step's cost and what it enters: the InfantryEntry record moved to Rules (pass 32.b).

    /// <summary>The angle between two bearings in degrees, 0 to 180.</summary>
    private static double AngleBetween(double one, double two) => ScenarioA1Geometry.AngleBetween(one, two);

    private static bool IsBuildingTerrain(string? key) => ScenarioA1Definitions.IsBuildingTerrain(key);

    private static bool IsRubbleTerrain(string? key) => ScenarioA1Definitions.IsRubbleTerrain(key);

    /// <summary>The wall or hedge on a hexside, as <c>wall</c> or <c>hedge</c>; null for none; <c>other</c> for hexside terrain the pass does not review.</summary>
    private static string? WallOn(HexsideFacts? side) => ScenarioA1TerrainCosts.WallOn(side?.HexsideTerrain?.Name);

    /// <summary>The hexside an Infantry step crosses, as Rules reads it (pass 32.b).</summary>
    private static CrossedHexsideFacts CrossedFacts(HexsideFacts crossed) =>
        new(crossed.Cliff, crossed.Slope, crossed.HexsideTerrain?.Name, crossed.Terrain?.IsRoad == true, crossed.Terrain?.Name);

    /// <summary>One Location of an Infantry step as Rules reads it (pass 32.b); null when the map cannot read it.</summary>
    private static StepLocationFacts? StepLocation(LocationRead? read) =>
        read is null ? null : new(TerrainKey(read), (read.Level.Terrain ?? read.Hex.Center.Terrain)?.Name, read.Hex.BaseLevel, read.Hex.Stairway);

    /// <summary>
    /// The cost of an Infantry step from one Location to another (rulings R10.1 to R10.3, R10.5): up or down one level in a stairwell hex
    /// (B23.4, 1 MF); into an ADJACENT hex of the same building at an upper level (B23.421, 2 MF); or into an adjacent hex at ground level at
    /// its terrain's cost (A4.13), a road hexside's rate (A4.132), doubled one level up (A4.133, B10.4), with Abrupt Elevation Changes
    /// (B10.51), one MF more across a wall or hedge (B9.4), and one more into SMOKE or a burning wreck (A24.7, B25.141). Null with the reason
    /// when the step is not allowed or not reviewed.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason) InfantryStep(GameState state, BoardLocation from, BoardLocation to)
    {
        // Pass 32.b: the map is read here as the step read it (both Locations within one hex, the step between two), and Rules decides.
        var hexsides = from.Side is not null || to.Side is not null;
        var sameHex = from.Board == to.Board && from.Hex == to.Hex;
        var (fromRead, toRead, adjacent, crossed) = hexsides ? (null, null, false, null)
            : sameHex ? (ReadLocation(state, from), ReadLocation(state, to), false, null) : Step(state, from, to);
        var facts = new InfantryStepFacts(from.Side is not null, to.Side is not null, sameHex, from.Level, to.Level, from.ToString(), to.ToString(), from.Hex.ToString(),
            StepLocation(fromRead), StepLocation(toRead), adjacent, crossed is null ? null : CrossedFacts(crossed), state.Night, state.ScenarioMonth);
        return ScenarioA1TerrainCosts.InfantryStep(facts, () => BlazeEntryHalfMf(state, to), (terrain, road, rise) => InfantryWeatherHalfMf(state, crossed!, terrain, road, rise));
    }

    /// <summary>
    /// The cost of an Infantry step into a ground-level Location across a hexside (rulings R10.1, R10.3): the step of <see cref="InfantryStep"/> from an
    /// adjacent hex <paramref name="rise"/> levels lower, or the entry from off board across the map edge's hexside, from the mirror-image hex at the same
    /// level (A2.51, A2.6; ruling R25.3).
    /// </summary>
    private static (InfantryEntry? Entry, string? Reason) GroundStep(GameState state, BoardLocation to, Maps.Derivation.HexsideFacts crossed, string terrain, int rise) =>
        ScenarioA1TerrainCosts.GroundStep(CrossedFacts(crossed), terrain, rise, state.ScenarioMonth, () => BlazeEntryHalfMf(state, to),
            (entered, road, climbed) => InfantryWeatherHalfMf(state, crossed, entered, road, climbed));

    /// <summary>
    /// The facts of a Location's hexside named in the map's frame (table player, pass 10): on a reversed board of a placed map the board's own
    /// hexside is the opposite one, as the composed map read turns it.
    /// </summary>
    private HexsideFacts? HexsideAt(GameState state, BoardLocation at, HexsideDirection mapSide)
    {
        var side = state.Map.IsPlaced && state.Map.Board(at.Board) is { Slot.Reversed: true } ? (HexsideDirection)(((int)mapSide + 3) % 6) : mapSide;
        return ReadLocation(state, at)?.Hex.Hexsides.FirstOrDefault(item => item.Side == side);
    }

    /// <summary>The Location across a hexside of a Location's hex, at ground level; null off the map.</summary>
    private BoardLocation? Across(GameState state, BoardLocation at, HexsideDirection side)
    {
        if (Composed(state) is { } composed)
        {
            return composed.Neighbor(at.Board, at.Hex, side) is { } other ? new BoardLocation(other.Board, other.Hex, 0) : null;
        }

        return state.Map.Board(at.Board) is { } placed && boards.TryGetBoard(at.Board, placed.Version).Board is { } handle
            && handle.Neighbor(at.Hex, side) is { } hex ? new BoardLocation(at.Board, hex, 0) : null;
    }

    /// <summary>The hexside of a Location's hex facing an adjacent hex; null when they are not adjacent.</summary>
    private HexsideDirection? SideToward(GameState state, BoardLocation at, BoardLocation other) =>
        Enum.GetValues<HexsideDirection>().Cast<HexsideDirection?>().FirstOrDefault(side => Across(state, at, side!.Value) is { } across
            && across.Board == other.Board && across.Hex == other.Hex);

    /// <summary>
    /// The hexsides of a target hex that an LOS from a firer's hex crosses into it: one, or the two that meet at the vertex it passes through,
    /// read from the bearing of each neighbor so a reversed board or a seam keeps one frame. Null when the bearings cannot be read.
    /// </summary>
    private IReadOnlyList<HexsideDirection>? LosEntrySides(GameState state, BoardLocation target, BoardLocation firer)
    {
        if (Bearing(state, target, firer) is not { } toward)
        {
            return null;
        }

        var sides = new List<(HexsideDirection Side, double Off)>();
        foreach (var side in Enum.GetValues<HexsideDirection>())
        {
            if (Across(state, target, side) is { } neighbor && Bearing(state, target, neighbor) is { } bearing)
            {
                sides.Add((side, AngleBetween(toward, bearing)));
            }
        }

        var within = sides.Where(item => item.Off <= 30 + 1e-6).OrderBy(item => item.Off).ToArray();
        return within.Length == 0 ? null
            : within[0].Off < 30 - 1e-6 ? [within[0].Side]
            : [.. within.Where(item => Math.Abs(item.Off - 30) <= 1e-6).Select(item => item.Side)];
    }

    /// <summary>
    /// The wall or hedge TEM a target claims against a group's LOS (B9.3, B9.31, B9.33, B9.35; rulings R10.5, R10.6): from the wall or hedge on
    /// the hexside each firer's LOS crosses into the target hex, or at a vertex on either hexside there or the hexspine leading away; reduced
    /// by one for each full level a firer's height above the target hex exceeds the range; none against a firer that holds Wall Advantage
    /// over that hexside. Every firer Location must give the same TEM. Null TEM with no reason when none applies.
    /// </summary>
    private (FireHexsideTem? Tem, string? Reason) HexsideTemAt(GameState state, BoardLocation target, IReadOnlyList<(BoardLocation From, int Range)> firers,
        string firingSide, IReadOnlyList<GameEvent>? history)
    {
        if (ReadLocation(state, target) is not { } targetRead || !targetRead.Hex.Hexsides.Any(side => WallOn(side) is not null))
        {
            return (null, null);
        }

        if (targetRead.Hex.Hexsides.Any(side => WallOn(side) == "other"))
        {
            return (null, "play.fire-hexside: hexside terrain other than walls and hedges at the target is not reviewed (ruling R10.1)");
        }

        FireHexsideTem? common = null;
        var first = true;
        foreach (var (from, range) in firers)
        {
            FireHexsideTem? tem = null;

            // B9.35: only a target at the base level of its hex, where its walls are, claims them.
            if (target.Level == 0 && from != target)
            {
                if (LosEntrySides(state, target, from) is not { } sides)
                {
                    return (null, "play.fire-hexside: the bearing of the LOS into the target hex cannot be read");
                }

                var crossed = new List<(string Wall, HexsideDirection? Side)>();
                foreach (var side in sides)
                {
                    if (WallOn(HexsideAt(state, target, side)) is { } wall)
                    {
                        crossed.Add((wall, side));
                    }
                }

                // B9.2, B9.3: an LOS through a vertex is also affected by the wall or hedge on the hexspine leading away from it.
                if (sides.Count == 2 && Across(state, target, sides[0]) is { } one && Across(state, target, sides[1]) is { } two
                    && Step(state, one, two).Crossed is { } spine && WallOn(spine) is { } spineWall)
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
                    if (side is { } hexside && Across(state, target, hexside) is { } beyond && ReadLocation(state, beyond) is { } beyondRead
                        && beyondRead.Hex.BaseLevel != targetRead.Hex.BaseLevel)
                    {
                        return (null, "play.fire-hillside-wall: a wall or hedge between hexes of different levels is a Hillside wall, which is not reviewed (B9.6)");
                    }

                    // B9.3 (referee, pass 10): through a road gap in the wall, its TEM applies only to a unit that is not moving.
                    if (side is { } gap && HexsideAt(state, target, gap)?.Terrain?.IsRoad == true && state.Phase == "mph"
                        && state.Movement?.Location == target)
                    {
                        continue;
                    }

                    // B9.31, B9.321 EX (ruling R10.6): no wall TEM against a firer that holds Wall Advantage over the shared hexside; B9.32 (referee,
                    // pass 10): only a ground-level unit at the wall's level holds it.
                    if (side is { } shared && from.Level == 0 && Across(state, target, shared) is { } across && across.Board == from.Board && across.Hex == from.Hex)
                    {
                        var (holder, reason) = WallAdvantageHolder(state, target, from, history);
                        if (reason is not null)
                        {
                            return (null, reason);
                        }

                        if (holder == firingSide)
                        {
                            continue;
                        }
                    }

                    // B9.33: a higher firer lowers the TEM by one for each full level its height above the target hex exceeds the range.
                    var height = ReadLocation(state, from) is { } firerRead ? firerRead.Hex.BaseLevel + from.Level - targetRead.Hex.BaseLevel : 0;
                    var value = Math.Max(0, ScenarioA1FireReference.HexsideTem[wall] - Math.Max(0, height - range));
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
    private (string? Side, string? Reason) WallAdvantageHolder(GameState state, BoardLocation one, BoardLocation two, IReadOnlyList<GameEvent>? history)
    {
        int InHexTem(BoardLocation at) => ReadLocation(state, at) is { } read && TerrainKey(read) is { } key ? ScenarioA1FireReference.Tem.GetValueOrDefault(key) : 0;

        // B9.31, B9.321 EX, B9.323 (referee, pass 10): a unit keeping a positive in-hex TEM does not hold WA; one with none must claim it.
        UnitInstance[] Claimants(BoardLocation at) => at.Level != 0 || InHexTem(at) > 0 ? []
            : [.. state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active
                && unit.Kind != UnitKinds.Dummy && !LiveFire.IsVehicle(unit) && !Is(unit, Conditions.Captured) && !Is(unit, Conditions.Broken))];
        var first = Claimants(one);
        var second = Claimants(two);
        if (first.Length == 0 || second.Length == 0)
        {
            return (first.Length > 0 ? first[0].Side : second.Length > 0 ? second[0].Side : null, null);
        }

        if (history is null)
        {
            return (null, "play.fire-wall-advantage: which side holds Wall Advantage cannot be read here (ruling R10.6)");
        }

        var arrivals = Arrivals(history);
        var oneAt = first.Min(unit => arrivals.GetValueOrDefault(unit.Id));
        var twoAt = second.Min(unit => arrivals.GetValueOrDefault(unit.Id));
        if (oneAt != twoAt)
        {
            return (oneAt < twoAt ? first[0].Side : second[0].Side, null);
        }

        return state.ScenarioDefender is { } defender
            ? (defender, null)
            : (null, "play.fire-wall-advantage: both sides' units have held their Locations since setup and no Scenario Defender is named, so which holds Wall Advantage is not decided (B9.32; ruling R10.6)");
    }

    /// <summary>
    /// The revision at which each unit last entered its Location: setup is 0, and a movement step, advance, or entry that changed its Location
    /// sets it; a unit produced by lineage keeps its predecessors' (ruling R10.6).
    /// </summary>
    private static Dictionary<string, long> Arrivals(IReadOnlyList<GameEvent> history)
    {
        var at = new Dictionary<string, BoardLocation>(StringComparer.Ordinal);
        var arrived = new Dictionary<string, long>(StringComparer.Ordinal);
        void Place(string id, BoardLocation location, long revision)
        {
            if (!at.TryGetValue(id, out var previous) || previous != location)
            {
                arrived[id] = at.ContainsKey(id) ? revision : 0;
                at[id] = location;
            }
        }

        foreach (var item in history)
        {
            switch (item.Payload)
            {
                case InstanceCreated { Instance.Position: MapPosition created } placed:
                    at[placed.Instance.Id] = created.Location;
                    arrived[placed.Instance.Id] = 0;
                    break;
                case MovementStepped moving:
                    foreach (var id in moving.Movers)
                    {
                        Place(id, moving.To, item.Revision);
                    }

                    break;
                case AdvanceMoved advanced:
                    foreach (var id in advanced.Units)
                    {
                        Place(id, advanced.To, item.Revision);
                    }

                    break;
                case InstanceMoved { Position: MapPosition moved } entered:
                    Place(entered.Id, moved.Location, item.Revision);
                    break;
                case LineageRecorded lineage:
                    var kept = lineage.Consumed.Select(id => arrived.GetValueOrDefault(id)).DefaultIfEmpty(0).Min();
                    foreach (var produced in lineage.Produced)
                    {
                        if (produced.Position is MapPosition position)
                        {
                            at[produced.Id] = position.Location;
                            arrived[produced.Id] = kept;
                        }
                    }

                    break;
            }
        }

        return arrived;
    }

    /// <summary>
    /// Whether a target at a higher elevation than every firer may claim Height Advantage (B10.31; ruling R10.4): not a mover under Defensive
    /// First Fire whose step came from a lower hex across the hexside a firer's LOS crosses into its hex.
    /// </summary>
    private bool HeightAdvantageAt(GameState state, BoardLocation target, LocationRead targetRead, IReadOnlyList<(BoardLocation From, int Level)> firers, bool snapShot)
    {
        // A.5, A7.52 (referee, pass 10): a DRM that helps the target applies to the whole group when any firer is lower.
        var height = targetRead.Hex.BaseLevel + target.Level;
        var lower = firers.Where(item => item.Level < height).ToArray();
        if (lower.Length == 0)
        {
            return false;
        }

        if (state.Phase == "mph" && state.Movement is { From: { } left } movement && movement.Location == target
            && ReadLocation(state, left) is { } leftRead && leftRead.Hex.BaseLevel < targetRead.Hex.BaseLevel && SideToward(state, target, left) is { } climbed)
        {
            // A Snap Shot is traced to the climbed hexside itself (referee, pass 10).
            return !snapShot && lower.Any(item => LosEntrySides(state, target, item.From) is { } sides && !sides.Contains(climbed));
        }

        return true;
    }

    /// <summary>
    /// A moving stack's step (rulings R10.1 to R10.3, R10.7): an ordinary step, a step into a woods or building hex in Bypass along one or two of its
    /// hexsides, or, from Bypass, a step out through the far vertex or into the obstacle itself. Returns the step's cost, the Bypass hexsides of a
    /// Bypass step, and whether the stack occupies the obstacle it Bypassed; or the reason it is refused.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason, IReadOnlyList<HexsideDirection>? Bypass, bool Occupy) MoveEntry(GameState state, UnitInstance[] movers,
        BoardLocation from, BoardLocation to, MovementState? current, List<HexsideDirection>? bypass)
    {
        // A4.3, A4.31, A4.32 (ruling R10.7): a stack in Bypass leaves through the far vertex of its last hexside, or pays the obstacle's cost to occupy it.
        if (current is { Bypass: { Count: > 0 } lane } && current.Location == from && movers.All(unit => current.Movers.Contains(unit.Id, StringComparer.Ordinal)))
        {
            // Table player, pass 10: the Bypassing stack moves on together, so its Bypass is kept for every member.
            if (current.Movers.Any(id => current.Members.Contains(id, StringComparer.Ordinal) && !movers.Any(unit => unit.Id == id)))
            {
                return (null, "play.move-bypass: a stack in Bypass leaves it or occupies the obstacle together; splitting it there is not built (A4.3; ruling R10.7)", null, false);
            }

            if (to == from && state.At(from).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != movers[0].Side && !Is(unit, Conditions.Captured)))
            {
                return (null, "play.move-bypass: occupying an obstacle that holds enemy units is not built (A4.14, A12.15; ruling R10.7)", null, false);
            }

            if (bypass is not null)
            {
                return (null, "play.move-bypass: continuing Bypass around the same hex is not built; leave it or occupy the obstacle (A4.31; ruling R10.7)", null, false);
            }

            if (to == from)
            {
                return ReadLocation(state, from) is { } obstacleRead && TerrainKey(obstacleRead) is { } obstacle && (obstacle == "woods" || IsBuildingTerrain(obstacle))
                    ? (new InfantryEntry(EntryHalfMf[obstacle] + BlazeEntryHalfMf(state, from), obstacle, false, false, false, false), null, null, true)
                    : (null, "play.move-bypass: the Bypassed hex has no woods or building to occupy", null, false);
            }

            if (BypassEntered(state, from, current.From, lane) is not { } entered)
            {
                return (null, "play.move-bypass: the hexside the stack entered its Bypass by cannot be read", null, false);
            }

            var turn = ((int)lane[0] - (int)entered + 6) % 6;
            var far = (HexsideDirection)(((int)lane[^1] + turn) % 6);
            if (!new[] { lane[^1], far }.Select(side => Across(state, from, side)).Any(exit => exit is { } open && open.Board == to.Board && open.Hex == to.Hex && to.Level == 0))
            {
                return (null, "play.move-bypass: from Bypass the stack leaves only into a hex at the far vertex of its last hexside, or occupies the obstacle (A4.31, A4.32)", null, false);
            }

            var (exit, exitReason) = InfantryStep(state, from, to);
            return (exit, exitReason, null, false);
        }

        if (current is { Bypass.Count: > 0 })
        {
            return (null, "play.move-bypass: while the stack is in Bypass, only it moves, together, out of the hex or into the obstacle (A4.32; ruling R10.7)", null, false);
        }

        if (bypass is null)
        {
            var (entry, reason) = InfantryStep(state, from, to);
            return (entry, reason, null, false);
        }

        // A4.3, A4.31 (ruling R10.7): into a woods or building hex along one or two contiguous hexsides the obstacle does not touch, starting at a vertex
        // of the hexside crossed.
        if (from.Level != 0 || ReadLocation(state, from) is not { } fromRead)
        {
            return (null, "play.move-bypass: Bypass moves at ground level along one or two hexsides of an adjacent woods or building hex (A4.3, A4.31)", null, false);
        }

        if (SideToward(state, to, from) is not { } side)
        {
            return (null, $"play.move-step: {to} is not an adjacent Location the map reads", null, false);
        }

        var entryWall = SideToward(state, from, to) is { } toward ? WallOn(HexsideAt(state, from, toward)) : null;
        return BypassStep(state, movers, to, side, fromRead.Hex.BaseLevel, entryWall, bypass);
    }

    /// <summary>
    /// The hexside a stack in Bypass entered its hex by (ruling R10.7): toward the Location it came from, or, for a stack that entered the map in Bypass,
    /// the map edge's hexside beside its first Bypass hexside (ruling R25.3).
    /// </summary>
    private HexsideDirection? BypassEntered(GameState state, BoardLocation at, BoardLocation? came, IReadOnlyList<HexsideDirection> lane) =>
        came is { } previous ? SideToward(state, at, previous)
            : EdgeSides(state, at).Select(item => (HexsideDirection?)item.Side).FirstOrDefault(side => ((int)lane[0] - (int)side!.Value + 6) % 6 is 1 or 5);

    /// <summary>
    /// A Bypass step into a woods or building hex (A4.3, A4.31; ruling R10.7) across its hexside <paramref name="side"/>: from an adjacent hex at
    /// <paramref name="fromBaseLevel"/>, or from off board across the map edge (ruling R25.3), with the wall or hedge on the hexside crossed.
    /// </summary>
    private (InfantryEntry? Entry, string? Reason, IReadOnlyList<HexsideDirection>? Bypass, bool Occupy) BypassStep(GameState state, UnitInstance[] movers,
        BoardLocation to, HexsideDirection side, int fromBaseLevel, string? entryWall, List<HexsideDirection> bypass)
    {
        if (bypass.Count is < 1 or > 2 || to.Level != 0 || ReadLocation(state, to) is not { } targetRead
            || TerrainKey(targetRead) is not { } obstacleKey || !(obstacleKey == "woods" || IsBuildingTerrain(obstacleKey)))
        {
            return (null, "play.move-bypass: Bypass moves at ground level along one or two hexsides of an adjacent woods or building hex (A4.3, A4.31)", null, false);
        }

        var direction = ((int)bypass[0] - (int)side + 6) % 6;
        if (direction is not (1 or 5) || (bypass.Count == 2 && ((int)bypass[1] - (int)bypass[0] + 6) % 6 != direction))
        {
            return (null, "play.move-bypass: the Bypassed hexsides are contiguous and start at a vertex of the hexside the stack crosses (A4.31)", null, false);
        }

        var laneCost = 0;
        var laneTerrain = "open-ground";
        foreach (var bypassed in bypass)
        {
            var facts = HexsideAt(state, to, bypassed);
            var name = facts?.Terrain?.Name;
            if (facts is null || name is null || name == "Woods" || OrdinaryBuildings.Contains(name) || WallOn(facts) == "other"
                || (FireTerrain.GetValueOrDefault(name) ?? (facts.Terrain!.IsRoad ? "open-ground" : null)) is not { } key || InfantryEntryHalfMf(state, key) is not { } cost)
            {
                return (null, $"play.move-bypass: the obstacle touches the {bypassed.ToString().ToLowerInvariant()} hexside, or its other terrain is not reviewed, so it may not be Bypassed there (A4.3, A4.31)", null, false);
            }

            if (cost >= laneCost)
            {
                (laneCost, laneTerrain) = (cost, key);
            }
        }

        // Table player, pass 10: a hex with a wall or hedge is not Bypassed until the vertex LOS for fire at a Bypassing stack is built, nor one
        // holding friendly units, whose TEM a Bypassing stack would share in the fire at the Location.
        if (targetRead.Hex.Hexsides.Any(item => WallOn(item) is not null)
            || state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side == movers[0].Side))
        {
            return (null, $"play.move-bypass: {to} has a wall or hedge, or holds friendly units; that Bypass is not built (A4.34; ruling R10.7)", null, false);
        }

        // A4.3: no Bypass of an obstacle holding an armed Known enemy unit (rubble is not an obstacle here).
        if (state.At(to).OfType<UnitInstance>().Any(unit => unit.Status == InstanceStatus.Active && unit.Side != movers[0].Side && KnownEnemy(unit)
            && !Is(unit, Conditions.Disrupted)))
        {
            return (null, $"play.move-bypass: {to} holds an armed Known enemy unit, so its obstacle may not be Bypassed (A4.3)", null, false);
        }

        var rise = targetRead.Hex.BaseLevel - fromBaseLevel;
        if (Math.Abs(rise) > 1)
        {
            return (null, "play.move-bypass: a Bypass across an Abrupt Elevation Change is not reviewed (B10.51; ruling R10.7)", null, false);
        }

        if (entryWall == "other")
        {
            return (null, "play.move-hexside: that hexside terrain is not reviewed (ruling R10.1)", null, false);
        }

        var halfMf = (rise == 1 ? 2 * laneCost : laneCost) + (entryWall is not null ? 2 : 0) + BlazeEntryHalfMf(state, to);
        return (new InfantryEntry(halfMf, laneTerrain, false, false, false, rise != 0), null, bypass, false);
    }

    /// <summary>
    /// Whether a Good Order MMC moving with a Good Order leader of its nationality, who began the MPh with it and has moved with it at every step, has
    /// the leader's two MF bonus (A4.12; ruling R10.8). A berserk unit's MF are never increased but by the Road Bonus (A15.431).
    /// </summary>
    private bool LeaderBonus(GameState state, UnitInstance unit, IReadOnlyList<UnitInstance> movers)
    {
        // A12.11 (table player, pass 10): a Dummy moves with a leader's bonus as a real MMC would.
        var dummy = unit.Kind == UnitKinds.Dummy;
        if ((!dummy && !vocabulary.IsA(unit.Kind, "asl:mmc")) || Is(unit, Conditions.Berserk) || Is(unit, Conditions.Broken)
            || (dummy ? movers.FirstOrDefault(item => vocabulary.IsA(item.Kind, "asl:leader") && item.Side == unit.Side) is not { } guide ? null : Nationality(guide) : Nationality(unit)) is not { } nationality)
        {
            return false;
        }

        var first = unit.MovedWith is null && unit.MfSpent == 0 && !unit.HalfMfSpent;
        return movers.Any(leader => leader.Id != unit.Id && vocabulary.IsA(leader.Kind, "asl:leader") && !Is(leader, Conditions.Broken) && !Is(leader, Conditions.Berserk)
            && Nationality(leader) == nationality
            && (first ? leader.MfSpent == 0 && !leader.HalfMfSpent && leader.MovedWith is null : unit.MovedWith?.Contains(leader.Id, StringComparer.Ordinal) == true));
    }

    /// <summary>
    /// The MMC a leader lends his IPC to (A4.42; ruling R10.8): the only MMC of the stack with his leader bonus that carries more than its own IPC; the
    /// leader's own IPC is then spent. Nulls when there is none, or more than one.
    /// </summary>
    private (string? Recipient, string? Leader) LeaderIpcRecipient(GameState state, IReadOnlyList<UnitInstance> movers)
    {
        var laden = movers.Where(unit => LeaderBonus(state, unit, movers) && Laden(state, unit)).ToArray();
        var leader = laden.Length != 1 ? null : movers.FirstOrDefault(item => vocabulary.IsA(item.Kind, "asl:leader") && !Is(item, Conditions.Broken) && !Is(item, Conditions.Wounded)
            && Nationality(item) == Nationality(laden[0]));
        return leader is not null ? (laden[0].Id, leader.Id) : (null, null);
    }

    /// <summary>A unit's nationality from the reviewed catalog; null when the catalog does not name it.</summary>
    private static string? Nationality(UnitInstance unit) =>
        unit.Definition is { } reference ? FireReference.Value.Definitions.GetValueOrDefault(reference.Definition)?.Nationality : null;
}
