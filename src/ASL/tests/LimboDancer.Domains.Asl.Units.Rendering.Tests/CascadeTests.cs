using System.Text;
using LimboDancer.Domains.Asl.Units.Rendering.Styles;
using LimboDancer.Domains.Asl.Units.Vocabulary;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>The cascade on small synthetic sheets (section 12): specificity, source order, kind inheritance, and @detail.</summary>
public sealed class CascadeTests
{
    private const string Squad = """
        {
          "id": "s", "kind": "asl:squad", "side": "german",
          "faces": { "front": { "firepower": 4, "class": "elite", "traits": ["asl:assault-fire"] }, "broken": { "broken-morale": 7 } },
          "unit": { "smoke-exponent": 2 },
          "states": ["asl:pinned"],
          "attached": [ { "id": "m", "kind": "asl:mg", "faces": { "front": { "size": "light" } } } ]
        }
        """;

    [Theory]
    [InlineData("asl|squad { fill: #000001; } asl|personnel { fill: #000002; }", "#000001")]
    [InlineData("asl|personnel { fill: #000002; } asl|squad { fill: #000001; }", "#000001")]
    [InlineData("asl|personnel { fill: #000001; } asl|personnel { fill: #000002; }", "#000002")]
    [InlineData("asl|squad[class=elite] { fill: #000001; } asl|squad { fill: #000002; }", "#000001")]
    [InlineData("asl|unit[class] { fill: #000001; } asl|squad { fill: #000002; }", "#000001")]
    [InlineData("asl|unit:asl\\:pinned { fill: #000001; } asl|squad[class=elite][smoke-exponent] { fill: #000002; }", "#000001")]
    [InlineData("unit.asl\\:assault-fire { fill: #000001; } asl|squad[class=elite] { fill: #000002; }", "#000001")]
    [InlineData("asl|squad { fill: #000001; fill: #000002; }", "#000002")]
    [InlineData("[side=german] { fill: #000001; } asl|squad { fill: #000002; }", "#000001")]
    [InlineData("[side=russian] { fill: #000001; } asl|squad { fill: #000002; }", "#000002")]
    [InlineData("asl|squad[class=green] { fill: #000001; } unit { fill: #000002; }", "#000002")]
    [InlineData("asl|mg { fill: #000001; } unit { fill: #000002; }", "#000002")]
    [InlineData("* { fill: #000001; }", "#000001")]
    public void SpecificityThenSourceOrderDecides(string text, string expected) =>
        Assert.Equal(expected, Fill(text, DetailTier.Near));

    [Fact]
    public void DetailBlocksApplyOnlyAtTheirTierAndOutrankOtherRules()
    {
        const string text = """
            asl|squad:asl\:pinned[class=elite] { fill: #000001; }
            @detail far { unit { fill: #000002; } }
            @detail mid { asl|personnel { fill: #000003; } }
            """;
        Assert.Equal("#000001", Fill(text, DetailTier.Near));
        Assert.Equal("#000003", Fill(text, DetailTier.Mid));
        Assert.Equal("#000002", Fill(text, DetailTier.Far));
    }

    [Fact]
    public void BadgesAccumulateInCascadeOrder()
    {
        var sheet = RenderingTestData.Sheet("""
            asl|unit:asl\:pinned { badge: "PIN" top-right; }
            asl|personnel[smoke-exponent] { badge: "S" attr(smoke-exponent) bottom-left; }
            unit { badge: "ANY"; }
            """);
        var svg = RenderingTestData.Render(RenderingTestData.Renderer(sheet), RenderingTestData.Document(Squad));
        var badges = svg.Descendants().Where(element => element.Ancestors().All(ancestor => ancestor.Attribute("data-attached-id") is null))
            .Select(element => element.Attribute("data-badge")?.Value).OfType<string>();

        // Cascade order is ANY, S2, PIN; the top edge is written before the bottom edge.
        Assert.Equal(["ANY", "PIN", "S2"], badges);
    }

    [Fact]
    public void AttachedRulesMatchTheOwnerAndTheItem()
    {
        var sheet = RenderingTestData.Sheet("""
            unit { face-template: "fp"; }
            asl|mg { fill: #000001; }
            asl|squad::attached(asl|mg) { fill: #000002; }
            asl|leader::attached(asl|mg) { fill: #000003; }
            """);
        var svg = RenderingTestData.Render(RenderingTestData.Renderer(sheet), RenderingTestData.Document(Squad));
        var attached = svg.Descendants().Single(element => element.Attribute("data-attached-id")?.Value == "m");
        Assert.Equal("#000002", attached.Elements().First().Attribute("fill")!.Value);
    }

