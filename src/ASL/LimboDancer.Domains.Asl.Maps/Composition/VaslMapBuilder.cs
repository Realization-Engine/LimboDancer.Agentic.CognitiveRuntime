using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Composition;

/// <summary>
/// Where a board sits in a map (ASL-MAP-024): its board slot, whether it is reversed (rotated 180 degrees), and the
/// LOS scenario-specific rules applied to it, by name in the order VASL applies them.
/// </summary>
public sealed record BoardPlacement(BoardRef Board, int Column, int Row, bool Reversed, IReadOnlyList<string> Rules)
{
    public BoardPlacement(BoardRef board, int column = 0, int row = 0)
        : this(board, column, row, false, [])
    {
    }

    /// <summary>The compact form used by scenario files and URLs: <c>01@0,0/r[NoStairwells,BrushToOpenGround]</c>.</summary>
    public override string ToString()
    {
        var name = Board.Kind == BoardRefKind.Vasl ? Board.VaslBoardName : Board.Value;
        var text = $"{name}@{Column},{Row}{(Reversed ? "/r" : string.Empty)}";
        return Rules.Count == 0 ? text : $"{text}[{string.Join(',', Rules)}]";
    }
}

/// <summary>A placed board with the grid and hexside annotations it contributes.</summary>
public sealed record PlacedBoard(BoardPlacement Placement, TerrainGrid Grid, HexsideAnnotations Annotations);

/// <summary>One board's hexes in a built map.</summary>
public sealed record PlacedBoardLayout(BoardPlacement Placement, BoardGeometry Geometry, int X, int Y, int MapColumn, int MapRow);

/// <summary>
/// A map built from placed boards as VASL's runtime builds it: the map geometry, the final grid, the derived facts with
/// each hex's name as VASL leaves it, and where each board's hexes went.
/// </summary>
public sealed class VaslMap
{
    private readonly Dictionary<(BoardRef Board, HexName Hex), HexIndex> locations = [];
    private readonly Dictionary<HexIndex, (BoardRef Board, HexName Hex)> owners = [];

    internal VaslMap(BoardGeometry geometry, TerrainGrid grid, HexFactSet facts, IReadOnlyList<PlacedBoardLayout> boards)
    {
        Geometry = geometry;
        Grid = grid;
        Facts = facts;
        Boards = boards;
        foreach (var board in boards)
        {
            foreach (var local in board.Geometry.Hexes())
            {
                var index = MapHex(board, local);
                var name = board.Geometry.NameOf(local);
                locations[(board.Placement.Board, name)] = index;

                // Later boards overwrite the names of shared edge hexes, so they own them.
                owners[index] = (board.Placement.Board, name);
            }
        }
    }

    public BoardGeometry Geometry
    {
        get;
    }

    public TerrainGrid Grid
    {
        get;
    }

    public HexFactSet Facts
    {
        get;
    }

    public IReadOnlyList<PlacedBoardLayout> Boards
    {
        get;
    }

    /// <summary>The map hex a placed board's hex occupies, such as <c>bd01:E4</c>; a shared edge hex answers for both boards.</summary>
    public HexIndex? Locate(BoardRef board, HexName hex) => locations.TryGetValue((board, hex), out var index) ? index : null;

    /// <summary>The board and hex name a map hex belongs to; a shared edge hex belongs to the board placed later.</summary>
    public (BoardRef Board, HexName Hex)? OwnerOf(HexIndex hex) => owners.TryGetValue(hex, out var owner) ? owner : null;

    internal static HexIndex MapHex(PlacedBoardLayout board, HexIndex local) => MapLayout.MapHex(board, local);
}

/// <summary>The outcome of building a map: the map, or the diagnostics that stopped it (VASL would disable LOS).</summary>
public sealed record VaslMapResult(VaslMap? Map, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Map is not null;
}

