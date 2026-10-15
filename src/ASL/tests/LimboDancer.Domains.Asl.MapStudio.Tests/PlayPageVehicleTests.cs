using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Play page for unit step 25 (U29): a vehicle is placed with its VCA, the Infantry move list leaves vehicles out, the vehicle
/// movement panel spends one MP expenditure at a time, the crew exposure panel offers the AFV's BU counter, and the units table shows
/// a vehicle's VCA, MP, and CE state. The verified synthetic board and the Studio's scripted dice.
/// </summary>
public sealed class PlayPageVehicleTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-vehicles-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPageVehicleTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
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

    private static string Board => FakeBoardProvider.Board.Ref.Value;

    /// <summary>Two ADJACENT Open Ground hexes at one level, with no hexside terrain.</summary>
    private (string One, string Two) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool Terrain(HexName? hex, string name, int level) => hex is { } at && handle.HexFacts(at) is { Center.Terrain.Name: { } terrain } facts && terrain == name && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let one = handle.Geometry.NameOf(index)
                let level = handle.HexFacts(one)!.BaseLevel
                where Terrain(one, "Open Ground", level)
                from side in Enum.GetValues<HexsideDirection>()
                let two = handle.Neighbor(one, side)
                where Terrain(two, "Open Ground", level) && handle.HexFacts(one)!.Hexsides.All(item => item.HexsideTerrain is null && !item.Cliff)
                select ($"{Board}:{one}:0", $"{Board}:{two}:0")).First();
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string facing)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-facing").Change(facing);
        page.Find("#place-add").Click();
    }

    [Fact]
    public void AVehicleIsPlacedWithItsVcaAndMovesByItsOwnPanel()
    {
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });

        // D2.11: the setup offers a VCA for a vehicle.
        page.Find("#place-definition").Change("attacker-truck");
        Assert.Contains("VCA", page.Find("#place-facing").ParentElement!.TextContent, StringComparison.Ordinal);
        Place(page, "de-t", "attacker-truck", one, "east");
        Place(page, "de-ht", "attacker-halftrack", two, "west");
        // Pass 30: a row's VCA is changed in the row.
        Assert.Equal("west", page.Find("#place-row-1-facing").GetAttribute("value"));
        Commit(page, "#propose-setup");

        // The units table shows each vehicle's VCA, and the OT halftrack's crew as CE (D5.3).
        Assert.Contains("VCA east", page.Find("tr[data-unit='de-t']").TextContent, StringComparison.Ordinal);
        Assert.Contains("CE", page.Find("tr[data-unit='de-ht']").TextContent, StringComparison.Ordinal);

        // In the PFPh the halftrack's AAMG may fire; the unarmed truck may not (D5.1).
        page.EndPhase(Commit);
        var firing = page.FindAll("#fire-from option").Select(item => item.GetAttribute("value")).ToArray();
        Assert.Contains(two, firing);
        Assert.DoesNotContain(one, firing);
        page.EndPhase(Commit);
        Assert.Empty(page.FindAll(".move-unit"));
        Assert.Equal(["", "de-ht", "de-t"], page.FindAll("#vehicle-unit option").Select(item => item.GetAttribute("value")).Order(StringComparer.Ordinal));
        Assert.Single(page.FindAll(".bu-toggle[data-vehicle='de-ht']"));

        // D2.12: Start costs 1 MP; the DEFENDER's window opens.
        page.Find("#vehicle-unit").Change("de-t");
        Assert.Contains("0 of 28 MP spent, 28 left", page.Find("#vehicle-state").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#vehicle-stop"));
        Commit(page, "#vehicle-start");
        Assert.Empty(page.FindAll("#vehicle-start"));
        Assert.Single(page.FindAll("#vehicle-stop"));
        Assert.Equal("open", page.Find("#move-state").GetAttribute("data-window"));
        Assert.Contains("1 of 28 MP spent, 27 left, moving", page.Find("#vehicle-state").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(page.FindAll(".vehicle-enter"));
    }

    [Fact]
    public void AnArmedAfvSetsUpWithAnArmorLeaderOrAnInexperiencedCrew()
    {
        // Pass 55 (D3.4, D3.41, D3.45; rulings R55.15, R55.18): the setup offers both for an armed AFV alone, and the committed game carries them.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        foreach (var unarmed in new[] { "attacker-squad", "attacker-truck" })
        {
            page.Find("#place-definition").Change(unarmed);
            Assert.Empty(page.FindAll("#place-armor-leader"));
            Assert.Empty(page.FindAll("#place-inexperienced-crew"));
        }

        page.Find("#place-definition").Change("attacker-tank");
        Assert.Equal(["", "8-1", "9-1", "9-2", "10-2"], page.FindAll("#place-armor-leader option").Select(item => item.GetAttribute("value")));
        page.Find("#place-armor-leader").Change("9-1");
        Place(page, "de-tank", "attacker-tank", one, "east");
        page.Find("#place-definition").Change("defender-tank");
        page.Find("#place-inexperienced-crew").Change(true);
        Place(page, "ru-tank", "defender-tank", two, "west");
        Commit(page, "#propose-setup");

        // What the page sent, read from the committed game.
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        var created = history.Events.Select(item => item.Payload).OfType<InstanceCreated>().ToDictionary(item => item.Instance.Id, item => item.Instance, StringComparer.Ordinal);
        Assert.Equal(new ArmorLeader(9, -1), created["de-tank"].ArmorLeader);
        Assert.False(created["de-tank"].Conditions.ContainsKey(Conditions.InexperiencedCrew));
        Assert.Null(created["ru-tank"].ArmorLeader);
        Assert.Equal(ConditionState.True, created["ru-tank"].Conditions[Conditions.InexperiencedCrew]);
        Assert.Equal(new ArmorLeader(9, -1), history.Current!.Unit("de-tank")!.ArmorLeader);

        // The units table names him, and the Inexperienced Crew, which is no secret.
        Assert.Contains("with Armor Leader 9-1", page.Find("tr[data-unit='de-tank']").TextContent, StringComparison.Ordinal);
        Assert.Contains("Inexperienced Crew (D3.45)", page.Find("tr[data-unit='ru-tank']").TextContent, StringComparison.Ordinal);
    }

    /// <summary>
    /// A Location for a watching vehicle, a Location it does not see, and an Open Ground Location next to that one which it does see: a unit that
    /// moves from the second to the third enters the watcher's LOS in its MPh (D2.401).
    /// </summary>
    private (string Watcher, string Start, string Entered) IntoTheLos()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        var map = LosMap.ForBoard(handle).Map!;
        HexName[] hexes = [.. handle.Geometry.Hexes().Select(handle.Geometry.NameOf)];
        bool Open(HexName hex) => handle.HexFacts(hex) is { Center.Terrain.Name: "Open Ground", BaseLevel: 0 };
        bool Sees(HexName one, HexName two) => LosCalculator.Check(map, new BoardLocation(FakeBoardProvider.Board.Ref, one, 0), new BoardLocation(FakeBoardProvider.Board.Ref, two, 0)) is { Status: LosStatus.Clear };
        return (from watcher in hexes
                where Open(watcher)
                from start in hexes
                where start != watcher && !Sees(watcher, start)
                from side in Enum.GetValues<HexsideDirection>()
                let next = handle.Neighbor(start, side)
                where next is not null
                let entered = next.Value
                where entered != watcher && Open(entered) && Sees(watcher, entered)
                select ($"{Board}:{watcher}:0", $"{Board}:{start}:0", $"{Board}:{entered}:0")).First();
    }

    [Fact]
    public void TheDefendersViewOffersItsVehicleAMotionAttempt()
    {
        // Pass 55 (D2.401; rulings R55.11 to R55.13): a German squad moves out of a Location the T-34 does not see into one it does. The
        // Russian view lists the T-34 with its chance, and proposes the attempt with the VCA and TCA it takes on success; the German view does not.
        var (watcher, start, entered) = IntoTheLos();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        page.Find("#place-definition").Change("attacker-squad");
        page.Find("#place-id").Change("g1");
        page.Find("#place-location").Change(start);
        page.Find("#place-add").Click();
        Place(page, "ru-tank", "defender-tank", watcher, "west");
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        Assert.Empty(page.FindAll("#motion-attempt"));
        page.Find(".move-unit[data-unit='g1']").Change(true);
        page.Find("#move-to").Change(entered);
        Commit(page, "#propose-move");
        Assert.Empty(page.FindAll("#motion-attempt"));

        // The MF the entry cost on this board, all of it spent in the T-34's LOS.
        var spent = Assert.Single(live.History("village")!.Events.Select(item => item.Payload).OfType<MovementStepped>()).HalfMf;
        Assert.True(spent >= 2 && spent % 2 == 0);
        var mf = (spent / 2).ToString(System.Globalization.CultureInfo.InvariantCulture);

        page.ViewAs("russian");
        var option = Assert.Single(page.FindAll("#motion-vehicle option"));
        Assert.Equal("ru-tank", option.GetAttribute("value"));
        Assert.False(option.HasAttribute("disabled"));
        Assert.Contains($"{mf} MF in its LOS: succeeds on a dr of {mf} or less", option.TextContent, StringComparison.Ordinal);
        page.Find("#motion-facing").Change("north-east");
        page.Find("#motion-turret").Change("east");
        dice.Enqueue([1, 3, 4]);
        Commit(page, "#propose-motion-attempt");

        // What the page sent, read from the committed game: the dr of 1 passes, and the T-34's Mechanical Reliability DR of 7 is no breakdown.
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        var attempt = Assert.Single(history.Events.Select(item => item.Payload).OfType<MotionAttempted>());
        Assert.Equal(("ru-tank", spent, MotionAttempted.Passed, "g1", (UnitFacing?)UnitFacing.NorthEast, (UnitFacing?)UnitFacing.East),
            (attempt.Vehicle, attempt.HalfPoints, attempt.Result, attempt.Mover, attempt.Facing, attempt.Turret));
        Assert.Equal(VehicleCheckRolled.Mechanical, Assert.Single(history.Events.Select(item => item.Payload).OfType<VehicleCheckRolled>()).Check);
        Assert.Equal(ConditionState.True, GameState.Condition(history.Current!.Unit("ru-tank")!, Conditions.Motion));
        Assert.Equal(UnitFacing.NorthEast, (history.Current.Unit("ru-tank")!.Position as MapPosition)?.Facing);

        // Its one attempt of this enemy MPh is made: it is listed with the reason, and the record says what happened. The DEFENDER may still pass.
        var made = Assert.Single(page.FindAll("#motion-vehicle option"));
        Assert.True(made.HasAttribute("disabled"));

        // The table player's review: the option names its vehicle once and says the bar in a few words; the whole sentence is its title.
        Assert.EndsWith(": it has made its attempt", made.TextContent.Trim(), StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(made.TextContent, "R1"));
        Assert.Contains("only once", made.GetAttribute("title"), StringComparison.Ordinal);
        Assert.DoesNotContain("Vehicle vehicle", page.Find("#motion-attempt").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#propose-motion-attempt"));

        // The record says who spent the MF.
        Assert.Contains(" attempts Motion as ", page.Markup, StringComparison.Ordinal);
        Assert.Contains($" has spent {mf} MF in its LOS, dr 1: it gains Motion and turns its VCA to north-east and its TCA to east (D2.401)", page.Markup, StringComparison.Ordinal);
        Assert.Single(page.FindAll("#propose-pass"));
    }

    /// <summary>
    /// The facing, one hexspine clockwise of the bearing from one ADJACENT hex to the other, whose Covered Arc has the second hex on its edge
    /// (C3.2), read from the grid as the planner reads a bearing: flat-topped hexes in columns, the odd columns half a hex higher.
    /// </summary>
    private string FacingToward(string from, string to)
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        (double X, double Y) Center(string location)
        {
            Assert.True(handle.Geometry.TryGetIndex(BoardLocation.Parse(location).Hex, out var hex));
            return (1.5 * hex.Column, Math.Sqrt(3) * (hex.Row - (0.5 * (hex.Column % 2))));
        }

        var (one, two) = (Center(from), Center(to));
        var bearing = Math.Atan2(-(two.Y - one.Y), two.X - one.X) * 180 / Math.PI;
        var hexspine = ((((int)Math.Round((bearing - 30) / 60) * 60) % 360) + 360) % 360;
        return hexspine switch
        {
            0 => "east",
            60 => "north-east",
            120 => "north-west",
            180 => "west",
            240 => "south-west",
            _ => "south-east",
        };
    }

    [Fact]
    public void AShotDeclaredAtAMovingTankOffersItsSideAGunDuel()
    {
        // Pass 55 (C2.2401; rulings R55.19, R55.20, R55.29): the AT Gun in the open, manned by a squad and turned away from the ADJACENT tank,
        // must turn two hexspines: Case A +4 and Case H +2. The PzKpfw IIIH that has just spent its Start MP would fire with Case C +4 and
        // Case I +1; its Armor Leader 9-2 is not yet revealed (D3.41), so he is not counted and his line is shown nowhere. The Russian shot is
        // declared and not rolled; the German view is offered the duel, and its answer resolves both shots in order.
        var (west, east) = Hexes();
        var facing = FacingToward(west, east);
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3", ["year"] = "1942" });
        page.Find("#place-definition").Change("attacker-tank");
        page.Find("#place-armor-leader").Change("9-2");

        // The UI review of pass 55 (D3.45): an Armor Leader and an Inexperienced Crew exclude each other where they are chosen, with the reason.
        var inexperienced = page.Find("#place-inexperienced-crew");
        Assert.True(inexperienced.HasAttribute("disabled"));
        Assert.Equal("an Inexperienced Crew is its own 6+1 Armor Leader (D3.45)", inexperienced.GetAttribute("title"));
        page.Find("#place-armor-leader").Change(string.Empty);
        Assert.False(page.Find("#place-inexperienced-crew").HasAttribute("disabled"));
        page.Find("#place-inexperienced-crew").Change(true);
        Assert.True(page.Find("#place-armor-leader").HasAttribute("disabled"));
        Assert.Equal("an Inexperienced Crew is its own 6+1 Armor Leader (D3.45)", page.Find("#place-armor-leader").GetAttribute("title"));
        page.Find("#place-inexperienced-crew").Change(false);
        page.Find("#place-armor-leader").Change("9-2");
        Place(page, "de-tank", "attacker-tank", west, facing);
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("ru-crew");
        page.Find("#place-location").Change(east);
        page.Find("#place-add").Click();
        page.Find("#place-definition").Change("defender-at-gun");
        page.Find("#place-id").Change("ru-gun");
        page.Find("#place-location").Change(east);
        page.Find("#place-holder").Change("ru-crew");
        page.Find("#place-facing").Change(facing);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        page.Find("#vehicle-unit").Change("de-tank");
        Commit(page, "#vehicle-start");

        // The DEFENDER declares: the proposal says what the commit will be, and no dice are drawn.
        page.ViewAs("russian");
        page.Find("#ordnance-gun").Change("ru-gun");
        page.Find("#ordnance-target").Change(west);
        page.Find("#ordnance-vehicle").Change("de-tank");
        page.Find("#propose-ordnance").Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), page.Find("#play-proposal").TextContent));
        Assert.Contains("answer with a Gun Duel (C2.2401)", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Assert.Contains("no dice are rolled until it answers", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Armor Leader", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var history = live.History("village")!;
        Assert.False(history.HasErrors);
        Assert.Empty(history.Events.Select(item => item.Payload).OfType<OrdnanceFired>());
        Assert.Equal(ChoicePending.GunDuel, history.Current!.Choice!.Kind);

        // The Russian view reads that its shot waits for the German answer, with both Gun Duel DRM.
        var waiting = page.Find("#play-choice");
        Assert.StartsWith("gun-duel:ru-gun:de-tank:", waiting.GetAttribute("data-key"), StringComparison.Ordinal);
        Assert.Contains("has declared a shot at", waiting.TextContent, StringComparison.Ordinal);
        Assert.Contains("+5 (Case C +4, Case I +1)", waiting.TextContent, StringComparison.Ordinal);
        Assert.Contains("+6 (Case A +4, Case H +2)", waiting.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Armor Leader", waiting.TextContent, StringComparison.Ordinal);

        // The table player's review: one sentence with its rule once, ended by the ruling every pending choice is under.
        Assert.Contains("which may fire its MA first in a Gun Duel (C2.2401); Gun Duel DRM: ", waiting.TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("(C2.2401) (ruling R5.8)", waiting.TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-choice .choose"));
        Assert.Contains("Waiting for the German side", waiting.TextContent, StringComparison.Ordinal);

        // The German view answers.
        page.ViewAs("german");
        Assert.Contains("which may fire its MA first in a Gun Duel (C2.2401)", page.Markup, StringComparison.Ordinal);
        var options = page.FindAll("#play-choice .choose");
        Assert.Equal(["duel", "decline"], options.Select(item => item.GetAttribute("data-option")));
        Assert.Equal(["Propose: fire first (Gun Duel)", "Propose: decline: the declared shot resolves"], options.Select(item => item.TextContent.Trim()));
        dice.Enqueue([5, 6, 5, 6]);
        Commit(page, "#play-choice .choose[data-option='duel']");
        Assert.Empty(page.FindAll("#play-choice"));

        // What the page sent, read from the committed game: the tank's shot, named a Gun Duel's first, then the Gun's.
        history = live.History("village")!;
        Assert.False(history.HasErrors);
        var shots = history.Events.Select(item => item.Payload).OfType<OrdnanceFired>().ToArray();
        Assert.Equal(["de-tank", "ru-gun"], shots.Select(item => item.Gun));
        Assert.True(shots[0].Facts.GetProperty("gunDuel").GetBoolean());
        Assert.False(shots[1].Facts.TryGetProperty("gunDuel", out _));
        Assert.Null(history.Current!.GunDuel);
        page.ViewAs(Perspective.AdjudicatorName);
        var records = page.FindAll("#play-ordnance .ordnance-record").Select(item => item.TextContent).ToArray();
        Assert.Contains(records, record => record.Contains(", first in a Gun Duel (C2.2401)", StringComparison.Ordinal));
        Assert.Contains(records, record => record.Contains("which may fire its MA first in a Gun Duel (C2.2401)", StringComparison.Ordinal));
    }

    [Fact]
    public void TheMovingHalftrackOffersBoundingFirstFireOnceTheDefenderPasses()
    {
        // D3.3 (ruling R6.9): after the DEFENDER passes on its Start, the halftrack's panel offers Bounding First Fire at a Location the
        // Germans can see.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        Place(page, "de-ht", "attacker-halftrack", one, "east");
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("r1");
        page.Find("#place-location").Change(two);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        page.Find("#vehicle-unit").Change("de-ht");
        Commit(page, "#vehicle-start");
        Assert.Empty(page.FindAll("#vehicle-bff"));
        page.PassAs("russian", Commit);
        Assert.Contains(two, page.FindAll("#vehicle-bff-target option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public void ATankIsOfferedInTheGunPanelWithTheEnemyVehicleAndItsAmmunition()
    {
        // D1.3, C3.31 (rulings R7.2, R7.10): in the PFPh the German tank is offered as a Gun, labeled with its VCA, TCA, and BU crew; a target
        // Location holding the T-34 offers it on the Vehicle Target Type with a choice of ammunition. The year setting is offered at setup.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3", ["year"] = "1942" });
        Place(page, "de-tank", "attacker-tank", one, "east");
        Place(page, "ru-tank", "defender-tank", two, "west");
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        var gun = page.FindAll("#ordnance-gun option").Single(item => item.GetAttribute("value") == "de-tank");
        Assert.Contains("VCA east, TCA east, BU", gun.TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#ordnance-vehicle"));
        page.Find("#ordnance-gun").Change("de-tank");
        page.Find("#ordnance-target").Change(two);
        Assert.Contains("ru-tank", page.FindAll("#ordnance-vehicle option").Select(item => item.GetAttribute("value")));

        // The UI review of pass 55: the list is the one the shot will name. With no vehicle chosen the shot is at the Location's Infantry: HE first,
        // then AP and APCR by their HE Equivalency (C8.31, pass 60), the note giving each one's FP; with the T-34 chosen it is the vehicle's list.
        var infantry = page.FindAll("#ordnance-ammunition option").Select(item => item.GetAttribute("value")).ToArray();
        Assert.Equal("he", infantry[0]);
        Assert.Contains("ap", infantry);
        Assert.Contains("AP: 2 FP by HE Equivalency, C8.31", page.Find("#ordnance-ammunition-note").TextContent, StringComparison.Ordinal);
        page.Find("#ordnance-vehicle").Change("ru-tank");

        // C8.1: the PzKpfw IIIH lists APCR and no HEAT.
        Assert.Equal(["ap", "apcr", "he"], page.FindAll("#ordnance-ammunition option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public void TheVehiclePanelOffersReverseAnOvrItsResolutionAndCcReactionFire()
    {
        // Backlog pass 11 (rulings R11.1, R11.11, R11.13): the tank may Start in Reverse; entering the Russian squad's hex with an OVR puts the OVR's
        // resolution on the panel once the DEFENDER passes, after which the squad is offered CC Reaction Fire.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        Place(page, "de-tank", "attacker-tank", one, "east");
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("r1");
        page.Find("#place-location").Change(two);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        page.Find("#vehicle-unit").Change("de-tank");
        Assert.Single(page.FindAll("#vehicle-start-reverse"));
        Commit(page, "#vehicle-start");
        page.PassAs("russian", Commit);
        Assert.Single(page.FindAll("#vehicle-esb"));
        if (page.FindAll($".vehicle-enter[data-to='{two}']").FirstOrDefault(item => item.GetAttribute("data-bypass") == "False") is not { } enter
            || enter.HasAttribute("disabled"))
        {
            // The two hexes are not in the tank's VCA from this placement; the OVR control is still offered.
            Assert.Single(page.FindAll("#vehicle-ovr"));
            return;
        }

        page.Find("#vehicle-ovr").Change(true);
        Commit(page, $".vehicle-enter[data-to='{two}'][data-bypass='False']");
        Assert.True(page.Find("#vehicle-resolve-ovr").HasAttribute("disabled"));
        page.PassAs("russian", Commit);
        Assert.False(page.Find("#vehicle-resolve-ovr").HasAttribute("disabled"));
        dice.Enqueue([6, 6, 1, 1, 1, 1, 1]);
        Commit(page, "#vehicle-resolve-ovr");

        // Pass 26: data-bypass now renders False, so the test reaches here; the CC Reaction Fire panel is the DEFENDER's since pass 25.
        page.ViewAs("russian");
        Assert.Contains("r1", page.FindAll("#reaction-attacker option").Select(item => item.GetAttribute("value")));
    }

    [Fact]
    public void TheGunPanelListsTargetsWithTheirCoveredArcAndOffersTurningIntensiveFireAndPushing()
    {
        // R8.2, R8.6, R8.9: in the PFPh the leIG's panel reads each enemy Location against its CA, offers Intensive Fire and a turn without
        // firing, and draws its CA; in the MPh its crew may push it.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3", ["defender"] = "russian" });
        page.Find("#place-definition").Change("attacker-crew");
        page.Find("#place-id").Change("de-crew");
        page.Find("#place-location").Change(one);
        page.Find("#place-add").Click();
        page.Find("#place-definition").Change("attacker-inf-gun");
        Assert.Single(page.FindAll("#place-bore"));
        page.Find("#place-id").Change("de-gun");
        page.Find("#place-location").Change(one);
        page.Find("#place-holder").Change("de-crew");
        page.Find("#place-facing").Change("east");
        page.Find("#place-add").Click();
        page.Find("#place-definition").Change("defender-squad");
        page.Find("#place-id").Change("r1");
        page.Find("#place-location").Change(two);
        page.Find("#place-add").Click();
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.Find("#ordnance-gun").Change("de-gun");
        Assert.Contains("range 1", page.Find($"#ordnance-arc li[data-at='{two}']").TextContent, StringComparison.Ordinal);
        Assert.Single(page.FindAll("#ordnance-intensive"));
        Assert.Single(page.FindAll("#propose-turn-gun"));
        page.WaitForAssertion(() => Assert.Contains("layer-covered-arc", context.MapLayer("setMarks"), StringComparison.Ordinal));
        page.EndPhase(Commit);
        page.Find(".move-unit[data-unit='de-crew']").Change(true);
        Assert.Single(page.FindAll("#move-push"));
    }

    [Fact]
    public void AVehicleCloseCombatAttackNamesOnlyItsOwnLocationsUnits()
    {
        // Pass 35 (found in the game p35-rst-cc): the Locations share one attack draft. A defender checked in one Location was sent with the next
        // Location's attack, which was refused as outside the review until the page was reloaded. Each Location now reads and proposes only its own
        // units, and a committed attack clears the draft.
        var (one, two) = Hexes();
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        foreach (var (id, at) in new[] { ("g1", one), ("g2", two) })
        {
            page.Find("#place-definition").Change("attacker-squad");
            page.Find("#place-id").Change(id);
            page.Find("#place-location").Change(at);
            page.Find("#place-add").Click();
        }

        Commit(page, "#propose-setup");

        // The setup refuses a vehicle beside an enemy unit, so the two T-34s and the phases to the CCPh are written as the store writes them.
        var scope = new GameScope(LivePlay.Tenant, "village");
        var existing = live.Store.Read(scope)!.Events;
        var clear = new Dictionary<string, ConditionState> { [Conditions.Concealed] = ConditionState.False, [Conditions.Hidden] = ConditionState.False };
        (string Type, EventPayload Payload)[] batch = [
            ("instance-created", new InstanceCreated(new NewInstance("t1", "asl:vehicle", "defender-tank", "russian", new MapPosition(BoardLocation.Parse(one), Facing: UnitFacing.East), null, clear))),
            ("instance-created", new InstanceCreated(new NewInstance("t2", "asl:vehicle", "defender-tank", "russian", new MapPosition(BoardLocation.Parse(two), Facing: UnitFacing.East), null, clear))),
            .. PhasesToCloseCombat.Select(phase => ("phase-changed", (EventPayload)new PhaseChanged(1, phase, "german")))];
        GameEvent[] events = [.. batch.Select((item, index) => new GameEvent(scope, $"staged-{index + 1}", existing.Count + index + 1, DateTimeOffset.UnixEpoch,
            LiveGames.Source, item.Type, item.Payload, null, [], null))];
        Assert.Equal(AppendStatus.Committed, live.Store.Append(scope, "Village", existing.Count, events, live.Planner.Replay).Status);
        page.OpenGame(string.Empty);
        page.OpenGame("village");

        // The adjudicator's view acts for both sides and keeps its draft from one Location to the next. A11.31, A11.12: the Infantry side attacks
        // first, one Location at a time; the Germans pass in the first, which is then the Russian side's.
        page.ViewAs(Perspective.AdjudicatorName);
        string At(string location, string control) => $".vehicle-cc[data-location='{location}'] {control}";
        Assert.Equal(2, page.FindAll(".vehicle-cc").Count);
        Commit(page, At(one, ".propose-vehicle-cc-pass"));

        // t1 and g1 are chosen in the first Location: the second reads no vehicle of its own and offers no attack.
        page.Find(At(one, ".vehicle-cc-vehicle")).Change("t1");
        page.Find(At(one, ".vehicle-cc-defender")).Change(true);
        Assert.False(page.Find(At(one, ".propose-vehicle-cc-vehicle")).HasAttribute("disabled"));
        Assert.Equal(string.Empty, page.Find(At(two, ".vehicle-cc-vehicle")).GetAttribute("value") ?? string.Empty);
        Assert.True(page.Find(At(two, ".propose-vehicle-cc-vehicle")).HasAttribute("disabled"));

        // The UI review, pass 35: a draft made and not proposed does not outlive its view. After another view and back, nothing is chosen or
        // checked in the first Location and its attack waits; this fails if the view's reset no longer clears the draft.
        page.ViewAs("german");
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Equal(string.Empty, page.Find(At(one, ".vehicle-cc-vehicle")).GetAttribute("value") ?? string.Empty);
        Assert.False(page.Find(At(one, ".vehicle-cc-defender")).HasAttribute("checked"));
        Assert.True(page.Find(At(one, ".propose-vehicle-cc-vehicle")).HasAttribute("disabled"));
        page.Find(At(one, ".vehicle-cc-vehicle")).Change("t1");
        page.Find(At(one, ".vehicle-cc-defender")).Change(true);
        dice.Enqueue([6, 6]);
        Commit(page, At(one, ".propose-vehicle-cc-vehicle"));
        Assert.Equal(["g1"], VehicleAttacks(scope).Last().Defenders);

        // The committed attack clears the draft: in the second Location nothing is chosen or checked, and its attack names g2 alone.
        Commit(page, At(two, ".propose-vehicle-cc-pass"));
        Assert.False(page.Find(At(two, ".vehicle-cc-defender")).HasAttribute("checked"));
        Assert.True(page.Find(At(two, ".propose-vehicle-cc-vehicle")).HasAttribute("disabled"));
        page.Find(At(two, ".vehicle-cc-vehicle")).Change("t2");
        page.Find(At(two, ".vehicle-cc-defender")).Change(true);
        dice.Enqueue([6, 6]);
        Commit(page, At(two, ".propose-vehicle-cc-vehicle"));
        Assert.Equal(["g2"], VehicleAttacks(scope).Last().Defenders);
        Assert.Equal(2, VehicleAttacks(scope).Count());
        Assert.False(live.History("village")!.HasErrors);
    }

    private static readonly string[] PhasesToCloseCombat = ["pfph", "mph", "dfph", "afph", "rtph", "aph", "ccph"];

    private IEnumerable<VehicleCloseCombatResolved> VehicleAttacks(GameScope scope) => live.Store.Read(scope)!.Events.Select(item => item.Payload).OfType<VehicleCloseCombatResolved>();
}
