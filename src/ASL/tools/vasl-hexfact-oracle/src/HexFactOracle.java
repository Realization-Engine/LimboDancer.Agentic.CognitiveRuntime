import java.awt.Point;
import java.awt.geom.Point2D;
import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.ObjectInputStream;
import java.io.PrintStream;
import java.lang.reflect.Field;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import java.util.zip.GZIPInputStream;

import VASL.LOS.Map.Hex;
import VASL.LOS.Map.LOSResult;
import VASL.LOS.Map.Location;
import VASL.LOS.Map.Map;
import VASL.LOS.Map.Terrain;
import VASL.LOS.VASLGameInterface;
import VASL.build.module.ScenInfo;
import VASL.build.module.map.boardArchive.BoardArchive;
import VASL.build.module.map.boardArchive.BoardMetadata;
import VASL.build.module.map.boardArchive.SharedBoardMetadata;
import VASL.build.module.map.boardPicker.ASLBoard;
import VASL.build.module.map.boardPicker.VASLBoard;

/**
 * F2 oracle (VASL Board Ingestion Design, sections 9.2 and 11): derives hex facts with VASL's own Map, Hex, Terrain,
 * and VASLBoard classes and writes them as JSON.
 *
 * It reproduces ASLMap.buildVASLMap, ASLMap.addBoardsToMap, and BoardArchive.addLOSDatatoVASLMap for uncropped boards
 * that VASL lays out as standard geomorphic boards (33 by 10 boards, a/b half boards, BFP double-width and Deluxe
 * boards), placed in board slots, optionally reversed, with scenario-specific terrain rules applied by VASL's own
 * VASLBoard methods. Overlays are not reproduced: VASL derives their terrain from overlay artwork (ASL-MAP-065).
 *
 * Usage:
 *   HexFactOracle vaslRoot outputDirectory boardName...                     writes bdNN.hexfacts.json
 *   HexFactOracle vaslRoot outputDirectory --scenarios file [name...]      writes name.scenario.hexfacts.json
 *   HexFactOracle vaslRoot outputDirectory --los boardName...              writes bdNN.los.json
 *   HexFactOracle vaslRoot outputDirectory --los --scenarios file [name...] writes name.scenario.los.json
 *
 * The LOS mode (LOS Design, section 4) runs VASL's own Map.LOS from sampled observer locations to every location
 * within LOS_RANGE, with no counters and not at night, and writes the results only.
 *
 * A scenario line is "name: placement placement ...", where a placement is board@column,row, optionally followed by
 * /r for a reversed board and [Rule,Rule] for LOS scenario-specific rules in the order VASL applies them.
 */
public final class HexFactOracle {
    static final String HARNESS_VERSION = "1.1.0";
    static final String GRID_CONFIGURATION = "HalfHexWidthLeftHexFullHeight";
    static final String LOS_HARNESS_VERSION = "1.0.0";
    static final int LOS_RANGE = 12;
    static final int OBSERVER_COLUMN_STRIDE = 5;
    static final int OBSERVER_ROW_STRIDE = 3;
    static final Pattern PLACEMENT = Pattern.compile("([A-Za-z0-9]+)@(\\d+),(\\d+)(/r)?(?:\\[([A-Za-z0-9_.,]*)\\])?");

    private HexFactOracle() {
    }

    record Placement(String board, int column, int row, boolean reversed, List<String> rules) {
    }

    record Scenario(String name, List<Placement> placements) {
    }

    public static void main(String[] args) throws Exception {
        if (args.length < 3) {
            System.err.println("Usage: HexFactOracle <vaslRoot> <outputDirectory> (<boardName>... | --scenarios <file> [<name>...])");
            System.exit(2);
        }

        Path root = Path.of(args[0]);
        Path output = Path.of(args[1]);
        Files.createDirectories(output);
        String commit = headCommit(root);
        int failures = 0;
        boolean los = args[2].equals("--los");
        int first = los ? 3 : 2;
        if (los) {
            stubGameModule();
        }

        if (args.length > first && args[first].equals("--scenarios")) {
            List<String> names = List.of(args).subList(first + 2, args.length);
            for (Scenario scenario : readScenarios(Path.of(args[first + 1]))) {
                if (!names.isEmpty() && !names.contains(scenario.name())) {
                    continue;
                }

                failures += los
                        ? write(output.resolve(scenario.name() + ".scenario.los.json"), scenario.name(),
                                out -> writeLos(out, root, commit, scenario, true))
                        : write(output.resolve(scenario.name() + ".scenario.hexfacts.json"), scenario.name(),
                                out -> writeScenario(out, root, commit, scenario));
            }
        } else {
            for (int index = first; index < args.length; index++) {
                String board = args[index];
                Scenario single = new Scenario("bd" + board, List.of(new Placement(board, 0, 0, false, List.of())));
                failures += los
                        ? write(output.resolve("bd" + board + ".los.json"), "bd" + board, out -> writeLos(out, root, commit, single, false))
                        : write(output.resolve("bd" + board + ".hexfacts.json"), "bd" + board, out -> writeBoard(out, root, commit, single));
            }
        }

        System.exit(failures == 0 ? 0 : 1);
    }

