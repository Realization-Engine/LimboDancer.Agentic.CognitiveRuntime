namespace LimboDancer.Abstractions.Domain;

public readonly record struct SemanticIdentifier
{
    public SemanticIdentifier(DomainId domainId, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(domainId.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        DomainId = domainId;
        Value = value;
    }

    public DomainId DomainId
    {
        get;
    }

    public string Value
    {
        get;
    }

    public override string ToString() => Value;
}
