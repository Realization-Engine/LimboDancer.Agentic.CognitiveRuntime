namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirReviewProjection(
    TirReviewStatus EffectiveReviewStatus,
    TirFormalizationStatus EffectiveFormalizationStatus,
    IReadOnlyList<string> OpenFindingIds,
    IReadOnlyList<string> BlockingFindingIds);

public sealed record TirReviewBundle(
    TirDocument Document,
    TirReviewSubjectReference Subject,
    TirValidationPolicy Policy,
    string DeclaredUse,
    IReadOnlyList<string> CoverageRefs,
    IReadOnlyList<TirReviewRecord> Records,
    TirReviewProjection Projection,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);
