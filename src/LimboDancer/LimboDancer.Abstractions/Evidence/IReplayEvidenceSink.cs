namespace LimboDancer.Abstractions.Evidence;

public interface IReplayEvidenceSink
{
    public ValueTask WriteAsync(
        RuntimeStepEvidence evidence,
        CancellationToken cancellationToken = default);
}
