using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class TerrainCatalogTests
{
    [Theory]
    [InlineData("Wall", LosCategory.Hexside, true)]
    [InlineData("Rowhouse Wall, 1 Level", LosCategory.Other, true)]
    [InlineData("Breach", LosCategory.Other, true)]
    [InlineData("Interior Factory Wall, 2 Level", LosCategory.Other, true)]
    [InlineData("Stone Building, 2 Level", LosCategory.Building, false)]
    public void HexsideTerrainIncludesVaslNamedWalls(string name, LosCategory category, bool expected)
    {
        Assert.Equal(expected, Type(name, category).IsHexsideTerrain);
    }

    [Theory]
    [InlineData(LosCategory.Building, true, true)]
    [InlineData(LosCategory.Marketplace, true, true)]
    [InlineData(LosCategory.Factory, true, false)]
    [InlineData(LosCategory.Open, false, false)]
    public void FactoriesAreBuildingsButNotBuildingTerrain(LosCategory category, bool building, bool buildingTerrain)
    {
        var type = Type("X", category);
        Assert.Equal(building, type.IsBuilding);
        Assert.Equal(buildingTerrain, type.IsBuildingTerrain);
    }

    [Theory]
    [InlineData(LosCategory.Open, true)]
    [InlineData(LosCategory.Road, true)]
    [InlineData(LosCategory.Water, true)]
    [InlineData(LosCategory.Woods, false)]
    [InlineData(LosCategory.Depression, false)]
    public void OpenTerrainFollowsVaslCategories(LosCategory category, bool expected)
    {
        Assert.Equal(expected, Type("X", category).IsOpen);
    }

    [Fact]
    public void NameBasedPredicatesMatchVasl()
    {
        Assert.True(Type("Shallow Stream", LosCategory.Depression).IsStream);
        Assert.False(Type("Gully", LosCategory.Depression).IsStream);
        Assert.True(Type("Stone Factory Wall, 2.5 Level", LosCategory.Factory).IsOutsideFactoryWall);
        Assert.True(Type("Cellar", LosCategory.Building).IsCellar);
        Assert.True(Type("Gutted Stone Building", LosCategory.Building).IsRoofless);
        Assert.True(Type("Roofless Stone Factory, 1.5 Level", LosCategory.Factory).IsRoofless);
        Assert.True(Type("Cliff", LosCategory.Hexside).IsCliff);
        Assert.True(Type("Rooftop", LosCategory.Open).IsRooftop);
    }

    [Fact]
    public void CatalogLooksUpByCodeAndNameAndRejectsDuplicates()
    {
        var catalog = new TerrainCatalog([Type("Woods", LosCategory.Woods, 60), Type("Open Ground", LosCategory.Open, 0)]);
        Assert.Equal(2, catalog.Count);
        Assert.Equal("Open Ground", catalog.Types[0].Name);
        Assert.Equal("Woods", catalog[60].Name);
        Assert.Equal((byte)60, catalog["Woods"].Code);
        Assert.False(catalog.TryGet(61, out _));
        Assert.Throws<KeyNotFoundException>(() => catalog[61]);

        Assert.Throws<ArgumentException>(() => new TerrainCatalog([Type("A", LosCategory.Open, 1), Type("B", LosCategory.Open, 1)]));
        Assert.Throws<ArgumentException>(() => new TerrainCatalog([Type("A", LosCategory.Open, 1), Type("A", LosCategory.Open, 2)]));
    }

    [Fact]
    public void ColorsFormatAsSvgHex()
    {
        Assert.Equal("#afbc6a", new TerrainColor(175, 188, 106).ToHex());
    }

    private static TerrainType Type(string name, LosCategory category, byte code = 0) =>
        new()
        {
            Code = code,
            Name = name,
            Category = category
        };
}