    interface Writer {
        void write(PrintStream out) throws Exception;
    }

    private static int write(Path path, String label, Writer writer) {
        try (PrintStream out = new PrintStream(Files.newOutputStream(path), false, StandardCharsets.UTF_8)) {
            writer.write(out);
            return 0;
        } catch (Exception exception) {
            System.err.println(label + ": " + exception);
            exception.printStackTrace();
            return 1;
        }
    }

    static List<Scenario> readScenarios(Path file) throws IOException {
        List<Scenario> scenarios = new ArrayList<>();
        for (String raw : Files.readAllLines(file, StandardCharsets.UTF_8)) {
            String line = raw.strip();
            if (line.isEmpty() || line.startsWith("#")) {
                continue;
            }

            int colon = line.indexOf(':');
            String name = line.substring(0, colon).strip();
            List<Placement> placements = new ArrayList<>();
            for (String token : line.substring(colon + 1).strip().split("\\s+")) {
                Matcher matcher = PLACEMENT.matcher(token);
                if (!matcher.matches()) {
                    throw new IllegalArgumentException("Bad placement '" + token + "' in scenario " + name);
                }

                List<String> rules = matcher.group(5) == null || matcher.group(5).isEmpty()
                        ? List.of() : List.of(matcher.group(5).split(","));
                placements.add(new Placement(matcher.group(1), Integer.parseInt(matcher.group(2)),
                        Integer.parseInt(matcher.group(3)), matcher.group(4) != null, rules));
            }

            scenarios.add(new Scenario(name, placements));
        }

        return scenarios;
    }

    /** One board of a scenario: its sources, geometry, LOSData header, and position on the map. */
    static final class PlacedBoard {
        Placement placement;
        Path directory;
        Path metadataPath;
        Path losDataPath;
        BoardMetadata metadata;
        double hexWidth;
        double hexHeight;
        double a1CenterX;
        double a1CenterY;
        int width;
        int height;
        int gridWidth;
        int gridHeight;
        int x;
        int y;
    }

