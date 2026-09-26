using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Los;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>An LOS oracle fixture: its file, the scenario it was built from (null for one board), its placements, and its pairs.</summary>
public sealed record LosFixture(string Name, string? Scenario, IReadOnlyList<BoardPlacement> Placements, IReadOnlyList<LosFixturePair> Pairs);

/// <summary>
/// One pair of an LOS oracle fixture: the locations and VASL's own result for them, or the name of the exception VASL's
/// LOS threw on this line (harness 1.1.0).
/// </summary>
public sealed record LosFixturePair(BoardLocation Source, BoardLocation Target, bool Blocked, string? BlockedHex, int Range, int Hindrance, string Reason)
{
    public string? VaslError
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
/// the hex where it is blocked, the range, the hindrance total, and the reason text must all agree.
/// </summary>
public static class LosFidelity
{
    /// <summary>Reads a gzipped LOS fixture.</summary>
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
        LosFixturePair[] pairs = [.. root.GetProperty("pairs").EnumerateArray().Select(pair => new LosFixturePair(
            BoardLocation.Parse(pair.GetProperty("source").GetString()!), BoardLocation.Parse(pair.GetProperty("target").GetString()!),
            pair.GetProperty("blocked").GetBoolean(), pair.GetProperty("blockedHex").GetString(), pair.GetProperty("range").GetInt32(),
            pair.GetProperty("hindrance").GetInt32(), pair.GetProperty("reason").GetString() ?? string.Empty)
            {
                VaslError = pair.TryGetProperty("vaslError", out var error) ? error.GetString() : null,
            })];
        var name = Path.GetFileName(path);
        return new LosFixture(name[..name.IndexOf('.', StringComparison.Ordinal)], root.GetProperty("scenario").GetString(), placements, pairs);
    }

    /// <summary>The LOS fixtures in an oracle directory and its <c>Scenarios</c> folder.</summary>
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
            var result = LosCalculator.Check(map, pair.Source, pair.Target);

            // Where VASL's own LOS fails, the read must not answer: it names VASL's failure, or an unreproduced rule the
            // line meets before it.
            if (pair.VaslError is { } error)
            {
                if (result.IsAnswered)
                {
                    disagreements.Add($"{pair.Source} -> {pair.Target}: VASL fails ({error}), read {result.Status} {result.Reason}");
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
            var expected = (pair.Blocked, pair.BlockedHex, pair.Range, pair.Hindrance, pair.Reason);
            var actual = (result.IsBlocked == true, result.BlockedAt is { Board: { } board, Hex: { } hex } ? $"{board}:{hex}" : null,
                result.Range, result.Hindrance, result.Reason);
            if (expected == actual)
            {
                agreed++;
            }
            else
            {
                disagreements.Add($"{pair.Source} -> {pair.Target}: VASL {expected}, read {actual}");
            }
        }

        return new LosFidelityResult(fixture.Name, fixture.Pairs.Count, answered, agreed, unsupported, disagreements);
    }
}
