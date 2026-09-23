using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirReviewProjectionTests
{
    private static readonly DateTimeOffset CreatedAt = new(
        2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CapturedBundleIsCanonicalAcrossRecordEnumerationOrder()
    {
        var document = Document();
        var id = document.Artifacts[0].Envelope.ArtifactId;
        var report = TirStructuralValidator.CreateReport(
            document, id, CreatedAt, "fixture", "validator");
        var diagnostic = Assert.Single(document.Diagnostics);
        var disposition = TirDiagnosticDispositionService.CreateForExtractedDiagnostic(
            document, id, diagnostic, TirFindingDisposition.Deferred,
            "Scope has not been adjudicated.", ["scope:example"], null,
            CreatedAt, "fixture", "reviewer");
        var first = Bundle(document, [report, disposition]);
        var reversed = Bundle(document, [disposition, report]);
        var json = TirReviewBundleJson.Serialize(first);
        using var parsed = JsonDocument.Parse(json);

        Assert.Equal(json, TirReviewBundleJson.Serialize(reversed));
        Assert.Equal("captured", parsed.RootElement.GetProperty("projection")
            .GetProperty("reviewStatus").GetString());
        Assert.Equal(TirReviewBundleJson.ComputePayloadSha256(first),
            parsed.RootElement.GetProperty("bundleSha256").GetString());
        Assert.NotEmpty(first.Projection.BlockingFindingIds);
    }

    [Fact]
    public void StaleSubjectAndMissingPrerequisiteFailClosed()
    {
        var document = Document();
        var id = document.Artifacts[0].Envelope.ArtifactId;
        var report = TirStructuralValidator.CreateReport(document, id, CreatedAt, "fixture", "validator");
        var changed = document with
        {
            Diagnostics = [new TirDiagnostic("OTHER", TirDiagnosticSeverity.Error, id, "Changed.")],
        };
        var stale = Assert.Throws<InvalidOperationException>(() => Bundle(changed, [report]));
        Assert.Contains("different exact subject", stale.Message, StringComparison.Ordinal);

        var prerequisite = report with
        {
            PrerequisiteRecordRefs = [$"asl-tir-review:sha256:{Sha('9')}"],
        };
        var missing = Assert.Throws<InvalidOperationException>(
            () => Bundle(document, [prerequisite]));
        Assert.Contains("prerequisite", missing.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void CapturedArtifactCannotBecomeAcceptedByAReviewOrAdjudication()
    {
        var document = Document();
        var id = document.Artifacts[0].Envelope.ArtifactId;
        var subject = TirReviewSubjects.Create(document, id);
        var review = new TirReviewDecisionRecord(
            subject, CreatedAt, "fixture",
            new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
            null, [], TirReviewStatus.Captured, TirReviewStatus.Accepted,
            TirReviewDecision.Approve, [], [], [], [], "Approve.", []);
        var adjudication = new TirAdjudicationRecord(
            subject, CreatedAt, "fixture",
            new TirReviewActor("adjudicator", TirReviewActorRole.Adjudicator),
            null, [], [$"asl-tir-review:sha256:{Sha('1')}", $"asl-tir-review:sha256:{Sha('2')}"],
            ["Question"], TirReviewDecision.Approve, "Approve.", [],
            TirReviewStatus.Accepted);

        Assert.Throws<InvalidOperationException>(() => Bundle(document, [review]));
        Assert.Throws<InvalidOperationException>(() => Bundle(document, [adjudication]));
    }

    [Fact]
    public void ForgedBundleProjectionAndWrongPolicyAreRejected()
    {
        var document = Document();
        var bundle = Bundle(document, []);
        var forged = bundle with
        {
            Projection = bundle.Projection with
            {
                EffectiveReviewStatus = TirReviewStatus.Accepted,
            },
        };
        Assert.Throws<InvalidOperationException>(() => TirReviewBundleJson.Serialize(forged));

        var id = document.Artifacts[0].Envelope.ArtifactId;
        var report = TirStructuralValidator.CreateReport(document, id, CreatedAt, "fixture", "validator");
        var wrongPolicy = report with
        {
            Policy = report.Policy with
            {
                ConfigurationSha256 = Sha('8'),
            },
        };
        Assert.Throws<InvalidOperationException>(() => Bundle(document, [wrongPolicy]));
    }

    [Fact]
    public void CuratedTransitionPolicyRejectsAuthorSelfApprovalAndIllegalEdges()
    {
        var document = Document();
        var subject = TirReviewSubjects.Create(document, document.Artifacts[0].Envelope.ArtifactId);
        var transition = new TirReviewDecisionRecord(
            subject, CreatedAt, "fixture",
            new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
            null, [], TirReviewStatus.InReview, TirReviewStatus.Accepted,
            TirReviewDecision.Approve, [], [], [], [], "Reviewed evidence.", []);

        TirReviewTransitionRules.ValidateShape(
            transition, TirReviewStatus.InReview, TirArtifactOrigin.Curated,
            "author");
        Assert.Throws<InvalidOperationException>(() => TirReviewTransitionRules.ValidateShape(
            transition, TirReviewStatus.InReview, TirArtifactOrigin.Curated,
            "reviewer"));
        Assert.Throws<InvalidOperationException>(() => TirReviewTransitionRules.ValidateShape(
            transition, TirReviewStatus.InReview, TirArtifactOrigin.Extracted,
            "author"));
        Assert.Throws<InvalidOperationException>(() => TirReviewTransitionRules.ValidateShape(
            transition with
            {
                RequestedStatus = TirReviewStatus.Superseded,
            }, TirReviewStatus.InReview, TirArtifactOrigin.Curated,
            "author"));
    }

    private static TirReviewBundle Bundle(TirDocument document, IReadOnlyList<TirReviewRecord> records)
    {
        return TirReviewStateProjector.CreateBundle(
            document, document.Artifacts[0].Envelope.ArtifactId,
            TirStructuralValidator.Policy, "structural review", ["scope:example"],
            records, CreatedAt, "fixture");
    }

    private static TirDocument Document()
    {
        var candidate = new TirPackageCandidate("asl", "fixture", "0.0.0-candidate.1");
        var fragmentId = $"asl-fragment:sha256:{Sha('1')}";
        var id = TirArtifactIdentity.Create(new TirArtifactIdentityInput(
            TirArtifactKind.Rule, "fixture-registry", "A.1", "A.1", [fragmentId], ""));
        var fragment = new TirSourceFragmentReference(
            fragmentId, "chapter-a", Sha('2'), Sha('3'), 10, 11, null, null);
        var timestamp = CreatedAt;
        var artifact = new TirRuleArtifact(
            new TirArtifactEnvelope(id, TirArtifactKind.Rule, candidate, "A.1", "A.1", null,
                [fragment], [], TirArtifactOrigin.Extracted, TirFormalizationStatus.Unmodeled,
                TirReviewStatus.Captured, 0.5m, ["heading"],
                new TirCreatedBy("fixture", "1.0.0", Sha('4'), new string('c', 40)),
                timestamp, []),
            new TirRulePayload(null, TirHierarchyStatus.Root,
                [TirHierarchyBasis.PublishedIdentifier], 0));
        return new TirDocument(
            TirCanonicalJson.SchemaId, TirCanonicalJson.SchemaVersion, candidate,
            new TirSourceRegistryReference("fixture-registry", Sha('5'), new string('c', 40)),
            new TirExtractorIdentity("fixture", "1.0.0", Sha('4')),
            new TirCanonicalizationProfile(TirCanonicalJson.ProfileName, TirCanonicalJson.ProfileVersion),
            timestamp, "fixture", [artifact],
            [new TirDiagnostic("TIR-MISSING-REFERENCE-TARGET", TirDiagnosticSeverity.Warning,
                id, "Unresolved reference.")]);
    }

    private static string Sha(char character) => new(character, 64);
}
