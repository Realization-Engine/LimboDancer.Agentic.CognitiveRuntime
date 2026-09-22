using LimboDancer.Abstractions.Observations;

namespace LimboDancer.Runtime.Orchestration;

public sealed class EmptyObservationProvider : IObservationProvider
{
    public ValueTask<ObservationAcquisitionResult> ObserveAsync(
        ObservationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(new ObservationAcquisitionResult(
            query,
            observations: [],
            reasonCodes: ["observation.provider_not_configured"]));
    }
}
