using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public sealed record TirCuratedReviewSubjectReference(
    TirReviewSubjectReference SourceSubject,
    string ProposalSchemaId,
    string ProposalSha256);

public sealed record TirCuratedReviewBundle(
    TirDocument SourceDocument,
    TirCuratedProposal Proposal,
    TirReviewBundle SourceReviewBundle,
    TirCuratedReviewSubjectReference Subject,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static class TirCuratedReviewSubjects
{
    public static TirCuratedReviewSubjectReference Create(
        TirCuratedProposal proposal,
        TirDocument sourceDocument)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        return new TirCuratedReviewSubjectReference(
            proposal.SourceSubject,
            TirCuratedProposalJson.SchemaId,
            TirCuratedProposalJson.ComputePayloadSha256(proposal, sourceDocument));
    }
}

/// <summary>
/// A submission manifest, not a review decision or acceptance authorization.
/// Its only effective review state is proposed; no curated review records are
/// represented by this schema version.
/// </summary>
public static class TirCuratedReviewBundleJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:curated-review-bundle:schema:1.0.0";
    public const string SchemaVersion = "1.0.0";
    public const string ProfileName = "asl-tir-curated-review-bundle-canonical-json";
    public const string ProfileVersion = "1.0.0";

    public static TirCuratedReviewBundle Create(
        TirDocument sourceDocument,
        TirCuratedProposal proposal,
        TirReviewBundle sourceReviewBundle,
        DateTimeOffset createdAt,
        string createdAtSource)
    {
        var subject = TirCuratedReviewSubjects.Create(proposal, sourceDocument);
        var bundle = new TirCuratedReviewBundle(
            sourceDocument, proposal, sourceReviewBundle, subject, createdAt, createdAtSource);
        _ = SerializePayload(bundle);
        return bundle;
    }

    public static string Serialize(TirCuratedReviewBundle bundle)
    {
        var digest = ComputePayloadSha256(bundle);
        return Write(bundle, digest) + "\n";
    }

    public static string SerializePayload(TirCuratedReviewBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        if (bundle.Subject != TirCuratedReviewSubjects.Create(
                bundle.Proposal, bundle.SourceDocument))
        {
            throw new InvalidOperationException("Curated review subject does not match the exact proposal.");
        }

        // Serialization of the captured bundle reprojects and validates every
        // embedded source record; the curated submission does not inherit approval.
        _ = TirReviewBundleJson.SerializePayload(bundle.SourceReviewBundle);
        if (bundle.SourceReviewBundle.Subject != bundle.Subject.SourceSubject
            || TirCanonicalJson.ComputePayloadSha256(bundle.SourceReviewBundle.Document)
                != TirCanonicalJson.ComputePayloadSha256(bundle.SourceDocument)
            || bundle.SourceReviewBundle.DeclaredUse != bundle.Proposal.DeclaredUse)
        {
            throw new InvalidOperationException(
                "Curated submission requires a source review bundle for the exact source and declared use.");
        }

        if (bundle.CreatedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(bundle.CreatedAtSource)
            || bundle.CreatedAt < bundle.Proposal.CreatedAt
            || bundle.CreatedAt < bundle.SourceReviewBundle.CreatedAt)
        {
            throw new InvalidOperationException(
                "Curated submission requires a UTC timestamp after its evidence.");
        }

        return Write(bundle, null);
    }

    public static string ComputePayloadSha256(TirCuratedReviewBundle bundle)
    {
        return Hashing.Sha256Text(SerializePayload(bundle));
    }

    private static string Write(TirCuratedReviewBundle bundle, string? digest)
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
            writer.WritePropertyName("canonicalization");
            writer.WriteStartObject();
            writer.WriteString("name", ProfileName);
            writer.WriteString("version", ProfileVersion);
            writer.WriteEndObject();
            writer.WritePropertyName("subject");
            writer.WriteStartObject();
            writer.WriteString("proposalSchemaId", bundle.Subject.ProposalSchemaId);
            writer.WriteString("proposalSha256", bundle.Subject.ProposalSha256);
            writer.WriteString("sourceTirDocumentSha256", bundle.Subject.SourceSubject.TirDocumentSha256);
            writer.WriteString("sourceArtifactId", bundle.Subject.SourceSubject.ArtifactId);
            writer.WriteString("sourceArtifactSha256", bundle.Subject.SourceSubject.ArtifactSha256);
            writer.WriteEndObject();
            writer.WritePropertyName("proposal");
            writer.WriteRawValue(TirCuratedProposalJson.Serialize(
                bundle.Proposal, bundle.SourceDocument), skipInputValidation: false);
            writer.WriteString("sourceReviewBundleSchemaId", TirReviewBundleJson.SchemaId);
            writer.WriteString("sourceReviewBundleSha256",
                TirReviewBundleJson.ComputePayloadSha256(bundle.SourceReviewBundle));
            writer.WriteString("declaredUse", bundle.Proposal.DeclaredUse);
            writer.WriteString("reviewStatus", "proposed");
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
