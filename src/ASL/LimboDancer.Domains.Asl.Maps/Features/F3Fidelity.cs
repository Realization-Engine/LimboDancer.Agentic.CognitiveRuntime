using System.Globalization;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Grid;
using LimboDancer.Domains.Asl.Maps.Terrain;

namespace LimboDancer.Domains.Asl.Maps.Features;

/// <summary>
/// Intersection over union for one code between the source and the compiled grid. Dithered codes and hexside terrain
/// are reported but not gated (Model Design section 7.5).
/// </summary>
public sealed record CodeOverlap(byte Code, string Name, int SourcePixels, double IntersectionOverUnion, bool Dithered, bool Hexside, bool Gated);

/// <summary>
/// The F3 result (ASL-MAP-043, Model Design section 7.5): Hex Facts must be identical; code agreement, elevation
/// agreement, and per-code overlap must meet their thresholds; fidelity pins are reported.
/// </summary>
public sealed record F3Result(
    IReadOnlyList<string> FactDifferences,
    double CodeAgreement,
    double ElevationAgreement,
    IReadOnlyList<CodeOverlap> Overlaps,
    int FeatureCount,
    int VertexCount,
    int PinCount,
    int PinPixels,
    IReadOnlyList<string> Failures)
{
    public bool Passed => Failures.Count == 0;

    public string Summary => string.Create(CultureInfo.InvariantCulture,
        $"facts {(FactDifferences.Count == 0 ? "identical" : FactDifferences.Count + " differences")}, codes {CodeAgreement:P2}, elevations {ElevationAgreement:P2}, "
        + $"{FeatureCount} features, {VertexCount} vertices, {PinCount} pins ({PinPixels} pixels)");
}

public static class F3Fidelity
{
    /// <summary>Initial thresholds from section 7.5, calibrated on board 01 in ASL-MAP-06.</summary>
    public const double MinimumCodeAgreement = 0.99;
    public const double MinimumElevationAgreement = 0.995;
    public const double MinimumOverlap = 0.95;
    public const int OverlapMinimumPixels = 2000;

    public static F3Result Measure(TerrainGrid source, HexFactSet sourceFacts, VectorizeResult result, TerrainCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(sourceFacts);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(catalog);
        var compiled = result.Compiled;
        var cells = source.CellCount;
        var codeMatches = 0;
        var elevationMatches = 0;
        var sourceCount = new int[256];
        var compiledCount = new int[256];
        var intersection = new int[256];
        for (var cell = 0; cell < cells; cell++)
        {
            var sourceCode = source.Codes[cell];
            var compiledCode = compiled.Codes[cell];
            sourceCount[sourceCode]++;
            compiledCount[compiledCode]++;
            if (sourceCode == compiledCode)
            {
                codeMatches++;
                intersection[sourceCode]++;
            }

            if (source.Elevations[cell] == compiled.Elevations[cell])
            {
                elevationMatches++;
            }
        }

        var dithered = Vectorizer.Dithered(source, catalog);
        var overlaps = new List<CodeOverlap>();
        for (var code = 0; code < 256; code++)
        {
            if (sourceCount[code] == 0)
            {
                continue;
            }

            var union = sourceCount[code] + compiledCount[code] - intersection[code];
            var name = catalog.TryGet((byte)code, out var type) ? type.Name : "code " + code.ToString(CultureInfo.InvariantCulture);
            var isDithered = dithered.Contains((byte)code);
            var isHexside = type is { IsHexsideTerrain: true };
            overlaps.Add(new CodeOverlap((byte)code, name, sourceCount[code], (double)intersection[code] / union, isDithered, isHexside,
                !isDithered && !isHexside && sourceCount[code] >= OverlapMinimumPixels));
        }

        var codeAgreement = (double)codeMatches / cells;
        var elevationAgreement = (double)elevationMatches / cells;
        var failures = new List<string>();
        var differences = HexFactComparer.Differences(sourceFacts, result.CompiledFacts);
        if (differences.Count > 0)
        {
            failures.Add($"{differences.Count} Hex Fact differences");
        }

        if (codeAgreement < MinimumCodeAgreement)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"code agreement {codeAgreement:P2} is below {MinimumCodeAgreement:P1}"));
        }

        if (elevationAgreement < MinimumElevationAgreement)
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"elevation agreement {elevationAgreement:P2} is below {MinimumElevationAgreement:P1}"));
        }

        foreach (var overlap in overlaps.Where(overlap => overlap.Gated && overlap.IntersectionOverUnion < MinimumOverlap))
        {
            failures.Add(string.Create(CultureInfo.InvariantCulture, $"{overlap.Name} overlap {overlap.IntersectionOverUnion:F3} is below {MinimumOverlap:F2}"));
        }

        var model = result.Model;
        var pins = model.Features.OfType<FidelityPin>().ToArray();
        var pinPixels = 0;
        foreach (var pin in pins)
        {
            ShapeRasterizer.Fill(pin.Shape.Rings, source.Geometry.GridWidth, source.Geometry.GridHeight, (_, _) => pinPixels++);
        }

        return new F3Result(differences, codeAgreement, elevationAgreement, overlaps, model.Features.Count, VertexCount(model), pins.Length, pinPixels, failures);
    }

    public static int VertexCount(FeatureModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.Features.Sum(feature => feature switch
        {
            ElevationRegion region => region.Shape.VertexCount,
            AreaTerrainFeature area => area.Shape.VertexCount,
            LinearTerrainFeature linear => linear.Outline?.VertexCount ?? 0,
            BridgeFeature bridge => bridge.Shape.VertexCount,
            BuildingFeature building => building.Footprints.Sum(footprint => footprint.VertexCount),
            FidelityPin pin => pin.Shape.VertexCount,
            _ => 0,
        });
    }
}
