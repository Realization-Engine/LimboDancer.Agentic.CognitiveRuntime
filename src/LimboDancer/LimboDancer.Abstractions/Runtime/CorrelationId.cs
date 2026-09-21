namespace LimboDancer.Abstractions.Runtime;

public readonly record struct CorrelationId
{
    public CorrelationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
