using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>A vehicle's MP expenditure on a route: a VCA change to <see cref="Facing"/>, an entry into <see cref="To"/>, or an exit.</summary>
public sealed record VehicleMove(string Kind, BoardLocation? To, UnitFacing? Facing, HexsideDirection? Direction, int HalfMp);

/// <summary>
/// Leaving the playing area (A2.6) and the Recall (D5.341, rulings R5.17 and R5.18): a vehicle exits from an edge hex into the mirror image
/// of that hex, within its VCA; a Recalled AFV, once its counter shows Recall; +1, must take a shortest route in MP to its side's Friendly
/// Board Edge in Motion; an immobilized Recalled AFV is Abandoned by its crew.
/// </summary>
public sealed partial class GamePlanner
{
    // The bearing of each hexside direction, counterclockwise from east, on a board of flat-topped hexes in columns (BoardGeometry).
    private static double DirectionDegrees(HexsideDirection side) => side switch
    {
        HexsideDirection.North => 90,
        HexsideDirection.NorthEast => 30,
        HexsideDirection.SouthEast => 330,
        HexsideDirection.South => 270,
        HexsideDirection.SouthWest => 210,
        _ => 150,
    };

    /// <summary>Whether a vehicle must leave by its Friendly Board Edge: Recalled, with its counter on the Recall; +1 side (D5.341).</summary>
    public static bool MustLeave(UnitInstance vehicle) =>
        LiveFire.IsVehicle(vehicle) && Is(vehicle, Conditions.Recalled) && Is(vehicle, Conditions.StunRecovery) && !Is(vehicle, Conditions.Immobilized)
        && !Is(vehicle, Conditions.Abandoned);

    /// <summary>
    /// The directions in which a Location's hex lies on the edge of the map, each with the edge it crosses (<c>top</c>, <c>bottom</c>,
    /// <c>left</c>, or <c>right</c> as the map is laid out), read from the map's geometry: a direction with no hex across it.
    /// </summary>
    private List<(HexsideDirection Side, string Edge)> EdgeSides(GameState state, BoardLocation at)
    {
        BoardGeometry geometry;
        HexIndex index;
        if (Composed(state) is { } composed)
        {
            if (composed.Layout.Locate(at.Board, at.Hex) is not { } located)
            {
                return [];
            }

            (geometry, index) = (composed.Layout.Geometry, located);
        }
        else if (state.Map.Board(at.Board) is { } placed && boards.TryGetBoard(at.Board, placed.Version).Board is { } handle && handle.Geometry.TryGetIndex(at.Hex, out var own))
        {
            (geometry, index) = (handle.Geometry, own);
        }
        else
        {
            return [];
        }

        var sides = new List<(HexsideDirection, string)>();
        foreach (var side in HexsideDirections.All)
        {
            if (geometry.Neighbor(index, side) is not null)
            {
                continue;
            }

            // The neighbor's index as BoardGeometry.Neighbor computes it, before the bounds check, says which edge it lies beyond.
            var even = index.Column % 2 == 0;
            var (column, row) = side switch
            {
                HexsideDirection.North => (index.Column, index.Row - 1),
                HexsideDirection.NorthEast => (index.Column + 1, index.Row + (even ? 0 : -1)),
                HexsideDirection.SouthEast => (index.Column + 1, index.Row + (even ? 1 : 0)),
                HexsideDirection.South => (index.Column, index.Row + 1),
                HexsideDirection.SouthWest => (index.Column - 1, index.Row + (even ? 1 : 0)),
                _ => (index.Column - 1, index.Row + (even ? 0 : -1)),
            };
            var edge = column < 0 ? "left" : column >= geometry.WidthInHexes ? "right" : row < 0 ? "top" : "bottom";
            sides.Add((side, edge));
        }

        return sides;
    }

    /// <summary>
    /// The half MP of a vehicle's exit across a map edge in a direction (A2.6): entering the mirror image of its hex, so its own terrain's
    /// cost, at the road rate when a road crosses that hexside (D2.16); null when the terrain's cost is not reviewed.
    /// </summary>
    private int? VehicleExitCost(GameState state, UnitInstance vehicle, BoardLocation at, HexsideDirection side)
    {
        if (ReadLocation(state, at) is not { } read || VehicleDefinition(vehicle)?.MovementType is not { } type || TerrainKey(read) is not { } terrain)
        {
            return null;
        }

        if (terrain == "grain")
        {
            if (state.ScenarioMonth is not { } month)
            {
                return null;
            }

            terrain = month is >= 4 and <= 9 ? "grain" : "open-ground";
        }

        if (!VehicleTerrainHalfMp.TryGetValue((type, terrain), out var halfMp))
        {
            return null;
        }

        return read.Hex.Hexsides.FirstOrDefault(item => item.Side == side)?.Terrain?.IsRoad == true
            ? (IsAfv(vehicle) && Is(vehicle, Conditions.ButtonedUp) ? 2 : 1)
            : halfMp;
    }

