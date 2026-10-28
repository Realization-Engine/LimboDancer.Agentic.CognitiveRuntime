using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingUnloadedCasualtyTests
{
    private static readonly TurnPackingSpecification Spec = new("period", "german", new("gun", "mule", "crew"), TurnPackingWeapon.Gun, 10, false);
    private static readonly TurnPackingFacts Facts = new(TurnPackingWeapon.Gun, true, true, true, true, true, true,
        true, true, null, true, true, true, true);
    private static readonly TurnPackingCasualtyState Origin = new(TurnPackingWeapon.Gun, TurnHorseSize.Squad, false, true, false, 0,
        TurnPackingRecovery.ContinueExistingPeriod, true, true);
    private static TurnPackingPeriod Reload(TurnPackingPeriod period) => JsonSerializer.Deserialize<TurnPackingPeriod>(JsonSerializer.Serialize(period))!;

    private static TurnPackingPeriod TurnFive()
    {
        var period = TurnPackingPeriods.Start(Spec, TurnPackingDirection.Unpack, new(2, true), Facts);
        period = TurnPackingPeriods.NextRally(period, new(2, false));
        period = TurnPackingPeriods.NextRally(period, new(3, true));
        period = TurnPackingPeriods.Interrupt(period);
        period = TurnPackingPeriods.NextRally(period, new(3, false));
        period = TurnPackingPeriods.NextRally(period, new(4, true));
        period = TurnPackingPeriods.Resume(period, Facts);
        period = TurnPackingPeriods.NextRally(period, new(4, false));
        period = TurnPackingPeriods.ApplyMidpoint(period);
        return TurnPackingPeriods.NextRally(period, new(5, true));
    }

    [Theory]
    [InlineData(TurnPackingMuleLoss.CasualtyReduced)]
    [InlineData(TurnPackingMuleLoss.Eliminated)]
    public void PrintedTurnFiveCasualtyPreservesProgressAndReleasesInTurnSix(TurnPackingMuleLoss loss)
    {
        var before = TurnFive();
        var period = Reload(TurnPackingPeriods.ApplyUnloadedCasualty(before, Origin, loss, 3, TurnPackingUnloadTerrain.Unknown));
        Assert.Equal(before, period with
        {
            LatestCasualty = null
        });
        Assert.Equal(2, period.CompletedGameTurns);
        Assert.Equal(new(4, false), period.MidpointAt);
        Assert.True(period.WeaponPackTi);
        Assert.Equal(loss == TurnPackingMuleLoss.CasualtyReduced, period.MulePackTi);
        Assert.Equal(1, period.LatestCasualty!.LowAmmoOccurrences);
        Assert.False(period.LatestCasualty.Malfunctioned);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Reverse(period));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Complete(period));
        period = TurnPackingPeriods.NextRally(period, new(5, false));
        period = TurnPackingPeriods.NextRally(period, new(6, true));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(period, period.LatestCasualty!, TurnPackingMuleLoss.Eliminated, 3, TurnPackingUnloadTerrain.Unknown));
        period = Reload(TurnPackingPeriods.Complete(period));
        Assert.True(period.Completed);
        Assert.False(period.WeaponPackTi);
        Assert.False(period.MulePackTi);
        Assert.Equal(3, period.CompletedGameTurns);
    }

    [Fact]
    public void SubsequentLossAccumulatesAmmoAndIndependentCrewInterruptionRequiresFreshEligibility()
    {
        var period = TurnPackingPeriods.ApplyUnloadedCasualty(TurnFive(), Origin, TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Unknown);
        var reduced = period.LatestCasualty!;
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(period, reduced with { LowAmmoOccurrences = 0 }, TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Unknown));
        period = Reload(TurnPackingPeriods.ApplyUnloadedCasualty(period, reduced, TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Unknown));
        Assert.Equal(2, period.LatestCasualty!.LowAmmoOccurrences);
        Assert.True(period.PersonnelPackTi);
        Assert.False(period.MulePackTi);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(period, period.LatestCasualty!, TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Unknown));
        period = TurnPackingPeriods.Interrupt(period);
        period = TurnPackingPeriods.NextRally(period, new(5, false));
        period = TurnPackingPeriods.NextRally(period, new(6, true));
        Assert.Equal(2, period.CompletedGameTurns);
        var survivingFacts = Facts with
        {
            MuleDesignated = null,
            SquadMule = false,
            MuleAvailableForSubject = false,
            DismantledOrLimberedIfPossible = false
        };
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.Resume(period, survivingFacts with { SameLocationAndTerrain = null }));
        period = TurnPackingPeriods.Resume(period, survivingFacts);
        period = TurnPackingPeriods.NextRally(period, new(6, false));
        period = TurnPackingPeriods.NextRally(period, new(7, true));
        Assert.True(TurnPackingPeriods.Complete(period).Completed);
    }

    [Fact]
    public void FurtherCasualtyDuringFullRecoveryPreservesItsDurationAndOrigin()
    {
        var casualty = TurnPackingCasualtyRules.Apply(Origin with
        {
            Loaded = true,
            Recovery = TurnPackingRecovery.None
        },
            TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Allowed).State;
        var period = TurnPackingPeriods.StartRecovery(Spec, casualty, new(2, true), Facts);
        period = TurnPackingPeriods.NextRally(period, new(2, false));
        period = TurnPackingPeriods.NextRally(period, new(3, true));
        var before = period;
        period = Reload(TurnPackingPeriods.ApplyUnloadedCasualty(period, casualty, TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Unknown));
        Assert.Equal(before, period with
        {
            LatestCasualty = null
        });
        Assert.Equal(TurnPackingRecovery.FullOrdinaryPeriod, period.LatestCasualty!.Recovery);
        Assert.Equal(2, period.LatestCasualty.LowAmmoOccurrences);
        Assert.True(period.LatestCasualty.Malfunctioned);
        Assert.Null(period.MidpointAt);
        Assert.False(period.MulePackTi);
        for (var turn = 3; turn < 5; turn++)
        {
            period = TurnPackingPeriods.NextRally(period, new(turn, false));
            period = TurnPackingPeriods.NextRally(period, new(turn + 1, true));
        }
        Assert.True(TurnPackingPeriods.Complete(period).Completed);
    }

    [Fact]
    public void LoadedOrUnprovenPeriodsAndChangedCasualtySnapshotsCannotUseTheUnloadedRoute()
    {
        var start = TurnPackingPeriods.Start(Spec, TurnPackingDirection.Unpack, new(2, true), Facts);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(start, Origin, TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Unknown));
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(start with { Loaded = false }, Origin, TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Unknown));
        var period = TurnFive();
        TurnPackingCasualtyState[] invalid = [Origin with { Loaded = true }, Origin with { Weapon = TurnPackingWeapon.OtherMortar },
            Origin with { WeaponAlive = false }, Origin with { MuleAlive = false }, Origin with { MulePackTi = false },
            Origin with { MuleSize = TurnHorseSize.HalfSquad }, Origin with { Recovery = TurnPackingRecovery.FullOrdinaryPeriod }];
        foreach (var state in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.ApplyUnloadedCasualty(period, state, TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Unknown));
        }
        var applied = TurnPackingPeriods.ApplyUnloadedCasualty(period, Origin, TurnPackingMuleLoss.CasualtyReduced, 3, TurnPackingUnloadTerrain.Unknown);
        Assert.Throws<InvalidDataException>(() => TurnPackingPeriods.NextRally(applied with { LatestCasualty = applied.LatestCasualty! with { Loaded = true } }, new(5, false)));
        Assert.Equal(TurnPackingEligibility.Ineligible, TurnPackingRules.EvaluateUnloadedContinuation(Facts, Origin with
        {
            Recovery = TurnPackingRecovery.None
        }).State);
    }
}
