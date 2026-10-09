using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Pass 35, the Infantry increment, read directly in Rules: Good Order as A.7 has it and the scans that read it (task 35.3), the Morale Level
/// ceiling of A.18 (task 35.3), and the Russian Conscript's Battle Hardening (task 35.8). The wound procedure of task 35.1 is tested with the rout.
/// </summary>
public sealed class ScenarioA1Pass35RulesTests
{
    private sealed class ClearLos : ILosFactReader
    {
        public LosFacts? Los(int fromLocation, int toLocation) => new(true, Math.Abs(fromLocation - toLocation));
    }

    private static EnemyUnitFacts Enemy(int at, bool goodOrder = true, bool hidden = false, bool aboard = false, bool? broken = false, bool dummy = false) =>
        new(true, "german", dummy, aboard, broken, hidden, goodOrder, at);

    [Fact]
    public void GoodOrderIsNotBrokenBerserkCapturedStunnedShockedOrInMelee()
    {
        // A.7 (p. 43): a pinned, CX, TI, or unarmed unit is still in Good Order, so none of those is a fact of the verdict.
        Assert.True(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(false, false, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, true, false, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, true, false, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, true, false, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, true, false, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, true, false));
        Assert.False(ScenarioA1Definitions.GoodOrderOf(true, false, false, false, false, false, true));

        // The planner's SW and Deployment actions keep their bar on a TI unit, apart from Good Order.
        Assert.True(ScenarioA1Definitions.FreeToActAsPlanned(true, false));
        Assert.False(ScenarioA1Definitions.FreeToActAsPlanned(true, true));
        Assert.False(ScenarioA1Definitions.FreeToActAsPlanned(false, false));
    }

    [Fact]
    public void TheScansCountAGoodOrderEnemyNotMerelyAnUnbrokenOne()
    {
        var los = new ClearLos();

        // A12.14 (backlog section 51): the move's read. An unbroken enemy that is not in Good Order (in Melee, say), a hidden one, and a Passenger do
        // not force the loss of a mover's "?"; a Good Order unit in the open does.
        Assert.True(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, goodOrder: false)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, hidden: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, aboard: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(3, dummy: true)], "russian", 0, los));
        Assert.False(ScenarioA1MovementCalculator.EnemyGoodOrderInLosWithin16([Enemy(17)], "russian", 0, los));

        // A12.34: the nearest Good Order enemy with a LOS.
        Assert.Equal(5, ScenarioA1MovementCalculator.NearestGoodOrderEnemyInLos([Enemy(2, goodOrder: false), Enemy(5)], "russian", 0, los));
        Assert.Null(ScenarioA1MovementCalculator.NearestGoodOrderEnemyInLos([Enemy(2, goodOrder: false)], "russian", 0, los));
    }

    [Fact]
    public void AMoraleLevelNeverExceedsTen()
    {
        // A.18 (p. 44): "even if the unit is Fanatic, heroic, with a Commissar, and/or part of a Human Wave".
        Assert.Equal(10, ScenarioA1Definitions.MoraleCeiling(11));
        Assert.Equal(10, ScenarioA1Definitions.MoraleCeiling(10));
        Assert.Equal(7, ScenarioA1Definitions.MoraleCeiling(7));
        Assert.Equal(10, ScenarioA1OverrunCalculator.PaatcMorale([new PaatcUnit(false, 10, true, false)]));
        Assert.Equal(9, ScenarioA1OverrunCalculator.PaatcMorale([new PaatcUnit(false, 8, true, false)]));
        Assert.Equal(10, ScenarioA1RoutCalculator.BrokenMorale(true, 10, 10, false, fanatic: true));
    }

    [Fact]
    public void ARussianConscriptBattleHardensToTheFiveTwoSeven()
    {
        // A25.2 (p. 93): "A 4-2-6 squad Battle Hardens to a 5-2-7", and its 2-2-6 HS to the 5-2-7's own 2-2-7; the 2-2-7 to a 3-2-8.
        Assert.Equal("defender-line-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-squad"));
        Assert.Equal("defender-line-half-squad", ScenarioA1FireReference.HardenedOf("defender-conscript-half-squad"));
        Assert.Equal("defender-guards-half-squad", ScenarioA1FireReference.HardenedOf("defender-line-half-squad"));
    }
}
