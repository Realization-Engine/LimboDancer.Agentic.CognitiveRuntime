using System.Net;
using LimboDancer.Domains.Asl.MapStudio.Services;
using LimboDancer.Domains.Asl.Maps;
using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Rendering;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.MapStudio.Tests;

internal static class FidelityTestData
{
    /// <summary>A report with bd02 verified from the fake provider's source blobs and bd79 failed.</summary>
    public static FidelityReport Report(DateTimeOffset startedAt, string? renderer = null) => new(
        FidelityReport.CurrentVersion,
        startedAt,
        1500,
        new string('a', 40),
        FakeBoardProvider.Catalog,
        new Dictionary<string, string>
        {
            ["derivation"] = VaslCompatibleHexFactDerivation.Version,
            ["importer"] = VaslBoardImporter.ImporterVersion,
            ["renderer"] = renderer ?? BoardRenderer.RendererVersion,
            ["report"] = FidelityReport.CurrentVersion,
        },
        [],
        [
            new BatchBoardResult("bd02", BatchOutcome.Verified, null, FakeBoardProvider.Board02LosData, FakeBoardProvider.Board02Metadata,
                F1Status.Pass, "exact", new BatchF2(BatchF2.Pass, [], []), [new FidelityCheck("exact-outlines", true, "lossless")], [],
                new BatchTimings(10, 20, 5, 30)),
            new BatchBoardResult("bd79", BatchOutcome.Failed, "BoardMetadata.xml is not well-formed XML", null, new string('7', 40), null, null, null, [],
                [new MapDiagnostic("VASL-META-000", MapDiagnosticSeverity.Error, "BoardMetadata.xml is not well-formed XML")], new BatchTimings(1, 0, 0, 0)),
        ]);
}

/// <summary>Returns a canned report, or waits for cancellation when <see cref="BlockUntilCanceled"/> is set.</summary>
internal sealed class FakeFidelityBatch : IFidelityBatch
{
    public bool BlockUntilCanceled
    {
        get; set;
    }

    public bool IsAvailable => true;

    public FidelityReport Run(IProgress<BatchProgress> progress, CancellationToken cancellationToken)
    {
        progress.Report(new BatchProgress(1, 2, "bd02"));
        if (BlockUntilCanceled)
        {
            cancellationToken.WaitHandle.WaitOne();
            cancellationToken.ThrowIfCancellationRequested();
        }

        progress.Report(new BatchProgress(2, 2, "bd79"));
        return FidelityTestData.Report(DateTimeOffset.UtcNow);
    }
}

public sealed class FidelityRunnerTests : IDisposable
{
    private readonly StudioOptions options = new() { CacheRoot = Path.Combine(Path.GetTempPath(), "asl-fidelity-tests-" + Guid.NewGuid().ToString("N")) };

    [Fact]
    public async Task ARunSavesItsReportAndBecomesTheLatest()
    {
        var store = new FidelityReportStore(options);
        using var runner = new FidelityJobRunner(new FakeFidelityBatch(), store);
        var changes = 0;
        runner.Changed += () => Interlocked.Increment(ref changes);

        Assert.Null(runner.Latest);
        Assert.True(runner.Start());
        await runner.Completion!;

        Assert.Equal(FidelityJobState.Completed, runner.State);
        Assert.Equal(new BatchProgress(2, 2, "bd79"), runner.Progress);
        var saved = Assert.Single(store.List());
        Assert.Equal(saved.Id, runner.Latest!.Value.Id);
        Assert.Equal(new BatchSummary(2, 1, 0, 1, 0), saved.Summary);
        Assert.True(changes >= 3);

        // A new runner, as after a restart, loads the saved report as the latest.
        using var restarted = new FidelityJobRunner(new FakeFidelityBatch(), store);
        Assert.Equal(saved.Id, restarted.Latest!.Value.Id);
    }

