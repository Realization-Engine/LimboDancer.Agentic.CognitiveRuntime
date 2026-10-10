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

    /// <summary>
    /// The units of the game as the scans for a seeing enemy read them (pass 32.b), with a table of their Locations by index, and the LOS between two
    /// Locations of the table as Rules asks for it (the pass 32 design, D4).
    /// </summary>
    private sealed class EnemyScan : ILosFactReader
    {
        private readonly GamePlanner planner;
        private readonly GameState state;
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        public EnemyScan(GamePlanner planner, GameState state)
        {
            this.planner = planner;
            this.state = state;
            Units = [.. state.Units.Select(unit => new EnemyUnitFacts(unit.Status == InstanceStatus.Active, unit.Side, unit.Kind == UnitKinds.Dummy, state.Aboard(unit.Id) is not null,
                GameState.Condition(unit, Conditions.Broken) switch { ConditionState.True => true, ConditionState.False => false, _ => null },
                Is(unit, Conditions.Hidden), GoodOrder(unit), state.Location(unit.Id)?.Location is { } at ? Index(at) : null))];
        }

        public IReadOnlyList<EnemyUnitFacts> Units
        {
            get;
        }

        /// <summary>The index of a Location in the table, added when it is new.</summary>
        public int Index(BoardLocation location)
        {
            if (!indexes.TryGetValue(location, out var index))
            {
                index = locations.Count;
                locations.Add(location);
                indexes[location] = index;
            }

            return index;
        }

        public LosFacts? Los(int fromLocation, int toLocation) =>
            planner.Los(state, locations[fromLocation], locations[toLocation]) is { } result ? new LosFacts(result.Status == LosStatus.Clear, result.Range) : null;
    }

    /// <summary>
    /// The range to the nearest Good Order enemy ground unit with a clear LOS to a Location (A12.34); null when none has one. A Passenger is not counted:
    /// its vehicle is (ruling R26.2).
    /// </summary>
    private int? NearestGoodOrderEnemyInLos(GameState state, string side, BoardLocation at)
    {
        var scan = new EnemyScan(this, state);
        return ScenarioA1MovementCalculator.NearestGoodOrderEnemyInLos(scan.Units, side, scan.Index(at), scan);
    }

    /// <summary>
    /// An attack with, for each concealed unit that fires or directs, whether a Good Order enemy ground unit within 16 hexes has a LOS to it (A12.14,
    /// read in the PDF, p. 77; pass 31d, ruling R31d.2). The Fire package sees the target Location alone, and refused fire by concealed units at a
    /// Location with no Good Order unit as undecided; a refusal marks no firer, so a side could try a "?" stack and read from the refusal that it
    /// held Dummies. With this read the attack is made, and the firer's "?" is lost or kept as the rule has it. A hidden unit, which would have to
    /// show itself to force the loss, does not force it. At night the read is not given (E1.31), and the package decides as before.
    /// </summary>
    private FireAttack WithSeen(GameState state, FireAttack attack)
    {
        // Pass 32.b: the attack's firers and directors are found in the state here, the units and their LOS read through the scan, and Rules decides.
        var scan = new EnemyScan(this, state);
        var subjects = new Dictionary<string, SeenSubjectFacts>(StringComparer.Ordinal);
        foreach (var unitId in (attack.Firers ?? []).Select(item => item.UnitId).Append(attack.Director?.UnitId).Concat((attack.OtherDirectors ?? []).Select(item => item.UnitId)))
        {
            if (unitId is not null && !subjects.ContainsKey(unitId) && state.Unit(unitId) is { } unit)
            {
                subjects[unitId] = new SeenSubjectFacts(unit.Side, state.Location(unit.Id)?.Location is { } at ? scan.Index(at) : null);
            }
        }

        return ScenarioA1MovementCalculator.WithSeen(attack, state.Night, subjects, scan.Units, scan);
    }

    /// <summary>Whether any Good Order enemy ground unit within 16 hexes has a clear LOS to a Location (A12.14, A12.141).</summary>

    private bool EnemyGoodOrderInLosWithin16(GameState state, string side, BoardLocation at)
    {
        var scan = new EnemyScan(this, state);
        return ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16(scan.Units, side, scan.Index(at), scan);
    }
}
