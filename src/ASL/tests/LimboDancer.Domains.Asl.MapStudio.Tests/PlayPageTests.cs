using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The verified synthetic board with a stone building painted over the center of B1, so a squad has somewhere to enter.</summary>
internal sealed class BuildingBoards(MapService maps) : IBoardProvider
{
    public static readonly StudioBoard Board = FakeBoardProvider.Board with { Render = Input() };

    public string? SourceDescription => "Synthetic test source";

    public string? CatalogBlob => null;

    public IReadOnlyList<BoardListing> List() => [new BoardListing(Board.Ref, Board.Title)];

    public BoardLoadResult Load(BoardRef board) =>
        board == Board.Ref ? new BoardLoadResult(Board, [])
        : board.Kind == BoardRefKind.ComposedMap ? maps.Load(board)
        : new BoardLoadResult(null, []);

    public BoardLoadResult? Cached(BoardRef board) => Load(board);

    private static BoardRenderInput Input()
    {
        var source = SyntheticBoard.Grid();
        var geometry = source.Geometry;
        var codes = source.Codes.ToArray();
        var center = geometry.CenterPoint(geometry.IndexOf(HexName.Parse("B1")));
        for (var x = 0; x < geometry.GridWidth; x++)
        {
            for (var y = 0; y < geometry.GridHeight; y++)
            {
                if ((x - center.X) * (x - center.X) + (y - center.Y) * (y - center.Y) < 100)
                {
                    codes[(x * geometry.GridHeight) + y] = 42;
                }
            }
        }

        var grid = new TerrainGrid(geometry, codes, source.Elevations, source.Stairways);
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, SyntheticBoard.Catalog, HexsideAnnotations.None);
        return BoardRenderInput.Create(FakeBoardProvider.Board.Ref, FakeBoardProvider.Board.Title, grid, SyntheticBoard.Catalog, facts);
    }
}