    private static Map build(Path root, Scenario scenario, SharedBoardMetadata shared, List<PlacedBoard> boards) throws Exception {
        for (Placement placement : scenario.placements()) {
            PlacedBoard board = new PlacedBoard();
            board.placement = placement;
            board.directory = root.resolve("boards/src/bd" + placement.board());
            board.metadataPath = board.directory.resolve("BoardMetadata.xml");
            board.losDataPath = board.directory.resolve("LOSData");
            board.metadata = new BoardMetadata(shared);
            try (InputStream in = new FileInputStream(board.metadataPath.toFile())) {
                board.metadata.parseBoardMetadataFile(in);
            }

            // BoardArchive getters: a missing value falls back to the standard geomorphic geometry.
            int missing = BoardArchive.missingValue();
            board.hexWidth = board.metadata.getHexWidth() == missing ? BoardArchive.GEO_HEX_WIDTH : board.metadata.getHexWidth();
            board.hexHeight = board.metadata.getHexHeight() == missing ? BoardArchive.GEO_HEX_HEIGHT : board.metadata.getHexHeight();
            board.a1CenterX = board.metadata.getA1CenterX() == missing ? BoardArchive.GEO_A1_Center.x : board.metadata.getA1CenterX();
            board.a1CenterY = board.metadata.getA1CenterY() == missing ? BoardArchive.GEO_A1_Center.y : board.metadata.getA1CenterY();
            try (ObjectInputStream infile = open(board.losDataPath)) {
                board.width = infile.readInt();
                board.height = infile.readInt();
                board.gridWidth = infile.readInt();
                board.gridHeight = infile.readInt();
            }

            boards.add(board);
        }

        // Boards abut in slots: a board's pixel position is the sum of the grid sizes before it in its row and column.
        for (PlacedBoard board : boards) {
            for (PlacedBoard other : boards) {
                if (other.placement.row() == board.placement.row() && other.placement.column() < board.placement.column()) {
                    board.x += other.gridWidth;
                }
            }

            for (int row = 0; row < board.placement.row(); row++) {
                final int current = row;
                board.y += boards.stream().filter(other -> other.placement.row() == current).findFirst().orElseThrow().gridHeight;
            }
        }

        // ASLMap.buildVASLMap for uncropped boards: the geometry comes from the first board, A1 is full height.
        PlacedBoard first = boards.get(0);
        for (PlacedBoard board : boards) {
            if (Math.round(board.hexHeight) != Math.round(first.hexHeight) || Math.round(board.hexWidth) != Math.round(first.hexWidth)) {
                throw new IllegalStateException("multiple hex sizes");
            }
        }

        int mapGridWidth = boards.stream().mapToInt(board -> board.x + board.gridWidth).max().orElseThrow();
        int mapGridHeight = boards.stream().mapToInt(board -> board.y + board.gridHeight).max().orElseThrow();

        // The width loop compares board bounds, which include the map's edge buffer, with a running sum of them.
        final int edgeBuffer = 1;
        int widthInHexes = 0;
        int previousX = 0;
        for (PlacedBoard board : boards) {
            int boundsX = edgeBuffer + board.x;
            if (boundsX > previousX) {
                widthInHexes += previousX == 0 ? board.width : board.width - 1;
                previousX += boundsX;
            }
        }

        int heightInHexes = (int) Math.round(mapGridHeight / first.hexHeight);
        return new Map(first.hexWidth, first.hexHeight, widthInHexes, heightInHexes, 0.0, first.hexHeight / 2.0,
                mapGridWidth, mapGridHeight, shared.getTerrainTypes(), GRID_CONFIGURATION, GRID_CONFIGURATION, false);
    }

    private static Map derive(Path root, Scenario scenario, SharedBoardMetadata shared, List<PlacedBoard> boards) throws Exception {
        Map map = build(root, scenario, shared, boards);
        for (PlacedBoard board : boards) {
            addBoard(root, shared, map, board);
        }

        // ASLMap.addBoardsToMap
        map.resetHexTerrain();
        return map;
    }

