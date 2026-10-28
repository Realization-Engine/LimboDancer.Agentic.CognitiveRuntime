using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    internal sealed partial class Replay
    {
        private GameState MaintainSpotterLosses(GameState? previous, GameState next, GameEvent gameEvent)
        {
            if (previous is null)
            {
                return next;
            }
            var reduced = gameEvent.Payload as LineageRecorded;
            var predecessor = reduced?.Consumed.Count == 1 ? previous.Unit(reduced.Consumed[0]) : null;
            var survivor = reduced?.Produced.Count == 1 ? next.Unit(reduced.Produced[0].Id)
                : reduced?.SpotterSuccessor is { } chosen ? next.Unit(chosen) : null;
            var inherited = new HashSet<string>(StringComparer.Ordinal);
            foreach (var assignment in previous.MortarSpotters.Where(item => item.Spotter == predecessor?.Id))
            {
                if (TurnSpotterLossRules.RetainsReducedSquad(reduced?.Action == LineageAction.Reduced && predecessor?.Kind == "asl:squad",
                    survivor is { Kind: "asl:half-squad", Status: InstanceStatus.Active }, survivor?.Side == predecessor?.Side,
                    survivor is not null && GameState.GoodOrder(survivor, vocabulary) == ConditionState.True,
                    previous.MortarSpotterLosses.Any(item => item.Gun == assignment.Gun && item.Spotter == assignment.Spotter))
                    || TurnSpotterLossRules.RetainsDeployedSquad(reduced?.Action == LineageAction.Deployed && predecessor?.Kind == "asl:squad",
                        survivor is { Kind: "asl:half-squad", Status: InstanceStatus.Active } && reduced!.Produced.Any(item => item.Id == survivor.Id),
                        survivor?.Side == predecessor?.Side,
                        survivor is not null && GameState.GoodOrder(survivor, vocabulary) == ConditionState.True,
                        previous.MortarSpotterLosses.Any(item => item.Gun == assignment.Gun && item.Spotter == assignment.Spotter)))
                {
                    inherited.Add(assignment.Gun);
                    next = next with
                    {
                        MortarSpotters = [.. next.MortarSpotters.Select(item => item == assignment ? item with { Spotter = survivor!.Id } : item)],
                        MortarSpotterSuccessors = [.. next.MortarSpotterSuccessors,
                            new(assignment.Gun, assignment.Spotter, survivor!.Id, gameEvent.EventId, gameEvent.Revision)],
                        SpottedThisPhase = [.. next.SpottedThisPhase.Select(item => item == assignment ? item with { Spotter = survivor.Id } : item)],
                        MortarSpotterUses = [.. next.MortarSpotterUses.Select(item => item.Spotter == assignment.Spotter
                            ? item with { Spotter = survivor.Id } : item)],
                        NoMoveThisPlayerTurn = previous.NoMoveThisPlayerTurn.Contains(assignment.Spotter, StringComparer.Ordinal)
                            ? [.. next.NoMoveThisPlayerTurn.Concat(reduced!.Produced.Select(item => item.Id)).Distinct(StringComparer.Ordinal)]
                            : next.NoMoveThisPlayerTurn,
                        SupportWeaponUses = [.. next.SupportWeaponUses.Select(item => item.Unit == assignment.Spotter
                            ? item with { Unit = survivor.Id } : item)],
                    };
                }
            }
            var losses = next.MortarSpotterLosses.ToList();
            foreach (var assignment in previous.MortarSpotters)
            {
                if (inherited.Contains(assignment.Gun) || losses.Any(item => item.Gun == assignment.Gun && item.Spotter == assignment.Spotter))
                {
                    continue;
                }
                var before = previous.Unit(assignment.Spotter);
                var after = predecessor?.Id == assignment.Spotter && reduced?.Action == LineageAction.Reduced
                    ? survivor : next.Unit(assignment.Spotter);
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
