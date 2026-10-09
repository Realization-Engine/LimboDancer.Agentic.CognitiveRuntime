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
/// Pass 35, task 35.16: the page tests passes 31c and 31d left unwritten (the backlog's sections 50 and 51), on the board with room, and the
/// sweep of the Play page for what a side may not read (the week review of 2026-10-04). The setup map's tooltips are in
/// <see cref="SetupPlansPageTests"/>, which has a card with an order of battle. Each game is set up through the game's own gate and then opened
/// on the page.
/// </summary>
public sealed class PlayPagePass35Tests : IDisposable
{
    private const string Game = "wide";
    private static readonly string[] Sides = ["german", "russian"];
    private static readonly string[] Views = ["german", "russian", Perspective.AdjudicatorName];
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-35-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BunitContext context = new();

    public PlayPagePass35Tests()
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

    private Task Advance() => Do(GameActions.AdvancePhase, new
    {
    });

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
            await Advance();
        }
    }

    private IRenderedComponent<PlayPage> Open(string view)
    {
        var page = context.Render<PlayPage>();
        page.OpenGame(Game);
        page.ViewAs(view);
        return page;
    }

    /// <summary>Reads the game again after an action taken through the gate, and returns to a view.</summary>
    private static void Reopen(IRenderedComponent<PlayPage> page, string view)
    {
        page.OpenGame(string.Empty);
        page.OpenGame(Game);
        page.ViewAs(view);
    }

    /// <summary>Proposes, returns the review's text as it stood before Confirm, and confirms.</summary>
    private static string Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal),
            page.Find("#play-proposal").TextContent));
        var review = Text(page, "#play-proposal");
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        return review;
    }

    private static string Text(IRenderedComponent<PlayPage> page, string selector) =>
        string.Join(" | ", page.FindAll(selector).Select(item => System.Text.RegularExpressions.Regex.Replace(item.TextContent, "\\s+", " ").Trim()));

    // Pass 31c's row: a tag through Deployment and Recombination (ruling R31c.1). The HS take the squad's tag with "a" and "b", the squad they
    // Recombine into takes the stem again, and the other side, which held the squad by name, reads the same tags.
    [Fact]
    public async Task ATagIsKeptThroughDeploymentAndRecombinationInBothViews()
    {
        await Start(0, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german"), Unit("gl", "asl:leader", "attacker-leader-8-1", WideBoards.At(5), "german"),
            Unit("r1", "asl:squad", "defender-squad", WideBoards.At(3), "russian"));
        var page = Open("german");
        page.Find("#deploy-squad").Change("g1");
        page.Find("#deploy-leader").Change("gl");
        dice.Enqueue([2, 2]);
        Commit(page, "#propose-deploy");
        foreach (var view in Sides)
        {
            page.ViewAs(view);
            var units = Text(page, "#play-units tr");
            Assert.Contains("2-4-7 half-squad G1a", units, StringComparison.Ordinal);
            Assert.Contains("2-4-7 half-squad G1b", units, StringComparison.Ordinal);
            Assert.DoesNotContain("4-6-7 squad G1", units, StringComparison.Ordinal);
            Assert.Contains($"4-6-7 squad G1 in {Said(WideBoards.At(5))} becomes the HS 2-4-7 half-squad G1a and 2-4-7 half-squad G1b (A1.31)", Text(page, "#play-latest"), StringComparison.Ordinal);
        }

        // The next German RPh: the two HS Recombine. The review names no identifier: the squad is not in the game yet, and is "their squad".
        for (var phase = 0; phase < 16; phase++)
        {
            await Advance();
        }

        Assert.Equal((2, "rph", "german"), (Current.Turn, Current.Phase, Current.PhasingSide));
        Reopen(page, "german");
        var halves = Current.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Kind == "asl:half-squad").Select(unit => unit.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(2, halves.Length);
        page.Find("#recombine-one").Change(halves[0]);
        page.Find("#recombine-two").Change(halves[1]);
        page.Find("#recombine-leader").Change("gl");
        var review = Commit(page, "#propose-recombine");
        Assert.Contains("2-4-7 half-squad G1a and 2-4-7 half-squad G1b Recombine into their squad, directed by 8-1 leader G1 (A1.32)", review, StringComparison.Ordinal);
        Assert.DoesNotContain("recombine-", review, StringComparison.Ordinal);
        foreach (var view in Sides)
        {
            page.ViewAs(view);
            var units = Text(page, "#play-units tr");
            Assert.Contains("4-6-7 squad G1", units, StringComparison.Ordinal);
            Assert.DoesNotContain("half-squad", units, StringComparison.Ordinal);
            Assert.Contains("Recombine into 4-6-7 squad G1 (A1.32)", Text(page, "#play-latest"), StringComparison.Ordinal);
        }
    }

    // Pass 31c's row (design D16; play test P-27): a Location in Melee is marked on its hex in each view that holds a unit of the Melee, and
    // in no other hex.
    [Fact]
    public async Task AMeleeIsMarkedOnItsHexInEachView()
    {
        await Start(1, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german", "asl:melee"), Unit("r1", "asl:squad", "defender-squad", WideBoards.At(5), "russian", "asl:melee"),
            Unit("r2", "asl:squad", "defender-squad", WideBoards.At(2), "russian"));
        var page = Open("german");
        foreach (var view in Views)
        {
            page.ViewAs(view);
            page.WaitForAssertion(() => Assert.Contains("class=\"play-melee\"", context.MapLayer("setMarks"), StringComparison.Ordinal));
            var marks = System.Text.RegularExpressions.Regex.Matches(context.MapLayer("setMarks"), "class=\"play-melee\" data-location=\"([^\"]+)\"");
            Assert.Equal([WideBoards.At(5)], marks.Select(match => match.Groups[1].Value));
            Assert.Contains(">Melee</text>", context.MapLayer("setMarks"), StringComparison.Ordinal);
        }
    }

    // Pass 31c's row (play test P-17; A9.2): a MG whose colored dr is within its ROF is said to have kept it, to both sides, and is offered again.
    [Fact]
    public async Task AKeptRateOfFireIsSaidToBothSidesAndTheMgIsOfferedAgain()
    {
        await Start(1, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(5), "german"), Sw("gm", "asl:mg", "attacker-mmg", "g1", "german"),
            Unit("r1", "asl:squad", "defender-squad", WideBoards.At(6), "russian"));
        var page = Open("german");
        page.Find("#fire-from").Change(WideBoards.At(5));
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        page.Find(".fire-weapon[data-weapon='gm']").Change(true);
        page.Find("#fire-target").Change(WideBoards.At(6));

        // The colored dr of 1 is within the MMG's ROF; 7 on the 16 column is a 2MC, which r1 fails on 3, 3.
        dice.Enqueue([1, 6, 3, 3]);
        Commit(page, "#propose-fire");
        const string Kept = "The MMG kept its rate of fire and may fire again this phase (A9.2).";
        Assert.Contains(Kept, Text(page, ".fire-record"), StringComparison.Ordinal);

        // The MG is listed again and may be ticked.
        page.Find("#fire-from").Change(WideBoards.At(5));
        page.Find(".fire-firer[data-unit='g1']").Change(true);
        Assert.Equal("the MMG of 4-6-7 squad G1", Text(page, "label:has(.fire-weapon)"));
        Assert.False(page.Find(".fire-weapon[data-weapon='gm']").HasAttribute("disabled"));

        page.ViewAs("russian");
        Assert.Contains(Kept, Text(page, ".fire-record"), StringComparison.Ordinal);
    }

    // Pass 31d's row (design D12): what a view does itself after it comes back is not listed as missed, and the list it came back to stays.
    [Fact]
    public async Task SinceYouLastLookedLeavesOutWhatTheViewThenDoesItself()
    {
        await Start(10, Unit("r1", "asl:squad", "defender-squad", WideBoards.At(1), "russian"), Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(3), "german"));
        Assert.Equal(("mph", "russian"), (Current.Phase, Current.PhasingSide));
        var page = Open("german");
        page.ViewAs("russian");
        page.Find(".move-unit[data-unit='r1']").Change(true);
        page.Find("#move-to").Change(WideBoards.At(2));
        Commit(page, "#propose-move");

        var moved = $"Turn 1, Russian Movement Phase: 4-4-7 squad R1 moves from {Said(WideBoards.At(1))} to {Said(WideBoards.At(2))}";
        page.ViewAs("german");
        Assert.Equal("Since you last looked (1)", Text(page, "#play-activity-since"));
        Assert.Equal(moved, Text(page, "#play-since li"));

        // The German side declines First Fire: its own pass is the latest record, and is not something it missed.
        Commit(page, "#propose-pass");
        Assert.Contains("The German side, the DEFENDER, declines First Fire", Text(page, "#play-latest"), StringComparison.Ordinal);
        Assert.Equal("Since you last looked (1)", Text(page, "#play-activity-since"));
        Assert.Equal(moved, Text(page, "#play-since li"));

        // The Russian side missed the pass, and not its own move.
        page.ViewAs("russian");
        Assert.Equal("Since you last looked (1)", Text(page, "#play-activity-since"));
        Assert.Equal("Turn 1, Russian Movement Phase: The German side, the DEFENDER, declines First Fire", Text(page, "#play-since li"));
    }

    // Pass 31d's row (A6.11; pass 31's warning): a shot whose LOS the hill blocks is reviewed with its consequence, confirmed by a button that
    // says so, and recorded as having no effect, with its firer marked as having fired.
    [Fact]
    public async Task AShotWithNoLosIsConfirmedByItsOwnButtonAndItsRecordSaysSo()
    {
        var (from, to) = (WideBoards.BesideHill(7), WideBoards.BesideHill(11));
        await Start(1, Unit("g1", "asl:squad", "attacker-squad", from, "german"), Unit("r1", "asl:squad", "defender-squad", to, "russian"));
        var page = Open("german");
        page.Find("#fire-from").Change(from);
        page.Find(".fire-firer[data-unit='g1']").Change(true);

        // A6.11: no LOS is read before the attack is declared, so the target is offered by its range alone.
        Assert.Equal($"{Said(to)}: 4 hexes, Normal Range", Text(page, "#fire-target option:not([value=''])"));
        page.Find("#fire-target").Change(to);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        Assert.Contains($"No firer has a LOS to {Said(to)}, so the attack has no effect and its firers are still marked as having fired (A6.11).", Text(page, "#play-consequences"), StringComparison.Ordinal);
        Assert.Equal("Confirm the shot with no LOS", Text(page, "#play-confirm"));
        dice.Enqueue([3, 3]);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));

        var record = $"4-6-7 squad G1 in {Said(from)} fires at {Said(to)}: the LOS is blocked, so the attack has no effect and its firers have fired (A6.11).";
        Assert.Contains(record, Text(page, "#play-latest"), StringComparison.Ordinal);
        Assert.Contains("prep-fire", Text(page, "#play-units tr[data-unit='g1']"), StringComparison.Ordinal);
        Assert.Contains("4-4-7 squad R1 unaffected", Text(page, ".fire-record"), StringComparison.Ordinal);
        page.ViewAs("russian");
        Assert.Contains(record, Text(page, "#play-latest"), StringComparison.Ordinal);
    }

    // Pass 31d's row (design D9; ruling R31d.6; A20.5): where the package would refuse the Ambush, the due list says why in the package's own
    // sentence, the Ambush is refused with it, and the phase's end says the Location was not fought.
    [Fact]
    public async Task TheCloseCombatDueListSaysWhyThePackageDoesNotDecideALocation()
    {
        // A German prisoner stands in the woods with the Russian r1 and has no Guard; g1 advances in from the next hex.
        var woods = WideBoards.At(7);
        await Start(6, Unit("g1", "asl:squad", "attacker-squad", WideBoards.At(6), "german"), Unit("r1", "asl:squad", "defender-squad", woods, "russian"),
            Unit("gp", "asl:squad", "attacker-squad", woods, "german", "asl:captured"));
        Assert.Equal("aph", Current.Phase);
        var page = Open("german");
        page.Find(".advance-unit[data-unit='g1']").Change(true);
        page.Find("#advance-to").Change(woods);
        Commit(page, "#propose-advance-units");
        Commit(page, "#propose-advance");
        Assert.Equal("ccph", Current.Phase);

        const string Why = "a prisoner's Guard is not an enemy unit in its Location (A20.5)";
        Assert.Contains($"{Said(woods)}: the Close Combat package does not decide this Location, since {Why}", Text(page, "#cc-due"), StringComparison.Ordinal);
        page.Find("#cc-location").Change(woods);
        page.Find("#propose-ambush").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Contains("A prisoner's Guard is not an enemy unit in its Location (A20.5)", Text(page, "#play-proposal"), StringComparison.Ordinal);

        page.Find("#propose-advance").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        Assert.Contains($"No Close Combat was fought in {Said(woods)} this phase", Text(page, "#play-consequences"), StringComparison.Ordinal);
        Assert.Equal("Confirm with a Close Combat not fought", Text(page, "#play-confirm"));
    }

    // The week review of 2026-10-04: information a side may not have reached the page in 13 of 15 passes, and Play had no standing test for it.
    // A game with concealed and hidden units on each side is taken through both Player Turns; at every phase, in each side's view, nothing
    // the page renders or draws names a unit that view does not hold, and no fire target list offers the Location only a hidden unit is in.
    [Fact]
    public async Task NoPhaseOfThePageNamesAUnitTheViewDoesNotHold()
    {
        var hidden = new Dictionary<string, string> { ["german"] = WideBoards.At(0), ["russian"] = WideBoards.At(11) };
        await Start(0,
            Unit("gseenq", "asl:squad", "attacker-squad", WideBoards.At(3), "german"), Unit("gleadq", "asl:leader", "attacker-leader-8-1", WideBoards.At(3), "german"),
            Sw("gmgq", "asl:mg", "attacker-mmg", "gseenq", "german"),
            Unit("gkeptq", "asl:squad", "attacker-squad", WideBoards.At(2), "german", "asl:concealed"), Unit("gbrokq", "asl:squad", "attacker-squad", WideBoards.At(2), "german", "asl:concealed", "asl:broken"),
            Unit("ghidq", "asl:squad", "attacker-squad", hidden["german"], "german", "asl:hidden"),
            Unit("rseenq", "asl:squad", "defender-squad", WideBoards.At(6), "russian"), Unit("rleadq", "asl:leader", "defender-leader", WideBoards.At(6), "russian"),
            Unit("rkeptq", "asl:squad", "defender-squad", WideBoards.At(7), "russian", "asl:concealed"), Sw("rmgq", "asl:mg", "defender-mmg", "rkeptq", "russian"),
            Unit("rbrokq", "asl:squad", "defender-squad", WideBoards.At(7), "russian", "asl:concealed", "asl:broken"),
            Unit("rhidq", "asl:squad", "defender-squad", hidden["russian"], "russian", "asl:hidden"));
        var page = Open("german");
        var swept = 0;
        for (var phase = 0; phase < 16; phase++)
        {
            if (phase > 0)
            {
                await Advance();
            }

            var at = $"turn {Current.Turn}, {Current.Phase}, {Current.PhasingSide} phasing";
            foreach (var view in Sides)
            {
                Reopen(page, view);
                var names = live.NamesOf(live.History(Game)!, new Perspective(view));
                var other = view == "german" ? "russian" : "german";

                // What the view does not hold: the other side's units it cannot name, and whatever they hold.
                var unheld = Current.Units.Where(unit => unit.Side == other && names.Held(unit.Id, Current.Revision) is var name && (name == unit.Id || name == UnitNames.Unnamed))
                    .Select(unit => unit.Id).ToHashSet(StringComparer.Ordinal);
                string[] secret = [.. unheld, .. Current.Equipment.Where(item => item.Holding is { } holding && unheld.Contains(holding.Holder)).Select(item => item.Id)];
                Assert.True(secret.Length >= 3, $"{at}, {view} view: the sweep needs units the view does not hold, and found {string.Join(", ", secret)}");

                void Check(string step)
                {
                    var read = page.Markup + context.MapText();
                    foreach (var id in secret)
                    {
                        Assert.False(read.Contains(id, StringComparison.Ordinal), $"{at}, {view} view, {step}: the page names {id}, which the view does not hold");
                    }

                    Assert.DoesNotContain(hidden[other], page.FindAll("#fire-target option, #ordnance-target option").Select(option => option.GetAttribute("value")));
                    swept++;
                }

                Check("as opened");

                // Each Location the view may fire from, with every firer ticked, so the target list and the weapons are rendered.
                foreach (var from in page.FindAll("#fire-from option").Select(option => option.GetAttribute("value")!).Where(value => value.Length > 0).ToArray())
                {
                    page.Find("#fire-from").Change(from);
                    for (var index = 0; index < page.FindAll(".fire-firer").Count; index++)
                    {
                        page.FindAll(".fire-firer")[index].Change(true);
                    }

                    Check($"firing from {from}");
                }

                // Every tab of the page's panes.
                foreach (var tab in page.FindAll("[role='tab']").Select(item => item.Id).Where(id => id is { Length: > 0 }).ToArray())
                {
                    page.Find("#" + tab).Click();
                    Check($"tab {tab}");
                }
            }
        }

        Assert.Equal((1, "ccph", "russian"), (Current.Turn, Current.Phase, Current.PhasingSide));
        Assert.True(swept >= 64, $"the sweep checked {swept} renderings");
    }
}
