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

    // Increment 3i: the scenario year of the game (D6.2: Riders of every nationality from 1943).
    private int year = 1942;

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
                scenarioYear = year,
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

    [Fact]
    public async Task TheRecordsSayALlmcAfterALeaderLostToFailureToRout()
    {
        // Increment 3b (A10.2, A10.5; rulings R70.3, R70.4): in the German RtPh the broken Russian 8-1 in the Open Ground two hexes from the German squad
        // does not rout and is eliminated as the phase ends; the 4-4-7 with him takes a LLMC, his -1 reversed: 4 + 3 + 1 = 8 against 7, broken. Both sides
        // read the line, with its dice, its cause, and its rule.
        var leader = Squad("ru-l", "defender-leader-8-1", WideBoards.At(5), "russian");
        leader["kind"] = "asl:leader";
        leader["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), leader, Squad("ru-s", "defender-squad", WideBoards.At(5), "russian"));
        for (var phase = 0; phase < 4; phase++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }

        dice.Enqueue([4, 3]);
        await Do(GameActions.AdvancePhase, new
        {
        });
        var check = Assert.Single(Events.Select(item => item.Payload).OfType<MoraleCheckResolved>());
        Assert.Equal("failure-to-rout", check.Cause);
        foreach (var view in new[] { Perspective.Adjudicator, new Perspective("german"), new Perspective("russian") })
        {
            var lines = new PlayRecords(live.History(Game)!, view, long.MaxValue).Since(0).Select(line => line.Text).ToArray();
            Assert.Contains(lines, line => line.Contains("ru-s takes LLMC after the loss of ru-l to Failure to Rout (A10.5): LLMC 4, 3 + 1 (leadership reversed, ru-l) = 8 against 7: failed, broken (A10.2)",
                StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task TheRoutPanelOffersAVoluntaryBreakByHexAndALeaderToRoutWith()
    {
        // Increment 3c (A10.41, A10.711; rulings R70.11, R70.12): at the start of the German RtPh the Russian's 4-4-7 and 8-1, two hexes from the German
        // squad, may break voluntarily: the Rout panel lists them under their hex, the leader first. The 4-4-7 breaks; then, choosing the broken 4-4-7 to
        // rout, the panel offers the unbroken 8-1 stacked with it to rout with it, named after his hex.
        var leader = Squad("ru-l", "defender-leader-8-1", WideBoards.At(5), "russian");
        leader["kind"] = "asl:leader";
        var broken = Squad("ru-b", "defender-squad", WideBoards.At(5), "russian");
        broken["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = true, ["asl:concealed"] = false, ["asl:hidden"] = false };
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Squad("ru-s", "defender-squad", WideBoards.At(5), "russian"), leader, broken);
        for (var phase = 0; phase < 4; phase++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }

        var page = Open("russian");
        string[] offered = [.. page.FindAll("input.voluntary-break-unit").Select(box => box.GetAttribute("data-unit")!)];
        Assert.Equal(VoluntaryBreakOffer, offered);
        Assert.NotEmpty(page.FindAll("#voluntary-break .unit-group-hex"));
        Assert.True(page.Find("#propose-voluntary-break").HasAttribute("disabled"));
        page.Find("input.voluntary-break-unit[data-unit='ru-s']").Change(true);
        page.Find("#propose-voluntary-break").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal(VoluntaryBreakTaken, Assert.Single(Events.Select(item => item.Payload).OfType<VoluntaryBreak>()).Units);
        var lines = new PlayRecords(live.History(Game)!, new Perspective("german"), long.MaxValue).Since(0).Select(line => line.Text).ToArray();
        Assert.Contains(lines, line => line.Contains("breaks voluntarily at the start of the RtPh, under DM, so as to rout (A10.41, A10.62)", StringComparison.Ordinal));

        page.Find("#rout-unit").Change("ru-b");
        var option = Assert.Single(page.FindAll("#rout-with-leader option"), item => item.GetAttribute("value") == "ru-l");
        Assert.StartsWith("[", option.TextContent.Trim(), StringComparison.Ordinal);
    }

    private static readonly string[] VoluntaryBreakOffer = ["ru-l", "ru-s"];
    private static readonly string[] VoluntaryBreakTaken = ["ru-s"];
    private static readonly string[] ConcealedMover = ["de-m"];

    private static Dictionary<string, object> Concealed(Dictionary<string, object> unit)
    {
        unit["conditions"] = new Dictionary<string, bool> { ["asl:broken"] = false, ["asl:concealed"] = true, ["asl:hidden"] = false };
        return unit;
    }

    [Fact]
    public async Task TheOwnerGivesUpAQuestionMarkFromThePlayPage()
    {
        // Increment 3e (A12.14; ruling R70.20): in the German PFPh the German view offers his concealed 4-6-7 under its hex to give up its "?"; the
        // Russian view offers nothing of his. Proposed and confirmed, the squad is concealed no more.
        await Start(Concealed(Squad("de-c", "attacker-squad", WideBoards.At(3), "german")), Concealed(Squad("de-k", "attacker-squad", WideBoards.At(2), "german")),
            Squad("ru-1", "defender-squad", WideBoards.At(5), "russian"));
        Assert.Empty(Open("russian").FindAll("#play-remove-concealment"));
        var page = Open("german");
        Assert.Equal(2, page.FindAll("#play-remove-concealment .unit-group-hex").Count);
        Assert.True(page.Find("#propose-remove-concealment").HasAttribute("disabled"));
        page.Find("input.remove-concealment-unit[data-unit='de-c']").Change(true);
        page.Find("#propose-remove-concealment").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.NotEqual(ConditionState.True, GameState.Condition(live.History(Game)!.Current!.Unit("de-c")!, Conditions.Concealed));
    }

    [Fact]
    public async Task TheViewersOptionIsShownToItsSideAloneAndTheMoverSeesTheWindow()
    {
        // Increment 3e (A12.14; ruling R70.21): in the German MPh the concealed 4-6-7 moves in Open Ground without Assault Movement, in the LOS of the
        // concealed Russian 4-4-7 alone. The Russian view shows the choice with its two answers; the German view shows no choice, only the DEFENDER's
        // window on its stack, and the German records say nothing of it.
        await Start(Concealed(Squad("de-m", "attacker-squad", WideBoards.At(3), "german")), Concealed(Squad("ru-v", "defender-squad", WideBoards.At(6), "russian")));
        await Do(GameActions.AdvancePhase, new
        {
        });
        await Do(GameActions.Move, new
        {
            unitIds = ConcealedMover,
            to = WideBoards.At(4),
        });
        Assert.Equal(ChoicePending.ConcealmentLoss, live.History(Game)!.Current!.Choice!.Kind);

        var russian = Open("russian");
        Assert.Equal(2, russian.FindAll("#play-choice button.choose").Count);
        Assert.Contains("[", russian.Find("#play-choice").TextContent, StringComparison.Ordinal);

        var german = Open("german");
        Assert.Empty(german.FindAll("#play-choice"));
        Assert.Empty(german.FindAll("#play-answer-first"));
        var lines = new PlayRecords(live.History(Game)!, new Perspective("german"), long.MaxValue).Since(0).Select(line => line.Text).ToArray();
        Assert.DoesNotContain(lines, line => line.Contains("A12.14; ruling R70.21", StringComparison.Ordinal));
    }

    private static readonly string[] Searcher = ["de-1"];

    [Fact]
    public async Task TheMovementPanelOffersASearchOfTheHexesTickedAndBothSidesReadIt()
    {
        // Increment 3g (A12.152; ruling R70.24): the German 4-6-7 moves to the fourth column in his MPh and the Russian passes; the movement panel then offers
        // a Search of the hexes it could enter, each in brackets. Ticked, the hex of the concealed Russian squad is searched with a dr of 1 (-1 for two
        // HS-equivalents): the squad loses its "?", the Casualty dr of 6 does nothing, and both sides read the record. The Russian view offers no Search.
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(3), "german"), Concealed(Squad("ru-c", "defender-squad", WideBoards.At(5), "russian")));
        await Do(GameActions.AdvancePhase, new
        {
        });
        await Do(GameActions.Move, new
        {
            unitIds = Searcher,
            to = WideBoards.At(4),
        });
        await Do(GameActions.PassFire, new
        {
        });
        Assert.Empty(Open("russian").FindAll("#search"));
        var page = Open("german");
        var box = page.Find($"input.search-hex[data-hex='{WideBoards.At(5)}']");
        Assert.StartsWith("[", box.ParentElement!.TextContent.Trim(), StringComparison.Ordinal);
        box.Change(true);
        dice.Enqueue([1]);
        dice.Enqueue([6]);
        page.Find("#propose-search").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.NotEqual(ConditionState.True, GameState.Condition(live.History(Game)!.Current!.Unit("ru-c")!, Conditions.Concealed));
        foreach (var view in new[] { new Perspective("german"), new Perspective("russian") })
        {
            var lines = new PlayRecords(live.History(Game)!, view, long.MaxValue).Since(0).Select(line => line.Text).ToArray();
            Assert.Contains(lines, line => line.Contains("de-1 Searches from", StringComparison.Ordinal) && line.Contains("1 concealed enemy unit loses its \"?\" (A12.152)", StringComparison.Ordinal));
        }
    }

    private static Dictionary<string, object> Held(string id, string definition, string holder) => new()
    {
        ["id"] = id,
        ["kind"] = "asl:mg",
        ["definition"] = definition,
        ["side"] = "german",
        ["holding"] = new
        {
            holder,
            role = "possessed"
        },
        ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
    };

    [Fact]
    public async Task TheCcPanelOffersADropAboveTheIpcWithdrawalsTheSideMayKnowOfAndAGunsCrewHexFirst()
    {
        // Increment 3h (A11.21; rulings R70.26, R70.27): in the German CCPh the 4-6-7 in Melee in the fifth column carries an MMG and an LMG, above its IPC;
        // the panel offers each to drop, and its withdrawal to the "?" in the fourth column (a "?" does not bar it) but not to the Known Russian squad in the
        // sixth. The Russian Gun's crew in its Location is a target in the attack builder, named hex first.
        var crew = Squad("ru-gc", "defender-crew", WideBoards.At(5), "russian");
        crew["kind"] = "asl:crew";
        var melee = Squad("de-1", "attacker-squad", WideBoards.At(5), "german");
        ((Dictionary<string, bool>)melee["conditions"])["asl:melee"] = true;
        await Start(crew, new Dictionary<string, object>
        {
            ["id"] = "ru-gun",
            ["kind"] = "asl:gun",
            ["definition"] = "defender-at-gun",
            ["side"] = "russian",
            ["position"] = new Dictionary<string, string> { ["at"] = WideBoards.At(5), ["facing"] = "west" },
            ["holding"] = new
            {
                holder = "ru-gc",
                role = "manned"
            },
            ["conditions"] = new Dictionary<string, bool> { ["asl:malfunctioned"] = false },
        }, melee, Held("de-mmg", "attacker-mmg", "de-1"), Held("de-lmg", "attacker-lmg", "de-1"), Concealed(Squad("ru-c", "defender-squad", WideBoards.At(4), "russian")),
            Squad("ru-k", "defender-squad", WideBoards.At(6), "russian"));
        for (var phase = 0; phase < 6; phase++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }

        Assert.Equal("ccph", live.History(Game)!.Current!.Phase);
        var page = Open("german");
        page.Find("#cc-location").Change(WideBoards.At(5));
        Assert.Equal(2, page.FindAll(".cc-drop[data-unit='de-1']").Count);
        string?[] destinations = [.. page.FindAll(".cc-withdraw[data-unit='de-1'] option").Select(option => option.GetAttribute("value"))];
        Assert.Contains(WideBoards.At(4), destinations);
        Assert.DoesNotContain(WideBoards.At(6), destinations);
        var target = page.Find(".cc-defender[data-unit='ru-gc']");
        Assert.StartsWith(DisplayText.Place(1, WideBoards.At(5)) + ": ", target.ParentElement!.TextContent.Trim(), StringComparison.Ordinal);
        page.Find(".cc-drop[data-weapon='de-mmg']").Change(true);
        Assert.True(page.Find(".cc-drop[data-weapon='de-mmg']").HasAttribute("checked"));
    }

    private static readonly string[] AdvancingSquad = ["de-1"];

    [Fact]
    public async Task TheVehicleCcPanelOffersAnInfantryAttackOnARiderNamedHexFirst()
    {
        // Increment 3i (A11.31, A11.611; rulings R70.30, R70.31): the German 4-6-7 advances onto the Russian T-34 with its Rider squad, passing its PAATC (3 +
        // 4 against 7). In the German CCPh the vehicle CC panel offers the Rider, hex first and with its vehicle, for an Infantry attack; the button waits
        // for an attacker and a checked defender, and the attack is recorded as one sequential round with -1 against the Rider.
        year = 1943;
        var tank = new Dictionary<string, object>
        {
            ["id"] = "ru-tank",
            ["kind"] = "asl:vehicle",
            ["definition"] = "defender-tank",
            ["side"] = "russian",
            ["position"] = new Dictionary<string, string> { ["at"] = WideBoards.At(5), ["facing"] = "west" },
            ["conditions"] = new Dictionary<string, bool>(),
        };
        var rider = Squad("ru-r", "defender-squad", WideBoards.At(5), "russian");
        rider["position"] = new Dictionary<string, string> { ["in"] = "ru-tank", ["role"] = "rider" };
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(4), "german"), tank, rider);
        for (var phase = 0; phase < 5; phase++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }

        dice.Enqueue([3, 4]);
        await Do(GameActions.Advance, new
        {
            unitIds = AdvancingSquad,
            to = WideBoards.At(5),
        });
        await Do(GameActions.AdvancePhase, new
        {
        });
        Assert.Equal("ccph", live.History(Game)!.Current!.Phase);

        var page = Open("german");
        string Here(string control) => $".vehicle-cc[data-location='{WideBoards.At(5)}'] {control}";
        var box = page.Find(Here(".vehicle-cc-defender"));
        var label = box.ParentElement!.TextContent.Trim();
        Assert.StartsWith(DisplayText.Place(1, WideBoards.At(5)) + ": ", label, StringComparison.Ordinal);
        Assert.Contains(", Rider on ", label, StringComparison.Ordinal);
        Assert.True(page.Find(Here(".propose-vehicle-cc-infantry")).HasAttribute("disabled"));
        page.Find(Here(".vehicle-cc-attacker")).Change("de-1");
        page.Find(Here(".vehicle-cc-defender")).Change(true);
        Assert.False(page.Find(Here(".propose-vehicle-cc-infantry")).HasAttribute("disabled"));
        dice.Enqueue([6, 6]);
        page.Find(Here(".propose-vehicle-cc-infantry")).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var round = Events.Select(item => item.Payload).OfType<CloseCombatResolved>().Last();
        Assert.Equal((CloseCombatResolved.SequentialRound, "ru-r"), (round.Round, Assert.Single(round.Defenders)));
        Assert.Contains(round.Resolution.GetProperty("attacks")[0].GetProperty("defending")[0].GetProperty("drm").EnumerateArray(),
            item => item.GetProperty("name").GetString() == "vs-rider");
    }

    [Fact]
    public async Task TheVehicleCcPanelOffersTheCaptureOfATrucksPassengerNamedHexFirst()
    {
        // Increment 3j (A11.52; ruling R70.34): the German 4-6-7 advances into the Russian truck's Location (no PAATC against an unarmored vehicle). In the
        // German CCPh the panel offers its Passenger squad, hex first and with its vehicle, to the capture button alone; 2 + 2 with -2 and the capture's +1
        // captures it, and the truck with it.
        var truck = new Dictionary<string, object>
        {
            ["id"] = "ru-truck",
            ["kind"] = "asl:vehicle",
            ["definition"] = "defender-truck",
            ["side"] = "russian",
            ["position"] = new Dictionary<string, string> { ["at"] = WideBoards.At(5), ["facing"] = "west" },
            ["conditions"] = new Dictionary<string, bool>(),
        };
        var passenger = Squad("ru-p", "defender-squad", WideBoards.At(5), "russian");
        passenger["position"] = new Dictionary<string, string> { ["in"] = "ru-truck", ["role"] = "passenger" };
        await Start(Squad("de-1", "attacker-squad", WideBoards.At(4), "german"), truck, passenger);
        for (var phase = 0; phase < 5; phase++)
        {
            await Do(GameActions.AdvancePhase, new
            {
            });
        }

        await Do(GameActions.Advance, new
        {
            unitIds = AdvancingSquad,
            to = WideBoards.At(5),
        });
        await Do(GameActions.AdvancePhase, new
        {
        });
        Assert.Equal("ccph", live.History(Game)!.Current!.Phase);

        var page = Open("german");
        string Here(string control) => $".vehicle-cc[data-location='{WideBoards.At(5)}'] {control}";
        var label = page.Find(Here(".vehicle-cc-defender")).ParentElement!.TextContent.Trim();
        Assert.StartsWith(DisplayText.Place(1, WideBoards.At(5)) + ": ", label, StringComparison.Ordinal);
        Assert.Contains(", Passenger in ", label, StringComparison.Ordinal);
        page.Find(Here(".vehicle-cc-attacker")).Change("de-1");
        page.Find(Here(".vehicle-cc-defender")).Change(true);
        Assert.True(page.Find(Here(".propose-vehicle-cc-infantry")).HasAttribute("disabled"));
        Assert.False(page.Find(Here(".propose-vehicle-cc-capture-passengers")).HasAttribute("disabled"));
        dice.Enqueue([2, 2]);
        page.Find(Here(".propose-vehicle-cc-capture-passengers")).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal("german", Assert.Single(Events.Select(item => item.Payload).OfType<VehicleCaptured>()).Side);
    }
}
