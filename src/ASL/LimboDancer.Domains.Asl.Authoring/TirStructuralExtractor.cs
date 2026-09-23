namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirExtractionOptions(
    TirPackageCandidate PackageCandidate,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static class TirStructuralExtractor
{
    public const string ExtractorName = "LimboDancer.Domains.Asl.Authoring.StructuralExtractor";
    public const string ExtractorVersion = "1.0.0";

    public static string ConfigurationSha256 { get; } = Hashing.Sha256Text(
        "asl-tir-structural-extractor/v1\n"
        + "sourceFragment,rule\n"
        + "publishedIdentifier,chapterContext,sourceOrder\n"
        + "no-semantic-inference");

    public static TirDocument Extract(
        SourceRegistryManifest registry,
        IReadOnlyList<SourceFragment> fragments,
        TirExtractionOptions options)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(fragments);
        ArgumentNullException.ThrowIfNull(options);
        if (options.CreatedAt.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("The reproducible extraction timestamp must be UTC.", nameof(options));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(options.CreatedAtSource);
        var createdBy = new TirCreatedBy(
            ExtractorName,
            ExtractorVersion,
            ConfigurationSha256,
            registry.SourceCommit);
        var sourceArtifacts = fragments
            .Select(fragment => CreateSourceFragmentArtifact(fragment, options, createdBy))
            .ToArray();
        var candidates = LocateRuleCandidates(fragments);
        var ruleArtifacts = CreateRuleArtifacts(candidates, registry.RegistryId, options, createdBy);
        var diagnostics = CreateDiagnostics(ruleArtifacts);
        var artifacts = new List<TirArtifact>(sourceArtifacts.Length + ruleArtifacts.Count);
        artifacts.AddRange(sourceArtifacts);
        artifacts.AddRange(ruleArtifacts.Select(static item => item.Artifact));

        return new TirDocument(
            TirCanonicalJson.SchemaId,
            TirCanonicalJson.SchemaVersion,
            options.PackageCandidate,
            new TirSourceRegistryReference(
                registry.RegistryId,
                Hashing.Sha256Text(ManifestJson.SerializeRegistry(registry)),
                registry.SourceCommit),
            new TirExtractorIdentity(ExtractorName, ExtractorVersion, ConfigurationSha256),
            new TirCanonicalizationProfile(
                TirCanonicalJson.ProfileName,
                TirCanonicalJson.ProfileVersion),
            options.CreatedAt,
            options.CreatedAtSource,
            artifacts,
            diagnostics);
    }

    public static TirDocument SelectRepresentativeSample(TirDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        var selected = new Dictionary<string, TirArtifact>(StringComparer.Ordinal);

        void Add(TirArtifact? artifact)
        {
            if (artifact is not null)
            {
                selected.TryAdd(artifact.Envelope.ArtifactId, artifact);
            }
        }

        var sourceArtifacts = document.Artifacts.OfType<TirSourceFragmentArtifact>().ToArray();
        var ruleArtifacts = document.Artifacts.OfType<TirRuleArtifact>().ToArray();
        foreach (var kind in Enum.GetValues<SourceFragmentKind>())
        {
            Add(sourceArtifacts.FirstOrDefault(artifact => artifact.Payload.FragmentKind == kind));
        }

        foreach (var chapter in "ABCDE")
        {
            Add(ruleArtifacts.FirstOrDefault(artifact =>
                artifact.Envelope.NormalizedPublishedId is not null
                && artifact.Envelope.NormalizedPublishedId.StartsWith(chapter)));
        }

        foreach (var identifier in new[] { "A1.1", "B1.13", "C1.2" })
        {
            Add(ruleArtifacts.FirstOrDefault(artifact => string.Equals(
                artifact.Envelope.NormalizedPublishedId,
                identifier,
                StringComparison.Ordinal)));
        }

        Add(sourceArtifacts.FirstOrDefault(static artifact => artifact.Payload.HasFootnoteMarkers));
        var evidenceFragmentIds = selected.Values
            .OfType<TirRuleArtifact>()
            .SelectMany(static artifact => artifact.Envelope.SourceFragments)
            .Select(static fragment => fragment.FragmentId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var sourceArtifact in sourceArtifacts.Where(artifact =>
            evidenceFragmentIds.Contains(artifact.Envelope.SourceFragments[0].FragmentId)))
        {
            Add(sourceArtifact);
        }

        var selectedIds = selected.Keys.ToHashSet(StringComparer.Ordinal);
        var diagnostics = document.Diagnostics
            .Where(diagnostic => diagnostic.ArtifactId is null || selectedIds.Contains(diagnostic.ArtifactId))
            .ToArray();
        return document with
        {
            Artifacts = selected.Values.ToArray(),
            Diagnostics = diagnostics,
        };
    }

    private static TirSourceFragmentArtifact CreateSourceFragmentArtifact(
        SourceFragment fragment,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var sourceReference = SourceReference(fragment);
        var artifactId = TirArtifactIdentity.Create(
            new TirArtifactIdentityInput(
                TirArtifactKind.SourceFragment,
                fragment.SourceId,
                fragment.Locator.PublishedElementId,
                fragment.Locator.NormalizedElementId,
                [fragment.FragmentId],
                string.Empty));
        var dependencies = fragment.Dependencies
            .Select(dependency => new TirDependency(
                TirDependencyKind.Figure,
                ResolveDependency(fragment.SourcePath, dependency)))
            .ToArray();
        var envelope = Envelope(
            artifactId,
            TirArtifactKind.SourceFragment,
            options,
            fragment.Locator.PublishedElementId,
            fragment.Locator.NormalizedElementId,
            [sourceReference],
            dependencies,
            1m,
            ["exact-source-span"],
            createdBy);
        return new TirSourceFragmentArtifact(
            envelope,
            new TirSourceFragmentPayload(
                fragment.Kind,
                fragment.SourcePath,
                fragment.Locator,
                fragment.HasFootnoteMarkers,
                fragment.VerificationStatus));
    }

    private static List<RuleCandidate> LocateRuleCandidates(IReadOnlyList<SourceFragment> fragments)
    {
        var candidates = new List<RuleCandidate>();
        var index = 0;
        while (index < fragments.Count)
        {
            var fragment = fragments[index];
            if (fragment.Kind != SourceFragmentKind.RuleText
                || fragment.Locator.PublishedElementId is null
                || fragment.Locator.NormalizedElementId is null)
            {
                index++;
                continue;
            }

            var evidence = new List<SourceFragment> { fragment };
            var end = index + 1;
            while (end < fragments.Count
                && fragments[end].Kind != SourceFragmentKind.RuleText
                && string.Equals(
                    fragments[end].Locator.NormalizedElementId,
                    fragment.Locator.NormalizedElementId,
                    StringComparison.Ordinal))
            {
                evidence.Add(fragments[end]);
                end++;
            }

            candidates.Add(new RuleCandidate(
                fragment.Locator.PublishedElementId,
                fragment.Locator.NormalizedElementId,
                HierarchyKey(fragment.Locator.NormalizedElementId),
                evidence));
            index = end;
        }

        return candidates;
    }

    private static List<ExtractedRule> CreateRuleArtifacts(
        List<RuleCandidate> candidates,
        string registryId,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var candidatesByKey = candidates
            .GroupBy(static candidate => candidate.HierarchyKey, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.Ordinal);
        var artifactIds = new Dictionary<RuleCandidate, string>();
        foreach (var candidate in candidates)
        {
            var duplicate = candidatesByKey[candidate.HierarchyKey].Length > 1;
            artifactIds.Add(
                candidate,
                TirArtifactIdentity.Create(
                    new TirArtifactIdentityInput(
                        TirArtifactKind.Rule,
                        registryId,
                        candidate.PublishedId,
                        candidate.NormalizedId,
                        candidate.Evidence.Select(static fragment => fragment.FragmentId).ToArray(),
                        duplicate ? candidate.Evidence[0].FragmentId : string.Empty)));
        }

        var siblingOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        var extracted = new List<ExtractedRule>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var parentKey = ParentHierarchyKey(candidate.HierarchyKey);
            var siblingKey = parentKey ?? candidate.HierarchyKey[..1];
            var order = siblingOrder.GetValueOrDefault(siblingKey);
            siblingOrder[siblingKey] = order + 1;
            var hierarchy = ResolveHierarchy(parentKey, candidatesByKey, artifactIds);
            var sourceReferences = candidate.Evidence.Select(SourceReference).ToArray();
            var dependencies = candidate.Evidence
                .SelectMany(fragment => fragment.Dependencies.Select(dependency => new TirDependency(
                    TirDependencyKind.Figure,
                    ResolveDependency(fragment.SourcePath, dependency))))
                .Distinct()
                .ToArray();
            var confidenceBasis = new List<string> { "exact-published-identifier-match" };
            if (!string.Equals(candidate.PublishedId, candidate.NormalizedId, StringComparison.Ordinal))
            {
                confidenceBasis.Add("chapter-local-identifier-normalized");
            }

            var envelope = Envelope(
                artifactIds[candidate],
                TirArtifactKind.Rule,
                options,
                candidate.PublishedId,
                candidate.NormalizedId,
                sourceReferences,
                dependencies,
                1m,
                confidenceBasis,
                createdBy);
            var artifact = new TirRuleArtifact(
                envelope,
                new TirRulePayload(
                    hierarchy.ParentArtifactId,
                    hierarchy.Status,
                    hierarchy.Basis,
                    order));
            extracted.Add(new ExtractedRule(candidate, artifact, parentKey));
        }

        return extracted;
    }

    private static List<TirDiagnostic> CreateDiagnostics(List<ExtractedRule> extractedRules)
    {
        var diagnostics = new List<TirDiagnostic>();
        foreach (var group in extractedRules.GroupBy(
            static item => item.Candidate.HierarchyKey,
            StringComparer.Ordinal))
        {
            var duplicates = group.ToArray();
            if (duplicates.Length < 2)
            {
                continue;
            }

            foreach (var duplicate in duplicates)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-DUPLICATE-NORMALIZED-ID",
                    TirDiagnosticSeverity.Error,
                    duplicate.Artifact.Envelope.ArtifactId,
                    $"Normalized rule identifier '{duplicate.Candidate.NormalizedId}' occurs more than once."));
            }
        }

        foreach (var rule in extractedRules)
        {
            if (rule.Artifact.Payload.HierarchyStatus == TirHierarchyStatus.Missing)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-MISSING-PARENT",
                    TirDiagnosticSeverity.Warning,
                    rule.Artifact.Envelope.ArtifactId,
                    $"Parent rule '{rule.ParentKey}' was not found for '{rule.Candidate.NormalizedId}'."));
            }
            else if (rule.Artifact.Payload.HierarchyStatus == TirHierarchyStatus.Ambiguous)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-AMBIGUOUS-PARENT",
                    TirDiagnosticSeverity.Error,
                    rule.Artifact.Envelope.ArtifactId,
                    $"Parent rule '{rule.ParentKey}' is ambiguous for '{rule.Candidate.NormalizedId}'."));
            }
        }

        return diagnostics;
    }

    private static HierarchyResolution ResolveHierarchy(
        string? parentKey,
        Dictionary<string, RuleCandidate[]> candidatesByKey,
        Dictionary<RuleCandidate, string> artifactIds)
    {
        if (parentKey is null)
        {
            return new HierarchyResolution(null, TirHierarchyStatus.Root, []);
        }

        if (!candidatesByKey.TryGetValue(parentKey, out var parents))
        {
            return new HierarchyResolution(
                null,
                TirHierarchyStatus.Missing,
                [TirHierarchyBasis.PublishedIdentifier]);
        }

        if (parents.Length > 1)
        {
            return new HierarchyResolution(
                null,
                TirHierarchyStatus.Ambiguous,
                [TirHierarchyBasis.PublishedIdentifier, TirHierarchyBasis.ChapterContext]);
        }

        return new HierarchyResolution(
            artifactIds[parents[0]],
            TirHierarchyStatus.Supported,
            [TirHierarchyBasis.PublishedIdentifier, TirHierarchyBasis.ChapterContext]);
    }

    private static TirArtifactEnvelope Envelope(
        string artifactId,
        TirArtifactKind artifactKind,
        TirExtractionOptions options,
        string? publishedId,
        string? normalizedPublishedId,
        IReadOnlyList<TirSourceFragmentReference> sourceFragments,
        IReadOnlyList<TirDependency> dependencies,
        decimal confidence,
        IReadOnlyList<string> confidenceBasis,
        TirCreatedBy createdBy)
    {
        return new TirArtifactEnvelope(
            artifactId,
            artifactKind,
            options.PackageCandidate,
            publishedId,
            normalizedPublishedId,
            null,
            sourceFragments,
            dependencies,
            TirArtifactOrigin.Extracted,
            TirFormalizationStatus.Unmodeled,
            TirReviewStatus.Captured,
            confidence,
            confidenceBasis,
            createdBy,
            options.CreatedAt,
            []);
    }

    private static TirSourceFragmentReference SourceReference(SourceFragment fragment)
    {
        return new TirSourceFragmentReference(
            fragment.FragmentId,
            fragment.SourceId,
            fragment.SourceSha256,
            fragment.ContentSha256,
            fragment.Locator.StartLine,
            fragment.Locator.EndLine);
    }

    private static string HierarchyKey(string normalizedId)
    {
        return normalizedId.Length > 2
            && char.IsLetter(normalizedId[0])
            && normalizedId[1] == '.'
            ? normalizedId.Remove(1, 1)
            : normalizedId;
    }

    private static string? ParentHierarchyKey(string hierarchyKey)
    {
        var separator = hierarchyKey.LastIndexOf('.');
        return separator < 0 ? null : hierarchyKey[..separator];
    }

    private static string ResolveDependency(string sourcePath, string dependency)
    {
        var separator = sourcePath.LastIndexOf('/');
        var sourceDirectory = separator < 0 ? string.Empty : sourcePath[..separator];
        var segments = new List<string>();
        foreach (var segment in $"{sourceDirectory}/{dependency}".Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (segments.Count == 0)
                {
                    throw new InvalidOperationException($"Dependency escapes the repository root: {dependency}");
                }

                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    private sealed record RuleCandidate(
        string PublishedId,
        string NormalizedId,
        string HierarchyKey,
        List<SourceFragment> Evidence);

    private sealed record ExtractedRule(
        RuleCandidate Candidate,
        TirRuleArtifact Artifact,
        string? ParentKey);

    private sealed record HierarchyResolution(
        string? ParentArtifactId,
        TirHierarchyStatus Status,
        TirHierarchyBasis[] Basis);
}
