using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Los;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>An LOS oracle fixture: its file, the scenario it was built from (null for one board), its placements, and its pairs.</summary>
public sealed record LosFixture(string Name, string? Scenario, IReadOnlyList<BoardPlacement> Placements, IReadOnlyList<LosFixturePair> Pairs);

/// <summary>
/// One pair of an LOS oracle fixture: the locations and VASL's own result for them, or the name of the exception VASL's
/// LOS threw on this line (harness 1.1.0). From harness 1.2.0 a pair also has VASL's hindrance breakdown and first
/// hindrance point, and a pair from a hexside location says which of its points the line starts at.
/// </summary>
public sealed record LosFixturePair(BoardLocation Source, BoardLocation Target, bool Blocked, string? BlockedHex, int Range, int Hindrance, string Reason)
{
    public string? VaslError
    {
        get; init;
    }

    /// <summary>VASL's <c>LOSResult.mapHindrances</c>: the largest map hindrance at each range, in range order.</summary>
    public IReadOnlyList<LosHindrance> Hindrances
    {
        get; init;
    } = [];

    /// <summary>VASL's <c>LOSResult.firstHindranceAt</c>, or null.</summary>
    public GridPoint? FirstHindranceAt
    {
        get; init;
    }

    /// <summary>The board-relative hex of <see cref="FirstHindranceAt"/>, as <c>bdNN:HEX</c>, or null.</summary>
    public string? FirstHindranceHex
    {
        get; init;
    }

    /// <summary>Whether the line starts at the source's auxiliary point (<c>useAuxSourceLOSPoint</c>); null on a center fixture.</summary>
    public bool? SourceAux
    {
        get; init;
    }
}

/// <summary>
/// The comparison of an LOS read with an oracle fixture (LOS Design, sections 7 and 8): the pairs, how many the read
/// answered, how many of those agree with VASL, the unanswered pairs by the rule that is not reproduced, and each
/// disagreement.
/// </summary>
public sealed record LosFidelityResult(
    string Fixture, int Pairs, int Answered, int Agreed, IReadOnlyDictionary<string, int> Unsupported, IReadOnlyList<string> Disagreements)
{
    /// <summary>Whether every answered pair agrees with VASL; unanswered pairs never fail the check.</summary>
    public bool Passed => Disagreements.Count == 0;
}

/// <summary>
/// Reads the LOS oracle fixtures that <c>src/ASL/tools/vasl-hexfact-oracle</c> writes with VASL's own
/// <c>Map.LOS</c>, and compares an LOS read with them pair by pair: on every answered pair, whether LOS is blocked,
/// the hex where it is blocked, the range, the hindrance total, the reason text, the hindrance breakdown, and the first
/// hindrance point and its hex must all agree. A hexside pair is read from the source point VASL used.
/// </summary>
public static class LosFidelity
{
    /// <summary>Reads a gzipped LOS fixture, center or hexside.</summary>
    public static LosFixture Read(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var document = JsonDocument.Parse(gzip);
        var root = document.RootElement;
        BoardPlacement[] placements = [.. root.GetProperty("boards").EnumerateArray().Select(board => new BoardPlacement(
            BoardRef.Parse(board.GetProperty("board").GetString()!), board.GetProperty("column").GetInt32(), board.GetProperty("row").GetInt32(),
            board.GetProperty("reversed").GetBoolean(), []))];
        LosFixturePair[] pairs = [.. root.GetProperty("pairs").EnumerateArray().Select(ReadPair)];
        var name = Path.GetFileName(path);
        return new LosFixture(name[..name.IndexOf('.', StringComparison.Ordinal)], root.GetProperty("scenario").GetString(), placements, pairs);
    }

    private static LosFixturePair ReadPair(JsonElement pair) =>
        new(BoardLocation.Parse(pair.GetProperty("source").GetString()!), BoardLocation.Parse(pair.GetProperty("target").GetString()!),
            pair.GetProperty("blocked").GetBoolean(), pair.GetProperty("blockedHex").GetString(), pair.GetProperty("range").GetInt32(),
            pair.GetProperty("hindrance").GetInt32(), pair.GetProperty("reason").GetString() ?? string.Empty)
        {
            VaslError = pair.TryGetProperty("vaslError", out var error) ? error.GetString() : null,
            Hindrances = pair.TryGetProperty("hindrances", out var hindrances)
                ? [.. hindrances.EnumerateArray().Select(entry => new LosHindrance(entry[0].GetInt32(), entry[1].GetDouble()))]
                : [],
            FirstHindranceAt = pair.TryGetProperty("firstHindranceAt", out var at) && at.ValueKind == JsonValueKind.Array
                ? new GridPoint(at[0].GetInt32(), at[1].GetInt32())
                : null,
            FirstHindranceHex = pair.TryGetProperty("firstHindranceHex", out var hex) ? hex.GetString() : null,
            SourceAux = pair.TryGetProperty("sourceAux", out var aux) ? aux.GetBoolean() : null,
        };

