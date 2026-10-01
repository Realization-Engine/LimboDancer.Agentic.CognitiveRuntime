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
}
