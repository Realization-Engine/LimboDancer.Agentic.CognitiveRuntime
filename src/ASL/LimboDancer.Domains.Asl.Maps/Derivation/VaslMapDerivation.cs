using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;
using HexState = LimboDancer.Domains.Asl.Maps.Derivation.VaslCompatibleHexFactDerivation.HexState;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>
/// The hex grid of a VASL <c>Map</c> as its runtime builds it (VASL Board Ingestion Design, section 11): hexes keep
/// their state between passes, so derivations that add boards one at a time, rename hexes, or reset single hexes can
/// follow VASL's order of operations exactly. <see cref="VaslCompatibleHexFactDerivation.Derive"/> is the single-board
/// case.
/// </summary>
public sealed class VaslMapDerivation
{
    private readonly HexState[] hexes;
    private readonly Dictionary<HexIndex, HexState> byIndex;

    public VaslMapDerivation(BoardGeometry geometry, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(catalog);
        Geometry = geometry;
        Catalog = catalog;

        // Map constructors create every hex with terrain code 0.
        var initial = catalog.TryGet(0, out var codeZero) ? codeZero : catalog["Open Ground"];
        hexes = geometry.Hexes().Select(index => new HexState(geometry, index, initial)).ToArray();
        byIndex = hexes.ToDictionary(hex => hex.Index);
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public TerrainCatalog Catalog
    {
        get;
    }

    public HexName NameOf(HexIndex hex) => State(hex).Name;

    /// <summary><c>Hex.resetHexAndLocationNames</c>.</summary>
    public void Rename(HexIndex hex, HexName name) => State(hex).Name = name;

    public bool HasStairway(HexIndex hex) => State(hex).Stairway;

    public void SetStairway(HexIndex hex, bool stairway) => State(hex).Stairway = stairway;

    /// <summary><c>Map.getHex(String)</c>: the first hex in column-major order with the name, as VASL finds it.</summary>
    public HexIndex? Find(HexName name)
    {
        foreach (var hex in hexes)
        {
            if (hex.Name == name)
            {
                return hex.Index;
            }
        }

        return null;
    }

    /// <summary>
    /// <c>Map.setRBrrembankments</c>, <c>setPartialOrchards</c>, and <c>setSlopes</c>: each named hex, found with
    /// <see cref="Find"/>, has that flag set replaced by the annotation's sides. Names that match no hex are ignored.
    /// </summary>
    public void ApplyAnnotations(HexsideAnnotations annotations)
    {
        ArgumentNullException.ThrowIfNull(annotations);
        Replace(annotations.RailroadEmbankments, hex => hex.RailroadEmbankment);
        Replace(annotations.PartialOrchards, hex => hex.PartialOrchard);
        Replace(annotations.Slopes, hex => hex.Slope);
    }

    /// <summary>One <c>Map.resetHexTerrain</c> pass over every hex against the grid.</summary>
    public void Pass(TerrainGrid grid)
    {
        EnsureGrid(grid);
        VaslCompatibleHexFactDerivation.Pass(grid, Catalog, hexes, byIndex);
    }

    /// <summary><c>Hex.resetTerrain</c> for one hex.</summary>
    public void ResetHex(HexIndex hex, TerrainGrid grid)
    {
        EnsureGrid(grid);
        VaslCompatibleHexFactDerivation.ResetTerrain(grid, Catalog, byIndex, State(hex));
    }

    /// <summary>
    /// <c>Hex.copy</c> from a freshly constructed hex, as <c>BoardArchive.addLOSDatatoVASLMap</c> copies a reversed
    /// board's hexes into the map: the name, a ground-level center and hexside locations without terrain, no hexside
    /// terrain or cliffs, the stairway, and cleared annotation flags. Existing upper and lower location links are kept.
    /// </summary>
    public void CopyFresh(HexIndex hex, HexName name, bool stairway)
    {
        var state = State(hex);
        state.Name = name;
        state.BaseLevel = 0;
        state.Center.Level = 0;
        state.Center.Terrain = null;
        state.Center.Depression = null;
        for (var side = 0; side < 6; side++)
        {
            state.Hexsides[side].Level = 0;
            state.Hexsides[side].Terrain = null;
            state.Hexsides[side].Depression = null;
            state.HexsideTerrain[side] = null;
            state.Cliff[side] = false;
            state.Slope[side] = false;
            state.RailroadEmbankment[side] = false;
            state.PartialOrchard[side] = false;
        }

        state.Stairway = stairway;
    }

    /// <summary>The terrain the hex's center location currently has.</summary>
    public TerrainType? CenterTerrain(HexIndex hex) => State(hex).Center.Terrain;

    /// <summary>Whether the center location currently records depression terrain (<c>Hex.isDepressionTerrain</c>).</summary>
    public bool IsDepression(HexIndex hex) => State(hex).Center.Depression is not null;

    /// <summary>The hex's current base level (<c>Hex.getBaseLevelofHex</c>).</summary>
    public int BaseLevel(HexIndex hex) => State(hex).BaseLevel;

    /// <summary>The terrain of a hexside location (<c>Hex.getHexsideLocation(side).getTerrain()</c>).</summary>
    public TerrainType? HexsideLocationTerrain(HexIndex hex, HexsideDirection side) => State(hex).Hexsides[(int)side].Terrain;

    public HexFactSet Facts() => new(Geometry, VaslCompatibleHexFactDerivation.Version, hexes.Select(hex => hex.ToFacts()).ToArray());

    /// <summary><c>Location.setTerrain</c> on the center location.</summary>
    internal void SetCenterTerrain(HexIndex hex, TerrainType terrain) => State(hex).Center.Terrain = terrain;

    /// <summary>
    /// The location edits of <c>VASLBoard.setAllBuildingstoSingleStory</c> for one hex: in a building hex with upper
    /// locations, the first rooftop found going up becomes level 1 directly above the center and the locations passed
    /// on the way are unlinked; any cellar below the center is unlinked.
    /// </summary>
    internal void MakeSingleStory(HexIndex hex)
    {
        var center = State(hex).Center;
        if (center.Up is not null && center.Terrain is { IsBuildingTerrain: true })
        {
            var current = center;
            VaslCompatibleHexFactDerivation.LocationState? next = null;
            do
            {
                current = next ?? current.Up!;
                if (current.Terrain is { IsRooftop: true })
                {
                    current.Level = 1;
                    next = null;
                    current.Down = center;
                    center.Up = current;
                    break;
                }

                next = current.Up;
                current.Down = null;
                current.Up = null;
                center.Up = null;
            }
            while (next is not null);
        }

        if (center.Down is { } down)
        {
            down.Up = null;
            down.Down = null;
            center.Down = null;
        }
    }

    private void Replace(IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> source, Func<HexState, bool[]> flags)
    {
        foreach (var (name, sides) in source)
        {
            if (Find(name) is not { } index)
            {
                continue;
            }

            var target = flags(byIndex[index]);
            Array.Clear(target);
            foreach (var side in sides)
            {
                target[(int)side] = true;
            }
        }
    }

    private HexState State(HexIndex hex) =>
        byIndex.TryGetValue(hex, out var state) ? state : throw new ArgumentOutOfRangeException(nameof(hex), hex, "Hex is not on this map.");

    private void EnsureGrid(TerrainGrid grid)
    {
        ArgumentNullException.ThrowIfNull(grid);
        if (grid.Geometry != Geometry)
        {
            throw new ArgumentException("The grid belongs to a different geometry.", nameof(grid));
        }
    }
}
