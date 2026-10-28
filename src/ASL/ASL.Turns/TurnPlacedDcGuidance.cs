using System.Collections.Immutable;
using LimboDancer.Domains.Asl.Rules;

namespace ASL.Turns;

public enum TurnCommandReadiness
{
    NotEvaluated, Ready, Refused, GovernedCheckRequired
}

/// <summary>Opaque state-bound host route. Not authorization and never a raw command plan.</summary>
public sealed record TurnPlacedDcAction(string Token, string SubjectId, string Profile, string ActionId);

public sealed record TurnPlacedDcAssessment(string SubjectId, string OwnerSideId, TurnDecisionTiming Timing,
    TurnCommandReadiness Command, string? Route);

public static class TurnPlacedDcGuidance
{
    public const string TopicId = "afph:placed";
    public const string WindowId = "afph:asop:5.22A";
    public const string ActionId = "asl.game.detonate-dc";
    public static ImmutableArray<TurnCapability> Registrations
    {
        get;
    } =
        [.. new[] { "existing-engine-dc-v1", "existing-engine-fire-v1", "existing-engine-atmosphere-v1", "existing-engine-work-v1" }
            .Select(profile => new TurnCapability("placed-dc:" + profile, TopicId, profile, TurnSourceReview.Reconciled,
                TurnSupport.IntegratedSubset, TurnSupport.IntegratedSubset,
                ["A23.3-.4; ASOP 5.22A", "TurnPlacedDcWindowRules; GamePlanner.PlanCoreAsync; BacklogPass15Tests.PlacedDcRoute"]))];

    public static TurnGuideVerdict Describe(TurnEvaluationContext context, TurnPlacedDcAssessment assessment)
    {
        var applicability = assessment.Timing switch
        {
            TurnDecisionTiming.OutsideWindow => TurnApplicability.Unavailable,
            TurnDecisionTiming.ReadyForCommandAssessment => assessment.Command switch
            {
                TurnCommandReadiness.Ready or TurnCommandReadiness.GovernedCheckRequired => TurnApplicability.Required,
                TurnCommandReadiness.Refused => TurnApplicability.Unavailable,
                _ => TurnApplicability.NotEvaluated,
            },
            _ => TurnApplicability.NotEvaluated,
        };
        var reason = applicability switch
        {
            TurnApplicability.Required when assessment.Command == TurnCommandReadiness.GovernedCheckRequired =>
                "This Placed DC has timing evidence. Request the governed command check; private decisions may remain. Permission and confirmation are still required.",
            TurnApplicability.Required => "This Placed DC has current timing evidence and passes the existing command assessment. Proposal and confirmation are required.",
            TurnApplicability.Unavailable => "This Placed DC cannot currently be offered through the governed route. Resolve its timing or existing game obligations first.",
            _ => "Current timing or command evidence is missing. Opening-rule automation and other unregistered rules remain pending.",
        };
        return new("placed-dc:" + assessment.SubjectId, context.ViewerId, "placed-dc:" + context.Profile,
            assessment.SubjectId, WindowId, assessment.OwnerSideId, applicability, reason,
            applicability == TurnApplicability.Required ? assessment.Route : null);
    }
}
