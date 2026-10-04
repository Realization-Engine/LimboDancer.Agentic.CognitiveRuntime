using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

/// <summary>
/// Range read before an attack (pass 31c, design section 14; ruling R31c.7): <see cref="FireRange"/> resolves nothing, so its bands are held
/// to what the Fire package itself does at every range: its Point Blank and Long Range multipliers and its refusal for range.
/// </summary>
public sealed class FireRangeTests
{
    private static readonly ScenarioA1FireReference Reference = new ScenarioA1FirePackage().Reference;
    private const string From = "bd01:F5:0";
    private const string At = "bd01:G5:0";

    private static FireAttack Attack(string firer, int range, string target = At) =>
        new("PFPh", "phasing", true, From, target, [new FireFirer("ru-1", firer, From, false, false, false, false, false)], null, range, true,
            new FireLos(false, 0, true, false), null, "open-ground",
            [new FireTarget("de-s", "attacker-squad", target, false, false, false, false, false, false, false)], 3, new FireRolls([3, 3], null, null, null));

    public static TheoryData<string> Squads => new() { "defender-squad", "defender-conscript-squad", "defender-guards-squad", "defender-elite-squad" };

    [Theory]
    [MemberData(nameof(Squads))]
    public void ABandIsWhatTheFirePackageDoesAtThatRange(string squad)
    {
        var definition = Reference.Definitions[squad];
        var normal = definition.Range!.Value;
        for (var range = 1; range <= (2 * normal) + 1; range++)
        {
            var attack = Attack(squad, range);
            var reading = FireRange.Band(definition, range, sameLocation: false)!;
            var refused = ScenarioA1FireCalculator.Precheck(attack, Reference).Contains("asl.a1.fire.out-of-range");
            Assert.Equal(reading.Band == FireRangeBand.Out, refused);
            Assert.Equal(2 * normal, reading.Limit);
            if (refused)
            {
                continue;
            }

            var multipliers = ScenarioA1FireCalculator.Preview(attack, Reference)!.Firers.Single().Multipliers.Select(item => item.Name).ToArray();
            Assert.Equal(reading.Band == FireRangeBand.PointBlank, multipliers.Contains("point-blank-fire"));
            Assert.Equal(reading.Band == FireRangeBand.LongRange, multipliers.Contains("long-range-fire"));
            Assert.Equal(reading.Multiplier, multipliers.Contains("point-blank-fire") ? 2m : multipliers.Contains("long-range-fire") ? 0.5m : 1m);
        }
    }

    [Fact]
    public void TheFirersOwnLocationIsTpbfAndAnotherLevelOfItsHexIsNotBuilt()
    {
        var definition = Reference.Definitions["defender-squad"];

        // A7.21 (ruling R10.14): TPBF at the enemy units of the firer's own Location, in the MPh.
        var own = FireRange.Band(definition, 0, sameLocation: true)!;
        Assert.Equal((FireRangeBand.TriplePointBlank, 3m), (own.Band, own.Multiplier));
        var tpbf = Attack("defender-squad", 0, From) with
        {
            Phase = "MPh",
            FiringSide = "non-phasing",
            FireKind = ScenarioA1FireCalculator.FirstFire,
            TargetMovement = new FireMovement(false),
        };
        Assert.DoesNotContain("asl.a1.fire.out-of-range", ScenarioA1FireCalculator.Precheck(tpbf, Reference));
        Assert.Contains("triple-point-blank-fire", ScenarioA1FireCalculator.Preview(tpbf, Reference)!.Firers.Single().Multipliers.Select(item => item.Name));

        // Range 0 at another Location of the hex: the package refuses it, and the band says it is not built.
        var level = FireRange.Band(definition, 0, sameLocation: false)!;
        Assert.Equal(FireRangeBand.SameHexOtherLevel, level.Band);
        Assert.Null(level.Multiplier);
        Assert.Contains("asl.a1.fire.out-of-range", ScenarioA1FireCalculator.Precheck(Attack("defender-squad", 0), Reference));
    }

    [Fact]
    public void PointBlankFireNeedsATargetAtMostOneLevelAbove()
    {
        // A7.21: adjacent and within one level of, or higher than, the target.
        Assert.Equal(FireRangeBand.PointBlank, FireRange.Band(1, false, 4, levelAbove: 1).Band);
        Assert.Equal(FireRangeBand.PointBlank, FireRange.Band(1, false, 4, levelAbove: -2).Band);
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(1, false, 4, levelAbove: 2).Band);
    }

    [Fact]
    public void AnAtrHasNoLongRangeAndAFtNoPointBlankFire()
    {
        // C13.24: an ATR fires to its Normal Range only.
        var atr = Reference.Definitions["defender-atr"];
        var normal = atr.Range!.Value;
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(atr, normal, false)!.Band);
        var beyond = FireRange.Band(atr, normal + 1, false)!;
        Assert.Equal((FireRangeBand.Out, normal), (beyond.Band, beyond.Limit));

        // A22.32 (ruling R15.1): an Infantry FT attacks an adjacent hex at full FP, never raised for PBF, or one two hexes away at Long Range.
        var ft = Reference.Definitions["attacker-ft"];
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(ft, 1, false)!.Band);
        Assert.Equal(FireRangeBand.LongRange, FireRange.Band(ft, 2, false)!.Band);
        Assert.Equal(FireRangeBand.Out, FireRange.Band(ft, 3, false)!.Band);
        Assert.Equal(FireRangeBand.Normal, FireRange.Band(ft, 0, sameLocation: true)!.Band);
    }

    [Fact]
    public void ALeaderHasNoBandAndAWoundedHeroReadsHisWoundedRange()
    {
        Assert.Null(FireRange.Band(Reference.Definitions["defender-leader"], 3, false));
        var hero = Reference.Definitions["defender-hero"];
        Assert.Equal(hero.WoundedRange ?? hero.Range, FireRange.Band(hero, 1, false, wounded: true)!.NormalRange);
    }

    [Theory]
    [InlineData(FireRangeBand.TriplePointBlank, "TPBF, FP x3 (A7.21)")]
    [InlineData(FireRangeBand.PointBlank, "PBF, FP x2 (A7.21)")]
    [InlineData(FireRangeBand.Normal, "Normal Range")]
    [InlineData(FireRangeBand.LongRange, "Long Range, FP x1/2 (A7.22)")]
    [InlineData(FireRangeBand.Out, "out of range: it fires to 12")]
    [InlineData(FireRangeBand.SameHexOtherLevel, "the same hex, another level: not built")]
    public void ABandIsSaidInAPlayersWords(FireRangeBand band, string words) =>
        Assert.Equal(words, FireRange.Words(new FireRangeReading(band, 7, 6, 12)));
}
