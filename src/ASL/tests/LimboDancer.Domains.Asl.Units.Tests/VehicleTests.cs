using LimboDancer.Domains.Asl.Units.Documents;
using LimboDancer.Domains.Asl.Units.Plausibility;

namespace LimboDancer.Domains.Asl.Units.Tests;

/// <summary>Vehicles (Unit Display Design, phase 3; D1.1 to D1.9, pp. 193 to 195).</summary>
public sealed class VehicleTests
{
    private const string Tank = """
        {
          "vocabulary": ["asl@1.2.0"], "id": "t1", "kind": "asl:vehicle", "side": "german", "facing": "east", "turretFacing": "north-east",
          "faces": {
            "front": { "designation": "PzKpfw IVH", "identity": "A", "movement-type": "fully-tracked", "movement-points": 12, "af-front": 8, "af-side": 3,
              "ma-type": "t", "caliber": 75, "caliber-suffix": "ll", "bmg": 2, "cmg": 4 },
            "wreck": { "crew-survival": 7 }
          },
          "states": ["asl:bu"]
        }
        """;

    [Theory]
    [InlineData("kind", "asl:vehicle")]
    [InlineData("attribute", "asl:movement-type")]
    [InlineData("attribute", "asl:movement-points")]
    [InlineData("attribute", "asl:amphibious-mp")]
    [InlineData("attribute", "asl:af-front")]
    [InlineData("attribute", "asl:af-side")]
    [InlineData("attribute", "asl:turret-af-front")]
    [InlineData("attribute", "asl:turret-af-side")]
    [InlineData("attribute", "asl:ma-type")]
    [InlineData("attribute", "asl:ma-weapon")]
    [InlineData("attribute", "asl:sa-mount")]
    [InlineData("attribute", "asl:sa-caliber")]
    [InlineData("attribute", "asl:ground-pressure")]
    [InlineData("attribute", "asl:towing")]
    [InlineData("attribute", "asl:passenger-capacity")]
    [InlineData("attribute", "asl:bmg")]
    [InlineData("attribute", "asl:cmg")]
    [InlineData("attribute", "asl:aamg")]
    [InlineData("attribute", "asl:hull-rear-mg")]
    [InlineData("attribute", "asl:turret-rear-mg")]
    [InlineData("attribute", "asl:vehicle-ft-mount")]
    [InlineData("attribute", "asl:vehicle-ft-firepower")]
    [InlineData("attribute", "asl:vehicle-ft-removal")]
    [InlineData("attribute", "asl:crew-survival")]
    [InlineData("trait", "asl:mechanically-unreliable")]
    [InlineData("trait", "asl:unarmored")]
    [InlineData("trait", "asl:partially-armored")]
    [InlineData("trait", "asl:rear-turret-unarmored")]
    [InlineData("trait", "asl:open-topped")]
    [InlineData("trait", "asl:fixed-bmg")]
    [InlineData("trait", "asl:vehicle-ft-range-2")]
    [InlineData("state", "asl:wrecked")]
    [InlineData("state", "asl:motion")]
    [InlineData("state", "asl:bu")]
    [InlineData("state", "asl:ce")]
    public void TheVehicleTermsAreDeclared(string what, string name)
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
    public void AVehicleHasHullAndTurretFacingsAndAWreckFace()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        Assert.True(vocabulary.HasFacing("asl:vehicle"));
        Assert.True(vocabulary.HasTurret("asl:vehicle"));
        Assert.False(vocabulary.HasTurret("asl:gun"));
        Assert.Equal(["front", "wreck"], vocabulary.Faces("asl:vehicle"));
        Assert.True(vocabulary.TryGetAttribute("asl:target-size", out var size));
        Assert.Equal(["very-small", "small", "large", "very-large"], size.Members.Select(member => member.Name));

        var tank = UnitsTestData.One(Tank);
        Assert.Equal(UnitFacing.East, tank.Facing);
        Assert.Equal(UnitFacing.NorthEast, tank.TurretFacing);
        Assert.Equal("wreck", (tank with
        {
            States = ["asl:wrecked"]
        }).ShownFace(vocabulary));
        var written = UnitDocumentJson.Write(tank, vocabulary);
        Assert.Contains("\"turretFacing\": \"north-east\"", written, StringComparison.Ordinal);
        Assert.Equal(written, UnitDocumentJson.Write(UnitsTestData.One(written), vocabulary));
    }

    [Fact]
    public void VehicleNamesReadMovementArmorArmamentAndBothFacings()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var tank = UnitsTestData.One(Tank);
        Assert.Equal("German PzKpfw IVH fully tracked vehicle A, 12 MP, AF 8/3, MA 75LL fast turret traverse, facing east, turret facing north-east, buttoned up",
            UnitLabels.AccessibleName(tank, vocabulary));
        Assert.Equal("German wrecked PzKpfw IVH vehicle A, crew survival 7, facing east, turret facing north-east, buttoned up",
            UnitLabels.AccessibleName(tank, vocabulary, "wreck"));
        Assert.Contains(new UnitDetail("Turret facing", "north-east hexspine"), UnitLabels.Details(tank, vocabulary));
    }

    [Theory]
    [InlineData("\"turretFacing\": \"north-east\"", "\"turretFacing\": \"up\"")]
    [InlineData("\"facing\": \"east\", ", "")]
    public void ATurretFacingMustBeAHexspineAndNeedsAHullFacing(string find, string replace)
    {
        var result = UnitDocumentReader.Read(Tank.Replace(find, replace, StringComparison.Ordinal), UnitsTestData.Asl.Value);
        Assert.Empty(result.Documents);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UNIT-DOC-018");
    }

    [Fact]
    public void AGunHasNoTurretFacing()
    {
        var result = UnitDocumentReader.Read("""
            { "vocabulary": ["asl@1.2.0"], "id": "g", "kind": "asl:gun", "facing": "east", "turretFacing": "west" }
            """, UnitsTestData.Asl.Value);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "UNIT-DOC-018");
    }

    [Theory]
    [InlineData("\"af-front\": 8", "\"af-front\": 9", "D1.6, p. 194")]
    [InlineData("\"ma-type\": \"t\"", "\"ma-type\": \"nt\"", "D3.12, p. 199")]
    [InlineData("\"cmg\": 4 }", "\"cmg\": 4, \"traits\": [\"asl:unarmored\"] }", "D1.21, p. 193")]
    public void ThePlausibilityCheckCoversVehicles(string find, string replace, string rule)
    {
        var tank = UnitsTestData.One(Tank.Replace(find, replace, StringComparison.Ordinal));
        Assert.Contains(UnitPlausibility.Check(tank, UnitsTestData.Asl.Value), warning => warning.Rule == rule);
    }

    [Fact]
    public void TheVehicleExamplesArePlausible()
    {
        var vocabulary = UnitsTestData.Asl.Value;
        var vehicles = UnitExamples.Catalog(vocabulary).Where(unit => unit.Kind == "asl:vehicle").ToArray();
        Assert.Equal(5, vehicles.Length);
        Assert.All(vehicles, vehicle => Assert.Empty(UnitPlausibility.Check(vehicle, vocabulary)));
    }
}
