using System.Collections.Immutable;
using LimboDancer.Domains.Asl.MapStudio.Services.Acronyms;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ASL.Acronyms;

public sealed class AcronymSearchTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private readonly AcronymCatalog catalog = new(NullLogger<AcronymCatalog>.Instance);

    [Fact]
    public void PublishedResourceHasAllEntries()
    {
        Assert.Null(catalog.Error);
        Assert.Equal(685, catalog.Entries.Length);
        var search = new AcronymSearchService(catalog);
        search.Search(new(Query: "Ocean overlay"));
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var result = search.Search(new(Query: "Ocean overlay"));
        watch.Stop();
        Assert.True(result.Total > 0);
        output.WriteLine($"Warm catalog search: {watch.Elapsed.TotalMilliseconds:F2} ms (685 entries).");
    }

    [Theory]
    [InlineData("DR", "main:DR")]
    [InlineData("dr", "main:dr")]
    [InlineData("HoB", "main:HOB")]
    [InlineData("WWI", "equipment:WW1 / WWI")]
    [InlineData("C#", "main:C#")]
    [InlineData("(a)", "main:(a)")]
    [InlineData("TO&E", "equipment:TO&E")]
    public void ExactFormsRankFirst(string query, string id)
    {
        var result = new AcronymSearchService(catalog).Search(new(Query: query));
        Assert.Equal(id, result.Hits[0].Entry.Id);
    }

    [Fact]
    public void DefinitionsAndCitationTokensRetrieveOceanOverlay()
    {
        var search = new AcronymSearchService(catalog);
        Assert.Contains(search.Search(new(Query: "Ocean overlay")).Hits, h => h.Entry.Id == "main:OC");
        Assert.Contains(search.Search(new(Query: "G13.1")).Hits, h => h.Entry.Id == "main:OC" && h.Reason == "Reference");
        Assert.Contains("Mortar Carrier", catalog.Find("main:MC")!.Definition, StringComparison.Ordinal);
    }

    [Fact]
    public void StrictCaseStillAllowsIndependentProseMatches()
    {
        var hits = new AcronymSearchService(catalog).Search(new(Query: "dr", MatchCase: true)).Hits;
        Assert.Equal("main:dr", hits[0].Entry.Id);
        Assert.DoesNotContain(hits, h => h.Entry.Id == "main:DR" && h.Reason == "Exact acronym");
    }

    [Fact]
    public void AuditQueriesAreOptInAndLowRank()
    {
        var original = catalog.Find("main:OC")!;
        var entry = original with
        {
            Queries = [new("uniquelyinvented audit wording", [])]
        };
        var doc = catalog.Document! with
        {
            Entries = [entry]
        };
        var search = new AcronymSearchService(new AcronymCatalog(doc));
        Assert.Equal(0, search.Search(new(Query: "uniquelyinvented")).Total);
        Assert.Equal("Audit search term", Assert.Single(search.Search(new(Query: "uniquelyinvented", Audit: true)).Hits).Reason);
        Assert.Equal(0, search.Search(new(Query: "uniquelyinvented missing", Audit: true)).Total);
    }

    [Fact]
    public void CitationPrefixIsNotAnExactCitationAndSymbolsDoNotMatchEverything()
    {
        var entry = catalog.Find("main:OC")! with
        {
            Definition = "Ocean overlay.",
            References = "G13.12",
            Source = "G13.12"
        };
        var search = new AcronymSearchService(new AcronymCatalog(catalog.Document! with
        {
            Entries = [entry]
        }));
        Assert.DoesNotContain(search.Search(new(Query: "G13.1")).Hits, h => h.Reason == "Reference");
        Assert.Equal(0, search.Search(new(Query: "!!!")).Total);
    }

    [Fact]
    public void PagingAndDuplicateMeaningsAreStable()
    {
        var search = new AcronymSearchService(catalog);
        var first = search.Search(new());
        Assert.Equal(685, first.Total);
        Assert.Equal(25, first.Hits.Length);
        Assert.Equal(28, search.Search(new(Page: int.MaxValue)).Page);
        Assert.Equal(1, search.Search(new(Query: "zzzzz-no-such-entry", Page: int.MaxValue)).Page);
        Assert.Equal(first.Hits.ToArray(), search.Search(new()).Hits.ToArray());
        Assert.NotEqual(catalog.Find("equipment:PC"), catalog.Find("equipment:PC:2"));
        Assert.Equal(247, search.Search(new(Category: "equipment")).Total);
    }

    [Fact]
    public void UrlStateRoundTripsCaseSymbolsAndInvalidInputs()
    {
        var state = new AcronymSearchState("K/#", "equipment", true, true, 2, "equipment:WW1 / WWI");
        Assert.Equal(state, AcronymSearchState.FromUri(state.ToUri("http://localhost/acronyms")));
        var invalid = AcronymSearchState.FromUri("http://localhost/acronyms?case=oops&audit=2&page=999999999999&category=bad&entry=main%3Adr");
        Assert.False(invalid.MatchCase);
        Assert.False(invalid.Audit);
        Assert.Equal(1, invalid.Page);
        Assert.Equal("all", invalid.Category);
        Assert.Equal("main:dr", invalid.Entry);
    }

    [Fact]
    public void MalformedResourceIsRejected()
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{}"));
        Assert.Throws<InvalidDataException>(() => AcronymCatalog.Read(stream));
    }
}
