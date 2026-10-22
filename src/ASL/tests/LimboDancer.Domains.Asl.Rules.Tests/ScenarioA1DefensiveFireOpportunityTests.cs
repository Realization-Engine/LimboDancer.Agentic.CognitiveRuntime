using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// The Play streamlining plan, S6 (ruling Q8): whether the DEFENDER could fire at a moving stack's step, case by case, and that a fact not known
/// always counts as "may fire".
/// </summary>
public sealed class ScenarioA1DefensiveFireOpportunityTests
{
    /// <summary>A Good Order squad with clear LOS to the step, three hexes away, Normal Range four, not yet fired.</summary>
    private static DefensiveFirerFacts Squad() => new("r1", false, false, null, true, true, 3, 4, false, false);

    [Fact]
    public void AGoodOrderUnitInLosAndInReachMayFire() => Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad()));

    // A dummy has no FP; a broken unit may not attack (A10.4); a unit the fire bar holds may not fire.
    [Fact]
    public void ADummyABrokenUnitAndABarredUnitMayNot()
    {
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            Dummy = true
        }));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            Broken = true
        }));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FireBar = "held in Melee"
        }));
    }

    // A8.1: in LOS of the step; A8.15: or of the hexside crossed, which the Location left stands for.
    [Fact]
    public void LosToTheStepOrToTheLocationLeftIsNeeded()
    {
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            LosToStep = false,
            LosToOrigin = false
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            LosToStep = false,
            LosToOrigin = true
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            LosToStep = false,
            LosToOrigin = null
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            LosToStep = null,
            LosToOrigin = false
        }));
    }

    // A7.22: an unfired unit reaches to Long Range, twice its Normal Range.
    [Fact]
    public void AnUnfiredUnitReachesToLongRange()
    {
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            Range = 8
        }));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            Range = 9
        }));
    }

    // A8.3: Subsequent First Fire only within Normal Range; A8.31: Final Fire only as FPF at an ADJACENT or same-hex stack.
    [Fact]
    public void FirstAndFinalFireMarkersShortenTheReach()
    {
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FirstFire = true,
            Range = 4
        }));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FirstFire = true,
            Range = 5
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FinalFire = true,
            Range = 1
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FinalFire = true,
            Range = 0
        }));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            FinalFire = true,
            Range = 2
        }));
    }

    // The answer errs one way: a range or a Normal Range not known counts as "may fire".
    [Fact]
    public void AFactNotKnownCountsAsMayFire()
    {
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            Range = null,
            FinalFire = true
        }));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire(Squad() with
        {
            NormalRange = null,
            Range = 30
        }));
    }

    // A8.312: Mandatory Fire owed is the DEFENDER's to resolve, so he is asked even when no unit's own facts say it may fire.
    [Fact]
    public void TheSideMayFireWhenAnyUnitMayOrMandatoryFireIsOwed()
    {
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire([], false));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire([], true));
        Assert.False(ScenarioA1DefensiveFireOpportunity.MayFire([Squad() with { Range = 12 }, Squad() with { Broken = true }], false));
        Assert.True(ScenarioA1DefensiveFireOpportunity.MayFire([Squad() with { Range = 12 }, Squad()], false));
    }
}
