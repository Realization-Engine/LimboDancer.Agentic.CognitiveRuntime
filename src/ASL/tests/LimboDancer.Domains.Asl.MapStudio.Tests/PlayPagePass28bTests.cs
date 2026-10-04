using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 28b: the Play panels it extracted, in use on the page. The Deploy control gives several SW to the second HS, and the records list shows DM
/// gained, a Failure to Rout elimination, and a SW transfer (backlog section 23); choices are cleared when the squad or view changes (section 15.3).
/// </summary>
public sealed class PlayPagePass28bTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-28b-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass28bTests()
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

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, string? holder = null)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        if (holder is not null)
        {
            page.Find("#place-holder").Change(holder);
        }

        page.Find("#place-add").Click();
    }

    private IRenderedComponent<PlayPage> NewGame()
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = "german", ["second"] = "russian", ["first-elr"] = "3", ["second-elr"] = "3" });
        return page;
    }

    private GameState Current => live.History("village")!.Current!;

    private static string HolderOf(GameState state, string weapon) => ((EquipmentInstance)state.Find(weapon)!).Holding!.Holder;

    // Backlog section 23 (A1.31), built in pass 28b: each SW checked goes to the second HS, the rest stay with the first.
    [Fact]
    public void DeployGivesEachCheckedSwToTheSecondHs()
    {
        var hexes = Hexes();
        var page = NewGame();
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "gl", "attacker-leader-9-1", hexes.One);
        Place(page, "gm1", "attacker-lmg", hexes.One, holder: "g1");
        Place(page, "gm2", "attacker-mmg", hexes.One, holder: "g1");
        Place(page, "gm3", "attacker-lmg", hexes.One, holder: "g1");
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        Assert.Equal("rph", Current.Phase);

        page.Find("#deploy-squad").Change("g1");
        Assert.Equal(["gm1", "gm2", "gm3"], page.FindAll(".deploy-weapon").Select(item => item.GetAttribute("data-weapon")));
        page.Find(".deploy-weapon[data-weapon='gm2']").Change(true);
        page.Find(".deploy-weapon[data-weapon='gm3']").Change(true);

        // Section 15.3: another squad clears the leader and the SW; choosing the squad again starts with none checked.
        page.Find("#deploy-squad").Change(string.Empty);
        Assert.Empty(page.FindAll("#deploy-weapons"));
        page.Find("#deploy-squad").Change("g1");
        Assert.Empty(page.FindAll(".deploy-weapon:checked"));

        page.Find("#deploy-leader").Change("gl");
        page.Find(".deploy-weapon[data-weapon='gm2']").Change(true);
        page.Find(".deploy-weapon[data-weapon='gm3']").Change(true);
        dice.Enqueue([1, 1]);
        Commit(page, "#propose-deploy");

        var state = Current;
        var halves = state.Units.Where(unit => unit.Status == InstanceStatus.Active && unit.Kind == "asl:half-squad").Select(unit => unit.Id).Order(StringComparer.Ordinal).ToArray();
        Assert.Equal(2, halves.Length);
        Assert.Equal(halves[0], HolderOf(state, "gm1"));
        Assert.Equal(halves[1], HolderOf(state, "gm2"));
        Assert.Equal(halves[1], HolderOf(state, "gm3"));
        Assert.Equal(string.Empty, page.Find("#deploy-squad").GetAttribute("value"));

        // Table player, pass 28b: the record says which HS took which SW. Pass 31c: each HS by its name and tag, each SW by its counter.
        var deployed = page.Find("#play-rallies .lineage-record").RecordText();
        Assert.Equal($"4-6-7 squad G1 in {DisplayText.Place(1, hexes.One)} becomes the HS 2-4-7 half-squad G1a and 2-4-7 half-squad G1b (A1.31); 2-4-7 half-squad G1a takes the LMG; 2-4-7 half-squad G1b takes the MMG, the LMG",
            deployed);
        Assert.DoesNotContain(halves[0], deployed, StringComparison.Ordinal);
    }

    // Backlog section 23 (A4.431, A10.62), built in pass 28b: a SW transfer and DM gained are records, for each side.
    [Fact]
    public void TheRecordsShowATransferAndDmGained()
    {
        var hexes = Hexes();
        var page = NewGame();
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "g2", "attacker-squad", hexes.One);
        Place(page, "gm", "attacker-lmg", hexes.One, holder: "g1");
        page.Find("#place-broken").Change(true);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");

        page.Find("#sw-weapon").Change("gm");
        page.Find("#sw-unit").Change("g2");
        Commit(page, "#propose-transfer");
        // Pass 31c: a record names each unit and says its hex; the SW reads by its counter.
        var transfer = page.Find("#play-rallies .transfer-record").RecordText();
        Assert.Equal($"4-6-7 squad G1 passes the LMG to 4-6-7 squad G2 in {DisplayText.Place(1, hexes.One)} (A4.431)", transfer);

        // A10.62: the transfer leaves the broken r1 ADJACENT to g1 and g2, so it comes under DM; its owner keeps it as the RPh ends (the EXC), which is
        // recorded as kept, not gained (referee, pass 28b).
        var gained = $"4-4-7 squad R1 in {DisplayText.Place(1, hexes.Two)} comes under DM (A10.62)";
        Assert.Equal(gained, page.Find("#play-rallies .dm-record").RecordText());
        // Pass 31 (ruling R31.6): r1's owner keeps its DM, from its own view, and either side ends the RPh.
        page.ViewAs("russian");
        page.Find(".retain-dm[data-unit='r1']").Change(true);
        page.EndPhase(Commit);
        page.ViewAs("german");
        var kept = page.Find("#play-rallies li").RecordText();
        Assert.Equal($"4-4-7 squad R1 in {DisplayText.Place(1, hexes.Two)} keeps DM as the RPh ends (A10.62)", kept);
        for (var phase = 0; phase < 4; phase++)
        {
            page.EndPhase(Commit);
        }

        Assert.Equal("rtph", Current.Phase);
        Assert.Contains(page.FindAll("#play-rallies .dm-record"), item => item.RecordText() == gained);

        // Each side reads the records too: none of these units is concealed.
        page.ViewAs("russian");
        Assert.Single(page.FindAll("#play-rallies .transfer-record"));
        Assert.Contains(page.FindAll("#play-rallies .dm-record"), item => item.RecordText() == gained);
    }

    // Backlog section 23 (A10.5): a broken unit ADJACENT to an enemy tank must rout, and with no captor that could take it (A20.21) it is eliminated
    // for Failure to Rout as the RtPh ends, which is a record.
    [Fact]
    public void AFailureToRoutIsARecord()
    {
        var hexes = Hexes();
        var page = NewGame();
        page.Find("#place-definition").Change("attacker-tank");
        page.Find("#place-facing").Change("east");
        Place(page, "gt", "attacker-tank", hexes.One);
        page.Find("#place-broken").Change(true);
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");
        for (var phase = 0; phase < 5; phase++)
        {
            page.EndPhase(Commit);
        }

        Assert.Equal("rtph", Current.Phase);

        // Pass 31 (ruling R31.6): the rout panel lists the viewing side's units, and a side ends the RtPh on its own units only, so the Russian
        // view reads r1's obligation and ends the phase without routing it.
        page.ViewAs("russian");
        Assert.Contains("must rout", page.Find(".rout-obligation[data-unit='r1']").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-rallies .failure-to-rout-record"));
        page.EndPhase(Commit);
        Assert.Equal(InstanceStatus.Eliminated, Current.Unit("r1")!.Status);
        var failure = page.Find("#play-rallies .failure-to-rout-record").RecordText();
        Assert.Equal($"4-4-7 squad R1 in {DisplayText.Place(1, hexes.Two)} is eliminated for Failure to Rout as the RtPh ends (A10.5)", failure);
        page.ViewAs("russian");
        Assert.Single(page.FindAll("#play-rallies .failure-to-rout-record"));
    }

    // Section 15.3: another view clears the Rout, SW, and Rally choices, so a panel never shows one unit and proposes another.
    [Fact]
    public void AnotherViewClearsTheRallyAndSupportWeaponChoices()
    {
        var hexes = Hexes();
        var page = NewGame();
        Place(page, "g1", "attacker-squad", hexes.One);
        Place(page, "g2", "attacker-squad", hexes.One);
        Place(page, "gm", "attacker-lmg", hexes.One, holder: "g1");
        Place(page, "r1", "defender-squad", hexes.Two);
        Commit(page, "#propose-setup");

        page.Find("#sw-weapon").Change("gm");
        page.Find("#sw-unit").Change("g2");
        page.Find("#deploy-squad").Change("g1");
        page.ViewAs("russian");
        // Pass 31 (ruling R31.6): the Russian view lists the Russian side's SW only, and it has none here, so the panel may be gone.
        Assert.All(page.FindAll("#sw-weapon"), select => Assert.Equal(string.Empty, select.GetAttribute("value")));
        Assert.All(page.FindAll("#sw-unit"), select => Assert.Equal(string.Empty, select.GetAttribute("value")));
        Assert.Equal(string.Empty, page.Find("#deploy-squad").GetAttribute("value"));
    }
}
