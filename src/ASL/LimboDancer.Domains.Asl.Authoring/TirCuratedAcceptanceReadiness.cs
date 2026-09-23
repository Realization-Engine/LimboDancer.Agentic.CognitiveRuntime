namespace LimboDancer.Domains.Asl.Authoring;

public enum TirCuratedAcceptanceBlockerCode
{
    ReviewNotOpen,
    StructuralEvidenceMissingOrFailed,
    SourceVerificationMissingOrDisputed,
    CapturedFindingRequiresScopedResolution,
    SemanticValidationUnavailable,
    UseDependencyClosureUnavailable,
    IndependentAcceptanceDecisionUnavailable,
    DisagreementAdjudicationUnavailable,
}

public sealed record TirCuratedAcceptanceBlocker(
    TirCuratedAcceptanceBlockerCode Code,
    IReadOnlyList<string> EvidenceRefs);

/// <summary>
/// Diagnostic snapshot only. No acceptance or publication API consumes this type.
/// An assessment is never an authorization or a substitute for semantic gates.
/// </summary>
public sealed record TirCuratedAcceptanceReadiness(
    string ReviewHistorySha256,
    TirCuratedReviewSubjectReference Subject,
    string DeclaredUse,
    IReadOnlyList<TirCuratedAcceptanceBlocker> Blockers)
{
    public bool CanAccept => false;
}

public static class TirCuratedAcceptanceReadinessEvaluator
{
    public const string ContractVersion = "1.0.0";

    public static TirCuratedAcceptanceReadiness Evaluate(TirCuratedReviewHistoryBundle history)
    {
        ArgumentNullException.ThrowIfNull(history);
        var exactHistorySha256 = TirCuratedReviewHistoryBundleJson.ComputePayloadSha256(history);
        var submission = history.Submission;
        var captured = submission.SourceReviewBundle;
        var document = submission.SourceDocument;
        var artifactId = submission.Subject.SourceSubject.ArtifactId;
        var artifact = document.Artifacts.Single(candidate => candidate.Envelope.ArtifactId == artifactId);
        var blockers = new List<TirCuratedAcceptanceBlocker>();

        if (history.EffectiveStatus != TirReviewStatus.InReview)
        {
            blockers.Add(new TirCuratedAcceptanceBlocker(
                TirCuratedAcceptanceBlockerCode.ReviewNotOpen,
                history.Transitions.Select(TirCuratedReviewTransitionJson.CreateRecordId)
                    .Order(StringComparer.Ordinal).ToArray()));
        }

        var structuralReports = captured.Records.OfType<TirValidationReportRecord>()
            .Where(report => report.Policy == TirStructuralValidator.Policy)
            .ToArray();
        var requiredGates = new[]
        {
            TirValidationGate.InputSchema,
            TirValidationGate.Identity,
            TirValidationGate.Structural,
            TirValidationGate.SourceProvenance,
            TirValidationGate.ArchitectureAuthority,
        };
        var passingReports = structuralReports.Where(report =>
        {
            var expected = TirStructuralValidator.CreateReport(
                document, artifactId, report.CreatedAt,
                report.CreatedAtSource, report.Actor.Identity);
            return TirReviewCanonicalJson.ComputePayloadSha256(report)
                    == TirReviewCanonicalJson.ComputePayloadSha256(expected)
                && requiredGates.All(gate => report.GateResults.Any(result =>
                    result.Gate == gate && result.Status == TirValidationGateStatus.Passed));
        }).ToArray();
        if (passingReports.Length == 0 || passingReports.Length != structuralReports.Length)
        {
            blockers.Add(new TirCuratedAcceptanceBlocker(
                TirCuratedAcceptanceBlockerCode.StructuralEvidenceMissingOrFailed,
                structuralReports.Select(TirReviewCanonicalJson.CreateRecordId)
                    .Order(StringComparer.Ordinal).ToArray()));
        }

        var verifications = captured.Records.OfType<TirSourceVerificationRecord>().ToArray();
        var visualDependencies = artifact.Envelope.Dependencies
            .Where(static dependency => dependency.Kind is TirDependencyKind.Figure or TirDependencyKind.Table)
            .Select(static dependency => (dependency.Kind, dependency.Target))
            .ToHashSet();
        var sourceComplete = artifact.Envelope.SourceFragments.All(fragment =>
            verifications.Any(record =>
                record.SourceFragment == fragment
                && record.Disposition == TirSourceVerificationDisposition.Verified
                && record.SourceEvidence.RegistryId == document.SourceRegistry.RegistryId
                && record.SourceEvidence.RegistrySha256 == document.SourceRegistry.Sha256
                && record.SourceEvidence.SourceArtifactSha256 == fragment.SourceSha256
                && visualDependencies.SetEquals(record.Dependencies.Select(static dependency =>
                    (dependency.Kind, dependency.Target)))))
            && verifications.All(record => record.Disposition == TirSourceVerificationDisposition.Verified);
        if (!sourceComplete)
        {
            blockers.Add(new TirCuratedAcceptanceBlocker(
                TirCuratedAcceptanceBlockerCode.SourceVerificationMissingOrDisputed,
                artifact.Envelope.SourceFragments.Select(static fragment => fragment.FragmentId)
                    .Order(StringComparer.Ordinal).ToArray()));
        }

        if (captured.Projection.BlockingFindingIds.Count != 0)
        {
            blockers.Add(new TirCuratedAcceptanceBlocker(
                TirCuratedAcceptanceBlockerCode.CapturedFindingRequiresScopedResolution,
                captured.Projection.BlockingFindingIds.ToArray()));
        }

        // These are capability gaps, not caller-supplied evidence flags. The
        // current proposal is opaque text; no ASL semantic model or exact-use
        // closure exists from which an accepting transition could be derived.
        blockers.Add(new TirCuratedAcceptanceBlocker(
            TirCuratedAcceptanceBlockerCode.SemanticValidationUnavailable, []));
        blockers.Add(new TirCuratedAcceptanceBlocker(
            TirCuratedAcceptanceBlockerCode.UseDependencyClosureUnavailable, []));
        blockers.Add(new TirCuratedAcceptanceBlocker(
            TirCuratedAcceptanceBlockerCode.IndependentAcceptanceDecisionUnavailable, []));
        blockers.Add(new TirCuratedAcceptanceBlocker(
            TirCuratedAcceptanceBlockerCode.DisagreementAdjudicationUnavailable, []));

        return new TirCuratedAcceptanceReadiness(
            exactHistorySha256, submission.Subject, submission.Proposal.DeclaredUse,
            blockers.OrderBy(static blocker => blocker.Code).ToArray());
    }
}
