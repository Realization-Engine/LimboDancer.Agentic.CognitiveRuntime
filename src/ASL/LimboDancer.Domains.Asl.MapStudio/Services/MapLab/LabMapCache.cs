using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace LimboDancer.Domains.Asl.MapStudio.Services.MapLab;

/// <summary>One measure of pan and zoom, as the page shows it: the map, the zoom, and the probe's figures.</summary>
public sealed record LabRun(string Map, string Zoom, string Elements, string FramesPerSecond, string MedianMs, string LongestMs, string SettleMs,
    string WheelMs, string Pane);

/// <summary>
/// Holds the one Map Lab map last asked for, with its drawn files (Map Lab Plan, section 7: the map belongs to the Lab and is dropped when another
/// is asked for). A million-hex map's files are tens of megabytes, so no second map is kept. Each map and file is built once, however many requests
/// ask for it at once, and the time each took is kept for the page to show.
/// </summary>
public sealed class LabMapCache
{
    /// <summary>How many measures are kept.</summary>
    public const int RunBound = 60;

    private readonly Lock gate = new();
    private readonly LinkedList<LabRun> runs = new();
    private Held? held;

    /// <summary>The measures taken since the Studio started, newest first, at most <see cref="RunBound"/>.</summary>
    public IReadOnlyList<LabRun> Runs
    {
        get
        {
            lock (gate)
            {
                return [.. runs];
            }
        }
    }

    /// <summary>Keeps a measure: each map loads its own page, so the page cannot keep them itself.</summary>
    public void Record(LabRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        lock (gate)
        {
            runs.AddFirst(run);
            while (runs.Count > RunBound)
            {
                runs.RemoveLast();
            }
        }
    }

    /// <summary>The map, built if it is not the one held.</summary>
    public LabMap Map(LabMapSpec spec) => For(spec).Map.Value;

    /// <summary>A file of the map: a layer (<c>grid.svg</c>), or the labels or tile index; null when the map has no such file.</summary>
    public RenderedSvg? File(LabMapSpec spec, string file)
    {
        var entry = For(spec);
        return entry.Files.GetOrAdd(file, name => new Lazy<RenderedSvg?>(() =>
        {
            var map = entry.Map.Value;
            var timer = Stopwatch.StartNew();
            var text = name switch
            {
                "labels.json" => LabMapRenderer.LabelIndex(map),
                "tiles.json" when spec.Tiled => LabMapRenderer.TileIndex(map),
                _ when name.EndsWith(".svg", StringComparison.Ordinal) => LabMapRenderer.Fragment(map, name[..^4]),
                _ => null,
            };
            if (text is null)
            {
                return null;
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            var rendered = new RenderedSvg(bytes, Compressed(bytes), "\"" + Convert.ToHexStringLower(SHA256.HashData(bytes)) + "\"");
            entry.Timings[name] = (timer.Elapsed.TotalMilliseconds, bytes.Length);
            return rendered;
        }, LazyThreadSafetyMode.ExecutionAndPublication)).Value;
    }

    /// <summary>What building the map and each file it has served took: milliseconds and bytes, by file; the map itself as "map".</summary>
    public IReadOnlyDictionary<string, (double Milliseconds, int Bytes)> Timings(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        lock (gate)
        {
            return held is { } entry && entry.Spec == spec ? new Dictionary<string, (double, int)>(entry.Timings) : new Dictionary<string, (double, int)>();
        }
    }

    private Held For(LabMapSpec spec)
    {
        ArgumentNullException.ThrowIfNull(spec);
        lock (gate)
        {
            if (held is not { } entry || entry.Spec != spec)
            {
                // The map's hexes are the same for every technique, so a change of technique keeps them and draws its files again.
                var map = held is { } old && old.Spec.MapKey == spec.MapKey && old.Map.IsValueCreated
                    ? new Lazy<LabMap>(old.Map.Value.With(spec))
                    : null;
                entry = new Held(spec);
                entry.Map = map ?? new Lazy<LabMap>(() =>
                {
                    var timer = Stopwatch.StartNew();
                    var built = LabMapGenerator.Generate(spec);
                    entry.Timings["map"] = (timer.Elapsed.TotalMilliseconds, built.HexCount);
                    return built;
                }, LazyThreadSafetyMode.ExecutionAndPublication);
                if (map is not null && held!.Timings.TryGetValue("map", out var built))
                {
                    entry.Timings["map"] = built;
                }

                held = entry;
            }

            return entry;
        }
    }

    private static byte[] Compressed(byte[] bytes)
    {
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, CompressionLevel.Fastest, leaveOpen: true))
        {
            gzip.Write(bytes);
        }

        return output.ToArray();
    }

    private sealed class Held(LabMapSpec spec)
    {
        public LabMapSpec Spec { get; } = spec;

        public Lazy<LabMap> Map { get; set; } = null!;

        public ConcurrentDictionary<string, Lazy<RenderedSvg?>> Files { get; } = new(StringComparer.Ordinal);

        public ConcurrentDictionary<string, (double Milliseconds, int Bytes)> Timings { get; } = new(StringComparer.Ordinal);
    }
}
