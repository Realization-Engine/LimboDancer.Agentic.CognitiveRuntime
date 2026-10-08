using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Rules;
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

        var entry = state.CloseCombats.FirstOrDefault(item => item.Location == location);
        if (ScenarioA1PrisonerCalculator.AmbushWithdrawalPhaseBar(state.Phase, entry?.Ambusher is not null, entry is { } begun && begun.Rounds.Count > 0 && !begun.Closed) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var ambusher = entry!.Ambusher!;

        var tiring = new List<string>();
        foreach (var id in ids)
        {
            if (state.Unit(id) is not { Status: InstanceStatus.Active } unit || !ScenarioA1PrisonerCalculator.AmbushWithdrawer(true, unit.Side == ambusher, state.Location(id)?.Location == location,
                Is(unit, Conditions.Pinned), Is(unit, Conditions.Berserk), Is(unit, Conditions.Disrupted), Is(unit, Conditions.Captured), LiveFire.IsVehicle(unit)))
            {
                return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.AmbushWithdrawerText(id, location.ToString()));
            }

            if (ScenarioA1PrisonerCalculator.AmbushWithdrawalToBar(id, WithdrawalDestinations(state, unit, location).Contains(to)) is { } toBar)
            {
                return Refused(scope, label, expected, toBar);
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
            [ScenarioA1PrisonerCalculator.AmbushWithdrawalSummary(ids, location.ToString(), to.ToString(), tiring)]);
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
            return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.GuardNoneText(guard.Id));
        }

        if (ScenarioA1PrisonerCalculator.GuardPhaseBar(state.Phase, guard.Side == state.PhasingSide, Is(guard, Conditions.Melee)) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        var package = ScenarioA1FirePackage.Identity.ToString();
        if (abandon)
        {
            return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
                [.. prisoners.Select((prisoner, index) => Event(scope, attemptId, index + 1, expected, "prisoner-freed", new PrisonerFreed(prisoner.Id), package, null))],
                [ScenarioA1PrisonerCalculator.PrisonersAbandonedText(guard.Id, [.. prisoners.Select(unit => unit.Id)])]);
        }

        Text(arguments, "to", out var toId);
        if (state.Unit(toId) is not { Status: InstanceStatus.Active } heir || !ScenarioA1PrisonerCalculator.Heir(true, heir.Id == guard.Id, heir.Side == guard.Side,
            state.Location(heir.Id)?.Location == state.Location(guard.Id)?.Location, Is(heir, Conditions.Captured), Is(heir, Conditions.Unarmed), Is(heir, Conditions.Melee),
            vocabulary.IsA(heir.Kind, "asl:mmc") || vocabulary.IsA(heir.Kind, "asl:smc")))
        {
            return Refused(scope, label, expected, ScenarioA1PrisonerCalculator.HeirText(toId, guard.Side, guard.Id));
        }

        if (ScenarioA1PrisonerCalculator.GuardCapacityBar(heir.Id, ScenarioA1PrisonerCalculator.CanGuard(GuardLoad(state, heir), prisoners.Sum(UnitSize), UnitSize(heir))) is { } capacityBar)
        {
            return Refused(scope, label, expected, capacityBar);
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected,
            [.. prisoners.Select((prisoner, index) => Event(scope, attemptId, index + 1, expected, "instance-captured", new InstanceCaptured(prisoner.Id, heir.Id), package, null))],
            [ScenarioA1PrisonerCalculator.PrisonersTransferredText(guard.Id, [.. prisoners.Select(unit => unit.Id)], heir.Id)]);
    }
}
