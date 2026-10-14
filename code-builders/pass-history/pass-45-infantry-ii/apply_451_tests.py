"""Pass 45, task 45.1: the range band's tests and the range refusal's sentence."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
EDITS = {
    "tests/LimboDancer.Domains.Asl.Rules.Tests/FireRangeTests.cs": [
        ("""        // Range 0 at another Location of the hex: the package refuses it, and the band says it is not built.
        var level = FireRange.Band(definition, 0, sameLocation: false)!;
        Assert.Equal(FireRangeBand.SameHexOtherLevel, level.Band);
        Assert.Null(level.Multiplier);
""",
         """        // Range 0 at another Location of the hex (pass 45; A7.21, B23.26): PBF at the next level; the package refuses it at the same level.
        var level = FireRange.Band(definition, 0, sameLocation: false)!;
        Assert.Equal(FireRangeBand.SameHexOtherLevel, level.Band);
        Assert.Equal(2m, level.Multiplier);
"""),
        ("""    [InlineData(FireRangeBand.SameHexOtherLevel, "the same hex, another level: not built")]
""",
         """    [InlineData(FireRangeBand.SameHexOtherLevel, "PBF, FP x2, at the next level of its hex by a stairwell (A7.21, B23.26)")]
"""),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1ResultTables.cs": [
        ("""                    named.Add($"{code}: {who} in {at} fires at {target}, another level of its own hex, which is not built (A7.21)");
""",
         """                    named.Add($"{code}: {who} in {at} fires at {target}, another level of its own hex, which is not the next level up or down (B23.26)");
"""),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/ScenarioA1Pass45RulesTests.cs": [
        ("""    [Fact]
    public void CasualtyReductionEliminatesACrew()
""",
         """    [Fact]
    public void FireAtTheNextLevelOfTheFirersHexIsPointBlankFire()
    {
        // A7.21, B23.26 (pp. 54, 136): "doubled while ... ADJACENT to its target"; "Attacks up or down a stairwell hex ... are limited to the next
        // higher or lower level." Two 4-4-7 at level 1 fire at ground level of their hex: 8 FP each, not the 12 of TPBF.
        const string upstairs = "bd01:G5:1";
        FireFirer Up(string id) => new(id, "defender-squad", upstairs, false, false, false, false, false);
        var down = Attack([6, 5], firers: [Up("ru-1"), Up("ru-2")]) with
        {
            FirerLocationId = upstairs,
            Range = 0,
            SameLevel = false,
            TargetLevelAbove = -1,
        };
        var result = Complete(down);
        Assert.Equal(FireResolution.Resolved, result.Disposition);
        Assert.Equal(16m, result.Arithmetic!.TotalFirepower);
        Assert.All(result.Arithmetic.Firers, firer => Assert.Contains(firer.Multipliers, item => item.Name == "point-blank-fire"));

        // Two levels apart there is no attack.
        Assert.Contains("asl.a1.fire.out-of-range", ScenarioA1FireCalculator.Resolve(down with { TargetLevelAbove = -2 }, Reference).Reasons);

        // The planner's read: the next level, and a stairwell between them.
        Assert.Null(ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 1, () => true));
        Assert.StartsWith("play.fire-level", ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 1, () => false));
        Assert.StartsWith("play.fire-level", ScenarioA1FireMapRules.SameHexLevelBar("a", "b", 2, () => true));
    }

    [Fact]
    public void FireFromAboveAndFromBelowEncircles()
    {
        // A7.7 (p. 57): "An Encirclement can also be created by a LOF from both the Location directly above and below it in a building hex."
        Assert.True(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove, ScenarioA1FireMapRules.FromBelow]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromAbove, 1]));
        Assert.False(ScenarioA1Geometry.Encircles([ScenarioA1FireMapRules.FromBelow, 7]));

        // The hexside cases stand as they were: opposite hexsides, and opposite hexspines.
        Assert.True(ScenarioA1Geometry.Encircles([1, 7]));
        Assert.True(ScenarioA1Geometry.Encircles([0, 6]));
    }

    [Fact]
    public void CasualtyReductionEliminatesACrew()
"""),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new in edits:
        if text.count(old) != 1:
            sys.exit(f"{name}: anchor found {text.count(old)} times: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
