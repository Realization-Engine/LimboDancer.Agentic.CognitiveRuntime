using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.Units.State;

public static partial class GameProjector
{
    private static (string Key, int Sequence) SpotterCommandIdentity(GameEvent item)
    {
        var cut = item.EventId.LastIndexOf('-');
        return cut > 0 && int.TryParse(item.EventId.AsSpan(cut + 1), System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture, out var sequence) && sequence > 0
            ? (item.Source + ":" + item.EventId[..cut], sequence) : (item.Source + ":" + item.EventId, 0);
    }

    internal static bool SameSpotterCommand(GameEvent before, GameEvent after)
    {
        var first = SpotterCommandIdentity(before);
        var second = SpotterCommandIdentity(after);
        return first.Sequence > 0 && first.Sequence < int.MaxValue && first.Key == second.Key && second.Sequence == first.Sequence + 1;
    }

    internal sealed partial class Replay
    {
        private GameState MaintainSpotterOpening(GameState? previous, GameState next, GameEvent gameEvent)
        {
            var (key, index) = SpotterCommandIdentity(gameEvent);
            if (gameEvent.Payload is PhaseChanged or GameStarted)
            {
                // Some imported fixtures number the entire journal as one attempt. Several phase boundaries cannot establish a
                // single phase-change command, so retain the calculator's conservative step instead of excluding all later activity.
                var ambiguous = spotterCommandTypes.Count(type => type is "phase-changed" or "game-started") > 1;
                return next with
                {
                    SpotterOpening = new(key, index, !ambiguous, spotterCommandTypes,
                        ambiguous && next.Phase == "mph" ? [ScenarioA1SpotterCalculator.OpeningStepOf(spotterCommandTypes)] : [])
                };
            }
            if (next.Phase != "mph")
            {
                return next;
            }
            var opening = previous?.SpotterOpening ?? next.SpotterOpening;
            var continuing = index > 0 && opening.Sequence > 0 && opening.Sequence < int.MaxValue
                && index == opening.Sequence + 1 && key == opening.Command;
            if (continuing && opening.PhaseChangeBatch)
            {
                return next with
                {
                    SpotterOpening = opening with
                    {
                        Sequence = index
                    }
                };
            }
            var types = spotterCommandTypes;
            var step = ScenarioA1SpotterCalculator.OpeningStepOf(types);
            IReadOnlyList<MovementOpeningStep> steps = continuing && opening.Steps.Count > 0
                ? [.. opening.Steps.Take(opening.Steps.Count - 1), step] : [.. opening.Steps, step];
            return next with
            {
                SpotterOpening = new(key, index, false, types, steps)
            };
        }

        /// <summary>Whether a squad holds a Spotter assignment the record has not lost (C9.3; ruling RC93.6, the calculator's reading).</summary>
        private static bool HoldsSpotterAssignment(GameState state, string squadId) => state.MortarSpotters.Any(item => item.Spotter == squadId
            && ScenarioA1SpotterCalculator.HoldsAssignment(true, state.ActiveSpotterLoss(item.Gun, item.Spotter) is not null));

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
                if (ScenarioA1SpotterCalculator.RetainsReducedSquad(reduced?.Action == LineageAction.Reduced && predecessor?.Kind == "asl:squad",
                    survivor is { Kind: "asl:half-squad", Status: InstanceStatus.Active }, survivor?.Side == predecessor?.Side,
                    survivor is not null && GameState.GoodOrder(survivor, vocabulary) == ConditionState.True,
                    previous.ActiveSpotterLoss(assignment.Gun, assignment.Spotter) is not null)
                    || ScenarioA1SpotterCalculator.RetainsDeployedSquad(reduced?.Action == LineageAction.Deployed && predecessor?.Kind == "asl:squad",
                        survivor is { Kind: "asl:half-squad", Status: InstanceStatus.Active } && reduced!.Produced.Any(item => item.Id == survivor.Id),
                        survivor?.Side == predecessor?.Side,
                        survivor is not null && GameState.GoodOrder(survivor, vocabulary) == ConditionState.True,
                        previous.ActiveSpotterLoss(assignment.Gun, assignment.Spotter) is not null))
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
                if (inherited.Contains(assignment.Gun) || previous.ActiveSpotterLoss(assignment.Gun, assignment.Spotter) is not null)
                {
                    continue;
                }
                var before = previous.Unit(assignment.Spotter);
                var after = predecessor?.Id == assignment.Spotter && reduced?.Action == LineageAction.Reduced
                    ? survivor : next.Unit(assignment.Spotter);
                var change = ScenarioA1SpotterCalculator.ObservedLoss(
                    before?.Status == InstanceStatus.Active, after?.Status == InstanceStatus.Eliminated,
                    after is not null && GameState.Condition(after, Conditions.Broken) == ConditionState.True,
                    after is not null && GameState.Condition(after, Conditions.Captured) == ConditionState.True);
                if (change != SpotterChange.None)
                {
                    losses.Add(new(assignment.Gun, assignment.Spotter, gameEvent.EventId, gameEvent.Revision, change, previous.TurnPhaseOccurrence)
                    {
                        PlayerTurnOrdinal = previous.PlayerTurnOrdinal
                    });
                }
            }
            return next with
            {
                MortarSpotterLosses = losses
            };
        }
    }
}
