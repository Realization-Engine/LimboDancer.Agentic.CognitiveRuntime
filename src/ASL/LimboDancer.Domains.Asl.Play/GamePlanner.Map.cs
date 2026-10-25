using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The map facts that Rally, movement, and fire share (unit steps 19 to 23): a Location's read and admitted terrain, a
/// step between adjacent Locations (across a seam on a placed map), LOS, and ADJACENT (A.8, p. 43).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The read of one Location of the game's map, or null when it cannot be read.</summary>
    private LocationRead? ReadLocation(GameState state, BoardLocation location) =>
        MemoOf(state).Reads.GetOrAdd(location, static (key, read) => read.Planner.ResolveOnce(read.State, key), (Planner: this, State: state));

    /// <summary>A Location's read from the state's map context, as <see cref="ReadLocation"/> keeps it once a state (task 3b).</summary>
    private LocationRead? ResolveOnce(GameState state, BoardLocation location)
    {
        if (state.Map.IsPlaced)
        {
            return Composed(state)?.Resolve(location).Read;
        }

        return Handle(state, location.Board) is { } handle ? handle.Resolve(location).Read : null;
    }

    /// <summary>Whether a ground-level Location is a woods-road hex (B13.31): the map's facts handed to Rules.</summary>
    private static bool IsWoodsRoad(LocationRead read) =>
        ScenarioA1Definitions.IsWoodsRoadHex((read.Level.Terrain ?? read.Hex.Center.Terrain)?.IsRoad == true, read.Hex.Hexsides.Select(side => side.Terrain?.Name));

    /// <summary>
    /// A4.132: whether the line from a firing Location to a target point of a hex crosses the hex's other terrain; the point is the center dot, or
    /// the hexside the mover crossed at the road rate. Null when the map has no LOS data to read, or on a map of several boards (not built).
    /// </summary>
    private bool? CrossesTerrainInTargetHex(GameState state, BoardLocation from, BoardLocation target, HexsideDirection? enteredAcross)
    {
        if (fireLos is not null)
        {
            return fireLos.CrossesTerrainInTargetHex(state, from, target, enteredAcross);
        }

        return !state.Map.IsPlaced && state.Map.Boards is [{ } placed] && Handle(state, placed.Board) is { } handle
            && BoardLosMaps.GetValue(handle, board => new LosMapHolder(LosMap.ForBoard(board).Map)).Map is { } map
            ? LosTargetPoints.CrossesTerrainInTargetHex(map, from, target, enteredAcross)
            : null;
    }

    /// <summary>The terrain of a Location as the Scenario A1 packages name it, or null when it is none they admit.</summary>
    private static string? TerrainKey(LocationRead read)
    {
        // B13.31 (pass 40, ruling R40.4): a woods-road hex is woods, though its center dot is on the road.
        if (IsWoodsRoad(read))
        {
            return "woods";
        }

        var name = (read.Level.Terrain ?? read.Hex.Center.Terrain)?.Name;
        return name is null ? null
            : FireTerrain.GetValueOrDefault(name)
                ?? (OrdinaryBuildings.Contains(name) ? name.StartsWith("Stone", StringComparison.Ordinal) ? "stone-building" : "wooden-building" : null);
    }

    /// <summary>
    /// A step between two Locations: both reads, whether the hexes are adjacent, and the hexside crossed, on one board or
    /// across a seam of a placed map (Composed Maps Design, section 7).
    /// </summary>
    private (LocationRead? From, LocationRead? To, bool Adjacent, HexsideFacts? Crossed) Step(GameState state, BoardLocation from, BoardLocation to)
    {
        if (Composed(state) is { } composed)
        {
            var fromRead = ReadLocation(state, from);
            var toRead = ReadLocation(state, to);
            var adjacent = fromRead is not null && toRead is not null && composed.Distance(from.Board, from.Hex, to.Board, to.Hex) == 1;
            return (fromRead, toRead, adjacent, adjacent ? composed.Crossed(from.Board, from.Hex, to.Board, to.Hex)?.Agreed : null);
        }

        if (from.Board != to.Board || Handle(state, to.Board) is not { } handle)
        {
            return (null, null, false, null);
        }

        // On a placed map whose composed read failed, the board's own handle, as before; otherwise the reads the state keeps (task 3b).
        var one = state.Map.IsPlaced ? handle.Resolve(from).Read : ReadLocation(state, from);
        var two = state.Map.IsPlaced ? handle.Resolve(to).Read : ReadLocation(state, to);
        var near = one is not null && two is not null && handle.Distance(from.Hex, to.Hex) == 1;
        HexsideFacts? crossed = null;
        if (near)
        {
            var side = Enum.GetValues<HexsideDirection>().FirstOrDefault(direction => handle.Neighbor(from.Hex, direction) == to.Hex);
            crossed = one!.Hex.Hexsides.FirstOrDefault(item => item.Side == side);
        }

        return (one, two, near, crossed);
    }

    /// <summary>The LOS result between two Locations, or null when the map has no LOS data to read.</summary>
    private LosResult? Los(GameState state, BoardLocation from, BoardLocation to)
    {
        if (fireLos is not null)
        {
            return fireLos.Read(state, from, to);
        }

        LosMap? map;
        if (state.Map.IsPlaced)
        {
            map = Composed(state) is { } composed ? LosMap.ForPlacedMap(composed).Map : null;
        }
        else
        {
            map = state.Map.Boards is [{ } placed] && Handle(state, placed.Board) is { } handle
                ? BoardLosMaps.GetValue(handle, board => new LosMapHolder(LosMap.ForBoard(board).Map)).Map : null;
        }

        // Backlog pass 13: a rout's search reads the same LOS many times; the terrain alone decides it, so each map keeps its reads.
        return map is null ? null : LosReads.GetOrCreateValue(map).GetOrAdd((from, to), key => LosCalculator.Check(map, key.From, key.To));
    }

    private sealed record LosMapHolder(LosMap? Map);

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<BoardHandle, LosMapHolder> BoardLosMaps = new();

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<LosMap, System.Collections.Concurrent.ConcurrentDictionary<(BoardLocation From, BoardLocation To), LosResult>> LosReads = new();

    /// <summary>
    /// The LOS from a Location to both ends of a hexside (A8.15; ruling R10.13): to its first vertex and to its second; nulls when the map has no
    /// LOS data to read.
    /// </summary>
    private (LosResult? First, LosResult? Second) LosToHexside(GameState state, BoardLocation from, BoardLocation hexside)
    {
        if (fireLos is not null)
        {
            return (fireLos.Read(state, from, hexside), fireLos.ReadAuxiliary(state, from, hexside));
        }

        LosMap? map = state.Map.IsPlaced
            ? Composed(state) is { } composed ? LosMap.ForPlacedMap(composed).Map : null
            : state.Map.Boards is [{ } placed] && Handle(state, placed.Board) is { } handle ? LosMap.ForBoard(handle).Map : null;
        return map is null ? (null, null)
            : (LosCalculator.Check(map, from, LosAim.LosPoint, hexside, LosAim.LosPoint), LosCalculator.Check(map, from, LosAim.LosPoint, hexside, LosAim.AuxiliaryPoint));
    }

    /// <summary>
    /// Whether two Locations are ADJACENT, as the planner reads it for a fire group across Locations (A7.5): the page offers a second Location
    /// to a group only when this holds, and the Fire package checks it again.
    /// </summary>
    public bool Adjacent(GameState state, BoardLocation one, BoardLocation two)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(one);
        ArgumentNullException.ThrowIfNull(two);
        return IsAdjacent(state, one, two);
    }

    /// <summary>
    /// The range between two Locations: the least number of hexes, whatever the LOS (A6.7), 0 within one hex, or null when the map cannot give
    /// it. The page reads its fire targets by it before a proposal (pass 31c, design section 14); an attack's own range is read with its LOS.
    /// </summary>
    public int? Range(GameState state, BoardLocation from, BoardLocation to)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(from);
        ArgumentNullException.ThrowIfNull(to);
        return HexDistance(state, from, to);
    }

    /// <summary>
    /// ADJACENT (A.8, p. 43; pass 35): a LOS between the two Locations, and an Infantry step from one into the other that the movement rules allow, in
    /// either direction. The step and the LOS are read here when Rules asks, and Rules decides. The step is one an advance may make: a marsh
    /// hex is never entered in the APh (B16.4, p. 130; the referee's review), which Rules answers for the entry read.
    /// </summary>
    private bool IsAdjacent(GameState state, BoardLocation one, BoardLocation two) =>
        one != two && ScenarioA1MovementCalculator.IsAdjacent(one.Board == two.Board && one.Hex == two.Hex,
            () => AdvanceStep(state, one, two) || AdvanceStep(state, two, one),
            () => Los(state, one, two) is { Status: LosStatus.Clear });

    /// <summary>Whether Infantry could advance from one Location into the other (A.8, A4.7, B16.4), enemy presence ignored.</summary>
    private bool AdvanceStep(GameState state, BoardLocation from, BoardLocation to) =>
        InfantryStep(state, from, to).Entry is { } entry && ScenarioA1AdvanceCalculator.MarshBar(entry.AllMf) is null;
}
