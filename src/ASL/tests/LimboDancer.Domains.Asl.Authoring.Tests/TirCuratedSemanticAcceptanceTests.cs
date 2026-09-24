using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirCuratedSemanticAcceptanceTests
{
    private static readonly DateTimeOffset At = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ExactVerifiedSourceAndIndependentSemanticReviewCreateAcceptedProjection()
    {
        var (history, artifact) = ReadyHistory();
        var vote = Approve("independent-reviewer");
        var accepted = TirCuratedAcceptedReview.Create(history, artifact, [vote]);
        Assert.Equal(TirReviewStatus.Accepted, accepted.EffectiveStatus);
        Assert.Equal(TirCuratedReviewHistoryBundleJson.ComputePayloadSha256(history),
            accepted.Decision.OpenHistorySha256);
        Assert.Equal(artifact.DeclaredUse, accepted.Decision.DeclaredUse);
        Assert.NotEmpty(accepted.Decision.ClosedSourceFragmentIds);
        Assert.False(accepted.Decision.DisagreementAdjudicated);
        Assert.Equal(accepted.Decision.DecisionSha256,
            TirCuratedAcceptedReview.Create(history, artifact, [vote]).Decision.DecisionSha256);
        using var json = JsonDocument.Parse(accepted.Serialize());
        Assert.Equal("accepted", json.RootElement.GetProperty("reviewStatus").GetString());
        Assert.Equal(TirReviewStatus.InReview, history.EffectiveStatus);
        Assert.Equal(TirReviewStatus.Captured,
            history.Submission.SourceDocument.Artifacts[0].Envelope.ReviewStatus);
    }

    [Fact]
    public void DisagreementNeedsIndependentAdjudicatorAndBindsTheDecision()
    {
        var (history, artifact) = ReadyHistory();
        TirCuratedSemanticVote[] votes =
        [
            Approve("reviewer-a"),
            new TirCuratedSemanticVote(new TirReviewActor("reviewer-b", TirReviewActorRole.DomainReviewer),
                TirReviewDecision.Reject, "Conflicting interpretation."),
        ];
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(
            history, artifact, votes));
        var adjudication = new TirCuratedSemanticAdjudication(
            new TirReviewActor("adjudicator", TirReviewActorRole.Adjudicator),
            TirReviewDecision.Approve, "Resolve the documented conflict for this declared use.");
        var accepted = TirCuratedAcceptedReview.Create(history, artifact, votes, adjudication);
        Assert.True(accepted.Decision.DisagreementAdjudicated);
        Assert.Equal(accepted.Decision.DecisionSha256,
            TirCuratedAcceptedReview.Create(history, artifact, votes.Reverse().ToArray(),
                adjudication).Decision.DecisionSha256);
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(
            history, artifact, votes, adjudication with
            {
                Actor = new TirReviewActor("reviewer-a", TirReviewActorRole.Adjudicator),
            }));
    }

    [Fact]
    public void StaleUseMissingEvidenceAuthorApprovalAndConflictingRulesFailClosed()
    {
        var (history, artifact) = ReadyHistory();
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact with
            {
                DeclaredUse = "another use",
            }, [Approve("reviewer")]));
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact with
            {
                ProposalSha256 = new string('0', 64),
            }, [Approve("reviewer")]));
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact with
            {
                Rules = [],
            }, [Approve("reviewer")]));
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact with
            {
                Rules = [artifact.Rules[0] with
                {
                    SourceFragmentIds = ["unknown-fragment"],
                }],
            }, [Approve("reviewer")]));
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact, [Approve("author")]));
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(history,
            artifact, [Approve("reviewer"), Approve("reviewer")]));
        var conflicting = artifact with
        {
            Rules = [artifact.Rules[0], artifact.Rules[0] with
            {
                RuleId = "other",
                Outcome = "prohibited",
            }],
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(
            history, conflicting, [Approve("reviewer")]));
        var noVerification = history.Submission.SourceReviewBundle with
        {
            Records = history.Submission.SourceReviewBundle.Records
                .Where(item => item is not TirSourceVerificationRecord).ToArray(),
        };
        var altered = history with
        {
            Submission = history.Submission with
            {
                SourceReviewBundle = noVerification,
            },
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedAcceptedReview.Create(
            altered, artifact, [Approve("reviewer")]));
    }

    private static TirCuratedSemanticVote Approve(string identity) => new(
        new TirReviewActor(identity, TirReviewActorRole.DomainReviewer),
        TirReviewDecision.Approve, "Reviewed typed semantics and source dependency closure.");

    private static (TirCuratedReviewHistoryBundle History, TirCuratedSemanticArtifact Artifact)
        ReadyHistory()
    {
        var document = TirStructuralValidatorTests.ValidSourceDocument();
        var source = Assert.Single(document.Artifacts.OfType<TirRuleArtifact>());
        var subject = TirReviewSubjects.Create(document, source.Envelope.ArtifactId);
        var fragment = Assert.Single(source.Envelope.SourceFragments);
        var proposal = new TirCuratedProposal(subject, "author", "exact example use",
            "Typed semantic interpretation follows separately.", At.AddMinutes(10), "fixture");
        var report = TirStructuralValidator.CreateReport(document, source.Envelope.ArtifactId,
            At.AddMinutes(20), "fixture", "validator");
        Assert.Empty(report.Findings);
        var verification = new TirSourceVerificationRecord(subject, At.AddMinutes(21), "fixture",
            new TirReviewActor("source-verifier", TirReviewActorRole.SourceVerifier),
            null, [], fragment,
            new TirSourceEvidenceContext(document.SourceRegistry.RegistryId,
                document.SourceRegistry.Sha256, "fixture", "chapter-a.md",
                fragment.SourceSha256, null, null),
            [], "Compared registered source bytes.", TirSourceVerificationDisposition.Verified,
            null, []);
        var sourceBundle = TirReviewStateProjector.CreateBundle(document,
            source.Envelope.ArtifactId, TirStructuralValidator.Policy, proposal.DeclaredUse,
            ["scope:example"], [report, verification], At.AddMinutes(30), "fixture");
        var submission = TirCuratedReviewBundleJson.Create(document, proposal, sourceBundle,
            At.AddMinutes(35), "fixture");
        var open = new TirCuratedReviewTransition(submission.Subject,
            TirCuratedReviewBundleJson.ComputePayloadSha256(submission), null,
            TirReviewStatus.Proposed, TirReviewStatus.InReview, TirCuratedReviewAction.OpenReview,
            new TirReviewActor("opening-reviewer", TirReviewActorRole.DomainReviewer),
            "Review the typed proposal.", [], At.AddMinutes(40), "fixture");
        var history = TirCuratedReviewHistoryBundleJson.Create(submission, [open],
            At.AddMinutes(41), "fixture");
        var artifact = new TirCuratedSemanticArtifact(submission.Subject.ProposalSha256,
            proposal.DeclaredUse,
            [new TirCuratedSemanticRule("case-a", new Dictionary<string, string>
            {
                ["phase"] = "mph", ["occupancy"] = "knownEmpty",
            }, "eligible", [fragment.FragmentId])], ["unknown-occupancy"]);
        return (history, artifact);
    }
}
