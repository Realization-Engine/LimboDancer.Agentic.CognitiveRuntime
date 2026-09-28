using System.Text.Json;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.ScenarioA1;
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
    private static IEnumerable<UnitInstance> ShockRollsOwed(GameState state) => state.Phase != "rph" ? [] : state.Units
        .Where(unit => unit.Status == InstanceStatus.Active && LiveFire.IsVehicle(unit) && (Is(unit, Conditions.Shocked) || Is(unit, Conditions.UnconfirmedKill))
            && !state.ShockRollsThisPhase.Contains(unit.Id, StringComparer.Ordinal))
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

        if (state.Phase != "rph")
        {
            return Refused(scope, label, expected, "play.shock-phase: a Shocked AFV or an Unconfirmed Kill makes its dr in the RPh (C7.42)");
        }

        if (state.Unit(vehicleId) is not { Status: InstanceStatus.Active } vehicle || !ShockRollsOwed(state).Any(item => item.Id == vehicle.Id))
        {
            return Refused(scope, label, expected, $"play.shock-vehicle: '{vehicleId}' is not a Shocked AFV or an Unconfirmed Kill that still owes its dr this RPh (C7.42)");
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
            if (result == ShockRecoveryRolled.Wrecked)
            {
                // C7.42: the Unconfirmed Kill is eliminated as a wreck, with no Crew Survival.
                events.Add(Event(scope, attemptId, 3, expected, "vehicle-wrecked", new VehicleWrecked(vehicle.Id, false), package, null, [record]));
            }
            else
            {
                events.Add(Event(scope, attemptId, 3, expected, "conditions-changed", new ConditionsChanged(vehicle.Id, new Dictionary<string, ConditionState>(StringComparer.Ordinal)
                {
                    [Conditions.Shocked] = ConditionState.False,
                    [Conditions.UnconfirmedKill] = result == ShockRecoveryRolled.UnconfirmedKill ? ConditionState.True : ConditionState.False,
                }), package, null, [record]));
            }

            return events;
        }

        return new GamePlan(GamePlanStatus.Ready, scope, label, expected, [],
            [unconfirmed
                ? $"play.shock-recovery: {vehicle.Id} is an Unconfirmed Kill: a dr of 1 to 3 removes it, 4 to 6 wrecks the AFV (C7.42)"
                : $"play.shock-recovery: {vehicle.Id} is Shocked: a dr of 1 or 2 removes the Shock, 3 to 6 makes it an Unconfirmed Kill (C7.42)"])
        {
            Roll = new PlannedRoll("shock-recovery", Build),
            FirstEventId = EventId(attemptId, 1),
        };
    }
}
