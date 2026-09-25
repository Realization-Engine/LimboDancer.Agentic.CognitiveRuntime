using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;
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
        boards = new BuildingBoards(new MapService(options, new FakeVaslMapSource()));
        live = new LivePlay(library, boards);
        games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
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
        page.Find("#new-board").Change(Board);
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
        Assert.Contains("Revision 2", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.Contains("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#play-units tr[data-unit='g1']"));
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
        Assert.Contains("Revision 4", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
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
