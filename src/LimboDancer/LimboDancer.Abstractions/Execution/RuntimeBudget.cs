namespace LimboDancer.Abstractions.Execution;

public sealed record RuntimeBudget
{
    public RuntimeBudget(
        int maxSteps,
        DateTimeOffset deadline,
        long? maxTokens,
        decimal? maxCost,
        int maxExternalCalls,
        int maxRetries)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxSteps);
        ArgumentOutOfRangeException.ThrowIfNegative(maxTokens ?? 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxCost ?? 0);
        ArgumentOutOfRangeException.ThrowIfNegative(maxExternalCalls);
        ArgumentOutOfRangeException.ThrowIfNegative(maxRetries);

        MaxSteps = maxSteps;
        Deadline = deadline;
        MaxTokens = maxTokens;
        MaxCost = maxCost;
        MaxExternalCalls = maxExternalCalls;
        MaxRetries = maxRetries;
    }

    public int MaxSteps
    {
        get;
    }

    public DateTimeOffset Deadline
    {
        get;
    }

    public long? MaxTokens
    {
        get;
    }

    public decimal? MaxCost
    {
        get;
    }

    public int MaxExternalCalls
    {
        get;
    }

    public int MaxRetries
    {
        get;
    }
}
