namespace LimboDancer.Abstractions.Evidence;

public interface IReplayEvidenceSink
{
    ValueTask WriteAsync(
        RuntimeStepEvidence evidence,
        CancellationToken cancellationToken = default);
}
