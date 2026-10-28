using System.Text.Json;
using ASL.Turns;

namespace LimboDancer.Domains.Asl.Play.Tests;

public static class TurnGuideAcceptanceCases
{
    private static readonly TurnGuideCatalog Catalog = TurnGuideCatalog.Load();
    private static readonly string[] ProfileIds =
    {
        "existing-engine", "existing-engine-dc-v1", "existing-engine-fire-v1",
        "existing-engine-atmosphere-v1", "existing-engine-work-v1"
    };
    public static IEnumerable<object[]> Profiles => ProfileIds.Select(id => new object[] { id });
    public static IEnumerable<object[]> FireProfiles => Profiles.Skip(2);
    public static IEnumerable<object[]> AtmosphereProfiles => Profiles.Skip(3);

    public static string Json(TurnHostGuide guide) => JsonSerializer.Serialize(guide);

    public static void AssertProfile(TurnHostGuide guide, string profile)
    {
        Assert.Equal(profile, guide.Profile.Id);
        Assert.Equal(Catalog.Phases.Single(phase => phase.Id == guide.Guide.BrowsedPhaseId).Topics.Select(topic => topic.Id),
            guide.Guide.Options.Select(option => option.Topic.Id));
        Assert.Equal(profile is "existing-engine-fire-v1" or "existing-engine-atmosphere-v1" or "existing-engine-work-v1", guide.FireHistory is not null);
        Assert.Equal(profile is "existing-engine-atmosphere-v1" or "existing-engine-work-v1", guide.Atmosphere is not null);
        if (guide.FireHistory is { } fire)
        {
            Assert.NotEmpty(fire.EvaluationGaps);
        }
        if (guide.Atmosphere is { } atmosphere)
        {
            Assert.NotEmpty(atmosphere.PendingRules);
        }
        if (profile == "existing-engine-work-v1")
        {
            Assert.NotEmpty(guide.WorkProgressGaps);
            Assert.All(guide.Work, work => Assert.Equal(TurnSupport.Pending, work.RuleAutomation));
        }
        else
        {
            Assert.Empty(guide.Work);
            Assert.Empty(guide.WorkProgressGaps);
        }
        Assert.NotEmpty(guide.PhaseClose.EvaluationGaps);
        Assert.All(guide.Guide.Options.Where(option => option.Topic.Id != TurnPlacedDcGuidance.TopicId), option =>
        {
            Assert.Empty(option.Capabilities);
            Assert.All(option.Decisions, decision =>
            {
                Assert.Equal(TurnApplicability.NotEvaluated, decision.Applicability);
                Assert.Null(decision.ActionRoute);
            });
        });
        if (guide.Guide.Historical)
        {
            Assert.Empty(guide.Actions);
            Assert.All(guide.Guide.Options.SelectMany(option => option.Decisions), decision => Assert.Null(decision.ActionRoute));
            Assert.Equal(TurnCloseDisposition.ReadOnly, guide.PhaseClose.Disposition);
        }
    }
}
