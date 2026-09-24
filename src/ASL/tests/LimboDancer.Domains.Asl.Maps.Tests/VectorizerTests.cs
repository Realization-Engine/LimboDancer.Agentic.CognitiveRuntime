using LimboDancer.Domains.Asl.Maps.Coordinates;
using LimboDancer.Domains.Asl.Maps.Derivation;
using LimboDancer.Domains.Asl.Maps.Features;
using LimboDancer.Domains.Asl.Maps.Geometry;
using static LimboDancer.Domains.Asl.Maps.Tests.FeatureTestCatalog;

namespace LimboDancer.Domains.Asl.Maps.Tests;

public sealed class VectorizerTests
{
    private static readonly BoardGeometry Geometry = BoardGeometry.StandardGeomorphic;

    [Fact]
    public void ACompiledBoardVectorizesBackToIdenticalFacts()
    {
        var source = FeatureCompiler.Compile(FeatureCompilerTests.SampleModel(), Catalog).Grid;
        var facts = Derive(source);
        var result = Vectorizer.Vectorize(new VectorizerSource(BoardRef.Parse("ab-sample"), "v1", source, facts, HexsideAnnotations.None, "test-catalog"), Catalog);

        Assert.True(result.FactsMatch, string.Join(Environment.NewLine, result.Differences.Take(10)));
        Assert.Empty(result.Diagnostics);
        var f3 = F3Fidelity.Measure(source, facts, result, Catalog);
        Assert.True(f3.CodeAgreement > 0.99, f3.Summary);
        Assert.True(f3.ElevationAgreement > 0.995, f3.Summary);
        Assert.Equal(FeatureProvenance.VectorizedKind, result.Model.Provenance.Kind);
        Assert.Equal("ab-sample", result.Model.Provenance.SourceBoard);
    }

    [Fact]
    public void KindsAreRecoveredFromTheirCodes()
    {
        var source = FeatureCompiler.Compile(FeatureCompilerTests.SampleModel(), Catalog).Grid;
        var result = Vectorizer.Vectorize(new VectorizerSource(BoardRef.Parse("ab-sample"), null, source, Derive(source), HexsideAnnotations.None, "test-catalog"), Catalog);
        var features = result.Model.Features;

        Assert.Contains(features, feature => feature is AreaTerrainFeature { Code: 60 });
        Assert.Contains(features, feature => feature is LinearTerrainFeature { Code: 66, Outline: not null });
        Assert.Contains(features, feature => feature is BridgeFeature { Code: 84 });
        Assert.Contains(features, feature => feature is ElevationRegion { Level: 1 });

        // The factory's walls return to the factory footprint; compiling puts them back.
        Assert.Contains(features, feature => feature is BuildingFeature { Code: 47 });
        Assert.DoesNotContain(features, feature => feature is BuildingFeature { Code: 45 });
        Assert.Contains(features, feature => feature is HexsideTerrainFeature { Code: 72, Spans.Count: 2 });

        // The stream's depression pass is reversed, so the model's lowest level is ground level, not the stream's -1.
        Assert.Equal(-1, source.Elevations.ToArray().Min());
        Assert.Equal(0, result.Model.BaseElevation);
    }

    [Fact]
    public void VectorizationIsDeterministic()
    {
        var source = FeatureCompiler.Compile(FeatureCompilerTests.SampleModel(), Catalog).Grid;
        var input = new VectorizerSource(BoardRef.Parse("ab-sample"), null, source, Derive(source), HexsideAnnotations.None, "test-catalog");
        var first = Vectorizer.Vectorize(input, Catalog);
        var second = Vectorizer.Vectorize(input, Catalog);
        Assert.Equal(first.Model.Features.Select(feature => feature.Id), second.Model.Features.Select(feature => feature.Id));
        Assert.Equal(FeatureCompilerTests.Hash(first.Compiled), FeatureCompilerTests.Hash(second.Compiled));
    }

    [Fact]
    public void ToleranceZeroReproducesEveryNonHexsidePixel()
    {
        var model = Model(Geometry,
            new AreaTerrainFeature("woods", 0, new FeatureShape([[FixedVector.FromPixels(50, 50), FixedVector.FromPixels(250, 80), FixedVector.FromPixels(180, 260)]]), Code("Woods")),
            new BuildingFeature("house", 0, [Box(420, 180, 470, 230)], Code("Stone Building, 2 Level"), "B1"),
            new ElevationRegion("hill", 0, Box(300, 100, 700, 400), 1));
        var source = FeatureCompiler.Compile(model, Catalog).Grid;
        var result = Vectorizer.Vectorize(new VectorizerSource(BoardRef.Parse("ab-sample"), null, source, Derive(source), HexsideAnnotations.None, "test-catalog"), Catalog,
            new VectorizerOptions { Tolerance = 0, LinearTolerance = 0 });
        Assert.Equal(FeatureCompilerTests.Hash(source), FeatureCompilerTests.Hash(result.Compiled));
        Assert.Equal(1, result.Iterations);
    }

    [Fact]
    public void ComparerReportsFieldLevelDifferences()
    {
        var plain = FeatureCompiler.Compile(FeatureModel.New(Geometry, "test-catalog"), Catalog).Grid;
        var hex = Geometry.IndexOf(HexName.Parse("E4"));
        var center = Geometry.CenterPoint(hex);
        var built = FeatureCompiler.Compile(Model(Geometry, new BuildingFeature("b", 0, [Box(center.X - 10, center.Y - 10, center.X + 10, center.Y + 10)], Code("Stone Building"), "B1")), Catalog).Grid;
        var expected = Derive(plain);
        var actual = Derive(built);

        Assert.Empty(HexFactComparer.Differences(expected, expected));
        Assert.Equal([hex], HexFactComparer.DifferingHexes(expected, actual));
        Assert.Contains(HexFactComparer.Differences(expected, actual), difference => difference.StartsWith("E4.center: expected level 0 Open Ground, derived level 0 Stone Building", StringComparison.Ordinal));
    }
}