    // BoardArchive.addLOSDatatoVASLMap for an uncropped board, without overlays.
    private static void addBoard(Path root, SharedBoardMetadata shared, Map map, PlacedBoard board) throws Exception {
        HashMap<String, Terrain> terrainTypes = shared.getTerrainTypes();
        VASLBoard vaslBoard = board.placement.rules().isEmpty() ? null : vaslBoard(root, shared, board);
        boolean isbboard = board.a1CenterX == -901;
        boolean isdwboard = board.a1CenterY == -612.75;
        try (ObjectInputStream infile = open(board.losDataPath)) {
            int width = infile.readInt();
            int height = infile.readInt();
            int gridWidth = infile.readInt();
            int gridHeight = infile.readInt();
            int boardposx = board.x;
            int boardposy = board.y;
            Hex boardstarthex = map.gridToHex(boardposx, boardposy);
            int mapcol = boardstarthex.getColumnNumber();
            int maprow = boardstarthex.getRowNumber();

            for (int x = 0; x < gridWidth; x++) {
                for (int y = 0; y < gridHeight; y++) {
                    map.setGridElevation((int) infile.readByte(), x + boardposx, y + boardposy);
                    int terrainint = infile.readByte() & 255;
                    if (boardposx > 0 && x == 0) {
                        Terrain firsthalfhex = map.getGridTerrain(boardposx - 1, y + boardposy);
                        Terrain secondhalfhex = map.getTerrain(terrainint);
                        if (!firsthalfhex.isOpen() && secondhalfhex.isOpen()) {
                            terrainint = map.getGridTerrainCode(boardposx - 1, y + boardposy);
                        }
                    }

                    if (boardposy > 0 && y == 0) {
                        Terrain firsthalfhex = map.getGridTerrain(x + boardposx, y + boardposy - 1);
                        Terrain secondhalfhex = map.getTerrain(terrainint);
                        if (!firsthalfhex.isOpen() && secondhalfhex.isOpen()) {
                            terrainint = firsthalfhex.getType();
                        } else if (firsthalfhex.isOpen() && !secondhalfhex.isOpen()) {
                            terrainint = secondhalfhex.getType();
                        }
                    }

                    map.setGridTerrainCode(terrainint, x + boardposx, y + boardposy);
                }
            }

            if (vaslBoard != null) {
                vaslBoard.applyColorSSRulestoTerrainElevationGrids(map, shared.getLOSSSRules());
            }

            if (board.placement.reversed()) {
                flipTerrainAndElevationGrids(map, boardposx, boardposy, gridWidth, gridHeight);
                Hex[][] flippedgrid = new Hex[width][];
                boolean isLongerCol = false;
                Point2D.Double flipcenterpoint = new Point2D.Double();
                for (int col = 0; col < width; col++) {
                    int heightadj = isLongerCol ? 1 : 0;
                    isLongerCol = !isLongerCol;
                    flippedgrid[col] = new Hex[height + heightadj];
                    for (int row = 0; row < height + heightadj; row++) {
                        flipcenterpoint.y = heightadj == 1 ? (board.hexHeight * row) : ((board.hexHeight * row) + (board.hexHeight / 2));
                        // The 28.25 offset VASL adds for boards cropped to the nearest full row never applies uncropped.
                        flipcenterpoint.x = col == 0 ? 0 : (col * board.hexWidth - 1);

                        flippedgrid[col][row] = new Hex(col, row, map.getGEOHexName(col, row, false, isdwboard),
                                flipcenterpoint, board.hexHeight, board.hexWidth, map, 0, terrainTypes.get(0));
                        byte stairway = infile.readByte();
                        flippedgrid[col][row].setStairway((int) stairway == 1);
                        flippedgrid[col][row].resetHexAndLocationNames(map.getGEOHexName(col, row, isbboard, isdwboard));
                    }
                }

                for (int col = 0; col < width; col++) {
                    for (int row = 0; row < flippedgrid[width - col - 1].length; row++) {
                        Hex flippedHex = flippedgrid[width - col - 1][flippedgrid[width - col - 1].length - row - 1];
                        map.flipthehex(flippedHex, true, map.getHexGrid()[mapcol + col][maprow + row]);
                        map.getHexGrid()[mapcol + col][maprow + row].copy(flippedHex);
                        map.getHexGrid()[mapcol + col][maprow + row].resetTerrain();
                    }
                }
            } else {
                for (int col = 0; col < width; col++) {
                    int heightadj = col % 2 == 1 ? 1 : 0;
                    for (int row = 0; row < height + heightadj; row++) {
                        byte stairway = infile.readByte();
                        Hex hex = map.getHex(mapcol + col, maprow + row);
                        hex.setStairway((int) stairway == 1);
                        hex.resetHexAndLocationNames(map.getGEOHexName(col, row, isbboard, isdwboard));
                    }
                }
            }
        }

        if (vaslBoard != null) {
            vaslBoard.applyColorSSRulestoHexGrid(map, shared.getLOSSSRules());
        }

        map.setRBrrembankments(board.metadata.getRBrrembankments());
        map.setPartialOrchards(board.metadata.getPartialOrchards());
        map.setSlopes(board.metadata.getSlopes());
        for (int col = 0; col < map.getHexGrid().length; col++) {
            for (int row = 0; row < map.getHexGrid()[col].length; row++) {
                map.getHexGrid()[col][row].resetHexsideLocationNames();
            }
        }

        map.resetHexTerrain();
    }

    // Map.flipTerrainAndElevationGrids: rotates the board's rectangle of the grids by 180 degrees.
    private static void flipTerrainAndElevationGrids(Map map, int boardposx, int boardposy, int boardwidth, int boardheight) {
        int[][] terrain = new int[boardwidth][boardheight];
        int[][] elevation = new int[boardwidth][boardheight];
        for (int x = 0; x < boardwidth; x++) {
            for (int y = 0; y < boardheight; y++) {
                terrain[x][y] = map.getGridTerrainCode(boardposx + boardwidth - x - 1, boardposy + boardheight - y - 1);
                elevation[x][y] = map.getGridElevation(boardposx + boardwidth - x - 1, boardposy + boardheight - y - 1);
            }
        }

        for (int x = 0; x < boardwidth; x++) {
            for (int y = 0; y < boardheight; y++) {
                map.setGridTerrainCode(terrain[x][y], boardposx + x, boardposy + y);
                map.setGridElevation(elevation[x][y], boardposx + x, boardposy + y);
            }
        }
    }

