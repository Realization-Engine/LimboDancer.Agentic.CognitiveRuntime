using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>ASL-MAP-05 and Requirements scenario M2: every board in batch, with a result per board and no fail-fast.</summary>
public sealed class VaslBatchImporterTests(ITestOutputHelper output)
{
    private static readonly string OracleDirectory = Path.Combine(AppContext.BaseDirectory, "Oracle");

    [VaslFact]
    public void FullBatchVerifiesEveryGeomorphicBoardAndExplainsTheRest()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var report = VaslBatchImporter.Run(vasl, new VaslBatchOptions { OracleDirectory = OracleDirectory });
        var summary = report.Summary;

        Assert.Empty(report.Diagnostics);
        Assert.Equal(vasl.BoardNames().Select(name => "bd" + name), report.Boards.Select(board => board.Board));
        Assert.Equal(vasl.Git?.HeadCommit, report.VaslCommit);
        Assert.Matches("^[0-9a-f]{40}$", report.SharedBoardMetadataBlob!);

        // Every board with an oracle fixture is verified; every other outcome carries its explanation.
        var fixtureBoards = Directory.GetFiles(OracleDirectory, "*.hexfacts.json.gz").Select(path => Path.GetFileName(path).Split('.')[0]).ToHashSet();
        foreach (var board in report.Boards)
        {
            switch (board.Outcome)
            {
                case BatchOutcome.Verified:
                    Assert.Equal(F1Status.Pass, board.F1);
                    Assert.Equal(BatchF2.Pass, board.F2!.Status);
                    Assert.True(board.Timings.Total > 0);
                    break;
                case BatchOutcome.Ingested:
                    Assert.False(string.IsNullOrEmpty(board.Reason), $"{board.Board} is ingested without a reason.");
                    Assert.DoesNotContain(board.Board, fixtureBoards);
                    break;
                case BatchOutcome.Failed:
                    Assert.Contains(board.Diagnostics, diagnostic => diagnostic.Severity == MapDiagnosticSeverity.Error);
                    break;
                case BatchOutcome.OutOfScope:
                    Assert.Contains(board.Diagnostics, diagnostic => diagnostic.Code == "VASL-SCOPE-001");
                    Assert.False(string.IsNullOrEmpty(board.Reason));
                    break;
            }
        }

        Assert.All(fixtureBoards, board => Assert.Equal(BatchOutcome.Verified, report.Boards.Single(result => result.Board == board).Outcome));

        // The pinned checkout: 156 verified; bd79 and bdLFT1 fail with their documented diagnostics.
        Assert.True(summary.Verified >= 150, $"Only {summary.Verified} boards were verified.");
        AssertFailure(report, "bd79", "VASL-META-000");
        AssertFailure(report, "bdLFT1", "VASL-LOS-005");

        var verified = report.Boards.Where(board => board.Outcome == BatchOutcome.Verified).ToArray();
        output.WriteLine($"{summary.Boards} boards: {summary.Verified} verified, {summary.Ingested} ingested, {summary.Failed} failed, {summary.OutOfScope} out of scope; {report.DurationMs:F0} ms wall clock.");
        output.WriteLine($"Per verified board, median ms: import {Median(verified, t => t.Import):F0}, derive {Median(verified, t => t.Derive):F0}, F2 {Median(verified, t => t.F2):F0}; max total {verified.Max(board => board.Timings.Total):F0}.");

