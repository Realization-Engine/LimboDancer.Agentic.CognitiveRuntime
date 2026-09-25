namespace LimboDancer.Dice;

/// <summary>The request and its individual die values in generation order.</summary>
public sealed class RollResult
{
    internal RollResult(RollRequest request, int[] values)
    {
        Request = request;
        Values = Array.AsReadOnly((int[])values.Clone());
    }

    public RollRequest Request
    {
        get;
    }

    public IReadOnlyList<int> Values
    {
        get;
    }
}
