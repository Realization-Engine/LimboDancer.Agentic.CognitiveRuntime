using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnPackingRulesTests
{
    private static readonly TurnPackingFacts Eligible = new(TurnPackingWeapon.Gun, true, true, true, true, true, true,
        true, true, null, true, true, true, true);

    [Fact]
    public void LoadStructureRequiresKnownWeaponCapabilityAndOneOwnDesignatedSquadMule()
    {
        var mule = new TurnHorseIdentity(TurnHorseSize.Squad, true, "scenario-mule");
        foreach (var weapon in new[] { TurnPackingWeapon.Gun, TurnPackingWeapon.OtherMortar, TurnPackingWeapon.Mortar76To82,
            TurnPackingWeapon.LightMortar, TurnPackingWeapon.MediumMachineGun, TurnPackingWeapon.HeavyMachineGun, TurnPackingWeapon.InfantryGun37 })
        {
            Assert.Null(TurnPackingRules.LoadRefusal(weapon, true, mule, true, true, false));
        }
        Assert.Null(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Mortar76To82, null, mule, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, null, mule, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, false, mule, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Unknown, true, mule, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal((TurnPackingWeapon)99, true, mule, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, null, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, mule with
        {
            Mule = false
        }, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, mule with
        {
            Size = TurnHorseSize.HalfSquad
        }, true, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, mule, false, true, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, mule, true, false, false));
        Assert.NotNull(TurnPackingRules.LoadRefusal(TurnPackingWeapon.Gun, true, mule, true, true, true));
    }

    [Theory]
    [InlineData(TurnPackingWeapon.OtherMortar, 9, false, 3)]
    [InlineData(TurnPackingWeapon.Gun, 10, false, 3)]
    [InlineData(TurnPackingWeapon.Gun, 10, true, 6)]
    [InlineData(TurnPackingWeapon.Mortar76To82, 11, false, 1)]
    [InlineData(TurnPackingWeapon.Mortar76To82, 12, false, 1)]
    [InlineData(TurnPackingWeapon.Mortar76To82, 12, true, 2)]
    [InlineData(TurnPackingWeapon.LightMortar, null, true, 2)]
    [InlineData(TurnPackingWeapon.MediumMachineGun, null, false, 1)]
    [InlineData(TurnPackingWeapon.HeavyMachineGun, 5, false, 1)]
    [InlineData(TurnPackingWeapon.InfantryGun37, null, false, 1)]
    [InlineData(TurnPackingWeapon.Gun, null, false, null)]
    [InlineData(TurnPackingWeapon.Unknown, 11, false, null)]
    public void DurationUsesMNumberMinimumNightAndSwException(TurnPackingWeapon weapon, int? number, bool night, int? expected) =>
        Assert.Equal(expected, TurnPackingRules.RequiredGameTurns(weapon, number, night));

    [Fact]
    public void QualificationRequiresKnownRolesAndPreservesTheSwException()
    {
        Assert.Equal(TurnPackingEligibility.Eligible, TurnPackingRules.Evaluate(Eligible).State);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, TurnPackingRules.Evaluate(Eligible with
        {
            Weapon = TurnPackingWeapon.OtherMortar,
            GunAllowedByNoteOrDyo = null
        }).State);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, TurnPackingRules.Evaluate(Eligible with
        {
            Weapon = TurnPackingWeapon.Unknown,
            CrewCounter = false,
            QualifiedMmcForSw = true
        }).State);
        Assert.Equal(TurnPackingEligibility.NotEvaluated, TurnPackingRules.Evaluate(Eligible with
        {
            GunAllowedByNoteOrDyo = null
        }).State);
        Assert.Equal(TurnPackingEligibility.Ineligible, TurnPackingRules.Evaluate(Eligible with
        {
            GunAllowedByNoteOrDyo = false
        }).State);
        Assert.Equal(TurnPackingEligibility.Eligible, TurnPackingRules.Evaluate(Eligible with
        {
            Weapon = TurnPackingWeapon.Mortar76To82,
            GunAllowedByNoteOrDyo = null
        }).State);
        Assert.Equal(TurnPackingEligibility.Eligible, TurnPackingRules.Evaluate(Eligible with
        {
            Weapon = TurnPackingWeapon.HeavyMachineGun,
            CrewCounter = false,
            SameNationality = false,
            QualifiedMmcForSw = true
        }).State);
        foreach (var facts in new[] { Eligible with { FirstRallyWorkWindow = false }, Eligible with { SameLocationAndTerrain = false },
            Eligible with { MuleDesignated = false }, Eligible with { SquadMule = false }, Eligible with { MuleAvailableForSubject = false },
            Eligible with { CrewCounter = false }, Eligible with { SameNationality = false }, Eligible with { GoodOrder = false },
            Eligible with { Unpinned = false }, Eligible with { NonPrc = false }, Eligible with { DismantledOrLimberedIfPossible = false } })
        {
            Assert.Equal(TurnPackingEligibility.Ineligible, TurnPackingRules.Evaluate(facts).State);
        }
        foreach (var facts in new[] { Eligible with { FirstRallyWorkWindow = null }, Eligible with { CrewCounter = null },
            Eligible with { SameNationality = null }, Eligible with { Weapon = TurnPackingWeapon.Unknown },
            Eligible with { Weapon = TurnPackingWeapon.LightMortar, QualifiedMmcForSw = null } })
        {
            Assert.Equal(TurnPackingEligibility.NotEvaluated, TurnPackingRules.Evaluate(facts).State);
        }
        Assert.Contains("G10.7", TurnPackingRules.Evaluate(Eligible).Reference, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TurnPackingCrewActivity.Repair)]
    [InlineData(TurnPackingCrewActivity.Assemble)]
    [InlineData(TurnPackingCrewActivity.Dismantle)]
    [InlineData(TurnPackingCrewActivity.Limber)]
    [InlineData(TurnPackingCrewActivity.Unlimber)]
    public void UnloadedGunHandlingDoesNotInterruptPackTi(TurnPackingCrewActivity activity)
    {
        Assert.True(TurnPackingRules.CrewRetainsPackTi(true, true, true, activity, false));
        Assert.False(TurnPackingRules.CrewRetainsPackTi(true, true, true, activity, true));
    }

    [Fact]
    public void CrewInterruptionLosesOnlyTheCurrentTurnAndReleaseWaitsForNextFirstRally()
    {
        Assert.True(TurnPackingRules.CrewRetainsPackTi(true, true, true, TurnPackingCrewActivity.None, true));
        Assert.False(TurnPackingRules.CrewRetainsPackTi(true, true, true, TurnPackingCrewActivity.OtherTiProhibited, false));
        Assert.False(TurnPackingRules.CrewRetainsPackTi(false, true, true, TurnPackingCrewActivity.None, false));
        Assert.False(TurnPackingRules.CrewRetainsPackTi(true, false, true, TurnPackingCrewActivity.None, false));
        Assert.False(TurnPackingRules.CrewRetainsPackTi(true, true, false, TurnPackingCrewActivity.None, false));
        // The printed G10.3 example: start turn 2, finish 2, interrupt 3, resume 4, finish 4 and 5, release in 6.
        var turns = TurnPackingRules.RequiredGameTurns(TurnPackingWeapon.Gun, 10, false);
        var work = TurnWorkLifecycle.Start(new("packing", "a", "gun", ["gun", "crew", "mule"],
            TurnWorkKind.AnimalPacking, null, turns, "G10.3 source-backed period"), 2, true);
        work = TurnWorkLifecycle.NextGameTurn(work, 2, 3);
        work = TurnWorkLifecycle.Change(work, "interrupt", 3, TurnWorkWindow.Unknown);
        Assert.Equal(1, work.CompletedGameTurns);
        Assert.Null(work.OpenGameTurn);
        Assert.Same(work, TurnWorkLifecycle.NextGameTurn(work, 3, 4));
        work = TurnWorkLifecycle.Change(work, "resume", 4, TurnWorkWindow.FirstRally);
        work = TurnWorkLifecycle.NextGameTurn(work, 4, 5);
        Assert.False(TurnWorkLifecycle.CompletionDue(work, 5, TurnWorkWindow.SecondCloseCombat));
        work = TurnWorkLifecycle.NextGameTurn(work, 5, 6);
        Assert.True(TurnWorkLifecycle.CompletionDue(work, 6, TurnWorkWindow.FirstRally));
        Assert.Equal(2, TurnPackingRules.ReversalGameTurns(2, 6, false));
        Assert.Equal(1, TurnPackingRules.ReversalGameTurns(0, 3, false));
        Assert.Equal(6, TurnPackingRules.ReversalGameTurns(2, 6, true));
    }

    [Fact]
    public void MalformedFactsAreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => TurnPackingRules.Evaluate(null!));
        Assert.Throws<ArgumentException>(() => TurnPackingRules.Evaluate(Eligible with { Weapon = (TurnPackingWeapon)99 }));
        Assert.Throws<ArgumentException>(() => TurnPackingRules.RequiredGameTurns((TurnPackingWeapon)99, 10, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnPackingRules.RequiredGameTurns(TurnPackingWeapon.Gun, -1, false));
        Assert.Throws<ArgumentException>(() => TurnPackingRules.CrewRetainsPackTi(true, true, true, (TurnPackingCrewActivity)99, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnPackingRules.ReversalGameTurns(-1, 3, false));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnPackingRules.ReversalGameTurns(1, 0, false));
    }
}
