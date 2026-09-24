using System.Diagnostics;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Terrain;
using Xunit.Abstractions;

namespace LimboDancer.Domains.Asl.Maps.Vasl.Tests;

/// <summary>F3 (ASL-MAP-043, Requirements scenario M3): vectorize a verified board, recompile, and compare.</summary>
public sealed class VectorizerF3Tests(ITestOutputHelper output)
{
    [VaslFact]
    public void Board01PassesF3()
    {
        var (result, f3) = Vectorize("01");
        Report("bd01", result, f3);
        Assert.True(f3.Passed, string.Join(Environment.NewLine, f3.Failures.Concat(f3.FactDifferences.Take(20))));
    }

    [VaslFact]
    public void EveryVerifiedBoardVectorizesWithIdenticalFacts()
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var catalog = Assert.IsType<TerrainCatalog>(vasl.ReadTerrainCatalog().Catalog);
        var fixtures = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Oracle"), "*.hexfacts.json.gz")
            .Select(path => Path.GetFileName(path).Split('.')[0][2..]).Order(StringComparer.Ordinal).ToArray();
        var results = new (string Board, F3Result F3, int Iterations, long Milliseconds)[fixtures.Length];
        var watch = Stopwatch.StartNew();
        Parallel.For(0, fixtures.Length, index =>
        {
            var board = VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, fixtures[index]), catalog).Board!;
            var facts = HexFactFidelity.Derive(board, catalog);
            var timer = Stopwatch.StartNew();
            var result = Vectorizer.Vectorize(new VectorizerSource(board.Board, board.Provenance.LosData.ContentBlob, board.Grid, facts,
                HexFactFidelity.Annotations(board.Metadata), board.Provenance.SharedBoardMetadata.ContentBlob), catalog);
            results[index] = (board.Board.Value, F3Fidelity.Measure(board.Grid, facts, result, catalog), result.Iterations, timer.ElapsedMilliseconds);
        });

        foreach (var (board, f3, iterations, milliseconds) in results)
        {
            output.WriteLine($"{board,-12} {(f3.Passed ? "pass" : "FAIL")} {iterations} it {milliseconds,6} ms  {f3.Summary}  {string.Join("; ", f3.Failures)}");
        }

        output.WriteLine($"{results.Length} boards in {watch.ElapsedMilliseconds} ms; facts identical on {results.Count(result => result.F3.FactDifferences.Count == 0)}; all thresholds met on {results.Count(result => result.F3.Passed)}.");
        Assert.All(results, result => Assert.True(result.F3.FactDifferences.Count == 0, $"{result.Board}: {string.Join("; ", result.F3.FactDifferences.Take(5))}"));
    }

    private (VectorizeResult Result, F3Result F3) Vectorize(string boardName)
    {
        var vasl = Assert.IsType<VaslSource>(VaslSource.FromEnvironment());
        var catalog = Assert.IsType<TerrainCatalog>(vasl.ReadTerrainCatalog().Catalog);
        var board = Assert.IsType<IngestedBoard>(VaslBoardImporter.Import(vasl, VaslBoardSource.SourceDirectory(vasl, boardName), catalog).Board);
        var facts = HexFactFidelity.Derive(board, catalog);
        var annotations = HexFactFidelity.Annotations(board.Metadata);
        var watch = Stopwatch.StartNew();
        var result = Vectorizer.Vectorize(new VectorizerSource(board.Board, board.Provenance.LosData.ContentBlob, board.Grid, facts, annotations,
            board.Provenance.SharedBoardMetadata.ContentBlob), catalog);
        output.WriteLine($"bd{boardName}: vectorized in {watch.ElapsedMilliseconds} ms, {result.Iterations} iterations");
        return (result, F3Fidelity.Measure(board.Grid, facts, result, catalog));
    }

    private void Report(string board, VectorizeResult result, F3Result f3)
    {
        output.WriteLine($"{board}: {f3.Summary}");
        foreach (var overlap in f3.Overlaps.OrderByDescending(overlap => overlap.SourcePixels))
        {
            output.WriteLine($"  {overlap.Name,-32} {overlap.SourcePixels,8} px  IoU {overlap.IntersectionOverUnion:F3}{(overlap.Dithered ? "  dithered" : string.Empty)}");
        }

        foreach (var group in result.Model.Features.GroupBy(feature => feature.GetType().Name))
        {
            output.WriteLine($"  {group.Key}: {group.Count()}");
        }

        foreach (var difference in f3.FactDifferences.Take(10))
        {
            output.WriteLine("  " + difference);
        }
    }
}
