using System.Globalization;
using System.Text.RegularExpressions;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Services;

/// <summary>Runs a fidelity batch over the configured source. The VASL implementation adds rendering checks.</summary>
public interface IFidelityBatch
{
    /// <summary>False when no source is configured, so no batch can run.</summary>
    public bool IsAvailable
    {
        get;
    }

    /// <summary>Runs every board; with <paramref name="includeF3"/>, also vectorizes and recompiles each one.</summary>
    public FidelityReport Run(bool includeF3, IProgress<BatchProgress> progress, CancellationToken cancellationToken);
}

/// <summary>The VASL fidelity batch: F1, F2 against the oracle fixtures, and rendering checks for every board.</summary>
public sealed class VaslFidelityBatch(StudioOptions options) : IFidelityBatch
{
    private readonly VaslSource? vasl = VaslSource.TryOpen(options?.VaslRoot);

    public bool IsAvailable => vasl is not null;

    public FidelityReport Run(bool includeF3, IProgress<BatchProgress> progress, CancellationToken cancellationToken)
    {
        var source = vasl ?? throw new InvalidOperationException("No VASL checkout is configured.");
        var versions = new Dictionary<string, string> { ["renderer"] = BoardRenderer.RendererVersion };
        if (includeF3)
        {
            versions["compiler"] = FeatureCompiler.Version;
            versions["vectorizer"] = Vectorizer.Version;
        }

        return VaslBatchImporter.Run(source, new VaslBatchOptions
        {
            OracleDirectory = options.ResolveOracleFixtures(),
            AdditionalChecks = (board, facts, catalog) =>
            {
                var checks = new List<FidelityCheck>(BoardRenderChecks.Run(BoardRenderInput.Create(board.Board, board.Board.Value, board.Grid, catalog, facts)));
                if (includeF3)
                {
                    checks.AddRange(F3Checks.Run(new VectorizerSource(board.Board, board.Provenance.LosData.ContentBlob, board.Grid, facts,
                        HexFactFidelity.Annotations(board.Metadata), board.Provenance.SharedBoardMetadata.ContentBlob), catalog));
                }

                return checks;
            },
            AdditionalVersions = versions,
        }, progress, cancellationToken);
    }
}

/// <summary>A saved report's identity and summary, for listing without loading every file.</summary>
public sealed record StoredReport(string Id, DateTimeOffset StartedAt, BatchSummary Summary, string? VaslCommit);

/// <summary>Saves fidelity reports as JSON under <c>{CacheRoot}/fidelity</c>, never in the repository (ASL-MAP-073).</summary>
public sealed partial class FidelityReportStore(StudioOptions options)
{
    private readonly Lock gate = new();

    public string Directory => Path.Combine(options.ResolveCacheRoot(), "fidelity");

