using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Vasl;

namespace LimboDancer.Domains.Asl.Maps.Rendering.Tests;

public sealed class BoardRenderChecksTests
{
    [Fact]
    public void SyntheticBoardPassesEveryCheckAndHashesMatchTheDocuments()
    {
        var input = SyntheticBoard.Input();
        var checks = BoardRenderChecks.Run(input);

        Assert.Equal([BoardRenderChecks.TerrainOutlines, BoardRenderChecks.ElevationOutlines, "exact-svg", "hexfacts-svg"], checks.Select(check => check.Name));
        Assert.All(checks, check => Assert.True(check.Passed, check.Detail));
        Assert.StartsWith("lossless, ", checks[0].Detail, StringComparison.Ordinal);
        Assert.Equal(Sha256(BoardRenderer.Document(input, BoardView.Exact)), BoardRenderChecks.Sha256(checks[2]));
        Assert.Equal(Sha256(BoardRenderer.Document(input, BoardView.HexFacts)), BoardRenderChecks.Sha256(checks[3]));
        Assert.Null(BoardRenderChecks.Sha256(checks[0]));
    }

    [VaslFact]
    public void EveryIngestedBoardRendersWithLosslessOutlines()
    {
        var vasl = VaslSource.FromEnvironment()!;
        var report = VaslBatchImporter.Run(vasl, new VaslBatchOptions
        {
            AdditionalChecks = (board, facts, catalog) => BoardRenderChecks.Run(BoardRenderInput.Create(board.Board, board.Board.Value, board.Grid, catalog, facts)),
        });

        var ingested = report.Boards.Where(board => board.Outcome is BatchOutcome.Verified or BatchOutcome.Ingested).ToArray();
        Assert.True(ingested.Length >= 150, $"Only {ingested.Length} boards were ingested.");
        var failures = ingested.SelectMany(board => board.Checks.Where(check => !check.Passed).Select(check => $"{board.Board} {check.Name}: {check.Detail}")).ToArray();
        Assert.True(failures.Length == 0, string.Join(Environment.NewLine, failures));
        Assert.All(ingested, board => Assert.Equal(4, board.Checks.Count));
    }

    private static string Sha256(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