    /// <summary>The center LOS fixtures in an oracle directory and its <c>Scenarios</c> folder.</summary>
    public static IReadOnlyList<string> Find(string oracleDirectory)
    {
        ArgumentNullException.ThrowIfNull(oracleDirectory);
        var scenarios = Path.Combine(oracleDirectory, "Scenarios");
        return
        [
            .. Files(oracleDirectory, "*.los.json.gz"),
            .. Files(scenarios, "*.scenario.los.json.gz"),
        ];
    }

    /// <summary>The hexside LOS fixtures (<c>bdNN.los-hexside.json.gz</c>) in an oracle directory.</summary>
    public static IReadOnlyList<string> FindHexside(string oracleDirectory)
    {
        ArgumentNullException.ThrowIfNull(oracleDirectory);
        return [.. Files(oracleDirectory, "*.los-hexside.json.gz")];
    }

    private static IEnumerable<string> Files(string directory, string pattern) =>
        Directory.Exists(directory) ? Directory.GetFiles(directory, pattern).Order(StringComparer.Ordinal) : [];

    /// <summary>Compares the read on a map with every pair of a fixture.</summary>
    public static LosFidelityResult Compare(LosMap map, LosFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(fixture);
        var answered = 0;
        var agreed = 0;
        var unsupported = new Dictionary<string, int>(StringComparer.Ordinal);
        var disagreements = new List<string>();
        foreach (var pair in fixture.Pairs)
        {
            var aim = pair.SourceAux == true ? LosAim.AuxiliaryPoint : LosAim.LosPoint;
            var result = LosCalculator.Check(map, pair.Source, aim, pair.Target, LosAim.LosPoint);
            var name = pair.SourceAux == true ? $"{pair.Source} (aux) -> {pair.Target}" : $"{pair.Source} -> {pair.Target}";

            // Where VASL's own LOS fails, the read must not answer: it names VASL's failure, or an unreproduced rule the
            // line meets before it.
            if (pair.VaslError is { } error)
            {
                if (result.IsAnswered)
                {
                    disagreements.Add($"{name}: VASL fails ({error}), read {result.Status} {result.Reason}");
                }
                else
                {
                    unsupported[result.Reason] = unsupported.GetValueOrDefault(result.Reason) + 1;
                }

                continue;
            }

            if (!result.IsAnswered)
            {
                unsupported[result.Reason] = unsupported.GetValueOrDefault(result.Reason) + 1;
                continue;
            }

            answered++;
            var expected = (pair.Blocked, pair.BlockedHex, pair.Range, pair.Hindrance, pair.Reason, Breakdown(pair.Hindrances),
                First(pair.FirstHindranceAt, pair.FirstHindranceHex));
            var actual = (result.IsBlocked == true, result.BlockedAt is { Board: { } board, Hex: { } hex } ? $"{board}:{hex}" : null,
                result.Range, result.Hindrance, result.Reason, Breakdown(result.Hindrances),
                First(result.FirstHindranceAt?.Point, result.FirstHindranceAt is { Board: { } firstBoard, Hex: { } firstHex } ? $"{firstBoard}:{firstHex}" : null));
            if (expected == actual)
            {
                agreed++;
            }
            else
            {
                disagreements.Add($"{name}: VASL {expected}, read {actual}");
            }
        }

        return new LosFidelityResult(fixture.Name, fixture.Pairs.Count, answered, agreed, unsupported, disagreements);
    }

    // The breakdown as text, so the tuples compare by value: "range:value" in range order.
    private static string Breakdown(IReadOnlyList<LosHindrance> hindrances) =>
        string.Join(" ", hindrances.Select(entry => string.Create(CultureInfo.InvariantCulture, $"{entry.Range}:{entry.Value}")));

    private static string First(GridPoint? point, string? hex) => point is { } at ? $"{hex} ({at.X}, {at.Y})" : "none";
}