    // A VASLBoard carrying only what the SSR methods read: the archive, the rule list, and the board name.
    private static VASLBoard vaslBoard(Path root, SharedBoardMetadata shared, PlacedBoard board) throws Exception {
        VASLBoard vaslBoard = new VASLBoard();
        BoardArchive archive = new BoardArchive("bd" + board.placement.board(), root.resolve("boards/bdFiles").toString(), shared);
        setField(vaslBoard, ASLBoard.class, "VASLBoardArchive", archive);
        setField(vaslBoard, ASLBoard.class, "terrainChanges", String.join("\t", board.placement.rules()));
        setField(vaslBoard, VASSAL.build.AbstractConfigurable.class, "name", board.placement.board());
        return vaslBoard;
    }

    // A null target sets a static field.
    private static void setField(Object target, Class<?> owner, String name, Object value) throws Exception {
        Field field = owner.getDeclaredField(name);
        field.setAccessible(true);
        field.set(target, value);
    }

    private static ObjectInputStream open(Path losData) throws IOException {
        return new ObjectInputStream(new BufferedInputStream(new GZIPInputStream(new FileInputStream(losData.toFile()))));
    }

    private static SharedBoardMetadata shared(Path root) throws Exception {
        SharedBoardMetadata shared = new SharedBoardMetadata();
        try (InputStream in = new FileInputStream(root.resolve("dist/boardData/SharedBoardMetadata.xml").toFile())) {
            shared.parseSharedBoardMetadataFile(in);
        }

        return shared;
    }

    private static void writeBoard(PrintStream out, Path root, String commit, Scenario scenario) throws Exception {
        SharedBoardMetadata shared = shared(root);
        List<PlacedBoard> boards = new ArrayList<>();
        Map map = derive(root, scenario, shared, boards);
        PlacedBoard board = boards.get(0);
        out.print("{\"board\":\"bd" + escape(board.placement.board()) + "\"");
        out.print(",\"gridConfiguration\":\"" + GRID_CONFIGURATION + "\"");
        out.print(",\"harnessVersion\":\"" + HARNESS_VERSION + "\"");
        writeHexes(out, map);
        out.print(",\"losDataBlob\":\"" + gitBlob(board.losDataPath) + "\"");
        out.print(",\"metadataBlob\":\"" + gitBlob(board.metadataPath) + "\"");
        out.print(",\"sharedBoardMetadataBlob\":\"" + gitBlob(root.resolve("dist/boardData/SharedBoardMetadata.xml")) + "\"");
        out.print(",\"vaslCommit\":\"" + escape(commit) + "\"");
        out.print("}\n");
    }

    private static void writeScenario(PrintStream out, Path root, String commit, Scenario scenario) throws Exception {
        SharedBoardMetadata shared = shared(root);
        List<PlacedBoard> boards = new ArrayList<>();
        Map map = derive(root, scenario, shared, boards);
        out.print("{\"boards\":[");
        for (int index = 0; index < boards.size(); index++) {
            PlacedBoard board = boards.get(index);
            out.print(index == 0 ? "\n" : ",\n");
            out.print("{\"board\":\"bd" + escape(board.placement.board()) + "\"");
            out.print(",\"column\":" + board.placement.column());
            out.print(",\"losDataBlob\":\"" + gitBlob(board.losDataPath) + "\"");
            out.print(",\"metadataBlob\":\"" + gitBlob(board.metadataPath) + "\"");
            out.print(",\"reversed\":" + board.placement.reversed());
            out.print(",\"row\":" + board.placement.row());
            out.print(",\"rules\":[");
            for (int rule = 0; rule < board.placement.rules().size(); rule++) {
                out.print((rule == 0 ? "\"" : ",\"") + escape(board.placement.rules().get(rule)) + "\"");
            }

            out.print("],\"x\":" + board.x + ",\"y\":" + board.y + "}");
        }

        out.print("\n]");
        out.print(",\"gridHeight\":" + map.getGridHeight());
        out.print(",\"gridWidth\":" + map.getGridWidth());
        out.print(",\"harnessVersion\":\"" + HARNESS_VERSION + "\"");
        out.print(",\"heightInHexes\":" + map.getHeight());
        writeHexes(out, map);
        out.print(",\"scenario\":\"" + escape(scenario.name()) + "\"");
        out.print(",\"sharedBoardMetadataBlob\":\"" + gitBlob(root.resolve("dist/boardData/SharedBoardMetadata.xml")) + "\"");
        out.print(",\"vaslCommit\":\"" + escape(commit) + "\"");
        out.print(",\"widthInHexes\":" + map.getWidth());
        out.print("}\n");
    }

