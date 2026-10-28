using System.Text.Json;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingCasualtyRulesTests
{
    private static readonly TurnPackingCasualtyState Loaded = new(TurnPackingWeapon.Mortar76To82,
        TurnHorseSize.Squad, true, false, false, 0, TurnPackingRecovery.None, true, true);
    private static TurnPackingCasualtyTransition Apply(TurnPackingCasualtyState state, TurnPackingMuleLoss loss = TurnPackingMuleLoss.CasualtyReduced,
        int die = 3, TurnPackingUnloadTerrain terrain = TurnPackingUnloadTerrain.Allowed) =>
        TurnPackingCasualtyRules.Apply(JsonSerializer.Deserialize<TurnPackingCasualtyState>(JsonSerializer.Serialize(state))!, loss, die, terrain);

    [Fact]
    public void PrintedMortarCasualtiesAccumulateAndKeepTheFullRecoveryRequirementAfterMuleLoss()
    {
        var first = Apply(Loaded);
        Assert.Equal(TurnHorseSize.HalfSquad, first.State.MuleSize);
        Assert.False(first.State.Loaded);
        Assert.True(first.State.MulePackTi);
        Assert.True(first.State.Malfunctioned);
        Assert.Equal(1, first.State.LowAmmoOccurrences);
        Assert.Equal(TurnPackingRecovery.FullOrdinaryPeriod, first.State.Recovery);
        var second = Apply(first.State, TurnPackingMuleLoss.Eliminated, 4);
        Assert.False(second.State.MuleAlive);
        Assert.False(second.State.MulePackTi);
        Assert.True(second.State.WeaponAlive);
        Assert.True(second.State.Malfunctioned);
        Assert.Equal(2, second.State.LowAmmoOccurrences);
        Assert.Equal(TurnPackingRecovery.ContinueExistingPeriod, second.Verdict.Effects!.Recovery);
        Assert.Equal(TurnPackingRecovery.FullOrdinaryPeriod, second.State.Recovery);
        Assert.Equal(new(10, 11), TurnPackingVulnerabilityRules.LowAmmoNumbers(12, second.State.LowAmmoOccurrences!.Value));
        Assert.Throws<InvalidDataException>(() => Apply(second.State, TurnPackingMuleLoss.Eliminated));
        Assert.Equal(0, Loaded.LowAmmoOccurrences);
        Assert.Equal(1, first.State.LowAmmoOccurrences);
    }

    [Fact]
    public void AnAlreadyUnloadedGunKeepsExistingRecoveryAndUnknownStatusInsteadOfRestarting()
    {
        var state = Loaded with
        {
            Weapon = TurnPackingWeapon.Gun,
            Loaded = false,
            MulePackTi = true,
            Recovery = TurnPackingRecovery.ContinueExistingPeriod,
            LowAmmoOccurrences = 1,
            Malfunctioned = null
        };
        var result = Apply(state);
        Assert.Equal(TurnPackingRecovery.ContinueExistingPeriod, result.State.Recovery);
        Assert.Null(result.State.Malfunctioned);
        Assert.Equal(2, result.State.LowAmmoOccurrences);
        Assert.False(result.Verdict.Effects!.UnloadWeapon);
        Assert.False(result.Verdict.Effects.MarkMalfunctioned);
    }

    [Fact]
    public void UnknownAmmunitionNeverBecomesAnInventedCountAndNoNewMalfunctionDoesNotRepairOne()
    {
        var unknown = Apply(Loaded with
        {
            LowAmmoOccurrences = null,
            Malfunctioned = null
        }, die: 6);
        Assert.Null(unknown.State.LowAmmoOccurrences);
        Assert.Null(unknown.State.Malfunctioned);
        var later = Apply(unknown.State, TurnPackingMuleLoss.Eliminated, 4);
        Assert.True(later.Verdict.Effects!.AddLowAmmo);
        Assert.Null(later.State.LowAmmoOccurrences);
        Assert.True(Apply(Loaded with
        {
            Malfunctioned = true
        }, die: 6).State.Malfunctioned);
    }

    [Fact]
    public void WeaponEliminationTerminatesThePairWithoutManufacturingRecovery()
    {
        foreach (var terrain in new[] { TurnPackingUnloadTerrain.Allowed, TurnPackingUnloadTerrain.Prohibited })
        {
            var result = Apply(Loaded, die: terrain == TurnPackingUnloadTerrain.Allowed ? 1 : 6, terrain: terrain);
            Assert.False(result.State.WeaponAlive);
            Assert.False(result.State.Loaded);
            Assert.Equal(TurnPackingRecovery.None, result.State.Recovery);
            Assert.Throws<InvalidDataException>(() => Apply(result.State, TurnPackingMuleLoss.Eliminated));
        }
    }

    [Fact]
    public void InconsistentUnknownAndNonTriggeredStatesCannotAcquireReceipts()
    {
        TurnPackingCasualtyState[] invalid =
        [Loaded with { MuleSize = TurnHorseSize.HalfSquad }, Loaded with { MuleSize = TurnHorseSize.Unknown },
            Loaded with { MuleAlive = false }, Loaded with { WeaponAlive = false }, Loaded with { LowAmmoOccurrences = -1 },
            Loaded with { Recovery = (TurnPackingRecovery)99 }, Loaded with { Recovery = TurnPackingRecovery.FullOrdinaryPeriod },
            Loaded with { Weapon = TurnPackingWeapon.Unknown }, Loaded with { Loaded = false }];
        foreach (var state in invalid)
        {
            Assert.Throws<InvalidDataException>(() => Apply(state));
        }
        Assert.Throws<InvalidDataException>(() => Apply(Loaded, TurnPackingMuleLoss.None));
        Assert.Throws<InvalidDataException>(() => Apply(Loaded, TurnPackingMuleLoss.Unknown));
        Assert.Throws<InvalidDataException>(() => Apply(Loaded, terrain: TurnPackingUnloadTerrain.Unknown));
        var half = Apply(Loaded).State;
        Assert.Throws<InvalidDataException>(() => Apply(half));
        Assert.Throws<ArgumentException>(() => Apply(Loaded, die: 0));
        Assert.Throws<OverflowException>(() => Apply(Loaded with { LowAmmoOccurrences = int.MaxValue }));
        Assert.Throws<ArgumentNullException>(() => TurnPackingCasualtyRules.Apply(null!, TurnPackingMuleLoss.Eliminated, 4, TurnPackingUnloadTerrain.Allowed));
    }
}
