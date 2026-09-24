namespace LimboDancer.Abstractions.Observations;

public interface IObservationProvider
{
    public ValueTask<ObservationAcquisitionResult> ObserveAsync(
        ObservationQuery query,
        CancellationToken cancellationToken = default);
}
