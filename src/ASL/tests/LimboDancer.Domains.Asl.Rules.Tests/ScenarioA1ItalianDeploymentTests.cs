using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1ItalianDeploymentTests
{
    [Theory]
    [InlineData("italian", "squad", true, "ordinary", "unrestricted")]
    [InlineData("italian", "squad", false, "ordinary", "restricted")]
    [InlineData("italian", "squad", false, "setup", "restricted")]
    [InlineData("eritrean", "squad", false, "ordinary", "restricted")]
    [InlineData("italian", "half-squad", false, "ordinary", "not-applicable")]
    [InlineData("italian", "crew", true, "ordinary", "not-applicable")]
    [InlineData("german", null, null, "ordinary", "not-applicable")]
    [InlineData(null, "squad", false, "ordinary", "not-evaluated")]
    [InlineData("italian", "squad", null, "ordinary", "not-evaluated")]
    [InlineData("italian", "squad", true, "invalid", "not-evaluated")]
    public void NationalityRestrictionDoesNotGrantFullDeployment(string? nation, string? kind, bool? elite, string context, string outcome)
    {
        Assert.Equal(outcome, ScenarioA1ItalianDeploymentCalculator.Evaluate(new(nation, kind, elite, context, false, false, false)).Outcome);
    }

    [Theory]
    [InlineData("guard", true, false, false, null, "unrestricted")]
    [InlineData("guard", true, false, true, false, "restricted")]
    [InlineData("guard", null, null, null, true, "unrestricted")]
    [InlineData("guard", null, null, null, false, "not-evaluated")]
    [InlineData("unarmed", false, true, false, null, "unrestricted")]
    [InlineData("unarmed", false, false, false, false, "not-evaluated")]
    [InlineData("temporary-crew", null, null, null, null, "unrestricted")]
    public void ExceptionsReadTheirRawFactsAndIgnoreIrrelevantUnknowns(string context, bool? guard, bool? unarmed, bool? broken, bool? elite, string outcome)
    {
        Assert.Equal(outcome, ScenarioA1ItalianDeploymentCalculator.Evaluate(new("italian", "squad", elite, context, guard, unarmed, broken)).Outcome);
    }

    [Theory]
    [InlineData("italian", "squad", 4, 4, 7, true)]
    [InlineData("italian", "half-squad", 2, 4, 7, true)]
    [InlineData("italian", "crew", 2, 2, 7, true)]
    [InlineData("italian", "squad", 3, 4, 7, false)]
    [InlineData("finnish", "squad", 4, 4, 7, false)]
    [InlineData("eritrean", "crew", 2, 2, 7, false)]
    [InlineData(null, "squad", 4, 4, 7, false)]
    public void PrintedEliteClassificationIsItalianOnly(string? nation, string kind, int fp, int range, int morale, bool elite)
    {
        Assert.Equal(elite, ScenarioA1ItalianDeploymentCalculator.EliteMmc(nation, kind, fp, range, morale));
        Assert.Equal("italian-line-squad", ScenarioA1FireReference.ReplacementOf("italian-elite-squad"));
    }

    [Fact]
    public void LegacyNationalityAndLeaderExceptionMembersKeepTheirAnswers()
    {
        Assert.True(ScenarioA1Definitions.MayDeploy("italian"));
        Assert.True(ScenarioA1Definitions.MayDeploy("eritrean"));
        Assert.True(ScenarioA1Definitions.MayDeploy(null));
        Assert.False(ScenarioA1Definitions.MayDeploy("russian"));
        Assert.False(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false));
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, true));
        Assert.True(ScenarioA1SupportWeaponRules.NeedsNoLeader(false, false, true));
    }
}
