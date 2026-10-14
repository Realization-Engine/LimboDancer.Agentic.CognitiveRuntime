"""Increment 2g: the Gun Duel page test uses two ADJACENT hexes and a facing computed from the grid, LF kept."""
import io

PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageVehicleTests.cs"
with io.open(PATH, "r", encoding="utf-8", newline="") as handle:
    text = handle.read()
start = text.index("    /// <summary>\n    /// Two Open Ground Locations of one row, an even number of columns apart")
end = text.index("    [Fact]\n    public void AShotDeclaredAtAMovingTankOffersItsSideAGunDuel()")
NEW = '''    /// <summary>
    /// The facing, one hexspine clockwise of the bearing from one ADJACENT hex to the other, whose Covered Arc has the second hex on its edge
    /// (C3.2), read from the grid as the planner reads a bearing: flat-topped hexes in columns, the odd columns half a hex higher.
    /// </summary>
    private string FacingToward(string from, string to)
    {
        var handle = new StudioBoardCatalog(boards).TryGetBoard(FakeBoardProvider.Board.Ref).Board!;
        (double X, double Y) Center(string location)
        {
            Assert.True(handle.Geometry.TryGetIndex(BoardLocation.Parse(location).Hex, out var hex));
            return (1.5 * hex.Column, Math.Sqrt(3) * (hex.Row - (0.5 * (hex.Column % 2))));
        }

        var (one, two) = (Center(from), Center(to));
        var bearing = Math.Atan2(-(two.Y - one.Y), two.X - one.X) * 180 / Math.PI;
        return ((((int)Math.Round((bearing - 30) / 60) * 60) % 360) + 360) % 360 switch
        {
            0 => "east",
            60 => "north-east",
            120 => "north-west",
            180 => "west",
            240 => "south-west",
            _ => "south-east",
        };
    }

'''
text = text[:start] + NEW + text[end:]
text = text.replace("        var (west, east) = AlongAHexspine();\n", "        var (west, east) = Hexes();\n        var facing = FacingToward(west, east);\n")
text = text.replace('        Place(page, "de-tank", "attacker-tank", west, "east");\n        page.Find("#place-definition").Change("defender-crew");',
                    '        Place(page, "de-tank", "attacker-tank", west, facing);\n        page.Find("#place-definition").Change("defender-crew");')
text = text.replace('        page.Find("#place-holder").Change("ru-crew");\n        page.Find("#place-facing").Change("east");\n        page.Find("#place-add").Click();\n        Commit(page, "#propose-setup");\n        page.EndPhase(Commit);\n        page.EndPhase(Commit);\n        page.Find("#vehicle-unit").Change("de-tank");',
                    '        page.Find("#place-holder").Change("ru-crew");\n        page.Find("#place-facing").Change(facing);\n        page.Find("#place-add").Click();\n        Commit(page, "#propose-setup");\n        page.EndPhase(Commit);\n        page.EndPhase(Commit);\n        page.Find("#vehicle-unit").Change("de-tank");')
text = text.replace("        // Pass 55 (C2.2401; rulings R55.19, R55.20): the AT Gun in the open must turn three hexspines, Case A +5; the PzKpfw IIIH that has just\n        // spent its Start MP in its LOS would fire with Case C +4, Case I +1, and its Armor Leader's -2: +3.",
                    "        // Pass 55 (C2.2401; rulings R55.19, R55.20): the AT Gun in the open, its barrel turned away from the ADJACENT tank, must turn two\n        // hexspines, Case A +4; the PzKpfw IIIH that has just spent its Start MP would fire with Case C +4, Case I +1, and its Armor Leader's -2: +3.")
text = text.replace('        Assert.Contains("+5 (Case A +5)", waiting.TextContent, StringComparison.Ordinal);', '        Assert.Contains("+4 (Case A +4)", waiting.TextContent, StringComparison.Ordinal);')
with io.open(PATH, "w", encoding="utf-8", newline="") as handle:
    handle.write(text)
print(text.count("FacingToward"), text.count("AlongAHexspine"), text.count("Change(facing)"), text.count("+4 (Case A +4)"))
