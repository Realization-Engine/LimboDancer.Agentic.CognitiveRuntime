namespace LimboDancer.Domains.Asl.Authoring;

public static class TirReviewSubjects
{
    public static TirReviewSubjectReference Create(TirDocument document, string artifactId)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);

        var documentSha256 = TirCanonicalJson.ComputePayloadSha256(document);
        var artifact = document.Artifacts.SingleOrDefault(candidate => string.Equals(
            candidate.Envelope.ArtifactId,
            artifactId,
            StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"TIR artifact was not found: {artifactId}.");
        return new TirReviewSubjectReference(
            documentSha256,
            artifact.Envelope.ArtifactId,
            TirCanonicalJson.ComputeArtifactSha256(document, artifactId),
            document.SchemaId,
            document.PackageCandidate);
    }
}
