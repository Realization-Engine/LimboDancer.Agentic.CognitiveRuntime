using System.Text;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

public sealed class SharedBoardMetadataParserTests
{
    [Fact]
    public void ReadsTerrainTypesIgnoringCommentsAndStrayText()
    {
        var result = Parse(
            Terrain("Open Ground", 0, "OPEN", red: 175, green: 188, blue: 106),
            "<!--" + Terrain("Scrub", 121, "OPEN") + "-->",
            Terrain("Stone Building, 2 Level", 42, "BUILDING", obstacle: "TRUE", height: 2) + "  //mapColorRed=\"1\" />",
            Terrain("Stone Market Place", 49, "MARKETPLACE", split: "1.5"));

        var catalog = Assert.IsType<TerrainCatalog>(result.Catalog);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(3, catalog.Count);
        Assert.False(catalog.TryGet(121, out _));

        var building = catalog[42];
        Assert.Equal("Stone Building, 2 Level", building.Name);
        Assert.Equal(LosCategory.Building, building.Category);
        Assert.True(building.IsLosObstacle);
        Assert.Equal(2, building.Height);
        Assert.Equal(new TerrainColor(175, 188, 106), catalog[0].MapColor);
        Assert.True(catalog[49].HasSplit);
        Assert.Equal(1.5f, catalog[49].Split);
    }

    [Theory]
    [InlineData("TRUE", true)]
    [InlineData("true", true)]
    [InlineData(" yes ", true)]
    [InlineData("on", true)]
    [InlineData("1", true)]
    [InlineData("FALSE", false)]
    [InlineData("off", false)]
    [InlineData("0", false)]
    public void BooleansFollowJdom(string value, bool expected)
    {
        var result = Parse(Terrain("Woods", 60, "WOODS", obstacle: value));
        Assert.Equal(expected, Assert.IsType<TerrainCatalog>(result.Catalog)[60].IsLosObstacle);
    }

    [Fact]
    public void UnknownCategoryIsAnError()
    {
        var result = Parse(Terrain("Odd", 5, "SWAMPY"));
        Assert.Null(result.Catalog);
        Assert.Equal("VASL-CAT-003", Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void DuplicateCodesAndNamesAreErrors()
    {
        Assert.Contains(
            Parse(Terrain("A", 1, "OPEN"), Terrain("B", 1, "OPEN")).Diagnostics,
            diagnostic => diagnostic.Code == "VASL-CAT-001");
        Assert.Contains(
            Parse(Terrain("A", 1, "OPEN"), Terrain("A", 2, "OPEN")).Diagnostics,
            diagnostic => diagnostic.Code == "VASL-CAT-004");
    }

    [Theory]
    [InlineData("typeCode=\"0\"", "typeCode=\"256\"")]
    [InlineData("typeCode=\"0\"", "typeCode=\"zero\"")]
    [InlineData(" height=\"0\"", "")]
    [InlineData("isLOSHindrance=\"FALSE\"", "isLOSHindrance=\"maybe\"")]
    [InlineData("mapColorRed=\"0\"", "mapColorRed=\"300\"")]
    public void MissingOrInvalidAttributesAreErrors(string original, string replacement)
    {
        var result = Parse(Terrain("Open Ground", 0, "OPEN").Replace(original, replacement, StringComparison.Ordinal));
        Assert.Null(result.Catalog);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "VASL-CAT-005");
    }

    [Fact]
    public void MalformedDocumentsAreReported()
    {
        using var broken = new MemoryStream(Encoding.UTF8.GetBytes("<sharedBoardMetadata><terrainTypes>"));
        Assert.Equal("VASL-CAT-000", Assert.Single(SharedBoardMetadataParser.Parse(broken).Diagnostics).Code);

        using var empty = new MemoryStream(Encoding.UTF8.GetBytes("<sharedBoardMetadata />"));
        Assert.Equal("VASL-CAT-000", Assert.Single(SharedBoardMetadataParser.Parse(empty).Diagnostics).Code);
    }

    private static SharedBoardMetadataResult Parse(params string[] terrainTypes)
    {
        var xml = "<?xml version=\"1.0\"?><sharedBoardMetadata><terrainTypes>"
            + string.Join("\n", terrainTypes)
            + "</terrainTypes></sharedBoardMetadata>";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
        return SharedBoardMetadataParser.Parse(stream);
    }

    private static string Terrain(string name, int code, string category, string obstacle = "FALSE", int height = 0,
        string split = "0.0", int red = 0, int green = 0, int blue = 0) =>
        $"<terrainType name=\"{name}\" typeCode=\"{code}\" isLOSObstacle=\"{obstacle}\" isLOSHindrance=\"FALSE\" "
        + $"isHalfLevelHeight=\"FALSE\" isInherentTerrain=\"FALSE\" split=\"{split}\" isLowerLOSObstacle=\"FALSE\" "
        + $"isLowerLOSHindrance=\"FALSE\" height=\"{height}\" mapColorRed=\"{red}\" mapColorGreen=\"{green}\" "
        + $"mapColorBlue=\"{blue}\" LOSCategory=\"{category}\" />";
}
