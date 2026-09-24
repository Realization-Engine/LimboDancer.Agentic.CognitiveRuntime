using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Derivation;

/// <summary>Per-hexside flags that VASL keeps in board metadata rather than in the grid.</summary>
public sealed record HexsideAnnotations(
    IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> Slopes,
    IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> RailroadEmbankments,
    IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> PartialOrchards)
{
    public static HexsideAnnotations None
    {
        get;
    } = new(
        new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(),
        new Dictionary<HexName, IReadOnlySet<HexsideDirection>>(),
        new Dictionary<HexName, IReadOnlySet<HexsideDirection>>());
}

/// <summary>
/// The VASL-compatible hex-fact derivation (VASL Board Ingestion Design, section 7), reproducing
/// <c>Hex.resetTerrain</c> and its helpers as VASL's runtime runs them for one board: every hex in column-major
/// order, twice. The rule is reproduce, not improve (section 7.4); quirks are kept and commented.
/// </summary>
public static class VaslCompatibleHexFactDerivation
{
    public const string Version = "1.0.0";

    private static readonly HashSet<string> BuildingsWithoutUpperLevels = new(StringComparer.Ordinal)
    {
        "Wooden Building",
        "Stone Building",
        "Huts",
        "MultipleWooden",
    };

    // Hex.fixspecialcasesAddRooftops: bdRO wooden warehouses, matched by hex name on any board.
    private static readonly HashSet<string> WarehouseRooftopHexes = new(StringComparer.Ordinal)
    {
        "I21", "I22", "I23", "I24", "I25", "I26", "I29", "I30", "J21", "J22", "J23", "J24", "J25", "J26",
    };

    public static HexFactSet Derive(TerrainGrid grid, TerrainCatalog catalog, HexsideAnnotations annotations)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(annotations);
        var context = new Context(grid, catalog, annotations);

        // BoardArchive.addLOSDatatoVASLMap ends with resetHexTerrain, and ASLMap.addBoardsToMap runs it again.
        for (var pass = 0; pass < 2; pass++)
        {
            foreach (var hex in context.Hexes)
            {
                ResetTerrain(context, hex);
            }
        }

