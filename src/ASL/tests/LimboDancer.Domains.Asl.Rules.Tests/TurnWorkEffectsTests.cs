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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SupplyRelocationHasFourTurnsAndNoAmmunitionRoll(bool offload)
    {
        var declaration = new TurnWorkDeclaration("work", "side", "truck", offload ? ["truck"] : ["truck", "other"],
            TurnWorkKind.AmmoTransfer, null, null, "E10.4")
        {
            CompletionEffects = new(offload ? TurnWorkEffectKind.OffloadSupply : TurnWorkEffectKind.TransferSupply,
                SupplyId: "supply", SupplyVehicle: "truck", DestinationVehicle: offload ? null : "other", DumpLocation: offload ? "location" : null),
        };
        var work = TurnWorkLifecycle.Start(declaration, 1, true);
        Assert.Equal(4, work.RequiredGameTurns);
        for (var turn = 1; turn < 4; turn++)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "complete", turn, TurnWorkWindow.SecondCloseCombat));
            work = TurnWorkLifecycle.NextGameTurn(work, turn, turn + 1);
        }
        Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Change(work, "complete", 4, TurnWorkWindow.SecondCloseCombat, 12));
        var done = TurnWorkLifecycle.Change(work, "complete", 4, TurnWorkWindow.SecondCloseCombat);
        Assert.Equal(new TurnWorkEffectResult(null, false, false, true), TurnWorkEffects.Resolve(done));
        Assert.Throws<InvalidDataException>(() => TurnWorkEffects.Resolve(done with { EffectsApplied = true }));
        Assert.False(TurnWorkEffects.CanRelocateSupply(false, false));
        Assert.True(TurnWorkEffects.CanRelocateSupply(true, false));
        Assert.True(TurnWorkEffects.CanRelocateSupply(false, true));
        Assert.True(TurnWorkEffects.WreckAfterRelocation(true));
        Assert.False(TurnWorkEffects.WreckAfterRelocation(false));
        TurnWorkEffectSpec[] invalid = [declaration.CompletionEffects with { RecordLowAmmo = true },
            declaration.CompletionEffects with { SupplyVehicle = "other" }, declaration.CompletionEffects with { SupplyId = " " },
            declaration.CompletionEffects with { Crew = "crew" },
            declaration.CompletionEffects with { DestinationVehicle = "truck" },
            declaration.CompletionEffects with { DestinationVehicle = "stranger" },
            declaration.CompletionEffects with { DestinationVehicle = null, DumpLocation = null },
            declaration.CompletionEffects with { DestinationVehicle = "other", DumpLocation = "location" }];
        foreach (var spec in invalid)
        {
            Assert.Throws<InvalidDataException>(() => TurnWorkLifecycle.Start(declaration with { CompletionEffects = spec }, 1, true));
        }
    }
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(null, false)]
    public void ReplenishmentEffectsRequireKnownCe(bool? buttonedUp, bool supported) =>
        Assert.Equal(supported, TurnWorkEffects.ReplenishmentPostureSupported(buttonedUp));
}
