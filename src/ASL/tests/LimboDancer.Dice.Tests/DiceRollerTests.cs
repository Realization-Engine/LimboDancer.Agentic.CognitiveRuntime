using System.Security.Cryptography;
using Xunit;

namespace LimboDancer.Dice.Tests;

public sealed class DiceRollerTests
{
    [Fact]
    public void PreservesRequestAndGenerationOrder()
    {
        var draws = new Queue<int>([5, 0, 3]);
        var roller = new DiceRoller(sides =>
        {
            Assert.Equal(6, sides);
            return draws.Dequeue();
        });
        var request = new RollRequest(3, 6);

        var result = roller.Roll(request);

        Assert.Equal(request, result.Request);
        Assert.Equal([6, 1, 4], result.Values);
        Assert.Empty(draws);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(20)]
    [InlineData(int.MaxValue)]
    public void SupportsBothInclusiveBoundaries(int sides)
    {
        Assert.Equal(1, Assert.Single(new DiceRoller(_ => 0).Roll(new RollRequest(1, sides)).Values));
        Assert.Equal(sides, Assert.Single(new DiceRoller(bound => bound - 1).Roll(new RollRequest(1, sides)).Values));
    }

    [Fact]
    public void SupportsMaximumCount()
    {
        var draws = 0;
        var result = new DiceRoller(_ => { draws++; return 0; }).Roll(new RollRequest(100, 2));

        Assert.Equal(100, draws);
        Assert.Equal(100, result.Values.Count);
        Assert.All(result.Values, value => Assert.Equal(1, value));
    }

    [Theory]
    [InlineData(int.MinValue, 6)]
    [InlineData(0, 6)]
    [InlineData(101, 6)]
    [InlineData(int.MaxValue, 6)]
    [InlineData(1, int.MinValue)]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void RejectsInvalidRequestsWithoutDrawing(int count, int sides)
    {
        var draws = 0;
        var roller = new DiceRoller(_ => { draws++; return 0; });

        Assert.Throws<ArgumentOutOfRangeException>(() => roller.Roll(new RollRequest(count, sides)));
        Assert.Equal(0, draws);
    }

    [Fact]
    public void RejectsNullRequestWithoutDrawing()
    {
        var draws = 0;
        var roller = new DiceRoller(_ => { draws++; return 0; });

        Assert.Throws<ArgumentNullException>(() => roller.Roll(null!));
        Assert.Equal(0, draws);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    [InlineData(int.MaxValue)]
    public void RejectsInvalidSourceOutput(int value)
    {
        var draws = 0;
        var roller = new DiceRoller(_ => { draws++; return value; });

        Assert.Throws<InvalidOperationException>(() => roller.Roll(new RollRequest(3, 6)));
        Assert.Equal(1, draws);
    }

    [Fact]
    public void PropagatesFailureWithoutReturningPartialResult()
    {
        var failure = new CryptographicException("Test source failure.");
        var draws = 0;
        var roller = new DiceRoller(_ => ++draws == 2 ? throw failure : 0);
        RollResult? result = null;

        var thrown = Assert.Throws<CryptographicException>(() => result = roller.Roll(new RollRequest(3, 6)));

        Assert.Same(failure, thrown);
        Assert.Null(result);
        Assert.Equal(2, draws);
    }

    [Fact]
    public void ResultOwnsItsValuesAndDisallowsMutation()
    {
        int[] source = [1, 6];
        var result = new RollResult(new RollRequest(2, 6), source);
        source[0] = 5;
        var values = Assert.IsAssignableFrom<IList<int>>(result.Values);

        Assert.Throws<NotSupportedException>(() => values[0] = 4);
        Assert.Throws<NotSupportedException>(() => values.Add(3));
        Assert.Equal([1, 6], result.Values);
    }

    [Fact]
    public void SubsequentRollDoesNotChangeEarlierResult()
    {
        var next = 0;
        var roller = new DiceRoller(_ => next++);
        var request = new RollRequest(2, 6);

        var first = roller.Roll(request);
        var second = roller.Roll(request);

        Assert.Equal([1, 2], first.Values);
        Assert.Equal([3, 4], second.Values);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(20)]
    [InlineData(int.MaxValue)]
    public void ProductionGeneratorReturnsRequestedCountWithinBounds(int sides)
    {
        var result = new DiceRoller().Roll(new RollRequest(100, sides));

        Assert.Equal(100, result.Values.Count);
        Assert.All(result.Values, value => Assert.InRange(value, 1, sides));
    }
}
