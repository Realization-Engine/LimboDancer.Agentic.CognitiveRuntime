using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingRecoveryTests
{
    private static readonly TurnPackingSpecification Specification = new("recovery", "german", new("gun", "mule", "crew"), TurnPackingWeapon.Mortar76To82, 11, false);
    private static readonly TurnPackingFacts Facts = new(TurnPackingWeapon.Mortar76To82, null, true, true, true, false, false,
        true, true, null, true, true, true, false);
    private static TurnPackingCasualtyState Casualty(bool eliminated) => TurnPackingCasualtyRules.Apply(
        new(TurnPackingWeapon.Mortar76To82, TurnHorseSize.Squad, true, false, false, 0, TurnPackingRecovery.None, true, true),
        eliminated ? TurnPackingMuleLoss.Eliminated : TurnPackingMuleLoss.CasualtyReduced, 6, TurnPackingUnloadTerrain.Allowed).State;
    private static TurnPackingPeriod Reload(TurnPackingPeriod period) => JsonSerializer.Deserialize<TurnPackingPeriod>(JsonSerializer.Serialize(period))!;

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FullRecoveryReleasesOnlyAfterItsWholeDayOrNightPeriodWithoutInventingAMidpoint(bool absentMule, bool night)
    {
        var facts = Facts with
        {
            MuleDesignated = absentMule ? null : true
        };
        Assert.NotEqual(TurnPackingEligibility.Eligible, TurnPackingRules.Evaluate(facts).State);
        var period = Reload(TurnPackingPeriods.StartRecovery(Specification with
        {
            Night = night
        }, Casualty(absentMule), new(2, true), facts));
        Assert.False(period.Loaded);
        Assert.True(period.WeaponPackTi);
        Assert.Equal(!absentMule, period.MulePackTi);
        Assert.Equal(!absentMule, period.WeaponAndMulePackTi);
        Assert.Equal(night ? 2 : 1, period.RequiredGameTurns);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyMidpoint(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Reverse(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Complete(period));
        for (var turn = 2; turn < 2 + period.RequiredGameTurns; turn++)
        {
            period = Reload(TurnPackingPeriods.NextRally(period, new(turn, false)));
            Assert.False(TurnPackingPeriods.MidpointDue(period));
            period = Reload(TurnPackingPeriods.NextRally(period, new(turn + 1, true)));
        }
        Assert.Null(period.MidpointAt);
        Assert.True(TurnPackingPeriods.CompletionDue(period));
        var completed = Reload(TurnPackingPeriods.Complete(period));
        Assert.False(completed.WeaponPackTi);
        Assert.False(completed.MulePackTi);
        Assert.True(completed.Completed);
        Assert.Equal(Casualty(absentMule), completed.RecoveryOrigin);
    }

    [Fact]
    public void InterruptionRequiresFreshRecoveryEligibilityAndLosesOnlyTheInterruptedTurn()
    {
        var period = TurnPackingPeriods.StartRecovery(Specification, Casualty(true), new(2, true), Facts);
        period = TurnPackingPeriods.Interrupt(period);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Facts));
        period = TurnPackingPeriods.NextRally(period, new(2, false));
        period = TurnPackingPeriods.NextRally(period, new(3, true));
        Assert.Equal(0, period.CompletedGameTurns);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, Facts with { GoodOrder = null }));
        period = TurnPackingPeriods.Resume(period, Facts);
        period = TurnPackingPeriods.NextRally(period, new(3, false));
        period = TurnPackingPeriods.NextRally(period, new(4, true));
        Assert.True(TurnPackingPeriods.Complete(period).Completed);
    }

    [Fact]
    public void MissingEligibilityAndAnExistingUnpackingPeriodCannotBeResetAsFullRecovery()
    {
        TurnPackingFacts[] missing = [Facts with { FirstRallyWorkWindow = null }, Facts with { SameLocationAndTerrain = null },
            Facts with { CrewCounter = null }, Facts with { SameNationality = false }, Facts with { GoodOrder = false },
            Facts with { Unpinned = null }, Facts with { NonPrc = false }, Facts with { MuleDesignated = null }];
        foreach (var facts in missing)
        {
            Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.StartRecovery(Specification, Casualty(false), new(2, true), facts));
        }
        TurnPackingCasualtyState[] invalid = [Casualty(false) with { WeaponAlive = false }, Casualty(false) with { Loaded = true },
            Casualty(false) with { Recovery = TurnPackingRecovery.ContinueExistingPeriod }, Casualty(false) with { Weapon = TurnPackingWeapon.Gun },
            Casualty(false) with { MuleSize = TurnHorseSize.Unknown }, Casualty(false) with { MulePackTi = false }];
        foreach (var casualty in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.StartRecovery(Specification, casualty, new(2, true), Facts));
        }
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.StartRecovery(Specification, Casualty(false), new(2, false), Facts));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.StartRecovery(Specification with { ManhandlingNumber = null }, Casualty(false), new(2, true), Facts));
    }

    [Fact]
    public void AlteredDurationDirectionLoadAndMidpointCannotAdvanceRecovery()
    {
        var period = TurnPackingPeriods.StartRecovery(Specification, Casualty(true), new(2, true), Facts);
        TurnPackingPeriod[] changed = [period with { RequiredGameTurns = 2 }, period with { Direction = TurnPackingDirection.Pack },
            period with { Loaded = true }, period with { MidpointAt = new(2, true) },
            period with { RecoveryOrigin = Casualty(true) with { Recovery = TurnPackingRecovery.None } }];
        foreach (var item in changed)
        {
            Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(item, new(2, false)));
        }
    }
}
