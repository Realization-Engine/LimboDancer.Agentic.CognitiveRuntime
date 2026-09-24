namespace LimboDancer.Domains.Asl.Authoring;

public static class TirReviewTransitionRules
{
    public static void ValidateShape(
        TirReviewDecisionRecord transition,
        TirReviewStatus effectiveStatus,
        TirArtifactOrigin origin,
        string? semanticAuthorIdentity)
    {
        ArgumentNullException.ThrowIfNull(transition);
        _ = TirReviewCanonicalJson.SerializePayload(transition);
        if (origin == TirArtifactOrigin.Extracted)
        {
            throw new InvalidOperationException("Captured extraction cannot transition on the same artifact revision.");
        }

        if (transition.PriorStatus != effectiveStatus)
        {
            throw new InvalidOperationException("Review transition prior status does not match effective status.");
        }

        var legal = (effectiveStatus, transition.RequestedStatus) switch
        {
            (TirReviewStatus.Proposed, TirReviewStatus.InReview) => true,
            (TirReviewStatus.InReview, TirReviewStatus.Accepted) => true,
            (TirReviewStatus.InReview, TirReviewStatus.Rejected) => true,
            (TirReviewStatus.Accepted, TirReviewStatus.Superseded) => true,
            _ => false,
        };
        if (!legal)
        {
            throw new InvalidOperationException("Illegal review-state transition.");
        }

        if (transition.RequestedStatus == TirReviewStatus.Accepted)
        {
            if (transition.Decision != TirReviewDecision.Approve
                || string.Equals(
                    semanticAuthorIdentity,
                    transition.Actor.Identity,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Acceptance requires a domain-review approval independent of the semantic author.");
            }
        }

        if (transition.RequestedStatus == TirReviewStatus.Superseded
            && transition.ReviewedDependencyRefs.Count == 0)
        {
            throw new InvalidOperationException(
                "Supersession requires an explicit replacement relationship.");
        }
    }
}
