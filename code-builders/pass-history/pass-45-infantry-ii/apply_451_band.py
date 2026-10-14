"""Pass 45, task 45.1: the range band tells the next level of a hex (PBF) from a farther one (no attack), and the page reads it so."""
import sys

ASL = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/"
PAGE = "LimboDancer.Domains.Asl.MapStudio/Components/Pages/Play.razor"
EDITS = {
    "LimboDancer.Domains.Asl.Rules/FireRange.cs": [
        ("""    /// <summary>An adjacent target at most one level above: PBF (A7.21).</summary>
""",
         """    /// <summary>A level of the firer's own hex that is not the next one up or down: no attack (B23.26; pass 45).</summary>
    SameHexFartherLevel,

    /// <summary>An adjacent target at most one level above: PBF (A7.21).</summary>
""", 1),
        ("""            : range < 1 ? FireRangeBand.SameHexOtherLevel
""",
         """            : range < 1 ? Math.Abs(levelAbove) == 1 && !noPointBlank ? FireRangeBand.SameHexOtherLevel : FireRangeBand.SameHexFartherLevel
""", 1),
        ("""            FireRangeBand.PointBlank => "PBF, FP x2 (A7.21)",
""",
         """            FireRangeBand.SameHexFartherLevel => "no attack: not the next level of its hex (B23.26)",
            FireRangeBand.PointBlank => "PBF, FP x2 (A7.21)",
""", 1),
    ],
    "LimboDancer.Domains.Asl.Rules/ScenarioA1ResultTables.cs": [
        ("""                else if (reading.Band == FireRangeBand.SameHexOtherLevel)
""",
         """                else if (reading.Band is FireRangeBand.SameHexOtherLevel or FireRangeBand.SameHexFartherLevel)
""", 1),
    ],
    PAGE: [
        ("""kind is not null || own.Band is FireRangeBand.Out or FireRangeBand.SameHexOtherLevel""",
         """kind is not null || own.Band is FireRangeBand.Out or FireRangeBand.SameHexFartherLevel""", 1),
        ("""                        reading.Band is FireRangeBand.Out or FireRangeBand.SameHexOtherLevel));
""",
         """                        reading.Band is FireRangeBand.Out or FireRangeBand.SameHexFartherLevel));
""", 1),
        ("""FireRange.Words(mg), mg.Band, mg.Band is FireRangeBand.Out or FireRangeBand.SameHexOtherLevel));""",
         """FireRange.Words(mg), mg.Band, mg.Band is FireRangeBand.Out or FireRangeBand.SameHexOtherLevel or FireRangeBand.SameHexFartherLevel));""", 1),
        ("""        var pointBlank = rows.Where(row => row.Band == FireRangeBand.PointBlank).ToArray();
""",
         """        var pointBlank = rows.Where(row => row.Band is FireRangeBand.PointBlank or FireRangeBand.SameHexOtherLevel && !row.Out).ToArray();
""", 1),
        ("""outs.Any(row => row.Band is not (FireRangeBand.Out or FireRangeBand.SameHexOtherLevel)) ? "no fire there" : "out of range";""",
         """outs.Any(row => row.Band is not (FireRangeBand.Out or FireRangeBand.SameHexOtherLevel or FireRangeBand.SameHexFartherLevel)) ? "no fire there" : "out of range";""", 1),
    ],
    "tests/LimboDancer.Domains.Asl.Rules.Tests/FireRangeTests.cs": [
        ("""        var level = FireRange.Band(definition, 0, sameLocation: false)!;
        Assert.Equal(FireRangeBand.SameHexOtherLevel, level.Band);
        Assert.Equal(2m, level.Multiplier);
""",
         """        var level = FireRange.Band(definition, 0, sameLocation: false, levelAbove: 1)!;
        Assert.Equal(FireRangeBand.SameHexOtherLevel, level.Band);
        Assert.Equal(2m, level.Multiplier);
        var farther = FireRange.Band(definition, 0, sameLocation: false, levelAbove: 2)!;
        Assert.Equal(FireRangeBand.SameHexFartherLevel, farther.Band);
        Assert.Null(farther.Multiplier);
""", 1),
    ],
}

texts = {}
for name, edits in EDITS.items():
    with open(ASL + name, encoding="utf-8", newline="") as f:
        text = f.read()
    for old, new, count in edits:
        if text.count(old) != count:
            sys.exit(f"{name}: anchor found {text.count(old)} times, not {count}: {old[:70]!r}")
        text = text.replace(old, new)
    texts[name] = text
for name, text in texts.items():
    with open(ASL + name, "w", encoding="utf-8", newline="") as f:
        f.write(text)
    print("written", name)
