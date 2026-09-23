using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirExtractionOptions(
    TirPackageCandidate PackageCandidate,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static partial class TirStructuralExtractor
{
    public const string ExtractorName = "LimboDancer.Domains.Asl.Authoring.StructuralExtractor";
    public const string ExtractorVersion = "1.4.0";

    public static string ConfigurationSha256
    {
        get;
    } = Hashing.Sha256Text(
        "asl-tir-structural-extractor/v1.4\n"
        + "sourceFragment,section,rule,crossReference,example,table\n"
        + "publishedIdentifier,chapterContext,majorSectionBoundary,digitHierarchy,zeroPaddedChild,sourceOrder,utf8ByteSpans,explicitMarkers\n"
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
        var sectionArtifacts = CreateSectionArtifacts(registry, fragments, options, createdBy);
        var candidates = LocateRuleCandidates(fragments);
        var ruleArtifacts = CreateRuleArtifacts(
            candidates,
            sectionArtifacts,
            registry.RegistryId,
            options,
            createdBy);
        var crossReferences = CreateCrossReferenceArtifacts(
            fragments,
            ruleArtifacts,
            registry.RegistryId,
            options,
            createdBy);
        var examples = CreateExampleArtifacts(
            fragments,
            ruleArtifacts,
            registry.RegistryId,
            options,
            createdBy);
        var tables = CreateTableArtifacts(fragments, registry.RegistryId, options, createdBy);
        var diagnostics = CreateDiagnostics(sectionArtifacts, ruleArtifacts, crossReferences);
        var artifacts = new List<TirArtifact>(
            sourceArtifacts.Length
            + sectionArtifacts.Count
            + ruleArtifacts.Count
            + crossReferences.Count
            + examples.Count
            + tables.Count);
        artifacts.AddRange(sourceArtifacts);
        artifacts.AddRange(sectionArtifacts.Select(static item => item.Artifact));
        artifacts.AddRange(ruleArtifacts.Select(static item => item.Artifact));
        artifacts.AddRange(crossReferences);
        artifacts.AddRange(examples);
        artifacts.AddRange(tables);

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
        var sectionArtifacts = document.Artifacts.OfType<TirSectionArtifact>().ToArray();
        var ruleArtifacts = document.Artifacts.OfType<TirRuleArtifact>().ToArray();
        var crossReferences = document.Artifacts.OfType<TirCrossReferenceArtifact>().ToArray();
        var examples = document.Artifacts.OfType<TirExampleArtifact>().ToArray();
        var tables = document.Artifacts.OfType<TirTableArtifact>().ToArray();
        foreach (var kind in Enum.GetValues<SourceFragmentKind>())
        {
            Add(sourceArtifacts.FirstOrDefault(artifact => artifact.Payload.FragmentKind == kind));
        }

        foreach (var chapter in "ABCDE")
        {
            Add(sectionArtifacts.FirstOrDefault(artifact =>
                artifact.Envelope.NormalizedPublishedId is not null
                && artifact.Envelope.NormalizedPublishedId.StartsWith(chapter)));
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
        Add(crossReferences.FirstOrDefault(static artifact =>
            artifact.Payload.ResolutionStatus == TirReferenceResolutionStatus.Resolved));
        Add(crossReferences.FirstOrDefault(static artifact =>
            artifact.Payload.ResolutionStatus == TirReferenceResolutionStatus.Missing));
        Add(examples.FirstOrDefault(static artifact => artifact.Payload.IllustratesArtifactIds.Count > 0)
            ?? examples.FirstOrDefault());
        Add(tables.FirstOrDefault());

        var artifactById = document.Artifacts.ToDictionary(
            static artifact => artifact.Envelope.ArtifactId,
            StringComparer.Ordinal);
        var relatedArtifactIds = selected.Values
            .OfType<TirCrossReferenceArtifact>()
            .Select(static artifact => artifact.Payload.ResolvedTargetArtifactId)
            .Where(static artifactId => artifactId is not null)
            .Concat(selected.Values
                .OfType<TirExampleArtifact>()
                .SelectMany(static artifact => artifact.Payload.IllustratesArtifactIds))
            .Concat(selected.Values
                .OfType<TirRuleArtifact>()
                .Select(static artifact => artifact.Payload.DirectParentArtifactId)
                .Where(static artifactId => artifactId is not null))
            .ToArray();
        foreach (var artifactId in relatedArtifactIds)
        {
            Add(artifactById[artifactId!]);
        }

        var evidenceFragmentIds = selected.Values
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

    private static List<ExtractedSection> CreateSectionArtifacts(
        SourceRegistryManifest registry,
        IReadOnlyList<SourceFragment> fragments,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var chapterBySourceId = registry.Artifacts
            .Where(static artifact => artifact.Chapter is not null)
            .ToDictionary(
                static artifact => artifact.SourceId,
                static artifact => artifact.Chapter!,
                StringComparer.Ordinal);
        var siblingOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        var extracted = new List<ExtractedSection>();
        foreach (var fragment in fragments)
        {
            if (!TryGetSectionBoundary(fragment, out var boundary)
                || !chapterBySourceId.TryGetValue(fragment.SourceId, out var chapter))
            {
                continue;
            }

            var publishedId = boundary.PublishedId;
            var normalizedId = $"{chapter.ToUpperInvariant()}{publishedId}";
            var order = siblingOrder.GetValueOrDefault(chapter);
            siblingOrder[chapter] = order + 1;
            var artifactId = TirArtifactIdentity.Create(
                new TirArtifactIdentityInput(
                    TirArtifactKind.Section,
                    registry.RegistryId,
                    publishedId,
                    normalizedId,
                    [fragment.FragmentId],
                    string.Empty));
            var envelope = Envelope(
                artifactId,
                TirArtifactKind.Section,
                options,
                publishedId,
                normalizedId,
                [SourceReference(
                    fragment,
                    boundary.StartCharacterOffset,
                    boundary.EndCharacterOffsetExclusive)],
                [],
                1m,
                boundary.ConfidenceBasis,
                createdBy);
            var artifact = new TirSectionArtifact(
                envelope,
                new TirSectionPayload(
                    boundary.Title,
                    boundary.Kind,
                    boundary.SourceHeadingLevel,
                    null,
                    TirHierarchyStatus.Root,
                    boundary.HierarchyBasis,
                    order));
            extracted.Add(new ExtractedSection(normalizedId, artifact));
        }

        return extracted;
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
                evidence));
            index = end;
        }

        return candidates;
    }

    private static List<ExtractedRule> CreateRuleArtifacts(
        List<RuleCandidate> candidates,
        List<ExtractedSection> sections,
        string registryId,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var candidatesByKey = candidates
            .GroupBy(static candidate => candidate.NormalizedId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.Ordinal);
        var artifactIds = new Dictionary<RuleCandidate, string>();
        foreach (var candidate in candidates)
        {
            var duplicate = candidatesByKey[candidate.NormalizedId].Length > 1;
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

        var parentArtifactsByKey = candidatesByKey.ToDictionary(
            static item => item.Key,
            item => item.Value
                .Select(candidate => new StructuralParent(
                    artifactIds[candidate],
                    [TirHierarchyBasis.PublishedIdentifier, TirHierarchyBasis.ChapterContext]))
                .ToList(),
            StringComparer.Ordinal);
        foreach (var section in sections)
        {
            if (!parentArtifactsByKey.TryGetValue(section.NormalizedId, out var artifacts))
            {
                artifacts = [];
                parentArtifactsByKey.Add(section.NormalizedId, artifacts);
            }

            artifacts.Add(new StructuralParent(
                section.Artifact.Envelope.ArtifactId,
                section.Artifact.Payload.HierarchyBasis));
        }

        var siblingOrder = new Dictionary<string, int>(StringComparer.Ordinal);
        var extracted = new List<ExtractedRule>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var parentKeys = ParentHierarchyKeys(candidate.NormalizedId);
            var hierarchy = ResolveHierarchy(parentKeys, parentArtifactsByKey);
            var siblingKey = hierarchy.ParentKey
                ?? (parentKeys.Count == 0 ? null : parentKeys[0])
                ?? candidate.NormalizedId[..1];
            var order = siblingOrder.GetValueOrDefault(siblingKey);
            siblingOrder[siblingKey] = order + 1;
            var sourceReferences = candidate.Evidence
                .Select(static fragment => SourceReference(fragment))
                .ToArray();
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

            if (hierarchy.UsedZeroPaddedChildConvention)
            {
                confidenceBasis.Add("zero-padded-child-convention");
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
            extracted.Add(new ExtractedRule(candidate, artifact, hierarchy.ParentKey));
        }

        return extracted;
    }

    private static List<TirCrossReferenceArtifact> CreateCrossReferenceArtifacts(
        IReadOnlyList<SourceFragment> fragments,
        List<ExtractedRule> extractedRules,
        string registryId,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var rulesByKey = extractedRules
            .GroupBy(static rule => rule.Candidate.NormalizedId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static rule => rule.Artifact).ToArray(),
                StringComparer.Ordinal);
        var artifacts = new List<TirCrossReferenceArtifact>();
        foreach (var fragment in fragments)
        {
            foreach (Match match in ExplicitCrossReferenceRegex().Matches(fragment.Content))
            {
                var referenceText = match.Groups["reference"].Value;
                var normalizedCandidate = referenceText.ToUpperInvariant();
                if (IsBoundaryIdentifierOccurrence(fragment, match, normalizedCandidate))
                {
                    continue;
                }

                rulesByKey.TryGetValue(normalizedCandidate, out var targets);
                var resolutionStatus = targets switch
                {
                    { Length: 1 } => TirReferenceResolutionStatus.Resolved,
                    { Length: > 1 } => TirReferenceResolutionStatus.Ambiguous,
                    _ => TirReferenceResolutionStatus.Missing,
                };
                var resolvedTargetId = targets is { Length: 1 }
                    ? targets[0].Envelope.ArtifactId
                    : null;
                var span = CreateUtf8ByteSpan(fragment.Content, match.Index, match.Index + match.Length);
                var artifactId = TirArtifactIdentity.Create(
                    new TirArtifactIdentityInput(
                        TirArtifactKind.CrossReference,
                        registryId,
                        referenceText,
                        normalizedCandidate,
                        [fragment.FragmentId],
                        $"utf8-byte-offset:{span.Start.ToString(CultureInfo.InvariantCulture)}"));
                var dependencies = resolvedTargetId is null
                    ? Array.Empty<TirDependency>()
                    : new[] { new TirDependency(TirDependencyKind.Artifact, resolvedTargetId) };
                var envelope = Envelope(
                    artifactId,
                    TirArtifactKind.CrossReference,
                    options,
                    referenceText,
                    normalizedCandidate,
                    [SourceReference(fragment, span)],
                    dependencies,
                    1m,
                    ["explicit-chapter-qualified-reference"],
                    createdBy);
                artifacts.Add(new TirCrossReferenceArtifact(
                    envelope,
                    new TirCrossReferencePayload(
                        referenceText,
                        fragment.FragmentId,
                        normalizedCandidate,
                        resolutionStatus,
                        resolvedTargetId)));
            }
        }

        return artifacts;
    }

    private static List<TirExampleArtifact> CreateExampleArtifacts(
        IReadOnlyList<SourceFragment> fragments,
        List<ExtractedRule> extractedRules,
        string registryId,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var ruleIdsByFragment = extractedRules
            .SelectMany(rule => rule.Artifact.Envelope.SourceFragments.Select(source => new
            {
                source.FragmentId,
                rule.Artifact.Envelope.ArtifactId,
            }))
            .GroupBy(static item => item.FragmentId, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static item => item.ArtifactId).Distinct().ToArray(),
                StringComparer.Ordinal);
        var artifacts = new List<TirExampleArtifact>();
        foreach (var fragment in fragments)
        {
            foreach (Match match in ExplicitExampleRegex().Matches(fragment.Content))
            {
                var illustrates = ruleIdsByFragment.GetValueOrDefault(fragment.FragmentId) ?? [];
                var span = CreateUtf8ByteSpan(fragment.Content, match.Index, match.Index + match.Length);
                var artifactId = TirArtifactIdentity.Create(
                    new TirArtifactIdentityInput(
                        TirArtifactKind.Example,
                        registryId,
                        null,
                        null,
                        [fragment.FragmentId],
                        $"utf8-byte-offset:{span.Start.ToString(CultureInfo.InvariantCulture)}"));
                var dependencies = illustrates
                    .Select(static ruleId => new TirDependency(TirDependencyKind.Artifact, ruleId))
                    .ToArray();
                var envelope = Envelope(
                    artifactId,
                    TirArtifactKind.Example,
                    options,
                    null,
                    null,
                    [SourceReference(fragment, span)],
                    dependencies,
                    1m,
                    ["explicit-example-marker"],
                    createdBy);
                artifacts.Add(new TirExampleArtifact(envelope, new TirExamplePayload(illustrates)));
            }
        }

        return artifacts;
    }

    private static List<TirTableArtifact> CreateTableArtifacts(
        IReadOnlyList<SourceFragment> fragments,
        string registryId,
        TirExtractionOptions options,
        TirCreatedBy createdBy)
    {
        var artifacts = new List<TirTableArtifact>();
        foreach (var fragment in fragments.Where(static fragment =>
            fragment.Kind == SourceFragmentKind.StructuredText
            && ExplicitTableLabelRegex().IsMatch(fragment.Content)))
        {
            var artifactId = TirArtifactIdentity.Create(
                new TirArtifactIdentityInput(
                    TirArtifactKind.Table,
                    registryId,
                    fragment.Locator.PublishedElementId,
                    fragment.Locator.NormalizedElementId,
                    [fragment.FragmentId],
                    string.Empty));
            var envelope = Envelope(
                artifactId,
                TirArtifactKind.Table,
                options,
                fragment.Locator.PublishedElementId,
                fragment.Locator.NormalizedElementId,
                [SourceReference(fragment)],
                [],
                1m,
                ["explicit-table-or-chart-label"],
                createdBy);
            artifacts.Add(new TirTableArtifact(envelope, new TirTablePayload([], false)));
        }

        return artifacts;
    }

    private static List<TirDiagnostic> CreateDiagnostics(
        List<ExtractedSection> extractedSections,
        List<ExtractedRule> extractedRules,
        List<TirCrossReferenceArtifact> crossReferences)
    {
        var diagnostics = new List<TirDiagnostic>();
        foreach (var group in extractedRules.GroupBy(
            static item => item.Candidate.NormalizedId,
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

        foreach (var group in extractedSections.GroupBy(
            static item => item.NormalizedId,
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
                    "TIR-DUPLICATE-SECTION-ID",
                    TirDiagnosticSeverity.Error,
                    duplicate.Artifact.Envelope.ArtifactId,
                    $"Normalized section identifier '{duplicate.NormalizedId}' occurs more than once."));
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
                    $"Parent structural artifact '{rule.ParentKey}' was not found for '{rule.Candidate.NormalizedId}'."));
            }
            else if (rule.Artifact.Payload.HierarchyStatus == TirHierarchyStatus.Ambiguous)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-AMBIGUOUS-PARENT",
                    TirDiagnosticSeverity.Error,
                    rule.Artifact.Envelope.ArtifactId,
                    $"Parent structural artifact '{rule.ParentKey}' is ambiguous for '{rule.Candidate.NormalizedId}'."));
            }
        }

        foreach (var crossReference in crossReferences)
        {
            if (crossReference.Payload.ResolutionStatus == TirReferenceResolutionStatus.Missing)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-MISSING-REFERENCE-TARGET",
                    TirDiagnosticSeverity.Warning,
                    crossReference.Envelope.ArtifactId,
                    $"Reference target '{crossReference.Payload.NormalizedTargetCandidate}' was not found."));
            }
            else if (crossReference.Payload.ResolutionStatus == TirReferenceResolutionStatus.Ambiguous)
            {
                diagnostics.Add(new TirDiagnostic(
                    "TIR-AMBIGUOUS-REFERENCE-TARGET",
                    TirDiagnosticSeverity.Error,
                    crossReference.Envelope.ArtifactId,
                    $"Reference target '{crossReference.Payload.NormalizedTargetCandidate}' is ambiguous."));
            }
        }

        return diagnostics;
    }

    private static bool IsBoundaryIdentifierOccurrence(
        SourceFragment fragment,
        Match match,
        string normalizedCandidate)
    {
        return match.Index < 12
            && fragment.Locator.NormalizedElementId is not null
            && string.Equals(
                fragment.Locator.NormalizedElementId,
                normalizedCandidate,
                StringComparison.Ordinal);
    }

    private static HierarchyResolution ResolveHierarchy(
        IReadOnlyList<string> parentKeys,
        Dictionary<string, List<StructuralParent>> parentArtifactsByKey)
    {
        if (parentKeys.Count == 0)
        {
            return new HierarchyResolution(null, null, TirHierarchyStatus.Root, [], false);
        }

        var parentKey = parentKeys.FirstOrDefault(parentArtifactsByKey.ContainsKey);
        if (parentKey is null)
        {
            return new HierarchyResolution(
                null,
                parentKeys[0],
                TirHierarchyStatus.Missing,
                [TirHierarchyBasis.PublishedIdentifier],
                false);
        }

        var parents = parentArtifactsByKey[parentKey];
        var usedZeroPaddedChildConvention = parentKeys.Count > 1
            && string.Equals(parentKey, parentKeys[1], StringComparison.Ordinal);

        if (parents.Count > 1)
        {
            return new HierarchyResolution(
                null,
                parentKey,
                TirHierarchyStatus.Ambiguous,
                [TirHierarchyBasis.PublishedIdentifier, TirHierarchyBasis.ChapterContext],
                usedZeroPaddedChildConvention);
        }

        return new HierarchyResolution(
            parents[0].ArtifactId,
            parentKey,
            TirHierarchyStatus.Supported,
            parents[0].Basis.ToArray(),
            usedZeroPaddedChildConvention);
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

    private static TirSourceFragmentReference SourceReference(
        SourceFragment fragment,
        int? startCharacterOffset = null,
        int? endCharacterOffsetExclusive = null)
    {
        if ((startCharacterOffset is null) != (endCharacterOffsetExclusive is null))
        {
            throw new ArgumentException("Sub-fragment character offsets must both be null or both be present.");
        }

        var span = startCharacterOffset is null
            ? (Utf8Span?)null
            : CreateUtf8ByteSpan(
                fragment.Content,
                startCharacterOffset.Value,
                endCharacterOffsetExclusive!.Value);
        return SourceReference(fragment, span);
    }

    private static TirSourceFragmentReference SourceReference(
        SourceFragment fragment,
        Utf8Span? span)
    {
        return new TirSourceFragmentReference(
            fragment.FragmentId,
            fragment.SourceId,
            fragment.SourceSha256,
            fragment.ContentSha256,
            fragment.Locator.StartLine,
            fragment.Locator.EndLine,
            span?.Start,
            span?.EndExclusive);
    }

    private static Utf8Span CreateUtf8ByteSpan(
        string content,
        int startCharacterOffset,
        int endCharacterOffsetExclusive)
    {
        if (startCharacterOffset < 0
            || endCharacterOffsetExclusive <= startCharacterOffset
            || endCharacterOffsetExclusive > content.Length
            || IsInsideSurrogatePair(content, startCharacterOffset)
            || IsInsideSurrogatePair(content, endCharacterOffsetExclusive))
        {
            throw new ArgumentOutOfRangeException(
                nameof(startCharacterOffset),
                "Sub-fragment character offsets must describe a non-empty Unicode boundary within the fragment.");
        }

        var start = Encoding.UTF8.GetByteCount(content.AsSpan(0, startCharacterOffset));
        var length = Encoding.UTF8.GetByteCount(
            content.AsSpan(startCharacterOffset, endCharacterOffsetExclusive - startCharacterOffset));
        return new Utf8Span(start, start + length);
    }

    private static bool IsInsideSurrogatePair(string content, int offset)
    {
        return offset > 0
            && offset < content.Length
            && char.IsHighSurrogate(content[offset - 1])
            && char.IsLowSurrogate(content[offset]);
    }

    private static IReadOnlyList<string> ParentHierarchyKeys(string normalizedId)
    {
        var match = ChapterLocalRuleRegex().Match(normalizedId);
        if (!match.Success)
        {
            return [];
        }

        var chapterAndMajor = $"{match.Groups["chapter"].Value}{match.Groups["major"].Value}";
        var fraction = match.Groups["fraction"].Value;
        var directParent = fraction.Length == 1
            ? chapterAndMajor
            : $"{chapterAndMajor}.{fraction[..^1]}";
        if (fraction.Length < 2 || fraction[^2] != '0' || fraction[^1] == '0')
        {
            return [directParent];
        }

        var zeroPaddedParentFraction = fraction[..^2];
        var zeroPaddedParent = zeroPaddedParentFraction.Length == 0
            ? chapterAndMajor
            : $"{chapterAndMajor}.{zeroPaddedParentFraction}";
        return string.Equals(directParent, zeroPaddedParent, StringComparison.Ordinal)
            ? [directParent]
            : [directParent, zeroPaddedParent];
    }

    private static bool TryGetSectionBoundary(SourceFragment fragment, out SectionBoundary boundary)
    {
        var content = fragment.Content.TrimEnd('\r', '\n');
        Match match;
        switch (fragment.Kind)
        {
            case SourceFragmentKind.Heading:
                match = MajorSectionHeadingRegex().Match(content);
                if (match.Success)
                {
                    boundary = new SectionBoundary(
                        match.Groups["identifier"].Value,
                        match.Groups["title"].Value.Trim(),
                        TirSectionBoundaryKind.MarkdownHeading,
                        2,
                        null,
                        null,
                        [
                            TirHierarchyBasis.PublishedIdentifier,
                            TirHierarchyBasis.ChapterContext,
                            TirHierarchyBasis.HeadingPath,
                        ],
                        ["chapter-context", "exact-numbered-heading"]);
                    return true;
                }

                break;
            case SourceFragmentKind.Paragraph:
            case SourceFragmentKind.RuleContinuation:
                match = BoldMajorSectionDeclarationRegex().Match(content);
                if (match.Success)
                {
                    boundary = new SectionBoundary(
                        match.Groups["identifier"].Value,
                        match.Groups["title"].Value.Trim(),
                        TirSectionBoundaryKind.BoldDeclaration,
                        null,
                        null,
                        null,
                        [
                            TirHierarchyBasis.PublishedIdentifier,
                            TirHierarchyBasis.ChapterContext,
                            TirHierarchyBasis.SourceOrder,
                        ],
                        ["chapter-context", "exact-bold-major-section-declaration"]);
                    return true;
                }

                break;
            case SourceFragmentKind.StructuredText:
                match = StructuredTextMajorSectionDeclarationRegex().Match(content);
                if (match.Success)
                {
                    boundary = new SectionBoundary(
                        match.Groups["identifier"].Value,
                        match.Groups["title"].Value.Trim(),
                        TirSectionBoundaryKind.StructuredTextDeclaration,
                        null,
                        match.Index,
                        match.Index + match.Length,
                        [
                            TirHierarchyBasis.PublishedIdentifier,
                            TirHierarchyBasis.ChapterContext,
                            TirHierarchyBasis.SourceOrder,
                        ],
                        ["chapter-context", "exact-structured-text-major-section-declaration"]);
                    return true;
                }

                break;
        }

        boundary = null!;
        return false;
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
        List<SourceFragment> Evidence);

    private sealed record ExtractedSection(string NormalizedId, TirSectionArtifact Artifact);

    private sealed record SectionBoundary(
        string PublishedId,
        string Title,
        TirSectionBoundaryKind Kind,
        int? SourceHeadingLevel,
        int? StartCharacterOffset,
        int? EndCharacterOffsetExclusive,
        TirHierarchyBasis[] HierarchyBasis,
        string[] ConfidenceBasis);

    private readonly record struct Utf8Span(int Start, int EndExclusive);

    private sealed record StructuralParent(
        string ArtifactId,
        IReadOnlyList<TirHierarchyBasis> Basis);

    private sealed record ExtractedRule(
        RuleCandidate Candidate,
        TirRuleArtifact Artifact,
        string? ParentKey);

    private sealed record HierarchyResolution(
        string? ParentArtifactId,
        string? ParentKey,
        TirHierarchyStatus Status,
        TirHierarchyBasis[] Basis,
        bool UsedZeroPaddedChildConvention);

    [GeneratedRegex(@"^##\s+\*?(?<identifier>\d+)\.\s+(?<title>.+?)\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex MajorSectionHeadingRegex();

    [GeneratedRegex(@"^\*\*(?<identifier>\d+)\.\s+(?<title>.+?)\*{2,}(?:\s*<sup>.*?</sup>\*{0,2})?\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex BoldMajorSectionDeclarationRegex();

    [GeneratedRegex(@"^\s*(?<identifier>\d+)\.\s+(?<title>[A-Z][A-Z &/-]+?)(?:\^[0-9^]+)?\s*$", RegexOptions.Multiline | RegexOptions.CultureInvariant)]
    private static partial Regex StructuredTextMajorSectionDeclarationRegex();

    [GeneratedRegex(@"^(?<chapter>[A-Z])(?<major>\d+)\.(?<fraction>\d+)$", RegexOptions.CultureInvariant)]
    private static partial Regex ChapterLocalRuleRegex();

    [GeneratedRegex(@"(?<![A-Za-z0-9])(?<reference>[A-E](?:\.\d+(?:\.\d+)*|\d+\.\d+(?:\.\d+)*))(?![A-Za-z0-9])", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitCrossReferenceRegex();

    [GeneratedRegex(@"(?<![A-Za-z])EX:", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitExampleRegex();

    [GeneratedRegex(@"\b(?:TABLE|CHART)\b", RegexOptions.CultureInvariant)]
    private static partial Regex ExplicitTableLabelRegex();
}
