using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Maps.Read;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The map facts that Rally, movement, and fire share (unit steps 19 to 23): a Location's read and admitted terrain, a
/// step between adjacent Locations (across a seam on a placed map), LOS, and ADJACENT (A.8, p. 43).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The read of one Location of the game's map, or null when it cannot be read.</summary>
    private LocationRead? ReadLocation(GameState state, BoardLocation location)
    {
        if (state.Map.IsPlaced)
        {
            return Composed(state)?.Resolve(location).Read;
        }

        return state.Map.Board(location.Board) is { } placed && boards.TryGetBoard(location.Board, placed.Version).Board is { } handle
            ? handle.Resolve(location).Read
            : null;
    }

    /// <summary>The terrain of a Location as the Scenario A1 packages name it, or null when it is none they admit.</summary>
    private static string? TerrainKey(LocationRead read)
    {
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
            var fromRead = composed.Resolve(from).Read;
            var toRead = composed.Resolve(to).Read;
            var adjacent = fromRead is not null && toRead is not null && composed.Distance(from.Board, from.Hex, to.Board, to.Hex) == 1;
            return (fromRead, toRead, adjacent, adjacent ? composed.Crossed(from.Board, from.Hex, to.Board, to.Hex)?.Agreed : null);
        }

        if (from.Board != to.Board || state.Map.Board(to.Board) is not { } placed || boards.TryGetBoard(to.Board, placed.Version).Board is not { } handle)
        {
            return (null, null, false, null);
        }

        var one = handle.Resolve(from).Read;
        var two = handle.Resolve(to).Read;
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
            map = state.Map.Boards is [{ } placed] && boards.TryGetBoard(placed.Board, placed.Version).Board is { } handle
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
            : state.Map.Boards is [{ } placed] && boards.TryGetBoard(placed.Board, placed.Version).Board is { } handle ? LosMap.ForBoard(handle).Map : null;
        return map is null ? (null, null)
            : (LosCalculator.Check(map, from, LosAim.LosPoint, hexside, LosAim.LosPoint), LosCalculator.Check(map, from, LosAim.LosPoint, hexside, LosAim.AuxiliaryPoint));
    }

    /// <summary>
    /// ADJACENT (A.8, p. 43): the Locations share a hexside at the same level, with a clear LOS and no hexside terrain or
    /// cliff between them, so Infantry could advance from one to the other. The review reads it this way for the
    /// terrain it admits.
    /// </summary>
    private bool IsAdjacent(GameState state, BoardLocation one, BoardLocation two)
    {
        var (from, to, adjacent, crossed) = Step(state, one, two);
        return adjacent && from is not null && to is not null && crossed is not null
            && from.Hex.BaseLevel + from.Level.Level == to.Hex.BaseLevel + to.Level.Level
            && crossed.HexsideTerrain is null && !crossed.Cliff
            && Los(state, one, two) is { Status: LosStatus.Clear };
    }

    /// <summary>Whether any Good Order enemy ground unit within 16 hexes has a clear LOS to a Location (A12.14, A12.141).</summary>
    private bool EnemyGoodOrderInLosWithin16(GameState state, string side, BoardLocation at) =>
        state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Side != side && unit.Kind != UnitKinds.Dummy
                && GameState.Condition(unit, Conditions.Broken) != ConditionState.True)
            .Select(unit => state.Location(unit.Id)?.Location).OfType<BoardLocation>().Distinct()
            .Any(location => Los(state, location, at) is { Status: LosStatus.Clear, Range: <= 16 });
}
