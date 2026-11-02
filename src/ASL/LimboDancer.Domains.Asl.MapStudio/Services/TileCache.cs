using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Rendering;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>A raster tile as PNG bytes, with its content-hash entity tag.</summary>
public sealed record RenderedTile(byte[] Png, string ETag);

/// <summary>
/// The Exact view's raster tiles (Raster Tiles Plan, section 2.4): painted from the board's grid when first asked for, and held by least recent
/// use up to a bound in bytes. Like <see cref="RenderCache"/>, it keeps one version of each board, the one last asked for, and a tile asked for
/// by several requests at once is painted once.
/// </summary>
public sealed partial class TileCache(ILogger<TileCache>? logger = null, long bound = TileCache.DefaultBound)
{
    public const long DefaultBound = 64L * 1024 * 1024;

    /// <summary>The pyramid's shape as JSON: the grid's size, the tile size, and the number of levels.</summary>
    public const string IndexFile = "tiles.json";

    private readonly Lock gate = new();
    private readonly Dictionary<TileKey, LinkedListNode<(TileKey Key, RenderedTile Tile)>> entries = [];
    private readonly LinkedList<(TileKey Key, RenderedTile Tile)> recent = new();
    private readonly ConcurrentDictionary<TileKey, Lazy<RenderedTile>> filling = new();
    private readonly ConcurrentDictionary<string, string> versions = new(StringComparer.Ordinal);
    private long bytes;
    private int painted;

    /// <summary>The bytes of PNG held.</summary>
    public long Bytes
    {
        get
        {
            lock (gate)
            {
                return bytes;
            }
        }
    }

    /// <summary>The number of tiles held.</summary>
    public int Count
    {
        get
        {
            lock (gate)
            {
                return entries.Count;
            }
        }
    }

    /// <summary>How many tiles have been painted since the cache was made.</summary>
    public int Painted => Volatile.Read(ref painted);

    /// <summary>The tile's file name: <c>tile-{variant}-{level}-{column}-{row}.png</c>, the variant being te, t, or e.</summary>
    public static string FileName(TileVariant variant, int level, int column, int row) =>
        string.Create(CultureInfo.InvariantCulture, $"tile-{VariantName(variant)}-{level}-{column}-{row}.png");

