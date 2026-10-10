using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 30: the setup mode. A game starts from its card with no counter placed; the side that sets up first then sets up in its own view, by hand or
/// from one of the card's setup plans. "Show on map" draws a plan and leaves the list alone, "Use this plan" fills the list, and a changed list is
/// replaced only after the page has asked. The plans are offered to that side alone, while it sets up (ruling R23.3).
/// </summary>
public sealed class SetupPlansPageTests : IDisposable
{
    private const string CardId = "planned";

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-setup-plans-" + Guid.NewGuid().ToString("N"));
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public SetupPlansPageTests()
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
        context.UseViewport();
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

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    private static ScenarioCardGroup Group(string name, int order, string area, string[] hexes, string definition, int count) =>
        new(name, 3, [new ScenarioCardSetup(area, "building", hexes, null, null, null, null, null, null)], [new ScenarioCardUnit(definition, count, area)], order);

    private static ScenarioCardSide Side(string side, ScenarioCardGroup group) =>
        new(side, 2, null, new ScenarioCardEdge(side == "german" ? "bottom" : "top", "manufactured", null), "None.", [group]);

    /// <summary>A user's card on the test board: two German squads set up first in A1 and B1, then a Russian squad in C1.</summary>
    private string SaveCard()
    {
        var catalog = live.Catalogs[0];
        var card = new ScenarioCard(ScenarioCards.Format, CardId, "Planned", $"{catalog.Identity.Catalog}@{catalog.Identity.Version}", new ScenarioCardSource("A test.", "none", []),
            string.Empty, new ScenarioCardDate(1, 7, 1942), string.Empty, [new ScenarioCardBoard(Board, 0, 0, false)], "top", null, new ScenarioCardTurns(5, false, "german", "german"), null,
            [Side("german", Group("The line", 1, "A1", ["A1", "B1"], "attacker-squad", 2)), Side("russian", Group("The guard", 2, "C1", ["C1"], "defender-squad", 1))], [],
            new ScenarioCardVictory("other", "The players judge the result.", ["A26.1"]), null);
        Assert.Empty(live.Cards.Save(card, catalog));
        return live.Cards.Sha256(CardId)!;
    }

    private static string Plan(string id, string name, string sha256, string first, string second) =>
        $$"""
        {"id":"{{id}}","cardSha256":"{{sha256}}","side":"german","name":"{{name}}","idea":"The idea of {{name}}.","givesUp":"What {{name}} gives up.","terrain":["A fact of {{name}}."],
         "placements":[{"id":"g-1","definition":"attacker-squad","group":"german-1","at":"{{Board}}:{{first}}:0"},{"id":"g-2","definition":"attacker-squad","group":"german-1","at":"{{Board}}:{{second}}:0"}]}
        """;

    private void SavePlans(string text) => File.WriteAllText(Path.Combine(live.Cards.Directory!, CardId + ScenarioSetupPlans.Suffix), text);

    private void SaveTwoPlans(string sha256) =>
        SavePlans($$"""{"format":"asl-setup-plans/1","card":"{{CardId}}","plans":[{{Plan("apart", "Apart", sha256, "A1", "B1")}},{{Plan("together", "Together", sha256, "B1", "B1")}}]}""");

