using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>An original Spotter designation (C9.3), as <see cref="ScenarioA1SpotterCalculator.DesignationRefusal"/> decides it (the C9.3 claim).</summary>
public sealed class ScenarioA1SpotterDesignationTests
{
    private static SpotterDesignationFacts Eligible => new(
        new("a", new(1, 1, "a", "pfph", TurnCheckpoint.DuringPhase), false, SpotterChange.None),
        true, new SpotterCandidateFacts(true, true, true, true, false, false, false), 1, true, false, false, false, false, false,
        new("s", false, false, false, false, null, false), false);

    [Fact]
    public void ATiSpotterMayNotSpotAndATiMortarIsNotDesignated()
    {
        // A4.8 (p. 52), C9.3 (the review of Wiring Prompt 001, C931): each is its own fact, so neither stands in for the other.
        Assert.Contains("A4.8", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            SpotterTemporarilyImmobilized = true
        }), StringComparison.Ordinal);
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            SpotterTemporarilyImmobilized = false
        }));
        Assert.Contains("Temporarily Immobilized", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            TemporarilyImmobilized = true,
            SpotterTemporarilyImmobilized = false
        }), StringComparison.Ordinal);

        // A caller that does not gather the Spotter's TI yet keeps its old answer.
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            SpotterTemporarilyImmobilized = null
        }));
    }

    [Fact]
    public void OriginalDaytimeDeclarationDoesNotGrantAReplacementOrAnUnbuiltProfile()
    {
        Assert.Equal(UnitCondition.PrepFire, ScenarioA1SpotterCalculator.FireCounter("pfph"));
        Assert.Equal(UnitCondition.FinalFire, ScenarioA1SpotterCalculator.FireCounter("dfph"));
        Assert.Throws<ArgumentException>(() => ScenarioA1SpotterCalculator.FireCounter("mph"));
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible));
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                Now = new(1, 2, "b", "dfph", TurnCheckpoint.DuringPhase)
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                HasDesignation = true
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Timing = Eligible.Timing with
            {
                HasDesignation = null
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Night = true
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            PendingDecision = true
        }));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1SpotterCalculator.DesignationRefusal(null!));
    }

    [Fact]
    public void EligibilityNeedsKnownReachGoodOrderOwnershipAndAvailableSupportWeaponUse()
    {
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Mortar = false
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Candidate = Eligible.Candidate with
            {
                SameSide = false
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Candidate = Eligible.Candidate with
            {
                GoodOrder = false
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Candidate = Eligible.Candidate with
            {
                MortarCrew = true
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            TemporarilyImmobilized = true
        }));
        Assert.StartsWith("Prove", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            HexRange = null
        }), StringComparison.Ordinal);
        Assert.Contains("2 hexes away", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            HexRange = 2
        }), StringComparison.Ordinal);
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            HexRange = 0
        }));
        Assert.StartsWith("Prove", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            SharesLocation = null
        }), StringComparison.Ordinal);
        Assert.Contains("one Location", ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            SharesLocation = false
        }), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Fired = true
        }));
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Fired = true,
            UsedAsSpotter = true
        }));
        Assert.Null(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
        {
            Squad = true,
            Support = Eligible.Support with
            {
                Squad = true
            }
        }));
        Assert.NotNull(ScenarioA1SpotterCalculator.DesignationRefusal(Eligible with
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
