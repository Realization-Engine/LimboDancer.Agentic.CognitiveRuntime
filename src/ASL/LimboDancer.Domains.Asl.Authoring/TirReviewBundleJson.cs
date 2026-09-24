using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

public static class TirReviewBundleJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:review-bundle:schema:1.0.0";
    public const string SchemaVersion = "1.0.0";
    public const string ProfileName = "asl-tir-review-bundle-canonical-json";
    public const string ProfileVersion = "1.0.0";

    public static string Serialize(TirReviewBundle bundle)
    {
        var digest = ComputePayloadSha256(bundle);
        return Write(bundle, digest) + "\n";
    }

    public static string SerializePayload(TirReviewBundle bundle)
    {
        ArgumentNullException.ThrowIfNull(bundle);
        var projection = TirReviewStateProjector.Project(
            bundle.Document,
            bundle.Subject,
            bundle.Policy,
            bundle.Records,
            bundle.DeclaredUse);
        if (!ProjectionMatches(bundle.Projection, projection))
        {
            throw new InvalidOperationException("Review bundle projection does not reproduce from its record set.");
        }

        if (bundle.CreatedAt.Offset != TimeSpan.Zero
            || string.IsNullOrWhiteSpace(bundle.CreatedAtSource)
            || bundle.CoverageRefs.Any(string.IsNullOrWhiteSpace)
            || bundle.CoverageRefs.Distinct(StringComparer.Ordinal).Count() != bundle.CoverageRefs.Count)
        {
            throw new InvalidOperationException("Review bundle metadata must be UTC, nonempty, and unambiguous.");
        }

        return Write(bundle, null);
    }

    public static string ComputePayloadSha256(TirReviewBundle bundle)
    {
        return Hashing.Sha256Text(SerializePayload(bundle));
    }

    private static bool ProjectionMatches(TirReviewProjection supplied, TirReviewProjection computed)
    {
        return supplied.EffectiveReviewStatus == computed.EffectiveReviewStatus
            && supplied.EffectiveFormalizationStatus == computed.EffectiveFormalizationStatus
            && supplied.OpenFindingIds.SequenceEqual(computed.OpenFindingIds, StringComparer.Ordinal)
            && supplied.BlockingFindingIds.SequenceEqual(computed.BlockingFindingIds, StringComparer.Ordinal);
    }

    private static string Write(TirReviewBundle bundle, string? digest)
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
            writer.WriteStartObject();
            writer.WriteString("schemaId", SchemaId);
            writer.WriteString("schemaVersion", SchemaVersion);
            writer.WritePropertyName("canonicalization");
            writer.WriteStartObject();
            writer.WriteString("name", ProfileName);
            writer.WriteString("version", ProfileVersion);
            writer.WriteEndObject();
            writer.WriteString("tirDocumentSha256", bundle.Subject.TirDocumentSha256);
            writer.WriteString("artifactId", bundle.Subject.ArtifactId);
            writer.WriteString("artifactSha256", bundle.Subject.ArtifactSha256);
            writer.WriteString("tirSchemaId", bundle.Subject.SchemaId);
            writer.WritePropertyName("packageCandidate");
            writer.WriteStartObject();
            writer.WriteString("domainId", bundle.Subject.PackageCandidate.DomainId);
            writer.WriteString("packageId", bundle.Subject.PackageCandidate.PackageId);
            writer.WriteString("version", bundle.Subject.PackageCandidate.Version);
            writer.WriteEndObject();
            writer.WritePropertyName("policy");
            writer.WriteStartObject();
            writer.WriteString("policyId", bundle.Policy.PolicyId);
            writer.WriteString("version", bundle.Policy.Version);
            writer.WriteString("configurationSha256", bundle.Policy.ConfigurationSha256);
            writer.WriteEndObject();
            writer.WriteString("declaredUse", bundle.DeclaredUse);
            WriteStrings(writer, "coverageRefs", bundle.CoverageRefs.Order(StringComparer.Ordinal));
            writer.WritePropertyName("records");
            writer.WriteStartArray();
            foreach (var record in bundle.Records
                .OrderBy(static item => item.CreatedAt)
                .ThenBy(TirReviewCanonicalJson.CreateRecordId, StringComparer.Ordinal))
            {
                writer.WriteRawValue(TirReviewCanonicalJson.Serialize(record), skipInputValidation: false);
            }

            writer.WriteEndArray();
            writer.WritePropertyName("projection");
            writer.WriteStartObject();
            writer.WriteString("reviewStatus", "captured");
            writer.WriteString("formalizationStatus", "unmodeled");
            WriteStrings(writer, "openFindingIds", bundle.Projection.OpenFindingIds);
            WriteStrings(writer, "blockingFindingIds", bundle.Projection.BlockingFindingIds);
            writer.WriteEndObject();
            writer.WriteString("createdAt", bundle.CreatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'",
                CultureInfo.InvariantCulture));
            writer.WriteString("createdAtSource", bundle.CreatedAtSource);
            if (digest is not null)
            {
                writer.WriteString("bundleSha256", digest);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteStrings(Utf8JsonWriter writer, string name, IEnumerable<string> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value);
        }

        writer.WriteEndArray();
    }
}
