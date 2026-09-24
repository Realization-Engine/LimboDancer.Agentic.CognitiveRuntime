using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>Which step of the center-terrain procedure decided a hex's terrain.</summary>
public enum CenterTerrainSource
{
    /// <summary>Step 1: the first on-grid point of the near-center probe order.</summary>
    CenterSample,

    /// <summary>Step 3: a building found at a hexside edge sample point.</summary>
    HexsideBuildingFallback,

    /// <summary>Step 4: a building found 5 pixels from the center (the last one found wins).</summary>
    ProbeBuildingFallback,

    /// <summary>Step 5: no terrain was found, so Open Ground is used.</summary>
    Default,
}

public sealed record CenterTerrainSample(TerrainType Terrain, CenterTerrainSource Source);

/// <summary>
/// Steps 1 to 5 of the VASL-compatible hex-fact derivation (VASL Board Ingestion Design, section 7.1),
/// reproducing <c>Hex.resetTerrain</c> up to building levels: the near-center sample and the two building
/// fallbacks. The remaining derivation steps arrive with ASL-MAP-03.
/// </summary>
public static class CenterTerrainSampler
{
    public static CenterTerrainSample Sample(TerrainGrid grid, TerrainCatalog catalog, HexIndex hex)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(catalog);
        var geometry = grid.Geometry;
        var center = geometry.CenterPoint(hex);

        // Step 1 (Hex.getnearcenterLocationTerrain): the first on-grid point of four diagonal neighbors, else the center.
        var terrain = FirstOnGrid(grid, catalog, center,
            (1, -1), (1, 1), (-1, 1), (-1, -1)) ?? TerrainAt(grid, catalog, center.X, center.Y);
        var source = CenterTerrainSource.CenterSample;

        // Step 3 (Hex.getHexsideBuildingTerrain): a building at any hexside edge sample, in hexside order.
        if (terrain is null || !terrain.IsBuilding)
        {
            foreach (var side in HexsideDirections.All)
            {
                var edge = geometry.EdgeSamplePoint(hex, side);
                var edgeTerrain = TerrainAt(grid, catalog, edge.X, edge.Y);
                if (edgeTerrain is { IsBuilding: true })
                {
                    terrain = edgeTerrain;
                    source = CenterTerrainSource.HexsideBuildingFallback;
                    break;
                }
            }
        }

        // Step 5 comes before step 4 in VASL: a missing terrain becomes Open Ground.
        if (terrain is null)
        {
            terrain = catalog["Open Ground"];
            source = CenterTerrainSource.Default;
        }

        // Step 4 (the probe fallback): 5 pixels right, left, below, above; every building found overwrites the last.
        if (!terrain.IsBuilding)
        {
            foreach (var (dx, dy) in new[] { (5, 0), (-5, 0), (0, 5), (0, -5) })
            {
                var probe = TerrainAt(grid, catalog, center.X + dx, center.Y + dy);
                if (probe is { IsBuilding: true })
                {
                    terrain = probe;
                    source = CenterTerrainSource.ProbeBuildingFallback;
                }
            }
        }

        return new CenterTerrainSample(terrain, source);
    }

    private static TerrainType? FirstOnGrid(TerrainGrid grid, TerrainCatalog catalog, GridPoint center, params (int Dx, int Dy)[] offsets)
    {
        foreach (var (dx, dy) in offsets)
        {
            if (grid.Geometry.ContainsCell(center.X + dx, center.Y + dy))
            {
                return TerrainAt(grid, catalog, center.X + dx, center.Y + dy);
            }
        }

        return null;
    }

    private static TerrainType? TerrainAt(TerrainGrid grid, TerrainCatalog catalog, int x, int y) =>
        grid.TryGetCode(x, y, out var code) && catalog.TryGet(code, out var terrain) ? terrain : null;
}
