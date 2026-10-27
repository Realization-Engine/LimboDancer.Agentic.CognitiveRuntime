using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnWorkRulesTests
{
    [Theory]
    [InlineData(8, 1)]
    [InlineData(9, 1)]
    [InlineData(10, 2)]
    [InlineData(11, 3)]
    [InlineData(12, null)]
    [InlineData(0, null)]
    [InlineData(null, null)]
    public void ReplenishmentUsesOriginalBNumber(int? b, int? turns)
    {
        var result = TurnWorkRules.Evaluate(TurnWorkKind.AmmoReplenishment, b, 3, true, true, TurnWorkWindow.SecondCloseCombat);
        Assert.Equal(turns, result.RequiredGameTurns);
        Assert.Equal(turns is null ? TurnWorkState.NotEvaluated : TurnWorkState.CompletionPending, result.State);
    }

    [Fact]
    public void TransferRequiresFourTurnsAndPackingDoesNotGuessDuration()
    {
        Assert.Equal(4, TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, 3, true, true, TurnWorkWindow.SecondCloseCombat).RequiredGameTurns);
        Assert.Equal(TurnWorkState.InProgress, TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, 3, true, true, TurnWorkWindow.SecondCloseCombat).State);
        Assert.Equal(TurnWorkState.NotEvaluated, TurnWorkRules.Evaluate(TurnWorkKind.AnimalPacking, null, 20, true, true, TurnWorkWindow.FirstRally).State);
        Assert.Equal(TurnWorkState.NotEvaluated, TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, 4, null, true, TurnWorkWindow.SecondCloseCombat).State);
        Assert.Equal(TurnWorkState.NotEvaluated, TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, 4, true, false, TurnWorkWindow.SecondCloseCombat).State);
        Assert.Throws<ArgumentException>(() => TurnWorkRules.Evaluate((TurnWorkKind)999, null, 0, true, true, TurnWorkWindow.Unknown));
        Assert.Throws<ArgumentException>(() => TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, 0, true, true, (TurnWorkWindow)999));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnWorkRules.Evaluate(TurnWorkKind.AmmoTransfer, null, -1, true, true, TurnWorkWindow.Unknown));
    }
}
