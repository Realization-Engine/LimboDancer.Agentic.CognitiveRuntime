using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirCuratedReviewHistoryTests
{
    private static readonly DateTimeOffset OpenedAt = new(
        2026, 9, 23, 13, 5, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset RejectedAt = OpenedAt.AddMinutes(5);
    private static readonly DateTimeOffset BundledAt = RejectedAt.AddMinutes(5);

    [Fact]
    public void OpeningAndRejectionProjectReproduciblyIndependentOfEnumerationOrder()
    {
        var submission = Submission();
        var open = Open(submission);
        var reject = Reject(submission, open);
        var opened = TirCuratedReviewHistoryBundleJson.Create(
            submission, [open], BundledAt, "fixture");
        var history = TirCuratedReviewHistoryBundleJson.Create(
            submission, [open, reject], BundledAt, "fixture");
        var reversed = TirCuratedReviewHistoryBundleJson.Create(
            submission, [reject, open], BundledAt, "fixture");
        using var parsed = JsonDocument.Parse(TirCuratedReviewHistoryBundleJson.Serialize(history));
        var root = parsed.RootElement;

        Assert.Equal(TirReviewStatus.InReview, opened.EffectiveStatus);
        Assert.Equal(TirReviewStatus.Rejected, history.EffectiveStatus);
        Assert.Equal(TirCuratedReviewHistoryBundleJson.Serialize(history),
            TirCuratedReviewHistoryBundleJson.Serialize(reversed));
        Assert.Equal(TirCuratedReviewHistoryBundleJson.SchemaId,
            root.GetProperty("schemaId").GetString());
        Assert.Equal("rejected", root.GetProperty("reviewStatus").GetString());
        Assert.Equal("unmodeled", root.GetProperty("formalizationStatus").GetString());
        Assert.Equal(TirCuratedReviewTransitionJson.CreateRecordId(open),
            root.GetProperty("transitions")[0].GetProperty("recordId").GetString());
        Assert.Equal(TirCuratedReviewHistoryBundleJson.ComputePayloadSha256(history),
            root.GetProperty("bundleSha256").GetString());

        var schemaPath = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "Schemas",
            "asl-tir-curated-review-bundle-1.1.schema.json");
        var transitionSchemaPath = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "Schemas",
            "asl-tir-curated-review-transition-1.0.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        using var transitionSchema = JsonDocument.Parse(File.ReadAllBytes(transitionSchemaPath));
        Assert.Equal(TirCuratedReviewHistoryBundleJson.SchemaId,
            schema.RootElement.GetProperty("$id").GetString());
        Assert.Equal(TirCuratedReviewTransitionJson.SchemaId,
            transitionSchema.RootElement.GetProperty("$id").GetString());
    }

    [Fact]
    public void MissingOrWrongPredecessorAndDuplicateTransitionsFailClosed()
    {
        var submission = Submission();
        var open = Open(submission);
        var reject = Reject(submission, open);
        var wrongPrior = reject with
        {
            PriorRecordRef = $"asl-curated-review:sha256:{new string('0', 64)}",
        };
        Assert.Throws<InvalidOperationException>(() => History(submission, [reject]));
        Assert.Throws<InvalidOperationException>(() => History(submission, [open, wrongPrior]));
        Assert.Throws<InvalidOperationException>(() => History(submission, [open, open]));
        Assert.Throws<InvalidOperationException>(() => History(submission, [open, reject, reject]));
    }

    [Fact]
    public void StaleSubmissionAuthorSelfReviewAndForgedProjectionFailClosed()
    {
        var submission = Submission();
        var open = Open(submission);
        var stale = open with
        {
            SubmissionBundleSha256 = new string('1', 64),
        };
        Assert.Throws<InvalidOperationException>(() => History(submission, [stale]));
        var authorReview = open with
        {
            Actor = new TirReviewActor(
                submission.Proposal.SemanticAuthorIdentity, TirReviewActorRole.DomainReviewer),
        };
        Assert.Throws<InvalidOperationException>(() => History(submission, [authorReview]));
        var bundle = History(submission, [open]);
        var forged = bundle with
        {
            EffectiveStatus = TirReviewStatus.Accepted,
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewHistoryBundleJson.Serialize(forged));
        var wrongRole = open with
        {
            Actor = new TirReviewActor("reviewer", TirReviewActorRole.SemanticAuthor),
        };
        Assert.Throws<InvalidOperationException>(() => History(submission, [wrongRole]));
    }

    [Fact]
    public void AcceptanceSupersessionAndIllegalEdgesAreUnrepresentable()
    {
        var submission = Submission();
        var open = Open(submission);
        var approval = open with
        {
            RequestedStatus = TirReviewStatus.Accepted,
        };
        var supersession = open with
        {
            RequestedStatus = TirReviewStatus.Superseded,
        };
        var directRejection = open with
        {
            Action = TirCuratedReviewAction.Reject,
            RequestedStatus = TirReviewStatus.Rejected,
        };
        Assert.Throws<InvalidOperationException>(() => History(submission, [approval]));
        Assert.Throws<InvalidOperationException>(() => History(submission, [supersession]));
        Assert.Throws<InvalidOperationException>(() => History(submission, [directRejection]));
    }

    private static TirCuratedReviewHistoryBundle History(
        TirCuratedReviewBundle submission,
        IReadOnlyList<TirCuratedReviewTransition> transitions) =>
        TirCuratedReviewHistoryBundleJson.Create(submission, transitions, BundledAt, "fixture");

    private static TirCuratedReviewBundle Submission()
    {
        var document = TirCuratedProposalTests.Document();
        var proposal = TirCuratedProposalTests.Proposal(document);
        return TirCuratedReviewBundleJson.Create(
            document, proposal, TirCuratedReviewBundleTests.SourceBundle(
                document, proposal.DeclaredUse),
            TirCuratedReviewBundleTests.SubmittedAt, "fixture");
    }

    private static TirCuratedReviewTransition Open(TirCuratedReviewBundle submission) => new(
        submission.Subject,
        TirCuratedReviewBundleJson.ComputePayloadSha256(submission),
        null, TirReviewStatus.Proposed, TirReviewStatus.InReview,
        TirCuratedReviewAction.OpenReview,
        new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
        "Review opened against exact proposal.", [], OpenedAt, "fixture");

    private static TirCuratedReviewTransition Reject(
        TirCuratedReviewBundle submission,
        TirCuratedReviewTransition open) => new(
            submission.Subject,
            TirCuratedReviewBundleJson.ComputePayloadSha256(submission),
            TirCuratedReviewTransitionJson.CreateRecordId(open),
            TirReviewStatus.InReview, TirReviewStatus.Rejected,
            TirCuratedReviewAction.Reject,
            new TirReviewActor("reviewer", TirReviewActorRole.DomainReviewer),
            "Proposal lacks supported semantics.", ["review:reason"], RejectedAt, "fixture");
}
