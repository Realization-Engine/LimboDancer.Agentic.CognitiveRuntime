using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The Play streamlining plan, S6 (ruling Q8): whether the DEFENDER could fire at the moving stack's latest step at all. The facts of each of his
/// units are read here, once a state and step, and Rules decides (<see cref="ScenarioA1DefensiveFireOpportunity"/>). The answer counts his concealed
/// and hidden units, so it belongs to his view and to passing for him.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Whether the DEFENDER could fire at the stack whose DEFENDER window is open, or owes Mandatory Fire there; false with no window open.
    /// <paramref name="from"/> is the Location the stack left for the step, for a Snap Shot's hexside (A8.15); null when it is not known.
    /// </summary>
    public bool DefenderMayFire(GameState state, IReadOnlyList<GameEvent> existing, BoardLocation? from)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(existing);
        if (state.Movement is not { WindowOpen: true } window || state.PhasingSide is not { } phasing)
        {
            return false;
        }

        return MemoOf(state).DefenderMayFire.GetOrAdd((window.Location, from), key =>
        {
            var firers = new List<DefensiveFirerFacts>();
            foreach (var unit in state.Units)
            {
                if (unit.Status != InstanceStatus.Active || unit.Side == phasing || state.Location(unit.Id)?.Location is not { } at)
                {
                    continue;
                }

                var vehicle = LiveFire.IsVehicle(unit);
                firers.Add(new DefensiveFirerFacts(unit.Id, unit.Kind == UnitKinds.Dummy, Is(unit, Conditions.Broken), FireBar(state, unit),
                    Sees(state, at, key.At), key.From is { } origin ? Sees(state, at, origin) : null, HexDistance(state, at, key.At),
                    vehicle ? null : NormalRange(state, unit), Is(unit, Conditions.FirstFire), Is(unit, Conditions.FinalFire)));
            }

            return ScenarioA1DefensiveFireOpportunity.MayFire(firers, MandatoryFire(state, existing) is not null);
        });
    }

    /// <summary>Whether a Location sees another: true when clear or hindered, false when blocked, null when the map cannot say.</summary>
    private bool? Sees(GameState state, BoardLocation from, BoardLocation to) => Los(state, from, to) switch
    {
        null => null,
        { Status: LosStatus.Blocked } => false,
        { Status: LosStatus.Clear } => true,
        _ => null,
    };
}
