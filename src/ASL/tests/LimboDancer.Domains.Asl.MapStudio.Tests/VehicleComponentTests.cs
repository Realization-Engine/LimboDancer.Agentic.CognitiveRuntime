using Bunit;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// The setup and vehicle components extracted in pass 26 (plan task 26.5), each on its own: they hold no draft, so each renders what the page gives it,
/// under the element ids the page tests use, and raises what the player does.
/// </summary>
public sealed class VehicleComponentTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    private static SetupPlacementEditor.SetupDraft Draft(bool offBoard = false) =>
        new("attacker-halftrack", "ght", "german-1", "bd04:I1:0", string.Empty, "east", string.Empty, false, false, false, offBoard, string.Empty);

    // S13: every hexspine, or the ones the caller offers, raising the choice.
    [Fact]
    public void TheFacingPickerOffersItsHexspines()
    {
        string? chosen = null;
        var picker = context.Render<FacingPicker>(parameters => parameters.Add(item => item.Id, "place-facing").Add(item => item.Label, "VCA")
            .Add(item => item.OnChange, value => chosen = value));
        Assert.Equal(6, picker.FindAll("#place-facing option").Count);
        picker.Find("#place-facing").Change("south-east");
        Assert.Equal("south-east", chosen);
        picker.Render(parameters => parameters.Add(item => item.Choices, [UnitFacing.SouthWest, UnitFacing.SouthEast]));
        Assert.Equal(["south-west", "south-east"], picker.FindAll("#place-facing option").Select(option => option.GetAttribute("value")));
    }

    // P08 (rulings R25.4, R26.1): a vehicle's VCA, the holder for a Passenger, and the entry area only for a counter off board in a group with several.
    [Fact]
    public void TheSetupEditorOffersAnEntryAreaOnlyOffBoard()
    {
        SetupPlacementEditor.SetupDraft? drafted = null;
        var added = false;
        var editor = context.Render<SetupPlacementEditor>(parameters => parameters.Add(item => item.Draft, Draft()).Add(item => item.Kind, "asl:vehicle")
            .Add(item => item.Definitions, [new SetupPlacementEditor.Choice("attacker-halftrack", "attacker-halftrack (german vehicle)")])
            .Add(item => item.Entries, [new SetupPlacementEditor.Choice("north", "north: the top edge, Turn 1"), new SetupPlacementEditor.Choice("west", "west: the left edge, Turn 2")])
            .Add(item => item.OnDraft, draft => drafted = draft).Add(item => item.OnAdd, () => added = true));
        Assert.NotNull(editor.Find("#place-facing"));
        Assert.Contains("VCA (hexspine)", editor.Markup, StringComparison.Ordinal);
        Assert.Empty(editor.FindAll("#place-entry"));
        Assert.Empty(editor.FindAll("#place-bore"));
        editor.Find("#place-offboard").Change(true);
        Assert.True(drafted!.OffBoard);
        editor.Render(parameters => parameters.Add(item => item.Draft, Draft(offBoard: true)));
        editor.Find("#place-entry").Change("west");
        Assert.Equal("west", drafted.Entry);
        editor.Find("#place-add").Click();
        Assert.True(added);

        // A Gun's holder is its crew or its towing vehicle, and it may Bore Sight.
        editor.Render(parameters => parameters.Add(item => item.Kind, "asl:gun"));
        Assert.Contains("Manned or towed by", editor.Markup, StringComparison.Ordinal);
        Assert.NotNull(editor.Find("#place-bore"));
    }

    // P09: each placement removable by its key; the proposal waits for one and for the card.
    [Fact]
    public void ThePlacementListRemovesAndProposes()
    {
        string? removed = null;
        var proposed = false;
        var list = context.Render<SetupPlacementList>(parameters => parameters
            .Add(item => item.Items, [new SetupPlacementList.Item("gs", "gs: attacker-squad aboard ght as a Passenger")])
            .Add(item => item.OnRemove, key => removed = key).Add(item => item.OnPropose, () => proposed = true));
        Assert.Contains("aboard ght as a Passenger", list.Find("#place-list").TextContent, StringComparison.Ordinal);
        list.Find("#place-list button").Click();
        Assert.Equal("gs", removed);
        list.Find("#propose-setup").Click();
        Assert.True(proposed);
        list.Render(parameters => parameters.Add(item => item.Ready, false));
        Assert.True(list.Find("#propose-setup").HasAttribute("disabled"));
        list.Render(parameters => parameters.Add(item => item.Items, []).Add(item => item.Ready, true));
        Assert.Empty(list.FindAll("#place-list"));
        Assert.True(list.Find("#propose-setup").HasAttribute("disabled"));
    }

    // A17, A18: the vehicles and the chosen one's status and content.
    [Fact]
    public void TheVehiclePanelChoosesAVehicleAndShowsItsStatus()
    {
        string? chosen = null;
        var panel = context.Render<VehicleMovementPanel>(parameters => parameters
            .Add(item => item.Vehicles, [new VehicleMovementPanel.VehicleChoice("ght", "ght (off board, to enter)")]).Add(item => item.OnSelect, value => chosen = value)
            .AddChildContent<VehicleMovementStatus>(status => status.Add(item => item.Text, "waits off board to enter, in Motion").Add(item => item.Recall, "Recalled: it leaves")));
        panel.Find("#vehicle-unit").Change("ght");
        Assert.Equal("ght", chosen);
        Assert.Equal("waits off board to enter, in Motion", panel.Find("#vehicle-state").TextContent);
        Assert.Equal(string.Empty, panel.Find("#vehicle-state").GetAttribute("data-vca"));
        Assert.NotNull(panel.Find("#vehicle-recall"));
    }

    // A19 (rulings R26.1, R26.2): entry from off board by hex and VCA; refused steps disabled with their reason; the hex wished to enter next by a VCA
    // hex; Passengers.
    [Fact]
    public void TheStepChoicesProposeEntriesPassengersAndTheHexWishedToEnterNext()
    {
        VehicleStepChoices.StepRequest? request = null;
        string? intended = null;
        (string Unit, bool Chosen)? unloading = null;
        string? hexChosen = null;
        var choices = context.Render<VehicleStepChoices>(parameters => parameters
            .Add(item => item.EntryHexes, [new VehicleStepChoices.EntryHexChoice("bd04:I1:0", "bd04:I1:0 (1 MP)", null),
                new VehicleStepChoices.EntryHexChoice("bd04:K1:0", "bd04:K1:0: not open", "play.entry-occupied: bd04:K1:0 holds a Known enemy unit")])
            .Add(item => item.OnEntryHex, value => hexChosen = value).Add(item => item.OnStep, value => request = value));
        Assert.True(choices.Find("#vehicle-enter-map").HasAttribute("disabled"));
        Assert.True(choices.FindAll("#vehicle-entry-hex option")[2].HasAttribute("disabled"));
        Assert.Empty(choices.FindAll("#vehicle-entry-facing"));
        choices.Find("#vehicle-entry-hex").Change("bd04:I1:0");
        Assert.Equal("bd04:I1:0", hexChosen);
        choices.Render(parameters => parameters.Add(item => item.EntryHex, "bd04:I1:0").Add(item => item.EntryFacings, [UnitFacing.SouthWest, UnitFacing.SouthEast])
            .Add(item => item.EntryFacing, "south-east"));
        Assert.Equal(2, choices.FindAll("#vehicle-entry-facing option").Count);
        choices.Find("#vehicle-enter-map").Click();
        Assert.Equal(new VehicleStepChoices.StepRequest("enter", "bd04:I1:0", "south-east"), request);

        // A refused entry on the map is disabled with its reason.
        choices.Render(parameters => parameters.Add(item => item.EntryHexes, []).Add(item => item.Steps, [new VehicleStepChoices.StepButton(new("enter", "bd04:K2:0"),
            "Propose: enter bd04:K2:0", Class: "vehicle-enter", Data: new Dictionary<string, object> { ["data-to"] = "bd04:K2:0" }, Bar: "a wall")]));
        Assert.True(choices.Find(".vehicle-enter[data-to='bd04:K2:0']").HasAttribute("disabled"));
        Assert.Equal("a wall", choices.Find(".vehicle-enter").GetAttribute("title"));
        Assert.Empty(choices.FindAll("#vehicle-intended"));

        choices.Render(parameters => parameters.Add(item => item.Steps, []).Add(item => item.Moving, true).Add(item => item.IntendedChoices, ["bd04:I2:0", "bd04:J2:0"])
            .Add(item => item.Unloadable, ["gl", "gs"]).Add(item => item.UnloadChosen, new HashSet<string> { "gs" })
            .Add(item => item.OnIntended, value => intended = value).Add(item => item.OnUnloadToggle, value => unloading = value));
        choices.Find(".vehicle-intended[data-at='bd04:J2:0']").Click();
        Assert.Equal("bd04:J2:0", intended);
        choices.Find(".vehicle-unload[value='gl']").Change(true);
        Assert.Equal(("gl", true), unloading);
        choices.Find("#vehicle-unload").Click();
        Assert.Equal("unload", request!.Kind);
        Assert.Empty(choices.FindAll("#vehicle-load"));
    }

    // A20 (C10.11, C10.12; ruling R26.2): unhook with a facing, or hook up with the crew boarding.
    [Fact]
    public void TheTowingActionsHookUpWithTheCrewOrUnhookWithAFacing()
    {
        (string Gun, bool Hooked)? hooked = null;
        bool? boards = null;
        var towing = context.Render<VehicleTowingActions>(parameters => parameters.Add(item => item.Guns, [new VehicleTowingActions.TowChoice("gg", true)])
            .Add(item => item.OnHook, value => hooked = value).Add(item => item.OnBoards, value => boards = value));
        Assert.NotNull(towing.Find("#vehicle-unhook-facing"));
        towing.Find(".vehicle-unhook").Click();
        Assert.Equal(("gg", false), hooked);
        towing.Render(parameters => parameters.Add(item => item.Guns, [new VehicleTowingActions.TowChoice("gg", false)]));
        towing.Find("#vehicle-hook-boards").Change(true);
        Assert.True(boards);
        towing.Find(".vehicle-hook").Click();
        Assert.Equal(("gg", true), hooked);
    }

    // A21, A22, C18: Bounding First Fire waits for a target; a BU toggle names what it does; the arc readings in order.
    [Fact]
    public void BoundingFireCrewExposureAndTheGunArc()
    {
        var fired = false;
        var bff = context.Render<BoundingFireAction>(parameters => parameters.Add(item => item.Targets, ["bd04:I3:0"]).Add(item => item.OnPropose, () => fired = true));
        Assert.True(bff.Find("#vehicle-bff").HasAttribute("disabled"));
        bff.Render(parameters => parameters.Add(item => item.Target, "bd04:I3:0"));
        bff.Find("#vehicle-bff").Click();
        Assert.True(fired);

        CrewExposureActions.Exposure? toggled = null;
        var exposure = context.Render<CrewExposureActions>(parameters => parameters.Add(item => item.Vehicles, [new CrewExposureActions.Exposure("gk", true)])
            .Add(item => item.OnToggle, value => toggled = value));
        Assert.Contains("exposes its crew (CE)", exposure.Find(".bu-toggle").TextContent, StringComparison.Ordinal);
        exposure.Find(".bu-toggle").Click();
        Assert.Equal("gk", toggled!.Id);

        var turned = false;
        var arc = context.Render<GunArcAction>(parameters => parameters.Add(item => item.Arc, [new GunArcAction.ArcReading("bd02:P6:0", "range 1, in its CA")])
            .Add(item => item.OnPropose, () => turned = true));
        Assert.Equal("bd02:P6:0: range 1, in its CA", arc.Find("#ordnance-arc li").TextContent);
        arc.Find("#propose-turn-gun").Click();
        Assert.True(turned);
    }
}
