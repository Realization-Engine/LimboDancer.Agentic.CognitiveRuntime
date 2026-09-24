using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Geometry;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>The source identity an oracle fixture was produced from (VASL Board Ingestion Design, section 9.2).</summary>
public sealed record OracleSource(string Board, string VaslCommit, string LosDataBlob, string MetadataBlob, string SharedBoardMetadataBlob, string HarnessVersion);

public sealed record F2Result(IReadOnlyList<string> Differences, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public bool Passed => Differences.Count == 0 && Diagnostics.Count == 0;
}

/// <summary>
/// The F2 hex-fact fidelity check (ASL-MAP-041): derived Hex Facts compared field by field with the facts VASL's own
/// classes compute, as written by the <c>src/ASL/tools/vasl-hexfact-oracle</c> harness.
/// </summary>
public static class HexFactFidelity
{
    /// <summary>Derives Hex Facts for an ingested board, with its metadata's hexside annotations.</summary>
    public static HexFactSet Derive(IngestedBoard board, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(board);
        var metadata = board.Metadata;
        var annotations = new HexsideAnnotations(
            ToDictionary(metadata.Slopes),
            ToDictionary(metadata.RailroadEmbankments),
            ToDictionary(metadata.PartialOrchards));
        return VaslCompatibleHexFactDerivation.Derive(board.Grid, catalog, annotations);
    }

    /// <summary>The fixture file name for a board, such as <c>bd01.hexfacts.json.gz</c>.</summary>
    public static string FixtureFileName(BoardRef board)
    {
        ArgumentNullException.ThrowIfNull(board);
        return board.Value + ".hexfacts.json.gz";
    }

    /// <summary>Compares with the board's fixture in a directory, or returns null when there is no fixture.</summary>
    public static F2Result? CompareWithFixture(IngestedBoard board, HexFactSet derived, string? fixtureDirectory)
    {
        ArgumentNullException.ThrowIfNull(board);
        var path = fixtureDirectory is null ? null : Path.Combine(fixtureDirectory, FixtureFileName(board.Board));
        if (path is null || !File.Exists(path))
        {
            return null;
        }

        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var fixture = JsonDocument.Parse(gzip);
        return Compare(board, derived, fixture);
    }

    public static OracleSource ReadSource(JsonDocument fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        var root = fixture.RootElement;
        return new OracleSource(
            root.GetProperty("board").GetString()!,
            root.GetProperty("vaslCommit").GetString()!,
            root.GetProperty("losDataBlob").GetString()!,
            root.GetProperty("metadataBlob").GetString()!,
            root.GetProperty("sharedBoardMetadataBlob").GetString()!,
            root.GetProperty("harnessVersion").GetString()!);
    }

    /// <summary>
    /// Compares derived facts with an oracle fixture. A fixture produced from different source bytes is reported as
    /// <c>F2-SOURCE-MISMATCH</c> and is never compared, so it cannot pass by accident.
    /// </summary>
    public static F2Result Compare(IngestedBoard board, HexFactSet derived, JsonDocument fixture)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(derived);
        var source = ReadSource(fixture);
        var provenance = board.Provenance;
        var expectedMetadataBlob = provenance.Metadata.IndexBlob ?? provenance.Metadata.ContentBlob;
        var expectedSharedBlob = provenance.SharedBoardMetadata.IndexBlob ?? provenance.SharedBoardMetadata.ContentBlob;
        if (source.Board != board.Board.Value || source.LosDataBlob != provenance.LosData.ContentBlob
            || source.MetadataBlob != expectedMetadataBlob || source.SharedBoardMetadataBlob != expectedSharedBlob)
        {
            return new F2Result([], [new MapDiagnostic("F2-SOURCE-MISMATCH", MapDiagnosticSeverity.Error,
                $"The fixture was produced from {source.Board} LOSData {source.LosDataBlob}, metadata {source.MetadataBlob}, catalog {source.SharedBoardMetadataBlob}; "
                + $"the board is {board.Board.Value} LOSData {provenance.LosData.ContentBlob}, metadata {expectedMetadataBlob}, catalog {expectedSharedBlob}.")]);
        }

        var differences = new List<string>();
        var expectedHexes = fixture.RootElement.GetProperty("hexes").EnumerateArray().ToArray();
        if (expectedHexes.Length != derived.Hexes.Count)
        {
            differences.Add($"hex count: expected {expectedHexes.Length}, derived {derived.Hexes.Count}");
        }

