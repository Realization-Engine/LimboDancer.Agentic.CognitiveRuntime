using System.Text.Json;
using LimboDancer.Abstractions.Evidence;
using LimboDancer.Abstractions.Execution;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Infrastructure.Audit;

namespace LimboDancer.Tests.Integration.Audit;

public sealed class InMemoryReplayEvidenceSinkTests
{
    [Fact]
    public async Task SnapshotIsImmutableAndTenantScoped()
    {
        var sink = new InMemoryReplayEvidenceSink();
        var included = CreateEvidence(Guid.NewGuid());
        var excluded = CreateEvidence(Guid.NewGuid());

        await sink.WriteAsync(included);
        await sink.WriteAsync(excluded);

        var snapshot = sink.Snapshot(included.Goal.TenantId);
        Assert.Same(included, Assert.Single(snapshot));
        Assert.Null(snapshot.GetType().GetProperty("Item")!.SetMethod);
    }

    private static RuntimeStepEvidence CreateEvidence(Guid tenantId)
    {
        var now = DateTimeOffset.UtcNow;
        var goal = new Goal(
            GoalId.New(),
            new CorrelationId("replay-evidence-integration-test"),
            tenantId,
            null,
            GoalOrigin.System,
            "test-intent",
            JsonSerializer.SerializeToElement(new Dictionary<string, string>()),
            now);
        return new RuntimeStepEvidence(
            Guid.NewGuid(),
            RuntimeInvocationId.New(),
            goal,
            StepId.New(),
            new RuntimeBudget(1, now.AddMinutes(1), null, null, 0, 0),
            now);
    }
}
