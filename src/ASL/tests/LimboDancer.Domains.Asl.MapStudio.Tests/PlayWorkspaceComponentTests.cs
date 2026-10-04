using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Layout;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using Microsoft.AspNetCore.Components;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The components of pass 28c: the context header, the picked hex a typed Location takes (pass 29), the records grouped by turn and phase, the tabs whose panels the caller renders,
/// the ready notes beside pass 28b's Propose buttons, and RuleHelp on the older panels' help. The page's use of them is in
/// <see cref="PlayPagePass28cTests"/>.
/// </summary>
public sealed class PlayWorkspaceComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    [Fact]
    public void TheContextOffersTheReviewAndTheHandOver()
    {
        var reviewed = 0;
        var handed = 0;
        var header = context.Render<PlayContextHeader>(parameters => parameters
            .Add(item => item.Summary, "Revision 3: turn 1, Rally Phase, german phasing.").Add(item => item.Live, true)
            .Add(item => item.PendingTitle, "Rally").Add(item => item.Review, () => reviewed++)
            .Add(item => item.Awaiting, "the russian side").Add(item => item.AwaitingNote, "The DEFENDER may fire at the moving stack or pass (A8.1).")
            .Add(item => item.HandOver, () => handed++).Add(item => item.Status, "Rally: Every check passed."));
        Assert.Equal("Live", header.Find("#play-live").TextContent.Trim());
        header.Find("#play-review-jump").Click();
        header.Find("#play-hand-over").Click();
        Assert.Equal((1, 1), (reviewed, handed));
        Assert.Equal("Hand over to the russian side", header.Find("#play-hand-over").TextContent);
        Assert.Equal("status", header.Find("#play-status").GetAttribute("role"));

        // With nothing pending and no answer awaited, the bar is not drawn.
        header.Render(parameters => parameters.Add(item => item.PendingTitle, (string?)null).Add(item => item.Awaiting, (string?)null));
        Assert.Empty(header.FindAll(".play-context-actions"));
    }

    // Table player, pass 29: a typed Location offers the hex picked on the map, and takes it.
    [Fact]
    public void ATypedLocationOffersTheHexPickedOnTheMap()
    {
        var taken = string.Empty;
        var field = context.Render<CascadingValue<string?>>(parameters => parameters.Add(item => item.Name, "PickedLocation").Add(item => item.Value, "bd01:D4:0")
            .AddChildContent<LocationField>(inner => inner.Add(item => item.Id, "move-to").Add(item => item.Label, "To").Add(item => item.Value, "bd01:C3:0")
                .Add(item => item.OnChange, at => taken = at)));
        Assert.Equal("Use [D4] on board 01", field.Find("#move-to-use").TextContent);
        field.Find("#move-to-use").Click();
        Assert.Equal("bd01:D4:0", taken);

        // Nothing is offered when no hex is picked, or when the field already holds it.
        var plain = context.Render<LocationField>(parameters => parameters.Add(item => item.Id, "advance-to").Add(item => item.Label, "To"));
        Assert.Empty(plain.FindAll(".use-picked"));
    }

    // Backlog section 43: records keep the list's order in runs of one turn and phase.
    [Fact]
    public void RecordsAreGroupedInRunsOfOneTurnAndPhase()
    {
        var when = new Dictionary<string, string> { ["e3"] = "Turn 2, Rally Phase", ["e2"] = "Turn 1, Advance Phase", ["e1"] = "Turn 1, Advance Phase" };
        var list = context.Render<ActionRecordList>(parameters => parameters.Add(item => item.Heading, "Rally").Add(item => item.ListId, "play-rallies")
            .Add(item => item.Records, [new("e3", "rally", "third"), new("e2", "rally", "second"), new("e1", "rally", "first")])
            .Add(item => item.When, id => when.GetValueOrDefault(id)));
        Assert.Equal(["Turn 2, Rally Phase", "Turn 1, Advance Phase"], list.FindAll("#play-rallies .record-when").Select(item => item.TextContent));
        Assert.Equal(["Third", "Second", "First"], list.FindAll("#play-rallies .rally-record").Select(item => item.TextContent.Trim()));

        // Without a turn and phase, the list is one plain list, as before.
        list.Render(parameters => parameters.Add(item => item.When, (Func<string, string?>?)null));
        Assert.Equal(3, list.FindAll("ul#play-rallies > li").Count);
    }

    [Fact]
    public void TabsLeaveTheirPanelsToTheCaller()
    {
        var tabs = context.Render<InspectorTabs>(parameters => parameters.Add(item => item.Tabs, [new("map", "Map"), new("actions", "Actions")])
            .Add(item => item.Selected, "map").Add(item => item.IdPrefix, "play").Add(item => item.SeparatePanels, true));
        Assert.Equal("play-panel-actions", tabs.Find("#play-tab-actions").GetAttribute("aria-controls"));
        Assert.Empty(tabs.FindAll("[role=tabpanel]"));
    }

    // Backlog section 43: a disabled Propose button says what is still to choose.
    [Fact]
    public void ADisabledProposeButtonSaysWhatIsMissing()
    {
        var panel = context.Render<RallyActionPanel>(parameters => parameters.Add(item => item.Units, [new PlayChoice("r1", "r1 (russian)")])
            .Add(item => item.Leaders, []));
        Assert.Equal("propose-rally-ready", panel.Find("#propose-rally").GetAttribute("aria-describedby"));
        Assert.Equal("Choose the broken unit that tries to rally.", panel.Find("#propose-rally-ready").TextContent);

        panel.Render(parameters => parameters.Add(item => item.Unit, "r1"));
        Assert.Empty(panel.FindAll("#propose-rally-ready"));
    }

    // Backlog section 43: the older panels' help is a summary with the full text collapsed, and a field is described by the summary.
    [Fact]
    public void TheOlderPanelsHelpIsASummaryWithTheRuleCollapsed()
    {
        var mopUp = context.Render<MoppingUpAction>(parameters => parameters.Add(item => item.Buildings, [new MoppingUpAction.Choice("b1", ["g1"], ["g1"], new Dictionary<string, string> { ["g1"] = "bd01:D4" })]));
        Assert.Contains("Mop Up a building once per Player Turn", mopUp.Find("#mop-up-help-summary").TextContent, StringComparison.Ordinal);
        Assert.Contains("A12.153", mopUp.Find("#mop-up-help").TextContent, StringComparison.Ordinal);
        Assert.Equal("mop-up-help-summary", mopUp.Find("#mop-up-building").GetAttribute("aria-describedby"));
    }
}