/// <summary>
/// Builds a map from placed boards following <c>ASLMap.buildVASLMap</c>, <c>ASLMap.addBoardsToMap</c>, and
/// <c>BoardArchive.addLOSDatatoVASLMap</c> for uncropped boards (VASL Board Ingestion Design, section 11): boards abut
/// in slots, half-hex seams merge with non-open terrain winning, each board's LOS scenario-specific rules are applied
/// in VASL's two phases, reversed boards are rotated 180 degrees, and the hex grid is reset after each board and once
/// more at the end. Where VASL's runtime would disable LOS, the build fails with a diagnostic. Overlays are not
/// supported: VASL derives their terrain from overlay artwork (ASL-MAP-065).
/// </summary>
public static class VaslMapBuilder
{
    public const string Version = "1.0.0";

    /// <summary>VASL counts at most three boards across a map row (the <c>previousx</c> sum in <c>buildVASLMap</c>).</summary>
    public const int MaxBoardsPerRow = 3;

    public static VaslMapResult Build(IReadOnlyList<PlacedBoard> boards, TerrainCatalog catalog, LosSsRuleSet rules)
    {
        ArgumentNullException.ThrowIfNull(boards);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rules);
        var diagnostics = new List<MapDiagnostic>();

        // VASL adds boards in board-picker order: row by row, left to right.
        boards = [.. boards.OrderBy(board => board.Placement.Row).ThenBy(board => board.Placement.Column)];
        var layout = MapLayout.Create([.. boards.Select(board => (board.Placement, board.Grid.Geometry))]);
        diagnostics.AddRange(layout.Diagnostics);
        if (layout.Layout is not { } laid)
        {
            return new VaslMapResult(null, diagnostics);
        }

        var geometry = laid.Geometry;
        var placed = laid.Boards.ToArray();
        var grid = new VaslMapGrid(geometry);
        var map = new VaslMapDerivation(geometry, catalog);
        var context = new BuildContext(grid, map, catalog, rules, new VaslHexLocator(geometry, runtime: true), diagnostics);
        for (var index = 0; index < boards.Count; index++)
        {
            if (!AddBoard(context, boards[index], placed[index]))
            {
                return new VaslMapResult(null, diagnostics);
            }
        }

