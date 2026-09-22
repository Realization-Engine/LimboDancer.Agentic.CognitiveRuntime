namespace LimboDancer.Abstractions.Domain;

public readonly record struct DomainId
{
    public DomainId(string value)
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
