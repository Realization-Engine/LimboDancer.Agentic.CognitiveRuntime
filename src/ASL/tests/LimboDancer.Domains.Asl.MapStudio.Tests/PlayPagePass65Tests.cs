using System.Text.Json;
using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Rules;
using LimboDancer.Domains.Asl.Units.Catalog;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 65, the UI review's page tests (finding 10): one test for each section the pass added, the crew acts, Scrounging, the Rider and Carrier-crew
/// checkboxes, the rout beneath, and the capture attempt. Each checks the hex in its labels, the arguments it sends, and its draft cleared on a view change
/// and, where the control is still offered, on the commit. Each game is set up through the game's own gate on the wide board (its woods aside) and then
/// opened on the page; what the setup refuses (a unit beside an enemy vehicle) is written as the store writes it.
/// </summary>
public sealed class PlayPagePass65Tests : IDisposable
{
    private static readonly string[] PhasesToCloseCombat = ["pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];
    private static readonly string[] RussianRiders = ["ru-l", "ru-s"];
    private static readonly int[] BoxCars = [6, 6];
    private static readonly int[] FiveAndOne = [5, 1];

    // Its own id: UnitNames keeps names by the game's id and first event id.
    private const string Game = "playpage-pass65";
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-65-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass65Tests()
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

    // The board's fourth row; its column 7 is woods, which no test here uses.
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

    private static Dictionary<string, object> Vehicle(string id, string definition, string at, string side, Dictionary<string, bool>? conditions = null) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:vehicle",
        ["definition"] = definition,
        ["side"] = side,
        ["position"] = new Dictionary<string, string> { ["at"] = at, ["facing"] = "east" },
        ["conditions"] = conditions ?? new Dictionary<string, bool>(),
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

    /// <summary>A game named after this class on the wide board, in July of the year, left in the first side's RPh.</summary>
    private async Task Start(string first, string second, int year, params Dictionary<string, object>[] placements)
    {
        var catalog = live.Catalogs.First(item => item.Publication == CatalogPublication.Published);
        await Do(GameActions.Setup, new
        {
            start = new
            {
                label = nameof(PlayPagePass65Tests),
                catalog = $"{catalog.Identity.Catalog}@{catalog.Identity.Version}",
                boards = new[] { WideBoards.Board.Ref.Value },
                firstSide = first,
                scenarioMonth = 7,
                scenarioYear = year,
                sides = new object[] { new { id = first, nationality = first, elr = 3 }, new { id = second, nationality = second, elr = 3 } },
            },
            placements,
        });
    }

    /// <summary>Records written after the game's events as the store writes them, for what the setup refuses.</summary>
    private void Inject(params (string Type, EventPayload Payload)[] items)
    {
        var scope = new GameScope(LivePlay.Tenant, Game);
        var existing = Events;
        GameEvent[] events = [.. items.Select((item, index) => new GameEvent(scope, $"staged-{existing.Count + index + 1}", existing.Count + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, [], null))];
        Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Pass65", existing.Count, events, live.Planner.Replay).Status);
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    private static void Propose(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal),
            page.Find("#play-proposal").TextContent));
    }

    private static void Confirm(IRenderedComponent<PlayPage> page)
    {
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        Propose(page, propose);
        Confirm(page);
    }

    /// <summary>The adjudicator passes for the DEFENDER on a crew that left its vehicle (D5.43), and ends the move where the page still offers it.</summary>
    private static void CloseTheWindow(IRenderedComponent<PlayPage> page)
    {
        Commit(page, "#propose-pass");
        if (page.FindAll("#propose-end-move").Count > 0)
        {
            Commit(page, "#propose-end-move");
        }
    }

    /// <summary>The view shown again: away to the other side's and back, which clears every draft (pass 28b, section 15.3).</summary>
    private static void ViewAgain(IRenderedComponent<PlayPage> page, string view, string other)
    {
        page.ViewAs(other);
        page.ViewAs(view);
    }

    private T Last<T>() where T : EventPayload => Events.Select(item => item.Payload).OfType<T>().Last();

    [Fact]
    public async Task TheCrewActsSectionNamesTheHexSendsItsChoicesAndClearsThem()
    {
        // D5.41, D5.411, D5.42 (ruling R65.20): in the German MPh the Abandoned PzKpfw IIIH may be entered by the HS beside it, and the other two tanks'
        // crews may Abandon them. A view change clears the HS chosen and the self-destruction; the malfunction chosen goes with the Abandonment, whose
        // proposal names the crew it creates by its counter, and is not kept for the next.
        await Start("german", "russian", 1943, Vehicle("de-tank", "attacker-tank", At(3), "german", new()
        {
            ["asl:abandoned"] = true
        }),
            Unit("de-h", "asl:half-squad", "attacker-half-squad", At(3), "german"), Vehicle("de-tank2", "attacker-tank", At(9), "german"),
            Vehicle("de-tank3", "attacker-tank", At(10), "german"), Vehicle("de-tank4", "attacker-tank", At(5), "german"),
            Unit("ru-s", "asl:squad", "defender-squad", At(0), "russian"));
        await Advance();
        await Advance();
        var page = Open("german");

        Assert.Contains("[", page.Find(".vehicle-abandon[data-vehicle='de-tank2']").TextContent, StringComparison.Ordinal);
        Assert.StartsWith("[", page.Find(".recrew-unit[data-vehicle='de-tank'] option[value='de-h']").TextContent.Trim(), StringComparison.Ordinal);
        var recrew = page.Find(".vehicle-recrew[data-vehicle='de-tank']");
        Assert.True(recrew.HasAttribute("disabled"));
        Assert.Equal("Choose the unit that enters", recrew.GetAttribute("title"));

        page.Find(".recrew-unit[data-vehicle='de-tank']").Change("de-h");
        page.Find("#abandon-destroy").Change("wreck");
        Assert.False(page.Find(".vehicle-recrew[data-vehicle='de-tank']").HasAttribute("disabled"));
        ViewAgain(page, "german", "russian");
        Assert.Equal(string.Empty, page.Find(".recrew-unit[data-vehicle='de-tank']").GetAttribute("value") ?? string.Empty);
        Assert.Equal(string.Empty, page.Find("#abandon-destroy").GetAttribute("value") ?? string.Empty);

        page.Find("#abandon-destroy").Change("malfunction");
        Propose(page, ".vehicle-abandon[data-vehicle='de-tank2']");
        Assert.DoesNotContain("-de-tank2-crew", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Confirm(page);
        var abandoned = Last<VehicleAbandoned>();
        Assert.Equal(("de-tank2", "malfunction"), (abandoned.Vehicle, abandoned.Destroyed));

        // From the adjudicator's view, which then stays, the DEFENDER passes on the crew and the move ends; the third tank's crew leaves it a wreck, the
        // window closes again, and the fourth tank is offered with no self-destruction chosen: the commit cleared it, with no view change between.
        page.ViewAs(Perspective.AdjudicatorName);
        CloseTheWindow(page);
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".vehicle-abandon[data-vehicle='de-tank3']")));
        page.Find("#abandon-destroy").Change("wreck");
        Commit(page, ".vehicle-abandon[data-vehicle='de-tank3']");
        Assert.Equal(("de-tank3", "wreck"), (Last<VehicleAbandoned>().Vehicle, Last<VehicleAbandoned>().Destroyed));
        CloseTheWindow(page);
        Assert.Equal(Perspective.AdjudicatorName, page.Find("#play-perspective").GetAttribute("value"));
        page.WaitForAssertion(() => Assert.NotEmpty(page.FindAll(".vehicle-abandon[data-vehicle='de-tank4']")));
        Assert.Equal(string.Empty, page.Find("#abandon-destroy").GetAttribute("value") ?? string.Empty);
    }

    [Fact]
    public async Task TheScroungingSectionNamesTheHexSendsTheUnitAndClearsItsPick()
    {
        // D10.5 (ruling R65.23): in the Russian RPh the squad and the HS stand with two German wrecks. A view change clears the units chosen; the squad's
        // Scrounging of the first wreck sends both ids, and only that wreck's pick goes with it.
        await Start("russian", "german", 1943, Vehicle("de-tank", "attacker-tank", At(3), "german"), Vehicle("de-tank2", "attacker-tank", At(3), "german"),
            Unit("ru-s", "asl:squad", "defender-squad", At(0), "russian"), Unit("ru-h", "asl:half-squad", "defender-half-squad", At(0), "russian"),
            Unit("de-1", "asl:squad", "attacker-squad", At(11), "german"));
        var location = new MapPosition(BoardLocation.Parse(At(3)));
        Inject(("vehicle-wrecked", new VehicleWrecked("de-tank", false)), ("vehicle-wrecked", new VehicleWrecked("de-tank2", false)),
            ("instance-moved", new InstanceMoved("ru-s", location)), ("instance-moved", new InstanceMoved("ru-h", location)));
        var page = Open("russian");

        Assert.StartsWith("[", page.Find(".scrounge-unit[data-vehicle='de-tank'] option[value='ru-s']").TextContent.Trim(), StringComparison.Ordinal);
        var scrounge = page.Find(".vehicle-scrounge[data-vehicle='de-tank']");
        Assert.True(scrounge.HasAttribute("disabled"));
        Assert.Equal("Choose the unit that scrounges", scrounge.GetAttribute("title"));

        page.Find(".scrounge-unit[data-vehicle='de-tank']").Change("ru-s");
        ViewAgain(page, "russian", "german");
        Assert.Equal(string.Empty, page.Find(".scrounge-unit[data-vehicle='de-tank']").GetAttribute("value") ?? string.Empty);

        page.Find(".scrounge-unit[data-vehicle='de-tank']").Change("ru-s");
        page.Find(".scrounge-unit[data-vehicle='de-tank2']").Change("ru-h");
        dice.Enqueue([4]);
        Commit(page, ".vehicle-scrounge[data-vehicle='de-tank']");
        var scrounged = Last<VehicleScrounged>();
        Assert.Equal(("de-tank", "ru-s"), (scrounged.Vehicle, scrounged.Unit));
        Assert.Empty(page.FindAll(".scrounge-unit[data-vehicle='de-tank']"));
        Assert.Equal("ru-h", page.Find(".scrounge-unit[data-vehicle='de-tank2']").GetAttribute("value"));
    }

    [Fact]
    public async Task TheRiderCheckboxIsLockedOnATankAndTheLoadSendsRiders()
    {
        // D6.2 (ruling R65.16): the T-34 takes no Passengers, so its units board as Riders, and the box says why it is locked. A view change clears the
        // units checked; the load sends them as Riders, and they are no longer offered.
        await Start("russian", "german", 1943, Vehicle("ru-tank", "defender-tank", At(3), "russian"), Unit("ru-s", "asl:squad", "defender-squad", At(3), "russian"),
            Unit("ru-l", "asl:leader", "defender-leader-7-0", At(3), "russian"), Unit("de-s", "asl:squad", "attacker-squad", At(11), "german"));
        await Advance();
        await Advance();
        var page = Open("russian");
        page.Find("#vehicle-unit").Change("ru-tank");

        var riders = page.Find("#vehicle-load-riders");
        Assert.True(riders.HasAttribute("disabled"));
        Assert.True(riders.HasAttribute("checked"));
        Assert.Equal("This vehicle takes no Passengers: units board it as Riders (D6.2)", riders.GetAttribute("title"));
        Assert.Contains("[", page.Find(".vehicle-load[value='ru-s']").ParentElement!.TextContent, StringComparison.Ordinal);

        page.Find(".vehicle-load[value='ru-s']").Change(true);
        ViewAgain(page, "russian", "german");
        page.Find("#vehicle-unit").Change("ru-tank");
        Assert.False(page.Find(".vehicle-load[value='ru-s']").HasAttribute("checked"));

        page.Find(".vehicle-load[value='ru-s']").Change(true);
        page.Find(".vehicle-load[value='ru-l']").Change(true);
        Commit(page, "#vehicle-load");
        var load = Last<VehicleStepped>();
        Assert.True(load.Riders);
        Assert.Equal(RussianRiders, load.Units!.Order(StringComparer.Ordinal));
        Assert.Empty(page.FindAll(".vehicle-load"));
    }

    [Fact]
    public async Task TheCarrierCrewCheckboxUnloadsItsHalfSquadAndTheRecordSaysItIsAbandoned()
    {
        // D6.83 (ruling R65.18): the Bren carrier's inherent HS unloads with the box checked. A view change clears the box; the unload sends the crew, its
        // proposal names the HS by its counter, not its id, and the record says the Carrier is Abandoned.
        await Start("british", "german", 1944, Vehicle("uk-bren", "campaign-british-bren-carrier", At(3), "british"),
            Unit("de-1", "asl:squad", "attacker-squad", At(11), "german"));
        await Advance();
        await Advance();
        var page = Open("british");
        page.Find("#vehicle-unit").Change("uk-bren");

        page.Find("#vehicle-unload-crew").Change(true);
        Assert.False(page.Find("#vehicle-unload").HasAttribute("disabled"));
        ViewAgain(page, "british", "german");
        page.Find("#vehicle-unit").Change("uk-bren");
        Assert.False(page.Find("#vehicle-unload-crew").HasAttribute("checked"));
        Assert.True(page.Find("#vehicle-unload").HasAttribute("disabled"));

        page.Find("#vehicle-unload-crew").Change(true);
        Propose(page, "#vehicle-unload");
        Assert.DoesNotContain("-uk-bren-crew", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Confirm(page);
        var unload = Last<VehicleStepped>();
        Assert.Equal(VehicleStepped.Unload, unload.Kind);
        Assert.EndsWith("-uk-bren-crew", Assert.Single(unload.Units!), StringComparison.Ordinal);
        Assert.Contains("is Abandoned (D6.83)", page.Find("#play-latest-panel").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#vehicle-unload-crew"));
    }

    [Fact]
    public async Task TheRoutBeneathButtonNamesTheHexAndItsRecordSaysBeneath()
    {
        // D6.1 (ruling R65.11): in the German RtPh the broken 4-6-7 aboard the truck routs beneath it from its button, which names its hex; the button
        // holds no draft. The record says it routed beneath the truck, not from its hex to the same hex.
        var passenger = Unit("de-s", "asl:squad", "attacker-squad", At(3), "german");
        passenger["position"] = new Dictionary<string, string> { ["in"] = "de-truck", ["role"] = "passenger" };
        passenger["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        await Start("german", "russian", 1943, Vehicle("de-truck", "attacker-truck", At(3), "german"), passenger,
            Unit("ru-s", "asl:squad", "defender-squad", At(11), "russian"));
        for (var phase = 0; phase < 5; phase++)
        {
            await Advance();
        }

        var page = Open("german");
        var beneath = page.Find(".rout-beneath[data-unit='de-s']");
        Assert.Contains("[", beneath.TextContent, StringComparison.Ordinal);
        Commit(page, ".rout-beneath[data-unit='de-s']");
        Assert.Equal("de-s", Last<RoutStepped>().Unit);
        Assert.Contains("routs beneath", page.Find("#play-latest-panel").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("routs from", page.Find("#play-latest-panel").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TheRecordsSayABailOutsDiceAFailedLoadAndATemporaryStart()
    {
        // R3's findings 3, 6, and 15 (the table player): in the Russian MPh the records, as the store holds them, of a Rider's Bail Out with an Original 12
        // (D6.24, A7.8), a broken loader back on foot (D5.43), and a Temporary crew's start dr of 5 (A21.22) are each a line with its dice, in the words of
        // the result; the rolls' purposes have words of their own.
        var rider = Unit("ru-s", "asl:squad", "defender-squad", At(3), "russian");
        rider["position"] = new Dictionary<string, string> { ["in"] = "ru-tank", ["role"] = "rider" };
        var loader = Unit("ru-h", "asl:half-squad", "defender-half-squad", At(9), "russian");
        loader["position"] = new Dictionary<string, string> { ["in"] = "ru-truck", ["role"] = "passenger" };
        loader["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        await Start("russian", "german", 1943, Vehicle("ru-tank", "defender-tank", At(3), "russian"), rider, Vehicle("ru-truck", "defender-truck", At(9), "russian"), loader,
            Unit("de-s", "asl:squad", "attacker-squad", At(0), "german"));
        await Advance();
        await Advance();
        Inject(("dice-rolled", new DiceRolled("p65-roll-1", "rider-bail-out", 2, 6, BoxCars, DiceRolled.SystemSource, "adjudicator")),
            ("rider-bailed-out", new RiderBailedOut("ru-s", "ru-tank", BoardLocation.Parse(At(3)), ScenarioA1RiderRules.Terrain, "p65-roll-1", 7, ScenarioA1RiderRules.Reduced)),
            ("instance-moved", new InstanceMoved("ru-h", new MapPosition(BoardLocation.Parse(At(9))))),
            ("dice-rolled", new DiceRolled("p65-roll-2", "vehicle-temporary-start", 2, 6, FiveAndOne, DiceRolled.SystemSource, "adjudicator")),
            ("vehicle-check-rolled", new VehicleCheckRolled("ru-tank", VehicleCheckRolled.TemporaryStart, "p65-roll-2", 0, VehicleCheckRolled.NoMove)));

        var lines = new PlayRecords(live.History(Game)!, Perspective.Adjudicator, long.MaxValue).Since(0).Select(line => line.Text).ToArray();
        Assert.Contains(lines, line => line.Contains("ru-s Bails Out of ru-tank", StringComparison.Ordinal)
            && line.Contains("; NMC DR 6, 6 = 12 against 7: Casualty Reduced and broken (D6.24)", StringComparison.Ordinal));
        Assert.Contains("ru-h does not board ru-truck: broken by the fire its boarding drew, so it is back on foot (D5.43)", lines);
        Assert.Contains("ru-tank's Temporary crew makes its start dr 5: no movement this phase (A21.22)", lines);
        Assert.Equal(("Bail Out NMC DR", "Scrounging dr", "CC DR at a vehicle"), (DisplayText.RollPurpose("rider-bail-out"), DisplayText.RollPurpose("scrounge"),
            DisplayText.RollPurpose("cc-vehicle-attack")));
    }

    [Fact]
    public async Task TheCaptureAttemptNamesTheAfvAfterItsHexSendsCaptureAndShowsItsArithmetic()
    {
        // A21.2 (ruling R65.22): in the Russian CCPh the squad stands with the Abandoned PzKpfw IIIH and the German HS beside it. The vehicle is offered by
        // its hex first; the capture waits for an attacker and the AFV, says so, and is cleared by a view change; it sends capture, and its record shows the
        // Kill Number and the DR: 3, 3 is a Final 4, which captures it.
        await Start("russian", "german", 1943, Vehicle("de-tank", "attacker-tank", At(3), "german", new()
        {
            ["asl:abandoned"] = true
        }),
            Unit("de-h", "asl:half-squad", "attacker-half-squad", At(3), "german"), Unit("ru-s", "asl:squad", "defender-squad", At(0), "russian"));
        Inject([("instance-moved", new InstanceMoved("ru-s", new MapPosition(BoardLocation.Parse(At(3))))),
            .. PhasesToCloseCombat.Select(phase => ("phase-changed", (EventPayload)new PhaseChanged(1, phase, "russian")))]);
        var page = Open("russian");
        string Here(string control) => $".vehicle-cc[data-location='{At(3)}'] {control}";

        var offered = page.Find(Here(".vehicle-cc-vehicle option[value='de-tank']")).TextContent.Trim();
        Assert.StartsWith("[", offered, StringComparison.Ordinal);
        Assert.EndsWith(", Abandoned", offered, StringComparison.Ordinal);
        var capture = page.Find(Here(".propose-vehicle-cc-capture"));
        Assert.True(capture.HasAttribute("disabled"));
        Assert.Equal("Choose an attacker and the Abandoned AFV", capture.GetAttribute("title"));

        page.Find(Here(".vehicle-cc-attacker")).Change("ru-s");
        page.Find(Here(".vehicle-cc-vehicle")).Change("de-tank");
        Assert.False(page.Find(Here(".propose-vehicle-cc-capture")).HasAttribute("disabled"));
        ViewAgain(page, "russian", "german");
        Assert.True(page.Find(Here(".propose-vehicle-cc-capture")).HasAttribute("disabled"));

        page.Find(Here(".vehicle-cc-attacker")).Change("ru-s");
        page.Find(Here(".vehicle-cc-vehicle")).Change("de-tank");
        dice.Enqueue([3, 3]);
        Commit(page, Here(".propose-vehicle-cc-capture"));
        var combat = Last<VehicleCloseCombatResolved>();
        Assert.Equal(("de-tank", "ru-s", true), (combat.Vehicle, Assert.Single(combat.Attackers), combat.Facts.GetProperty("capture").GetBoolean()));
        var latest = page.Find("#play-latest-panel").TextContent;
        Assert.Contains("to capture it: Kill Number", latest, StringComparison.Ordinal);
        Assert.Contains("= 4: it is captured (A11.5, A21.2)", latest, StringComparison.Ordinal);
    }
}
