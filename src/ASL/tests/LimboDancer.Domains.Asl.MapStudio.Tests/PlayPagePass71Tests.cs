using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 71, Infantry IV: Snipers and wounds, on the Play page. Increment 4a: the Sniper's record line names the hex attacked in brackets and the target
/// after it, "[hex]: ...", as the page's other lines do, with the new results worded and cited (design section 5, item 3).
/// </summary>
public sealed class PlayPagePass71Tests : IDisposable
{
    // Its own id: UnitNames keeps names by the game's id and first event id.
    private const string Game = "playpage-pass71";

    // CA1861: the German squad that fires in each test.
    private static readonly string[] GermanFirer = ["de-1"];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-71-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass71Tests()
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

    private static Dictionary<string, object> Placed(string id, string kind, string? definition, string at, string side, Dictionary<string, bool> conditions)
    {
        var placed = new Dictionary<string, object>
        {
            ["id"] = id,
            ["kind"] = kind,
            ["side"] = side,
            ["position"] = kind == "asl:vehicle" ? new Dictionary<string, string> { ["at"] = at, ["facing"] = "east" } : new Dictionary<string, string> { ["at"] = at },
            ["conditions"] = conditions,
        };
        if (definition is not null)
        {
            placed["definition"] = definition;
        }

        return placed;
    }

    private static Dictionary<string, object> Squad(string id, string definition, string at, string side) =>
        Placed(id, "asl:squad", definition, at, side, new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false });

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

    /// <summary>A game named after this class on the wide board, the Russian SAN 6, gone on to the German PFPh, where setup has closed.</summary>
    private async Task Start(params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(PlayPagePass71Tests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = "german",
                scenarioMonth = 7,
                scenarioYear = 1942,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 3, san = 6 } },
            },
            placements,
        });
        await Do(GameActions.AdvancePhase, new
        {
        });
    }

    [Fact]
    public async Task TheSniperLineNamesTheHexAndTheStunnedCrewsTank()
    {
        // A14.3, D5.34 (ruling R71.5): the German squad's IFT DR of 3 + 3 equals the Russian SAN; the Russian 4-4-7 passes its MC (3 + 3); the Russian Sniper
        // in the fifth row rolls dr 2 and a Random Location DR of 1, 1 (one hex north), onto the CE German tank in the fourth row, whose crew is Stunned.
        // Every view reads the line with the hex in brackets first and the tank after it.
        var tankAt = WideBoards.At(9);
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Placed("de-tk", "asl:vehicle", "attacker-tank", tankAt, "german", new Dictionary<string, bool> { [Conditions.ButtonedUp] = false }),
            Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        dice.Enqueue([3, 3, 3, 3, 2, 1, 1]);
        await Do(GameActions.Fire, new
        {
            firers = GermanFirer,
            target = WideBoards.At(5),
        });
        var sniped = Assert.Single(Events.Select(item => item.Payload).OfType<SniperAttacked>());
        Assert.Equal(("de-tk", "stunned", tankAt), (sniped.Unit, sniped.Result, sniped.Target?.ToString()));
        foreach (var view in new[] { Perspective.Adjudicator, new Perspective("german"), new Perspective("russian") })
        {
            var history = live.History(Game)!;
            var line = Assert.Single(new PlayRecords(history, view, long.MaxValue, UnitNames.For(history, view, [.. live.Catalogs])).Snipers).Text;
            Assert.StartsWith("The Russian Sniper (", line, StringComparison.Ordinal);
            Assert.Matches(@": dr 2; \[[A-Z]{1,2}\d{1,2}\]: the crew of .+ is Stunned \(A14\.3, D5\.34\)$", line);
            Assert.DoesNotContain("ab-wide:", line, StringComparison.Ordinal);
        }
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    private static Dictionary<string, object> Plain(string id, string kind, string definition, string at, string side) =>
        Placed(id, kind, definition, at, side, new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false });

    [Fact]
    public async Task TheSniperCheckIsTheGermanSidesChoiceAndItsLinesNameTheHex()
    {
        // Increment 4b (A14.4; rulings R71.9, R71.10): as above, the Russian Sniper's dr 2 lands one hex north, now on a German 8-1, 4-6-7, and 2-4-7;
        // Random Selection's 6, 1, 1 picks the HS, which is pinned. The German view offers the Sniper Check with its two answers, the Russian view none. Taken,
        // the DR 3 + 2 - 3 eliminates the Russian Sniper, and the lines say the hex in brackets and the Russian SAN of 5.
        var stackAt = WideBoards.At(9);
        var sniperAt = GameMaps.LocationOf(WideBoards.Board, new HexIndex(9, 4))!.ToString();
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"),
            Plain("de-l", "asl:leader", "attacker-leader-8-1", stackAt, "german"), Squad("de-s", "attacker-squad", stackAt, "german"),
            Plain("de-h", "asl:half-squad", "attacker-half-squad", stackAt, "german"), Placed("rs", "asl:sniper", null, sniperAt, "russian", new Dictionary<string, bool>()));
        dice.Enqueue([3, 3, 3, 3, 2, 1, 1, 6, 1, 1]);
        await Do(GameActions.Fire, new
        {
            firers = GermanFirer,
            target = WideBoards.At(5),
        });
        var choice = live.History(Game)!.Current!.Choice!;
        Assert.Equal((ChoicePending.SniperCheck, "german"), (choice.Kind, choice.Side));

        var german = Open("german");
        Assert.Equal(2, german.FindAll("#play-choice button.choose").Count);
        Assert.Contains("take the Sniper Check", german.Find("#play-choice").TextContent, StringComparison.Ordinal);
        Assert.Empty(Open("russian").FindAll("#play-choice button.choose"));

        dice.Enqueue([3, 2]);
        await Do(GameActions.Choose, new
        {
            key = choice.Key,
            option = ChoicePending.Take,
        });
        var history = live.History(Game)!;
        var view = new Perspective("german");
        var lines = new PlayRecords(history, view, long.MaxValue, UnitNames.For(history, view, [.. live.Catalogs])).Snipers.Select(item => item.Text).ToArray();
        Assert.Contains(lines, line => line.Contains("Sniper Check against the Russian Sniper", StringComparison.Ordinal) && line.Contains("= 2, the Sniper is eliminated", StringComparison.Ordinal)
            && line.Contains(" in [", StringComparison.Ordinal));
        Assert.Contains(lines, line => line.EndsWith("the Russian SAN is now 5 (A14.4)", StringComparison.Ordinal));
        Assert.DoesNotContain(lines, line => line.Contains("ab-wide:", StringComparison.Ordinal));
    }
}
