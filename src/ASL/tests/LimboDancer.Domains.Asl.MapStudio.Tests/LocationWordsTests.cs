using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31c (designs D12 and D13; design section 9, "Locations" and "Picking"): the one way a Location is written for a player
/// (<see cref="DisplayText.Place(int, BoardLocation)"/>), each form a player may type read back into the identifier
/// (<see cref="DisplayText.ReadPlace"/>), and the typed field that shows the words, reads them, and takes a hex picked on the map
/// (<see cref="LocationField"/> with the page's <see cref="LocationPicking"/>).
/// </summary>
public sealed class LocationWordsTests : IDisposable
{
    private static readonly string[] OneBoard = ["bd01"];
    private static readonly string[] TwoBoards = ["bd01", "bd02"];
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    // On a map of one board the board is not said; on a map of several it is. Ground level is the hex alone, and level -1 the cellar.
    [Theory]
    [InlineData(1, "bd01:G4:0", "[G4]")]
    [InlineData(1, "bd01:G4:1", "[G4], level 1")]
    [InlineData(1, "bd01:G4:2", "[G4], level 2")]
    [InlineData(1, "bd01:G4:-1", "[G4], cellar")]
    [InlineData(1, "bd01:AA10:0", "[AA10]")]
    [InlineData(1, "ab-synthetic:B1:0", "[B1]")]
    [InlineData(2, "bd01:G4:0", "[G4] on board 01")]
    [InlineData(2, "bd01:G4:1", "[G4] on board 01, level 1")]
    [InlineData(2, "bd02:G4:-1", "[G4] on board 02, cellar")]
    [InlineData(3, "bd17:Q10:2", "[Q10] on board 17, level 2")]
    [InlineData(2, "ab-synthetic:B1:1", "[B1] on ab-synthetic, level 1")]
    public void ALocationIsWrittenOneWayOnOneBoardAndOnSeveral(int boards, string location, string words)
    {
        Assert.Equal(words, DisplayText.Place(boards, BoardLocation.Parse(location)));
        Assert.Equal(words, DisplayText.Place(boards, location));
    }

    [Fact]
    public void ATextThatIsNoLocationIsLeftAsItIs()
    {
        Assert.Equal("the top edge", DisplayText.Place(1, "the top edge"));
        Assert.Equal(string.Empty, DisplayText.Place(2, string.Empty));
        Assert.Equal("bd01:G4", DisplayText.Place(1, "bd01:G4"));
    }

    // Each form a player may type, on a map of one board: the short hex, the hex with its level, the words the page writes (with or without
    // the brackets), and the identifier, in either case.
    [Theory]
    [InlineData("G4", "bd01:G4:0")]
    [InlineData("G4:1", "bd01:G4:1")]
    [InlineData("G4:-1", "bd01:G4:-1")]
    [InlineData("[G4]", "bd01:G4:0")]
    [InlineData("[G4]:1", "bd01:G4:1")]
    [InlineData("[G4], level 1", "bd01:G4:1")]
    [InlineData("G4, level 1", "bd01:G4:1")]
    [InlineData("G4 level 2", "bd01:G4:2")]
    [InlineData("G4 1", "bd01:G4:1")]
    [InlineData("[G4], cellar", "bd01:G4:-1")]
    [InlineData("G4 cellar", "bd01:G4:-1")]
    [InlineData("[G4] on board 01, level 1", "bd01:G4:1")]
    [InlineData("G4 on board 01", "bd01:G4:0")]
    [InlineData("bd01:G4:1", "bd01:G4:1")]
    [InlineData("bd01:g4:1", "bd01:G4:1")]
    [InlineData("BD01:G4:0", "bd01:G4:0")]
    [InlineData("g4", "bd01:G4:0")]
    [InlineData("g4:1", "bd01:G4:1")]
    [InlineData("[g4], Level 1", "bd01:G4:1")]
    [InlineData("  G4  ", "bd01:G4:0")]
    [InlineData("aa10", "bd01:AA10:0")]
    public void EachTypedFormIsReadIntoTheIdentifier(string typed, string identifier) =>
        Assert.Equal(identifier, DisplayText.ReadPlace(typed, OneBoard));

