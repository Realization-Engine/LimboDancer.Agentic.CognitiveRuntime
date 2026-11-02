using System.Globalization;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>
/// Draws a Map Lab map in the files the Studio's viewport reads for a board's Exact view (Map Lab Plan, section 3.3): SVG layer fragments, the
/// labels as data, and the raster tile pyramid. So the Lab's maps pan, zoom, tile, and probe through the same viewport as every board.
/// </summary>
public static class LabMapRenderer
{
    public const string TerrainLayer = "exact-terrain";
    public const string GridLayer = "grid";
    public const string BoundariesLayer = "boundaries";
    public const string LabelsLayer = "labels";

    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>The layers a map has, bottom first: formations' boundaries only when hexes are grouped.</summary>
    public static IReadOnlyList<string> Layers(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        // With lines in the tiles, the grid and the boundaries are part of the terrain layer.
        return spec.Technique == LabTechnique.TilesLines ? [TerrainLayer, LabelsLayer]
            : spec.Group > 1 ? [TerrainLayer, GridLayer, BoundariesLayer, LabelsLayer] : [TerrainLayer, GridLayer, LabelsLayer];
    }

    /// <summary>The map's extent in board units, as a view box.</summary>
    public static string ViewBox(LabMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        return string.Create(CultureInfo.InvariantCulture, $"0 0 {map.Geometry.GridWidth} {map.Geometry.GridHeight}");
    }

    /// <summary>A layer as an SVG document holding its one group, or null when the map has no such layer.</summary>
    public static string? Fragment(LabMap map, string layer)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (!Layers(map.Spec).Contains(layer))
        {
            return null;
        }

        var body = new StringBuilder();
        switch (layer)
        {
            case TerrainLayer when map.Spec.Technique == LabTechnique.Elements:
                body.Append("<g id=\"layer-exact-terrain\" stroke=\"none\">");
                ForEachHex(map, (hex, ordinal) =>
                {
                    body.Append("<polygon fill=\"").Append(map.ColourOf(ordinal)).Append("\" points=\"");
                    AppendPoints(body, map.Geometry.Vertices(hex));
                    body.Append("\"/>");
                });
                break;
            case TerrainLayer:
                body.Append("<g id=\"layer-exact-terrain\" stroke=\"none\">");
                AppendTerrainPaths(body, map);
                break;
            case GridLayer when map.Spec.Technique == LabTechnique.Elements:
                body.Append("<g id=\"layer-grid\" fill=\"none\" stroke=\"#5d5a45\" stroke-opacity=\"0.35\" stroke-width=\"0.8\" vector-effect=\"non-scaling-stroke\" pointer-events=\"none\">");
                ForEachHex(map, (hex, _) =>
                {
                    body.Append("<polygon points=\"");
                    AppendPoints(body, map.Geometry.Vertices(hex));
                    body.Append("\"/>");
                });
                break;
            case GridLayer:
                body.Append("<g id=\"layer-grid\" pointer-events=\"none\"><path fill=\"none\" stroke=\"#5d5a45\" stroke-opacity=\"0.35\" stroke-width=\"0.8\" vector-effect=\"non-scaling-stroke\" d=\"");
                var own = new int[map.HexCount];
                for (var i = 0; i < own.Length; i++)
                {
                    own[i] = i;
                }

                CampaignMapBoundaries.AppendEdges(body, map.Geometry, own);
                body.Append("\"/>");
                break;
            case BoundariesLayer when map.Spec.Group > 1:
                // The Formation boundaries outline of the Goodwood ASL map, drawn by the same method (Map Lab Plan, section 3.4).
                body.Append("<g id=\"layer-boundaries\" pointer-events=\"none\"><path fill=\"none\" stroke=\"#9b5f17\" stroke-width=\"1.5\" vector-effect=\"non-scaling-stroke\" d=\"");
                CampaignMapBoundaries.AppendEdges(body, map.Geometry, map.Parents);
                body.Append("\"/>");
                break;
            default:
                return null;
        }