        foreach (var expected in expectedHexes)
        {
            var name = expected.GetProperty("hex").GetString()!;
            var index = new HexIndex(expected.GetProperty("col").GetInt32(), expected.GetProperty("row").GetInt32());
            if (!derived.Geometry.Contains(index))
            {
                differences.Add($"{name}: not on the derived board");
                continue;
            }

            CompareHex(name, expected, derived[index], differences);
        }

        return new F2Result(differences, []);
    }

    private static void CompareHex(string name, JsonElement expected, HexFacts actual, List<string> differences)
    {
        Check(differences, name, "hex", expected.GetProperty("hex").GetString(), actual.Hex.ToString());
        Check(differences, name, "baseLevel", expected.GetProperty("baseLevel").GetInt32(), actual.BaseLevel);
        Check(differences, name, "stairway", expected.GetProperty("stairway").GetBoolean(), actual.Stairway);
        CompareLocation(differences, name + ".center", expected.GetProperty("center"), actual.Center);

        var expectedLocations = expected.GetProperty("locations").EnumerateArray().ToArray();
        Check(differences, name, "locations.count", expectedLocations.Length, actual.Locations.Count);
        for (var index = 0; index < Math.Min(expectedLocations.Length, actual.Locations.Count); index++)
        {
            CompareLocation(differences, $"{name}.locations[{index}]", expectedLocations[index], actual.Locations[index]);
        }

        var expectedBridge = expected.GetProperty("bridge");
        if (expectedBridge.ValueKind == JsonValueKind.Null || actual.Bridge is null)
        {
            Check(differences, name, "bridge", expectedBridge.ValueKind == JsonValueKind.Null ? "none" : "present", actual.Bridge is null ? "none" : "present");
        }
        else
        {
            Check(differences, name, "bridge.terrain", Name(expectedBridge.GetProperty("terrain")), actual.Bridge.Terrain?.Name);
            Check(differences, name, "bridge.roadLevel", expectedBridge.GetProperty("roadLevel").GetInt32(), actual.Bridge.RoadLevel);
        }

        var expectedSides = expected.GetProperty("hexsides").EnumerateArray().ToArray();
        foreach (var side in expectedSides)
        {
            var number = side.GetProperty("side").GetInt32();
            var facts = actual.Hexsides[number];
            var prefix = $"{name}.hexsides[{number}]";
            Check(differences, prefix, "onMap", side.GetProperty("onMap").GetBoolean(), facts.OnMap);
            Check(differences, prefix, "terrain", Name(side.GetProperty("terrain")), facts.Terrain?.Name);
            Check(differences, prefix, "hexsideTerrain", Name(side.GetProperty("hexsideTerrain")), facts.HexsideTerrain?.Name);
            Check(differences, prefix, "cliff", side.GetProperty("cliff").GetBoolean(), facts.Cliff);
            Check(differences, prefix, "slope", side.GetProperty("slope").GetBoolean(), facts.Slope);
            Check(differences, prefix, "railroadEmbankment", side.GetProperty("railroadEmbankment").GetBoolean(), facts.RailroadEmbankment);
            Check(differences, prefix, "partialOrchard", side.GetProperty("partialOrchard").GetBoolean(), facts.PartialOrchard);
            Check(differences, prefix, "depressionTerrain", Name(side.GetProperty("depressionTerrain")), facts.DepressionTerrain?.Name);
        }
    }

    private static void CompareLocation(List<string> differences, string prefix, JsonElement expected, LocationFacts actual)
    {
        Check(differences, prefix, "level", expected.GetProperty("level").GetInt32(), actual.Level);
        Check(differences, prefix, "terrain", Name(expected.GetProperty("terrain")), actual.Terrain?.Name);
        Check(differences, prefix, "depressionTerrain", Name(expected.GetProperty("depressionTerrain")), actual.DepressionTerrain?.Name);
    }

    private static void Check<T>(List<string> differences, string prefix, string field, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            differences.Add(string.Create(CultureInfo.InvariantCulture, $"{prefix}.{field}: expected {expected}, derived {actual}"));
        }
    }

    private static string? Name(JsonElement element) => element.ValueKind == JsonValueKind.Null ? null : element.GetString();

    private static Dictionary<HexName, IReadOnlySet<HexsideDirection>> ToDictionary(IReadOnlyList<HexsideFlags> flags) =>
        flags.ToDictionary(item => item.Hex, item => (IReadOnlySet<HexsideDirection>)item.Sides.ToHashSet());
}
