using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps.Composition;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>
/// A named map of placed VASL boards, as written in oracle scenario files and Studio map links: board@column,row,
/// optionally /r for a reversed board and [Rule,...] for LOS scenario-specific rules in the order VASL applies them.
/// </summary>
public sealed partial record VaslScenario(string Name, IReadOnlyList<BoardPlacement> Placements)
{
    /// <summary>Parses one placement such as <c>01@0,0/r[NoStairwells,BrushToOpenGround]</c>.</summary>
    public static bool TryParsePlacement(string text, out BoardPlacement? placement)
    {
        placement = null;
        var match = PlacementPattern().Match(text ?? string.Empty);
        if (!match.Success || !BoardRef.TryParse("bd" + match.Groups[1].Value, out var board))
        {
            return false;
        }

        var rules = match.Groups[5].Success && match.Groups[5].Value.Length > 0
            ? match.Groups[5].Value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];
        placement = new BoardPlacement(board, int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture),
            int.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture), match.Groups[4].Success, rules);
        return true;
    }

    /// <summary>Parses whitespace-separated placements, or returns null if any is malformed.</summary>
    public static IReadOnlyList<BoardPlacement>? ParsePlacements(string text)
    {
        var placements = new List<BoardPlacement>();
        foreach (var token in (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParsePlacement(token, out var placement))
            {
                return null;
            }

            placements.Add(placement!);
        }

        return placements;
    }

    /// <summary>Parses a scenario file: "name: placement ..." lines, with blank lines and # comments ignored.</summary>
    public static IReadOnlyList<VaslScenario> ParseFile(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var scenarios = new List<VaslScenario>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var colon = line.IndexOf(':', StringComparison.Ordinal);
            var placements = colon > 0 ? ParsePlacements(line[(colon + 1)..]) : null;
            if (placements is null || placements.Count == 0)
            {
                throw new FormatException($"Bad scenario line '{line}'.");
            }

            scenarios.Add(new VaslScenario(line[..colon].Trim(), placements));
        }

        return scenarios;
    }

    [GeneratedRegex(@"^([A-Za-z0-9]+)@(\d+),(\d+)(/r)?(?:\[([A-Za-z0-9_.,]*)\])?$", RegexOptions.CultureInvariant)]
    private static partial Regex PlacementPattern();
}

/// <summary>A map built from VASL boards: the boards as ingested and the built map, or diagnostics.</summary>
public sealed record VaslMapImport(VaslMap? Map, IReadOnlyList<IngestedBoard> Boards, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Succeeded => Map is not null;
}

/// <summary>
/// Ingests the placed boards from a VASL checkout and builds the map as VASL's runtime does (VASL Board Ingestion
/// Design, section 11), and compares it with an oracle scenario fixture (F2 for maps).
/// </summary>
public static class VaslMapImporter
{
    public static VaslMapImport Build(VaslSource vasl, TerrainCatalog catalog, LosSsRuleSet rules, IReadOnlyList<BoardPlacement> placements)
    {
        ArgumentNullException.ThrowIfNull(vasl);
        ArgumentNullException.ThrowIfNull(placements);
        var boards = new Dictionary<BoardRef, IngestedBoard>();
        var diagnostics = new List<MapDiagnostic>();
        foreach (var placement in placements)
        {
            if (boards.ContainsKey(placement.Board))
            {
                continue;
            }

            if (placement.Board.Kind != BoardRefKind.Vasl)
            {
                diagnostics.Add(new MapDiagnostic("VASL-MAP-005", MapDiagnosticSeverity.Error, $"{placement.Board} is not a VASL board."));
                return new VaslMapImport(null, [], diagnostics);
            }

            var import = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, placement.Board.VaslBoardName), catalog);
            if (import.Board is not { } board)
            {
                diagnostics.AddRange(import.Diagnostics.Where(diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error || diagnostic.Code == "VASL-SCOPE-001"));
                diagnostics.Add(new MapDiagnostic("VASL-MAP-005", MapDiagnosticSeverity.Error, $"{placement.Board} could not be ingested."));
                return new VaslMapImport(null, [.. boards.Values], diagnostics);
            }