        var roundTrip = FidelityReport.FromJson(report.ToJson());
        Assert.Equal(summary, roundTrip.Summary);
        Assert.Equal(
            report.Boards.Select(board => (board.Board, board.Outcome, board.LosDataBlob)),
            roundTrip.Boards.Select(board => (board.Board, board.Outcome, board.LosDataBlob)));
    }

    [VaslFact]
    public void AdditionalChecksDecideVerificationAndFailuresDoNotStopTheBatch()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var options = new VaslBatchOptions
        {
            OracleDirectory = OracleDirectory,
            Boards = ["79", "01", "02"],
            AdditionalChecks = (board, _, _) =>
                [new FidelityCheck("probe", board.Board.Value != "bd02", "probe detail"), new FidelityCheck("informational", false, "never gates", Gating: false)],
            AdditionalVersions = new Dictionary<string, string> { ["probe"] = "9.9.9" },
        };
        var progress = new List<BatchProgress>();
        var report = VaslBatchImporter.Run(vasl, options, new CollectingProgress(progress));

        Assert.Equal(["bd79", "bd01", "bd02"], report.Boards.Select(board => board.Board));
        Assert.Equal([BatchOutcome.Failed, BatchOutcome.Verified, BatchOutcome.Ingested], report.Boards.Select(board => board.Outcome));
        Assert.Equal("probe: probe detail", report.Boards[2].Reason);
        Assert.Equal("9.9.9", report.Versions["probe"]);
        lock (progress)
        {
            Assert.Equal([1, 2, 3], progress.Select(item => item.Completed).Order());
        }
    }

    [VaslFact]
    public void ACanceledBatchStops()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsAny<OperationCanceledException>(() => VaslBatchImporter.Run(vasl, new VaslBatchOptions(), cancellationToken: cancellation.Token));
    }

    [Fact]
    public void ReportsRoundTripThroughJson()
    {
        var report = new FidelityReport(
            FidelityReport.CurrentVersion,
            new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero),
            1234.5,
            new string('a', 40),
            new string('b', 40),
            new Dictionary<string, string> { ["importer"] = "1.0.0" },
            [],
            [
                new BatchBoardResult("bd01", BatchOutcome.Verified, null, new string('c', 40), new string('d', 40), F1Status.Pass, "exact",
                    new BatchF2(BatchF2.Pass, [], []), [new FidelityCheck("exact-outlines", true, "lossless")], [], new BatchTimings(10, 20, 30, 40)),
                new BatchBoardResult("bd02", BatchOutcome.Ingested, "F2 failed", null, null, F1Status.Pass, "exact",
                    new BatchF2(BatchF2.Fail, ["E4.center.terrain: expected Woods, derived Open Ground"], []), [], [], new BatchTimings(1, 2, 3, 4)),
                new BatchBoardResult("bd1a", BatchOutcome.OutOfScope, "bd1a: half board", null, null, null, null, null, [],
                    [new MapDiagnostic("VASL-SCOPE-001", MapDiagnosticSeverity.Info, "bd1a: half board")], new BatchTimings(1, 0, 0, 0)),
            ]);

        var json = report.ToJson();
        var back = FidelityReport.FromJson(json);

        Assert.Contains("\"outcome\": \"OutOfScope\"", json, StringComparison.Ordinal);
        Assert.Equal(new BatchSummary(3, 1, 1, 0, 1), back.Summary);
        Assert.Equal(report.Boards[1].F2!.Differences, back.Boards[1].F2!.Differences);
        Assert.Equal(report.Boards[0].Checks, back.Boards[0].Checks);
        Assert.Equal(report.Boards[2].Diagnostics, back.Boards[2].Diagnostics);
        Assert.Equal(100, back.Boards[0].Timings.Total);
        Assert.Equal(json, back.ToJson());
    }

    private static void AssertFailure(FidelityReport report, string board, string code)
    {
        var result = report.Boards.SingleOrDefault(item => item.Board == board);
        if (result is not null)
        {
            Assert.Equal(BatchOutcome.Failed, result.Outcome);
            Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == code);

            // Failed boards still record their source, so the Studio can tell whether a later checkout changed them.
            Assert.Matches("^[0-9a-f]{40}$", result.LosDataBlob!);
            Assert.Matches("^[0-9a-f]{40}$", result.MetadataBlob!);
        }
    }

    private static double Median(IEnumerable<BatchBoardResult> boards, Func<BatchTimings, double> stage)
    {
        var values = boards.Select(board => stage(board.Timings)).Order().ToArray();
        return values.Length == 0 ? 0 : values[values.Length / 2];
    }

    private sealed class CollectingProgress(List<BatchProgress> items) : IProgress<BatchProgress>
    {
        public void Report(BatchProgress value)
        {
            lock (items)
            {
                items.Add(value);
            }
        }
    }
}
