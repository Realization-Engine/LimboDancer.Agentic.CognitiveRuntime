using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>Runs only when <c>AslMaps__VaslRoot</c> points to a local VASL checkout (ASL-MAP-073).</summary>
public sealed class VaslFactAttribute : FactAttribute
{
    public VaslFactAttribute()
    {
        if (VaslSource.FromEnvironment() is null)
        {
            Skip = $"Set {VaslSource.EnvironmentVariable} to a local VASL checkout to run this test.";
        }
    }
}

public sealed class LocalVaslCatalogTests
{
    [VaslFact]
    public void PinnedCatalogHas181UniqueTerrainTypes()
    {
        var catalog = LoadCatalog();

        // The file contains 182 terrainType elements, but Scrub 121 is inside an XML comment.
        Assert.Equal(181, catalog.Count);
        Assert.False(catalog.TryGet(121, out _));
        Assert.Equal((byte)100, catalog["Scrub"].Code);
        Assert.Equal((byte)213, catalog.Types[^1].Code);
        Assert.DoesNotContain(catalog.Types, type => type.Category == LosCategory.Stream);
    }

    [VaslFact]
    public void CatalogEntriesUsedByBoard01MatchVasl()
    {
        var catalog = LoadCatalog();
        Assert.Equal(new TerrainColor(175, 188, 106), catalog[0].MapColor);
        Assert.Equal("Open Ground", catalog[0].Name);
        Assert.True(catalog[0].IsOpen);

        var stone2 = catalog[42];
        Assert.Equal("Stone Building, 2 Level", stone2.Name);
        Assert.True(stone2.IsBuilding);
        Assert.Equal(2, stone2.Height);

        Assert.True(catalog["Paved Road"].IsOpen);
        Assert.True(catalog["Paved Road"].IsRoad);
        Assert.True(catalog["Wall"].IsHexsideTerrain);
        Assert.True(catalog["Rowhouse Wall, 1 Level"].IsHexsideTerrain);
        Assert.True(catalog["Stone Factory, 1.5 Level"].IsBuilding);
        Assert.False(catalog["Stone Factory, 1.5 Level"].IsBuildingTerrain);
        Assert.True(catalog["Stone Market Place"].IsBuildingTerrain);
        Assert.True(catalog["Gully"].IsDepression);
        Assert.True(catalog["Shallow Stream"].IsStream);
        Assert.True(catalog["Crags"].IsInherent);
    }

    private static TerrainCatalog LoadCatalog()
    {
        var source = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var result = source.ReadTerrainCatalog();
        Assert.Empty(result.Diagnostics);
        return Assert.IsType<TerrainCatalog>(result.Catalog);
    }
}
