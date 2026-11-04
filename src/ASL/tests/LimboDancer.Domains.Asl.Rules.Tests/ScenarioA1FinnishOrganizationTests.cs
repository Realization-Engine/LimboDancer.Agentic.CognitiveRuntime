using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1FinnishOrganizationTests
{
    [Theory]
    [InlineData("deploy", "squad", "directed", "directed-ntc", true, 0)]
    [InlineData("deploy", "squad", "leaderless", "leaderless-1tc", false, 1)]
    [InlineData("deploy", "half-squad", "leaderless", "not-applicable", null, null)]
    [InlineData("recombine", "half-squad", "not-applicable", "no-leader-required", false, null)]
    [InlineData("recombine", "half-squad", "leaderless", "not-evaluated", null, null)]
    [InlineData("deploy", "squad", "not-applicable", "not-evaluated", null, null)]
    public void ActionAndExplicitModeDetermineRequirementsOnly(string action, string kind, string mode, string outcome, bool? leader, int? drm)
    {
        var result = ScenarioA1FinnishOrganizationCalculator.Evaluate(new("finnish", kind, action, mode, false, false, false));
        Assert.Equal(outcome, result.Outcome);
        Assert.Equal(leader, result.LeaderRequired);
        Assert.Equal(drm, result.BaseTcDrm);
    }

    [Fact]
    public void GuardPriorityAndConditionalUnknownsArePreserved()
    {
        var squad = new FinnishOrganizationFacts("finnish", "squad", "deploy", "leaderless", true, null, false);
        Assert.Equal("automatic", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad).Outcome);
        Assert.Equal("not-evaluated", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Broken = null
        }).Outcome);
        Assert.Equal("leaderless-ntc", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Guard = false,
            Unarmed = true
        }).Outcome);
        Assert.Equal("not-applicable", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Nationality = "german",
            Kind = null
        }).Outcome);
        Assert.Equal("no-leader-required", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Kind = "half-squad",
            Action = "recombine",
            Mode = "not-applicable",
            Guard = null,
            Broken = null,
        }).Outcome);
        Assert.Equal("leaderless-ntc", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Guard = null,
            Broken = true,
            Unarmed = true
        }).Outcome);
        Assert.Equal("directed-ntc", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Guard = null,
            Broken = true,
            Mode = "directed"
        }).Outcome);
        Assert.Equal("not-evaluated", ScenarioA1FinnishOrganizationCalculator.Evaluate(squad with
        {
            Guard = null,
            Broken = true,
            Unarmed = false
        }).Outcome);
        Assert.NotNull(ScenarioA1SupportWeaponRules.RecombineStartBar(true));
    }

    [Theory]
    [InlineData("leaderless", null, false, 1, 3, 3)]
    [InlineData("directed", -1, false, -1, 4, 4)]
    [InlineData("directed", -1, true, 0, 3, 4)]
    public void DeploymentRequirementComposesWithLeaderDrmAtThePassingBoundary(string mode, int? leadership, bool wounded, int expectedDrm, int first, int second)
    {
        var requirement = ScenarioA1FinnishOrganizationCalculator.Evaluate(new("finnish", "squad", "deploy", mode, false, false, false));
        var drm = requirement.BaseTcDrm!.Value + (requirement.LeaderRequired == true ? ScenarioA1SupportWeaponRules.LeaderDrm(leadership, wounded) : 0);
        Assert.Equal(expectedDrm, drm);
        Assert.True(ScenarioA1OrdnanceProjection.DeploymentPassed(first, second, drm, 7));
        Assert.False(ScenarioA1OrdnanceProjection.DeploymentPassed(first, second + 1, drm, 7));
    }

    [Fact]
    public void ExistingFinnishRanksAndFieldPromotionRemainTheSingleTables()
    {
        string[] ascending = ["8-plus-1", "8-0", "9-0", "9-1", "10-0", "10-1"];
        for (var index = 0; index < ascending.Length; index++)
        {
            var id = "finnish-leader-" + ascending[index];
            Assert.Equal(index == 0 ? null : "finnish-leader-" + ascending[index - 1], ScenarioA1FireReference.ReplacementOf(id));
            Assert.Equal(index == ascending.Length - 1 ? null : "finnish-leader-" + ascending[index + 1], ScenarioA1FireReference.HardenedOf(id));
        }

        Assert.False(ScenarioA1FieldPromotion.Applicable("finnish"));
        Assert.True(ScenarioA1FieldPromotion.Applicable("italian"));
    }
}
