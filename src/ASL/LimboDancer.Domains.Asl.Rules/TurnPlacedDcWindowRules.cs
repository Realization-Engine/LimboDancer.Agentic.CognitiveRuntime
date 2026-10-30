namespace LimboDancer.Domains.Asl.Rules;

public enum TurnOpeningEvidence
{
    Unknown, Resolved, NotApplicable
}
public enum TurnDecisionTiming
{
    NotEvaluated, OutsideWindow, ReadyForCommandAssessment
}

/// <summary>Journal identity of an actual phase entry, never just a phase name.</summary>
public sealed record TurnPhaseOccurrence(string EventId, long Revision, int GameTurn, string Owner, string Phase);

/// <summary>Externally adjudicated opening tasks, including consequences and waived optional choices.</summary>
public sealed record TurnAfphOpening(TurnOpeningEvidence Drift, TurnOpeningEvidence Wp,
    TurnOpeningEvidence Gusts, TurnOpeningEvidence Gliders);

public sealed record TurnPlacedDcReview(string Charge, string OriginEventId, string PhaseEventId,
    string Owner, string RuleBasis, TurnAfphOpening Opening);

public sealed record TurnPlacedDcWindowFacts(TurnPhaseOccurrence? Occurrence, string Charge,
    string? OriginEventId, int OriginGameTurn, string? OriginOwner, bool Operable, TurnPlacedDcReview? Review);

/// <summary>A23.3-.4; ASOP 5.11B, 5.12A, 5.13A, 5.21A, 5.22A. Timing evidence only, not attack eligibility.</summary>
public static class TurnPlacedDcWindowRules
{
    public static bool Valid(TurnOpeningEvidence value) => value is TurnOpeningEvidence.Unknown or TurnOpeningEvidence.Resolved or TurnOpeningEvidence.NotApplicable;

    public static bool Matches(TurnPlacedDcWindowFacts facts, TurnPlacedDcReview review) =>
        facts.Occurrence is { Phase: "afph" } occurrence
        && !string.IsNullOrWhiteSpace(facts.OriginEventId) && !string.IsNullOrWhiteSpace(facts.OriginOwner)
        && facts.OriginGameTurn == occurrence.GameTurn && facts.OriginOwner == occurrence.Owner
        && facts.Operable && review.Charge == facts.Charge && review.OriginEventId == facts.OriginEventId
        && review.PhaseEventId == occurrence.EventId && review.Owner == occurrence.Owner
        && !string.IsNullOrWhiteSpace(review.RuleBasis) && review.Opening is { } opening
        && Valid(opening.Drift) && Valid(opening.Wp) && Valid(opening.Gusts) && Valid(opening.Gliders);

    public static TurnDecisionTiming Assess(TurnPlacedDcWindowFacts facts)
    {
        if (facts.Occurrence is null || string.IsNullOrWhiteSpace(facts.OriginEventId) || string.IsNullOrWhiteSpace(facts.OriginOwner))
        {
            return TurnDecisionTiming.NotEvaluated;
        }
        // The window is A23.4's (the A23.3 claim, RA23.8): the owner's AFPh of the Game Turn it was Placed, operable. The opening's evidence below is the ASOP's.
        if (!ScenarioA1DemolitionChargeRules.DetonationWindow(facts.Occurrence.Phase, facts.Occurrence.Owner == facts.OriginOwner, facts.Operable,
            facts.Occurrence.GameTurn == facts.OriginGameTurn))
        {
            return TurnDecisionTiming.OutsideWindow;
        }
        if (facts.Review is not { } review || !Matches(facts, review)
            || review.Opening.Drift == TurnOpeningEvidence.Unknown || review.Opening.Wp == TurnOpeningEvidence.Unknown
            || review.Opening.Gusts == TurnOpeningEvidence.Unknown || review.Opening.Gliders == TurnOpeningEvidence.Unknown)
        {
            return TurnDecisionTiming.NotEvaluated;
        }
        return TurnDecisionTiming.ReadyForCommandAssessment;
    }
}
