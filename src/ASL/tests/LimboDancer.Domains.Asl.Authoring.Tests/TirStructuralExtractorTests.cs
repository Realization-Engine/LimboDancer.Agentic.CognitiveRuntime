namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirStructuralExtractorTests
{
    private const string SourceCommit = "a3254ff1d492dbdd28483d86f5b42437b48e80d4";

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
            Fragment(SourceFragmentKind.Heading, null, null, 2, content: "## 1. PERSONNEL COUNTERS\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 3),
            Fragment(SourceFragmentKind.RuleContinuation, "1.1", "A1.1", 4),
            Fragment(SourceFragmentKind.FigureReference, "1.1", "A1.1", 5, "images/example.png"),
            Fragment(SourceFragmentKind.RuleText, "1.2", "A1.2", 6),
        };

        var document = Extract(fragments);
        var section = Assert.Single(document.Artifacts.OfType<TirSectionArtifact>());
        var rules = document.Artifacts.OfType<TirRuleArtifact>().ToArray();
        var root = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A.1");
        var child = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A1.1");
        var sibling = Assert.Single(rules, static rule => rule.Envelope.NormalizedPublishedId == "A1.2");

        Assert.Equal(TirHierarchyStatus.Root, root.Payload.HierarchyStatus);
        Assert.Null(root.Payload.DirectParentArtifactId);
        Assert.Equal("A1", section.Envelope.NormalizedPublishedId);
        Assert.Equal("PERSONNEL COUNTERS", section.Payload.Title);
        Assert.Equal(TirHierarchyStatus.Supported, child.Payload.HierarchyStatus);
        Assert.Equal(section.Envelope.ArtifactId, child.Payload.DirectParentArtifactId);
        Assert.Equal(0, child.Payload.SiblingOrder);
        Assert.Equal(1, sibling.Payload.SiblingOrder);
        Assert.Equal(3, child.Envelope.SourceFragments.Count);
        Assert.Contains(
            child.Envelope.Dependencies,
            static dependency => dependency.Target.EndsWith("/images/example.png", StringComparison.Ordinal));
        Assert.Empty(document.Diagnostics);
    }

    [Fact]
    public void DecimalDigitsProduceNestedRuleHierarchyBelowSection()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.Heading, null, null, 1, content: "## 1. PERSONNEL COUNTERS\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2),
            Fragment(SourceFragmentKind.RuleText, "1.11", "A1.11", 3),
            Fragment(SourceFragmentKind.RuleText, "1.111", "A1.111", 4),
        };

        var document = Extract(fragments);
        var section = Assert.Single(document.Artifacts.OfType<TirSectionArtifact>());
        var rules = document.Artifacts
            .OfType<TirRuleArtifact>()
            .ToDictionary(static rule => rule.Envelope.NormalizedPublishedId!, StringComparer.Ordinal);

        Assert.Equal(section.Envelope.ArtifactId, rules["A1.1"].Payload.DirectParentArtifactId);
        Assert.Equal(rules["A1.1"].Envelope.ArtifactId, rules["A1.11"].Payload.DirectParentArtifactId);
        Assert.Equal(rules["A1.11"].Envelope.ArtifactId, rules["A1.111"].Payload.DirectParentArtifactId);
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
    public void ExtractsOnlyChapterQualifiedCrossReferencesAndReportsResolution()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, content: "**A.1:** See B1.1, B9.9, and 1.2.\n"),
            Fragment(SourceFragmentKind.RuleText, "B.1", "B.1", 2),
            Fragment(SourceFragmentKind.RuleText, "1.1", "B1.1", 3),
        };

        var document = Extract(fragments);
        var references = document.Artifacts.OfType<TirCrossReferenceArtifact>().ToArray();
        var resolved = Assert.Single(references, static item => item.Payload.ReferenceText == "B1.1");
        var missing = Assert.Single(references, static item => item.Payload.ReferenceText == "B9.9");

        Assert.Equal(TirReferenceResolutionStatus.Resolved, resolved.Payload.ResolutionStatus);
        Assert.NotNull(resolved.Payload.ResolvedTargetArtifactId);
        Assert.Equal(TirReferenceResolutionStatus.Missing, missing.Payload.ResolutionStatus);
        Assert.Null(missing.Payload.ResolvedTargetArtifactId);
        Assert.DoesNotContain(references, static item => item.Payload.ReferenceText == "A.1");
        Assert.DoesNotContain(references, static item => item.Payload.ReferenceText == "1.2");
        Assert.Contains(
            document.Diagnostics,
            diagnostic => diagnostic.Code == "TIR-MISSING-REFERENCE-TARGET"
                && diagnostic.ArtifactId == missing.Envelope.ArtifactId);
    }

    [Fact]
    public void ExplicitMarkersProduceUnmodeledExampleAndUnverifiedTableArtifacts()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, content: "**A.1:** EX: mechanical illustration.\n"),
            Fragment(SourceFragmentKind.StructuredText, "A.1", "A.1", 2, content: "```text\nRESULT TABLE\n```\n"),
        };

        var document = Extract(fragments);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var example = Assert.Single(document.Artifacts.OfType<TirExampleArtifact>());
        var table = Assert.Single(document.Artifacts.OfType<TirTableArtifact>());

        Assert.Collection(
            example.Payload.IllustratesArtifactIds,
            artifactId => Assert.Equal(rule.Envelope.ArtifactId, artifactId));
        Assert.False(table.Payload.StructureVerified);
        Assert.Empty(table.Payload.NoteFragmentIds);
        Assert.Equal(TirFormalizationStatus.Unmodeled, example.Envelope.FormalizationStatus);
        Assert.Equal(TirReviewStatus.Captured, table.Envelope.ReviewStatus);
    }

    [Fact]
    public void RepresentativeSampleRetainsSelectedRuleEvidenceWithoutSemanticPromotion()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.Heading, null, null, 1, content: "## 1. PERSONNEL COUNTERS\n"),
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

    [Fact]
    public void CommittedStructuralSampleMatchesCurrentCSharpExtraction()
    {
        var manifests = AslAuthoringManifestGenerator.Generate(RepositoryPaths.Root, SourceCommit);
        var extracted = TirStructuralExtractor.Extract(
            manifests.Registry,
            manifests.Fragments,
            new TirExtractionOptions(
                new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1"),
                CreatedAt,
                "operator-supplied-reproducible-build-metadata"));
        var expected = TirCanonicalJson.Serialize(
            TirStructuralExtractor.SelectRepresentativeSample(extracted));
        var samplePath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "TIR",
            "asl-3.10-a-e.structural-sample.tir.json");

        Assert.Equal(expected, File.ReadAllText(samplePath));
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
            [
                new SourceArtifact(
                    "asl-easlrb-3.10:chapter-a",
                    "docs/ASL/Rulebook_Markdown/chapter-a.md",
                    SourceArtifactKind.Markdown,
                    Sha('a'),
                    0,
                    "A"),
            ]);
    }

    private static SourceFragment Fragment(
        SourceFragmentKind kind,
        string? publishedId,
        string? normalizedId,
        int line,
        string? dependency = null,
        string? content = null)
    {
        content ??= $"fragment-{line}\n";
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
