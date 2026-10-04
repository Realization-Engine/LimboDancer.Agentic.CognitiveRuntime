using Bunit;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.MapStudio.Components.Board;
using LimboDancer.Domains.Asl.MapStudio.Components.Play;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Rules;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

/// <summary>
/// Pass 31c after its report (design section 14; rulings R31c.6 and R31c.7): the Range tab, the fire panel's targets read by range, the dropdown of
/// units, the surrender block by name, and hexes in brackets. Each component is rendered on its own with what the page would give it.
/// </summary>
public sealed class Pass31cRangeTests : IDisposable
{
    private readonly BunitContext context = new();

    public void Dispose() => context.Dispose();

    private static RangeRow Row(string key, string name, int normal, int limit, FireRangeBand band, bool beyond = false) =>
        new(key, name, "bd01:G4:0", normal, limit, FireRange.Words(new FireRangeReading(band, 7, normal, limit)), band, beyond);

    [Fact]
    public void TheRangeTabGivesTheHexesAloneWithNoGame()
    {
        var read = 0;
        var panel = context.Render<RangePanel>(parameters => parameters.Add(item => item.Draft, new LosPanel.LosDraft("bd01:G4:0", "bd01:N5:1", false))
            .Add(item => item.OnCheck, () => read++));
        Assert.Empty(panel.FindAll("#range-result"));
        Assert.True(panel.Find("#range-clear").HasAttribute("disabled"));
        panel.Find("#range-check").Click();
        Assert.Equal(1, read);

        panel.Render(parameters => parameters.Add(item => item.Result,
            new RangeCheck("bd01:G4:0", "bd01:N5:1", 7, null, BoardLocation.Parse("bd01:G4:0"), BoardLocation.Parse("bd01:N5:1"), new HexIndex(6, 3), new HexIndex(13, 4))));
        Assert.Equal("[G4] to [N5], level 1: 7 hexes.", panel.Find("#range-result").TextContent);
        Assert.Empty(panel.FindAll("#range-rows"));
    }

    [Fact]
    public void TheRangeTabReadsEachUnitAndWeaponAndDrawsTheOneChosen()
    {
        string? chosen = null;
        var cleared = 0;
        var panel = context.Render<RangePanel>(parameters => parameters.Add(item => item.Draft, new LosPanel.LosDraft("bd01:G4:0", "bd01:N5:1", false))
            .Add(item => item.Result, new RangeCheck("bd01:G4:0", "bd01:N5:1", 7, null, BoardLocation.Parse("bd01:G4:0"), BoardLocation.Parse("bd01:N5:1"), new HexIndex(6, 3), new HexIndex(13, 4)))
            .Add(item => item.Rows, [Row("g1", "4-6-7 squad G1", 6, 12, FireRangeBand.LongRange), Row("mg", "the MMG of 4-6-7 squad G1", 12, 24, FireRangeBand.Normal),
                Row("r1", "4-2-6 squad R3", 2, 4, FireRangeBand.Out, beyond: true)])
            .Add(item => item.Chosen, "g1").Add(item => item.OnChoose, key => chosen = key).Add(item => item.OnClear, () => cleared++));
        var rows = panel.FindAll("#range-rows tbody tr");
        Assert.Equal(3, rows.Count);
        Assert.Contains("Long Range, FP x1/2 (A7.22)", rows[0].TextContent, StringComparison.Ordinal);
        Assert.Contains("Normal Range", rows[1].TextContent, StringComparison.Ordinal);
        Assert.Contains("out of range: it fires to 4", rows[2].TextContent, StringComparison.Ordinal);
        Assert.Equal("range-out", rows[2].GetAttribute("class"));
        Assert.True(rows[0].QuerySelector("input")!.HasAttribute("checked"));
        Assert.False(rows[1].QuerySelector("input")!.HasAttribute("checked"));
        rows[1].QuerySelector("input")!.Change(true);
        Assert.Equal("mg", chosen);
        panel.Find("#range-clear").Click();
        Assert.Equal(1, cleared);
    }