            boards[placement.Board] = board;
        }

        return Build(boards.Values.ToArray(), catalog, rules, placements, diagnostics);
    }

    /// <summary>Builds a map from boards already ingested.</summary>
    public static VaslMapImport Build(IReadOnlyList<IngestedBoard> boards, TerrainCatalog catalog, LosSsRuleSet rules, IReadOnlyList<BoardPlacement> placements) =>
        Build(boards, catalog, rules, placements, []);

    private static VaslMapImport Build(IReadOnlyList<IngestedBoard> boards, TerrainCatalog catalog, LosSsRuleSet rules, IReadOnlyList<BoardPlacement> placements,
        List<MapDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(boards);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(rules);
        var byRef = boards.ToDictionary(board => board.Board);
        var placed = new List<PlacedBoard>();
        foreach (var placement in placements)
        {
            if (!byRef.TryGetValue(placement.Board, out var board))
            {
                diagnostics.Add(new MapDiagnostic("VASL-MAP-005", MapDiagnosticSeverity.Error, $"{placement.Board} was not supplied."));
                return new VaslMapImport(null, boards, diagnostics);
            }

            placed.Add(new PlacedBoard(placement, board.Grid, HexFactFidelity.Annotations(board.Metadata)));
        }

        var result = VaslMapBuilder.Build(placed, catalog, rules);
        diagnostics.AddRange(result.Diagnostics);
        return new VaslMapImport(result.Map, boards, diagnostics);
    }

    /// <summary>The fixture file name for a scenario, such as <c>bd01r.scenario.hexfacts.json.gz</c>.</summary>
    public static string FixtureFileName(string scenario) => scenario + ".scenario.hexfacts.json.gz";

    /// <summary>
    /// Compares a built map with an oracle scenario fixture, hex by hex in map position. A fixture produced from other
    /// boards, placements, rules, or source bytes is <c>F2-SOURCE-MISMATCH</c> and is never compared.
    /// </summary>
    public static F2Result Compare(VaslMapImport import, IReadOnlyList<BoardPlacement> placements, JsonDocument fixture)
    {
        ArgumentNullException.ThrowIfNull(import);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(fixture);
        if (import.Map is not { } map)
        {
            return new F2Result([], [new MapDiagnostic("F2-MAP-FAILED", MapDiagnosticSeverity.Error, "The map could not be built.")]);
        }

        var root = fixture.RootElement;
        var expectedBoards = root.GetProperty("boards").EnumerateArray().ToArray();
        var ordered = placements.OrderBy(placement => placement.Row).ThenBy(placement => placement.Column).ToArray();
        var mismatch = expectedBoards.Length != ordered.Length;
        for (var index = 0; !mismatch && index < ordered.Length; index++)
        {
            var expected = expectedBoards[index];
            var placement = ordered[index];
            var board = import.Boards.Single(item => item.Board == placement.Board);
            mismatch = expected.GetProperty("board").GetString() != placement.Board.Value
                || expected.GetProperty("column").GetInt32() != placement.Column
                || expected.GetProperty("row").GetInt32() != placement.Row
                || expected.GetProperty("reversed").GetBoolean() != placement.Reversed
                || !expected.GetProperty("rules").EnumerateArray().Select(rule => rule.GetString()).SequenceEqual(placement.Rules)
                || expected.GetProperty("losDataBlob").GetString() != board.Provenance.LosData.ContentBlob
                || expected.GetProperty("metadataBlob").GetString() != (board.Provenance.Metadata.IndexBlob ?? board.Provenance.Metadata.ContentBlob);
        }

        var shared = import.Boards.Count > 0 ? import.Boards[0].Provenance.SharedBoardMetadata : null;
        if (mismatch || shared is null || root.GetProperty("sharedBoardMetadataBlob").GetString() != (shared.IndexBlob ?? shared.ContentBlob))
        {
            return new F2Result([], [new MapDiagnostic("F2-SOURCE-MISMATCH", MapDiagnosticSeverity.Error,
                $"The fixture for {root.GetProperty("scenario").GetString()} was produced from other boards, placements, rules, or source bytes.")]);
        }

        var differences = new List<string>();
        Check(differences, "widthInHexes", root.GetProperty("widthInHexes").GetInt32(), map.Geometry.WidthInHexes);
        Check(differences, "heightInHexes", root.GetProperty("heightInHexes").GetInt32(), map.Geometry.HeightInHexes);
        Check(differences, "gridWidth", root.GetProperty("gridWidth").GetInt32(), map.Geometry.GridWidth);
        Check(differences, "gridHeight", root.GetProperty("gridHeight").GetInt32(), map.Geometry.GridHeight);
        var expectedHexes = root.GetProperty("hexes").EnumerateArray().ToArray();
        Check(differences, "hex count", expectedHexes.Length, map.Facts.Hexes.Count);
        foreach (var expected in expectedHexes)
        {
            var index = new HexIndex(expected.GetProperty("col").GetInt32(), expected.GetProperty("row").GetInt32());
            var label = $"[{index.Column},{index.Row}]{expected.GetProperty("hex").GetString()}";
            if (!map.Geometry.Contains(index))
            {
                differences.Add($"{label}: not on the derived map");
                continue;
            }

            HexFactFidelity.CompareHex(label, expected, map.Facts[index], differences);
        }

        return new F2Result(differences, []);
    }

    /// <summary>Compares with the scenario's fixture in a directory, or returns null when there is no fixture.</summary>
    public static F2Result? CompareWithFixture(VaslMapImport import, VaslScenario scenario, string fixtureDirectory)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        var path = Path.Combine(fixtureDirectory, FixtureFileName(scenario.Name));
        if (!File.Exists(path))
        {
            return null;
        }

        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var fixture = JsonDocument.Parse(gzip);
        return Compare(import, scenario.Placements, fixture);
    }

    private static void Check(List<string> differences, string field, int expected, int actual)
    {
        if (expected != actual)
        {
            differences.Add(FormattableString.Invariant($"{field}: expected {expected}, derived {actual}"));
        }
    }
}
