using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 60, the UI review's page tests: the AFV a FT names, a Thrown DC's and a Placed DC's AFV in the view's words, the MG offered in the Gun panel
/// only when its side sees an enemy AFV, and the records of a named AFV and of Random SW/Gun Destruction; with the words FireText gives them. Each
/// game is set up through the game's own gate on the wide board and then opened on the page.
/// </summary>
public sealed class PlayPagePass60Tests : IDisposable
{
    private static readonly string[] G1 = ["g1"];

    private const string Game = "pass60";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-60-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass60Tests()
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

    private static string At(int column) => WideBoards.At(column);

    private static Dictionary<string, object> Unit(string id, string kind, string definition, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = kind,
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new
        {
            at
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = false, ["asl:hidden"] = false },
    };

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = "west" },
        ["conditions"] = new Dictionary<string, bool> { ["asl:bu"] = false },
    };

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

    private Task Advance() => Do(GameActions.AdvancePhase, new
    {
    });

    /// <summary>A game named after this class on the wide board, in July 1943, gone on to the first side's PFPh.</summary>
    private async Task Start(string first, params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(PlayPagePass60Tests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = first,
                scenarioMonth = 7,
                scenarioYear = 1943,
                sides = new object[] { new { id = "german", nationality = "german", elr = 3 }, new { id = "russian", nationality = "russian", elr = 3 } },
            },
            placements,
        });
        await Advance();
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal),
            page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    [Fact]
    public async Task AFtNamesTheAfvInTheViewsWordsAndItsRecordShowsTheAttackOnIt()
    {
        // A22.34 (ruling R60.15): the FT's attack offers the PzKpfw IIIH by its name, not its identifier; the commit carries it, and the record lists
        // the To Kill DR at it.
        await Start("russian", Unit("r1", "asl:squad", "defender-squad", At(5), "russian"), Sw("ru-ft", "asl:ft", "defender-ft", "r1", "russian"),
            Vehicle("de-tank", "attacker-tank", At(6), "german"));
        var page = Open("russian");
        page.Find("#fire-from").Change(At(5));
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-weapon[data-weapon='ru-ft']").Change(true);
        page.Find("#fire-target").Change(At(6));
        var offered = page.FindAll("#fire-vehicle option").Single(item => item.GetAttribute("value") == "de-tank");
        Assert.NotEqual("de-tank (attacker-tank)", offered.TextContent.Trim());
        page.Find("#fire-vehicle").Change("de-tank");
        dice.Enqueue([5, 6, 2, 2, 2, 2]);
        Commit(page, "#propose-fire");
        var fire = Events.Select(item => item.Payload).OfType<FireResolved>().Last();
        Assert.Equal("de-tank", fire.Facts.GetProperty("targetVehicle").GetProperty("vehicleId").GetString());
        Assert.Contains("To Kill DR at", string.Join(" ", page.FindAll(".fire-named-vehicle li").Select(item => item.TextContent)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AThrownDcOffersTheAfvInTheViewsWords()
    {
        // A23.5 (ruling R60.16; the UI review): the Throw's AFV is said as the other panels say it.
        await Start("russian", Unit("r1", "asl:squad", "defender-squad", At(5), "russian"), Sw("ru-dc", "asl:dc", "defender-dc", "r1", "russian"),
            Vehicle("de-tank", "attacker-tank", At(6), "german"));
        var page = Open("russian");
        page.Find("#throw-dc").Change("r1|ru-dc");
        page.Find("#throw-dc-at").Change(At(6));
        var offered = page.FindAll("#throw-dc-vehicle option").Single(item => item.GetAttribute("value") == "de-tank");
        Assert.NotEqual("de-tank (attacker-tank)", offered.TextContent.Trim());
    }

    [Fact]
    public async Task APlacedDcOffersItsDetonationAtTheAfvInTheViewsWords()
    {
        // A23.3, A23.5: the German squad passes its PAATC (DR 4) and Places its DC where the T-34 is; in the AFPh the page offers the detonation at it,
        // its charge, Location, and AFV in the view's words.
        await Start("german", Unit("g1", "asl:squad", "attacker-squad", At(5), "german"), Sw("dc", "asl:dc", "attacker-dc", "g1", "german"),
            Vehicle("ru-tank", "defender-tank", At(6), "russian"));
        await Advance();
        dice.Enqueue([2, 2]);
        await Do(GameActions.Move, new
        {
            unitIds = G1,
            to = At(5),
            placeDc = "dc",
            placeDcAt = At(6),
        });
        await Do(GameActions.PassFire, new
        {
        });
        await Do(GameActions.EndMove, new
        {
        });
        await Advance();
        await Advance();
        var page = Open("german");
        var detonate = page.Find(".detonate-dc-vehicle[data-dc='dc'][data-vehicle='ru-tank']");
        Assert.DoesNotContain("ru-tank (defender-tank)", detonate.TextContent, StringComparison.Ordinal);
        Assert.Contains("a Position DR first", detonate.TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AMgIsOfferedAsOrdnanceOnlyWhenItsSideSeesAnEnemyAfv()
    {
        // A9.61 (the UI review): a truck is no AFV, so the German LMG is not offered in the Gun panel.
        await Start("german", Unit("g1", "asl:squad", "attacker-squad", At(5), "german"), Sw("de-lmg", "asl:mg", "attacker-lmg", "g1", "german"),
            Vehicle("ru-truck", "defender-truck", At(6), "russian"));
        var page = Open("german");
        Assert.DoesNotContain("de-lmg", page.FindAll("#ordnance-gun option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public async Task AMgIsOfferedAsOrdnanceWhenItsSideSeesAnEnemyAfvAndWaitsForItsName()
    {
        // A9.61: with the T-34 in view the LMG is offered, at an AFV only; with a target Location and no AFV named the shot is not ready.
        await Start("german", Unit("g1", "asl:squad", "attacker-squad", At(5), "german"), Sw("de-lmg", "asl:mg", "attacker-lmg", "g1", "german"),
            Vehicle("ru-tank", "defender-tank", At(6), "russian"));
        var page = Open("german");
        var offered = page.FindAll("#ordnance-gun option").Single(item => item.GetAttribute("value") == "de-lmg");
        Assert.Contains("at an AFV only (A9.61)", offered.TextContent, StringComparison.Ordinal);
        page.Find("#ordnance-gun").Change("de-lmg");
        page.Find("#ordnance-target").Change(At(6));
        Assert.True(page.Find("#propose-ordnance").HasAttribute("disabled"));
        Assert.Contains("Name the AFV (A9.61).", page.Find("#propose-ordnance-ready").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AKiaOnASquadHoldingALmgShowsTheLmgsFateInItsRecord()
    {
        // A9.74 (ruling R60.17): two Russian squads at 16 FP (PBF), IFT DR 3: 1KIA; the LMG's dr of 3 on the same column destroys it.
        await Start("russian", Unit("r1", "asl:squad", "defender-squad", At(5), "russian"), Unit("r2", "asl:squad", "defender-squad", At(5), "russian"),
            Unit("g1", "asl:squad", "attacker-squad", At(6), "german"), Sw("de-lmg", "asl:mg", "attacker-lmg", "g1", "german"));
        var page = Open("russian");
        page.Find("#fire-from").Change(At(5));
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-target").Change(At(6));
        dice.Enqueue([1, 2, 3, 3]);
        Commit(page, "#propose-fire");
        Assert.Contains("Random SW/Gun Destruction dr 3", string.Join(" ", page.FindAll(".fire-weapon-destruction li").Select(item => item.TextContent)), StringComparison.Ordinal);
    }

    [Fact]
    public void ANamedAfvsLinesSayTheCollateralDrmAndAPoorDcAtABuCrew()
    {
        // D.8A: the Specific Collateral Attack's DRM are the recorded ones; C7.346: a poorly positioned DC at a BU AFV has no effect at all.
        var collateral = new OrdnanceCollateral(24, "1MC", new FireVehicleEffect("de-tank", "attacker-tank", FireVehicleEffect.None, null,
            [new FireModifier("crew-exposed", 2, "D5.31")], 9, null, null, FireVehicleEffect.None));
        var resolution = new FireResolution(FireResolution.Resolved, [], null, [], [], null, [])
        {
            SpecificCollateral = collateral
        };
        Assert.Contains("a Specific Collateral Attack of 24 FP, CE +2 (D.8A): Final DR 9", FireText.NamedVehicleLines("de-tank", resolution).Single(), StringComparison.Ordinal);
        var poor = new FireResolution(FireResolution.Resolved, [], null, [], [], null, [])
        {
            Position = new FirePosition([5, 5], 10, [], 10, "hull", FirePosition.Poor)
        };
        Assert.Contains("no effect, as its crew is BU", FireText.NamedVehicleLines("de-tank", poor, crewExposed: false).Single(), StringComparison.Ordinal);
        Assert.Contains("its CE crew alone is attacked", FireText.NamedVehicleLines("de-tank", poor).Single(), StringComparison.Ordinal);
    }

    [Fact]
    public void TheWeaponDestructionLinesSayTheDrOrTheDcsResult()
    {
        // A9.74: a SW's dr on the attack's column; C11.6: a DC's result on the Gun its target manned.
        string[] lines = [.. FireText.WeaponDestructionLines([new FireWeaponDestruction("de-lmg", "g1", "1KIA", ScenarioA1GunDestruction.Destroyed, "A9.74") { Dr = 3 },
            new FireWeaponDestruction("ru-gun", "ru-crew", "K/4", ScenarioA1GunDestruction.Malfunctioned, "C11.6")])];
        Assert.StartsWith("de-lmg, held by g1: Random SW/Gun Destruction dr 3 on the same column", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("destroyed (A9.74)", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("ru-gun, the Gun ru-crew manned: the DC's", lines[1], StringComparison.Ordinal);
        Assert.EndsWith("malfunctioned (C11.6)", lines[1], StringComparison.Ordinal);
        Assert.Empty(FireText.WeaponDestructionLines(null));
    }
}
