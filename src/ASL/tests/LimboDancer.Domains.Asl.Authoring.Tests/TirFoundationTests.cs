using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirFoundationTests
{
    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        9,
        23,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void SchemaDeclaresCompleteVocabularyAndStructuralAuthorityBoundary()
    {
        var schemaPath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "Schemas",
            "asl-tir-1.1.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        var definitions = schema.RootElement.GetProperty("$defs");
        var artifactProperties = definitions
            .GetProperty("artifact")
            .GetProperty("properties");
        var kinds = artifactProperties
            .GetProperty("artifactKind")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .ToArray();

        Assert.Equal(18, kinds.Length);
        Assert.Contains("sourceFragment", kinds);
        Assert.Contains("section", kinds);
        Assert.Contains("rule", kinds);
        Assert.Contains("exception", kinds);
        Assert.Contains("validationShape", kinds);
        Assert.Equal("extracted", artifactProperties.GetProperty("origin").GetProperty("const").GetString());
        Assert.Equal(
            "unmodeled",
            artifactProperties.GetProperty("formalizationStatus").GetProperty("const").GetString());
        Assert.Equal("captured", artifactProperties.GetProperty("reviewStatus").GetProperty("const").GetString());
        Assert.Equal("null", artifactProperties.GetProperty("semanticId").GetProperty("type").GetString());
    }

    [Fact]
    public void ArtifactIdentityIsStableAndSensitiveToStructuralEvidence()
    {
        var input = new TirArtifactIdentityInput(
            TirArtifactKind.Rule,
            "asl-easlrb-3.10-a-e",
            "1.1",
            "A1.1",
            [FragmentId('1')],
            string.Empty);

        var first = TirArtifactIdentity.Create(input);
        var repeated = TirArtifactIdentity.Create(input);
        var changed = TirArtifactIdentity.Create(input with
        {
            SourceFragmentIds = [FragmentId('2')],
        });

        Assert.Equal(first, repeated);
        Assert.StartsWith("asl-tir:sha256:", first, StringComparison.Ordinal);
        Assert.Equal(79, first.Length);
        Assert.NotEqual(first, changed);
    }

    [Fact]
    public void CanonicalDocumentIsIndependentOfSetAndArtifactEnumerationOrder()
    {
        var firstArtifact = CreateRule("1.1", "A1.1", '1');
        var secondArtifact = CreateRule("1.2", "A1.2", '2');
        var first = CreateDocument([firstArtifact, secondArtifact]);
        var reordered = CreateDocument([secondArtifact, firstArtifact]);

        var firstJson = TirCanonicalJson.Serialize(first);
        var reorderedJson = TirCanonicalJson.Serialize(reordered);
        var payload = TirCanonicalJson.SerializePayload(first);
        using var document = JsonDocument.Parse(firstJson);

        Assert.Equal(firstJson, reorderedJson);
        Assert.DoesNotContain("documentSha256", payload, StringComparison.Ordinal);
        Assert.Equal(
            TirCanonicalJson.ComputePayloadSha256(first),
            document.RootElement.GetProperty("documentSha256").GetString());
        Assert.DoesNotContain("\r", firstJson, StringComparison.Ordinal);
        Assert.Contains("\"semanticId\":null", firstJson, StringComparison.Ordinal);
        Assert.Contains("\"formalizationStatus\":\"unmodeled\"", firstJson, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalWriterPreservesOrderedSourceEvidence()
    {
        var artifact = CreateRule("1.1", "A1.1", '1');
        var secondFragment = artifact.Envelope.SourceFragments[0] with
        {
            FragmentId = FragmentId('2'),
            StartLine = 12,
            EndLine = 13,
        };
        var changedEnvelope = artifact.Envelope with
        {
            SourceFragments = [artifact.Envelope.SourceFragments[0], secondFragment],
        };
        var document = CreateDocument([artifact with { Envelope = changedEnvelope }]);
        using var json = JsonDocument.Parse(TirCanonicalJson.Serialize(document));
        var sourceFragments = json.RootElement
            .GetProperty("artifacts")[0]
            .GetProperty("sourceFragments")
            .EnumerateArray()
            .ToArray();

        Assert.Equal(FragmentId('1'), sourceFragments[0].GetProperty("fragmentId").GetString());
        Assert.Equal(FragmentId('2'), sourceFragments[1].GetProperty("fragmentId").GetString());
    }

    [Fact]
    public void CanonicalWriterRejectsPrematureSemanticAuthority()
    {
        var artifact = CreateRule("1.1", "A1.1", '1');
        var invalidEnvelope = artifact.Envelope with
        {
            FormalizationStatus = TirFormalizationStatus.Validated,
            ReviewStatus = TirReviewStatus.Accepted,
            SemanticId = "asl:rule:A1.1",
        };
        var document = CreateDocument([artifact with { Envelope = invalidEnvelope }]);

        var exception = Assert.Throws<InvalidOperationException>(
            () => TirCanonicalJson.Serialize(document));

        Assert.Contains("extracted, unmodeled, captured", exception.Message, StringComparison.Ordinal);
    }

    private static TirDocument CreateDocument(TirArtifact[] artifacts)
    {
        return new TirDocument(
            TirCanonicalJson.SchemaId,
            TirCanonicalJson.SchemaVersion,
            PackageCandidate(),
            new TirSourceRegistryReference("asl-easlrb-3.10-a-e", Sha('a'), new string('c', 40)),
            new TirExtractorIdentity("LimboDancer.Domains.Asl.Authoring", "1.0.0", Sha('b')),
            new TirCanonicalizationProfile(
                TirCanonicalJson.ProfileName,
                TirCanonicalJson.ProfileVersion),
            CreatedAt,
            "committed-build-metadata",
            artifacts,
            []);
    }

    private static TirRuleArtifact CreateRule(string publishedId, string normalizedId, char fragmentHash)
    {
        var fragmentId = FragmentId(fragmentHash);
        var artifactId = TirArtifactIdentity.Create(
            new TirArtifactIdentityInput(
                TirArtifactKind.Rule,
                "asl-easlrb-3.10-a-e",
                publishedId,
                normalizedId,
                [fragmentId],
                string.Empty));
        var envelope = new TirArtifactEnvelope(
            artifactId,
            TirArtifactKind.Rule,
            PackageCandidate(),
            publishedId,
            normalizedId,
            null,
            [new TirSourceFragmentReference(fragmentId, "chapter-a", Sha('d'), Sha('e'), 10, 11)],
            [
                new TirDependency(TirDependencyKind.Footnote, "footnote:2"),
                new TirDependency(TirDependencyKind.Figure, "images/example.png"),
            ],
            TirArtifactOrigin.Extracted,
            TirFormalizationStatus.Unmodeled,
            TirReviewStatus.Captured,
            1m,
            ["heading-supported-boundary", "exact-published-identifier-match"],
            new TirCreatedBy(
                "LimboDancer.Domains.Asl.Authoring",
                "1.0.0",
                Sha('b'),
                new string('c', 40)),
            CreatedAt,
            []);
        return new TirRuleArtifact(
            envelope,
            new TirRulePayload(
                null,
                TirHierarchyStatus.Candidate,
                [TirHierarchyBasis.HeadingPath, TirHierarchyBasis.PublishedIdentifier],
                0));
    }

    private static TirPackageCandidate PackageCandidate()
    {
        return new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1");
    }

    private static string FragmentId(char value)
    {
        return $"asl-fragment:sha256:{new string(value, 64)}";
    }

    private static string Sha(char value)
    {
        return new string(value, 64);
    }
}
