using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 70, Infantry III, on the Play page. Increment 3a: the Right of Inspection (A12.16; ruling R70.18). A German stack beyond the wide board's hill
/// from the only Russian unit shows the Russian its top counter and a count, which the unit table names for what it is; the German sees it in full.
/// </summary>
public sealed class PlayPagePass70Tests : IDisposable
{
    // Its own id: UnitNames keeps names by the game's id and first event id.
    private const string Game = "playpage-pass70";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-70-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass70Tests()
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

    private static Dictionary<string, object> Squad(string id, string definition, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:squad",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    private IReadOnlyList<GameEvent> Events => live.Store.Read(new GameScope(LivePlay.Tenant, Game))?.Events ?? [];

    private async Task Do(LimboDancer.Abstractions.Actions.ActionDescriptor action, object arguments)
    {
        var node = JsonSerializer.SerializeToNode(arguments)!.AsObject();
        var revision = Events.Count;
        node["gameId"] = Game;
        node["attemptId"] = $"{action.Id.Value.Replace('.', '-')}-{revision}";
        node["expectedRevision"] = revision;
        var element = JsonSerializer.SerializeToElement(node);
        var proposed = await live.Play.ProposeAsync(action, element, live.Principal);
        var result = proposed.Outcome == PlayOutcome.NeedsConfirmation ? await live.Play.ConfirmAsync(action, element, live.Principal, proposed.Correlation) : proposed;
        Assert.True(result.Outcome == PlayOutcome.Committed, string.Join("; ", result.Reasons));
    }

    /// <summary>A game named after this class on the wide board, gone on to the German PFPh, where setup has closed.</summary>
    private async Task Start(params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(PlayPagePass70Tests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                scenarioYear = 1942,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 3 } },
            },
            placements,
        });
        await Do(GameActions.AdvancePhase, new
        {
        });
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    [Fact]
    public async Task AStackBeyondTheHillShowsTheRussianItsTopAndACountNamedForTheRule()
    {
        // A12.16 (p. 79; ruling R70.18): the hill between the Russian squad and the German stack blocks the only Russian LOS to it, so the Russian's unit
        // table lists de-1, the top, and a row for de-2 that says why it is not shown; the German's lists both.
        await Start(Squad("de-1", "attacker-squad", WideBoards.BesideHill(11), "german"), Squad("de-2", "attacker-squad", WideBoards.BesideHill(11), "german"),
            Squad("ru-1", "defender-squad", WideBoards.BesideHill(7), "russian"));
        var russian = Open("russian");
        Assert.NotEmpty(russian.FindAll("tr[data-unit='de-1']"));
        Assert.Empty(russian.FindAll("tr[data-unit='de-2']"));
        var row = Assert.Single(russian.FindAll("tr[data-sealed]"));
        Assert.Equal("not inspectable: out of the LOS of this side's Good Order units (A12.16)", row.QuerySelector("td:nth-child(3)")!.TextContent);

        var german = Open("german");
        Assert.NotEmpty(german.FindAll("tr[data-unit='de-2']"));
        Assert.Empty(german.FindAll("tr[data-sealed]"));
    }
}
