using System.Security.Cryptography;

namespace LimboDancer.Dice;

/// <summary>Generates dice using the system cryptographic random number generator.</summary>
public sealed class DiceRoller
{
    private readonly Func<int, int> next;

    public DiceRoller() : this(RandomNumberGenerator.GetInt32)
    {
    }

    internal DiceRoller(Func<int, int> next)
    {
        ArgumentNullException.ThrowIfNull(next);
        this.next = next;
    }

    /// <summary>Rolls 1 through 100 dice with 2 through int.MaxValue sides.</summary>
    /// <exception cref="ArgumentNullException">The request is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Count or sides is outside the supported range.</exception>
    public RollResult Roll(RollRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(request.Count, 100);
        ArgumentOutOfRangeException.ThrowIfLessThan(request.Sides, 2);

        var values = new int[request.Count];
        for (var index = 0; index < values.Length; index++)
        {
            var value = next(request.Sides);
            if (value < 0 || value >= request.Sides)
            {
                throw new InvalidOperationException("The random source returned a value outside its requested bounds.");
            }

            values[index] = value + 1;
        }

        return new RollResult(request, values);
    }
}
