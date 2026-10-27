using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPlacedDcWindowRulesTests
{
    private static TurnPlacedDcWindowFacts Facts => new(new("entry", 12, 1, "a", "afph"), "dc", "origin", 1, "a", true,
        new("dc", "origin", "entry", "a", "A23.4", new(TurnOpeningEvidence.Resolved,
            TurnOpeningEvidence.NotApplicable, TurnOpeningEvidence.Resolved, TurnOpeningEvidence.NotApplicable)));

    [Fact]
    public void AllRequiredFactsAreBoundAndNoPhaseLabelReopensAnOldWindow()
    {
        var f = Facts;
        Assert.Equal(TurnDecisionTiming.ReadyForCommandAssessment, TurnPlacedDcWindowRules.Assess(f));
        foreach (var missing in new[] { f with { Occurrence = null }, f with { OriginEventId = null }, f with { OriginOwner = null }, f with { Review = null } })
        {
            Assert.Equal(TurnDecisionTiming.NotEvaluated, TurnPlacedDcWindowRules.Assess(missing));
        }
        foreach (var outside in new[] { f with { Operable = false }, f with { OriginGameTurn = 2 }, f with { OriginOwner = "b" }, f with { Occurrence = f.Occurrence! with { Phase = "mph" } } })
        {
            Assert.Equal(TurnDecisionTiming.OutsideWindow, TurnPlacedDcWindowRules.Assess(outside));
        }
        var review = f.Review!;
        foreach (var invalid in new[] { review with { Charge = "other" }, review with { OriginEventId = "other" }, review with { PhaseEventId = "another-afph" },
            review with { Owner = "b" }, review with { RuleBasis = " " }, review with { Opening = null! },
            review with { Opening = review.Opening with { Drift = (TurnOpeningEvidence)99 } }, review with { Opening = review.Opening with { Wp = (TurnOpeningEvidence)99 } },
            review with { Opening = review.Opening with { Gusts = (TurnOpeningEvidence)99 } }, review with { Opening = review.Opening with { Gliders = (TurnOpeningEvidence)99 } } })
        {
            Assert.False(TurnPlacedDcWindowRules.Matches(f, invalid));
            Assert.Equal(TurnDecisionTiming.NotEvaluated, TurnPlacedDcWindowRules.Assess(f with
            {
                Review = invalid
            }));
        }
        foreach (var opening in new[] { review.Opening with { Drift = TurnOpeningEvidence.Unknown }, review.Opening with { Wp = TurnOpeningEvidence.Unknown },
            review.Opening with { Gusts = TurnOpeningEvidence.Unknown }, review.Opening with { Gliders = TurnOpeningEvidence.Unknown } })
        {
            var unknown = f with
            {
                Review = review with
                {
                    Opening = opening
                }
            };
            Assert.True(TurnPlacedDcWindowRules.Matches(unknown, unknown.Review));
            Assert.Equal(TurnDecisionTiming.NotEvaluated, TurnPlacedDcWindowRules.Assess(unknown));
        }
    }
}
