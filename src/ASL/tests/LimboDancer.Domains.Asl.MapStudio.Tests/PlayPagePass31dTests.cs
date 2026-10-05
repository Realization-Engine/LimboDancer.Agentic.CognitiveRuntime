using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Rendering.Tests;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// A page-test board with room (pass 31d, design D13): 12 by 6 hexes of Open Ground painted in code as the synthetic board is, with woods in one
/// hex. The 3 by 2 board has three hexes that take units, so a range beyond a squad's reach, a rout, and a route could not be set on it.
/// </summary>
internal sealed class WideBoards(MapService maps) : IBoardProvider
{
    /// <summary>The hex of the woods, in the board's fourth row.</summary>
    public static readonly HexIndex Woods = new(7, 3);

    public static readonly StudioBoard Board = FakeBoardProvider.Board with
    {
        Ref = BoardRef.Parse("ab-wide"),
        Title = "Synthetic 12 by 6 board",
        Render = Input(),
    };

    public string? SourceDescription => "Synthetic test source";

    public string? CatalogBlob => null;

    public IReadOnlyList<BoardListing> List() => [new BoardListing(Board.Ref, Board.Title)];

    public BoardLoadResult Load(BoardRef board) =>
        board == Board.Ref ? new BoardLoadResult(Board, [])
        : board.Kind == BoardRefKind.ComposedMap ? maps.Load(board)
        : new BoardLoadResult(null, []);

    public BoardLoadResult? Cached(BoardRef board) => Load(board);

    /// <summary>A hex of the board's fourth row, as a ground-level Location.</summary>
    public static string At(int column) => GameMaps.LocationOf(Board, new HexIndex(column, 3))!.ToString();

    private static BoardRenderInput Input()
    {
        var geometry = BoardGeometry.Standard(12, 6);
        var codes = new byte[geometry.GridWidth * geometry.GridHeight];
        var center = geometry.CenterPoint(Woods);
        for (var x = 0; x < geometry.GridWidth; x++)
        {
            for (var y = 0; y < geometry.GridHeight; y++)
            {
                if (((x - center.X) * (x - center.X)) + ((y - center.Y) * (y - center.Y)) < 400)
                {
                    codes[(x * geometry.GridHeight) + y] = 60;
                }
            }
        }

        var grid = new TerrainGrid(geometry, codes, new sbyte[codes.Length], new bool[geometry.HexCount]);
        var facts = VaslCompatibleHexFactDerivation.Derive(grid, SyntheticBoard.Catalog, HexsideAnnotations.None);
        return BoardRenderInput.Create(BoardRef.Parse("ab-wide"), "Synthetic 12 by 6 board", grid, SyntheticBoard.Catalog, facts);
    }
}

/// <summary>
/// Pass 31d on the Play page (design section 9), on a board with room: a target out of every ticked firer's range and its refusal (pass 31c's
/// tests not written); the Rout panel's load line, its way to the woods, and the consequence of a laden rout (designs D2, D3); a FT that fires
/// alone and a weapon that may not be ticked (design D10). Each game is set up through the game's own gate and then opened on the page.
/// </summary>
public sealed class PlayPagePass31dTests : IDisposable
{
    private const string Game = "wide";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-31d-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass31dTests()
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

    private GameState Current => live.History(Game)!.Current!;

