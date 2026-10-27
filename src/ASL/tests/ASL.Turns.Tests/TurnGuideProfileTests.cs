using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnGuideProfileTests
{
    [Theory]
    [InlineData("existing-engine", TurnSupport.Pending)]
    [InlineData("existing-engine-dc-v1", TurnSupport.IntegratedSubset)]
    public void ProfilesNameOnlyProvenSlicesAndKeepPendingDefinitions(string id, TurnSupport dc)
    {
        var profile = TurnGuideProfiles.Get(id);
        Assert.Equal(id, profile.Id);
        Assert.NotEmpty(profile.Version);
        Assert.NotEmpty(profile.Evidence);
        Assert.Contains("stun", profile.IntegratedDependencies);
        Assert.Equal(dc, profile.CommandSupport("placedDc", true));
        Assert.Equal(TurnSupport.Pending, profile.CommandSupport("placedDc", false));
        Assert.Equal(TurnSupport.Pending, profile.CommandSupport("pack", true));
        var catalog = TurnGuideCatalog.Load();
        Assert.Equal(538, catalog.Phases.Sum(phase => phase.Topics.Length));
        Assert.Equal(38, catalog.Dependencies.Length);
    }

    [Fact]
    public void UnknownProfilesCannotClaimIntegration() => Assert.Throws<ArgumentException>(() => TurnGuideProfiles.Get("all-asl"));
}
