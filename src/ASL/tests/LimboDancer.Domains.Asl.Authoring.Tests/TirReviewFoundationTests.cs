using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirReviewFoundationTests
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
    public void SchemaDeclaresTheVersionedRecordFamilyAndExactSubject()
    {
        var schemaPath = Path.Combine(
            RepositoryPaths.Root,
            "docs",
            "ASL",
            "Schemas",
            "asl-tir-review-record-1.0.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        var root = schema.RootElement;
        var kinds = root
            .GetProperty("properties")
            .GetProperty("recordKind")
            .GetProperty("enum")
            .EnumerateArray()
            .Select(static item => item.GetString())
            .ToArray();
        var subject = root.GetProperty("$defs").GetProperty("subject");

        Assert.Equal(TirReviewCanonicalJson.SchemaId, root.GetProperty("$id").GetString());
        Assert.Equal(5, kinds.Length);
        Assert.Contains("validationReport", kinds);
        Assert.Contains("sourceVerification", kinds);
        Assert.Contains("diagnosticDisposition", kinds);
        Assert.Contains("review", kinds);
        Assert.Contains("adjudication", kinds);
        Assert.Equal(
            TirCanonicalJson.SchemaId,
            subject.GetProperty("properties").GetProperty("schemaId").GetProperty("const").GetString());
        Assert.Contains(
            "artifactSha256",
            subject.GetProperty("required").EnumerateArray().Select(static item => item.GetString()));
    }

    [Fact]
    public void ArtifactDigestBindsTheExactCanonicalArtifactSnapshot()
    {
        var artifact = CreateRule();
        var original = CreateDocument(artifact);
        var changed = CreateDocument(artifact with
        {
            Payload = artifact.Payload with { SiblingOrder = 1 },
        });

        var originalDigest = TirCanonicalJson.ComputeArtifactSha256(
            original,
            artifact.Envelope.ArtifactId);
        var repeatedDigest = TirCanonicalJson.ComputeArtifactSha256(
            original,
            artifact.Envelope.ArtifactId);
        var changedDigest = TirCanonicalJson.ComputeArtifactSha256(
            changed,
            artifact.Envelope.ArtifactId);

        Assert.Equal(originalDigest, repeatedDigest);
        Assert.NotEqual(originalDigest, changedDigest);
    }

    [Fact]
    public void CanonicalValidationReportIsIndependentOfSetEnumerationOrder()
    {
        var subject = CreateSubject();
        var firstFinding = CreateFinding(subject, "ASL-TIR-001", TirValidationGate.Identity, ["evidence:b", "evidence:a"]);
        var secondFinding = CreateFinding(subject, "ASL-TIR-002", TirValidationGate.Structural, ["evidence:d", "evidence:c"]);
        var first = CreateValidationReport(
            subject,
            [
                new TirValidationGateResult(TirValidationGate.Structural, TirValidationGateStatus.Failed, [secondFinding.FindingId]),
                new TirValidationGateResult(TirValidationGate.Identity, TirValidationGateStatus.Failed, [firstFinding.FindingId]),
            ],
            [secondFinding, firstFinding],
            [ReviewRecordId('2'), ReviewRecordId('1')]);
        var reordered = CreateValidationReport(
            subject,
            [
                new TirValidationGateResult(TirValidationGate.Identity, TirValidationGateStatus.Failed, [firstFinding.FindingId]),
                new TirValidationGateResult(TirValidationGate.Structural, TirValidationGateStatus.Failed, [secondFinding.FindingId]),
            ],
            [firstFinding with { EvidenceRefs = ["evidence:a", "evidence:b"] }, secondFinding with { EvidenceRefs = ["evidence:c", "evidence:d"] }],
            [ReviewRecordId('1'), ReviewRecordId('2')]);

        Assert.Equal(
            TirReviewCanonicalJson.Serialize(first),
            TirReviewCanonicalJson.Serialize(reordered));
    }

    [Fact]
    public void RecordDigestIsSensitiveToSubjectAndPayloadChanges()
    {
        var subject = CreateSubject();
        var finding = CreateFinding(subject, "ASL-TIR-001", TirValidationGate.Identity, ["evidence:a"]);
        var record = CreateValidationReport(
            subject,
            [new TirValidationGateResult(TirValidationGate.Identity, TirValidationGateStatus.Failed, [finding.FindingId])],
            [finding],
            []);
        var changedSubject = subject with { ArtifactSha256 = Sha('9') };
        var changedFinding = CreateFinding(
            changedSubject,
            finding.Code,
            finding.Gate,
            finding.EvidenceRefs);
        var subjectChangedRecord = CreateValidationReport(
            changedSubject,
            [new TirValidationGateResult(TirValidationGate.Identity, TirValidationGateStatus.Failed, [changedFinding.FindingId])],
            [changedFinding],
            []);
        var payloadChangedRecord = record with
        {
            Findings = [finding with { Message = "A changed finding message." }],
        };

        Assert.NotEqual(
            TirReviewCanonicalJson.ComputePayloadSha256(record),
            TirReviewCanonicalJson.ComputePayloadSha256(subjectChangedRecord));
        Assert.NotEqual(
            TirReviewCanonicalJson.ComputePayloadSha256(record),
            TirReviewCanonicalJson.ComputePayloadSha256(payloadChangedRecord));
    }

    [Fact]
    public void FindingIdentityIsStableForEvidenceSetsAndSensitiveToPolicy()
    {
        var subject = CreateSubject();
        var policy = Policy();
        var first = TirReviewIdentity.CreateFindingId(
            subject,
            policy,
            TirValidationGate.Structural,
            "ASL-TIR-001",
            subject.ArtifactId,
            ["evidence:b", "evidence:a"]);
        var reordered = TirReviewIdentity.CreateFindingId(
            subject,
            policy,
            TirValidationGate.Structural,
            "ASL-TIR-001",
            subject.ArtifactId,
            ["evidence:a", "evidence:b"]);
        var changedPolicy = TirReviewIdentity.CreateFindingId(
            subject,
            policy with { ConfigurationSha256 = Sha('8') },
            TirValidationGate.Structural,
            "ASL-TIR-001",
            subject.ArtifactId,
            ["evidence:a", "evidence:b"]);

        Assert.Equal(first, reordered);
        Assert.StartsWith("asl-tir-finding:sha256:", first, StringComparison.Ordinal);
        Assert.NotEqual(first, changedPolicy);
    }

    [Fact]
    public void ReviewerEvidenceOrderRemainsDigestSignificant()
    {
        var first = CreateReview(["evidence:first", "evidence:second"]);
        var reordered = CreateReview(["evidence:second", "evidence:first"]);

        Assert.NotEqual(
            TirReviewCanonicalJson.ComputePayloadSha256(first),
            TirReviewCanonicalJson.ComputePayloadSha256(reordered));
    }

    [Fact]
    public void CanonicalWriterRejectsNonUtcTimestampsAndInvalidActorAuthority()
    {
        var record = CreateReview([]);
        var nonUtc = record with { CreatedAt = CreatedAt.ToOffset(TimeSpan.FromHours(1)) };
        var wrongRole = record with
        {
            Actor = new TirReviewActor("reviewer@example.invalid", TirReviewActorRole.Validator),
        };

        var timestampException = Assert.Throws<InvalidOperationException>(
            () => TirReviewCanonicalJson.Serialize(nonUtc));
        var roleException = Assert.Throws<InvalidOperationException>(
            () => TirReviewCanonicalJson.Serialize(wrongRole));

        Assert.Contains("must be supplied in UTC", timestampException.Message, StringComparison.Ordinal);
        Assert.Contains("domain-reviewer role", roleException.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EveryRecordKindSerializesWithItsCanonicalIdentity()
    {
        var subject = CreateSubject();
        var finding = CreateFinding(subject, "ASL-TIR-001", TirValidationGate.Identity, ["evidence:a"]);
        TirReviewRecord[] records =
        [
            CreateValidationReport(
                subject,
                [new TirValidationGateResult(TirValidationGate.Identity, TirValidationGateStatus.Failed, [finding.FindingId])],
                [finding],
                []),
            new TirSourceVerificationRecord(
                subject,
                CreatedAt,
                "committed-build-metadata",
                new TirReviewActor("source-verifier@example.invalid", TirReviewActorRole.SourceVerifier),
                null,
                [],
                SourceFragment(),
                [new TirDependency(TirDependencyKind.Figure, "images/example.png")],
                "manual visual comparison",
                TirSourceVerificationDisposition.Verified,
                null,
                []),
            new TirDiagnosticDispositionRecord(
                subject,
                CreatedAt,
                "committed-build-metadata",
                new TirReviewActor("reviewer@example.invalid", TirReviewActorRole.DomainReviewer),
                null,
                [],
                new TirFindingReference(
                    TirFindingOrigin.ExtractedDiagnostic,
                    $"asl-tir-diagnostic:sha256:{Sha('7')}",
                    "ASL-TIR-MISSING-REFERENCE",
                    null),
                TirFindingDisposition.Deferred,
                "Outside the current review scope.",
                ["scope:scenario-a1"],
                null),
            CreateReview([]),
            new TirAdjudicationRecord(
                subject,
                CreatedAt,
                "committed-build-metadata",
                new TirReviewActor("adjudicator@example.invalid", TirReviewActorRole.Adjudicator),
                null,
                [ReviewRecordId('1'), ReviewRecordId('2')],
                [ReviewRecordId('1'), ReviewRecordId('2')],
                ["Does the source support the proposed interpretation?"],
                TirReviewDecision.RequestChanges,
                "The source evidence is incomplete.",
                ["source:chapter-a"],
                TirReviewStatus.InReview),
        ];
        var expectedKinds = new[]
        {
            "validationReport",
            "sourceVerification",
            "diagnosticDisposition",
            "review",
            "adjudication",
        };

        for (var index = 0; index < records.Length; index++)
        {
            var json = TirReviewCanonicalJson.Serialize(records[index]);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            Assert.Equal(expectedKinds[index], root.GetProperty("recordKind").GetString());
            Assert.Equal(
                TirReviewCanonicalJson.CreateRecordId(records[index]),
                root.GetProperty("recordId").GetString());
            Assert.Equal(
                TirReviewCanonicalJson.ComputePayloadSha256(records[index]),
                root.GetProperty("recordSha256").GetString());
        }
    }

    private static TirValidationReportRecord CreateValidationReport(
        TirReviewSubjectReference subject,
        IReadOnlyList<TirValidationGateResult> gateResults,
        IReadOnlyList<TirValidationFinding> findings,
        IReadOnlyList<string> prerequisiteRecordRefs)
    {
        return new TirValidationReportRecord(
            subject,
            CreatedAt,
            "committed-build-metadata",
            new TirReviewActor("validator@example.invalid", TirReviewActorRole.Validator),
            new TirReviewTool("LimboDancer.Domains.Asl.Authoring", "1.0.0", Sha('6')),
            prerequisiteRecordRefs,
            Policy(),
            gateResults,
            findings);
    }

    private static TirValidationFinding CreateFinding(
        TirReviewSubjectReference subject,
        string code,
        TirValidationGate gate,
        IReadOnlyList<string> evidenceRefs)
    {
        var policy = Policy();
        var findingId = TirReviewIdentity.CreateFindingId(
            subject,
            policy,
            gate,
            code,
            subject.ArtifactId,
            evidenceRefs);
        return new TirValidationFinding(
            findingId,
            code,
            TirDiagnosticSeverity.Error,
            gate,
            subject.ArtifactId,
            "The deterministic validation gate failed.",
            evidenceRefs);
    }

    private static TirReviewDecisionRecord CreateReview(IReadOnlyList<string> evidenceRefs)
    {
        return new TirReviewDecisionRecord(
            CreateSubject(),
            CreatedAt,
            "committed-build-metadata",
            new TirReviewActor("reviewer@example.invalid", TirReviewActorRole.DomainReviewer),
            null,
            [ReviewRecordId('1')],
            TirReviewStatus.Proposed,
            TirReviewStatus.InReview,
            TirReviewDecision.Approve,
            [],
            [],
            [],
            ["dependency:chapter-a"],
            "The exact subject is ready for review.",
            evidenceRefs);
    }

    private static TirReviewSubjectReference CreateSubject()
    {
        var document = CreateDocument(CreateRule());
        var artifactId = document.Artifacts[0].Envelope.ArtifactId;
        return new TirReviewSubjectReference(
            TirCanonicalJson.ComputePayloadSha256(document),
            artifactId,
            TirCanonicalJson.ComputeArtifactSha256(document, artifactId),
            TirCanonicalJson.SchemaId,
            document.PackageCandidate);
    }

    private static TirValidationPolicy Policy()
    {
        return new TirValidationPolicy("asl-tir-structural", "1.0.0", Sha('5'));
    }

    private static TirDocument CreateDocument(TirRuleArtifact artifact)
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
            [artifact],
            []);
    }

    private static TirRuleArtifact CreateRule()
    {
        var fragment = SourceFragment();
        var artifactId = TirArtifactIdentity.Create(
            new TirArtifactIdentityInput(
                TirArtifactKind.Rule,
                "asl-easlrb-3.10-a-e",
                "1.1",
                "A1.1",
                [fragment.FragmentId],
                string.Empty));
        var envelope = new TirArtifactEnvelope(
            artifactId,
            TirArtifactKind.Rule,
            PackageCandidate(),
            "1.1",
            "A1.1",
            null,
            [fragment],
            [],
            TirArtifactOrigin.Extracted,
            TirFormalizationStatus.Unmodeled,
            TirReviewStatus.Captured,
            1m,
            ["exact-published-identifier-match"],
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
                [TirHierarchyBasis.PublishedIdentifier],
                0));
    }

    private static TirSourceFragmentReference SourceFragment()
    {
        return new TirSourceFragmentReference(
            $"asl-fragment:sha256:{Sha('1')}",
            "chapter-a",
            Sha('2'),
            Sha('3'),
            10,
            11,
            null,
            null);
    }

    private static TirPackageCandidate PackageCandidate()
    {
        return new TirPackageCandidate("asl", "easlrb-3.10-a-e", "0.0.0-candidate.1");
    }

    private static string ReviewRecordId(char value)
    {
        return $"asl-tir-review:sha256:{Sha(value)}";
    }

    private static string Sha(char value)
    {
        return new string(value, 64);
    }
}
