using System.Text;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

public sealed class BoardMetadataParserTests
{
    private const string Header =
        "<boardMetadata name=\"01\" version=\"6.9\" versionDate=\"Jan 2025\" author=\"TR\" boardImageFileName=\"bd01.gif\" hasHills=\"FALSE\" width=\"33\" height=\"10\"";

    [Fact]
    public void ReadsBoardAttributesAndSections()
    {
        var result = Parse(Header + ">"
            + "<!-- comment -->"
            + "<buildingTypes><buildingType hexName=\"E4\" buildingTypeName=\"Stone Building, 2 Level\" /></buildingTypes>"
            + "<slopes><slope hex=\"E9\" hexsides=\"5\" /></slopes>"
            + "<rrembankments><rrembankment hex=\"E1\" hexsides=\"12\" /></rrembankments>"
            + "<partialorchards><partialorchard hex=\"JJ1\" hexsides=\"21\" /></partialorchards>"
            + "<colors><color name=\"X\" red=\"1\" green=\"2\" blue=\"3\" /></colors>"
            + "<colorSSRules /><overlaySSRules />"
            + "</boardMetadata>");

        var metadata = Assert.IsType<BoardMetadata>(result.Metadata);
        Assert.Equal("01", metadata.Name);
        Assert.Equal("6.9", metadata.Version);
        Assert.Equal("Jan 2025", metadata.VersionDate);
        Assert.Equal("TR", metadata.Author);
        Assert.Equal("bd01.gif", metadata.BoardImageFileName);
        Assert.False(metadata.HasHills);
        Assert.Equal((33, 10), (metadata.Width, metadata.Height));
        Assert.Empty(metadata.GeometryAttributes);
        Assert.Equal(new BuildingTypeOverride(HexName.Parse("E4"), "Stone Building, 2 Level"), Assert.Single(metadata.BuildingTypes));
        Assert.Equal([HexsideDirection.NorthWest], Assert.Single(metadata.Slopes).Sides);
        Assert.Equal([HexsideDirection.NorthEast, HexsideDirection.SouthEast], Assert.Single(metadata.RailroadEmbankments).Sides);
        Assert.Equal(HexName.Parse("JJ1"), Assert.Single(metadata.PartialOrchards).Hex);
        Assert.Equal(["colorSSRules", "colors", "overlaySSRules"], metadata.DeferredElements.Keys.Order(StringComparer.Ordinal));
        Assert.All(result.Diagnostics, diagnostic => Assert.Equal("VASL-META-003", diagnostic.Code));
    }

    [Fact]
    public void LaterDuplicatesReplaceEarlierOnesInTheirOriginalPosition()
    {
        var metadata = Parse(Header + "><buildingTypes>"
            + "<buildingType hexName=\"E4\" buildingTypeName=\"Stone Building, 1 Level\" />"
            + "<buildingType hexName=\"F3\" buildingTypeName=\"Stone Building, 2 Level\" />"
            + "<buildingType hexName=\"E4\" buildingTypeName=\"Stone Building, 2 Level\" />"
            + "</buildingTypes><slopes><slope hex=\"A1\" hexsides=\"03\" /><slope hex=\"A1\" hexsides=\"0\" /></slopes></boardMetadata>").Metadata!;

        Assert.Equal(["E4", "F3"], metadata.BuildingTypes.Select(item => item.Hex.ToString()));
        Assert.Equal("Stone Building, 2 Level", metadata.BuildingTypes[0].BuildingTypeName);
        Assert.Equal([HexsideDirection.North], Assert.Single(metadata.Slopes).Sides);
    }

    [Fact]
    public void OnlyTheFirstSixHexsideCharactersAreRead()
    {
        var metadata = Parse(Header + "><slopes><slope hex=\"A1\" hexsides=\"0123459\" /></slopes></boardMetadata>").Metadata!;
        Assert.Equal(6, Assert.Single(metadata.Slopes).Sides.Count);
    }

