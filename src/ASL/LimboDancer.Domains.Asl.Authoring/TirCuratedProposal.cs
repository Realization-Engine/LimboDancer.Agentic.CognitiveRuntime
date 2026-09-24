using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace LimboDancer.Domains.Asl.Authoring;

/// <summary>
/// An exact, non-executable authoring proposal. This is not a TIR 1.3 artifact,
/// a validated semantic model, or an accepted review subject.
/// </summary>
public sealed record TirCuratedProposal(
    TirReviewSubjectReference SourceSubject,
    string SemanticAuthorIdentity,
    string DeclaredUse,
    string ProposalText,
    DateTimeOffset CreatedAt,
    string CreatedAtSource);

public static class TirCuratedProposalJson
{
    public const string SchemaId = "urn:limbodancer:asl:tir:curated-proposal:schema:1.0.0";
    public const string SchemaVersion = "1.0.0";
    public const string ProfileName = "asl-tir-curated-proposal-canonical-json";
    public const string ProfileVersion = "1.0.0";

    public static string Serialize(TirCuratedProposal proposal, TirDocument sourceDocument)
    {
        var digest = ComputePayloadSha256(proposal, sourceDocument);
        return Write(proposal, digest) + "\n";
    }

    public static string SerializePayload(TirCuratedProposal proposal, TirDocument sourceDocument)
    {
        Validate(proposal, sourceDocument);
        return Write(proposal, null);
    }

    public static string ComputePayloadSha256(TirCuratedProposal proposal, TirDocument sourceDocument)
    {
        return Hashing.Sha256Text(SerializePayload(proposal, sourceDocument));
    }

    private static void Validate(TirCuratedProposal proposal, TirDocument sourceDocument)
    {
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentNullException.ThrowIfNull(sourceDocument);
        ArgumentNullException.ThrowIfNull(proposal.SourceSubject);
        if (proposal.SourceSubject != TirReviewSubjects.Create(
                sourceDocument, proposal.SourceSubject.ArtifactId))
        {
            throw new InvalidOperationException("Curated proposal source is not the exact TIR snapshot.");
        }

        if (string.IsNullOrWhiteSpace(proposal.SemanticAuthorIdentity)
            || string.IsNullOrWhiteSpace(proposal.DeclaredUse)
            || string.IsNullOrWhiteSpace(proposal.ProposalText)
            || string.IsNullOrWhiteSpace(proposal.CreatedAtSource)
            || proposal.CreatedAt.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException("Curated proposal requires an author, use, text, and UTC timestamp source.");
        }
    }

    private static string Write(TirCuratedProposal proposal, string? digest)
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
            writer.WritePropertyName("sourceSubject");
            writer.WriteStartObject();
            writer.WriteString("tirDocumentSha256", proposal.SourceSubject.TirDocumentSha256);
            writer.WriteString("artifactId", proposal.SourceSubject.ArtifactId);
            writer.WriteString("artifactSha256", proposal.SourceSubject.ArtifactSha256);
            writer.WriteString("schemaId", proposal.SourceSubject.SchemaId);
            writer.WritePropertyName("packageCandidate");
            writer.WriteStartObject();
            writer.WriteString("domainId", proposal.SourceSubject.PackageCandidate.DomainId);
            writer.WriteString("packageId", proposal.SourceSubject.PackageCandidate.PackageId);
            writer.WriteString("version", proposal.SourceSubject.PackageCandidate.Version);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteString("semanticAuthorIdentity", proposal.SemanticAuthorIdentity);
            writer.WriteString("declaredUse", proposal.DeclaredUse);
            writer.WriteString("proposalText", proposal.ProposalText);
            writer.WriteString("createdAt", proposal.CreatedAt.ToString(
                "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture));
            writer.WriteString("createdAtSource", proposal.CreatedAtSource);
            if (digest is not null)
            {
                writer.WriteString("proposalSha256", digest);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }
}
