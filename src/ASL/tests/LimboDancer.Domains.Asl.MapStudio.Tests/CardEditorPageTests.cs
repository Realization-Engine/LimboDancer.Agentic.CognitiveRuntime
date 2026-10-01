using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using EditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;
using ScenariosPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Scenarios;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The card editor (pass 22 of the Scenario Card Games Plan, rulings R22.1 to R22.4): a minimal card or a copy of a built-in card, checked as it is
/// edited, saved under the boards folder, listed on the Play and Scenarios pages, and played.
/// </summary>
public sealed class CardEditorPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-card-editor-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly LivePlay live;

    public CardEditorPageTests()
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
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
        context.Services.AddSingleton(dice);
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // R22.1, R22.3: a new minimal card validates as edited and is saved under the boards folder; a bad id is refused before saving.
    [Fact]
    public void AMinimalCardIsEditedAndSaved()
    {
        var editor = context.Render<EditorPage>();
        Assert.NotNull(editor.Find("#edit-valid"));
        editor.Find("#edit-id").Change("guards-counterattack");
        Assert.Contains("built-in card", editor.Find("#edit-diagnostics").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-save").Click();
        Assert.Contains("Not saved", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("guards-counterattack", live.Cards.UserNames);
        editor.Find("#edit-id").Change("village-fight");
        editor.Find("#edit-title").Change("Village fight");
        editor.Find("#edit-elr-0").Change("9");
        Assert.Contains("card.elr", editor.Find("#edit-diagnostics").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-elr-0").Change("3");
        editor.Find("#edit-edge-1").Change("top");
        editor.Find("#edit-month").Change("7");
        editor.Find("#edit-save").Click();
        Assert.Contains("Saved 'village-fight'", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "boards", "cards", "village-fight.scenario-card.json")));
        Assert.Contains("village-fight", live.Cards.UserNames);
    }

    // R22.2: a built-in card is copied under a new id; the counter picker adds a line to a group; the copy is listed as yours and edited again.
    [Fact]
    public void ABuiltInCardIsCopiedAndChanged()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("guards-counterattack");
        Assert.Equal("guards-counterattack-copy", editor.Find("#edit-id").GetAttribute("value"));
        Assert.NotNull(editor.Find("#edit-valid"));
        editor.Find("#edit-pick-side").Change("0");
        editor.Find("#edit-pick-group").Change("1");
        editor.Find("#edit-pick-definition").Change("attacker-lmg");
        editor.Find("#edit-pick-count").Change("1");
        editor.Find("#edit-pick-area").Change("F5");
        editor.Find("#edit-pick-add").Click();
        Assert.Contains("Added 1", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-save").Click();
        var saved = live.Cards.Read("guards-counterattack-copy", live.Catalogs[0])!;
        Assert.True(saved.IsValid, string.Join("; ", saved.Diagnostics));
        Assert.Equal(2, saved.Card!.Sides.Single(side => side.Side == "german").Groups[0].Units.Count(unit => unit.Definition == "attacker-lmg" && unit.Area == "F5"));

        // Referee, pass 22: the copy keeps the printed Battlefield Integrity totals.
        Assert.Equal(ScenarioCards.Read("guards-counterattack", live.Catalogs[0])!.Card!.Sides.Select(side => side.IntegrityBpv), saved.Card.Sides.Select(side => side.IntegrityBpv));

        // Referee, pass 22: the picker adds the counter it shows, the first of the side it names.
        editor.Find("#edit-pick-side").Change("1");
        var shown = editor.Find("#edit-pick-definition option").GetAttribute("value");
        editor.Find("#edit-pick-add").Click();
        Assert.Contains($"({shown})", editor.Find("#edit-pick-definition option").OuterHtml, StringComparison.Ordinal);
        Assert.EndsWith($"\"{shown}\"", JsonDocument.Parse(editor.Find("#edit-groups-1").TextContent).RootElement[0].GetProperty("units").EnumerateArray().Last().GetProperty("definition").GetRawText(), StringComparison.Ordinal);

        // Referee, pass 22: a second copy under the same id does not overwrite the first.
        var again = context.Render<EditorPage>();
        again.Find("#edit-source").Change("guards-counterattack");
        Assert.Contains("already have a card", again.Find("#edit-diagnostics").TextContent, StringComparison.Ordinal);
        again.Find("#edit-save").Click();
        Assert.Contains("Not saved", again.Find("#edit-note").TextContent, StringComparison.Ordinal);

        // Broken JSON is reported, not saved.
        editor.Find("#edit-victory").Change("{ nope");
        Assert.Contains("card.victory", editor.Find("#edit-diagnostics").TextContent, StringComparison.Ordinal);

        // The Scenarios page lists the copy as yours and links it back to the editor.
        var scenarios = context.Render<ScenariosPage>();
        Assert.Contains(scenarios.FindAll("#card-choice option"), option => option.TextContent.Contains("(yours)", StringComparison.Ordinal));
        scenarios.Find("#card-choice").Change("guards-counterattack-copy");
        Assert.Contains("Edit this card", scenarios.Find("#card-edit").TextContent, StringComparison.Ordinal);
    }

    // R22.3, R22.4: a game starts from a minimal card on the Play page, and its units set up by hand, with no OB group.
    [Fact]
    public void AGameStartsFromAMinimalCard()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-id").Change("free-game");
        editor.Find("#edit-boards").Change(FakeBoardProvider.Board.Ref.Value);
        editor.Find("#edit-save").Click();

        var page = context.Render<PlayPage>();
        Assert.True(page.Find("#propose-setup").HasAttribute("disabled"));
        page.Find("#new-card").Change("free-game");
        Assert.Contains("(yours)", page.Find("#new-card option[value='free-game']").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#new-balance"));
        Assert.NotNull(page.Find("#setup-minimal"));
        Assert.Empty(page.FindAll("#setup-pools"));
        page.Find("#new-id").Change("free");
        page.Find("#place-definition").Change("attacker-squad");
        page.Find("#place-id").Change("g1");
        page.Find("#place-location").Change($"{FakeBoardProvider.Board.Ref.Value}:A1:0");
        page.Find("#place-add").Click();
        page.Find("#propose-setup").Click();
        page.Find("#play-confirm").Click();
        Assert.Contains("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.Contains("My game", page.Find("#play-card").TextContent, StringComparison.Ordinal);
        Assert.Contains("matches", page.Find("#play-card-provenance .provenance-match").TextContent, StringComparison.Ordinal);
        Assert.Contains("Your card", page.Find("#play-card-provenance .provenance-kept").TextContent, StringComparison.Ordinal);
    }

    // R22.4: a game recorded without a card (before pass 22) still loads and plays on the Play page.
    [Fact]
    public async Task AGameWithoutACardStillLoads()
    {
        var arguments = JsonSerializer.SerializeToElement(new
        {
            gameId = "old",
            attemptId = "setup-0",
            expectedRevision = 0,
            start = new Dictionary<string, object>
            {
                ["label"] = "Old game",
                ["catalog"] = "asl-scenario-a1@1.13.0",
                ["boards"] = new[] { FakeBoardProvider.Board.Ref.Value },
                ["firstSide"] = "german",
                ["sides"] = new object[]
                {
                    new { id = "german", nationality = "german", elr = 3 },
                    new { id = "russian", nationality = "russian", elr = 2 },
                },
            },
            placements = Array.Empty<object>(),
        });
        var proposed = await live.Play.ProposeAsync(GameActions.Setup, arguments, live.Principal);
        var confirmed = await live.Play.ConfirmAsync(GameActions.Setup, arguments, live.Principal, proposed.Correlation);
        Assert.True(confirmed.Outcome == PlayOutcome.Committed, string.Join("; ", confirmed.Reasons));

        var navigation = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("play?game=old");
        var page = context.Render<PlayPage>();
        Assert.Contains("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-card"));
        Assert.Empty(page.FindAll("#play-replay-failed"));
    }
}
