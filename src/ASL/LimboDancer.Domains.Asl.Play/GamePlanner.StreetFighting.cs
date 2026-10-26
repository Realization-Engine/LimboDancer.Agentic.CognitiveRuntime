using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Street Fighting (A11.8, p. 76; pass 70, ruling R70.33): the planner reads the sides of a road hex from the map (the Maps read of its road flanks) and where
/// each unit advanced from (the state's advance records), and Rules decides who qualifies, the -1/+1 in the vehicle's CC, and the return to the building.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// The sides of the road through a ground-level Location, each as the names of the building hexes ADJACENT to it (A11.8): read from the board's hex facts,
    /// a few hexsides per call, so no cache; empty off the map, above ground level, or where no road crosses the hex.
    /// </summary>
    private IReadOnlyList<IReadOnlyList<string>> RoadSides(GameState state, BoardLocation location)
    {
        if (location.Level != 0 || Handle(state, location.Board) is not { } handle || !handle.Geometry.TryGetIndex(location.Hex, out var index) || !handle.Facts.Contains(index))
        {
            return [];
        }

        return [.. RoadFlanks.Of(handle.Facts, index).Select(side => (IReadOnlyList<string>)[.. side.Select(hex => handle.Geometry.NameOf(hex).ToString())])];
    }

    /// <summary>Whether a unit qualifies for Street Fighting in its road Location, as Rules reads its advance (A11.8; ruling R70.33).</summary>
    private static bool StreetFighter(GameState state, IReadOnlyList<IReadOnlyList<string>> sides, BoardLocation location, string unitId, bool enemyVehicleThere) =>
        state.Advances.LastOrDefault(item => item.Unit == unitId && item.To == location) is { } advance
        && ScenarioA1StreetFighting.Qualifies(sides, true, enemyVehicleThere, advance.AdvancedFrom is { } from && from.Board == location.Board ? from.Hex.ToString() : null,
            advance.AdvancedFrom?.Level);

    /// <summary>
    /// The units of a vehicle's CC attack, or of the attack on it, that qualify for Street Fighting (A11.8; ruling R70.33); null for none, as every attack
    /// planned before the pass records.
    /// </summary>
    private string[]? StreetFighters(GameState state, BoardLocation location, IReadOnlyList<string> units)
    {
        var sides = RoadSides(state, location);
        if (!ScenarioA1StreetFighting.Flanked(sides))
        {
            return null;
        }

        string[] found = [.. units.Where(id => StreetFighter(state, sides, location, id, true))];
        return found.Length == 0 ? null : found;
    }

    /// <summary>
    /// The units of a side that may return to the building Location they advanced from (A11.8; ruling R70.33), each with that Location: in the CCPh, from a
    /// road Location whose CC with an enemy vehicle is over, unless broken, pinned, or a prisoner. Read from the state's advance records and the planner's
    /// index, for the Play page; the side's own units, which its view sees.
    /// </summary>
    public IReadOnlyList<(UnitInstance Unit, BoardLocation From, BoardLocation At)> StreetFightingReturns(GameState state, string side)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Phase != "ccph" || state.Advances.Count == 0)
        {
            return [];
        }

        var index = MemoOf(state).Objects(state);
        var found = new List<(UnitInstance Unit, BoardLocation From, BoardLocation At)>();
        foreach (var advance in state.Advances.Where(item => item.AdvancedFrom is not null))
        {
            if (index.Unit(advance.Unit) is { Status: InstanceStatus.Active } unit && unit.Side == side && index.Location(unit.Id)?.Location == advance.To
                && ReturnBar(state, index, unit) is null)
            {
                found.Add((unit, advance.AdvancedFrom!, advance.To));
            }
        }

        return found;
    }

    /// <summary>Why a unit may not return to the building Location it advanced from (A11.8), as Rules reads it, or null.</summary>
    private string? ReturnBar(GameState state, ObjectIndex index, UnitInstance unit)
    {
        var at = unit.Position is MapPosition ? index.Location(unit.Id)?.Location : null;
        var advance = at is null ? null : state.Advances.LastOrDefault(item => item.Unit == unit.Id && item.To == at);
        var enemyVehicle = at is not null && (index.At(at).OfType<UnitInstance>().Any(other => LiveFire.IsVehicle(other) && other.Side != unit.Side) || index.Wrecks(at).Any());
        var qualified = advance is not null && StreetFighter(state, RoadSides(state, at!), at!, unit.Id, enemyVehicle);
        return ScenarioA1StreetFighting.ReturnBar(unit.Id, state.Phase, qualified, advance is not null, state.CloseCombats.FirstOrDefault(item => item.Location == at) is { Closed: true },
            Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned), Is(unit, Conditions.Captured));
    }

    /// <summary>
    /// A unit that fought a vehicle by Street Fighting returns to the building Location it advanced from (A11.8; ruling R70.33): one <c>instance-moved</c>,
    /// spending no MF; staying needs no step.
    /// </summary>
    private GamePlan PlanStreetFightingReturn(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "unitId", out var unitId) || state.Unit(unitId) is not { Status: InstanceStatus.Active } unit)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Street Fighting return names a unit in play");
        }

        var index = MemoOf(state).Objects(state);
        if (ReturnBar(state, index, unit) is { } bar)
        {
            return Refused(scope, label, expected, bar);
        }

        var at = index.Location(unit.Id)!.Location;
        var from = state.Advances.Last(item => item.Unit == unit.Id && item.To == at).AdvancedFrom!;
        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [Event(scope, attemptId, 1, expected, "instance-moved", new InstanceMoved(unit.Id, new MapPosition(from)) { Ended = true }, ScenarioA1CloseCombatPackage.Identity.ToString(), null)],
            [ScenarioA1StreetFighting.ReturnText(unit.Id, at.ToString(), from.ToString())]);
    }
}
