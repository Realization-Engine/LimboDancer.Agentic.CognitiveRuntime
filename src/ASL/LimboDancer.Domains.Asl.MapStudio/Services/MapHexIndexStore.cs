using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>
/// The saved MapHexIndex (the MapHexIndex Design, rulings RMH.3 to RMH.5): written by the fidelity batch into <c>{CacheRoot}/hexindex</c>, beside its
/// report and keyed by the same start time, and read by the board library's "Has terrain" filter and the board viewer, the newest first. A read is kept
/// while the newest file is unchanged.
/// </summary>
public sealed class MapHexIndexStore(StudioOptions options)
{
    private const string Prefix = "maphexindex-";
    private readonly Lock gate = new();
    private (string Path, DateTime Written, MapHexIndex? Index) kept;

    public string Directory => Folder(options);

    private static string Folder(StudioOptions options) => Path.Combine(options.ResolveCacheRoot(), "hexindex");

    /// <summary>The report's id, as <see cref="FidelityReportStore"/> names it, so an index and its report pair by name.</summary>
    public static string ReportId(FidelityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return "fidelity-" + report.StartedAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Writes the index of a finished batch: every board of its report, with its outcome and LOSData blob, and the features the batch derived for it;
    /// a board the batch could not derive has none.
    /// </summary>
    public static string Save(StudioOptions options, FidelityReport report, IReadOnlyDictionary<string, IReadOnlyList<MapHexFeature>> features)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(features);
        var id = ReportId(report);
        var index = new MapHexIndex(MapHexIndexer.Version, report.Versions.GetValueOrDefault("derivation") ?? VaslCompatibleHexFactDerivation.Version,
            report.VaslCommit, report.SharedBoardMetadataBlob, id,
            [.. report.Boards.Select(board => new MapHexIndexBoard(board.Board, board.Outcome.ToString().ToLowerInvariant(), board.LosDataBlob,
                features.TryGetValue(board.Board, out var found) ? found : []))]);
        var folder = Folder(options);
        System.IO.Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, Prefix + id[("fidelity-".Length)..] + ".json");
        File.WriteAllText(path, index.ToJson());
        return path;
    }

    /// <summary>The newest saved index, or null when none has been written.</summary>
    public MapHexIndex? Latest()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return null;
        }

        var path = System.IO.Directory.GetFiles(Directory, Prefix + "*.json").Order(StringComparer.Ordinal).LastOrDefault();
        if (path is null)
        {
            return null;
        }

        var written = File.GetLastWriteTimeUtc(path);
        lock (gate)
        {
            if (kept.Path == path && kept.Written == written)
            {
                return kept.Index;
            }
        }

        MapHexIndex? index;
        try
        {
            index = MapHexIndex.FromJson(File.ReadAllText(path));
        }
        catch (System.Text.Json.JsonException)
        {
            index = null;
        }

        lock (gate)
        {
            kept = (path, written, index);
        }

        return index;
    }
}