    [Fact]
    public void ATypedLocationIsReadOnTheMapsOwnBoards()
    {
        // On a map of several boards a hex without its board is not guessed: it is returned as typed, for the gate to refuse in its own words.
        Assert.Equal("G4", DisplayText.ReadPlace("G4", TwoBoards));
        Assert.Equal("G4:1", DisplayText.ReadPlace("[G4]:1", TwoBoards));
        Assert.Equal("bd02:G4:0", DisplayText.ReadPlace("G4 on board 02", TwoBoards));
        Assert.Equal("bd02:G4:1", DisplayText.ReadPlace("[G4] on board 02, level 1", TwoBoards));
        Assert.Equal("bd01:G4:-1", DisplayText.ReadPlace("[G4] on board 01, cellar", TwoBoards));

        // An authored board is a board like any other.
        Assert.Equal("ab-synthetic:B1:0", DisplayText.ReadPlace("B1", ["ab-synthetic"]));
        Assert.Equal("ab-synthetic:B1:1", DisplayText.ReadPlace("[B1], level 1", ["ab-synthetic"]));
        Assert.Equal("ab-synthetic:B1:0", DisplayText.ReadPlace("ab-synthetic:B1:0", ["ab-synthetic"]));

        // What is none of these is returned as typed, and nothing is made of an empty field.
        Assert.Equal("the building", DisplayText.ReadPlace("the building", OneBoard));
        Assert.Equal("G4 level one", DisplayText.ReadPlace("G4 level one", OneBoard));
        Assert.Equal(string.Empty, DisplayText.ReadPlace("   ", OneBoard));
        Assert.Equal("G4", DisplayText.ReadPlace("G4", []));
    }

    // What the page writes is what it reads: every Location's words come back as its identifier, on one board and on several.
    [Fact]
    public void TheWordsOfALocationAreReadBackAsItsIdentifier()
    {
        string[] locations = ["bd01:G4:0", "bd01:G4:1", "bd01:G4:2", "bd01:G4:-1", "bd01:AA10:0", "bd01:A1:1", "bd02:Q10:-1", "bd02:GG5:2"];
        Assert.All(locations.Where(location => location.StartsWith("bd01", StringComparison.Ordinal)),
            location => Assert.Equal(location, DisplayText.ReadPlace(DisplayText.Place(1, location), OneBoard)));
        Assert.All(locations, location => Assert.Equal(location, DisplayText.ReadPlace(DisplayText.Place(2, location), TwoBoards)));
    }

    private static LocationPicking Picking(string[] boards, params int[] levels)
    {
        var picking = new LocationPicking { Boards = () => boards, Levels = _ => levels };
        picking.Arm = (id, label) => (picking.Armed, picking.ArmedLabel) = (id, label);
        return picking;
    }

    // Design D12: the field shows the Location in the game's words, and reads what is typed into the identifier the caller keeps.
    [Fact]
    public void ATypedFieldShowsTheWordsAndRaisesTheIdentifier()
    {
        var taken = string.Empty;
        var field = context.Render<LocationField>(parameters => parameters.AddCascadingValue("Picking", Picking(OneBoard, 0, 1, 2))
            .Add(item => item.Id, "move-to").Add(item => item.Label, "To").Add(item => item.Value, "bd01:G4:1").Add(item => item.OnChange, at => taken = at));
        Assert.Equal("[G4], level 1", field.Find("#move-to").GetAttribute("value"));
        field.Find("#move-to").Change("[h5], level 2");
        Assert.Equal("bd01:H5:2", taken);
        field.Find("#move-to").Change("h5");
        Assert.Equal("bd01:H5:0", taken);

        // A hex with more than one level offers them, and a level chosen keeps the hex.
        Assert.Equal(["ground level", "level 1", "level 2"], field.FindAll("#move-to-level option").Select(option => option.TextContent));
        field.Find("#move-to-level").Change("2");
        Assert.Equal("bd01:G4:2", taken);

        // On a map of several boards the words say the board.
        var several = context.Render<LocationField>(parameters => parameters.AddCascadingValue("Picking", Picking(TwoBoards, 0))
            .Add(item => item.Id, "advance-to").Add(item => item.Label, "To").Add(item => item.Value, "bd02:G4:0"));
        Assert.Equal("[G4] on board 02", several.Find("#advance-to").GetAttribute("value"));
        Assert.Empty(several.FindAll("#advance-to-level"));
    }

