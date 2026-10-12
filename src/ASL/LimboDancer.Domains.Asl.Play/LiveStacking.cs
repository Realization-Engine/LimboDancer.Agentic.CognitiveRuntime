using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The facts of a side's stack in a Location, read from the game for the stacking limits (A5; pass 40). Rules counts them
/// (<see cref="ScenarioA1Stacking"/>); nothing is decided here.
/// </summary>
public static class LiveStacking
{
    /// <summary>
    /// One side's stack in a Location: its active counters there, and any units about to arrive. Passengers are not in the Location's
    /// stack (<see cref="GameState.At"/> leaves them out; A5.3), and a wreck is not active.
    /// </summary>
    public static StackCount Of(GameState state, BoardLocation at, string side, IEnumerable<UnitInstance>? arriving = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        var units = state.At(at).OfType<UnitInstance>().Where(unit => unit.Status == InstanceStatus.Active && unit.Side == side)
            .Concat(arriving ?? []).DistinctBy(unit => unit.Id).ToArray();
        return ScenarioA1Stacking.Count(units.Select(unit => Facts(state, unit)));
    }

    /// <summary>A unit as the stacking limits read it: its kind, whether it mans a Gun (A5.5), and whether it is a prisoner (A20.51).</summary>
    public static StackUnitFacts Facts(GameState state, UnitInstance unit)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(unit);
        return new StackUnitFacts(ScenarioA1Stacking.KindOf(unit.Kind), Manning(state, unit.Id), GameState.Condition(unit, Conditions.Captured) == ConditionState.True);
    }

    /// <summary>Whether the unit mans a Gun.</summary>
    public static bool Manning(GameState state, string unitId)
    {
        ArgumentNullException.ThrowIfNull(state);
        return state.Equipment.Any(item => item.Status == InstanceStatus.Active && item.Kind == "asl:gun" && item.Holding is { Role: HoldingRole.Manned } manned && manned.Holder == unitId);
    }
}
