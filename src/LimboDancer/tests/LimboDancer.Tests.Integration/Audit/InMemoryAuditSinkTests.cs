using LimboDancer.Abstractions.Audit;
using LimboDancer.Abstractions.Runtime;
using LimboDancer.Infrastructure.Audit;

namespace LimboDancer.Tests.Integration.Audit;

public sealed class InMemoryAuditSinkTests
{
    [Fact]
    public async Task WrittenEventsAreAvailableAsAnImmutableSnapshot()
    {
        var sink = new InMemoryAuditSink();
        var auditEvent = new RuntimeAuditEvent(
            Guid.NewGuid(),
            AuditEventType.InvocationAdmitted,
            RuntimeInvocationId.New(),
            new CorrelationId("audit-integration-test"),
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        await sink.WriteAsync(auditEvent);

        var snapshot = sink.Snapshot();
        Assert.Same(auditEvent, Assert.Single(snapshot));
        Assert.Null(snapshot.GetType().GetProperty("Item")!.SetMethod);
    }
}
