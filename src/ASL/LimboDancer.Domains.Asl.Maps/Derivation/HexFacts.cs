using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>One location in a hex: a level (cellar -1, ground 0, upper levels) with its terrain.</summary>
public sealed record LocationFacts(int Level, TerrainType? Terrain, TerrainType? DepressionTerrain);

/// <summary>What the derivation concluded about one hexside of a hex.</summary>
public sealed record HexsideFacts(
    HexsideDirection Side,
    bool OnMap,
    TerrainType? Terrain,
    TerrainType? HexsideTerrain,
    bool Cliff,
    bool Slope,
    bool RailroadEmbankment,
    bool PartialOrchard,
    TerrainType? DepressionTerrain);

public sealed record BridgeFacts(TerrainType? Terrain, int RoadLevel);

/// <summary>
/// The derived facts for one hex (ASL-MAP-012). <see cref="Locations"/> runs from the lowest location to the
/// highest and includes <see cref="Center"/>. <see cref="CenterSource"/> is the derivation trace and is not part
/// of F2 equality.
/// </summary>
public sealed record HexFacts(
    HexName Hex,
    HexIndex Index,
    int BaseLevel,
    bool Stairway,
    LocationFacts Center,
    IReadOnlyList<LocationFacts> Locations,
    IReadOnlyList<HexsideFacts> Hexsides,
    BridgeFacts? Bridge,
    CenterTerrainSource CenterSource);

/// <summary>Hex facts for every hex of a board, in VASL hex order.</summary>
public sealed class HexFactSet
{
    private readonly Dictionary<HexIndex, HexFacts> byIndex;

    public HexFactSet(BoardGeometry geometry, string derivationVersion, IReadOnlyList<HexFacts> hexes)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(hexes);
        Geometry = geometry;
        DerivationVersion = derivationVersion;
        Hexes = hexes;
        byIndex = hexes.ToDictionary(facts => facts.Index);
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public string DerivationVersion
    {
        get;
    }

    public IReadOnlyList<HexFacts> Hexes
    {
        get;
    }

    public HexFacts this[HexIndex hex] => byIndex[hex];

    public HexFacts this[HexName hex] => byIndex[Geometry.IndexOf(hex)];
}
