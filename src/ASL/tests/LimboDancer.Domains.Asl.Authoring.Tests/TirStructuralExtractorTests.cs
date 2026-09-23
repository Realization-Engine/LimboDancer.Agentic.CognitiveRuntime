using System.Text;

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
        Assert.Equal(TirSectionBoundaryKind.MarkdownHeading, section.Payload.BoundaryKind);
        Assert.Equal(2, section.Payload.SourceHeadingLevel);
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
    public void AlternateMajorSectionDeclarationsRemainExplicitlyTyped()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.Heading, null, null, 1, content: "## *16. BATTLEFIELD INTEGRITY\n"),
            Fragment(SourceFragmentKind.RuleText, "16.1", "A16.1", 2),
            Fragment(SourceFragmentKind.RuleContinuation, "16.1", "A16.1", 3, content: "**17. AEROSANS**<sup>14</sup>\n"),
            Fragment(SourceFragmentKind.RuleText, "17.1", "A17.1", 4),
            Fragment(SourceFragmentKind.StructuredText, null, null, 5, content: "```text\n36. PREPARED FIRE ZONE^2^0\n```\n"),
            Fragment(SourceFragmentKind.RuleText, "36.2", "A36.2", 6),
        };

        var document = Extract(fragments);
        var sections = document.Artifacts
            .OfType<TirSectionArtifact>()
            .ToDictionary(static section => section.Envelope.NormalizedPublishedId!, StringComparer.Ordinal);
        var rules = document.Artifacts
            .OfType<TirRuleArtifact>()
            .ToDictionary(static rule => rule.Envelope.NormalizedPublishedId!, StringComparer.Ordinal);

        Assert.Equal(TirSectionBoundaryKind.MarkdownHeading, sections["A16"].Payload.BoundaryKind);
        Assert.Equal(2, sections["A16"].Payload.SourceHeadingLevel);
        Assert.Equal(TirSectionBoundaryKind.BoldDeclaration, sections["A17"].Payload.BoundaryKind);
        Assert.Null(sections["A17"].Payload.SourceHeadingLevel);
        Assert.Equal(
            TirSectionBoundaryKind.StructuredTextDeclaration,
            sections["A36"].Payload.BoundaryKind);
        AssertSpanEquals(
            fragments[4].Content,
            "36. PREPARED FIRE ZONE^2^0",
            sections["A36"].Envelope.SourceFragments[0]);
        Assert.Equal(
            sections["A16"].Envelope.ArtifactId,
            rules["A16.1"].Payload.DirectParentArtifactId);
        Assert.Equal(
            sections["A17"].Envelope.ArtifactId,
            rules["A17.1"].Payload.DirectParentArtifactId);
        Assert.Equal(
            sections["A36"].Envelope.ArtifactId,
            rules["A36.2"].Payload.DirectParentArtifactId);
        Assert.Empty(document.Diagnostics);
    }

    [Fact]
    public void ZeroPaddedChildrenFallBackToPublishedParentConvention()
    {
        var fragments = new[]
        {
            Fragment(SourceFragmentKind.Heading, null, null, 1, content: "## 14. SNIPERS\n"),
            Fragment(SourceFragmentKind.RuleText, "14.01", "A14.01", 2),
            Fragment(SourceFragmentKind.Heading, null, null, 3, content: "## 7. FIRE ATTACKS\n"),
            Fragment(SourceFragmentKind.RuleText, "7.3", "A7.3", 4),
            Fragment(SourceFragmentKind.RuleText, "7.301", "A7.301", 5),
            Fragment(SourceFragmentKind.RuleText, "7.309", "A7.309", 6),
            Fragment(SourceFragmentKind.Heading, null, null, 7, content: "## 2. EQUIPMENT\n"),
            Fragment(SourceFragmentKind.RuleText, "2.2", "A2.2", 8),
            Fragment(SourceFragmentKind.RuleText, "2.24", "A2.24", 9),
            Fragment(SourceFragmentKind.RuleText, "2.2401", "A2.2401", 10),
        };

        var document = Extract(fragments);
        var section = Assert.Single(
            document.Artifacts.OfType<TirSectionArtifact>(),
            static artifact => artifact.Envelope.NormalizedPublishedId == "A14");
        var rules = document.Artifacts
            .OfType<TirRuleArtifact>()
            .ToDictionary(static rule => rule.Envelope.NormalizedPublishedId!, StringComparer.Ordinal);

        Assert.Equal(section.Envelope.ArtifactId, rules["A14.01"].Payload.DirectParentArtifactId);
        Assert.Equal(rules["A7.3"].Envelope.ArtifactId, rules["A7.301"].Payload.DirectParentArtifactId);
        Assert.Equal(rules["A7.3"].Envelope.ArtifactId, rules["A7.309"].Payload.DirectParentArtifactId);
        Assert.Equal(rules["A2.24"].Envelope.ArtifactId, rules["A2.2401"].Payload.DirectParentArtifactId);
        Assert.All(
            new[] { rules["A14.01"], rules["A7.301"], rules["A7.309"], rules["A2.2401"] },
            static rule => Assert.Contains(
                "zero-padded-child-convention",
                rule.Envelope.ConfidenceBasis));
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
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, content: "**A.1:** Café — see B1.1, B9.9, and 1.2.\n"),
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
        AssertSpanEquals(fragments[0].Content, "B1.1", resolved.Envelope.SourceFragments[0]);
        AssertSpanEquals(fragments[0].Content, "B9.9", missing.Envelope.SourceFragments[0]);
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
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, content: "**A.1:** naïve EX: mechanical illustration.\n"),
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
        AssertSpanEquals(fragments[0].Content, "EX:", example.Envelope.SourceFragments[0]);
        Assert.Null(table.Envelope.SourceFragments[0].StartUtf8ByteOffset);
        Assert.Null(table.Envelope.SourceFragments[0].EndUtf8ByteOffsetExclusive);
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

    private static void AssertSpanEquals(
        string content,
        string expected,
        TirSourceFragmentReference sourceReference)
    {
        var characterOffset = content.IndexOf(expected, StringComparison.Ordinal);
        Assert.True(characterOffset >= 0);
        var start = Encoding.UTF8.GetByteCount(content.AsSpan(0, characterOffset));
        var end = start + Encoding.UTF8.GetByteCount(expected);

        Assert.Equal(start, sourceReference.StartUtf8ByteOffset);
        Assert.Equal(end, sourceReference.EndUtf8ByteOffsetExclusive);
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