    [Fact]
    public void AKindThatExtendsAnAslKindIsDrawnByTheAslRulesAndAddsItsOwnSlot()
    {
        var vocabulary = ExtendedVocabulary();
        var band = RenderingTestData.Document("""
            {
              "id": "band", "kind": "sla:scavenger-band", "side": "american",
              "faces": { "front": { "firepower": 3, "range": 3, "morale": 6 } }, "unit": { "supply": 2 }
            }
            """, vocabulary);
        var sheet = StyleSheetParser.Parse("digital-plus", UnitStyles.Text(UnitStyles.Digital) + """

            sla|scavenger-band { face-template: "class . supply" "fp range morale"; }
            sla|scavenger-band::slot(supply) { content: attr(sla\:supply); badge-shape: circle; fill: #ffffff; }
            """).Sheet!;
        var renderer = new UnitRenderer(vocabulary, sheet, RenderingTestData.Renderer(UnitStyles.Digital).Palette);
        var slots = Slots(RenderingTestData.Render(renderer, band));
        Assert.Equal("3", slots["fp"]);
        Assert.Equal("6", slots["morale"]);
        Assert.Equal("2", slots["supply"]);
    }

    [Fact]
    public void AKindThatExtendsNothingStartsFromTheBaseUnitStyle()
    {
        var vocabulary = ExtendedVocabulary();
        var mutant = RenderingTestData.Document("""{ "id": "x", "kind": "sla:mutant", "side": "russian", "faces": { "front": { "endurance": 4 } } }""", vocabulary);
        var renderer = new UnitRenderer(vocabulary, UnitStyles.Load(UnitStyles.Digital), RenderingTestData.Renderer(UnitStyles.Digital).Palette);
        var svg = RenderingTestData.Render(renderer, mutant);
        var slots = svg.Descendants().Where(element => element.Attribute("data-slot") is not null).Select(element => element.Attribute("data-slot")!.Value);
        Assert.Equal(["glyph"], slots);
        var face = svg.Descendants().Single(element => element.Attribute("data-face") is not null);
        Assert.Equal(renderer.Palette.For("russian").Fill, face.Attribute("fill")!.Value);
    }

    [Fact]
    public void TheFacePropertyPicksTheFaceToShow()
    {
        var sheet = RenderingTestData.Sheet("""
            unit { face-template: "m"; }
            unit::slot(m) { content: attr(broken-morale); }
            asl|unit:asl\:pinned { face: broken; }
            """);
        var slots = Slots(RenderingTestData.Render(RenderingTestData.Renderer(sheet), RenderingTestData.Document(Squad)));
        Assert.Equal("7", slots["m"]);
    }

    private static string Fill(string text, DetailTier tier)
    {
        var svg = RenderingTestData.Render(RenderingTestData.Renderer(RenderingTestData.Sheet(text)), RenderingTestData.Document(Squad), tier);
        return svg.Descendants().Single(element => element.Attribute("data-face") is not null && element.Parent!.Attribute("data-attached-id") is null)
            .Attribute("fill")!.Value;
    }

    internal static Dictionary<string, string> Slots(System.Xml.Linq.XElement svg) =>
        svg.Descendants().Where(element => element.Attribute("data-slot") is not null && element.Ancestors().All(a => a.Attribute("data-attached-id") is null))
            .ToDictionary(element => element.Attribute("data-slot")!.Value, element => string.Concat(element.Elements().Where(e => e.Name.LocalName == "text").Select(e => e.Value)),
                StringComparer.Ordinal);

    private static UnitVocabulary ExtendedVocabulary()
    {
        var pack = VocabularyPackReader.Read(Encoding.UTF8.GetBytes("""
            {
              "pack": "sla", "version": "0.1.0", "extends": ["asl@1.0.0"],
              "kinds": [
                { "name": "sla:scavenger-band", "extends": "asl:mmc", "label": "Scavenger band" },
                { "name": "sla:mutant", "label": "Mutant", "attributes": ["sla:endurance"] }
              ],
              "attributes": [
                { "name": "sla:supply", "type": "integer", "label": "Supply", "scope": "unit" },
                { "name": "sla:endurance", "type": "integer", "label": "Endurance" }
              ],
              "augments": [ { "kind": "sla:scavenger-band", "attributes": ["sla:supply"] } ]
            }
            """)).Pack!;
        return UnitVocabulary.Create([VocabularyPackReader.Asl(), pack]).Vocabulary!;
    }
}
