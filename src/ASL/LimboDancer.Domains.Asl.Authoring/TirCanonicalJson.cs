using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public static partial class TirCanonicalJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:schema:1.3.0";
    public const string SchemaVersion = "1.3.0";
    public const string ProfileName = "asl-tir-canonical-json";
    public const string ProfileVersion = "1.0.0";

    public static string Serialize(TirDocument document)
    {
        Validate(document);
        var digest = ComputePayloadSha256(document);
        return Write(writer => WriteDocument(writer, document, digest)) + "\n";
    }

    public static string SerializePayload(TirDocument document)
    {
        Validate(document);
        return Write(writer => WriteDocument(writer, document, null));
    }

    public static string ComputePayloadSha256(TirDocument document)
    {
        return Hashing.Sha256Text(SerializePayload(document));
    }

    public static string SerializeArtifactPayload(TirDocument document, string artifactId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactId);
        Validate(document);
        var artifact = document.Artifacts.SingleOrDefault(item => string.Equals(
            item.Envelope.ArtifactId,
            artifactId,
            StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"TIR artifact was not found: {artifactId}.");
        return Write(writer => WriteArtifact(writer, artifact));
    }

    public static string ComputeArtifactSha256(TirDocument document, string artifactId)
    {
        return Hashing.Sha256Text(SerializeArtifactPayload(document, artifactId));
    }

    private static string Write(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(
            stream,
            new JsonWriterOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                Indented = false,
            }))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteDocument(Utf8JsonWriter writer, TirDocument document, string? digest)
    {
        writer.WriteStartObject();
        writer.WriteString("schemaId", document.SchemaId);
        writer.WriteString("schemaVersion", document.SchemaVersion);
        WritePackageCandidate(writer, "packageCandidate", document.PackageCandidate);
        writer.WritePropertyName("sourceRegistry");
        writer.WriteStartObject();
        writer.WriteString("registryId", document.SourceRegistry.RegistryId);
        writer.WriteString("sha256", document.SourceRegistry.Sha256);
        writer.WriteString("sourceCommit", document.SourceRegistry.SourceCommit);
        writer.WriteEndObject();
        writer.WritePropertyName("extractor");
        writer.WriteStartObject();
        writer.WriteString("name", document.Extractor.Name);
        writer.WriteString("version", document.Extractor.Version);
        writer.WriteString("configurationSha256", document.Extractor.ConfigurationSha256);
        writer.WriteEndObject();
        writer.WritePropertyName("canonicalization");
        writer.WriteStartObject();
        writer.WriteString("name", document.Canonicalization.Name);
        writer.WriteString("version", document.Canonicalization.Version);
        writer.WriteEndObject();
        writer.WriteString("createdAt", FormatTimestamp(document.CreatedAt));
        writer.WriteString("createdAtSource", document.CreatedAtSource);
        writer.WritePropertyName("artifacts");
        writer.WriteStartArray();
        foreach (var artifact in document.Artifacts.OrderBy(
            static artifact => artifact.Envelope.ArtifactId,
            StringComparer.Ordinal))
        {
            WriteArtifact(writer, artifact);
        }

        writer.WriteEndArray();
        writer.WritePropertyName("diagnostics");
        writer.WriteStartArray();
        foreach (var diagnostic in document.Diagnostics
            .OrderBy(static item => item.Code, StringComparer.Ordinal)
            .ThenBy(static item => item.ArtifactId, StringComparer.Ordinal)
            .ThenBy(static item => item.Message, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("code", diagnostic.Code);
            writer.WriteString("severity", DiagnosticSeverity(diagnostic.Severity));
            WriteNullableString(writer, "artifactId", diagnostic.ArtifactId);
            writer.WriteString("message", diagnostic.Message);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        if (digest is not null)
        {
            writer.WriteString("documentSha256", digest);
        }

        writer.WriteEndObject();
    }

    private static void WriteArtifact(Utf8JsonWriter writer, TirArtifact artifact)
    {
        var envelope = artifact.Envelope;
        writer.WriteStartObject();
        writer.WriteString("artifactId", envelope.ArtifactId);
        writer.WriteString("artifactKind", TirValues.ArtifactKind(envelope.ArtifactKind));
        WritePackageCandidate(writer, "packageCandidate", envelope.PackageCandidate);
        WriteNullableString(writer, "publishedId", envelope.PublishedId);
        WriteNullableString(writer, "normalizedPublishedId", envelope.NormalizedPublishedId);
        WriteNullableString(writer, "semanticId", envelope.SemanticId);
        writer.WritePropertyName("sourceFragments");
        writer.WriteStartArray();
        foreach (var sourceFragment in envelope.SourceFragments)
        {
            writer.WriteStartObject();
            writer.WriteString("fragmentId", sourceFragment.FragmentId);
            writer.WriteString("sourceId", sourceFragment.SourceId);
            writer.WriteString("sourceSha256", sourceFragment.SourceSha256);
            writer.WriteString("contentSha256", sourceFragment.ContentSha256);
            writer.WriteNumber("startLine", sourceFragment.StartLine);
            writer.WriteNumber("endLine", sourceFragment.EndLine);
            WriteNullableNumber(writer, "startUtf8ByteOffset", sourceFragment.StartUtf8ByteOffset);
            WriteNullableNumber(
                writer,
                "endUtf8ByteOffsetExclusive",
                sourceFragment.EndUtf8ByteOffsetExclusive);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("dependencies");
        writer.WriteStartArray();
        foreach (var dependency in envelope.Dependencies
            .OrderBy(static item => DependencyKind(item.Kind), StringComparer.Ordinal)
            .ThenBy(static item => item.Target, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", DependencyKind(dependency.Kind));
            writer.WriteString("target", dependency.Target);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("origin", ArtifactOrigin(envelope.Origin));
        writer.WriteString("formalizationStatus", FormalizationStatus(envelope.FormalizationStatus));
        writer.WriteString("reviewStatus", ReviewStatus(envelope.ReviewStatus));
        writer.WriteNumber("confidence", envelope.Confidence);
        WriteSortedStringArray(writer, "confidenceBasis", envelope.ConfidenceBasis);
        writer.WritePropertyName("createdBy");
        writer.WriteStartObject();
        writer.WriteString("name", envelope.CreatedBy.Name);
        writer.WriteString("version", envelope.CreatedBy.Version);
        writer.WriteString("configurationSha256", envelope.CreatedBy.ConfigurationSha256);
        writer.WriteString("sourceRevision", envelope.CreatedBy.SourceRevision);
        writer.WriteEndObject();
        writer.WriteString("createdAt", FormatTimestamp(envelope.CreatedAt));
        WriteSortedStringArray(writer, "reviewRecordRefs", envelope.ReviewRecordRefs);
        writer.WritePropertyName("payload");
        WritePayload(writer, artifact);
        writer.WriteEndObject();
    }

    private static void WritePayload(Utf8JsonWriter writer, TirArtifact artifact)
    {
        writer.WriteStartObject();
        switch (artifact)
        {
            case TirSourceFragmentArtifact sourceFragment:
                writer.WriteString("fragmentKind", FragmentKind(sourceFragment.Payload.FragmentKind));
                writer.WriteString("sourcePath", sourceFragment.Payload.SourcePath);
                WriteSourceLocator(writer, sourceFragment.Payload.Locator);
                writer.WriteBoolean("hasFootnoteMarkers", sourceFragment.Payload.HasFootnoteMarkers);
                writer.WriteString(
                    "verificationStatus",
                    Verification(sourceFragment.Payload.VerificationStatus));
                break;
            case TirSectionArtifact section:
                writer.WriteString("title", section.Payload.Title);
                writer.WriteString("boundaryKind", SectionBoundaryKind(section.Payload.BoundaryKind));
                if (section.Payload.SourceHeadingLevel is null)
                {
                    writer.WriteNull("sourceHeadingLevel");
                }
                else
                {
                    writer.WriteNumber("sourceHeadingLevel", section.Payload.SourceHeadingLevel.Value);
                }
                WriteHierarchyPayload(
                    writer,
                    section.Payload.DirectParentArtifactId,
                    section.Payload.HierarchyStatus,
                    section.Payload.HierarchyBasis,
                    section.Payload.SiblingOrder);
                break;
            case TirRuleArtifact rule:
                WriteHierarchyPayload(
                    writer,
                    rule.Payload.DirectParentArtifactId,
                    rule.Payload.HierarchyStatus,
                    rule.Payload.HierarchyBasis,
                    rule.Payload.SiblingOrder);
                break;
            case TirCrossReferenceArtifact crossReference:
                writer.WriteString("referenceText", crossReference.Payload.ReferenceText);
                writer.WriteString("containingFragmentId", crossReference.Payload.ContainingFragmentId);
                WriteNullableString(
                    writer,
                    "normalizedTargetCandidate",
                    crossReference.Payload.NormalizedTargetCandidate);
                writer.WriteString(
                    "resolutionStatus",
                    ReferenceResolutionStatus(crossReference.Payload.ResolutionStatus));
                WriteNullableString(
                    writer,
                    "resolvedTargetArtifactId",
                    crossReference.Payload.ResolvedTargetArtifactId);
                break;
            case TirExampleArtifact example:
                WriteSortedStringArray(writer, "illustratesArtifactIds", example.Payload.IllustratesArtifactIds);
                break;
            case TirTableArtifact table:
                writer.WritePropertyName("noteFragmentIds");
                writer.WriteStartArray();
                foreach (var noteFragmentId in table.Payload.NoteFragmentIds)
                {
                    writer.WriteStringValue(noteFragmentId);
                }

                writer.WriteEndArray();
                writer.WriteBoolean("structureVerified", table.Payload.StructureVerified);
                break;
            default:
                throw new InvalidOperationException($"Unsupported TIR artifact type: {artifact.GetType().Name}.");
        }

        writer.WriteEndObject();
    }

    private static void WriteHierarchyPayload(
        Utf8JsonWriter writer,
        string? directParentArtifactId,
        TirHierarchyStatus hierarchyStatus,
        IReadOnlyList<TirHierarchyBasis> hierarchyBasis,
        int? siblingOrder)
    {
        WriteNullableString(writer, "directParentArtifactId", directParentArtifactId);
        writer.WriteString("hierarchyStatus", HierarchyStatus(hierarchyStatus));
        writer.WritePropertyName("hierarchyBasis");
        writer.WriteStartArray();
        foreach (var basis in hierarchyBasis.OrderBy(
            static item => HierarchyBasis(item),
            StringComparer.Ordinal))
        {
            writer.WriteStringValue(HierarchyBasis(basis));
        }

        writer.WriteEndArray();
        if (siblingOrder is null)
        {
            writer.WriteNull("siblingOrder");
        }
        else
        {
            writer.WriteNumber("siblingOrder", siblingOrder.Value);
        }
    }

    private static void WriteSourceLocator(Utf8JsonWriter writer, SourceLocator locator)
    {
        writer.WritePropertyName("locator");
        writer.WriteStartObject();
        writer.WriteNumber("startLine", locator.StartLine);
        writer.WriteNumber("endLine", locator.EndLine);
        WriteNullableNumber(writer, "startPage", locator.StartPage);
        WriteNullableNumber(writer, "endPage", locator.EndPage);
        writer.WritePropertyName("headingPath");
        writer.WriteStartArray();
        foreach (var heading in locator.HeadingPath)
        {
            writer.WriteStringValue(heading);
        }

        writer.WriteEndArray();
        WriteNullableString(writer, "publishedElementId", locator.PublishedElementId);
        WriteNullableString(writer, "normalizedElementId", locator.NormalizedElementId);
        writer.WriteEndObject();
    }

    private static void WritePackageCandidate(
        Utf8JsonWriter writer,
        string propertyName,
        TirPackageCandidate candidate)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartObject();
        writer.WriteString("domainId", candidate.DomainId);
        writer.WriteString("packageId", candidate.PackageId);
        writer.WriteString("version", candidate.Version);
        writer.WriteEndObject();
    }

    private static void WriteSortedStringArray(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyList<string> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (var value in values.OrderBy(static value => value, StringComparer.Ordinal))
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteNullableString(Utf8JsonWriter writer, string propertyName, string? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteString(propertyName, value);
        }
    }

    private static void WriteNullableNumber(Utf8JsonWriter writer, string propertyName, int? value)
    {
        if (value is null)
        {
            writer.WriteNull(propertyName);
        }
        else
        {
            writer.WriteNumber(propertyName, value.Value);
        }
    }

    private static void Validate(TirDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!string.Equals(document.SchemaId, SchemaId, StringComparison.Ordinal)
            || !string.Equals(document.SchemaVersion, SchemaVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported TIR schema identity or version.");
        }

        if (!string.Equals(document.Canonicalization.Name, ProfileName, StringComparison.Ordinal)
            || !string.Equals(document.Canonicalization.Version, ProfileVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported TIR canonicalization profile.");
        }

        ValidateUtc(document.CreatedAt, nameof(document.CreatedAt));
        ValidateSha256(document.SourceRegistry.Sha256, nameof(document.SourceRegistry.Sha256));
        ValidateSha256(document.Extractor.ConfigurationSha256, nameof(document.Extractor.ConfigurationSha256));
        var artifactIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var artifact in document.Artifacts)
        {
            ValidateArtifact(document, artifact, artifactIds);
        }
    }

    private static void ValidateArtifact(
        TirDocument document,
        TirArtifact artifact,
        HashSet<string> artifactIds)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var envelope = artifact.Envelope;
        if (!ArtifactIdRegex().IsMatch(envelope.ArtifactId)
            || !artifactIds.Add(envelope.ArtifactId))
        {
            throw new InvalidOperationException($"Invalid or duplicate TIR artifact identity: {envelope.ArtifactId}.");
        }

        var expectedKind = artifact switch
        {
            TirSourceFragmentArtifact => TirArtifactKind.SourceFragment,
            TirSectionArtifact => TirArtifactKind.Section,
            TirRuleArtifact => TirArtifactKind.Rule,
            TirCrossReferenceArtifact => TirArtifactKind.CrossReference,
            TirExampleArtifact => TirArtifactKind.Example,
            TirTableArtifact => TirArtifactKind.Table,
            _ => throw new InvalidOperationException($"Unsupported TIR artifact type: {artifact.GetType().Name}."),
        };
        if (envelope.ArtifactKind != expectedKind)
        {
            throw new InvalidOperationException("The artifact envelope kind does not match its payload type.");
        }

        if (envelope.PackageCandidate != document.PackageCandidate)
        {
            throw new InvalidOperationException("Artifact and document package candidates must match.");
        }

        if (envelope.Origin != TirArtifactOrigin.Extracted
            || envelope.FormalizationStatus != TirFormalizationStatus.Unmodeled
            || envelope.ReviewStatus != TirReviewStatus.Captured
            || envelope.SemanticId is not null
            || envelope.ReviewRecordRefs.Count != 0)
        {
            throw new InvalidOperationException(
                "ASL-OT-02 artifacts must remain extracted, unmodeled, captured, and unaccepted.");
        }

        if (envelope.Confidence is < 0 or > 1 || envelope.ConfidenceBasis.Count == 0)
        {
            throw new InvalidOperationException("Structural confidence must be in [0,1] and have a basis.");
        }

        if (envelope.SourceFragments.Count == 0)
        {
            throw new InvalidOperationException("Every TIR artifact must retain source-fragment evidence.");
        }

        ValidateUtc(envelope.CreatedAt, nameof(envelope.CreatedAt));
        ValidateSha256(envelope.CreatedBy.ConfigurationSha256, nameof(envelope.CreatedBy.ConfigurationSha256));
        foreach (var sourceFragment in envelope.SourceFragments)
        {
            ValidateSha256(sourceFragment.SourceSha256, nameof(sourceFragment.SourceSha256));
            ValidateSha256(sourceFragment.ContentSha256, nameof(sourceFragment.ContentSha256));
            if (sourceFragment.StartLine < 1 || sourceFragment.EndLine < sourceFragment.StartLine)
            {
                throw new InvalidOperationException("Source-fragment line ranges must be positive and ordered.");
            }

            if ((sourceFragment.StartUtf8ByteOffset is null)
                != (sourceFragment.EndUtf8ByteOffsetExclusive is null))
            {
                throw new InvalidOperationException(
                    "Sub-fragment UTF-8 byte offsets must either both be null or both be present.");
            }

            if (sourceFragment.StartUtf8ByteOffset is not null
                && (sourceFragment.StartUtf8ByteOffset < 0
                    || sourceFragment.EndUtf8ByteOffsetExclusive <= sourceFragment.StartUtf8ByteOffset))
            {
                throw new InvalidOperationException(
                    "Sub-fragment UTF-8 byte offsets must be non-negative, non-empty, and ordered.");
            }
        }
    }

    private static void ValidateSha256(string value, string name)
    {
        if (!Sha256Regex().IsMatch(value))
        {
            throw new InvalidOperationException($"{name} must be a lowercase SHA-256 value.");
        }
    }

    private static void ValidateUtc(DateTimeOffset value, string name)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{name} must be supplied in UTC.");
        }
    }

    private static string FormatTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
            CultureInfo.InvariantCulture);
    }

    private static string ArtifactOrigin(TirArtifactOrigin value) => value switch
    {
        TirArtifactOrigin.Extracted => "extracted",
        TirArtifactOrigin.Curated => "curated",
        TirArtifactOrigin.Derived => "derived",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string FormalizationStatus(TirFormalizationStatus value) => value switch
    {
        TirFormalizationStatus.Unmodeled => "unmodeled",
        TirFormalizationStatus.Partial => "partial",
        TirFormalizationStatus.Validated => "validated",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ReviewStatus(TirReviewStatus value) => value switch
    {
        TirReviewStatus.Captured => "captured",
        TirReviewStatus.Proposed => "proposed",
        TirReviewStatus.InReview => "inReview",
        TirReviewStatus.Accepted => "accepted",
        TirReviewStatus.Rejected => "rejected",
        TirReviewStatus.Superseded => "superseded",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string DependencyKind(TirDependencyKind value) => value switch
    {
        TirDependencyKind.Source => "source",
        TirDependencyKind.Artifact => "artifact",
        TirDependencyKind.Figure => "figure",
        TirDependencyKind.Table => "table",
        TirDependencyKind.Footnote => "footnote",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string HierarchyBasis(TirHierarchyBasis value) => value switch
    {
        TirHierarchyBasis.PublishedIdentifier => "publishedIdentifier",
        TirHierarchyBasis.ChapterContext => "chapterContext",
        TirHierarchyBasis.HeadingPath => "headingPath",
        TirHierarchyBasis.SourceOrder => "sourceOrder",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string HierarchyStatus(TirHierarchyStatus value) => value switch
    {
        TirHierarchyStatus.Root => "root",
        TirHierarchyStatus.Supported => "supported",
        TirHierarchyStatus.Candidate => "candidate",
        TirHierarchyStatus.Missing => "missing",
        TirHierarchyStatus.Ambiguous => "ambiguous",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string SectionBoundaryKind(TirSectionBoundaryKind value) => value switch
    {
        TirSectionBoundaryKind.MarkdownHeading => "markdownHeading",
        TirSectionBoundaryKind.BoldDeclaration => "boldDeclaration",
        TirSectionBoundaryKind.StructuredTextDeclaration => "structuredTextDeclaration",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ReferenceResolutionStatus(TirReferenceResolutionStatus value) => value switch
    {
        TirReferenceResolutionStatus.Resolved => "resolved",
        TirReferenceResolutionStatus.Ambiguous => "ambiguous",
        TirReferenceResolutionStatus.Missing => "missing",
        TirReferenceResolutionStatus.Unresolved => "unresolved",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string DiagnosticSeverity(TirDiagnosticSeverity value) => value switch
    {
        TirDiagnosticSeverity.Information => "information",
        TirDiagnosticSeverity.Warning => "warning",
        TirDiagnosticSeverity.Error => "error",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string FragmentKind(SourceFragmentKind value) => value switch
    {
        SourceFragmentKind.Heading => "heading",
        SourceFragmentKind.RuleText => "ruleText",
        SourceFragmentKind.RuleContinuation => "ruleContinuation",
        SourceFragmentKind.FigureReference => "figureReference",
        SourceFragmentKind.StructuredText => "structuredText",
        SourceFragmentKind.Paragraph => "paragraph",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string Verification(VerificationStatus value) => value switch
    {
        VerificationStatus.Unverified => "unverified",
        VerificationStatus.Verified => "verified",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();

    [GeneratedRegex("^asl-tir:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ArtifactIdRegex();
}
