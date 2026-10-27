using System.Collections.Immutable;
using System.Text.Json;
using ASL.Turns;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

public sealed partial class GamePlanner
{
    private GamePlan PlanPlacedDcWindow(GameScope scope, JsonElement arguments, IReadOnlyList<GameEvent> existing,
        string attemptId, long expected, string label)
    {
        TurnPlacedDcReview? review = null;
        try
        {
            if (arguments.TryGetProperty("review", out var node))
            {
                review = node.Deserialize<TurnPlacedDcReview>(JsonSerializerOptions.Web);
            }
        }
        catch (JsonException)
        {
            return Refused(scope, label, expected, "play.window: invalid typed review");
        }
        if (review is null)
        {
            return Refused(scope, label, expected, "play.window: review required");
        }
        var proposed = Event(scope, attemptId, 1, expected, "placed-dc-window-reviewed",
            new PlacedDcWindowReviewed(review), "asl.turn-window.v1", [review.Owner]);
        if (Replay([.. existing, proposed]).HasErrors)
        {
            return Refused(scope, label, expected, "play.window: invalid occurrence, placement or adjudication evidence");
        }
        return new(GamePlanStatus.Ready, scope, label, expected, [proposed],
            ["play.window: externally adjudicated opening tasks; pending rules and command checks remain"])
        {
            FirstEventId = EventId(attemptId, 1)
        };
    }

    private ImmutableArray<TurnDecisionWindow> ReadDecisionWindows(GameHistory history, long revision, Perspective perspective, bool historical)
    {
        var state = history.At(revision)!;
        var visible = ViewOf(history, revision, perspective).Equipment.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        var events = history.Events.Where(item => item.Revision <= revision).ToDictionary(item => item.EventId, StringComparer.Ordinal);
        return [.. state.PlacedCharges.Where(charge => perspective.IsAdjudicator
            || charge.OwnerSide == perspective.Name && visible.Contains(charge.Charge)
            && charge.OriginEventId is { } origin && events[origin].IsVisibleTo(perspective))
            .Select(charge =>
            {
                var facts = PlacedDcWindowFacts.Read(state, charge);
                // A withheld boundary or review is missing evidence, never an implicit approval.
                if (facts.Occurrence is { } occurrence && !events[occurrence.EventId].IsVisibleTo(perspective))
                {
                    facts = facts with { Occurrence = null };
                }
                if (!history.Events.Single(item => item.Revision == revision).IsVisibleTo(perspective))
                {
                    facts = facts with { Review = null };
                }
                return TurnDecisionWindows.Describe(facts, historical);
            })];
    }
}
