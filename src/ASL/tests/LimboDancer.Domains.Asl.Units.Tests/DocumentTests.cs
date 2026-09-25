using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Plausibility;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Unit documents (section 3.2), their errors (section 9), names, and the plausibility check (section 11).</summary>
public sealed class DocumentTests
{
    /// <summary>The design's section 3.2 example.</summary>
    public const string DesignExample = """
        {
          "vocabulary": ["asl@1.0.0"],
          "id": "ger-467-a",
          "kind": "asl:squad",
          "side": "german",
          "location": "bd01:E4:0",
          "faces": {
            "front": {
              "firepower": 4, "range": 6, "morale": 7,
              "identity": "A", "class": "1st-line",
              "traits": ["asl:assault-fire"]
            },
            "broken": { "broken-morale": 7, "bpv": 10, "traits": ["asl:self-rally"] }
          },
          "unit": { "smoke-exponent": 1 },
          "states": ["asl:pinned"],
          "attached": [
            {
              "id": "ger-lmg-1",
              "kind": "asl:mg",
              "faces": {
                "front": { "size": "light", "firepower": 3, "range": 6, "breakdown": 12, "rate-of-fire": 1, "portage": 1 },
                "malfunctioned": { "repair": 6 }
              }
            }
          ],
          "stackOrder": 0
        }
        """;

    [Fact]
    public void TheDesignExampleReads()
    {
        var unit = UnitsTestData.One(DesignExample);
        Assert.Equal("asl:squad", unit.Kind);
        Assert.Equal(["front", "broken"], unit.Faces.Select(face => face.Name));
        Assert.Equal("4", unit.Value("front", "asl:firepower")!.Display);
        Assert.Equal("1", unit.Value("front", "asl:class")!.Display);
        Assert.Equal("1st Line", unit.Value("front", "asl:class")!.Spoken);
        Assert.Equal("1", unit.Value("front", "asl:smoke-exponent")!.Display);
        Assert.Equal("front", unit.ShownFace(UnitsTestData.Asl.Value));
        var mg = Assert.Single(unit.Attached);
        Assert.Equal("LMG", mg.Value("front", "asl:size")!.Display);
    }

