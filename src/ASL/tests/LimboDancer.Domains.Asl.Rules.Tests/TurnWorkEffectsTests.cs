using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnWorkEffectsTests
{
    private static TurnWorkDeclaration Declaration => new("work", "side", "gun", ["gun", "crew", "truck"], TurnWorkKind.AnimalPacking, null, 1, "ruling");

    [Theory]
    [InlineData(TurnWorkEffectKind.PackGun, true)]
    [InlineData(TurnWorkEffectKind.UnpackGun, false)]
    public void PackingHasOnlyItsBoundAssemblyEffect(TurnWorkEffectKind kind, bool packed)
    {
        var work = TurnWorkLifecycle.Start(Declaration with
        {
            CompletionEffects = new(kind, Crew: "crew")
        }, 1, true);
        Assert.Throws<InvalidDataException>(() => TurnWorkEffects.Resolve(work));
        work = TurnWorkLifecycle.Change(TurnWorkLifecycle.NextGameTurn(work, 1, 2), "complete", 2, TurnWorkWindow.FirstRally);
        Assert.Equal(new TurnWorkEffectResult(packed, false, false), TurnWorkEffects.Resolve(work));
        Assert.Throws<InvalidDataException>(() => TurnWorkEffects.Resolve(work with { EffectsApplied = true }));
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(12, true)]
    public void ReplenishmentRestoresAmmoEvenWhenItsSupplyDepletes(int dr, bool depleted)
    {
        var work = TurnWorkLifecycle.Start(Declaration with
        {
            Kind = TurnWorkKind.AmmoReplenishment,
            OriginalBNumber = 9,
            PackingGameTurns = null,
            CompletionEffects = new(TurnWorkEffectKind.Replenish, SupplyId: "supply", SupplyVehicle: "truck", CreateSupply: true, RecordLowAmmo: true),
        }, 1, true);
        var effect = TurnWorkEffects.Resolve(TurnWorkLifecycle.Change(work, "complete", 1, TurnWorkWindow.SecondCloseCombat, dr));
        Assert.Equal(new TurnWorkEffectResult(null, true, depleted), effect);
        Assert.Equal(0, TurnWorkEffects.PassengerCapacity(15, true));
        Assert.Equal(15, TurnWorkEffects.PassengerCapacity(15, false));
        Assert.Null(TurnWorkEffects.PassengerCapacity(null, false));
        Assert.NotNull(TurnWorkEffects.PackedGunBar(true));
        Assert.Null(TurnWorkEffects.PackedGunBar(false));
    }

    [Fact]
    public void LegacyAndMismatchedSpecificationsCannotCreateEffects()
    {
        var legacy = TurnWorkLifecycle.Start(Declaration, 1, true);
        Assert.Throws<InvalidDataException>(() => TurnWorkEffects.Resolve(legacy with { Completed = true }));
        TurnWorkEffectSpec[] invalid = [new((TurnWorkEffectKind)99), new(TurnWorkEffectKind.PackGun),
            new(TurnWorkEffectKind.PackGun, Crew: "gun"), new(TurnWorkEffectKind.PackGun, Crew: "stranger"),
            new(TurnWorkEffectKind.PackGun, Crew: "crew", CreateSupply: true),
            new(TurnWorkEffectKind.Replenish, SupplyId: "supply", SupplyVehicle: "truck")];
        foreach (var effect in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Start(Declaration with { CompletionEffects = effect }, 1, true));
        }
    }
}
