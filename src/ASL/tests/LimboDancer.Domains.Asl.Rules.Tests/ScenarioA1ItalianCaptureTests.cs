using LimboDancer.Domains.Asl.Rules;
using Xunit;

namespace LimboDancer.Domains.Asl.Rules.Tests;

public sealed class ScenarioA1ItalianCaptureTests
{
    [Theory]
    [InlineData("italian", false, false, 0)]
    [InlineData("italian", true, false, 1)]
    [InlineData("italian", false, true, -1)]
    [InlineData("eritrean", false, false, 0)]
    [InlineData("eritrean", true, false, 1)]
    [InlineData("eritrean", false, true, -1)]
    [InlineData("german", null, false, 1)]
    [InlineData(null, null, true, -1)]
    [InlineData(null, true, false, 1)]
    [InlineData("italian", null, false, null)]
    [InlineData("italian", false, null, null)]
    public void CaptureTermsAreAlternativesForEachDefender(string? nation, bool? elite, bool? inexperienced, int? expected)
    {
        Assert.Equal(expected, ScenarioA1ItalianCaptureCalculator.CaptureModifier(new(nation, elite, inexperienced)).CaptureDrm);
    }

    [Fact]
    public void MixedDefendersNeverShareTheMostFavorableTerm()
    {
        ItalianCaptureFacts[] defenders = [new("italian", false, false), new("italian", false, true), new("italian", true, false), new("german", false, false)];
        Assert.Equal(new int?[] { 0, -1, 1, 1 }, defenders.Select(facts => ScenarioA1ItalianCaptureCalculator.CaptureModifier(facts).CaptureDrm));
    }

    [Theory]
    [InlineData("italian")]
    [InlineData("eritrean")]
    [InlineData("japanese")]
    [InlineData("hungarian")]
    [InlineData("german")]
    [InlineData(null)]
    public void EscapeInheritanceIsOptInAndPreservesEveryOtherBranch(string? nationality)
    {
        foreach (var infantry in new[] { false, true })
        {
            foreach (var melee in new[] { false, true })
            {
                foreach (var broken in new[] { false, true })
                {
                    var facts = new PrisonerEscapeFacts("p", nationality, "russian", infantry, true, broken, melee);
                    var old = ScenarioA1PrisonerCalculator.Escape(facts);
                    Assert.Equal(old, ScenarioA1PrisonerCalculator.Escape(facts, ItalianEscapeContext.Legacy));
                    var originalRefusal = ScenarioA1PrisonerCalculator.EscapeRefusal("p", nationality, "russian", infantry, true)
                        ?? (broken || melee ? null : ScenarioA1PrisonerCalculator.EscapeGuardText("p"));
                    Assert.Equal(originalRefusal, old.Refusal);
                    var current = ScenarioA1PrisonerCalculator.Escape(facts, ItalianEscapeContext.WithEritreanInheritance);
                    if (nationality == "eritrean" && infantry)
                    {
                        Assert.Equal("A25.66", current.RefusalRule);
                        Assert.Equal(PrisonerEscapeBar.Nationality, current.Bar);
                        Assert.Equal(broken || melee, old.MayAttack);
                    }
                    else
                    {
                        Assert.Equal(old, current);
                    }
                }
            }
        }
    }
}
