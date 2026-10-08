using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>
/// A SMC freed in any way is Armed (A20.551: "Escaped SMC are always Armed"; ruling R31.8, the user's answer of 2026-10-04). Ruling R14.6 armed
/// a SMC again only when his own escape succeeded; a SMC freed by his side eliminating his Guard, abandoned by his Guard, or freed because no
/// captor could guard him stayed Unarmed. The planner now follows any plan that leaves a SMC free and Unarmed with the event that arms him, so a
/// game recorded before the ruling replays as it was played.
/// </summary>
public sealed partial class GamePlanner
{
    /// <summary>The SMC that are free (not prisoners) and still Unarmed.</summary>
    private UnitInstance[] FreedUnarmedSmc(GameState state) =>
        [.. state.Units.Where(unit => ScenarioA1RoutCalculator.FreedUnarmedSmc(unit.Status == InstanceStatus.Active, unit.Custodian is null, vocabulary.IsA(unit.Kind, "asl:smc"),
            Is(unit, Conditions.Unarmed), Is(unit, Conditions.Captured))).OrderBy(unit => unit.Id, StringComparer.Ordinal)];

    private void AddArmedSmc(GameScope scope, string attemptId, long expected, GameState state, List<GameEvent> events)
    {
        foreach (var unit in FreedUnarmedSmc(state))
        {
            events.Add(Event(scope, attemptId, events.Count + 1, expected, "conditions-changed",
                new ConditionsChanged(unit.Id, new Dictionary<string, ConditionState> { [Conditions.Unarmed] = ConditionState.False }), null, null));
        }
    }

    /// <summary>A plan whose events are followed by the arming of every SMC they leave free and Unarmed; a plan that rolls adds it once its rolls are drawn.</summary>
    private GamePlan WithArmedSmc(GamePlan plan, GameScope scope, IReadOnlyList<GameEvent> existing, string attemptId, long expected)
    {
        if (plan.Status != GamePlanStatus.Ready)
        {
            return plan;
        }

        if (plan.Roll is { } roll)
        {
            return plan with
            {
                Roll = roll with
                {
                    Build = draw =>
                    {
                        var events = roll.Build(draw).ToList();
                        if (ScenarioA1RoutCalculator.ArmsFreedSmc(events.Count, events.Count > 0 && events[^1].Payload is GameEnded or ChoicePending or SurrenderPending)
                            && Replay([.. existing, .. events]) is { HasErrors: false, Current: { } rolled })
                        {
                            AddArmedSmc(scope, attemptId, expected, rolled, events);
                        }

                        return events;
                    },
                },
            };
        }

        if (!ScenarioA1RoutCalculator.ArmsFreedSmc(plan.Events.Count, plan.Events.Count > 0 && plan.Events[^1].Payload is GameEnded or ChoicePending or SurrenderPending)
            || Replay([.. existing, .. plan.Events]) is not { HasErrors: false, Current: { } after } || FreedUnarmedSmc(after) is not { Length: > 0 } freed)
        {
            return plan;
        }

        var armed = plan.Events.ToList();
        AddArmedSmc(scope, attemptId, expected, after, armed);
        return plan with
        {
            Events = armed,
            Reasons = [.. plan.Reasons, ScenarioA1RoutCalculator.SmcArmedReason([.. freed.Select(unit => unit.Id)])],
        };
    }
}
