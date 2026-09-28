using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Vehicle concealment (backlog pass 6, rulings R6.7 and R6.8): a vehicle sets up concealed or hidden only in Concealment Terrain,
/// here grain in season (A12.2, A12.12, B15.6); it loses its "?" when it moves within 16 hexes and in the LOS of a Good Order enemy
/// ground unit, or is in the LOS of one while not in Concealment Terrain (Case H), when it fires, and when an attack gives it at least a
/// PTC; and a vehicle entering a Location holding enemy units it cannot see reveals them (A12.41; the PAATC is not built).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>Whether a Location is Concealment Terrain for a vehicle (A12.2; ruling R6.7): grain in season, June to September (B15.6).</summary>
    private bool VehicleConcealmentTerrain(GameState state, BoardLocation at) =>
        state.ScenarioMonth is >= 6 and <= 9 && ReadLocation(state, at) is { } read && TerrainKey(read) == "grain";

    /// <summary>Whether a unit is a Good Order enemy ground unit that can see (A12.2): Good Order Personnel, or a vehicle whose crew is not Stunned or Recalled.</summary>
    private bool Watching(UnitInstance unit) => unit.Status == InstanceStatus.Active
        && (LiveFire.IsVehicle(unit) ? !Is(unit, Conditions.Stunned) && !Is(unit, Conditions.Shocked) && !Is(unit, Conditions.UnconfirmedKill) && !Is(unit, Conditions.Recalled) && !Is(unit, Conditions.Abandoned)
            : vocabulary.IsA(unit.Kind, "asl:personnel") && !Is(unit, Conditions.Broken) && !Is(unit, Conditions.Berserk) && !Is(unit, Conditions.Captured)
                && !Is(unit, Conditions.Melee));

    /// <summary>Whether a Good Order enemy ground unit has LOS to a Location, within a range when one is given (A12.2).</summary>
    private bool SeenByEnemy(GameState state, string side, BoardLocation at, int? within)
    {
        foreach (var watcher in state.Units.Where(unit => unit.Side != side && Watching(unit)))
        {
            if (state.Location(watcher.Id)?.Location is not { } from)
            {
                continue;
            }

            if (within is { } limit && (HexDistance(state, from, at) is not { } range || range > limit))
            {
                continue;
            }

            if (Los(state, from, at) is { IsBlocked: false })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The concealed or hidden vehicles that lose their "?" in a state (A12.2; ruling R6.7): the moving vehicle, when it entered a hex,
    /// changed its VCA, or moved in Motion within 16 hexes and in the LOS of a Good Order enemy ground unit; and any vehicle not in
    /// Concealment Terrain in the LOS of one (Case H).
    /// </summary>
    private List<string> VehicleConcealmentLost(GameState state, string? moving, bool moved)
    {
        var lost = new List<string>();
        foreach (var vehicle in state.Units.Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit)
            && (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))))
        {
            if (state.Location(vehicle.Id)?.Location is not { } at)
            {
                continue;
            }

            if ((vehicle.Id == moving && moved && SeenByEnemy(state, vehicle.Side, at, 16))
                || (!VehicleConcealmentTerrain(state, at) && SeenByEnemy(state, vehicle.Side, at, null)))
            {
                lost.Add(vehicle.Id);
            }
        }

        return lost;
    }

    /// <summary>The <c>concealment-lost</c> events that reveal units (A12.2, A12.41).</summary>
    private IEnumerable<GameEvent> RevealEvents(GameScope scope, string attemptId, long expected, int first, IEnumerable<string> ids) =>
        ids.Select((id, index) => Event(scope, attemptId, first + index, expected, "concealment-lost",
            new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }),
            null, null));

    /// <summary>
    /// A12.41 (ruling R6.8): a vehicle entering a Location of enemy units its side cannot see reveals their concealed and hidden Personnel,
    /// the owner's reveal in place of a PAATC, and removes their Dummies; the events follow the vehicle's step.
    /// </summary>
    private static IEnumerable<(string Type, EventPayload Payload)> EntryReveals(GameState state, UnitInstance vehicle, BoardLocation to)
    {
        foreach (var unit in state.At(to).OfType<UnitInstance>().Where(unit => unit.Side != vehicle.Side && !LiveFire.IsVehicle(unit))
            .OrderBy(unit => unit.Id, StringComparer.Ordinal))
        {
            if (unit.Kind == UnitKinds.Dummy)
            {
                yield return ("instance-eliminated", new InstanceEliminated(unit.Id));
            }
            else if (Is(unit, Conditions.Concealed) || Is(unit, Conditions.Hidden))
            {
                yield return ("concealment-lost", new ConditionsChanged(unit.Id,
                    new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False }));
            }
        }
    }
}
