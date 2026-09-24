using System.Diagnostics;
using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// F3 as informational fidelity checks for a batch run: vectorize the board, recompile, and compare. F3 describes the
/// vectorized model, not the board, so it never changes a board's verified status (Model Design section 10).
/// </summary>
public static class F3Checks
{
    public const string HexFacts = "f3-hexfacts";
    public const string Pixels = "f3-pixels";

    public static IReadOnlyList<FidelityCheck> Run(VectorizerSource source, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(catalog);
        var watch = Stopwatch.StartNew();
        var result = Vectorizer.Vectorize(source, catalog);
        var f3 = F3Fidelity.Measure(source.Grid, source.Facts, result, catalog);
        watch.Stop();
        var facts = f3.FactDifferences.Count == 0
            ? string.Create(CultureInfo.InvariantCulture, $"identical after {result.Iterations} iterations, {f3.PinCount} pins ({f3.PinPixels} pixels), {watch.Elapsed.TotalMilliseconds:F0} ms")
            : $"{f3.FactDifferences.Count} differences: {string.Join("; ", f3.FactDifferences.Take(5))}";
        var pixels = f3.Summary + (f3.Passed ? string.Empty : "; " + string.Join("; ", f3.Failures));
        return
        [
            new FidelityCheck(HexFacts, f3.FactDifferences.Count == 0, facts, Gating: false),
            new FidelityCheck(Pixels, f3.Passed, pixels, Gating: false),
        ];
    }
}