    /**
     * Map.LOS asks the game module for the scenario information on every call (checkNVRRule), and ScenInfo opens a
     * window in its constructor. The stub module holds one ScenInfo made without its constructor, not at night, so
     * VASL's own night rule runs and never applies.
     */
    private static void stubGameModule() throws Exception {
        Field unsafeField = Class.forName("sun.misc.Unsafe").getDeclaredField("theUnsafe");
        unsafeField.setAccessible(true);
        Object unsafe = unsafeField.get(null);
        java.lang.reflect.Method allocate = unsafe.getClass().getMethod("allocateInstance", Class.class);
        ScenInfo info = (ScenInfo) allocate.invoke(unsafe, ScenInfo.class);
        setField(info, ScenInfo.class, "night", "No");
        VASSAL.build.GameModule module = (VASSAL.build.GameModule) allocate.invoke(unsafe, VASSAL.build.GameModule.class);
        List<VASSAL.build.Buildable> components = new ArrayList<>();
        components.add(info);
        setField(module, VASSAL.build.AbstractBuildable.class, "buildComponents", components);
        setField(null, VASSAL.build.GameModule.class, "theModule", module);
    }

    /** A placed board of the LOS map: its board name and the pixel rectangle it covers. */
    private static String ownerOf(Hex hex, List<PlacedBoard> boards) {
        String owner = null;
        Point2D center = hex.getHexCenter();
        for (PlacedBoard board : boards) {
            if (center.getX() >= board.x && center.getX() < board.x + board.gridWidth
                    && center.getY() >= board.y && center.getY() < board.y + board.gridHeight) {
                owner = "bd" + board.placement.board();
            }
        }

        return owner;
    }

    private static List<Location> chain(Hex hex) {
        List<Location> locations = new ArrayList<>();
        Location down = hex.getCenterLocation().getDownLocation();
        while (down != null && !locations.contains(down)) {
            locations.add(0, down);
            down = down.getDownLocation();
        }

        locations.add(hex.getCenterLocation());
        Location up = hex.getCenterLocation().getUpLocation();
        while (up != null && !locations.contains(up)) {
            locations.add(up);
            up = up.getUpLocation();
        }

        return locations;
    }

    private static String locationId(Location location, List<PlacedBoard> boards) {
        return ownerOf(location.getHex(), boards) + ":" + location.getHex().getName() + ":" + location.getLevelInHex();
    }

    /**
     * The LOS fixture of a board or scenario: VASL's Map.LOS from every location of each observer hex (every
     * OBSERVER_COLUMN_STRIDE-th column and OBSERVER_ROW_STRIDE-th row of the map) to every location within LOS_RANGE.
     * Locations are board-relative, named by the board that owns their hex.
     */
    private static void writeLos(PrintStream out, Path root, String commit, Scenario scenario, boolean composed) throws Exception {
        SharedBoardMetadata shared = shared(root);
        List<PlacedBoard> boards = new ArrayList<>();
        Map map = derive(root, scenario, shared, boards);
        VASLGameInterface game = new VASLGameInterface(null, null);
        out.print("{\"boards\":[");
        for (int index = 0; index < boards.size(); index++) {
            PlacedBoard board = boards.get(index);
            out.print(index == 0 ? "\n" : ",\n");
            out.print("{\"board\":\"bd" + escape(board.placement.board()) + "\"");
            out.print(",\"column\":" + board.placement.column());
            out.print(",\"losDataBlob\":\"" + gitBlob(board.losDataPath) + "\"");
            out.print(",\"metadataBlob\":\"" + gitBlob(board.metadataPath) + "\"");
            out.print(",\"reversed\":" + board.placement.reversed());
            out.print(",\"row\":" + board.placement.row() + "}");
        }

        out.print("\n]");
        out.print(",\"gridConfiguration\":\"" + GRID_CONFIGURATION + "\"");
        out.print(",\"harnessVersion\":\"" + LOS_HARNESS_VERSION + "\"");
        out.print(",\"losRange\":" + LOS_RANGE);
        out.print(",\"observerStride\":[" + OBSERVER_COLUMN_STRIDE + "," + OBSERVER_ROW_STRIDE + "]");
        out.print(",\"pairs\":[\n");
        Hex[][] grid = map.getHexGrid();
        boolean firstPair = true;
        for (int col = 0; col < grid.length; col += OBSERVER_COLUMN_STRIDE) {
            for (int row = 0; row < grid[col].length; row += OBSERVER_ROW_STRIDE) {
                Hex observer = grid[col][row];
                for (Location source : chain(observer)) {
                    for (Hex[] column : grid) {
                        for (Hex hex : column) {
                            if (Map.range(observer, hex, map.getMapConfiguration()) > LOS_RANGE) {
                                continue;
                            }

                            for (Location target : chain(hex)) {
                                if (target == source) {
                                    continue;
                                }

                                LOSResult result = new LOSResult();
                                map.LOS(source, false, target, false, result, game, null);
                                out.print(firstPair ? "" : ",\n");
                                firstPair = false;
                                writePair(out, map, boards, source, target, result);
                            }
                        }
                    }
                }
            }
        }

        out.print("\n]");
        out.print(",\"scenario\":" + (composed ? "\"" + escape(scenario.name()) + "\"" : "null"));
        out.print(",\"sharedBoardMetadataBlob\":\"" + gitBlob(root.resolve("dist/boardData/SharedBoardMetadata.xml")) + "\"");
        out.print(",\"vaslCommit\":\"" + escape(commit) + "\"");
        out.print("}\n");
    }

