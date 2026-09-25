using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Plausibility;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Entities that are not units (Unit Display Design, phase 4; ASL-UNIT-026).</summary>
public sealed class EntityTests
{
    [Theory]
    [InlineData("asl:sniper")]
    [InlineData("asl:foxhole")]
    [InlineData("asl:trench")]
    [InlineData("asl:wire")]
    [InlineData("asl:minefield")]
    [InlineData("asl:roadblock")]
    [InlineData("asl:pillbox")]
    [InlineData("asl:fortified-location")]
    [InlineData("asl:smoke")]
    [InlineData("asl:residual")]
    [InlineData("asl:fire")]
    [InlineData("asl:rubble")]
    public void EachEntityIsItsOwnKindOutsideTheUnitTree(string kind)
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.IsA(kind, "asl:entity"));
        Assert.False(vocabulary.IsA(kind, "asl:unit"));
    }

    [Fact]
    public void FortificationsAndMarkersAreGroupedAndThePillboxAndRoadblockPoint()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.IsA("asl:pillbox", "asl:fortification"));
        Assert.True(vocabulary.IsA("asl:smoke", "asl:marker"));
        Assert.True(vocabulary.HasFacing("asl:pillbox"));
        Assert.True(vocabulary.HasHexside("asl:roadblock"));
        Assert.False(vocabulary.HasHexside("asl:pillbox"));
        Assert.Equal(["front", "pinned"], vocabulary.Faces("asl:sniper"));
        Assert.Equal(["front", "flame"], vocabulary.Faces("asl:fire"));
    }

    [Fact]
    public void APinnedSniperShowsItsPinnedSideAndAPinnedSquadDoesNot()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var sniper = UnitsTestData.One("""
            { "vocabulary": ["asl@1.3.0"], "id": "s", "kind": "asl:sniper", "side": "german", "faces": { "front": { }, "pinned": { } }, "unit": { "san": 3 },
              "states": ["asl:pinned"] }
            """);
        Assert.Equal("pinned", sniper.ShownFace(vocabulary));
        Assert.Equal("German Sniper, SAN 3, pinned", UnitLabels.AccessibleName(sniper, vocabulary));
        var squad = UnitsTestData.One("""{ "vocabulary": ["asl@1.3.0"], "id": "q", "kind": "asl:squad", "states": ["asl:pinned"] }""");
        Assert.Equal("front", squad.ShownFace(vocabulary));
    }

    [Fact]
    public void EntityNamesReadTheirPrintedValues()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var names = UnitExamples.Catalog(vocabulary).Where(unit => vocabulary.IsA(unit.Kind, "asl:entity"))
            .ToDictionary(unit => unit.Id, unit => UnitLabels.AccessibleName(unit, vocabulary), StringComparer.Ordinal);
        Assert.Equal(12, names.Count);
        Assert.Equal("Russian 2S foxhole", names["example-foxhole"]);
        Assert.Equal("German minefield, 6 A-P factors, 2 A-T Mines", names["example-minefield"]);
        Assert.Equal("German pillbox, 1+5+7, facing west", names["example-pillbox"]);
        Assert.Equal("American roadblock, across the north-east hexside", names["example-roadblock"]);
        Assert.Equal("SMOKE, hindrance +3", names["example-smoke"]);
        Assert.Equal("4 Residual FP", names["example-residual"]);
        Assert.Equal("Flame, clearance 6, hamper 4", names["example-fire"]);
        Assert.Equal("stone rubble", names["example-rubble"]);
    }

    [Fact]
    public void AHexsideIsWrittenReadAndRefusedWhereItDoesNotApply()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var roadblock = UnitsTestData.One("""{ "vocabulary": ["asl@1.3.0"], "id": "r", "kind": "asl:roadblock", "hexside": "south-west" }""");
        Assert.Equal(UnitHexside.SouthWest, roadblock.Hexside);
        Assert.Equal(150, UnitHexside.SouthWest.Degrees());
        var written = UnitDocumentJson.Write(roadblock, vocabulary);
        Assert.Contains("\"hexside\": \"south-west\"", written, StringComparison.Ordinal);
        Assert.Equal(written, UnitDocumentJson.Write(UnitsTestData.One(written), vocabulary));

        foreach (var json in new[]
        {
            """{ "vocabulary": ["asl@1.3.0"], "id": "r", "kind": "asl:roadblock", "hexside": "east" }""",
            """{ "vocabulary": ["asl@1.3.0"], "id": "p", "kind": "asl:pillbox", "hexside": "north" }""",
        })
        {
            Assert.Contains(UnitDocumentReader.Read(json, vocabulary).Diagnostics, diagnostic => diagnostic.Code == "UNIT-DOC-018");
        }
    }

    [Theory]
    [InlineData("""{ "id": "s", "kind": "asl:sniper", "unit": { "san": 8 } }""", "A14.1, p. 81")]
    [InlineData("""{ "id": "m", "kind": "asl:minefield", "faces": { "front": { "ap-strength": 7 } } }""", "B28.1, p. 148")]
    [InlineData("""{ "id": "m", "kind": "asl:minefield", "faces": { "front": { "at-strength": 6 } } }""", "B28.5, p. 149")]
    public void ThePlausibilityCheckCoversSnipersAndMinefields(string json, string rule) =>
        Assert.Contains(UnitPlausibility.Check(UnitsTestData.One(json), UnitsTestData.Asl.Value), warning => warning.Rule == rule);

    [Fact]
    public void TheEntityExamplesArePlausible()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.All(UnitExamples.Catalog(vocabulary).Where(unit => vocabulary.IsA(unit.Kind, "asl:entity")),
            entity => Assert.Empty(UnitPlausibility.Check(entity, vocabulary)));
    }
}
