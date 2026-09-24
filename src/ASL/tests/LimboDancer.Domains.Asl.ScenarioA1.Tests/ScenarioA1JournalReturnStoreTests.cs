using LimboDancer.Domains.Asl.ScenarioA1;
using Xunit;

namespace LimboDancer.Domains.Asl.ScenarioA1.Tests;

public sealed class ScenarioA1JournalReturnStoreTests
{
    [Fact]
    public async Task RestartRecoversOneCommittedReturnWithoutAnotherMfDebit()
    {
        var directory = TemporaryDirectory();
        try
        {
            var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
            var attempt = await Attempt();
            var first = new ScenarioA1JournalReturnStore(directory);
            await first.SeedAsync(initial);
            var applied = await new ScenarioA1ReturnSimulation(first).ApplyAsync(
                initial.TenantId, initial.GameId, initial.UnitId, attempt);
            Assert.Equal(ScenarioA1ReturnTransitionStatus.Applied, applied.Status);

            var reopened = new ScenarioA1JournalReturnStore(directory);
            var recovered = (await reopened.ReadAsync(initial.TenantId, initial.GameId,
                initial.UnitId))!;
            Assert.Equal(11, recovered.Version);
            Assert.Equal(2, recovered.RemainingMf);
            Assert.Equal("bd01:D4:0", recovered.UnitLocationId);
            Assert.Equal("bd01:D4:0", recovered.MfExpenditureLocationId);
            var replay = await new ScenarioA1ReturnSimulation(reopened).ApplyAsync(
                initial.TenantId, initial.GameId, initial.UnitId, attempt);
            Assert.Equal(ScenarioA1ReturnTransitionStatus.Replay, replay.Status);
            Assert.Equal(2, ((await reopened.ReadAsync(initial.TenantId, initial.GameId,
                initial.UnitId))!).RemainingMf);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task SeparateStoreInstancesCompareVersionAndCommitExactlyOnce()
    {
        var directory = TemporaryDirectory();
        try
        {
            var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
            var one = new ScenarioA1JournalReturnStore(directory);
            var two = new ScenarioA1JournalReturnStore(directory);
            await one.SeedAsync(initial);
            var attempt = await Attempt();
            var results = await Task.WhenAll(Enumerable.Range(0, 16).Select(index =>
                Task.Run(async () => await new ScenarioA1ReturnSimulation(index % 2 == 0
                    ? one : two).ApplyAsync(initial.TenantId, initial.GameId,
                    initial.UnitId, attempt))));
            Assert.Single(results, item => item.Status == ScenarioA1ReturnTransitionStatus.Applied);
            Assert.Equal(15, results.Count(item => item.Status == ScenarioA1ReturnTransitionStatus.Replay));
            var current = (await two.ReadAsync(initial.TenantId, initial.GameId,
                initial.UnitId))!;
            Assert.Equal(11, current.Version);
            Assert.Equal(2, current.RemainingMf);
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await one.SeedAsync(initial));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task CorruptJournalTailAndForeignTenantFailClosed()
    {
        var directory = TemporaryDirectory();
        try
        {
            var initial = ScenarioA1SecondDefenderReturnTransitionTests.State();
            var store = new ScenarioA1JournalReturnStore(directory);
            await store.SeedAsync(initial);
            Assert.Null(await store.ReadAsync(Guid.NewGuid(), initial.GameId, initial.UnitId));
            var path = Assert.Single(Directory.GetFiles(directory, "*.jsonl"));
            await File.AppendAllTextAsync(path, "{\"sha256\":\"broken\"\n");
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await store.ReadAsync(initial.TenantId, initial.GameId, initial.UnitId));
            var attempt = await Attempt();
            await Assert.ThrowsAsync<InvalidDataException>(async () =>
                await new ScenarioA1ReturnSimulation(store).ApplyAsync(initial.TenantId,
                    initial.GameId, initial.UnitId, attempt));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string TemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "asl-return-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<ScenarioA1ReturnAttempt> Attempt() => new("attempt-1", 10,
        await ScenarioA1SecondDefenderReturnTransitionTests.Conclusion(
            "smc-revealed", "revealedEnemySmc-after-election"));
}
