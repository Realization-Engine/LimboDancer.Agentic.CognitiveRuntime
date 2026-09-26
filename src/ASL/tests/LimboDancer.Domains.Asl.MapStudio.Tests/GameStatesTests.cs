using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Pages;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Units.Rendering;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>The Game states page and the game library (State Model Design, section 10).</summary>
public sealed class GameStatesTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-games-" + Guid.NewGuid().ToString("N"));
    private readonly UnitLibrary library;
    private readonly GameLibrary games;
    private readonly MapService maps;
    private readonly BunitContext context = new();

    public GameStatesTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        maps = new MapService(options, new FakeVaslMapSource());
        library = new UnitLibrary(options);
        games = new GameLibrary(library, new SyntheticBoards(maps));
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(games);
    }

    [Fact]
    public void TheFixtureReplaysAndSaysWhenBoardsCannotBeChecked()
    {
        var entry = games.Load("a1-village.synthetic");
        Assert.False(entry.History!.HasErrors);
        Assert.Equal(23, entry.History.States.Count);

        // The synthetic board provider has no bd01, so positions cannot be checked, and the entry says so.
        Assert.False(entry.PositionsChecked);
        Assert.Contains(entry.Diagnostics, diagnostic => diagnostic.Code == "UNIT-STATE-020");
    }

    [Fact]
    public void TheAdjudicatorSeesEverything()
    {
        var page = context.Render<Games>();
        Assert.Contains("Revision 23 of 23", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#game-units tr[data-unit='r2']"));
        Assert.Contains("prisoner of g1", page.Find("#game-units tr[data-unit='r1']").TextContent, StringComparison.Ordinal);
        Assert.Contains("from g2", page.Find("#game-units tr[data-unit='g2-hs']").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ASideSeesOnlyWhatItMayKnow()
    {
        var page = context.Render<Games>();
        page.Find("#game-perspective").Change("german");
        page.Find("#game-revision").Change("8");
        Assert.Contains("Revision 8 of 23", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#game-units tr[data-unit='r1']"));
        Assert.Empty(page.FindAll("#game-units tr[data-unit='r2']"));
        Assert.Contains("withheld", page.Find("#game-units tr[data-unit='sealed-1']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r1", page.Find("#game-events").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("r2", page.Markup, StringComparison.Ordinal);

        page.Find("#game-next").Click();
        page.Find("#game-next").Click();
        Assert.Contains("Revision 10 of 23", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll("#game-units tr[data-unit='r1']"));
    }

    [Fact]
    public void TheRussianSideSeesOnlyWhatItMayKnow()
    {
        var page = context.Render<Games>();
        page.Find("#game-perspective").Change("russian");
        Assert.Contains("Revision 23 of 23", page.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#game-units tr[data-unit='g3']"));
        Assert.Empty(page.FindAll("#game-units tr[data-unit='gh2']"));
        Assert.Contains("concealed german presence", page.Find("#game-units tr[data-unit='sealed-1']").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("gh2", page.Markup, StringComparison.Ordinal);

        var projection = games.Projection(games.Load("a1-village.synthetic"), Perspective.Side("russian"), 23);
        Assert.DoesNotContain(projection.Set.Units, unit => unit.Id is "g3" or "gh2");
        Assert.Contains(projection.Set.Units, unit => unit.Id == "sealed-1" && unit.Concealed && unit.Location == "bd01:C5:0");
    }

    [Fact]
    public void AProjectionIsWhatThePerspectiveMayKnow()
    {
        var projection = games.Projection(games.Load("a1-village.synthetic"), Perspective.Side("german"), 8);
        Assert.True(projection.Set.Synthetic);
        Assert.Equal(["f1", "g1", "g2", "gh1", "sealed-1"], projection.Set.Units.Select(unit => unit.Id).Order(StringComparer.Ordinal));

        // The possessed LMG is drawn attached to its holder, and nothing names the Russian units.
        Assert.Equal(["g-lmg"], projection.Set.Units.Single(unit => unit.Id == "g1").Attached.Select(item => item.Id));
        Assert.DoesNotContain(projection.Set.Units, unit => unit.Id is "r1" or "r2");
        Assert.DoesNotContain(library.Sets(), entry => entry.Set.SetId == projection.Set.SetId);
        Assert.Equal("boards/bd01?game=a1-village.synthetic&perspective=german&revision=8",
            Games.BoardLink("bd01", "a1-village.synthetic", Perspective.Side("german"), 8));
    }

    [Fact]
    public void AGameOnPlacedBoardsShowsOnTheirComposedMap()
    {
        Directory.CreateDirectory(Path.Combine(root, "boards", "units", "games"));
        File.WriteAllText(Path.Combine(root, "boards", "units", "games", "map-game.game.json"), GameOn(["bd02", "bd03"], "bd03:B1:0", "bd02:A1:0"));
        var map = maps.Save("Game map", [new BoardPlacement(BoardRef.Parse("bd02")), new BoardPlacement(BoardRef.Parse("bd03"), 1, 0)]).Ref!;
        var board = maps.Load(map).Board!;
        var game = Assert.Single(games.GamesFor(board));
        Assert.Equal("map-game", game.Name);
        var projection = games.Projection(game, Perspective.Adjudicator, game.History!.States.Count);
        var overlay = library.Overlay(board, projection.Set, library.Renderer(UnitStyles.Classic)!);
        var attacker = overlay.Units.Single(unit => unit.Document.Id == "a1");
        Assert.Equal(board.Composition!.Map.Locate(BoardRef.Parse("bd03"), HexName.Parse("B1")), attacker.Hex);
        Assert.Equal("bd03:B1:0", attacker.Document.Location);
    }

    /// <summary>A small synthetic game: a German squad and a hidden Russian leader on the given boards.</summary>
    internal static string GameOn(string[] boards, string attackerAt, string leaderAt, string version = "v1") => $$"""
        {
          "schemaVersion": 1, "tenant": "3f6a9c1e-0000-4000-8000-00000000b202", "game": "test-game", "label": "Test game", "synthetic": true,
          "events": [
            { "eventId": "e1", "revision": 1, "time": "2026-09-26T09:00:00Z", "source": "test", "type": "game-started",
              "payload": { "sides": [ { "id": "german", "nationality": "german" }, { "id": "russian", "nationality": "russian" } ],
                "map": { "reference": "test-map", "version": "v1", "boards": [ {{string.Join(", ", boards.Select(board => $$"""{ "board": "{{board}}", "version": "{{version}}" }"""))}} ] },
                "catalog": "asl-scenario-a1@1.1.0", "turn": 1, "phase": "rph", "phasingSide": "german", "synthetic": true } },
            { "eventId": "e2", "revision": 2, "time": "2026-09-26T09:00:01Z", "source": "test", "type": "instance-created",
              "payload": { "instance": { "id": "a1", "kind": "asl:squad", "definition": "attacker-squad", "side": "german", "position": { "at": "{{attackerAt}}" },
                "conditions": { "asl:broken": false, "asl:berserk": false, "asl:captured": false, "asl:melee": false } } } },
            { "eventId": "e3", "revision": 3, "time": "2026-09-26T09:00:02Z", "source": "test", "type": "instance-created", "visibility": ["russian"],
              "payload": { "instance": { "id": "hidden-leader", "kind": "asl:leader", "definition": "defender-leader", "side": "russian", "position": { "at": "{{leaderAt}}" },
                "conditions": { "asl:hidden": true } } } }
          ]
        }
        """;

    [Fact]
    public void ACaseReadSaysWhenTheBoardCannotBeRead()
    {
        // The synthetic board provider has no bd01, so the map read API cannot resolve the location.
        var page = context.Render<Games>();
        page.Find("#case-attacker").Change("gh1");
        page.Find("#case-location").Change("bd01:E5:0");
        page.Find("#case-read").Click();
        Assert.Contains("Unavailable (CASE-009)", page.Find("#case-result").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ACaseReadAtAnOldRevisionIsStale()
    {
        var page = context.Render<Games>();
        page.Find("#case-expected").Change("20");
        page.Find("#case-read").Click();
        Assert.Contains("Stale (CASE-005)", page.Find("#case-result").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void ASideCanOnlyReadItsOwnUnitsAsAttackers()
    {
        var page = context.Render<Games>();
        page.Find("#game-perspective").Change("german");
        Assert.DoesNotContain(page.FindAll("#case-attacker option"), option => option.GetAttribute("value") is "r1" or "r2");
        Assert.Contains(page.FindAll("#case-attacker option"), option => option.GetAttribute("value") == "gh1");
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
