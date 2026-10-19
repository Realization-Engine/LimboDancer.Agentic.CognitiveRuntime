using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Whether a unit is Encircled, with the upper-level Encirclement of A7.72 (p. 58; pass 45, task 45.1b). The state knows the Encirclement counters
/// (A7.7); the cut-off path is a read of the map and the units, so it is made here when asked, and Rules decides it
/// (<see cref="ScenarioA1UpperLevelEncirclement"/>): this file hands over the unit's facts, the steps the movement rules allow, and who stands in
/// each Location. The rule counts unconcealed enemy units only, so the result tells a side nothing it may not know once setup is closed.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Whether a unit is Encircled: by a counter in its Location (A7.7), or in an upper building level with no path to ground level (A7.72).</summary>
    public bool Encircled(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return state.Encircled(unit) || UpperLevelEncircled(state, unit);
    }

    /// <summary>
    /// A7.72: an Infantry unit in an upper level of a building, cut off from ground level by enemy units. Searched once a state and unit, and kept
    /// with the state (the Play Step Plan, task 1): the rout's enemy lists ask it for every Known enemy in every scan.
    /// </summary>
    public bool UpperLevelEncircled(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        var kept = MemoOf(state).UpperLevelEncircled;
        if (kept.TryGetValue(unit, out var known))
        {
            return known;
        }

        var found = SearchUpperLevelEncircled(state, unit);
        kept.TryAdd(unit, found);
        return found;
    }

    private bool SearchUpperLevelEncircled(GameState state, UnitInstance unit)
    {
        if (unit.Side is not { } side || state.Location(unit.Id)?.Location is not { Level: > 0 } at)
        {
            return false;
        }

        var facts = new UpperLevelUnitFacts(unit.Status == InstanceStatus.Active, LiveFire.IsVehicle(unit), state.Aboard(unit.Id) is not null, at.Side is not null, at.Level,
            ReadLocation(state, at) is { } read && TerrainKey(read)?.EndsWith("building", StringComparison.Ordinal) == true,
            Is(unit, Conditions.Berserk), Is(unit, Conditions.Heroic) || unit.Kind == "asl:hero");
        var paths = new UpperLevelPaths(this, state, side);
        return ScenarioA1UpperLevelEncirclement.Encircled(facts, paths.Index(at), paths);
    }

    /// <summary>The A7.72 search's fact reader: Locations numbered as the search reaches them, the steps Infantry could make, and the units in a Location.</summary>
    private sealed class UpperLevelPaths(GamePlanner planner, GameState state, string side) : IUpperLevelPathReader
    {
        private readonly List<BoardLocation> locations = [];
        private readonly Dictionary<BoardLocation, int> indexes = [];

        public int Index(BoardLocation at)
        {
            if (!indexes.TryGetValue(at, out var index))
            {
                index = locations.Count;
                locations.Add(at);
                indexes[at] = index;
            }

            return index;
        }

        public IReadOnlyList<int> StepsFrom(int location)
        {
            var from = locations[location];
            return [.. planner.ChargeNeighbors(state, from).Distinct().Where(to => planner.InfantryStep(state, from, to).Entry is not null).Select(Index)];
        }

        public bool GroundLevel(int location) => locations[location].Level == 0;

        public IReadOnlyList<PathUnitFacts> UnitsAt(int location) => [.. state.At(locations[location]).OfType<UnitInstance>().Select(unit => new PathUnitFacts(
            unit.Status == InstanceStatus.Active, unit.Side != side, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Broken), Is(unit, Conditions.Unarmed),
            Is(unit, Conditions.Captured), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden)))];
    }
}