    private static void writePair(PrintStream out, Map map, List<PlacedBoard> boards, Location source, Location target, LOSResult result) {
        Point at = result.isBlocked() ? result.getBlockedAtPoint() : null;
        Hex atHex = at == null ? null : map.gridToHex(at.x, at.y);
        out.print("{\"blocked\":" + result.isBlocked());
        out.print(",\"blockedAt\":" + (at == null ? "null" : "[" + at.x + "," + at.y + "]"));
        out.print(",\"blockedHex\":" + (atHex == null ? "null" : "\"" + ownerOf(atHex, boards) + ":" + escape(atHex.getName()) + "\""));
        out.print(",\"hindrance\":" + result.getHindrance());
        out.print(",\"range\":" + result.getRange());
        out.print(",\"reason\":\"" + escape(result.getReason() == null ? "" : result.getReason()) + "\"");
        out.print(",\"source\":\"" + escape(locationId(source, boards)) + "\"");
        out.print(",\"target\":\"" + escape(locationId(target, boards)) + "\"}");
    }

    private static void writeHexes(PrintStream out, Map map) {
        out.print(",\"hexes\":[\n");
        boolean first = true;
        for (int col = 0; col < map.getHexGrid().length; col++) {
            for (int row = 0; row < map.getHexGrid()[col].length; row++) {
                out.print(first ? "" : ",\n");
                first = false;
                writeHex(out, map.getHexGrid()[col][row], col, row);
            }
        }

        out.print("\n]");
    }

    // Keys are written in ascending ordinal order so the output is canonical.
    private static void writeHex(PrintStream out, Hex hex, int col, int row) {
        out.print("{\"baseLevel\":" + hex.getBaseLevelofHex());
        out.print(",\"bridge\":");
        if (hex.hasBridge()) {
            out.print("{\"roadLevel\":" + hex.getBridge().getRoadLevel()
                    + ",\"terrain\":" + name(hex.getBridge().getTerrain()) + "}");
        } else {
            out.print("null");
        }

        out.print(",\"center\":" + location(hex.getCenterLocation()));
        out.print(",\"col\":" + col);
        out.print(",\"hex\":\"" + escape(hex.getName()) + "\"");
        out.print(",\"hexsides\":[");
        for (int side = 0; side < 6; side++) {
            Location location = hex.getHexsideLocation(side);
            out.print(side == 0 ? "" : ",");
            out.print("{\"cliff\":" + hex.hasCliff(side)
                    + ",\"depressionTerrain\":" + name(location.getDepressionTerrain())
                    + ",\"hexsideTerrain\":" + name(hex.getHexsideTerrain(side))
                    + ",\"onMap\":" + hex.isHexsideOnMap(side)
                    + ",\"partialOrchard\":" + hex.hasPartialOrchard(side)
                    + ",\"railroadEmbankment\":" + hex.hasRRembankment(side)
                    + ",\"side\":" + side
                    + ",\"slope\":" + hex.hasSlope(side)
                    + ",\"terrain\":" + name(location.getTerrain()) + "}");
        }

        out.print("],\"locations\":[");
        List<Location> locations = new ArrayList<>();
        Location down = hex.getCenterLocation().getDownLocation();
        while (down != null && !locations.contains(down)) {
            locations.add(0, down);
            down = down.getDownLocation();
        }

        locations.add(hex.getCenterLocation());
        Location up = hex.getCenterLocation().getUpLocation();
        while (up != null && !locations.contains(up)) {
            locations.add(up);
            up = up.getUpLocation();
        }

        for (int index = 0; index < locations.size(); index++) {
            out.print((index == 0 ? "" : ",") + location(locations.get(index)));
        }

        out.print("],\"row\":" + row);
        out.print(",\"stairway\":" + hex.hasStairway() + "}");
    }

