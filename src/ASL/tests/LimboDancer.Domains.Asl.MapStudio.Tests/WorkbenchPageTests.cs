using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using WorkbenchPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Workbench;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Rule Workbench's views (the review of Wiring Prompt 001, 2026-10-29): a game still played opens behind the hand-over screen (PS3); a viewer
/// change leaves nothing the last view picked or entered (PS2); and the opponent names an advancing "?" stack by its handle, so it can answer its own
/// LOS question without learning what is under it (PS4). Each test loads the page afresh.
/// </summary>
public sealed class WorkbenchPageTests : IDisposable
{
    private const string Live = "workbench-live";
    private const string Minigame = "workbench-minigame";
    private const string Contract = "a12.14-advance-concealment";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-workbench-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public WorkbenchPageTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        var boards = new WideBoards(maps);
        live = new LivePlay(library, boards, dice.Roller);
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, library, games));
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

    private static string ConcealedAt => WideBoards.At(3);

    private static Dictionary<string, object> Squad(string id, string definition, string at, string side, bool concealed) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:squad",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = concealed, ["asl:hidden"] = false },
    };

    private async Task Do(LimboDancer.Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        var revision = live.Store.Read(new GameScope(LivePlay.Tenant, Live))?.Events.Count ?? 0;
        node["gameId"] = Live;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{revision}";
        node["expectedRevision"] = revision;
        var element = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, element, live.Principal);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(action, element, live.Principal, proposed.Correlation) : proposed;
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
    }

    /// <summary>
    /// A game still played on the wide board, a concealed German squad and an unconcealed Russian one, gone on to the German PFPh; and the same
    /// record as the A12.14 minigame's game, which the workbench lets any view read (ruling R31b.1).
    /// </summary>
    private async Task Start()
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(WorkbenchPageTests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                scenarioYear = 1942,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3, san = 6 }, new { id = "russian", nationality = "russian", elr = 3, san = 6 } },
            },
            placements = new[] { Squad("de-c", "attacker-squad", ConcealedAt, "german", true), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian", false) },
        });
        await Do(GameActions.AdvancePhase, new
        {
        });

        var scope = new GameScope(LivePlay.Tenant, Minigame);
        var record = live.Store.Read(new GameScope(LivePlay.Tenant, Live))!;
        var path = Path.Combine(live.Root, LivePlay.Tenant.ToString("N"), Minigame + ".game.json");
        File.WriteAllText(path, GameEventWriter.Write(scope, new GameRecord("Workbench minigame", false, record.Events) { Minigame = "a12-advance-concealment" }));
        Assert.Equal("a12-advance-concealment", live.MinigameOf(Minigame));
    }

    private IRenderedComponent<WorkbenchPage> Open(string address)
    {
        context.Services.GetRequiredService<NavigationManager>().NavigateTo(address);
        return context.Render<WorkbenchPage>();
    }

    /// <summary>A map click at a Location, as the board viewer reports it, after the fact's "Pick on the map".</summary>
    private static void PickAt(IRenderedComponent<WorkbenchPage> page, string fact, string location)
    {
        page.Find($"tr[data-fact='{fact}'] button.workbench-pick").Click();
        var workspace = page.FindComponent<BoardWorkspace>();
        var facts = GameMaps.FactsAt(workspace.Instance.Board!, BoardLocation.Parse(location));
        page.InvokeAsync(() => workspace.Instance.SelectedChanged.InvokeAsync(facts));
    }

    private static string[] UnitChoices(IRenderedComponent<WorkbenchPage> page, string fact) =>
        [.. page.FindAll($"tr[data-fact='{fact}'] select.workbench-unit option").Select(option => option.GetAttribute("value") ?? string.Empty).Where(value => value.Length > 0)];

    private static string Source(IRenderedComponent<WorkbenchPage> page, string fact) => page.Find($"tr[data-fact='{fact}']").GetAttribute("data-source")!;

    [Fact]
    public async Task AGameStillPlayedOpensTheViewTheLinkNamesOnlyBehindTheHandOver()
    {
        await Start();

        // PS3: the address names the adjudicator; nothing of the game is shown until the viewer confirms it is at the screen.
        var page = Open($"games/workbench?game={Live}&viewer=adjudicator");
        Assert.Contains("adjudicator", page.Find("#play-handover-title").TextContent, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(page.FindAll("#workbench"));
        Assert.True(page.Find("#workbench-viewer").HasAttribute("disabled"));
        page.Find("#play-handover-confirm").Click();
        Assert.Single(page.FindAll("#workbench"));
    }

    [Fact]
    public async Task AViewTheGameDoesNotHaveGivesWayToThePhasingSidesBehindTheHandOver()
    {
        await Start();
        var page = Open($"games/workbench?game={Live}&viewer=nobody");
        Assert.Contains("German", page.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#workbench"));
    }

    [Fact]
    public async Task ChangingTheViewerDropsWhatTheLastViewPickedAndEntered()
    {
        await Start();
        var page = Open($"games/workbench?game={Minigame}&viewer=german&contract={Contract}");
        Assert.Empty(page.FindAll("#play-handover"));

        // PS2: the owner picks its concealed squad and enters a terrain by hand.
        PickAt(page, "advancerId", ConcealedAt);
        Assert.Contains("de-c", UnitChoices(page, "advancerId"));
        page.Find("tr[data-fact='advancerId'] select.workbench-unit").Change("de-c");
        page.Find("input.workbench-hand[data-fact='terrain']").Change("woods");
        Assert.Contains("de-c", page.Markup, StringComparison.Ordinal);

        // The opponent's view: the pick is gone, as is the entry, and nothing names the squad.
        page.Find("#workbench-viewer").Change("russian");
        Assert.DoesNotContain("de-c", page.Markup, StringComparison.Ordinal);
        Assert.Equal(string.Empty, page.Find("input.workbench-hand[data-fact='terrain']").GetAttribute("value") ?? string.Empty);
        Assert.Contains("none", page.Find("tr[data-fact='advancerId']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheOpponentNamesTheAdvancingStackByItsQuestionMarkAndAnswersItsOwnLos()
    {
        await Start();
        var page = Open($"games/workbench?game={Minigame}&viewer=russian&contract={Contract}");

        // PS4: the Russian view offers the German "?" stack, never the squad under it.
        PickAt(page, "advancerId", ConcealedAt);
        var handle = Assert.Single(UnitChoices(page, "advancerId"));
        Assert.NotEqual("de-c", handle);
        page.Find("tr[data-fact='advancerId'] select.workbench-unit").Change(handle);
        Assert.DoesNotContain("de-c", page.Markup, StringComparison.Ordinal);
        Assert.Contains("\"?\" stack", page.Find("tr[data-fact='advancerId']").TextContent, StringComparison.Ordinal);

        // What is under the "?" stays the owner's; whether the Russian units see it is the Russians' to say.
        Assert.Equal("sealed", Source(page, "advancerId"));
        Assert.Equal("pending", Source(page, "enemyGoodOrderInLosWithin16"));
        PickAt(page, "to", WideBoards.At(4));
        Assert.Equal("gathered", Source(page, "terrain"));
        Assert.Equal("gathered", Source(page, "enemyGoodOrderInLosWithin16"));
        Assert.DoesNotContain("de-c", page.Markup, StringComparison.Ordinal);
        // The Rules contract requires smoke and cover; their viewer-safe gathering follows in W-A10.531.
        Assert.Equal("not-evaluated", page.Find("#workbench-outcome").GetAttribute("data-outcome"));

        // The owner's view of the same game cannot name the Russian squad as its advancer, nor see the Russians' LOS.
        var owner = Open($"games/workbench?game={Minigame}&viewer=german&contract={Contract}");
        PickAt(owner, "advancerId", WideBoards.At(5));
        Assert.Empty(UnitChoices(owner, "advancerId"));
    }

    [Fact]
    public async Task RallyFactsDoNotTellTheOwnerWhetherAnEnemyQuestionMarkIsASeerOrADummy()
    {
        await Start();
        var source = live.History(Live)!.Current!;
        var squad = source.Unit("de-c")! with
        {
            Conditions = new Dictionary<string, ConditionState>(source.Unit("de-c")!.Conditions) { [Conditions.Broken] = ConditionState.True, [Conditions.Concealed] = ConditionState.False },
        };
        var leader = squad with
        {
            Id = "de-leader",
            Kind = "asl:leader",
            Definition = new DefinitionReference(source.Catalog, "attacker-leader"),
            Conditions = new Dictionary<string, ConditionState>(squad.Conditions) { [Conditions.Broken] = ConditionState.False, [Conditions.Concealed] = ConditionState.True },
        };
        var enemy = source.Unit("ru-1")! with
        {
            Conditions = new Dictionary<string, ConditionState>(source.Unit("ru-1")!.Conditions) { [Conditions.Broken] = ConditionState.False, [Conditions.Concealed] = ConditionState.True },
        };
        var real = source with
        {
            Phase = "rph",
            Units = [squad, leader, enemy]
        };
        var dummy = real with
        {
            Units = [squad, leader, enemy with { Kind = UnitKinds.Dummy, Definition = null }]
        };
        var picks = new Dictionary<string, string> { ["unitId"] = squad.Id, ["leaderId"] = leader.Id };

        var ownerReal = live.Planner.ContractFacts("a10.6-rally", real, picks, Perspective.Side("german"));
        var ownerDummy = live.Planner.ContractFacts("a10.6-rally", dummy, picks, Perspective.Side("german"));
        Assert.Equal(ownerReal.Select(pair => (pair.Key, pair.Value?.ToJsonString())), ownerDummy.Select(pair => (pair.Key, pair.Value?.ToJsonString())));
        Assert.False(ownerReal.ContainsKey("enemyGoodOrderInLosWithin16"));
        Assert.True(ownerReal["goodOrderLeaderInLocation"]!.GetValue<bool>());

        var hiddenEnemy = enemy with
        {
            Conditions = new Dictionary<string, ConditionState>(enemy.Conditions) { [Conditions.Hidden] = ConditionState.True },
        };
        var hidden = real with
        {
            Units = [squad, leader, hiddenEnemy]
        };
        var absent = real with
        {
            Units = [squad, leader]
        };
        var ownerHidden = live.Planner.ContractFacts("a10.6-rally", hidden, picks, Perspective.Side("german"));
        var ownerAbsent = live.Planner.ContractFacts("a10.6-rally", absent, picks, Perspective.Side("german"));
        Assert.Equal(ownerAbsent.Select(pair => (pair.Key, pair.Value?.ToJsonString())), ownerHidden.Select(pair => (pair.Key, pair.Value?.ToJsonString())));
        Assert.False(ownerHidden["knownEnemyInLos"]!.GetValue<bool>());
        Assert.Equal("none", ownerHidden["captors"]!.GetValue<string>());

        var opponentReal = live.Planner.ContractFacts("a10.6-rally", real, picks, Perspective.Side("russian"));
        var opponentDummy = live.Planner.ContractFacts("a10.6-rally", dummy, picks, Perspective.Side("russian"));
        Assert.True(opponentReal["enemyGoodOrderInLosWithin16"]!.GetValue<bool>());
        Assert.False(opponentDummy["enemyGoodOrderInLosWithin16"]!.GetValue<bool>());
        Assert.False(opponentReal.ContainsKey("goodOrderLeaderInLocation"));
        Assert.False(opponentReal.ContainsKey("leaderDefinition"));
        Assert.Null(opponentReal["leaderId"]);
        Assert.True(opponentReal["unitBroken"]!.GetValue<bool>());

        var adjudicator = live.Planner.ContractFacts("a10.6-rally", real, picks, Perspective.Adjudicator);
        Assert.True(adjudicator["enemyGoodOrderInLosWithin16"]!.GetValue<bool>());
        Assert.Equal(leader.Id, adjudicator["leaderId"]!.GetValue<string>());
    }

    [Fact]
    public async Task ContractFactsRejectPrivateIdsAndDoNotResolveASealedHandleToAnIdentity()
    {
        await Start();
        var state = live.History(Live)!.Current!;
        var viewer = Perspective.Side("russian");
        var view = live.Planner.ViewOf(state, viewer, []);
        var handle = Assert.Single(view.Sealed).PlacementId;
        var privatePick = new Dictionary<string, string> { ["advancerId"] = "de-c", ["to"] = WideBoards.At(4) };
        var rejected = live.Planner.ContractFacts(Contract, state, privatePick, viewer);
        Assert.False(rejected.ContainsKey("advancerId"));
        Assert.False(rejected.ContainsKey("enemyGoodOrderInLosWithin16"));

        privatePick["advancerId"] = handle;
        var gathered = live.Planner.ContractFacts(Contract, state, privatePick, viewer);
        Assert.False(gathered.ContainsKey("advancerId"));
        Assert.True(gathered["enemyGoodOrderInLosWithin16"]!.GetValue<bool>());
        Assert.DoesNotContain(gathered.Values, value => value?.ToJsonString().Contains("de-c", StringComparison.Ordinal) == true);

        var owner = live.Planner.ContractFacts(Contract, state, new Dictionary<string, string>
        {
            ["advancerId"] = "de-c",
            ["to"] = WideBoards.At(4),
        }, Perspective.Side("german"));
        Assert.Equal("de-c", owner["advancerId"]!.GetValue<string>());
        Assert.False(owner.ContainsKey("enemyGoodOrderInLosWithin16"));
    }
}