    // Design D13: a field is armed, the next hex picked fills it and disarms it; the pick that was already there when it was armed does not.
    [Fact]
    public void AnArmedFieldTakesTheNextPickAndDisarms()
    {
        var picking = Picking(OneBoard, 0);
        (picking.Picked, picking.Serial) = ("bd01:C3:0", 4);
        var taken = string.Empty;
        var field = context.Render<LocationField>(parameters => parameters.AddCascadingValue("Picking", picking)
            .Add(item => item.Id, "move-to").Add(item => item.Label, "To").Add(item => item.OnChange, at => taken = at));
        Assert.Equal("false", field.Find(".location-field").GetAttribute("data-armed"));
        Assert.Equal("Pick on the map", field.Find("#move-to-pick").TextContent);

        field.Find("#move-to-pick").Click();
        Assert.Equal(("move-to", "fills \"To\""), (picking.Armed, picking.ArmedLabel));

        // The page renders again when a field is armed: the hex picked before the arming is not taken.
        field.Render(parameters => parameters.Add(item => item.Value, taken));
        Assert.Equal(string.Empty, taken);
        Assert.Equal("true", field.Find(".location-field").GetAttribute("data-armed"));
        Assert.Equal("Stop picking", field.Find("#move-to-pick").TextContent);
        Assert.Empty(field.FindAll("#move-to-use"));

        // The next pick fills the field, at ground level, and disarms it.
        (picking.Picked, picking.Serial) = ("bd01:D4:0", 5);
        field.Render(parameters => parameters.Add(item => item.Value, taken));
        Assert.Equal("bd01:D4:0", taken);
        Assert.Null(picking.Armed);
        field.Render(parameters => parameters.Add(item => item.Value, taken));
        Assert.Equal("[D4]", field.Find("#move-to").GetAttribute("value"));
        Assert.Equal("false", field.Find(".location-field").GetAttribute("data-armed"));

        // A later pick, with the field not armed, is only offered.
        (picking.Picked, picking.Serial) = ("bd01:E5:0", 6);
        field.Render(parameters => parameters.Add(item => item.Value, taken));
        Assert.Equal("bd01:D4:0", taken);

        // "Stop picking" disarms with nothing taken.
        field.Find("#move-to-pick").Click();
        field.Render(parameters => parameters.Add(item => item.Value, taken));
        field.Find("#move-to-pick").Click();
        Assert.Null(picking.Armed);
        Assert.Equal("bd01:D4:0", taken);
    }

    // Design D13: a route takes one hex a click, in order, and stays armed; its last hex can be taken off, and the whole route cleared.
    [Fact]
    public void ARouteTakesOneHexAClickInOrder()
    {
        var picking = Picking(OneBoard, 0);
        var route = "bd01:G4:0";
        var field = context.Render<LocationField>(parameters => parameters.AddCascadingValue("Picking", picking)
            .Add(item => item.Id, "rout-path").Add(item => item.Label, "Route").Add(item => item.Name, "the rout route").Add(item => item.Many, true)
            .Add(item => item.Value, route).Add(item => item.OnChange, value => route = value));
        Assert.Equal("[G4]", field.Find("#rout-path").GetAttribute("value"));
        field.Find("#rout-path-pick").Click();
        Assert.Equal(("rout-path", "adds to the rout route"), (picking.Armed, picking.ArmedLabel));
        field.Render(parameters => parameters.Add(item => item.Value, route));

        (picking.Picked, picking.Serial) = ("bd01:H4:0", 1);
        field.Render(parameters => parameters.Add(item => item.Value, route));
        Assert.Equal("bd01:G4:0, bd01:H4:0", route);
        (picking.Picked, picking.Serial) = ("bd01:I5:0", 2);
        field.Render(parameters => parameters.Add(item => item.Value, route));
        Assert.Equal("bd01:G4:0, bd01:H4:0, bd01:I5:0", route);
        Assert.Equal("rout-path", picking.Armed);
        field.Render(parameters => parameters.Add(item => item.Value, route));
        Assert.Equal("[G4], [H4], [I5]", field.Find("#rout-path").GetAttribute("value"));

        // A route typed with a level in words is read step by step.
        field.Find("#rout-path").Change("G4, H4, level 1, i5 cellar");
        Assert.Equal("bd01:G4:0, bd01:H4:1, bd01:I5:-1", route);
        field.Render(parameters => parameters.Add(item => item.Value, route));
        Assert.Equal("[G4], [H4]:1, [I5]:-1", field.Find("#rout-path").GetAttribute("value"));

        Assert.Equal("Remove [I5]:-1", field.Find("#rout-path-undo").TextContent);
        field.Find("#rout-path-undo").Click();
        Assert.Equal("bd01:G4:0, bd01:H4:1", route);
        field.Find("#rout-path-clear").Click();
        Assert.Equal(string.Empty, route);
    }

    // A field taken off the page while armed disarms, so no pick goes nowhere.
    [Fact]
    public void AnArmedFieldTakenOffThePageDisarms()
    {
        var picking = Picking(OneBoard, 0);
        using (var other = new BunitContext())
        {
            var field = other.Render<LocationField>(parameters => parameters.AddCascadingValue("Picking", picking)
                .Add(item => item.Id, "move-to").Add(item => item.Label, "To"));
            field.Find("#move-to-pick").Click();
            Assert.Equal("move-to", picking.Armed);
        }

        Assert.Null(picking.Armed);
        Assert.Null(picking.ArmedLabel);
    }
}
