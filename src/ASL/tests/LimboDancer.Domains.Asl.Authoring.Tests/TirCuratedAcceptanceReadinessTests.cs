namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirCuratedAcceptanceReadinessTests
{
    private static readonly DateTimeOffset OpenedAt = new(
        2026, 9, 23, 13, 5, 0, TimeSpan.Zero);

    [Fact]
    public void MissingEvidenceIsReportedButCannotBePromotedToAcceptance()
    {
        var (history, _) = History([]);
        var result = TirCuratedAcceptanceReadinessEvaluator.Evaluate(history);
        var codes = result.Blockers.Select(static blocker => blocker.Code).ToArray();

        Assert.False(result.CanAccept);
        Assert.Equal(TirCuratedReviewHistoryBundleJson.ComputePayloadSha256(history),
            result.ReviewHistorySha256);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.SourceVerificationMissingOrDisputed, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.SemanticValidationUnavailable, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.UseDependencyClosureUnavailable, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.IndependentAcceptanceDecisionUnavailable, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.DisagreementAdjudicationUnavailable, codes);
        Assert.DoesNotContain(TirCuratedAcceptanceBlockerCode.ReviewNotOpen, codes);
    }

    [Fact]
    public void SourceAttestationClearsOnlyItsOwnMissingEvidenceBlocker()
    {
        var document = TirCuratedProposalTests.Document();
        var fragment = document.Artifacts[0].Envelope.SourceFragments[0];
        var verification = new TirSourceVerificationRecord(
            TirReviewSubjects.Create(document, document.Artifacts[0].Envelope.ArtifactId),
            OpenedAt.AddMinutes(-50), "fixture",
            new TirReviewActor("verifier", TirReviewActorRole.SourceVerifier),
            null, [], fragment,
            new TirSourceEvidenceContext(document.SourceRegistry.RegistryId,
                document.SourceRegistry.Sha256, "fixture", "chapter-a.md",
                fragment.SourceSha256, null, null),
            [], "Compared to registered source.",
            TirSourceVerificationDisposition.Verified, null, []);
        var (history, _) = History([verification], document);
        var result = TirCuratedAcceptanceReadinessEvaluator.Evaluate(history);
        var codes = result.Blockers.Select(static blocker => blocker.Code).ToArray();

        Assert.DoesNotContain(TirCuratedAcceptanceBlockerCode.SourceVerificationMissingOrDisputed,
            codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed, codes);
        Assert.Contains(TirCuratedAcceptanceBlockerCode.SemanticValidationUnavailable, codes);
        Assert.False(result.CanAccept);
    }

    [Fact]
    public void StructuralReportMustReproduceItsFindingsAndPassApplicableGates()
    {
        var document = TirCuratedProposalTests.Document();
        var report = TirStructuralValidator.CreateReport(
            document, document.Artifacts[0].Envelope.ArtifactId,
            OpenedAt.AddMinutes(-50), "fixture", "validator");
        var (history, _) = History([report], document);
        var assessment = TirCuratedAcceptanceReadinessEvaluator.Evaluate(history);
        Assert.Contains(assessment.Blockers, blocker =>
            blocker.Code == TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed);

        var forged = report with
        {
            GateResults = report.GateResults.Select(result => result with
            {
                Status = TirValidationGateStatus.Passed,
            }).ToArray(),
        };
        var (forgedHistory, _) = History([forged], document);
        var forgedAssessment = TirCuratedAcceptanceReadinessEvaluator.Evaluate(forgedHistory);
        Assert.Contains(forgedAssessment.Blockers, blocker =>
            blocker.Code == TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed);
        Assert.False(forgedAssessment.CanAccept);
    }

    [Fact]
    public void RejectionAddsAReviewBlockerWithoutErasingOtherFailures()
    {
        var (history, open) = History([]);
        var submission = history.Submission;
        var rejection = new TirCuratedReviewTransition(
            submission.Subject,
            TirCuratedReviewBundleJson.ComputePayloadSha256(submission),
            TirCuratedReviewTransitionJson.CreateRecordId(open),
            TirReviewStatus.InReview, TirReviewStatus.Rejected,
            TirCuratedReviewAction.Reject,
            new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
            "Reject this exact proposal.", [], OpenedAt.AddMinutes(5), "fixture");
        var rejected = TirCuratedReviewHistoryBundleJson.Create(
            submission, [open, rejection], OpenedAt.AddMinutes(10), "fixture");
        var result = TirCuratedAcceptanceReadinessEvaluator.Evaluate(rejected);

        Assert.Contains(result.Blockers, blocker =>
            blocker.Code == TirCuratedAcceptanceBlockerCode.ReviewNotOpen);
        Assert.Contains(result.Blockers, blocker =>
            blocker.Code == TirCuratedAcceptanceBlockerCode.SemanticValidationUnavailable);
        Assert.False(result.CanAccept);
    }

    [Fact]
    public void TamperedHistoryCannotBeAssessedAsEvidence()
    {
        var (history, _) = History([]);
        var forged = history with
        {
            EffectiveStatus = TirReviewStatus.Accepted,
        };
        Assert.Throws<InvalidOperationException>(() =>
            TirCuratedAcceptanceReadinessEvaluator.Evaluate(forged));
    }

    private static (TirCuratedReviewHistoryBundle History, TirCuratedReviewTransition Open)
        History(IReadOnlyList<TirReviewRecord> sourceRecords, TirDocument? suppliedDocument = null)
    {
        var document = suppliedDocument ?? TirCuratedProposalTests.Document();
        var proposal = TirCuratedProposalTests.Proposal(document);
        var sourceBundle = TirReviewStateProjector.CreateBundle(
            document, document.Artifacts[0].Envelope.ArtifactId,
            TirStructuralValidator.Policy, proposal.DeclaredUse, ["scope:example"],
            sourceRecords, TirCuratedReviewBundleTests.SubmittedAt.AddMinutes(-30), "fixture");
        var submission = TirCuratedReviewBundleJson.Create(
            document, proposal, sourceBundle,
            TirCuratedReviewBundleTests.SubmittedAt, "fixture");
        var open = new TirCuratedReviewTransition(
            submission.Subject,
            TirCuratedReviewBundleJson.ComputePayloadSha256(submission),
            null, TirReviewStatus.Proposed, TirReviewStatus.InReview,
            TirCuratedReviewAction.OpenReview,
            new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
            "Open review.", [], OpenedAt, "fixture");
        var history = TirCuratedReviewHistoryBundleJson.Create(
            submission, [open], OpenedAt.AddMinutes(1), "fixture");
        return (history, open);
    }
}
