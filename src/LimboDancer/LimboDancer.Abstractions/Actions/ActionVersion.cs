namespace LimboDancer.Abstractions.Actions;

public readonly record struct ActionVersion
{
    public ActionVersion(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
