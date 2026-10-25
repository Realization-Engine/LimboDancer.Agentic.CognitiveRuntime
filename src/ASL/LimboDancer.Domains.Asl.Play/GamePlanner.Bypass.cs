using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Bypass and detection (pass 70, task 70.8; ruling R70.23): the units of a Bypassed Location are read here, through the state's index, and
/// <see cref="ScenarioA1BypassDetection"/> decides who loses "?"; the reveals are public <c>concealment-lost</c> events, and a Dummy's loss its removal.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>One unit as the Bypass reveals read it.</summary>
    private static BypassLocationUnitFacts BypassFacts(UnitInstance unit) =>
        new(unit.Id, unit.Status == InstanceStatus.Active, unit.Side, Is(unit, Conditions.Captured), Is(unit, Conditions.Concealed), Is(unit, Conditions.Hidden),
            unit.Kind == UnitKinds.Dummy);

    /// <summary>The units of a Location as the Bypass reveals read them, from the state's index.</summary>
    private BypassLocationUnitFacts[] BypassLocation(GameState state, BoardLocation at) =>
        [.. MemoOf(state).Objects(state).At(at).OfType<UnitInstance>().Select(BypassFacts)];

    /// <summary>The events of a Bypass reveal, appended in order: each real unit's <c>concealment-lost</c>, then each Dummy's removal.</summary>
    private void AddBypassReveal(GameScope scope, string attemptId, long expected, List<GameEvent> events, BypassRevealVerdict reveal)
    {
        events.AddRange(RevealEvents(scope, attemptId, expected, events.Count + 1, reveal.Revealed));
        foreach (var dummy in reveal.DummiesRemoved)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-eliminated", new InstanceEliminated(dummy), ScenarioA1FirePackage.Identity.ToString(), null));
        }
    }

    /// <summary>
    /// A12.151 (p. 79): as the DEFENDER's window closes, a member of the Bypassing Infantry stack broken or pinned there ends its MPh in the Bypass Location;
    /// with a concealed enemy unit there, every "?" of the Location is lost. None when the stack is not in Bypass.
    /// </summary>
    private BypassRevealVerdict EndedInBypassReveal(GameState state, MovementState movement)
    {
        if (movement.Bypass is not { Count: > 0 } || state.PhasingSide is not { } phasing)
        {
            return BypassRevealVerdict.None;
        }

        var index = MemoOf(state).Objects(state);
        return ScenarioA1BypassDetection.EndsInBypass(true, movement.Vehicle, movement.Movers.Select(index.Unit).OfType<UnitInstance>()
                .Select(unit => (unit.Status == InstanceStatus.Active, Is(unit, Conditions.Broken), Is(unit, Conditions.Pinned))))
            ? ScenarioA1BypassDetection.AllConcealmentLost(BypassLocation(state, movement.Location), phasing, false, [])
            : BypassRevealVerdict.None;
    }

    /// <summary>
    /// A12.42 (p. 80): a vehicle in Bypass (D2.3, straddling a hexside) that ends its MPh reveals the concealed enemy units of its Location, unless it is
    /// "broken" (A12.1: no inherent crew and no PRC). Hidden units stay hidden.
    /// </summary>
    private BypassRevealVerdict VehicleBypassReveal(GameState state, UnitInstance vehicle)
    {
        var index = MemoOf(state).Objects(state);
        return index.Location(vehicle.Id)?.Location is { } at
            && ScenarioA1BypassDetection.VehicleEndRevealsInBypass(vehicle.Straddling is not null, index.Aboard(vehicle.Id).Count > 0, () => HasInherentCrew(state, vehicle))
            ? ScenarioA1BypassDetection.VehicleReveals(BypassLocation(state, at), vehicle.Side)
            : BypassRevealVerdict.None;
    }
}
