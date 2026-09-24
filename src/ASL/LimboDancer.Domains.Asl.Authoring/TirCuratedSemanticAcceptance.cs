using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirCuratedSemanticRule(
    string RuleId,
    IReadOnlyDictionary<string, string> Conditions,
    string Outcome,
    IReadOnlyList<string> SourceFragmentIds);

/// <summary>Typed semantics for one declared use of an exact curated proposal.</summary>
public sealed record TirCuratedSemanticArtifact(
    string ProposalSha256,
    string DeclaredUse,
    IReadOnlyList<TirCuratedSemanticRule> Rules,
    IReadOnlyList<string> UnsupportedBranches);

public sealed record TirCuratedSemanticVote(
    TirReviewActor Actor,
    TirReviewDecision Decision,
    string Rationale);

public sealed record TirCuratedSemanticAdjudication(
    TirReviewActor Actor,
    TirReviewDecision Decision,
    string Rationale);

/// <summary>Content-addressed acceptance of typed semantics, separate from captured TIR status.</summary>
public sealed record TirCuratedSemanticAcceptance(
    string OpenHistorySha256,
    string SemanticArtifactSha256,
    string DecisionSha256,
    string DeclaredUse,
    IReadOnlyList<string> ClosedSourceFragmentIds,
    IReadOnlyList<string> UnsupportedBranches,
    bool DisagreementAdjudicated);

public static class TirCuratedSemanticAcceptanceGate
{
    public const string PolicyId = "asl-tir-curated-semantic-exact-use-v1";

    public static TirCuratedSemanticAcceptance Accept(
        TirCuratedReviewHistoryBundle openHistory,
        TirCuratedSemanticArtifact artifact,
        IReadOnlyList<TirCuratedSemanticVote> votes,
        TirCuratedSemanticAdjudication? adjudication = null)
    {
        ArgumentNullException.ThrowIfNull(openHistory);
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(votes);
        var readiness = TirCuratedAcceptanceReadinessEvaluator.Evaluate(openHistory);
        var sourceBlockers = readiness.Blockers.Where(item => item.Code is
            TirCuratedAcceptanceBlockerCode.ReviewNotOpen or
            TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed or
            TirCuratedAcceptanceBlockerCode.SourceVerificationMissingOrDisputed or
            TirCuratedAcceptanceBlockerCode.CapturedFindingRequiresScopedResolution).ToArray();
        if (sourceBlockers.Length != 0)
        {
            throw new InvalidOperationException("Captured source or structural evidence is not ready for semantic review.");
        }

        var submission = openHistory.Submission;
        if (artifact.ProposalSha256 != submission.Subject.ProposalSha256
            || artifact.DeclaredUse != submission.Proposal.DeclaredUse
            || artifact.Rules.Count == 0
            || artifact.UnsupportedBranches.Any(string.IsNullOrWhiteSpace)
            || artifact.UnsupportedBranches.Distinct(StringComparer.Ordinal).Count()
                != artifact.UnsupportedBranches.Count)
        {
            throw new InvalidOperationException("Semantic artifact does not bind the exact proposal and use.");
        }

        var fragments = submission.SourceDocument.Artifacts
            .Single(item => item.Envelope.ArtifactId == submission.Subject.SourceSubject.ArtifactId)
            .Envelope.SourceFragments.Select(item => item.FragmentId)
            .ToHashSet(StringComparer.Ordinal);
        var ruleIds = new HashSet<string>(StringComparer.Ordinal);
        var covered = new HashSet<string>(StringComparer.Ordinal);
        var contracts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var rule in artifact.Rules)
        {
            if (string.IsNullOrWhiteSpace(rule.RuleId) || !ruleIds.Add(rule.RuleId)
                || string.IsNullOrWhiteSpace(rule.Outcome) || rule.Conditions.Count == 0
                || rule.Conditions.Any(item => string.IsNullOrWhiteSpace(item.Key)
                    || string.IsNullOrWhiteSpace(item.Value))
                || rule.SourceFragmentIds.Count == 0
                || rule.SourceFragmentIds.Distinct(StringComparer.Ordinal).Count()
                    != rule.SourceFragmentIds.Count
                || rule.SourceFragmentIds.Any(id => !fragments.Contains(id)))
            {
                throw new InvalidOperationException("Semantic rule has incomplete or unverified source closure.");
            }
            covered.UnionWith(rule.SourceFragmentIds);
            var key = JsonSerializer.Serialize(rule.Conditions
                .OrderBy(item => item.Key, StringComparer.Ordinal).ToArray());
            if (contracts.TryGetValue(key, out var previous) && previous != rule.Outcome)
            {
                throw new InvalidOperationException("Conflicting exact predicates require a changed semantic artifact.");
            }
            contracts[key] = rule.Outcome;
        }
        if (!covered.SetEquals(fragments))
        {
            throw new InvalidOperationException("Declared use does not close every source fragment of the subject.");
        }

