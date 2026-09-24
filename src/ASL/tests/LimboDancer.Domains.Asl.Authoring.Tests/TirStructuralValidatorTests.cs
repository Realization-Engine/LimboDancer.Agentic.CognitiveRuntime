namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirStructuralValidatorTests
{
    internal static TirDocument ValidSourceDocument() => Extract(
    [
        Fragment(SourceFragmentKind.Heading, null, null, 1, "## 1. PERSONNEL COUNTERS\n"),
        Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2, "**1.1:** A structural rule.\n"),
    ]);

    private static readonly DateTimeOffset CreatedAt = new(
        2026,
        9,
        23,
        12,
        0,
        0,
        TimeSpan.Zero);

    [Fact]
    public void ValidStructuralSubjectPassesApplicableGatesDeterministically()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.Heading, null, null, 1, "## 1. PERSONNEL COUNTERS\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2, "**1.1:** A structural rule.\n"),
        ]);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());

        var first = Validate(document, rule.Envelope.ArtifactId);
        var repeated = Validate(document, rule.Envelope.ArtifactId);

        Assert.Empty(first.Findings);
        Assert.Equal(
            TirReviewCanonicalJson.Serialize(first),
            TirReviewCanonicalJson.Serialize(repeated));
        Assert.Equal(TirValidationGateStatus.Passed, Status(first, TirValidationGate.InputSchema));
        Assert.Equal(TirValidationGateStatus.Passed, Status(first, TirValidationGate.Identity));
        Assert.Equal(TirValidationGateStatus.Passed, Status(first, TirValidationGate.Structural));
        Assert.Equal(TirValidationGateStatus.Passed, Status(first, TirValidationGate.SourceProvenance));
        Assert.Equal(TirValidationGateStatus.NotApplicable, Status(first, TirValidationGate.Semantic));
        Assert.Equal(TirValidationGateStatus.NotApplicable, Status(first, TirValidationGate.Review));
        Assert.Equal(TirValidationGateStatus.Passed, Status(first, TirValidationGate.ArchitectureAuthority));
    }

    [Fact]
    public void DuplicateNormalizedIdentityProducesStableBlockingFinding()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, "**A.1:** Root.\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2, "**1.1:** First.\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 3, "**1.1:** Duplicate.\n"),
        ]);
        var duplicate = document.Artifacts
            .OfType<TirRuleArtifact>()
            .First(static rule => rule.Envelope.NormalizedPublishedId == "A1.1");

        var report = Validate(document, duplicate.Envelope.ArtifactId);
        var finding = Assert.Single(
            report.Findings,
            static finding => finding.Code == "TIR-VAL-DUPLICATE-NORMALIZED-ID");

        Assert.Equal(TirDiagnosticSeverity.Error, finding.Severity);
        Assert.Equal(TirValidationGateStatus.Failed, Status(report, TirValidationGate.Identity));
        Assert.Equal(
            TirReviewIdentity.CreateFindingId(
                report.Subject,
                report.Policy,
                finding.Gate,
                finding.Code,
                finding.ArtifactId,
                finding.EvidenceRefs),
            finding.FindingId);
    }

    [Fact]
    public void HierarchyCycleFailsStructuralGate()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.Heading, null, null, 1, "## 1. PERSONNEL COUNTERS\n"),
            Fragment(SourceFragmentKind.RuleText, "1.1", "A1.1", 2, "**1.1:** First.\n"),
            Fragment(SourceFragmentKind.RuleText, "1.2", "A1.2", 3, "**1.2:** Second.\n"),
        ]);
        var rules = document.Artifacts.OfType<TirRuleArtifact>().ToArray();
        var first = rules[0] with
        {
            Payload = rules[0].Payload with
            {
                DirectParentArtifactId = rules[1].Envelope.ArtifactId,
            },
        };
        var second = rules[1] with
        {
            Payload = rules[1].Payload with
            {
                DirectParentArtifactId = rules[0].Envelope.ArtifactId,
            },
        };
        var changed = document with
        {
            Artifacts = document.Artifacts
                .Select(artifact => artifact.Envelope.ArtifactId switch
                {
                    var id when id == first.Envelope.ArtifactId => first,
                    var id when id == second.Envelope.ArtifactId => second,
                    _ => artifact,
                })
                .ToArray(),
        };

        var report = Validate(changed, first.Envelope.ArtifactId);

        Assert.Contains(report.Findings, static finding => finding.Code == "TIR-VAL-HIERARCHY-CYCLE");
        Assert.Equal(TirValidationGateStatus.Failed, Status(report, TirValidationGate.Structural));
    }

    [Fact]
    public void MissingCrossReferenceRemainsExplicitAndFailsStructuralGate()
    {
        var document = Extract(
        [
            Fragment(
                SourceFragmentKind.RuleText,
                "A.1",
                "A.1",
                1,
                "**A.1:** See B9.9.\n"),
        ]);
        var crossReference = Assert.Single(document.Artifacts.OfType<TirCrossReferenceArtifact>());

        var report = Validate(document, crossReference.Envelope.ArtifactId);
        var finding = Assert.Single(
            report.Findings,
            static finding => finding.Code == "TIR-VAL-REFERENCE-UNRESOLVED");

        Assert.Equal(TirDiagnosticSeverity.Warning, finding.Severity);
        Assert.Equal(TirValidationGateStatus.Failed, Status(report, TirValidationGate.Structural));
        Assert.Equal(TirReferenceResolutionStatus.Missing, crossReference.Payload.ResolutionStatus);
    }

    [Fact]
    public void CreatorMismatchFailsProvenanceGateWithoutChangingExtractionEvidence()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, "**A.1:** Root.\n"),
        ]);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var changedRule = rule with
        {
            Envelope = rule.Envelope with
            {
                CreatedBy = rule.Envelope.CreatedBy with
                {
                    SourceRevision = new string('9', 40),
                },
            },
        };
        var changed = document with
        {
            Artifacts = document.Artifacts
                .Select(artifact => artifact.Envelope.ArtifactId == rule.Envelope.ArtifactId
                    ? changedRule
                    : artifact)
                .ToArray(),
        };

        var report = Validate(changed, changedRule.Envelope.ArtifactId);

        Assert.Contains(
            report.Findings,
            static finding => finding.Code == "TIR-VAL-CREATOR-PROVENANCE-MISMATCH");
        Assert.Equal(TirValidationGateStatus.Failed, Status(report, TirValidationGate.SourceProvenance));
        Assert.Equal(TirArtifactOrigin.Extracted, changedRule.Envelope.Origin);
        Assert.Equal(TirReviewStatus.Captured, changedRule.Envelope.ReviewStatus);
    }

    [Fact]
    public void UnsupportedTirSchemaFailsBeforeAReportCanClaimAnExactSubject()
    {
        var document = Extract(
        [
            Fragment(SourceFragmentKind.RuleText, "A.1", "A.1", 1, "**A.1:** Root.\n"),
        ]);
        var rule = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var unsupported = document with
        {
            SchemaVersion = "2.0.0",
        };

        var exception = Assert.Throws<InvalidOperationException>(
            () => Validate(unsupported, rule.Envelope.ArtifactId));

        Assert.Contains("Unsupported TIR schema", exception.Message, StringComparison.Ordinal);
    }

    private static TirValidationReportRecord Validate(TirDocument document, string artifactId)
    {
        return TirStructuralValidator.CreateReport(
            document,
            artifactId,
            CreatedAt,
            "test-fixture",
            "validator@example.invalid");
    }

    private static TirValidationGateStatus Status(
        TirValidationReportRecord report,
        TirValidationGate gate)
    {
        return Assert.Single(report.GateResults, result => result.Gate == gate).Status;
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
        string content)
    {
        return new SourceFragment(
            $"asl-fragment:sha256:{Hashing.Sha256Text($"fragment-{line}")}",
            "asl-easlrb-3.10:chapter-a",
            "docs/ASL/Rulebook_Markdown/chapter-a.md",
            Sha('a'),
            kind,
            Hashing.Sha256Text(content),
            content,
            new SourceLocator(
                line,
                line,
                43,
                43,
                ["A. INFANTRY"],
                publishedId,
                normalizedId),
            [],
            false);
    }

    private static string Sha(char value)
    {
        return new string(value, 64);
    }
}
