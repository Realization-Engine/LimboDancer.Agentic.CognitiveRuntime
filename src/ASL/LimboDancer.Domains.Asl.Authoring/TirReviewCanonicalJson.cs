using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LimboDancer.Domains.Asl.Authoring;

public static partial class TirReviewCanonicalJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:review-record:schema:1.1.0";
    public const string SchemaVersion = "1.1.0";
    public const string ProfileName = "asl-tir-review-canonical-json";
    public const string ProfileVersion = "1.1.0";

    public static string Serialize(TirReviewRecord record)
    {
        Validate(record);
        var digest = ComputePayloadSha256(record);
        var recordId = $"asl-tir-review:sha256:{digest}";
        return Write(writer => WriteRecord(writer, record, recordId, digest)) + "\n";
    }

    public static string SerializePayload(TirReviewRecord record)
    {
        Validate(record);
        return Write(writer => WriteRecord(writer, record, null, null));
    }

    public static string ComputePayloadSha256(TirReviewRecord record)
    {
        return Hashing.Sha256Text(SerializePayload(record));
    }

    public static string CreateRecordId(TirReviewRecord record)
    {
        return $"asl-tir-review:sha256:{ComputePayloadSha256(record)}";
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

    private static void WriteRecord(
        Utf8JsonWriter writer,
        TirReviewRecord record,
        string? recordId,
        string? digest)
    {
        writer.WriteStartObject();
        writer.WriteString("schemaId", SchemaId);
        writer.WriteString("schemaVersion", SchemaVersion);
        if (recordId is not null)
        {
            writer.WriteString("recordId", recordId);
        }

        writer.WriteString("recordKind", RecordKind(record.Kind));
        WriteSubject(writer, record.Subject);
        writer.WriteString("createdAt", FormatTimestamp(record.CreatedAt));
        writer.WriteString("createdAtSource", record.CreatedAtSource);
        WriteActor(writer, record.Actor);
        WriteTool(writer, record.Tool);
        WriteSortedStrings(writer, "prerequisiteRecordRefs", record.PrerequisiteRecordRefs);
        writer.WritePropertyName("canonicalization");
        writer.WriteStartObject();
        writer.WriteString("name", ProfileName);
        writer.WriteString("version", ProfileVersion);
        writer.WriteEndObject();
        writer.WritePropertyName("payload");
        WritePayload(writer, record);
        if (digest is not null)
        {
            writer.WriteString("recordSha256", digest);
        }

        writer.WriteEndObject();
    }

    private static void WriteSubject(Utf8JsonWriter writer, TirReviewSubjectReference subject)
    {
        writer.WritePropertyName("subject");
        writer.WriteStartObject();
        writer.WriteString("tirDocumentSha256", subject.TirDocumentSha256);
        writer.WriteString("artifactId", subject.ArtifactId);
        writer.WriteString("artifactSha256", subject.ArtifactSha256);
        writer.WriteString("schemaId", subject.SchemaId);
        writer.WritePropertyName("packageCandidate");
        writer.WriteStartObject();
        writer.WriteString("domainId", subject.PackageCandidate.DomainId);
        writer.WriteString("packageId", subject.PackageCandidate.PackageId);
        writer.WriteString("version", subject.PackageCandidate.Version);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    private static void WriteActor(Utf8JsonWriter writer, TirReviewActor actor)
    {
        writer.WritePropertyName("actor");
        writer.WriteStartObject();
        writer.WriteString("identity", actor.Identity);
        writer.WriteString("role", ActorRole(actor.Role));
        writer.WriteEndObject();
    }

    private static void WriteTool(Utf8JsonWriter writer, TirReviewTool? tool)
    {
        writer.WritePropertyName("tool");
        if (tool is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("name", tool.Name);
        writer.WriteString("version", tool.Version);
        writer.WriteString("configurationSha256", tool.ConfigurationSha256);
        writer.WriteEndObject();
    }

    private static void WritePayload(Utf8JsonWriter writer, TirReviewRecord record)
    {
        writer.WriteStartObject();
        switch (record)
        {
            case TirValidationReportRecord validation:
                WriteValidationPayload(writer, validation);
                break;
            case TirSourceVerificationRecord verification:
                WriteSourceVerificationPayload(writer, verification);
                break;
            case TirDiagnosticDispositionRecord disposition:
                WriteDiagnosticDispositionPayload(writer, disposition);
                break;
            case TirReviewDecisionRecord review:
                WriteReviewPayload(writer, review);
                break;
            case TirAdjudicationRecord adjudication:
                WriteAdjudicationPayload(writer, adjudication);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported TIR review record type: {record.GetType().Name}.");
        }

        writer.WriteEndObject();
    }

    private static void WriteValidationPayload(
        Utf8JsonWriter writer,
        TirValidationReportRecord record)
    {
        writer.WritePropertyName("policy");
        writer.WriteStartObject();
        writer.WriteString("policyId", record.Policy.PolicyId);
        writer.WriteString("version", record.Policy.Version);
        writer.WriteString("configurationSha256", record.Policy.ConfigurationSha256);
        writer.WriteEndObject();
        writer.WritePropertyName("gateResults");
        writer.WriteStartArray();
        foreach (var result in record.GateResults.OrderBy(
            static result => ValidationGate(result.Gate),
            StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("gate", ValidationGate(result.Gate));
            writer.WriteString("status", GateStatus(result.Status));
            WriteSortedStrings(writer, "findingIds", result.FindingIds);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WritePropertyName("findings");
        writer.WriteStartArray();
        foreach (var finding in record.Findings.OrderBy(
            static finding => finding.FindingId,
            StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("findingId", finding.FindingId);
            writer.WriteString("code", finding.Code);
            writer.WriteString("severity", DiagnosticSeverity(finding.Severity));
            writer.WriteString("gate", ValidationGate(finding.Gate));
            WriteNullableString(writer, "artifactId", finding.ArtifactId);
            writer.WriteString("message", finding.Message);
            WriteSortedStrings(writer, "evidenceRefs", finding.EvidenceRefs);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteSourceVerificationPayload(
        Utf8JsonWriter writer,
        TirSourceVerificationRecord record)
    {
        writer.WritePropertyName("sourceFragment");
        WriteSourceFragment(writer, record.SourceFragment);
        writer.WritePropertyName("sourceEvidence");
        writer.WriteStartObject();
        writer.WriteString("registryId", record.SourceEvidence.RegistryId);
        writer.WriteString("registrySha256", record.SourceEvidence.RegistrySha256);
        writer.WriteString("edition", record.SourceEvidence.Edition);
        writer.WriteString("sourcePath", record.SourceEvidence.SourcePath);
        writer.WriteString("sourceArtifactSha256", record.SourceEvidence.SourceArtifactSha256);
        WriteNullableNumber(writer, "startPage", record.SourceEvidence.StartPage);
        WriteNullableNumber(writer, "endPage", record.SourceEvidence.EndPage);
        writer.WriteEndObject();
        writer.WritePropertyName("dependencies");
        writer.WriteStartArray();
        foreach (var dependency in record.Dependencies
            .OrderBy(static dependency => DependencyKind(dependency.Kind), StringComparer.Ordinal)
            .ThenBy(static dependency => dependency.Target, StringComparer.Ordinal))
        {
            writer.WriteStartObject();
            writer.WriteString("kind", DependencyKind(dependency.Kind));
            writer.WriteString("target", dependency.Target);
            writer.WriteString("sha256", dependency.Sha256);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteString("comparisonMethod", record.ComparisonMethod);
        writer.WriteString("disposition", SourceVerificationDisposition(record.Disposition));
        WriteNullableString(writer, "observedDiscrepancy", record.ObservedDiscrepancy);
        WriteSortedStrings(writer, "correctionProposalRefs", record.CorrectionProposalRefs);
    }

    private static void WriteDiagnosticDispositionPayload(
        Utf8JsonWriter writer,
        TirDiagnosticDispositionRecord record)
    {
        writer.WritePropertyName("finding");
        writer.WriteStartObject();
        writer.WriteString("origin", FindingOrigin(record.Finding.Origin));
        writer.WriteString("findingId", record.Finding.FindingId);
        writer.WriteString("code", record.Finding.Code);
        writer.WriteString("severity", DiagnosticSeverity(record.Finding.Severity));
        WriteNullableString(writer, "reportRecordRef", record.Finding.ReportRecordRef);
        writer.WriteEndObject();
        writer.WriteString("disposition", FindingDisposition(record.Disposition));
        writer.WriteString("rationale", record.Rationale);
        WriteStrings(writer, "evidenceRefs", record.EvidenceRefs);
        WriteNullableString(writer, "replacementRef", record.ReplacementRef);
    }

    private static void WriteReviewPayload(Utf8JsonWriter writer, TirReviewDecisionRecord record)
    {
        writer.WriteString("priorStatus", ReviewStatus(record.PriorStatus));
        writer.WriteString("requestedStatus", ReviewStatus(record.RequestedStatus));
        writer.WriteString("decision", ReviewDecision(record.Decision));
        WriteSortedStrings(
            writer,
            "sourceVerificationRecordRefs",
            record.SourceVerificationRecordRefs);
        WriteSortedStrings(writer, "validationReportRefs", record.ValidationReportRefs);
        WriteSortedStrings(
            writer,
            "diagnosticDispositionRefs",
            record.DiagnosticDispositionRefs);
        WriteSortedStrings(writer, "reviewedDependencyRefs", record.ReviewedDependencyRefs);
        writer.WriteString("rationale", record.Rationale);
        WriteStrings(writer, "evidenceRefs", record.EvidenceRefs);
    }

    private static void WriteAdjudicationPayload(
        Utf8JsonWriter writer,
        TirAdjudicationRecord record)
    {
        WriteSortedStrings(
            writer,
            "conflictingReviewRecordRefs",
            record.ConflictingReviewRecordRefs);
        WriteSortedStrings(writer, "disputedQuestions", record.DisputedQuestions);
        writer.WriteString("decision", ReviewDecision(record.Decision));
        writer.WriteString("rationale", record.Rationale);
        WriteStrings(writer, "evidenceRefs", record.EvidenceRefs);
        if (record.AuthorizedStatus is null)
        {
            writer.WriteNull("authorizedStatus");
        }
        else
        {
            writer.WriteString("authorizedStatus", ReviewStatus(record.AuthorizedStatus.Value));
        }
    }

    private static void WriteSourceFragment(
        Utf8JsonWriter writer,
        TirSourceFragmentReference fragment)
    {
        writer.WriteStartObject();
        writer.WriteString("fragmentId", fragment.FragmentId);
        writer.WriteString("sourceId", fragment.SourceId);
        writer.WriteString("sourceSha256", fragment.SourceSha256);
        writer.WriteString("contentSha256", fragment.ContentSha256);
        writer.WriteNumber("startLine", fragment.StartLine);
        writer.WriteNumber("endLine", fragment.EndLine);
        WriteNullableNumber(writer, "startUtf8ByteOffset", fragment.StartUtf8ByteOffset);
        WriteNullableNumber(
            writer,
            "endUtf8ByteOffsetExclusive",
            fragment.EndUtf8ByteOffsetExclusive);
        writer.WriteEndObject();
    }

    private static void WriteSortedStrings(
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

    private static void WriteStrings(
        Utf8JsonWriter writer,
        string propertyName,
        IReadOnlyList<string> values)
    {
        writer.WritePropertyName(propertyName);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }

    private static void WriteNullableString(
        Utf8JsonWriter writer,
        string propertyName,
        string? value)
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

    private static void WriteNullableNumber(
        Utf8JsonWriter writer,
        string propertyName,
        int? value)
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

    private static void Validate(TirReviewRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ValidateSubject(record.Subject);
        ValidateUtc(record.CreatedAt, nameof(record.CreatedAt));
        Require(record.CreatedAtSource, nameof(record.CreatedAtSource));
        Require(record.Actor.Identity, nameof(record.Actor.Identity));
        ValidateStringSet(record.PrerequisiteRecordRefs, nameof(record.PrerequisiteRecordRefs), true);
        if (record.Tool is not null)
        {
            Require(record.Tool.Name, nameof(record.Tool.Name));
            Require(record.Tool.Version, nameof(record.Tool.Version));
            ValidateSha256(record.Tool.ConfigurationSha256, nameof(record.Tool.ConfigurationSha256));
        }

        switch (record)
        {
            case TirValidationReportRecord validation:
                ValidateValidationReport(validation);
                break;
            case TirSourceVerificationRecord verification:
                ValidateSourceVerification(verification);
                break;
            case TirDiagnosticDispositionRecord disposition:
                ValidateDiagnosticDisposition(disposition);
                break;
            case TirReviewDecisionRecord review:
                ValidateReview(review);
                break;
            case TirAdjudicationRecord adjudication:
                ValidateAdjudication(adjudication);
                break;
            default:
                throw new InvalidOperationException(
                    $"Unsupported TIR review record type: {record.GetType().Name}.");
        }
    }

    private static void ValidateSubject(TirReviewSubjectReference subject)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ValidateSha256(subject.TirDocumentSha256, nameof(subject.TirDocumentSha256));
        ValidateSha256(subject.ArtifactSha256, nameof(subject.ArtifactSha256));
        if (!ArtifactIdRegex().IsMatch(subject.ArtifactId))
        {
            throw new InvalidOperationException("Review subject artifact identity is invalid.");
        }

        if (!string.Equals(subject.SchemaId, TirCanonicalJson.SchemaId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Unsupported review subject TIR schema.");
        }

        Require(subject.PackageCandidate.DomainId, nameof(subject.PackageCandidate.DomainId));
        Require(subject.PackageCandidate.PackageId, nameof(subject.PackageCandidate.PackageId));
        Require(subject.PackageCandidate.Version, nameof(subject.PackageCandidate.Version));
    }

    private static void ValidateValidationReport(TirValidationReportRecord record)
    {
        if (record.Actor.Role != TirReviewActorRole.Validator || record.Tool is null)
        {
            throw new InvalidOperationException(
                "A validation report requires the validator role and tool identity.");
        }

        Require(record.Policy.PolicyId, nameof(record.Policy.PolicyId));
        Require(record.Policy.Version, nameof(record.Policy.Version));
        ValidateSha256(record.Policy.ConfigurationSha256, nameof(record.Policy.ConfigurationSha256));
        var findingIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var finding in record.Findings)
        {
            if (!FindingIdRegex().IsMatch(finding.FindingId) || !findingIds.Add(finding.FindingId))
            {
                throw new InvalidOperationException("Validation finding identities must be unique and valid.");
            }

            Require(finding.Code, nameof(finding.Code));
            Require(finding.Message, nameof(finding.Message));
            if (finding.ArtifactId is not null && !ArtifactIdRegex().IsMatch(finding.ArtifactId))
            {
                throw new InvalidOperationException("Validation finding artifact identity is invalid.");
            }

            ValidateStringSet(finding.EvidenceRefs, nameof(finding.EvidenceRefs), false);
            var expectedId = TirReviewIdentity.CreateFindingId(
                record.Subject,
                record.Policy,
                finding.Gate,
                finding.Code,
                finding.ArtifactId,
                finding.EvidenceRefs);
            if (!string.Equals(finding.FindingId, expectedId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Validation finding identity does not match its canonical inputs: {finding.FindingId}.");
            }
        }

        var gates = new HashSet<TirValidationGate>();
        foreach (var result in record.GateResults)
        {
            if (!gates.Add(result.Gate))
            {
                throw new InvalidOperationException("Validation gates must occur at most once per report.");
            }

            ValidateStringSet(result.FindingIds, nameof(result.FindingIds), false);
            if (result.FindingIds.Any(id => !FindingIdRegex().IsMatch(id)))
            {
                throw new InvalidOperationException("Gate results contain an invalid finding identity.");
            }

            if (result.FindingIds.Any(id => !findingIds.Contains(id)))
            {
                throw new InvalidOperationException("Gate results may reference only findings in the report.");
            }

            if (result.Status == TirValidationGateStatus.Failed && result.FindingIds.Count == 0)
            {
                throw new InvalidOperationException("A failed validation gate requires a finding.");
            }

            if (result.Status == TirValidationGateStatus.NotApplicable
                && result.FindingIds.Count != 0)
            {
                throw new InvalidOperationException(
                    "A not-applicable validation gate cannot reference findings.");
            }
        }
    }

    private static void ValidateSourceVerification(TirSourceVerificationRecord record)
    {
        if (record.Actor.Role != TirReviewActorRole.SourceVerifier)
        {
            throw new InvalidOperationException(
                "A source-verification record requires the source-verifier role.");
        }

        ValidateSourceFragment(record.SourceFragment);
        Require(record.SourceEvidence.RegistryId, nameof(record.SourceEvidence.RegistryId));
        ValidateSha256(
            record.SourceEvidence.RegistrySha256,
            nameof(record.SourceEvidence.RegistrySha256));
        Require(record.SourceEvidence.Edition, nameof(record.SourceEvidence.Edition));
        Require(record.SourceEvidence.SourcePath, nameof(record.SourceEvidence.SourcePath));
        ValidateSha256(
            record.SourceEvidence.SourceArtifactSha256,
            nameof(record.SourceEvidence.SourceArtifactSha256));
        if ((record.SourceEvidence.StartPage is null) != (record.SourceEvidence.EndPage is null)
            || record.SourceEvidence.StartPage is < 1
            || record.SourceEvidence.EndPage < record.SourceEvidence.StartPage)
        {
            throw new InvalidOperationException(
                "Source-evidence pages must both be null or form a positive ordered range.");
        }

        Require(record.ComparisonMethod, nameof(record.ComparisonMethod));
        var dependencies = new HashSet<(TirDependencyKind Kind, string Target)>();
        foreach (var dependency in record.Dependencies)
        {
            if (dependency.Kind is not TirDependencyKind.Figure
                and not TirDependencyKind.Table)
            {
                throw new InvalidOperationException(
                    "Source verification may hash only figure and table dependencies.");
            }

            Require(dependency.Target, nameof(dependency.Target));
            ValidateSha256(dependency.Sha256, nameof(dependency.Sha256));
            if (!dependencies.Add((dependency.Kind, dependency.Target)))
            {
                throw new InvalidOperationException(
                    "Verified source dependencies must be unique by kind and target.");
            }
        }

        ValidateStringSet(record.CorrectionProposalRefs, nameof(record.CorrectionProposalRefs), false);
        if (record.Disposition == TirSourceVerificationDisposition.Verified
            && record.ObservedDiscrepancy is not null)
        {
            throw new InvalidOperationException(
                "A verified source record cannot report a discrepancy.");
        }

        if (record.Disposition == TirSourceVerificationDisposition.Mismatch
            && string.IsNullOrWhiteSpace(record.ObservedDiscrepancy))
        {
            throw new InvalidOperationException("A source mismatch requires discrepancy details.");
        }

        if (record.Disposition != TirSourceVerificationDisposition.Verified
            && string.IsNullOrWhiteSpace(record.ObservedDiscrepancy))
        {
            throw new InvalidOperationException(
                "A non-verified source record requires discrepancy or indeterminacy details.");
        }

        if (record.Disposition == TirSourceVerificationDisposition.Mismatch
            && record.CorrectionProposalRefs.Count == 0)
        {
            throw new InvalidOperationException(
                "A source mismatch requires a correction proposal reference.");
        }
    }

    private static void ValidateDiagnosticDisposition(TirDiagnosticDispositionRecord record)
    {
        if (record.Actor.Role is not TirReviewActorRole.DomainReviewer
            and not TirReviewActorRole.Adjudicator)
        {
            throw new InvalidOperationException(
                "A diagnostic disposition requires a domain-reviewer or adjudicator role.");
        }

        var validFindingId = record.Finding.Origin switch
        {
            TirFindingOrigin.ExtractedDiagnostic => DiagnosticIdRegex().IsMatch(
                record.Finding.FindingId),
            TirFindingOrigin.ValidationFinding => FindingIdRegex().IsMatch(
                record.Finding.FindingId),
            _ => false,
        };
        if (!validFindingId)
        {
            throw new InvalidOperationException("Diagnostic disposition finding identity is invalid.");
        }

        Require(record.Finding.Code, nameof(record.Finding.Code));
        if (record.Finding.Origin == TirFindingOrigin.ExtractedDiagnostic
            && record.Finding.ReportRecordRef is not null)
        {
            throw new InvalidOperationException(
                "An extracted diagnostic cannot claim a validation-report reference.");
        }

        if (record.Finding.Origin == TirFindingOrigin.ValidationFinding
            && record.Finding.ReportRecordRef is null)
        {
            throw new InvalidOperationException(
                "A validation finding requires its validation-report reference.");
        }

        ValidateOptionalRecordRef(record.Finding.ReportRecordRef, nameof(record.Finding.ReportRecordRef));
        Require(record.Rationale, nameof(record.Rationale));
        ValidateStringSequence(record.EvidenceRefs, nameof(record.EvidenceRefs));
        ValidateOptionalReference(record.ReplacementRef, nameof(record.ReplacementRef));
        if (record.Disposition == TirFindingDisposition.Resolved
            && record.ReplacementRef is null)
        {
            throw new InvalidOperationException(
                "A resolved finding requires changed-evidence or replacement-subject reference.");
        }

        if ((record.Disposition is TirFindingDisposition.Resolved
                or TirFindingDisposition.NotApplicable
                or TirFindingDisposition.AcceptedLimitation)
            && record.EvidenceRefs.Count == 0)
        {
            throw new InvalidOperationException(
                "This diagnostic disposition requires supporting evidence.");
        }
    }

    private static void ValidateReview(TirReviewDecisionRecord record)
    {
        if (record.Actor.Role != TirReviewActorRole.DomainReviewer)
        {
            throw new InvalidOperationException("A review record requires the domain-reviewer role.");
        }

        ValidateStringSet(
            record.SourceVerificationRecordRefs,
            nameof(record.SourceVerificationRecordRefs),
            true);
        ValidateStringSet(record.ValidationReportRefs, nameof(record.ValidationReportRefs), true);
        ValidateStringSet(
            record.DiagnosticDispositionRefs,
            nameof(record.DiagnosticDispositionRefs),
            true);
        ValidateStringSet(record.ReviewedDependencyRefs, nameof(record.ReviewedDependencyRefs), false);
        Require(record.Rationale, nameof(record.Rationale));
        ValidateStringSequence(record.EvidenceRefs, nameof(record.EvidenceRefs));
    }

    private static void ValidateAdjudication(TirAdjudicationRecord record)
    {
        if (record.Actor.Role != TirReviewActorRole.Adjudicator)
        {
            throw new InvalidOperationException("An adjudication record requires the adjudicator role.");
        }

        ValidateStringSet(
            record.ConflictingReviewRecordRefs,
            nameof(record.ConflictingReviewRecordRefs),
            true);
        if (record.ConflictingReviewRecordRefs.Count < 2)
        {
            throw new InvalidOperationException(
                "Adjudication requires at least two conflicting review records.");
        }

        ValidateStringSet(record.DisputedQuestions, nameof(record.DisputedQuestions), false);
        if (record.DisputedQuestions.Count == 0)
        {
            throw new InvalidOperationException("Adjudication requires a disputed question.");
        }

        if (record.Decision == TirReviewDecision.Abstain)
        {
            throw new InvalidOperationException("An adjudicator cannot abstain in an adjudication record.");
        }

        Require(record.Rationale, nameof(record.Rationale));
        ValidateStringSequence(record.EvidenceRefs, nameof(record.EvidenceRefs));
    }

    private static void ValidateSourceFragment(TirSourceFragmentReference fragment)
    {
        ValidateSha256(fragment.SourceSha256, nameof(fragment.SourceSha256));
        ValidateSha256(fragment.ContentSha256, nameof(fragment.ContentSha256));
        if (!FragmentIdRegex().IsMatch(fragment.FragmentId))
        {
            throw new InvalidOperationException("Source-fragment identity is invalid.");
        }

        Require(fragment.SourceId, nameof(fragment.SourceId));
        if (fragment.StartLine < 1 || fragment.EndLine < fragment.StartLine)
        {
            throw new InvalidOperationException("Source-fragment line ranges must be positive and ordered.");
        }

        if ((fragment.StartUtf8ByteOffset is null)
            != (fragment.EndUtf8ByteOffsetExclusive is null))
        {
            throw new InvalidOperationException(
                "Source-fragment UTF-8 offsets must both be null or both be present.");
        }

        if (fragment.StartUtf8ByteOffset is not null
            && (fragment.StartUtf8ByteOffset < 0
                || fragment.EndUtf8ByteOffsetExclusive <= fragment.StartUtf8ByteOffset))
        {
            throw new InvalidOperationException(
                "Source-fragment UTF-8 offsets must be non-negative, non-empty, and ordered.");
        }
    }

    private static void ValidateStringSet(
        IReadOnlyList<string> values,
        string name,
        bool recordReferences)
    {
        ArgumentNullException.ThrowIfNull(values);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values)
        {
            Require(value, name);
            if (!seen.Add(value))
            {
                throw new InvalidOperationException($"{name} must not contain duplicates.");
            }

            if (recordReferences && !ReviewRecordIdRegex().IsMatch(value))
            {
                throw new InvalidOperationException($"{name} contains an invalid review-record reference.");
            }
        }
    }

    private static void ValidateOptionalRecordRef(string? value, string name)
    {
        if (value is not null && !ReviewRecordIdRegex().IsMatch(value))
        {
            throw new InvalidOperationException($"{name} is not a valid review-record reference.");
        }
    }

    private static void ValidateStringSequence(IReadOnlyList<string> values, string name)
    {
        ArgumentNullException.ThrowIfNull(values);
        foreach (var value in values)
        {
            Require(value, name);
        }
    }

    private static void ValidateOptionalReference(string? value, string name)
    {
        if (value is not null)
        {
            Require(value, name);
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

    private static void Require(string value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{name} is required.");
        }
    }

    private static string FormatTimestamp(DateTimeOffset value)
    {
        return value.ToUniversalTime().ToString(
            "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
            CultureInfo.InvariantCulture);
    }

    private static string RecordKind(TirReviewRecordKind value) => value switch
    {
        TirReviewRecordKind.ValidationReport => "validationReport",
        TirReviewRecordKind.SourceVerification => "sourceVerification",
        TirReviewRecordKind.DiagnosticDisposition => "diagnosticDisposition",
        TirReviewRecordKind.Review => "review",
        TirReviewRecordKind.Adjudication => "adjudication",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ActorRole(TirReviewActorRole value) => value switch
    {
        TirReviewActorRole.Validator => "validator",
        TirReviewActorRole.SourceVerifier => "sourceVerifier",
        TirReviewActorRole.SemanticAuthor => "semanticAuthor",
        TirReviewActorRole.DomainReviewer => "domainReviewer",
        TirReviewActorRole.Adjudicator => "adjudicator",
        TirReviewActorRole.ReleaseApprover => "releaseApprover",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ValidationGate(TirValidationGate value) => value switch
    {
        TirValidationGate.InputSchema => "inputSchema",
        TirValidationGate.Identity => "identity",
        TirValidationGate.Structural => "structural",
        TirValidationGate.SourceProvenance => "sourceProvenance",
        TirValidationGate.Semantic => "semantic",
        TirValidationGate.Review => "review",
        TirValidationGate.ArchitectureAuthority => "architectureAuthority",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string GateStatus(TirValidationGateStatus value) => value switch
    {
        TirValidationGateStatus.Passed => "passed",
        TirValidationGateStatus.Failed => "failed",
        TirValidationGateStatus.NotApplicable => "notApplicable",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string FindingOrigin(TirFindingOrigin value) => value switch
    {
        TirFindingOrigin.ExtractedDiagnostic => "extractedDiagnostic",
        TirFindingOrigin.ValidationFinding => "validationFinding",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string FindingDisposition(TirFindingDisposition value) => value switch
    {
        TirFindingDisposition.Resolved => "resolved",
        TirFindingDisposition.NotApplicable => "notApplicable",
        TirFindingDisposition.Deferred => "deferred",
        TirFindingDisposition.AcceptedLimitation => "acceptedLimitation",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string SourceVerificationDisposition(TirSourceVerificationDisposition value) => value switch
    {
        TirSourceVerificationDisposition.Verified => "verified",
        TirSourceVerificationDisposition.Mismatch => "mismatch",
        TirSourceVerificationDisposition.DependencyMissing => "dependencyMissing",
        TirSourceVerificationDisposition.Indeterminate => "indeterminate",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, null),
    };

    private static string ReviewDecision(TirReviewDecision value) => value switch
    {
        TirReviewDecision.Approve => "approve",
        TirReviewDecision.Reject => "reject",
        TirReviewDecision.RequestChanges => "requestChanges",
        TirReviewDecision.Abstain => "abstain",
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

    private static string DiagnosticSeverity(TirDiagnosticSeverity value) => value switch
    {
        TirDiagnosticSeverity.Information => "information",
        TirDiagnosticSeverity.Warning => "warning",
        TirDiagnosticSeverity.Error => "error",
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

    [GeneratedRegex("^[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Regex();

    [GeneratedRegex("^asl-tir:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ArtifactIdRegex();

    [GeneratedRegex("^asl-fragment:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex FragmentIdRegex();

    [GeneratedRegex("^asl-tir-review:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReviewRecordIdRegex();

    [GeneratedRegex("^asl-tir-finding:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex FindingIdRegex();

    [GeneratedRegex("^asl-tir-diagnostic:sha256:[0-9a-f]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex DiagnosticIdRegex();
}