        var author = submission.Proposal.SemanticAuthorIdentity;
        if (votes.Count == 0 || votes.Any(item => item.Actor.Role != TirReviewActorRole.DomainReviewer
                || string.IsNullOrWhiteSpace(item.Actor.Identity)
                || item.Actor.Identity == author || string.IsNullOrWhiteSpace(item.Rationale)
                || item.Decision is not (TirReviewDecision.Approve or TirReviewDecision.Reject))
            || votes.Select(item => item.Actor.Identity).Distinct(StringComparer.Ordinal).Count() != votes.Count
            || !votes.Any(item => item.Decision == TirReviewDecision.Approve))
        {
            throw new InvalidOperationException("Acceptance requires an independent affirmative domain review.");
        }
        var disputed = votes.Any(item => item.Decision == TirReviewDecision.Reject);
        if (disputed ? adjudication is null
                || adjudication.Actor.Role != TirReviewActorRole.Adjudicator
                || adjudication.Decision != TirReviewDecision.Approve
                || string.IsNullOrWhiteSpace(adjudication.Rationale)
                || adjudication.Actor.Identity == author
                || votes.Any(item => item.Actor.Identity == adjudication.Actor.Identity)
            : adjudication is not null)
        {
            throw new InvalidOperationException("Conflicting reviews require an independent affirmative adjudication.");
        }

        var canonicalRules = artifact.Rules.OrderBy(item => item.RuleId, StringComparer.Ordinal)
            .Select(item => new
            {
                item.RuleId,
                Conditions = item.Conditions.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray(),
                item.Outcome,
                SourceFragmentIds = item.SourceFragmentIds.Order(StringComparer.Ordinal).ToArray(),
            }).ToArray();
        var artifactDigest = Digest(JsonSerializer.Serialize(new
        {
            artifact.ProposalSha256,
            artifact.DeclaredUse,
            Rules = canonicalRules,
            UnsupportedBranches = artifact.UnsupportedBranches.Order(StringComparer.Ordinal).ToArray(),
        }));
        var votePayload = votes.OrderBy(item => item.Actor.Identity, StringComparer.Ordinal)
            .Select(item => new { item.Actor.Identity, item.Decision, item.Rationale }).ToArray();
        var decisionDigest = Digest(JsonSerializer.Serialize(new
        {
            PolicyId,
            readiness.ReviewHistorySha256,
            artifactDigest,
            Votes = votePayload,
            Adjudication = adjudication is null ? null
                : new
                {
                    adjudication.Actor.Identity,
                    adjudication.Decision,
                    adjudication.Rationale,
                },
        }));
        return new TirCuratedSemanticAcceptance(readiness.ReviewHistorySha256,
            artifactDigest, decisionDigest, artifact.DeclaredUse,
            covered.Order(StringComparer.Ordinal).ToArray(),
            artifact.UnsupportedBranches.Order(StringComparer.Ordinal).ToArray(), disputed);
    }

    private static string Digest(string content) => Convert.ToHexStringLower(
        SHA256.HashData(Encoding.UTF8.GetBytes(content)));
}

/// <summary>An accepted review projection built only by replaying the semantic gate.</summary>
public sealed class TirCuratedAcceptedReview
{
    private TirCuratedAcceptedReview(TirCuratedSemanticAcceptance decision)
    {
        Decision = decision;
    }

    public TirCuratedSemanticAcceptance Decision
    {
        get;
    }

    public TirReviewStatus EffectiveStatus
    {
        get;
    } = TirReviewStatus.Accepted;

    public static TirCuratedAcceptedReview Create(
        TirCuratedReviewHistoryBundle openHistory, TirCuratedSemanticArtifact artifact,
        IReadOnlyList<TirCuratedSemanticVote> votes,
        TirCuratedSemanticAdjudication? adjudication = null) =>
        new(TirCuratedSemanticAcceptanceGate.Accept(openHistory, artifact, votes, adjudication));

    public string Serialize() => JsonSerializer.Serialize(new
    {
        schemaVersion = "1.0.0",
        reviewStatus = "accepted",
        Decision.OpenHistorySha256,
        Decision.SemanticArtifactSha256,
        Decision.DecisionSha256,
        Decision.DeclaredUse,
        Decision.ClosedSourceFragmentIds,
        Decision.UnsupportedBranches,
        Decision.DisagreementAdjudicated,
    }) + "\n";
}
