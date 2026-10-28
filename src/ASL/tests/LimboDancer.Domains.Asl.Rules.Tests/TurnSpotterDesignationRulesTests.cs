using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class TurnSpotterDesignationRulesTests
{
    private static TurnSpotterDesignationFacts Eligible => new(
        new("a", new(1, 1, "a", "pfph", TurnCheckpoint.DuringPhase), false, TurnSpotterChange.None),
        true, true, true, true, true, false, false, false, false, false,
        new("s", false, false, false, false, null, false), false);

    [Fact]
    public void OriginalDaytimeDeclarationDoesNotGrantAReplacementOrAnUnbuiltProfile()
    {
        Assert.Equal(UnitCondition.PrepFire, TurnSpotterDesignationRules.FireCounter("pfph"));
        Assert.Equal(UnitCondition.FinalFire, TurnSpotterDesignationRules.FireCounter("dfph"));
        Assert.Throws<ArgumentException>(() => TurnSpotterDesignationRules.FireCounter("mph"));
        Assert.Null(TurnSpotterDesignationRules.Refusal(Eligible));
        Assert.Null(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                Now = new(1, 2, "b", "dfph", TurnCheckpoint.DuringPhase)
            }
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                HasDesignation = true
            }
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                HasDesignation = null
            }
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Night = true
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            PendingDecision = true
        }));
        Assert.Throws<ArgumentNullException>(() => TurnSpotterDesignationRules.Refusal(null!));
    }

    [Fact]
    public void EligibilityNeedsKnownReachGoodOrderOwnershipAndAvailableSupportWeaponUse()
    {
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Mortar = false
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            ActiveOwner = false
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            GoodOrderPersonnel = false
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            TemporarilyImmobilized = true
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            InReach = null
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            InReach = false
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            OtherMortarsSameHex = null
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            OtherMortarsSameHex = false
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Fired = true
        }));
        Assert.Null(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Fired = true,
            UsedAsSpotter = true
        }));
        Assert.Null(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Squad = true,
            Support = Eligible.Support with
            {
                Squad = true
            }
        }));
        Assert.NotNull(TurnSpotterDesignationRules.Refusal(Eligible with
        {
            Squad = true,
            Support = Eligible.Support with
            {
                Squad = true,
                Fired = true
            }
        }));
    }
}
