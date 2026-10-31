using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Play;
using LimboDancer.Domains.Asl.Units.State;
using Microsoft.AspNetCore.Components;

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
        Assert.Empty(list.FindAll("details"));

        // Pass 31c: a named group is a dropdown of checkboxes; its closed line names the group and the units ticked.
        list.Render(parameters => parameters.Add(item => item.Legend, "Units moving"));
        Assert.Equal("Units moving", list.Find("details.move-units.unit-dropdown summary .unit-dropdown-name").TextContent);
        Assert.Equal("b1 (bd04:E5:0)", list.Find("details.move-units summary .unit-dropdown-chosen").TextContent);
        Assert.Equal("Units moving", list.Find("details.move-units .unit-dropdown-list[role='group']").GetAttribute("aria-label"));
        Assert.Equal(2, list.FindAll("details.move-units .unit-dropdown-list .move-unit").Count);
        list.Find(".move-unit[data-unit='bs9']").Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("bs9", true), toggled);

        // With no unit to offer, the group says so in place of an empty box.
        list.Render(parameters => parameters.Add(item => item.Rows, []));
        Assert.Empty(list.FindAll("details"));
        Assert.Contains("none may be chosen now.", list.Find("p.move-units.unit-dropdown-empty").TextContent, StringComparison.Ordinal);
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

    [Fact]
    public void RoutMapScopeFollowsInspectionAndLeavesRoutePickingAlone()
    {
        string? selected = null;
        string? route = null;
        var picking = new LocationPicking { Boards = () => ["bd01", "bd02"] };
        picking.Arm = (id, label) => { picking.Armed = id; picking.ArmedLabel = label; };
        var host = context.Render<CascadingValue<string>>(parameters => parameters
            .AddCascadingValue("Picking", picking).Add(item => item.Name, "PickedLocation").Add(item => item.Value, "bd01:A1:0")
            .AddChildContent<RoutActionPanel>(parameters => parameters
            .Add(item => item.Units, [new("ground", "ground squad"), new("upper", "upper squad"), new("other", "other board squad"), new("second", "second hex squad")])
            .Add(item => item.Obligations, [new("ground", "ground advice"), new("upper", "upper advice"), new("other", "other board advice"), new("second", "second hex advice", [new("bd01:B3:0", "bd01:B3:0")])])
            .Add(item => item.Locations, new Dictionary<string, string?>
            {
                ["ground"] = "bd01:A1:0",
                ["upper"] = "bd01:A1:1",
                ["other"] = "bd02:A1:0",
                ["second"] = "bd01:B2:0",
            })
            .Add(item => item.OnUnit, value => selected = value).Add(item => item.OnRoute, value => route = value)));
        var rout = host.FindComponent<RoutActionPanel>();
        var childContent = host.Instance.ChildContent;

        void Pick(string hex)
        {
            picking.Picked = hex;
            picking.Serial++;
            host.Render(parameters => parameters.Add(item => item.Name, "PickedLocation")
                .Add(item => item.Value, hex).Add(item => item.ChildContent, childContent));
        }

        Assert.Equal(["", "ground", "upper"], rout.FindAll("#rout-unit option").Select(option => option.GetAttribute("value")));
        Assert.Equal(2, rout.FindAll(".rout-obligation").Count);
        Assert.Null(selected);
        Assert.True(rout.Find("#rout-route").HasAttribute("disabled"));

        rout.Find("#rout-scope button").Click();
        rout.Render();
        Assert.Equal(5, rout.FindAll("#rout-unit option").Count);
        Pick("bd01:A1:0");
        Assert.Equal(3, rout.FindAll("#rout-unit option").Count);
        rout.Render(parameters => parameters.Add(item => item.Unit, "upper"));
        Pick("bd01:B2:0");
        Assert.Equal(string.Empty, selected);
        Assert.Equal(["", "second"], rout.FindAll("#rout-unit option").Select(option => option.GetAttribute("value")));
        Assert.Equal("second", Assert.Single(rout.FindAll(".rout-obligation")).GetAttribute("data-unit"));

        rout.Render(parameters => parameters.Add(item => item.Unit, "second"));
        rout.Find("#rout-route-pick").Click();
        Pick("bd01:A1:0");
        Assert.Equal("bd01:A1:0", route);
        Assert.Equal(["", "second"], rout.FindAll("#rout-unit option").Select(option => option.GetAttribute("value")));
        rout.Find("#rout-each").Change(true);
        rout.Find(".use-route").Click();
        Assert.Null(picking.Armed);

        Pick("bd01:C4:0");
        Assert.Empty(rout.FindAll("#rout-scope"));
        Assert.Equal(5, rout.FindAll("#rout-unit option").Count);
        picking.Armed = "another-field";
        rout.Find("#rout-unit").Change("ground");
        Assert.Equal("another-field", picking.Armed);
        rout.Render(parameters => parameters.Add(item => item.Units, []).Add(item => item.Obligations, []));
        Assert.NotNull(rout.Find("#rout-empty"));
        Assert.Empty(rout.FindAll("#rout-unit, #rout-route"));
    }

    [Fact]
    public void AdvanceScopePreservesDestinationPicksAfterTheChooserDisarms()
    {
        var selected = new HashSet<string>();
        string? destination = null;
        var picking = new LocationPicking();
        picking.Arm = (id, label) => { picking.Armed = id; picking.ArmedLabel = label; };
        var host = context.Render<CascadingValue<string>>(parameters => parameters
            .AddCascadingValue("Picking", picking).Add(item => item.Name, "PickedLocation").Add(item => item.Value, "bd01:A1:0")
            .AddChildContent<AdvanceActionPanel>(parameters => parameters
                .Add(item => item.Side, "russian")
                .Add(item => item.Units, [new("ground", "ground squad"), new("upper", "upper squad"), new("other", "other board"), new("second", "second hex")])
                .Add(item => item.Locations, new Dictionary<string, string?>
                {
                    ["ground"] = "bd01:A1:0",
                    ["upper"] = "bd01:A1:1",
                    ["other"] = "bd02:A1:0",
                    ["second"] = "bd01:B2:0",
                })
                .Add(item => item.Selected, selected).Add(item => item.Edges, [])
                .Add(item => item.Destinations, ["bd01:B2:0"])
                .Add(item => item.OnTo, value => destination = value)
                .Add(item => item.OnClear, () => selected.Clear())));
        var panel = host.FindComponent<AdvanceActionPanel>();
        var childContent = host.Instance.ChildContent;

        void RenderPick(string hex) => host.Render(parameters => parameters.Add(item => item.Name, "PickedLocation")
            .Add(item => item.Value, hex).Add(item => item.ChildContent, childContent));
        void Pick(string hex)
        {
            picking.PickedBy = picking.Armed;
            picking.Picked = hex;
            picking.Serial++;
            RenderPick(hex);
        }

        Assert.Equal(["ground", "upper"], panel.FindAll(".advance-unit").Select(unit => unit.GetAttribute("data-unit")));
        Assert.Empty(selected);
        Assert.Empty(panel.FindAll("#advance-to, #advance-exit"));
        panel.Find("#advance-scope button").Click();
        Assert.Equal(4, panel.FindAll(".advance-unit").Count);
        Pick("bd01:A1:0");
        Assert.Equal(2, panel.FindAll(".advance-unit").Count);
        selected.Add("ground");
        panel.Render();
        panel.Find(".pick-on-map").Click();

        // The page's mutable picker can arrive before the PickedLocation cascade. The child disarms on success;
        // the later cascade must not reinterpret that same click as a change of origin.
        picking.PickedBy = picking.Armed;
        picking.Picked = "bd01:B2:0";
        picking.Serial++;
        RenderPick("bd01:A1:0");
        Assert.Null(picking.Armed);
        Assert.Equal("bd01:B2:0", destination);
        RenderPick("bd01:B2:0");
        Assert.Contains("A1", panel.Find("#advance-scope").TextContent, StringComparison.Ordinal);
        Assert.Contains("ground", selected);
        Pick("bd01:B2:0");
        Assert.Empty(selected);
        Assert.Equal("second", Assert.Single(panel.FindAll(".advance-unit")).GetAttribute("data-unit"));
        Pick("bd01:C4:0");
        Assert.Empty(panel.FindAll("#advance-scope"));
        Assert.Equal(4, panel.FindAll(".advance-unit").Count);
        Pick("bd02:A1:0");
        Assert.Equal("other", Assert.Single(panel.FindAll(".advance-unit")).GetAttribute("data-unit"));
        panel.Render(parameters => parameters.Add(item => item.Units, []));
        Assert.Empty(panel.FindAll("#advance-scope, #advance-to, #advance-exit"));
    }

    // A13: the step's options come back as one draft; the push shows only for a crew with a Gun.
    [Fact]
    public void MapOriginsKeepBoardsLevelsAndUnitSelectionDistinct()
    {
        UnitSelectionList.Toggle? toggled = null;
        var selected = new HashSet<string>();
        var move = context.Render<InfantryMovementAction>(parameters => parameters
            .AddCascadingValue("PickedLocation", "bd01:A1:0")
            .Add(item => item.Movers, [new("ground", "ground squad"), new("upper", "upper squad"), new("other", "other board squad")])
            .Add(item => item.Locations, new Dictionary<string, string?>
            {
                ["ground"] = "bd01:A1:0",
                ["upper"] = "bd01:A1:1",
                ["other"] = "bd02:A1:0",
            })
            .Add(item => item.Selected, selected).Add(item => item.OnToggle, value => toggled = value));
        var origins = move.FindAll("button").Where(button => button.TextContent.StartsWith("Move units from", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, origins.Length);
        origins.Single(button => button.TextContent.Contains("level 1", StringComparison.Ordinal)).Click();
        Assert.Null(toggled);
        Assert.Empty(selected);
        var offered = Assert.Single(move.FindAll(".picked-move-unit"));
        Assert.Equal("upper", offered.GetAttribute("data-unit"));
        offered.Change(true);
        Assert.Equal(new UnitSelectionList.Toggle("upper", true), toggled);
        Assert.NotNull(move.Find("#move-to"));
        Assert.False(move.Find("#move-options").HasAttribute("open"));

        move.Render(parameters => parameters.Add(item => item.Movers, []));
        Assert.NotNull(move.Find("#move-empty"));
        Assert.Empty(move.FindAll("#move-to, .picked-move-unit"));
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

        // The Play streamlining plan, S7: each button only while it may be used.
        var window = context.Render<MovementWindowActions>(parameters => parameters.Add(item => item.WindowOpen, true));
        Assert.NotEmpty(window.FindAll("#propose-pass"));
        Assert.Empty(window.FindAll("#propose-end-move"));
        window.Render(parameters => parameters.Add(item => item.WindowOpen, false));
        Assert.Empty(window.FindAll("#propose-pass"));
        Assert.NotEmpty(window.FindAll("#propose-end-move"));
        window.Render(parameters => parameters.Add(item => item.WindowOpen, (bool?)null));
        Assert.Empty(window.FindAll("#propose-pass"));
        Assert.Empty(window.FindAll("#propose-end-move"));
    }

    // A23 (rulings R25.1, R25.5): units waiting off board are named, and the advance panel carries its own exit.
    [Fact]
    public void TheAdvanceNamesUnitsWaitingOffBoard()
    {
        var panel = context.Render<AdvanceActionPanel>(parameters => parameters.Add(item => item.Side, "british")
            .Add(item => item.Units, [new UnitSelectionList.Row("bs9", "bs9 (off board: enters along the top edge)")]).Add(item => item.Selected, new HashSet<string>())
            .Add(item => item.Destinations, []).Add(item => item.Waiting, ["bs9"]).Add(item => item.Edges, [new MapExitAction.EdgeChoice("top", "top (north)")]));
        Assert.Contains("bs9: waiting off board", panel.Find("#advance-entry").TextContent, StringComparison.Ordinal);
        Assert.Empty(panel.FindAll("#advance-to, #advance-exit"));
        panel.Render(parameters => parameters.Add(item => item.Selected, new HashSet<string> { "bs9" })
            .Add(item => item.Destinations, ["bd04:A1:0"]));
        Assert.Contains(panel.FindAll("#advance-to option"), option => option.GetAttribute("value") == "bd04:A1:0");
        Assert.NotNull(panel.Find("#advance-exit"));
        Assert.True(panel.Find("#propose-advance-exit").HasAttribute("disabled"));
        Assert.Empty(context.Render<AdvanceActionPanel>(parameters => parameters.Add(item => item.Side, "british").Add(item => item.Units, [])
            .Add(item => item.Selected, new HashSet<string>()).Add(item => item.Destinations, []).Add(item => item.Edges, [new MapExitAction.EdgeChoice("top", "top (north)")])).FindAll("#advance-entry"));
    }

    // A16, C19, N03, N04 (UI review, pass 25): each keeps its ids, gates its button, and raises what the player chose. A01, the building entry, left
    // the page with the Play streamlining plan's S7: a step into a building through the Movement form is the same entry.
    [Fact]
    public void TheSmallActionsKeepTheirIdsAndRaiseTheirChoices()
    {
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
