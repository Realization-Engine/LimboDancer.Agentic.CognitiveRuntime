using Bunit;
using LimboDancer.Dice;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Unit step 24 on the Play page (U28): a Gun and its crew are placed with the Gun's facing, and in the PFPh the Ordnance panel fires
/// the Gun at an ADJACENT Russian squad; the record shows the To Hit arithmetic with the colored die. The verified synthetic board and
/// dice from a fixed queue.
/// </summary>
public sealed class PlayPageOrdnanceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-ordnance-" + Guid.NewGuid().ToString("N"));
    private readonly Queue<int> dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageOrdnanceTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards, new DiceRoller(_ => dice.TryDequeue(out var value) ? value - 1
            : throw new InvalidOperationException("The test drew a roll it did not expect.")));
        var games = new GameLibrary(library, boards, live);
        context.Services.AddSingleton(library);
        context.Services.AddSingleton(live);
        context.Services.AddSingleton(games);
        context.Services.AddSingleton(new GameMaps(boards, maps, new RenderCache(), library, games));
        context.UseViewport();
        context.Services.AddSingleton(new StudioLos(boards, maps, options));
    }

    public void Dispose()
    {
        context.Dispose();
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    /// <summary>Two ADJACENT Open Ground hexes at one level.</summary>
    private (string One, string Two) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName? hex, int level) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let one = handle.Geometry.NameOf(index)
                let level = handle.HexFacts(one)!.BaseLevel
                where OpenGround(one, level)
                from side in Enum.GetValues<HexsideDirection>()
                let two = handle.Neighbor(one, side)
                where OpenGround(two, level) && handle.HexFacts(one)!.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff)
                select ($"{Board}:{one}:0", $"{Board}:{two}:0")).First();
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-add").Click();
    }

    [Fact]
    public void U28OnThePageAGunManByItsCrewFiresAndItsRecordShowsTheToHitArithmetic()
    {
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "2" });
        Place(page, "de-crew", "attacker-crew", hexes.One);
        page.Find("#place-definition").Change("attacker-inf-gun");
        page.Find("#place-facing").Change("east");
        page.Find("#place-holder").Change("de-crew");
        Place(page, "de-gun", "attacker-inf-gun", hexes.One);
        Assert.Equal(string.Empty, page.Find("#place-holder").GetAttribute("value"));
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        // C3.3: black Basic TH# 8 at one hex with Case L -2 (and Case A if the Gun turns): 6 and 5 miss whatever the turn.
        Assert.Contains(page.FindAll("#ordnance-gun option"), option => option.GetAttribute("value") == "de-gun");
        page.Find("#ordnance-gun").Change("de-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        dice.Enqueue(6);
        dice.Enqueue(5);
        Commit(page, "#propose-ordnance");
        Assert.Empty(dice);
        var record = page.Find("#play-ordnance .ordnance-record").TextContent;
        Assert.StartsWith($"Gun G1 in {DisplayText.Place(1, hexes.One)} fires HE at {DisplayText.Place(1, hexes.Two)}: ", record, StringComparison.Ordinal);
        Assert.DoesNotContain("de-gun", record, StringComparison.Ordinal);
        Assert.Contains("Basic TH# 8 (black) = 8; DR 6 (colored), 5 = 11", record, StringComparison.Ordinal);
        Assert.Contains("miss", record, StringComparison.Ordinal);
        Assert.False(live.History("village")!.HasErrors);
    }

    [Fact]
    public void AGunWhoseHeHasADepletionNumberNamesItAtInfantryAndShowsTheNumber()
    {
        // Pass 55 (C2.21, C8.8, C8.9): the British 6-Pounder's HE is Special Ammunition, HE8 in June 1944. At Infantry the panel offers it with
        // its number and the shot names it; an Original 9 finds none, asks for no IFT DR, and the panel then says it has run out.
        var hexes = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string>
        {
            ["board"] = Board, ["first"] = "british", ["second"] = "german", ["first-elr"] = "3", ["second-elr"] = "3", ["year"] = "1944", ["month"] = "6",
        });
        Place(page, "uk-crew", "british-crew", hexes.One);
        page.Find("#place-definition").Change("british-at-gun");
        page.Find("#place-facing").Change("east");
        page.Find("#place-holder").Change("uk-crew");
        Place(page, "uk-gun", "british-at-gun", hexes.One);
        Place(page, "g1", "attacker-squad", hexes.Two);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        page.Find("#ordnance-gun").Change("uk-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        Assert.Empty(page.FindAll("#ordnance-vehicle"));
        var option = Assert.Single(page.FindAll("#ordnance-ammunition option"));
        Assert.Equal(("he", "HE (Depletion Number 8)"), (option.GetAttribute("value"), option.TextContent));
        Assert.Empty(page.FindAll("#ordnance-ammunition-note"));
        dice.Enqueue(5);
        dice.Enqueue(4);
        Commit(page, "#propose-ordnance");
        Assert.Empty(dice);

        // What the page sent, read from the committed game.
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        var fired = history.Events.Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        Assert.Equal("he", fired.Facts.GetProperty("ammunition").GetString());
        Assert.Equal(1944, fired.Facts.GetProperty("scenarioYear").GetInt32());
        Assert.Equal("none", fired.Resolution.GetProperty("ammunitionUse").GetString());
        Assert.False(fired.Resolution.TryGetProperty("hit", out _));

        // The table player's and the UI review of pass 55: the round that was not there is no miss, the Gun has not fired (C8.9), and the
        // number is the one the record itself names.
        Assert.Equal(8, fired.Resolution.GetProperty("depletionNumber").GetInt32());
        var record = page.Find("#play-ordnance .ordnance-record").TextContent;
        Assert.Contains(": no shot: it had no HE; ", record, StringComparison.Ordinal);
        Assert.Contains("the Original DR is above the Depletion Number 8: it has none for the rest of the scenario (C8.9)", record, StringComparison.Ordinal);
        Assert.Contains("it has not fired and may fire other ammunition (C8.9)", record, StringComparison.Ordinal);
        Assert.DoesNotContain("miss", record, StringComparison.Ordinal);
        Assert.DoesNotContain("may fire again this phase", record, StringComparison.Ordinal);

        // The Gun has not fired and is still offered, with no HE left to name.
        page.Find("#ordnance-gun").Change("uk-gun");
        page.Find("#ordnance-target").Change(hexes.Two);
        Assert.Empty(page.FindAll("#ordnance-ammunition"));
        Assert.Equal("HE: it has run out (C8.9)", page.Find("#ordnance-ammunition-note").TextContent);

        // The shot still names HE, so the refusal is that the Gun has none, not that it was left unnamed.
        page.Find("#propose-ordnance").Click();
        page.WaitForAssertion(() => Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.True(page.Find("#play-proposal").TextContent.Contains("it has none now", StringComparison.Ordinal), page.Find("#play-proposal").TextContent);
        Assert.DoesNotContain("name HE for the shot", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// A hex, a facing, and an aim of the small test board from which a Canister shot attacks a hex in Open Ground at the firing hex's level,
    /// with no hexside terrain, where the target squad stands: a hex of the aim's cluster, or the adjacent hex on its LOS. C8.41 (the referee's
    /// review of pass 55): the adjacent hex is attacked with a cluster that is, so a hex of the cluster is at the Gun's level and in its LOS, as
    /// the board's own LOS reads it. No hex of the aim is seen through a Hindrance, which the Fire package would not attribute. Every such spot
    /// is given: the fire planner reads its own Hindrances, so the test takes the first spot whose shot the gate accepts.
    /// </summary>
    private IEnumerable<(string Gun, string Facing, int Aim, string Squad, bool Adjacent)> CanisterSpots()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        var los = context.Services.GetRequiredService<StudioLos>();
        bool Open(HexName? hex) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts
            && facts.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff);

        // Every order of so many steps across the left and the right hexside of the Covered Arc.
        static IEnumerable<int[]> Orders(int lefts, int rights, int left, int right) =>
            lefts == 0 && rights == 0 ? [[]]
            : (lefts > 0 ? Orders(lefts - 1, rights, left, right).Select(rest => (int[])[left, .. rest]) : [])
                .Concat(rights > 0 ? Orders(lefts, rights - 1, left, right).Select(rest => (int[])[right, .. rest]) : []);
        foreach (var index in handle.Geometry.Hexes())
        {
            var gun = handle.Geometry.NameOf(index);
            var level = handle.HexFacts(gun)!.BaseLevel;
            if (!Open(gun))
            {
                continue;
            }

            LimboDancer.Domains.Asl.Maps.Los.LosResult? Read(HexName far) => los.Check(FakeBoardProvider.Board, $"{Board}:{gun}:0", $"{Board}:{far}:0").Result;
            bool Attacked(HexName hex) => handle.HexFacts(hex)!.BaseLevel == level && Read(hex) is { IsBlocked: false, Hindrance: 0 };
            bool Passed(HexName hex) => handle.HexFacts(hex)!.BaseLevel != level || Read(hex) is { IsBlocked: true };
            foreach (var facing in Enum.GetValues<LimboDancer.Domains.Asl.Units.Documents.UnitFacing>())
            {
                var (left, right) = LimboDancer.Domains.Asl.Rules.ScenarioA1CanisterCalculator.ArcSides((int)facing);
                HexName? At(LimboDancer.Domains.Asl.Rules.CanisterOffset offset) => Orders(offset.Left, offset.Right, left, right)
                    .Select(order => order.Aggregate((HexName?)gun, (at, side) => at is { } hex ? handle.Neighbor(hex, (HexsideDirection)side) : null))
                    .FirstOrDefault(hex => hex is not null);
                foreach (var aim in LimboDancer.Domains.Asl.Rules.ScenarioA1CanisterCalculator.Aims.Where(item => item.Adjacent.Count == 1))
                {
                    HexName[] cluster = [.. aim.Cluster.Select(At).OfType<HexName>()];
                    HexName[] adjacent = [.. aim.Adjacent.Select(At).OfType<HexName>()];
                    if (!cluster.Any(Attacked) || !cluster.Concat(adjacent).All(hex => Attacked(hex) || Passed(hex)))
                    {
                        continue;
                    }

                    if (cluster.Where(hex => Attacked(hex) && Open(hex)).Select(hex => (HexName?)hex).FirstOrDefault() is { } inCluster)
                    {
                        yield return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{inCluster}:0", false);
                    }
                    else if (adjacent.Where(hex => Attacked(hex) && Open(hex)).Select(hex => (HexName?)hex).FirstOrDefault() is { } beside)
                    {
                        yield return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{beside}:0", true);
                    }
                }
            }
        }
    }

    [Fact]
    public void AGunThatListsCanisterAimsItAtAVertexOfItsCoveredArc()
    {
        // Pass 55 (C8.4, C8.41; rulings R55.8, R55.9): the American 37mm lists Canister, C7 in July 1943. The ammunition is chosen before any
        // target; choosing Canister puts the five aims of the Covered Arc in the target's place, and the shot names its aim.
        foreach (var spot in CanisterSpots())
        {
            using var attempt = new PlayPageOrdnanceTests();
            if (attempt.FiresCanister(spot))
            {
                return;
            }
        }

        Assert.Fail("The test board has no spot from which the gate accepts a Canister shot.");
    }

    /// <summary>The page fires Canister from the spot; false when the gate refuses the shot there, for a Hindrance the fire planner reads.</summary>
    private bool FiresCanister((string Gun, string Facing, int Aim, string Squad, bool Adjacent) spot)
    {
        var aimId = spot.Aim.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string>
        {
            ["board"] = Board, ["first"] = "american", ["second"] = "german", ["first-elr"] = "3", ["second-elr"] = "3", ["year"] = "1943", ["month"] = "7",
        });
        Place(page, "us-crew", "american-crew", spot.Gun);
        page.Find("#place-definition").Change("american-at-gun");
        page.Find("#place-facing").Change(spot.Facing);
        page.Find("#place-holder").Change("us-crew");
        Place(page, "us-gun", "american-at-gun", spot.Gun);
        Place(page, "g1", "attacker-squad", spot.Squad);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        page.Find("#ordnance-gun").Change("us-gun");
        Assert.Equal([("he", "HE"), ("canister", "Canister (Depletion Number 7)")], page.FindAll("#ordnance-ammunition option").Select(option => (option.GetAttribute("value"), option.TextContent)));
        Assert.Single(page.FindAll("#ordnance-target"));
        Assert.Empty(page.FindAll("#ordnance-canister-aim"));

        page.Find("#ordnance-ammunition").Change("canister");
        Assert.Empty(page.FindAll("#ordnance-target"));
        Assert.True(page.Find("#propose-ordnance").HasAttribute("disabled"));
        var facings = page.FindAll("#ordnance-canister-facing option");
        Assert.Equal(6, facings.Count);
        Assert.Equal((spot.Facing, $"{spot.Facing} (its Covered Arc now)"), (facings[0].GetAttribute("value"), facings[0].TextContent));
        var aim = Assert.Single(page.FindAll("#ordnance-canister-aim option"), option => option.GetAttribute("value") == aimId);
        Assert.StartsWith($"aim {aimId}: ", aim.TextContent, StringComparison.Ordinal);

        // The UI review: what the view sees there, in words; an aim shows no count of what Canister cannot harm or the view cannot see.
        Assert.Contains(DisplayText.Place(1, spot.Squad), aim.TextContent, StringComparison.Ordinal);
        Assert.EndsWith(": 1 unit in view", aim.TextContent, StringComparison.Ordinal);
        if (spot.Adjacent)
        {
            Assert.Contains($"and {DisplayText.Place(1, spot.Squad)} at half FP: ", aim.TextContent, StringComparison.Ordinal);
        }
        Assert.All(page.FindAll("#ordnance-canister-aim option").Where(option => option.GetAttribute("value") is { Length: > 0 }),
            shown => Assert.Matches(": (1 unit in view|no unit in view)$", shown.TextContent));
        Assert.DoesNotContain(Board, aim.TextContent, StringComparison.Ordinal);

        // A 2 and a 4: below the Depletion Number, the ROF kept; the squad passes whatever check the attack on its hex gives it.
        page.Find("#ordnance-canister-aim").Change(aimId);
        Assert.False(page.Find("#propose-ordnance").HasAttribute("disabled"));
        foreach (var die in new[] { 2, 4, 1, 2, 1, 2 })
        {
            dice.Enqueue(die);
        }

        page.Find("#propose-ordnance").Click();
        page.WaitForAssertion(() => Assert.Matches("Confirm to commit|Refused", page.Find("#play-outcome").TextContent));
        if (!page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal))
        {
            Assert.Contains("LOS Hindrance", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
            dice.Clear();
            return false;
        }

        var proposal = page.Find("#play-proposal").TextContent;
        Assert.Contains($"fires Canister at aim {aimId} of the CA facing {spot.Facing}", proposal, StringComparison.Ordinal);
        Assert.Contains("he same DR is the check of the Depletion Number 7", proposal, StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));

        // What the page sent, read from the committed game.
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        var fired = history.Events.Select(item => item.Payload).OfType<OrdnanceFired>().Single();
        Assert.Equal(("canister", spot.Aim, spot.Facing), (fired.Facts.GetProperty("ammunition").GetString(), fired.Facts.GetProperty("canister").GetProperty("aim").GetInt32(),
            fired.Facts.GetProperty("canister").GetProperty("facing").GetString()));
        Assert.Null(fired.Facing);
        Assert.Equal("used", fired.Resolution.GetProperty("ammunitionUse").GetString());
        var fires = history.Events.Select(item => item.Payload).OfType<FireResolved>().ToArray();
        var near = Assert.Single(fires, fire => fire.TargetLocation == spot.Squad);
        Assert.Equal((fired.Rolls["canister"], spot.Adjacent ? 6m : 12m), (near.Rolls["attack"], near.Resolution.GetProperty("arithmetic").GetProperty("totalFirepower").GetDecimal()));
        Assert.Equal(spot.Adjacent, near.Facts.GetProperty("canister").TryGetProperty("adjacentHexes", out _));

        // C8.41 (the referee's review): a Location of the cluster is attacked whenever the adjacent hex is.
        Assert.Contains(fires, fire => !fire.Facts.GetProperty("canister").TryGetProperty("adjacentHexes", out _));
        Assert.All(fires, fire => Assert.Equal(fired.Rolls["canister"], fire.Rolls["attack"]));

        var record = page.Find("#play-ordnance .ordnance-record").TextContent;
        Assert.Contains($"fires Canister at aim {aimId} of the CA facing {spot.Facing}: no To Hit DR; IFT DR 2 (colored), 4 = 6", record, StringComparison.Ordinal);
        Assert.Contains("the DR is below the Depletion Number 7: Canister fired (C8.9)", record, StringComparison.Ordinal);
        Assert.Contains("it may fire again this phase (C2.24)", record, StringComparison.Ordinal);

        // The table player's review: a Location of the aim where the view knows of no unit says that, and not that it was fire at a
        // concealed target with a result on nobody.
        if (fires.Any(fire => fire.Facts.GetProperty("targets").GetArrayLength() == 0))
        {
            Assert.Contains(page.FindAll(".fire-record .fire-no-target"), line => line.TextContent.StartsWith("No Known unit in [", StringComparison.Ordinal)
                && line.TextContent.EndsWith(": no effect to show.", StringComparison.Ordinal));
            Assert.DoesNotContain("concealed target", page.Markup, StringComparison.Ordinal);
        }

        return true;
    }
}
