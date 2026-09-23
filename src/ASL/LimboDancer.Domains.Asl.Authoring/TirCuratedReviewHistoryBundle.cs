using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirCuratedReviewHistoryBundle(
    TirCuratedReviewBundle Submission,
    IReadOnlyList<TirCuratedReviewTransition> Transitions,
    TirReviewStatus EffectiveStatus,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static class TirCuratedReviewHistoryProjector
{
    public static (TirReviewStatus Status, IReadOnlyList<TirCuratedReviewTransition> Ordered)
        Project(TirCuratedReviewBundle submission, IReadOnlyList<TirCuratedReviewTransition> transitions)
    {
        ArgumentNullException.ThrowIfNull(submission);
        ArgumentNullException.ThrowIfNull(transitions);
        _ = TirCuratedReviewBundleJson.SerializePayload(submission);
        var submissionSha256 = TirCuratedReviewBundleJson.ComputePayloadSha256(submission);
        if (transitions.Count is < 1 or > 2)
        {
            throw new InvalidOperationException("A curated review history requires one or two supported transitions.");
        }

        var byId = new Dictionary<string, TirCuratedReviewTransition>(StringComparer.Ordinal);
        foreach (var transition in transitions)
        {
            var id = TirCuratedReviewTransitionJson.CreateRecordId(transition);
            if (!byId.TryAdd(id, transition)
                || transition.Subject != submission.Subject
                || transition.SubmissionBundleSha256 != submissionSha256
                || transition.CreatedAt < submission.CreatedAt
                || transition.Actor.Identity == submission.Proposal.SemanticAuthorIdentity)
            {
                throw new InvalidOperationException(
                    "Curated transition must bind the exact submission, reviewer, and timeline.");
            }
        }

        var opens = transitions.Where(static transition =>
            transition.Action == TirCuratedReviewAction.OpenReview).ToArray();
        if (opens.Length != 1)
        {
            throw new InvalidOperationException("Review history must contain one opening transition.");
        }

        var open = opens[0];
        var openId = TirCuratedReviewTransitionJson.CreateRecordId(open);
        if (transitions.Count == 1)
        {
            return (TirReviewStatus.InReview, [open]);
        }

        var rejection = transitions.Single(transition => transition != open);
        if (rejection.Action != TirCuratedReviewAction.Reject
            || rejection.PriorRecordRef != openId
            || rejection.CreatedAt < open.CreatedAt)
        {
            throw new InvalidOperationException(
                "Rejection must directly follow the exact opening transition.");
        }

        return (TirReviewStatus.Rejected, [open, rejection]);
    }
}

public static class TirCuratedReviewHistoryBundleJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:curated-review-bundle:schema:1.1.0";
    public const string SchemaVersion = "1.1.0";
    public const string ProfileName = "asl-tir-curated-review-bundle-canonical-json";
    public const string ProfileVersion = "1.1.0";

    public static TirCuratedReviewHistoryBundle Create(
        TirCuratedReviewBundle submission,
        IReadOnlyList<TirCuratedReviewTransition> transitions,
        DateTimeOffset createdAt,
        string createdAtSource)
    {
        var status = TirCuratedReviewHistoryProjector.Project(submission, transitions).Status;
        var bundle = new TirCuratedReviewHistoryBundle(
            submission, transitions, status, createdAt, createdAtSource);
        _ = SerializePayload(bundle);
        return bundle;
    }

    public static string Serialize(TirCuratedReviewHistoryBundle bundle)
    {
        var digest = ComputePayloadSha256(bundle);
        return Write(bundle, digest) + "\n";
    }

    public static string SerializePayload(TirCuratedReviewHistoryBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var projection = TirCuratedReviewHistoryProjector.Project(
            bundle.Submission, bundle.Transitions);
        if (bundle.EffectiveStatus != projection.Status
            || bundle.CreatedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(bundle.CreatedAtSource)
            || bundle.CreatedAt < projection.Ordered[^1].CreatedAt)
        {
            throw new InvalidOperationException(
                "Curated review history projection or timestamp does not reproduce.");
        }

        return Write(bundle, null);
    }

    public static string ComputePayloadSha256(TirCuratedReviewHistoryBundle bundle) =>
        Hashing.Sha256Text(SerializePayload(bundle));

    private static string Write(TirCuratedReviewHistoryBundle bundle, string? digest)
    {
        var projection = TirCuratedReviewHistoryProjector.Project(
            bundle.Submission, bundle.Transitions);
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
            writer.WritePropertyName("canonicalization");
            writer.WriteStartObject();
            writer.WriteString("name", ProfileName);
            writer.WriteString("version", ProfileVersion);
            writer.WriteEndObject();
            writer.WritePropertyName("submission");
            writer.WriteRawValue(TirCuratedReviewBundleJson.Serialize(
                bundle.Submission), skipInputValidation: false);
            writer.WriteString("submissionBundleSha256",
                TirCuratedReviewBundleJson.ComputePayloadSha256(bundle.Submission));
            writer.WritePropertyName("transitions");
            writer.WriteStartArray();
            foreach (var record in projection.Ordered)
            {
                writer.WriteRawValue(TirCuratedReviewTransitionJson.Serialize(
                    record), skipInputValidation: false);
            }

            writer.WriteEndArray();
            writer.WriteString("reviewStatus", projection.Status switch
            {
                TirReviewStatus.InReview => "in-review",
                TirReviewStatus.Rejected => "rejected",
                _ => throw new InvalidOperationException("Unsupported curated review projection."),
            });
            writer.WriteString("formalizationStatus", "unmodeled");
            writer.WriteString("createdAt", bundle.CreatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            writer.WriteString("createdAtSource", bundle.CreatedAtSource);
            if (digest is not null)
            {
                writer.WriteString("bundleSha256", digest);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