        return new HexFactSet(grid.Geometry, Version, context.Hexes.Select(hex => hex.ToFacts()).ToArray());
    }

    // Hex.resetTerrain
    private static void ResetTerrain(Context context, HexState hex)
    {
        var sample = CenterTerrainSampler.Sample(context.Grid, context.Catalog, hex.Index);
        var centerTerrain = sample.Terrain;
        hex.CenterSource = sample.Source;
        var centerElevation = NearCenterElevation(context, hex);
        hex.Center.Terrain = centerTerrain;
        AddBuildingLevels(context, hex, centerTerrain);

        for (var side = 0; side < 6; side++)
        {
            if (!hex.OnMap[side])
            {
                continue;
            }

            var edge = hex.Edges[side];
            var terrain = context.TerrainAt(edge.X, edge.Y);
            if (hex.RailroadEmbankment[side])
            {
                terrain = context.Catalog["Rrembankment"];
                hex.HexsideTerrain[side] = terrain;
            }

            if (hex.PartialOrchard[side])
            {
                terrain = context.Catalog["PartialOrchard"];
                hex.HexsideTerrain[side] = terrain;
            }

            terrain ??= context.OpenGround;

            // Where this hex's edge sample is not hexside terrain, the neighbor's opposite edge sample may be.
            if (context.Geometry.Neighbor(hex.Index, (HexsideDirection)side) is { } neighborIndex)
            {
                var neighbor = context[neighborIndex];
                var opposite = neighbor.Edges[(side + 3) % 6];
                var oppositeTerrain = context.TerrainAt(opposite.X, opposite.Y);
                if (oppositeTerrain is not null && !terrain.IsHexsideTerrain && oppositeTerrain.IsHexsideTerrain)
                {
                    terrain = oppositeTerrain;
                }
            }

            if (terrain.IsHexsideTerrain)
            {
                hex.HexsideTerrain[side] = terrain;

                // This first test is an exact name match; setHexsideTerrain later tests the name contains "Cliff".
                if (terrain.Name == "Cliff")
                {
                    hex.Cliff[side] = true;
                }
            }

            hex.Hexsides[side].Terrain = terrain;
            if (terrain.Name.Contains("OutOfBounds", StringComparison.Ordinal))
            {
                hex.OnMap[side] = false;
            }
        }

        hex.BaseLevel = centerElevation;
        SetDepressionTerrain(hex);
        SetInherentTerrain(context, hex);
        ResetHexsideTerrain(context, hex);
        FixBridgesTunnelWater(context, hex);
    }

    // Hex.getnearcenterLocationElevation. When the fourth probe (x-1, y-1) is the first on the grid, VASL reads
    // the elevation at (x-1, y+1); that is reproduced.
    private static int NearCenterElevation(Context context, HexState hex)
    {
        var (x, y) = (hex.CenterPoint.X, hex.CenterPoint.Y);
        var geometry = context.Geometry;
        if (geometry.ContainsCell(x + 1, y - 1))
        {
            return context.ElevationAt(x + 1, y - 1);
        }

        if (geometry.ContainsCell(x + 1, y + 1))
        {
            return context.ElevationAt(x + 1, y + 1);
        }

        if (geometry.ContainsCell(x - 1, y + 1))
        {
            return context.ElevationAt(x - 1, y + 1);
        }

        if (geometry.ContainsCell(x - 1, y - 1))
        {
            return context.ElevationAt(x - 1, y + 1);
        }

        return context.ElevationAt(x, y);
    }

    // Hex.addBuildingLevels. Links are replaced, never cleared, so a second pass rebuilds the same chains.
    private static void AddBuildingLevels(Context context, HexState hex, TerrainType centerTerrain)
    {
        if (!centerTerrain.IsBuilding)
        {
            return;
        }

        var center = hex.Center;
        var oldStairway = hex.Stairway
            && center.Terrain?.Name != "Stone Building, 1 Level"
            && center.Terrain?.Name != "Wooden Building, 1 Level";
        if (centerTerrain.Category == LosCategory.Marketplace)
        {
            center.Terrain = context.OpenGround;
        }

        var hasUpperLevels = !BuildingsWithoutUpperLevels.Contains(centerTerrain.Name);
        var previous = center;
        for (var level = 1; level <= centerTerrain.Height; level++)
        {
            // Factories without a stairway get no upper levels.
            if (hasUpperLevels && !(centerTerrain.IsFactory && !hex.Stairway))
            {
                var location = new LocationState { Level = level, Terrain = centerTerrain };
                previous.Up = location;
                location.Down = previous;
                previous = location;
            }
        }

        hex.Stairway = center.Terrain?.Name is "Stone Building, 1 Level" or "Wooden Building, 1 Level" || oldStairway;

        if (hasUpperLevels && !centerTerrain.IsFactory)
        {
            var cellar = new LocationState { Level = -1, Terrain = context.Catalog["Cellar"] };
            center.Down = cellar;
            cellar.Up = center;
        }

        if (hasUpperLevels && !centerTerrain.IsRoofless)
        {
            var top = center;
            var level = 0;
            while (top.Up is not null)
            {
                top = top.Up;
                level++;
            }

            if (centerTerrain.IsFactory)
            {
                level = centerTerrain.Height;
            }

            var rooftop = new LocationState { Level = level + 1, Terrain = context.Catalog["Rooftop"] };
            top.Up = rooftop;
            rooftop.Down = top;
        }

        if (center.Terrain?.Name == "Wooden Building" && WarehouseRooftopHexes.Contains(hex.Name.ToString()))
        {
            var rooftop = new LocationState { Level = 1, Terrain = context.Catalog["Rooftop"] };
            center.Up = rooftop;
            rooftop.Down = center;
        }
    }

    // Hex.setDepressionTerrain: the depression recorded on an earlier pass persists.
    private static void SetDepressionTerrain(HexState hex)
    {
        var center = hex.Center;
        if (center.Terrain is { IsDepression: true })
        {
            center.Depression = center.Terrain;
        }
        else if (center.Depression is { IsDepression: true })
        {
            center.Terrain = center.Depression;
        }
    }

    // Hex.setInherentTerrain: scans the border's bounding rectangle (not the polygon itself), x then y, and takes
    // the first inherent-terrain cell whose nearest location is the hex center.
    private static void SetInherentTerrain(Context context, HexState hex)
    {
        var bounds = hex.Border;
        for (var x = bounds.MinX; x < bounds.MinX + bounds.Width && x < context.Geometry.GridWidth; x++)
        {
            for (var y = bounds.MinY; y < bounds.MinY + bounds.Height; y++)
            {
                if (x >= 0 && context.Geometry.ContainsCell(x, y)
                    && context.TerrainAt(x, y) is { IsInherent: true } terrain
                    && NearestLocationIsCenter(hex, x, y))
                {
                    hex.Center.Terrain = terrain;
                    return;
                }
            }
        }
    }

    // Hex.getNearestLocation(x, y) == centerLocation: no on-map hexside edge point is strictly closer.
    private static bool NearestLocationIsCenter(HexState hex, int x, int y)
    {
        var distance = Distance(x, y, hex.CenterPoint.X, hex.CenterPoint.Y);
        var nearestIsCenter = true;
        for (var side = 0; side < 6; side++)
        {
            if (!hex.OnMap[side])
            {
                continue;
            }

            var next = Distance(x, y, hex.Edges[side].X, hex.Edges[side].Y);
            if (next < distance)
            {
                distance = next;
                nearestIsCenter = false;
            }
        }

        return nearestIsCenter;
    }

    // Hex.resetHexsideTerrain
    private static void ResetHexsideTerrain(Context context, HexState hex)
    {
        for (var side = 0; side < 6; side++)
        {
            if (!hex.OnMap[side])
            {
                continue;
            }

            var edge = hex.Edges[side];
            var terrain = context.TerrainAt(edge.X, edge.Y);
            if (terrain is { IsOpen: true })
            {
                terrain = CheckWallHedgeGap(context, hex, side);
            }

            terrain ??= context.OpenGround;
            if (terrain.IsHexsideTerrain)
            {
                SetHexsideTerrain(hex, side, terrain);
            }
            else if (terrain.IsDepression)
            {
                hex.Hexsides[side].Depression = terrain;
            }

            // Takes the neighbor's current recorded hexside terrain, so the result depends on processing order.
            if (context.Geometry.Neighbor(hex.Index, (HexsideDirection)side) is { } neighborIndex
                && context[neighborIndex].HexsideTerrain[(side + 3) % 6] is { } neighborTerrain)
            {
                SetHexsideTerrain(hex, side, neighborTerrain);
            }
        }
    }

    private static void SetHexsideTerrain(HexState hex, int side, TerrainType terrain)
    {
        if (terrain.IsHexsideTerrain)
        {
            hex.HexsideTerrain[side] = terrain;
            if (terrain.IsCliff)
            {
                hex.Cliff[side] = true;
            }
        }
    }

    // Hex.checkWallHedgeGap: halfway between the edge sample and each end vertex of the hexside. The second point is
    // tested only when the first is off the grid.
    private static TerrainType? CheckWallHedgeGap(Context context, HexState hex, int side)
    {
        var (firstVertex, secondVertex) = side switch
        {
            0 => (0, 1),
            1 => (1, 2),
            2 => (3, 2),
            3 => (4, 3),
            4 => (5, 4),
            _ => (0, 5),
        };
        var edge = hex.Edges[side];
        var border = hex.Border.Points;
        var firstX = (int)((border[firstVertex].X + (double)edge.X) / 2);
        var firstY = (int)((border[firstVertex].Y + (double)edge.Y) / 2);
        var secondX = (int)((border[secondVertex].X + (double)edge.X) / 2);
        var secondY = (int)((border[secondVertex].Y + (double)edge.Y) / 2);
        if (context.Geometry.ContainsCell(firstX, firstY))
        {
            return WallOrHedge(context, firstX, firstY);
        }

        return context.Geometry.ContainsCell(secondX, secondY) ? WallOrHedge(context, secondX, secondY) : null;
    }

    private static TerrainType? WallOrHedge(Context context, int x, int y) =>
        context.TerrainAt(x, y)?.Name switch
        {
            "Wall" => context.Catalog["Wall"],
            "Hedge" => context.Catalog["Hedge"],
            _ => null,
        };

    // Hex.fixBridgesTunnelWater
    private static void FixBridgesTunnelWater(Context context, HexState hex)
    {
        if (!BorderContains(context, hex, terrain => terrain.IsBridge || terrain.IsTunnel, out _))
        {
            return;
        }

        var center = hex.Center;
        LocationState? bridgeLocation = null;
        BorderContains(context, hex, terrain => terrain.IsBridge, out var bridgeTerrain);
        if (center.Depression is not null)
        {
            var location = center.Copy();
            location.Depression = null;
            location.Terrain = bridgeTerrain;
            location.Level = BridgeAbsoluteLevel(context, hex) - hex.BaseLevel;
            location.Down = center;
            center.Up = location;
            bridgeLocation = location;
        }
        else if (center.Terrain is { IsBridge: true })
        {
            BorderContains(context, hex, terrain => terrain.IsDepression, out var depressionTerrain);
            var location = center.Copy();
            location.Depression = depressionTerrain;
            var adjustment = 1;
            if (hex.BaseLevel == 0 && center.Level == 0)
            {
                center.Level = 1;
            }
            else if (hex.BaseLevel > 0)
            {
                adjustment = 2;
            }

            location.Level = center.Level - adjustment;
            location.Terrain = depressionTerrain;
            location.Up = center;
            center.Down = location;
            bridgeLocation = center;
        }
        else if (center.Terrain is { IsTunnel: true })
        {
            var location = center.Copy();
            location.Depression = null;
            location.Terrain = hex.Name.ToString().Contains("II50", StringComparison.Ordinal)
                ? context.Catalog["Woods"]
                : context.OpenGround;
            location.Level = center.Level + 1;
            location.Down = center;
            center.Up = location;
        }
        else if (center.Terrain is { IsWater: true })
        {
            var location = center.Copy();
            location.Depression = null;
            location.Level = BridgeAbsoluteLevel(context, hex) - hex.BaseLevel;
            location.Terrain = bridgeTerrain;
            location.Down = center;
            center.Up = location;
            bridgeLocation = location;
        }

        if (bridgeLocation is not null)
        {
            hex.Bridge = new BridgeFacts(bridgeTerrain, BridgeAbsoluteLevel(context, hex));
        }
    }

    // Hex.getBridgeLocationAbsoluteLevel: the elevation at the first road hexside, else one above the hex.
    private static int BridgeAbsoluteLevel(Context context, HexState hex)
    {
        for (var side = 0; side < 6; side++)
        {
            if (hex.Hexsides[side].Terrain is { IsDepression: false, IsRoad: true })
            {
                var edge = hex.Edges[side];
                return context.ElevationAt(edge.X, edge.Y);
            }
        }

        return hex.BaseLevel + 1;
    }

    // Scans the border's bounding rectangle, x then y, for the first cell inside the polygon matching the predicate.
    private static bool BorderContains(Context context, HexState hex, Func<TerrainType, bool> predicate, out TerrainType? found)
    {
        var bounds = hex.Border;
        for (var x = bounds.MinX; x < bounds.MinX + bounds.Width; x++)
        {
            for (var y = bounds.MinY; y < bounds.MinY + bounds.Height; y++)
            {
                if (bounds.Contains(x, y) && context.TerrainAt(x, y) is { } terrain && predicate(terrain))
                {
                    found = terrain;
                    return true;
                }
            }
        }

        found = null;
        return false;
    }

    private static double Distance(double x1, double y1, double x2, double y2)
    {
        var dx = x1 - x2;
        var dy = y1 - y2;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    private sealed class Context
    {
        private readonly Dictionary<HexIndex, HexState> byIndex;

        public Context(TerrainGrid grid, TerrainCatalog catalog, HexsideAnnotations annotations)
        {
            Grid = grid;
            Catalog = catalog;
            Geometry = grid.Geometry;
            OpenGround = catalog["Open Ground"];
            var initial = catalog.TryGet(0, out var codeZero) ? codeZero : OpenGround;
            Hexes = Geometry.Hexes().Select(index => new HexState(Geometry, grid, annotations, index, initial)).ToArray();
            byIndex = Hexes.ToDictionary(hex => hex.Index);
        }

        public TerrainGrid Grid
        {
            get;
        }

        public TerrainCatalog Catalog
        {
            get;
        }

        public BoardGeometry Geometry
        {
            get;
        }

        public TerrainType OpenGround
        {
            get;
        }

        public IReadOnlyList<HexState> Hexes
        {
            get;
        }

        public HexState this[HexIndex index] => byIndex[index];

        // Map.getGridTerrain: null off the grid.
        public TerrainType? TerrainAt(int x, int y) =>
            Grid.TryGetCode(x, y, out var code) && Catalog.TryGet(code, out var terrain) ? terrain : null;

        // Map.getGridElevation: 0 off the grid.
        public int ElevationAt(int x, int y) => Geometry.ContainsCell(x, y) ? Grid.ElevationAt(x, y) : 0;
    }

    private sealed class LocationState
    {
        public int Level
        {
            get; set;
        }

        public TerrainType? Terrain
        {
            get; set;
        }

        public TerrainType? Depression
        {
            get; set;
        }

        public LocationState? Up
        {
            get; set;
        }

        public LocationState? Down
        {
            get; set;
        }

        // Location(Location): copies name, level, terrain, and depression terrain, but not the links.
        public LocationState Copy() => new() { Level = Level, Terrain = Terrain, Depression = Depression };

        public LocationFacts ToFacts() => new(Level, Terrain, Depression);
    }

    private sealed class HexState
    {
        public HexState(BoardGeometry geometry, TerrainGrid grid, HexsideAnnotations annotations, HexIndex index, TerrainType initial)
        {
            Index = index;
            Name = geometry.NameOf(index);
            CenterPoint = geometry.CenterPoint(index);
            Edges = HexsideDirections.All.Select(side => geometry.EdgeSamplePoint(index, side)).ToArray();
            Border = new JavaPolygon(geometry.Border(index));
            Stairway = grid.HasStairway(index);
            Center = new LocationState { Terrain = initial };
            Hexsides = Enumerable.Range(0, 6).Select(_ => new LocationState { Terrain = initial }).ToArray();
            Slope = Flags(annotations.Slopes);
            RailroadEmbankment = Flags(annotations.RailroadEmbankments);
            PartialOrchard = Flags(annotations.PartialOrchards);
        }

        public HexIndex Index
        {
            get;
        }

        public HexName Name
        {
            get;
        }

        public GridPoint CenterPoint
        {
            get;
        }

        public GridPoint[] Edges
        {
            get;
        }

        public JavaPolygon Border
        {
            get;
        }

        // Under the runtime grid configuration no hexside starts off the map; OutOfBounds terrain can turn one off.
        public bool[] OnMap { get; } = [true, true, true, true, true, true];

        public bool Stairway
        {
            get; set;
        }

        public int BaseLevel
        {
            get; set;
        }

        public LocationState Center
        {
            get;
        }

        public LocationState[] Hexsides
        {
            get;
        }

        public TerrainType?[] HexsideTerrain { get; } = new TerrainType?[6];

        public bool[] Cliff { get; } = new bool[6];

        public bool[] Slope
        {
            get;
        }

        public bool[] RailroadEmbankment
        {
            get;
        }

        public bool[] PartialOrchard
        {
            get;
        }

        public BridgeFacts? Bridge
        {
            get; set;
        }

        public CenterTerrainSource CenterSource
        {
            get; set;
        }

        public HexFacts ToFacts()
        {
            var locations = new List<LocationFacts>();
            var visited = new HashSet<LocationState> { Center };
            for (var down = Center.Down; down is not null && visited.Add(down); down = down.Down)
            {
                locations.Insert(0, down.ToFacts());
            }

            locations.Add(Center.ToFacts());
            for (var up = Center.Up; up is not null && visited.Add(up); up = up.Up)
            {
                locations.Add(up.ToFacts());
            }

            var hexsides = Enumerable.Range(0, 6).Select(side => new HexsideFacts(
                (HexsideDirection)side,
                OnMap[side],
                Hexsides[side].Terrain,
                HexsideTerrain[side],
                Cliff[side],
                Slope[side],
                RailroadEmbankment[side],
                PartialOrchard[side],
                Hexsides[side].Depression)).ToArray();
            return new HexFacts(Name, Index, BaseLevel, Stairway, Center.ToFacts(), locations, hexsides, Bridge, CenterSource);
        }

        private bool[] Flags(IReadOnlyDictionary<HexName, IReadOnlySet<HexsideDirection>> source)
        {
            var flags = new bool[6];
            if (source.TryGetValue(Name, out var sides))
            {
                foreach (var side in sides)
                {
                    flags[(int)side] = true;
                }
            }

            return flags;
        }
    }
}