        body.Append("</g>");
        return string.Create(CultureInfo.InvariantCulture, $"<svg xmlns=\"{SvgNamespace}\" viewBox=\"{ViewBox(map)}\">") + body + "</svg>";
    }

    /// <summary>The hex labels as data, in the shape the viewport draws labels in view from: a name and a place for every hex.</summary>
    public static string LabelIndex(LabMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var names = new StringBuilder();
        var xs = new StringBuilder();
        var ys = new StringBuilder();
        ForEachHex(map, (hex, ordinal) =>
        {
            if (ordinal > 0)
            {
                names.Append(',');
                xs.Append(',');
                ys.Append(',');
            }

            var centre = map.Geometry.CenterDot(hex);
            names.Append(CultureInfo.InvariantCulture, $"\"{hex.Column}.{hex.Row}\"");
            xs.Append(Number(centre.X));
            ys.Append(Number(centre.Y - 22));
        });
        return "{\"fontSize\":9,\"names\":[" + names + "],\"x\":[" + xs + "],\"y\":[" + ys + "]}";
    }

    /// <summary>The tile pyramid's shape, as the viewport reads it.</summary>
    public static string TileIndex(LabMap map)
    {
        ArgumentNullException.ThrowIfNull(map);
        var geometry = map.Geometry;
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"width\":{geometry.GridWidth},\"height\":{geometry.GridHeight},\"tileSize\":{TilePyramid.TileSize},\"levels\":{TilePyramid.LevelCount(geometry.GridWidth, geometry.GridHeight)}}}");
    }

    /// <summary>Whether the tile lies in the map's pyramid.</summary>
    public static bool HasTile(LabMap map, int level, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(map);
        return TilePyramid.Contains(map.Geometry.GridWidth, map.Geometry.GridHeight, level, column, row);
    }

    /// <summary>The fewest pixels across a hex at which the grid is painted into a tile, and across a formation for its boundaries.</summary>
    public const double GridMinimumPixels = 6;

    /// <summary>
    /// A terrain tile as PNG: each pixel the colour of the hex under it, a pixel being 2^level board units on a side; above level 0 the mean of
    /// four samples, so far tiles are softened, not speckled. With <see cref="LabTechnique.TilesLines"/> a pixel whose hex differs from the
    /// hex of the pixel to its right or below is a line pixel: a Formation boundary where the two hexes' formations differ, else the grid. Each
    /// is left out at a level where its hexes or formations are too small to see, so no line grows with the map.
    /// </summary>
    public static byte[] TilePng(LabMap map, int level, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(map);
        var geometry = map.Geometry;
        var (width, height) = TilePyramid.PixelSize(geometry.GridWidth, geometry.GridHeight, level, column, row);
        var scale = 1 << level;
        var span = TilePyramid.Span(level);
        var pixels = new byte[width * height * 3];
        var rgb = Rgb();
        var samples = level == 0 ? 1 : 2;
        var lines = map.Spec.Technique == LabTechnique.TilesLines;
        var hexPixels = geometry.HexWidth / scale;
        var grid = lines && hexPixels >= GridMinimumPixels;
        var boundaries = lines && map.Spec.Group > 1 && hexPixels * Math.Sqrt(map.Spec.Group) >= GridMinimumPixels;
        // The hex at each pixel's centre, with one more column and row, so a tile's last pixels compare with the next tile's first.
        int[]? centres = null;
        if (grid || boundaries)
        {
            centres = new int[(width + 1) * (height + 1)];
            for (var py = 0; py <= height; py++)
            {
                for (var px = 0; px <= width; px++)
                {
                    centres[(py * (width + 1)) + px] = OrdinalAt(geometry, (column * span) + ((px + 0.5) * scale), (row * span) + ((py + 0.5) * scale));
                }
            }
        }

        for (var py = 0; py < height; py++)
        {
            for (var px = 0; px < width; px++)
            {
                var at = ((py * width) + px) * 3;
                if (centres is not null && LineAt(map, centres, width, px, py, grid, boundaries) is { } line)
                {
                    pixels[at] = line.R;
                    pixels[at + 1] = line.G;
                    pixels[at + 2] = line.B;
                    continue;
                }

                int r = 0, g = 0, b = 0;
                for (var sy = 0; sy < samples; sy++)
                {
                    for (var sx = 0; sx < samples; sx++)
                    {
                        var x = (column * span) + ((px + ((sx + 0.5) / samples)) * scale);
                        var y = (row * span) + ((py + ((sy + 0.5) / samples)) * scale);
                        var ordinal = OrdinalAt(geometry, x, y);
                        var colour = rgb[(int)map.Terrain[ordinal], map.Levels[ordinal]];
                        r += colour.R;
                        g += colour.G;
                        b += colour.B;
                    }
                }

                var n = samples * samples;
                pixels[at] = (byte)(r / n);
                pixels[at + 1] = (byte)(g / n);
                pixels[at + 2] = (byte)(b / n);
            }
        }

        return PngEncoder.Encode(pixels, width, height, alpha: false);
    }

    // The boundary colour of the vector layer, and the grid's colour at its opacity over open ground.
    private static readonly (byte R, byte G, byte B) BoundaryColour = (0x9b, 0x5f, 0x17);
    private static readonly (byte R, byte G, byte B) GridColour = (0xbd, 0xb5, 0x94);

    private static (byte R, byte G, byte B)? LineAt(LabMap map, int[] centres, int width, int px, int py, bool grid, bool boundaries)
    {
        var stride = width + 1;
        var here = centres[(py * stride) + px];
        var right = centres[(py * stride) + px + 1];
        var below = centres[((py + 1) * stride) + px];
        if (here == right && here == below)
        {
            return null;
        }

        var parent = map.Parents[here];
        if (boundaries && (map.Parents[right] != parent || map.Parents[below] != parent))
        {
            return BoundaryColour;
        }

        return grid ? GridColour : null;
    }

    /// <summary>The ordinal of the hex whose centre is nearest the point, clamped to the map.</summary>
    public static int OrdinalAt(BoardGeometry geometry, double x, double y)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        var near = (int)Math.Round(x / geometry.HexWidth);
        var best = 0;
        var bestDistance = double.MaxValue;
        for (var column = Math.Max(0, near - 1); column <= Math.Min(geometry.WidthInHexes - 1, near + 1); column++)
        {
            var offset = geometry.HexHeight / 2 * (column % 2);
            var middle = (int)Math.Round(((y + offset) / geometry.HexHeight) - 0.5);
            var cx = column * geometry.HexWidth;
            for (var row = Math.Max(0, middle - 1); row <= Math.Min(geometry.RowCount(column) - 1, middle + 1); row++)
            {
                var cy = (geometry.HexHeight / 2) + (geometry.HexHeight * row) - offset;
                var distance = ((cx - x) * (cx - x)) + ((cy - y) * (cy - y));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = (column * geometry.HeightInHexes) + (column / 2) + row;
                }
            }
        }

        return best;
    }

    // One path per colour, each hex a closed subpath: the browser holds a few elements whatever the map's size.
    private static void AppendTerrainPaths(StringBuilder body, LabMap map)
    {
        var groups = new Dictionary<string, StringBuilder>(StringComparer.Ordinal);
        ForEachHex(map, (hex, ordinal) =>
        {
            var colour = map.ColourOf(ordinal);
            if (!groups.TryGetValue(colour, out var path))
            {
                path = new StringBuilder();
                groups[colour] = path;
            }

            var vertices = map.Geometry.Vertices(hex);
            path.Append('M');
            AppendPoints(path, vertices);
            path.Append('Z');
        });

        foreach (var (colour, path) in groups.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            body.Append("<path fill=\"").Append(colour).Append("\" d=\"").Append(path).Append("\"/>");
        }
    }

    private static void ForEachHex(LabMap map, Action<HexIndex, int> action)
    {
        var ordinal = 0;
        foreach (var hex in map.Geometry.Hexes())
        {
            action(hex, ordinal++);
        }
    }

    private static void AppendPoints(StringBuilder builder, IReadOnlyList<PixelPoint> vertices)
    {
        for (var i = 0; i < vertices.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(Number(vertices[i].X)).Append(',').Append(Number(vertices[i].Y));
        }
    }

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private static (byte R, byte G, byte B)[,] Rgb()
    {
        var colours = LabMap.Colours;
        var rgb = new (byte, byte, byte)[colours.GetLength(0), colours.GetLength(1)];
        for (var kind = 0; kind < colours.GetLength(0); kind++)
        {
            for (var level = 0; level < colours.GetLength(1); level++)
            {
                var text = colours[kind, level];
                rgb[kind, level] = (Convert.ToByte(text[1..3], 16), Convert.ToByte(text[3..5], 16), Convert.ToByte(text[5..7], 16));
            }
        }

        return rgb;
    }
}
