using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Vasl;

/// <summary>How one board came out of a batch run.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<BatchOutcome>))]
public enum BatchOutcome
{
    /// <summary>F1, F2, and every additional check passed.</summary>
    Verified,

    /// <summary>Ingested, but F2 has no fixture or a check did not pass.</summary>
    Ingested,

    /// <summary>In scope, or of unknown scope, but ingestion refused it with an error diagnostic.</summary>
    Failed,

    /// <summary>Declined by design (<c>VASL-SCOPE-001</c>).</summary>
    OutOfScope,
}

/// <summary>F2 as a batch records it: <c>pass</c>, <c>fail</c>, or <c>no-fixture</c>, with the differences when it fails.</summary>
public sealed record BatchF2(string Status, IReadOnlyList<string> Differences, IReadOnlyList<MapDiagnostic> Diagnostics)
{
    public const string Pass = "pass";
    public const string Fail = "fail";
    public const string NoFixture = "no-fixture";
}

/// <summary>Stage timings in milliseconds for one board.</summary>
public sealed record BatchTimings(double Import, double Derive, double F2, double Checks)
{
    public double Total => Import + Derive + F2 + Checks;
}

/// <summary>One board's result in a batch run (Requirements scenario M2).</summary>
public sealed record BatchBoardResult(
    string Board,
    BatchOutcome Outcome,
    string? Reason,
    string? LosDataBlob,
    string? MetadataBlob,
    F1Status? F1,
    string? F1Detail,
    BatchF2? F2,
    IReadOnlyList<FidelityCheck> Checks,
    IReadOnlyList<MapDiagnostic> Diagnostics,
    BatchTimings Timings);

/// <summary>Counts of a report's boards by outcome.</summary>
public sealed record BatchSummary(int Boards, int Verified, int Ingested, int Failed, int OutOfScope);

/// <summary>
/// The result of one batch run: the source it read, the tool versions that judged it, and every board's result in
/// VASL board-name order. Serialized as the Studio's fidelity report.
/// </summary>
public sealed record FidelityReport(
    string ReportVersion,
    DateTimeOffset StartedAt,
    double DurationMs,
    string? VaslCommit,
    string? SharedBoardMetadataBlob,
    IReadOnlyDictionary<string, string> Versions,
    IReadOnlyList<MapDiagnostic> Diagnostics,
    IReadOnlyList<BatchBoardResult> Boards)
{
    public const string CurrentVersion = "1.0.0";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public BatchSummary Summary => new(
        Boards.Count,
        Boards.Count(board => board.Outcome == BatchOutcome.Verified),
        Boards.Count(board => board.Outcome == BatchOutcome.Ingested),
        Boards.Count(board => board.Outcome == BatchOutcome.Failed),
        Boards.Count(board => board.Outcome == BatchOutcome.OutOfScope));

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static FidelityReport FromJson(string json) =>
        JsonSerializer.Deserialize<FidelityReport>(json, JsonOptions) ?? throw new JsonException("The report is empty.");
}

/// <summary>Progress of a running batch: boards finished, total boards, and the board that just finished.</summary>
public sealed record BatchProgress(int Completed, int Total, string LastBoard);

/// <summary>Batch settings. Additional checks run on every ingested board; the Studio adds rendering checks.</summary>
public sealed class VaslBatchOptions
{
    /// <summary>Directory of F2 oracle fixtures; boards without a fixture record <c>no-fixture</c>.</summary>
    public string? OracleDirectory
    {
        get; init;
    }

    /// <summary>VASL board names to run, such as <c>01</c>; all boards when null.</summary>
    public IReadOnlyList<string>? Boards
    {
        get; init;
    }

    public int MaxParallelism { get; init; } = Environment.ProcessorCount;

    public Func<IngestedBoard, HexFactSet, TerrainCatalog, IReadOnlyList<FidelityCheck>>? AdditionalChecks
    {
        get; init;
    }

    /// <summary>Versions of the tools behind the additional checks, recorded in the report.</summary>
    public IReadOnlyDictionary<string, string> AdditionalVersions { get; init; } = new Dictionary<string, string>();
}

