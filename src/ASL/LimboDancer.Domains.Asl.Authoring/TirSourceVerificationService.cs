using System.Text;

namespace LimboDancer.Domains.Asl.Authoring;

public static class TirSourceVerificationService
{
    public static TirSourceVerificationRecord CreateRecord(
        TirDocument document,
        string artifactId,
        SourceRegistryManifest registry,
        SourceFragment fragment,
        string comparisonMethod,
        TirSourceVerificationDisposition disposition,
        string? observedDiscrepancy,
        IReadOnlyList<string> correctionProposalRefs,
        DateTimeOffset createdAt,
        string createdAtSource,
        string actorIdentity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(fragment);
        ArgumentNullException.ThrowIfNull(correctionProposalRefs);

        var artifact = document.Artifacts.SingleOrDefault(candidate => string.Equals(
            candidate.Envelope.ArtifactId,
            artifactId,
            StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"TIR artifact was not found: {artifactId}.");
        var sourceReference = artifact.Envelope.SourceFragments.SingleOrDefault(candidate =>
            string.Equals(candidate.FragmentId, fragment.FragmentId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                "The source fragment is not evidence for the exact verification subject.");
        ValidateRegistry(document, registry);
        var sourceArtifact = ValidateFragment(registry, fragment, sourceReference);
        var (verifiedDependencies, missingDependencies) = ResolveDependencies(
            artifact.Envelope.Dependencies,
            registry);
        if (missingDependencies.Length > 0
            && disposition != TirSourceVerificationDisposition.DependencyMissing)
        {
            throw new InvalidOperationException(
                $"Missing source dependencies require a dependency-missing disposition: {string.Join(", ", missingDependencies)}.");
        }

        if (missingDependencies.Length == 0
            && disposition == TirSourceVerificationDisposition.DependencyMissing)
        {
            throw new InvalidOperationException(
                "A dependency-missing disposition requires an unresolved figure or table dependency.");
        }

        var record = new TirSourceVerificationRecord(
            TirReviewSubjects.Create(document, artifactId),
            createdAt,
            createdAtSource,
            new TirReviewActor(actorIdentity, TirReviewActorRole.SourceVerifier),
            null,
            [],
            sourceReference,
            new TirSourceEvidenceContext(
                registry.RegistryId,
                Hashing.Sha256Text(ManifestJson.SerializeRegistry(registry)),
                registry.Edition,
                sourceArtifact.Path,
                sourceArtifact.Sha256,
                fragment.Locator.StartPage,
                fragment.Locator.EndPage),
            verifiedDependencies,
            comparisonMethod,
            disposition,
            observedDiscrepancy,
            correctionProposalRefs);
        _ = TirReviewCanonicalJson.SerializePayload(record);
        return record;
    }

    private static void ValidateRegistry(
        TirDocument document,
        SourceRegistryManifest registry)
    {
        var registrySha256 = Hashing.Sha256Text(ManifestJson.SerializeRegistry(registry));
        if (!string.Equals(
                document.SourceRegistry.RegistryId,
                registry.RegistryId,
                StringComparison.Ordinal)
            || !string.Equals(
                document.SourceRegistry.Sha256,
                registrySha256,
                StringComparison.Ordinal)
            || !string.Equals(
                document.SourceRegistry.SourceCommit,
                registry.SourceCommit,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The source registry does not reproduce the TIR document registry reference.");
        }
    }

    private static SourceArtifact ValidateFragment(
        SourceRegistryManifest registry,
        SourceFragment fragment,
        TirSourceFragmentReference sourceReference)
    {
        var sourceArtifact = registry.Artifacts.SingleOrDefault(candidate =>
            string.Equals(candidate.SourceId, fragment.SourceId, StringComparison.Ordinal))
            ?? throw new InvalidOperationException(
                $"The source fragment source is absent from the registry: {fragment.SourceId}.");
        if (!string.Equals(sourceArtifact.Path, fragment.SourcePath, StringComparison.Ordinal)
            || !string.Equals(sourceArtifact.Sha256, fragment.SourceSha256, StringComparison.Ordinal)
            || !string.Equals(
                Hashing.Sha256Text(fragment.Content),
                fragment.ContentSha256,
                StringComparison.Ordinal)
            || !string.Equals(sourceReference.SourceId, fragment.SourceId, StringComparison.Ordinal)
            || !string.Equals(
                sourceReference.SourceSha256,
                fragment.SourceSha256,
                StringComparison.Ordinal)
            || !string.Equals(
                sourceReference.ContentSha256,
                fragment.ContentSha256,
                StringComparison.Ordinal)
            || sourceReference.StartLine < fragment.Locator.StartLine
            || sourceReference.EndLine > fragment.Locator.EndLine)
        {
            throw new InvalidOperationException(
                "The source fragment does not reproduce its registry and TIR evidence.");
        }

        if (sourceReference.EndUtf8ByteOffsetExclusive is { } endOffset
            && endOffset > Encoding.UTF8.GetByteCount(fragment.Content))
        {
            throw new InvalidOperationException(
                "The TIR sub-fragment span exceeds the exact registered fragment bytes.");
        }

        return sourceArtifact;
    }

    private static (TirVerifiedDependency[] Verified, string[] Missing) ResolveDependencies(
        IReadOnlyList<TirDependency> dependencies,
        SourceRegistryManifest registry)
    {
        var verified = new List<TirVerifiedDependency>();
        var missing = new List<string>();
        foreach (var dependency in dependencies.Where(static dependency =>
            dependency.Kind is TirDependencyKind.Figure or TirDependencyKind.Table))
        {
            var sourceArtifact = registry.Artifacts.SingleOrDefault(candidate => string.Equals(
                candidate.Path,
                dependency.Target,
                StringComparison.Ordinal));
            if (sourceArtifact is null)
            {
                missing.Add(dependency.Target);
                continue;
            }

            verified.Add(
                new TirVerifiedDependency(
                    dependency.Kind,
                    dependency.Target,
                    sourceArtifact.Sha256));
        }

        return (
            verified
                .OrderBy(static dependency => dependency.Kind)
                .ThenBy(static dependency => dependency.Target, StringComparer.Ordinal)
                .ToArray(),
            missing.Order(StringComparer.Ordinal).ToArray());
    }
}
