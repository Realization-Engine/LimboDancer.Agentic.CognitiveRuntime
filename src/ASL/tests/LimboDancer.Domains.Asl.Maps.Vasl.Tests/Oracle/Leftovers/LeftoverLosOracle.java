import java.io.*;
import java.lang.reflect.*;
import java.nio.file.*;
import java.util.*;
import javax.xml.parsers.DocumentBuilderFactory;
import org.w3c.dom.*;
import VASL.LOS.Map.Hex;
import VASL.LOS.Map.Location;
import VASL.LOS.Map.LOSResult;
import VASL.LOS.Map.Map;
import VASL.LOS.VASLGameInterface;
import VASL.build.module.map.boardArchive.SharedBoardMetadata;

/** Controlled, authored maps. No VASL board data is copied or changed. */
public class LeftoverLosOracle {
    // Reuse the established harness's serialization and headless game setup verbatim.
    static Object invoke(String name, Class<?>[] types, Object... args) throws Exception {
        Method method = HexFactOracle.class.getDeclaredMethod(name, types);
        method.setAccessible(true);
        return method.invoke(null, args);
    }

    public static void main(String[] args) throws Exception {
        Path root = Path.of(args[0]);
        Path spec = Path.of(args[1]);
        Path output = Path.of(args[2]);
        Files.createDirectories(output);
        invoke("stubGameModule", new Class<?>[0]);
        SharedBoardMetadata shared = (SharedBoardMetadata) invoke("shared", new Class<?>[]{Path.class}, root);
        String commit = (String) invoke("headCommit", new Class<?>[]{Path.class}, root);
        // Hash the current authored specification, independent of the Git index and checkout line endings.
        byte[] specBytes = Files.readString(spec).replace("\r\n", "\n").getBytes(java.nio.charset.StandardCharsets.UTF_8);
        java.security.MessageDigest hash = java.security.MessageDigest.getInstance("SHA-1");
        hash.update(("blob " + specBytes.length + "\0").getBytes(java.nio.charset.StandardCharsets.US_ASCII));
        String specBlob = HexFormat.of().formatHex(hash.digest(specBytes));
        String sharedBlob = (String) invoke("gitBlob", new Class<?>[]{Path.class}, root.resolve("dist/boardData/SharedBoardMetadata.xml"));
        NodeList cases = DocumentBuilderFactory.newInstance().newDocumentBuilder().parse(spec.toFile()).getElementsByTagName("map");
        for (int index = 0; index < cases.getLength(); index++) {
            Element test = (Element) cases.item(index);
            String name = test.getAttribute("name");
            Map map = new Map(56.25, 64.5, 33, 10, 0, 32.25, 1800, 645,
                shared.getTerrainTypes(), HexFactOracle.GRID_CONFIGURATION, HexFactOracle.GRID_CONFIGURATION, false);
            for (int x = 0; x < 1800; x++) for (int y = 0; y < 645; y++) {
                map.setGridTerrainCode(0, x, y);
                map.setGridElevation(0, x, y);
            }
            for (Element paint : children(test, "paint")) {
                int code = map.getTerrain(paint.getAttribute("terrain")).getType();
                int elevation = Integer.parseInt(paint.getAttribute("elevation"));
                String[] box = paint.getAttribute("box").split(",");
                for (int x = Integer.parseInt(box[0]); x < Integer.parseInt(box[2]); x++)
                    for (int y = Integer.parseInt(box[1]); y < Integer.parseInt(box[3]); y++) {
                        map.setGridTerrainCode(code, x, y);
                        map.setGridElevation(elevation, x, y);
                    }
            }
            for (Element annotation : children(test, "annotation")) {
                boolean[] sides = new boolean[6];
                for (char side : annotation.getAttribute("sides").toCharArray()) sides[side - '0'] = true;
                Hex hex = map.getHex(annotation.getAttribute("hex"));
                if (annotation.getAttribute("kind").equals("partialOrchard")) hex.setPartialOrchards(sides);
                else hex.setRBrrembankments(sides);
            }
            map.resetHexTerrain();
            map.resetHexTerrain();
            // Entrenchments normally arrive through counters. Supply the resolved location terrain explicitly.
            for (Element end : children(test, "location")) {
                Hex hex = map.getHex(end.getAttribute("hex"));
                hex.getCenterLocation().setTerrain(map.getTerrain(end.getAttribute("terrain")));
            }
            HexFactOracle.PlacedBoard board = new HexFactOracle.PlacedBoard();
            board.placement = new HexFactOracle.Placement("01", 0, 0, false, List.of());
            board.gridWidth = 1800; board.gridHeight = 645;
            List<HexFactOracle.PlacedBoard> boards = List.of(board);
            try (PrintStream out = new PrintStream(output.resolve(name + ".json").toFile(), "UTF-8")) {
                out.print("{\"harnessVersion\":\"leftovers-1\",\"specBlob\":\"" + specBlob + "\",\"vaslCommit\":\"" + commit
                    + "\",\"sharedBoardMetadataBlob\":\"" + sharedBlob + "\",\"scenario\":null,\"boards\":[{\"board\":\"bd01\",\"column\":0,\"row\":0,\"reversed\":false}],\"pairs\":[");
                List<Location> locations = new ArrayList<>();
                for (String hexName : test.getAttribute("hexes").split(",")) {
                    Hex hex = map.getHex(hexName);
                    @SuppressWarnings("unchecked") List<Location> chain = (List<Location>) invoke("chain", new Class<?>[]{Hex.class}, hex);
                    locations.addAll(chain);
                }
                List<Location> sources = new ArrayList<>(locations);
                for (String hexName : test.getAttribute("hexsides").split(",")) {
                    if (!hexName.isEmpty()) for (int side = 0; side < 6; side++) sources.add(map.getHex(hexName).getHexsideLocation(side));
                }
                boolean first = true;
                VASLGameInterface game = new VASLGameInterface(null, null);
                for (Location source : sources) for (Location target : locations) {
                    if (source == target) continue;
                    boolean side = !source.isCenterLocation();
                    for (int aim = 0; aim < (side ? 2 : 1); aim++) {
                        LOSResult result = new LOSResult();
                        String failure = null;
                        try { map.LOS(source, aim == 1, target, false, result, game, null); }
                        catch (RuntimeException ex) { failure = ex.getClass().getSimpleName(); result = new LOSResult(); }
                        if (!first) out.print(",\n");
                        first = false;
                        invoke("writePair", new Class<?>[]{PrintStream.class, Map.class, List.class, Location.class, Boolean.class, Location.class, LOSResult.class, String.class},
                            out, map, boards, source, side ? Boolean.valueOf(aim == 1) : null, target, result, failure);
                    }
                }
                out.print("]");
                invoke("writeHexes", new Class<?>[]{PrintStream.class, Map.class}, out, map);
                out.println("}");
            }
            System.err.println(name);
        }
    }

    static List<Element> children(Element parent, String tag) {
        List<Element> result = new ArrayList<>();
        NodeList nodes = parent.getElementsByTagName(tag);
        for (int i = 0; i < nodes.getLength(); i++) result.add((Element) nodes.item(i));
        return result;
    }
}
