using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1ReturnSimulationStoreTests
{
    [Fact]
    public async Task AtomicCommitAcceptsOnlyExactCandidateForOneExpectedVersion()
    {
        var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
        var attempt = await Attempt();
        var store = new ScenarioA1InMemoryReturnStore([initial]);
        var expected = (await store.ReadAsync(initial.TenantId, initial.GameId, initial.UnitId))!;
        var candidate = ScenarioA1SecondDefenderReturnTransition.Evaluate(expected, attempt);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Applied, candidate.Status);
        Assert.False(await store.TryCommitAsync(expected, attempt,
            candidate.State with { RemainingMf = 0 }));
        Assert.True(await store.TryCommitAsync(expected, attempt, candidate.State));
        Assert.False(await store.TryCommitAsync(expected, attempt, candidate.State));

        var committed = (await store.ReadAsync(initial.TenantId, initial.GameId, initial.UnitId))!;
        Assert.Equal(11, committed.Version);
        Assert.Equal(2, committed.RemainingMf);
        Assert.Equal("bd01:D4:0", committed.UnitLocationId);
        Assert.Equal("bd01:D4:0", committed.MfExpenditureLocationId);
        Assert.Equal(ScenarioA1ReturnStage.Returned, committed.Stage);
    }

    [Fact]
    public async Task RacingIdenticalAttemptsApplyOnceAndReplayWithoutSecondDebit()
    {
        var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
        var store = new ScenarioA1InMemoryReturnStore([initial]);
        var simulation = new ScenarioA1ReturnSimulation(store);
        var attempt = await Attempt();
        var results = await Task.WhenAll(Enumerable.Range(0, 24).Select(_ => Task.Run(async () =>
            await simulation.ApplyAsync(initial.TenantId, initial.GameId, initial.UnitId, attempt))));
        Assert.Single(results, item => item.Status == ScenarioA1ReturnTransitionStatus.Applied);
        Assert.Equal(23, results.Count(item => item.Status == ScenarioA1ReturnTransitionStatus.Replay));
        var final = (await store.ReadAsync(initial.TenantId, initial.GameId, initial.UnitId))!;
        Assert.Equal(11, final.Version);
        Assert.Equal(2, final.RemainingMf);
        Assert.True(final.MovementEnded);
    }

    [Fact]
    public async Task StaleConflictingAndHazardAttemptsCannotCommit()
    {
        var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
        var store = new ScenarioA1InMemoryReturnStore([initial]);
        var simulation = new ScenarioA1ReturnSimulation(store);
        var attempt = await Attempt();
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Denied,
            (await simulation.ApplyAsync(Guid.NewGuid(), initial.GameId,
                initial.UnitId, attempt)).Status);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Stale,
            (await simulation.ApplyAsync(initial.TenantId, initial.GameId,
                initial.UnitId, attempt with { ExpectedVersion = 9 })).Status);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Applied,
            (await simulation.ApplyAsync(initial.TenantId, initial.GameId,
                initial.UnitId, attempt)).Status);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Conflict,
            (await simulation.ApplyAsync(initial.TenantId, initial.GameId,
                initial.UnitId, attempt with { ExpectedVersion = 11 })).Status);

        var hazard = initial with { GameId = "hazard-game", ReturnHazard = ScenarioA1ReturnHazard.Minefield };
        var hazardStore = new ScenarioA1InMemoryReturnStore([hazard]);
        var hazardSimulation = new ScenarioA1ReturnSimulation(hazardStore);
        Assert.Equal(ScenarioA1ReturnTransitionStatus.Denied,
            (await hazardSimulation.ApplyAsync(hazard.TenantId, hazard.GameId,
                hazard.UnitId, attempt)).Status);
        Assert.Same(hazard, await hazardStore.ReadAsync(hazard.TenantId, hazard.GameId,
            hazard.UnitId));
    }

    private static async Task<ScenarioA1ReturnAttempt> Attempt() => new("attempt-1", 10,
        await ScenarioA1SecondDefenderReturnTransitionTests.Conclusion(
            "smc-revealed", "revealedEnemySmc-after-election"));
}
