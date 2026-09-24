namespace LimboDancer.Abstractions.Observations;

public sealed record ObservationSource
{
    public ObservationSource(string sourceId, string? sourceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);
        if (sourceVersion is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sourceVersion);
        }

        SourceId = sourceId;
        SourceVersion = sourceVersion;
    }

    public string SourceId
    {
        get;
    }

    public string? SourceVersion
    {
        get;
    }
}