    [Fact]
    public async Task ACanceledRunSavesNothing()
    {
        var batch = new FakeFidelityBatch { BlockUntilCanceled = true };
        var store = new FidelityReportStore(options);
        using var runner = new FidelityJobRunner(batch, store);

        Assert.True(runner.Start());
        Assert.False(runner.Start());
        runner.Cancel();
        await runner.Completion!;

        Assert.Equal(FidelityJobState.Canceled, runner.State);
        Assert.Empty(store.List());
    }

    [Fact]
    public void FreshResultsRequireTheSameSourcesCatalogAndTools()
    {
        var store = new FidelityReportStore(options);
        store.Save(FidelityTestData.Report(new DateTimeOffset(2026, 9, 24, 8, 0, 0, TimeSpan.Zero)));
        using var runner = new FidelityJobRunner(new FakeFidelityBatch(), store);
        var entry = new BoardListing(BoardRef.Parse("bd02"), "VASL board 02", BoardScope.InScope, null, FakeBoardProvider.Board02LosData, FakeBoardProvider.Board02Metadata);

        Assert.Equal(BatchOutcome.Verified, runner.FreshResult(entry, FakeBoardProvider.Catalog)!.Outcome);
        Assert.Null(runner.FreshResult(entry with
        {
            LosDataBlob = new string('9', 40)
        }, FakeBoardProvider.Catalog));
        Assert.Null(runner.FreshResult(entry with
        {
            MetadataBlob = new string('9', 40)
        }, FakeBoardProvider.Catalog));
        Assert.Null(runner.FreshResult(entry, new string('9', 40)));
        Assert.Null(runner.FreshResult(entry with
        {
            Ref = BoardRef.Parse("bd03")
        }, FakeBoardProvider.Catalog));

        // A report judged by another renderer version is stale.
        store.Save(FidelityTestData.Report(new DateTimeOffset(2026, 9, 24, 9, 0, 0, TimeSpan.Zero), renderer: "0.9.0"));
        using var later = new FidelityJobRunner(new FakeFidelityBatch(), store);
        Assert.Null(later.FreshResult(entry, FakeBoardProvider.Catalog));
    }

    [Fact]
    public void StoreRejectsMalformedIds()
    {
        var store = new FidelityReportStore(options);
        Assert.False(FidelityReportStore.IsValidId("../secrets"));
        Assert.False(FidelityReportStore.IsValidId("fidelity-2026"));
        Assert.True(FidelityReportStore.IsValidId("fidelity-20260924T080000Z"));
        Assert.Null(store.TryLoad("../../etc/passwd"));
        Assert.Null(store.ReadJson("fidelity-20260924T080000Z"));
    }

    public void Dispose()
    {
        if (Directory.Exists(options.CacheRoot))
        {
            Directory.Delete(options.CacheRoot, recursive: true);
        }
    }
}

public sealed class FidelityPageTests(StudioFactory factory) : IClassFixture<StudioFactory>
{
    [Fact]
    public async Task SavedReportsDownloadAsJson()
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri($"/fidelity/reports/{factory.SeededReportId}.json", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType!.MediaType);
        var report = FidelityReport.FromJson(await response.Content.ReadAsStringAsync());
        Assert.Equal(new BatchSummary(2, 1, 0, 1, 0), report.Summary);
    }

    [Theory]
    [InlineData("/fidelity/reports/fidelity-20000101T000000Z.json")]
    [InlineData("/fidelity/reports/not-a-report.json")]
    [InlineData("/fidelity/reports/fidelity-20260924T080000Z.txt")]
    public async Task UnknownReportsAreNotFound(string path)
    {
        using var client = factory.CreateClient();
        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task FidelityPageShowsTheLatestReport()
    {
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync(new Uri("/fidelity", UriKind.Relative));
        Assert.Contains("Run batch", html, StringComparison.Ordinal);
        Assert.Contains("2 boards in scope", html, StringComparison.Ordinal);
        Assert.Contains("href=\"boards/bd02\"", html, StringComparison.Ordinal);
        Assert.Contains("VASL-META-000 (error)", html, StringComparison.Ordinal);
        Assert.Contains($"fidelity/reports/{factory.SeededReportId}.json", html, StringComparison.Ordinal);
    }
}
