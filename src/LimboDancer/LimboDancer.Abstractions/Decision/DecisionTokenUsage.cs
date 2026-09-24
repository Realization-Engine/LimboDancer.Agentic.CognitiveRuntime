namespace LimboDancer.Abstractions.Decision;

public sealed record DecisionTokenUsage
{
    public DecisionTokenUsage(long inputTokens, long outputTokens)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inputTokens);
        ArgumentOutOfRangeException.ThrowIfNegative(outputTokens);
        InputTokens = inputTokens;
        OutputTokens = outputTokens;
        TotalTokens = checked(inputTokens + outputTokens);
    }

    public long InputTokens
    {
        get;
    }

    public long OutputTokens
    {
        get;
    }

    public long TotalTokens
    {
        get;
    }
}
