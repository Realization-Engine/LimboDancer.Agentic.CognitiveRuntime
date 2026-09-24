namespace LimboDancer.Abstractions.Runtime;

public readonly record struct RuntimeInvocationId
{
    public RuntimeInvocationId(Guid value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, Guid.Empty);
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    public static RuntimeInvocationId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
