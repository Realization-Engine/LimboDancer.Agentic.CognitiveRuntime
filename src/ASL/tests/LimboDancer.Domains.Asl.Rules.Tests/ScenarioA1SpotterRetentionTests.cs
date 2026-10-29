using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The C9.3 claim's one reading of who may spot, reach, sharing at designation, retention, a held assignment, and supersession (rulings RC93.3 to
/// RC93.7), each once in <see cref="ScenarioA1SpotterCalculator"/> where several callers decided it before.
/// </summary>
public sealed class ScenarioA1SpotterRetentionTests
{
    private static readonly SpotterCandidateFacts Squad = new(true, true, true, true, false, false, false);

    [Theory]
    [InlineData(true, true, false, false, false, true)]
    [InlineData(false, true, false, false, false, false)]
    [InlineData(true, false, false, false, false, false)]
    [InlineData(true, true, true, false, false, false)]
    [InlineData(true, true, false, true, false, false)]
    [InlineData(true, true, false, false, true, false)]
    public void ARecordedLossIsFinalEvenForASpotterThatRallied(bool active, bool sameSide, bool broken, bool captured, bool recordedLoss, bool expected)
    {
        Assert.Equal(expected, ScenarioA1SpotterCalculator.Retains(active, sameSide, broken, captured, recordedLoss));
    }

    [Fact]
    public void AGoodOrderPersonnelUnitOfTheMortarsSideOtherThanItsCrewMaySpot()
    {
        Assert.Null(ScenarioA1SpotterCalculator.CandidateRefusal(Squad));
        Assert.Contains("active unit of the mortar's side", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            SameSide = false
        }), StringComparison.Ordinal);
        Assert.Contains("Personnel", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            Personnel = false
        }), StringComparison.Ordinal);
        Assert.Contains("Good Order", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            GoodOrder = false
        }), StringComparison.Ordinal);
        Assert.Contains("Melee", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            Melee = true
        }), StringComparison.Ordinal);
        Assert.Contains("Captured", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            Captured = true
        }), StringComparison.Ordinal);
        Assert.Contains("own crew", ScenarioA1SpotterCalculator.CandidateRefusal(Squad with
        {
            MortarCrew = true
        }), StringComparison.Ordinal);
        Assert.True(ScenarioA1SpotterCalculator.Personnel("asl:half-squad"));
        Assert.False(ScenarioA1SpotterCalculator.Personnel("asl:vehicle"));
    }

    [Fact]
    public void ReachIsTheMortarsHexOrAnAdjacentOneAndAnUnreadRangeIsNotInReach()
    {
        Assert.True(ScenarioA1SpotterCalculator.InReach(0));
        Assert.True(ScenarioA1SpotterCalculator.InReach(1));
        Assert.False(ScenarioA1SpotterCalculator.InReach(2));
        Assert.False(ScenarioA1SpotterCalculator.InReach(null));
    }

    [Fact]
    public void AtDesignationEveryOtherMortarOfTheSpotterIsInThisMortarsLocation()
    {
        Assert.True(ScenarioA1SpotterCalculator.SharesLocation("bd02:E6:0", []));
        Assert.True(ScenarioA1SpotterCalculator.SharesLocation("bd02:E6:0", ["bd02:E6:0"]));
        Assert.False(ScenarioA1SpotterCalculator.SharesLocation("bd02:E6:0", ["bd02:E6:1"]));
        Assert.Null(ScenarioA1SpotterCalculator.SharesLocation(null, ["bd02:E6:0"]));
        Assert.Null(ScenarioA1SpotterCalculator.SharesLocation("bd02:E6:0", [null]));
    }

    [Fact]
    public void ARetainedSpotterKeepsItsMortarAndALaterSpotterSupersedesOnlyWhenNotTheSuccessor()
    {
        Assert.Null(ScenarioA1SpotterCalculator.KeptRefusal("m", null, "s2"));
        Assert.Null(ScenarioA1SpotterCalculator.KeptRefusal("m", "s1", "s1"));
        Assert.StartsWith("play.ordnance-spotter-kept: s1 spots for m", ScenarioA1SpotterCalculator.KeptRefusal("m", "s1", "s2"), StringComparison.Ordinal);
        Assert.True(ScenarioA1SpotterCalculator.Supersedes("s1", "s2", null));
        Assert.False(ScenarioA1SpotterCalculator.Supersedes("s1", "s1", null));
        Assert.False(ScenarioA1SpotterCalculator.Supersedes("s1", "s1-hs", "s1-hs"));
        Assert.True(ScenarioA1SpotterCalculator.HoldsAssignment(true, false));
        Assert.False(ScenarioA1SpotterCalculator.HoldsAssignment(true, true));
        Assert.False(ScenarioA1SpotterCalculator.HoldsAssignment(false, false));
    }
}
