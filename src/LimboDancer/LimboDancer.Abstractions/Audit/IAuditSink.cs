namespace LimboDancer.Abstractions.Audit;

public interface IAuditSink
{
    public ValueTask WriteAsync(
        RuntimeAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
