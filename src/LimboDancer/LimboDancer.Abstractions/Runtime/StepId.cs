namespace LimboDancer.Abstractions.Runtime;

public readonly record struct StepId
{
    public StepId(Guid value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, Guid.Empty);
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    public static StepId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
