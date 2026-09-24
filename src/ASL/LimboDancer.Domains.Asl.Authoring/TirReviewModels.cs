namespace LimboDancer.Domains.Asl.Authoring;

public enum TirReviewRecordKind
{
    ValidationReport,
    SourceVerification,
    DiagnosticDisposition,
    Review,
    Adjudication,
}

public enum TirReviewActorRole
{
    Validator,
    SourceVerifier,
    SemanticAuthor,
    DomainReviewer,
    Adjudicator,
    ReleaseApprover,
}

public enum TirValidationGate
{
    InputSchema,
    Identity,
    Structural,
    SourceProvenance,
    Semantic,
    Review,
    ArchitectureAuthority,
}

public enum TirValidationGateStatus
{
    Passed,
    Failed,
    NotApplicable,
}

public enum TirFindingOrigin
{
    ExtractedDiagnostic,
    ValidationFinding,
}

public enum TirFindingDisposition
{
    Resolved,
    NotApplicable,
    Deferred,
    AcceptedLimitation,
}

public enum TirSourceVerificationDisposition
{
    Verified,
    Mismatch,
    DependencyMissing,
    Indeterminate,
}

public enum TirReviewDecision
{
    Approve,
    Reject,
    RequestChanges,
    Abstain,
}

public sealed record TirReviewSubjectReference(
    string TirDocumentSha256,
    string ArtifactId,
    string ArtifactSha256,
    string SchemaId,
    TirPackageCandidate PackageCandidate);

public sealed record TirReviewActor(string Identity, TirReviewActorRole Role);

public sealed record TirReviewTool(
    string Name,
    string Version,
    string ConfigurationSha256);

public sealed record TirValidationPolicy(
    string PolicyId,
    string Version,
    string ConfigurationSha256);

public sealed record TirValidationFinding(
    string FindingId,
    string Code,
    TirDiagnosticSeverity Severity,
    TirValidationGate Gate,
    string? ArtifactId,
    string Message,
    IReadOnlyList<string> EvidenceRefs);

public sealed record TirValidationGateResult(
    TirValidationGate Gate,
    TirValidationGateStatus Status,
    IReadOnlyList<string> FindingIds);

public sealed record TirSourceEvidenceContext(
    string RegistryId,
    string RegistrySha256,
    string Edition,
    string SourcePath,
    string SourceArtifactSha256,
    int? StartPage,
    int? EndPage);

public sealed record TirVerifiedDependency(
    TirDependencyKind Kind,
    string Target,
    string Sha256);

public sealed record TirFindingReference(
    TirFindingOrigin Origin,
    string FindingId,
    string Code,
    TirDiagnosticSeverity Severity,
    string? ReportRecordRef);

public abstract record TirReviewRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool? Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs)
{
    public abstract TirReviewRecordKind Kind
    {
        get;
    }
}

public sealed record TirValidationReportRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs,
    TirValidationPolicy Policy,
    IReadOnlyList<TirValidationGateResult> GateResults,
    IReadOnlyList<TirValidationFinding> Findings)
    : TirReviewRecord(
        Subject,
        CreatedAt,
        CreatedAtSource,
        Actor,
        Tool,
        PrerequisiteRecordRefs)
{
    public override TirReviewRecordKind Kind => TirReviewRecordKind.ValidationReport;
}

public sealed record TirSourceVerificationRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool? Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs,
    TirSourceFragmentReference SourceFragment,
    TirSourceEvidenceContext SourceEvidence,
    IReadOnlyList<TirVerifiedDependency> Dependencies,
    string ComparisonMethod,
    TirSourceVerificationDisposition Disposition,
    string? ObservedDiscrepancy,
    IReadOnlyList<string> CorrectionProposalRefs)
    : TirReviewRecord(
        Subject,
        CreatedAt,
        CreatedAtSource,
        Actor,
        Tool,
        PrerequisiteRecordRefs)
{
    public override TirReviewRecordKind Kind => TirReviewRecordKind.SourceVerification;
}

public sealed record TirDiagnosticDispositionRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool? Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs,
    TirFindingReference Finding,
    TirFindingDisposition Disposition,
    string Rationale,
    IReadOnlyList<string> EvidenceRefs,
    string? ReplacementRef)
    : TirReviewRecord(
        Subject,
        CreatedAt,
        CreatedAtSource,
        Actor,
        Tool,
        PrerequisiteRecordRefs)
{
    public override TirReviewRecordKind Kind => TirReviewRecordKind.DiagnosticDisposition;
}

public sealed record TirReviewDecisionRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool? Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs,
    TirReviewStatus PriorStatus,
    TirReviewStatus RequestedStatus,
    TirReviewDecision Decision,
    IReadOnlyList<string> SourceVerificationRecordRefs,
    IReadOnlyList<string> ValidationReportRefs,
    IReadOnlyList<string> DiagnosticDispositionRefs,
    IReadOnlyList<string> ReviewedDependencyRefs,
    string Rationale,
    IReadOnlyList<string> EvidenceRefs)
    : TirReviewRecord(
        Subject,
        CreatedAt,
        CreatedAtSource,
        Actor,
        Tool,
        PrerequisiteRecordRefs)
{
    public override TirReviewRecordKind Kind => TirReviewRecordKind.Review;
}

public sealed record TirAdjudicationRecord(
    TirReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    TirReviewActor Actor,
    TirReviewTool? Tool,
    IReadOnlyList<string> PrerequisiteRecordRefs,
    IReadOnlyList<string> ConflictingReviewRecordRefs,
    IReadOnlyList<string> DisputedQuestions,
    TirReviewDecision Decision,
    string Rationale,
    IReadOnlyList<string> EvidenceRefs,
    TirReviewStatus? AuthorizedStatus)
    : TirReviewRecord(
        Subject,
        CreatedAt,
        CreatedAtSource,
        Actor,
        Tool,
        PrerequisiteRecordRefs)
{
    public override TirReviewRecordKind Kind => TirReviewRecordKind.Adjudication;
}
