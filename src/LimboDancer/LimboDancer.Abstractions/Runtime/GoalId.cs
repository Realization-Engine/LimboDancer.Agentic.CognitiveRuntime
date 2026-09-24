namespace LimboDancer.Abstractions.Runtime;

public readonly record struct GoalId
{
    public GoalId(Guid value)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(value, Guid.Empty);
        Value = value;
    }

    public Guid Value
    {
        get;
    }

    public static GoalId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString("D");
}
