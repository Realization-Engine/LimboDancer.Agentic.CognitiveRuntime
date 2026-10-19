using System.Text.RegularExpressions;
using System.Xml.Linq;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Units.Rendering.Tests;

/// <summary>
/// The leads in the builder (the Counter Layer Design, decision L6 and section 5): the text before each counter's name, written as the counter is
/// drawn, gives the names and titles the Studio's rewrite of the layer gave (<c>GameMaps.Led</c>, kept here as the oracle). The bytes differ in
/// escaping only, which is the same text to a parser, so the comparison is of the parsed values.
/// </summary>
public sealed class LeadTests
{
    private static readonly UnitMapTarget Target = UnitMapTarget.ForBoard(BoardRef.Parse("bd01"), BoardGeometry.StandardGeomorphic);

    public static TheoryData<string, string> Layers => LayerOracleTests.Layers;

    // A lead with every character the two escapings treat differently: quotes, an apostrophe, angle brackets, an ampersand, and a non-ASCII letter.
    private static string LeadOf(PlacedUnit unit) => $"[{unit.Location.Hex}] \"Ivan's\" <{unit.Document.Id.Length}> & Ž: ";

    [Theory]
    [MemberData(nameof(Layers))]
    public void TheLeadsAreTheNamesTheRewriteGave(string sheet, string fixture)
    {
        var units = LayerOracleTests.Fixture(fixture);
        var renderer = RenderingTestData.Renderer(sheet);
        var rewritten = Led(UnitOverlayBuilder.Build(Target, "test-set", units, renderer), LeadOf);
        var cache = new UnitFragmentCache();
        foreach (var built in new[] { UnitOverlayBuilder.Build(Target, "test-set", units, renderer, null, LeadOf), UnitOverlayBuilder.Build(Target, "test-set", units, renderer, cache, LeadOf),
                     UnitOverlayBuilder.Build(Target, "test-set", units, renderer, cache, LeadOf) })
        {
            Assert.Equal(Names(rewritten.Svg), Names(built.Svg));
            Assert.Equal(rewritten.Units.Select(unit => unit.Name), built.Units.Select(unit => unit.Name));
        }

        Assert.True(cache.Hits > 0);
    }

    [Fact]
    public void AnotherLeadIsAnotherHex()
    {
        var units = LayerOracleTests.Fixture("demo");
        var renderer = RenderingTestData.Renderer(UnitStyles.Digital);
        var cache = new UnitFragmentCache();
        UnitOverlayBuilder.Build(Target, "test-set", units, renderer, cache, unit => "first: ");
        var second = UnitOverlayBuilder.Build(Target, "test-set", units, renderer, cache, unit => "second: ");
        Assert.All(Names(second.Svg), name => Assert.StartsWith("second: ", name.Label, StringComparison.Ordinal));
        Assert.Equal(0, cache.Hits);
    }

    /// <summary>Each counter's id, accessible name, and title, as a parser reads them.</summary>
    private static (string Id, string Label, string Title)[] Names(string svg) => [.. XElement.Parse(svg).Descendants()
        .Where(element => element.Attribute("data-unit-id") is not null && element.Attribute("role")?.Value == "button")
        .Select(element => (element.Attribute("data-unit-id")!.Value, element.Attribute("aria-label")!.Value, element.Element("title")!.Value))];

    /// <summary>A copy of the Studio's former rewrite of the layer (<c>GameMaps.Led</c>, pass 31d), the oracle of the leads.</summary>
    private static (string Svg, IReadOnlyList<PlacedUnit> Units) Led(UnitOverlay overlay, Func<PlacedUnit, string> leadOf)
    {
        var lead = overlay.Units.ToDictionary(unit => unit.Document.Id, leadOf, StringComparer.Ordinal);
        var svg = Regex.Replace(overlay.Svg, "(<g data-unit-id=\"([^\"]+)\"[^>]*aria-label=\")([^\"]*\"[^>]*>\\s*<title>)",
            match => lead.TryGetValue(match.Groups[2].Value, out var said)
                ? match.Groups[1].Value + System.Net.WebUtility.HtmlEncode(said) + match.Groups[3].Value + System.Net.WebUtility.HtmlEncode(said)
                : match.Value);
        return (svg, [.. overlay.Units.Select(unit => unit with { Name = lead[unit.Document.Id] + unit.Name })]);
    }
}
