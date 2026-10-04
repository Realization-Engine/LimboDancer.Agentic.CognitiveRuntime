using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.Extensions.DependencyInjection;
using GamesPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Games;
using PlayPage = LimboDancer.Domains.Asl.MapStudio.Components.Pages.Play;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31c on the Play page (design section 9, with sections 13 and 14 for what was built): a fire proposal's arithmetic before the dice and the
/// record it becomes (D14); a fire group across ADJACENT Locations; the fire panel's targets read by range and the inspector's Range tab (section
/// 14); a Location armed and filled from the map (D13); the workspace that holds still (D15); "Since you last looked" (D17); the Residual FP note
/// and the hand-over screen's map (D16); and the Game inspector's list (D0). The board is the verified synthetic one with its stone building in
/// B1, a map of one board, so a Location reads "[B1]".
/// </summary>
public sealed class PlayPagePass31cTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "asl-play-31c-" + Guid.NewGuid().ToString("N"));
    private readonly ScriptedDice dice = new();
    private readonly LivePlay live;
    private readonly GameLibrary games;
    private readonly BuildingBoards boards;
    private readonly BunitContext context = new();

    public PlayPagePass31cTests()
    {
        var options = new StudioOptions { CacheRoot = Path.Combine(root, "cache"), BoardsRoot = Path.Combine(root, "boards") };
        var library = new UnitLibrary(options);
        var maps = new MapService(options, new FakeVaslMapSource());
        boards = new BuildingBoards(maps);
        live = new LivePlay(library, boards, dice.Roller);
        games = new GameLibrary(library, boards, live);
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

    private GameState Current => live.History("village")!.Current!;

    /// <summary>A Location as the page writes it on this map of one board: "[B1]".</summary>
    private static string Said(string location) => DisplayText.Place(1, location);

    /// <summary>
    /// An Open Ground hex beside the ground-level building, the building, and another Open Ground hex beside the first: on this board [B0], [B1],
    /// and [C1], each ADJACENT to the other two.
    /// </summary>
    private (string From, string Building, string Open) Hexes()
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        bool OpenGround(HexName? hex, int level) => hex is { } name && handle.HexFacts(name) is { Center.Terrain.Name: "Open Ground" } facts && facts.BaseLevel == level;
        return (from index in handle.Geometry.Hexes()
                let building = handle.Geometry.NameOf(index)
                where handle.HexFacts(building)!.Locations.Any(location => location.Level == 0 && location.Terrain?.Name.Contains("Building", StringComparison.Ordinal) == true)
                let level = handle.HexFacts(building)!.BaseLevel
                from side in Enum.GetValues<HexsideDirection>()
                let open = handle.Neighbor(building, side)
                where OpenGround(open, level)
                from other in Enum.GetValues<HexsideDirection>()
                let second = handle.Neighbor(open!.Value, other)
                where second != building && OpenGround(second, level)
                select ($"{Board}:{open}:0", $"{Board}:{building}:0", $"{Board}:{second}:0")).First();
    }

    private static void Commit(IRenderedComponent<PlayPage> page, string propose)
    {
        page.Find(propose).Click();
        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal),
            page.Find("#play-proposal").TextContent));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
    }

    private static void Place(IRenderedComponent<PlayPage> page, string id, string definition, string at, bool concealed = false, bool hidden = false)
    {
        page.Find("#place-definition").Change(definition);
        page.Find("#place-id").Change(id);
        page.Find("#place-location").Change(at);
        page.Find("#place-concealed").Change(concealed);
        page.Find("#place-hidden").Change(hidden);
        page.Find("#place-add").Click();
    }

    private IRenderedComponent<PlayPage> NewGame(string first, string second)
    {
        var page = context.Render<PlayPage>();
        MinimalCards.Choose(page, new Dictionary<string, string> { ["board"] = Board, ["first"] = first, ["second"] = second, ["first-elr"] = "3", ["second-elr"] = "3", ["month"] = "7" });
        return page;
    }

    /// <summary>
    /// A game in the Russian PFPh, shown in the Russian view: r1, r2, and the 8-0 rl in the open hex; the German g1 in the building, under "?"
    /// when asked; g2 in the second open hex.
    /// </summary>
    private IRenderedComponent<PlayPage> FireGame((string From, string Building, string Open) hexes, bool concealed = false)
    {
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From);
        Place(page, "r2", "defender-squad", hexes.From);
        Place(page, "rl", "defender-leader", hexes.From);
        Place(page, "g1", "attacker-squad", hexes.Building, concealed);
        Place(page, "g2", "attacker-squad", hexes.Open);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        Assert.Equal("pfph", Current.Phase);
        return page;
    }

    private static string[] Texts(IRenderedComponent<PlayPage> page, string selector) => [.. page.FindAll(selector).Select(item => item.TextContent.Trim())];

    /// <summary>A click on the map, which bUnit cannot make on the viewport's script: the workspace raises the hex as the script would.</summary>
    private static void ClickHex(IRenderedComponent<PlayPage> page, string location)
    {
        var workspace = page.FindComponent<BoardWorkspace>();
        var facts = GameMaps.FactsAt(BuildingBoards.Board, BoardLocation.Parse(location));
        Assert.NotNull(facts);
        page.InvokeAsync(() => workspace.Instance.SelectedChanged.InvokeAsync(facts));
    }

    // Design D14 (play test P-14): the review shows each firer's FP with its multipliers, the total, the column, and the DRM known before the dice,
    // and no dice or result; the record of the attack then holds the same arithmetic. Design D17: "Latest" gives the fire's result.
    [Fact]
    public void TheArithmeticShownBeforeTheDiceIsWhatTheRecordThenHolds()
    {
        var hexes = Hexes();
        var page = FireGame(hexes);
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-director").Change("rl");
        page.Find("#fire-target").Change(hexes.Building);
        Assert.Empty(page.FindAll("#fire-preview"));
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));

        var firepower = Texts(page, "#fire-preview .fire-fp");
        Assert.Equal(["4-4-7 squad R1: 4 FP × 2 (Point Blank Fire, A7.21) = 8", "4-4-7 squad R2: 4 FP × 2 (Point Blank Fire, A7.21) = 8"], firepower);
        var column = page.Find("#fire-preview .fire-column").TextContent.Trim();
        Assert.Equal("Total 16 FP: the 16 column", column);
        Assert.Equal("DRM: + 3 (stone building TEM, A7.6) + 0 (leadership, 8-0 leader R1, A7.531), +3 in all", page.Find("#fire-preview .fire-drm").TextContent.Trim());
        var preview = page.Find("#fire-preview").TextContent;
        Assert.DoesNotContain("IFT DR", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("Final DR", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("result", preview, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#fire-preview-unknown"));
        Assert.DoesNotContain(live.History("village")!.Events, item => item.Payload is FireResolved);

        // IFT 3+4 = 7, +3 for the stone building: Final DR 10 on the 16 column, a NMC; g1 rolls 3+4 = 7 against its morale 7 and is pinned (A7.8).
        Assert.True(dice.Enqueue([3, 4, 3, 4]));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var record = page.Find("#play-fires .fire-record");
        Assert.Equal(firepower, record.QuerySelectorAll(".fire-fp").Select(item => item.TextContent.Trim()));
        Assert.Equal(column, record.QuerySelector(".fire-column")!.TextContent.Trim());
        Assert.Contains("IFT DR 3, 4 = 7 + 3 (stone building TEM, A7.6) + 0 (leadership, 8-0 leader R1, A7.531):", record.QuerySelector(".fire-dr")!.TextContent, StringComparison.Ordinal);
        Assert.Equal(("10", "NMC"), (record.QuerySelector(".fire-final-dr")!.TextContent, record.QuerySelector(".fire-result")!.TextContent));
        Assert.Empty(page.FindAll("#fire-preview"));

        var group = $"4-4-7 squad R1, 4-4-7 squad R2 in {Said(hexes.From)}, directed by 8-0 leader R1 fire at {Said(hexes.Building)}";
        Assert.Equal(group, record.QuerySelector(".fire-group")!.TextContent.Trim());
        Assert.Equal($"Latest: Turn 1, Russian Prep Fire Phase: {group}: NMC; 4-6-7 squad G1: pinned.", page.Find("#play-latest").TextContent);
    }

    // Design D14 and ruling R31c.4: with a "?" in the target Location the firing view is not shown what rests on the units beneath it: a line's
    // final FP, the total, the column, and the DRM that would tell of them.
    [Fact]
    public void ATargetUnderAConcealmentCounterLeavesTheTotalNotKnownToTheFiringSide()
    {
        var hexes = Hexes();
        var page = FireGame(hexes, concealed: true);
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find("#fire-target").Change(hexes.Building);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));

        // The firer's line stops before the halving a concealed target gives (A12.14), so it has no final FP.
        Assert.Equal(["4-4-7 squad R1: 4 FP × 2 (Point Blank Fire, A7.21)"], Texts(page, "#fire-preview .fire-fp"));
        var unknown = page.Find("#fire-preview-unknown").TextContent;
        Assert.Contains("The total and its column are not known to you", unknown, StringComparison.Ordinal);
        Assert.Equal("DRM: + 3 (stone building TEM, A7.6); more may apply for what lies under the \"?\"", page.Find("#fire-preview .fire-drm").TextContent.Trim());
        var preview = page.Find("#fire-preview").TextContent;
        Assert.DoesNotContain("Total ", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("× 0.5", preview, StringComparison.Ordinal);
        Assert.DoesNotContain("4-6-7", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);

        // The facts of the target Location are the map's, and are shown.
        Assert.Equal("stone building", page.Find("#fire-facts tr[data-fact='terrain'] td:last-child").TextContent);
        Assert.Equal("1", page.Find("#fire-facts tr[data-fact='range'] td:last-child").TextContent);
    }

    // Design D14 (A7.5): "From" takes a second Location ADJACENT to the first, each with its firers; the group is proposed and accepted as one.
    [Fact]
    public void AFireGroupAcrossTwoAdjacentLocationsIsProposedAndAccepted()
    {
        var hexes = Hexes();
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", hexes.From);
        Place(page, "r2", "defender-squad", hexes.Open);
        Place(page, "g1", "attacker-squad", hexes.Building);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);

        // Before a Location is chosen there is nothing to add to; then only the ADJACENT Location that holds a firer is offered.
        Assert.Empty(page.FindAll("#fire-also"));
        page.Find("#fire-from").Change(hexes.From);
        Assert.Equal(["", hexes.Open], page.FindAll("#fire-also option").Select(option => option.GetAttribute("value")));
        Assert.Equal(["r1"], page.FindAll(".fire-firer").Select(item => item.GetAttribute("data-unit")));

        page.Find("#fire-also").Change(hexes.Open);
        Assert.Equal($"and {Said(hexes.Open)} remove", page.Find($".fire-also[data-location='{hexes.Open}']").TextContent.Trim());
        Assert.Equal([$"4-4-7 squad R1 in {Said(hexes.From)}", $"4-4-7 squad R2 in {Said(hexes.Open)}"], Texts(page, "label:has(.fire-firer)"));

        // The Location is taken out again, and its firer with it; then added once more.
        page.Find(".fire-also-remove").Click();
        Assert.Empty(page.FindAll(".fire-firer[data-unit='r2']"));
        page.Find("#fire-also").Change(hexes.Open);

        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-target").Change(hexes.Building);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        var group = $"4-4-7 squad R1 in {Said(hexes.From)}; 4-4-7 squad R2 in {Said(hexes.Open)}";
        Assert.Equal(group, page.Find("#fire-facts tr[data-fact='firers'] td:last-child").TextContent);
        Assert.Equal("Total 16 FP: the 16 column", page.Find("#fire-preview .fire-column").TextContent.Trim());

        Assert.True(dice.Enqueue([6, 6]));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal($"{group} fire at {Said(hexes.Building)}", page.Find("#play-fires .fire-group").TextContent.Trim());
        Assert.Equal($"Latest: Turn 1, Russian Prep Fire Phase: {group} fire at {Said(hexes.Building)}: no effect.", page.Find("#play-latest").TextContent);
        Assert.Contains("prep-fire", page.Find("#play-units tr[data-unit='r1']").TextContent, StringComparison.Ordinal);
        Assert.Contains("prep-fire", page.Find("#play-units tr[data-unit='r2']").TextContent, StringComparison.Ordinal);
        Assert.False(live.History("village")!.HasErrors);
    }

    // Design section 14 (R5): once a firer is ticked each target says its range and band, nearest first; the range is a closed line under the
    // select, with a line for each firer; no proposal is needed to read it.
    [Fact]
    public void TheFirePanelReadsItsTargetsByRangeOnceAFirerIsTicked()
    {
        // r1 in [C1]; g1 two hexes away in [A1], g2 beside it in [B0]: by name [A1] comes first, by range [B0].
        var (from, far, near) = ($"{Board}:C1:0", $"{Board}:A1:0", $"{Board}:B0:0");
        var page = NewGame("russian", "german");
        Place(page, "r1", "defender-squad", from);
        Place(page, "g1", "attacker-squad", far);
        Place(page, "g2", "attacker-squad", near);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        var events = live.History("village")!.Events.Count;

        page.Find("#fire-from").Change(from);
        Assert.Equal(["choose", "[A1]", "[B0]"], Texts(page, "#fire-target option"));
        Assert.Empty(page.FindAll("#fire-range"));

        page.Find(".fire-firer[data-unit='r1']").Change(true);
        Assert.Equal(["choose", "[B0]: 1 hex, PBF", "[A1]: 2 hexes, Normal Range"], Texts(page, "#fire-target option"));
        Assert.Equal(["", near, far], page.FindAll("#fire-target option").Select(option => option.GetAttribute("value")));
        Assert.All(page.FindAll("#fire-target option"), option => Assert.False(option.HasAttribute("disabled")));

        page.Find("#fire-target").Change(far);
        var range = page.Find("details#fire-range");
        Assert.False(range.HasAttribute("open"));
        Assert.Equal("Range: 2 hexes, Normal Range", range.QuerySelector("summary")!.TextContent);
        Assert.Equal(["4-4-7 squad R1, Normal Range 4: Normal Range"], range.QuerySelectorAll("li").Select(item => item.TextContent));

        // A typed Location reads the same way, and nothing has been proposed.
        page.Find("#fire-target").Change(string.Empty);
        page.Find("#fire-free-target").Change("B0");
        Assert.Equal("Range: 1 hex, PBF", page.Find("details#fire-range summary").TextContent);
        Assert.Empty(page.FindAll("#play-confirm"));
        Assert.Empty(page.FindAll("#fire-preview"));
        Assert.Equal(events, live.History("village")!.Events.Count);

        // "Show the range on the map" opens the Range tab with the group's ends read.
        page.Find("#fire-range-show").Click();
        Assert.Equal("true", page.Find("#play-inspector-tab-range").GetAttribute("aria-selected"));
        Assert.Equal("[C1] to [B0]: 1 hex.", page.Find("#range-result").TextContent);
    }

    // Design section 14 (R1, R3; section 14.4): the inspector has a Range tab beside LOS; it gives the hexes between two Locations and a row for
    // each unit of the From Location the view holds by name, and none for a unit under "?".
    [Fact]
    public void TheInspectorsRangeTabReadsTheHexesAndTheUnitsTheViewHolds()
    {
        var hexes = Hexes();
        var page = FireGame(hexes, concealed: true);
        Assert.Equal(["Proposal", "Selection", "LOS", "Range", "Evidence"], Texts(page, "[role=tablist][aria-label='Review and inspector'] [role=tab]"));
        page.Find("#play-inspector-tab-range").Click();
        Assert.Empty(page.FindAll("#range-result"));
        page.Find("#range-source").Change(hexes.From);
        page.Find("#range-target").Change(hexes.Building);
        page.Find("#range-check").Click();
        Assert.Equal($"{Said(hexes.From)} to {Said(hexes.Building)}: 1 hex.", page.Find("#range-result").TextContent);
        var rows = page.FindAll("#range-rows tbody tr");
        Assert.Equal(2, rows.Count);
        Assert.Contains("4-4-7 squad R1", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("PBF, FP x2 (A7.21)", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("4-4-7 squad R2", rows[1].TextContent, StringComparison.Ordinal);

        // From the building, where the Russian view holds only a "?": the hexes are the map's, and there is no row.
        page.Find("#range-source").Change(hexes.Building);
        page.Find("#range-target").Change(hexes.From);
        page.Find("#range-check").Click();
        Assert.Equal($"{Said(hexes.Building)} to {Said(hexes.From)}: 1 hex.", page.Find("#range-result").TextContent);
        Assert.Empty(page.FindAll("#range-rows"));
        Assert.DoesNotContain("4-6-7", page.Find("#range-panel").TextContent, StringComparison.Ordinal);

        page.Find("#range-clear").Click();
        Assert.Empty(page.FindAll("#range-result"));
    }

    // Design D13: a typed Location is armed, the header says what the next click does, the next hex clicked fills it and disarms it; an armed
    // field is the view's draft, so a hand-over drops it.
    [Fact]
    public void AnArmedLocationTakesTheNextHexClickedAndAHandOverDropsIt()
    {
        var hexes = Hexes();
        var page = NewGame("german", "russian");
        Place(page, "g1", "attacker-squad", hexes.From);
        Place(page, "r1", "defender-squad", hexes.Open);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);
        Assert.Equal("mph", Current.Phase);

        // With no field armed a click only picks the hex, and the field offers to take it.
        Assert.Empty(page.FindAll("#play-picking"));
        ClickHex(page, hexes.Open);
        Assert.Equal(string.Empty, page.Find("#move-to").GetAttribute("value"));
        Assert.Equal($"Use {Said(hexes.Open)}", page.Find("#move-to-use").TextContent);

        page.Find("#move-to-pick").Click();
        Assert.Equal("true", page.Find("span.location-field:has(#move-to)").GetAttribute("data-armed"));
        Assert.Equal("Stop picking", page.Find("#move-to-pick").TextContent);
        Assert.Contains("The next hex clicked on the map fills \"To\", at ground level.", page.Find("#play-picking").TextContent, StringComparison.Ordinal);
        Assert.Equal(string.Empty, page.Find("#move-to").GetAttribute("value"));

        ClickHex(page, hexes.Building);
        Assert.Equal(Said(hexes.Building), page.Find("#move-to").GetAttribute("value"));
        Assert.Equal("false", page.Find("span.location-field:has(#move-to)").GetAttribute("data-armed"));
        Assert.Empty(page.FindAll("#play-picking"));

        // The building has levels, so the field offers them; the next click, with no field armed, leaves the field as it is.
        Assert.Equal(["ground level", "level 1", "level 2"], Texts(page, "#move-to-level option"));
        ClickHex(page, hexes.Open);
        Assert.Equal(Said(hexes.Building), page.Find("#move-to").GetAttribute("value"));

        // Armed again and handed over: the next view has no armed field, and neither has this view when it comes back.
        page.Find("#move-to-pick").Click();
        Assert.NotEmpty(page.FindAll("#play-picking"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#play-picking"));
        Assert.All(page.FindAll("span.location-field"), field => Assert.Equal("false", field.GetAttribute("data-armed")));
        page.ViewAs("german");
        Assert.Empty(page.FindAll("#play-picking"));
        Assert.Equal("false", page.Find("span.location-field:has(#move-to)").GetAttribute("data-armed"));
        Assert.Equal("Pick on the map", page.Find("#move-to-pick").TextContent);
    }

    // Design D15: the card opens from the header's link and takes no room in the workspace; the units table is a panel of the activity strip, one
    // panel open at a time; a side is shown no revision in a game still played; and the header names a proposal only while it waits.
    [Fact]
    public void TheWorkspaceKeepsTheCardTheUnitsAndTheReviewInTheirPlaces()
    {
        var hexes = Hexes();
        var page = FireGame(hexes);

        // The card: a closed panel the header's link opens through the workspace's script, with its own way to close.
        Assert.Equal("DETAILS", page.Find("#play-card").TagName);
        Assert.False(page.Find("#play-card").HasAttribute("open"));
        Assert.Empty(page.FindAll("#play-workspace #play-card"));
        Assert.DoesNotContain(context.JSInterop.Invocations, invocation => invocation.Identifier == "openDetails");
        page.Find("#play-card-link").Click();
        page.WaitForAssertion(() => Assert.Contains(context.JSInterop.Invocations, invocation => invocation.Identifier == "openDetails" && invocation.Arguments is ["play-card"]));
        Assert.NotEmpty(page.FindAll("#play-card-close"));

        // The units table: a panel over the activity strip, closed until its button opens it, and closed again when another panel opens.
        Assert.NotEmpty(page.FindAll("#play-panel-activity #play-units-panel #play-units tr[data-unit='r1']"));
        Assert.True(page.Find("#play-units-panel").HasAttribute("hidden"));
        Assert.Equal("false", page.Find("#play-activity-units").GetAttribute("aria-expanded"));
        page.Find("#play-activity-units").Click();
        Assert.False(page.Find("#play-units-panel").HasAttribute("hidden"));
        Assert.Equal("true", page.Find("#play-activity-units").GetAttribute("aria-expanded"));
        page.Find("#play-activity-records").Click();
        Assert.True(page.Find("#play-units-panel").HasAttribute("hidden"));
        Assert.False(page.Find("#play-history").HasAttribute("hidden"));
        Assert.Equal(["Records", "Units", "Scripted dice"], Texts(page, ".play-activity-tabs button"));

        // No revision for a side: not in the header, the units table's note, or the board link. The adjudicator reads it in each.
        var revision = live.History("village")!.Events.Count;
        Assert.Empty(page.FindAll("#play-revision"));
        Assert.Equal("As the Russian side sees them.", page.Find("#play-units-view").TextContent);
        Assert.DoesNotContain("revision", page.Find("#play-view").GetAttribute("href"), StringComparison.Ordinal);
        Assert.DoesNotContain("revision", page.Find("#play-summary").TextContent, StringComparison.OrdinalIgnoreCase);
        page.ViewAs(Perspective.AdjudicatorName);
        Assert.Equal($"revision {revision}", page.Find("#play-revision").TextContent.Trim());
        Assert.Equal($"As the adjudicator sees them: every unit, at revision {revision}.", page.Find("#play-units-view").TextContent);
        Assert.Contains($"revision={revision}", page.Find("#play-view").GetAttribute("href"), StringComparison.Ordinal);
        Assert.Equal(["Records", "Units", "Scripted dice", "Audit"], Texts(page, ".play-activity-tabs button"));
        page.ViewAs("russian");

        // The header's Review button: there while a proposal waits for Confirm, and gone once it is committed.
        Assert.Empty(page.FindAll("#play-review-jump"));
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find("#fire-target").Change(hexes.Open);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.Contains("Confirm to commit", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Equal("Review: Fire", page.Find("#play-review-jump").TextContent);
        Assert.True(dice.Enqueue([6, 6]));
        page.Find("#play-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("Committed", page.Find("#play-outcome").TextContent, StringComparison.Ordinal));
        Assert.Empty(page.FindAll("#play-review-jump"));

        // A refused proposal does not wait, so the header does not name it: r2 may not fire into its own Location.
        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-free-target").Change(hexes.From);
        page.Find("#propose-fire").Click();
        page.WaitForAssertion(() => Assert.NotEmpty(page.Find("#play-outcome").TextContent));
        Assert.Contains("Refused", page.Find("#play-outcome").TextContent, StringComparison.Ordinal);
        Assert.Empty(page.FindAll("#play-review-jump"));
    }

    // Design D17: after a hand-over, "Since you last looked" lists what happened while the other view had the screen, from the incoming view's
    // own records; a view that has not had the screen before, or that missed nothing, is shown no such list.
    [Fact]
    public void SinceYouLastLookedListsWhatTheViewMissedInItsOwnWords()
    {
        var hexes = Hexes();
        var page = FireGame(hexes);
        Assert.Empty(page.FindAll("#play-activity-since"));

        // The German side looks for the first time, and has missed nothing it was shown before.
        page.ViewAs("german");
        Assert.Empty(page.FindAll("#play-activity-since"));
        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#play-activity-since"));

        page.Find("#fire-from").Change(hexes.From);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-target").Change(hexes.Building);
        Assert.True(dice.Enqueue([3, 4, 3, 4]));
        Commit(page, "#propose-fire");

        page.ViewAs("german");
        Assert.Equal("Since you last looked (1)", page.Find("#play-activity-since").TextContent);
        Assert.Equal([$"Turn 1, Russian Prep Fire Phase: 4-4-7 squad R1, 4-4-7 squad R2 in {Said(hexes.From)} fire at {Said(hexes.Building)}: NMC; 4-6-7 squad G1: pinned"],
            Texts(page, "#play-since li"));
        Assert.False(page.Find("#play-since-panel").HasAttribute("hidden"));

        // Back to the Russian side, which did it: nothing happened while it was away.
        page.ViewAs("russian");
        Assert.Empty(page.FindAll("#play-activity-since"));
    }

    // Design D16: Residual FP (A8.2) is public; the Selection tab says it for the hex picked, in each view, and for no other hex.
    [Fact]
    public void ResidualFpIsSaidInTheSelectionTabOfItsHex()
    {
        var hexes = Hexes();
        var page = NewGame("german", "russian");
        Place(page, "g1", "attacker-squad", hexes.From);
        Place(page, "r1", "defender-squad", hexes.Open);
        Place(page, "r2", "defender-squad", hexes.Open);
        Commit(page, "#propose-setup");
        page.EndPhase(Commit);
        page.EndPhase(Commit);

        // g1 enters the building, and the DEFENDER's First Fire at it leaves Residual FP there.
        page.Find(".move-unit[data-unit='g1']").Change(true);
        page.Find("#move-to").Change(hexes.Building);
        Commit(page, "#propose-move");
        page.ViewAs("russian");
        ClickHex(page, hexes.Building);
        page.Find("#play-inspector-tab-selection").Click();
        Assert.Empty(page.FindAll("#hex-notes"));

        page.Find("#fire-from").Change(hexes.Open);
        page.Find(".fire-firer[data-unit='r1']").Change(true);
        page.Find(".fire-firer[data-unit='r2']").Change(true);
        page.Find("#fire-free-target").Change(hexes.Building);
        Assert.True(dice.Enqueue([6, 5]));
        Commit(page, "#propose-fire");
        Assert.Single(Current.ResidualFire);

        foreach (var view in new[] { "russian", "german" })
        {
            page.ViewAs(view);
            ClickHex(page, hexes.Building);
            page.Find("#play-inspector-tab-selection").Click();
            Assert.Equal([$"8 Residual FP in {Said(hexes.Building)} (A8.2)"], Texts(page, "#hex-notes li"));
            ClickHex(page, hexes.From);
            Assert.Empty(page.FindAll("#hex-notes"));
        }
    }

    // Design D16 and ruling R31c.3: the hand-over screen's map shows what both sides know: a Known unit as its counter, a "?" for a concealed
    // one, and nothing for a hidden one; it takes no click, and it is not drawn while setup is open.
    [Fact]
    public void TheHandOverScreensMapShowsNoCounterOfAConcealedOrHiddenUnit()
    {
        var hexes = Hexes();
        var page = NewGame("german", "russian");
        Place(page, "g1", "attacker-squad", hexes.From);
        Place(page, "r1", "defender-squad", hexes.Open);
        Place(page, "r2", "defender-squad", hexes.Building, concealed: true);
        Place(page, "rh", "defender-squad", hexes.Building, hidden: true);
        Commit(page, "#propose-setup");

        // Setup is still open: a side sets up out of the other's sight, so the screen has no map.
        Assert.Contains("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);
        page.Find("#play-perspective").Change("russian");
        Assert.NotEmpty(page.FindAll("#play-handover"));
        Assert.Empty(page.FindAll("#play-handover-map"));
        page.Find("#play-handover-confirm").Click();
        page.ViewAs("german");
        page.EndPhase(Commit);
        Assert.DoesNotContain("Setup is open", page.Find("#play-summary").TextContent, StringComparison.Ordinal);

        page.Find("#play-perspective").Change("russian");
        var map = page.Find("#play-handover #play-handover-map");
        Assert.True(map.HasAttribute("inert"));
        Assert.Empty(page.FindAll("#play-workspace"));
        page.WaitForAssertion(() => Assert.Contains("-public\"", context.MapLayer("setUnits"), StringComparison.Ordinal));
        var units = context.MapLayer("setUnits");
        Assert.Contains("data-unit-id=\"g1\"", units, StringComparison.Ordinal);
        Assert.Contains("data-unit-id=\"r1\"", units, StringComparison.Ordinal);
        Assert.DoesNotContain("data-unit-id=\"r2\"", units, StringComparison.Ordinal);
        Assert.DoesNotContain("data-unit-id=\"rh\"", units, StringComparison.Ordinal);
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(units, "data-unit-id=\"[a-z]+-sealed-[0-9]+\""));
        Assert.Contains("hidden units are not on it", page.Find("#play-handover").TextContent, StringComparison.Ordinal);

        // The Russian side's own view then draws its own units, the hidden one too.
        page.Find("#play-handover-confirm").Click();
        page.WaitForAssertion(() => Assert.Contains("data-unit-id=\"rh\"", context.MapLayer("setUnits"), StringComparison.Ordinal));
        Assert.Contains("data-unit-id=\"r2\"", context.MapLayer("setUnits"), StringComparison.Ordinal);
    }

    // Design D0: the Game inspector lists fixtures and ended games; a game still played is not listed, since any view of it could be chosen there
    // with no hand-over.
    [Fact]
    public void TheGameInspectorListsAnEndedGameAndNotOneStillPlayed()
    {
        FireGame(Hexes());
        var name = GameLibrary.LivePrefix + "village";
        Assert.True(games.StillPlayed(name));
        Assert.Contains(name, games.Names);
        Assert.DoesNotContain(name, games.OpenNames);
        var inspector = context.Render<GamesPage>();
        Assert.DoesNotContain(name, inspector.FindAll("#game-name option").Select(option => option.GetAttribute("value") ?? option.TextContent));
        Assert.NotEmpty(inspector.FindAll("#game-name option"));
        Assert.Contains("A game still being played is not listed here", inspector.Find("#games-still-played").TextContent, StringComparison.Ordinal);

        // The game ends: it is listed, and opens at any view.
        var game = live.History("village")!;
        var scope = new GameScope(LivePlay.Tenant, "village");
        var ended = live.Store.Append(scope, "village", game.Events.Count,
            [new GameEvent(scope, "over-000000000000-1", game.Events.Count + 1, game.Events[^1].Time, "test", "game-ended", new GameEnded(1, "the test ends the game"), null, [], null)],
            live.Planner.Replay);
        Assert.Equal(AppendStatus.Committed, ended.Status);
        Assert.False(games.StillPlayed(name));
        Assert.Contains(name, games.OpenNames);
        var after = context.Render<GamesPage>();
        Assert.Contains(name, after.FindAll("#game-name option").Select(option => option.GetAttribute("value") ?? option.TextContent));
        after.Find("#game-name").Change(name);
        Assert.Contains($"Revision {game.Events.Count + 1} of {game.Events.Count + 1}", after.Find("#game-summary").TextContent, StringComparison.Ordinal);
        Assert.NotEmpty(after.FindAll("#game-units tr[data-unit='r1']"));
    }
}
