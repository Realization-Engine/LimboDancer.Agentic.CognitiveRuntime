using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Play.Tests;

/// <summary>A scenario card's provenance (plan task 22b.5, section 13.6): identity, the game's start, counters' sources, and citations.</summary>
public sealed class CardProvenanceTests
{
    private static readonly UnitCatalog Catalog = UnitCatalogs.Read(UnitCatalogs.ScenarioA1, UnitVocabulary.Asl())!.Catalog!;

    private static CardProvenance Of(string name, ScenarioCardReferenceFacts? game = null) =>
        CardProvenance.Of(ScenarioCards.Read(name, Catalog)!.Card!, false, null, ScenarioCards.Sha256(name)!, Catalog, game);

    // 22b.5's acceptance: every citation of a built-in card resolves to a registered fragment's page or to a ruling.
    [Theory]
    [InlineData("guards-counterattack")]
    [InlineData("gambit")]
    [InlineData("tractor-works")]
    public void EveryCitationOfABuiltInCardResolves(string name)
    {
        var provenance = Of(name);
        Assert.NotEmpty(provenance.Citations);
        Assert.All(provenance.Citations, citation => Assert.NotEqual(CitationKind.Unregistered, citation.Kind));
        Assert.All(provenance.Citations.Where(citation => citation.Kind == CitationKind.Fragment), citation =>
        {
            Assert.NotEmpty(citation.Pages);
            Assert.StartsWith("asl-fragment:sha256:", citation.FragmentId, StringComparison.Ordinal);
            Assert.Contains("match", citation.Comparison, StringComparison.Ordinal);
        });
        Assert.All(provenance.Citations.Where(citation => citation.Kind == CitationKind.Ruling), citation => Assert.Equal(CardProvenance.RulingsLink, citation.Link));
    }

    [Fact]
    public void ARegisteredFragmentGivesItsPdfPageAndAnUnknownRuleIsMarked()
    {
        var victory = CardProvenance.Citation("A26.1");
        Assert.Equal(CitationKind.Fragment, victory.Kind);
        Assert.Equal([98], victory.Pages);
        Assert.Equal(CitationKind.Ruling, CardProvenance.Citation("R17.2").Kind);
        Assert.Equal(CitationKind.Unregistered, CardProvenance.Citation("E1.1").Kind);
    }

    [Fact]
    public void CountersCarryTheirSourcesAndManufacturedValuesAreMarked()
    {
        var provenance = Of("guards-counterattack");
        Assert.Equal(("guards-counterattack", "asl-scenario-card/1"), (provenance.Identity.Id, provenance.Identity.Format));
        Assert.Contains(provenance.Counters, counter => counter.Manufactured && counter.Counter.Sheet == "MFG");
        Assert.All(provenance.Counters, counter =>
        {
            Assert.True(counter.Count > 0);
            Assert.NotNull(counter.Source);
            Assert.All(counter.Values, value => Assert.Equal(counter.Counter, value.Source.Counter));
        });
        Assert.Equal(provenance.Counters.Sum(counter => counter.Count),
            ScenarioCards.Read("guards-counterattack", Catalog)!.Card!.Sides.Sum(side => side.Groups.Sum(group => group.Units.Sum(unit => unit.Count)) + (side.BalanceUnits?.Sum(unit => unit.Count) ?? 0)));
    }

    [Fact]
    public void AGameSaysWhetherItsCardStillMatches()
    {
        var sha = ScenarioCards.Sha256("gambit")!;
        Assert.Equal(CardMatch.Same, Of("gambit", new("gambit", sha, "asl-scenario-a1@1.12.0", sha)).Game!.Match);
        Assert.Equal(CardMatch.Changed, Of("gambit", new("gambit", "0" + sha[1..], "asl-scenario-a1@1.12.0", sha)).Game!.Match);
        Assert.Equal(CardMatch.Gone, Of("gambit", new("gambit", sha, "asl-scenario-a1@1.12.0", null)).Game!.Match);
    }
}