    /// <summary>
    /// Reads a tile's file name, or false when the name is not a tile's. Not named <c>TryParse</c>: the endpoint takes the cache as a
    /// service, and a static <c>TryParse</c> would make ASP.NET try to bind it from the route.
    /// </summary>
    public static bool TryReadFileName(string file, out TileVariant variant, out int level, out int column, out int row)
    {
        variant = TileVariant.TerrainAndElevation;
        level = column = row = 0;
        var match = TileName().Match(file);
        if (!match.Success)
        {
            return false;
        }

        variant = match.Groups[1].Value switch
        {
            "t" => TileVariant.Terrain,
            "e" => TileVariant.Elevation,
            _ => TileVariant.TerrainAndElevation,
        };
        return int.TryParse(match.Groups[2].Value, CultureInfo.InvariantCulture, out level)
            && int.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out column)
            && int.TryParse(match.Groups[4].Value, CultureInfo.InvariantCulture, out row);
    }

    /// <summary>The pyramid's shape for the board's Exact view, or null when the board has none.</summary>
    public static string? Index(StudioBoard board)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.InputFor(BoardView.Exact) is not { } input)
        {
            return null;
        }

        var geometry = input.Grid.Geometry;
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"width\":{geometry.GridWidth},\"height\":{geometry.GridHeight},\"tileSize\":{TilePyramid.TileSize},\"levels\":{TilePyramid.LevelCount(geometry.GridWidth, geometry.GridHeight)}}}");
    }

    /// <summary>The tile, painted if it is not held; null when the board has no Exact view or no such tile.</summary>
    public RenderedTile? Get(StudioBoard board, TileVariant variant, int level, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (board.InputFor(BoardView.Exact) is not { } input)
        {
            return null;
        }

        var geometry = input.Grid.Geometry;
        if (!TilePyramid.Contains(geometry.GridWidth, geometry.GridHeight, level, column, row))
        {
            return null;
        }

        return Get(board.Ref.Value, board.Version, variant, level, column, row, key => Paint(input, key));
    }

    /// <summary>
    /// The tile of any source that names itself and its version (the Map Lab's generated maps): held and bounded as a board's tiles are, and
    /// painted by <paramref name="paint"/> when not held. The caller checks the tile lies in its pyramid.
    /// </summary>
    public RenderedTile Get(string source, string version, TileVariant variant, int level, int column, int row, Func<byte[]> paint)
    {
        ArgumentNullException.ThrowIfNull(paint);
        return Get(source, version, variant, level, column, row, key => PaintWith(key, paint));
    }

    private RenderedTile Get(string source, string version, TileVariant variant, int level, int column, int row, Func<TileKey, RenderedTile> paint)
    {
        DropOtherVersions(source, version);
        var key = new TileKey(source, version, variant, level, column, row);
        lock (gate)
        {
            if (entries.TryGetValue(key, out var node))
            {
                recent.Remove(node);
                recent.AddFirst(node);
                return node.Value.Tile;
            }
        }

        // One fill per key: requests that arrive while it is painted wait for the same painting.
        var fill = filling.GetOrAdd(key, _ => new Lazy<RenderedTile>(() => paint(key), LazyThreadSafetyMode.ExecutionAndPublication));
        try
        {
            var tile = fill.Value;
            Store(key, tile);
            return tile;
        }
        finally
        {
            filling.TryRemove(new KeyValuePair<TileKey, Lazy<RenderedTile>>(key, fill));
        }
    }

    private RenderedTile Paint(BoardRenderInput input, TileKey key) =>
        PaintWith(key, () => TilePainter.Png(input.Grid, input.Catalog, key.Variant, key.Level, key.Column, key.Row));

    private RenderedTile PaintWith(TileKey key, Func<byte[]> paint)
    {
        var timer = Stopwatch.StartNew();
        var png = paint();
        Interlocked.Increment(ref painted);
        LoadMeasurements.Record(logger, key.Board, FileName(key.Variant, key.Level, key.Column, key.Row), timer.Elapsed.TotalMilliseconds);
        return new RenderedTile(png, "\"" + Convert.ToHexStringLower(SHA256.HashData(png)) + "\"");
    }

    private void Store(TileKey key, RenderedTile tile)
    {
        lock (gate)
        {
            if (entries.ContainsKey(key) || (versions.TryGetValue(key.Board, out var held) && held != key.Version))
            {
                return;
            }

            entries[key] = recent.AddFirst((key, tile));
            bytes += tile.Png.Length;
            while (bytes > bound && recent.Last is { } oldest && oldest.Value.Key != key)
            {
                Remove(oldest);
            }
        }
    }

    private void DropOtherVersions(string source, string version)
    {
        if (versions.TryGetValue(source, out var held) && held == version)
        {
            return;
        }

        lock (gate)
        {
            versions[source] = version;
            foreach (var node in entries.Values.Where(node => node.Value.Key.Board == source && node.Value.Key.Version != version).ToList())
            {
                Remove(node);
            }
        }
    }

    private void Remove(LinkedListNode<(TileKey Key, RenderedTile Tile)> node)
    {
        recent.Remove(node);
        entries.Remove(node.Value.Key);
        bytes -= node.Value.Tile.Png.Length;
    }

    private static string VariantName(TileVariant variant) => variant switch
    {
        TileVariant.Terrain => "t",
        TileVariant.Elevation => "e",
        _ => "te",
    };

    [GeneratedRegex(@"^tile-(te|t|e)-(\d{1,2})-(\d{1,5})-(\d{1,5})\.png$", RegexOptions.CultureInvariant)]
    private static partial Regex TileName();

    private readonly record struct TileKey(string Board, string Version, TileVariant Variant, int Level, int Column, int Row);
}
