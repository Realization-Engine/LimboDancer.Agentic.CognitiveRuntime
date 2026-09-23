using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirCuratedReviewBundleTests
{
    private static readonly DateTimeOffset SubmittedAt = new(
        2026, 9, 23, 13, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SubmissionBindsExactProposalAndCapturedBundleButNeverAccepts()
    {
        var document = TirCuratedProposalTests.Document();
        var proposal = TirCuratedProposalTests.Proposal(document);
        var sourceBundle = SourceBundle(document, proposal.DeclaredUse);
        var bundle = TirCuratedReviewBundleJson.Create(
            document, proposal, sourceBundle, SubmittedAt, "fixture");
        var serialized = TirCuratedReviewBundleJson.Serialize(bundle);
        using var parsed = JsonDocument.Parse(serialized);
        var root = parsed.RootElement;

        Assert.Equal(TirCuratedReviewBundleJson.SchemaId, root.GetProperty("schemaId").GetString());
        Assert.Equal("proposed", root.GetProperty("reviewStatus").GetString());
        Assert.Equal("unmodeled", root.GetProperty("formalizationStatus").GetString());
        Assert.Equal(TirCuratedProposalJson.ComputePayloadSha256(proposal, document),
            root.GetProperty("subject").GetProperty("proposalSha256").GetString());
        Assert.Equal(TirReviewBundleJson.ComputePayloadSha256(sourceBundle),
            root.GetProperty("sourceReviewBundleSha256").GetString());
        Assert.Equal(TirCuratedReviewBundleJson.ComputePayloadSha256(bundle),
            root.GetProperty("bundleSha256").GetString());
        Assert.False(root.TryGetProperty("reviewRecords", out _));

        var schemaPath = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "Schemas",
            "asl-tir-curated-review-bundle-1.0.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        Assert.Equal(TirCuratedReviewBundleJson.SchemaId,
            schema.RootElement.GetProperty("$id").GetString());
        Assert.Equal("proposed", schema.RootElement.GetProperty("properties")
            .GetProperty("reviewStatus").GetProperty("const").GetString());
    }

    [Fact]
    public void StaleProposalSubjectUseOrSourceBundleFailClosed()
    {
        var document = TirCuratedProposalTests.Document();
        var proposal = TirCuratedProposalTests.Proposal(document);
        var sourceBundle = SourceBundle(document, proposal.DeclaredUse);
        var bundle = TirCuratedReviewBundleJson.Create(
            document, proposal, sourceBundle, SubmittedAt, "fixture");
        var alteredText = proposal with
        {
            ProposalText = "Different authoring content.",
        };
        var staleBundle = bundle with
        {
            Proposal = alteredText,
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewBundleJson.Serialize(
            staleBundle));

        var differentUse = proposal with
        {
            DeclaredUse = "another use",
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewBundleJson.Create(
            document, differentUse, sourceBundle, SubmittedAt, "fixture"));

        var changedDocument = document with
        {
            Diagnostics = [new TirDiagnostic("CHANGED", TirDiagnosticSeverity.Warning,
                proposal.SourceSubject.ArtifactId, "New finding.")],
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewBundleJson.Create(
            changedDocument, proposal, sourceBundle, SubmittedAt, "fixture"));

        var changedSourceBundle = sourceBundle with
        {
            CoverageRefs = ["new coverage"],
        };
        var changedBundle = bundle with
        {
            SourceReviewBundle = changedSourceBundle,
        };
        Assert.NotEqual(TirCuratedReviewBundleJson.ComputePayloadSha256(bundle),
            TirCuratedReviewBundleJson.ComputePayloadSha256(changedBundle));
    }

    [Fact]
    public void ForgedSourceProjectionAndPrematureSubmissionFailClosed()
    {
        var document = TirCuratedProposalTests.Document();
        var proposal = TirCuratedProposalTests.Proposal(document);
        var sourceBundle = SourceBundle(document, proposal.DeclaredUse);
        var forged = sourceBundle with
        {
            Projection = sourceBundle.Projection with
            {
                EffectiveReviewStatus = TirReviewStatus.Accepted,
            },
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewBundleJson.Create(
            document, proposal, forged, SubmittedAt, "fixture"));
        Assert.Throws<InvalidOperationException>(() => TirCuratedReviewBundleJson.Create(
            document, proposal, sourceBundle, proposal.CreatedAt.AddMinutes(-1), "fixture"));
    }

    private static TirReviewBundle SourceBundle(TirDocument document, string declaredUse) =>
        TirReviewStateProjector.CreateBundle(
            document, document.Artifacts[0].Envelope.ArtifactId,
            TirStructuralValidator.Policy, declaredUse, ["scope:example"], [],
            SubmittedAt.AddMinutes(-30), "fixture");
}
