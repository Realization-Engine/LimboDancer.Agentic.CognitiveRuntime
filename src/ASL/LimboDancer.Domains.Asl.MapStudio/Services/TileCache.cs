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
public sealed partial class TileCache(ILogger<TileCache>? logger = null, long bound = TileCache.DefaultBound, string? diskRoot = null)
{
    public const long DefaultBound = 64L * 1024 * 1024;

    // Screen-Bounded Map Analysis, section 3.5: boards' tiles are also kept on disk under diskRoot, one version per board, so a restart or a
    // second game on the board starts with them painted; the in-memory bound stays in front.
    private readonly ConcurrentDictionary<string, byte> persisted = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> ahead = new(StringComparer.Ordinal);
    private int diskReads;

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
    public static string FileName(TileVariant variant, int level, int column, int row) => FileName(variant, TileLines.None, level, column, row);

    /// <summary>
    /// The tile's file name with its lines (Screen-Bounded Map Analysis, section 3.1): <c>tile-{variant}+{lines}-{level}-{column}-{row}.png</c>,
    /// the lines being g (grid), b (Formation boundaries), or gb; without lines, as <see cref="FileName(TileVariant, int, int, int)"/>.
    /// </summary>
    public static string FileName(TileVariant variant, TileLines lines, int level, int column, int row) =>
        string.Create(CultureInfo.InvariantCulture, $"tile-{VariantName(variant)}{LinesName(lines)}-{level}-{column}-{row}.png");

    /// <summary>
    /// Reads a tile's file name without lines, or false when the name is not one. Not named <c>TryParse</c>: the endpoint takes the cache as a
    /// service, and a static <c>TryParse</c> would make ASP.NET try to bind it from the route.
    /// </summary>
    public static bool TryReadFileName(string file, out TileVariant variant, out int level, out int column, out int row) =>
        TryReadFileName(file, out variant, out var lines, out level, out column, out row) && lines == TileLines.None;

    /// <summary>Reads a tile's file name with its lines, or false when the name is not a tile's.</summary>
    public static bool TryReadFileName(string file, out TileVariant variant, out TileLines lines, out int level, out int column, out int row)
    {
        variant = TileVariant.TerrainAndElevation;
        lines = TileLines.None;
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
        lines = match.Groups[2].Value switch
        {
            "g" => TileLines.Grid,
            "b" => TileLines.Boundaries,
            "gb" => TileLines.Grid | TileLines.Boundaries,
            _ => TileLines.None,
        };
        return int.TryParse(match.Groups[3].Value, CultureInfo.InvariantCulture, out level)
            && int.TryParse(match.Groups[4].Value, CultureInfo.InvariantCulture, out column)
            && int.TryParse(match.Groups[5].Value, CultureInfo.InvariantCulture, out row);
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
        // The hex lattice too (Screen-Bounded Map Analysis, section 3.4): the viewport places a counter fragment by its hex from it.
        return string.Create(CultureInfo.InvariantCulture,
            $"{{\"width\":{geometry.GridWidth},\"height\":{geometry.GridHeight},\"tileSize\":{TilePyramid.TileSize},\"levels\":{TilePyramid.LevelCount(geometry.GridWidth, geometry.GridHeight)},\"hexWidth\":{geometry.HexWidth},\"hexHeight\":{geometry.HexHeight},\"a1x\":{geometry.A1CenterX},\"a1y\":{geometry.A1CenterY}}}");
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

        Persisted(board);
        DropOtherVersions(board.Ref.Value, board.Version);
        return GetKeyed(new TileKey(board.Ref.Value, board.Version, variant, level, column, row), key => Kept(key, () => Paint(input, key)));
    }

    /// <summary>
    /// The tile with lines painted over its terrain (Screen-Bounded Map Analysis, section 3.1); the boundaries need <paramref name="owners"/>,
    /// whose token keys the tile, and are left out without them. Null as <see cref="Get(StudioBoard, TileVariant, int, int, int)"/> is.
    /// </summary>
    public RenderedTile? Get(StudioBoard board, TileVariant variant, TileLines lines, BoundaryParents? owners, int level, int column, int row)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (lines == TileLines.None)
        {
            return Get(board, variant, level, column, row);
        }

