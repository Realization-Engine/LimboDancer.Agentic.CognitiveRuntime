using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Pages;
using LimboDancer.Domains.Asl.MapStudio.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Scenario cards page (backlog pass 17, ruling R17.1): a card is read and shown, not played.</summary>
public sealed class ScenarioCardsPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-cards-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();

    public ScenarioCardsPageTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        context.Services.AddSingleton(library);

        // Pass 22 (ruling R22.2): the page lists the cards of the Studio's library, built-in and the user's.
        context.Services.AddSingleton(new LivePlay(library, new BuildingBoards(new MapService(options, new FakeVaslMapSource()))));
    }

    [Fact]
    public void GambitIsShownFirstWithItsEntryExitAndManufacturedCounters()
    {
        var page = context.Render<Scenarios>();
        Assert.Empty(page.FindAll("#card-diagnostics"));
        Assert.Equal("Gambit", page.Find("#card-title").TextContent);
        Assert.Contains("21 May 1941", page.Find("#card-place").TextContent, StringComparison.Ordinal);
        Assert.Contains("bd04 in the first row from the top; bd02 in the second row from the top, turned 180 degrees", page.Find("#card-boards").TextContent,
            StringComparison.Ordinal);
        Assert.Contains("8 Game Turns", page.Find("#card-turns").TextContent, StringComparison.Ordinal);
        Assert.Contains("British sets up first; German moves first", page.Find("#card-turns").TextContent, StringComparison.Ordinal);
        Assert.StartsWith("None", page.Find("#card-defender").TextContent.Trim(), StringComparison.Ordinal);

        var british = page.Find("#card-side-british");
        Assert.Contains("The top edge (north), from its entry", british.QuerySelector(".card-edge")!.TextContent, StringComparison.Ordinal);
        Assert.Equal("none printed", british.QuerySelector(".card-integrity")!.TextContent);
        Assert.Contains("4-5-8 elite squad", british.TextContent, StringComparison.Ordinal);
        Assert.Contains("51mm light mortar", british.TextContent, StringComparison.Ordinal);
        Assert.Contains("enters on Turn 1 along the top edge (north)", british.TextContent, StringComparison.Ordinal);
        Assert.Equal(2, british.QuerySelectorAll(".card-manufactured").Length);
        Assert.Contains("5-4-8 elite squad", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);

        Assert.Contains("Shown, not enforced", page.Find("#card-ssr").TextContent, StringComparison.Ordinal);
        Assert.Contains("evaluated by the game", page.Find("#card-victory").TextContent, StringComparison.Ordinal);
        Assert.Contains("paraphrased", page.Find("#card-adaptation").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TheGuardsCounterattackShowsItsBuildingsIntegrityAndDefaultWeather()
    {
        var page = context.Render<Scenarios>();
        page.Find("#card-choice").Change("guards-counterattack");
        Assert.Equal("The Guards Counterattack", page.Find("#card-title").TextContent);
        Assert.Contains("Only hexrows A to P are playable. (enforced:", page.Find("#card-playable").TextContent, StringComparison.Ordinal);
        Assert.StartsWith("[130] (A16 is optional", page.Find("#card-side-german .card-integrity").TextContent, StringComparison.Ordinal);
        Assert.StartsWith("[207]", page.Find("#card-side-russian .card-integrity").TextContent, StringComparison.Ordinal);
        Assert.Contains("building F3 (E4, F3, G3, G4)", page.Find("#card-side-russian").TextContent, StringComparison.Ordinal);
        Assert.Contains("9-0 Commissar", page.Find("#card-side-russian").TextContent, StringComparison.Ordinal);
        Assert.Contains("ruling R16.9", page.Find("#card-ssr").TextContent, StringComparison.Ordinal);
        Assert.Contains("building M9", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.Contains("heavy MG 7-16", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.Contains("10-2 leader", page.Find("#card-side-russian").TextContent, StringComparison.Ordinal);
        Assert.Contains("ELR 4", page.Find("#card-side-german caption").TextContent, StringComparison.Ordinal);
        Assert.Contains("EC are Moderate", page.Find("#card-ssr").TextContent, StringComparison.Ordinal);
        Assert.Contains("does not have (backlog). (A7.7)", page.Find("#card-ssr").TextContent, StringComparison.Ordinal);
        Assert.Equal(3, page.FindAll("#card-ssr .card-ssr-not-enforced").Count);
    }

    [Fact]
    public void TheTractorWorksShowsItsSequentialSetupAndDummies()
    {
        var page = context.Render<Scenarios>();
        page.Find("#card-choice").Change("tractor-works");
        Assert.Empty(page.FindAll("#card-diagnostics"));
        Assert.Equal("The Tractor Works", page.Find("#card-title").TextContent);
        Assert.Contains("Russian sets up first. A die roll before play decides which side moves first.", page.Find("#card-turns").TextContent, StringComparison.Ordinal);
        var russian = page.Find("#card-side-russian");
        Assert.Contains("Elements of the 308th Rifle Division, ELR 3, sets up first", russian.TextContent, StringComparison.Ordinal);
        Assert.Contains("sets up last", russian.TextContent, StringComparison.Ordinal);
        Assert.Contains("18 \"?\" counters", russian.TextContent, StringComparison.Ordinal);
        Assert.Contains("heavy MG 6-12", russian.TextContent, StringComparison.Ordinal);
        Assert.Contains("8-3-8 elite squad", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.Contains("FT 24-1", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.Contains("DC 30", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("attacker-", page.Find("#card").TextContent, StringComparison.Ordinal);
        Assert.Contains("sets up second", page.Find("#card-side-german").TextContent, StringComparison.Ordinal);
        Assert.Contains("The bottom edge (south), from its setup (A20.53).", page.Find("#card-side-german .card-edge").TextContent, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