        // ASLMap.addBoardsToMap
        map.Pass(grid.Snapshot());
        var final = grid.Snapshot(map.HasStairway);
        return new VaslMapResult(new VaslMap(geometry, final, map.Facts(), placed), diagnostics);
    }

    private sealed record BuildContext(
        VaslMapGrid Grid, VaslMapDerivation Map, TerrainCatalog Catalog, LosSsRuleSet Rules, VaslHexLocator Locator, List<MapDiagnostic> Diagnostics);

    // BoardArchive.addLOSDatatoVASLMap for an uncropped board without overlays.
    private static bool AddBoard(BuildContext context, PlacedBoard board, PlacedBoardLayout layout)
    {
        var grid = context.Grid;
        var source = board.Grid;
        var geometry = source.Geometry;
        var catalog = context.Catalog;
        for (var x = 0; x < geometry.GridWidth; x++)
        {
            for (var y = 0; y < geometry.GridHeight; y++)
            {
                var mapX = x + layout.X;
                var mapY = y + layout.Y;
                grid.SetElevation(mapX, mapY, source.ElevationAt(x, y));
                var code = source.CodeAt(x, y);

                // Half-hex seams: where the earlier board's terrain is not open and this board's is, the earlier one wins.
                if (layout.X > 0 && x == 0)
                {
                    var earlier = grid.CodeAt(layout.X - 1, mapY);
                    if (!catalog[earlier].IsOpen && catalog[code].IsOpen)
                    {
                        code = earlier;
                    }
                }

                if (layout.Y > 0 && y == 0)
                {
                    var earlier = catalog[grid.CodeAt(mapX, mapY - 1)];
                    var current = catalog[code];
                    if (!earlier.IsOpen && current.IsOpen)
                    {
                        code = earlier.Code;
                    }
                    else if (earlier.IsOpen && !current.IsOpen)
                    {
                        code = current.Code;
                    }
                }

                grid.SetCode(mapX, mapY, code);
            }
        }

        var rules = board.Placement.Rules;
        if (!ApplyGridRules(context, board.Placement))
        {
            return false;
        }

        var map = context.Map;
        if (board.Placement.Reversed)
        {
            Flip(grid, layout.X, layout.Y, geometry.GridWidth, geometry.GridHeight);
            var snapshot = grid.Snapshot();

            // Freshly built hexes are copied onto the map in rotated order, each reset as it lands.
            for (var column = 0; column < geometry.WidthInHexes; column++)
            {
                var sourceColumn = geometry.WidthInHexes - column - 1;
                var rowCount = geometry.RowCount(sourceColumn);
                for (var row = 0; row < rowCount; row++)
                {
                    var local = new HexIndex(sourceColumn, rowCount - row - 1);
                    var destination = new HexIndex(layout.MapColumn + column, layout.MapRow + row);
                    map.CopyFresh(destination, geometry.NameOf(local), source.HasStairway(local));
                    map.ResetHex(destination, snapshot);
                }
            }
        }
        else
        {
            foreach (var local in geometry.Hexes())
            {
                var destination = VaslMap.MapHex(layout, local);
                map.SetStairway(destination, source.HasStairway(local));
                map.Rename(destination, geometry.NameOf(local));
            }
        }

        if (rules.Count > 0 && !ApplyHexRules(context, board.Placement))
        {
            return false;
        }

        map.ApplyAnnotations(board.Annotations);
        map.Pass(grid.Snapshot());
        return true;
    }

    // Map.flipTerrainAndElevationGrids: the board's rectangle of both grids rotated 180 degrees.
    private static void Flip(VaslMapGrid grid, int left, int top, int width, int height)
    {
        var codes = new byte[width, height];
        var elevations = new sbyte[width, height];
        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                codes[x, y] = grid.CodeAt(left + width - x - 1, top + height - y - 1);
                elevations[x, y] = grid.ElevationAt(left + width - x - 1, top + height - y - 1);
            }
        }

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                grid.SetCode(left + x, top + y, codes[x, y]);
                grid.SetElevation(left + x, top + y, elevations[x, y]);
            }
        }
    }

    // VASLBoard.applyColorSSRulestoTerrainElevationGrids. Every rule reads and writes the whole map grid, including
    // boards added earlier.
    private static bool ApplyGridRules(BuildContext context, BoardPlacement placement)
    {
        var pacific = false;
        foreach (var name in placement.Rules)
        {
            if (!context.Rules.TryGet(name, out var rule))
            {
                // Unknown rules are ignored for geomorphic boards (only RBv3 and RO give them meaning).
                continue;
            }

            var applied = rule.Kind switch
            {
                LosSsRuleKind.CustomCode => GridCustomCode(context, rule, ref pacific),
                LosSsRuleKind.TerrainMap => ChangeTerrain(context, rule, rule.FromValue, rule.ToValue),
                LosSsRuleKind.ElevationMap => ElevationMap(context, rule),
                LosSsRuleKind.TerrainToElevationMap => TerrainToElevation(context, rule),
                LosSsRuleKind.ElevationToTerrainMap => ElevationToTerrain(context, rule),
                LosSsRuleKind.TerrainToSelectElevationMap => TerrainToSelectElevation(context, rule),
                _ => true,
            };
            if (!applied)
            {
                return false;
            }
        }

        // There is no explicit Pacific rule: bamboo, palm trees, and dense jungle imply that woods are light jungle.
        return !pacific || ChangeTerrain(context, null, "Woods", "Light Jungle");
    }

    private static bool GridCustomCode(BuildContext context, LosSsRule rule, ref bool pacific)
    {
        switch (rule.Name)
        {
            case "RowhouseBarsToBuildings":
                return ChangeTerrain(context, rule, "Rowhouse Wall", "Stone Building")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 1 Level", "Stone Building, 1 Level")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 2 Level", "Stone Building, 2 Level")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 3 Level", "Stone Building, 3 Level")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 4 Level", "Stone Building, 4 Level");
            case "RowhouseBarsToOpenGround":
                return ChangeTerrain(context, rule, "Rowhouse Wall", "Open Ground")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 1 Level", "Open Ground")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 2 Level", "Open Ground")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 3 Level", "Open Ground")
                    && ChangeTerrain(context, rule, "Rowhouse Wall, 4 Level", "Open Ground");
            case "NoCliffs":
                return ChangeTerrain(context, rule, "Cliff", "Open Ground");
            case "Bamboo":
                pacific = true;
                return ChangeTerrain(context, rule, "Brush", "Bamboo");
            case "PalmTrees":
                pacific = true;
                return ChangeTerrain(context, rule, "Orchard", "Palm Trees") && ChangeTerrain(context, rule, "Orchard, Out of Season", "Palm Trees");
            case "DenseJungle":
                pacific = true;
                return ChangeTerrain(context, rule, "Woods", "Dense Jungle");
            case "AllBuildingsLevel1" or "NoStairwells" or "NoBridge" or "BridgeToFord" or "RoadsToPaths" or "NoWoodsRoads" or "NoWoodsRoad"
                or "SwampToSwampPattern":
                return true;
            default:
                return Fail(context, "VASL-SSR-002", $"Unsupported custom code SSR: {rule.Name}.");
        }
    }

    // VASLBoard.applyColorSSRulestoHexGrid, run after the board's hexes are on the map and before they are reset.
    private static bool ApplyHexRules(BuildContext context, BoardPlacement placement)
    {
        foreach (var name in placement.Rules)
        {
            if (!context.Rules.TryGet(name, out var rule))
            {
                continue;
            }

            var applied = rule.Kind switch
            {
                LosSsRuleKind.CustomCode => HexCustomCode(context, rule),

                // resetHexTerrain on the grid as it stands, so later hex rules see reset hexes.
                LosSsRuleKind.TerrainToElevationMap => Pass(context),
                LosSsRuleKind.ElevationToTerrainMap => ElevationToTerrain(context, rule),
                LosSsRuleKind.TerrainToSelectElevationMap => TerrainToSelectElevation(context, rule),
                _ => true,
            };
            if (!applied)
            {
                return false;
            }
        }

        return true;
    }

    private static bool Pass(BuildContext context)
    {
        context.Map.Pass(context.Grid.Snapshot());
        return true;
    }

    private static bool HexCustomCode(BuildContext context, LosSsRule rule)
    {
        var map = context.Map;
        switch (rule.Name)
        {
            case "AllBuildingsLevel1":
                if (!AllBuildingsSingleStoryGrid(context, rule))
                {
                    return false;
                }

                foreach (var hex in map.Geometry.Hexes())
                {
                    map.MakeSingleStory(hex);
                }

                return true;
            case "NoStairwells":
                foreach (var hex in map.Geometry.Hexes())
                {
                    map.SetStairway(hex, false);
                }

                return true;
            case "NoBridge" or "BridgeToFord":
                return BridgesToFords(context, rule);
            case "RoadsToPaths" or "NoWoodsRoads" or "NoWoodsRoad":
                return FillWoodsRoadHexes(context, rule);
            case "NoCliffs":
                return RemoveCliffTerrain(context);
            case "Bamboo":
                return FillHexesWithCenterTerrain(context, rule, "Brush", "Bamboo");
            case "SwampToSwampPattern":
                return SwampPattern(context, rule);
            case "DenseJungle":
                return FillHexesWithCenterTerrain(context, rule, "Woods", "Dense Jungle") && ChangeTerrain(context, rule, "Woods", "Dense Jungle");
            case "RowhouseBarsToBuildings" or "RowhouseBarsToOpenGround" or "PalmTrees":
                return true;
            default:
                return Fail(context, "VASL-SSR-002", $"Unsupported custom code SSR: {rule.Name}.");
        }
    }

    // VASLBoard.changeGridTerrain. Map.getTerrain returns null for an unknown name: an unknown from terrain matches no
    // cell, and an unknown to terrain throws, disabling LOS, only if some cell matches.
    private static bool ChangeTerrain(BuildContext context, LosSsRule? rule, string from, string to)
    {
        if (!context.Catalog.TryGet(from, out var fromTerrain))
        {
            return true;
        }

        context.Catalog.TryGet(to, out var toTerrain);
        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.CodeAt(x, y) == fromTerrain.Code)
                {
                    if (toTerrain is null)
                    {
                        return Fail(context, "VASL-SSR-001", $"SSR {rule?.Name ?? "Pacific"} names terrain '{to}', which is not in the catalog.");
                    }

                    grid.SetCode(x, y, toTerrain.Code);
                }
            }
        }

        return true;
    }

    private static bool ElevationMap(BuildContext context, LosSsRule rule)
    {
        if (!TryInt(rule.FromValue, out var from) || !TryInt(rule.ToValue, out var to))
        {
            return Fail(context, "VASL-SSR-001", $"Invalid from or to value in SSR elevation map {rule.Name}.");
        }

        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.ElevationAt(x, y) == from)
                {
                    grid.SetElevation(x, y, to);
                }
            }
        }

        return true;
    }

    // VASLBoard.applyTerrainToElevationMapRule. Its "GrainTo" repair for stray open-ground cells never fires, because
    // isPixelSurroundedByTerrainType assigns rather than adds ("=+1") and so never counts past one.
    private static bool TerrainToElevation(BuildContext context, LosSsRule rule)
    {
        if (!TryInt(rule.ToValue, out var to) || !context.Catalog.TryGet("Open Ground", out var open))
        {
            return Fail(context, "VASL-SSR-001", $"Invalid from or to value in SSR terrain to elevation map {rule.Name}.");
        }

        if (!context.Catalog.TryGet(rule.FromValue, out var from))
        {
            return true;
        }

        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.CodeAt(x, y) == from.Code)
                {
                    grid.SetElevation(x, y, to);
                    grid.SetCode(x, y, open.Code);
                }
            }
        }

        return true;
    }

    private static bool ElevationToTerrain(BuildContext context, LosSsRule rule)
    {
        if (!TryInt(rule.FromValue, out var from))
        {
            return Fail(context, "VASL-SSR-001", $"Invalid from or to value in SSR elevation to terrain map {rule.Name}.");
        }

        context.Catalog.TryGet(rule.ToValue, out var to);
        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.ElevationAt(x, y) == from)
                {
                    grid.SetElevation(x, y, 0);
                    if (!context.Catalog[grid.CodeAt(x, y)].IsLosObstacle)
                    {
                        if (to is null)
                        {
                            return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} names terrain '{rule.ToValue}', which is not in the catalog.");
                        }

                        grid.SetCode(x, y, to.Code);
                    }
                }
            }
        }

        return true;
    }

    private static bool TerrainToSelectElevation(BuildContext context, LosSsRule rule)
    {
        if (!TryInt(rule.ToValue, out var elevation) || !context.Catalog.TryGet("Open Ground", out var open))
        {
            return Fail(context, "VASL-SSR-001", $"Invalid from or to value in SSR terrain to elevation map {rule.Name}.");
        }

        if (!context.Catalog.TryGet(rule.FromValue, out var from))
        {
            return true;
        }

        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (grid.CodeAt(x, y) == from.Code && grid.ElevationAt(x, y) == elevation)
                {
                    grid.SetCode(x, y, open.Code);
                }
            }
        }

        return true;
    }

    // VASLBoard.changeGridTerrainforAllBuildings: first matching row wins; rows marked true also set the center terrain
    // of the hex VASL finds for the cell.
    private static readonly (string From, string To, bool Center)[] SingleStoryBuildings =
    [
        ("Stone Building, 1 Level", "Stone Building", true),
        ("Stone Building, 2 Level", "Stone Building", true),
        ("Stone Building, 3 Level", "Stone Building", true),
        ("Stone Building, 4 Level", "Stone Building", true),
        ("Stone Factory, 1.5 Level", "Wooden Factory, 1 Level", true),
        ("Stone Factory, 2.5 Level", "Wooden Factory, 1 Level", true),
        ("Stone Factory Wall, 1.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Stone Factory Wall, 2.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Stone Marketplace", "Stone Building", true),
        ("Rowhouse Wall, 1 Level", "Rowhouse Wall", false),
        ("Rowhouse Wall, 2 Level", "Rowhouse Wall", false),
        ("Rowhouse Wall, 3 Level", "Rowhouse Wall", false),
        ("Rowhouse Wall, 4 Level", "Rowhouse Wall", false),
        (" Roofless Stone Factory, 1.5 Level", "Wooden Factory, 1 Level", true),
        (" Roofless Stone Factory, 2.5 Level", "Wooden Factory, 1 Level", true),
        ("Interior Factory Wall, 1.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Interior Factory Wall, 2.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Gutted Stone Factory, 1.5 Level", "Wooden Factory, 1 Level", true),
        ("Gutted Factory Wall, 2.5 Level", "Wooden Factory, 1 Level", true),
        ("Gutted Stone Building, 1 Level", "Gutted Stone Building", true),
        ("Gutted Stone Building, 2 Level", "Gutted Stone Building", true),
        ("Gutted Stone Building, 3 Level", "Gutted Stone Building", true),
        ("Gutted Stone Building, 4 Level", "Gutted Stone Building", true),
        ("Wooden Building, 1 Level", "Wooden Building", true),
        ("Wooden Building, 2 Level", "Wooden Building", true),
        ("Wooden Building, 3 Level", "Wooden Building", true),
        ("Wooden Building, 4 Level", "Wooden Building", true),
        ("Wooden Factory, 1.5 Level", "Wooden Factory, 1 Level", true),
        ("Wooden Factory, 2.5 Level", "Wooden Factory, 1 Level", true),
        ("Wooden Factory Wall, 1.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Wooden Factory Wall, 2.5 Level", "Wooden Factory Wall, 1 Level", false),
        ("Wooden Marketplace", "Wooden Building", true),
        ("Tower, 2 Level Hindrance", "Tower Hindrance", false),
        ("Tower, 3 Level Hindrance", "Tower Hindrance", false),
        ("Tower, 2 Level Obstacle", "Tower Obstacle", false),
        ("Tower, 3 Level Obstacle", "Tower Obstacle", false),
        ("Storage Tank, 2 Level", "Storage Tank", false),
        ("BFP Tower, 1 Level", "Tower Obstacle", false),
        ("BFP Tower, 2 Level", "Tower Obstacle", false),
    ];

    private static bool AllBuildingsSingleStoryGrid(BuildContext context, LosSsRule rule)
    {
        var catalog = context.Catalog;
        var table = new (TerrainType To, bool Center)?[256];
        foreach (var (from, to, center) in SingleStoryBuildings)
        {
            if (!catalog.TryGet(from, out var fromTerrain) || table[fromTerrain.Code] is not null)
            {
                continue;
            }

            if (!catalog.TryGet(to, out var toTerrain))
            {
                return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} names terrain '{to}', which is not in the catalog.");
            }

            table[fromTerrain.Code] = (toTerrain, center);
        }

        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (table[grid.CodeAt(x, y)] is not { } change)
                {
                    continue;
                }

                grid.SetCode(x, y, change.To.Code);
                if (change.Center)
                {
                    if (context.Locator.GridToHex(x, y) is not { } hex)
                    {
                        return Fail(context, "VASL-SSR-003", $"SSR {rule.Name}: VASL finds no hex for cell ({x}, {y}).");
                    }

                    context.Map.SetCenterTerrain(hex, change.To);
                }
            }
        }

        return true;
    }

    // VASLBoard.bridgesToFord: bridge cells become gully, and a disc around each bridge hex's center drops one level
    // below the base level of the hex VASL finds for each cell.
    private static bool BridgesToFords(BuildContext context, LosSsRule rule)
    {
        if (!context.Catalog.TryGet("Gully", out var gully))
        {
            return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} needs Gully terrain.");
        }

        var grid = context.Grid;
        var bridgeHexes = new HashSet<HexIndex>();
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (context.Catalog[grid.CodeAt(x, y)].IsBridge)
                {
                    grid.SetCode(x, y, gully.Code);
                    if (context.Locator.GridToHex(x, y) is not { } hex)
                    {
                        return Fail(context, "VASL-SSR-003", $"SSR {rule.Name}: VASL finds no hex for cell ({x}, {y}).");
                    }

                    bridgeHexes.Add(hex);
                }
            }
        }

        var hexHeight = grid.Geometry.HexHeight;
        foreach (var hex in bridgeHexes)
        {
            var center = grid.Geometry.CenterPoint(hex);
            var disc = new JavaEllipse(center.X - (hexHeight / 3), center.Y - (hexHeight / 3), hexHeight * 2 / 3, hexHeight * 2 / 3);
            var (left, top, width, height) = disc.Bounds;

            // LOSDataEditor.setGridGroundLevel includes the far edge of the bounds.
            for (var x = Math.Max(left, 0); x <= Math.Min(left + width, grid.Width - 1); x++)
            {
                for (var y = Math.Max(top, 0); y <= Math.Min(top + height, grid.Height - 1); y++)
                {
                    if (!disc.Contains(x, y))
                    {
                        continue;
                    }

                    if (context.Locator.GridToHex(x, y) is not { } cellHex)
                    {
                        return Fail(context, "VASL-SSR-003", $"SSR {rule.Name}: VASL finds no hex for cell ({x}, {y}).");
                    }

                    var baseLevel = context.Map.BaseLevel(cellHex);
                    grid.SetElevation(x, y, context.Map.IsDepression(cellHex) ? baseLevel : baseLevel - 1);
                }
            }
        }

        return true;
    }

    // VASLBoard.fillWoodsRoadHexes: in hexes whose center is road and one of whose hexside locations is woods, as the
    // hexes stand at this point, a disc of woods is set at the center.
    private static bool FillWoodsRoadHexes(BuildContext context, LosSsRule rule)
    {
        if (!context.Catalog.TryGet("Woods", out var woods))
        {
            return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} needs Woods terrain.");
        }

        var map = context.Map;
        var hexHeight = context.Grid.Geometry.HexHeight;
        foreach (var hex in map.Geometry.Hexes())
        {
            var isForestRoad = map.CenterTerrain(hex) is { IsRoad: true }
                && HexsideDirections.All.Any(side => map.HexsideLocationTerrain(hex, side)?.Name == "Woods");
            if (isForestRoad)
            {
                var center = map.Geometry.CenterPoint(hex);
                var disc = new JavaEllipse(center.X - (hexHeight / 3), center.Y - (hexHeight / 3), hexHeight * 2 / 3, hexHeight * 2 / 3);
                var (left, top, width, height) = disc.Bounds;
                SetGridTerrain(context, left, top, width, height, disc.Contains, woods);
            }
        }

        return true;
    }

    // VASLBoard.removeCliffTerrain after the grid rule has already turned cliffs into open ground. VASL's code for any
    // remaining cliff cells depends on hex state this model does not reproduce, so such a board is refused.
    private static bool RemoveCliffTerrain(BuildContext context)
    {
        var grid = context.Grid;
        for (var x = 0; x < grid.Width; x++)
        {
            for (var y = 0; y < grid.Height; y++)
            {
                if (context.Catalog[grid.CodeAt(x, y)].IsCliff)
                {
                    return Fail(context, "VASL-SSR-003", $"SSR NoCliffs: cliff terrain '{context.Catalog[grid.CodeAt(x, y)].Name}' remains at ({x}, {y}).");
                }
            }
        }

        return true;
    }

    // The hex-phase Bamboo and DenseJungle code: every hex whose center currently has the terrain is filled with the
    // new terrain inside its border polygon.
    private static bool FillHexesWithCenterTerrain(BuildContext context, LosSsRule rule, string centerName, string terrainName)
    {
        if (!context.Catalog.TryGet(terrainName, out var terrain))
        {
            return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} needs {terrainName} terrain.");
        }

        var map = context.Map;
        foreach (var hex in map.Geometry.Hexes())
        {
            if (map.CenterTerrain(hex)?.Name == centerName)
            {
                var border = new JavaPolygon(map.Geometry.Border(hex));
                SetGridTerrain(context, border.MinX, border.MinY, border.Width, border.Height, border.Contains, terrain);
            }
        }

        return true;
    }

    // VASLBoard SwampToSwampPattern: a hex whose near-center cell is marsh, next to a hex whose near-center cell is
    // woods or jungle, has its marsh cells inside the border turned to swamp.
    private static bool SwampPattern(BuildContext context, LosSsRule rule)
    {
        if (!context.Catalog.TryGet("Marsh", out var marsh) || !context.Catalog.TryGet("Swamp", out var swamp))
        {
            return Fail(context, "VASL-SSR-001", $"SSR {rule.Name} needs Marsh and Swamp terrain.");
        }

        var geometry = context.Map.Geometry;
        foreach (var hex in geometry.Hexes())
        {
            if (NearCenterTerrain(context, hex)?.Name != "Marsh")
            {
                continue;
            }

            var apply = HexsideDirections.All.Any(side => geometry.Neighbor(hex, side) is { } neighbor
                && NearCenterTerrain(context, neighbor)?.Name is "Woods" or "Light Jungle" or "Dense Jungle");
            if (!apply)
            {
                continue;
            }

            var border = new JavaPolygon(geometry.Border(hex));
            var grid = context.Grid;
            for (var x = 0; x < grid.Width; x++)
            {
                for (var y = 0; y < grid.Height; y++)
                {
                    if (grid.CodeAt(x, y) == marsh.Code && border.Contains(x, y))
                    {
                        grid.SetCode(x, y, swamp.Code);
                    }
                }
            }
        }

        return true;
    }

    // Hex.getnearcenterLocationTerrain
    private static TerrainType? NearCenterTerrain(BuildContext context, HexIndex hex)
    {
        var center = context.Map.Geometry.CenterPoint(hex);
        var grid = context.Grid;
        (int X, int Y)[] probes = [(center.X + 1, center.Y - 1), (center.X + 1, center.Y + 1), (center.X - 1, center.Y + 1), (center.X - 1, center.Y - 1)];
        foreach (var (x, y) in probes)
        {
            if (grid.Contains(x, y))
            {
                return context.Catalog.TryGet(grid.CodeAt(x, y), out var terrain) ? terrain : null;
            }
        }

        return grid.Contains(center.X, center.Y) && context.Catalog.TryGet(grid.CodeAt(center.X, center.Y), out var own) ? own : null;
    }

    // LOSDataEditor.setGridTerrain for the terrains the SSR code uses: dense jungle does not replace water.
    private static void SetGridTerrain(BuildContext context, int left, int top, int width, int height, Func<double, double, bool> contains, TerrainType terrain)
    {
        var grid = context.Grid;
        for (var x = Math.Max(left, 0); x < Math.Min(left + width, grid.Width); x++)
        {
            for (var y = Math.Max(top, 0); y < Math.Min(top + height, grid.Height); y++)
            {
                if (!contains(x, y))
                {
                    continue;
                }

                if (terrain.Name == "Dense Jungle" && context.Catalog[grid.CodeAt(x, y)].IsWater)
                {
                    continue;
                }

                grid.SetCode(x, y, terrain.Code);
            }
        }
    }

    private static bool TryInt(string text, out int value) =>
        int.TryParse(text, System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out value);

    private static bool Fail(BuildContext context, string code, string message)
    {
        context.Diagnostics.Add(Error(code, message));
        return false;
    }

    private static MapDiagnostic Error(string code, string message) => new(code, MapDiagnosticSeverity.Error, message);
}