    /// <summary>
    /// Whether a vehicle not yet moving may begin its MP expenditures (D2.1): not immobilized, Stunned, Recalled, or after Prep Fire (D.7,
    /// D5.34, D.3), except a Recalled vehicle that must now leave (D5.341; ruling R5.17).
    /// </summary>
    public static bool MayStartVehicleMove(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return MustLeave(vehicle) || new[] { Conditions.Immobilized, Conditions.Stunned, Conditions.Shocked, Conditions.UnconfirmedKill, Conditions.Recalled, Conditions.PrepFire }.All(condition => !Is(vehicle, condition));
    }

    /// <summary>Whether an AFV's crew may place or remove its BU counter by its state (D5.33, D5.34): not Stunned, and not Recalled until Recall; +1.</summary>
    public static bool MayChangeExposure(UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        return !Is(vehicle, Conditions.Stunned) && !Is(vehicle, Conditions.Shocked) && !Is(vehicle, Conditions.UnconfirmedKill) && (!Is(vehicle, Conditions.Recalled) || Is(vehicle, Conditions.StunRecovery));
    }

    /// <summary>The map edge an exit leaves by: top, bottom, left, or right; null when the Location is not an edge hex that way.</summary>
    public string? ExitEdge(GameState state, UnitInstance vehicle, VehicleMove exit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(exit);
        return state.Location(vehicle.Id) is { } at ? EdgeSides(state, at.Location).FirstOrDefault(item => item.Side == exit.Direction).Edge : null;
    }

