using System.Text.Json;
using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnPlacedDcGuidanceTests
{
    [Theory]
    [InlineData(TurnDecisionTiming.NotEvaluated, TurnCommandReadiness.Ready, TurnApplicability.NotEvaluated)]
    [InlineData(TurnDecisionTiming.OutsideWindow, TurnCommandReadiness.Ready, TurnApplicability.Unavailable)]
    [InlineData(TurnDecisionTiming.ReadyForCommandAssessment, TurnCommandReadiness.NotEvaluated, TurnApplicability.NotEvaluated)]
    [InlineData(TurnDecisionTiming.ReadyForCommandAssessment, TurnCommandReadiness.Refused, TurnApplicability.Unavailable)]
    [InlineData(TurnDecisionTiming.ReadyForCommandAssessment, TurnCommandReadiness.Ready, TurnApplicability.Required)]
    [InlineData(TurnDecisionTiming.ReadyForCommandAssessment, TurnCommandReadiness.GovernedCheckRequired, TurnApplicability.Required)]
    [InlineData((TurnDecisionTiming)99, TurnCommandReadiness.Ready, TurnApplicability.NotEvaluated)]
    [InlineData(TurnDecisionTiming.ReadyForCommandAssessment, (TurnCommandReadiness)99, TurnApplicability.NotEvaluated)]
    public void TimingAndCommandEvidenceAreBothRequired(TurnDecisionTiming timing, TurnCommandReadiness command, TurnApplicability expected)
    {
        var context = new TurnEvaluationContext("game", "viewer", "a", "existing-engine-dc-v1", "afph", TurnPlacedDcGuidance.WindowId, "stamp");
        var facts = new TurnPlacedDcAssessment("dc", "a", timing, command, "opaque");
        var verdict = TurnPlacedDcGuidance.Describe(context, facts);
        Assert.Equal(expected, verdict.Applicability);
        Assert.Equal(expected == TurnApplicability.Required ? "opaque" : null, verdict.ActionRoute);
        Assert.Equal("a", verdict.DecisionOwnerSideId);
        Assert.Equal("dc", verdict.SubjectId);
        Assert.Equal(context.ViewerId, verdict.ViewerId);
        Assert.NotEmpty(verdict.Reason);
        if (command == TurnCommandReadiness.GovernedCheckRequired)
        {
            Assert.Contains("private decisions may remain", verdict.Reason);
            Assert.DoesNotContain("passes the existing command assessment", verdict.Reason);
        }
        var service = new TurnGuideService(TurnGuideCatalog.Load(), TurnPlacedDcGuidance.Registrations);
        var result = service.Describe(new(context, "afph", false), new(context, [verdict]));
        Assert.Equal(verdict.ActionRoute, Assert.Single(result.Options.Single(item => item.Topic.Id == TurnPlacedDcGuidance.TopicId).Decisions).ActionRoute);
        Assert.All(result.Options.Where(item => item.Topic.Id != TurnPlacedDcGuidance.TopicId), item => Assert.Empty(item.Capabilities));
        var route = new TurnPlacedDcAction("opaque", "dc", context.Profile, TurnPlacedDcGuidance.ActionId);
        Assert.Equal(route, JsonSerializer.Deserialize<TurnPlacedDcAction>(JsonSerializer.Serialize(route)));
    }

    [Fact]
    public void OnlyTheBoundedTopicIsRegisteredAcrossTheFourAdmittedProfiles()
    {
        Assert.Equal(4, TurnPlacedDcGuidance.Registrations.Length);
        var catalog = TurnGuideCatalog.Load();
        var service = new TurnGuideService(catalog, TurnPlacedDcGuidance.Registrations);
        foreach (var profile in new[] { "existing-engine", "existing-engine-dc-v1", "existing-engine-fire-v1", "existing-engine-atmosphere-v1", "existing-engine-work-v1" })
        {
            var count = 0;
            foreach (var phase in catalog.Phases)
            {
                var context = new TurnEvaluationContext("game", "a", "a", profile, phase.Id, phase.Id + ":phase", "stamp");
                var snapshot = service.Describe(new(context, phase.Id, false), new(context, []));
                count += snapshot.Options.Length;
                Assert.All(snapshot.Options.SelectMany(item => item.Decisions), item => Assert.Equal(TurnApplicability.NotEvaluated, item.Applicability));
                Assert.All(snapshot.Options.Where(item => item.Topic.Id != TurnPlacedDcGuidance.TopicId), item => Assert.Empty(item.Capabilities));
            }
            Assert.Equal(538, count);
        }
    }
}