/// <summary>The Play page and live games (Governed Writes Design, section 10), on the verified synthetic board.</summary>
public sealed class PlayPageTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-" + Guid.NewGuid().ToString("N"));
    private readonly UnitLibrary library;
    private readonly BuildingBoards boards;
    private readonly LivePlay live;
    private readonly GameLibrary games;
    private readonly BunitContext context = new();

    public PlayPageTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards);
        games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
    }

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    /// <summary>An open hex and the adjacent ground-level building an attacker there may enter.</summary>
    private (string From, string Building) EntryPair()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        return (from index in handle.Geometry.Hexes()
                let building = handle.Geometry.NameOf(index)
                where handle.HexFacts(building)!.Locations.Any(location => location.Level == 0 && location.Terrain?.Name.Contains("Building", StringComparison.Ordinal) == true)
                from side in Enum.GetValues<HexsideDirection>()
                let open = handle.Neighbor(building, side)
                where open is not null && handle.HexFacts(open.Value) is { Center.Terrain.Name: "Open Ground" } facts
                    && facts.BaseLevel == handle.HexFacts(building)!.BaseLevel
                select ($"{Board}:{open}:0", $"{Board}:{building}:0")).First();
    }

    private IRenderedComponent<PlayPage> StartGame(string at)
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board });
        page.Find("#place-id").Change("g1");
        page.Find("#place-location").Change(at);
        page.Find("#place-add").Click();
        page.Find("#propose-setup").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Empty(live.Games());
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        return page;
    }

    // Pass 23 (rulings R23.2, R23.3; plan section 15.4): a new view waits behind the hand-over screen, which leaves the map, the units, and the board
    // link out of the page until its viewer confirms; the board link then reads the game as that view, not as the adjudicator.
    [Fact]
    public void ANewViewWaitsForItsViewer()
    {
        var page = StartGame($"{Board}:A1:0");
        var first = page.Find("#play-perspective").GetAttribute("value")!;
        Assert.NotEqual(Perspective.AdjudicatorName, first);
        Assert.Contains($"perspective={first}", page.Find("#play-view").GetAttribute("href"), StringComparison.Ordinal);

        page.Find("#play-perspective").Change(Perspective.AdjudicatorName);
        Assert.Contains("Hand the screen to the adjudicator", page.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-units"));
        Assert.Empty(page.FindAll("#play-workspace"));
        Assert.Empty(page.FindAll("#play-view"));
        page.Find("#play-handover-confirm").Click();
        Assert.Empty(page.FindAll("#play-handover"));
        Assert.Contains("perspective=adjudicator", page.Find("#play-view").GetAttribute("href"), StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#play-units tr[data-unit='g1']"));

        // Choosing the view shown again drops a waiting hand-over.
        page.Find("#play-perspective").Change(first);
        page.Find("#play-perspective").Change(Perspective.AdjudicatorName);
        Assert.Empty(page.FindAll("#play-handover"));
    }

    // Plan section 13.4 (UI review and table player, pass 23): a hand-over drops what the last view drafted; the movers are the moving side's own.
    [Fact]
    public void AHandOverDropsTheLastViewsDrafts()
    {
        var page = StartGame($"{Board}:A1:0");
        page.Find("#place-id").Change("g2");
        page.Find("#place-location").Change($"{Board}:A1:0");
        page.Find("#place-add").Click();
        Assert.NotEmpty(page.FindAll("#place-list"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#place-list"));

        page.ViewAs("german");
        Commit(page, "#propose-advance");

        // The Studio demo of pass 23: Opportunity Fire is the phasing side's choice, so the other side sees none of it.
        Assert.NotEmpty(page.FindAll("#propose-opportunity"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#propose-opportunity"));
        page.ViewAs("german");
        Commit(page, "#propose-advance");
        Assert.Contains("Movement Phase", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll(".move-unit"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll(".move-unit"));
        Assert.Contains("German side moves now", page.Find("#move-elsewhere").TextContent, StringComparison.Ordinal);
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public void SetupCommitsOnlyAfterConfirmation()
    {
        var page = StartGame($"{Board}:A1:0");
        var game = Assert.Single(live.Games());
        Assert.Equal("village", game.Game);
        Assert.Equal("revision 2", page.Find("#play-revision").TextContent);
        Assert.Contains("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#play-units tr[data-unit='g1']"));

        // The audit tail is the adjudicator's; a side's view does not show it.
        Assert.DoesNotContain("ExecutorCompleted", page.Markup, StringComparison.Ordinal);
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains("ExecutorCompleted", page.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void TheReviewedEntryShowsItsFactsAndCommits()
    {
        var (from, building) = EntryPair();
        var page = StartGame(from);
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        Assert.Contains("Movement Phase", page.Find("#play-summary").TextContent, StringComparison.Ordinal);

        page.ViewAs(Perspective.AdjudicatorName);
        page.Find("#enter-location").Change(building);
        page.Find("#propose-enter").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll("#play-facts")));
        Assert.Contains("Definitive", page.Find("#play-conclusion").TextContent, StringComparison.Ordinal);
        Assert.Equal(9, page.FindAll("#play-facts tbody tr").Count);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var row = page.Find("#play-units tr[data-unit='g1']").TextContent;
        Assert.Contains(building, row, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMovingSideSeesAnEntryOnlyAsDeclaredUntilItIsConfirmed()
    {
        // Occupied and Concealed Entry Design, section 7: the side cannot tell from the proposal whether the building is empty.
        var (from, building) = EntryPair();
        var page = StartGame(from);
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        page.Find("#enter-location").Change(building);
        page.Find("#propose-enter").Click();
        page.WaitForAssertion(() => Assert.Contains("The entry is declared", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#play-facts"));
        Assert.Equal([EntryDisclosure.ResolvedOnConfirmation], page.FindAll("#play-reasons li").Select(item => item.TextContent));

        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains(building, page.Find("#play-units tr[data-unit='g1']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ARefusedEntryShowsWhyAndChangesNothing()
    {
        var page = StartGame($"{Board}:A1:0");
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        page.Find("#enter-location").Change($"{Board}:A1:0");
        page.Find("#propose-enter").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#play-confirm"));
        var reasons = page.FindAll("#play-reasons li").Select(item => item.TextContent).ToList();
        Assert.Equal(reasons.Count, reasons.Distinct(StringComparer.Ordinal).Count());

        // The refusal depends only on the mover and the terrain, so the side is told why at once.
        Assert.Contains("play.fact-false: isAdjacentGroundLevelOrdinaryBuilding", reasons);
        Assert.Equal("revision 4", page.Find("#play-revision").TextContent);
    }

    /// <summary>A game in the German MPh with g1 in A1 and the Russian units given, each concealed, in B1.</summary>
    private IRenderedComponent<PlayPage> GameWithDefenders(params (string Id, string Definition)[] defenders)
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board });
        page.Find("#place-id").Change("g1");
        page.Find("#place-location").Change($"{Board}:A1:0");
        page.Find("#place-add").Click();
        foreach (var (id, definition) in defenders)
        {
            page.Find("#place-definition").Change(definition);
            page.Find("#place-id").Change(id);
            page.Find("#place-location").Change($"{Board}:B1:0");
            page.Find("#place-concealed").Change(true);
            page.Find("#place-add").Click();
            page.Find("#place-concealed").Change(false);
        }

        Commit(page, "#propose-setup");
        Commit(page, "#propose-advance");
        Commit(page, "#propose-advance");
        return page;
    }

    /// <summary>Appends a committed batch the way the store does, so the page can be shown a rolled or pending entry.</summary>
    private void Append(params (string Type, EventPayload Payload, string[]? Visibility)[] batch)
    {
        var scope = new GameScope(LivePlay.Tenant, "village");
        var existing = live.Store.Read(scope)!.Events;
        GameEvent[] events = [.. batch.Select((item, index) => new GameEvent(scope, $"enter-1-{index + 1}", existing.Count + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, index == 0 ? [] : ["enter-1-1"], item.Visibility))];
        Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Village", existing.Count, events, live.Planner.Replay).Status);
    }

    private static readonly Dictionary<string, ConditionState> Revealed = new() { [Conditions.Concealed] = ConditionState.False };

    [Fact]
    public void RollsAreShownToEveryoneAndTheirSubjectsOnlyToTheSelectedSideAndTheAdjudicator()
    {
        var page = GameWithDefenders(("r1", "defender-squad"), ("r2", "defender-squad"));
        var a1 = BoardLocation.Parse($"{Board}:A1:0");
        Append(("entry-attempted", new EntryAttempted("g1", BoardLocation.Parse($"{Board}:B1:0"), 2), null),
            ("dice-rolled", new DiceRolled("enter-1-roll-1", "random-selection", 2, 6, [6, 2], DiceRolled.SystemSource, "studio-user"), null),
            ("random-selection", new RandomSelection("enter-1-roll-1", "enter-1-1", ["r1", "r2"]), ["russian"]),
            ("conditions-changed", new ConditionsChanged("r1", Revealed), null),
            ("entry-forced-back", new EntryForcedBack("g1", "enter-1-1", a1, 2, false), null));
        page.OpenGame("village");

        var german = page.Find("#play-rolls li").TextContent;
        Assert.Contains("random-selection: 6, 2", german, StringComparison.Ordinal);
        Assert.DoesNotContain("r2", german, StringComparison.Ordinal);
        Assert.Contains("movement ended", page.Find("#play-units tr[data-unit='g1']").TextContent, StringComparison.Ordinal);

        page.ViewAs("russian");
        Assert.Contains("for r1, r2", page.Find("#play-rolls li").TextContent, StringComparison.Ordinal);
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Contains("for r1, r2", page.Find("#play-rolls li").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void AnNtcIsShownWithItsArithmetic()
    {
        var page = GameWithDefenders(("l1", "defender-leader"), ("r1", "defender-squad"));
        var a1 = BoardLocation.Parse($"{Board}:A1:0");
        Append(("entry-attempted", new EntryAttempted("g1", BoardLocation.Parse($"{Board}:B1:0"), 2), null),
            ("conditions-changed", new ConditionsChanged("l1", Revealed), null),
            ("overrun-declared", new OverrunDeclared("g1", "enter-1-1", OverrunDeclared.Elected), null),
            ("dice-rolled", new DiceRolled("enter-1-roll-1", TaskCheck.OvrNtc, 2, 6, [3, 5], DiceRolled.SystemSource, "studio-user"), null),
            ("task-check", new TaskCheck("g1", "enter-1-roll-1", TaskCheck.OvrNtc, 7, [new TaskCheckModifier("B23.3", 3)], 11, false), null),
            ("entry-forced-back", new EntryForcedBack("g1", "enter-1-1", a1, 2, false), null));
        page.OpenGame("village");
        Assert.Contains("OVR NTC: 3, 5 + 3 (TEM) = 11 against morale 7: failed", page.Find("#play-rolls li").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TheMapIsDrawnWithTheUnitsTheViewerMaySee()
    {
        var page = GameWithDefenders(("r1", "defender-squad"));
        page.ViewAs(Perspective.AdjudicatorName);
        page.WaitForAssertion(() => Assert.Contains("data-unit-id=\"r1\"", context.MapLayer("setUnits"), StringComparison.Ordinal));
        Assert.Equal(Board, page.MapSource());

        // The German side sees the concealed r1 only as a sealed presence.
        page.ViewAs("german");
        page.WaitForAssertion(() => Assert.DoesNotContain("data-unit-id=\"r1\"", context.MapLayer("setUnits"), StringComparison.Ordinal));
        Assert.Contains("data-unit-id=\"g1\"", context.MapLayer("setUnits"), StringComparison.Ordinal);

        // Choosing a unit's location highlights its hex, and the picked hex lists what the view sees there (pass 28c).
        Assert.Empty(context.MapLayer("highlight"));
        page.Find("#play-units tr[data-unit='g1'] .play-locate").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(context.MapLayer("highlight")));
        Assert.Contains("g1", page.Find("#hex-units").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r1", page.Find("#play-panel-review").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void LosIsCheckedOverTheGamesMapAndDrawnOnIt()
    {
        // The synthetic board's terrain includes a depression, which the read now answers.
        var page = GameWithDefenders(("r1", "defender-squad"));
        page.Find("#play-inspector-tab-los").Click();
        page.Find("#los-source").Change("A1");
        page.Find("#los-target").Change("C1");
        page.Find("#los-check").Click();
        Assert.Equal("Clear", page.Find("#los-result").GetAttribute("data-status"));
        Assert.Contains("Clear, range 2", page.Find("#los-result").TextContent, StringComparison.Ordinal);
        page.WaitForAssertion(() => Assert.Contains("id=\"layer-los\"", context.MapLayer("setLos"), StringComparison.Ordinal));

        page.Find("#los-clear").Click();
        page.WaitForAssertion(() => Assert.DoesNotContain("layer-los", context.MapLayer("setLos"), StringComparison.Ordinal));
    }

    // Pass 29: the LOS tab has no list of units; opened with an empty From, it starts from the hex the view picked.
    [Fact]
    public void TheLosTabStartsFromThePickedHex()
    {
        var page = GameWithDefenders(("r1", "defender-squad"));
        page.ViewAs("german");
        page.Find("#play-units tr[data-unit='g1'] .play-locate").Click();
        page.Find("#play-inspector-tab-los").Click();
        Assert.Contains("A1", page.Find("#los-source").GetAttribute("value"), StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#los-panel select"));
    }

    [Fact]
    public void PlacedBoardsAreRecordedAndTheMapNeedsTheVaslCheckout()
    {
        // One board placed in slot (0, 0), turned (a card lists a single unturned board as a plain board; ruling R22.4): the game records the placement;
        // drawing a placed map needs the VASL checkout.
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = $"{Board}@0,0/r" });
        page.Find("#place-id").Change("g1");
        page.Find("#place-location").Change($"{Board}:A1:0");
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        var map = live.Planner.Replay(live.Store.Read(new GameScope(LivePlay.Tenant, "village"))!.Events).Current!.Map;
        Assert.True(map.IsPlaced);
        Assert.Equal($"{Board}@0,0/r", map.Reference);
        Assert.NotEmpty(page.FindAll("#play-map-problems li"));
        Assert.NotEmpty(page.FindAll("#play-units tr[data-unit='g1']"));
    }

    [Fact]
    public void APendingDeclarationIsShownAndBlocksThePhase()
    {
        var page = GameWithDefenders(("l1", "defender-leader"));
        Append(("entry-attempted", new EntryAttempted("g1", BoardLocation.Parse($"{Board}:B1:0"), 2), null),
            ("conditions-changed", new ConditionsChanged("l1", Revealed), null));
        page.OpenGame("village");
        Assert.Contains("awaits the attacker's Infantry OVR declaration", page.Find("[data-open-attempt='enter-1-1']").TextContent, StringComparison.Ordinal);

        page.Find("#propose-advance").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains(page.FindAll("#play-reasons li"), item => item.TextContent.StartsWith("play.declaration-pending", StringComparison.Ordinal));

        // The synthetic board is not the pinned board 01, so an election and a decline are both refused here, with that
        // reason, and nothing changes.
        var revision = live.Store.Read(new GameScope(LivePlay.Tenant, "village"))!.Events.Count;
        page.Find(".declare-elect").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains(page.FindAll("#play-reasons li"), item => item.TextContent.StartsWith("play.outside-reviewed-board", StringComparison.Ordinal));
        page.Find(".declare-decline").Click();
        page.WaitForAssertion(() => Assert.Contains(page.FindAll("#play-reasons li"), item => item.TextContent.StartsWith("play.outside-reviewed-board", StringComparison.Ordinal)));
        Assert.Equal(revision, live.Store.Read(new GameScope(LivePlay.Tenant, "village"))!.Events.Count);
    }

    [Fact]
    public void ALiveGameIsListedAndReplaysInTheGameLibrary()
    {
        StartGame($"{Board}:A1:0");
        Assert.Contains(GameLibrary.LivePrefix + "village", games.Names);
        var entry = games.Load(GameLibrary.LivePrefix + "village");
        Assert.EndsWith("(live)", entry.Label, StringComparison.Ordinal);
        Assert.False(entry.History!.HasErrors);
        Assert.True(entry.PositionsChecked);
        Assert.Equal(LiveGames.Source, entry.History.Current!.Source);

        // The live game is refused where it is not the live source, so no other reader can pass it off as its own.
        var record = live.Store.Read(new GameScope(LivePlay.Tenant, "village"))!;
        Assert.Contains(GameProjector.Project(record.Events, library.Vocabulary, live.Catalogs).Diagnostics,
            diagnostic => diagnostic.Code == "UNIT-STATE-017");
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
