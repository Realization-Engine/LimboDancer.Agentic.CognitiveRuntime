using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 30b: setup plans for the side that sets up second. Its plans stand in groups, one for each plan of the other side, ordered by how closely
/// that plan matches the stacks in the viewer's own view, with the plans for any setup; every plan is always offered. The other side and the
/// adjudicator are shown none of it (rulings R23.1, R23.3). Two stacks of the list change hexes with "Swap".
/// </summary>
public sealed class SetupAnswersPageTests : IDisposable
{
    private const string CardId = "answered";

    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-setup-answers-" + Guid.NewGuid().ToString("N"));
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public SetupAnswersPageTests()
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

    /// <summary>
    /// A user's card on the test board: two German squads set up first in A1 and B1, then a Russian squad in C1. Its setups file has two German
    /// plans, a Russian answer to the first, and a Russian plan for any setup.
    /// </summary>
    private void SaveCardAndPlans()
    {
        var catalog = live.Catalogs[0];
        var card = new ScenarioCard(ScenarioCards.Format, CardId, "Answered", $"{catalog.Identity.Catalog}@{catalog.Identity.Version}", new ScenarioCardSource("A test.", "none", []),
            string.Empty, new ScenarioCardDate(1, 7, 1942), string.Empty, [new ScenarioCardBoard(Board, 0, 0, false)], "top", null, new ScenarioCardTurns(5, false, "german", "german"), null,
            [Side("german", Group("The line", 1, "A1", ["A1", "B1"], "attacker-squad", 2)), Side("russian", Group("The guard", 2, "C1", ["C1"], "defender-squad", 1))], [],
            new ScenarioCardVictory("other", "The players judge the result.", ["A26.1"]), null);
        Assert.Empty(live.Cards.Save(card, catalog));
        var sha = live.Cards.Sha256(CardId)!;

        string German(string id, string name, string first, string second) =>
            $$"""
            {"id":"{{id}}","cardSha256":"{{sha}}","side":"german","name":"{{name}}","idea":"The idea of {{name}}.","givesUp":"What {{name}} gives up.","terrain":[],
             "placements":[{"id":"g-1","definition":"attacker-squad","group":"german-1","at":"{{Board}}:{{first}}:0"},{"id":"g-2","definition":"attacker-squad","group":"german-1","at":"{{Board}}:{{second}}:0"}]}
            """;
        string Russian(string id, string name, string answers) =>
            $$"""
            {"id":"{{id}}","cardSha256":"{{sha}}","side":"russian",{{answers}}"name":"{{name}}","idea":"The idea of {{name}}.","givesUp":"What {{name}} gives up.","terrain":[],
             "placements":[{"id":"r-1","definition":"defender-squad","group":"russian-1","at":"{{Board}}:C1:0"}]}
            """;

        // The answer's hash is of the plan it answers, as the file's reader computes it.
        var draft = $$"""{"format":"{{ScenarioSetupPlans.Format}}","card":"{{CardId}}","plans":[{{German("apart", "Apart", "A1", "B1")}},{{German("together", "Together", "B1", "B1")}}]}""";
        var apart = ScenarioSetupPlans.Parse(card, draft, catalog).Plans.Single(plan => plan.Id == "apart");
        var answers = "\"answers\":{\"plan\":\"apart\",\"placementsSha256\":\"" + ScenarioSetupPlans.PlacementsSha256(apart) + "\"},";
        File.WriteAllText(Path.Combine(live.Cards.Directory!, CardId + ScenarioSetupPlans.Suffix),
            $$"""{"format":"{{ScenarioSetupPlans.Format}}","card":"{{CardId}}","plans":[{{German("apart", "Apart", "A1", "B1")}},{{German("together", "Together", "B1", "B1")}},{{Russian("reply", "Reply", answers)}},{{Russian("anyhow", "Anyhow", string.Empty)}}]}""");
        Assert.Empty(live.Cards.Plans(CardId, catalog)!.Diagnostics);
    }

