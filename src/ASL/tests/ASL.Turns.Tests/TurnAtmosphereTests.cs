using System.Text.Json;
using Xunit;

namespace ASL.Turns.Tests;

public sealed class TurnAtmosphereTests
{
    [Theory]
    [InlineData(false, null, null, false, TurnSmokeState.FailedAttempt)]
    [InlineData(true, null, null, false, TurnSmokeState.NotEvaluated)]
    [InlineData(true, "counter", true, false, TurnSmokeState.Active)]
    [InlineData(true, "counter", false, false, TurnSmokeState.NotEvaluated)]
    [InlineData(true, "counter", null, false, TurnSmokeState.NotEvaluated)]
    [InlineData(true, "counter", false, true, TurnSmokeState.RemovedAtMovementEnd)]
    public void GrenadeLifecycleRequiresCausalEvidence(bool placed, string? counter, bool? active, bool cleanup, TurnSmokeState expected)
    {
        var facts = new TurnGrenadeFacts("g1", "german", 2, "mph", placed, counter, active, cleanup, true);
        var result = TurnAtmosphere.Compose([facts], [], []);
        var grenade = Assert.Single(result.Grenades);
        Assert.Equal(expected, grenade.State);
        Assert.True(grenade.AttemptSpentThisPhase);
        Assert.Equal("g1", grenade.Unit);
        Assert.Equal("german", grenade.OwnerSide);
        Assert.Equal(2, grenade.GameTurn);
        Assert.Equal("mph", grenade.Phase);
        Assert.Equal(counter, grenade.Counter);
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(TurnAtmosphere.Compose([facts], [], [])));
        Assert.False(Assert.Single(TurnAtmosphere.Compose([facts with { AttemptInCurrentPhase = false }], [], []).Grenades).AttemptSpentThisPhase);
    }

    [Fact]
    public void AllPendingFamiliesRemainDistinctAndRecordedGustsAreNotConsumedByReads()
    {
        var wind = new TurnWindHistory(2, "german", "rph", true);
        var result = TurnAtmosphere.Compose([], [wind], ["untyped-smoke"]);
        Assert.Equal(wind, Assert.Single(result.Wind));
        Assert.Equal("untyped-smoke", Assert.Single(result.UnclassifiedSmoke));
        Assert.Equal(Enum.GetValues<TurnAtmosphereFamily>(), result.PendingRules.Select(item => item.Family));
        var catalog = TurnGuideCatalog.Load();
        Assert.All(result.PendingRules, gap =>
        {
            Assert.Contains(catalog.Dependencies, dependency => dependency.Id == gap.DependencyId);
            Assert.NotEmpty(gap.Reference);
            Assert.NotEmpty(gap.Timing);
            Assert.NotEmpty(gap.MissingFacts);
            Assert.Equal(TurnSupport.Pending, gap.CommandSupport);
        });
        Assert.Equal(JsonSerializer.Serialize(result), JsonSerializer.Serialize(TurnAtmosphere.Compose([], [wind], ["untyped-smoke"])));
        Assert.Equal(7, TurnAtmosphere.Compose([], [], []).PendingRules.Length);
    }

    [Fact]
    public void DefaultEvidenceIsRejected()
    {
        Assert.Throws<ArgumentException>(() => TurnAtmosphere.Compose(default, [], []));
        Assert.Throws<ArgumentException>(() => TurnAtmosphere.Compose([], default, []));
        Assert.Throws<ArgumentException>(() => TurnAtmosphere.Compose([], [], default));
    }
}
