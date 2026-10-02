using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Cards;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Play;
using Microsoft.Extensions.DependencyInjection;
using EditorPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.CardEditor;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 28 of the Card Play and Map Studio Redesign Plan (rulings R28.1 to R28.5): the card editor's forms, its map, the checks against the boards, card
/// management, and the card components.
/// </summary>
public sealed class CardEditorPass28Tests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-card-28-" + Guid.NewGuid().ToString("N"));
    private readonly BunitContext context = new();
    private readonly LivePlay live;
    private readonly MapService maps;

    public CardEditorPass28Tests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        maps = new MapService(options, new FakeVaslMapSource());
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

    // R28.1: every field of every built-in card survives the forms: a card read into the draft builds back to the same text.
    [Theory]
    [InlineData("guards-counterattack")]
    [InlineData("gambit")]
    [InlineData("tractor-works")]
    [InlineData("armor-test")]
    public void EveryBuiltInCardSurvivesTheForms(string name)
    {
        var card = ScenarioCards.Read(name, live.Catalogs[0])!.Card!;
        var (built, problems) = CardDraft.From(card, user: true).Build(card.Catalog);
        Assert.Empty(problems);
        Assert.Equal(ScenarioCardLibrary.Serialize(card), ScenarioCardLibrary.Serialize(built!));
    }

    // R28.1 (and R27.1): an Axis Minor side's nation is kept by the forms, which the JSON editor's rebuild dropped.
    [Fact]
    public void AnAxisMinorSideKeepsItsNation()
    {
        var draft = CardDraft.Minimal();
        draft.ChangeSide(0, "axis-minor");
        draft.Sides[0].Nation = "hungarian";
        var (card, problems) = draft.Build(live.Catalogs[0].Identity.Catalog + "@" + live.Catalogs[0].Identity.Version);
        Assert.Empty(problems);
        Assert.Equal("hungarian", card!.Sides[0].Nation);
        Assert.Equal("axis-minor", card.Turns.SetsUpFirst);
        Assert.Empty(ScenarioCards.Validate(card, live.Catalogs[0]));

        // The nation goes when the side is no longer Axis Minor.
        draft.ChangeSide(0, "german");
        Assert.Null(draft.Build("x").Card!.Sides[0].Nation);
    }

    // R28.1 (referee, pass 28): an SSR's tokens are kept as typed, so a token left on a rule the game does not read is reported rather than dropped.
    [Fact]
    public void AnSsrsTokensAreKeptAsTyped()
    {
        var catalog = live.Catalogs[0];
        var draft = CardDraft.Minimal();
        draft.Rules.Add(new RuleDraft { Text = "Mud.", Status = "game-default", Tokens = "weather:mud" });
        var card = draft.Build($"{catalog.Identity.Catalog}@{catalog.Identity.Version}").Card!;
        Assert.Equal(["weather:mud"], card.SpecialRules[0].Tokens);
        Assert.Contains(ScenarioCards.Validate(card, catalog), item => item.Contains("has tokens exactly when the game reads it", StringComparison.Ordinal));
    }

    // Referee, pass 28: what a card says is kept as read: hexrows shown but not enforced, a note beside a named first side, "?" on an entry, and an
    // explicit false.
    [Fact]
    public void LesserFieldsAreKeptAsRead()
    {
        var card = ScenarioCards.Read("tractor-works", live.Catalogs[0])!.Card!;
        var outcomes = card.VictoryConditions.Outcomes!.Select(outcome => outcome with { Any = [.. outcome.Any.Select(item => item.Type == "control-count" ? item with { MeleeUncontrolled = false } : item)] }).ToList();
        var changed = card with
        {
            PlayableArea = card.PlayableArea! with
            {
                Enforced = false
            },
            Turns = card.Turns with
            {
                MovesFirst = "russian",
                MovesFirstNote = "The Russians move first, as the legacy card has it."
            },
            VictoryConditions = card.VictoryConditions with
            {
                Outcomes = outcomes
            },
        };
        var built = CardDraft.From(changed, user: true).Build(card.Catalog).Card!;
        Assert.Equal(ScenarioCardLibrary.Serialize(changed), ScenarioCardLibrary.Serialize(built));
    }

    // Referee, pass 28: an earlier text is accepted only against the text it was revised into.
    [Fact]
    public void AnEarlierTextMatchesOnlyItsRevision()
    {
        var (earlier, current) = ScenarioCards.EarlierRevisions["guards-counterattack"][0];
        Assert.True(ScenarioCards.SameCard("guards-counterattack", earlier, current));
        Assert.False(ScenarioCards.SameCard("guards-counterattack", earlier, new string('1', 64)));
    }

    // R28.2, R28.3: the card's boards drawn as one map, the hex under a point, and areas and exit hexes checked against the boards. The synthetic board
    // has no building hex, so the whole-building join is checked on board 01 in the Studio (the built-in cards' areas pass it).
    [Fact]
    public void TheMapChecksTheAreas()
    {
        var cardMaps = new CardMaps(maps);
        var (map, problems) = cardMaps.Load([new ScenarioCardBoard("bd02", 0, 0, false)]);
        Assert.True(map is not null, string.Join("; ", problems));
        var openHex = map!.Facts.Hexes.First(hex => hex.Center.Terrain is not { IsBuilding: true }).Hex.ToString();
        Assert.Empty(CardMaps.Building(map, new CardHex("bd02", openHex)));
        Assert.Equal(new CardHex("bd02", "A1"), CardMaps.HexAt(map, map.Render.Grid.Geometry.CenterDot(map.Composition!.Map.Locate(BoardRef.Parse("bd02"), HexName.Parse("A1"))!.Value).X,
            map.Render.Grid.Geometry.CenterDot(map.Composition.Map.Locate(BoardRef.Parse("bd02"), HexName.Parse("A1"))!.Value).Y));

        // The render endpoint can draw the map: its reference loads as a board.
        Assert.NotNull(maps.Load(map.Ref).Board);

        var catalog = live.Catalogs[0];
        var card = Minimal(catalog, "bd02") with
        {
            Sides =
            [
                Side("german", Group("G", (openHex, "building", [openHex, "Z9"]), "attacker-squad")),
                Side("russian", Group("R", ("entry", "entry", ["B2"]), "defender-squad")),
            ],
            VictoryConditions = new ScenarioCardVictory("exit", "Exit.", ["A26.23"],
                [new ScenarioCardOutcome("german", false, [new ScenarioCardCondition("exit-vp", "german", AtLeast: 1, Edge: "top", Near: ["bd02:A1", "bd02:Z9"])])], "russian"),
        };
        var found = CardMaps.Check(card, map);
        Assert.Contains(found, item => item.Contains("names Z9, not hexes of bd02", StringComparison.Ordinal));
        var single = CardMaps.Check(card with
        {
            Sides = [Side("german", Group("G", (openHex, "building", [openHex]), "attacker-squad")), card.Sides[1]]
        }, map);
        Assert.Contains(single, item => item.Contains($"bd02 {openHex} is not a building hex", StringComparison.Ordinal));
        Assert.Contains(found, item => item.Contains("names B2, which are not on the top edge", StringComparison.Ordinal));
        Assert.Contains(found, item => item.Contains("bd02:Z9", StringComparison.Ordinal));
        Assert.DoesNotContain(found, item => item.Contains("bd02:A1,", StringComparison.Ordinal));
    }

    // R28.4: a built-in card's earlier text, revised only in a note, still matches a game that recorded it; another text does not.
    [Fact]
    public void ABuiltInCardsEarlierRevisionStillMatches()
    {
        var current = live.Cards.Sha256("guards-counterattack")!;
        Assert.NotEqual("132c2241db7d192460b1c7146f58b9b63027c8d5af17e698111772f871dddcab", current);
        Assert.True(live.Cards.Matches("guards-counterattack", current));
        Assert.True(live.Cards.Matches("guards-counterattack", "132c2241db7d192460b1c7146f58b9b63027c8d5af17e698111772f871dddcab"));
        Assert.False(live.Cards.Matches("guards-counterattack", new string('0', 64)));
        Assert.False(live.Cards.Matches("gambit", "132c2241db7d192460b1c7146f58b9b63027c8d5af17e698111772f871dddcab"));
        Assert.False(live.Cards.Matches("no-such-card", current));

        // The revision: SSR 3 is played by the game's setup now (ruling R19.1).
        var rule = ScenarioCards.Read("guards-counterattack", live.Catalogs[0])!.Card!.SpecialRules[2];
        Assert.Equal("game-default", rule.Status);
        Assert.DoesNotContain("pass 19", rule.Note, StringComparison.Ordinal);
    }

    // R28.4: the game-to-card index names the games that started from a card, read from their records.
    [Fact]
    public async Task TheIndexNamesTheGamesFromACard()
    {
        var catalog = live.Catalogs[0];
        Assert.Empty(live.Cards.Save(Minimal(catalog, FakeBoardProvider.Board.Ref.Value) with
        {
            Id = "indexed"
        }, catalog));
        var arguments = System.Text.Json.JsonSerializer.SerializeToElement(new
        {
            gameId = "from-indexed",
            attemptId = "setup-0",
            expectedRevision = 0,
            start = ScenarioCards.Start(live.Cards.Read("indexed", catalog)!.Card!, live.Cards.Sha256("indexed")!, $"{catalog.Identity.Catalog}@{catalog.Identity.Version}", "Indexed", null),
            placements = Array.Empty<object>(),
        });
        var proposed = await live.Play.ProposeAsync(GameActions.Setup, arguments, live.Principal);
        var confirmed = await live.Play.ConfirmAsync(GameActions.Setup, arguments, live.Principal, proposed.Correlation);
        Assert.True(confirmed.Outcome == PlayOutcome.Committed, string.Join("; ", confirmed.Reasons));
        Assert.Equal(["from-indexed"], live.GamesFrom("indexed"));
        Assert.Empty(live.GamesFrom("other"));

        // The editor names the game before a delete, and nothing is deleted until it is confirmed; Cancel closes the question.
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-source").Change("indexed");
        editor.Find("#edit-delete").Click();
        Assert.Contains("from-indexed", editor.Find("#edit-confirm-games").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-confirm-cancel").Click();
        Assert.Empty(editor.FindAll("#edit-confirm"));
        Assert.Contains("indexed", live.Cards.UserNames);
    }

    // R28.1: groups, areas, and counter lines are built from the forms, and the counter picker offers the group's areas.
    [Fact]
    public void AnObIsBuiltFromTheForms()
    {
        var editor = context.Render<EditorPage>();
        editor.Find("#edit-id").Change("formed");
        CardEditorDriver.Boards(editor, FakeBoardProvider.Board.Ref.Value);
        CardEditorDriver.AddGroup(editor, 0, "Germans", "4", "B1", "attacker-squad");
        CardEditorDriver.AddGroup(editor, 1, "Russians", "3", "C1", "defender-squad");
        Assert.Equal(["", "B1"], editor.FindAll("#edit-group-0-0-pick-area option").Select(option => option.GetAttribute("value")));
        editor.Find("#edit-balance-0").Change("Add a squad.");
        editor.Find("#edit-balance-1").Change("Add a leader.");
        editor.Find("#edit-victory-rules").Change("A26.1");
        editor.Find("#edit-day").Change("3");
        editor.Find("#edit-month").Change("10");
        editor.Find("#edit-year").Change("1942");
        editor.Find("#edit-edge-0").Change("bottom");
        editor.Find("#edit-edge-1").Change("top");
        Assert.NotNull(editor.Find("#edit-valid"));
        editor.Find("#edit-save").Click();
        Assert.Contains("Saved 'formed'", editor.Find("#edit-note").TextContent, StringComparison.Ordinal);
        var card = live.Cards.Read("formed", live.Catalogs[0])!.Card!;
        Assert.Equal("B1", card.Sides[0].Groups[0].Areas[0].Id);
        Assert.Equal(4, card.Sides[0].Groups[0].Elr);
        Assert.Null(card.Sides[0].Elr);
        Assert.Equal(new ScenarioCardUnit("defender-squad", 1, "C1"), card.Sides[1].Groups[0].Units[0]);
    }

    // K12 (task 28.5): one SSR list, plain on Play and detailed on the Scenarios page.
    [Fact]
    public void TheSsrListIsPlainOrDetailed()
    {
        ScenarioCardRule[] rules = [new(1, "Mud.", "token", ["weather:mud"], ["A2.1"], null), new(2, "Sewers.", "not-enforced", [], [], "Not built.")];
        var plain = context.Render<CardSpecialRulesList>(parameters => parameters.Add(item => item.Id, "plain").Add(item => item.Rules, rules));
        Assert.Equal(["Mud.", "Sewers."], plain.FindAll("li").Select(item => item.TextContent.Trim()));
        var detailed = context.Render<CardSpecialRulesList>(parameters => parameters.Add(item => item.Id, "detailed").Add(item => item.Rules, rules).Add(item => item.Detailed, true));
        Assert.Contains("Read by the game: weather:mud.", detailed.Find("li.card-ssr-token").TextContent, StringComparison.Ordinal);
        Assert.Contains("Shown, not enforced. Not built.", detailed.Find("li.card-ssr-not-enforced").TextContent, StringComparison.Ordinal);
    }

    // Task 28.5: the Source label names its input.
    [Fact]
    public void TheSourceLabelNamesItsInput()
    {
        var editor = context.Render<EditorPage>();
        Assert.NotNull(editor.Find("label[for='edit-source-basis']"));
        Assert.NotNull(editor.Find("#edit-source-basis"));
    }

    private static ScenarioCard Minimal(LimboDancer.Domains.Asl.Units.Catalog.UnitCatalog catalog, string board) =>
        new(ScenarioCards.Format, "test-card", "Test card", $"{catalog.Identity.Catalog}@{catalog.Identity.Version}", new ScenarioCardSource("A test.", "none", []), string.Empty,
            new ScenarioCardDate(0, 0, 0), string.Empty, [new ScenarioCardBoard(board, 0, 0, false)], "top", null, new ScenarioCardTurns(5, false, "german", "german"), null,
            [Side("german"), Side("russian")], [], new ScenarioCardVictory("other", "The players judge the result.", []), null);

    private static ScenarioCardSide Side(string side, params ScenarioCardGroup[] groups) =>
        new(side, 2, null, new ScenarioCardEdge(side == "german" ? "bottom" : "top", "manufactured", null), "None.", groups, Elr: groups.Length == 0 ? 3 : null);

    private static ScenarioCardGroup Group(string name, (string Id, string Kind, string[] Hexes) area, string definition) =>
        new(name, 3, [area.Kind == "entry" ? new ScenarioCardSetup(area.Id, "entry", area.Hexes, null, null, null, 1, "top", null)
            : new ScenarioCardSetup(area.Id, area.Kind, area.Hexes, null, null, null, null, null, null)], [new ScenarioCardUnit(definition, 1, area.Id)]);

    // Table player and UI review, pass 28: picking ends when its area is removed or stops taking hexes from the map; a removed SSR's text goes with it.
    [Fact]
    public void PickingAndRowsFollowTheirItems()
    {
        var editor = context.Render<EditorPage>();
        CardEditorDriver.Boards(editor, "bd02");
        editor.Find("#edit-group-add-0").Click();
        editor.Find("#edit-group-0-0-area-add").Click();
        editor.Find("#edit-group-0-0-area-0-pick").Click();
        Assert.Contains("area 'area-1'", editor.Find("#edit-map-target").TextContent, StringComparison.Ordinal);
        editor.Find("#edit-group-0-0-area-0-kind").Change("hex-numbers");
        Assert.Contains("Pick on the map", editor.Find("#edit-map-target").TextContent, StringComparison.Ordinal);
        Assert.NotNull(editor.Find("#edit-group-0-0-area-0-board"));

        editor.Find("#edit-group-0-0-area-0-kind").Change("building");
        editor.Find("#edit-group-0-0-area-0-pick").Click();
        editor.Find("#edit-group-0-0-area-0-remove").Click();
        Assert.Contains("Pick on the map", editor.Find("#edit-map-target").TextContent, StringComparison.Ordinal);

        CardEditorDriver.AddRule(editor, "First.", "game-default");
        CardEditorDriver.AddRule(editor, "Second.", "game-default");
        editor.Find("#edit-ssr-0-remove").Click();
        Assert.Equal("Second.", editor.Find("#edit-ssr-0-text").GetAttribute("value"));
    }

    // Table player, pass 28: a side's list leaves out the other side's nationality, so two sides cannot be merged by a swap.
    [Fact]
    public void ASideCannotTakeTheOtherSidesNationality()
    {
        var editor = context.Render<EditorPage>();
        Assert.DoesNotContain(editor.FindAll("#edit-side-0 option"), option => option.GetAttribute("value") == "russian");
        Assert.DoesNotContain(editor.FindAll("#edit-side-1 option"), option => option.GetAttribute("value") == "german");
    }
}