    public string Save(FidelityReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var id = "fidelity-" + report.StartedAt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture);
        lock (gate)
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(PathOf(id), report.ToJson());
        }

        return id;
    }

    /// <summary>Saved reports, newest first.</summary>
    public IReadOnlyList<StoredReport> List()
    {
        if (!System.IO.Directory.Exists(Directory))
        {
            return [];
        }

        var reports = new List<StoredReport>();
        foreach (var path in System.IO.Directory.GetFiles(Directory, "fidelity-*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(path);
            if (TryLoad(id) is { } report)
            {
                reports.Add(new StoredReport(id, report.StartedAt, report.Summary, report.VaslCommit));
            }
        }

        return [.. reports.OrderByDescending(report => report.Id, StringComparer.Ordinal)];
    }

    /// <summary>A saved report, or null when the id is malformed, missing, or unreadable.</summary>
    public FidelityReport? TryLoad(string id)
    {
        if (!IsValidId(id) || !File.Exists(PathOf(id)))
        {
            return null;
        }

        try
        {
            return FidelityReport.FromJson(File.ReadAllText(PathOf(id)));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>The raw JSON of a saved report, or null.</summary>
    public string? ReadJson(string id) => IsValidId(id) && File.Exists(PathOf(id)) ? File.ReadAllText(PathOf(id)) : null;

    public static bool IsValidId(string? id) => id is not null && IdPattern().IsMatch(id);

    private string PathOf(string id) => Path.Combine(Directory, id + ".json");

    [GeneratedRegex("^fidelity-[0-9]{8}T[0-9]{6}Z$")]
    private static partial Regex IdPattern();
}

public enum FidelityJobState
{
    Idle,
    Running,
    Completed,
    Canceled,
    Failed,
}

/// <summary>
/// Runs one fidelity batch at a time in the background, with progress and cancellation, and keeps the latest
/// report (Architecture and Rendering Design, section 4.3). Boards run by the batch are not cached for viewing.
/// </summary>
public sealed class FidelityJobRunner(IFidelityBatch batch, FidelityReportStore store) : IDisposable
{
    private readonly Lock gate = new();
    private CancellationTokenSource? cancellation;
    private (string Id, FidelityReport Report)? latest;
    private bool latestLoaded;

    /// <summary>Raised on start, progress, and completion, from background threads.</summary>
    public event Action? Changed;

    public FidelityJobState State { get; private set; } = FidelityJobState.Idle;

    public BatchProgress? Progress
    {
        get; private set;
    }

    public string? Error
    {
        get; private set;
    }

    public bool IsAvailable => batch.IsAvailable;

    /// <summary>The running batch, for callers that wait on it.</summary>
    public Task? Completion
    {
        get; private set;
    }

    /// <summary>The newest saved report and its id, loaded from the store on first use.</summary>
    public (string Id, FidelityReport Report)? Latest
    {
        get
        {
            lock (gate)
            {
                if (!latestLoaded)
                {
                    latestLoaded = true;
                    if (store.List() is [var stored, ..] && store.TryLoad(stored.Id) is { } report)
                    {
                        latest = (stored.Id, report);
                    }
                }

                return latest;
            }
        }
    }

    /// <summary>Whether the running or last batch included F3.</summary>
    public bool IncludesF3
    {
        get; private set;
    }

    /// <summary>Starts a batch unless one is running or no source is configured; returns whether it started.</summary>
    public bool Start(bool includeF3 = false)
    {
        lock (gate)
        {
            if (State == FidelityJobState.Running || !batch.IsAvailable)
            {
                return false;
            }

            cancellation?.Dispose();
            cancellation = new CancellationTokenSource();
            State = FidelityJobState.Running;
            IncludesF3 = includeF3;
            Progress = null;
            Error = null;
            var token = cancellation.Token;
            Completion = Task.Run(() => Execute(includeF3, token), CancellationToken.None);
        }

        Changed?.Invoke();
        return true;
    }

    public void Cancel()
    {
        lock (gate)
        {
            if (State == FidelityJobState.Running)
            {
                cancellation?.Cancel();
            }
        }
    }

    /// <summary>
    /// The latest report's result for a library entry when it was computed from the same source bytes, catalog, and
    /// tool versions; otherwise null, so a stale report never marks a board verified.
    /// </summary>
    public BatchBoardResult? FreshResult(BoardListing entry, string? catalogBlob)
    {
        ArgumentNullException.ThrowIfNull(entry);
        if (Latest is not { Report: var report } || catalogBlob is null || report.SharedBoardMetadataBlob != catalogBlob || !CurrentVersions(report))
        {
            return null;
        }

        var result = report.Boards.FirstOrDefault(board => board.Board == entry.Ref.Value);
        return result is not null && result.LosDataBlob == entry.LosDataBlob && result.MetadataBlob == entry.MetadataBlob ? result : null;
    }

    public void Dispose()
    {
        lock (gate)
        {
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;
        }
    }

    private static bool CurrentVersions(FidelityReport report) =>
        report.Versions.GetValueOrDefault("importer") == VaslBoardImporter.ImporterVersion
        && report.Versions.GetValueOrDefault("derivation") == LimboDancer.Domains.Asl.Maps.Derivation.VaslCompatibleHexFactDerivation.Version
        && report.Versions.GetValueOrDefault("renderer") == BoardRenderer.RendererVersion;

    private void Execute(bool includeF3, CancellationToken token)
    {
        var state = FidelityJobState.Completed;
        try
        {
            var report = batch.Run(includeF3, new CallbackProgress(this), token);
            var id = store.Save(report);
            lock (gate)
            {
                latest = (id, report);
                latestLoaded = true;
            }
        }
        catch (OperationCanceledException)
        {
            state = FidelityJobState.Canceled;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            state = FidelityJobState.Failed;
            Error = exception.Message;
        }

        lock (gate)
        {
            State = state;
        }

        Changed?.Invoke();
    }

    private sealed class CallbackProgress(FidelityJobRunner runner) : IProgress<BatchProgress>
    {
        public void Report(BatchProgress value)
        {
            lock (runner.gate)
            {
                if (runner.Progress is null || value.Completed > runner.Progress.Completed)
                {
                    runner.Progress = value;
                }
            }

            runner.Changed?.Invoke();
        }
    }
}
