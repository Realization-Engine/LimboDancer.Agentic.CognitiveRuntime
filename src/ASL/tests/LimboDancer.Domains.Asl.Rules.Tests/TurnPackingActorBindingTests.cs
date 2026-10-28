using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingActorBindingTests
{
    private static readonly TurnPackingFacts Eligible = new(TurnPackingWeapon.Gun, true, true, true, true, true, true, true, true, null, true, true, true, true);
    [Theory]
    [InlineData(true, TurnPackingEligibility.Eligible)]
    [InlineData(false, TurnPackingEligibility.Ineligible)]
    [InlineData(null, TurnPackingEligibility.NotEvaluated)]
    public void ActorBindingMustBeProvenSeparatelyFromRuleFacts(bool? bound, TurnPackingEligibility expected) =>
        Assert.Equal(expected, TurnPackingRules.EvaluateActors(Eligible, bound).State);

    [Theory]
    [InlineData("asl:light-mortar", null, null, null, TurnPackingWeapon.LightMortar)]
    [InlineData("asl:mg", null, null, "medium", TurnPackingWeapon.MediumMachineGun)]
    [InlineData("asl:mg", null, null, "heavy", TurnPackingWeapon.HeavyMachineGun)]
    [InlineData("asl:mg", null, null, "light", TurnPackingWeapon.Unknown)]
    [InlineData("asl:gun", "mtr", 75, null, TurnPackingWeapon.OtherMortar)]
    [InlineData("asl:gun", "mtr", 76, null, TurnPackingWeapon.Mortar76To82)]
    [InlineData("asl:gun", "mtr", 82, null, TurnPackingWeapon.Mortar76To82)]
    [InlineData("asl:gun", "mtr", 83, null, TurnPackingWeapon.OtherMortar)]
    [InlineData("asl:gun", "mtr", null, null, TurnPackingWeapon.Unknown)]
    [InlineData("asl:gun", "inf", 75, null, TurnPackingWeapon.Gun)]
    [InlineData("asl:gun", null, 75, null, TurnPackingWeapon.Unknown)]
    [InlineData("asl:gun", "unrecognized", 75, null, TurnPackingWeapon.Unknown)]
    [InlineData(null, null, null, null, TurnPackingWeapon.Unknown)]
    public void ClassificationFollowsKnownWeaponFactsWithoutGrantingPermission(string? kind, string? type, int? caliber, string? size, TurnPackingWeapon expected) =>
        Assert.Equal(expected, TurnPackingRules.ClassifyWeapon(kind, type, caliber, size));

    [Fact]
    public void UnknownActorsDoNotEraseAnIndependentKnownRefusal() =>
        Assert.Equal(TurnPackingEligibility.Ineligible, TurnPackingRules.EvaluateActors(Eligible with
        {
            GoodOrder = false
        }, null).State);
}