/// <summary>
/// Ingests every board and runs F1, F2, and any additional checks, with a result per board and no fail-fast
/// (ASL-MAP-035, VASL Board Ingestion Design section 10). Boards run in parallel; results keep board-name order.
/// </summary>
public static class VaslBatchImporter
{
    public static FidelityReport Run(VaslSource vasl, VaslBatchOptions options, IProgress<BatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vasl);
        ArgumentNullException.ThrowIfNull(options);
        var startedAt = DateTimeOffset.UtcNow;
        var watch = Stopwatch.StartNew();
        var versions = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["importer"] = VaslBoardImporter.ImporterVersion,
            ["derivation"] = VaslCompatibleHexFactDerivation.Version,
            ["report"] = FidelityReport.CurrentVersion,
        };
        foreach (var (name, version) in options.AdditionalVersions)
        {
            versions[name] = version;
        }

        var catalogResult = vasl.ReadTerrainCatalog();
        var sharedBlob = catalogResult.Catalog is null ? null : vasl.SharedBoardMetadataProvenance().ContentBlob;
        if (catalogResult.Catalog is not { } catalog)
        {
            return new FidelityReport(FidelityReport.CurrentVersion, startedAt, watch.Elapsed.TotalMilliseconds, vasl.Git?.HeadCommit, sharedBlob,
                versions, catalogResult.Diagnostics, []);
        }

        var names = (options.Boards ?? vasl.BoardNames()).ToArray();
        var results = new BatchBoardResult[names.Length];
        var completed = 0;
        Parallel.For(0, names.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, options.MaxParallelism), CancellationToken = cancellationToken },
            index =>
            {
                results[index] = RunBoard(vasl, names[index], catalog, options);
                progress?.Report(new BatchProgress(Interlocked.Increment(ref completed), names.Length, "bd" + names[index]));
            });

        return new FidelityReport(FidelityReport.CurrentVersion, startedAt, watch.Elapsed.TotalMilliseconds, vasl.Git?.HeadCommit, sharedBlob,
            versions, catalogResult.Diagnostics, results);
    }

    /// <summary>Runs one board. Unexpected exceptions become a <c>BATCH-001</c> failure so the batch continues.</summary>
    public static BatchBoardResult RunBoard(VaslSource vasl, string boardName, TerrainCatalog catalog, VaslBatchOptions options)
    {
        ArgumentNullException.ThrowIfNull(vasl);
        ArgumentNullException.ThrowIfNull(options);
        var board = "bd" + boardName;
        double import = 0, derive = 0, f2Time = 0, checksTime = 0;
        try
        {
            var watch = Stopwatch.StartNew();
            var source = VaslBoardSource.SourceDirectory(vasl, boardName);
            var result = VaslBoardImporter.Import(vasl, source, catalog);
            import = Lap(watch);
            if (result.Board is not { } ingested)
            {
                var outcome = result.OutOfScope ? BatchOutcome.OutOfScope : BatchOutcome.Failed;
                var reason = result.Diagnostics.LastOrDefault(diagnostic => diagnostic.Code == "VASL-SCOPE-001" || diagnostic.Severity == MapDiagnosticSeverity.Error)?.Message;
                // A failed board still records its source identity, so a later report can tell whether it changed.
                var losDataBlob = outcome == BatchOutcome.Failed ? Blob(source, VaslBoardSource.LosDataEntry) : null;
                var metadataBlob = outcome == BatchOutcome.Failed ? Blob(source, VaslBoardSource.MetadataEntry) : null;
                return new BatchBoardResult(board, outcome, reason, losDataBlob, metadataBlob, null, null, null, [], result.Diagnostics,
                    new BatchTimings(import, 0, 0, 0));
            }

            var facts = HexFactFidelity.Derive(ingested, catalog);
            derive = Lap(watch);
            var f2 = HexFactFidelity.CompareWithFixture(ingested, facts, options.OracleDirectory);
            f2Time = Lap(watch);
            var checks = options.AdditionalChecks?.Invoke(ingested, facts, catalog) ?? [];
            checksTime = Lap(watch);
            var batchF2 = f2 is null
                ? new BatchF2(BatchF2.NoFixture, [], [])
                : new BatchF2(f2.Passed ? BatchF2.Pass : BatchF2.Fail, f2.Differences, f2.Diagnostics);
            var verified = ingested.F1.Passed && f2 is { Passed: true } && checks.All(check => check.Passed);
            var why = verified ? null : WhyNotVerified(ingested, batchF2, checks);
            return new BatchBoardResult(board, verified ? BatchOutcome.Verified : BatchOutcome.Ingested, why,
                ingested.Provenance.LosData.ContentBlob, ingested.Provenance.Metadata.ContentBlob, ingested.F1.Status, ingested.F1.Detail,
                batchF2, checks, result.Diagnostics, new BatchTimings(import, derive, f2Time, checksTime));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new BatchBoardResult(board, BatchOutcome.Failed, exception.Message, null, null, null, null, null, [],
                [new MapDiagnostic("BATCH-001", MapDiagnosticSeverity.Error, $"{board}: {exception.GetType().Name}: {exception.Message}")],
                new BatchTimings(import, derive, f2Time, checksTime));
        }
    }

    private static string WhyNotVerified(IngestedBoard board, BatchF2 f2, IReadOnlyList<FidelityCheck> checks)
    {
        var reasons = new List<string>();
        if (!board.F1.Passed)
        {
            reasons.Add("F1 " + board.F1.Status);
        }

        if (f2.Status != BatchF2.Pass)
        {
            reasons.Add(f2.Status == BatchF2.NoFixture ? "no F2 fixture" : $"F2 failed ({f2.Differences.Count} differences, {f2.Diagnostics.Count} diagnostics)");
        }

        reasons.AddRange(checks.Where(check => !check.Passed).Select(check => $"{check.Name}: {check.Detail}"));
        return string.Join("; ", reasons);
    }

    private static string? Blob(VaslBoardSource source, string entry) => source.ReadEntry(entry) is { } bytes ? GitBlob.Sha(bytes) : null;

    private static double Lap(Stopwatch watch)
    {
        var elapsed = watch.Elapsed.TotalMilliseconds;
        watch.Restart();
        return Math.Round(elapsed, 1);
    }
}
