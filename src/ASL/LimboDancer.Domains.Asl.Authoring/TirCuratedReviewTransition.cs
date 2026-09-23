using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public enum TirCuratedReviewAction
{
    OpenReview,
    Reject,
}

public sealed record TirCuratedReviewTransition(
    TirCuratedReviewSubjectReference Subject,
    string SubmissionBundleSha256,
    string? PriorRecordRef,
    TirReviewStatus PriorStatus,
    TirReviewStatus RequestedStatus,
    TirCuratedReviewAction Action,
    TirReviewActor Actor,
    string Rationale,
    IReadOnlyList<string> EvidenceRefs,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static class TirCuratedReviewTransitionJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:curated-review-transition:schema:1.0.0";
    public const string SchemaVersion = "1.0.0";
    public const string ProfileName = "asl-tir-curated-review-transition-canonical-json";
    public const string ProfileVersion = "1.0.0";

    public static string Serialize(TirCuratedReviewTransition record)
    {
        var digest = ComputePayloadSha256(record);
        return Write(record, digest) + "\n";
    }

    public static string SerializePayload(TirCuratedReviewTransition record)
    {
        Validate(record);
        return Write(record, null);
    }

    public static string ComputePayloadSha256(TirCuratedReviewTransition record) =>
        Hashing.Sha256Text(SerializePayload(record));

    public static string CreateRecordId(TirCuratedReviewTransition record) =>
        $"asl-curated-review:sha256:{ComputePayloadSha256(record)}";

    private static void Validate(TirCuratedReviewTransition record)
    {
        ArgumentNullException.ThrowIfNull(record);
        ArgumentNullException.ThrowIfNull(record.Subject);
        ArgumentNullException.ThrowIfNull(record.Actor);
        ArgumentNullException.ThrowIfNull(record.EvidenceRefs);
        if (record.Subject.SourceSubject is null
            || record.Subject.ProposalSchemaId != TirCuratedProposalJson.SchemaId
            || !IsSha256(record.Subject.ProposalSha256)
            || !IsSha256(record.Subject.SourceSubject.TirDocumentSha256)
            || !IsSha256(record.Subject.SourceSubject.ArtifactSha256)
            || record.Subject.SourceSubject.SchemaId != TirCanonicalJson.SchemaId
            || !IsSha256(record.SubmissionBundleSha256)
            || record.Actor.Role != TirReviewActorRole.DomainReviewer
            || string.IsNullOrWhiteSpace(record.Actor.Identity)
            || string.IsNullOrWhiteSpace(record.Rationale)
            || string.IsNullOrWhiteSpace(record.CreatedAtSource)
            || record.CreatedAt.Offset != TimeSpan.Zero
            || record.EvidenceRefs.Any(string.IsNullOrWhiteSpace)
            || record.EvidenceRefs.Distinct(StringComparer.Ordinal).Count() != record.EvidenceRefs.Count)
        {
            throw new InvalidOperationException("Invalid curated review transition identity or attestation.");
        }

        if (record.PriorRecordRef is not null
            && (!record.PriorRecordRef.StartsWith("asl-curated-review:sha256:", StringComparison.Ordinal)
                || !IsSha256(record.PriorRecordRef["asl-curated-review:sha256:".Length..])))
        {
            throw new InvalidOperationException("Invalid prior curated review record reference.");
        }

        var valid = record.Action switch
        {
            TirCuratedReviewAction.OpenReview =>
                record.PriorStatus == TirReviewStatus.Proposed
                && record.RequestedStatus == TirReviewStatus.InReview
                && record.PriorRecordRef is null,
            TirCuratedReviewAction.Reject =>
                record.PriorStatus == TirReviewStatus.InReview
                && record.RequestedStatus == TirReviewStatus.Rejected
                && record.PriorRecordRef is not null,
            _ => false,
        };
        if (!valid)
        {
            throw new InvalidOperationException(
                "Unsupported curated review transition; acceptance and supersession are unavailable.");
        }
    }

    private static bool IsSha256(string? value) =>
        value is { Length: 64 } && value.All(static character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string Write(TirCuratedReviewTransition record, string? digest)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            Indented = false,
        }))
        {
            writer.WriteStartObject();
            writer.WriteString("schemaId", SchemaId);
            writer.WriteString("schemaVersion", SchemaVersion);
            if (digest is not null)
            {
                writer.WriteString("recordId", $"asl-curated-review:sha256:{digest}");
            }

            writer.WritePropertyName("canonicalization");
            writer.WriteStartObject();
            writer.WriteString("name", ProfileName);
            writer.WriteString("version", ProfileVersion);
            writer.WriteEndObject();
            writer.WritePropertyName("subject");
            writer.WriteStartObject();
            writer.WriteString("proposalSchemaId", record.Subject.ProposalSchemaId);
            writer.WriteString("proposalSha256", record.Subject.ProposalSha256);
            writer.WriteString("sourceTirDocumentSha256", record.Subject.SourceSubject.TirDocumentSha256);
            writer.WriteString("sourceArtifactId", record.Subject.SourceSubject.ArtifactId);
            writer.WriteString("sourceArtifactSha256", record.Subject.SourceSubject.ArtifactSha256);
            writer.WriteEndObject();
            writer.WriteString("submissionBundleSha256", record.SubmissionBundleSha256);
            if (record.PriorRecordRef is null)
            {
                writer.WriteNull("priorRecordRef");
            }
            else
            {
                writer.WriteString("priorRecordRef", record.PriorRecordRef);
            }

            writer.WriteString("priorStatus", Status(record.PriorStatus));
            writer.WriteString("requestedStatus", Status(record.RequestedStatus));
            writer.WriteString("action", record.Action switch
            {
                TirCuratedReviewAction.OpenReview => "openReview",
                TirCuratedReviewAction.Reject => "reject",
                _ => throw new InvalidOperationException("Unsupported curated review action."),
            });
            writer.WritePropertyName("actor");
            writer.WriteStartObject();
            writer.WriteString("identity", record.Actor.Identity);
            writer.WriteString("role", "domainReviewer");
            writer.WriteEndObject();
            writer.WriteString("rationale", record.Rationale);
            writer.WritePropertyName("evidenceRefs");
            writer.WriteStartArray();
            foreach (var reference in record.EvidenceRefs.Order(StringComparer.Ordinal))
            {
                writer.WriteStringValue(reference);
            }

            writer.WriteEndArray();
            writer.WriteString("createdAt", record.CreatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            writer.WriteString("createdAtSource", record.CreatedAtSource);
            if (digest is not null)
            {
                writer.WriteString("recordSha256", digest);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Status(TirReviewStatus status) => status switch
    {
        TirReviewStatus.Proposed => "proposed",
        TirReviewStatus.InReview => "in-review",
        TirReviewStatus.Rejected => "rejected",
        _ => throw new InvalidOperationException("Unsupported curated review status."),
    };
}