    private static String location(Location location) {
        return "{\"depressionTerrain\":" + name(location.getDepressionTerrain())
                + ",\"level\":" + location.getLevelInHex()
                + ",\"terrain\":" + name(location.getTerrain()) + "}";
    }

    private static String name(Terrain terrain) {
        return terrain == null ? "null" : "\"" + escape(terrain.getName()) + "\"";
    }

    private static String escape(String value) {
        return value.replace("\\", "\\\\").replace("\"", "\\\"");
    }

    private static java.util.Map<String, String> indexBlobs;
    private static Path indexRoot;

    // The committed blob id from the git index (versions 2 and 3), which checkout line-ending conversion does not
    // change; the blob id of the file's bytes when the path is not in the index.
    private static String gitBlob(Path path) throws IOException {
        Path root = path;
        while (root != null && !Files.isDirectory(root.resolve(".git"))) {
            root = root.getParent();
        }

        if (root != null) {
            if (!root.equals(indexRoot)) {
                indexRoot = root;
                indexBlobs = readIndex(root.resolve(".git/index"));
            }

            String relative = root.relativize(path).toString().replace('\\', '/');
            String committed = indexBlobs.get(relative);
            if (committed != null) {
                return committed;
            }
        }

        byte[] content = Files.readAllBytes(path);
        try {
            MessageDigest sha1 = MessageDigest.getInstance("SHA-1");
            sha1.update(("blob " + content.length + "\0").getBytes(StandardCharsets.US_ASCII));
            sha1.update(content);
            StringBuilder hex = new StringBuilder();
            for (byte b : sha1.digest()) {
                hex.append(String.format("%02x", b));
            }

            return hex.toString();
        } catch (java.security.NoSuchAlgorithmException exception) {
            throw new IllegalStateException(exception);
        }
    }

    private static java.util.Map<String, String> readIndex(Path index) throws IOException {
        java.util.Map<String, String> blobs = new HashMap<>();
        if (!Files.exists(index)) {
            return blobs;
        }

        java.nio.ByteBuffer data = java.nio.ByteBuffer.wrap(Files.readAllBytes(index));
        if (data.limit() < 12 || data.getInt(0) != 0x44495243) {
            return blobs;
        }

        int version = data.getInt(4);
        if (version != 2 && version != 3) {
            return blobs;
        }

        int count = data.getInt(8);
        int position = 12;
        for (int entry = 0; entry < count && position + 62 <= data.limit(); entry++) {
            int start = position;
            StringBuilder sha = new StringBuilder();
            for (int offset = 40; offset < 60; offset++) {
                sha.append(String.format("%02x", data.get(position + offset)));
            }

            int flags = data.getShort(position + 60) & 0xFFFF;
            position += 62;
            if (version == 3 && (flags & 0x4000) != 0) {
                position += 2;
            }

            int end = position;
            while (end < data.limit() && data.get(end) != 0) {
                end++;
            }

            if (end >= data.limit()) {
                break;
            }

            byte[] pathBytes = new byte[end - position];
            data.get(position, pathBytes);
            if (((flags >> 12) & 0x3) == 0) {
                blobs.put(new String(pathBytes, StandardCharsets.UTF_8), sha.toString());
            }

            int entryLength = end - start + 1;
            position = start + ((entryLength + 7) / 8 * 8);
        }

        return blobs;
    }

    private static String headCommit(Path root) throws IOException {
        Path git = root.resolve(".git");
        String head = Files.readString(git.resolve("HEAD")).trim();
        if (!head.startsWith("ref: ")) {
            return head;
        }

        String reference = head.substring(5);
        Path loose = git.resolve(reference);
        if (Files.exists(loose)) {
            return Files.readString(loose).trim();
        }

        for (String line : Files.readAllLines(git.resolve("packed-refs"))) {
            if (line.endsWith(" " + reference)) {
                return line.substring(0, 40);
            }
        }

        return "unknown";
    }
}
