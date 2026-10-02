using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;
using EditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;
using ScenariosPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Scenarios;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>A table player tries the pass 22 card editor and the card-only Play page, as a player making their own scenario would.</summary>
public sealed class TablePlayer22Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-table-22-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly LivePlay live;
    private readonly ITestOutputHelper output;

    public TablePlayer22Tests(ITestOutputHelper output)
    {
        this.output = output;
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

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Diagnostics(IRenderedComponent<EditorPage> editor) =>
        editor.FindAll("#edit-diagnostics").Count > 0 ? editor.Find("#edit-diagnostics").TextContent.Trim() : "(valid)";

    private IRenderedComponent<EditorPage> MinimalOnBoard(string id)
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-id").Change(id);
        editor.Find("#edit-title").Change("Quick fight " + id);
        CardEditorDriver.Boards(editor, Board);
        return editor;
    }

    private void Save(IRenderedComponent<EditorPage> editor)
    {
        editor.Find("#edit-save").Click();
        output.WriteLine($"save: note={(editor.FindAll("#edit-note").Count > 0 ? editor.Find("#edit-note").TextContent : "(none)")}; diagnostics={Diagnostics(editor)}");
    }

    private static void Place(IRenderedComponent<PlayPage> page, string definition, string id, string at, string? group = null)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        if (group is not null)
        {
            page.Find("#place-group").Change(group);
        }

        page.Find("#place-add").Click();
    }

    /// <summary>Proposes and, when every check passes, confirms; returns the outcome and its reasons.</summary>
    private string Propose(IRenderedComponent<PlayPage> page, string button)
    {
        page.Find(button).Click();
        var outcome = page.FindAll("#play-outcome").Count > 0 ? page.Find("#play-outcome").TextContent : "(no outcome)";
        var reasons = page.FindAll("#play-reasons").Count > 0 ? page.Find("#play-reasons").TextContent.Trim() : string.Empty;
        if (outcome.Contains("Confirm to commit", StringComparison.Ordinal))
        {
            page.Find("#play-confirm").Click();
            outcome = page.Find("#play-outcome").TextContent;
        }

        output.WriteLine($"{button}: {outcome} {reasons}");
        return outcome + " " + reasons;
    }

    private IRenderedComponent<PlayPage> StartMinimal(string card, string game)
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(card);
        page.Find("#new-id").Change(game);
        Place(page, "attacker-squad", "g1", $"{Board}:A1:0");
        Place(page, "defender-squad", "r1", $"{Board}:C1:0");
        Assert.Contains("Committed", Propose(page, "#propose-setup"), StringComparison.Ordinal);
        return page;
    }

    // 1. A minimal card for a quick game on one board: made, started, set up, and played a few phases.
    [Fact]
    public void AQuickGameOnOneBoard()
    {
        var editor = MinimalOnBoard("quick");
        editor.Find("#edit-turns").Change("2");
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var page = StartMinimal("quick", "quick-1");
        output.WriteLine("summary: " + page.Find("#play-summary").TextContent);
        for (var step = 0; step < 4; step++)
        {
            Assert.Contains("Committed", Propose(page, "#propose-advance"), StringComparison.Ordinal);
            output.WriteLine("summary: " + page.Find("#play-summary").TextContent.Trim());
        }

        Assert.Contains("Play has started", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.Contains("2 Game Turns", page.Find("#play-card-turns").TextContent, StringComparison.Ordinal);
    }

    // 2. A copy of The Guards Counterattack: fewer turns, a group's ELR raised, a counter added with the picker; the Play page shows the change.
    [Fact]
    public void ACopyOfTheGuardsWithChanges()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("guards-counterattack");
        editor.Find("#edit-turns").Change("4");
        Assert.Equal("4", editor.Find("#edit-group-0-0-elr").GetAttribute("value"));
        editor.Find("#edit-group-0-0-elr").Change("5");
        editor.Find("#edit-group-0-0-pick-definition").Change("attacker-squad");
        editor.Find("#edit-group-0-0-pick-count").Change("2");
        editor.Find("#edit-group-0-0-pick-area").Change("K5");
        editor.Find("#edit-group-0-0-pick-add").Click();

        // Referee, pass 22: the copy keeps the printed Battlefield Integrity total, which the added squads no longer match (A16.1).
        Assert.Contains("card.integrity: german prints [130]", Diagnostics(editor), StringComparison.Ordinal);
        editor.Find("#edit-integrity-0").Change(string.Empty);
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var saved = live.Cards.Read("guards-counterattack-copy", live.Catalogs[0])!.Card!;
        Assert.Equal(4, saved.Turns.Count);
        Assert.Equal(5, saved.Sides.Single(side => side.Side == "german").Groups[0].Elr);

        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("guards-counterattack-copy");
        output.WriteLine("start: " + page.Find("#new-card-summary").TextContent.Trim());
        Assert.Contains("german: ELR 5", page.Find("#new-summary-sides").TextContent, StringComparison.Ordinal);
        var pool = page.Find("#setup-pools tr[data-group='german-1']").TextContent;
        output.WriteLine("german-1 pool: " + pool);
        Assert.Contains("in K5", pool, StringComparison.Ordinal);
    }

    // 3. A copy of the Guards moved to a board the player has, with small OB groups: the OB checks apply to the setup.
    [Fact]
    public void ACopyMovedToAnotherBoardKeepsItsObChecks()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("guards-counterattack");
        editor.Find("#edit-id").Change("small-guards");
        CardEditorDriver.Boards(editor, Board);
        output.WriteLine("after the board change: " + Diagnostics(editor));
        editor.Find("#edit-playable").Change(string.Empty);
        editor.Find("#edit-playable-from").Change(string.Empty);
        editor.Find("#edit-playable-to").Change(string.Empty);
        editor.Find("#edit-group-0-0-remove").Click();
        editor.Find("#edit-group-1-1-remove").Click();
        editor.Find("#edit-group-1-0-remove").Click();
        CardEditorDriver.AddGroup(editor, 0, "Germans", "4", "B1", "attacker-squad");
        CardEditorDriver.AddGroup(editor, 1, "Russians", "3", "C1", "defender-squad");
        output.WriteLine("after the new groups: " + Diagnostics(editor));
        editor.Find("#edit-victory-evaluated").Change(false);
        editor.Find("#edit-victory-kind").Change("other");
        editor.Find("#edit-victory-text").Change("The players judge the result.");
        editor.Find("#edit-victory-rules").Change("A26.1");
        output.WriteLine("after the new victory: " + Diagnostics(editor));
        Assert.Contains("card.integrity", Diagnostics(editor), StringComparison.Ordinal);
        editor.Find("#edit-integrity-0").Change(string.Empty);
        editor.Find("#edit-integrity-1").Change(string.Empty);
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("small-guards");
        page.Find("#new-id").Change("small");
        Place(page, "attacker-squad", "g1", $"{Board}:A1:0", "german-1");
        var refused = Propose(page, "#propose-setup");
        Assert.DoesNotContain("Committed", refused, StringComparison.Ordinal);

        page.Find("#place-list button").Click();
        Place(page, "attacker-squad", "g1", $"{Board}:B1:0", "german-1");
        Assert.Contains("Committed", Propose(page, "#propose-setup"), StringComparison.Ordinal);
    }

    // 4. A card on two boards.
    [Fact]
    public void ACardOnTwoBoards()
    {
        var editor = MinimalOnBoard("two-boards");
        CardEditorDriver.Boards(editor, "bd01", "bd02");
        output.WriteLine("bd01 bd02: " + Diagnostics(editor));
        Assert.Contains("two boards share a slot", Diagnostics(editor), StringComparison.Ordinal);
        CardEditorDriver.Boards(editor, "bd01@0,0", "bd02@1,0");
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("two-boards");
        output.WriteLine("start: " + page.Find("#new-summary-boards").TextContent);
        Assert.Contains("bd01@0,0 bd02@1,0", page.Find("#new-summary-boards").TextContent, StringComparison.Ordinal);

        // The synthetic board beside itself cannot be named twice; the player sees why.
        CardEditorDriver.Boards(editor, $"{Board}@0,0", $"{Board}@1,0");
        output.WriteLine("the same board twice: " + Diagnostics(editor));
        Assert.Contains("a board is named twice", Diagnostics(editor), StringComparison.Ordinal);
    }

    // 5. SSR tokens on a card reach the game.
    [Fact]
    public void NightAndMudReachTheGame()
    {
        var editor = MinimalOnBoard("night-mud");
        CardEditorDriver.AddRule(editor, "Night, Base NVR 1.", "token", "night:1");
        CardEditorDriver.AddRule(editor, "Mud.", "token", "weather:mud");
        output.WriteLine("ssr: " + Diagnostics(editor));
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("night-mud");
        Assert.Contains("night:1 weather:mud", page.Find("#new-summary-date").TextContent, StringComparison.Ordinal);
        page.Find("#new-id").Change("dark");
        Place(page, "attacker-squad", "g1", $"{Board}:A1:0");
        Assert.Contains("Committed", Propose(page, "#propose-setup"), StringComparison.Ordinal);
        var conditions = page.Find("#play-conditions").TextContent;
        output.WriteLine("conditions: " + conditions);
        Assert.Contains("NVR 1", conditions, StringComparison.Ordinal);
        Assert.Contains("mud", conditions, StringComparison.Ordinal);
    }

    // 6. Extreme Winter on a minimal card with no date: the editor says why not, as the game would (E3.741, E3.742; ruling R16.14).
    [Fact]
    public void ExtremeWinterWithoutADate()
    {
        var editor = MinimalOnBoard("cold");
        CardEditorDriver.AddRule(editor, "Extreme Winter, Ground Snow.", "token", "weather:extreme-winter weather:ground-snow");
        Assert.Contains("Extreme Winter needs the scenario's month and year", Diagnostics(editor), StringComparison.Ordinal);
        editor.Find("#edit-month").Change("1");
        editor.Find("#edit-year").Change("1942");
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("cold");
        page.Find("#new-id").Change("cold");
        Place(page, "attacker-squad", "g1", $"{Board}:A1:0");
        Assert.Contains("Committed", Propose(page, "#propose-setup"), StringComparison.Ordinal);
    }

    // 7. Invalid values and the messages they give.
    [Fact]
    public void InvalidValuesGiveHelpfulMessages()
    {
        var editor = MinimalOnBoard("bad-values");
        string Try(string field, string value)
        {
            editor.Find(field).Change(value);
            var text = Diagnostics(editor);
            output.WriteLine($"{field} = '{value}': {text}");
            return text;
        }

        Assert.Contains("card.san: german's SAN is 'x', which is not a whole number", Try("#edit-san-0", "x"), StringComparison.Ordinal);
        Try("#edit-san-0", "0");
        Assert.Contains("card.boards", Try("#edit-board-0", "board one"), StringComparison.Ordinal);
        Assert.Contains("card.boards: board 1's column is '0;0', which is not a whole number", Try("#edit-board-0-column", "0;0"), StringComparison.Ordinal);
        Try("#edit-board-0-column", "0");
        Try("#edit-board-0", Board);
        Assert.Contains("card.date", Try("#edit-month", "13"), StringComparison.Ordinal);
        Try("#edit-month", "0");
        Try("#edit-day", "6");
        Try("#edit-day", "0");
        Try("#edit-turns", "ten");
        Try("#edit-turns", "10");
        Assert.Contains("built-in", Try("#edit-id", "tractor-works"), StringComparison.Ordinal);

        // An id with capitals: the page should flag it before the player presses Save.
        var capitals = Try("#edit-id", "My-Game");
        editor.Find("#edit-save").Click();
        output.WriteLine("save of My-Game: " + Diagnostics(editor));
        Assert.NotEqual("(valid)", capitals);
    }

    // 8. A side ELR typed on a card with an OB.
    [Fact]
    public void ASideElrOnACardWithAnOb()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("guards-counterattack");
        output.WriteLine("side ELR field on the copy: '" + editor.Find("#edit-elr-0").GetAttribute("value") + "'");
        editor.Find("#edit-elr-0").Change("2");

        // A player who typed ELR 2 is told that the OB groups give the ELR; nothing is dropped silently.
        Assert.Contains("card.elr", Diagnostics(editor), StringComparison.Ordinal);
        editor.Find("#edit-save").Click();
        Assert.Contains("Not saved", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("guards-counterattack-copy", live.Cards.UserNames);
    }

    // 9. A card deleted after a game started from it: the game is refused, and says the card is gone (ruling R19.1; backlog 32).
    [Fact]
    public void DeletingACardAfterAGameStarted()
    {
        Save(MinimalOnBoard("doomed"));
        var page = StartMinimal("doomed", "doomed-1");

        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("doomed");
        editor.Find("#edit-delete").Click();

        // Ruling R28.4: the delete asks first and names the game that started from the card.
        Assert.Contains("doomed", live.Cards.UserNames);
        Assert.Contains("doomed-1", editor.Find("#edit-confirm-games").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-delete-confirm").Click();
        output.WriteLine("delete: " + editor.Find("#edit-note").TextContent);
        Assert.DoesNotContain("doomed", live.Cards.UserNames);

        var again = context.Render<PlayPage>();
        again.Find("#play-game").Change("doomed-1");
        Assert.NotNull(again.Find("#play-card-missing"));
        output.WriteLine("missing: " + again.Find("#play-card-missing").TextContent);
        var advance = Propose(again, "#propose-advance");
        Assert.Contains("is no longer among the scenario cards", advance, StringComparison.Ordinal);
    }

    // 10. A saved user card edited after a game started from it.
    [Fact]
    public void EditingACardAfterAGameStarted()
    {
        Save(MinimalOnBoard("changing"));
        StartMinimal("changing", "changing-1");

        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("changing");
        Assert.Equal("changing", editor.Find("#edit-id").GetAttribute("value"));
        editor.Find("#edit-turns").Change("3");
        Save(editor);

        var again = context.Render<PlayPage>();
        again.Find("#play-game").Change("changing-1");
        output.WriteLine("changed: " + (again.FindAll("#play-card-changed").Count > 0 ? again.Find("#play-card-changed").TextContent : "(no warning)"));
        output.WriteLine("turns shown: " + again.Find("#play-card-turns").TextContent.Trim());
        Assert.NotEmpty(again.FindAll("#play-card-changed"));
        // Rulings R18.1, R19.1: the game is refused while its card differs; the editor gave no warning when it saved over a card in play.
        Assert.Contains("Refused", Propose(again, "#propose-advance"), StringComparison.Ordinal);

        // Putting the card back as it was lets the game go on.
        editor.Find("#edit-turns").Change("10");
        Save(editor);
        var restored = context.Render<PlayPage>();
        restored.Find("#play-game").Change("changing-1");
        output.WriteLine("after restoring, warning shown: " + restored.FindAll("#play-card-changed").Count);
        var resumed = Propose(restored, "#propose-advance");
        Assert.Contains("Committed", resumed, StringComparison.Ordinal);
    }

    // 11. What the retired form set that a minimal card now carries: a Scenario Defender, a month without a year, edges.
    [Fact]
    public void TheStartSummaryShowsWhatTheFormUsedToSet()
    {
        var editor = MinimalOnBoard("old-form");
        editor.Find("#edit-defender").Change("russian");
        editor.Find("#edit-month").Change("7");
        editor.Find("#edit-edge-0").Change("bottom");
        editor.Find("#edit-edge-1").Change("top");
        editor.Find("#edit-san-0").Change("3");
        editor.Find("#edit-elr-1").Change("2");
        editor.Find("#edit-moves").Change("russian");
        output.WriteLine("editor: " + Diagnostics(editor));
        Assert.Equal("(valid)", Diagnostics(editor));
        Save(editor);

        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change("old-form");
        var summary = page.Find("#new-card-summary").TextContent.Trim();
        output.WriteLine("start: " + summary);
        Assert.Contains("Scenario Defender: russian", summary, StringComparison.Ordinal);
        Assert.Contains("July; SSRs the game enforces", summary, StringComparison.Ordinal);
        Assert.Contains("FBE bottom", summary, StringComparison.Ordinal);
        Assert.Contains("ELR 2", summary, StringComparison.Ordinal);

        page.Find("#new-id").Change("old");
        Place(page, "attacker-squad", "g1", $"{Board}:A1:0");
        Assert.Contains("Committed", Propose(page, "#propose-setup"), StringComparison.Ordinal);
        output.WriteLine("game: " + page.Find("#play-summary").TextContent.Trim());
    }

    // 12. Changing side 1's nationality: the turn selects should follow.
    [Fact]
    public void ChangingASideKeepsTheTurnRecordConsistent()
    {
        var editor = MinimalOnBoard("americans");
        editor.Find("#edit-side-0").Change("american");
        var text = Diagnostics(editor);
        output.WriteLine("after side 1 = american: " + text);
        output.WriteLine("sets up first options: " + string.Join(", ", editor.FindAll("#edit-sets-up option").Select(option => option.TextContent)));
        Assert.Equal("(valid)", text);
        Assert.Equal("american", editor.Find("#edit-sets-up").GetAttribute("value"));
    }

    // 13. A group added to a minimal card takes the side's ELR, which a card with an OB no longer carries (ruling R22.3).
    [Fact]
    public void AGroupOnAMinimalCardTakesTheSidesElr()
    {
        var editor = MinimalOnBoard("picker");
        editor.Find("#edit-elr-0").Change("4");
        editor.Find("#edit-group-add-0").Click();
        Assert.Equal("4", editor.Find("#edit-group-0-0-elr").GetAttribute("value"));
        Assert.Equal(string.Empty, editor.Find("#edit-elr-0").GetAttribute("value"));
        Assert.DoesNotContain("card.elr: german", Diagnostics(editor), StringComparison.Ordinal);
    }

    // 14. The Scenarios page shows a minimal card.
    [Fact]
    public void TheScenariosPageShowsAMinimalCard()
    {
        var editor = MinimalOnBoard("shown");
        editor.Find("#edit-edge-0").Change("left");
        Save(editor);
        var scenarios = context.Render<ScenariosPage>();
        scenarios.Find("#card-choice").Change("shown");
        var card = scenarios.Find("#card").TextContent;
        output.WriteLine("card: " + string.Join(" ", card.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
        Assert.Contains("Edit this card", scenarios.Find("#card-edit").TextContent, StringComparison.Ordinal);
        Assert.Empty(scenarios.FindAll("#card-diagnostics"));
        Assert.DoesNotContain(" , ", card, StringComparison.Ordinal);
        Assert.Contains("not evaluated by the game", scenarios.Find("#card-victory").TextContent, StringComparison.Ordinal);
        Assert.Equal(["3", "3"], scenarios.FindAll(".card-elr").Select(item => item.TextContent[..1]));
    }

    // 15. A user card under a new id: Save keeps both; Rename asks first and keeps only the new one (ruling R28.4).
    [Fact]
    public void RenamingAUserCard()
    {
        Save(MinimalOnBoard("first-name"));
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("first-name");
        editor.Find("#edit-id").Change("second-name");
        Save(editor);
        Assert.Equal(["first-name", "second-name"], live.Cards.UserNames);

        editor.Find("#edit-id").Change("third-name");
        editor.Find("#edit-rename").Click();
        Assert.Contains("No game started from it", editor.Find("#edit-confirm-games").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-rename-confirm").Click();
        Assert.Contains("Renamed 'second-name' to 'third-name'", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        Assert.Equal(["first-name", "third-name"], live.Cards.UserNames);
    }

    // 16. The editor opened from a Scenarios link (?card=).
    [Fact]
    public void TheEditorOpensFromALink()
    {
        Save(MinimalOnBoard("linked"));
        var navigation = context.Services.GetRequiredService<Microsoft.AspNetCore.Components.NavigationManager>();
        navigation.NavigateTo("units/cards/edit?card=linked");
        var editor = context.Render<EditorPage>();
        output.WriteLine("id: " + editor.Find("#edit-id").GetAttribute("value") + ", delete: " + editor.FindAll("#edit-delete").Count);
        Assert.Equal("linked", editor.Find("#edit-id").GetAttribute("value"));
    }
}
