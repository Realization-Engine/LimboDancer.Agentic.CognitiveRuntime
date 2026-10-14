"""Pass 35 review fix 2: the Play test, and a base level for the test board's hexes. Exact blocks, each asserted once; run once."""
PATH = "E:/Archive/GitHub/dlandi/LimboDancer.MCP/src/ASL/tests/LimboDancer.Domains.Asl.Play.Tests/BacklogPass35Tests.cs"
EDITS = [
    ("""    private readonly Dictionary<string, string> terrain = new(StringComparer.Ordinal);
""",
     """    private readonly Dictionary<string, string> terrain = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> levels = new(StringComparer.Ordinal);
"""),
    ("    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain a test draws on it.</summary>",
     "    /// <summary>Board 01's grid, all Open Ground at level 0, with the terrain and the hill levels a test draws on it.</summary>"),
    ("            hexes.Add(new HexFacts(name, index, 0, false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));",
     "            hexes.Add(new HexFacts(name, index, levels.GetValueOrDefault(text), false, center, [center], hexsides, null, CenterTerrainSource.CenterSample));"),
    ("""    [Fact]
    public async Task TwoConcealedUnitsDrawByRandomSelectionForTheOneThatRepulsesTheRout()""",
     """    [Theory]
    [InlineData("E6", true)]
    [InlineData("D5", false)]
    public async Task ARoutUpAHillIsInterdictedOnlyAcrossTheCrestLineTheLosCrosses(string from, bool interdicted)
    {
        // B1.14 (p. 113), B10.31: E5 is a level 1 hill hex, Open Ground, and g1 stands below it in E7. From E6 the squad climbs into E5 across the
        // hexside g1's LOS crosses, so it has no Height Advantage and is Interdicted; from D5 it climbs across another hexside and is not.
        levels["E5"] = 1;
        terrain["E4"] = "Woods";
        await SetupAt(5, "german", Unit("g1", "attacker-squad", "E7", "german"), Unit("r1", "defender-squad", from, "russian", "asl:broken"));
        var before = Revision;
        Committed(await Rout("r1", ["E5", "E4"], interdicted ? Once(3, 3) : null));
        Assert.Equal(interdicted, Since(before).Any(item => item.Payload is RoutInterdicted { Interdictor: "g1" }));
        Assert.Equal(L("E4"), Current.Location("r1")!.Location.ToString());
    }

    [Fact]
    public async Task TwoConcealedUnitsDrawByRandomSelectionForTheOneThatRepulsesTheRout()"""),
]
text = open(PATH, encoding="utf-8", newline="").read()
for old, new in EDITS:
    assert text.count(old) == 1, (text.count(old), old[:70])
    text = text.replace(old, new)
with open(PATH, "w", encoding="utf-8", newline="") as out:
    out.write(text)
print("ok", len(EDITS))