    [Fact]
    public void ARangeThatCouldNotBeReadSaysWhy()
    {
        var panel = context.Render<RangePanel>(parameters => parameters.Add(item => item.Draft, new LosPanel.LosDraft("nowhere", "bd01:N5:1", false))
            .Add(item => item.Result, new RangeCheck("nowhere", "bd01:N5:1", null, "Give both locations as board:hex:level, such as bd01:E4:0.", null, null, null, null)));
        Assert.Equal("fail", panel.Find("#range-result").GetAttribute("class"));
        Assert.Contains("board:hex:level", panel.Find("#range-result").TextContent, StringComparison.Ordinal);
    }

    [Fact]
    public void TheFirePanelReadsItsTargetsByRangeBeforeAnyProposal()
    {
        var shown = 0;
        var panel = context.Render<SmallArmsFirePanel>(parameters => parameters.Add(item => item.Side, "german").Add(item => item.Phase, "pfph")
            .Add(item => item.Locations, ["bd01:G4:0"]).Add(item => item.From, "bd01:G4:0").Add(item => item.Firers, ["g1"])
            .Add(item => item.SelectedFirers, new HashSet<string> { "g1" }).Add(item => item.Targets, ["bd01:N5:1", "bd01:R4:0"]).Add(item => item.Target, "bd01:N5:1")
            .Add(item => item.TargetNotes, new Dictionary<string, string> { ["bd01:N5:1"] = "7 hexes, Long Range", ["bd01:R4:0"] = "14 hexes, out of range" })
            .Add(item => item.TargetsOut, new HashSet<string> { "bd01:R4:0" }).Add(item => item.RangeSummary, "7 hexes, Long Range")
            .Add(item => item.RangeRows, [Row("g1", "4-6-7 squad G1", 6, 12, FireRangeBand.LongRange)]).Add(item => item.OnShowRange, () => shown++));
        var options = panel.FindAll("#fire-target option");
        Assert.Equal("bd01:N5:1: 7 hexes, Long Range", options[1].TextContent);
        Assert.False(options[1].HasAttribute("disabled"));
        Assert.Equal("bd01:R4:0: 14 hexes, out of range", options[2].TextContent);
        Assert.True(options[2].HasAttribute("disabled"));

        // The range is a closed line under the select, with a line for each firer and weapon.
        var range = panel.Find("details#fire-range");
        Assert.False(range.HasAttribute("open"));
        Assert.Equal("Range: 7 hexes, Long Range", range.QuerySelector("summary")!.TextContent);
        Assert.Equal("4-6-7 squad G1, Normal Range 6: Long Range, FP x1/2 (A7.22)", range.QuerySelector("li")!.TextContent);
        panel.Find("#fire-range-show").Click();
        Assert.Equal(1, shown);

        // Before a firer is ticked the page gives no notes, and the list reads as it did.
        panel.Render(parameters => parameters.Add(item => item.TargetNotes, new Dictionary<string, string>()).Add(item => item.TargetsOut, new HashSet<string>())
            .Add(item => item.RangeSummary, null));
        Assert.Equal("bd01:N5:1", panel.FindAll("#fire-target option")[1].TextContent);
        Assert.Empty(panel.FindAll("#fire-range"));
    }

    [Fact]
    public void ANamedGroupOfUnitsIsADropdownOfCheckboxes()
    {
        UnitSelectionList.Row[] rows = [new("g1", "4-6-7 squad G1 ([G4])"), new("g2", "4-6-7 squad G2 ([G4])"), new("g3", "8-1 leader G1 ([G5])"), new("g4", "Dummy G1 ([T4])")];
        var list = context.Render<UnitSelectionList>(parameters => parameters.Add(item => item.Rows, rows).Add(item => item.Selected, new HashSet<string>())
            .Add(item => item.CssClass, "move-unit").Add(item => item.Legend, "Units moving"));
        var dropdown = list.Find("details.move-units.unit-dropdown");
        Assert.False(dropdown.HasAttribute("open"));
        Assert.Equal("Units moving", dropdown.QuerySelector("summary .unit-dropdown-name")!.TextContent);
        Assert.Equal("choose (4 offered)", dropdown.QuerySelector("summary .unit-dropdown-chosen")!.TextContent);
        Assert.Equal(4, dropdown.QuerySelectorAll(".unit-dropdown-list .move-unit").Length);

        // The closed line names the units ticked while they fit, then counts them.
        list.Render(parameters => parameters.Add(item => item.Selected, new HashSet<string> { "g1", "g3" }));
        Assert.Equal("4-6-7 squad G1 ([G4]); 8-1 leader G1 ([G5])", list.Find(".unit-dropdown-chosen").TextContent);
        list.Render(parameters => parameters.Add(item => item.Selected, new HashSet<string> { "g1", "g2", "g3" }));
        Assert.Equal("3 of 4 chosen", list.Find(".unit-dropdown-chosen").TextContent);

        // With no unit to offer it says so, in place of an empty box.
        list.Render(parameters => parameters.Add(item => item.Rows, []).Add(item => item.Empty, "none may move now."));
        Assert.Empty(list.FindAll("details"));
        Assert.Equal("Units moving none may move now.", list.Find("p.move-units.unit-dropdown-empty").TextContent);
    }

