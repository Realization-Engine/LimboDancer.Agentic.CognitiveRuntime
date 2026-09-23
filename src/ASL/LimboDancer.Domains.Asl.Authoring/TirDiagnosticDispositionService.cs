namespace LimboDancer.Domains.Asl.Authoring;

public static class TirDiagnosticDispositionService
{
    public static TirDiagnosticDispositionRecord CreateForExtractedDiagnostic(
        TirDocument document,
        string artifactId,
        TirDiagnostic diagnostic,
        TirFindingDisposition disposition,
        string rationale,
        IReadOnlyList<string> evidenceRefs,
        string? replacementRef,
        DateTimeOffset createdAt,
        string createdAtSource,
        string actorIdentity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(diagnostic);
        if (!document.Diagnostics.Contains(diagnostic)
            || diagnostic.ArtifactId is not null
            && !string.Equals(diagnostic.ArtifactId, artifactId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The extracted diagnostic does not belong to the exact disposition subject.");
        }

        var subject = TirReviewSubjects.Create(document, artifactId);
        return Create(
            subject,
            new TirFindingReference(
                TirFindingOrigin.ExtractedDiagnostic,
                TirDiagnosticIdentity.Create(subject, diagnostic),
                diagnostic.Code,
                diagnostic.Severity,
                null),
            disposition,
            rationale,
            evidenceRefs,
            replacementRef,
            createdAt,
            createdAtSource,
            actorIdentity,
            []);
    }

    public static TirDiagnosticDispositionRecord CreateForValidationFinding(
        TirValidationReportRecord report,
        TirValidationFinding finding,
        TirFindingDisposition disposition,
        string rationale,
        IReadOnlyList<string> evidenceRefs,
        string? replacementRef,
        DateTimeOffset createdAt,
        string createdAtSource,
        string actorIdentity)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(finding);
        if (!report.Findings.Contains(finding))
        {
            throw new InvalidOperationException(
                "The validation finding is not present in the referenced report.");
        }

        var reportRef = TirReviewCanonicalJson.CreateRecordId(report);
        return Create(
            report.Subject,
            new TirFindingReference(
                TirFindingOrigin.ValidationFinding,
                finding.FindingId,
                finding.Code,
                finding.Severity,
                reportRef),
            disposition,
            rationale,
            evidenceRefs,
            replacementRef,
            createdAt,
            createdAtSource,
            actorIdentity,
            [reportRef]);
    }

    public static bool IsOpen(TirDiagnosticDispositionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.Disposition is TirFindingDisposition.Deferred
            or TirFindingDisposition.AcceptedLimitation;
    }

    public static bool BlocksDefinitiveUse(TirDiagnosticDispositionRecord record)
    {
        return IsOpen(record);
    }

    private static TirDiagnosticDispositionRecord Create(
        TirReviewSubjectReference subject,
        TirFindingReference finding,
        TirFindingDisposition disposition,
        string rationale,
        IReadOnlyList<string> evidenceRefs,
        string? replacementRef,
        DateTimeOffset createdAt,
        string createdAtSource,
        string actorIdentity,
        IReadOnlyList<string> prerequisiteRecordRefs)
    {
        var record = new TirDiagnosticDispositionRecord(
            subject,
            createdAt,
            createdAtSource,
            new TirReviewActor(actorIdentity, TirReviewActorRole.DomainReviewer),
            null,
            prerequisiteRecordRefs,
            finding,
            disposition,
            rationale,
            evidenceRefs,
            replacementRef);
        _ = TirReviewCanonicalJson.SerializePayload(record);
        return record;
    }
}
