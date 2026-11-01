using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Cards;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using EditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Card editor redesign (2026-10-31): the sections and their problem counts, one section shown at a time, the address that keeps it, and the id
/// that follows a new card's title.
/// </summary>
public sealed class CardEditorRedesignTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-card-redesign-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly LivePlay live;

    public CardEditorRedesignTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new BuildingBoards(maps);
        var dice = new ScriptedDice();
        live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        context.Services.AddSingleton(dice);
        context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // A problem goes to the section its field key names; a side's goes to the side whose group, counter, or name it mentions.
    [Fact]
    public void EachProblemGoesToItsSection()
    {
        var card = ScenarioCards.Read("guards-counterattack", live.Catalogs[0])!.Card!;
        var draft = CardDraft.From(card, user: true);
        var german = draft.Sides[0].Groups[0];
        var russian = draft.Sides[1].Groups[0];

        Assert.Equal(CardSections.Card, CardSections.SectionOf("card.date: the day is not a whole number", draft));
        Assert.Equal(CardSections.Map, CardSections.SectionOf("card.boards: board 2's row is not a whole number", draft));
        Assert.Equal(CardSections.Turns, CardSections.SectionOf("card.setup: a sequential setup numbers the groups from 1 (A12.12)", draft));
        Assert.Equal(CardSections.Rules, CardSections.SectionOf("card.ssr: 'night:x' is not a token", draft));
        Assert.Equal(CardSections.Victory, CardSections.SectionOf("card.victory: the exit condition of german names no hex", draft));
        Assert.Equal(CardSections.Side(0), CardSections.SectionOf($"card.elr: '{german.Name}' has an ELR of 0 to 5 (A19.1)", draft));
        Assert.Equal(CardSections.Side(1), CardSections.SectionOf($"card.ob: '{russian.Units[0].Definition}' has a count of at least 1", draft));
        Assert.Equal(CardSections.Side(1), CardSections.SectionOf("card.san: russian's SAN is 0 to 7 (A14.1)", draft));
        Assert.Null(CardSections.SectionOf("not a card problem", draft));
        Assert.Equal("the day is not a whole number", CardSections.Message("card.date: the day is not a whole number"));
    }

    [Theory]
    [InlineData("My game", "my-game")]
    [InlineData("The Guards Counterattack (copy)", "the-guards-counterattack-copy")]
    [InlineData("  Hill 621: day 2!  ", "hill-621-day-2")]
    [InlineData("!!!", "")]
    public void ATitleGivesAnId(string title, string id) => Assert.Equal(id, CardSections.Slug(title));

    // One section is shown at a time; the list counts each section's problems, and a group's problem is shown under that group.
    [Fact]
    public void TheListShowsOneSectionAndCountsItsProblems()
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("units/cards/edit?card=guards-counterattack");
        var editor = context.Render<EditorPage>();
        Assert.False(editor.Find("#edit-section-card").HasAttribute("hidden"));
        Assert.True(editor.Find("#edit-section-side-1").HasAttribute("hidden"));
        Assert.True(editor.Find("#edit-map-dock").HasAttribute("hidden"));
        Assert.Equal("Valid", editor.Find("#edit-problems-toggle").TextContent.Trim());

        editor.Find("#edit-nav-side-1").Click();
        Assert.True(editor.Find("#edit-section-card").HasAttribute("hidden"));
        Assert.False(editor.Find("#edit-section-side-1").HasAttribute("hidden"));
        Assert.False(editor.Find("#edit-map-dock").HasAttribute("hidden"));
        Assert.Equal("true", editor.Find("#edit-nav-side-1").GetAttribute("aria-current"));
        Assert.Contains("section=side-1", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);

        editor.Find("#edit-group-1-0-unit-0-count").Change("0");
        Assert.Contains("count of at least 1", editor.Find("#edit-problems-edit-group-1-0").TextContent, StringComparison.Ordinal);
        Assert.Contains("card.ob", editor.Find("#edit-diagnostics").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(editor.Find("#edit-nav-side-1").QuerySelectorAll(".card-section-count"));
        Assert.Empty(editor.Find("#edit-nav-side-0").QuerySelectorAll(".card-section-count"));
        Assert.Contains("problem", editor.Find("#edit-problems-toggle").TextContent, StringComparison.Ordinal);
        Assert.Equal("a copy of a built-in card, not saved", editor.Find("#edit-state").TextContent);
    }

    // A new card's id follows its title until the id is typed by hand.
    [Fact]
    public void ANewCardsIdFollowsItsTitleUntilTyped()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-title").Change("Village Fight");
        Assert.Equal("village-fight", editor.Find("#edit-id").GetAttribute("value"));

        editor.Find("#edit-id").Change("my-own-id");
        editor.Find("#edit-title").Change("Another title");
        Assert.Equal("my-own-id", editor.Find("#edit-id").GetAttribute("value"));
    }

    // The address names the section to open, and a section the editor lacks opens the first.
    [Theory]
    [InlineData("victory", "#edit-section-victory")]
    [InlineData("no-such-section", "#edit-section-card")]
    public void TheAddressNamesTheSection(string section, string shown)
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo($"units/cards/edit?section={section}");
        var editor = context.Render<EditorPage>();
        Assert.False(editor.Find(shown).HasAttribute("hidden"));
        Assert.Single(editor.FindAll(".card-section:not([hidden])"));
    }
}
