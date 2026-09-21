namespace LimboDancer.Abstractions.Diagnostics;

public readonly record struct DiagnosticCheckId
{
    public DiagnosticCheckId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value
    {
        get;
    }

    public override string ToString() => Value;
}
