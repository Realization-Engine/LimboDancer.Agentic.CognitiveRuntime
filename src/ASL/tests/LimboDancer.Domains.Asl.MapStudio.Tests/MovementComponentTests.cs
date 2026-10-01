using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The movement, entry, advance, and exit components extracted in pass 25 (plan task 25.6), each on its own: they hold no draft, so each renders what
/// the page gives it and raises what the player does; and the K08 Location note shown only with a Location row (the user, after the pass 24 demo).
/// </summary>
public sealed class MovementComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    // S12: the rows the caller gives, checked as it says, raising each toggle.
    [Fact]
    public void TheUnitListRaisesEachToggle()
    {
        UnitSelectionList.Toggle? toggled = null;
        var list = context.Render<UnitSelectionList>(parameters => parameters
            .Add(item => item.Rows, [new UnitSelectionList.Row("bs9", "bs9 (off board: enters along the top edge)"), new UnitSelectionList.Row("b1", "b1 (bd04:E5:0)")])
            .Add(item => item.Selected, new HashSet<string> { "b1" }).Add(item => item.CssClass, "move-unit").Add(item => item.OnToggle, toggle => toggled = toggle));
        Assert.False(list.Find(".move-unit[data-unit='bs9']").HasAttribute("checked"));
        Assert.True(list.Find(".move-unit[data-unit='b1']").HasAttribute("checked"));
        Assert.Contains("off board: enters along the top edge", list.Markup, StringComparison.Ordinal);
        Assert.Empty(list.FindAll("fieldset"));
        list.Render(parameters => parameters.Add(item => item.Legend, "Units moving"));
        Assert.Equal("Units moving", list.Find("fieldset.move-units legend").TextContent);
        list.Find(".move-unit[data-unit='bs9']").Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("bs9", true), toggled);
    }

    // K09 (ruling R25.5): the exit waits for checked units and an edge, under the ids the caller names.
    [Fact]
    public void TheExitWaitsForUnitsAndAnEdge()
    {
        var proposed = false;
        string? edge = null;
        var exit = context.Render<MapExitAction>(parameters => parameters.Add(item => item.SelectId, "advance-exit").Add(item => item.ButtonId, "propose-advance-exit")
            .Add(item => item.Edges, [new MapExitAction.EdgeChoice("top", "top (north)"), new MapExitAction.EdgeChoice("bottom", "bottom (south)")]).Add(item => item.Ready, true).Add(item => item.OnEdge, value => edge = value)
            .Add(item => item.OnPropose, () => proposed = true));
        Assert.True(exit.Find("#propose-advance-exit").HasAttribute("disabled"));
        Assert.Equal("bottom (south)", exit.FindAll("#advance-exit option")[2].TextContent);
        Assert.NotNull(exit.Find("#propose-advance-exit-ready"));
        exit.Find("#advance-exit").Change("top");
        Assert.Equal("top", edge);
        exit.Render(parameters => parameters.Add(item => item.Edge, "top"));
        exit.Find("#propose-advance-exit").Click();
        Assert.True(proposed);
        exit.Render(parameters => parameters.Add(item => item.Ready, false));
        Assert.True(exit.Find("#propose-advance-exit").HasAttribute("disabled"));
    }

    // A14 (A24.1; backlog section 19): only the squads and places the page offers, each with its cost.
    [Fact]
    public void SmokeOffersItsSquadsAndPlaces()
    {
        var smoke = context.Render<SmokeGrenadeAction>(parameters => parameters
            .Add(item => item.Placers, [new SmokeGrenadeAction.Placer("bs9", "bs9 (exponent 2)")])
            .Add(item => item.Places, [new SmokeGrenadeAction.Place("bd04:E1:0", "bd04:E1:0 (its own, 1 MF)"), new SmokeGrenadeAction.Place("bd04:E2:0", "bd04:E2:0 (ADJACENT, 2 MF)")])
            .Add(item => item.Ready, true));
        Assert.Equal(["", "bs9"], smoke.FindAll("#smoke-by option").Select(option => option.GetAttribute("value")));
        Assert.Equal(["", "bd04:E1:0", "bd04:E2:0"], smoke.FindAll("#smoke-at option").Select(option => option.GetAttribute("value")));
        Assert.True(smoke.Find("#propose-smoke").HasAttribute("disabled"));
        smoke.Render(parameters => parameters.Add(item => item.By, "bs9").Add(item => item.At, "bd04:E2:0"));
        Assert.False(smoke.Find("#propose-smoke").HasAttribute("disabled"));
    }

    // A13: the step's options come back as one draft; the push shows only for a crew with a Gun.
    [Fact]
    public void TheMoveRaisesItsDraft()
    {
        InfantryMovementAction.MoveDraft? draft = null;
        var move = context.Render<InfantryMovementAction>(parameters => parameters.Add(item => item.Movers, [new UnitSelectionList.Row("b1", "b1 (bd04:E5:0)")])
            .Add(item => item.Selected, new HashSet<string> { "b1" }).Add(item => item.Draft, new InfantryMovementAction.MoveDraft("bd04:E4:0", false, string.Empty, false, false, true))
            .Add(item => item.OnDraftChanged, value => draft = value));
        Assert.Empty(move.FindAll("#move-push"));
        Assert.False(move.Find("#propose-move").HasAttribute("disabled"));
        move.Find("#move-minimum").Change(true);
        Assert.Equal(new InfantryMovementAction.MoveDraft("bd04:E4:0", true, string.Empty, false, false, true), draft);
        move.Find("#move-to").Change("bd04:E3:0");
        Assert.Equal("bd04:E3:0", draft!.To);
        move.Render(parameters => parameters.Add(item => item.PushGun, "gun1"));
        Assert.Contains("push gun1", move.Find("#move-push").ParentElement!.TextContent, StringComparison.Ordinal);
    }

    // A11, A15: the status line and the window's two commands follow the moving stack.
    [Fact]
    public void TheStatusAndWindowFollowTheStack()
    {
        var movement = new MovementState(["bs9"], BoardLocation.Parse("bd04:G1:0"), 2, 1, false, WindowOpen: true) { Bypass = [Maps.Geometry.HexsideDirection.NorthEast] };
        var status = context.Render<MovementStatus>(parameters => parameters.Add(item => item.Movement, movement));
        Assert.Equal("open", status.Find("#move-state").GetAttribute("data-window"));
        Assert.Contains("In Bypass along northeast", status.Find("#move-bypass-state").TextContent, StringComparison.Ordinal);

        var window = context.Render<MovementWindowActions>(parameters => parameters.Add(item => item.WindowOpen, true));
        Assert.False(window.Find("#propose-pass").HasAttribute("disabled"));
        Assert.True(window.Find("#propose-end-move").HasAttribute("disabled"));
        window.Render(parameters => parameters.Add(item => item.WindowOpen, (bool?)null));
        Assert.True(window.Find("#propose-pass").HasAttribute("disabled"));
        Assert.True(window.Find("#propose-end-move").HasAttribute("disabled"));
    }

    // A23 (rulings R25.1, R25.5): units waiting off board are named, and the advance panel carries its own exit.
    [Fact]
    public void TheAdvanceNamesUnitsWaitingOffBoard()
    {
        var panel = context.Render<AdvanceActionPanel>(parameters => parameters.Add(item => item.Side, "british")
            .Add(item => item.Units, [new UnitSelectionList.Row("bs9", "bs9 (off board: enters along the top edge)")]).Add(item => item.Selected, new HashSet<string>())
            .Add(item => item.Destinations, []).Add(item => item.Waiting, ["bs9"]).Add(item => item.Edges, [new MapExitAction.EdgeChoice("top", "top (north)")]));
        Assert.Contains("bs9 waits off board", panel.Find("#advance-entry").TextContent, StringComparison.Ordinal);
        Assert.NotNull(panel.Find("#advance-exit"));
        Assert.True(panel.Find("#propose-advance-exit").HasAttribute("disabled"));
        Assert.Empty(context.Render<AdvanceActionPanel>(parameters => parameters.Add(item => item.Side, "british").Add(item => item.Units, [])
            .Add(item => item.Selected, new HashSet<string>()).Add(item => item.Destinations, []).Add(item => item.Edges, [new MapExitAction.EdgeChoice("top", "top (north)")])).FindAll("#advance-entry"));
    }

    // A01, A16, C19, N03, N04 (UI review, pass 25): each keeps its ids, gates its button, and raises what the player chose.
    [Fact]
    public void TheSmallActionsKeepTheirIdsAndRaiseTheirChoices()
    {
        string? unit = null;
        var entry = context.Render<BuildingEntryAction>(parameters => parameters.Add(item => item.Units, [new BuildingEntryAction.Choice("g1", "g1 (german)")])
            .Add(item => item.Location, "bd01:E4:0").Add(item => item.OnUnit, value => unit = value));
        entry.Find("#enter-unit").Change("g1");
        Assert.Equal("g1", unit);
        Assert.Equal("bd01:E4:0", entry.Find("#enter-location").GetAttribute("value"));

        var reaction = context.Render<ReactionFireAction>(parameters => parameters.Add(item => item.Vehicle, "t34").Add(item => item.Attackers, ["g1", "gl1"])
            .Add(item => item.Leaders, ["gl1"]));
        Assert.True(reaction.Find("#propose-reaction").HasAttribute("disabled"));
        Assert.Equal(["", "gl1"], reaction.FindAll("#reaction-leader option").Select(option => option.GetAttribute("value")));
        reaction.Render(parameters => parameters.Add(item => item.Attacker, "g1"));
        Assert.Equal("Propose: CC Reaction Fire at t34", reaction.Find("#propose-reaction").TextContent);
        Assert.False(reaction.Find("#propose-reaction").HasAttribute("disabled"));

        var declined = false;
        var open = context.Render<OpenEntryDeclaration>(parameters => parameters.Add(item => item.EventId, "e1").Add(item => item.Unit, "g1").Add(item => item.Target, "bd01:E4:0")
            .Add(item => item.OnDecline, () => declined = true));
        Assert.Equal("e1", open.Find("p").GetAttribute("data-open-attempt"));
        open.Find(".declare-decline").Click();
        Assert.True(declined);

        var charge = context.Render<PlaceDemolitionChargeAction>(parameters => parameters.Add(item => item.Charges, [new PlaceDemolitionChargeAction.ChargeChoice("dc1", "dc1 (g1)")])
            .Add(item => item.Ready, true));
        Assert.True(charge.Find("#propose-place-dc").HasAttribute("disabled"));
        Assert.NotNull(charge.Find("#propose-place-dc-ready"));
        charge.Render(parameters => parameters.Add(item => item.Charge, "dc1").Add(item => item.At, "bd01:E5:0"));
        Assert.False(charge.Find("#propose-place-dc").HasAttribute("disabled"));
        Assert.Empty(charge.FindAll("#propose-place-dc-ready"));

        string? kept = null;
        var keep = context.Render<BerserkRetainedWeaponsField>(parameters => parameters.Add(item => item.OnChange, value => kept = value));
        keep.Find("#move-keep").Change("lmg1");
        Assert.Equal("lmg1", kept);
    }

    // K08 (the user, after the pass 24 demo): with no Location row, no Location note, even when Location items exist.
    [Fact]
    public void TheLocationNoteShowsOnlyWithALocationRow()
    {
        var hex = BoardLocation.Parse("bd01:F5:0");
        var report = new VictoryReport(
            [new VictoryControl("F5", "building", [hex], "russian"), new VictoryControl("bd01:F5:1", "location", [hex], "russian") { Level = 1, Building = "F5" }],
            [new VictorySide("german", 0, 0, 2), new VictorySide("russian", 0, 0, 3)],
            null, new GameResult("german", "no Victory Condition of the other side holds (A26.3)", []));
        var table = context.Render<VictoryStandingTable>(parameters => parameters.Add(item => item.Report, report));
        Assert.Empty(table.FindAll("tr[data-control='bd01:F5:1']"));
        Assert.Empty(table.FindAll("#play-victory-locations"));
    }
}