    /// <summary>Starts a game from the card, sets up the German side from a plan, and hands the screen to the Russian side.</summary>
    private IRenderedComponent<PlayPage> GermanSetUp(string plan)
    {
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(CardId);
        page.Find("#new-id").Change("g1");
        page.Find("#start-game").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll("#play-handover-confirm")));
        page.Find("#play-handover-confirm").Click();

        // The side that sets up first is offered its own plans, as they are, and none of the other side's.
        Assert.Equal(["apart", "together"], page.FindAll(".setup-plan").Select(card => card.GetAttribute("data-plan")));
        Assert.Empty(page.FindAll(".setup-plan-group"));
        Assert.DoesNotContain("Reply", page.Markup, StringComparison.Ordinal);

        page.Find($"#use-plan-{plan}").Click();
        page.Find("#propose-setup").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Next: the Russian side sets up.", page.Find("#setup-done-live").TextContent, StringComparison.Ordinal));
        page.Find("#setup-handover").Click();
        page.Find("#play-handover-confirm").Click();
        return page;
    }

    private static string Text(IRenderedComponent<PlayPage> page, string selector) => page.Find(selector).TextContent.Trim();

    private static string[] Groups(IRenderedComponent<PlayPage> page) => [.. page.FindAll(".setup-plan-group").Select(group => group.GetAttribute("data-group")!)];

    [Fact]
    public void TheNewGameFormCountsEachSidesPlans()
    {
        SaveCardAndPlans();
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(CardId);

        Assert.Contains("2 setup plans for the German side and 2 setup plans for the Russian side", Text(page, "#start-plans-note"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheSecondSidesPlansStandInGroupsWithTheClosestFirst()
    {
        SaveCardAndPlans();
        var page = GermanSetUp("apart");

        Assert.Equal("Russian setup", Text(page, "#setup-bar-title"));
        Assert.Contains("The German side has set up: you see its stacks in 2 hexes.", Text(page, "#setup-seen"), StringComparison.Ordinal);
        Assert.Equal(["apart", "together", "_any"], Groups(page));

        // The plan the stacks match is first, marked, and open, with the answer to it; the other plan has no answer; the plan for any setup is last.
        var first = page.Find(".setup-plan-group[data-group='apart']");
        Assert.Contains("If they set up like Apart", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("2 of 2 hexes match", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("Closest to what you see", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("Their plan, Apart:", first.TextContent, StringComparison.Ordinal);
        Assert.True(first.HasAttribute("open"));
        Assert.Single(first.QuerySelectorAll(".setup-plan[data-plan='reply']"));
        var second = page.Find(".setup-plan-group[data-group='together']");
        Assert.Contains("0 of 2 hexes match", second.TextContent, StringComparison.Ordinal);
        Assert.Contains("No answer to this plan is written", second.TextContent, StringComparison.Ordinal);
        Assert.False(second.HasAttribute("open"));
        Assert.Single(page.Find(".setup-plan-group[data-group='_any']").QuerySelectorAll(".setup-plan[data-plan='anyhow']"));

        // An answer shown on the map brings the outline of the plan it answers, which may be turned off; the list is left alone.
        page.Find("#show-plan-reply").Click();
        Assert.Equal("true", page.Find("#map-their-outline").GetAttribute("aria-pressed"));
        Assert.Contains("where Apart would put their stacks", Text(page, "#setup-their-legend"), StringComparison.Ordinal);
        Assert.Contains("0 of 1", Text(page, "#setup-bar-progress"), StringComparison.Ordinal);
        page.Find("#map-their-outline").Click();
        Assert.Equal("false", page.Find("#map-their-outline").GetAttribute("aria-pressed"));
        Assert.Empty(page.FindAll("#setup-their-legend"));

        // The answer fills the list, and the bar and the review name the plan it answers; the gate checks it like any setup.
        page.Find("#use-plan-reply").Click();
        Assert.Equal("From Reply (answers Apart)", Text(page, "#setup-bar-source"));
        page.Find("#propose-setup").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("Set up the Russian side from Reply (answers Apart), unchanged: 1 counter in 1 hex.", Text(page, "#setup-review-text"), StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("from Reply (answers Apart)", Text(page, "#setup-done-live"), StringComparison.Ordinal));
        Assert.Contains("Each side may now place a \"?\"", Text(page, "#setup-done-live"), StringComparison.Ordinal);
        Assert.Equal(["r-1"], live.History("g1")!.Current!.Units.Where(unit => unit.Side == "russian").Select(unit => unit.Id));
    }

    [Fact]
    public void ASetupThatFollowsAPlanWithNoAnswerOpensThePlansForAnySetup()
    {
        SaveCardAndPlans();
        var page = GermanSetUp("together");

        // Together is the plan the stacks match, and it has no answer: the plan for any setup comes first and stands open.
        Assert.Equal(["_any", "together", "apart"], Groups(page));
        Assert.True(page.Find(".setup-plan-group[data-group='_any']").HasAttribute("open"));
        Assert.False(page.Find(".setup-plan-group[data-group='together']").HasAttribute("open"));
        Assert.Contains("1 of 1 hex match", page.Find(".setup-plan-group[data-group='together']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TheOtherSideAndTheAdjudicatorAreShownNothingOfTheSecondSidesPlans()
    {
        SaveCardAndPlans();
        var page = GermanSetUp("apart");
        page.Find("#use-plan-reply").Click();
        Assert.Single(page.FindAll(".setup-stack"));

        page.ViewAs("german");
        Assert.Empty(page.FindAll("#setup-plans"));
        Assert.Empty(page.FindAll(".setup-plan-group"));
        Assert.Empty(page.FindAll("#setup-seen"));
        Assert.Empty(page.FindAll("#map-their-outline"));
        Assert.Empty(page.FindAll(".setup-stack"));
        Assert.DoesNotContain("Reply", page.Markup, StringComparison.Ordinal);
        Assert.DoesNotContain("Anyhow", page.Markup, StringComparison.Ordinal);

        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Empty(page.FindAll("#setup-plans"));
        Assert.Empty(page.FindAll(".setup-plan-group"));
        Assert.DoesNotContain("Reply", page.Markup, StringComparison.Ordinal);

        // Back in its own view the second side starts again: the hand-over cleared its list.
        page.ViewAs("russian");
        Assert.Equal(3, page.FindAll(".setup-plan-group").Count);
        Assert.Contains("0 of 1", Text(page, "#setup-bar-progress"), StringComparison.Ordinal);
    }

    [Fact]
    public void SwapMakesTwoStacksOfTheListChangeHexes()
    {
        SaveCardAndPlans();
        var page = context.Render<PlayPage>();
        page.Find("#new-card").Change(CardId);
        page.Find("#new-id").Change("g1");
        page.Find("#start-game").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Single(page.FindAll("#play-handover-confirm")));
        page.Find("#play-handover-confirm").Click();
        page.Find("#use-plan-apart").Click();
        page.Find("#setup-row-g-1 .setup-row-concealed").Change(true);

        string Stack(string hex) => "#" + SetupCounters.StackId($"{Board}:{hex}:0");
        Assert.Single(page.FindAll($"{Stack("A1")} #setup-row-g-1"));

        // "Swap" waits for another stack; the same hex swaps nothing and says so.
        page.Find($"{Stack("A1")} .setup-stack-swap").Click();
        Assert.Contains("swap its counters", Text(page, "#setup-swap-banner"), StringComparison.Ordinal);
        Assert.Single(page.FindAll(".setup-stack-swapping"));
        page.Find($"{Stack("A1")} .setup-stack-name").Click();
        Assert.Contains("That is the same hex", Text(page, "#setup-swap-note-list"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#setup-swap-banner"));

        // Another stack completes it: the two change hexes, and the change is counted against the plan.
        page.Find($"{Stack("A1")} .setup-stack-swap").Click();
        page.Find($"{Stack("B1")} .setup-stack-name").Click();
        Assert.Contains("Swapped the counters", Text(page, "#setup-swap-note-list"), StringComparison.Ordinal);
        Assert.Single(page.FindAll($"{Stack("B1")} #setup-row-g-1"));
        Assert.Single(page.FindAll($"{Stack("A1")} #setup-row-g-2"));
        Assert.Equal("From Apart, 2 changes", Text(page, "#setup-bar-source"));

        // A swap whose stack leaves the list ends with it.
        page.Find($"{Stack("A1")} .setup-stack-swap").Click();
        page.Find("#setup-row-g-2 .setup-row-remove").Click();
        Assert.Empty(page.FindAll("#setup-swap-banner"));
        Assert.Empty(page.FindAll(".setup-stack-swapping"));
    }

    [Fact]
    public void ThePickerShowsGroupsWithTheirScoresAndWhatAPlanSaysOfBeingPublic()
    {
        var their = new SetupPlan("apart", new string('0', 64), "german", "Apart", "Their idea.", "Their cost.", [], []);
        var reply = new SetupPlan("reply", new string('0', 64), "russian", "Reply", "The idea.", "What it gives up.", [], [], new SetupPlanAnswer("apart", new string('0', 64)));
        var anyhow = reply with
        {
            Id = "anyhow",
            Name = "Anyhow",
            Answers = null
        };
        SetupPlanPicker.Group[] groups =
        [
            new("apart", "If they set up like Apart", "2 of 2 hexes match", true, true, their, [reply]),
            new("together", "If they set up like Together", "0 of 2 hexes match", false, false, their with { Id = "together", Name = "Together" }, []),
            new("_any", "For any setup", null, false, false, null, [anyhow]),
        ];
        var picker = context.Render<SetupPlanPicker>(parameters => parameters
            .Add(item => item.Side, "Russian")
            .Add(item => item.Plans, [reply, anyhow])
            .Add(item => item.Groups, groups)
            .Add(item => item.MatchNote, "Their plans look the same from here.")
            .Add(item => item.EarlierAnswers, new Dictionary<string, string> { ["reply"] = "Apart" })
            .Add(item => item.PublicNotes, new Dictionary<string, string> { ["anyhow"] = "Setup plans are public." }));

        Assert.Contains("Their plans look the same from here.", picker.Find("#setup-plans-match").TextContent, StringComparison.Ordinal);
        Assert.Equal(3, picker.FindAll(".setup-plan-group").Count);
        var first = picker.Find("#setup-plan-group-apart");
        Assert.Contains("closest", first.ClassName, StringComparison.Ordinal);
        Assert.True(first.HasAttribute("open"));
        Assert.Contains("Their plan, Apart: Their idea. Gives up: Their cost.", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("Made for an earlier version of Apart", first.TextContent, StringComparison.Ordinal);
        Assert.Contains("No answer to this plan is written", picker.Find("#setup-plan-group-together").TextContent, StringComparison.Ordinal);
        Assert.Equal("Setup plans are public.", picker.Find("#setup-plan-anyhow-public").TextContent);
        Assert.Empty(picker.FindAll("#setup-plan-reply-public"));

        // With no groups the plans are listed as they are, as for the side that sets up first.
        picker.Render(parameters => parameters.Add(item => item.Groups, []));
        Assert.Empty(picker.FindAll(".setup-plan-group"));
        Assert.Equal(2, picker.FindAll(".setup-plan").Count);
    }
}
