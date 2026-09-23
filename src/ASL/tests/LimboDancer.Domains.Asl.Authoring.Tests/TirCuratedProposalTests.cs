using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring.Tests;

public sealed class TirCuratedProposalTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProposalPinsSourceAndTextWithoutClaimingSemanticAcceptance()
    {
        var document = Document();
        var proposal = Proposal(document);
        var serialized = TirCuratedProposalJson.Serialize(proposal, document);
        using var parsed = JsonDocument.Parse(serialized);
        var root = parsed.RootElement;

        Assert.Equal(TirCuratedProposalJson.SchemaId, root.GetProperty("schemaId").GetString());
        Assert.Equal(TirCuratedProposalJson.ComputePayloadSha256(proposal, document),
            root.GetProperty("proposalSha256").GetString());
        Assert.Equal(proposal.SourceSubject.ArtifactSha256,
            root.GetProperty("sourceSubject").GetProperty("artifactSha256").GetString());
        Assert.False(root.TryGetProperty("reviewStatus", out _));
        Assert.False(root.TryGetProperty("formalizationStatus", out _));
        Assert.False(root.TryGetProperty("semanticValidation", out _));

        var schemaPath = Path.Combine(RepositoryPaths.Root, "docs", "ASL", "Schemas",
            "asl-tir-curated-proposal-1.0.schema.json");
        using var schema = JsonDocument.Parse(File.ReadAllBytes(schemaPath));
        Assert.Equal(TirCuratedProposalJson.SchemaId,
            schema.RootElement.GetProperty("$id").GetString());
    }

    [Fact]
    public void AuthoredTextUseAndSourceChangeProposalIdentity()
    {
        var document = Document();
        var proposal = Proposal(document);
        var digest = TirCuratedProposalJson.ComputePayloadSha256(proposal, document);

        var changedText = proposal with
        {
            ProposalText = "A different interpretation.",
        };
        var changedUse = proposal with
        {
            DeclaredUse = "another use",
        };
        var changedAuthor = proposal with
        {
            SemanticAuthorIdentity = "another author",
        };
        Assert.NotEqual(digest, TirCuratedProposalJson.ComputePayloadSha256(changedText, document));
        Assert.NotEqual(digest, TirCuratedProposalJson.ComputePayloadSha256(changedUse, document));
        Assert.NotEqual(digest, TirCuratedProposalJson.ComputePayloadSha256(changedAuthor, document));

        var changed = document with
        {
            Diagnostics = [new TirDiagnostic(
                "CHANGED", TirDiagnosticSeverity.Warning,
                proposal.SourceSubject.ArtifactId, "Changed extraction evidence.")],
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedProposalJson.Serialize(proposal, changed));
        var changedSubject = proposal.SourceSubject with
        {
            ArtifactSha256 = new string('0', 64),
        };
        var stale = proposal with
        {
            SourceSubject = changedSubject,
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedProposalJson.Serialize(
            stale, document));
    }

    [Fact]
    public void InvalidTimestampAndBlankProposalFailClosed()
    {
        var document = Document();
        var proposal = Proposal(document);
        var localTimestamp = proposal with
        {
            CreatedAt = Timestamp.ToOffset(TimeSpan.FromHours(1)),
        };
        var blankText = proposal with
        {
            ProposalText = "  ",
        };
        Assert.Throws<InvalidOperationException>(() => TirCuratedProposalJson.Serialize(
            localTimestamp, document));
        Assert.Throws<InvalidOperationException>(() => TirCuratedProposalJson.Serialize(
            blankText, document));
    }

    private static TirCuratedProposal Proposal(TirDocument document) => new(
        TirReviewSubjects.Create(document, document.Artifacts[0].Envelope.ArtifactId),
        "author", "structural example", "A proposed interpretation, not executable semantics.",
        Timestamp, "fixture");

    private static TirDocument Document()
    {
        static string Sha(char value) => new(value, 64);
        var candidate = new TirPackageCandidate("asl", "fixture", "0.0.0-candidate.1");
        var fragmentId = $"asl-fragment:sha256:{Sha('1')}";
        var id = TirArtifactIdentity.Create(new TirArtifactIdentityInput(
            TirArtifactKind.Rule, "fixture-registry", "A.1", "A.1", [fragmentId], ""));
        var fragment = new TirSourceFragmentReference(
            fragmentId, "chapter-a", Sha('2'), Sha('3'), 10, 11, null, null);
        var artifact = new TirRuleArtifact(
            new TirArtifactEnvelope(id, TirArtifactKind.Rule, candidate, "A.1", "A.1", null,
                [fragment], [], TirArtifactOrigin.Extracted, TirFormalizationStatus.Unmodeled,
                TirReviewStatus.Captured, 0.5m, ["heading"],
                new TirCreatedBy("fixture", "1.0.0", Sha('4'), new string('c', 40)),
                Timestamp, []),
            new TirRulePayload(null, TirHierarchyStatus.Root,
                [TirHierarchyBasis.PublishedIdentifier], 0));
        return new TirDocument(
            TirCanonicalJson.SchemaId, TirCanonicalJson.SchemaVersion, candidate,
            new TirSourceRegistryReference("fixture-registry", Sha('5'), new string('c', 40)),
            new TirExtractorIdentity("fixture", "1.0.0", Sha('4')),
            new TirCanonicalizationProfile(TirCanonicalJson.ProfileName, TirCanonicalJson.ProfileVersion),
            Timestamp, "fixture", [artifact], []);
    }
}
