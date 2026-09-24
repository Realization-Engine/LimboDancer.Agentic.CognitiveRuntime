import java.io.BufferedInputStream;
import java.io.FileInputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.ObjectInputStream;
import java.io.PrintStream;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.MessageDigest;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.zip.GZIPInputStream;

import VASL.LOS.Map.Hex;
import VASL.LOS.Map.Location;
import VASL.LOS.Map.Map;
import VASL.LOS.Map.Terrain;
import VASL.build.module.map.boardArchive.BoardMetadata;
import VASL.build.module.map.boardArchive.SharedBoardMetadata;

/**
 * F2 oracle (VASL Board Ingestion Design, section 9.2): derives hex facts for one standard geomorphic board
 * with VASL's own Map, Hex, and Terrain classes and writes them as JSON.
 *
 * It reproduces BoardArchive.addLOSDatatoVASLMap for a single uncropped, unrotated board with no overlay or SSR,
 * under the runtime grid configuration HalfHexWidthLeftHexFullHeight, followed by the second resetHexTerrain
 * pass from ASLMap.addBoardsToMap.
 *
 * Usage: HexFactOracle vaslRoot outputDirectory boardName...  (writes outputDirectory/bdNN.hexfacts.json)
 */
public final class HexFactOracle {
    static final String HARNESS_VERSION = "1.0.0";
    static final String GRID_CONFIGURATION = "HalfHexWidthLeftHexFullHeight";

    private HexFactOracle() {
    }

    public static void main(String[] args) throws Exception {
        if (args.length < 3) {
            System.err.println("Usage: HexFactOracle <vaslRoot> <outputDirectory> <boardName>...");
            System.exit(2);
        }

        Path root = Path.of(args[0]);
        Path output = Path.of(args[1]);
        Files.createDirectories(output);
        Path sharedPath = root.resolve("dist/boardData/SharedBoardMetadata.xml");
        String commit = headCommit(root);
        int failures = 0;
        for (int index = 2; index < args.length; index++) {
            String board = args[index];
            try (PrintStream out = new PrintStream(Files.newOutputStream(output.resolve("bd" + board + ".hexfacts.json")),
                    false, StandardCharsets.UTF_8)) {
                writeBoard(out, root, sharedPath, commit, board);
            } catch (Exception exception) {
                failures++;
                System.err.println("bd" + board + ": " + exception);
            }
        }

        System.exit(failures == 0 ? 0 : 1);
    }

    private static void writeBoard(PrintStream out, Path root, Path sharedPath, String commit, String board) throws Exception {
        Path boardDirectory = root.resolve("boards/src/bd" + board);
        Path metadataPath = boardDirectory.resolve("BoardMetadata.xml");
        Path losDataPath = boardDirectory.resolve("LOSData");

        SharedBoardMetadata shared = new SharedBoardMetadata();
        try (InputStream in = new FileInputStream(sharedPath.toFile())) {
            shared.parseSharedBoardMetadataFile(in);
        }

        BoardMetadata metadata = new BoardMetadata(shared);
        try (InputStream in = new FileInputStream(metadataPath.toFile())) {
            metadata.parseBoardMetadataFile(in);
        }

        HashMap<String, Terrain> terrainTypes = shared.getTerrainTypes();
        Map map = new Map(1800.0 / 32.0, 645.0 / 10.0, 33, 10, 0.0, 32.25, 1800, 645, terrainTypes,
                GRID_CONFIGURATION, GRID_CONFIGURATION, false);

        try (ObjectInputStream infile = new ObjectInputStream(new BufferedInputStream(
                new GZIPInputStream(new FileInputStream(losDataPath.toFile()))))) {
            int width = infile.readInt();
            int height = infile.readInt();
            int gridWidth = infile.readInt();
            int gridHeight = infile.readInt();
            if (width != 33 || height != 10 || gridWidth != 1800 || gridHeight != 645) {
                throw new IllegalStateException("not a standard geomorphic board");
            }

            for (int x = 0; x < gridWidth; x++) {
                for (int y = 0; y < gridHeight; y++) {
                    map.setGridElevation((int) infile.readByte(), x, y);
                    map.setGridTerrainCode(infile.readByte() & 255, x, y);
                }
            }

            for (int col = 0; col < width; col++) {
                int heightAdjustment = col % 2 == 1 ? 1 : 0;
                for (int row = 0; row < height + heightAdjustment; row++) {
                    byte stairway = infile.readByte();
                    Hex hex = map.getHex(col, row);
                    hex.setStairway((int) stairway == 1);
                    hex.resetHexAndLocationNames(map.getGEOHexName(col, row, false, false));
                }
            }
        }

        map.setRBrrembankments(metadata.getRBrrembankments());
        map.setPartialOrchards(metadata.getPartialOrchards());
        map.setSlopes(metadata.getSlopes());
        for (int col = 0; col < map.getHexGrid().length; col++) {
            for (int row = 0; row < map.getHexGrid()[col].length; row++) {
                map.getHexGrid()[col][row].resetHexsideLocationNames();
            }
        }

        map.resetHexTerrain();
        map.resetHexTerrain();

        out.print("{\"board\":\"bd" + escape(board) + "\"");
        out.print(",\"gridConfiguration\":\"" + GRID_CONFIGURATION + "\"");
        out.print(",\"harnessVersion\":\"" + HARNESS_VERSION + "\"");
        out.print(",\"hexes\":[\n");
        boolean first = true;
        for (int col = 0; col < map.getHexGrid().length; col++) {
            for (int row = 0; row < map.getHexGrid()[col].length; row++) {
                out.print(first ? "" : ",\n");
                first = false;
                writeHex(out, map.getHex(col, row));
            }
        }

        out.print("\n]");
        out.print(",\"losDataBlob\":\"" + gitBlob(losDataPath) + "\"");
        out.print(",\"metadataBlob\":\"" + gitBlob(metadataPath) + "\"");
        out.print(",\"sharedBoardMetadataBlob\":\"" + gitBlob(sharedPath) + "\"");
        out.print(",\"vaslCommit\":\"" + escape(commit) + "\"");
        out.print("}\n");
    }

    // Keys are written in ascending ordinal order so the output is canonical.
    private static void writeHex(PrintStream out, Hex hex) {
        out.print("{\"baseLevel\":" + hex.getBaseLevelofHex());
        out.print(",\"bridge\":");
        if (hex.hasBridge()) {
            out.print("{\"roadLevel\":" + hex.getBridge().getRoadLevel()
                    + ",\"terrain\":" + name(hex.getBridge().getTerrain()) + "}");
        } else {
            out.print("null");
        }

        out.print(",\"center\":" + location(hex.getCenterLocation()));
        out.print(",\"col\":" + hex.getColumnNumber());
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

        out.print("],\"row\":" + hex.getRowNumber());
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
