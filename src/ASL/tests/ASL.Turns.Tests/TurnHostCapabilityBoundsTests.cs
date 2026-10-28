using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnHostCapabilityBoundsTests
{
    [Theory]
    [InlineData("existing-engine", 1)]
    [InlineData("existing-engine-dc-v1", 2)]
    [InlineData("existing-engine-fire-v1", 3)]
    [InlineData("existing-engine-atmosphere-v1", 4)]
    [InlineData("existing-engine-work-v1", 5)]
    public void EachShippedProfileNamesItsFactsLimitationsAndAuthority(string id, int count)
    {
        var profile = TurnGuideProfiles.Get(id);
        Assert.Equal(count, profile.Bounds.Length);
        Assert.Equal(count, profile.Bounds.Select(item => item.Id).Distinct().Count());
        Assert.All(profile.Bounds, bound =>
        {
            Assert.NotEmpty(bound.Scope);
            Assert.NotEmpty(bound.RequiredFacts);
            Assert.NotEmpty(bound.Pending);
            Assert.NotEmpty(bound.Permission);
            Assert.True(Enum.IsDefined(bound.Kind));
        });
        foreach (var state in Enum.GetValues<TurnObligationState>())
        {
            Assert.Equal(state == TurnObligationState.NotEvaluated ? TurnSupport.Pending : TurnSupport.IntegratedSubset,
                profile.AssessedCommandSupport("stun", true, state));
            Assert.Equal(TurnSupport.Pending, profile.AssessedCommandSupport("stun", false, state));
            Assert.Equal(TurnSupport.Pending, profile.AssessedCommandSupport("ammo", true, state));
        }
        Assert.Equal(TurnSupport.Pending, profile.AssessedCommandSupport("cx", true, (TurnObligationState)99));
    }

    [Fact]
    public void WorkRemainsAdjudicatedAndLegacyVersionIsNotInferred()
    {
        var bound = Assert.Single(TurnGuideProfiles.Get("existing-engine-work-v1").Bounds, item => item.Id == "work");
        Assert.Equal(TurnEvidenceKind.AdjudicatedEffect, bound.Kind);
        Assert.Equal("asl.game.adjudicate-work", bound.Permission);
        var declaration = new TurnWorkDeclaration("id", "side", "a", ["a", "b"], TurnWorkKind.AnimalPacking, null, 1, "ruling");
        var legacy = TurnWorkLifecycle.Start(declaration, 1, true);
        Assert.Equal(0, TurnWorkDisclosure.From(legacy, false).HostAuditVersion);
        var audited = legacy with
        {
            HostAuditVersion = 1
        };
        Assert.Equal(1, TurnWorkDisclosure.From(audited, false).HostAuditVersion);
        Assert.Equal(TurnSupport.Pending, TurnWorkDisclosure.From(audited, false).RuleAutomation);
        Assert.Throws<ArgumentException>(() => TurnHostCapabilityBounds.ForProfile("unknown"));
    }
}
