using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingAssemblyTests
{
    [Theory]
    [InlineData(TurnPackingWeapon.MediumMachineGun, "german", null, false, true, false)]
    [InlineData(TurnPackingWeapon.HeavyMachineGun, "british", null, true, false, true)]
    [InlineData(TurnPackingWeapon.HeavyMachineGun, "american", null, null, true, null)]
    [InlineData(TurnPackingWeapon.HeavyMachineGun, "russian", null, false, false, null)]
    [InlineData(TurnPackingWeapon.HeavyMachineGun, "russian", null, true, false, true)]
    [InlineData(TurnPackingWeapon.MediumMachineGun, null, null, false, false, null)]
    [InlineData(TurnPackingWeapon.MediumMachineGun, "", null, false, false, null)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", 3, false, false, true)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", 3, null, null, true)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", 4, false, true, false)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", 5, true, false, true)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", null, false, false, null)]
    [InlineData(TurnPackingWeapon.LightMortar, "german", null, true, null, true)]
    [InlineData(TurnPackingWeapon.Mortar76To82, "german", null, false, true, false)]
    [InlineData(TurnPackingWeapon.Mortar76To82, "german", null, true, false, true)]
    [InlineData(TurnPackingWeapon.Mortar76To82, "german", null, null, true, null)]
    [InlineData(TurnPackingWeapon.Gun, "german", null, false, true, true)]
    [InlineData(TurnPackingWeapon.Gun, "german", null, false, false, null)]
    [InlineData(TurnPackingWeapon.OtherMortar, "german", null, null, null, null)]
    [InlineData(TurnPackingWeapon.Unknown, "german", null, true, true, null)]
    [InlineData(TurnPackingWeapon.InfantryGun37, "american", null, true, true, null)]
    public void AssemblyUsesKnownWeaponRequirementsAndPreservesUnknowns(TurnPackingWeapon weapon, string? nationality,
        int? portage, bool? dismantled, bool? limbered, bool? expected) =>
        Assert.Equal(expected, TurnPackingRules.AssemblyReady(weapon, nationality, portage, dismantled, limbered));

    [Fact]
    public void InvalidWeaponAndNegativePortageCannotBecomeAssemblyEvidence()
    {
        Assert.Throws<ArgumentException>(() => TurnPackingRules.AssemblyReady((TurnPackingWeapon)999, "german", 3, true, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnPackingRules.AssemblyReady(TurnPackingWeapon.LightMortar, "german", -1, true, false));
    }
}