    [Fact]
    public void ASurrenderSaysItsUnitsByNameWithTheirHexesOneAnswerToALine()
    {
        string? taken = null;
        var block = context.Render<PendingSurrenderPanel>(parameters => parameters
            .AddCascadingValue("Say", (Func<string, string>)(text => text == "g-leader-1" ? "9-2 leader G3" : text == "r-squad-19" ? "6-2-8 squad R19" : text))
            .AddCascadingValue("SayAt", (Func<string, string>)(text => text == "g-leader-1" ? "9-2 leader G3 in [H5]" : text == "r-squad-19" ? "6-2-8 squad R19 in [H4]" : text))
            .Add(item => item.Unit, "g-leader-1").Add(item => item.MayAnswer, true).Add(item => item.Captors, ["r-squad-19"]).Add(item => item.OnTake, captor => taken = captor));
        Assert.StartsWith("9-2 leader G3 in [H5] has surrendered (A15.5)", block.Find(".pending-surrender > p.fail").TextContent, StringComparison.Ordinal);
        var answers = block.FindAll(".pending-surrender > button");
        Assert.Equal(3, answers.Count);
        Assert.Equal("Propose: 6-2-8 squad R19 in [H4] takes it prisoner", answers[0].TextContent);
        Assert.DoesNotContain("g-leader-1", block.Find(".pending-surrender").TextContent, StringComparison.Ordinal);
        answers[0].Click();
        Assert.Equal("r-squad-19", taken);

        // The other side reads the sentence and is offered no answer.
        block.Render(parameters => parameters.Add(item => item.MayAnswer, false));
        Assert.Empty(block.FindAll("button"));
    }

    [Theory]
    [InlineData("bd01:G4:0", "[G4]")]
    [InlineData("bd01:G4:1", "[G4], level 1")]
    [InlineData("bd01:G4:-1", "[G4], cellar")]
    public void AHexIsWrittenInBrackets(string location, string words) =>
        Assert.Equal(words, DisplayText.Place(1, BoardLocation.Parse(location)));

    [Fact]
    public void TheVictoryAccountWritesItsHexesAndBuildingsInBrackets()
    {
        Assert.Equal("Russian Controls 8 hexes of building [X3], at least 6 (A26.13)", DisplayText.Hexes("Russian Controls 8 hexes of building X3, at least 6 (A26.13)", 1));
        Assert.Equal("hexes [W4], [W5], [Y4]: russian", DisplayText.Hexes("hexes bd01:W4, bd01:W5, bd01:Y4: russian", 1));
        Assert.Equal("hexes [W4] on board 01: russian", DisplayText.Hexes("hexes bd01:W4: russian", 2));

        // A Location's whole identifier is left for the names' own pass, and a hex already in brackets is not bracketed twice.
        Assert.Equal("bd01:W4:1", DisplayText.Hexes("bd01:W4:1", 1));
        Assert.Equal("Building [X3]: Russian", DisplayText.Hexes(DisplayText.Hexes("Building X3: Russian", 1), 1));
    }

    [Fact]
    public void TheGamesOwnRefusalIsReadWithoutItsCodeAndRevision()
    {
        var (code, text) = DisplayText.Reason("UNIT-STATE-030 revision 525 (advance-7d5a27e6301d-1): The CC in bd01:X4:0 awaits the ambushed side's round, which may declare no attacks (A11.32).");
        Assert.Equal("UNIT-STATE-030", code);
        Assert.Equal("The CC in bd01:X4:0 awaits the ambushed side's round, which may declare no attacks (A11.32).", text);
        Assert.DoesNotContain("revision", text, StringComparison.Ordinal);
    }
}
