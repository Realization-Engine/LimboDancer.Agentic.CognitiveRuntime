using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private static GameState MaintainSpotterLosses(GameState? previous, GameState next, GameEvent gameEvent)
        {
            if (previous is null)
            {
                return next;
            }
            var losses = next.MortarSpotterLosses.ToList();
            foreach (var assignment in previous.MortarSpotters)
            {
                if (losses.Any(item => item.Gun == assignment.Gun && item.Spotter == assignment.Spotter))
                {
                    continue;
                }
                var before = previous.Unit(assignment.Spotter);
                var after = next.Unit(assignment.Spotter);
                var change = TurnSpotterLossRules.ObservedLoss(
                    before?.Status == InstanceStatus.Active, after?.Status == InstanceStatus.Eliminated,
                    after is not null && GameState.Condition(after, Conditions.Broken) == ConditionState.True,
                    after is not null && GameState.Condition(after, Conditions.Captured) == ConditionState.True);
                if (change != TurnSpotterChange.None)
                {
                    losses.Add(new(assignment.Gun, assignment.Spotter, gameEvent.EventId, gameEvent.Revision, change, previous.TurnPhaseOccurrence));
                }
            }
            return next with
            {
                MortarSpotterLosses = losses
            };
        }
    }
}
