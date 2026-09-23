namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirStructuralExtractorTests
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
    public void ExtractsOrderedRuleEvidenceAndSupportedHierarchy()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2),
            Fragment(SourceFragmentKind.RuleContinuation, "1.1", "A1.1", 3),
            Fragment(SourceFragmentKind.FigureReference, "1.1", "A1.1", 4, "images/example.png"),
            Fragment(SourceFragmentKind.RuleText, "1.2", "A1.2", 5),
        };

        var document = Extract(fragments);
        var rules = document.Artifacts.OfType<TirRuleArtifact>().ToArray();
        var root = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A.1");
        var child = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A1.1");
        var sibling = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A1.2");

        Assert.Equal(TirHierarchyStatus.Root, root.Payload.HierarchyStatus);
        Assert.Null(root.Payload.DirectParentArtifactId);
        Assert.Equal(TirHierarchyStatus.Supported, child.Payload.HierarchyStatus);
        Assert.Equal(root.Envelope.ArtifactId, child.Payload.DirectParentArtifactId);
        Assert.Equal(0, child.Payload.SiblingOrder);
        Assert.Equal(1, sibling.Payload.SiblingOrder);
        Assert.Equal(3, child.Envelope.SourceFragments.Count);
        Assert.Contains(
            child.Envelope.Dependencies,
            static dependency => dependency.Target.EndsWith("/images/example.png", StringComparison.Ordinal));
        Assert.Empty(document.Diagnostics);
    }

    [Fact]
    public void MissingParentProducesExplicitDiagnostic()
    {
        var document = Extract([Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 1)]);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var diagnostic = Assert.Single(document.Diagnostics);

        Assert.Equal(TirHierarchyStatus.Missing, rule.Payload.HierarchyStatus);
        Assert.Null(rule.Payload.DirectParentArtifactId);
        Assert.Equal("TIR-MISSING-PARENT", diagnostic.Code);
        Assert.Equal(rule.Envelope.ArtifactId, diagnostic.ArtifactId);
    }

    [Fact]
    public void DuplicateNormalizedIdentityIsDisambiguatedAndDiagnosed()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 3),
        ]);
        var duplicates = document.Artifacts
            .OfType<TirRuleArtifact>()
            .Where(static rule => rule.Envelope.NormalizedPublishedId == "A1.1")
            .ToArray();

        Assert.Equal(2, duplicates.Length);
        Assert.NotEqual(duplicates[0].Envelope.ArtifactId, duplicates[1].Envelope.ArtifactId);
        Assert.Equal(
            2,
            document.Diagnostics.Count(static diagnostic =>
                diagnostic.Code == "TIR-DUPLICATE-NORMALIZED-ID"));
    }

    [Fact]
    public void RepresentativeSampleRetainsSelectedRuleEvidenceWithoutSemanticPromotion()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.Heading, null, null, 1),
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 2),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 3),
            Fragment(SourceFragmentKind.RuleContinuation, "1.1", "A1.1", 4),
        };
        var sample = TirStructuralExtractor.SelectRepresentativeSample(Extract(fragments));
        var selectedRule = Assert.Single(
            sample.Artifacts.OfType<TirRuleArtifact>(),
            static rule => rule.Envelope.NormalizedPublishedId == "A1.1");
        var evidenceIds = sample.Artifacts
            .OfType<TirSourceFragmentArtifact>()
            .Select(static artifact => artifact.Envelope.SourceFragments[0].FragmentId)
            .ToHashSet(StringComparer.Ordinal);

        Assert.All(
            selectedRule.Envelope.SourceFragments,
            fragment => Assert.Contains(fragment.FragmentId, evidenceIds));
        Assert.All(sample.Artifacts, static artifact => Assert.Null(artifact.Envelope.SemanticId));
        Assert.All(
            sample.Artifacts,
            static artifact => Assert.Equal(
                TirFormalizationStatus.Unmodeled,
                artifact.Envelope.FormalizationStatus));
    }

    private static TirDocument Extract(SourceFragment[] fragments)
    {
        return TirStructuralExtractor.Extract(
            Registry(),
            fragments,
            new TirExtractionOptions(
                new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1"),
                CreatedAt,
                "test-fixture"));
    }

    private static SourceRegistryManifest Registry()
    {
        return new SourceRegistryManifest(
            AslSourceRegistryBuilder.SchemaVersion,
            AslSourceRegistryBuilder.RegistryId,
            AslSourceRegistryBuilder.Edition,
            new string('c', 40),
            AslSourceRegistryBuilder.SourceRoot,
            new ConversionTool(AslSourceRegistryBuilder.ConversionToolPath, Sha('f')),
            []);
    }

    private static SourceFragment Fragment(
        SourceFragmentKind kind,
        string? publishedId,
        string? normalizedId,
        int line,
        string? dependency = null)
    {
        var content = $"fragment-{line}\n";
        var contentHash = Hashing.Sha256Text(content);
        return new SourceFragment(
            $"asl-fragment:sha256:{Hashing.Sha256Text($"fragment-{line}")}",
            "asl-easlrb-3.10:chapter-a",
            "docs/ASL/Rulebook_Markdown/chapter-a.md",
            Sha('a'),
            kind,
            contentHash,
            content,
            new SourceLocator(
                line,
                line,
                43,
                43,
                ["A. INFANTRY"],
                publishedId,
                normalizedId),
            dependency is null ? [] : [dependency],
            false);
    }

    private static string Sha(char value)
    {
        return new string(value, 64);
    }
}