    /// <summary>Starts a game from the card with no counter placed, and takes the hand-over the page then offers.</summary>
    private IRenderedComponent<PlayPage> Start(string game = "g1")
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(CardId);
        page.Find("#new-id").Change(game);
        page.Find("#start-game").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll("#play-handover-confirm")));
        return page;
    }

    private static string Text(IRenderedComponent<PlayPage> page, string selector) => page.Find(selector).TextContent.Trim();

    [Fact]
    public void TheCardSaysItsPlansBeforeTheGameStartsAndTheFirstSideTakesTheScreen()
    {
        SaveTwoPlans(SaveCard());
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(CardId);

        Assert.Contains("2 setup plans for the German side", Text(page, "#start-plans-note"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#setup-plans"));

        page = Start();

        // The hand-over is to the side that sets up first, and the address names the game, so a reload opens it again.
        Assert.Contains("German", Text(page, "#play-handover-confirm"), StringComparison.Ordinal);
        Assert.EndsWith("?game=g1", context.Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
        page.Find("#play-handover-confirm").Click();
        Assert.Equal("German setup", Text(page, "#setup-bar-title"));
        Assert.Contains("0 of 2 counters in the list", Text(page, "#setup-bar-progress"), StringComparison.Ordinal);
        Assert.True(page.Find("#propose-setup").HasAttribute("disabled"));
        Assert.Equal(["Plans (2)", "Counters (0)", "Card OB"], page.FindAll("[id^='setup-tab-']").Select(tab => tab.TextContent.Trim()));
        Assert.Equal(2, page.FindAll(".setup-plan").Count);
    }

    [Fact]
    public void ShowOnMapLeavesTheListAloneAndUseThisPlanFillsIt()
    {
        SaveTwoPlans(SaveCard());
        var page = Start();
        page.Find("#play-handover-confirm").Click();

        page.Find("#show-plan-apart").Click();
        Assert.Equal("true", page.Find("#show-plan-apart").GetAttribute("aria-pressed"));
        Assert.Equal("true", page.Find("#map-show-apart").GetAttribute("aria-pressed"));
        Assert.Contains("0 of 2", Text(page, "#setup-bar-progress"), StringComparison.Ordinal);
        Assert.Contains("Counters (0)", Text(page, "#setup-tab-counters"), StringComparison.Ordinal);

        // The list is empty, so the plan fills it with no question.
        page.Find("#use-plan-apart").Click();
        Assert.Empty(page.FindAll("#setup-bar-ask"));
        Assert.Contains("2 of 2 counters in the list", Text(page, "#setup-bar-progress"), StringComparison.Ordinal);
        Assert.Equal("From Apart", Text(page, "#setup-bar-source"));
        Assert.Equal("true", page.Find("#setup-tab-counters").GetAttribute("aria-selected"));
        Assert.Equal(2, page.FindAll(".setup-stack").Count);
        Assert.NotNull(page.Find("#setup-row-g-1"));

        // A change is counted against the plan, and another plan then replaces the list only after the page has asked.
        page.Find("#setup-row-g-1 .setup-row-concealed").Change(true);
        Assert.Equal("From Apart, 1 change", Text(page, "#setup-bar-source"));
        Assert.Single(page.FindAll("#setup-row-g-1 .setup-row-changed"));
        page.Find("#setup-tab-plans").Click();
        page.Find("#use-plan-together").Click();
        Assert.Single(page.FindAll("#setup-bar-ask"));
        Assert.Empty(page.FindAll("#propose-setup"));
        page.Find("#setup-ask-no").Click();
        Assert.Equal("From Apart, 1 change", Text(page, "#setup-bar-source"));
        page.Find("#setup-tab-plans").Click();
        page.Find("#use-plan-together").Click();
        page.Find("#setup-ask-yes").Click();
        Assert.Equal("From Together", Text(page, "#setup-bar-source"));
        Assert.Single(page.FindAll(".setup-stack"));

        // The plan is proposed like any setup: the gate checks it, and the review says where the list came from.
        page.Find("#propose-setup").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("Set up the German side from Together, unchanged: 2 counters in 1 hex.", Text(page, "#setup-review-text"), StringComparison.Ordinal);
        Assert.Contains("Confirm is final", Text(page, "#setup-final"), StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Next: the Russian side sets up.", Text(page, "#setup-done-live"), StringComparison.Ordinal));
        var state = live.History("g1")!.Current!;
        Assert.Equal(["g-1", "g-2"], state.Units.Where(unit => unit.Side == "german").Select(unit => unit.Id).Order(StringComparer.Ordinal));
    }

    // Pass 35, task 35.16 (pass 31d's row; design D12, ruling R31c.6): the setup map's overlay is built again from the draft, and each draft
    // counter's tooltip and accessible name are led by its hex and by the counter as it reads, since a draft has no tag until it is in the game.
    [Fact]
    public void TheSetupMapLeadsEachDraftCounterWithItsHexAndItsCounter()
    {
        SaveTwoPlans(SaveCard());
        var page = Start();
        page.Find("#play-handover-confirm").Click();
        page.Find("#use-plan-apart").Click();
        page.WaitForAssertion(() => Assert.Contains("data-unit-id=\"draft:g-2\"", context.MapLayer("setUnits"), StringComparison.Ordinal));

        var counters = System.Text.RegularExpressions.Regex.Matches(context.MapLayer("setUnits"), "<g data-unit-id=\"(draft:[^\"]+)\"[^>]*aria-label=\"([^\"]*)\"[^>]*>\\s*<title>([^<]*)</title>");
        Assert.Equal(["draft:g-1", "draft:g-2"], counters.Select(match => match.Groups[1].Value).Order(StringComparer.Ordinal));
        foreach (System.Text.RegularExpressions.Match counter in counters)
        {
            var hex = counter.Groups[1].Value == "draft:g-1" ? "A1" : "B1";
            Assert.StartsWith($"{DisplayText.Place(1, $"{Board}:{hex}:0")}: 4-6-7 squad. ", counter.Groups[2].Value, StringComparison.Ordinal);
            Assert.Equal(counter.Groups[2].Value, counter.Groups[3].Value);
            Assert.DoesNotContain(counter.Groups[1].Value["draft:".Length..], counter.Groups[2].Value, StringComparison.Ordinal);
        }

        Assert.Contains("drawn paler", Text(page, "#setup-draft-note"), StringComparison.Ordinal);
    }

    [Fact]
    public void ACounterWithNoHexIsSaidBeforeTheGateIsAsked()
    {
        SaveTwoPlans(SaveCard());
        var page = Start();
        page.Find("#play-handover-confirm").Click();
        page.Find("#setup-tab-counters").Click();

        // "Set up" a group adds one line for each counter it owes; the list then lacks nothing, so the button goes.
        page.Find("#setup-group-german-1").Click();
        Assert.Empty(page.FindAll("#setup-groups"));
        Assert.Contains("Not placed yet (2)", Text(page, "#setup-unplaced-title"), StringComparison.Ordinal);

        page.Find("#propose-setup").Click();
        Assert.Contains("2 counters have no hex yet", Text(page, "#place-note"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#setup-review"));
    }

    [Fact]
    public void ThePlansAreOfferedToTheSideThatSetsUpFirstAlone()
    {
        SaveTwoPlans(SaveCard());
        var page = Start();
        page.Find("#play-handover-confirm").Click();
        Assert.Single(page.FindAll("#setup-plans"));

        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#setup-plans"));
        Assert.Empty(page.FindAll("[id^='show-plan-']"));
        Assert.Empty(page.FindAll("#setup-switcher"));
        Assert.Empty(page.FindAll("#propose-setup"));
        Assert.DoesNotContain("Apart", page.Markup, StringComparison.Ordinal);

        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Empty(page.FindAll("#setup-plans"));
        Assert.DoesNotContain("Apart", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void APlanForAnEarlierTextIsMarkedAndAFileThatCannotBeReadOffersNone()
    {
        SaveCard();
        SavePlans($$"""{"format":"asl-setup-plans/1","card":"{{CardId}}","plans":[{{Plan("old", "Old", new string('0', 64), "A1", "B1")}}]}""");
        var page = Start();
        page.Find("#play-handover-confirm").Click();
        Assert.Contains("Made for an earlier version of this card", Text(page, ".setup-plan[data-plan='old']"), StringComparison.Ordinal);

        // A second game: the address still names the first, so the page is opened afresh.
        SavePlans("{");
        context.Services.GetRequiredService<NavigationManager>().NavigateTo("games/play");
        page = Start("g2");
        page.Find("#play-handover-confirm").Click();
        page.Find("#setup-tab-plans").Click();
        Assert.Contains("cannot be read", Text(page, "#setup-plans-problem"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll(".setup-plan"));
    }

    [Fact]
    public void TheBarHoldsTheOnlyProposeAndAsksBeforeAListIsReplaced()
    {
        var proposed = 0;
        bool? answer = null;
        var bar = context.Render<SetupBar>(parameters => parameters
            .Add(item => item.Title, "German setup")
            .Add(item => item.Progress, "1 of 2 counters in the list")
            .Add(item => item.Source, "From Apart, 1 change")
            .Add(item => item.Count, 1)
            .Add(item => item.Problems, 2)
            .Add(item => item.CanAct, true)
            .Add(item => item.OnPropose, () => proposed++)
            .Add(item => item.OnAnswer, (bool yes) => answer = yes));

        Assert.Contains("problems", bar.Find("#setup-bar").ClassName, StringComparison.Ordinal);
        Assert.Equal("2 problems", bar.Find("#setup-bar-problems").TextContent);
        Assert.NotNull(bar.Find("#place-clear"));
        bar.Find("#propose-setup").Click();
        Assert.Equal(1, proposed);

        bar.Render(parameters => parameters.Add(item => item.Question, "Replace your list, with its 1 change?"));
        Assert.Empty(bar.FindAll("#propose-setup"));
        Assert.Equal("Replace my list", bar.Find("#setup-ask-yes").TextContent);
        bar.Find("#setup-ask-no").Click();
        Assert.False(answer);

        // A view that may not set up reads who is setting up, with nothing to press.
        bar.Render(parameters => parameters.Add(item => item.Question, null).Add(item => item.CanAct, false));
        Assert.Empty(bar.FindAll("button"));

        // An empty list has nothing to propose or clear.
        bar.Render(parameters => parameters.Add(item => item.CanAct, true).Add(item => item.Count, 0).Add(item => item.Problems, 0));
        Assert.True(bar.Find("#propose-setup").HasAttribute("disabled"));
        Assert.Empty(bar.FindAll("#place-clear"));
        Assert.Equal("The list is empty.", bar.Find("#propose-setup-ready").TextContent);
    }

    [Fact]
    public void APlanCardSaysWhatItIsAndWhereItStands()
    {
        var plan = new SetupPlan("apart", new string('0', 64), "german", "Apart", "The idea.", "What it gives up.", ["A fact."], []);
        var other = plan with
        {
            Id = "together",
            Name = "Together"
        };
        var shown = new List<string>();
        var used = new List<string>();
        var picker = context.Render<SetupPlanPicker>(parameters => parameters
            .Add(item => item.Side, "German")
            .Add(item => item.Plans, [plan, other])
            .Add(item => item.Chosen, "apart")
            .Add(item => item.Changes, 2)
            .Add(item => item.Shown, "together")
            .Add(item => item.Earlier, new HashSet<string> { "together" })
            .Add(item => item.OnShow, shown.Add)
            .Add(item => item.OnUse, used.Add));

        var first = picker.Find(".setup-plan[data-plan='apart']");
        Assert.Contains("chosen", first.ClassName, StringComparison.Ordinal);
        Assert.Contains("In your list, 2 changes", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("Gives up: What it gives up.", first.TextContent, StringComparison.Ordinal);
        Assert.Equal("Reset to Apart", picker.Find("#use-plan-apart").TextContent);
        var second = picker.Find(".setup-plan[data-plan='together']");
        Assert.Contains("Shown on the map", second.TextContent, StringComparison.Ordinal);
        Assert.Contains("Made for an earlier version of this card", second.TextContent, StringComparison.Ordinal);
        Assert.Equal("true", picker.Find("#show-plan-together").GetAttribute("aria-pressed"));
        Assert.Equal("Use this plan", picker.Find("#use-plan-together").TextContent);

        picker.Find("#show-plan-apart").Click();
        picker.Find("#use-plan-together").Click();
        Assert.Equal(["apart"], shown);
        Assert.Equal(["together"], used);

        // The plan in the list, unchanged, has nothing to reset.
        picker.Render(parameters => parameters.Add(item => item.Changes, 0));
        Assert.Empty(picker.FindAll("#use-plan-apart"));

        picker.Render(parameters => parameters.Add(item => item.Problems, ["setups.json: bad"]));
        Assert.Contains("setups.json: bad", picker.Find("#setup-plans-problem").TextContent, StringComparison.Ordinal);
        Assert.Empty(picker.FindAll(".setup-plan"));
    }

    [Fact]
    public void TheCountersTabGroupsTheListByStack()
    {
        var squad = new SetupCounters.Row("k1", "4-6-7 1st-line squad", "g-1") { Levels = [0, 1], Level = 1, Concealed = false, CanMove = true, Changed = true };
        var mg = new SetupCounters.Row("k2", "light MG 3-8", "g-lmg") { Held = true, Holders = [new SetupPlacementEditor.Choice("g-1", "4-6-7 1st-line squad (g-1)")], Holder = "g-1", Problem = true };
        var waiting = new SetupCounters.Row("k3", "4-6-7 1st-line squad", "g-2") { CanMove = true };
        var entering = new SetupCounters.Row("k4", "4-6-7 1st-line squad", "g-3") { OffBoard = true, Note = "enters on Turn 2 along the north edge" };
        var model = new SetupCounters.Counters([waiting], [new SetupCounters.Stack("bd01:F5:1", "F5 on board 01, level 1", [0, 1], 1, [squad, mg]) { Selected = true }], [], [entering])
        {
            OnMapSummary = "2 counters in 1 hex",
            OffBoardSummary = "1 counter",
            Notes = ["\"?\" used: 0 of 2."],
            CanPick = true,
        };
        var moved = new List<string>();
        var changes = new List<SetupPlacementList.Change>();
        var levels = new List<SetupCounters.LevelChange>();
        var removed = new List<string>();
        var list = context.Render<SetupCounters>(parameters => parameters
            .Add(item => item.Model, model)
            .Add(item => item.OnMove, moved.Add)
            .Add(item => item.OnChange, changes.Add)
            .Add(item => item.OnLevel, levels.Add)
            .Add(item => item.OnRemove, removed.Add));

        Assert.Equal("Not placed yet (1)", list.Find("#setup-unplaced-title").TextContent);
        Assert.Equal("On the map: 2 counters in 1 hex", list.Find("#setup-on-map-title").TextContent);
        Assert.Equal("\"?\" used: 0 of 2.", list.Find(".setup-area-note").TextContent);
        var stack = list.Find("#" + SetupCounters.StackId("bd01:F5:1"));
        Assert.Contains("selected", stack.ClassName, StringComparison.Ordinal);
        Assert.Contains("problem", stack.ClassName, StringComparison.Ordinal);
        Assert.Contains("held", list.Find("#setup-row-g-lmg").ClassName, StringComparison.Ordinal);
        Assert.Single(list.FindAll("#setup-row-g-lmg .setup-row-problem"));
        Assert.Single(list.FindAll("#setup-row-g-1 .setup-row-changed"));
        Assert.Contains("enters on Turn 2 along the north edge", list.Find("#setup-row-g-3").TextContent, StringComparison.Ordinal);

        // The off-board section stays closed until a counter in it needs the player, and is then never closed by the page.
        Assert.False(list.Find("#setup-off-board").HasAttribute("open"));
        list.Render(parameters => parameters.Add(item => item.Model, model with { OffBoardOpen = true }));
        Assert.True(list.Find("#setup-off-board").HasAttribute("open"));
        list.Render(parameters => parameters.Add(item => item.Model, model));
        Assert.True(list.Find("#setup-off-board").HasAttribute("open"));

        list.Find("#setup-row-g-2 .setup-row-move").Click();
        list.Find(".setup-stack-move").Click();
        list.Find(".setup-stack-level").Change("0");
        list.Find("#setup-row-g-1 .setup-row-level").Change("0");
        list.Find("#setup-row-g-1 .setup-row-concealed").Change(true);
        list.Find("#setup-row-g-lmg .setup-row-holder").Change(string.Empty);
        list.Find("#setup-row-g-3 .setup-row-offboard").Change(false);
        list.Find("#setup-row-g-3 .setup-row-remove").Click();
        Assert.Equal(["k3", SetupCounters.StackKey("bd01:F5:1")], moved);
        Assert.Equal([new SetupCounters.LevelChange("bd01:F5:1", 0)], levels);
        Assert.Equal([new("k1", "level", "0"), new("k1", "concealed", "true"), new("k2", "holder", string.Empty), new("k4", "offBoard", "false")], changes);
        Assert.Equal(["k4"], removed);

        // With no map to click, nothing offers a move.
        list.Render(parameters => parameters.Add(item => item.Model, model with { CanPick = false }));
        Assert.Empty(list.FindAll(".setup-row-move"));
        Assert.Empty(list.FindAll(".setup-stack-move"));
    }
}