        if (board.InputFor(BoardView.Exact) is not { } input)
        {
            return null;
        }

        var geometry = input.Grid.Geometry;
        if (!TilePyramid.Contains(geometry.GridWidth, geometry.GridHeight, level, column, row))
        {
            return null;
        }

        if (owners is null)
        {
            lines &= ~TileLines.Boundaries;
        }

        Persisted(board);
        DropOtherVersions(board.Ref.Value, board.Version);
        var key = new TileKey(board.Ref.Value, board.Version, variant, level, column, row, lines, owners?.Token ?? string.Empty);
        return GetKeyed(key, keyed => Kept(keyed, () => PaintLined(input, keyed, owners)));
    }

    /// <summary>
    /// Paints, in the background, every tile of the board's pyramid in the given variant and lines that the disk does not hold
    /// yet, coarsest first (Screen-Bounded Map Analysis, section 3.5), so a zoom finds its tiles painted. The tiles go to the disk only, never
    /// into the in-memory bound, so the painting does not push out the tiles in view. Once per board version, variant, lines, and ownership;
    /// nothing without a disk. Returns the number of tiles it will look at, 0 when it has already run or cannot.
    /// </summary>
    public int PaintAhead(StudioBoard board, TileVariant variant, TileLines lines, BoundaryParents? owners)
    {
        ArgumentNullException.ThrowIfNull(board);
        if (diskRoot is null || board.InputFor(BoardView.Exact) is not { } input)
        {
            return 0;
        }

        if (owners is null)
        {
            lines &= ~TileLines.Boundaries;
        }

        var geometry = input.Grid.Geometry;
        var levels = TilePyramid.LevelCount(geometry.GridWidth, geometry.GridHeight);
        var tiles = new List<TileKey>();
        // Every level, coarsest first: a deep zoom goes straight to level 0, which the browser's next-level fetch does not reach
        // (Screen-Bounded Map Analysis, section 5.3).
        for (var level = levels - 1; level >= 0; level--)
        {
            for (var column = 0; column < TilePyramid.Columns(geometry.GridWidth, level); column++)
            {
                for (var row = 0; row < TilePyramid.Rows(geometry.GridHeight, level); row++)
                {
                    tiles.Add(new TileKey(board.Ref.Value, board.Version, variant, level, column, row, lines, owners?.Token ?? string.Empty));
                }
            }
        }

        var job = $"{board.Ref.Value}/{board.Version}/{FileName(variant, lines, 0, 0, 0)}/{owners?.Token}";
        if (!ahead.TryAdd(job, 0))
        {
            return 0;
        }

        Persisted(board);
        DropOtherVersions(board.Ref.Value, board.Version);

        _ = Task.Run(() =>
        {
            try
            {
                Parallel.ForEach(tiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, key =>
                {
                    if (DiskPath(key) is { } path && !File.Exists(path) && versions.TryGetValue(key.Board, out var held) && held == key.Version)
                    {
                        Persist(key, key.Lines == TileLines.None ? Paint(input, key) : PaintLined(input, key, owners));
                    }
                });
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or AggregateException)
            {
                Warn(error, "painting ahead", job);
            }
        });
        return tiles.Count;
    }

    /// <summary>How many tiles were read from the disk instead of painted.</summary>
    public int DiskReads => Volatile.Read(ref diskReads);

    private RenderedTile PaintLined(BoardRenderInput input, TileKey key, BoundaryParents? owners) =>
        PaintWith(key, () => TilePainter.Png(input.Grid, input.Catalog, key.Variant, key.Lines, owners is null ? [] : owners.Parents,
            owners?.HexesPerParent ?? 1, key.Level, key.Column, key.Row));

    // A board's tile from the disk when it is there, else painted and written there (Screen-Bounded Map Analysis, section 3.5).
    private RenderedTile Kept(TileKey key, Func<RenderedTile> paint)
    {
        if (DiskPath(key) is { } path && File.Exists(path))
        {
            try
            {
                var png = File.ReadAllBytes(path);
                Interlocked.Increment(ref diskReads);
                return new RenderedTile(png, "\"" + Convert.ToHexStringLower(SHA256.HashData(png)) + "\"");
            }
            catch (IOException)
            {
                // A file half written by a stopped Studio: painted again below.
            }
        }

        return Persist(key, paint());
    }

    private RenderedTile Persist(TileKey key, RenderedTile tile)
    {
        if (DiskPath(key) is not { } path)
        {
            return tile;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            // Written beside and moved, so a reader never sees half a file.
            var stage = path + ".stage-" + Guid.NewGuid().ToString("N");
            File.WriteAllBytes(stage, tile.Png);
            File.Move(stage, path, overwrite: true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            Warn(error, "keeping a tile", path);
        }

        return tile;
    }

    // {disk}/{board}/{version}/{ownership token, or _}/{file}. Only boards' tiles are kept, never the Map Lab's.
    private string? DiskPath(TileKey key) => diskRoot is null || !persisted.ContainsKey(key.Board) ? null
        : Path.Combine(diskRoot, Safe(key.Board), Safe(key.Version), key.Owner.Length == 0 ? "_" : Safe(key.Owner),
            FileName(key.Variant, key.Lines, key.Level, key.Column, key.Row));

    private static string Safe(string name) => string.Concat(name.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));

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
        return GetKeyed(new TileKey(source, version, variant, level, column, row), paint);
    }

    private RenderedTile GetKeyed(TileKey key, Func<TileKey, RenderedTile> paint)
    {
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
        LoadMeasurements.Record(logger, key.Board, FileName(key.Variant, key.Lines, key.Level, key.Column, key.Row), timer.Elapsed.TotalMilliseconds);
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

        // The disk keeps one version per board too: the others, from this run or an earlier one, are removed in the background.
        if (diskRoot is not null && persisted.ContainsKey(source))
        {
            var folder = Path.Combine(diskRoot, Safe(source));
            var keep = Safe(version);
            _ = Task.Run(() =>
            {
                try
                {
                    if (Directory.Exists(folder))
                    {
                        foreach (var old in Directory.EnumerateDirectories(folder).Where(path => Path.GetFileName(path) != keep).ToList())
                        {
                            Directory.Delete(old, recursive: true);
                        }
                    }
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException)
                {
                    Warn(error, "removing old tiles", source);
                }
            });
        }
    }

    private void Warn(Exception error, string action, string subject)
    {
        if (logger is not null)
        {
            DiskProblem(logger, error, action, subject);
        }
    }

    [LoggerMessage(EventId = 902, Level = LogLevel.Warning, Message = "ASL tiles on disk: {Action} failed for {Subject}")]
    private static partial void DiskProblem(ILogger logger, Exception error, string action, string subject);

    // A board's tiles are kept on disk; a source of the Map Lab's is not.
    private void Persisted(StudioBoard board) => persisted.TryAdd(board.Ref.Value, 0);

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

    private static string LinesName(TileLines lines) => lines switch
    {
        TileLines.Grid => "+g",
        TileLines.Boundaries => "+b",
        TileLines.Grid | TileLines.Boundaries => "+gb",
        _ => string.Empty,
    };

    [GeneratedRegex(@"^tile-(te|t|e)(?:\+(gb|g|b))?-(\d{1,2})-(\d{1,5})-(\d{1,5})\.png$", RegexOptions.CultureInvariant)]
    private static partial Regex TileName();

    // Lines and Owner are none and empty for tiles without lines; Owner names the ownership a boundary tile was painted from.
    private readonly record struct TileKey(string Board, string Version, TileVariant Variant, int Level, int Column, int Row,
        TileLines Lines = TileLines.None, string Owner = "");
}
