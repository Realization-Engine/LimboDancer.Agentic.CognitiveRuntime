using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.ScenarioA1;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// The backlog pass 14 actions outside a CC round: Ambush Withdrawal (A11.41; ruling R14.9), and a Guard transferring or abandoning its prisoners (A20.5;
/// ruling R14.7).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>
    /// Ambush Withdrawal (A11.41; ruling R14.9): in the CCPh, units of the side that ambushed, not pinned, berserk, or Disrupted, withdraw from the
    /// Location to one a withdrawal could reach, before its first round or once its CC is over, CX when the advance would need it.
    /// </summary>
    private GamePlan PlanAmbushWithdrawal(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var ids = Strings(arguments, "unitIds").ToArray();
        if (!Text(arguments, "location", out var locationText) || !BoardLocation.TryParse(locationText, out var location) || !Text(arguments, "to", out var toText)
            || !BoardLocation.TryParse(toText, out var to) || ids.Length == 0 || ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
        {
            return Refused(scope, label, expected, "play.invalid-arguments: an Ambush Withdrawal names its Location, its units, and where they go");
        }

        if (state.Phase != "ccph" || state.CloseCombats.FirstOrDefault(item => item.Location == location) is not { Ambusher: { } ambusher } entry
            || (entry.Rounds.Count > 0 && !entry.Closed))
        {
            return Refused(scope, label, expected,
                "play.ambush-withdraw-phase: an Ambush Withdrawal is made in the CCPh by the side that ambushed, before the Location's first round or once its CC is over (A11.41)");
        }

        var tiring = new List<string>();
        foreach (var id in ids)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || unit.Side != ambusher || state.Location(id)?.Location != location
                || new[] { Conditions.Pinned, Conditions.Berserk, Conditions.Disrupted, Conditions.Captured }.Any(condition => Is(unit, condition)) || LiveFire.IsVehicle(unit))
            {
                return Refused(scope, label, expected, $"play.ambush-withdraw-unit: {id} is not Infantry of the ambushing side in {location} that is free to withdraw (A11.41)");
            }

            if (!WithdrawalDestinations(state, unit, location).Contains(to))
            {
                return Refused(scope, label, expected, $"play.ambush-withdraw-to: {id} may withdraw only to an ADJACENT Location a withdrawal could reach (A11.41, A11.21)");
            }

            if (WithdrawalTires(state, unit, location, to))
            {
                tiring.Add(id);
            }
        }

        var package = ScenarioA1CloseCombatPackage.Identity.ToString();
        var events = new List<GameEvent>();
        foreach (var id in ids)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "instance-moved", new InstanceMoved(id, new MapPosition(to)), package, null));
        }

        foreach (var id in tiring)
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(id, new Dictionary<string, ConditionState> { [Conditions.Cx] = ConditionState.True }), package, null, [EventId(attemptId, 1)]));
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, events,
            [$"play.ambush-withdraw: {string.Join(", ", ids)} withdraw from {location} to {to} (A11.41)" + (tiring.Count > 0 ? $"; {string.Join(", ", tiring)} become CX (A4.72)" : string.Empty)]);
    }

    /// <summary>
    /// A Guard transferring or abandoning its prisoners (A20.5; ruling R14.7): in its side's RPh or APh, not held in Melee; a transfer goes to another armed
    /// unit of its side in its Location with Guard capacity, and an abandonment frees them as Unarmed units of their own side.
    /// </summary>
    private GamePlan PlanGuardPrisoners(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        var abandon = arguments.TryGetProperty("abandon", out var abandoning) && abandoning.ValueKind == JsonValueKind.True;
        if (!Text(arguments, "guardId", out var guardId) || state.Unit(guardId) is not { Status: InstanceStatus.Active } guard || (!abandon && !Text(arguments, "to", out _)))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Guard's prisoners are transferred to a named unit or abandoned");
        }

        UnitInstance[] prisoners = [.. state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Custodian == guard.Id).OrderBy(unit => unit.Id, StringComparer.Ordinal)];
        if (prisoners.Length == 0)
        {
            return Refused(scope, label, expected, $"play.guard-none: {guard.Id} guards no prisoners (A20.5)");
        }

        if (state.Phase is not ("rph" or "aph") || guard.Side != state.PhasingSide || Is(guard, Conditions.Melee))
        {
            return Refused(scope, label, expected, "play.guard-phase: a Guard hands over or abandons its prisoners in its own side's RPh or APh, when not held in Melee (A20.5)");
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        if (abandon)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [.. prisoners.Select((prisoner, index) => Event(scope, attemptId, index + 1, expected, "prisoner-freed", new PrisonerFreed(prisoner.Id), package, null))],
                [$"play.prisoners-abandoned: {guard.Id} abandons {string.Join(", ", prisoners.Select(unit => unit.Id))}, who are Unarmed units of their own side (A20.5, A20.53)"]);
        }

        Text(arguments, "to", out var toId);
        if (state.Unit(toId) is not { Status: InstanceStatus.Active } heir || heir.Id == guard.Id || heir.Side != guard.Side || state.Location(heir.Id)?.Location != state.Location(guard.Id)?.Location
            || Is(heir, Conditions.Captured) || Is(heir, Conditions.Unarmed) || Is(heir, Conditions.Melee) || !(vocabulary.IsA(heir.Kind, "asl:mmc") || vocabulary.IsA(heir.Kind, "asl:smc")))
        {
            return Refused(scope, label, expected, $"play.guard-heir: {toId} is not an armed Personnel unit of {guard.Side} in {guard.Id}'s Location, free of Melee (A20.5)");
        }

        if (GuardLoad(state, heir) + prisoners.Sum(UnitSize) > 5 * UnitSize(heir))
        {
            return Refused(scope, label, expected, $"play.guard-capacity: {heir.Id} can guard prisoners of at most five times its US# (A20.51)");
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [.. prisoners.Select((prisoner, index) => Event(scope, attemptId, index + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, heir.Id), package, null))],
            [$"play.prisoners-transferred: {guard.Id} hands {string.Join(", ", prisoners.Select(unit => unit.Id))} to {heir.Id} (A20.5, A4.431)"]);
    }
}