    [Theory]
    [InlineData("<slopes><slope hex=\"A1\" hexsides=\"6\" /></slopes>")]
    [InlineData("<slopes><slope hex=\"A1\" hexsides=\"0 1\" /></slopes>")]
    [InlineData("<rrembankments><rrembankment hex=\"a1\" hexsides=\"1\" /></rrembankments>")]
    [InlineData("<buildingTypes><buildingType hexName=\"E4\" /></buildingTypes>")]
    public void InvalidEntriesRejectTheMetadataAsVaslDoes(string section)
    {
        var result = Parse(Header + ">" + section + "</boardMetadata>");
        Assert.Null(result.Metadata);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "VASL-META-005");
    }

    [Fact]
    public void GeometryAttributesAndUnknownElementsAreKept()
    {
        var result = Parse(Header.Replace("height=\"10\"", "height=\"10\" hexWidth=\"56.25\" altHexGrain=\"FALSE\"", StringComparison.Ordinal)
            + "><mystery /></boardMetadata>");
        var metadata = result.Metadata!;
        Assert.Equal("56.25", metadata.GeometryAttributes["hexWidth"]);
        Assert.Equal("FALSE", metadata.GeometryAttributes["altHexGrain"]);
        Assert.Equal("VASL-META-001", Assert.Single(result.Diagnostics).Code);
        Assert.Null(VaslBoardImporter.ScopeProblem(metadata));
    }

    // Boards VASL lays out as geomorphic are in scope whatever their size or hex size (VASL Board Ingestion Design, section 11.1).
    [Theory]
    [InlineData("width=\"33\"", "width=\"17\"")]
    [InlineData("height=\"10\"", "height=\"10\" hexWidth=\"56.3125\"")]
    [InlineData("height=\"10\"", "height=\"10\" hexHeight=\"64.47\"")]
    [InlineData("height=\"10\"", "height=\"10\" A1CenterX=\"-901\" A1CenterY=\"32.25\"")]
    [InlineData("height=\"10\"", "height=\"10\" A1CenterY=\"-612.75\"")]
    [InlineData("height=\"10\"", "height=\"10\" HexGridConfig=\"Normal\"")]
    public void GeomorphicLayoutBoardsAreInScope(string original, string replacement)
    {
        var metadata = Parse(Header.Replace(original, replacement, StringComparison.Ordinal) + " />").Metadata!;
        Assert.Null(VaslBoardImporter.ScopeProblem(metadata));
    }

    [Theory]
    [InlineData("name=\"01\"", "name=\"RO\"", "HASL map")]
    [InlineData("height=\"10\"", "height=\"10\" hexHeight=\"64.4528\"", "lays out no hexes")]
    [InlineData("height=\"10\"", "height=\"10\" A1CenterX=\"22\"", "custom geometry A1CenterX")]
    [InlineData("height=\"10\"", "height=\"10\" A1CenterY=\"65\"", "custom geometry A1CenterY")]
    [InlineData("height=\"10\"", "height=\"10\" altHexGrain=\"TRUE\"", "alternate hex grain")]
    public void NonStandardBoardsAreOutOfScope(string original, string replacement, string reason)
    {
        var metadata = Parse(Header.Replace(original, replacement, StringComparison.Ordinal) + " />").Metadata!;
        Assert.Contains(reason, VaslBoardImporter.ScopeProblem(metadata), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("<boardMetadata")]
    [InlineData("<other />")]
    [InlineData("<boardMetadata name=\"x\" hasHills=\"FALSE\" height=\"10\" />")]
    [InlineData("<boardMetadata name=\"x\" hasHills=\"maybe\" width=\"33\" height=\"10\" />")]
    public void MalformedMetadataIsReported(string xml)
    {
        var result = Parse(xml);
        Assert.Null(result.Metadata);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code is "VASL-META-000" or "VASL-META-005");
    }

    [Fact]
    public void AnXml11DeclarationIsReadAsXml10()
    {
        var result = Parse("﻿<?xml version=\"1.1\" encoding=\"UTF-8\"?>" + Header + " />");
        Assert.NotNull(result.Metadata);
        Assert.Equal("VASL-META-006", Assert.Single(result.Diagnostics).Code);
        Assert.True(VaslBoardComparison.SemanticallyEqual(
            Encoding.UTF8.GetBytes("<?xml version='1.1'?><a />"), Encoding.UTF8.GetBytes("<a />")));
    }

    [Fact]
    public void XmlComparisonIgnoresFormattingButNotContent()
    {
        var left = "<a x=\"1\" y=\"2\"><!-- c --><b>t</b></a>"u8.ToArray();
        var reformatted = "<a y=\"2\" x=\"1\">\r\n  <b> t </b>\r\n</a>"u8.ToArray();
        var changed = "<a x=\"1\" y=\"3\"><b>t</b></a>"u8.ToArray();
        Assert.True(VaslBoardComparison.SemanticallyEqual(left, reformatted));
        Assert.False(VaslBoardComparison.SemanticallyEqual(left, changed));
        Assert.False(VaslBoardComparison.SemanticallyEqual(left, "<a"u8.ToArray()));
    }

    private static BoardMetadataResult Parse(string xml)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return BoardMetadataParser.Parse(stream);
    }
}
