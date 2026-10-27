using Xunit;

namespace ASL.Turns.Tests;

public sealed class ScenarioGuidanceTests
{
    [Theory]
    [InlineData("mph", "mph:reaction", "mph:asop:3.36D", TurnApplicability.Available, TurnAttentionKind.OptionalChoice)]
    [InlineData("rtph", "rtph:mustRout", "rtph:asop:6.21B", TurnApplicability.Required, TurnAttentionKind.RequiredDecision)]
    public void MovementAndRoutConsumeHostVerdictsWithoutRuleCalculations(string phase, string topic,
        string window, TurnApplicability applicability, TurnAttentionKind attention)
    {
        var catalog = TurnGuideCatalog.Load();
        var context = new TurnEvaluationContext("game", "viewer", "side-a", "base", phase, window, "internal-stamp");
        var capability = new TurnCapability("fixture", topic, "base", TurnSourceReview.Reconciled,
            TurnSupport.IntegratedSubset, TurnSupport.IntegratedSubset, ["fixture-proof"]);
        var service = new TurnGuideService(catalog, [capability]);
        var verdict = new TurnGuideVerdict("decision", "viewer", "fixture", "unit-1", window, "side-a", applicability, "Safe reason", "opaque-route");
        var result = service.Describe(new TurnGuideRequest(context, phase, false), new TurnFacts(context, [verdict]));
        var option = Assert.Single(result.Options, item => item.Topic.Id == topic);
        var decision = Assert.Single(option.Decisions);
        Assert.Equal(attention, decision.Attention);
        Assert.Equal("side-a", decision.DecisionOwnerSideId);
        Assert.Equal("opaque-route", decision.ActionRoute);
        Assert.Equal(catalog.Phases.Single(item => item.Id == phase).Topics.Length, result.Options.Length);
        Assert.Contains("do not establish complete ASL legality", result.CoverageNote, StringComparison.Ordinal);
    }
}
