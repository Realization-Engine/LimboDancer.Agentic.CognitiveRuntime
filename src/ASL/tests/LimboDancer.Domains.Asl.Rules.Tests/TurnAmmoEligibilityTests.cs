using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnAmmoEligibilityTests
{
    private static readonly TurnAmmoVehicleSelection Selection = new(true, true, true, false, null, 15, 75);
    private static readonly TurnAmmoArmament Gun = new(75, TurnAmmoBarrel.LongBarrel);
    private static readonly TurnAmmoSupplyFacts Supply = new("supply", "german", true, true, true, Gun);
    private static readonly TurnAmmoRecipientFacts Recipient = new("tank-1", "german", true, true, true, true, true, true, Gun);
    private static readonly TurnAmmoMobility Mobile = new(false, false, false, false, false, false, false);

    [Theory]
    [InlineData(14, 75, TurnAmmoEligibility.Ineligible)]
    [InlineData(15, 75, TurnAmmoEligibility.Eligible)]
    [InlineData(19, 99, TurnAmmoEligibility.Eligible)]
    [InlineData(19, 100, TurnAmmoEligibility.Ineligible)]
    [InlineData(20, 100, TurnAmmoEligibility.Eligible)]
    [InlineData(20, 150, TurnAmmoEligibility.Eligible)]
    [InlineData(null, 75, TurnAmmoEligibility.NotEvaluated)]
    public void OrdinaryVehicleSelectionHonorsCapacityAndHundredMillimeterBoundary(int? pp, int caliber, TurnAmmoEligibility expected) =>
        Assert.Equal(expected, TurnAmmoEligibilityRules.SelectVehicle(Selection with
        {
            OrdinaryPassengerPp = pp,
            RecipientCaliberMm = caliber
        }).State);

    [Fact]
    public void SpecifiedVehicleOverridesCapacityButCannotInventAuthorizationOrRecipientType()
    {
        var specified = Selection with
        {
            HasSpecifiedVehicle = true,
            MatchesSpecifiedVehicle = true,
            OrdinaryPassengerPp = 0,
            RecipientCaliberMm = null
        };
        Assert.Equal(TurnAmmoEligibility.Eligible, TurnAmmoEligibilityRules.SelectVehicle(specified).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            MatchesSpecifiedVehicle = null
        }).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            MatchesSpecifiedVehicle = false
        }).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            HasSpecifiedVehicle = null
        }).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            Authorized = null
        }).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            Authorized = false
        }).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            IsVehicle = false
        }).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.SelectVehicle(specified with
        {
            RecipientHasLowAmmoBNumber = false
        }).State);
    }

    [Fact]
    public void UnknownCaliberOnlyAllowsCapacityConclusionsThatHoldOnBothSidesOfTheThreshold()
    {
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.SelectVehicle(Selection with
        {
            RecipientCaliberMm = null
        }).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.SelectVehicle(Selection with
        {
            RecipientCaliberMm = null,
            OrdinaryPassengerPp = 14
        }).State);
        Assert.Equal(TurnAmmoEligibility.Eligible, TurnAmmoEligibilityRules.SelectVehicle(Selection with
        {
            RecipientCaliberMm = null,
            OrdinaryPassengerPp = 20
        }).State);
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnAmmoEligibilityRules.SelectVehicle(Selection with { RecipientCaliberMm = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnAmmoEligibilityRules.SelectVehicle(Selection with { OrdinaryPassengerPp = -1 }));
    }

    [Fact]
    public void EveryD7ConditionMakesTheVehicleImmobileAndIncompleteNegativeEvidenceStaysUnknown()
    {
        TurnAmmoMobility[] cases = [Mobile with { Bogged = true }, Mobile with { Mired = true }, Mobile with { Immobilized = true },
            Mobile with { Abandoned = true }, Mobile with { InherentCrewBroken = true }, Mobile with { Shocked = true }, Mobile with { Stunned = true }];
        foreach (var facts in cases)
        {
            Assert.True(TurnAmmoEligibilityRules.Immobile(facts));
        }
        Assert.False(TurnAmmoEligibilityRules.Immobile(Mobile));
        Assert.Null(TurnAmmoEligibilityRules.Immobile(Mobile with
        {
            Abandoned = null
        }));
        Assert.True(TurnAmmoEligibilityRules.Immobile(Mobile with
        {
            Bogged = true,
            Abandoned = null
        }));
        // TI, Prep Fire and radioless refusal are deliberately not D.7 Immobile conditions.
        Assert.True(TurnWorkEffects.CanRelocateSupply(true, false));
        Assert.False(TurnWorkEffects.CanRelocateSupply(false, false)); // Legacy bounded contract stays unchanged.
    }

    [Theory]
    [InlineData(true, false, false, true)]
    [InlineData(false, true, true, true)]
    [InlineData(false, true, false, false)]
    [InlineData(false, false, null, false)]
    [InlineData(false, true, null, null)]
    [InlineData(null, false, false, null)]
    [InlineData(null, true, true, true)]
    public void AdjacencyAloneDoesNotEstablishAccessibleLocation(bool? same, bool? adjacent, bool? advance, bool? expected) =>
        Assert.Equal(expected, TurnAmmoEligibilityRules.SameOrAccessible(same, adjacent, advance));

    [Fact]
    public void CompatibilityRequiresBothExactCaliberAndBarrelLength()
    {
        Assert.True(TurnAmmoEligibilityRules.Compatible(Gun, Gun));
        Assert.False(TurnAmmoEligibilityRules.Compatible(Gun, new(76, TurnAmmoBarrel.LongBarrel)));
        Assert.False(TurnAmmoEligibilityRules.Compatible(Gun, new(75, TurnAmmoBarrel.Standard)));
        Assert.False(TurnAmmoEligibilityRules.Compatible(Gun, new(75, TurnAmmoBarrel.ShortBarrel)));
        Assert.False(TurnAmmoEligibilityRules.Compatible(Gun, new(75, TurnAmmoBarrel.ExtraLongBarrel)));
        Assert.Null(TurnAmmoEligibilityRules.Compatible(Gun, new(null, TurnAmmoBarrel.LongBarrel)));
        Assert.Null(TurnAmmoEligibilityRules.Compatible(Gun, new(75, TurnAmmoBarrel.Unknown)));
        Assert.False(TurnAmmoEligibilityRules.Compatible(Gun, new(76, TurnAmmoBarrel.Unknown)));
        Assert.True(TurnAmmoEligibilityRules.Compatible(new(12.7m, TurnAmmoBarrel.Standard), new(12.7m, TurnAmmoBarrel.Standard)));
        Assert.Throws<ArgumentException>(() => TurnAmmoEligibilityRules.Compatible(Gun, new(75, (TurnAmmoBarrel)99)));
        Assert.Throws<ArgumentOutOfRangeException>(() => TurnAmmoEligibilityRules.Compatible(Gun, new(-1, TurnAmmoBarrel.LongBarrel)));
    }

    [Fact]
    public void ReplenishmentRequiresKnownLowAmmoAndBothRecipientsToQualifyIndependently()
    {
        var second = Recipient with
        {
            Id = "tank-2"
        };
        Assert.Equal(TurnAmmoEligibility.Eligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [Recipient, second], true, TurnAmmoUse.Replenishment).State);
        TurnAmmoRecipientFacts[] invalid = [second with { Id = Recipient.Id }, second with { Id = Supply.Id }, second with { Owner = "russian" },
            second with { Active = false }, second with { IsVehicle = false }, second with { HasLowAmmoBNumber = false }, second with { LowAmmo = false },
            second with { CrewExposed = false }, second with { SameOrAccessibleLocation = false }, second with { Armament = new(75, TurnAmmoBarrel.Standard) }];
        foreach (var item in invalid)
        {
            Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [Recipient, item], true, TurnAmmoUse.Replenishment).State);
        }
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [], true, TurnAmmoUse.Replenishment).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [Recipient, second, second with { Id = "tank-3" }], true, TurnAmmoUse.Replenishment).State);
    }

    [Fact]
    public void MissingFactsAndPartialAssignmentsCannotGrantEligibility()
    {
        TurnAmmoRecipientFacts[] missing = [Recipient with { Id = null }, Recipient with { Owner = null }, Recipient with { Active = null },
            Recipient with { IsVehicle = null }, Recipient with { HasLowAmmoBNumber = null }, Recipient with { LowAmmo = null },
            Recipient with { CrewExposed = null }, Recipient with { SameOrAccessibleLocation = null }, Recipient with { Armament = new(null, TurnAmmoBarrel.Unknown) }];
        foreach (var item in missing)
        {
            Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [item], true, TurnAmmoUse.Replenishment).State);
        }
        TurnAmmoSupplyFacts[] unknownSupply = [Supply with { Id = null }, Supply with { Owner = null }, Supply with { Authorized = null }, Supply with { Active = null }, Supply with { Available = null }];
        foreach (var supply in unknownSupply)
        {
            Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.EvaluateRecipients(supply, [Recipient], true, TurnAmmoUse.Replenishment).State);
        }
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [Recipient], null, TurnAmmoUse.Replenishment).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [Recipient], false, TurnAmmoUse.Replenishment).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply with
        {
            Available = false
        }, [Recipient], null, TurnAmmoUse.Replenishment).State);
    }

    [Fact]
    public void FireBenefitDoesNotRequireExistingLowAmmoButNeverAllowsBoundingFire()
    {
        var recipient = Recipient with
        {
            LowAmmo = false
        };
        Assert.Equal(TurnAmmoEligibility.Eligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [recipient], true, TurnAmmoUse.FireBenefit, true).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [recipient], true, TurnAmmoUse.FireBenefit, false).State);
        Assert.Equal(TurnAmmoEligibility.NotEvaluated, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [recipient], true, TurnAmmoUse.FireBenefit).State);
        Assert.Equal(TurnAmmoEligibility.Ineligible, TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [recipient with { CrewExposed = false }], true, TurnAmmoUse.FireBenefit, true).State);
        Assert.Throws<ArgumentException>(() => TurnAmmoEligibilityRules.EvaluateRecipients(Supply, [recipient], true, (TurnAmmoUse)99));
    }
}