    /// <summary>The exits a vehicle may make from its Location (A2.6): an edge direction within its VCA (D2.11) whose exit cost is reviewed.</summary>
    public IReadOnlyList<VehicleMove> VehicleExits(GameState state, UnitInstance vehicle, string? edge = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing })
        {
            return [];
        }

        return [.. EdgeSides(state, at.Location).Where(item => (edge is null || item.Edge == edge)
                && Math.Abs(Math.Abs(((DirectionDegrees(item.Side) - FacingDegrees(facing) + 540) % 360) - 180) - 30) < 1)
            .Select(item => (item.Side, Cost: VehicleExitCost(state, vehicle, at.Location, item.Side))).Where(item => item.Cost is not null)
            .Select(item => new VehicleMove(VehicleStepped.Exit, null, null, item.Side, item.Cost!.Value))];
    }

    /// <summary>
    /// The first MP expenditures of a shortest route in MP from a vehicle's Location and VCA off its side's Friendly Board Edge (D5.341,
    /// ruling R5.17), by Dijkstra over Locations and VCAs: a VCA change of one hexspine for one MP, an entry into a hex of the VCA at its cost,
    /// and an exit from an edge hex. Hexes the terrain review does not admit are not entered; when one of them, at the least cost any entry has,
    /// could shorten the route, the route is undecided. Returns the first moves and the route's cost, or why it is undecided.
    /// </summary>
    public (IReadOnlyList<VehicleMove> Moves, int? HalfMp, string? Undecided) RecallRoute(GameState state, UnitInstance vehicle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(vehicle);
        if (state.Side(vehicle.Side)?.FriendlyEdge is not { } edge)
        {
            return ([], null, $"play.recall-edge: the {vehicle.Side} side names no Friendly Board Edge, so {vehicle.Id}'s route off the map is not decided (ruling R5.16)");
        }

        if (state.Location(vehicle.Id) is not { } at || vehicle.Position is not MapPosition { Facing: { } facing })
        {
            return ([], null, "play.recall-edge: the vehicle is not on the map with a VCA");
        }

        var vca = new Dictionary<(BoardLocation, UnitFacing), IReadOnlyList<BoardLocation>>();
        var costs = new Dictionary<(BoardLocation, BoardLocation), int?>();
        var exits = new Dictionary<(BoardLocation, UnitFacing), int?>();
        IReadOnlyList<(VehicleMove Move, BoardLocation At, UnitFacing Facing)> Moves((BoardLocation At, UnitFacing Facing) node, bool lowerBound)
        {
            var list = new List<(VehicleMove, BoardLocation, UnitFacing)>();
            foreach (var turn in new[] { 1, 5 })
            {
                var next = (UnitFacing)(((int)node.Facing + turn) % 6);
                list.Add((new VehicleMove(VehicleStepped.Turn, null, next, null, 2), node.At, next));
            }

            if (!vca.TryGetValue(node, out var hexes))
            {
                vca[node] = hexes = VcaHexes(state, node.At, node.Facing);
            }

            foreach (var to in hexes)
            {
                if (!costs.TryGetValue((node.At, to), out var cost))
                {
                    costs[(node.At, to)] = cost = VehicleEntryBar(state, vehicle, to) is null ? VehicleEntryCost(state, vehicle, node.At, to) ?? -1 : null;
                }

                // -1 marks terrain the review does not decide, which a lower bound enters at the least cost any entry has (½ MP).
                if (cost is { } value && (value >= 0 || lowerBound))
                {
                    list.Add((new VehicleMove(VehicleStepped.Enter, to, null, null, value >= 0 ? value : 1), to, node.Facing));
                }
            }

            return list;
        }

        int? Exit((BoardLocation At, UnitFacing Facing) node)
        {
            if (!exits.TryGetValue(node, out var cost))
            {
                var moved = vehicle with
                {
                    Position = new MapPosition(node.At) { Facing = node.Facing }
                };
                exits[node] = cost = EdgeSides(state, node.At).Where(item => item.Edge == edge
                        && Math.Abs(Math.Abs(((DirectionDegrees(item.Side) - FacingDegrees(node.Facing) + 540) % 360) - 180) - 30) < 1)
                    .Select(item => VehicleExitCost(state, moved, node.At, item.Side)).OfType<int>().DefaultIfEmpty(int.MaxValue).Min() is var least && least < int.MaxValue
                    ? least : null;
            }

            return cost;
        }

        int? Distance((BoardLocation At, UnitFacing Facing) start, bool lowerBound)
        {
            var best = new Dictionary<(BoardLocation, UnitFacing), int> { [start] = 0 };
            var queue = new PriorityQueue<(BoardLocation, UnitFacing), int>();
            queue.Enqueue(start, 0);
            int? goal = null;
            while (queue.TryDequeue(out var node, out var cost))
            {
                if (goal is { } found && cost >= found)
                {
                    break;
                }

                if (cost > best[node])
                {
                    continue;
                }

                if (Exit(node) is { } exit && cost + exit < (goal ?? int.MaxValue))
                {
                    goal = cost + exit;
                }

                foreach (var (move, to, turned) in Moves(node, lowerBound))
                {
                    var total = cost + move.HalfMp;
                    if (total < best.GetValueOrDefault((to, turned), int.MaxValue))
                    {
                        best[(to, turned)] = total;
                        queue.Enqueue((to, turned), total);
                    }
                }
            }

            return goal;
        }

        var origin = (at.Location, facing);
        var exact = Distance(origin, lowerBound: false);
        var lower = Distance(origin, lowerBound: true);
        if (exact is not { } route || lower < route)
        {
            return ([], exact, $"play.recall-route: the shortest route from {at.Location} to the {edge} edge may cross terrain the review does not admit, so {vehicle.Id} may end its move in place (ruling R5.17)");
        }

        var first = new List<VehicleMove>();
        if (Exit(origin) is { } here && here == route)
        {
            first.AddRange(VehicleExits(state, vehicle, edge).Where(item => item.HalfMp == here));
        }

        foreach (var (move, to, turned) in Moves(origin, lowerBound: false))
        {
            if (Distance((to, turned), lowerBound: false) is { } rest && move.HalfMp + rest == route)
            {
                first.Add(move);
            }
        }

        return (first, route, null);
    }

    /// <summary>
    /// A Recalled AFV that is immobilized is Abandoned (D5.341, D5.41; ruling R5.18): marked Abandoned and stopped, with its crew placed beneath
    /// it as a crew counter of its nationality, carrying the Stun +1 (D5.34).
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> AbandonEvents(UnitInstance vehicle, string attemptId)
    {
        yield return ("conditions-changed", new ConditionsChanged(vehicle.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
        {
            [Conditions.Abandoned] = ConditionState.True,
            [Conditions.Motion] = ConditionState.False,
        }));

        var nationality = VehicleDefinition(vehicle)?.Nationality;
        if (FireReference.Value.Definitions.Values.Where(item => item.Kind == "asl:crew" && item.Nationality == nationality).Select(item => item.Id)
            .Order(StringComparer.Ordinal).FirstOrDefault() is { } crew && vehicle.Position is MapPosition position)
        {
            var conditions = new Dictionary<string, ConditionState>(StringComparer.Ordinal)
            {
                [Conditions.Broken] = ConditionState.False,
                [Conditions.Pinned] = ConditionState.False,
                [Conditions.Concealed] = ConditionState.False,
                [Conditions.Hidden] = ConditionState.False,
                [Conditions.StunRecovery] = ConditionState.True,
            };
            yield return ("instance-created", new InstanceCreated(new NewInstance($"{attemptId}-{vehicle.Id}-crew", "asl:crew", crew, vehicle.Side,
                new MapPosition(position.Location), null, conditions)));
        }
    }
}
