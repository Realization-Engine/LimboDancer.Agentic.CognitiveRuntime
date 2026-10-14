"""The Canister page test tries the spots the small board offers until the gate accepts a shot (review fixes of pass 55)."""
import io

PATH = r"E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.MapStudio.Tests/PlayPageOrdnanceTests.cs"
text = io.open(PATH, encoding="utf-8", newline="").read()
swaps = [
    ('    /// the board\'s own LOS reads it. No hex of the aim is seen through a Hindrance, which the Fire package would not attribute.\n'
     '    /// </summary>\n'
     '    private (string Gun, string Facing, int Aim, string Squad, bool Adjacent) CanisterSpot()\n',
     '    /// the board\'s own LOS reads it. No hex of the aim is seen through a Hindrance, which the Fire package would not attribute. Every such spot\n'
     '    /// is given: the fire planner reads its own Hindrances, so the test takes the first spot whose shot the gate accepts.\n'
     '    /// </summary>\n'
     '    private IEnumerable<(string Gun, string Facing, int Aim, string Squad, bool Adjacent)> CanisterSpots()\n'),
    ('                        return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{inCluster}:0", false);\n                    }\n\n                    if',
     '                        yield return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{inCluster}:0", false);\n                    }\n                    else if'),
    ('                        return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{beside}:0", true);',
     '                        yield return ($"{Board}:{gun}:0", LimboDancer.Domains.Asl.Units.Documents.UnitFacings.Name(facing), aim.Aim, $"{Board}:{beside}:0", true);'),
    ('            }\n        }\n\n        throw new InvalidOperationException("The test board has no open spot for a Canister aim.");\n    }\n',
     '            }\n        }\n    }\n'),
    ('        // target; choosing Canister puts the five aims of the Covered Arc in the target\'s place, and the shot names its aim.\n'
     '        var spot = CanisterSpot();\n',
     '        // target; choosing Canister puts the five aims of the Covered Arc in the target\'s place, and the shot names its aim.\n'
     '        foreach (var spot in CanisterSpots())\n'
     '        {\n'
     '            using var attempt = new PlayPageOrdnanceTests();\n'
     '            if (attempt.FiresCanister(spot))\n'
     '            {\n'
     '                return;\n'
     '            }\n'
     '        }\n'
     '\n'
     '        Assert.Fail("The test board has no spot from which the gate accepts a Canister shot.");\n'
     '    }\n'
     '\n'
     '    /// <summary>The page fires Canister from the spot; false when the gate refuses the shot there, for a Hindrance the fire planner reads.</summary>\n'
     '    private bool FiresCanister((string Gun, string Facing, int Aim, string Squad, bool Adjacent) spot)\n'
     '    {\n'),
    ('        page.WaitForAssertion(() => Assert.True(page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal), $"{spot}: {aim.TextContent}: {page.Find("#play-proposal").TextContent}"));\n',
     '        page.WaitForAssertion(() => Assert.Matches("Confirm to commit|Refused", page.Find("#play-outcome").TextContent));\n'
     '        if (!page.Find("#play-outcome").TextContent.Contains("Confirm to commit", StringComparison.Ordinal))\n'
     '        {\n'
     '            Assert.Contains("LOS Hindrance", page.Find("#play-proposal").TextContent, StringComparison.Ordinal);\n'
     '            dice.Clear();\n'
     '            return false;\n'
     '        }\n'
     '\n'),
    ('        Assert.Contains("it may fire again this phase (C2.24)", record, StringComparison.Ordinal);\n    }\n}',
     '        Assert.Contains("it may fire again this phase (C2.24)", record, StringComparison.Ordinal);\n        return true;\n    }\n}'),
]
for old, new in swaps:
    count = text.count(old)
    print(count, old[:70].replace("\n", " "))
    if count != 1:
        raise SystemExit("not exactly one: " + old[:70])
    text = text.replace(old, new)
io.open(PATH, "w", encoding="utf-8", newline="").write(text)
