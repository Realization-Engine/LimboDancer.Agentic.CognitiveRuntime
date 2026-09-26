using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Los;

/// <summary>
/// What an LOS read needs from one board beyond its hex facts (LOS Design, section 5): the terrain grid the walk samples,
/// the terrain catalog its codes refer to, and, for composing the board into a placed map with
/// <see cref="VaslMapBuilder"/>, the board's hexside annotations and the LOS scenario-specific rules its placements may
/// name. VASL boards and authored boards both supply it.
/// </summary>
public sealed record LosData(TerrainGrid Grid, TerrainCatalog Catalog, HexsideAnnotations Annotations, LosSsRuleSet Rules)
{
    /// <summary>LOS data for a board that is never composed with rules or annotations.</summary>
    public LosData(TerrainGrid grid, TerrainCatalog catalog)
        : this(grid, catalog, HexsideAnnotations.None, LosSsRuleSet.Empty)
    {
    }
}
