using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// C9 Mortars (p. 179) less C9.3, as <see cref="ScenarioA1MortarCalculator"/> decides it (the C9 claim): what a mortar is, its Target Type and
/// Indirect Fire, the light mortar as a SW, A21.13's lone SMC, the minimum range, and the Critical Hit.
/// </summary>
public sealed class ScenarioA1MortarCalculatorTests
{
    private static MortarShotFacts Shot => new(Mortar: true, Caliber: 50, MinimumRange: 2, Range: 2, AreaTargetType: true, NamesVehicle: false, LoneSmc: false);

    [Fact]
    public void AMortarIsALightMortarSwOrAGunOfAMortarGunTypeAndLightIsSixtyMillimetresOrLess()
    {
        // Ruling RC9.2: both spellings the code has; C9.2: 60mm or less, an unknown caliber not light.
        Assert.True(ScenarioA1MortarCalculator.IsMortar("asl:light-mortar", null));
        Assert.True(ScenarioA1MortarCalculator.IsMortar("asl:gun", "mtr"));
        Assert.True(ScenarioA1MortarCalculator.IsMortar(null, "mortar"));
        Assert.False(ScenarioA1MortarCalculator.IsMortar("asl:gun", "at"));
        Assert.False(ScenarioA1MortarCalculator.IsMortar(null, null));
        Assert.True(ScenarioA1MortarCalculator.IsLight(60));
        Assert.True(ScenarioA1MortarCalculator.IsLight(50));
        Assert.False(ScenarioA1MortarCalculator.IsLight(61));
        Assert.False(ScenarioA1MortarCalculator.IsLight(81));
        Assert.False(ScenarioA1MortarCalculator.IsLight(null));
        Assert.False(ScenarioA1MortarCalculator.IsLight(0));
    }

