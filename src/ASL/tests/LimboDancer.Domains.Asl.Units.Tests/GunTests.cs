using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Plausibility;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Guns and ordnance values (Unit Display Design, phase 2; C2.2 to C2.3, pp. 167 and 168).</summary>
public sealed class GunTests
{
    private const string Gun = """
        {
          "vocabulary": ["asl@1.1.0"], "id": "g1", "kind": "asl:gun", "side": "german", "facing": "north-east",
          "faces": {
            "front": { "designation": "PaK 40", "gun-type": "at", "caliber": 75, "caliber-suffix": "l", "rate-of-fire": 2, "manhandling": 8, "special-ammo": ["A4", "H6"] },
            "malfunctioned": { "repair": 1, "removal": 6 },
            "limbered": { "manhandling": 8 }
          }
        }
        """;

    [Theory]
    [InlineData("kind", "asl:gun")]
    [InlineData("attribute", "asl:designation")]
    [InlineData("attribute", "asl:caliber")]
    [InlineData("attribute", "asl:caliber-suffix")]
    [InlineData("attribute", "asl:gun-type")]
    [InlineData("attribute", "asl:range-minimum")]
    [InlineData("attribute", "asl:range-maximum")]
    [InlineData("attribute", "asl:special-ammo")]
    [InlineData("attribute", "asl:manhandling")]
    [InlineData("attribute", "asl:target-size")]
    [InlineData("attribute", "asl:ife")]
    [InlineData("attribute", "asl:removal")]
    [InlineData("trait", "asl:no-ap")]
    [InlineData("trait", "asl:no-he")]
    [InlineData("trait", "asl:mount-360")]
    [InlineData("trait", "asl:qsu")]
    [InlineData("trait", "asl:nm")]
    [InlineData("trait", "asl:rfnm")]
    [InlineData("state", "asl:limbered")]
    public void TheGunTermsAreDeclared(string what, string name)
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(what switch
        {
            "kind" => vocabulary.HasKind(name),
            "attribute" => vocabulary.TryGetAttribute(name, out _),
            "trait" => vocabulary.TryGetTrait(name, out _),
            _ => vocabulary.TryGetState(name, out _),
        });
    }

    [Fact]
    public void AGunFacesAHexspineAndHasLimberedAndMalfunctionedFaces()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.HasFacing("asl:gun"));
        Assert.False(vocabulary.HasFacing("asl:squad"));
        Assert.Equal(["front", "malfunctioned", "limbered"], vocabulary.Faces("asl:gun"));
        Assert.Contains("asl:caliber", vocabulary.AcceptedAttributes("asl:light-mortar"));
        Assert.Contains("asl:caliber", vocabulary.AcceptedAttributes("asl:latw"));

        var gun = UnitsTestData.One(Gun);
        Assert.Equal(UnitFacing.NorthEast, gun.Facing);
        Assert.Equal("A4 H6", gun.Value("front", "asl:special-ammo")!.Display);
        Assert.Equal("limbered", (gun with
        {
            States = ["asl:limbered"]
        }).ShownFace(vocabulary));
    }

    [Fact]
    public void GunNamesReadTheCaliberTypeAndFacing()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var gun = UnitsTestData.One(Gun);
        Assert.Equal("German 75L anti-tank gun PaK 40, ROF 2, M8, facing north-east", UnitLabels.AccessibleName(gun, vocabulary));
        Assert.Equal("German limbered 75L anti-tank gun PaK 40, M8, facing north-east", UnitLabels.AccessibleName(gun, vocabulary, "limbered"));
        var noSuffix = UnitsTestData.One(Gun.Replace("\"caliber-suffix\": \"l\", ", "", StringComparison.Ordinal));
        Assert.StartsWith("German 75 anti-tank gun", UnitLabels.AccessibleName(noSuffix, vocabulary), StringComparison.Ordinal);
        Assert.Contains(new UnitDetail("Facing", "north-east hexspine"), UnitLabels.Details(gun, vocabulary));
    }

    [Fact]
    public void FacingIsWrittenAndReadBack()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var written = UnitDocumentJson.Write(UnitsTestData.One(Gun), vocabulary);
        Assert.Contains("\"facing\": \"north-east\"", written, StringComparison.Ordinal);
        Assert.Equal(written, UnitDocumentJson.Write(UnitsTestData.One(written), vocabulary));
    }

    [Theory]
    [InlineData("\"facing\": \"north-east\"", "\"facing\": \"north\"")]
    [InlineData("\"kind\": \"asl:gun\"", "\"kind\": \"asl:mg\"")]
    public void AFacingMustBeAHexspineOnAKindThatFaces(string find, string replace)
    {
        var json = Gun.Replace(find, replace, StringComparison.Ordinal);
        if (replace.Contains("asl:mg", StringComparison.Ordinal))
        {
            json = """{ "vocabulary": ["asl@1.1.0"], "id": "m", "kind": "asl:mg", "facing": "east" }""";
        }

        var result = UnitDocumentReader.Read(json, UnitsTestData.Asl.Value);
        Assert.Empty(result.Documents);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UNIT-DOC-018");
    }

    [Theory]
    [InlineData("asl@1.0.0", true)]
    [InlineData("asl@1.1.0", true)]
    [InlineData("asl@1.2.0", true)]
    [InlineData("asl@1.3.0", true)]
    [InlineData("asl@1.4.0", true)]
    [InlineData("asl@1.5.0", false)]
    [InlineData("asl@2.0.0", false)]
    [InlineData("asl@0.9.0", false)]
    public void ADocumentReadsUnderALaterMinorVersionOfItsPack(string reference, bool served) =>
        Assert.Equal(served, UnitsTestData.Asl.Value.Serves(reference));

    [Theory]
    [InlineData("\"traits\": [\"asl:no-ap\", \"asl:no-he\"]", "C2.21, p. 167")]
    [InlineData("\"ife\": 4", "C2.29, p. 168")]
    [InlineData("\"range-minimum\": 9, \"range-maximum\": 3", "C2.25, p. 168")]
    public void ThePlausibilityCheckCoversGuns(string addition, string rule)
    {
        var json = Gun.Replace("\"rate-of-fire\": 2", "\"rate-of-fire\": 1, " + addition, StringComparison.Ordinal);
        var warnings = UnitPlausibility.Check(UnitsTestData.One(json), UnitsTestData.Asl.Value);
        Assert.Contains(warnings, warning => warning.Rule == rule);
    }

    [Fact]
    public void TheGunExamplesArePlausible()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var guns = UnitExamples.Catalog(vocabulary).Where(unit => unit.Kind == "asl:gun").ToArray();
        Assert.Equal(4, guns.Length);
        Assert.All(guns, gun => Assert.NotNull(gun.Facing));
        Assert.All(guns, gun => Assert.Empty(UnitPlausibility.Check(gun, vocabulary)));
    }
}
