namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Optional stage measurements, emitted only when information logging is enabled.</summary>
internal static partial class LoadMeasurements
{
    public static void Record(ILogger? logger, string subject, string stage, double milliseconds)
    {
        if (logger is not null)
        {
            Stage(logger, subject, stage, milliseconds);
        }
    }

    [LoggerMessage(EventId = 901, Level = LogLevel.Information, Message = "ASL load {Subject}, {Stage}: {Milliseconds} ms")]
    private static partial void Stage(ILogger logger, string subject, string stage, double milliseconds);
}