    [Fact]
    public void AStateSwitchesTheShownFaceWhenTheDocumentHasIt()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var broken = UnitsTestData.One(DesignExample.Replace("\"asl:pinned\"", "\"asl:broken\"", StringComparison.Ordinal));
        Assert.Equal("broken", broken.ShownFace(vocabulary));
        var hero = UnitsTestData.One("""
            { "vocabulary": ["asl@1.0.0"], "id": "h", "kind": "asl:hero", "faces": { "front": { "morale": 9 } }, "states": ["asl:broken"] }
            """);
        Assert.Equal("front", hero.ShownFace(vocabulary));
    }

    [Fact]
    public void TheExampleFilesReadWithoutErrors()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var catalog = UnitDocumentReader.Read(File.ReadAllBytes(Path.Combine(UnitsTestData.UnitsDirectory(), "examples", "catalog.units.json")), vocabulary);
        Assert.Empty(catalog.Diagnostics);
        Assert.Equal(12, catalog.Documents.Count);
        var set = UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(UnitsTestData.UnitsDirectory(), "examples", "bd01-demo.units.json")), vocabulary);
        Assert.Empty(set.Diagnostics);
        Assert.True(set.Set!.Synthetic);
        Assert.Equal(4, set.Set.Units.Count);
        Assert.Equal(["bd01"], set.Set.Boards.Select(board => board.Value));
    }

    [Theory]
    [InlineData("\"kind\": \"asl:squad\"", "\"kind\": \"asl:platoon\"", "UNIT-DOC-002")]
    [InlineData("\"firepower\": 4", "\"firepower\": \"4\"", "UNIT-DOC-004")]
    [InlineData("\"firepower\": 4", "\"firepower\": 4, \"leadership\": -1", "UNIT-DOC-003")]
    [InlineData("\"class\": \"1st-line\"", "\"class\": \"veteran\"", "UNIT-DOC-004")]
    [InlineData("\"asl:assault-fire\"", "\"asl:night-vision\"", "UNIT-DOC-005")]
    [InlineData("\"asl:assault-fire\"", "\"asl:self-rally\"", "UNIT-DOC-014")]
    [InlineData("\"asl:pinned\"", "\"asl:dazed\"", "UNIT-DOC-006")]
    [InlineData("\"asl:pinned\"", "\"asl:broken\", \"asl:berserk\"", "UNIT-DOC-012")]
    [InlineData("\"broken\": {", "\"wounded\": {", "UNIT-DOC-007")]
    [InlineData("\"side\": \"german\"", "\"side\": \"martian\"", "UNIT-DOC-008")]
    [InlineData("\"location\": \"bd01:E4:0\"", "\"location\": \"E4\"", "UNIT-DOC-013")]
    [InlineData("\"asl@1.0.0\"", "\"asl@0.9.0\"", "UNIT-DOC-011")]
    [InlineData("\"unit\": { \"smoke-exponent\": 1 }", "\"unit\": { \"firepower\": 1 }", "UNIT-DOC-014")]
    [InlineData("\"stackOrder\": 0", "\"stackOrder\": 0, \"concealed\": true", "UNIT-DOC-017")]
    [InlineData("\"repair\": 6", "\"repair\": 6 }, \"broken\": { \"repair\": 6", "UNIT-DOC-007")]
    public void AnUndeclaredOrMistypedTermRefusesTheDocument(string find, string replace, string code)
    {
        var json = DesignExample.Replace(find, replace, StringComparison.Ordinal);
        Assert.NotEqual(DesignExample, json);
        var result = UnitDocumentReader.Read(json, UnitsTestData.Asl.Value);
        Assert.Empty(result.Documents);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code && diagnostic.Severity == UnitDiagnosticSeverity.Error);
        Assert.All(result.Diagnostics.Where(d => d.Code == code), diagnostic => Assert.NotNull(diagnostic.Path));
    }

    [Fact]
    public void OneRefusedDocumentLeavesTheOthersInAList()
    {
        var result = UnitDocumentReader.Read("""
            [
              { "vocabulary": ["asl@1.0.0"], "id": "a", "kind": "asl:squad" },
              { "vocabulary": ["asl@1.0.0"], "id": "b", "kind": "asl:tank" },
              "not a document"
            ]
            """, UnitsTestData.Asl.Value);
        Assert.Equal("a", Assert.Single(result.Documents).Id);
        Assert.Equal(2, result.Diagnostics.Count);
    }

    [Fact]
    public void InvalidJsonIsReportedWithItsPosition()
    {
        var result = UnitDocumentReader.Read("{ \"id\": ", UnitsTestData.Asl.Value);
        Assert.Contains("line 1", Assert.Single(result.Diagnostics).Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AConcealedPlaceholderCarriesOnlyWhatItsViewerMayKnow()
    {
        var placeholder = UnitsTestData.One("""
            { "vocabulary": ["asl@1.0.0"], "id": "c1", "kind": "unit", "concealed": true, "side": "german", "location": "bd01:E4:0", "sizeClass": 3 }
            """);
        Assert.True(placeholder.Concealed);
        Assert.Empty(placeholder.Faces);
        Assert.Equal(3, placeholder.Figures(UnitsTestData.Asl.Value));
        Assert.Equal("German concealed unit, size class 3", UnitLabels.AccessibleName(placeholder, UnitsTestData.Asl.Value));
    }

    [Fact]
    public void AccessibleNamesComeFromVocabularyLabels()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var squad = UnitsTestData.One(DesignExample);
        Assert.Equal("German 1st Line squad A, 4-6-7, pinned, with light MG 3-6", UnitLabels.AccessibleName(squad, vocabulary));
        Assert.Equal("German 1st Line squad A, broken morale 7, pinned, with light MG 3-6", UnitLabels.AccessibleName(squad, vocabulary, "broken"));
        var leader = UnitsTestData.One("""
            { "vocabulary": ["asl@1.0.0"], "id": "l", "kind": "asl:leader", "side": "russian", "faces": { "front": { "leadership": -2, "morale": 9 } } }
            """);
        Assert.Equal("Russian leader, leadership -2, morale 9", UnitLabels.AccessibleName(leader, vocabulary));
        var mg = UnitsTestData.One("""
            { "vocabulary": ["asl@1.0.0"], "id": "m", "kind": "asl:mg", "faces": { "front": { "size": "heavy", "firepower": 8 } }, "states": ["asl:malfunctioned"] }
            """);
        Assert.Equal("heavy MG, malfunctioned", UnitLabels.AccessibleName(mg, vocabulary));
    }

    [Fact]
    public void DetailsListEveryValueTraitStateAndAttachment()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var details = UnitLabels.Details(UnitsTestData.One(DesignExample), vocabulary);
        Assert.Contains(new UnitDetail("Kind", "squad"), details);
        Assert.Contains(new UnitDetail("Firepower", "4"), details);
        Assert.Contains(new UnitDetail("Trait", "Assault Fire (AF)"), details);
        Assert.Contains(new UnitDetail("Smoke Placement Exponent", "1"), details);
        Assert.Contains(new UnitDetail("States", "pinned"), details);
        Assert.Contains(new UnitDetail("Broken face", "Broken Morale Level 7, Basic Point Value 10, Self-Rally (SR)"), details);
        Assert.Contains(new UnitDetail("Carries", "light MG 3-6"), details);
    }

    [Fact]
    public void WritingIsCanonicalAndRoundTrips()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var unit = UnitsTestData.One(DesignExample);
        var written = UnitDocumentJson.Write(unit, vocabulary);
        Assert.DoesNotContain("\r", written, StringComparison.Ordinal);
        Assert.Contains("\"firepower\": 4", written, StringComparison.Ordinal);
        var again = UnitsTestData.One(written);
        Assert.Equal(written, UnitDocumentJson.Write(again, vocabulary));

        var set = UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(UnitsTestData.UnitsDirectory(), "examples", "bd01-demo.units.json")), vocabulary).Set!;
        var setText = UnitDocumentJson.Write(set, vocabulary);
        var reread = UnitPlacementSetReader.Read(System.Text.Encoding.UTF8.GetBytes(setText), vocabulary).Set!;
        Assert.Equal(setText, UnitDocumentJson.Write(reread, vocabulary));
    }

    [Fact]
    public void APlacementSetLeavesOutUnitsWithoutALocationOrWithADuplicateId()
    {
        var result = UnitPlacementSetReader.Read("""
            {
              "schemaVersion": 1, "setId": "test", "synthetic": true, "vocabulary": ["asl@1.0.0"],
              "units": [
                { "id": "a", "kind": "asl:squad", "location": "bd01:E4:0" },
                { "id": "a", "kind": "asl:squad", "location": "bd01:E5:0" },
                { "id": "b", "kind": "asl:squad" }
              ]
            }
            """u8, UnitsTestData.Asl.Value);
        Assert.Single(result.Set!.Units);
        Assert.Equal(["UNIT-DOC-010", "UNIT-SET-002"], result.Diagnostics.Select(d => d.Code));
    }

    [Fact]
    public void ExtensionDocumentsUseLocalNamesForBothPacks()
    {
        var vocabulary = UnitsTestData.WithSla();
        var band = UnitsTestData.One("""
            {
              "vocabulary": ["asl@1.0.0", "sla@0.1.0"], "id": "band-1", "kind": "sla:scavenger-band", "side": "american",
              "faces": { "front": { "firepower": 3, "range": 3, "morale": 6, "traits": ["sla:scrounger"] } },
              "unit": { "supply": 2 }, "states": ["sla:irradiated"]
            }
            """, vocabulary);
        Assert.Equal("2", band.Value("front", "sla:supply")!.Display);
        Assert.Equal("American Scavenger band, 3-3-6, Irradiated", UnitLabels.AccessibleName(band, vocabulary));
    }

    [Theory]
    [InlineData("""{ "id": "l", "kind": "asl:leader", "faces": { "front": { "leadership": -1, "morale": 8, "range": 4 } } }""", "A1.22, p. 44")]
    [InlineData("""{ "id": "s", "kind": "asl:squad", "faces": { "broken": { "firepower": 4, "broken-morale": 7 } } }""", "A1.4, p. 45")]
    [InlineData("""{ "id": "h", "kind": "asl:hero", "faces": { "broken": { "broken-morale": 9 } } }""", "A1.4, p. 45")]
    [InlineData("""{ "id": "m", "kind": "asl:mg", "faces": { "front": { "size": "light", "firepower": 3, "range": 6 } } }""", "A9.7, p. 65")]
    public void ThePlausibilityCheckWarnsWithTheRule(string json, string rule)
    {
        var unit = UnitsTestData.One(json);
        var warning = Assert.Single(UnitPlausibility.Check(unit, UnitsTestData.Asl.Value));
        Assert.Equal(rule, warning.Rule);
    }

    [Fact]
    public void ThePlausibilityCheckIsQuietForTheExamples()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var catalog = UnitDocumentReader.Read(File.ReadAllBytes(Path.Combine(UnitsTestData.UnitsDirectory(), "examples", "catalog.units.json")), vocabulary);
        Assert.All(catalog.Documents, unit => Assert.Empty(UnitPlausibility.Check(unit, vocabulary)));
    }
}
