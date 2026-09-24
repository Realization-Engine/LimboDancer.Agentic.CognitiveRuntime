using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using LimboDancer.Domains.Asl.Maps.Outlines;

namespace LimboDancer.Domains.Asl.Maps.Rendering;

/// <summary>
/// Rendering checks for a fidelity batch (ASL-MAP-05): the terrain and elevation outlines refill the grid exactly,
/// and each view's document is recorded by SHA-256 and size so later runs can detect rendering changes.
/// </summary>
public static class BoardRenderChecks
{
    public const string TerrainOutlines = "exact-outlines";
    public const string ElevationOutlines = "elevation-outlines";

    public static IReadOnlyList<FidelityCheck> Run(BoardRenderInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var checks = new List<FidelityCheck>
        {
            Outlines(TerrainOutlines, GridOutlineVerifier.Verify(input.Grid, input.Outlines), input.Outlines),
            Outlines(ElevationOutlines, VerifyElevation(input), input.ElevationOutlines),
        };

        foreach (var view in new[] { BoardView.Exact, BoardView.HexFacts })
        {
            var watch = Stopwatch.StartNew();
            var document = BoardRenderer.Document(input, view);
            watch.Stop();
            var bytes = Encoding.UTF8.GetBytes(document);
            checks.Add(new FidelityCheck(
                BoardRenderer.ViewName(view) + "-svg",
                !document.Contains("<image", StringComparison.Ordinal),
                string.Create(CultureInfo.InvariantCulture,
                    $"sha256 {Convert.ToHexStringLower(SHA256.HashData(bytes))}, {bytes.Length} bytes, {watch.Elapsed.TotalMilliseconds:F0} ms")));
        }

        return checks;
    }

    /// <summary>The SHA-256 recorded in a render check's detail, or null for other checks.</summary>
    public static string? Sha256(FidelityCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        const string Prefix = "sha256 ";
        return check.Detail.StartsWith(Prefix, StringComparison.Ordinal) ? check.Detail.Substring(Prefix.Length, 64) : null;
    }

    private static OutlineVerification VerifyElevation(BoardRenderInput input)
    {
        var grid = input.Grid;
        var elevationOnly = new Grid.TerrainGrid(grid.Geometry, new byte[grid.CellCount], grid.Elevations, grid.Stairways);
        return GridOutlineVerifier.Verify(elevationOnly, input.ElevationOutlines);
    }

    private static FidelityCheck Outlines(string name, OutlineVerification verification, GridOutlines outlines) =>
        new(name, verification.Lossless, verification.Lossless
            ? string.Create(CultureInfo.InvariantCulture, $"lossless, {outlines.Regions.Count} regions, {outlines.VertexCount} vertices")
            : string.Create(CultureInfo.InvariantCulture, $"{verification.MismatchedCells} cells differ, first at ({verification.FirstMismatch!.Value.X}, {verification.FirstMismatch.Value.Y})"));
}
