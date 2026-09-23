namespace LimboDancer.Domains.Asl.Authoring;

public enum TirArtifactKind
{
    SourceFragment,
    Rule,
    Definition,
    Condition,
    Effect,
    Exception,
    CrossReference,
    Example,
    Table,
    PhaseRestriction,
    Term,
    EntityType,
    Property,
    Relation,
    Enumeration,
    ValidationShape,
    CalculationRequirement,
}

public enum TirArtifactOrigin
{
    Extracted,
    Curated,
    Derived,
}

public enum TirFormalizationStatus
{
    Unmodeled,
    Partial,
    Validated,
}

public enum TirReviewStatus
{
    Captured,
    Proposed,
    InReview,
    Accepted,
    Rejected,
    Superseded,
}

public enum TirDependencyKind
{
    Source,
    Artifact,
    Figure,
    Table,
    Footnote,
}

public enum TirHierarchyBasis
{
    PublishedIdentifier,
    ChapterContext,
    HeadingPath,
    SourceOrder,
}

public enum TirHierarchyStatus
{
    Supported,
    Candidate,
    Missing,
    Ambiguous,
}

public enum TirReferenceResolutionStatus
{
    Resolved,
    Ambiguous,
    Missing,
    Unresolved,
}

public enum TirDiagnosticSeverity
{
    Information,
    Warning,
    Error,
}

public sealed record TirPackageCandidate(string DomainId, string PackageId, string Version);

public sealed record TirSourceRegistryReference(string RegistryId, string Sha256, string SourceCommit);

public sealed record TirExtractorIdentity(string Name, string Version, string ConfigurationSha256);

public sealed record TirCanonicalizationProfile(string Name, string Version);

public sealed record TirSourceFragmentReference(
    string FragmentId,
    string SourceId,
    string SourceSha256,
    string ContentSha256,
    int StartLine,
    int EndLine);

public sealed record TirDependency(TirDependencyKind Kind, string Target);

public sealed record TirCreatedBy(
    string Name,
    string Version,
    string ConfigurationSha256,
    string SourceRevision);

public sealed record TirArtifactEnvelope(
    string ArtifactId,
    TirArtifactKind ArtifactKind,
    TirPackageCandidate PackageCandidate,
    string? PublishedId,
    string? NormalizedPublishedId,
    string? SemanticId,
    IReadOnlyList<TirSourceFragmentReference> SourceFragments,
    IReadOnlyList<TirDependency> Dependencies,
    TirArtifactOrigin Origin,
    TirFormalizationStatus FormalizationStatus,
    TirReviewStatus ReviewStatus,
    decimal Confidence,
    IReadOnlyList<string> ConfidenceBasis,
    TirCreatedBy CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> ReviewRecordRefs);

public abstract record TirArtifact(TirArtifactEnvelope Envelope);

public sealed record TirSourceFragmentPayload(
    SourceFragmentKind FragmentKind,
    string SourcePath,
    SourceLocator Locator,
    bool HasFootnoteMarkers,
    VerificationStatus VerificationStatus);

public sealed record TirSourceFragmentArtifact(
    TirArtifactEnvelope Envelope,
    TirSourceFragmentPayload Payload)
    : TirArtifact(Envelope);

public sealed record TirRulePayload(
    string? DirectParentArtifactId,
    TirHierarchyStatus HierarchyStatus,
    IReadOnlyList<TirHierarchyBasis> HierarchyBasis,
    int? SiblingOrder);

public sealed record TirRuleArtifact(TirArtifactEnvelope Envelope, TirRulePayload Payload)
    : TirArtifact(Envelope);

public sealed record TirCrossReferencePayload(
    string ReferenceText,
    string ContainingFragmentId,
    string? NormalizedTargetCandidate,
    TirReferenceResolutionStatus ResolutionStatus,
    string? ResolvedTargetArtifactId);

public sealed record TirCrossReferenceArtifact(
    TirArtifactEnvelope Envelope,
    TirCrossReferencePayload Payload)
    : TirArtifact(Envelope);

public sealed record TirExamplePayload(IReadOnlyList<string> IllustratesArtifactIds);

public sealed record TirExampleArtifact(TirArtifactEnvelope Envelope, TirExamplePayload Payload)
    : TirArtifact(Envelope);

public sealed record TirTablePayload(
    IReadOnlyList<string> NoteFragmentIds,
    bool StructureVerified);

public sealed record TirTableArtifact(TirArtifactEnvelope Envelope, TirTablePayload Payload)
    : TirArtifact(Envelope);

public sealed record TirDiagnostic(
    string Code,
    TirDiagnosticSeverity Severity,
    string? ArtifactId,
    string Message);

public sealed record TirDocument(
    string SchemaId,
    string SchemaVersion,
    TirPackageCandidate PackageCandidate,
    TirSourceRegistryReference SourceRegistry,
    TirExtractorIdentity Extractor,
    TirCanonicalizationProfile Canonicalization,
    DateTimeOffset CreatedAt,
    string CreatedAtSource,
    IReadOnlyList<TirArtifact> Artifacts,
    IReadOnlyList<TirDiagnostic> Diagnostics);
