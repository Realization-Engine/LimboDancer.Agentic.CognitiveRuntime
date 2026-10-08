using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// Shock and the Unconfirmed Kill (C7.42; ruling R7.8): in the RPh after it was placed, a Shocked AFV makes a dr: 1 or 2 removes the
/// Shock, 3 to 6 turns it into an Unconfirmed Kill; an Unconfirmed Kill's dr of 1 to 3 removes it and 4 to 6 wrecks the AFV, with no
/// Crew Survival. The RPh does not end until every such AFV has made its dr (the rule's "end of the RPh" is read as before the RPh ends).
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The Shocked AFVs and Unconfirmed Kills that still owe their dr this RPh.</summary>
    private static IEnumerable<UnitInstance> ShockRollsOwed(GameState state) => !ScenarioA1RallyRules.ShockRollPhase(state.Phase) ? [] : state.Units
        .Where(unit => ScenarioA1RallyRules.OwesShockRoll(unit.Status == InstanceStatus.Active, LiveFire.IsVehicle(unit), Is(unit, Conditions.Shocked), Is(unit, Conditions.UnconfirmedKill),
            state.ShockRollsThisPhase.Contains(unit.Id, StringComparer.Ordinal)))
        .OrderBy(unit => unit.Id, StringComparer.Ordinal);

    private GamePlan PlanRecoverShock(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing, string attemptId, long expected, string label,
        string actor)
    {
        if (Replay(existing).Current is not { } state)
        {
            return Refused(scope, label, expected, "play.no-game: the game has no state yet");
        }

        if (!Text(arguments, "vehicleId", out var vehicleId))
        {
            return Refused(scope, label, expected, "play.invalid-arguments: a Shock recovery names the vehicle");
        }

        if (ScenarioA1RallyRules.ShockPhaseBar(state.Phase) is { } phaseBar)
        {
            return Refused(scope, label, expected, phaseBar);
        }

        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !ShockRollsOwed(state).Any(item => item.Id == vehicle.Id))
        {
            return Refused(scope, label, expected, ScenarioA1RallyRules.ShockVehicleText(vehicleId));
        }

        var unconfirmed = Is(vehicle, Conditions.UnconfirmedKill);
        var package = ScenarioA1OrdnancePackage.Identity.ToString();
        IReadOnlyList<GameEvent> Build(Func<RollRequest, RollResult> draw)
        {
            var drawn = draw(new RollRequest(1, 6));
            var rollId = $"{attemptId}-roll-1";
            var result = ShockRecoveryRolled.For(unconfirmed, drawn.Values[0]);
            var events = new List<GameEvent>
            {
                Event(scope, attemptId, 1, expected, "dice-rolled", new DiceRolled(rollId, "shock-recovery", 1, 6, drawn.Values, DiceRolled.SystemSource, actor), package, null),
                Event(scope, attemptId, 2, expected, "shock-recovery-rolled", new ShockRecoveryRolled(vehicle.Id, rollId, result), package, null),
            };
            var record = EventId(attemptId, 2);
            if (ScenarioA1RallyRules.ShockRecoveryWrecks(result))
            {
                // C7.42: the Unconfirmed Kill is eliminated as a wreck, with no Crew Survival.
                events.Add(Event(scope, attemptId, 3, expected, "vehicle-wrecked", new VehicleWrecked(vehicle.Id, false), package, null, [record]));
            }
            else
            {
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, ConditionChanges(ScenarioA1RallyRules.ShockRecoveryConditions(result))),
                    package, null, [record]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [ScenarioA1RallyRules.ShockRecoverySummary(vehicle.Id, unconfirmed)])
        {
            Roll = new PlannedRoll("shock-recovery", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
