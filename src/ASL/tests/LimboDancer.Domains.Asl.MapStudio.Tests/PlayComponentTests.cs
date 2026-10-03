using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The Play components of pass 23 (plan task 23.5): the Victory standing in a side's and the adjudicator's view (ruling R23.4), the hand-over screen
/// (ruling R23.2), the unit table's "?" rows (A2.9), and the audit, which only the adjudicator's view receives.
/// </summary>
public sealed class PlayComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    private static VictoryReport Standing() => new(
        [new VictoryControl("M9", "building", [BoardLocation.Parse("bd01:M9:0")], "german"), new VictoryControl("bd01:N4", "hex", [BoardLocation.Parse("bd01:N4:0")], null)],
        [new VictorySide("german", 2, 0, 4.5), new VictorySide("russian", 0, 0, 3)],
        null, new GameResult("german", "no Victory Condition of the other side holds (A26.3)", []));

    // Plan task 23.5 (A26.15; ruling R23.4): the standing pinned in the adjudicator's view, with what the result would be now.
    [Fact]
    public void TheAdjudicatorReadsTheWholeStanding()
    {
        var table = context.Render<VictoryStandingTable>(parameters => parameters.Add(item => item.Report, Standing()).Add(item => item.IfEndedNow, "german would win"));
        Assert.Contains("if the game ended now, german would win", table.Find("#play-victory caption").TextContent, StringComparison.Ordinal);
        Assert.Equal("german", table.Find("tr[data-control='M9'] td:last-child").TextContent);
        Assert.Equal("neither", table.Find("tr[data-control='bd01:N4'] td:last-child").TextContent);
        Assert.Equal("2 CVP, 0 Exit VP, 4.5 unbroken squad-equivalents", table.Find("tr[data-side='german'] td:last-child").TextContent);
        Assert.Equal("0 CVP, 0 Exit VP, 3 unbroken squad-equivalents", table.Find("tr[data-side='russian'] td:last-child").TextContent);
        Assert.Empty(table.FindAll("#play-victory-known"));
    }

    // Plan task 23.5 (A26.15; ruling R23.4): in a side's view the standing is Control as that side knows it, the enemy's squads its Known ones, and no
    // line says what the result would be now.
    [Fact]
    public void ASideReadsTheStandingAsItKnowsIt()
    {
        var table = context.Render<VictoryStandingTable>(parameters => parameters.Add(item => item.Report, Standing()).Add(item => item.KnownTo, "russian"));
        Assert.DoesNotContain("would win", table.Find("#play-victory caption").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("draw", table.Find("#play-victory caption").TextContent, StringComparison.Ordinal);
        Assert.Equal("german", table.Find("tr[data-control='M9'] td:last-child").TextContent);
        Assert.EndsWith("among its Known units", table.Find("tr[data-side='german'] td:last-child").TextContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Known units", table.Find("tr[data-side='russian'] td:last-child").TextContent, StringComparison.Ordinal);
        Assert.Contains("A26.15", table.Find("#play-victory-known").TextContent, StringComparison.Ordinal);

        // Once the game has ended, Control is declared: the page reads the standing for no side and gives no "would win" line.
        var ended = context.Render<VictoryStandingTable>(parameters => parameters.Add(item => item.Report, Standing()));
        Assert.DoesNotContain("would win", ended.Find("#play-victory caption").TextContent, StringComparison.Ordinal);
        Assert.Empty(ended.FindAll("#play-victory-known"));
    }

    // Ruling R23.2: while a view waits, the content is not rendered at all; confirming calls back.
    [Fact]
    public void TheHandOverScreenLeavesTheContentOut()
    {
        var confirmed = false;
        RenderFragment content = builder => builder.AddMarkupContent(0, "<p id=\"secret\">a side's view</p>");
        var screen = context.Render<HandOverScreen>(parameters => parameters.Add(item => item.Next, "russian").Add(item => item.Label, "the russian side")
            .Add(item => item.Confirmed, () => confirmed = true).AddChildContent(content));
        Assert.Empty(screen.FindAll("#secret"));
        Assert.Contains("Hand the screen to the russian side", screen.Find("#play-handover-title").TextContent, StringComparison.Ordinal);
        screen.Find("#play-handover-confirm").Click();
        Assert.True(confirmed);

        screen.Render(parameters => parameters.Add(item => item.Next, (string?)null));
        Assert.NotNull(screen.Find("#secret"));
        Assert.Empty(screen.FindAll("#play-handover"));
    }

    // A2.9 (ruling R23.3): a counter beneath an enemy stack's top before play reads as such, a "?" as concealed; the audit renders only when given.
    [Fact]
    public void TheUnitTableNamesWhatItCannotShow()
    {
        var table = context.Render<PlayUnitTable>(parameters => parameters
            .Add(item => item.Rows, [new PlayUnitTable.Row("g1", "german", "squad", BoardLocation.Parse("bd01:D4:0"), "active", "0", string.Empty)])
            .Add(item => item.Sealed, [new SealedPresence("sealed-1", "russian", BoardLocation.Parse("bd01:E4:0")),
                new SealedPresence("sealed-2", "russian", BoardLocation.Parse("bd01:E5:0")) { Uninspected = true }]));
        Assert.Equal("concealed", table.Find("tr[data-sealed='sealed-1'] td:nth-child(3)").TextContent);
        Assert.Equal("not inspectable before play (A2.9)", table.Find("tr[data-sealed='sealed-2'] td:nth-child(3)").TextContent);
        Assert.Equal("bd01:D4:0", table.Find("tr[data-unit='g1'] .play-locate").TextContent);

        Assert.Empty(context.Render<AdjudicatorAuditPanel>().FindAll("#play-audit"));
        Assert.Contains("ExecutorCompleted", context.Render<AdjudicatorAuditPanel>(parameters => parameters.Add(item => item.Lines, ["ExecutorCompleted"])).Markup,
            StringComparison.Ordinal);
    }

    // Pass 24 (rulings R24.1, R24.3, R24.5): a Location is a row only where its Control differs from its building's, a vehicle's hold is named, and a
    // side's CVP say how many come from Guns and vehicles.
    [Fact]
    public void TheStandingListsDifferingLocationsAndGunAndVehicleCvp()
    {
        var hex = BoardLocation.Parse("bd01:F5:0");
        var report = new VictoryReport(
            [new VictoryControl("F5", "building", [hex], "russian"),
                new VictoryControl("bd01:F5:0", "location", [hex], "german") { Level = 0, Building = "F5" },
                new VictoryControl("bd01:F5:1", "location", [hex], "russian") { Level = 1, Building = "F5" },
                new VictoryControl("bd01:F5:-1", "location", [hex], "russian") { Level = -1, Building = "F5", ByVehicle = true },
                new VictoryControl("K5", "building", [hex], "russian"),
                new VictoryControl("bd01:K5:1", "location", [hex], "german") { Level = 1, Building = "K5" },
                new VictoryControl("bd01:K5:2", "location", [hex], "german") { Level = 2, Building = "K5" }],
            [new VictorySide("german", 0, 0, 2), new VictorySide("russian", 8, 0, 3) { GunAndVehicleCvp = 6 }],
            null, new GameResult("german", "no Victory Condition of the other side holds (A26.3)", []));
        var table = context.Render<VictoryStandingTable>(parameters => parameters.Add(item => item.Report, report));
        Assert.Equal("bd01:F5 ground level of building F5", table.Find("tr[data-control='bd01:F5:0'] td").TextContent);
        Assert.Empty(table.FindAll("tr[data-control='bd01:F5:1']"));
        Assert.Empty(table.FindAll("tr[data-control='bd01:F5:-1']"));
        Assert.StartsWith("8 CVP (6 for Guns and vehicles)", table.Find("tr[data-side='russian'] td:last-child").TextContent, StringComparison.Ordinal);
        Assert.Contains("R24.1", table.Find("#play-victory-locations").TextContent, StringComparison.Ordinal);

        // Table player, pass 24: several Locations of one building and side are one row.
        Assert.Equal("2 Locations of building K5", table.Find("tr[data-locations='K5'] summary").TextContent);
        Assert.Equal("German", table.Find("tr[data-locations='K5'] td:last-child").TextContent);
    }

    // Ruling R24.2: the Mopping Up panel offers a building's units with their places, none checked (each becomes TI), and proposes the ones checked
    // with the guard chosen.
    [Fact]
    public void MoppingUpProposesTheCheckedUnits()
    {
        MoppingUpAction.Proposal? proposed = null;
        var panel = context.Render<MoppingUpAction>(parameters => parameters
            .Add(item => item.Buildings, [new MoppingUpAction.Choice("F3", ["r1", "r2"], ["r1", "r2", "l1"],
                new Dictionary<string, string> { ["r1"] = "E4", ["r2"] = "F3 level 1", ["l1"] = "E4" })])
            .Add(item => item.OnPropose, (MoppingUpAction.Proposal value) => proposed = value));
        Assert.True(panel.Find("#propose-mop-up").HasAttribute("disabled"));
        panel.Find("#mop-up-building").Change("F3");
        Assert.True(panel.Find("#propose-mop-up").HasAttribute("disabled"));
        Assert.Contains("r2 in F3 level 1", panel.Find(".mop-up-units").TextContent, StringComparison.Ordinal);
        panel.Find("input[data-unit='r1']").Change(true);
        panel.Find("#mop-up-guard").Change("l1");
        panel.Find("#propose-mop-up").Click();
        Assert.Equal("F3", proposed!.Building);
        Assert.Equal(["r1"], proposed.Units);
        Assert.Equal("l1", proposed.Guard);
    }

    // UI review, pass 24: a checked unit the new choices no longer hold leaves the button disabled, no guard chosen sends none, and a building no longer
    // offered (Mopped Up this Player Turn) drops the draft.
    [Fact]
    public void MoppingUpDropsAStaleDraft()
    {
        MoppingUpAction.Proposal? proposed = null;
        var places = new Dictionary<string, string> { ["r1"] = "bd01:E4", ["r2"] = "bd01:E4" };
        var panel = context.Render<MoppingUpAction>(parameters => parameters
            .Add(item => item.Buildings, [new MoppingUpAction.Choice("F3", ["r1", "r2"], ["r1", "r2"], places)])
            .Add(item => item.OnPropose, (MoppingUpAction.Proposal value) => proposed = value));
        panel.Find("#mop-up-building").Change("F3");
        panel.Find("input[data-unit='r1']").Change(true);
        panel.Render(parameters => parameters.Add(item => item.Buildings, [new MoppingUpAction.Choice("F3", ["r2"], ["r2"], places)]));
        Assert.True(panel.Find("#propose-mop-up").HasAttribute("disabled"));
        panel.Find("input[data-unit='r2']").Change(true);
        panel.Find("#propose-mop-up").Click();
        Assert.Equal(["r2"], proposed!.Units);
        Assert.Null(proposed.Guard);
        panel.Render(parameters => parameters.Add(item => item.Buildings, []));
        Assert.Empty(panel.FindAll(".mop-up-units"));
        Assert.True(panel.Find("#propose-mop-up").HasAttribute("disabled"));
    }
}
