using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.Play;

/// <summary>An opaque current-state review offer, not an authorization credential.</summary>
public sealed record TurnOpeningReviewOffer(string Token, string SubjectId, string OwnerSide);
public sealed record TurnOpeningReviewRequest(TurnOpeningReviewOffer Offer, string RuleBasis, TurnAfphOpening Opening);

public sealed partial class GamePlanner
{
    private static string OpeningReviewToken(GameState state, string subject) =>
        Convert.ToHexString(HMACSHA256.HashData(TurnRouteKey, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            action = "opening-review",
            state.Stamp,
            subject
        }))));

    private static TurnPlacedDcReview? OpeningReview(GameState state, string subject, string basis, TurnAfphOpening opening)
    {
        if (state.Ended is not null || state.PlacedCharges.SingleOrDefault(item => item.Charge == subject) is not { } charge
            || state.TurnPhaseOccurrence is not { } phase || charge.OriginEventId is not { } origin || charge.OwnerSide is not { } owner)
        {
            return null;
        }
        var review = new TurnPlacedDcReview(subject, origin, phase.EventId, owner, basis, opening);
        return TurnPlacedDcWindowRules.Matches(PlacedDcWindowFacts.Read(state, charge), review) ? review : null;
    }

    private static ImmutableArray<TurnOpeningReviewOffer> OpeningReviewOffers(GameState state, Perspective perspective, bool historical, string browsedPhase)
    {
        if (!perspective.IsAdjudicator || historical || browsedPhase != "afph")
        {
            return [];
        }
        return [.. state.PlacedCharges.Where(charge => OpeningReview(state, charge.Charge, "Review requested",
                new(TurnOpeningEvidence.Unknown, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.Unknown, TurnOpeningEvidence.Unknown)) is not null)
            .Select(charge => new TurnOpeningReviewOffer(OpeningReviewToken(state, charge.Charge), charge.Charge, charge.OwnerSide!))];
    }

    /// <summary>Trusted host supplies the authenticated adjudicator perspective. The gate still requires permission and confirmation.</summary>
    public JsonElement? PrepareOpeningReview(GameScope scope, Perspective perspective, TurnOpeningReviewRequest request, string attemptId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(attemptId);
        if (!perspective.IsAdjudicator || store.Read(scope) is not { } record || Replay(record.Events).Current is not { } state
            || !OpeningReviewOffers(state, perspective, false, "afph").Contains(request.Offer)
            || OpeningReview(state, request.Offer.SubjectId, request.RuleBasis, request.Opening) is not { } review)
        {
            return null;
        }
        return JsonSerializer.SerializeToElement(new
        {
            gameId = scope.Game,
            attemptId,
            expectedRevision = state.Revision,
            review
        });
    }
}
