using System.Globalization;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public static partial class TirStructuralValidator
{
    public const string PolicyId = "asl-tir-1.3-structural";
    public const string PolicyVersion = "1.0.0";
    public const string ToolName = "LimboDancer.Domains.Asl.Authoring.TirStructuralValidator";
    public const string ToolVersion = "1.0.0";

    private const string PolicyMaterial = """
        asl-tir-structural-validation/v1
        schema=urn:limbodancer:asl:tir:schema:1.3.0
        gates=inputSchema,identity,structural,sourceProvenance,architectureAuthority
        normalizedIdentityScope=artifactKind
        unresolvedReference=warning
        ambiguousReference=error
        missingParent=warning
        ambiguousParent=error
        candidateHierarchy=warning
        semanticGate=notApplicable
        reviewGate=notApplicable
        """;

    public static TirValidationPolicy Policy { get; } =
        new(
            PolicyId,
            PolicyVersion,
            Hashing.Sha256Text(PolicyMaterial));

    public static TirValidationReportRecord CreateReport(
        TirDocument document,
        string artifactId,
        DateTimeOffset createdAt,
        string createdAtSource,
        string actorIdentity)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdAtSource);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorIdentity);

        // Canonical serialization is the input boundary. An unsupported or internally
        // malformed document cannot produce an exact review subject or a report.
        var documentSha256 = TirCanonicalJson.ComputePayloadSha256(document);
        var artifact = document.Artifacts.SingleOrDefault(candidate => string.Equals(
            candidate.Envelope.ArtifactId,
            artifactId,
            StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"TIR artifact was not found: {artifactId}.");
        var subject = new TirReviewSubjectReference(
            documentSha256,
            artifactId,
            TirCanonicalJson.ComputeArtifactSha256(document, artifactId),
            document.SchemaId,
            document.PackageCandidate);
        var findings = new List<TirValidationFinding>();

        ValidateIdentity(document, artifact, subject, findings);
        ValidateStructure(document, artifact, subject, findings);
        ValidateProvenance(document, artifact, subject, findings);
        ValidateArchitectureAuthority(artifact, subject, findings);

        var gateResults = new[]
        {
            GateResult(TirValidationGate.InputSchema, findings),
            GateResult(TirValidationGate.Identity, findings),
            GateResult(TirValidationGate.Structural, findings),
            GateResult(TirValidationGate.SourceProvenance, findings),
            new TirValidationGateResult(
                TirValidationGate.Semantic,
                TirValidationGateStatus.NotApplicable,
                []),
            new TirValidationGateResult(
                TirValidationGate.Review,
                TirValidationGateStatus.NotApplicable,
                []),
            GateResult(TirValidationGate.ArchitectureAuthority, findings),
        };

        return new TirValidationReportRecord(
            subject,
            createdAt,
            createdAtSource,
            new TirReviewActor(actorIdentity, TirReviewActorRole.Validator),
            new TirReviewTool(ToolName, ToolVersion, Policy.ConfigurationSha256),
            [],
            Policy,
            gateResults,
            findings);
    }

    private static void ValidateIdentity(
        TirDocument document,
        TirArtifact artifact,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var envelope = artifact.Envelope;
        if (!CanReproduceArtifactIdentity(document, artifact))
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Identity,
                "TIR-VAL-ARTIFACT-ID-MISMATCH",
                TirDiagnosticSeverity.Error,
                "The artifact identity does not reproduce from its structural identity inputs.",
                [envelope.ArtifactId]);
        }

        if (envelope.NormalizedPublishedId is { } normalizedId)
        {
            var matches = document.Artifacts.Count(candidate =>
                candidate.Envelope.ArtifactKind == envelope.ArtifactKind
                && string.Equals(
                    candidate.Envelope.NormalizedPublishedId,
                    normalizedId,
                    StringComparison.Ordinal));
            if (matches > 1)
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Identity,
                    "TIR-VAL-DUPLICATE-NORMALIZED-ID",
                    TirDiagnosticSeverity.Error,
                    "The normalized published identity is not unique within its artifact-kind scope.",
                    [TirValues.ArtifactKind(envelope.ArtifactKind), normalizedId]);
            }
        }
    }

    private static bool CanReproduceArtifactIdentity(TirDocument document, TirArtifact artifact)
    {
        var envelope = artifact.Envelope;
        var sourceRegistryId = artifact is TirSourceFragmentArtifact
            ? envelope.SourceFragments[0].SourceId
            : document.SourceRegistry.RegistryId;
        var fragmentIds = envelope.SourceFragments
            .Select(static fragment => fragment.FragmentId)
            .ToArray();
        var disambiguators = ArtifactDisambiguators(artifact).Distinct(StringComparer.Ordinal);

        return disambiguators.Any(disambiguator => string.Equals(
            TirArtifactIdentity.Create(
                new TirArtifactIdentityInput(
                    envelope.ArtifactKind,
                    sourceRegistryId,
                    envelope.PublishedId,
                    envelope.NormalizedPublishedId,
                    fragmentIds,
                    disambiguator)),
            envelope.ArtifactId,
            StringComparison.Ordinal));
    }

    private static IEnumerable<string> ArtifactDisambiguators(TirArtifact artifact)
    {
        var envelope = artifact.Envelope;
        if (artifact is TirCrossReferenceArtifact or TirExampleArtifact)
        {
            if (envelope.SourceFragments[0].StartUtf8ByteOffset is { } offset)
            {
                yield return $"utf8-byte-offset:{offset.ToString(CultureInfo.InvariantCulture)}";
            }

            yield break;
        }

        if (artifact is TirRuleArtifact
            && envelope.SourceFragments[0].StartUtf8ByteOffset is { } ruleOffset)
        {
            yield return $"utf8-byte-offset:{ruleOffset.ToString(CultureInfo.InvariantCulture)}";
            yield break;
        }

        yield return string.Empty;
        if (artifact is TirRuleArtifact)
        {
            yield return envelope.SourceFragments[0].FragmentId;
        }
    }

    private static void ValidateStructure(
        TirDocument document,
        TirArtifact artifact,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var artifactsById = document.Artifacts.ToDictionary(
            static candidate => candidate.Envelope.ArtifactId,
            StringComparer.Ordinal);
        ValidateHierarchy(document, artifact, artifactsById, subject, findings);
        ValidateDependencies(artifact, artifactsById, subject, findings);

        switch (artifact)
        {
            case TirCrossReferenceArtifact crossReference:
                ValidateCrossReference(crossReference, artifactsById, subject, findings);
                break;
            case TirExampleArtifact example:
                ValidateExample(example, artifactsById, subject, findings);
                break;
            case TirTableArtifact table:
                ValidateTable(document, table, subject, findings);
                break;
        }
    }

    private static void ValidateHierarchy(
        TirDocument document,
        TirArtifact artifact,
        Dictionary<string, TirArtifact> artifactsById,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        if (!TryGetHierarchy(
            artifact,
            out var parentId,
            out var status,
            out var basis,
            out var siblingOrder))
        {
            return;
        }

        if (basis.Count == 0)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-HIERARCHY-BASIS-MISSING",
                TirDiagnosticSeverity.Error,
                "A structural hierarchy claim requires at least one declared basis.",
                [artifact.Envelope.ArtifactId]);
        }

        if (siblingOrder is < 0)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-SIBLING-ORDER-INVALID",
                TirDiagnosticSeverity.Error,
                "A hierarchy sibling order cannot be negative.",
                [siblingOrder.Value.ToString(CultureInfo.InvariantCulture)]);
        }

        if (status == TirHierarchyStatus.Root && parentId is not null)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-ROOT-HAS-PARENT",
                TirDiagnosticSeverity.Error,
                "A root hierarchy artifact cannot identify a direct parent.",
                [parentId]);
        }

        if (status is TirHierarchyStatus.Supported or TirHierarchyStatus.Candidate)
        {
            if (parentId is null || !artifactsById.TryGetValue(parentId, out var parent))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-PARENT-NOT-FOUND",
                    TirDiagnosticSeverity.Error,
                    "The declared direct parent does not identify an artifact in the TIR document.",
                    [parentId ?? "parent:null"]);
            }
            else if (parent is not TirSectionArtifact and not TirRuleArtifact)
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-PARENT-KIND-INVALID",
                    TirDiagnosticSeverity.Error,
                    "A structural parent must be a section or rule artifact.",
                    [parentId, TirValues.ArtifactKind(parent.Envelope.ArtifactKind)]);
            }
        }

        if (status == TirHierarchyStatus.Candidate)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-HIERARCHY-CANDIDATE",
                TirDiagnosticSeverity.Warning,
                "The hierarchy relationship remains a candidate rather than a supported relationship.",
                [parentId ?? "parent:null"]);
        }
        else if (status == TirHierarchyStatus.Missing)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-HIERARCHY-MISSING",
                TirDiagnosticSeverity.Warning,
                "The artifact reports a missing hierarchy parent.",
                [artifact.Envelope.ArtifactId]);
        }
        else if (status == TirHierarchyStatus.Ambiguous)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-HIERARCHY-AMBIGUOUS",
                TirDiagnosticSeverity.Error,
                "The artifact reports an ambiguous hierarchy parent.",
                [artifact.Envelope.ArtifactId]);
        }

        if (HasHierarchyCycle(document, artifact.Envelope.ArtifactId))
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-HIERARCHY-CYCLE",
                TirDiagnosticSeverity.Error,
                "The artifact participates in or depends on a hierarchy cycle.",
                [artifact.Envelope.ArtifactId]);
        }
    }

    private static bool HasHierarchyCycle(TirDocument document, string artifactId)
    {
        var parents = document.Artifacts
            .Select(static artifact => TryGetHierarchy(
                artifact,
                out var parentId,
                out _,
                out _,
                out _)
                ? new KeyValuePair<string, string?>(artifact.Envelope.ArtifactId, parentId)
                : new KeyValuePair<string, string?>(artifact.Envelope.ArtifactId, null))
            .ToDictionary(static item => item.Key, static item => item.Value, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = artifactId;
        while (parents.TryGetValue(current, out var parentId) && parentId is not null)
        {
            if (!seen.Add(current))
            {
                return true;
            }

            current = parentId;
        }

        return false;
    }

    private static bool TryGetHierarchy(
        TirArtifact artifact,
        out string? parentId,
        out TirHierarchyStatus status,
        out IReadOnlyList<TirHierarchyBasis> basis,
        out int? siblingOrder)
    {
        switch (artifact)
        {
            case TirSectionArtifact section:
                parentId = section.Payload.DirectParentArtifactId;
                status = section.Payload.HierarchyStatus;
                basis = section.Payload.HierarchyBasis;
                siblingOrder = section.Payload.SiblingOrder;
                return true;
            case TirRuleArtifact rule:
                parentId = rule.Payload.DirectParentArtifactId;
                status = rule.Payload.HierarchyStatus;
                basis = rule.Payload.HierarchyBasis;
                siblingOrder = rule.Payload.SiblingOrder;
                return true;
            default:
                parentId = null;
                status = default;
                basis = [];
                siblingOrder = null;
                return false;
        }
    }

    private static void ValidateDependencies(
        TirArtifact artifact,
        Dictionary<string, TirArtifact> artifactsById,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        foreach (var dependency in artifact.Envelope.Dependencies)
        {
            if (dependency.Kind == TirDependencyKind.Artifact
                && !artifactsById.ContainsKey(dependency.Target))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-ARTIFACT-DEPENDENCY-MISSING",
                    TirDiagnosticSeverity.Error,
                    "An artifact dependency target is absent from the TIR document.",
                    [dependency.Target]);
            }
        }
    }

    private static void ValidateCrossReference(
        TirCrossReferenceArtifact crossReference,
        Dictionary<string, TirArtifact> artifactsById,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var payload = crossReference.Payload;
        if (!crossReference.Envelope.SourceFragments.Any(fragment => string.Equals(
            fragment.FragmentId,
            payload.ContainingFragmentId,
            StringComparison.Ordinal)))
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                "TIR-VAL-REFERENCE-FRAGMENT-MISMATCH",
                TirDiagnosticSeverity.Error,
                "The containing fragment is not part of the cross-reference evidence.",
                [payload.ContainingFragmentId]);
        }

        if (payload.ResolutionStatus == TirReferenceResolutionStatus.Resolved)
        {
            if (payload.ResolvedTargetArtifactId is null
                || !artifactsById.ContainsKey(payload.ResolvedTargetArtifactId))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-RESOLVED-REFERENCE-TARGET-MISSING",
                    TirDiagnosticSeverity.Error,
                    "A resolved cross-reference must identify an existing target artifact.",
                    [payload.ResolvedTargetArtifactId ?? "target:null"]);
            }
            else if (!crossReference.Envelope.Dependencies.Any(dependency =>
                dependency.Kind == TirDependencyKind.Artifact
                && string.Equals(
                    dependency.Target,
                    payload.ResolvedTargetArtifactId,
                    StringComparison.Ordinal)))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-REFERENCE-DEPENDENCY-MISSING",
                    TirDiagnosticSeverity.Error,
                    "A resolved cross-reference must retain its target as an artifact dependency.",
                    [payload.ResolvedTargetArtifactId]);
            }
        }
        else
        {
            if (payload.ResolvedTargetArtifactId is not null)
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-UNRESOLVED-REFERENCE-HAS-TARGET",
                    TirDiagnosticSeverity.Error,
                    "A non-resolved cross-reference cannot claim a resolved target artifact.",
                    [payload.ResolvedTargetArtifactId]);
            }

            var severity = payload.ResolutionStatus == TirReferenceResolutionStatus.Ambiguous
                ? TirDiagnosticSeverity.Error
                : TirDiagnosticSeverity.Warning;
            AddFinding(
                findings,
                subject,
                TirValidationGate.Structural,
                payload.ResolutionStatus == TirReferenceResolutionStatus.Ambiguous
                    ? "TIR-VAL-REFERENCE-AMBIGUOUS"
                    : "TIR-VAL-REFERENCE-UNRESOLVED",
                severity,
                "The cross-reference does not resolve uniquely to an artifact.",
                [payload.NormalizedTargetCandidate ?? payload.ReferenceText]);
        }
    }

    private static void ValidateExample(
        TirExampleArtifact example,
        Dictionary<string, TirArtifact> artifactsById,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        foreach (var illustratedId in example.Payload.IllustratesArtifactIds)
        {
            if (!artifactsById.TryGetValue(illustratedId, out var illustrated)
                || illustrated is not TirRuleArtifact)
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-EXAMPLE-TARGET-INVALID",
                    TirDiagnosticSeverity.Error,
                    "An example may illustrate only an existing structural rule artifact.",
                    [illustratedId]);
            }
        }
    }

    private static void ValidateTable(
        TirDocument document,
        TirTableArtifact table,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var fragmentIds = document.Artifacts
            .OfType<TirSourceFragmentArtifact>()
            .SelectMany(static artifact => artifact.Envelope.SourceFragments)
            .Select(static fragment => fragment.FragmentId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var noteFragmentId in table.Payload.NoteFragmentIds)
        {
            if (!fragmentIds.Contains(noteFragmentId))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.Structural,
                    "TIR-VAL-TABLE-NOTE-FRAGMENT-MISSING",
                    TirDiagnosticSeverity.Error,
                    "A table note refers to a source fragment absent from the TIR document.",
                    [noteFragmentId]);
            }
        }
    }

    private static void ValidateProvenance(
        TirDocument document,
        TirArtifact artifact,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var envelope = artifact.Envelope;
        if (!string.Equals(envelope.CreatedBy.Name, document.Extractor.Name, StringComparison.Ordinal)
            || !string.Equals(envelope.CreatedBy.Version, document.Extractor.Version, StringComparison.Ordinal)
            || !string.Equals(
                envelope.CreatedBy.ConfigurationSha256,
                document.Extractor.ConfigurationSha256,
                StringComparison.Ordinal)
            || !string.Equals(
                envelope.CreatedBy.SourceRevision,
                document.SourceRegistry.SourceCommit,
                StringComparison.Ordinal))
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.SourceProvenance,
                "TIR-VAL-CREATOR-PROVENANCE-MISMATCH",
                TirDiagnosticSeverity.Error,
                "Artifact creation provenance does not match the containing document inputs.",
                [envelope.ArtifactId]);
        }

        var registeredFragments = document.Artifacts
            .OfType<TirSourceFragmentArtifact>()
            .SelectMany(static candidate => candidate.Envelope.SourceFragments)
            .GroupBy(static fragment => fragment.FragmentId, StringComparer.Ordinal)
            .ToDictionary(static group => group.Key, static group => group.ToArray(), StringComparer.Ordinal);
        foreach (var fragment in envelope.SourceFragments)
        {
            if (!FragmentIdRegex().IsMatch(fragment.FragmentId)
                || !registeredFragments.TryGetValue(fragment.FragmentId, out var candidates))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.SourceProvenance,
                    "TIR-VAL-SOURCE-FRAGMENT-MISSING",
                    TirDiagnosticSeverity.Error,
                    "Artifact evidence does not identify a registered source-fragment artifact.",
                    [fragment.FragmentId]);
                continue;
            }

            if (!candidates.Any(candidate => SourceReferenceContains(candidate, fragment)))
            {
                AddFinding(
                    findings,
                    subject,
                    TirValidationGate.SourceProvenance,
                    "TIR-VAL-SOURCE-FRAGMENT-MISMATCH",
                    TirDiagnosticSeverity.Error,
                    "Artifact evidence does not reproduce the registered fragment identity, hashes, or bounds.",
                    [fragment.FragmentId]);
            }
        }
    }

    private static bool SourceReferenceContains(
        TirSourceFragmentReference registered,
        TirSourceFragmentReference evidence)
    {
        return string.Equals(registered.SourceId, evidence.SourceId, StringComparison.Ordinal)
            && string.Equals(registered.SourceSha256, evidence.SourceSha256, StringComparison.Ordinal)
            && string.Equals(registered.ContentSha256, evidence.ContentSha256, StringComparison.Ordinal)
            && evidence.StartLine >= registered.StartLine
            && evidence.EndLine <= registered.EndLine;
    }

    private static void ValidateArchitectureAuthority(
        TirArtifact artifact,
        TirReviewSubjectReference subject,
        List<TirValidationFinding> findings)
    {
        var envelope = artifact.Envelope;
        if (envelope.Origin != TirArtifactOrigin.Extracted
            || envelope.FormalizationStatus != TirFormalizationStatus.Unmodeled
            || envelope.ReviewStatus != TirReviewStatus.Captured
            || envelope.SemanticId is not null
            || envelope.ReviewRecordRefs.Count != 0)
        {
            AddFinding(
                findings,
                subject,
                TirValidationGate.ArchitectureAuthority,
                "TIR-VAL-AUTHORITY-ESCALATION",
                TirDiagnosticSeverity.Error,
                "Structural TIR cannot claim semantic, review, publication, conclusion, or action authority.",
                [envelope.ArtifactId]);
        }
    }

    private static TirValidationGateResult GateResult(
        TirValidationGate gate,
        IReadOnlyList<TirValidationFinding> findings)
    {
        var gateFindings = findings
            .Where(finding => finding.Gate == gate)
            .Select(static finding => finding.FindingId)
            .Order(StringComparer.Ordinal)
            .ToArray();
        return new TirValidationGateResult(
            gate,
            gateFindings.Length == 0
                ? TirValidationGateStatus.Passed
                : TirValidationGateStatus.Failed,
            gateFindings);
    }

    private static void AddFinding(
        List<TirValidationFinding> findings,
        TirReviewSubjectReference subject,
        TirValidationGate gate,
        string code,
        TirDiagnosticSeverity severity,
        string message,
        IReadOnlyList<string> evidenceRefs)
    {
        var canonicalEvidence = evidenceRefs
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        findings.Add(
            new TirValidationFinding(
                TirReviewIdentity.CreateFindingId(
                    subject,
                    Policy,
                    gate,
                    code,
                    subject.ArtifactId,
                    canonicalEvidence),
                code,
                severity,
                gate,
                subject.ArtifactId,
                message,
                canonicalEvidence));
    }

    [GeneratedRegex("^asl-fragment:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex FragmentIdRegex();
}