    [Fact]
    public void AMortarFiresOnlyOnTheAreaTargetTypeAsIndirectFireAndNamesNoVehicle()
    {
        // C9.1, C1.55 (ruling RC9.3): the calculator says nothing of a non-mortar's Target Type.
        Assert.True(ScenarioA1MortarCalculator.IndirectFire(true));
        Assert.False(ScenarioA1MortarCalculator.IndirectFire(false));
        Assert.True(ScenarioA1MortarCalculator.IndirectFireOfHit(true));
        Assert.NotNull(ScenarioA1MortarCalculator.TargetTypeRefusal(true, false));
        Assert.Null(ScenarioA1MortarCalculator.TargetTypeRefusal(true, true));
        Assert.Null(ScenarioA1MortarCalculator.TargetTypeRefusal(false, false));
        Assert.Contains("C9.1", ScenarioA1MortarCalculator.VehicleTargetRefusal(true, true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1MortarCalculator.VehicleTargetRefusal(true, false));
        Assert.Null(ScenarioA1MortarCalculator.VehicleTargetRefusal(false, true));
    }

    [Fact]
    public void ALightMortarHasNoCaAndNeedsNoCrewAndALoneSmcLosesItsMultipleRof()
    {
        // C9.2, A21.13 (ruling RC9.4).
        Assert.False(ScenarioA1MortarCalculator.HasCoveredArc(true));
        Assert.True(ScenarioA1MortarCalculator.HasCoveredArc(false));
        Assert.False(ScenarioA1MortarCalculator.CrewNeeded(true));
        Assert.True(ScenarioA1MortarCalculator.CrewNeeded(false));
        Assert.True(ScenarioA1MortarCalculator.UsesAreaAcquisition(true));
        Assert.True(ScenarioA1MortarCalculator.MultipleRofKept(false));
        Assert.False(ScenarioA1MortarCalculator.MultipleRofKept(true));

        // A21.13 (p. 88): a lone SMC fires a mortar of 82mm or less as if it were light, and no larger one.
        Assert.True(ScenarioA1MortarCalculator.FiresAsLight(50, loneSmc: false));
        Assert.False(ScenarioA1MortarCalculator.FiresAsLight(81, loneSmc: false));
        Assert.True(ScenarioA1MortarCalculator.FiresAsLight(81, loneSmc: true));
        Assert.True(ScenarioA1MortarCalculator.FiresAsLight(82, loneSmc: true));
        Assert.False(ScenarioA1MortarCalculator.FiresAsLight(83, loneSmc: true));
        Assert.False(ScenarioA1MortarCalculator.FiresAsLight(null, loneSmc: true));
        Assert.Null(ScenarioA1MortarCalculator.LoneSmcRefusal(82, true));
        Assert.Contains("A21.13", ScenarioA1MortarCalculator.LoneSmcRefusal(120, true), StringComparison.Ordinal);
        Assert.Null(ScenarioA1MortarCalculator.LoneSmcRefusal(120, false));
    }

    [Fact]
    public void AMortarNeverFiresNearerThanItsMinimumRangeAndAnUnknownRangeOrMinimumIsRefused()
    {
        // C9.4 (ruling RC9.5): at the minimum is allowed, one nearer is not.
        Assert.Null(ScenarioA1MortarCalculator.MinimumRangeRefusal(true, 2, 2));
        Assert.Contains("minimum range of 2", ScenarioA1MortarCalculator.MinimumRangeRefusal(true, 1, 2), StringComparison.Ordinal);
        Assert.NotNull(ScenarioA1MortarCalculator.MinimumRangeRefusal(true, null, 2));
        Assert.NotNull(ScenarioA1MortarCalculator.MinimumRangeRefusal(true, 5, null));
        Assert.Null(ScenarioA1MortarCalculator.MinimumRangeRefusal(false, 5, null));

        // The ordnance calculator's own check, for every Gun: only a recorded minimum bars.
        Assert.True(ScenarioA1MortarCalculator.BelowMinimum(1, 2));
        Assert.False(ScenarioA1MortarCalculator.BelowMinimum(2, 2));
        Assert.False(ScenarioA1MortarCalculator.BelowMinimum(1, null));
        Assert.False(ScenarioA1MortarCalculator.BelowMinimum(null, 2));
    }

    [Fact]
    public void TheShotsVerdictGivesItsFirstRefusalInOrder()
    {
        Assert.Equal(new MortarShotVerdict(null, true, true), ScenarioA1MortarCalculator.Shot(Shot));
        Assert.Contains("Area Target Type", ScenarioA1MortarCalculator.Shot(Shot with
        {
            AreaTargetType = false,
            NamesVehicle = true,
            Range = 1
        }).Refusal, StringComparison.Ordinal);
        Assert.Contains("To Kill", ScenarioA1MortarCalculator.Shot(Shot with
        {
            NamesVehicle = true,
            Range = 1
        }).Refusal, StringComparison.Ordinal);
        Assert.Contains("A21.13", ScenarioA1MortarCalculator.Shot(Shot with
        {
            Caliber = 120,
            LoneSmc = true,
            Range = 1
        }).Refusal, StringComparison.Ordinal);
        Assert.Contains("minimum range", ScenarioA1MortarCalculator.Shot(Shot with
        {
            Range = 1
        }).Refusal, StringComparison.Ordinal);

        // A lone SMC fires the light mortar and loses its Multiple ROF; with an 81mm mortar it fires it as light (A21.13).
        Assert.Equal(new MortarShotVerdict(null, true, false), ScenarioA1MortarCalculator.Shot(Shot with
        {
            LoneSmc = true
        }));
        Assert.Equal(new MortarShotVerdict(null, true, false), ScenarioA1MortarCalculator.Shot(Shot with
        {
            Caliber = 81,
            MinimumRange = 3,
            Range = 3,
            LoneSmc = true
        }));
        Assert.Throws<ArgumentNullException>(() => ScenarioA1MortarCalculator.Shot(null!));
    }

    [Fact]
    public void AMortarsCriticalHitComesOnlyOnAnOriginalTwoAtTheFullHeFirepower()
    {
        // C9.5 (ruling RC9.6): C3.71 doubles it later, in the fire calculator.
        Assert.True(ScenarioA1MortarCalculator.CriticalHitAllowed(2));
        Assert.False(ScenarioA1MortarCalculator.CriticalHitAllowed(3));
        Assert.Equal(8, ScenarioA1MortarCalculator.CriticalFirepower(8));
    }
}
