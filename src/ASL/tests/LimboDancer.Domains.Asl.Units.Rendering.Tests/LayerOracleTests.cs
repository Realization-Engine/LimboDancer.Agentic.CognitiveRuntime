using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Units.Documents;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>The equivalence oracle itself (the Counter Layer Design, section 11): what it expands, and that it refuses a reference it cannot find.</summary>
public sealed class LayerOracleTests
{
    private static readonly UnitMapTarget Target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);

    public static TheoryData<string, string> Layers => new()
    {
        { UnitStyles.Digital, "demo" }, { UnitStyles.Classic, "demo" }, { UnitStyles.Digital, "crowded" }, { UnitStyles.Classic, "crowded" },
    };

    internal static IReadOnlyList<UnitDocument> Fixture(string name) => name == "demo"
        ? UnitPlacementSetReader.Read(File.ReadAllBytes(Path.Combine(RenderingTestData.UnitsDirectory(), "examples", "bd01-demo.units.json")),
            RenderingTestData.Vocabulary.Value).Set!.Units
        : LayerFixtures.Crowded(UnitExamples.Catalog(RenderingTestData.Vocabulary.Value));

    [Theory]
    [MemberData(nameof(Layers))]
    public void ALayerExpandsWithEveryReferenceFound(string sheet, string fixture)
    {
        var layer = UnitOverlayBuilder.Build(Target, "test-set", Fixture(fixture), RenderingTestData.Renderer(sheet)).Svg;
        var original = XElement.Parse(layer);
        var uses = original.Descendants().Count(element => element.Name.LocalName == "use");
        var drawn = original.Descendants().Count(element => element.Name.LocalName == "path" && element.Ancestors().All(ancestor => ancestor.Name.LocalName != "defs"));
        var expanded = LayerOracle.Expand(layer);
        Assert.DoesNotContain(expanded.Descendants(), element => element.Name.LocalName is "use" or "defs");

        // Every reference (to a figure outline) became a copy of its path, which carries no id.
        Assert.True(uses > 0, "the fixture draws no figure, so the oracle would test nothing");
        var paths = expanded.Descendants().Where(element => element.Name.LocalName == "path").ToArray();
        Assert.Equal(drawn + uses, paths.Length);
        Assert.All(paths, path => Assert.Null(path.Attribute("id")));
    }

    [Fact]
    public void AReferenceThatFindsNothingIsAnError()
    {
        var error = Assert.Throws<InvalidOperationException>(() => LayerOracle.Expand("""<g id="layer-units"><use href="#nowhere"/></g>"""));
        Assert.Contains("#nowhere", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AUsesPlaceAndTransformGoOnItsGroup()
    {
        var expanded = LayerOracle.Expanded("""
            <g id="layer-units"><defs><g id="face-1" data-face-def="near"><rect width="4" height="4"/></g></defs><use href="#face-1" x="10" y="2.5" data-tier="near"/></g>
            """);
        Assert.Equal("""<g id="layer-units"><g data-tier="near" transform="translate(10 2.5)"><g data-face-def="near"><rect width="4" height="4" /></g></g></g>""", expanded);
    }
}
