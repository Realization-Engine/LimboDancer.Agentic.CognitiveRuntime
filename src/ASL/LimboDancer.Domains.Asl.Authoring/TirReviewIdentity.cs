namespace LimboDancer.Domains.Asl.Authoring;

public static class TirReviewIdentity
{
    public static string CreateFindingId(
        TirReviewSubjectReference subject,
        TirValidationPolicy policy,
        TirValidationGate gate,
        string code,
        string? artifactId,
        IReadOnlyList<string> evidenceRefs)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentNullException.ThrowIfNull(evidenceRefs);

        var material = string.Join(
            "\n",
            [
                "asl-tir-review-finding/v1",
                subject.TirDocumentSha256,
                subject.ArtifactId,
                subject.ArtifactSha256,
                subject.SchemaId,
                subject.PackageCandidate.DomainId,
                subject.PackageCandidate.PackageId,
                subject.PackageCandidate.Version,
                policy.PolicyId,
                policy.Version,
                policy.ConfigurationSha256,
                gate.ToString(),
                code,
                artifactId ?? string.Empty,
                string.Join('\n', evidenceRefs.Order(StringComparer.Ordinal)),
            ]);
        return $"asl-tir-finding:sha256:{Hashing.Sha256Text(material)}";
    }
}