    private static string Said(string location) => DisplayText.Place(1, location);

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side, params string[] states)
    {
        var conditions = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false };
        foreach (var state in states)
        {
            conditions[state] = true;
        }

        return new()
        {
            ["id"] = id,
            ["kind"] = kind,
            ["definition"] = definition,
            ["side"] = side,
            ["position"] = new
            {
                at
            },
            ["conditions"] = conditions,
        };
    }

    private static Dictionary<string, object> Sw(string id, string kind, string definition, string holder, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    private async Task Do(LimboDancer.Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        var revision = live.Store.Read(new GameScope(LivePlay.Tenant, Game))?.Events.Count ?? 0;
        node["gameId"] = Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{revision}";
        node["expectedRevision"] = revision;
        var element = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, element, live.Principal);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(action, element, live.Principal, proposed.Correlation) : proposed;
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
    }

    /// <summary>A game on the wide board with the German side moving first, advanced a number of phases from its Rally Phase.</summary>
    private async Task Start(int advances, params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = "Wide",
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 3 } },
            },
            placements,
        });
        for (var index = 0; index < advances; index++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    private static string[] Texts(IRenderedComponent<PlayPage> page, string selector) => [.. page.FindAll(selector).Select(item => item.TextContent.Trim())];

    [Fact]
    public async Task ATargetBeyondEveryTickedFirersRangeIsSaidAndCannotBeChosenAndItsRefusalNamesTheRange()
    {
        // Pass 31c's row: a German 4-6-7 (Normal Range 6, so it fires to 12)... the board is 12 hexes wide, so the firer is a Russian 4-4-7
        // (Normal Range 4, firing to 8) nine hexes from one German squad and two from another.
        await Start(9, Unit("r1", "asl:squad", "defender-squad", WideBoards.At(1), "russian"), Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(3), "german"),
            Unit("g2", "asl:squad", "attacker-squad", WideBoards.At(10), "german"));
        Assert.Equal(("pfph", "russian"), (Current.Phase, Current.PhasingSide));
        var page = Open("russian");
        page.Find("#fire-from").Change(WideBoards.At(1));
        page.Find(".fire-firer[data-unit='r1']").Change(true);

        // Nearest first; the far Location is said out of range and may not be chosen.
        var options = page.FindAll("#fire-target option").Where(option => option.GetAttribute("value") is { Length: > 0 }).ToArray();
        Assert.Equal([WideBoards.At(3), WideBoards.At(10)], options.Select(option => option.GetAttribute("value")));
        Assert.Equal($"{Said(WideBoards.At(3))}: 2 hexes, Normal Range", options[0].TextContent.Trim());
        Assert.Contains("9 hexes", options[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("out of range", options[1].TextContent, StringComparison.Ordinal);
        Assert.False(options[0].HasAttribute("disabled"));
        Assert.True(options[1].HasAttribute("disabled"));

        // Typed as any Location, the far target is proposed and refused, and the refusal names the firer, its range, and how far it fires.
        page.Find("#fire-free-target").Change(WideBoards.At(10));
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var refusal = page.Find("#play-proposal").TextContent;
        Assert.Contains("4-4-7 squad R1", refusal, StringComparison.Ordinal);
        Assert.Contains("is 9 hexes from", refusal, StringComparison.Ordinal);
        Assert.Contains("its Normal Range is 4, so it fires to 8", refusal, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheRoutPanelSaysWhatALadenUnitLeavesOffersAWayAndTheReviewSaysWhatIsLeft()
    {
        // The German RtPh: the broken g1 with a FT (5 PP) and a DC (2 PP) in the open two hexes from the Russian r9 (ADJACENT, it would surrender, A20.21), with woods two hexes away across Open Ground.
        await Start(5, Unit("r9", "asl:squad", "defender-squad", WideBoards.At(3), "russian"), Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german", "asl:broken"),
            Sw("gf", "asl:ft", "attacker-ft", "g1", "german"), Sw("gd", "asl:dc", "attacker-dc", "g1", "german"));
        Assert.Equal("rtph", Current.Phase);
        var page = Open("german");

        // The status gives the place the rout must end in with the hex on the way, and offers the way as a route.
        var woods = WideBoards.At(7);
        var way = page.Find(".use-route");
        Assert.Equal($"Use the route to {Said(woods)}", way.TextContent.Trim());
        var route = way.GetAttribute("data-route")!.Split(", ");
        Assert.Equal(2, route.Length);
        Assert.Equal(woods, route[1]);
        var owes = page.Find("#rout-obligations li").TextContent;
        Assert.Contains($"Its route must end in {Said(woods)} (by {Said(route[0])})", owes, StringComparison.Ordinal);

        // No load is said until the unit is chosen; the way's button chooses it and fills its route.
        Assert.Empty(page.FindAll("#rout-load"));
        way.Click();
        Assert.Equal("g1", page.Find("#rout-unit").GetAttribute("value"));
        Assert.Equal($"{Said(route[0])}, {Said(woods)}", page.Find("#rout-route").GetAttribute("value"));
        var load = page.Find("#rout-load").TextContent;
        Assert.Contains("carries 7 PP and may rout with 3 PP (A10.4)", load, StringComparison.Ordinal);
        Assert.Contains($"It leaves the FT (5 PP) in {Said(WideBoards.At(5))} and keeps the DC (2 PP)", load, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#rout-keep"));

        // The review says what is left before Confirm, and the button names it.
        page.Find("#propose-rout").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        Assert.Contains("leaves the FT (5 PP)", page.Find("#play-consequences").TextContent, StringComparison.Ordinal);
        Assert.Equal("Confirm, leaving SW behind", page.Find("#play-confirm").TextContent.Trim());

        // Confirmed (the Interdiction NMC in the Open Ground hex passes on 3, 3): the FT lies where the rout began, and the DC went with the squad.
        dice.Enqueue([3, 3]);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Null(((EquipmentInstance)Current.Find("gf")!).Holding);
        Assert.Equal(WideBoards.At(5), Current.Location("gf")!.Location.ToString());
        Assert.Equal("g1", ((EquipmentInstance)Current.Find("gd")!).Holding!.Holder);
        Assert.Equal(woods, Current.Location("g1")!.Location.ToString());
    }

    [Fact]
    public async Task AFtIsSaidToFireAloneAndHoldsProposeWhenTickedWithAnotherUnit()
    {
        // The German PFPh: g1 with a FT, and g2, in one hex; a Russian squad next to them.
        await Start(1, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german"), Unit("g2", "asl:squad", "attacker-squad", WideBoards.At(5), "german"),
            Sw("gf", "asl:ft", "attacker-ft", "g1", "german"), Unit("r1", "asl:squad", "defender-squad", WideBoards.At(6), "russian"));
        Assert.Equal("pfph", Current.Phase);
        var page = Open("german");
        page.Find("#fire-from").Change(WideBoards.At(5));
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        page.Find(".fire-firer[data-unit='g2']").Change(true);
        page.Find("#fire-target").Change(WideBoards.At(6));

        // The FT's row says it fires alone, and the "only its MG fires" box is not offered for it.
        Assert.Equal(["the FT of 4-6-7 squad G1: fires alone, without its holder's own FP (A22.31)"], Texts(page, "label:has(.fire-weapon)"));
        Assert.Empty(page.FindAll(".fire-alone"));
        Assert.Empty(page.FindAll("#fire-ft-alone"));
        Assert.False(page.Find("#propose-fire").HasAttribute("disabled"));

        // Ticked with the two squads, Propose waits and says why; with its holder alone it is free again.
        page.Find(".fire-weapon[data-weapon='gf']").Change(true);
        Assert.Contains("A FT fires alone", page.Find("#fire-ft-alone").TextContent, StringComparison.Ordinal);
        Assert.True(page.Find("#propose-fire").HasAttribute("disabled"));
        page.Find(".fire-firer[data-unit='g2']").Change(false);
        Assert.Empty(page.FindAll("#fire-ft-alone"));
        Assert.False(page.Find("#propose-fire").HasAttribute("disabled"));
    }

    [Fact]
    public async Task AWeaponThatHasMalfunctionedIsListedAndCannotBeTicked()
    {
        var broken = Sw("gm", "asl:mg", "attacker-mmg", "g1", "german");
        broken["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = true };
        await Start(1, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german"), broken, Unit("r1", "asl:squad", "defender-squad", WideBoards.At(6), "russian"));
        var page = Open("german");
        page.Find("#fire-from").Change(WideBoards.At(5));
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        Assert.Equal(["the MMG of 4-6-7 squad G1: has malfunctioned"], Texts(page, "label:has(.fire-weapon)"));
        Assert.True(page.Find(".fire-weapon[data-weapon='gm']").HasAttribute("disabled"));
        Assert.Empty(page.FindAll(".fire-alone"));
    }
}
